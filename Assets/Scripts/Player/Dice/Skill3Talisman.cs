using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dice face 3, 부적 낙뢰 talisman lightning (DECISIONS 32, replaces 18/19's paralysis talismans). Input locked, enemies held, hero
// invulnerable: the hero winds up and throws, a talisman flies from the hand to every on-screen enemy (nearest first) and sticks on
// its chest; a beat, a finger snap, then a jagged lightning bolt drops from the top of the screen onto every marked enemy at the same
// instant (white flash, shake, one shared hit-stop). Androids overload for 3 s, everyone else takes a piercing player hit.
// Nobody on screen: the pose and the snap still play, then the talisman fizzles. Boss fight: RunBoss marks the core (the chest when
// the core is up); LuckyDice.BossSkill then calls Smite + BossFight.SkillHit.
public static class Skill3Talisman
{
    const float OverloadSeconds = 3f; // DECISIONS 18
    const float WindUp = 0.15f;       // (T) s wind-up pose before the throw
    const float Fly = 0.14f;          // (T) s for the first talisman to land
    const float Stagger = 0.035f;     // (T) s between talismans (nearest first)
    const float Beat = 0.4f;          // (T) s the marked enemies hang there before the snap
    const float SnapWait = 0.18f;     // (T) s from the snap to the bolts
    const float Recover = 0.3f;       // (T) s still invulnerable after the strike
    const float BoltLife = 0.24f, BoltFlick = 0.06f; // (T) s on screen / s per redraw (about 3-4 flicker frames)
    const int BoltJags = 7;
    static readonly Vector3[] BossTags = { new Vector3(-0.35f, 0.2f), new Vector3(0f, 0.4f), new Vector3(0.35f, 0.2f) }; // 3 on the core

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        List<TestEnemy> targets = DiceSkillKit.Targets(ctx, p.Body.position);
        DiceSkillKit.Caption("TALISMAN LIGHTNING", "부적 낙뢰", DiceSkillKit.Yellow);
        var follow = new List<Transform>();
        var offset = new List<Vector3>();
        foreach (TestEnemy e in targets)
        {
            follow.Add(e.transform);
            offset.Add((Vector3)(DiceSkillKit.Center(e) + Vector2.up * 0.15f) - e.transform.position); // on the chest
        }
        var tags = new List<DiceSkillKit.Fade>();
        try
        {
            DiceSkillKit.Lock();
            DiceSkillKit.Hold();
            DiceSkillKit.SetInvulnerable(ctx);
            IEnumerator m = Mark(ctx, follow, offset, tags);
            while (m.MoveNext()) yield return m.Current; // driven here, not nested: an exception in it reaches Roll's catch and this finally
            if (DiceSkillKit.PlayerGone(ctx)) yield break;
            if (targets.Count == 0) { DiceSkillKit.Fizzle(ctx); yield break; }

            var live = new List<TestEnemy>();
            var points = new List<Vector2>();
            foreach (TestEnemy e in targets)
                if (DiceSkillKit.Alive(e)) { live.Add(e); points.Add(DiceSkillKit.Center(e)); }
            foreach (DiceSkillKit.Fade t in tags) if (t != null) Object.Destroy(t.gameObject); // burnt by the bolt
            DiceSkillKit.ReleaseHold(); // the hit-stop and the kill knockback need both flags off
            DiceSkillKit.ReleaseLock();
            Smite(points, ctx.Cam);
            foreach (TestEnemy e in live)
            {
                if (e.IsAndroid) { e.Overload(OverloadSeconds); MomentFx.HackPulse(DiceSkillKit.Center(e)); }
                else DiceSkillKit.Strike(ctx, e);
            }
            if (live.Count > 0) p.DuelHitStop(Juice.HitStopKill); // one shared stop (the strikes only extend it)
            yield return DiceSkillKit.Wait(Recover);
        }
        finally
        {
            DiceSkillKit.ReleaseHold();
            DiceSkillKit.ReleaseLock();
            DiceSkillKit.ReleaseInvulnerable();
            foreach (DiceSkillKit.Fade t in tags) if (t != null) Object.Destroy(t.gameObject);
        }
    }

    // Boss fight (DECISIONS 32): pose, three talismans onto the core, beat, snap. The boss is frozen by the input lock; flags are
    // released here, so BossSkill's InputLocked guard (a duel / dialogue that took over) runs before Smite + SkillHit.
    public static IEnumerator RunBoss(LuckyDice ctx, BossFight boss)
    {
        if (DiceSkillKit.PlayerGone(ctx) || boss == null) yield break;
        DiceSkillKit.Caption("TALISMAN LIGHTNING", "부적 낙뢰", DiceSkillKit.Yellow);
        var follow = new List<Transform>();
        for (int i = 0; i < BossTags.Length; i++) follow.Add(boss.CoreTransform);
        var tags = new List<DiceSkillKit.Fade>();
        try
        {
            DiceSkillKit.Lock();
            DiceSkillKit.SetInvulnerable(ctx);
            IEnumerator m = Mark(ctx, follow, new List<Vector3>(BossTags), tags);
            while (m.MoveNext()) yield return m.Current;
        }
        finally
        {
            DiceSkillKit.ReleaseLock();
            DiceSkillKit.ReleaseInvulnerable();
            foreach (DiceSkillKit.Fade t in tags) if (t != null) Object.Destroy(t.gameObject);
        }
    }

    // Wind-up, throw (talismans fly to follow[i] + offset[i]), beat, snap. The caller owns the flags and destroys `tags`.
    static IEnumerator Mark(LuckyDice ctx, List<Transform> follow, List<Vector3> offset, List<DiceSkillKit.Fade> tags)
    {
        PlayerController p = ctx.Player;
        int n = follow.Count;
        float land = n > 0 ? Fly + (n - 1) * Stagger : Fly;
        p.HoldPose(DraftSprites.Pick(DraftSprites.HeroAttack, 0, DraftSprites.Hero), WindUp + 0.05f);
        yield return DiceSkillKit.Wait(WindUp);

        p.HoldPose(DraftSprites.Pick(DraftSprites.HeroAttack, 2, DraftSprites.Hero), land + Beat + 0.05f); // arm out: the throw, held through the beat
        Sfx.Play(SfxId.HackOk);
        Vector2 hand = p.HandPosition;
        for (int i = 0; i < n; i++)
        {
            if (follow[i] == null) continue;
            float at = Fly + i * Stagger;
            DiceSkillKit.Line(hand, (Vector2)(follow[i].position + offset[i]), 2f * DiceSkillKit.Px, DiceSkillKit.A(DiceSkillKit.Yellow, 0.7f), 0.12f); // throw streak
            DiceSkillKit.Fade tal = DiceSkillKit.Talisman(hand, 10f); // destroyed by the caller at the strike
            tal.follow = follow[i];
            tal.offset = offset[i];
            tal.flyFrom = hand;
            tal.flyTime = at;
            tal.fadeAt = 10f;
            tags.Add(tal);
        }
        yield return DiceSkillKit.Wait(land);
        if (n > 0) Sfx.Play(SfxId.Stunned, 0.6f);

        yield return DiceSkillKit.Wait(Beat);
        p.HoldPose(DraftSprites.Pick(DraftSprites.HeroParryPose(ParryHeight.High, null), 0, DraftSprites.Hero), SnapWait + 0.15f); // hand up: the snap
        Sfx.Play(SfxId.Parry, 0.5f, 1.9f); // the snap: a short high click
        hand = p.HandPosition;
        DiceSkillKit.Ring(hand, 0.12f, 1f * DiceSkillKit.Px, DiceSkillKit.A(Color.white, 0.9f), 0.15f, 3f, 98);
        DiceSkillKit.Overlay.Get().Flash(DiceSkillKit.Yellow, 0.12f, 0.08f);
        yield return DiceSkillKit.Wait(SnapWait);
    }

    // The strike instant (field and boss): a bolt from the top of the view onto every point, one white flash, a shake, the thunder.
    public static void Smite(List<Vector2> points, Camera cam)
    {
        if (cam == null) cam = Camera.main;
        float top = cam != null ? cam.transform.position.y + cam.orthographicSize + 0.5f : 20f;
        foreach (Vector2 at in points)
        {
            Bolt.Create(at, Mathf.Max(top, at.y + 2f));
            SpriteFx.Play("impact-ring", at, false, 101);
        }
        DiceSkillKit.Overlay.Get().Flash(Color.white, 0.55f, 0.18f);
        Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime, Vector2.down);
        Sfx.Play(SfxId.Hit, 1f, 0.5f); // low crack (no thunder clip)
        Sfx.Play(SfxId.Stunned, 0.8f, 0.7f);
    }

    // Jagged bolt from (x, top) to `at`: stretched squares like Skill2Rope (glow + white core + a branch), re-jagged every BoltFlick
    // with the glow alternating cyan / yellow. Scaled time: the strike's hit-stop holds the first frame.
    class Bolt : MonoBehaviour
    {
        DiceSkillKit.Segs segs;
        Vector2 at;
        float top, t, nextFlick;
        int frame;
        readonly Vector2[] pts = new Vector2[BoltJags + 1];

        public static void Create(Vector2 at, float top)
        {
            DiceSkillKit.Segs s = DiceSkillKit.Segs.Create("DiceBolt");
            Bolt b = s.gameObject.AddComponent<Bolt>();
            b.segs = s;
            b.at = at;
            b.top = top;
            b.Redraw();
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t >= BoltLife) { Destroy(gameObject); return; }
            if (t >= nextFlick) Redraw();
        }

        void Redraw()
        {
            nextFlick = t + BoltFlick;
            frame++;
            float fade = 1f - t / BoltLife;
            for (int i = 0; i <= BoltJags; i++)
            {
                float u = i / (float)BoltJags;
                float jx = i == 0 || i == BoltJags ? 0f : Random.Range(-0.45f, 0.45f);
                pts[i] = new Vector2(at.x + jx + (1f - u) * 0.6f * (frame % 2 == 0 ? 1f : -1f), Mathf.Lerp(top, at.y, u));
            }
            Color glow = DiceSkillKit.A(frame % 2 == 0 ? DiceSkillKit.Yellow : DiceSkillKit.Cyan, 0.55f * fade);
            segs.Begin();
            for (int i = 1; i <= BoltJags; i++) segs.Seg(pts[i - 1], pts[i], 9f * DiceSkillKit.Px, glow, 108);
            for (int i = 1; i <= BoltJags; i++) segs.Seg(pts[i - 1], pts[i], 3f * DiceSkillKit.Px, DiceSkillKit.A(Color.white, fade), 109);
            int k = Random.Range(2, BoltJags - 1); // one short branch
            Vector2 tip = pts[k] + new Vector2(Random.value < 0.5f ? -0.7f : 0.7f, -0.6f);
            segs.Seg(pts[k], tip, 2f * DiceSkillKit.Px, DiceSkillKit.A(Color.white, 0.8f * fade), 109);
            segs.Dot(at, 10f * DiceSkillKit.Px, DiceSkillKit.A(Color.white, fade), 110, 45f); // hot spot on the body
            segs.Flush();
        }
    }
}
