using System.Collections;
using UnityEngine;

// Dice face 3, 부적 마비 paralysis talismans (DECISIONS 18/19): every enemy on screen is overloaded for 3 s
// (androids: Overload state; citizens / bugs / snipers: Frozen, like broadcast 0). A talisman flies from the hand to each one,
// nearest first, and sticks on its chest for the duration; the screen glitches cyan / red on the cast.
public static class Skill3Talisman
{
    const float Seconds = 3f;      // DECISIONS 18
    const float Fly = 0.14f;       // (T) s for the first talisman to land
    const float Stagger = 0.035f;  // (T) s between talismans (nearest first)

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        var targets = DiceSkillKit.Targets(ctx, p.Body.position);
        if (targets.Count == 0) { DiceSkillKit.Fizzle(ctx); yield break; }

        DiceSkillKit.Caption("PARALYSIS TALISMAN", "부적 마비", DiceSkillKit.Yellow);
        var overlay = DiceSkillKit.Overlay.Get();
        overlay.Glitch(0.3f);
        overlay.Flash(DiceSkillKit.Cyan, 0.3f, 0.15f);
        Juice.Shake(Juice.ShakeParryAmp, Juice.ShakeParryTime);
        Sfx.Play(SfxId.HackOk);

        Vector2 hand = p.HandPosition;
        int n = targets.Count;
        var arrive = new float[n];
        for (int i = 0; i < n; i++)
        {
            TestEnemy e = targets[i];
            arrive[i] = Fly + i * Stagger;
            DiceSkillKit.Fade tal = DiceSkillKit.Talisman(hand, arrive[i] + Seconds);
            tal.follow = e.transform;
            tal.offset = (Vector3)(DiceSkillKit.Center(e) + Vector2.up * 0.15f) - e.transform.position;
            tal.flyFrom = hand;
            tal.flyTime = arrive[i];
            tal.fadeAt = arrive[i] + Seconds - 0.35f;
        }

        // Overload each enemy as its talisman lands (skips enemies that went down meanwhile).
        var landed = new bool[n];
        int left = n;
        for (float t = 0f; left > 0; t += Time.deltaTime)
        {
            for (int i = 0; i < n; i++)
            {
                if (landed[i] || t < arrive[i]) continue;
                landed[i] = true;
                left--;
                TestEnemy e = targets[i];
                if (!DiceSkillKit.Alive(e)) continue;
                e.Overload(Seconds);
                SpriteFx.Play("impact-ring", DiceSkillKit.Center(e), false, 101); // pivot = ring centre
            }
            if (left > 0) yield return null;
        }
        Sfx.Play(SfxId.Stunned);
    }
}
