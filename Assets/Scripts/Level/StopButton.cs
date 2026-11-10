using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The AI stop button (GDD ch.10, both endings [confirmed]). Press E near it -> choose END-01 or END-02 -> the ending
// plays through the dialogue system (talk mode) -> back to the title.
// Pick: 1 = END-01 right away. 2 = END-02 opens the same number dial the player hacked with all game (NumberDial,
// docs/design/impl/ending-dial.md): pick a number, E confirms, Esc goes back to the choice. Esc / P / Backspace cancels
// the choice. Input stays locked while choosing. The number is symbolic (any number -> END-02); the END-02 lines may
// contain {N}, which is replaced by the chosen number.
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

    enum Phase { Idle, Choosing, Dial, Playing, Done }
    Phase phase;
    PlayerController player;
    GameObject panel;
    GameObject prompt;
    TMP_Text promptText, choiceText;
    NumberDial dial; // END-02: same look and keys as the broadcast hack dial
    bool lockedLastFrame;

    void Start() { player = FindFirstObjectByType<PlayerController>(); }

    void OnEnable() { Loc.Changed += RefreshTexts; }
    void OnDisable() { Loc.Changed -= RefreshTexts; }

    // Language switch: re-apply the code-built texts (same as BroadcastHackUi).
    void RefreshTexts()
    {
        if (promptText == null) return;
        promptText.text = T("[E] STOP BUTTON", "[E] 정지 버튼");
        choiceText.text = T("<color=#4CE0FF>[1]</color> Press the stop button\n\n<color=#FFD24A>[2]</color> Press it, and set the lucky number yourself\n\nEsc / P: back",
                            "<color=#4CE0FF>[1]</color> 정지 버튼을 누른다\n\n<color=#FFD24A>[2]</color> 누르되, 행운의 숫자를 직접 정한다\n\nEsc / P: 돌아가기");
        dial.RefreshTexts();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        switch (phase)
        {
            case Phase.Idle:
                bool near = player != null && !GameFlow.IsPlayerDead(player) && !GameFlow.InputLocked &&
                            Vector2.Distance(player.transform.position, transform.position) <= interactRange;
                SetPrompt(near);
                if (near) GameFlow.InteractFrame = Time.frameCount; // E presses the button, not the dice
                // !lockedLastFrame: the E that closes a talk scene must not also open this menu
                if (near && !lockedLastFrame && kb.eKey.wasPressedThisFrame) OpenChoice();
                break;
            case Phase.Choosing:
                if (GameFlow.IsPlayerDead(player)) Cancel(); // a hit that landed as the menu opened: the respawn reload must not cut an ending
                else if (kb.digit1Key.wasPressedThisFrame) Play(ending01, -1);
                else if (kb.digit2Key.wasPressedThisFrame) OpenDial();
                else if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) { GameFlow.EscConsumedFrame = Time.frameCount; Cancel(); }
                break;
            case Phase.Dial:
                if (GameFlow.IsPlayerDead(player)) { dial.Close(); Cancel(); break; }
                NumberDial.Input r = dial.Read();
                if (r == NumberDial.Input.Cancel || kb.pKey.wasPressedThisFrame) { dial.Close(); panel.SetActive(true); phase = Phase.Choosing; } // back to the choice, not to the game
                else if (r == NumberDial.Input.Confirm) { dial.Close(); Play(ending02, dial.Selected); }
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

    void Cancel()
    {
        panel.SetActive(false);
        GameFlow.InputLocked = false;
        phase = Phase.Idle;
    }

    // END-02: the dial starts on the number the broadcast shows in the player's zone (same as the hack dial), else 0.
    void OpenDial()
    {
        panel.SetActive(false);
        BroadcastBoard b = BroadcastBoard.At(player.transform.position);
        dial.Open(b != null ? b.Number : 0);
        phase = Phase.Dial;
    }

    // number >= 0: END-02, replaces {N} in the lines with the chosen number (symbolic; no branching on the value).
    void Play(TextAsset file, int number)
    {
        panel.SetActive(false);
        // Keep InputLocked set (no unlocked frame before DialoguePlayer picks the lines up next Update).
        // Only a talk-mode file with lines will ever clear it, so anything else goes straight to the title.
        if (file != null && number >= 0 && file.text.Contains("{N}")) file = new TextAsset(file.text.Replace("{N}", number.ToString()));
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
        TMP_Text p = promptText = NewText(prompt.transform, TextSize, TextAlignmentOptions.Center);
        p.color = Gold;
        prompt.SetActive(false);

        panel = NewRect("Choice", canvasGo.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 200f));
        panel.AddComponent<Image>().color = BorderColor;
        GameObject fill = NewRect("Fill", panel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(716f, 196f));
        if (!UiKit.Slice(panel.GetComponent<Image>(), DraftHud.Ui("panel_9s"))) fill.AddComponent<Image>().color = BoxColor; // skin = pixel panel, else flat box
        TMP_Text t = choiceText = NewText(fill.transform, TextSize, TextAlignmentOptions.Center);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.color = Color.white;
        panel.SetActive(false);
        dial = new NumberDial(canvasGo.transform, font, "LUCKY NUMBER", "행운의 숫자");
        RefreshTexts();
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
