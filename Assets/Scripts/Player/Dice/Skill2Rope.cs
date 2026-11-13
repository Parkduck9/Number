using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dice face 2, 오랏줄 binding rope (DECISIONS 18/19): enemies are held while a curvy neon rope with talisman-yellow knots
// whirls one full turn around the hero (radius R); every on-screen enemy within R is struck (piercing) as the rope passes it.
// The hold ends with the whirl, so the struck bodies all fly at once. Drawn like GrappleRope (stretched squares, FX layer).
public static class Skill2Rope
{
    public const float Radius = 6.75f;  // (T) R (units): DECISIONS 27 longer rope, 1.5x the original 4.5
    const float Reach = 0.07f;   // (T) s the rope shoots out to R
    const float Sweep = 0.32f;   // (T) s for one full turn
    const float Retract = 0.06f; // (T) s the rope comes back
    const float Bend = 1.1f;     // (T) rad the tip trails behind the hand end (whip curve)
    const float TrailArc = 1.9f; // (T) rad of afterglow behind the tip (the "wide curvature" path)
    const int Points = 16, TrailSteps = 12;
    const float CoreW = 2f * DiceSkillKit.Px, GlowW = 6f * DiceSkillKit.Px, KnotSize = 4f * DiceSkillKit.Px;

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        Vector2 o = p.Body.position;
        var targets = new List<TestEnemy>();
        foreach (TestEnemy e in DiceSkillKit.Targets(ctx, o))
        {
            Collider2D col = e.BodyCollider;
            Vector2 cp = col != null ? col.ClosestPoint(o) : DiceSkillKit.Center(e);
            if ((cp - o).sqrMagnitude <= Radius * Radius) targets.Add(e);
        }

        DiceSkillKit.Caption("BINDING ROPE", "오랏줄", DiceSkillKit.Yellow);
        Sfx.Play(SfxId.Swing);
        // Starts behind the hero and swings overhead toward the facing side.
        int dir = p.Facing > 0 ? -1 : 1;
        float a0 = p.Facing > 0 ? Mathf.PI : 0f;
        var hit = new HashSet<TestEnemy>();
        DiceSkillKit.Segs segs = DiceSkillKit.Segs.Create("DiceRope");
        bool completed = false;

        try
        {
            DiceSkillKit.Hold();
            float total = Reach + Sweep + Retract;
            for (float t = 0f; t < total; t += Time.deltaTime)
            {
                if (DiceSkillKit.PlayerGone(ctx)) yield break;
                o = p.Body.position;
                float k = Mathf.Clamp01((t - Reach) / Sweep);
                float swept = 2f * Mathf.PI * k * k * (3f - 2f * k); // smoothstep: wind-up, whip, settle
                float len = Radius * (t < Reach ? t / Reach : t > Reach + Sweep ? 1f - (t - Reach - Sweep) / Retract : 1f);
                Draw(segs, o, a0 + dir * swept, dir, Mathf.Max(0f, len), t, swept);

                if (t >= Reach)
                    foreach (TestEnemy e in targets)
                        if (!hit.Contains(e) && Passed(DiceSkillKit.Center(e) - o, a0, dir, swept))
                        {
                            hit.Add(e);
                            DiceSkillKit.StunOrStrike(ctx, e);
                        }
                yield return null;
            }
            completed = true;
        }
        finally
        {
            if (completed) // a frame step that jumped past the last angles
                foreach (TestEnemy e in targets) if (!hit.Contains(e)) DiceSkillKit.StunOrStrike(ctx, e);
            DiceSkillKit.ReleaseHold();
            if (segs != null) Object.Destroy(segs.gameObject);
        }
    }

    // True once the sweep (radians from a0 in direction dir) has reached the direction of `d`.
    static bool Passed(Vector2 d, float a0, int dir, float swept)
    {
        float delta = Mathf.Repeat((Mathf.Atan2(d.y, d.x) - a0) * dir, 2f * Mathf.PI);
        return swept >= delta;
    }

    static readonly Vector2[] pts = new Vector2[Points];

    static Vector2 Dir(float a) => new Vector2(Mathf.Cos(a), Mathf.Sin(a));

    static void Draw(DiceSkillKit.Segs s, Vector2 o, float ang, int dir, float len, float t, float swept)
    {
        s.Begin();
        if (len <= 0.01f) { s.Flush(); return; }
        Color cyan = DiceSkillKit.Cyan;
        float tipAng = ang - dir * Bend;

        // Afterglow arc behind the tip.
        float arc = Mathf.Min(swept, TrailArc);
        Vector2 prev = o + Dir(tipAng) * len;
        for (int j = 1; j <= TrailSteps; j++)
        {
            float u = j / (float)TrailSteps;
            Vector2 q = o + Dir(tipAng - dir * arc * u) * len;
            if (arc > 0.01f) s.Seg(prev, q, 3f * DiceSkillKit.Px, DiceSkillKit.A(cyan, 0.5f * (1f - u)), 117);
            prev = q;
        }

        // The rope: tip trails behind the hand end, with a small travelling wave.
        for (int i = 0; i < Points; i++)
        {
            float u = i / (float)(Points - 1);
            float r = len * u + 0.1f * u * Mathf.Sin(u * 3f * Mathf.PI - t * 35f);
            pts[i] = o + Dir(ang - dir * Bend * u * u) * r;
        }
        float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 30f, 0.71f); // neon buzz like GrappleRope
        for (int i = 1; i < Points; i++) s.Seg(pts[i - 1], pts[i], GlowW, DiceSkillKit.A(cyan, 0.28f * flicker), 118);
        for (int i = 1; i < Points; i++) s.Seg(pts[i - 1], pts[i], CoreW, DiceSkillKit.A(cyan, flicker), 119);
        for (int i = 3; i < Points - 1; i += 3) s.Dot(pts[i], KnotSize, DiceSkillKit.Yellow, 120, 45f); // talisman knots

        // Tip: a small yellow tag with a red mark.
        Vector2 tip = pts[Points - 1];
        s.Dot(tip, 6f * DiceSkillKit.Px, DiceSkillKit.Yellow, 121, 45f);
        s.Dot(tip, 2f * DiceSkillKit.Px, DiceSkillKit.Red, 122);
        s.Flush();
    }
}
