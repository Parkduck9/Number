using UnityEngine;
using UnityEngine.SceneManagement;

// Stage order (GDD ch.8: STG-00 .. STG-05) and scene switching. Static, no scene object needed.
// Missing stage scenes (not built yet / not in Build Settings) are skipped with a log line.
// Checkpoint reset on stage change needs nothing here: RespawnManager only uses its record after its own death reload,
// so any other LoadScene starts the new stage fresh and unlocks input.
public static class StageFlow
{
    public const string TitleScene = "Title"; // Card D's title scene; loaded by name if it exists
    static readonly string[] Stages = { "STG00", "STG01", "STG02", "STG03", "STG04", "STG05" };

    // Loads the next existing stage after the current scene. Returns false (and logs) when there is none.
    public static bool LoadNext()
    {
        string current = SceneManager.GetActiveScene().name;
        int i = System.Array.IndexOf(Stages, current);
        if (i < 0) { Debug.LogWarning($"[StageFlow] '{current}' is not in the stage list; cannot pick a next stage."); return false; }
        for (int n = i + 1; n < Stages.Length; n++)
        {
            if (Application.CanStreamedLevelBeLoaded(Stages[n])) { Load(Stages[n]); return true; }
            Debug.Log($"[StageFlow] {Stages[n]} does not exist yet (or is not in Build Settings); skipped.");
        }
        Debug.Log($"[StageFlow] No stage after {current}.");
        return false;
    }

    public static void LoadTitle()
    {
        if (Application.CanStreamedLevelBeLoaded(TitleScene)) Load(TitleScene);
        else Debug.LogWarning($"[StageFlow] Scene '{TitleScene}' not found (not built yet or not in Build Settings); staying here.");
    }

    static void Load(string scene)
    {
        GameFlow.InputLocked = false;
        SceneManager.LoadScene(scene);
    }
}
