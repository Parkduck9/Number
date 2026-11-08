using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// DRAFT control hints: a strip of key / mouse icons (Assets/Resources/DraftUi, key letters drawn with the game font) near the bottom of
// STG00 (move, jump, dash) and STG01 (left click = attack / parry), shown once per run for a few seconds of play. Self-bootstrapping, no scene setup.
// No icon art (or DraftSprites.Use = false) = nothing is shown; the Title / Pause "Controls" panel still lists every key.
public class ControlHints : MonoBehaviour
{
    const float ShowSeconds = 10f;       // (T) playable seconds before the hint fades away
    const float Scale = 2f;              // 16 px icon art at 2x = 32 reference px
    static readonly HashSet<string> Shown = new HashSet<string>();

    // icon file, letter on the key ("" for mouse icons), hint EN / KO
    static readonly Dictionary<string, string[][]> Hints = new Dictionary<string, string[][]>
    {
        { "STG00", new[]
        {
            new[] { "ctrl_key_a", "A", "", "" },
            new[] { "ctrl_key_d", "D", "Move", "이동" },
            new[] { "ctrl_key_w", "W", "Jump", "점프" },
            new[] { "ctrl_key_shift", "SHIFT", "Dash", "대시" },
        } },
        { "STG01", new[]
        {
            new[] { "ctrl_mouse_left", "", "Attack · Parry", "공격 · 패링" },
        } },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Shown.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!DraftSprites.Use || !Hints.TryGetValue(scene.name, out var entries) || Shown.Contains(scene.name)) return;
        Shown.Add(scene.name);
        var go = new GameObject("ControlHints");
        go.AddComponent<ControlHints>().Build(entries);
    }

    float visibleFor;
    GameObject root;

    void Build(string[][] entries)
    {
        foreach (var e in entries)
            if (DraftHud.Ui(e[0]) == null) { Destroy(gameObject); return; } // missing art: skip the whole strip

        var canvas = UiKit.CreateCanvas("HintCanvas", transform, 35); // below the shockwave HUD (40)
        root = canvas.gameObject;
        var strip = UiKit.Rect("Strip", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 56f), Vector2.zero);
        var row = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 12f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        var fit = strip.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var dim = UiTheme.Current.dim;
        foreach (var e in entries)
        {
            Sprite icon = DraftHud.Ui(e[0]);
            var img = UiKit.FilledImage(UiKit.Rect("Icon " + e[0], strip, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), Color.white);
            img.sprite = icon;
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = icon.rect.width * Scale;
            le.preferredHeight = icon.rect.height * Scale;
            if (e[1] != "") UiKit.Label("Key", img.transform, new Vector2(0.5f, 0.5f), Vector2.zero, icon.rect.size * Scale, e[1], e[1], 12, Color.white);
            if (e[2] != "") UiKit.Label("Text " + e[2], strip, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, e[2], e[3], 18, dim);
        }
    }

    void Update()
    {
        if (root == null || GameFlow.InputLocked || FindFirstObjectByType<PlayerController>() == null) return;
        visibleFor += Time.unscaledDeltaTime;
        if (visibleFor >= ShowSeconds) Destroy(gameObject);
    }
}
