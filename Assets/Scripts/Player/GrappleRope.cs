using System.Collections.Generic;
using UnityEngine;

// DECISIONS 16 / 22 RMB talisman throw, drawn in code (no asset): a 7x11 px talisman (부적) flies from the hand to the target and
// sticks there; a thin talisman-yellow thread (curvy polyline of stretched 1x1 squares) links hand and talisman while it flies and
// pulls. Miss: the talisman flies out and burns away (a few yellow / red embers). Unlit (Sprites/Default) on the FX sorting layer;
// scaled time like SpriteFx, so it freezes with the hit-stop.
// PlayerController drives it: Shoot (out to the target; a miss burns by itself), Hide (release / cancel: a stuck talisman burns away).
// When the talisman reaches its end point it calls PlayerController.TalismanPathKill (humans on the throw path die).
public class GrappleRope : MonoBehaviour
{
    static readonly Color Paper = new Color32(0xF2, 0xC9, 0x4C, 0xFF); // #F2C94C
    static readonly Color Glyph = new Color32(0xC8, 0x32, 0x3C, 0xFF); // #C8323C
    static readonly Color Rim = new Color32(0x14, 0x12, 0x1C, 0xFF);   // #14121C
    static readonly Color Thread = new Color(0.949f, 0.788f, 0.298f, 0.6f);
    const float Ppu = 32f;
    const float ThreadWidth = 1f / Ppu;  // 1 px
    const int Points = 16;               // (T) polyline points
    const float WhipAmp = 0.3f;          // (T) u: wave height mid-thread in flight (clamped to 25% of its length)
    const float Waves = 1.5f;            // (T) wave cycles along the thread
    const float WaveSpeed = 40f;         // (T) rad/s the wave travels toward the talisman
    const float SnapDamp = 14f;          // (T) 1/s: how fast the wave dies after the talisman sticks
    const float SnapFreq = 38f;          // (T) rad/s of the taut-thread jolt
    const float PullAmp = 0.08f;         // (T) u: wobble while pulled at full range; shrinks to 0 near the talisman
    const float Spin = 2f;               // (T) turns in flight
    const float StickPop = 0.08f;        // (T) s: the stuck talisman pops from 1.4x to 1x
    const float BurnTime = 0.25f;        // (T) s: miss / release burn (visual only; movement rules are in DashRules)
    const int Embers = 6;

    static Sprite talismanSprite;
    static Material mat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { talismanSprite = null; mat = null; }

    PlayerController player;
    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    readonly Vector2[] pts = new Vector2[Points];
    readonly Vector2[] emberVel = new Vector2[Embers];
    SpriteRenderer talisman;
    int used;
    Vector2 from, target;
    float angle;               // talisman rotation (top points along the throw)
    bool active, hit, burning, arrived;
    float t, burnT;

    public static GrappleRope Create(PlayerController player)
    {
        var rope = new GameObject("TalismanThrow").AddComponent<GrappleRope>();
        rope.player = player;
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        if (talismanSprite == null) talismanSprite = MakeTalisman();
        rope.talisman = rope.MakeRenderer("Talisman", 123);
        rope.talisman.sprite = talismanSprite;
        rope.talisman.enabled = false;
        return rope;
    }

    // from = the hand at the throw (start of the kill path), to = hit point or the range end on a miss.
    public void Shoot(Vector2 from, Vector2 to, bool attached)
    {
        this.from = from;
        target = to;
        hit = attached;
        t = 0f;
        active = true;
        burning = arrived = false;
        Vector2 d = to - from;
        angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
        Draw();
    }

    // Release / cancel: a talisman that already stuck burns away where it is. One still in flight (LMB / Shift released the throw
    // during the extend) finishes its flight like a miss (path kill, then burns); in dialogue / on a disabled player it just vanishes.
    public void Hide()
    {
        if (active && hit && !burning)
        {
            if (arrived) { StartBurn(); return; }
            if (!GameFlow.InputLocked && player != null && player.isActiveAndEnabled) { hit = false; return; }
        }
        active = burning = false;
        used = 0;
        talisman.enabled = false;
        Flush();
    }

    void StartBurn()
    {
        burning = true;
        burnT = 0f;
        for (int i = 0; i < Embers; i++) // spread up and out (scaled-time Random at the moment of the burn, not per frame)
            emberVel[i] = new Vector2(Random.Range(-0.9f, 0.9f), Random.Range(0.6f, 1.6f));
        Draw();
    }

    void LateUpdate() // after the player moved this frame, so the thread leaves the hand
    {
        if (!active) return;
        if (burning)
        {
            burnT += Time.deltaTime;
            if (burnT >= BurnTime) { active = burning = false; talisman.enabled = false; used = 0; Flush(); return; }
            Draw();
            return;
        }
        if (player == null) { Destroy(gameObject); return; }
        t += Time.deltaTime;
        if (!arrived && t >= DashRules.GrappleExtendTime)
        {
            arrived = true;
            if (hit) MomentFx.GrappleHit(target); // talisman just stuck
            player.TalismanPathKill(from, target); // humans on the path die as it lands
            if (!hit) { StartBurn(); return; }
        }
        Draw();
    }

