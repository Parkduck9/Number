using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Scene singleton: top-center broadcast display ((( N ))), "press E" prompt, and the hack dial.
// The game is NOT paused while the dial is open ([proposal] GDD ch.6): enemies keep coming and the player stays controllable.
// Dial keys: 0-9 pick directly, Up/Down arrows or mouse wheel step, E confirms, Esc cancels.
// (Left/Right arrows are bound to Move and Enter to Attack in InputSystem_Actions, so they are not used here.)
// E is read straight from Keyboard (same as DialoguePlayer) so PlayerInputReader is untouched.
// The UI is built from code. ALL placeholder styling lives in the constants below.
public class BroadcastHackUi : MonoBehaviour
{
    [SerializeField] TMP_FontAsset font;
    [SerializeField] float interactRange = 3f;
    [SerializeField] bool showCounterHackBar = true; // progress bar next to the number (DECISIONS 3-A)

    // ---- Style (placeholder; restyle here) ----
    static readonly Vector2 RefResolution = new Vector2(960f, 540f);
    static readonly Color BoxColor = new Color32(0x0B, 0x0F, 0x1E, 0xE6);
    static readonly Color BorderColor = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
    static readonly Color BroadcastGold = new Color32(0xFF, 0xD2, 0x4A, 0xFF); // AI's number
    static readonly Color HackedCyan = new Color32(0x4C, 0xE0, 0xFF, 0xFF);    // number set by the player's hack
    static readonly Color WarnRed = new Color32(0xFF, 0x2A, 0x2A, 0xFF);
    static readonly Color Dim = new Color32(0x8A, 0x90, 0xA0, 0xFF);
    const int HudSize = 48, DialNumberSize = 96, DigitSize = 24, TextSize = 24; // multiples of 12 at 960x540

    static readonly Key[] DigitKeys =
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    PlayerController player;
    BroadcastBoard dialBoard; // non-null = dial open
    int selected;
    bool lockedLastFrame; // the E that closes a Talk line must not also open the dial
    int shownNumber = -1, shownHacked = -1; // HUD cache: rebuild the string only when it changes
    bool hudWasShown;
    float punchAt = -10f; // unscaled time of the last number punch

    TMP_Text hudText, promptText, dialNumber, dialDigits, dialHint, dialWarn;
    GameObject dialRoot, barRoot, promptRoot;
    RectTransform barFill;

    void Awake() { BuildUi(); }

    void Start() { player = FindFirstObjectByType<PlayerController>(); }

    void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        BroadcastBoard hud = player != null ? BroadcastBoard.At(player.transform.position) : null; // only the player's zone
        UpdateHud(hud);

