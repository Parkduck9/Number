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

    PlayerHealth health;
    int shown = -1;

    void Update()
    {
        if (health == null)
        {
            health = FindFirstObjectByType<PlayerHealth>();
            if (health == null) return;
        }

        int hits = Mathf.Clamp(health.MaxHp - health.Hp, 0, Names.Length - 1);
        if (hits == shown) return;
        shown = hits;

        Sprite s = stages != null && hits < stages.Length ? stages[hits] : null;
        face.sprite = s;
        face.color = s != null ? Color.white : PlaceholderColors[hits];
        if (label != null) label.text = Names[hits];
    }
}
