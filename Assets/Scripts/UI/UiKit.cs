using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Tiny code-built UI helpers for the title screen and pause menu. Style values come from UiTheme.
public static class UiKit
{
    public static Canvas CreateCanvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = UiTheme.Current.referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform Stretch(string name, Transform parent, float inset = 0f)
    {
        var rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        return rt;
    }

    public static Image FilledImage(RectTransform rt, Color color, bool raycast = false)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    // Pixel UI kit skin (Assets/Resources/DraftUi, docs/design/concept/ui_kit): 9-slice corners keep the art's 2x pixel scale on the 960x540 canvas
    // (1 source px = 2 reference px). Returns false (image untouched) when the sprite is missing, so callers keep the flat look.
    public static bool Slice(Image img, Sprite s)
    {
        if (s == null) return false;
        img.sprite = s;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 50f / s.pixelsPerUnit; // Image divides by (spritePPU / 100 reference PPU): 0.5 units per source px = 2 ref px per art pixel
        img.color = Color.white;
        return true;
    }

    // Square pixel frame: border-colored rect with a fill rect inset by frameWidth. Returns the fill rect (content parent).
    // With the panel_9s skin the outer image is the skin and the fill rect is an empty content parent.
    public static RectTransform Frame(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, out Image border)
    {
        var t = UiTheme.Current;
        var outer = Rect(name, parent, anchor, pos, size);
        border = FilledImage(outer, t.border);
        var fill = Stretch("Fill", outer, t.frameWidth);
        if (!Slice(border, DraftHud.Ui("panel_9s"))) FilledImage(fill, t.panelFill);
        return fill;
    }

    public static TMP_Text Label(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string en, string ko, int fontSize, Color color)
    {
        var rt = Rect(name, parent, anchor, pos, size);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        var t = UiTheme.Current;
        if (t.font != null) tmp.font = t.font;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        rt.gameObject.AddComponent<LocalizedText>().Set(tmp, en, ko);
        return tmp;
    }

    // A framed button; text dim normally, white with a bright border when selected/hovered.
    public static Button AddButton(Transform column, string en, string ko, int index, UnityAction onClick)
    {
        var t = UiTheme.Current;
        var outer = Rect("Btn " + en, column, new Vector2(0.5f, 1f), new Vector2(0f, -index * t.buttonStep), t.buttonSize2);
        outer.pivot = new Vector2(0.5f, 1f);
        var border = FilledImage(outer, t.border, true);
        var fill = Stretch("Fill", outer, t.frameWidth);
        Sprite[] skin = { DraftHud.Ui("button_9s_normal"), DraftHud.Ui("button_9s_hover"), DraftHud.Ui("button_9s_pressed") };
        bool skinned = Slice(border, skin[0]) && skin[1] != null && skin[2] != null;
        if (!skinned) FilledImage(fill, t.panelFill);
        var label = Label("Label", fill, new Vector2(0.5f, 0.5f), Vector2.zero, t.buttonSize2, en, ko, t.buttonSize, t.dim);

        var btn = outer.gameObject.AddComponent<Button>();
        btn.targetGraphic = border;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(onClick);
        outer.gameObject.AddComponent<RectMask2D>(); // clips the shine band to the button
        var shine = FilledImage(Rect("Shine", outer, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(ButtonHighlight.ShineWidth, t.buttonSize2.y * 2f)), new Color(1f, 1f, 1f, 0.35f));
        shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        shine.enabled = false;
        outer.gameObject.AddComponent<CanvasGroup>();
        outer.gameObject.AddComponent<ButtonHighlight>().Init(border, label, t, shine, index, skinned ? skin : null);
        return btn;
    }

    // Compact in-game controls guide (EN/KO): 4 lines at size 18 in a 600x104 frame. Returns the frame root (for toggling).
    public static GameObject ControlsPanel(Transform parent, Vector2 anchor, Vector2 pos)
    {
        var fill = Frame("Controls", parent, anchor, pos, new Vector2(600f, 104f), out _);
        Label("Text", fill, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(590f, 100f),
            "A/D  Move      W  Jump\nShift  Dodge + counter      E  Interact / hack\nLeft click  Attack / Parry      Right click  Issen (flash strike)\nSpace  Lucky dice (after unlock)      Esc / P  Pause",
            "A/D  이동      W  점프\nShift  회피 + 반격      E  상호작용 / 해킹\n좌클릭  공격 / 패링      우클릭  일섬\nSpace  점괘 주사위(해금 후)      Esc / P  일시정지",
            18, UiTheme.Current.dim);
        return fill.parent.gameObject;
    }

