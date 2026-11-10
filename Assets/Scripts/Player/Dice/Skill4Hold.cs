using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dice face 4, 칼집 일섬 sheath cut (DECISIONS 32, replaces 18/27's time hold). The hero does not move: input locked, enemies held,
// hero invulnerable. The hero stands in the drawn-blade follow-through pose and the world greys (blue-grey quad on the FX layer, a
// full-colour copy of the hero above it); a thin white slash line appears across each of the nearest four on-screen enemies (DECISIONS 33), one by one (nearest first)
// with a tick; then the click (the sheath closes: sound + white flash), the colour comes back and every marked enemy is struck at
// once (piercing player hit, one shared hit-stop + shake). Boss fight: RunBoss draws the lines over the core while BossFight.Held
// freezes the boss; LuckyDice.BossSkill then calls Click + BossFight.SkillHit.
public static class Skill4Hold
{
    const float Lead = 0.4f;     // (T) s from the grey to the first line
    const float Gap = 0.1f;      // (T) s between lines (0.08 when more than 6)
    const float HoldEnd = 0.35f; // (T) s the lines hang before the click
    const float Recover = 0.3f;  // (T) s still invulnerable after the strike
    const float LineLen = 2.2f, BossLineLen = 3f; // (T) u
    const int BossLines = 4;
    const int MaxCuts = 4;       // field: at most four enemies (user, 11/10)
    const int TintOrder = 60, MirrorOrder = 62, LineOrder = 96; // FX layer: under the spark sheets (100+)
    static readonly Color Tint = new Color(0.36f, 0.42f, 0.54f, 0.45f); // blue-grey "stopped world"

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        List<TestEnemy> targets = DiceSkillKit.Targets(ctx, p.Body.position);
        if (targets.Count > MaxCuts) targets = targets.GetRange(0, MaxCuts); // nearest four (face 4 = 4 cuts, like face 5 = 5 talismans)
        var points = new List<Vector2>();
        foreach (TestEnemy e in targets) points.Add(DiceSkillKit.Center(e));
        DiceSkillKit.Caption("SHEATH CUT", "칼집 일섬", Color.white);
        Grey grey = null;
        try
        {
            DiceSkillKit.Lock();
            DiceSkillKit.Hold();
            DiceSkillKit.SetInvulnerable(ctx);
            grey = Grey.Create(ctx);
            IEnumerator c = Cuts(ctx, grey, points, LineLen);
            while (c.MoveNext()) yield return c.Current; // driven here, not nested: an exception in it reaches Roll's catch and this finally
            if (DiceSkillKit.PlayerGone(ctx)) yield break;

            grey.End(); // colour back
            DiceSkillKit.ReleaseHold(); // the hit-stop and the kill knockback need both flags off
            DiceSkillKit.ReleaseLock();
            Click(p);
            if (targets.Count == 0) DiceSkillKit.Fizzle(ctx);
            bool any = false;
            foreach (TestEnemy e in targets)
                if (DiceSkillKit.Alive(e)) { DiceSkillKit.Strike(ctx, e); any = true; }
            if (any) p.DuelHitStop(Juice.HitStopKill); // one shared stop (the strikes only extend it)
            yield return DiceSkillKit.Wait(Recover);
        }
        finally
        {
            if (grey != null) grey.End();
            DiceSkillKit.ReleaseHold();
            DiceSkillKit.ReleaseLock();
            DiceSkillKit.ReleaseInvulnerable();
        }
    }

    // Boss fight (DECISIONS 32): grey + frozen boss (BossFight.Held stops its timers and drawing; the lock freezes its shots), four lines
    // across the core (the chest when the core is up). Flags are released here, so BossSkill's InputLocked guard runs before Click + SkillHit.
    public static IEnumerator RunBoss(LuckyDice ctx, BossFight boss)
    {
        if (DiceSkillKit.PlayerGone(ctx) || boss == null) yield break;
        DiceSkillKit.Caption("SHEATH CUT", "칼집 일섬", Color.white);
        Grey grey = null;
        try
        {
            DiceSkillKit.Lock();
            DiceSkillKit.SetInvulnerable(ctx);
            BossFight.Held = true;
            grey = Grey.Create(ctx);
            var points = new List<Vector2>();
            for (int i = 0; i < BossLines; i++) points.Add(boss.CorePosition + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f)));
            IEnumerator c = Cuts(ctx, grey, points, BossLineLen);
            while (c.MoveNext()) yield return c.Current;
        }
        finally
        {
            BossFight.Held = false;
            if (grey != null) grey.End();
            DiceSkillKit.ReleaseLock();
            DiceSkillKit.ReleaseInvulnerable();
        }
    }

    // The sheath closes (field and boss): hero stands sheathed, a metal click, a white flash, a shake.
    public static void Click(PlayerController p)
    {
        if (p != null) p.HoldPose(DraftSprites.Pick(DraftSprites.HeroIdle, 0, DraftSprites.Hero), 0.35f);
        Sfx.Play(SfxId.Parry, 0.9f, 0.75f);
        DiceSkillKit.Overlay.Get().Flash(Color.white, 0.6f, 0.15f);
        Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime);
    }

    // Follow-through pose, then one thin white line per point (alternating diagonals, a tick each), then a short hang.
    static IEnumerator Cuts(LuckyDice ctx, Grey grey, List<Vector2> points, float len)
    {
        int n = points.Count;
        float gap = n > 6 ? 0.08f : Gap;
        ctx.Player.HoldPose(DraftSprites.Pick(DraftSprites.HeroAttack, 4, DraftSprites.Hero), Lead + n * gap + HoldEnd + 0.1f);
        DiceSkillKit.Overlay.Get().Flash(new Color(0.75f, 0.85f, 1f), 0.3f, 0.12f);
        Sfx.Play(SfxId.Swing, 0.8f, 0.7f);
        yield return DiceSkillKit.Wait(Lead);
        for (int i = 0; i < n; i++)
        {
            float a = Random.Range(25f, 55f) * (i % 2 == 0 ? 1f : -1f) * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (len * 0.5f);
            DiceSkillKit.Fade line = DiceSkillKit.Line(points[i] - d, points[i] + d, 2f * DiceSkillKit.Px, Color.white, 30f, LineOrder);
            line.fadeAt = 30f; // removed by Grey.End at the click
            line.pop = 0.06f;  // snaps in from a longer streak
            grey.Lines.Add(line.gameObject);
            DiceSkillKit.Line(points[i] - d * 1.2f, points[i] + d * 1.2f, 5f * DiceSkillKit.Px, DiceSkillKit.A(DiceSkillKit.Cyan, 0.45f), 0.1f, LineOrder - 1); // swish
            Sfx.Play(SfxId.Swing, 0.3f, 2.2f); // the tick
            yield return DiceSkillKit.Wait(gap);
        }
        yield return DiceSkillKit.Wait(HoldEnd);
    }

    // The grey world: tint quad (camera child) + colour mirror of the hero + the slash lines. End() is safe to call twice.
    sealed class Grey
    {
        SpriteRenderer tint;
        DiceSkillKit.Mirror mirror;
        public readonly List<GameObject> Lines = new List<GameObject>();

        public static Grey Create(LuckyDice ctx)
        {
            var g = new Grey();
            Camera cam = ctx.Cam != null ? ctx.Cam : Camera.main;
            if (cam != null) g.tint = BuildTint(cam);
            SpriteRenderer visual = ctx.Player.GetComponentInChildren<SpriteRenderer>(); // same lookup PlayerController uses for its visual
            if (visual != null) g.mirror = DiceSkillKit.Mirror.Create(visual, MirrorOrder);
            return g;
        }

        public void End()
        {
            if (tint != null) Object.Destroy(tint.gameObject);
            if (mirror != null) Object.Destroy(mirror.gameObject);
            foreach (GameObject l in Lines) if (l != null) Object.Destroy(l);
            tint = null;
            mirror = null;
            Lines.Clear();
        }
    }

    // Camera child, so it moves with the follow / shake without lagging a frame.
    static SpriteRenderer BuildTint(Camera cam)
    {
        var sr = new GameObject("DiceHoldTint").AddComponent<SpriteRenderer>();
        sr.sprite = ParryFx.Square;
        sr.color = Tint;
        sr.sortingLayerID = DiceSkillKit.FxLayer;
        sr.sortingOrder = TintOrder;
        sr.transform.SetParent(cam.transform, false);
        sr.transform.localPosition = new Vector3(0f, 0f, Mathf.Max(cam.nearClipPlane + 0.5f, -cam.transform.position.z)); // world z ~0
        float h = cam.orthographicSize * 2f + 1f; // +1: margin for the punch zoom
        sr.transform.localScale = new Vector3(h * cam.aspect + 1f, h, 1f);
        return sr;
    }
}
