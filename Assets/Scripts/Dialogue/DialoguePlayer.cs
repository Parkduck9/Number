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
        public Image portrait;
        public TMP_Text nameText, body;
    }

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

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        GameFlow.InputLocked = false;
    }

    public void Play(TextAsset file)
    {
        var script = DialogueScript.Parse(file.text, file.name);
        foreach (var l in script.lines) (script.talk ? talkQueue : radioQueue).Enqueue(l);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame) DialogueLanguage.Korean = !DialogueLanguage.Korean;
        UpdateRadio();
        UpdateTalk();
    }

    // ---- Radio ----
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
        box.portrait.sprite = s != null ? s.portrait : null;
        box.portrait.color = box.portrait.sprite != null ? Color.white : c; // placeholder block
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
        radioBox = BuildBox(canvasGo.transform, "Radio", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(480f, 84f), 60f, true);
        // talk: wide, bottom of screen
        talkBox = BuildBox(canvasGo.transform, "Talk", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(912f, 132f), 96f, false);
        radioBox.root.SetActive(false);
        talkBox.root.SetActive(false);
    }

    Box BuildBox(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, float portraitSize, bool compact)
    {
        var b = new Box();
        b.root = NewRect(name, parent, anchor, anchor, anchor, pos, size);
        var border = b.root.AddComponent<Image>();
        border.color = BorderColor;
        border.raycastTarget = false;
        var fill = NewRect("Fill", b.root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -4f));
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = BoxColor;
        fillImg.raycastTarget = false;

        var pr = NewRect("Portrait", fill.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(portraitSize, portraitSize));
        b.portrait = pr.AddComponent<Image>();
        b.portrait.raycastTarget = false;

        float left = 24f + portraitSize;
        float nameH = NameSize + 6f;
        b.nameText = NewText("Name", fill.transform, NameSize, left, nameH, true);
        b.body = NewText("Body", fill.transform, BodySize, left, compact ? 0f : nameH, false);
        if (compact) b.nameText.gameObject.SetActive(false); // radio: portrait + subtitle only
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


