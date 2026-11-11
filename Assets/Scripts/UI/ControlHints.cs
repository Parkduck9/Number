using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// DRAFT control hints: a strip of key / mouse icons (Assets/Resources/DraftUi, key letters drawn with the game font) near the bottom of
// STG00 (move, jump, issen), shown once per run for a few seconds of play. Self-bootstrapping, no scene setup.
// No icon art (or DraftSprites.Use = false) = nothing is shown; the Title / Pause "Controls" panel still lists every key.
public class ControlHints : MonoBehaviour
{
    const float ShowSeconds = 10f;       // (T) playable seconds before the hint fades away
    const float Scale = 2f;              // 16 px icon art at 2x = 32 reference px
    static readonly Color PlateColor = new Color32(0x0B, 0x0F, 0x1E, 0xC0); // (T) UiTheme background navy, ~75% opaque
    static readonly HashSet<string> Shown = new HashSet<string>();

    // icon file, letter on the key ("" for mouse icons), hint EN / KO
    static readonly Dictionary<string, string[][]> Hints = new Dictionary<string, string[][]>
    {
        { "STG00", new[]
        {
            new[] { "ctrl_key_a", "A", "", "" },
            new[] { "ctrl_key_d", "D", "Move", "이동" },
            new[] { "ctrl_key_space", "SPACE", "Jump", "점프" },
            new[] { "ctrl_key_shift", "SHIFT", "Issen (level enemy)", "일섬 (같은 높이 적)" },
        } },
        { "STG05", new[] // boss room (DECISIONS 38): the rope's new targets
        {
            new[] { "ctrl_mouse_right", "", "Rope: hooks & open hatch", "갈고리 줄: 고리 · 열린 해치" },
        } },
        { "LuckyDice", new[] // not a scene: shown by Show("LuckyDice") when the Luddite unlocks it (STG03)
        {
            new[] { "ctrl_key_e", "E", "Lucky dice · 7 hits = 1 roll", "점괘 주사위 · 적중 7번 = 굴림 1번" },
        } },
    };

    // Shows a non-scene strip once per run (e.g. an ability unlocked mid-level).
    public static void Show(string id)
    {
        if (!DraftSprites.Use || !Hints.TryGetValue(id, out var entries) || Shown.Contains(id)) return;
        Shown.Add(id);
        new GameObject("ControlHints").AddComponent<ControlHints>().Build(entries);
    }

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

    const float SlideIn = 0.3f, FadeOut = 0.2f, SlideDistance = 120f; // (T) toast timings, reference px
    const float RestY = 56f;
    float visibleFor, age, hideAt = -1f;
    GameObject root;
    RectTransform stripRt;
    CanvasGroup group;

    void Build(string[][] entries)
    {
        foreach (var e in entries)
            if (DraftHud.Ui(e[0]) == null) { Destroy(gameObject); return; } // missing art: skip the whole strip

        var canvas = UiKit.CreateCanvas("HintCanvas", transform, 35); // below the lucky dice HUD (40)
        root = canvas.gameObject;
        group = root.AddComponent<CanvasGroup>();
        var strip = UiKit.Rect("Strip", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, RestY - SlideDistance), Vector2.zero);
        stripRt = strip;
        var row = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 12f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        var fit = strip.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        // Readability on any ground: dark translucent plate behind the strip (sized by the fitter + padding), light text.
        row.padding = new RectOffset(12, 12, 6, 6);
        var plate = UiKit.FilledImage(strip, PlateColor);
        if (UiKit.Slice(plate, DraftHud.Ui("hint_plate_9s"))) row.padding = new RectOffset(14, 14, 8, 8); // skin borders are 8 x 6 ref px

        var textColor = UiTheme.Current.text;
        foreach (var e in entries)
        {
            Sprite icon = DraftHud.Ui(e[0]);
            var img = UiKit.FilledImage(UiKit.Rect("Icon " + e[0], strip, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), Color.white);
            img.sprite = icon;
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = icon.rect.width * Scale;
            le.preferredHeight = icon.rect.height * Scale;
            if (e[1] != "") UiKit.Label("Key", img.transform, new Vector2(0.5f, 0.5f), Vector2.zero, icon.rect.size * Scale, e[1], e[1], 12, Color.white);
            if (e[2] != "") UiKit.Label("Text " + e[2], strip, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, e[2], e[3], 18, textColor);
        }
    }

    PlayerController player;

    void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>(); // cached: was a full scene search every frame
        if (root == null) return;
        // Toast: slide up from below the screen edge with back-out, then slide down + fade out.
        age += Time.unscaledDeltaTime;
        if (hideAt >= 0f)
        {
            float k = (Time.unscaledTime - hideAt) / FadeOut;
            if (k >= 1f) { Destroy(gameObject); return; }
            group.alpha = 1f - k;
            stripRt.anchoredPosition = new Vector2(0f, RestY - SlideDistance * k);
            return;
        }
        stripRt.anchoredPosition = new Vector2(0f, RestY - SlideDistance * (1f - UiMotion.BackOut(age / SlideIn)));
        if (GameFlow.InputLocked || player == null) return;
        visibleFor += Time.unscaledDeltaTime;
        if (visibleFor >= ShowSeconds) hideAt = Time.unscaledTime;
    }
}
