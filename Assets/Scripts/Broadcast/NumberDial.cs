using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The number dial panel (look + controls), shared by the broadcast hack (BroadcastHackUi) and the END-02 ending choice
// (StopButton) so the same dial is the symbol from the first hack to the last decision. Not a MonoBehaviour: the owner
// builds it under its own canvas, calls Read() every frame while open and RefreshTexts() on Loc.Changed.
// Keys: 0-9 pick directly, Up/Down arrows or mouse wheel step, E confirms, Esc cancels (consumes Esc for the pause menu).
// ALL placeholder styling lives in the constants below.
public class NumberDial
{
    public enum Input { None, Confirm, Cancel }

    // ---- Style (placeholder; restyle here) ----
    static readonly Color BoxColor = new Color32(0x0B, 0x0F, 0x1E, 0xE6);
    static readonly Color BorderColor = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
    static readonly Color Cyan = new Color32(0x4C, 0xE0, 0xFF, 0xFF);
    static readonly Color WarnRed = new Color32(0xFF, 0x2A, 0x2A, 0xFF);
    static readonly Color Dim = new Color32(0x8A, 0x90, 0xA0, 0xFF);
    const int NumberSize = 96, DigitSize = 24, TextSize = 24, HintSize = 12; // multiples of 12 at 960x540
    static readonly Vector2 PanelSize = new Vector2(480f, 260f);

    static readonly Key[] DigitKeys =
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    public int Selected { get; private set; }
    public bool IsOpen => root.activeSelf;

    readonly GameObject root;
    readonly TMP_Text title, number, digits, warn, hint;
    readonly string titleEn, titleKo;
    readonly TMP_FontAsset font;

    public NumberDial(Transform canvasRoot, TMP_FontAsset font, string titleEn, string titleKo)
    {
        this.font = font;
        this.titleEn = titleEn;
        this.titleKo = titleKo;

        root = NewRect("Dial", canvasRoot, PanelSize);
        root.AddComponent<Image>().color = BorderColor;
        GameObject panel = NewRect("Fill", root.transform, Vector2.zero);
        Stretch((RectTransform)panel.transform, 2f, 2f, 2f, 2f);
        panel.AddComponent<Image>().color = BoxColor;

        // panel is 476x256 inside the border: title 12-36, number 40-136, digits 140-170, warn 174-204, hint at the bottom
        title = NewText("Title", panel.transform, TextSize, TextAlignmentOptions.Top);
        Stretch((RectTransform)title.transform, 12f, 12f, 12f, 12f);
        title.color = Dim;
        number = NewText("Number", panel.transform, NumberSize, TextAlignmentOptions.Center);
        Stretch((RectTransform)number.transform, 12f, 40f, 12f, 120f);
        number.richText = false; // "<  N  >" must not be parsed as a tag
        digits = NewText("Digits", panel.transform, DigitSize, TextAlignmentOptions.Center);
        Stretch((RectTransform)digits.transform, 12f, 140f, 12f, 86f);
        digits.richText = true;
        warn = NewText("Warn", panel.transform, TextSize, TextAlignmentOptions.Center);
        Stretch((RectTransform)warn.transform, 12f, 174f, 12f, 52f);
        warn.color = WarnRed;
        hint = NewText("Hint", panel.transform, HintSize, TextAlignmentOptions.Bottom);
        Stretch((RectTransform)hint.transform, 12f, 8f, 12f, 8f);
        hint.richText = false;
        hint.color = Dim;
        root.SetActive(false);
        RefreshTexts();
    }

    public void Open(int start)
    {
        Selected = ((start % 10) + 10) % 10;
        root.SetActive(true);
        Refresh();
    }

    public void Close() { root.SetActive(false); }

    // Call once per frame while open. Esc wins over E, E over digit/step keys (same order as the original hack dial).
    public Input Read()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Input.None;
        if (kb.escapeKey.wasPressedThisFrame) { GameFlow.EscConsumedFrame = Time.frameCount; return Input.Cancel; }
        if (kb.eKey.wasPressedThisFrame) return Input.Confirm;
        int before = Selected;
        for (int i = 0; i < DigitKeys.Length; i++)
            if (kb[DigitKeys[i]].wasPressedThisFrame) Selected = i;
        if (kb.upArrowKey.wasPressedThisFrame) Selected = (Selected + 1) % 10;
        if (kb.downArrowKey.wasPressedThisFrame) Selected = (Selected + 9) % 10;
        if (Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (wheel > 0f) Selected = (Selected + 1) % 10;
            else if (wheel < 0f) Selected = (Selected + 9) % 10;
        }
        if (Selected != before) Refresh();
        return Input.None;
    }

    // Language switch: re-apply the static texts; the digits are language-free, the warning is redone by Refresh.
    public void RefreshTexts()
    {
        title.text = T(titleEn, titleKo);
        hint.text = T("0-9 / UP DOWN / wheel: pick    E: confirm    Esc: cancel",
                      "0-9 / 위 아래 / 휠: 선택    E: 확정    Esc: 취소");
        if (IsOpen) Refresh();
    }

    void Refresh()
    {
        number.text = "<  " + Selected + "  >";
        number.color = Selected == 0 ? WarnRed : Cyan;
        string s = "";
        for (int i = 0; i < 10; i++)
            s += i == Selected ? "<color=#4CE0FF>" + i + "</color>" : "<color=#8A90A0>" + i + "</color>";
        digits.text = s.Replace("</color><color", "</color> <color");
        warn.text = Selected == 0 ? T("EVERYONE STOPS BREATHING", "모두 숨을 멈춘다") : "";
    }

    static string T(string en, string ko) { return DialogueLanguage.Korean ? ko : en; }

    // ---- UI building ----
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

    // centered rect
    static GameObject NewRect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
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