    void Draw()
    {
        used = 0;
        if (burning) { DrawBurn(); Flush(); return; }
        Vector2 hand = player.HandPosition;
        float ext = DashRules.GrappleExtendTime, since = Mathf.Max(0f, t - ext);
        float k = Mathf.Clamp01(t / ext);
        Vector2 tip = Vector2.Lerp(hand, target, k);
        Vector2 d = tip - hand;
        float len = d.magnitude;
        Vector2 dir = len > 0.001f ? d / len : Vector2.right, n = new Vector2(-dir.y, dir.x);

        float amp = WhipAmp;
        if (hit && t >= ext) amp = WhipAmp * Mathf.Exp(-since * SnapDamp) * Mathf.Cos(since * SnapFreq) // damped jolt, then a taut wobble
                                   + PullAmp * Mathf.Clamp01(len / DashRules.GrappleRange);
        amp = Mathf.Clamp(amp, -len * 0.25f, len * 0.25f);
        for (int i = 0; i < Points; i++)
        {
            float u = i / (float)(Points - 1), bow = Mathf.Sin(Mathf.PI * u); // 0 at hand and talisman
            pts[i] = Vector2.Lerp(hand, tip, u) + n * (amp * bow * Mathf.Sin(u * Waves * 2f * Mathf.PI - t * WaveSpeed));
        }
        for (int i = 1; i < Points; i++) Seg(pts[i - 1], pts[i], ThreadWidth, Thread, 121);

        // Talisman: spins in flight, sticks upright along the throw with a small pop.
        float rot = k < 1f ? angle + (1f - k) * Spin * 360f : angle;
        float s = hit && t >= ext && since < StickPop ? Mathf.Lerp(1.4f, 1f, since / StickPop) : 1f;
        PlaceTalisman(tip, rot, s, 1f);
        Flush();
    }

    // The talisman shrinks and fades from the bottom up while embers (yellow / red pixels) rise from it.
    void DrawBurn()
    {
        float k = burnT / BurnTime;
        PlaceTalisman(target, angle, 1f - 0.6f * k, 1f - k);
        for (int i = 0; i < Embers; i++)
        {
            Vector2 p = target + emberVel[i] * burnT * 1.6f;
            p = new Vector2(Mathf.Round(p.x * Ppu) / Ppu, Mathf.Round(p.y * Ppu) / Ppu); // pixel grid
            Color c = i % 3 == 0 ? Glyph : Paper;
            c.a = 1f - k * k;
            Seg(p, p, 1f / Ppu, c, 124).transform.localScale = new Vector3(1f / Ppu, 1f / Ppu, 1f);
        }
    }

    void PlaceTalisman(Vector2 at, float rot, float scale, float alpha)
    {
        talisman.enabled = true;
        talisman.transform.SetPositionAndRotation(new Vector3((Mathf.Floor(at.x * Ppu) + 0.5f) / Ppu, (Mathf.Floor(at.y * Ppu) + 0.5f) / Ppu, 0f), Quaternion.Euler(0f, 0f, rot));
        talisman.transform.localScale = new Vector3(scale, scale, 1f);
        talisman.color = new Color(1f, 1f, 1f, alpha);
    }

    // One stretched square from a to b (slightly longer so joints have no gaps), taken from the pool.
    SpriteRenderer Seg(Vector2 a, Vector2 b, float width, Color c, int order)
    {
        if (used == pool.Count)
        {
            var sr = MakeRenderer("Seg", order);
            sr.sprite = ParryFx.Square;
            pool.Add(sr);
        }
        var s = pool[used++];
        Vector2 d = b - a;
        s.enabled = true;
        s.sortingOrder = order;
        s.color = c;
        s.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
        s.transform.localScale = new Vector3(d.magnitude + width * 0.5f, width, 1f);
        return s;
    }

    SpriteRenderer MakeRenderer(string name, int order)
    {
        var sr = new GameObject(name).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(transform, false);
        sr.sharedMaterial = mat; // unlit: 2D lights must not dim it
        sr.sortingLayerID = SortingLayer.NameToID("FX"); // 0 (Default) if missing
        sr.sortingOrder = order;
        return sr;
    }

    void Flush() { for (int i = used; i < pool.Count; i++) pool[i].enabled = false; }

    // 7x11 px: ink rim, yellow paper, red glyph (top bar, short bar, stem, two feet). Point filter, built once.
    static Sprite MakeTalisman()
    {
        const int w = 7, h = 11;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool rim = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                bool glyph = (y == 8 && x >= 1 && x <= 5) || (y == 6 && x >= 2 && x <= 4) || (x == 3 && y >= 3 && y <= 7)
                             || (y == 2 && (x == 1 || x == 2 || x == 4 || x == 5));
                px[y * w + x] = rim ? Rim : glyph ? Glyph : Paper;
            }
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Talisman", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
    }
}
