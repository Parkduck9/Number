using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Tutorial (DECISIONS 22, 48; playtest 11-12: strictly in order, one enemy at a time, every move forced): the contact teaches the controls
// over the radio with an on-screen prompt, one step at a time. A step ends only when the player does it (a short check mark, then the next
// step). Each step may have a gate (a barrier opened when the step is done) and a target enemy; shift / parry / dice targets are Guarded
// (no other hit lands) until the taught move is done. Esc skips everything. Plays once per run (static flag survives death reloads; the
// title resets it). Lives in the TUTORIAL scene (a tutorial-only map between STG-00 and STG-01); its exit opens when the tutorial ends.
// File format (stg01_tutorial.txt): "#step N" starts a step, "#check <kind>" what ends it (move, kill, shift, parry, rope, dice),
// "#prompt English | Korean" is its on-screen prompt, "#info English | Korean" adds a line to a panel under it, the rest are radio lines.
public class Stg01Tutorial : MonoBehaviour
{
    [SerializeField] TextAsset lines;
    [SerializeField] GameObject exit; // inactive until the tutorial ends / is skipped / was already done
    [SerializeField] GameObject[] gates;   // per step (null = none): deactivated when that step is done
    [SerializeField] TestEnemy[] targets;  // per step (null = none): the enemy that step is about

    const float StartDelay = 1.5f, DoneHold = 0.6f, OverloadAfterParry = 3f; // (T) seconds
    const float MoveDistance = 3f, JumpSpeed = 2f;                                       // (T) units
    static readonly Color PlateColor = new Color32(0x0B, 0x0F, 0x1E, 0xC0);

    static bool done;
    static int nextStep; // a death reload mid-tutorial resumes at the step it was on, not at step 1

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { done = false; nextStep = 0; } // Domain Reload is off. Not reset by RespawnManager: a death reload must not replay it.
    public static void ResetRun() => ResetStatics(); // TitleScreen: a new game teaches again

    struct Step { public string text, promptEn, promptKo, check, infoEn, infoKo; }

