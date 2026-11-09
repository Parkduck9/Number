using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Face-portrait health (GDD ch.5): 6 stages = number of hits taken (0 normal ... 5 AI chip at 0 HP).
// Placeholder: solid colour per stage until real sprites are dropped into `stages` (ART-08).
public class FaceHealthHud : MonoBehaviour
{
    [SerializeField] Image face;
    [SerializeField] TMP_Text label; // optional stage name, remove when real art exists
    [SerializeField] Sprite[] stages = new Sprite[6]; // 0 normal, 1 ear, 2 nose, 3 eye, 4 mouth, 5 chip

    static readonly Color[] PlaceholderColors =
    {
        new Color32(0xD9, 0xB3, 0x8C, 0xFF), new Color32(0xC9, 0xAE, 0x9C, 0xFF), new Color32(0xB0, 0xA9, 0xA8, 0xFF),
        new Color32(0x8F, 0xA3, 0xB3, 0xFF), new Color32(0x6F, 0x9C, 0xBE, 0xFF), new Color32(0xFF, 0x2A, 0x2A, 0xFF),
    };
    static readonly string[] Names = { "NORMAL", "EAR", "NOSE", "EYE", "MOUTH", "AI CHIP" };

    const float ShakeSeconds = 0.3f, ShakeAmplitude = 6f, FlashSeconds = 0.08f;

    PlayerHealth health;
    int shown = -1;
    float hitAt = -10f;    // unscaled time of the last hit (shake + flash)
    bool hitFx;
    Vector2 restPos;
    Image flash;           // white overlay, only visible during the flash

    // DraftHud: 48px face art drawn at 2x (96 reference px) in place of the 64px placeholder box; the stage-name label goes away.
    void Awake()
    {
        if (face != null)
        {
            restPos = face.rectTransform.anchoredPosition;
            flash = UiKit.FilledImage(UiKit.Rect("Flash", face.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), Color.clear);
            flash.rectTransform.anchorMin = Vector2.zero;
            flash.rectTransform.anchorMax = Vector2.one;
            flash.rectTransform.offsetMin = flash.rectTransform.offsetMax = Vector2.zero;
        }
        Sprite[] art = DraftHud.Faces;
        if (art == null || face == null) return;
        stages = art;
        face.rectTransform.sizeDelta = new Vector2(96f, 96f);
        face.preserveAspect = true;
        if (label != null) { label.gameObject.SetActive(false); label = null; }
    }

    void Update()
    {
        if (hitFx) AnimateHit();
        if (health == null)
        {
            health = FindFirstObjectByType<PlayerHealth>();
            if (health == null) return;
        }

        int hits = Mathf.Clamp(health.MaxHp - health.Hp, 0, Names.Length - 1);
        if (hits == shown) return;
        if (shown >= 0 && hits > shown && flash != null) { hitAt = Time.unscaledTime; hitFx = true; } // took a hit (not a respawn heal / first frame)
        shown = hits;

        Sprite s = stages != null && hits < stages.Length ? stages[hits] : null;
        face.sprite = s;
        face.color = s != null ? Color.white : PlaceholderColors[hits];
        if (label != null) label.text = Names[hits];
    }

    // Decaying horizontal shake (whole px) + a brief white flash, on unscaled time (hit-stop safe).
    void AnimateHit()
    {
        float t = Time.unscaledTime - hitAt;
        face.rectTransform.anchoredPosition = restPos + new Vector2(UiMotion.Shake(t, ShakeSeconds, ShakeAmplitude), 0f);
        flash.color = t < FlashSeconds ? new Color(1f, 1f, 1f, 0.7f) : Color.clear;
        if (t >= ShakeSeconds) hitFx = false;
    }
}
