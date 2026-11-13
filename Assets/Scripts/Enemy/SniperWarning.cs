using UnityEngine;

// DECISIONS 58: a sniper aiming from off-screen shows a blinking pixel warning ("!" triangle + chevron toward the sniper) at the screen
// edge in its direction. Tracking = slow blink; locked (SniperLockTime before the shot) = fast blink, bigger. Code-drawn, unlit.
// Owned by TestEnemy (Kind.Sniper) next to SniperSight. The marker lives under the camera (so it rides camera moves); this component hides it.
public class SniperWarning : MonoBehaviour
{
    const float Ppu = 32f;                     // same pixel size as the game art
    const float EdgeMargin = 1.0f;             // (T) u from the view edge to the triangle centre
    const float ChevronOffset = 0.5f;          // (T) u from the triangle toward the sniper
    const float TrackBlinkHz = 4f, LockBlinkHz = 16f;
    const float TrackDimAlpha = 0.3f, LockDimAlpha = 0.45f;
    const float LockScale = 1.3f;

    // '.' clear, 'o' outline, 'r' crimson, 'w' pale glyph
    static readonly string[] TrianglePx =
    {
        ".......o.......",
        "......oro......",
        "......oro......",
        ".....orrro.....",
        ".....orwro.....",
        "....orrwrro....",
        "....orrwrro....",
        "...orrrwrrro...",
        "...orrrrrrro...",
        "..orrrrwrrrro..",
        "..orrrrrrrrro..",
        ".orrrrrrrrrrro.",
        "ooooooooooooooo",
    };
    static readonly string[] ChevronPx = // points +x
    {
        "oo.....",
        "oro....",
        "orro...",
        ".orro..",
        "..orro.",
        "...orro",
        "..orro.",
        ".orro..",
        "orro...",
        "oro....",
        "oo.....",
    };
    static Sprite triangleSprite, chevronSprite;
    static Material mat;

    Transform marker, chevron;
    SpriteRenderer triSr, chevSr;

    public static SniperWarning For(GameObject owner)
    {
        var w = owner.GetComponent<SniperWarning>();
        return w != null ? w : owner.AddComponent<SniperWarning>();
    }

    // muzzle in world space. Shown only while the muzzle is outside the camera view.
    public void Show(Vector2 muzzle, bool locked)
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic) { Hide(); return; }
        Vector2 c = cam.transform.position;
        float hy = cam.orthographicSize, hx = hy * cam.aspect;
        Vector2 d = muzzle - c;
        if (Mathf.Abs(d.x) <= hx && Mathf.Abs(d.y) <= hy) { Hide(); return; } // on screen: the laser and the sniper are the tell
        if (marker == null) Build(cam);

        // where the ray centre -> muzzle meets the inset view rect
        float ix = Mathf.Max(hx - EdgeMargin, 0.1f), iy = Mathf.Max(hy - EdgeMargin, 0.1f);
        float tx = Mathf.Abs(d.x) > 0.0001f ? ix / Mathf.Abs(d.x) : float.MaxValue;
        float ty = Mathf.Abs(d.y) > 0.0001f ? iy / Mathf.Abs(d.y) : float.MaxValue;
        Vector2 p = d * Mathf.Min(tx, ty);
        Vector2 dir = d.normalized;
        marker.localPosition = new Vector3(p.x, p.y, -cam.transform.position.z); // world z 0
        chevron.localPosition = dir * ChevronOffset;
        chevron.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        marker.localScale = Vector3.one * (locked ? LockScale : 1f);

        float hz = locked ? LockBlinkHz : TrackBlinkHz;
        bool on = Mathf.FloorToInt(Time.time * hz) % 2 == 0;
        Color col = new Color(1f, 1f, 1f, on ? 1f : (locked ? LockDimAlpha : TrackDimAlpha));
        triSr.color = chevSr.color = col;
        if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (marker != null && marker.gameObject.activeSelf) marker.gameObject.SetActive(false);
    }

    void OnDisable() => Hide(); // knocked out / despawned: the marker is not our child, so hide it here

    void OnDestroy()
    {
        if (marker != null) Destroy(marker.gameObject);
    }

    void Build(Camera cam)
    {
        if (triangleSprite == null) triangleSprite = MakeSprite(TrianglePx);
        if (chevronSprite == null) chevronSprite = MakeSprite(ChevronPx);
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default")); // unlit like SniperSight: readable in the night lighting
        marker = new GameObject("SniperWarning").transform;
        marker.SetParent(cam.transform, false);
        triSr = AddRenderer(marker.gameObject, triangleSprite);
        chevron = new GameObject("Chevron").transform;
        chevron.SetParent(marker, false);
        chevSr = AddRenderer(chevron.gameObject, chevronSprite);
    }

    static SpriteRenderer AddRenderer(GameObject go, Sprite s)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sharedMaterial = mat;
        sr.sortingLayerName = "FX";
        sr.sortingOrder = 130;
        return sr;
    }

    static Sprite MakeSprite(string[] rows)
    {
        int h = rows.Length, w = rows[0].Length;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        var outline = new Color32(0x2A, 0x05, 0x08, 0xFF);
        var red = new Color32(0xE0, 0x14, 0x2A, 0xFF);   // crimson, like the boss tells
        var glyph = new Color32(0xFF, 0xE8, 0xE0, 0xFF);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                char ch = rows[h - 1 - y][x]; // texture rows go bottom-up
                px[y * w + x] = ch == 'o' ? outline : ch == 'r' ? red : ch == 'w' ? glyph : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Ppu);
    }
}
