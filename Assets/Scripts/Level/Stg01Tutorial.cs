using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

// Tutorial (DECISIONS 22, 48; playtest 11-12: strictly in order, one enemy at a time, every move forced): the contact teaches the controls
// one step at a time. Each step opens with a FROZEN centred card (timeScale 0 + GameFlow.InputLocked, like the pause menu) holding the step title,
// the contact's portrait + all of the step's radio lines, the controls line, the optional dice list and an optional looping clip; a click
// closes it. Afterwards the goal sits top-left under the face HUD until the player does it (a short "OK!", then the next card). A step ends
// only when the player does it. Each step may have a gate (a barrier opened when the step is done) and a target enemy; shift / parry / dice
// targets are Guarded (no other hit lands) until the taught move is done. Esc skips everything (also while a card is up). Plays once per run
// (static flag survives death reloads; the title resets it). Lives in the TUTORIAL scene; its exit opens when the tutorial ends.
// File format (stg01_tutorial.txt): "#step N" starts a step, "#check <kind>" what ends it (move, kill, shift, parry, rope, dice),
// "#title English | Korean" the card header, "#clip <name>" Assets/StreamingAssets/Tutorial/<name>.mp4 (optional, no file = no video),
// "#prompt English | Korean" the controls / goal line, "#info English | Korean" adds a line to the list in the card, the rest are the
// card's text lines (speaker | ko | en | seconds; the seconds are unused here).
public class Stg01Tutorial : MonoBehaviour
{
    [SerializeField] TextAsset lines;
    [SerializeField] GameObject exit; // inactive until the tutorial ends / is skipped / was already done
    [SerializeField] GameObject[] gates;   // per step (null = none): deactivated when that step is done
    [SerializeField] TestEnemy[] targets;  // per step (null = none): the enemy that step is about

    const float StartDelay = 1.5f, DoneHold = 0.6f, OverloadAfterParry = 3f; // (T) seconds
    const float MoveDistance = 3f, JumpSpeed = 2f;                                       // (T) units
    const float CardInputGuard = 0.6f, VideoWait = 2f;                                   // (T) unscaled seconds
    static readonly Color PlateColor = new Color32(0x0B, 0x0F, 0x1E, 0xC0);
    static readonly Color Accent = new Color32(0xFF, 0xD2, 0x5A, 0xFF);

    static bool done;
    static int nextStep; // a death reload mid-tutorial resumes at the step it was on, not at step 1

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { done = false; nextStep = 0; } // Domain Reload is off. Not reset by RespawnManager: a death reload must not replay it.
    public static void ResetRun() => ResetStatics(); // TitleScreen: a new game teaches again

    struct Step { public string bodyEn, bodyKo, portraitId, titleEn, titleKo, clip, promptEn, promptKo, check, infoEn, infoKo; }

    PlayerController player;
    Rigidbody2D body;
    GameObject ui;
    bool skipped, running;
    bool frozen, ownLock; // frozen = timeScale held at 0, ownLock = we set GameFlow.InputLocked

    // card
    GameObject cardRoot;
    RectTransform cardRect;
    TMP_Text cardHeader, cardBody, cardPrompt, cardInfo;
    LocalizedText headerLoc, bodyLoc, promptLoc, infoLoc;
    Image portrait;
    RawImage video;
    bool hasInfo, videoOn, videoWait, videoError;
    VideoPlayer vp;
    RenderTexture rt;

    // goal plate (top-left, under the face HUD)
    RectTransform goalPlate;
    TMP_Text goalTitle, goalText;
    LocalizedText goalTitleLoc, goalTextLoc;

