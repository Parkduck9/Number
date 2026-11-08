using UnityEngine;

// Global gameplay lock. Set by blocking dialogue (talk mode). Respected by PlayerInputReader and TestEnemy.
// Not Time.timeScale: that is already used by the parry hit-stop in PlayerController.
public static class GameFlow
{
    public static bool InputLocked;
    // A script that uses Esc for its own purpose (closing a dial, cancelling a choice) sets this to Time.frameCount so the pause menu stays closed.
    public static int EscConsumedFrame = -1;
    public static bool EscConsumed => EscConsumedFrame == Time.frameCount;
}
