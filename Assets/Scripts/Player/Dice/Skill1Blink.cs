using System.Collections;
using UnityEngine;

// Dice face 1, 축지법 shrink-earth step (DECISIONS 18/19): blink behind the nearest on-screen enemy and break it.
// Enemies are held for the blink so the target cannot react; the player is invulnerable until the blow settles.
// No free spot behind the nearest: the next nearest is tried; nobody: the blow lands from where the player stands.
public static class Skill1Blink
{
    const float StrikeDelay = 0.06f; // (T) s between appearing behind the target and the blow
    const float Recover = 0.18f;     // (T) s of invulnerability after the blow

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        Vector2 from = p.Body.position;
        var targets = DiceSkillKit.Targets(ctx, from);
        if (targets.Count == 0) { DiceSkillKit.Fizzle(ctx); yield break; }
        DiceSkillKit.Caption("SHRINK EARTH", "축지법", DiceSkillKit.Yellow);

        try
        {
            DiceSkillKit.Hold();
            DiceSkillKit.SetInvulnerable(ctx);

            TestEnemy target = null;
            foreach (TestEnemy e in targets)
                if (p.TryBlinkBehind(e)) { target = e; break; }
            if (target == null) target = targets[0];
            Vector2 to = p.Body.position;

            // The step talisman burns away where the hero stood; the earth folds at both ends; neon streak between.
            DiceSkillKit.Fade burn = DiceSkillKit.Talisman(from + Vector2.up * 0.3f, 0.35f);
            burn.drift = new Vector2(0f, 1.2f);
            burn.grow = 0.2f;
            DiceSkillKit.Fold(from + Vector2.down); // body pivot is the centre of the 2-unit body
            DiceSkillKit.Fold(to + Vector2.down);
            DiceSkillKit.Streak(from, to);

            yield return DiceSkillKit.Wait(StrikeDelay);
            if (DiceSkillKit.PlayerGone(ctx)) yield break;
            DiceSkillKit.Strike(ctx, target);
            DiceSkillKit.ReleaseHold(); // the broken body flies now
            yield return DiceSkillKit.Wait(Recover);
        }
        finally
        {
            DiceSkillKit.ReleaseHold();
            DiceSkillKit.ReleaseInvulnerable();
        }
    }
}
