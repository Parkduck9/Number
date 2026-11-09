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

    // The goal's camera segment (SegmentCameraBounds): enemies and spawners in other segments do not count, so running past
    // SEG-A (one-way door) or respawning in SEG-B cannot soft-lock the exit. No segments in the scene = the whole scene counts.
    public static bool SegmentAt(float worldX, out float xMin, out float xMax)
    {
        var seg = Object.FindFirstObjectByType<SegmentCameraBounds>();
        if (seg != null && seg.TryGetRangeAt(worldX, out xMin, out xMax)) return true;
        xMin = float.NegativeInfinity; xMax = float.PositiveInfinity;
        return false;
    }

    public static int EnemiesLeft(float xMin, float xMax)
    {
        int n = 0;
        foreach (var e in Object.FindObjectsByType<TestEnemy>(FindObjectsSortMode.None))
        {
            float x = e.transform.position.x;
            if (e.Current != TestEnemy.State.Stunned && x >= xMin && x <= xMax) n++; // inactive (vanished) enemies are not returned
        }
        return n;
    }

    public static bool IsWatched(TestEnemy e)
    {
        foreach (var s in Spawners) if (s != null && s.Watches(e)) return true;
        return false;
    }

    public static int SpawnersLeft(float xMin, float xMax)
    {
        int n = 0;
        foreach (var s in Spawners)
            if (s != null && !s.Broken && s.transform.position.x >= xMin && s.transform.position.x <= xMax) n++;
        return n;
    }

    // goalPos = the exit trigger. true = the player may leave; false = the hint was shown.
    public static bool TryClear(Vector2 goalPos, bool showHint = true)
    {
        SegmentAt(goalPos.x, out float xMin, out float xMax);
        int enemies = EnemiesLeft(xMin, xMax), spawners = SpawnersLeft(xMin, xMax);
        if (enemies == 0 && spawners == 0) return true;
        if (!showHint) return false;
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
