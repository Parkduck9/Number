// All broadcast-number enemy rule values in one place (tune here).
// Implements docs/design/DECISIONS.md items 1-A and 7-A. To remove a rule, delete its block in TestEnemy.
public static class BroadcastRules
{
    // NUM-3: melee enemies chain faster and stay open longer
    public static float Num3ChainWindupTime = 0.10f; // windup of chain strikes after the first (base chainWindupTime 0.15)
    public static float Num3OpenTime = 1.5f;         // counter window after the chain (base openTime 1)
    public static int Num3Number = 3;                // melee enemies' number is pinned to this (ranged keep theirs)

    // NUM-7: doped for a while, then exhausted
    public static float Num7DopeSeconds = 7f;        // how long the dope lasts
    public static float Num7SpeedMul = 1.3f;         // move speed x, windup times /
    public static float Num7ExhaustSeconds = 2f;     // open (hittable) time after the dope runs out

    // Android gold fan special
    public static float AndroidFanCooldown = 6f;     // seconds between fans
    public static float AndroidFanTell = 0.5f;       // tell time before the shots
    public static int AndroidFanCount = 3;           // projectiles per fan
    public static float AndroidFanSpreadDeg = 15f;   // angle between neighbouring shots
    public static float AndroidFanSpeed = 8f;        // projectile speed
}
