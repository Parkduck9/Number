using UnityEngine;
using UnityEngine.UI;

// Elite duel letterbox (EliteDuel): two black bars (Height of the screen each) slide in / out over SlideTime, plus a short white
// flash that hides the camera's one-step zoom. Overlay canvas, real time (frozen while the pause menu is open).
public class DuelLetterbox : MonoBehaviour
{
    const float Height = 0.08f;     // (T) share of the screen height per bar
    const float SlideTime = 0.2f;   // (T) s
    const float FlashAlpha = 0.7f;  // (T)
    const int CanvasOrder = 25;     // under ParryFeedback (30), control hints (35) and the HUD

    RectTransform top, bottom;
    Image flash;
    float k, target, flashLeft;
    bool dirty = true;

    public static DuelLetterbox Create(Transform parent)
    {
        var go = new GameObject("DuelLetterbox");
        go.transform.SetParent(parent, false);
        return go.AddComponent<DuelLetterbox>();
    }

    void Awake()
    {
        var canvas = UiKit.CreateCanvas("DuelCanvas", transform, CanvasOrder);
        top = UiKit.Stretch("Top", canvas.transform);
        UiKit.FilledImage(top, Color.black);
        bottom = UiKit.Stretch("Bottom", canvas.transform);
        UiKit.FilledImage(bottom, Color.black);
        flash = UiKit.FilledImage(UiKit.Stretch("Flash", canvas.transform), Color.clear);
        Apply();
    }

    public void Show(bool on) { target = on ? 1f : 0f; dirty = true; }
    public void Flash(float seconds) { flashLeft = seconds; dirty = true; }

    void Update()
    {
        if (!dirty || PauseMenu.IsOpen) return;
        float dt = Time.unscaledDeltaTime;
        k = Mathf.MoveTowards(k, target, dt / SlideTime);
        flashLeft = Mathf.Max(0f, flashLeft - dt);
        Apply();
        dirty = k != target || flashLeft > 0f;
    }

    void Apply()
    {
        float h = Height * (1f - (1f - k) * (1f - k)); // ease-out
        top.anchorMin = new Vector2(0f, 1f - h);
        bottom.anchorMax = new Vector2(1f, h);
        flash.color = flashLeft > 0f ? new Color(1f, 1f, 1f, FlashAlpha) : Color.clear;
    }
}
