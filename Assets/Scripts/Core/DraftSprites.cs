using System.Linq;
using UnityEngine;

// DRAFT art for a play test. Set Use = false to fall back to the placeholder squares. Removal: docs/design/impl/draft-sprites.md.
// Strips live in Assets/Resources/DraftSprites/<name>_<N>f.png (N frames of 64x64). A missing/unsliced strip is null = keep the placeholder.
public static class DraftSprites
{
    public const bool Use = true;
    public const float WalkFps = 16f; // walk / flap strips are 2x frames (old 8 fps)
    public const float IdleFps = 4f;       // hero idle 4f (old 2 fps)
    public const float LudditeIdleFps = 2f; // luddite idle strip is still 2f

    static Sprite[] hero, heroRun, heroIdle, heroAttack, heroDash, heroHurt, citizen, citizenRanged, android, luddite,
        heroJump, heroParry, citizenWalk, citizenStunned, citizenRangedWalk, androidWalk, androidStunned,
        bug, bugWalk, bugFlyer, bugFlyerAttack, heroGrapple, sniperAim, sniperStunned, sniperWalk,
        bulletSniper, bulletAndroid, bulletCitizen, bulletBug;
    public static Sprite[] Hero => hero ??= Load("hero_walk_6f", 6);
    public static Sprite[] HeroRun => heroRun ??= Load("hero_run_8f", 8) ?? Load("hero_run_6f", 6); // 8f strip first, 6f test strip (Codex); null = walk
    public static float RunFps => HeroRun != null && HeroRun.Length == 8 ? 20f : 12f; // 0.4 s (8f) / 0.5 s (6f) per cycle
    public static Sprite[] HeroIdle => heroIdle ??= Load("hero_idle_4f", 4);
    public static Sprite[] HeroAttack => heroAttack ??= Load("hero_attack_5f", 5);
    public static Sprite[] HeroDash => heroDash ??= Load("hero_dash_3f", 3);
    public static Sprite[] HeroHurt => heroHurt ??= Load("hero_hurt_1f", 1);
    public static Sprite[] Citizen => citizen ??= Load("citizen_attack_5f", 5);
    public static Sprite[] CitizenRanged => citizenRanged ??= Load("citizen_b_ranged_5f", 5);
    public static Sprite[] Android => android ??= Load("android_attack_5f", 5);
    public static Sprite[] Luddite => luddite ??= Load("luddite_npc_idle_2f", 2);
    public static Sprite[] HeroJump => heroJump ??= Load("hero_jump_4f", 4);                 // 0 rise, 1 rise->apex, 2 apex->fall, 3 fall
    public static Sprite[] HeroGrapple => heroGrapple ??= Load("hero_grapple_1f", 1);        // pulled by the rope (Codex); null = jump
    public static Sprite[] HeroParry => heroParry ??= Load("hero_parry_2f", 2);
    public static Sprite[] CitizenWalk => citizenWalk ??= Load("citizen_walk_6f", 6);
    public static Sprite[] CitizenStunned => citizenStunned ??= Load("citizen_stunned_1f", 1);
    public static Sprite[] CitizenRangedWalk => citizenRangedWalk ??= Load("citizen_b_walk_4f", 4);
    public static Sprite[] AndroidWalk => androidWalk ??= Load("android_walk_4f", 4);
    public static Sprite[] AndroidStunned => androidStunned ??= Load("android_stunned_1f", 1);
    public static Sprite[] Bug => bug ??= Load("bug_attack_5f", 5);                          // melee bug (spider): 0 windup, 2 strike, 4 recover (1, 3 = in-betweens)
    public static Sprite[] BugWalk => bugWalk ??= Load("bug_walk_4f", 4);
    public static Sprite[] BugFlyer => bugFlyer ??= Load("bug_flyer_4f", 4);                 // ranged bug: flap loop (hovers, body ~1.1u above the pivot)
    public static Sprite[] BugFlyerAttack => bugFlyerAttack ??= Load("bug_flyer_attack_5f", 5); // 0 charge, 2 fire, 4 recover

    public static Sprite[] SniperAim => sniperAim ??= Load("sniper_aim_5f", 5);              // 0 stand, 1 raise, 2 aim, 3 charge (locked), 4 fire
    public static Sprite[] SniperStunned => sniperStunned ??= Load("sniper_stunned_1f", 1);
    public static Sprite[] SniperWalk => sniperWalk ??= Load("sniper_walk_4f", 4);
    // Enemy shots: 16x16 2-frame loops, pivot centre (EnemyProjectile rotates +x along the velocity).
    public static Sprite[] BulletSniper => bulletSniper ??= Load("bullet_sniper_2f", 2);
    public static Sprite[] BulletAndroid => bulletAndroid ??= Load("bullet_android_2f", 2);
    public static Sprite[] BulletCitizen => bulletCitizen ??= Load("bullet_citizen_2f", 2);
    public static Sprite[] BulletBug => bulletBug ??= Load("bullet_bug_2f", 2);

    // Successful-parry poses (user 11-10): hero_parry_{mid,hi,lo}{1,2,3}_2f, 0 = impact, 1 = after. Missing strips are skipped.
    static readonly Sprite[][] heroParryPoses = new Sprite[9][];
    static readonly string[] PoseHeights = { "mid", "hi", "lo" }; // ParryHeight order

    // A random pose for that height other than 'previous' (unless it is the only one in); HeroParry (may be null) when none is in.
    public static Sprite[] HeroParryPose(ParryHeight height, Sprite[] previous)
    {
        int h = (int)height, n = 0;
        Sprite[] pick = null, same = null;
        for (int i = 0; i < 3; i++)
        {
            Sprite[] s = heroParryPoses[h * 3 + i] ??= Load("hero_parry_" + PoseHeights[h] + (i + 1) + "_2f", 2);
            if (s == null) continue;
            if (s == previous) same = s;
            else if (Random.Range(0, ++n) == 0) pick = s; // uniform over the others
        }
        return pick ?? same ?? HeroParry;
    }

    // Helper NPC strips (copies of the hero set, no NPC yet); null until Codex's PNGs are in.
    static Sprite[] helperWalk, helperRun, helperIdle, helperAttack, helperDash, helperHurt, helperJump, helperGrapple, helperParry;
    public static Sprite[] HelperWalk => helperWalk ??= Load("helper_walk_6f", 6);
    public static Sprite[] HelperRun => helperRun ??= Load("helper_run_6f", 6);
    public static Sprite[] HelperIdle => helperIdle ??= Load("helper_idle_4f", 4);
    public static Sprite[] HelperAttack => helperAttack ??= Load("helper_attack_5f", 5);
    public static Sprite[] HelperDash => helperDash ??= Load("helper_dash_3f", 3);
    public static Sprite[] HelperHurt => helperHurt ??= Load("helper_hurt_1f", 1);
    public static Sprite[] HelperJump => helperJump ??= Load("helper_jump_4f", 4);
    public static Sprite[] HelperGrapple => helperGrapple ??= Load("helper_grapple_1f", 1);
    public static Sprite[] HelperParry => helperParry ??= Load("helper_parry_2f", 2);

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
