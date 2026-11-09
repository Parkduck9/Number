using System.Collections.Generic;
using TMPro;
using UnityEngine;

// DECISIONS 15: a stage is cleared only when every enemy is dead and every spawn device is broken.
// Used by StageGoal and LevelTransition (Goal mode). A refused exit shows a short bilingual hint.
public static class StageClear
{
    public static readonly List<EnemySpawner> Spawners = new List<EnemySpawner>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Spawners.Clear(); // Domain Reload is off

    public static int EnemiesLeft()
    {
        int n = 0;
        foreach (var e in Object.FindObjectsByType<TestEnemy>(FindObjectsSortMode.None))
            if (e.Current != TestEnemy.State.Stunned) n++; // inactive (vanished) enemies are not returned
        return n;
    }

    public static bool IsWatched(TestEnemy e)
    {
        foreach (var s in Spawners) if (s != null && s.Watches(e)) return true;
        return false;
    }

    public static int SpawnersLeft()
    {
        int n = 0;
        foreach (var s in Spawners) if (s != null && !s.Broken) n++;
        return n;
    }

    // true = the player may leave; false = the hint was shown.
    public static bool TryClear()
    {
        int enemies = EnemiesLeft(), spawners = SpawnersLeft();
        if (enemies == 0 && spawners == 0) return true;
        string en = "", ko = "";
        if (enemies > 0) { en = "Enemies left: " + enemies; ko = "적 " + enemies + " 남음"; }
        if (spawners > 0)
        {
            string sep = enemies > 0 ? "\n" : "";
            en += sep + "Break the spawner"; ko += sep + "스폰 장치를 부숴라";
        }
        ShowHint(en, ko);
        return false;
    }

    static StageClearHint hint;

    static void ShowHint(string en, string ko)
    {
        if (hint == null)
        {
            var canvas = UiKit.CreateCanvas("StageClearHint", null, 150);
            var label = UiKit.Label("Hint", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(700f, 80f), en, ko, 24, UiTheme.Current.text);
            label.textWrappingMode = TextWrappingModes.Normal;
            hint = canvas.gameObject.AddComponent<StageClearHint>();
            hint.Init(label);
        }
        hint.Show(en, ko);
    }
}

public class StageClearHint : MonoBehaviour
{
    const float Hold = 2f;
    TMP_Text label;
    float hideAt;

    public void Init(TMP_Text text) { label = text; }

    public void Show(string en, string ko)
    {
        label.GetComponent<LocalizedText>().Set(label, en, ko);
        label.gameObject.SetActive(true);
        hideAt = Time.unscaledTime + Hold;
    }

    void Update()
    {
        if (label.gameObject.activeSelf && Time.unscaledTime >= hideAt) label.gameObject.SetActive(false);
    }
}