    IEnumerator Start()
    {
        if (done || lines == null) { Finish(); yield break; }
        player = FindFirstObjectByType<PlayerController>();
        if (player == null) { Finish(); yield break; } // (its own map now: a respawn at a later checkpoint must not skip it, so no start-x check)
        body = player.GetComponent<Rigidbody2D>();
        var steps = Parse(lines.text);
        for (int k = 0; k < steps.Count; k++)
        {
            var tg = Target(k);
            if (k < nextStep) { OpenGate(k); if (tg != null) tg.gameObject.SetActive(false); continue; } // a death reload: steps already done stay done
            if (tg != null && steps[k].check is "shift" or "parry") tg.Guarded = true;
        }
        BuildUi();
        running = true;

        float t = Time.time + StartDelay;
        while (Time.time < t) { if (skipped) { Finish(); yield break; } yield return null; }

        for (int i = nextStep; i < steps.Count; i++)
        {
            nextStep = i;
            while (GameFlow.InputLocked) yield return null;
            yield return Card(steps[i], i + 1, steps.Count);
            if (skipped) { Finish(); yield break; }
            ShowGoal(steps[i], i + 1, steps.Count);

            float startX = player.transform.position.x;
            int parries = PlayerController.ParryCount;
            bool jumped = false, ok = false;
            var target = Target(i);
            var dice = FindFirstObjectByType<LuckyDice>();
            if (steps[i].check != "dice") LuckyDice.Charges = 0; // no surprise auto-roll before its step
            else LuckyDice.Charges = LuckyDice.ChargesPerRoll; // playtest 11-13 (user): full after the card, so it rolls by itself right away
            while (true)
            {
                if (skipped) { Finish(); yield break; }
                if (body != null && body.linearVelocity.y > JumpSpeed) jumped = true;
                var st = player.CurrentState;
                ok = steps[i].check switch
                {
                    "move" => jumped && Mathf.Abs(player.transform.position.x - startX) >= MoveDistance,
                    "kill" => target == null ? st == PlayerController.State.Attack : target.IsDown,
                    "parry" => PlayerController.ParryCount > parries,
                    "shift" => st == PlayerController.State.Issen,
                    "rope" => st == PlayerController.State.Grapple,
                    "dice" => dice != null && dice.Busy,
                    _ => true,
                };
                if (ok) break;
                yield return null;
            }
            Release(steps[i].check, target);
            OpenGate(i);
            {
                SetGoalText("OK!", "좋아!");
                float hold = Time.time + DoneHold;
                while (Time.time < hold) { if (skipped) { Finish(); yield break; } yield return null; }
            }
        }
        Finish();
    }

    // The step card: freeze, (optionally) wait for the clip, show, wait for a click, release the mouse, unfreeze.
    IEnumerator Card(Step s, int n, int count)
    {
        Freeze();
        PrepareClip(s.clip);
        float t = Time.unscaledTime;
        while (videoWait && !skipped)
        {
            if (videoError) { videoWait = false; vp.Stop(); }
            else if (vp.isPrepared) { vp.Play(); videoOn = true; videoWait = false; }
            else if (Time.unscaledTime - t > VideoWait) { videoWait = false; vp.Stop(); } // no file / slow: the card just has no clip
            else yield return null;
        }
        if (skipped) yield break;

        ShowCard(s, n, count);
        float opened = Time.unscaledTime;
        while (!skipped)
        {
            if (Time.unscaledTime - opened >= CardInputGuard)
            {
                var m = Mouse.current; var kb = Keyboard.current;
                if ((m != null && m.leftButton.wasPressedThisFrame) || (kb != null && (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))) break;
            }
            yield return null;
        }
        HideCard();
        if (skipped) yield break;

        // The closing click must not become an attack: stay frozen until the button is up (PlayerInputReader only sees WasPressedThisFrame, nothing buffered).
        while (Mouse.current != null && Mouse.current.leftButton.isPressed && !skipped) yield return null;
        frozen = false;
        Time.timeScale = 1f;
        yield return null; // one more locked frame: LuckyDice drops a press it kept during a hit-stop
        Unfreeze();
    }

    void Freeze()
    {
        frozen = true;
        ownLock = true;
        GameFlow.InputLocked = true;
        Time.timeScale = 0f;
    }

    void Unfreeze()
    {
        if (frozen) { frozen = false; Time.timeScale = 1f; }
        if (ownLock) { ownLock = false; GameFlow.InputLocked = false; }
    }

    // Runs before PauseMenu (order 1000) and marks Esc as used, so the skip press does not also open the pause menu.
    // Ignored while paused / in dialogue (a lock that is not ours), so Esc closing the pause menu does not skip the tutorial.
    void Update()
    {
        if (!running || skipped || PauseMenu.IsOpen || GameFlow.EscConsumed) return;
        if (GameFlow.InputLocked && !frozen) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        skipped = true;
        GameFlow.EscConsumedFrame = Time.frameCount;
        StopAllCoroutines(); // finish here, not in the coroutine: Esc must unfreeze even if the coroutine died on an exception
        Finish();
    }

    void LateUpdate()
    {
        if (frozen && Time.timeScale != 0f) Time.timeScale = 0f; // the parry hit-stop end sets it back to 1 (same as PauseMenu)
        if (relayout) // after every Loc.Changed handler: the labels re-subscribe on SetActive(true), so they refresh after OnLanguage
        {
            relayout = false;
            if (cardRoot != null && cardRoot.activeSelf) LayoutCard();
            if (goalPlate != null && goalPlate.gameObject.activeSelf) LayoutGoal();
        }
    }

