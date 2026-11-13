using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Scene singleton: the top-center broadcast display ((( N ))).
// Hacking a board with E was removed on 11-13 (user: stopping the crowd with a 0 killed the pace), so this is a read-out only.
// The number still drives the NUM-3 / NUM-7 enemy rules, and NumberDial lives on for the END-02 ending choice.
// The UI is built from code. ALL placeholder styling lives in the constants below.
public class BroadcastHackUi : MonoBehaviour
{
    [SerializeField] TMP_FontAsset font;
    [SerializeField] bool showCounterHackBar = true; // progress bar next to the number (DECISIONS 3-A)

    // ---- Style (placeholder; restyle here) ----
    static readonly Vector2 RefResolution = new Vector2(960f, 540f);
    static readonly Color BorderColor = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
    static readonly Color BroadcastGold = new Color32(0xFF, 0xD2, 0x4A, 0xFF); // AI's number
    static readonly Color HackedCyan = new Color32(0x4C, 0xE0, 0xFF, 0xFF);    // number set by the player's hack
    static readonly Color GlitchRed = new Color32(0xFF, 0x2D, 0x55, 0xCC), GlitchCyan = new Color32(0x00, 0xD5, 0xFF, 0xCC);
    const float GlitchSeconds = 0.3f, GlitchStep = 0.04f;
    const int HudSize = 48, TextSize = 24; // multiples of 12 at 960x540

    PlayerController player;
    int shownNumber = -1, shownHacked = -1; // HUD cache: rebuild the string only when it changes
    bool hudWasShown;
    float punchAt = -10f; // unscaled time of the last number punch

    TMP_Text hudText;
    GameObject barRoot;
    RectTransform barFill;
    RectTransform digitRow; // DraftHud art; null = the TMP text above is used
    Image arcL, digitImg, arcR, ghostRed, ghostCyan;
    float glitchAt = -10f; // unscaled time the glitch started
    int glitchStep = -1;   // -1 = idle (settled on the real digit)

    void Awake() { BuildUi(); }

    void Start() { player = FindFirstObjectByType<PlayerController>(); }

    void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        UpdateHud(player != null ? BroadcastBoard.At(player.transform.position) : null); // only the player's zone
    }

    void UpdateHud(BroadcastBoard b)
    {
        hudText.transform.parent.gameObject.SetActive(b != null);
        bool wasShown = hudWasShown;
        hudWasShown = b != null;
        if (b == null) return;
        int hacked = b.IsHacked ? 1 : 0;
        if (!wasShown || b.Number != shownNumber || hacked != shownHacked)
        {
            if (!wasShown || b.Number != shownNumber) punchAt = Time.unscaledTime; // the SFX-01 chime is played by GameAudio's board observer
            shownNumber = b.Number;
            shownHacked = hacked;
            hudText.text = "((( " + b.Number + " )))";
            hudText.color = b.IsHacked ? HackedCyan : BroadcastGold;
            if (digitRow != null) ShowDigits(b);
            glitchAt = Time.unscaledTime;
            glitchStep = -1;
        }
        UpdateGlitch(b);
        float k = Mathf.Clamp01((Time.unscaledTime - punchAt) / 0.3f);
        float s = Mathf.Lerp(1.4f, 1f, k); // short scale punch on a new / changed number
        (digitRow != null ? digitRow : hudText.rectTransform).localScale = new Vector3(s, s, 1f);
        barRoot.SetActive(showCounterHackBar && b.IsHacked);
        if (barRoot.activeSelf) barFill.anchorMax = new Vector2(b.CounterHackProgress, 1f);
    }

    // Glitch on a new / hacked number: random digit every 0.04s + red / cyan ghost copies jittering in whole pixels, settles on the real digit.
    void UpdateGlitch(BroadcastBoard b)
    {
        float t = Time.unscaledTime - glitchAt;
        int step = t < GlitchSeconds ? (int)(t / GlitchStep) : -1;
        if (step == glitchStep) return;
        glitchStep = step;
        bool on = step >= 0;
        int digit = on ? Random.Range(0, 10) : Mathf.Clamp(b.Number, 0, 9);
        if (digitRow != null)
        {
            Sprite s = DraftHud.BroadcastDigits(b.IsHacked)[digit];
            Place(digitImg, s);
            ghostRed.enabled = ghostCyan.enabled = on;
            if (!on) return;
            Place(ghostRed, s);
            Place(ghostCyan, s);
            ghostRed.rectTransform.anchoredPosition = new Vector2(Random.Range(2, 4) * (Random.value < 0.5f ? -1 : 1), 0f);
            ghostCyan.rectTransform.anchoredPosition = new Vector2(Random.Range(2, 4) * (Random.value < 0.5f ? -1 : 1), 0f);
        }
        else hudText.text = "((( " + (on ? digit : b.Number) + " )))"; // no art: scramble the text only
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
        BuildDigitRow(hud.transform);
        barRoot =NewRect("Bar", hud.transform, new Vector2(0.5f, 0f), new Vector2(0f, -6f), new Vector2(240f, 8f));
        barRoot.AddComponent<Image>().color = BorderColor;
        GameObject fill = NewRect("Fill", barRoot.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch((RectTransform)fill.transform, 0f, 0f, 0f, 0f);
        fill.AddComponent<Image>().color = HackedCyan;
        barFill = (RectTransform)fill.transform;
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = new Vector2(0f, 1f);
        barRoot.SetActive(false);
    }

    // DraftHud: arc + 12x18 glyph + arc at 3x replaces the "((( N )))" text (magenta = the AI's number, cyan = hacked). No art = text stays.
    void BuildDigitRow(Transform parent)
    {
        if (DraftHud.BroadcastDigits(false) == null || DraftHud.BroadcastDigits(true) == null ||
            DraftHud.BroadcastArcs(false) == null || DraftHud.BroadcastArcs(true) == null) return;
        digitRow = UiKit.Rect("DigitRow", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(114f, 54f));
        arcL = DigitPiece("ArcL", -40.5f);
        ghostRed = DigitPiece("GhostRed", 0f);   // drawn behind the real digit
        ghostCyan = DigitPiece("GhostCyan", 0f);
        ghostRed.color = GlitchRed;
        ghostCyan.color = GlitchCyan;
        ghostRed.enabled = ghostCyan.enabled = false;
        digitImg = DigitPiece("Digit", 0f);
        arcR = DigitPiece("ArcR", 40.5f);
        hudText.enabled = false;
    }

    Image DigitPiece(string name, float x)
    {
        var img = UiKit.FilledImage(UiKit.Rect(name, digitRow, new Vector2(0.5f, 0.5f), new Vector2(x, 0f), Vector2.zero), Color.white);
        return img;
    }

    void ShowDigits(BroadcastBoard b)
    {
        Sprite[] digits = DraftHud.BroadcastDigits(b.IsHacked), arcs = DraftHud.BroadcastArcs(b.IsHacked);
        Place(arcL, arcs[0]);
        Place(digitImg, digits[Mathf.Clamp(b.Number, 0, 9)]);
        Place(arcR, arcs[1]);
    }

    static void Place(Image img, Sprite s)
    {
        img.sprite = s;
        img.rectTransform.sizeDelta = s.rect.size * 3f; // 3x the 960x540 reference grid
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
