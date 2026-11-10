using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dice face 6, 점괘 길 divination path (DECISIONS 18/19): reads the best route through every enemy on screen (nearest
// neighbour + 2-opt, the enemy closest to the stage exit fixed last), draws it as a numbered cyan line with a yellow dashed
// leg to the exit (0.35 s), then chains skill-1 blinks along it: blink behind, piercing strike, 0.12 s apart.
// Input is locked (enemies cannot react) and the hero is invulnerable throughout; both restored only if this skill set them.
// Exit = nearest StageGoal or LevelTransition (goal or gate) to the hero; none in the scene = an open route.
public static class Skill6Path
{
    const float DrawTime = 0.35f; // contract: the route shows before the chain
    const float Gap = 0.12f;      // contract: s between blinks
    const float EndHold = 0.15f;  // (T) s the finished route stays
    const float NodeLift = 0.55f; // (T) u the numbered node floats above the head
    const float NodeR = 9f * DiceSkillKit.Px;
    const float LegMax = 10f;     // (T) u: the dashed exit leg points the way, at most this long
    const float CoreW = 2f * DiceSkillKit.Px, GlowW = 6f * DiceSkillKit.Px;

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        Vector2 start = p.Body.position;
        var targets = DiceSkillKit.Targets(ctx, start);
        if (targets.Count == 0) { DiceSkillKit.Fizzle(ctx); yield break; }

        bool hasGoal = FindExit(start, out Vector2 goal);
        TestEnemy last = null;
        if (hasGoal)
        {
            float best = float.MaxValue;
            foreach (TestEnemy e in targets)
            {
                float d = (DiceSkillKit.Center(e) - goal).sqrMagnitude;
                if (d < best) { best = d; last = e; }
            }
        }
        List<TestEnemy> route = Route(start, targets, last);

        DiceSkillKit.Caption("DIVINATION PATH", "점괘 길", DiceSkillKit.Cyan);
        Sfx.Play(SfxId.HackOk);
        var nodes = new List<Vector2> { start };
        foreach (TestEnemy e in route) nodes.Add(DiceSkillKit.Head(e) + Vector2.up * NodeLift);
        float total = 0f;
        for (int i = 1; i < nodes.Count; i++) total += (nodes[i] - nodes[i - 1]).magnitude;
        if (hasGoal) total += Mathf.Min(LegMax, (goal - nodes[nodes.Count - 1]).magnitude);

        DiceSkillKit.Segs segs = DiceSkillKit.Segs.Create("DicePath");
        var labels = new List<GameObject>();
        for (int i = 1; i < nodes.Count; i++) labels.Add(Number(i, nodes[i]));

        try
        {
            DiceSkillKit.Lock();
            DiceSkillKit.SetInvulnerable(ctx);

            for (float t = 0f; t < DrawTime; t += Time.deltaTime)
            {
                Draw(segs, nodes, hasGoal, goal, total * Mathf.Clamp01(t / (DrawTime * 0.8f)), 0, labels);
                yield return null;
            }
            Draw(segs, nodes, hasGoal, goal, float.MaxValue, 0, labels);

            for (int i = 0; i < route.Count; i++)
            {
                if (DiceSkillKit.PlayerGone(ctx)) yield break;
                TestEnemy e = route[i];
                if (!DiceSkillKit.Alive(e)) continue; // died meanwhile
                Vector2 from = p.Body.position;
                p.TryBlinkBehind(e); // no free spot: the blow lands from where the hero is
                DiceSkillKit.Streak(from, p.Body.position, 0.3f);
                DiceSkillKit.Strike(ctx, e);
                Draw(segs, nodes, hasGoal, goal, float.MaxValue, i + 1, labels);
                yield return DiceSkillKit.Wait(Gap);
            }
            yield return DiceSkillKit.Wait(EndHold);
        }
        finally
        {
            DiceSkillKit.ReleaseLock();
            DiceSkillKit.ReleaseInvulnerable();
            if (segs != null) Object.Destroy(segs.gameObject);
            foreach (GameObject l in labels) if (l != null) Object.Destroy(l);
        }
    }

    // ---- route ----

    // Nearest neighbour from the hero, `last` (if any) appended, then 2-opt with both ends fixed (the hero; `last` when given).
    static List<TestEnemy> Route(Vector2 start, List<TestEnemy> enemies, TestEnemy last)
    {
        var rest = new List<TestEnemy>(enemies);
        if (last != null) rest.Remove(last);
        var path = new List<TestEnemy>();
        Vector2 cur = start;
        while (rest.Count > 0)
        {
            int bi = 0;
            float bd = float.MaxValue;
            for (int i = 0; i < rest.Count; i++)
            {
                float d = (DiceSkillKit.Center(rest[i]) - cur).sqrMagnitude;
                if (d < bd) { bd = d; bi = i; }
            }
            path.Add(rest[bi]);
            cur = DiceSkillKit.Center(rest[bi]);
            rest.RemoveAt(bi);
        }
        if (last != null) path.Add(last);

        // pts[0] = hero, pts[k] = path[k - 1]. Reversing pts[i..j] swaps edges (i-1,i) and (j,j+1) for (i-1,j) and (i,j+1).
        var pts = new List<Vector2> { start };
        foreach (TestEnemy e in path) pts.Add(DiceSkillKit.Center(e));
        int n = pts.Count;
        int hi = last != null ? n - 2 : n - 1; // last movable index
        bool improved = true;
        for (int guard = 0; improved && guard < 50; guard++)
        {
            improved = false;
            for (int i = 1; i < hi; i++)
                for (int j = i + 1; j <= hi; j++)
                {
                    bool tail = j + 1 < n;
                    float before = (pts[i] - pts[i - 1]).magnitude + (tail ? (pts[j + 1] - pts[j]).magnitude : 0f);
                    float after = (pts[j] - pts[i - 1]).magnitude + (tail ? (pts[j + 1] - pts[i]).magnitude : 0f);
                    if (after < before - 0.0001f)
                    {
                        pts.Reverse(i, j - i + 1);
                        path.Reverse(i - 1, j - i + 1);
                        improved = true;
                    }
                }
        }
        return path;
    }

    // The way to the next map: the nearest stage exit (StageGoal, LevelTransition goal or gate) to the hero.
    static bool FindExit(Vector2 from, out Vector2 exit)
    {
        exit = default;
        float best = float.MaxValue;
        foreach (StageGoal g in Object.FindObjectsByType<StageGoal>(FindObjectsSortMode.None))
            Consider(g.transform.position, from, ref best, ref exit);
        foreach (LevelTransition lt in Object.FindObjectsByType<LevelTransition>(FindObjectsSortMode.None))
            Consider(lt.transform.position, from, ref best, ref exit);
        return best < float.MaxValue;
    }

    static void Consider(Vector2 pos, Vector2 from, ref float best, ref Vector2 exit)
    {
        float d = (pos - from).sqrMagnitude;
        if (d < best) { best = d; exit = pos; }
    }

    // ---- drawing ----

    // Route up to `shown` units of length; nodes 1..struck are spent (drawn talisman yellow), the rest cyan.
    static void Draw(DiceSkillKit.Segs s, List<Vector2> nodes, bool hasGoal, Vector2 goal, float shown, int struck, List<GameObject> labels)
    {
        s.Begin();
        float left = shown;
        for (int i = 1; i < nodes.Count && left > 0f; i++)
        {
            Vector2 a = nodes[i - 1], b = nodes[i];
            float len = (b - a).magnitude;
            bool reached = left >= len;
            Vector2 end = reached ? b : a + (b - a) * (left / Mathf.Max(0.0001f, len));
            left -= len;
            Color c = i <= struck ? DiceSkillKit.Yellow : DiceSkillKit.Cyan;
            s.Seg(a, end, GlowW, DiceSkillKit.A(c, 0.28f), 118);
            s.Seg(a, end, CoreW, c, 119);
            if (!reached) break;
            s.Dot(b, NodeR * 1.6f, DiceSkillKit.A(DiceSkillKit.Ink, 0.75f), 120);
            s.RingAt(b, NodeR, 1f * DiceSkillKit.Px, c, 121);
            if (labels[i - 1] != null) labels[i - 1].SetActive(true);
        }
        if (hasGoal && left > 0f) // dashed yellow leg to the exit, then a diamond on it
        {
            Vector2 a = nodes[nodes.Count - 1], d = goal - a;
            float full = d.magnitude, len = Mathf.Min(left, Mathf.Min(full, LegMax));
            Vector2 dir = full > 0.0001f ? d / full : Vector2.right;
            for (float u = 0f; u < len; u += 0.4f)
                s.Seg(a + dir * u, a + dir * Mathf.Min(u + 0.25f, len), CoreW, DiceSkillKit.Yellow, 119);
            if (full <= LegMax && left >= full)
            {
                s.Dot(goal, 10f * DiceSkillKit.Px, DiceSkillKit.Yellow, 120, 45f);
                s.Dot(goal, 4f * DiceSkillKit.Px, DiceSkillKit.Red, 121, 45f);
            }
        }
        s.Flush();
    }

    // Route number with the cyan holo digits (hidden until the line reaches it). No sheet = ring only.
    static GameObject Number(int n, Vector2 pos)
    {
        var root = new GameObject("PathNumber");
        root.transform.position = pos;
        Sprite[] sheet = DiceSkillKit.CyanDigits();
        if (sheet != null)
        {
            string s = n.ToString();
            float w = sheet[0].bounds.size.x + DiceSkillKit.Px;
            for (int k = 0; k < s.Length; k++)
            {
                var sr = new GameObject("Digit").AddComponent<SpriteRenderer>();
                sr.sprite = sheet[s[k] - '0'];
                sr.sortingLayerID = DiceSkillKit.FxLayer;
                sr.sortingOrder = 124;
                sr.transform.SetParent(root.transform, false);
                sr.transform.localPosition = new Vector3((k - (s.Length - 1) * 0.5f) * w, 0f, 0f);
            }
        }
        root.SetActive(false);
        return root;
    }
}