    public static void EnsureEventSystem(out GameObject created)
    {
        created = null;
        if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
        created = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}

// Selection / hover look for UiKit buttons (color swap) plus GUI motion, all on unscaled time (the pause menu runs at timeScale 0):
// shine sweep on enter/select, press scale with back-out release, and a staggered slide+fade-in whenever the button becomes active.
public class ButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public const float ShineWidth = 24f;
    const float ShineTime = 0.35f, PressScale = 0.93f, ReleaseTime = 0.2f, InTime = 0.2f, InStep = 0.06f, InSlide = 16f;

    Image border; TMP_Text label; UiTheme theme; Image shine; CanvasGroup group; RectTransform rt;
    int index;
    bool selected, hover, down;
    float shineT = ShineTime, releaseT = ReleaseTime, introT = InTime, baseX;

    Sprite[] skin; // normal, hover/selected, pressed (null = flat color look)

    public void Init(Image b, TMP_Text l, UiTheme t, Image s, int i, Sprite[] skinSprites = null)
    {
        border = b; label = l; theme = t; shine = s; index = i; skin = skinSprites;
        rt = (RectTransform)transform; group = GetComponent<CanvasGroup>(); baseX = rt.anchoredPosition.x;
        Apply();
        StartIntro();
    }
    public void OnSelect(BaseEventData e) { selected = true; Sfx.Play(SfxId.UiSelect); StartShine(); Apply(); }
    public void OnDeselect(BaseEventData e) { selected = false; Apply(); }
    public void OnPointerEnter(PointerEventData e) { hover = true; StartShine(); Apply(); }
    public void OnPointerExit(PointerEventData e) { hover = false; Apply(); }
    public void OnPointerDown(PointerEventData e) { down = true; transform.localScale = new Vector3(PressScale, PressScale, 1f); Apply(); }
    public void OnPointerUp(PointerEventData e) { if (down) { down = false; releaseT = 0f; Apply(); } }
    void OnEnable() { if (theme != null) StartIntro(); }
    void OnDisable()
    {
        selected = hover = down = false;
        shineT = ShineTime; releaseT = ReleaseTime;
        if (shine != null) shine.enabled = false;
        if (rt != null) transform.localScale = Vector3.one;
        if (border != null) Apply();
    }

    void StartShine() { if (shineT >= ShineTime) shineT = 0f; }

    void StartIntro()
    {
        introT = -index * InStep;
        group.alpha = 0f;
        rt.anchoredPosition = new Vector2(baseX - InSlide, rt.anchoredPosition.y);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (introT < InTime)
        {
            introT += dt;
            float k = Mathf.Clamp01(introT / InTime);
            k = 1f - (1f - k) * (1f - k); // ease-out
            group.alpha = k;
            rt.anchoredPosition = new Vector2(baseX - InSlide * (1f - k), rt.anchoredPosition.y);
        }
        if (shineT < ShineTime)
        {
            shineT += dt;
            bool on = shineT < ShineTime;
            shine.enabled = on;
            if (on)
            {
                float k = shineT / ShineTime;
                shine.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-ShineWidth, rt.rect.width + ShineWidth, k), 0f);
            }
        }
        if (!down && releaseT < ReleaseTime)
        {
            releaseT += dt;
            float u = Mathf.Clamp01(releaseT / ReleaseTime) - 1f;
            float back = 1f + 2.70158f * u * u * u + 1.70158f * u * u; // back-out
            float s = Mathf.LerpUnclamped(PressScale, 1f, back);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }

    void Apply()
    {
        bool on = selected || hover;
        if (skin != null) border.sprite = down ? skin[2] : on ? skin[1] : skin[0];
        else border.color = on ? theme.selectedBorder : theme.border;
        label.rectTransform.anchoredPosition = skin != null && down ? new Vector2(0f, -2f) : Vector2.zero; // pressed: text drops 1 art px (2 ref px)
        label.color = on ? theme.text : theme.dim;
    }
}
