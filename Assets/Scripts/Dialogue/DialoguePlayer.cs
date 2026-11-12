using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Radio: non-blocking corner subtitle, lines auto-advance. Talk: blocking scene (GameFlow.InputLocked), advance with click / E.
// The UI is built from code. ALL placeholder styling lives in the constants below.
public class DialoguePlayer : MonoBehaviour
{
    public static DialoguePlayer Instance { get; private set; }

    [SerializeField] DialogueSpeakers speakers;
    [SerializeField] TMP_FontAsset font;

    // ---- Style (placeholder; restyle here) ----
    static readonly Vector2 RefResolution = new Vector2(960f, 540f);
    static readonly Color BoxColor = new Color32(0x0B, 0x0F, 0x1E, 0xE6);
    static readonly Color BorderColor = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
    static readonly Color TextColor = Color.white;
    const int BodySize = 24, NameSize = 24; // multiples of 12 at 960x540
    const float TypeSpeed = 40f;            // characters per second
    const float RadioHold = 1.2f, RadioPerChar = 0.06f;
    const float TalkInputGuard = 0.25f;

    class Box
    {
        public GameObject root;
        public Image portrait, next;
        public TMP_Text nameText, body;
    }

    Sprite[] nextFrames; // dialogue_next_2f: 2 frames, 2 fps blink, shown when a talk line finished typing

    Box radioBox, talkBox;
    readonly Queue<DialogueLine> radioQueue = new Queue<DialogueLine>();
    readonly Queue<DialogueLine> talkQueue = new Queue<DialogueLine>();

