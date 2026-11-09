using UnityEngine;
using UnityEngine.SceneManagement;

// One per level scene. Keeps the last checkpoint in a static record (survives scene reload),
// and on player death reloads the scene, which resets enemies; Start() then puts the player back at the record.
// First checkpoint = the player's start position (recorded when no record exists for this scene).
public class RespawnManager : MonoBehaviour
{
    static bool hasRecord;
    static Vector2 recordPos;
    static string recordScene;
    static bool respawnPending;

    PlayerController player;
    PlayerHealth health;
    float deadSince = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => ResetState();

    // New game / back to title: nothing from the previous run may leak into the next one.
    public static void ResetState() { hasRecord = false; recordScene = null; respawnPending = false; }

    public static void SetCheckpoint(Vector2 pos)
    {
        hasRecord = true;
        recordPos = pos;
        recordScene = SceneManager.GetActiveScene().name;
    }

    // True during a respawn reload when the record is this scene's and at pos (Checkpoint uses it to stay lit).
    // respawnPending is still set in Checkpoint.Awake (cleared in our Start); other loads reset to the start point.
    public static bool IsRecordedAt(Vector2 pos)
    {
        return respawnPending && hasRecord && recordScene == SceneManager.GetActiveScene().name && (recordPos - pos).sqrMagnitude < 0.01f;
    }

    // Pause menu: reload the scene and put the player at the last checkpoint (same path as after death).
    public static void RestartFromCheckpoint()
    {
        if (FindFirstObjectByType<RespawnManager>() != null) respawnPending = true; // scenes without one (STG00) would leak the flag
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void Start()
    {
        GameFlow.InputLocked = false; // a reload mid-dialogue must not leave input locked
        player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        health = player.GetComponent<PlayerHealth>();

        // Use the record only after our own death reload; any other load (new game, R, scene re-entry) starts fresh.
        bool respawn = respawnPending && hasRecord && recordScene == SceneManager.GetActiveScene().name;
        respawnPending = false;
        if (!respawn)
        {
            SetCheckpoint(player.transform.position); // game start point
            return;
        }

        player.transform.position = recordPos;
        var rb = player.GetComponent<Rigidbody2D>();
        rb.position = recordPos;
        rb.linearVelocity = Vector2.zero;
        FindFirstObjectByType<CameraFollow2D>()?.Snap(); // follow camera may have started before us
    }

    void Update()
    {
        if (player == null || !health.IsDead || player.CurrentState != PlayerController.State.Dead) return;
        if (deadSince < 0f) deadSince = Time.unscaledTime;
        else if (Time.unscaledTime - deadSince >= Juice.DeathRespawnDelay)
        {
            respawnPending = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
