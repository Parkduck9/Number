using UnityEngine;
using UnityEngine.SceneManagement;

// Stage order (GDD ch.8: STG-00 .. STG-05) and scene switching. Static, no scene object needed.
// Missing stage scenes (not built yet / not in Build Settings) are skipped with a log line.
// Checkpoint reset on stage change needs nothing here: RespawnManager only uses its record after its own death reload,
// so any other LoadScene starts the new stage fresh and unlocks input.
public static class StageFlow
{
    public const string TitleScene = "Title"; // Card D's title scene; loaded by name if it exists
    public static readonly System.Collections.Generic.IReadOnlyList<string> Stages = new[] { "STG00", "STG01", "STG02", "STG03", "STG04", "STG05" };

    // Field BGM for the title and every stage scene (a scene reload after a death switches the boss track back). Test scenes stay silent.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitBgm()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == TitleScene || System.Linq.Enumerable.Contains(Stages, scene.name)) Bgm.Play(BgmId.Field);
    }

    // Loads the next existing stage after the current scene. Returns false (and logs) when there is none.
    public static bool LoadNext()
    {
        string current = SceneManager.GetActiveScene().name;
        int i = -1;
        for (int k = 0; k < Stages.Count; k++) if (Stages[k] == current) { i = k; break; }
        if (i < 0) { Debug.LogWarning($"[StageFlow] '{current}' is not in the stage list; cannot pick a next stage."); return false; }
        for (int n = i + 1; n < Stages.Count; n++)
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

    // StageSkipKeys (hidden test shortcut). Also clears a pause menu's timeScale 0.
    public static void LoadStage(string scene)
    {
        if (!Application.CanStreamedLevelBeLoaded(scene)) { Debug.LogWarning($"[StageFlow] Scene '{scene}' not in Build Settings."); return; }
        Time.timeScale = 1f;
        Load(scene);
    }

    static void Load(string scene)
    {
        GameFlow.InputLocked = false;
        SceneManager.LoadScene(scene);
    }
}
