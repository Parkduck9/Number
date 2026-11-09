using System.Linq;
using UnityEngine;

// DRAFT art for a play test. Set Use = false to fall back to the placeholder squares. Removal: docs/design/impl/draft-sprites.md.
// Strips live in Assets/Resources/DraftSprites/<name>_<N>f.png (N frames of 64x64). A missing/unsliced strip is null = keep the placeholder.
public static class DraftSprites
{
    public const bool Use = true;
    public const float WalkFps = 8f;
    public const float IdleFps = 2f;
    public const float RunFps = 12f; // hero run cycle (6 frames = 0.5 s per cycle)

    static Sprite[] hero, heroRun, heroIdle, heroAttack, heroDash, heroHurt, citizen, citizenRanged, android, luddite,
        heroJump, heroParry, citizenWalk, citizenStunned, citizenRangedWalk, androidWalk, androidStunned,
        bug, bugWalk, bugFlyer, bugFlyerAttack, heroGrapple;
    public static Sprite[] Hero => hero ??= Load("hero_walk_3f", 3);
    public static Sprite[] HeroRun => heroRun ??= Load("hero_run_6f", 6);                   // test strip (Codex); null = walk
    public static Sprite[] HeroIdle => heroIdle ??= Load("hero_idle_2f", 2);
    public static Sprite[] HeroAttack => heroAttack ??= Load("hero_attack_3f", 3);
    public static Sprite[] HeroDash => heroDash ??= Load("hero_dash_2f", 2);
    public static Sprite[] HeroHurt => heroHurt ??= Load("hero_hurt_1f", 1);
    public static Sprite[] Citizen => citizen ??= Load("citizen_attack_3f", 3);
    public static Sprite[] CitizenRanged => citizenRanged ??= Load("citizen_b_ranged_3f", 3);
    public static Sprite[] Android => android ??= Load("android_attack_3f", 3);
    public static Sprite[] Luddite => luddite ??= Load("luddite_npc_idle_2f", 2);
    public static Sprite[] HeroJump => heroJump ??= Load("hero_jump_2f", 2);                 // 0 rising, 1 falling
    public static Sprite[] HeroGrapple => heroGrapple ??= Load("hero_grapple_1f", 1);        // pulled by the rope (Codex); null = jump
    public static Sprite[] HeroParry => heroParry ??= Load("hero_parry_2f", 2);
    public static Sprite[] CitizenWalk => citizenWalk ??= Load("citizen_walk_2f", 2);
    public static Sprite[] CitizenStunned => citizenStunned ??= Load("citizen_stunned_1f", 1);
    public static Sprite[] CitizenRangedWalk => citizenRangedWalk ??= Load("citizen_b_walk_2f", 2);
    public static Sprite[] AndroidWalk => androidWalk ??= Load("android_walk_2f", 2);
    public static Sprite[] AndroidStunned => androidStunned ??= Load("android_stunned_1f", 1);
    public static Sprite[] Bug => bug ??= Load("bug_attack_3f", 3);                          // melee bug (spider): 0 windup, 1 strike, 2 recover
    public static Sprite[] BugWalk => bugWalk ??= Load("bug_walk_2f", 2);
    public static Sprite[] BugFlyer => bugFlyer ??= Load("bug_flyer_2f", 2);                 // ranged bug: flap loop (hovers, body ~1.1u above the pivot)
    public static Sprite[] BugFlyerAttack => bugFlyerAttack ??= Load("bug_flyer_attack_3f", 3); // 0 charge, 1 fire, 2 recover

    static Sprite[] Load(string name, int count)
    {
        Sprite[] s = Resources.LoadAll<Sprite>("DraftSprites/" + name).OrderBy(x => x.name).ToArray();
        return s.Length == count ? s : null;
    }

    // frames[i] clamped; falls back to `fallback` when the strip is missing.
    public static Sprite Pick(Sprite[] frames, int i, Sprite[] fallback)
    {
        if (frames == null || frames.Length == 0) frames = fallback;
        return frames[Mathf.Clamp(i, 0, frames.Length - 1)];
    }

    // Swaps the placeholder square for frame 0: 2 units tall at PPU 32, feet at the 2-unit collider bottom (root centre - 1). Returns false = keep placeholder.
    public static bool Setup(SpriteRenderer r, Sprite[] frames)
    {
        if (!Use || r == null || frames == null) return false;
        r.sprite = frames[0];
        r.color = Color.white;
        r.transform.localScale = Vector3.one;
        r.transform.localPosition = new Vector3(0f, -1f, 0f);
        return true;
    }
}