    PlayerController player;
    Rigidbody2D body;
    GameObject ui;
    TMP_Text prompt, info;
    LocalizedText promptLoc, infoLoc;
    RectTransform plate, infoPlate;
    bool skipped, running;

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
            if (tg != null && steps[k].check is "shift" or "parry" or "dice") tg.Guarded = true;
        }
        BuildUi();
        running = true;

        float t = Time.time + StartDelay;
        while (Time.time < t) { if (skipped) { Finish(); yield break; } yield return null; }

        for (int i = nextStep; i < steps.Count; i++)
        {
            nextStep = i;
            while (GameFlow.InputLocked) yield return null;
            if (DialoguePlayer.Instance != null) { DialoguePlayer.Instance.ClearRadio(); DialoguePlayer.Instance.Play(new TextAsset("@mode radio\n" + steps[i].text)); } // a quick player: drop the last step's leftover lines
            ShowPrompt(steps[i], i + 1, steps.Count);

            float startX = player.transform.position.x;
            int parries = PlayerController.ParryCount;
            bool jumped = false, ok = false;
            var target = Target(i);
            var dice = FindFirstObjectByType<LuckyDice>();
            if (steps[i].check == "dice" && LuckyDice.Charges < LuckyDice.ChargesPerRoll) LuckyDice.Charges = LuckyDice.ChargesPerRoll; // one free roll to try it
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
                SetPrompt($"{i + 1}/{steps.Count}  OK!", $"{i + 1}/{steps.Count}  좋아!");
                float hold = Time.time + DoneHold;
                while (Time.time < hold) { if (skipped) { Finish(); yield break; } yield return null; }
            }
        }
        Finish();
    }

    // Runs before PauseMenu (order 1000) and marks Esc as used, so the skip press does not also open the pause menu.
    // Ignored while paused / in dialogue, so Esc closing the pause menu does not skip the tutorial.
    void Update()
    {
        if (!running || skipped || PauseMenu.IsOpen || GameFlow.InputLocked || GameFlow.EscConsumed) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        skipped = true;
        GameFlow.EscConsumedFrame = Time.frameCount;
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
        if (ui != null) Destroy(ui);
        if (exit != null) exit.SetActive(true);
    }

    const string NewLine = "\n";

    static List<Step> Parse(string text)
    {
        var steps = new List<Step>();
        var sb = new StringBuilder();
        Step cur = default;
        bool open = false;
        foreach (var raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.StartsWith("#step"))
            {
                if (open) { cur.text = sb.ToString(); steps.Add(cur); }
                open = true; cur = default; sb.Clear();
            }
            else if (line.StartsWith("#check")) cur.check = line.Substring(6).Trim();
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
            else if (open) sb.AppendLine(line);
        }
        if (open) { cur.text = sb.ToString(); steps.Add(cur); }
        return steps;
    }

    const float MaxW = 820f, PadX = 24f, PadY = 10f, SkipH = 20f; // (T) ref px

    void BuildUi()
    {
        var canvas = UiKit.CreateCanvas("TutorialCanvas", transform, 35); // below the lucky dice HUD (40)
        ui = canvas.gameObject;
        plate = UiKit.Rect("Plate", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(620f, 64f));
        plate.pivot = new Vector2(0.5f, 1f);
        UiKit.FilledImage(plate, PlateColor);
        prompt = UiKit.Label("Prompt", plate, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(MaxW - 2f * PadX, 32f), "", "", 22, UiTheme.Current.text);
        prompt.textWrappingMode = TextWrappingModes.Normal; // playtest 11-12: long prompts ran out of the plate
        prompt.rectTransform.pivot = new Vector2(0.5f, 1f);
        promptLoc = prompt.GetComponent<LocalizedText>();
        UiKit.Label("Skip", plate, new Vector2(0.5f, 0f), new Vector2(0f, PadY * 0.5f + SkipH * 0.5f), new Vector2(600f, SkipH), "Esc: skip tutorial", "Esc: 튜토리얼 건너뛰기", 14, UiTheme.Current.dim);

        infoPlate = UiKit.Rect("Info", canvas.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(620f, 64f));
        infoPlate.pivot = new Vector2(0.5f, 1f);
        UiKit.FilledImage(infoPlate, PlateColor);
        info = UiKit.Label("InfoText", infoPlate, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MaxW - 2f * PadX, 32f), "", "", 18, UiTheme.Current.text);
        info.textWrappingMode = TextWrappingModes.Normal;
        info.alignment = TextAlignmentOptions.Left;
        infoLoc = info.GetComponent<LocalizedText>();
        infoPlate.gameObject.SetActive(false);
    }

    void ShowPrompt(Step s, int n, int count)
    {
        SetPrompt($"{n}/{count}  {s.promptEn}", $"{n}/{count}  {s.promptKo}");
        bool hasInfo = !string.IsNullOrEmpty(s.infoEn);
        infoPlate.gameObject.SetActive(hasInfo);
        if (!hasInfo) return;
        infoLoc.Set(info, s.infoEn, s.infoKo);
        Vector2 size = Fit(info);
        infoPlate.sizeDelta = size + new Vector2(2f * PadX, 2f * PadY);
        info.rectTransform.sizeDelta = size;
        infoPlate.anchoredPosition = new Vector2(0f, plate.anchoredPosition.y - plate.sizeDelta.y - 8f);
    }

    // Plate grows with the prompt (wraps past MaxW): text on top, the Esc line under it.
    void SetPrompt(string en, string ko)
    {
        promptLoc.Set(prompt, en, ko);
        Vector2 size = Fit(prompt);
        prompt.rectTransform.sizeDelta = size;
        prompt.rectTransform.anchoredPosition = new Vector2(0f, -PadY);
        plate.sizeDelta = new Vector2(Mathf.Max(size.x + 2f * PadX, 360f), size.y + 2f * PadY + SkipH);
        if (infoPlate != null && infoPlate.gameObject.activeSelf)
            infoPlate.anchoredPosition = new Vector2(0f, plate.anchoredPosition.y - plate.sizeDelta.y - 8f);
    }

    static Vector2 Fit(TMP_Text t)
    {
        float maxW = MaxW - 2f * PadX;
        Vector2 one = t.GetPreferredValues(t.text, Mathf.Infinity, 0f);
        if (one.x <= maxW) return new Vector2(Mathf.Ceil(one.x) + 2f, Mathf.Ceil(one.y));
        Vector2 wrapped = t.GetPreferredValues(t.text, maxW, 0f);
        return new Vector2(maxW, Mathf.Ceil(wrapped.y));
    }
}
