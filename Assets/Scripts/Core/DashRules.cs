// DECISIONS 12 tunables in one place (docs/design/impl/dash-attack.md): the player's mouse-aimed lunge attack and the enemy rush.
// Static like BroadcastRules: change a value, recompile, every scene uses it (no prefab / scene rebuild).
public static class DashRules
{
    // ---- Player lunge attack (PlayerController) ----
    public const float LungeRange = 5f;         // M: how far (u, from the player's centre) an enemy may be to trigger a lunge
    public const float LungeTolerance = 0.6f;   // window = M + this; also how far ahead of the body the kill sweep reaches
    public const float LungeConeDeg = 30f;      // half-angle of the search cone around the mouse direction
    public const float LungeSpeed = 40f;        // u/s along the line (5.6u + 1.2u behind = ~0.17 s)
    public const float LungeMaxTime = 0.25f;    // a lunge never lasts longer (wall / clamp safety)
    public const float LungeBehind = 1.2f;      // end point past the target's centre (enemy half 0.45 + player half 0.45 + 0.3 gap)
    public const float LungeStopInFront = 1.0f; // end point in front of the target: non-overloaded Android (blocked) or a pit behind it
    public const float LungeSweepWidth = 1.4f;  // thickness of the kill sweep (= attack box height)
    public const int LungeMaxHitStops = 2;      // hit-stop + shake only for the first N blows of one swing (many kills must not freeze the game)
    public const int LungeKillSparks = 2;       // extra hit-sparks on a lunge kill (cheap "shatter")

    // ---- LMB parry merge (PlayerController.FaceParryThreat): parry wins over the lunge when a threat is about to land ----
    public const float ParryShotRange = 2f;     // a parryable shot this close (u) and flying at the player
    public const float ParryShotLateral = 1.3f; // ...whose line passes within this of the player's centre (half height 1 + shot 0.25)
    // melee: an enemy in Windup/Strike whose attack box overlaps the player box (TestEnemy.MeleeThreatens)

    // ---- Enemy rush (TestEnemy, melee Citizen/Bug only; Android and ranged unchanged) ----
    public const float RushRange = 5f;          // player within this x-distance (and attackRangeY) but outside attackRangeX -> rush tell
    public const float RushWindup = 0.3f;       // tell length (s); NUM-7 doping divides it by Num7SpeedMul
    public const float RushSpeed = 16f;         // u/s (plain mid-chain Charge keeps chargeSpeed 14)
    public const float RushMaxDistance = 6f;    // give up after this distance -> back to Approach
    public const float RushCooldown = 0.6f;     // no new rush tell for this long after a rush that did not reach the player
}
