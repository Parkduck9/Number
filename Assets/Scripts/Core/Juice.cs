// Hit-feel tunables in one place (see docs/design/impl/juice.md). Set an amplitude / time to 0 to disable that effect.
public static class Juice
{
    // Camera shake: amplitude in world units (1 px = 1/32), duration in seconds (unscaled).
    public const float ShakeHitAmp = 0.04f,   ShakeHitTime = 0.08f;   // player hit lands
    public const float ShakeParryAmp = 0.07f, ShakeParryTime = 0.12f; // parry
    public const float ShakeHurtAmp = 0.12f,  ShakeHurtTime = 0.2f;   // player takes damage
    public const float ShakeKillAmp = 0.12f,  ShakeKillTime = 0.2f;   // enemy goes down

    // Hit-stop (seconds, real time). Parry keeps PlayerController.hitStopTime.
    public const float HitStopHit = 0.04f;
    public const float HitStopKill = 0.08f;

    // One-hit kill of normal enemies (Citizen, Bug): knocked out, then vanish after this delay. Android keeps hp/blocking.
    public const float KillVanishDelay = 0.5f;
    public static readonly UnityEngine.Vector2 KillKnockback = new UnityEngine.Vector2(3f, 2f);

    // Citizen/Bug tempo (Android unchanged): timings x NormalTimeMul, approach speed x NormalSpeedMul. 1 = old values.
    public const float NormalTimeMul = 0.65f;
    public const float NormalSpeedMul = 1.25f;
    public const float MinFirstWindup = 0.2f; // first-strike windup never shorter than this (readable tell)

    public static void Shake(float amp, float time) => CameraFollow2D.Shake(amp, time);
}
