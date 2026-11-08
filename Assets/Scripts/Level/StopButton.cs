using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The AI stop button (GDD ch.10, both endings [confirmed]). Press E near it -> choose END-01 or END-02 -> the ending
// plays through the dialogue system (talk mode) -> back to the title.
// Pick: 1 / 2 keys (Esc cancels). Input stays locked while choosing.
// The ending lines are PLACEHOLDER text files (Assets/Data/Dialogue/ending_0x_placeholder.txt) for the user to rewrite.
public class StopButton : MonoBehaviour
{
    [SerializeField] TextAsset ending01; // press the stop button
    [SerializeField] TextAsset ending02; // press it, but set the lucky number yourself
    [SerializeField] TMP_FontAsset font;
    [SerializeField] float interactRange = 2.5f;

    // ---- Style (placeholder; restyle here) ----
    static readonly Color BoxColor = new Color32(0x0B, 0x0F, 0x1E, 0xF0);
    static readonly Color BorderColor = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
    static readonly Color Cyan = new Color32(0x4C, 0xE0, 0xFF, 0xFF);
    static readonly Color Gold = new Color32(0xFF, 0xD2, 0x4A, 0xFF);
    const int TextSize = 24; // multiple of 12 at 960x540

    enum Phase { Idle, Choosing, Playing, Done }
    Phase phase;
    PlayerController player;
    GameObject panel;
    GameObject prompt;
    bool lockedLastFrame;

    void Start() { player = FindFirstObjectByType<PlayerController>(); }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        switch (phase)
        {
            case Phase.Idle:
                bool near = player != null && !GameFlow.InputLocked &&
                            Vector2.Distance(player.transform.position, transform.position) <= interactRange;
                SetPrompt(near);
                // !lockedLastFrame: the E that closes a talk scene must not also open this menu
                if (near && !lockedLastFrame && kb.eKey.wasPressedThisFrame) OpenChoice();
                break;
            case Phase.Choosing:
                if (kb.digit1Key.wasPressedThisFrame) Play(ending01);
                else if (kb.digit2Key.wasPressedThisFrame) Play(ending02);
                else if (kb.escapeKey.wasPressedThisFrame) { panel.SetActive(false); GameFlow.InputLocked = false; phase = Phase.Idle; }
                break;
            case Phase.Playing:
                // Input stays locked from the choice through the talk scene; DialoguePlayer clears it after the last line.
                if (!GameFlow.InputLocked) Finish();
                break;
        }
        lockedLastFrame = GameFlow.InputLocked;
    }

    void OpenChoice()
    {
        if (panel == null) BuildUi();
        panel.SetActive(true);
        GameFlow.InputLocked = true;
        phase = Phase.Choosing;
        SetPrompt(false);
    }

    void Play(TextAsset file)
    {
        panel.SetActive(false);
        // Keep InputLocked set (no unlocked frame before DialoguePlayer picks the lines up next Update).
        // Only a talk-mode file with lines will ever clear it, so anything else goes straight to the title.
        DialogueScript s = file != null ? DialogueScript.Parse(file.text, file.name) : null;
        bool playable = s != null && s.talk && s.lines.Count > 0 && DialoguePlayer.Instance != null;
        if (!playable) { Debug.LogWarning("[StopButton] Ending file missing/not talk mode, or no DialoguePlayer; going straight to the title."); Finish(); return; }
        phase = Phase.Playing;
        DialoguePlayer.Instance.Play(file);
    }

    void Finish()
    {
        phase = Phase.Done;
        GameFlow.InputLocked = false; // if the title scene is missing we stay here, so give control back
        StageFlow.LoadTitle();
    }

    void SetPrompt(bool on)
    {
        if (prompt == null) { if (!on) return; BuildUi(); }
        prompt.SetActive(on);
    }

    static string T(string en, string ko) { return DialogueLanguage.Korean ? ko : en; }

    // ---- UI (built from code on first use) ----
    void BuildUi()
    {
        var canvasGo = new GameObject("StopButtonCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        prompt = NewRect("Prompt", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(480f, 36f));
        TMP_Text p = NewText(prompt.transform, TextSize, TextAlignmentOptions.Center);
        p.text = T("[E] STOP BUTTON", "[E] 정지 버튼");
        p.color = Gold;
        prompt.SetActive(false);

        panel = NewRect("Choice", canvasGo.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 200f));
        panel.AddComponent<Image>().color = BorderColor;
        GameObject fill = NewRect("Fill", panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(716f, 196f));
        fill.AddComponent<Image>().color = BoxColor;
        TMP_Text t = NewText(fill.transform, TextSize, TextAlignmentOptions.Center);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.color = Color.white;
        t.text = T("<color=#4CE0FF>[1]</color> Press the stop button\n\n<color=#FFD24A>[2]</color> Press it, and set the lucky number yourself\n\nEsc: back",
                   "<color=#4CE0FF>[1]</color> 정지 버튼을 누른다\n\n<color=#FFD24A>[2]</color> 누르되, 행운의 숫자를 직접 정한다\n\nEsc: 돌아가기");
        panel.SetActive(false);
    }

    TMP_Text NewText(Transform parent, int size, TextAlignmentOptions align)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12f, 8f); rt.offsetMax = new Vector2(-12f, -8f);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    static GameObject NewRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, anchor.y == 0f ? 0f : 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }
}
