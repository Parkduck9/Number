using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// "Moment" FX (docs/design/concept/fx_moments): one static call per game event, drawn through SpriteFx
// (Resources/FX/<name>.png + .json, pivot = the anchor baked into the json). A missing sheet only warns once.
// Positions are the effect's anchor: feet / ground point for dust, warp-in, checkpoint, death, goal; centre for the others.
public static class MomentFx
{
    const float RealFallHeight = 1.5f; // (T) units dropped before a landing raises dust (a step or a small hop does not)

    static readonly HashSet<string> missing = new HashSet<string>();
    static bool wasGrounded = true;
    static float peakY;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        missing.Clear(); wasGrounded = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // A respawn / stage reload must not read the fall from the previous scene's player (death height -> checkpoint).
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => wasGrounded = true;

    static GameObject Play(string name, Vector2 pos, float angle = 0f, int order = 103)
    {
        if (missing.Contains(name)) return null;
        GameObject go = SpriteFx.Play(name, pos, false, order);
        if (go == null) { missing.Add(name); return null; }
        if (angle != 0f) go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        return go;
    }

    // Call every frame the player is simulated (feet = ground contact point). Dust on touching down after a real fall.
    public static void TrackLand(bool grounded, Vector2 feet)
    {
        if (!grounded) { if (wasGrounded) peakY = feet.y; else if (feet.y > peakY) peakY = feet.y; }
        else if (!wasGrounded && peakY - feet.y >= RealFallHeight) Play("land-dust", feet);
        wasGrounded = grounded;
    }

    public static void SpawnerBreak(Vector2 center) => Play("spawner-break", center);
    public static GameObject EnemyWarpIn(Vector2 feet) => Play("enemy-warp-in", feet); // null = sheet missing (caller may fall back)

    // muzzle = sheet anchor, the flash points along +x; dir = shot direction (unit).
    public static void SniperMuzzle(Vector2 muzzle, Vector2 dir) => Play("sniper-muzzle", muzzle, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

    public static void Checkpoint(Vector2 feet) => Play("checkpoint", feet);
    public static void PlayerDeath(Vector2 feet) => Play("player-glitch-death", feet, 0f, 110);
    public static void HackPulse(Vector2 center) => Play("hack-pulse", center);

    // Looping 30 fps pad: returns the object, Destroy it to stop (or parent it to something that goes away).
    public static GameObject GoalOpen(Vector2 padBottomCenter) => Play("goal-open", padBottomCenter, 0f, 15);
}
