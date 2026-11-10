using UnityEngine;

// Global gameplay lock. Set by blocking dialogue (talk mode). Respected by PlayerInputReader and TestEnemy.
// Not Time.timeScale: that is already used by the parry hit-stop in PlayerController.
public static class GameFlow
{
    public static bool InputLocked;
    // Lucky dice hold: enemies (AI, velocity, timers), enemy projectiles, spawner respawns and sniper sights freeze; the player keeps moving.
    public static bool EnemiesHeld;
    // A script that uses Esc for its own purpose (closing a dial, cancelling a choice) sets this to Time.frameCount so the pause menu stays closed.
    public static int EscConsumedFrame = -1;
    public static bool EscConsumed => EscConsumedFrame == Time.frameCount;
    // DECISIONS 31: E = interact AND lucky dice. An interactable that would take E (board hack prompt / open dial, stop button) stamps
    // Time.frameCount here every frame; while it is fresh, E does the interaction and not the dice (last frame counts: Update order).
    public static int InteractFrame = -10;
    public static bool InteractNear => Time.frameCount - InteractFrame <= 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { InputLocked = false; EnemiesHeld = false; EscConsumedFrame = -1; InteractFrame = -10; } // Domain Reload is off: a lock from the last Play session must not leak

    // True while the player is dying/dead: stage exits, the stop button and the like must ignore it.
    public static bool IsPlayerDead(PlayerController pc)
    {
        if (pc == null) return false;
        var h = pc.GetComponent<PlayerHealth>();
        return (h != null && h.IsDead) || pc.CurrentState == PlayerController.State.Dead;
    }
}
