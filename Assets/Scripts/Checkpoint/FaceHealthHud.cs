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
        Sprite bezel = DraftHud.Ui("hud_face_bezel"); // 56x56 art at 2x = 112, its 48x48 hole is exactly the face; child of the face so it shakes with it
        if (bezel != null)
        {
            var bz = UiKit.FilledImage(UiKit.Rect("Bezel", face.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f)), Color.white);
            bz.sprite = bezel;
            bz.transform.SetSiblingIndex(0); // under the hit flash overlay
        }
        if (label != null) { label.gameObject.SetActive(false); label = null; }
        BuildHearts();
    }

    // ---- HP hearts (playtest 11-13, user: the degrading face alone did not show how much HP is left). One heart per HP,
    // right of the face at the same 2x; a lost heart cracks (hp_heart_lose_0/1) then stays hollow. hud_hp option B.
    const float HeartPitch = 30f, HeartFrame0 = 0.08f, HeartFrame1 = 0.18f; // 15 native px pitch at 2x; crack 80 ms, split 100 ms, then hollow
    Image[] hearts;
    Sprite heartFull, heartEmpty, heartLose0, heartLose1;
    float[] heartLostAt;

    void BuildHearts()
    {
        heartFull = Resources.Load<Sprite>("DraftHud/hp_heart_full");
        heartEmpty = Resources.Load<Sprite>("DraftHud/hp_heart_empty");
        heartLose0 = Resources.Load<Sprite>("DraftHud/hp_heart_lose_0");
        heartLose1 = Resources.Load<Sprite>("DraftHud/hp_heart_lose_1");
        if (heartFull == null || heartEmpty == null) return;
        var fr = face.rectTransform;
        hearts = new Image[5];
        heartLostAt = new float[hearts.Length];
        for (int i = 0; i < hearts.Length; i++)
        {
            var rt = UiKit.Rect("Heart", fr.parent, new Vector2(0f, 1f), Vector2.zero, new Vector2(20f, 32f));
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(fr.anchoredPosition.x + 112f + i * HeartPitch, fr.anchoredPosition.y - 4f); // past the 112 px bezel
            hearts[i] = UiKit.FilledImage(rt, Color.white);
            hearts[i].sprite = heartFull;
            hearts[i].raycastTarget = false;
            heartLostAt[i] = -10f;
        }
    }

    void UpdateHearts(int hp, bool hitNow)
    {
        if (hearts == null) return;
        float now = Time.unscaledTime;
        for (int i = 0; i < hearts.Length; i++)
        {
            bool full = i < hp;
            if (!full && hitNow && hearts[i].sprite == heartFull) heartLostAt[i] = now; // just lost this one: play the crack
            float t = now - heartLostAt[i];
            hearts[i].sprite = full ? heartFull
                : t < HeartFrame0 && heartLose0 != null ? heartLose0
                : t < HeartFrame1 && heartLose1 != null ? heartLose1
                : heartEmpty;
        }
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
        UpdateHearts(health.Hp, shown >= 0 && hits > shown);
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
