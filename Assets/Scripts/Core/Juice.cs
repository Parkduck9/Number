// Hit-feel tunables in one place (see docs/design/impl/juice.md). Set an amplitude / time to 0 to disable that effect.
public static class Juice
{
    // Camera shake: amplitude in world units (1 px = 1/32), duration in seconds (unscaled).
    public const float ShakeHitAmp = 0.04f,   ShakeHitTime = 0.08f;   // player hit lands
    public const float ShakeParryAmp = 0.1f,  ShakeParryTime = 0.15f; // parry (playtest 11-09: 0.07/0.12 read as nothing)
    public const float ShakeHurtAmp = 0.12f,  ShakeHurtTime = 0.2f;   // player takes damage
    public const float ShakeKillAmp = 0.14f,  ShakeKillTime = 0.22f;  // enemy goes down (kicks the camera along the blow)
    public const float ShakeKick = 0.6f;      // directional shakes: share of the amplitude that pushes along the blow (rest stays random)

    // Hit-stop (seconds, real time). Parry: DashRules.ParryHitStop. Playtest 11-09 "hits feel weak": 0.04/0.08 -> 0.05/0.12.
    public const float HitStopHit = 0.05f;
    public const float HitStopKill = 0.12f;
    public const int KillSparks = 3;          // extra hit-sparks scattered on the body for every kill (was lunge kills only, 2)

    // One-hit kill of normal enemies (Citizen, Bug): knocked out, then vanish after this delay. Android: only while overloaded (below).
    public const float KillVanishDelay = 0.5f;
    public static readonly UnityEngine.Vector2 KillKnockback = new UnityEngine.Vector2(6f, 3.5f); // was (3, 2)

    // Sprite tints. A tint multiplies the sprite, and the draft sprites are white, so a "white flash" changes nothing: use colours.
    public static readonly UnityEngine.Color ParryPlayerTint = new UnityEngine.Color(0.3f, 0.95f, 0.85f); // player on a successful parry (#4CE8D8-ish)
    public static readonly UnityEngine.Color ParryEnemyTint = new UnityEngine.Color(0.35f, 0.75f, 1f);    // enemy whose strike was parried
    public static readonly UnityEngine.Color BlockTint = new UnityEngine.Color(0.55f, 0.6f, 0.75f);       // Android blocks a hit (no damage)
    public const float ParryEnemyTintTime = 0.25f; // s (scaled: holds through the parry hit-stop)

    // Successful parry, screen side (ParryFeedback, real time so it shows during the hit-stop).
    public const float ParryFlashAlpha = 0.35f, ParryFlashTime = 0.12f; // full-screen white flash
    public const float ParryPopupTime = 0.5f;  // "PARRY!" text at the contact point, rises and fades
    public const int ParryPopupSize = 24;      // reference px (Mulmaru: multiples of 12)
    public const float ParryPopupRise = 24f;   // reference px over its life
    public static readonly UnityEngine.Color ParryPopupColor = new UnityEngine.Color(1f, 0.824f, 0.29f); // broadcast gold #FFD24A
    // Parried shots fly back (harmless, then vanish) instead of disappearing.
    public const float ReflectLife = 0.6f; // a parried shot drops away for this long (EnemyProjectile: weakened, falls)

    // Android overload (gimmick A): after a parried last strike / dodged 7th strike, the next hit kills it.
    public const float OverloadSeconds = 1.5f;        // (T) how long the window stays open
    public const float OverloadSparkInterval = 0.2f;  // (T) spark burst spacing
    public const float OverloadGlitch = 0.35f;        // (T) SpriteFlash shader _GlitchAmount while overloaded (0..1)
    public const float OverloadGlitchHz = 10f;        // (T) flicker ticks per second (glitch off every 3rd tick)

    // Citizen/Bug tempo (Android unchanged): timings x NormalTimeMul, approach speed x NormalSpeedMul. 1 = old values.
    public const float NormalTimeMul = 0.55f;  // DECISIONS 12: 0.65 -> 0.55 (faster tells / chains)
    public const float NormalSpeedMul = 1.5f;  // DECISIONS 12: 1.25 -> 1.5
    public const float MinFirstWindup = 0.2f; // first-strike windup never shorter than this (readable tell)

    // Punch zoom (CameraFollow2D): brief zoom-in on a shake of kill / parry size, eases back in real time. Fractions of the view size.
    public const float PunchKill = 0.07f, PunchParry = 0.10f; // parry stronger than a kill. PPU is an int (32): 0.07 -> 34, 0.10 -> 35, i.e. a 1-3 px pop that eases back
    public const float PunchTime = 0.15f;                     // s
    // Look-ahead (CameraFollow2D): the camera leads the player's movement; a dash (speed above the threshold) leads further.
    public const float LookAhead = 1.75f, LookAheadDash = 3f; // world units
    public const float LookAheadSmooth = 0.3f;                // s
    public const float DashSpeedThreshold = 14f;              // units/s (walk ~10, dash 24)
    public const float LookAheadFlipSpeed = 1.5f;             // units/s: slower than this never flips the lead (no jitter on stops / turns)
    // Vertical follow (CameraFollow2D): airborne, the camera holds the last ground height, so hops don't bob the view.
    public const float CamJumpHold = 2.5f;                    // units the player can rise above the last ground before the view rises too
    public const float CamFallLead = 1.25f, CamFallLeadSpeed = 9f; // falling faster than this (units/s): the view leads down this far
    public const float CamYSmoothAir = 0.22f, CamYSmoothLand = 0.14f; // s: vertical smoothing in the air / landing catch-up
    // Death -> retry: the Dead pose stays this long (real time) before the scene reloads. Hurt 0.25 s + this + load = control back.
    public const float DeathRespawnDelay = 0.55f;

    public static void Shake(float amp, float time) => CameraFollow2D.Shake(amp, time);
    public static void Shake(float amp, float time, UnityEngine.Vector2 blowDir) => CameraFollow2D.Shake(amp, time, blowDir);
}
