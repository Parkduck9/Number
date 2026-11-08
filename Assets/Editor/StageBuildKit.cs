using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Shared helpers for Stg00/02/03/04Builder (same pattern as Stg01Builder, whose helpers are private).
// Optional runtime pieces (Checkpoint, RespawnManager, CameraFollow2D...) are resolved by name/property so this compiles without them;
// whatever is skipped is listed in Todo and printed by Finish().
public static class StageBuildKit
{
    public const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    public const string EnemyPrefab = "Assets/Prefabs/Prototype/TestEnemy.prefab";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string FontPath = "Assets/Fonts/Mulmaru/Mulmaru SDF.asset";
    const string TitleScenePath = "Assets/Scenes/Title.unity";
    const int GroundLayer = 6, PlayerLayer = 7, EnemyLayer = 8;

    public const float SegWidth = 50.625f, SegHeight = 16.875f;
    // DECISIONS 12-C: STG-01/02/04 segments are 1.5x wide (2430 px). The 1620 px background is tiled pixel-exact (BuildWideBackground).
    public const float WideWidth = SegWidth * 1.5f; // 75.9375
    public static readonly Color GroundColor = new Color32(0x4A, 0x40, 0x38, 0xFF);
    public static readonly Color OneWayColor = new Color32(0x7A, 0x6A, 0x55, 0xFF);

    // ------------------------------------------------------------------ data structs (segment index + segment-local coords)

    public enum Kind { Solid, OneWay }

    public struct Box
    {
        public string id; public int seg; public Kind kind; public float x1, x2, y1, y2;
        public Box(string id, int seg, Kind kind, float x1, float x2, float y1, float y2)
        { this.id = id; this.seg = seg; this.kind = kind; this.x1 = x1; this.x2 = x2; this.y1 = y1; this.y2 = y2; }
    }

    public struct Pt
    {
        public string id; public int seg; public float x, y; // y = feet
        public Pt(string id, int seg, float x, float y) { this.id = id; this.seg = seg; this.x = x; this.y = y; }
    }

    // fixedNumber 0 = reroll by progress. detect = activation range (u).
    // ranged = gold ranged bug, number fixed 1. cap > 0 = highest rolled number (placement rule: 5+ only from late STG-02).
    public struct Foe
    {
        public string id; public int seg; public float x, y; public int fixedNumber; public float progress, detect; public bool android, ranged; public int cap;
        public Foe(string id, int seg, float x, float y, int fixedNumber, float progress, float detect, bool android = false, bool ranged = false, int cap = 0)
        { this.id = id; this.seg = seg; this.x = x; this.y = y; this.fixedNumber = fixedNumber; this.progress = progress; this.detect = detect; this.android = android; this.ranged = ranged; this.cap = cap; }
    }

