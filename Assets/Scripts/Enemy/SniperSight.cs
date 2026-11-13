using UnityEngine;

// Sniper aim line (DECISIONS 15): a dotted line of small red pixel squares from the muzzle, like the boss's aim tell.
// Tracking = small dots, slow blink, marching toward the hero; locked = bigger, brighter, fast blink.
// Only the part of the line inside the camera view is drawn (fixed dot pool). Owned by TestEnemy (Kind.Sniper), built on first use.
// Removal: delete this file and the Kind.Sniper branches in TestEnemy.
public class SniperSight : MonoBehaviour
{
    const int PoolSize = 90;
    const float Px = 1f / 32f, Spacing = 0.5f, ViewMargin = 1f;
    const float TrackSize = 3f * Px, LockSize = 4f * Px;
    const float TrackBlinkHz = 4f, LockBlinkHz = 18f, MarchSpeed = 1.5f;
    const float TrackAlphaHi = 0.9f, TrackAlphaLo = 0.45f, LockAlphaHi = 1f, LockAlphaLo = 0.55f;
    static readonly Color LaserRed = new Color(1f, 0.1647f, 0.1647f); // #FF2A2A
    static readonly Color LockRed = Color.Lerp(LaserRed, Color.white, 0.35f);
    static Material mat;

    SpriteRenderer[] dots;
    int shown; // dots currently enabled

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { mat = null; } // Domain Reload is off

    public static SniperSight For(GameObject owner)
    {
        var s = owner.GetComponent<SniperSight>();
        return s != null ? s : owner.AddComponent<SniperSight>();
    }

    void Build()
    {
        var old = transform.Find("SniperLaser"); // old clones carried a LineRenderer child
        if (old != null) old.gameObject.SetActive(false);
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        dots = new SpriteRenderer[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject("SniperDot");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ParryFx.Square;
            sr.sharedMaterial = mat;
            sr.sortingLayerName = "FX";
            sr.sortingOrder = 30; // same as the boss aim dots, under projectiles (50)
            sr.enabled = false;
            dots[i] = sr;
        }
    }

    static float Snap(float v) { return Mathf.Round(v * 32f) / 32f; }

    // from/to in world space. locked = the aim is fixed and the shot is coming.
    public void Show(Vector2 from, Vector2 to, bool locked)
    {
        if (dots == null) Build();
        Vector2 d = to - from;
        float len = d.magnitude;
        Camera cam = Camera.main;
        if (len < 0.01f || cam == null) { Hide(); return; }
        Vector2 dir = d / len;

        // Clip the segment (parameter in world units from the muzzle) to the camera rect + margin (Liang-Barsky).
        float halfH = cam.orthographicSize + ViewMargin, halfW = cam.orthographicSize * cam.aspect + ViewMargin;
        Vector2 c = cam.transform.position;
        float t0 = 0f, t1 = len;
        if (!Clip(from.x - c.x, dir.x, halfW, ref t0, ref t1) || !Clip(from.y - c.y, dir.y, halfH, ref t0, ref t1)) { Hide(); return; }

        // Dots sit at fixed distances along the line from the muzzle; the phase marches toward the hero.
        float phase = (Time.time * MarchSpeed) % Spacing;
        int k = Mathf.Max(0, Mathf.CeilToInt((t0 - phase) / Spacing));
        float size = locked ? LockSize : TrackSize;
        Vector3 ls = transform.lossyScale; // dots are children of the sniper: undo its scale / facing flip
        Vector3 dotScale = new Vector3(size / Mathf.Max(1e-4f, Mathf.Abs(ls.x)), size / Mathf.Max(1e-4f, Mathf.Abs(ls.y)), 1f);
        bool hi = Mathf.FloorToInt(Time.time * (locked ? LockBlinkHz : TrackBlinkHz) * 2f) % 2 == 0;
        Color col = locked ? LockRed : LaserRed;
        col.a = locked ? (hi ? LockAlphaHi : LockAlphaLo) : (hi ? TrackAlphaHi : TrackAlphaLo);

        int n = 0;
        for (; n < PoolSize; k++, n++)
        {
            float dist = k * Spacing + phase;
            if (dist > t1) break;
            Vector2 p = from + dir * dist;
            var sr = dots[n];
            sr.enabled = true;
            sr.transform.position = new Vector3(Snap(p.x), Snap(p.y), 0f);
            sr.transform.localScale = dotScale;
            sr.color = col;
        }
        for (int i = n; i < shown; i++) dots[i].enabled = false;
        shown = n;
    }

    // Narrows [t0,t1] to where origin + dir*t stays within +-half on one axis. False when fully outside.
    static bool Clip(float origin, float dir, float half, ref float t0, ref float t1)
    {
        if (Mathf.Abs(dir) < 1e-5f) return Mathf.Abs(origin) <= half;
        float a = (-half - origin) / dir, b = (half - origin) / dir;
        if (a > b) { float tmp = a; a = b; b = tmp; }
        if (a > t0) t0 = a;
        if (b < t1) t1 = b;
        return t0 <= t1;
    }

    public void Hide()
    {
        if (dots == null) return;
        for (int i = 0; i < shown; i++) dots[i].enabled = false;
        shown = 0;
    }
}