    DialogueLine radioLine;
    float radioTime, radioHoldTime;
    DialogueLine talkLine;
    float talkTime, talkStartedAt;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        BuildUi();
    }

    void OnEnable() { Loc.Changed += OnLanguageChanged; }
    void OnDisable() { Loc.Changed -= OnLanguageChanged; }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        GameFlow.InputLocked = false;
    }

    // Language toggled mid-line: redraw the lines on screen in the new language, keeping the typewriter position.
    void OnLanguageChanged()
    {
        if (radioLine != null) Reshow(radioBox, radioLine);
        if (talkLine != null) Reshow(talkBox, talkLine);
    }

    void Reshow(Box box, DialogueLine line)
    {
        int visible = box.body.maxVisibleCharacters;
        Show(box, line);
        box.body.maxVisibleCharacters = visible;
    }

    public void Play(TextAsset file)
    {
        var script = DialogueScript.Parse(file.text, file.name);
        foreach (var l in script.lines) (script.talk ? talkQueue : radioQueue).Enqueue(l);
    }

    void Update()
    {
        if (PauseMenu.IsOpen) return; // radio and talk freeze while paused
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) Loc.SetKorean(!Loc.Korean);
#endif
        UpdateRadio();
        UpdateTalk();
    }

    // ---- Radio ----
    // Drops the current and queued radio lines (Stg01Tutorial: the next step's lines replace a quick player's leftovers).
    public void ClearRadio()
    {
        radioQueue.Clear();
        radioLine = null;
        if (radioBox != null) radioBox.root.SetActive(false);
    }

    void UpdateRadio()
    {
        if (radioLine == null && radioQueue.Count > 0)
        {
            radioLine = radioQueue.Dequeue();
            radioTime = 0f;
            Show(radioBox, radioLine);
            radioHoldTime = radioLine.duration >= 0f ? radioLine.duration : RadioHold + radioLine.Text.Length * RadioPerChar;
        }
        if (radioLine == null) return;
        radioTime += Time.unscaledDeltaTime;
        radioBox.body.maxVisibleCharacters = Mathf.FloorToInt(radioTime * TypeSpeed);
        if (radioTime >= radioHoldTime) { radioLine = null; radioBox.root.SetActive(false); }
    }

    // ---- Talk ----
    void UpdateTalk()
    {
        if (talkLine == null)
        {
            if (talkQueue.Count == 0) return;
            talkLine = talkQueue.Dequeue();
            talkStartedAt = Time.unscaledTime;
            talkTime = 0f;
            GameFlow.InputLocked = true;
            Show(talkBox, talkLine);
            return;
        }
        talkTime += Time.unscaledDeltaTime;
        int total = talkBox.body.textInfo.characterCount;
        bool typing = Mathf.FloorToInt(talkTime * TypeSpeed) < total;
        talkBox.body.maxVisibleCharacters = typing ? Mathf.FloorToInt(talkTime * TypeSpeed) : int.MaxValue;
        if (talkBox.next != null)
        {
            talkBox.next.enabled = !typing;
            talkBox.next.sprite = nextFrames[(int)(Time.unscaledTime * 2f) & 1];
        }

        bool advance = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                       (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (!advance || Time.unscaledTime - talkStartedAt < TalkInputGuard) return;
        if (typing) { talkTime = 9999f; return; } // first press completes the line

        if (talkQueue.Count > 0)
        {
            talkLine = talkQueue.Dequeue();
            talkTime = 0f;
            Show(talkBox, talkLine);
        }
        else
        {
            talkLine = null;
            talkBox.root.SetActive(false);
            GameFlow.InputLocked = false;
        }
    }

    // ---- UI ----
    void Show(Box box, DialogueLine line)
    {
        var s = speakers != null ? speakers.Find(line.speaker) : null;
        box.root.SetActive(true);
        box.body.text = line.Text;
        box.body.maxVisibleCharacters = 0;
        box.body.ForceMeshUpdate();
        box.nameText.text = s != null ? (DialogueLanguage.Korean ? s.nameKo : s.nameEn) : line.speaker;
        Color c = s != null ? s.color : Color.gray;
        box.nameText.color = c;
        box.portrait.sprite = s != null ? (s.portrait != null ? s.portrait : LoadPortrait(s.id)) : null;
        box.portrait.color = box.portrait.sprite != null ? Color.white : c; // placeholder block
    }

    // Fallback when the speaker row has no portrait assigned: Assets/Resources/Portraits/<id>.png (100x100, shown at 50 ref px radio = 1x / 100 ref px talk = 2x on 1080p).
    readonly Dictionary<string, Sprite> portraitCache = new Dictionary<string, Sprite>();
    Sprite LoadPortrait(string id)
    {
        if (!portraitCache.TryGetValue(id, out var sp)) portraitCache[id] = sp = Resources.Load<Sprite>("Portraits/" + id);
        return sp;
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("DialogueCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = RefResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // radio: small, top-left corner
        radioBox = BuildBox(canvasGo.transform, "Radio", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(480f, 84f), 50f, true); // portrait 50 = the 100x100 art at exactly 1x on 1080p
        // talk: wide, bottom of screen
        talkBox = BuildBox(canvasGo.transform, "Talk", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(912f, 132f), 100f, false); // portrait 100 = the 100x100 portrait art, exact 2x on 1080p (ref 960x540)
        radioBox.root.SetActive(false);
        talkBox.root.SetActive(false);
    }

    Box BuildBox(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, float portraitSize, bool compact)
    {
        // Pixel UI kit skin (2 ref px per art pixel): sliced box, portrait frame (56 art px around the 50 px portrait art), nameplate, next arrow.
        Sprite boxArt = DraftHud.Ui("dialogue_box_9s"), frameArt = DraftHud.Ui("portrait_frame"), plateArt = DraftHud.Ui("dialogue_nameplate_9s");
        bool skin = boxArt != null;
        float frameSize = portraitSize * 1.12f;
        if (skin) size.y = frameSize + 36f; // 16 ref px border above and below, 2 ref px fill inset

        var b = new Box();
        b.root = NewRect(name, parent, anchor, anchor, anchor, pos, size);
        var border = b.root.AddComponent<Image>();
        border.color = BorderColor;
        border.raycastTarget = false;
        UiKit.Slice(border, boxArt);
        var fill = NewRect("Fill", b.root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
        if (!skin)
        {
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = BoxColor;
            fillImg.raycastTarget = false;
        }

        float portraitX = skin ? 18f + (frameSize - portraitSize) * 0.5f : 12f;
        var pr = NewRect("Portrait", fill.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(portraitX, 0f), new Vector2(portraitSize, portraitSize));
        b.portrait = pr.AddComponent<Image>();
        b.portrait.raycastTarget = false;
        if (skin && frameArt != null)
        {
            var fr = NewRect("PortraitFrame", pr.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(frameSize, frameSize));
            var frImg = fr.AddComponent<Image>();
            frImg.sprite = frameArt;
            frImg.raycastTarget = false;
        }

        float left = skin ? portraitX + frameSize + 14f : 24f + portraitSize;
        float nameH = NameSize + 6f;
        b.body = NewText("Body", fill.transform, BodySize, left, compact ? 0f : nameH, false);
        if (skin)
        {
            var bodyRt = b.body.rectTransform;
            bodyRt.offsetMin = new Vector2(left, 14f);
            bodyRt.offsetMax = new Vector2(compact ? -20f : -44f, -14f - (compact || plateArt != null ? 0f : nameH));
        }
        if (skin && !compact && plateArt != null)
        {
            // nameplate straddles the top edge of the box; the name text sits inside it
            var plate = NewRect("Nameplate", fill.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(left - 12f, -10f), new Vector2(240f, 40f));
            var plateImg = plate.AddComponent<Image>();
            plateImg.raycastTarget = false;
            UiKit.Slice(plateImg, plateArt);
            b.nameText = NewText("Name", plate.transform, NameSize, 0f, 0f, true);
            var nrt = b.nameText.rectTransform;
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one; nrt.pivot = new Vector2(0.5f, 0.5f);
            nrt.offsetMin = new Vector2(18f, 4f); nrt.offsetMax = new Vector2(-18f, -4f);
            b.nameText.alignment = TextAlignmentOptions.MidlineLeft;
        }
        else b.nameText = NewText("Name", fill.transform, NameSize, left, nameH, true);
        if (compact) b.nameText.gameObject.SetActive(false); // radio: portrait + subtitle only

        if (skin && !compact)
        {
            nextFrames = DraftHud.UiFrames("dialogue_next_2f", 2);
            if (nextFrames != null)
            {
                var nx = NewRect("Next", fill.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 18f), new Vector2(16f, 16f));
                b.next = nx.AddComponent<Image>();
                b.next.raycastTarget = false;
                b.next.enabled = false;
            }
        }
        return b;
    }

    TMP_Text NewText(string name, Transform parent, int size, float left, float nameH, bool isName)
    {
        var go = NewRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var rt = (RectTransform)go.transform;
        rt.offsetMin = new Vector2(left, 8f);
        rt.offsetMax = new Vector2(-12f, isName ? -8f : -8f - nameH);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = TextColor;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.alignment = TextAlignmentOptions.TopLeft;
        if (isName)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -8f - nameH);
            rt.offsetMax = new Vector2(-12f, -8f);
        }
        return t;
    }
    static GameObject NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return go;
    }
}


