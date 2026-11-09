using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Successful-parry feedback that reads at a glance (playtest 11-09 "can't tell whether parries succeed"): a short full-screen
// white flash and a "PARRY!" popup that rises from the contact point. Overlay canvas built on demand (one per scene).
// Real time, so both play through the parry hit-stop; frozen while the pause menu is open. Tunables: Juice.Parry*.
public class ParryFeedback : MonoBehaviour
{
    static ParryFeedback inst; // scene object: a scene change destroys it, the next Show builds a new one

    Canvas canvas;
    Image flash;
    TMP_Text popup;
    float flashLeft, popupLeft;
    Vector2 popupStart;

    public static void Show(Vector2 worldPoint)
    {
        if (inst == null) inst = new GameObject("ParryFeedback").AddComponent<ParryFeedback>();
        inst.Play(worldPoint);
    }

    void Awake()
    {
        canvas = UiKit.CreateCanvas("ParryCanvas", transform, 30); // under the control hints (35) and HUD
        flash = UiKit.FilledImage(UiKit.Stretch("Flash", canvas.transform), Color.clear);
        popup = UiKit.Label("Popup", canvas.transform, Vector2.zero, Vector2.zero, new Vector2(240f, 48f), "PARRY!", "패링!", Juice.ParryPopupSize, Juice.ParryPopupColor);
        popup.gameObject.SetActive(false);
    }

    void Play(Vector2 worldPoint)
    {
        flashLeft = Juice.ParryFlashTime;
        popupLeft = Juice.ParryPopupTime;
        Camera cam = Camera.main;
        Vector2 screen = cam != null ? (Vector2)cam.WorldToScreenPoint(worldPoint + Vector2.up) : new Vector2(Screen.width, Screen.height) * 0.5f;
        popupStart = screen / canvas.scaleFactor; // anchored bottom-left, so reference px from the corner
        popup.gameObject.SetActive(true);
        Apply();
    }

    void Update()
    {
        if (PauseMenu.IsOpen || (flashLeft <= 0f && popupLeft <= 0f)) return;
        float dt = Time.unscaledDeltaTime;
        flashLeft = Mathf.Max(0f, flashLeft - dt);
        popupLeft = Mathf.Max(0f, popupLeft - dt);
        Apply();
    }

    void Apply()
    {
        flash.color = new Color(1f, 1f, 1f, Juice.ParryFlashAlpha * flashLeft / Juice.ParryFlashTime);
        float t = 1f - popupLeft / Juice.ParryPopupTime; // 0 -> 1 over its life
        popup.rectTransform.anchoredPosition = popupStart + new Vector2(0f, Juice.ParryPopupRise * t);
        Color c = Juice.ParryPopupColor;
        c.a = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f; // hold, then fade over the second half
        popup.color = c;
        if (popupLeft <= 0f) popup.gameObject.SetActive(false);
    }
}