    void OnDisable()
    {
        Unfreeze();
        Loc.Changed -= OnLanguage;
    }

    void OnDestroy()
    {
        Unfreeze();
        ReleaseVideo();
    }

    TestEnemy Target(int i) => targets != null && i < targets.Length ? targets[i] : null;
    void OpenGate(int i) { if (gates != null && i < gates.Length && gates[i] != null) gates[i].SetActive(false); }

    // The taught move landed: the target stops guarding. Issen passes through it, so it falls as if cut; the android is left overloaded
    // (one more strike finishes it); a dice skill hits it on its own.
    void Release(string check, TestEnemy t)
    {
        if (t == null || t.IsDown) return;
        t.Guarded = false;
        if (check == "shift") t.ReceivePlayerHit(new HitInfo { Damage = 1, SourcePosition = player.transform.position, Pierce = true });
        else if (check == "parry") t.Overload(OverloadAfterParry);
    }

    void Finish()
    {
        done = true;
        if (gates != null) foreach (var g in gates) if (g != null) g.SetActive(false);
        if (targets != null) foreach (var t in targets) if (t != null) t.Guarded = false;
        running = false;
        Unfreeze();
        ReleaseVideo();
        if (ui != null) Destroy(ui);
        if (exit != null) exit.SetActive(true);
    }

    // ------------------------------------------------------------------ parsing

    const string NewLine = "\n";

