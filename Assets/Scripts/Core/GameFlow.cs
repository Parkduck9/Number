// Global gameplay lock. Set by blocking dialogue (talk mode). Respected by PlayerInputReader and TestEnemy.
// Not Time.timeScale: that is already used by the parry hit-stop in PlayerController.
public static class GameFlow
{
    public static bool InputLocked;
}
