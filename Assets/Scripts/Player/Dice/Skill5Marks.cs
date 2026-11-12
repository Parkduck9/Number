using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Dice face 5, 부적 다섯 장 five talismans (DECISIONS 18/19, 27): up to 5 nearest on-screen enemies get a talisman stuck on the
// body for 5 s. If the hero strikes any marked enemy in that time (LMB / lunge / issen / Shift counter: PlayerController.EnemyStruck),
// every marked enemy bursts in a quick chain, struck one first (piercing player hit). Otherwise the talismans fade and nothing
// happens. Head numbers are not touched.
public static class Skill5Marks
{
    const int MaxMarks = 5;         // DECISIONS 27
    const float Seconds = 5f;       // (T) talisman life
    const float FadeTime = 0.4f;    // (T) fade-out after the 5 s
    const float ChainGap = 0.1f;    // (T) s between bursts

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        var all = DiceSkillKit.Targets(ctx, p.Body.position);
        if (all.Count == 0) { DiceSkillKit.Fizzle(ctx); yield break; }
        var marks = all.GetRange(0, Mathf.Min(MaxMarks, all.Count));

        DiceSkillKit.Caption("FIVE TALISMANS", "부적 다섯 장", DiceSkillKit.Yellow);
        DiceSkillKit.Overlay.Get().Flash(DiceSkillKit.Yellow, 0.18f, 0.12f);
        Sfx.Play(SfxId.HackOk);

        var tags = new Dictionary<TestEnemy, DiceSkillKit.Fade>();
        TestEnemy struck = null;
        System.Action<TestEnemy> onStruck = e => { if (struck == null && marks.Contains(e)) struck = e; }; // flag only: no hits from inside the hit path
        p.EnemyStruck += onStruck;
        try
        {
            Vector2 hand = p.HandPosition;
            foreach (TestEnemy e in marks)
            {
                Vector2 c = DiceSkillKit.Center(e);
                DiceSkillKit.Line(hand, c, 2f * DiceSkillKit.Px, DiceSkillKit.A(DiceSkillKit.Yellow, 0.8f), 0.15f); // throw streak
                DiceSkillKit.Fade tal = DiceSkillKit.Talisman(hand, Seconds + FadeTime);
                tal.follow = e.transform;
                tal.offset = (Vector3)(c + new Vector2(0.1f, 0.1f)) - e.transform.position; // on the chest
                tal.flyFrom = hand;
                tal.flyTime = 0.12f;
                tal.fadeAt = Seconds;
                tags[e] = tal;
            }

            for (float t = 0f; t < Seconds && struck == null; t += Time.deltaTime)
            {
                if (DiceSkillKit.PlayerGone(ctx)) yield break;
                yield return null;
            }
            p.EnemyStruck -= onStruck;
            if (struck == null) yield break; // time out: the talismans fade on their own

            // Chain: the struck one first (usually already down from the blow), then the rest nearest-first.
            marks.Remove(struck);
            marks.Insert(0, struck);
            Sfx.Play(SfxId.HackOk);
            foreach (TestEnemy e in marks)
            {
                if (DiceSkillKit.PlayerGone(ctx)) yield break;
                if (e == null) continue;
                if (tags.TryGetValue(e, out DiceSkillKit.Fade tag) && tag != null) Object.Destroy(tag.gameObject);
                if (!e.gameObject.activeInHierarchy) continue;
                Vector2 c = DiceSkillKit.Center(e);
                MomentFx.HackPulse(c);
                DiceSkillKit.Ring(c, 0.3f, 2f * DiceSkillKit.Px, DiceSkillKit.A(DiceSkillKit.Yellow, 0.9f), 0.25f, 2.5f); // burning paper
                DiceSkillKit.StunOrStrike(ctx, e); // no-op on one already down
                yield return DiceSkillKit.Wait(ChainGap);
            }
            DiceSkillKit.Overlay.Get().Flash(DiceSkillKit.Yellow, 0.22f, 0.12f);
        }
        finally
        {
            if (p != null) p.EnemyStruck -= onStruck;
            if (struck != null) // chain cut short (death / unload): no talisman left hanging
                foreach (DiceSkillKit.Fade tag in tags.Values) if (tag != null) Object.Destroy(tag.gameObject);
        }
    }
}