    static List<Step> Parse(string text)
    {
        var steps = new List<Step>();
        var en = new StringBuilder();
        var ko = new StringBuilder();
        Step cur = default;
        bool open = false;
        foreach (var raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.StartsWith("#step"))
            {
                if (open) { cur.bodyEn = en.ToString(); cur.bodyKo = ko.ToString(); steps.Add(cur); }
                open = true; cur = default; en.Clear(); ko.Clear();
            }
            else if (line.StartsWith("#check")) cur.check = line.Substring(6).Trim();
            else if (line.StartsWith("#clip")) cur.clip = line.Substring(5).Trim();
            else if (line.StartsWith("#title"))
            {
                var p = line.Substring(6).Split('|');
                cur.titleEn = p[0].Trim();
                cur.titleKo = p.Length > 1 ? p[1].Trim() : cur.titleEn;
            }
            else if (line.StartsWith("#info"))
            {
                var p = line.Substring(5).Split('|');
                cur.infoEn = (cur.infoEn == null ? "" : cur.infoEn + NewLine) + p[0].Trim();
                cur.infoKo = (cur.infoKo == null ? "" : cur.infoKo + NewLine) + (p.Length > 1 ? p[1].Trim() : p[0].Trim());
            }
            else if (line.StartsWith("#prompt"))
            {
                var p = line.Substring(7).Split('|');
                cur.promptEn = p[0].Trim();
                cur.promptKo = p.Length > 1 ? p[1].Trim() : cur.promptEn;
            }
            else if (open && line.Length > 0 && line[0] != '#' && line[0] != '@')
            {
                var p = line.Split('|');
                if (p.Length < 3) continue;
                string who = p[0].Trim();
                if (cur.portraitId == null) cur.portraitId = who;
                if (en.Length > 0) { en.Append(NewLine); ko.Append(NewLine); }
                if (who != cur.portraitId) // someone else speaking (the player, yudo): show the name
                {
                    var sp = DialoguePlayer.Instance != null ? DialoguePlayer.Instance.FindSpeaker(who) : null;
                    en.Append(sp != null && !string.IsNullOrEmpty(sp.nameEn) ? sp.nameEn : who).Append(": ");
                    ko.Append(sp != null && !string.IsNullOrEmpty(sp.nameKo) ? sp.nameKo : who).Append(": ");
                }
                en.Append(p[2].Trim());
                ko.Append(p[1].Trim());
            }
        }
        if (open) { cur.bodyEn = en.ToString(); cur.bodyKo = ko.ToString(); steps.Add(cur); }
        return steps;
    }

    // ------------------------------------------------------------------ UI (reference 960x540)

    const float CardW = 640f, CardPadX = 30f, CardPadTop = 24f, CardPadBot = 22f, PortraitSz = 64f, VidW = 352f, VidH = 198f, CardMaxH = 510f;
    const float GoalX = 16f, GoalY = 132f, GoalW = 400f, GoalPad = 10f; // under the face HUD (96 px face at (16,16) + 112 px bezel)

    void BuildUi()
    {
        var t = UiTheme.Current;
        var canvas = UiKit.CreateCanvas("TutorialCanvas", transform, 35); // below the lucky dice HUD (40)
        ui = canvas.gameObject;

        // --- card
        var rootRt = UiKit.Stretch("Card", canvas.transform);
        cardRoot = rootRt.gameObject;
        UiKit.FilledImage(UiKit.Stretch("Dim", rootRt), t.dimmer);
        var fill = UiKit.Frame("Panel", rootRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardW, 300f), out _);
        cardRect = (RectTransform)fill.parent;

        cardHeader = Text("Header", fill, 24, Accent, TextAlignmentOptions.Center, out headerLoc);
        var vr = UiKit.Rect("Video", fill, new Vector2(0f, 1f), Vector2.zero, new Vector2(VidW, VidH));
        video = vr.gameObject.AddComponent<RawImage>();
        video.raycastTarget = false;
        var pr = UiKit.Rect("Portrait", fill, new Vector2(0f, 1f), Vector2.zero, new Vector2(PortraitSz, PortraitSz));
        portrait = pr.gameObject.AddComponent<Image>();
        portrait.raycastTarget = false;
        portrait.preserveAspect = true;
        cardBody = Text("Body", fill, 20, t.text, TextAlignmentOptions.TopLeft, out bodyLoc);
        cardPrompt = Text("Prompt", fill, 20, Accent, TextAlignmentOptions.TopLeft, out promptLoc);
        cardInfo = Text("Info", fill, 18, t.text, TextAlignmentOptions.TopLeft, out infoLoc);
        var footer = UiKit.Label("Footer", fill, new Vector2(0f, 1f), Vector2.zero, new Vector2(CardW - 2f * CardPadX, 18f), "Click / E: continue    Esc: skip tutorial", "클릭 / E: 계속    Esc: 튜토리얼 건너뛰기", 14, t.dim);
        footerRt = footer.rectTransform;
        cardRoot.SetActive(false);

        // --- goal plate
        goalPlate = UiKit.Rect("Goal", canvas.transform, new Vector2(0f, 1f), Vector2.zero, new Vector2(GoalW, 80f));
        goalPlate.pivot = new Vector2(0f, 1f);
        goalPlate.anchoredPosition = new Vector2(GoalX, -GoalY);
        UiKit.FilledImage(goalPlate, PlateColor);
        goalTitle = Text("GoalTitle", goalPlate, 16, Accent, TextAlignmentOptions.TopLeft, out goalTitleLoc);
        goalText = Text("GoalText", goalPlate, 18, t.text, TextAlignmentOptions.TopLeft, out goalTextLoc);
        goalSkip = UiKit.Label("GoalSkip", goalPlate, new Vector2(0f, 1f), Vector2.zero, new Vector2(GoalW - 2f * GoalPad, 16f), "Esc: skip tutorial", "Esc: 튜토리얼 건너뛰기", 12, t.dim);
        goalSkip.alignment = TextAlignmentOptions.TopLeft;
        goalPlate.gameObject.SetActive(false);

        Loc.Changed += OnLanguage;
    }

    RectTransform footerRt;
    TMP_Text goalSkip;

    static TMP_Text Text(string name, Transform parent, int size, Color color, TextAlignmentOptions align, out LocalizedText loc)
    {
        var tmp = UiKit.Label(name, parent, new Vector2(0f, 1f), Vector2.zero, new Vector2(100f, 20f), "", "", size, color);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.alignment = align;
        loc = tmp.GetComponent<LocalizedText>();
        return tmp;
    }

    static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static float TextH(TMP_Text t, float w) => Mathf.Ceil(t.GetPreferredValues(t.text, w, 0f).y);

    void ShowCard(Step s, int n, int count)
    {
        headerLoc.Set(cardHeader, $"{n}/{count} · {s.titleEn}", $"{n}/{count} · {s.titleKo}");
        bodyLoc.Set(cardBody, s.bodyEn, s.bodyKo);
        promptLoc.Set(cardPrompt, s.promptEn, s.promptKo);
        hasInfo = !string.IsNullOrEmpty(s.infoEn);
        if (hasInfo) infoLoc.Set(cardInfo, s.infoEn, s.infoKo);
        cardInfo.gameObject.SetActive(hasInfo);

        var sp = DialoguePlayer.Instance != null ? DialoguePlayer.Instance.FindSpeaker(s.portraitId) : null;
        portrait.sprite = DialoguePlayer.Instance != null && s.portraitId != null ? DialoguePlayer.Instance.PortraitFor(s.portraitId) : null;
        portrait.color = portrait.sprite != null ? Color.white : (sp != null ? sp.color : Color.gray); // placeholder block like the radio box
        cardRoot.SetActive(true);
        LayoutCard();
    }

    void HideCard()
    {
        cardRoot.SetActive(false);
        if (vp != null && videoOn) vp.Stop();
        videoOn = false;
    }

    // Content stacks top-down; the card shrinks when there is no clip / dice list, and scales down if it would not fit the screen.
    void LayoutCard()
    {
        float inner = CardW - 2f * CardPadX;
        float y = CardPadTop;
        Place(cardHeader.rectTransform, CardPadX, y, inner, 30f);
        y += 38f;
        video.gameObject.SetActive(videoOn);
        if (videoOn)
        {
            Place(video.rectTransform, (CardW - VidW) * 0.5f, y, VidW, VidH);
            y += VidH + 12f;
        }
        float bw = inner - PortraitSz - 14f;
        float bh = TextH(cardBody, bw);
        Place(portrait.rectTransform, CardPadX, y, PortraitSz, PortraitSz);
        Place(cardBody.rectTransform, CardPadX + PortraitSz + 14f, y, bw, bh);
        y += Mathf.Max(PortraitSz, bh) + 14f;
        float ph = TextH(cardPrompt, inner);
        Place(cardPrompt.rectTransform, CardPadX, y, inner, ph);
        y += ph + 10f;
        if (hasInfo)
        {
            float ih = TextH(cardInfo, inner);
            Place(cardInfo.rectTransform, CardPadX, y, inner, ih);
            y += ih + 10f;
        }
        Place(footerRt, CardPadX, y + 4f, inner, 18f);
        footerRt.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
        y += 22f + CardPadBot;
        cardRect.sizeDelta = new Vector2(CardW + 4f, y + 4f);
        float k = Mathf.Min(1f, CardMaxH / (y + 4f));
        cardRect.localScale = new Vector3(k, k, 1f);
    }

    void ShowGoal(Step s, int n, int count)
    {
        goalTitleLoc.Set(goalTitle, $"Goal {n}/{count}", $"목표 {n}/{count}");
        goalTextLoc.Set(goalText, s.promptEn, s.promptKo);
        goalPlate.gameObject.SetActive(true);
        LayoutGoal();
    }

    void SetGoalText(string en, string ko)
    {
        goalTextLoc.Set(goalText, en, ko);
        LayoutGoal();
    }

    void LayoutGoal()
    {
        float w = GoalW - 2f * GoalPad;
        float y = GoalPad - 2f;
        Place(goalTitle.rectTransform, GoalPad, y, w, 20f);
        y += 20f;
        float h = TextH(goalText, w);
        Place(goalText.rectTransform, GoalPad, y, w, h);
        y += h + 4f;
        Place(goalSkip.rectTransform, GoalPad, y, w, 16f);
        y += 16f + GoalPad - 2f;
        goalPlate.sizeDelta = new Vector2(GoalW, y);
    }

    bool relayout;
    void OnLanguage() => relayout = true;

    // ------------------------------------------------------------------ clip (VideoPlayer -> RenderTexture -> RawImage; runs on unscaled time so it plays under timeScale 0)

    void PrepareClip(string clip)
    {
        videoOn = videoWait = videoError = false;
        if (string.IsNullOrEmpty(clip)) return;
        if (vp == null)
        {
            vp = ui.AddComponent<VideoPlayer>();
            vp.playOnAwake = false;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.isLooping = true;
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            vp.source = VideoSource.Url;
            rt = new RenderTexture(480, 270, 0);
            vp.targetTexture = rt;
            video.texture = rt;
            vp.errorReceived += OnVideoError;
        }
        vp.Stop();
        vp.url = Application.streamingAssetsPath + "/Tutorial/" + clip + ".mp4"; // a URL on WebGL: plain concat, not Path.Combine
        vp.Prepare();
        videoWait = true;
    }

    void OnVideoError(VideoPlayer source, string message) { videoError = true; }

    void ReleaseVideo()
    {
        if (vp != null) { vp.errorReceived -= OnVideoError; vp.Stop(); }
        if (rt != null) { rt.Release(); Destroy(rt); rt = null; }
    }
}
