using System.Collections.Generic;
using UnityEngine;

// Elite duel finisher (EliteDuel): the body's current sprite cut into a 4x4 grid of cells that fly outward with gravity, no rotation,
// pixel-snapped, stepped alpha fade, plus a few digit-coloured shards where the head number was. Cells are cached per sprite.
// Scaled time (freezes in the hit-stop and the pause menu); the first SlowTime runs at SlowMul for the slow-mo feel (no timeScale).
public class ShatterFx : MonoBehaviour
{
    const int Grid = 4;
    const float Ppu = 32f;
    const float Life = 0.7f;                     // (T) s
    const float Gravity = 22f;                   // (T) u/s^2
    const float SlowTime = 0.4f, SlowMul = 0.35f; // (T)
    const int Shards = 3;
    const int SortOrder = 100;                   // FX layer

    static readonly Dictionary<Sprite, Sprite[]> cells = new Dictionary<Sprite, Sprite[]>();
    static Material mat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { cells.Clear(); mat = null; } // Domain Reload is off

    SpriteRenderer[] parts;
    Vector2[] pos, vel;
    Color[] colors;
    float t;

    // blow = direction of the finishing blow (unit-ish); digitAt / digitColor = where the head number was and its colour.
    public static void Play(SpriteRenderer src, Vector2 blow, Vector2 digitAt, Color digitColor)
    {
        if (src == null || src.sprite == null) return;
        if (!cells.TryGetValue(src.sprite, out Sprite[] c)) cells[src.sprite] = c = Cut(src.sprite);
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        new GameObject("Shatter").AddComponent<ShatterFx>().Build(src, c, blow, digitAt, digitColor);
    }

    static Sprite[] Cut(Sprite s)
    {
        Rect r = s.rect; // no sprite atlas in this project: rect == texture rect
        float w = Mathf.Floor(r.width / Grid), h = Mathf.Floor(r.height / Grid);
        var c = new Sprite[Grid * Grid];
        for (int j = 0; j < Grid; j++)
            for (int i = 0; i < Grid; i++)
                c[j * Grid + i] = Sprite.Create(s.texture, new Rect(r.x + i * w, r.y + j * h, w, h), new Vector2(0.5f, 0.5f), s.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        return c;
    }

    void Build(SpriteRenderer src, Sprite[] c, Vector2 blow, Vector2 digitAt, Color digitColor)
    {
        int n = c.Length + Shards;
        parts = new SpriteRenderer[n];
        pos = new Vector2[n];
        vel = new Vector2[n];
        colors = new Color[n];
        Transform st = src.transform;
        Vector2 pivot = src.sprite.pivot;
        float w = c[0].rect.width, h = c[0].rect.height, ppu = src.sprite.pixelsPerUnit;
        Vector2 centre = st.TransformPoint(new Vector3((Grid * w * 0.5f - pivot.x) / ppu * (src.flipX ? -1f : 1f), (Grid * h * 0.5f - pivot.y) / ppu, 0f));
        for (int j = 0; j < Grid; j++)
            for (int i = 0; i < Grid; i++)
            {
                int k = j * Grid + i;
                float lx = ((i + 0.5f) * w - pivot.x) / ppu, ly = ((j + 0.5f) * h - pivot.y) / ppu;
                if (src.flipX) lx = -lx;
                pos[k] = st.TransformPoint(new Vector3(lx, ly, 0f));
                Vector2 out_ = pos[k] - centre;
                vel[k] = (out_.sqrMagnitude > 0.0001f ? out_.normalized : Vector2.up) * Random.Range(2f, 4.5f) + blow * Random.Range(1.5f, 3.5f) + new Vector2(0f, 3f);
                colors[k] = src.color;
                parts[k] = MakePart(c[k], st.lossyScale, src.flipX);
            }
        for (int k = c.Length; k < n; k++) // head number shards: 2-3 px squares
        {
            pos[k] = digitAt;
            vel[k] = new Vector2(Random.Range(-3f, 3f) + blow.x * 2f, Random.Range(4f, 7f));
            colors[k] = digitColor;
            float px = (k % 2 == 0 ? 3f : 2f) / Ppu;
            parts[k] = MakePart(ParryFx.Square, new Vector3(px, px, 1f), false);
        }
        Place();
    }

    SpriteRenderer MakePart(Sprite s, Vector3 scale, bool flipX)
    {
        var go = new GameObject("Part");
        go.transform.SetParent(transform, false);
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.flipX = flipX;
        sr.sharedMaterial = mat;
        sr.sortingLayerName = "FX";
        sr.sortingOrder = SortOrder;
        return sr;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= Life) { Destroy(gameObject); return; }
        float dt = Time.deltaTime * (t < SlowTime ? SlowMul : 1f);
        for (int k = 0; k < parts.Length; k++)
        {
            vel[k].y -= Gravity * dt;
            pos[k] += vel[k] * dt;
        }
        Place();
    }

    // Pixel-snapped positions, alpha in 4 steps.
    void Place()
    {
        float a = Mathf.Ceil((1f - t / Life) * 4f) / 4f;
        for (int k = 0; k < parts.Length; k++)
        {
            parts[k].transform.position = new Vector3(Mathf.Round(pos[k].x * Ppu) / Ppu, Mathf.Round(pos[k].y * Ppu) / Ppu, 0f);
            Color c = colors[k];
            c.a *= a;
            parts[k].color = c;
        }
    }
}
