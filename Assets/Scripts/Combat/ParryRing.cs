using UnityEngine;

// Melee parry timing tell (user 11-10, replaces the red swing box): a pale target ring at the contact point and a red ring sliding in
// from the enemy's side that meets it exactly when the strike lands (TestEnemy's parry eta, so the picture and the LMB window agree).
// Merge = short talisman-yellow flash, then gone. Code-drawn 1 px pixel ring (+ dark rim), unlit Sprites/Default on the FX layer
// so 2D lights don't darken it. Owned and driven by TestEnemy (melee only); no Update of its own except ending the flash.
public class ParryRing : MonoBehaviour
{
    const int Radius = 10;                        // (T) px: 21 px ring, 1 px dark rim outside it
    const float Ppu = 32f;
    const float ApproachSpeed = 5f;               // (T) u/s: red ring start distance = this x the windup length...
    const float MinStart = 0.5f, MaxStart = 1.5f; // (T) ...clamped (u), so short chain windups still show a visible slide
    const float FlashTime = 0.03f;                // (T) s, scaled time: 2 frames at 60 fps, 1 at 30; hit-stop holds it
    const int SortOrder = 60;                     // FX layer; the approaching ring draws one above the target ring
    static readonly Color ApproachColor = new Color32(0xFF, 0x5A, 0x3C, 0xFF); // #FF5A3C
    static readonly Color TargetColor = new Color32(0xFF, 0xF1, 0xD6, 0xFF);   // #FFF1D6, LMB parry window open
    static readonly Color TargetDimColor = new Color32(0xFF, 0xF1, 0xD6, 0x80); // before the window opens
    static readonly Color FlashColor = new Color32(0xF2, 0xC9, 0x4C, 0xFF);    // #F2C94C talisman yellow
    static readonly Color32 RimColor = new Color32(0x14, 0x0C, 0x10, 0xA0);

    static Sprite sprite;
    static Material mat;

    SpriteRenderer target, approach;
    float flashUntil = -1f;

    public bool Flashing => flashUntil >= 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { sprite = null; mat = null; } // Domain Reload is off

    public static ParryRing For(GameObject owner)
    {
        var r = owner.GetComponent<ParryRing>();
        return r != null ? r : owner.AddComponent<ParryRing>();
    }

    // contact = target ring centre (world); facing = enemy facing (the red ring comes from -facing); total = eta when the ring started.
    // armed = the LMB parry window is open (target ring at full alpha).
    public void Show(Vector2 contact, int facing, float total, float eta, bool armed)
    {
        if (target == null) Build();
        flashUntil = -1f;
        float dist = Mathf.Clamp(ApproachSpeed * total, MinStart, MaxStart);
        float k = total > 0f ? Mathf.Clamp01(eta / total) : 0f;
        Place(target, contact, armed ? TargetColor : TargetDimColor);
        Place(approach, contact - new Vector2(facing * dist * k, 0f), ApproachColor);
    }

    // The strike landed: the merged ring flashes yellow, then hides itself. No-op when nothing is shown.
    public void Flash()
    {
        if (target == null || !target.enabled) return;
        approach.enabled = false;
        target.color = FlashColor;
        flashUntil = Time.time + FlashTime;
    }

    public void Hide()
    {
        flashUntil = -1f;
        if (target == null) return;
        target.enabled = false;
        approach.enabled = false;
    }

    void LateUpdate()
    {
        if (flashUntil >= 0f && Time.time >= flashUntil) Hide();
    }

    static void Place(SpriteRenderer sr, Vector2 p, Color c)
    {
        sr.transform.position = new Vector3(Mathf.Round(p.x * Ppu) / Ppu, Mathf.Round(p.y * Ppu) / Ppu, 0f); // pixel grid
        sr.color = c;
        sr.enabled = true;
    }

    void Build()
    {
        if (sprite == null) sprite = MakeSprite();
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        target = MakeRenderer("ParryRingTarget", SortOrder);
        approach = MakeRenderer("ParryRingApproach", SortOrder + 1);
    }

    SpriteRenderer MakeRenderer(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        Vector3 s = transform.lossyScale; // world size stays 1 px = 1/32 u whatever the owner's scale
        go.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = mat;
        sr.sortingLayerName = "FX";
        sr.sortingOrder = order;
        sr.enabled = false;
        return sr;
    }

    // White 1 px midpoint circle (tinted by the renderer) with a dark 1 px rim on its outer side; built once.
    static Sprite MakeSprite()
    {
        int size = 2 * (Radius + 2) + 1, c = Radius + 2;
        var px = new Color32[size * size]; // all clear
        int x = Radius, y = 0, err = 1 - Radius;
        while (x >= y)
        {
            for (int i = 0; i < 8; i++)
            {
                int dx = (i & 1) == 0 ? x : y, dy = (i & 1) == 0 ? y : x;
                if ((i & 2) != 0) dx = -dx;
                if ((i & 4) != 0) dy = -dy;
                px[(c + dy) * size + c + dx] = new Color32(255, 255, 255, 255);
            }
            y++;
            if (err < 0) err += 2 * y + 1;
            else { x--; err += 2 * (y - x) + 1; }
        }
        for (int ty = 1; ty < size - 1; ty++)
            for (int tx = 1; tx < size - 1; tx++)
            {
                int i = ty * size + tx;
                int ddx = tx - c, ddy = ty - c;
                if (px[i].a != 0 || ddx * ddx + ddy * ddy <= Radius * Radius) continue; // outer side only
                if (px[i - 1].a == 255 || px[i + 1].a == 255 || px[i - size].a == 255 || px[i + size].a == 255) // ring pixels are the only opaque ones
                    px[i] = RimColor;
            }
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();
        // Pivot on the centre pixel's lower-left corner, so a position snapped to 1/32 puts every texel on the world pixel grid.
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2((float)c / size, (float)c / size), Ppu);
    }
}
