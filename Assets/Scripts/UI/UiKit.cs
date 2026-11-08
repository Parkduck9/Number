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

    // Square pixel frame: border-colored rect with a fill rect inset by frameWidth. Returns the fill rect (content parent).
    public static RectTransform Frame(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, out Image border)
    {
        var t = UiTheme.Current;
        var outer = Rect(name, parent, anchor, pos, size);
        border = FilledImage(outer, t.border);
        var fill = Stretch("Fill", outer, t.frameWidth);
        FilledImage(fill, t.panelFill);
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
        FilledImage(fill, t.panelFill);
        var label = Label("Label", fill, new Vector2(0.5f, 0.5f), Vector2.zero, t.buttonSize2, en, ko, t.buttonSize, t.dim);

        var btn = outer.gameObject.AddComponent<Button>();
        btn.targetGraphic = border;
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(onClick);
        outer.gameObject.AddComponent<ButtonHighlight>().Init(border, label, t);
        return btn;
    }

    // Compact in-game controls guide (EN/KO): 4 lines at size 18 in a 600x104 frame.
    public static void ControlsPanel(Transform parent, Vector2 anchor, Vector2 pos)
    {
        var fill = Frame("Controls", parent, anchor, pos, new Vector2(600f, 104f), out _);
        Label("Text", fill, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(590f, 100f),
            "A/D  Move      W  Jump\nShift  Dash      E  Interact / hack\nLeft click  Attack      Right click  Parry\nSpace  Shockwave (after unlock)      Esc / P  Pause",
            "A/D  이동      W  점프\nShift  대시      E  상호작용 / 해킹\n좌클릭  공격      우클릭  패링\nSpace  충격파(해금 후)      Esc / P  일시정지",
            18, UiTheme.Current.dim);
    }

    public static void EnsureEventSystem(out GameObject created)
    {
        created = null;
        if (EventSystem.current != null || Object.FindFirstObjectByType<EventSystem>() != null) return;
        created = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}

// Selection / hover look for UiKit buttons (placeholder: color swap only).
public class ButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    Image border; TMP_Text label; UiTheme theme;
    bool selected, hover;

    public void Init(Image b, TMP_Text l, UiTheme t) { border = b; label = l; theme = t; Apply(); }
    public void OnSelect(BaseEventData e) { selected = true; Apply(); }
    public void OnDeselect(BaseEventData e) { selected = false; Apply(); }
    public void OnPointerEnter(PointerEventData e) { hover = true; Apply(); }
    public void OnPointerExit(PointerEventData e) { hover = false; Apply(); }
    void OnDisable() { selected = hover = false; if (border != null) Apply(); }

    void Apply()
    {
        bool on = selected || hover;
        border.color = on ? theme.selectedBorder : theme.border;
        label.color = on ? theme.text : theme.dim;
    }
}
