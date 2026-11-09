using UnityEngine;

// The big broadcast screen painted on the STG-01 street backdrop shows the broadcast number of the zone the player stands in
// (same digit sprites as the HUD: magenta = AI number, cyan = hacked). No board in the player's zone -> the screen stays blank.
// A new / hacked number plays a short glitch: scrambled digits + red / cyan ghost copies jittering in whole sprite pixels.
[RequireComponent(typeof(SpriteRenderer))]
public class BroadcastScreen : MonoBehaviour
{
    static readonly Color GlitchRed = new Color32(0xFF, 0x2D, 0x55, 0xCC), GlitchCyan = new Color32(0x00, 0xD5, 0xFF, 0xCC);
    const float GlitchSeconds = 0.3f, GlitchStep = 0.04f;

    SpriteRenderer sr, ghostRed, ghostCyan;
    Transform player;
    int shownNumber = -1;
    bool shownHacked;
    float glitchAt = -10f; // unscaled time the glitch started
    int glitchStep = -1;   // -1 = idle (settled on the real digit)

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
        ghostRed = NewGhost("GhostRed", GlitchRed);
        ghostCyan = NewGhost("GhostCyan", GlitchCyan);
    }

    SpriteRenderer NewGhost(string name, Color color)
    {
        var g = new GameObject(name).AddComponent<SpriteRenderer>();
        g.transform.SetParent(transform, false);
        g.sortingLayerID = sr.sortingLayerID;
        g.sortingOrder = sr.sortingOrder + 1;
        g.color = color;
        g.enabled = false;
        return g;
    }

    void Update()
    {
        if (player == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc == null) return;
            player = pc.transform;
        }
        BroadcastBoard b = BroadcastBoard.At(player.position);
        if (b == null) { sr.enabled = false; ghostRed.enabled = ghostCyan.enabled = false; shownNumber = -1; glitchStep = -1; return; }
        Sprite[] digits = DraftHud.BroadcastDigits(b.IsHacked);
        if (digits == null) return;
        if (!sr.enabled || b.Number != shownNumber || b.IsHacked != shownHacked)
        {
            shownNumber = b.Number;
            shownHacked = b.IsHacked;
            sr.sprite = digits[Mathf.Clamp(shownNumber, 0, digits.Length - 1)];
            sr.enabled = true;
            glitchAt = Time.unscaledTime;
            glitchStep = -1;
        }
        UpdateGlitch(digits);
    }

    void UpdateGlitch(Sprite[] digits)
    {
        float t = Time.unscaledTime - glitchAt;
        int step = t < GlitchSeconds ? (int)(t / GlitchStep) : -1;
        if (step == glitchStep) return;
        glitchStep = step;
        bool on = step >= 0;
        Sprite s = digits[on ? Random.Range(0, digits.Length) : Mathf.Clamp(shownNumber, 0, digits.Length - 1)];
        sr.sprite = s;
        ghostRed.enabled = ghostCyan.enabled = on;
        if (!on) return;
        ghostRed.sprite = ghostCyan.sprite = s;
        Jitter(ghostRed, s);
        Jitter(ghostCyan, s);
    }

    // +-2..3 sprite pixels sideways (local units = sprite pixels / PPU)
    static void Jitter(SpriteRenderer g, Sprite s)
    {
        g.transform.localPosition = new Vector3(Random.Range(2, 4) * (Random.value < 0.5f ? -1 : 1) / s.pixelsPerUnit, 0f, 0f);
    }
}
