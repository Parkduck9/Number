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
    public const float HitStopGap = 0.25f;      // s (real time): a hit/kill hit-stop never starts within this after the previous one ended (parry exempt)

    // ---- LMB parry merge (PlayerController.FaceParryThreat): parry wins over the lunge when a threat is about to land ----
    public const float ParryShotRange = 3f;     // a parryable shot this close (u) and flying at the player (playtest 11-09: 2 was too late for humans)
    // ...or (fast shots) one arriving within this many seconds, whatever the distance. QA R2-04: the 14 u/s sniper shot inside 3 u gave a
    // 0.16 s window; 0.25 s = ~4.2 u for it. The guard lasts until arrival + ParryCoverMargin.
    public const float ParryShotWindow = 0.25f;
    public const float ParryShotLateral = 1.3f; // ...whose line passes within this of the player's centre (half height 1 + shot 0.25)
    // melee: an enemy in Windup/Strike whose attack box overlaps the player box and whose strike lands within this (TestEnemy.MeleeThreatens)
    public const float ParryMeleeWindow = 0.3f; // s; was parryActiveTime 0.2 (playtest 11-09: too strict)
    public const float ParryCoverMargin = 0.1f;  // an LMB parry stays active until the chosen threat's arrival + this (at least parryActiveTime; the Parry state stretches to cover it)
                                                 // >= 2 frames at 30 fps (WebGL): TestEnemy lands a melee strike 1-2 Updates after WindupWait elapses (Windup->Strike, then DoStrike next frame)
    public const float ParryHitStop = 0.15f;    // minimum hit-stop on a successful parry (0.06, then 0.1 read as nothing; playtest 11-09). Longer than a kill (Juice.HitStopKill)

    // ---- Shift dodge + counter (DECISIONS 13, PlayerController.TryPlanDashBehind / DoShiftCounter) ----
    public const float ShiftAssistRange = 4f;   // u (centre to centre, horizontal): enemy this close on either side -> dash behind it and counter
    public const float ShiftAssistDy = 1.5f;    // max height difference (centres) for that search
    public const float ShiftStepDy = 0.5f;      // feet-level gap the dash may climb/drop onto the target's ground; larger stays horizontal
    public const float ShiftCooldown = 3.5f;    // s (was 2.0); bullet-dodge invulnerability is the dash itself, unchanged

    // ---- RMB neon grapple rope (DECISIONS 16, PlayerController.TryGrapple / UpdateGrapple, GrappleRope visual) ----
    public const float GrappleRange = 9f;        // u, from the hand toward the mouse; ground, walls, ceilings and one-way platforms anchor, enemies don't
    public const float GrappleExtendTime = 0.06f; // s: rope flies out (the body hangs still meanwhile), then the pull starts
    public const float GrappleRetractTime = 0.1f; // s: a missed rope snaps back
    public const float GrapplePullSpeed = 22f;   // u/s toward the anchor, gravity off
    public const float GrappleReleaseDist = 0.8f; // release this close (u, body centre to anchor)
    public const float GrappleMaxPull = 0.6f;    // s: the pull never lasts longer
    public const float GrapplePopFrac = 0.8f;    // release with the anchor above: vy >= this * jump speed (slingshot onto the ledge)
    public const float GrappleCarryTime = 0.4f;  // s after release the horizontal momentum eases into normal air control instead of snapping to it
    public const float GrappleAirAccel = 40f;    // u/s^2: how fast that carried momentum blends to the held direction
    public const float GrappleMissCooldown = 0.25f; // s before another rope after a miss
    public const float GrappleCooldown = 0.4f;   // s before another rope after a release (ground ropes refill at once)
    public const float GrappleLedgeSnap = 2f;    // u: a wall hit within this below a ledge top anchors on the top corner instead
    // Uses: 1 per airtime (an attached rope spends it), refilled on landing.

    // ---- Enemy rush (TestEnemy, melee Citizen/Bug only; Android and ranged unchanged) ----
    public const float RushRange = 5f;          // player within this x-distance (and attackRangeY) but outside attackRangeX -> rush tell
    public const float RushWindup = 0.3f;       // tell length (s); NUM-7 doping divides it by Num7SpeedMul
    public const float RushSpeed = 16f;         // u/s (plain mid-chain Charge keeps chargeSpeed 14)
    public const float RushMaxDistance = 6f;    // give up after this distance -> back to Approach
    public const float RushCooldown = 0.6f;     // no new rush tell for this long after a rush that did not reach the player

    // ---- Enemy awareness (TestEnemy; playtest 11-09 "enemies notice too slowly") ----
    public const float MeleeDetectMin = 7f;     // melee enemies spot the player at max(builder activationRange, this) x-distance (ranged keep theirs)
    // Melee enemies also stay aggro once they have spotted the player (no going idle when the player steps back out of range).
}
