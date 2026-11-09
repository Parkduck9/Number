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
    public const float ReflectSpeedMul = 1.3f, ReflectLife = 0.35f;

    // Android overload (gimmick A): after a parried last strike / dodged 7th strike, the next hit kills it.
    public const float OverloadSeconds = 1.5f;        // (T) how long the window stays open
    public const float OverloadSparkInterval = 0.2f;  // (T) spark burst spacing

    // Citizen/Bug tempo (Android unchanged): timings x NormalTimeMul, approach speed x NormalSpeedMul. 1 = old values.
    public const float NormalTimeMul = 0.55f;  // DECISIONS 12: 0.65 -> 0.55 (faster tells / chains)
    public const float NormalSpeedMul = 1.5f;  // DECISIONS 12: 1.25 -> 1.5
    public const float MinFirstWindup = 0.2f; // first-strike windup never shorter than this (readable tell)

    public static void Shake(float amp, float time) => CameraFollow2D.Shake(amp, time);
    public static void Shake(float amp, float time, UnityEngine.Vector2 blowDir) => CameraFollow2D.Shake(amp, time, blowDir);
}
