using System.Collections;
using UnityEngine;

// Dice face 5, 부적 다섯 장 five talismans (DECISIONS 18/19): up to 5 nearest on-screen enemies get a talisman and their head
// number overwritten with a cyan 5, counting down once a second while the hero fights on; at 0 every marked enemy still
// standing bursts at once (piercing player hit). Marks of enemies killed meanwhile just go away with them.
public static class Skill5Marks
{
    const int MaxMarks = 5;  // DECISIONS 18
    const int From = 5;      // countdown start (5 s)
    const float Tick = 1f;   // s per count
    const float ZeroBeat = 0.12f; // (T) s the cyan 0 stays before the burst

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        var all = DiceSkillKit.Targets(ctx, p.Body.position);
        if (all.Count == 0) { DiceSkillKit.Fizzle(ctx); yield break; }
        var marks = all.GetRange(0, Mathf.Min(MaxMarks, all.Count));
        Sprite[] sheet = DiceSkillKit.CyanDigits() ?? DraftHud.HoloDigits(true); // gold sheet only if the cyan one is missing

        DiceSkillKit.Caption("FIVE TALISMANS", "부적 다섯 장", DiceSkillKit.Cyan);
        DiceSkillKit.Overlay.Get().Flash(DiceSkillKit.Cyan, 0.18f, 0.12f);
        Sfx.Play(SfxId.HackOk);

        try
        {
            Vector2 hand = p.HandPosition;
            foreach (TestEnemy e in marks)
            {
                e.SetNumberOverride(From, sheet);
                Vector2 c = DiceSkillKit.Center(e);
                DiceSkillKit.Line(hand, c, 3f * DiceSkillKit.Px, DiceSkillKit.A(DiceSkillKit.Cyan, 0.9f), 0.2f); // brand beam
                DiceSkillKit.Fade tal = DiceSkillKit.Talisman(hand, From * Tick + 0.3f);
                tal.follow = e.transform;
                tal.offset = (Vector3)(c + Vector2.up * 0.15f) - e.transform.position;
                tal.flyFrom = hand;
                tal.flyTime = 0.12f;
                tal.fadeAt = From * Tick;
            }

            for (int n = From - 1; n >= 0; n--)
            {
                for (float t = 0f; t < Tick; t += Time.deltaTime)
                {
                    if (DiceSkillKit.PlayerGone(ctx)) yield break;
                    yield return null;
                }
                foreach (TestEnemy e in marks)
                {
                    if (!DiceSkillKit.Alive(e)) continue;
                    e.SetNumberOverride(n, sheet);
                    DiceSkillKit.Ring(DiceSkillKit.Center(e), 0.35f, 1f * DiceSkillKit.Px, DiceSkillKit.A(DiceSkillKit.Cyan, 0.8f), 0.2f, 2.2f); // tick pulse
                }
            }

            // 0 shows for a beat, then the chain burst.
            for (float t = 0f; t < ZeroBeat; t += Time.deltaTime)
            {
                if (DiceSkillKit.PlayerGone(ctx)) yield break;
                yield return null;
            }
            int burst = 0;
            foreach (TestEnemy e in marks)
            {
                if (!DiceSkillKit.Alive(e)) continue;
                MomentFx.HackPulse(DiceSkillKit.Center(e));
                DiceSkillKit.Strike(ctx, e);
                burst++;
            }
            if (burst > 0)
            {
                DiceSkillKit.Overlay.Get().Flash(DiceSkillKit.Cyan, 0.25f, 0.12f);
                Sfx.Play(SfxId.HackOk);
            }
        }
        finally
        {
            // Bodies already knocked down keep their (hidden) number; clearing them could bring the number back on a corpse.
            foreach (TestEnemy e in marks)
                if (e != null && !(e.gameObject.activeInHierarchy && e.IsDown)) e.ClearNumberOverride();
        }
    }
}