    // Gold ranged "1" (mirrors Stg05Builder's bug template / EnemyVariantsTestSetup "Ranged").
    public static void TuneRanged(GameObject go, EnemyNumber number, TestEnemy te)
    {
        SetInt(te, "kind", 1); // TestEnemy.Kind.Bug
        SetBool(te, "ranged", true);
        SetFloat(te, "attackRangeX", 9f);
        SetFloat(te, "lastStrikeDelay", 0.5f);
        SetInt(number, "minValue", 1);
        SetInt(number, "maxValue", 1);
        SetInt(number, "fixedNumber", 1);
        Apply(number, "numberColor", p => p.colorValue = new Color(1f, 0.788f, 0.235f));
        var sr = go.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) sr.color = new Color32(0xE8, 0xD0, 0x5A, 0xFF);
    }

    // Left/right walls of a segment (full height).
    public static Box[] Walls(string name, int seg, float width = SegWidth) => new[]
    {
        new Box("WALL-" + name + "-L", seg, Kind.Solid, -1f, 0f, 0f, SegHeight),
        new Box("WALL-" + name + "-R", seg, Kind.Solid, width, width + 1f, 0f, SegHeight),
    };

    // ------------------------------------------------------------------ session state

    public static readonly List<string> Todo = new List<string>();
    public static Sprite Square;
    public static GameObject Player, Enemy;

    // Unsaved-scene guard: never raises a modal (runs via MCP/batchmode too). Only the scene being rebuilt is discarded silently.
    public static bool CheckScene(string tag, string scenePath)
    {
        foreach (var s in Enumerable.Range(0, EditorSceneManager.sceneCount).Select(EditorSceneManager.GetSceneAt))
            if (s.isDirty && s.path != scenePath)
            {
                Debug.LogError($"[{tag}] Scene '{s.name}' has unsaved changes. Save or discard it, then run the build again.");
                return false;
            }
        return true;
    }

    // Common start: guard, layers, load prefabs/sprite. Returns false (after logging) when something is missing.
    public static bool Begin(string tag, string scenePath, bool needsPrefabs = true)
    {
        if (!CheckScene(tag, scenePath)) return false;
        Todo.Clear();
        SetupLayers();
        Square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        Player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        Enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        if (Square == null || Player == null || (needsPrefabs && Enemy == null))
        {
            Debug.LogError($"[{tag}] Missing Square sprite / Player / TestEnemy prefab. Run NUMBER/Build Prototype Scene first.");
            return false;
        }
        Directory.CreateDirectory("Assets/Scenes");
        return true;
    }

    public static void SaveScene(UnityEngine.SceneManagement.Scene scene, string tag, string scenePath)
    {
        EditorSceneManager.SaveScene(scene, scenePath);
        SyncBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"[StageBuildKit] [{tag}] Built {scenePath}" + (Todo.Count == 0 ? "" : "\nTODO / skipped:\n - " + string.Join("\n - ", Todo)));
    }

    public static Vector3 World(float[] segX, Pt p, float footToCenter) => new Vector3(segX[p.seg] + p.x, p.y + footToCenter, 0f);

    // ------------------------------------------------------------------ menu

    [MenuItem("NUMBER/Build All Stages (00,02,03,04)")]
    public static void BuildAll()
    {
        Stg00Builder.Build();
        Stg02Builder.Build();
        Stg03Builder.Build();
        Stg04Builder.Build();
        SyncBuildSettings();
    }

    // One click for playtests: every scene the game flow uses (Title, STG-00..05) + build order.
    [MenuItem("NUMBER/Build EVERYTHING (Title + STG-00~05)", priority = 0)]
    public static void BuildEverything()
    {
        TitleSceneBuilder.Build();
        Stg00Builder.Build();
        Stg01Builder.Build();
        Stg02Builder.Build();
        Stg03Builder.Build();
        Stg04Builder.Build();
        Stg05Builder.Build();
        SyncBuildSettings();
    }

    // Build Settings order: Title, then StageFlow.Stages (existing files only), then every other existing entry as it was.
    [MenuItem("NUMBER/Sync Build Settings (stage order)")]
    public static void SyncBuildSettings()
    {
        var paths = new List<string>();
        if (File.Exists(TitleScenePath)) paths.Add(TitleScenePath);
        foreach (var name in StageFlow.Stages)
        {
            string p = $"Assets/Scenes/{name}.unity";
            if (File.Exists(p) && !paths.Contains(p)) paths.Add(p);
        }
        foreach (var s in EditorBuildSettings.scenes)
            if (File.Exists(s.path) && !paths.Contains(s.path)) paths.Add(s.path);
        var old = EditorBuildSettings.scenes.ToDictionary(s => s.path, s => s.enabled);
        bool IsFlow(string p) => p == TitleScenePath || StageFlow.Stages.Any(n => p == $"Assets/Scenes/{n}.unity");
        EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, IsFlow(p) || (old.TryGetValue(p, out var en) && en))).ToArray();
        Debug.Log("[StageBuildKit] Build Settings: " + string.Join(", ", paths.Select(Path.GetFileNameWithoutExtension)));
    }

    // ------------------------------------------------------------------ scene objects

    public static void SetupLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        SetLayer(layers, GroundLayer, "Ground");
        SetLayer(layers, PlayerLayer, "Player");
        SetLayer(layers, EnemyLayer, "Enemy");
        tagManager.ApplyModifiedProperties();
        Physics2D.IgnoreLayerCollision(PlayerLayer, EnemyLayer, true);
        Physics2D.IgnoreLayerCollision(EnemyLayer, EnemyLayer, true);
    }

    static void SetLayer(SerializedProperty layers, int index, string name)
    {
        SerializedProperty p = layers.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = name;
        else if (p.stringValue != name) Debug.LogWarning($"Layer {index} already used by '{p.stringValue}', expected '{name}'.");
    }

    // Ground-layer collider. OneWay = PlatformEffector2D (y2 = top surface). visible = draw a tinted Square so levels play without art.
    public static GameObject BuildCollider(Transform root, Box b, bool visible)
    {
        var go = new GameObject(b.id) { layer = GroundLayer };
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3((b.x1 + b.x2) * 0.5f, (b.y1 + b.y2) * 0.5f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(b.x2 - b.x1, b.y2 - b.y1);
        if (b.kind == Kind.OneWay)
        {
            col.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;
            eff.useSideFriction = false;
            eff.useSideBounce = false;
        }
        if (visible)
            AddSquare(go.transform, "Visual (PLACEHOLDER)", Vector2.zero, col.size, b.kind == Kind.OneWay ? OneWayColor : GroundColor, 0);
        return go;
    }

    public static GameObject AddSquare(Transform parent, string name, Vector2 localPos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f) / Square.bounds.size.x;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    public static GameObject BuildTrigger(Transform root, Box b)
    {
        var go = new GameObject(b.id);
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3((b.x1 + b.x2) * 0.5f, (b.y1 + b.y2) * 0.5f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(b.x2 - b.x1, b.y2 - b.y1);
        col.isTrigger = true;
        return go;
    }

    public static void BuildCheckpoint(Transform root, Pt c)
    {
        var go = new GameObject(c.id);
        go.transform.SetParent(root, false);
        // Pivot at player-center height (feet + 1): Checkpoint records transform.position as the respawn point.
        go.transform.localPosition = new Vector3(c.x, c.y + 1f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.5f, 3f);
        col.offset = new Vector2(0f, 0.5f); // trigger spans feet .. feet + 3
        col.isTrigger = true;

        var marker = AddSquare(go.transform, "Marker (placeholder)", new Vector2(0f, -0.5f), new Vector2(0.3f, 1f), Color.white, 3);
        var cp = AddByName("Checkpoint", go, "Card A Checkpoint (" + c.id + ")");
        if (cp != null) SetObj(cp, "marker", marker.GetComponent<SpriteRenderer>());
    }

    public static void BuildEnemy(Transform root, Foe f)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Enemy);
        go.name = f.android ? f.id + " (ANDROID)" : f.id;
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(f.x, f.y + 1f, 0f);

        var number = go.GetComponent<EnemyNumber>();
        var te = go.GetComponent<TestEnemy>();
        if (f.progress > 0f) SetFloat(number, "progress", f.progress);
        if (f.cap > 0 && f.fixedNumber <= 0 && !f.android) SetInt(number, "maxValue", f.cap);

        if (f.fixedNumber > 0)
        {
            if (HasProp(number, "fixedNumber")) SetInt(number, "fixedNumber", f.fixedNumber);
            else if (HasProp(number, "minValue") && HasProp(number, "maxValue")) // min == max pins the number
            {
                SetInt(number, "minValue", f.fixedNumber);
                SetInt(number, "maxValue", f.fixedNumber);
            }
            else Todo.Add($"{f.id}: set fixed number {f.fixedNumber} (EnemyNumber has no fixedNumber/min-max field yet)");
        }

        if (HasProp(te, "activationRange")) SetFloat(te, "activationRange", f.detect);
        else Todo.Add($"{f.id}: activation range {f.detect}u (TestEnemy.activationRange missing)");
        if (HasProp(te, "groundMask")) SetInt(te, "groundMask", 1 << GroundLayer); // enables the roof-edge rule (D2)

        if (f.ranged) TuneRanged(go, number, te);

        if (f.android)
        {
            if (HasProp(te, "kind")) SetInt(te, "kind", 2); // TestEnemy.Kind.Android
            // Strong-enemy rules, same as Stg05Builder's android template: number 1~7, 7th strike dash-only, 0.5s last-strike delay.
            SetFloat(te, "lastStrikeDelay", 0.5f);
            SetInt(te, "unparryableNumber", 7);
            SetInt(te, "maxHp", 5);
            if (f.fixedNumber <= 0) { SetInt(number, "minValue", 1); SetInt(number, "maxValue", 7); } // fixed-number androids keep the pin set above
        }
    }

    public static void BuildLight(float intensity, Color color)
    {
        var go = new GameObject("Global Light 2D");
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Global;
        l.intensity = intensity;
        l.color = color;
    }

    public static Rect SegRect(float worldX, float width = SegWidth) => new Rect(worldX, 0f, width, SegHeight);

    public static GameObject BuildCamera(Transform player, Rect[] segments, Color background)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        go.AddComponent<UniversalAdditionalCameraData>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f; // 540 / 2 / PPU 32
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        go.transform.position = new Vector3(segments[0].center.x, SegHeight * 0.5f, -10f);

        var setup = FindType("CameraSetup");
        if (setup != null) setup.GetMethod("Configure", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { go });
        else Todo.Add("Pixel Perfect Camera (Card D CameraSetup) not found - camera is plain orthographic");

        var follow = AddByName("CameraFollow2D", go, "Card D CameraFollow2D");
        if (follow == null) { Todo.Add("Camera follow: add CameraFollow2D (or any follow) and target the Player"); return go; }
        SetObj(follow, "target", player);

        // y fixed (view height == segment height), x clamped per segment. Switched at runtime by SegmentCameraBounds.
        var seg = go.AddComponent<SegmentCameraBounds>();
        SetObj(seg, "player", player);
        SetObj(seg, "cameraFollow", follow);
        var so = new SerializedObject(seg);
        var arr = so.FindProperty("segments");
        arr.arraySize = segments.Length;
        for (int i = 0; i < segments.Length; i++) arr.GetArrayElementAtIndex(i).rectValue = segments[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // Face-portrait health HUD (top-left) + broadcast number HUD (always); allowHack = dial and [E] prompt (STG-04/05 only, DECISIONS 01-fixes #5).
    public static void BuildHud(bool allowHack)
    {
        var font = LoadFont();
        var canvasGo = new GameObject("HUD Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        scaler.matchWidthOrHeight = 0.5f;

        var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
        faceGo.transform.SetParent(canvasGo.transform, false);
        var fr = (RectTransform)faceGo.transform;
        fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0f, 1f);
        fr.anchoredPosition = new Vector2(16f, -16f);
        fr.sizeDelta = new Vector2(64f, 64f);
        var img = faceGo.GetComponent<Image>();
        img.raycastTarget = false;

        var labelGo = new GameObject("StageName", typeof(RectTransform));
        labelGo.transform.SetParent(canvasGo.transform, false);
        var lr = (RectTransform)labelGo.transform;
        lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0f, 1f);
        lr.anchoredPosition = new Vector2(88f, -16f);
        lr.sizeDelta = new Vector2(200f, 24f);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = 18f;
        label.raycastTarget = false;
        if (font != null) label.font = font;

        var hud = canvasGo.AddComponent<FaceHealthHud>();
        SetObj(hud, "face", img);
        SetObj(hud, "label", label);

        var ui = new GameObject("BroadcastHackUi").AddComponent<BroadcastHackUi>();
        if (font != null) SetObj(ui, "font", font);
        SetBool(ui, "allowHack", allowHack);
    }

    public static void BuildDialoguePlayer()
    {
        var dp = new GameObject("DialoguePlayer").AddComponent<DialoguePlayer>();
        SetObj(dp, "speakers", DialogueSetup.EnsureSpeakers());
        SetObj(dp, "font", DialogueSetup.EnsureFont());
    }

    // Placeholder billboard: dark panel + big yellow world-space number. zoneX = world x range the board rules (field arrives with the enemy-rules branch).
    public static BroadcastBoard BuildBoard(Transform parent, string id, Vector2 localPos, int aiNumber, Vector2 zoneX)
    {
        var go = new GameObject(id);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        AddSquare(go.transform, "Panel (PLACEHOLDER)", Vector2.zero, new Vector2(4f, 2.5f), new Color32(0x14, 0x16, 0x1E, 0xFF), -50);

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var label = textGo.AddComponent<TextMeshPro>();
        var font = LoadFont();
        if (font != null) label.font = font;
        label.text = aiNumber.ToString();
        label.fontSize = 12f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color32(0xFF, 0xD2, 0x4A, 0xFF);
        label.sortingOrder = -49;
        ((RectTransform)textGo.transform).sizeDelta = new Vector2(4f, 2.5f);

        var board = go.AddComponent<BroadcastBoard>();
        SetInt(board, "aiNumber", aiNumber);
        SetObj(board, "label", label);
        if (HasProp(board, "zoneX")) SetVector2(board, "zoneX", zoneX);
        else Todo.Add($"{id}: set BroadcastBoard.zoneX = ({zoneX.x}, {zoneX.y}) (field missing; arrives with the enemy-rules branch)");
        return board;
    }

    // Square scaled to cover the segment (bottom-left at the root).
    public static void BuildSolidBackground(Transform root, float width, float height, Color color)
    {
        AddSquare(root, "Background (PLACEHOLDER solid)", new Vector2(width * 0.5f, height * 0.5f), new Vector2(width, height), color, -100);
    }

    // Final background art (Assets/Art/Backgrounds, 1 px = 1/32 u, so it is not scaled); falls back to the solid colour when the image is missing.
    public static void BuildImageBackground(Transform root, string assetPath, float width, float height, Color fallback)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            Todo.Add("Background image missing (" + assetPath + "): solid colour used");
            BuildSolidBackground(root, width, height, fallback);
            return;
        }
        if (width > sprite.bounds.size.x + 0.01f) BuildWideBackground(root, sprite, width);
        else BuildSpriteBackground(root, sprite, width);
    }

    // Segment wider than the image (DECISIONS 12-C), native scale only (never a non-integer stretch):
    // the image is right-aligned so its right edge (gate / door art) ends the segment, and the strip to its left is a mirrored copy
    // (scale.x -1 around the shared edge = pixel-continuous seam). The mirrored copy also reaches left of the segment origin
    // (off-camera), so segments must be at least 2 * imageWidth (101.25u) apart or the copies overlap (Stg01/02/04 put SEG-B at x 110).
    public static void BuildWideBackground(Transform root, Sprite sprite, float width)
    {
        float w = sprite.bounds.size.x, off = width - w;
        if (off > w) Todo.Add($"Background {sprite.name}: segment {width}u is wider than 2 images ({2f * w}u), right part left empty");
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject(i == 0 ? "Background (image)" : "Background (mirrored fill)");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(off, 0f, 0f); // bottom-left pivot: normal copy spans [off, width], mirrored [off - w, off]
            go.transform.localScale = new Vector3(i == 0 ? 1f : -1f, 1f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100;
        }
    }

    // Image background placed bottom-left (the sprite's pivot is BottomLeft), scaled to the segment width.
    public static void BuildSpriteBackground(Transform root, Sprite sprite, float width)
    {
        var go = new GameObject("Background (PLACEHOLDER concept image)");
        go.transform.SetParent(root, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -100;
        float k = width / sprite.bounds.size.x;
        go.transform.localScale = new Vector3(k, k, 1f);
    }

    static TMP_FontAsset LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null && !Todo.Contains(FontTodo)) Todo.Add(FontTodo);
        return font;
    }
    const string FontTodo = "Mulmaru SDF font missing - text uses the default TMP font (run the dialogue setup menu first)";

    // ------------------------------------------------------------------ reflection / SerializedObject helpers

    public static Type FindType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.GetName().Name.StartsWith("Assembly-CSharp")) continue;
            var t = asm.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    public static Component AddByName(string typeName, GameObject go, string what)
    {
        var t = FindType(typeName);
        if (t != null) return go.AddComponent(t);
        Todo.Add(what + " not found (type " + typeName + ")");
        return null;
    }

    public static bool HasProp(UnityEngine.Object o, string prop) => o != null && new SerializedObject(o).FindProperty(prop) != null;

    public static void SetInt(UnityEngine.Object o, string prop, int v) => Apply(o, prop, p => p.intValue = v);
    public static void SetFloat(UnityEngine.Object o, string prop, float v) => Apply(o, prop, p => p.floatValue = v);
    public static void SetBool(UnityEngine.Object o, string prop, bool v) => Apply(o, prop, p => p.boolValue = v);
    public static void SetObj(UnityEngine.Object o, string prop, UnityEngine.Object v) => Apply(o, prop, p => p.objectReferenceValue = v);
    public static void SetVector2(UnityEngine.Object o, string prop, Vector2 v) => Apply(o, prop, p => p.vector2Value = v);

    public static void Apply(UnityEngine.Object o, string prop, Action<SerializedProperty> set)
    {
        var so = new SerializedObject(o);
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning($"[StageBuildKit] {o.GetType().Name} has no field '{prop}'"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