        BroadcastBoard near = player != null ? Nearest(interactRange) : null;
        if (dialBoard != null)
        {
            if (GameFlow.InputLocked || near != dialBoard) Close(); // walked away / dialogue took over
            else UpdateDial();
        }
        else if (near != null && !GameFlow.InputLocked && !lockedLastFrame)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) Open(near);
        }
        promptRoot.SetActive(dialBoard == null && near != null && !GameFlow.InputLocked);
        lockedLastFrame = GameFlow.InputLocked;
    }

    BroadcastBoard Nearest(float range)
    {
        if (player == null) return null;
        BroadcastBoard best = null;
        float bestD = range;
        Vector2 p = player.transform.position;
        foreach (BroadcastBoard b in BroadcastBoard.All)
        {
            float d = Vector2.Distance(p, b.transform.position);
            if (d <= bestD) { bestD = d; best = b; }
        }
        return best;
    }

    // ---- HUD ----
    void UpdateHud(BroadcastBoard b)
    {
        hudText.transform.parent.gameObject.SetActive(b != null);
        bool wasShown = hudWasShown;
        hudWasShown = b != null;
        if (b == null) return;
        int hacked = b.IsHacked ? 1 : 0;
        if (!wasShown || b.Number != shownNumber || hacked != shownHacked)
        {
            if (!wasShown || b.Number != shownNumber) punchAt = Time.unscaledTime; // TODO(audio): SFX-01 broadcast chime here
            shownNumber = b.Number;
            shownHacked = hacked;
            hudText.text = "((( " + b.Number + " )))";
            hudText.color = b.IsHacked ? HackedCyan : BroadcastGold;
        }
        float k = Mathf.Clamp01((Time.unscaledTime - punchAt) / 0.3f);
        float s = Mathf.Lerp(1.4f, 1f, k); // short scale punch on a new / changed number
        hudText.transform.localScale = new Vector3(s, s, 1f);
        barRoot.SetActive(showCounterHackBar && b.IsHacked);
        if (barRoot.activeSelf) barFill.anchorMax = new Vector2(b.CounterHackProgress, 1f);
    }

    // ---- Dial ----
    void Open(BroadcastBoard b)
    {
        dialBoard = b;
        selected = b.Number;
        dialRoot.SetActive(true);
        RefreshDial();
    }

    void Close()
    {
        dialBoard = null;
        dialRoot.SetActive(false);
    }

    void UpdateDial()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (kb.escapeKey.wasPressedThisFrame) { GameFlow.EscConsumedFrame = Time.frameCount; Close(); return; }
        if (kb.eKey.wasPressedThisFrame)
        {
            dialBoard.Hack(selected);
            Close();
            return;
        }
        int before = selected;
        for (int i = 0; i < DigitKeys.Length; i++)
            if (kb[DigitKeys[i]].wasPressedThisFrame) selected = i;
        if (kb.upArrowKey.wasPressedThisFrame) selected = (selected + 1) % 10;
        if (kb.downArrowKey.wasPressedThisFrame) selected = (selected + 9) % 10;
        if (Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (wheel > 0f) selected = (selected + 1) % 10;
            else if (wheel < 0f) selected = (selected + 9) % 10;
        }
        if (selected != before) RefreshDial();
    }

    void RefreshDial()
    {
        dialNumber.text = "<  " + selected + "  >";
        dialNumber.color = selected == 0 ? WarnRed : HackedCyan;
        string digits = "";
        for (int i = 0; i < 10; i++)
            digits += i == selected ? "<color=#4CE0FF>" + i + "</color>" : "<color=#8A90A0>" + i + "</color>";
        dialDigits.text = digits.Replace("</color><color", "</color> <color");
        dialWarn.text = selected == 0 ? "EVERYONE STOPS BREATHING" : "";
    }

    static string T(string en, string ko) { return DialogueLanguage.Korean ? ko : en; }

    // ---- UI building ----
    void BuildUi()
    {
        var canvasGo = new GameObject("BroadcastCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = RefResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        // HUD: top center, number + optional counter-hack bar
        GameObject hud = NewRect("Hud", root, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(360f, 72f));
        hudText = NewText("Number", hud.transform, HudSize, TextAlignmentOptions.Center);
        Stretch((RectTransform)hudText.transform, 0f, 0f, 0f, 0f);
        barRoot = NewRect("Bar", hud.transform, new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(240f, 8f));
        barRoot.AddComponent<Image>().color = BorderColor;
        GameObject fill = NewRect("Fill", barRoot.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch((RectTransform)fill.transform, 0f, 0f, 0f, 0f);
        fill.AddComponent<Image>().color = HackedCyan;
        barFill = (RectTransform)fill.transform;
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = new Vector2(0f, 1f);
        barRoot.SetActive(false);

        // Prompt: bottom center
        GameObject prompt = NewRect("Prompt", root, new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(360f, 36f));
        promptText = NewText("Text", prompt.transform, TextSize, TextAlignmentOptions.Center);
        Stretch((RectTransform)promptText.transform, 0f, 0f, 0f, 0f);
        promptText.text = T("[E] HACK", "[E] 해킹");
        promptText.color = HackedCyan;
        promptRoot = prompt;
        promptRoot.SetActive(false);

        // Dial panel: center
        dialRoot = NewRect("Dial", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480f, 260f));
        dialRoot.AddComponent<Image>().color = BorderColor;
        GameObject panel = NewRect("Fill", dialRoot.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch((RectTransform)panel.transform, 2f, 2f, 2f, 2f);
        panel.AddComponent<Image>().color = BoxColor;

        TMP_Text title = NewText("Title", panel.transform, TextSize, TextAlignmentOptions.Top);
        Stretch((RectTransform)title.transform, 12f, 12f, 12f, 12f);
        title.text = T("BROADCAST HACK", "방송 해킹");
        title.color = Dim;
        dialNumber = NewText("Number", panel.transform, DialNumberSize, TextAlignmentOptions.Center);
        // panel is 476x256 inside the border: title 12-36, number 40-136, digits 140-170, warn 174-204, hint at the bottom
        Stretch((RectTransform)dialNumber.transform, 12f, 40f, 12f, 120f);
        dialNumber.richText = false; // "<  N  >" must not be parsed as a tag
        dialDigits = NewText("Digits", panel.transform, DigitSize, TextAlignmentOptions.Center);
        Stretch((RectTransform)dialDigits.transform, 12f, 140f, 12f, 86f);
        dialDigits.richText = true;
        dialWarn = NewText("Warn", panel.transform, TextSize, TextAlignmentOptions.Center);
        Stretch((RectTransform)dialWarn.transform, 12f, 174f, 12f, 52f);
        dialWarn.color = WarnRed;
        dialHint = NewText("Hint", panel.transform, 12, TextAlignmentOptions.Bottom);
        Stretch((RectTransform)dialHint.transform, 12f, 8f, 12f, 8f);
        dialHint.richText = false;
        dialHint.text = T("0-9 / UP DOWN / wheel: pick    E: confirm    Esc: cancel",
                          "0-9 / 위 아래 / 휠: 선택    E: 확정    Esc: 취소");
        dialHint.color = Dim;
        dialRoot.SetActive(false);
    }

    TMP_Text NewText(string name, Transform parent, int size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
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
        rt.pivot = new Vector2(0.5f, anchor.y == 1f ? 1f : anchor.y == 0f ? 0f : 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    // inset from each edge (left, top, right, bottom)
    static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}
