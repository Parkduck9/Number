using System.Collections.Generic;
using UnityEngine;

// DECISIONS 16 neon grapple rope, drawn in code (no asset): a thin cyan core over a wider low-alpha glow, a polyline of stretched 1x1
// squares from the hand to the tip, on the FX sorting layer. Scaled time like SpriteFx, so it freezes with the hit-stop.
// Flight = travelling whip wave + hook head; hit = damped snap + a wrap knot at the anchor; pull = shrinking wobble; miss = droops slack.
// PlayerController drives it: Shoot (out to the target; a miss snaps back by itself), Hide (release / cancel).
public class GrappleRope : MonoBehaviour
{
    static readonly Color Core = new Color(0.31f, 0.94f, 1f);          // #4FF0FF
    static readonly Color Glow = new Color(0.31f, 0.94f, 1f, 0.28f);
    static readonly Color Head = new Color(0.85f, 1f, 1f);
    const float CoreWidth = 2f / 32f, GlowWidth = 6f / 32f;          // 2 px core, 6 px glow at PPU 32
    const int Points = 16;                                            // (T) polyline points; 15 segments x (core + glow)
    const float WhipAmp = 0.45f;       // (T) u: wave height mid-rope in flight (clamped to 25% of the rope length)
    const float Waves = 1.5f;          // (T) wave cycles along the rope
    const float WaveSpeed = 40f;       // (T) rad/s the wave travels toward the tip
    const float SnapDamp = 14f;        // (T) 1/s: how fast the wave dies after the hook bites
    const float SnapFreq = 38f;        // (T) rad/s of the taut-rope jolt
    const float PullAmp = 0.12f;       // (T) u: wobble while pulled at full range; shrinks to 0 as the player nears the anchor
    const float MissDamp = 7f;         // (T) 1/s: wave decay on a miss
    const float SagAmp = 0.22f;        // (T) slack droop depth as a fraction of the rope length (miss retract)
    const float HeadSize = 0.2f;       // (T) u hook head
    const float KnotPop = 0.08f;       // (T) s the wrap grows in
    const float KnotRx = 0.13f, KnotRy = 0.06f; // (T) u wrap loop radii (across / along the rope)
    const int KnotLoops = 2, KnotSteps = 8;

    PlayerController player;
    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    readonly Vector2[] pts = new Vector2[Points];
    int used;
    Vector2 target;
    bool active, hit;
    float t;

    public static GrappleRope Create(PlayerController player)
    {
        var rope = new GameObject("GrappleRope").AddComponent<GrappleRope>();
        rope.player = player;
        return rope;
    }

    public void Shoot(Vector2 to, bool attached)
    {
        target = to;
        hit = attached;
        t = 0f;
        active = true;
        Draw();
    }

    public void Hide()
    {
        active = false;
        used = 0;
        Flush();
    }

    void LateUpdate() // after the player moved this frame, so the rope leaves the hand
    {
        if (!active) return;
        if (player == null) { Destroy(gameObject); return; }
        float prevT = t;
        t += Time.deltaTime;
        if (hit && prevT < DashRules.GrappleExtendTime && t >= DashRules.GrappleExtendTime) MomentFx.GrappleHit(target); // hook just attached
        if (!hit && t >= DashRules.GrappleExtendTime + DashRules.GrappleRetractTime) { Hide(); return; }
        Draw();
    }

    void Draw()
    {
        used = 0;
        Vector2 from = player.HandPosition;
        float ext = DashRules.GrappleExtendTime, since = Mathf.Max(0f, t - ext);
        // Out over GrappleExtendTime; a miss then comes back over GrappleRetractTime.
        float back = !hit ? Mathf.Clamp01(since / DashRules.GrappleRetractTime) : 0f;
        float k = back > 0f ? 1f - back : Mathf.Clamp01(t / ext);
        Vector2 tip = Vector2.Lerp(from, target, k);
        Vector2 d = tip - from;
        float len = d.magnitude;
        Vector2 dir = len > 0.001f ? d / len : Vector2.right, n = new Vector2(-dir.y, dir.x);

        float amp = WhipAmp;
        if (hit) amp = WhipAmp * Mathf.Exp(-since * SnapDamp) * Mathf.Cos(since * SnapFreq) // damped spring jolt, then a taut wobble
                       + (t >= ext ? PullAmp * Mathf.Clamp01(len / DashRules.GrappleRange) : 0f);
        else amp *= Mathf.Exp(-since * MissDamp);
        amp = Mathf.Clamp(amp, -len * 0.25f, len * 0.25f);
        float sag = SagAmp * len * Mathf.Clamp01(back * 3f); // miss: goes slack
        for (int i = 0; i < Points; i++)
        {
            float u = i / (float)(Points - 1), bow = Mathf.Sin(Mathf.PI * u); // 0 at hand and tip
            pts[i] = Vector2.Lerp(from, tip, u) + n * (amp * bow * Mathf.Sin(u * Waves * 2f * Mathf.PI - t * WaveSpeed)) + Vector2.down * (sag * bow);
        }

        float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 30f, 0.37f); // neon buzz
        Color glow = Glow, core = Core;
        glow.a *= flicker; core.a *= flicker;
        for (int i = 1; i < Points; i++) Seg(pts[i - 1], pts[i], GlowWidth, glow, 120);
        for (int i = 1; i < Points; i++) Seg(pts[i - 1], pts[i], CoreWidth, core, 121);

        // Hook head: a small diamond at the tip.
        var h = Seg(tip, tip, HeadSize, Head, 122);
        h.transform.localScale = new Vector3(HeadSize, HeadSize, 1f);
        h.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 45f);

        // Wrap knot: a few tilted loops around the rope end at the anchor, popping in on attach.
        if (hit && t >= ext)
        {
            float pop = Mathf.Clamp01(since / KnotPop);
            for (int r = 0; r < KnotLoops; r++)
            {
                Vector2 c = target - dir * (0.1f + 0.14f * r);
                float tilt = (r % 2 == 0 ? 0.45f : -0.45f), ct = Mathf.Cos(tilt), st = Mathf.Sin(tilt);
                Vector2 prev = default;
                for (int s = 0; s <= KnotSteps; s++)
                {
                    float a = s * 2f * Mathf.PI / KnotSteps;
                    float x = Mathf.Cos(a) * KnotRx * pop, y = Mathf.Sin(a) * KnotRy * pop;
                    Vector2 p = c + n * (x * ct - y * st) + dir * (x * st + y * ct);
                    if (s > 0) Seg(prev, p, CoreWidth, Head, 122);
                    prev = p;
                }
            }
        }
        Flush();
    }

    // One stretched square from a to b (slightly longer so joints have no gaps), taken from the pool.
    SpriteRenderer Seg(Vector2 a, Vector2 b, float width, Color c, int order)
    {
        if (used == pool.Count)
        {
            var sr = new GameObject("Seg").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.sprite = ParryFx.Square;
            sr.sortingLayerID = SortingLayer.NameToID("FX"); // 0 (Default) if missing
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

    void Flush() { for (int i = used; i < pool.Count; i++) pool[i].enabled = false; }
}
