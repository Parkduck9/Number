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

// Menu NUMBER/Build STG-05 -> Assets/Scenes/STG05.unity (final boss room, one fixed screen 30 x 16.875 u, origin bottom-left).
// Idempotent: regenerates the scene each run. Also callable in batchmode: -executeMethod Stg05Builder.Build
// TestEnemy / EnemyNumber fields are guarded with HasProp so this compiles and runs even if those scripts change; the log lists what was skipped.
public static class Stg05Builder
{
    const string ScenePath = "Assets/Scenes/STG05.unity";
    const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    const string EnemyPrefab = "Assets/Prefabs/Prototype/TestEnemy.prefab";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string Ending01 = "Assets/Data/Dialogue/ending_01_placeholder.txt";
    const string Ending02 = "Assets/Data/Dialogue/ending_02_placeholder.txt";
    const int GroundLayer = 6, PlayerLayer = 7, EnemyLayer = 8;

    const float RoomW = 30f, RoomH = 16.875f;

    static readonly List<string> Todo = new List<string>();
    static Sprite square;

    [MenuItem("NUMBER/Build STG-05")]
    public static void Build()
    {
        foreach (var s in Enumerable.Range(0, EditorSceneManager.sceneCount).Select(EditorSceneManager.GetSceneAt))
            if (s.isDirty && s.path != ScenePath)
            {
                Debug.LogError($"[Stg05Builder] Scene '{s.name}' has unsaved changes. Save or discard it, then run NUMBER/Build STG-05 again.");
                return;
            }

        Todo.Clear();
        SetupLayers();
        square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        if (square == null || player == null || enemy == null)
        {
            Debug.LogError("[Stg05Builder] Missing Square sprite / Player / TestEnemy prefab. Run NUMBER/Build Prototype Scene first.");
            return;
        }
        var font = DialogueSetup.EnsureFont();

        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Background image (30 x 16.875 u at PPU 32, pivot bottom-left, behind everything); no image = the camera's solid colour shows.
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/bg_stg05_boss_room.png");
        if (bgSprite != null)
        {
            var bg = new GameObject("Background (boss room)").AddComponent<SpriteRenderer>();
            bg.sprite = bgSprite;
            bg.sortingOrder = -100;
            // spill light from the big broadcast screen (gold) + the stone lantern right of the stairs (warm), px on the image
            StageBuildKit.AddLamps(bg.transform, RoomW, new StageBuildKit.Lamp(480f, 230f, StageBuildKit.LampWarm, 8f, 0.1f), new StageBuildKit.Lamp(750f, 420f, StageBuildKit.LampWarm, 4f));
        }
        else Todo.Add("Background image missing (Assets/Art/Backgrounds/bg_stg05_boss_room.png)");

        // Room: ground + walls (layer Ground; walls invisible, visible floor strip so the room reads).
        BuildCollider("GROUND", 0f, RoomW, 0f, 2f, new Color32(0x3A, 0x3F, 0x4B, 0xFF));
        BuildCollider("WALL-L", -1f, 0f, 0f, RoomH, null);
        BuildCollider("WALL-R", RoomW, RoomW + 1f, 0f, RoomH, null);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(player);
        playerGo.transform.position = new Vector3(4f, 3f, 0f); // feet (4, 2), player pivot = feet + 1

        // Boss board: root at (15, 3.5) so the hack UI's 3u range works from the floor; screen visuals are children.
        var boardGo = new GameObject("BossBoard");
        boardGo.transform.position = new Vector3(15f, 3.5f, 0f);
        var screen = Block("Screen", new Color32(0x0B, 0x0F, 0x1E, 0xFF), 12f, 5f, 0);
        screen.transform.SetParent(boardGo.transform, false);
        screen.transform.position = new Vector3(15f, 11f, 0f);
        var border = Block("ScreenBorder", new Color32(0x4C, 0xE0, 0xFF, 0xFF), 12.3f, 5.3f, -1);
        border.transform.SetParent(boardGo.transform, false);
        border.transform.position = new Vector3(15f, 11f, 0f);
        var number = WorldText("Number", boardGo.transform, new Vector3(15f, 11.6f, 0f), 12f, new Color32(0x4C, 0xE0, 0xFF, 0xFF), font);
        var message = WorldText("Message", boardGo.transform, new Vector3(15f, 9.6f, 0f), 4f, Color.white, font);
        var board = boardGo.AddComponent<BroadcastBoard>();
        SetInt(board, "aiNumber", 3);
        SetObj(board, "label", number);
        SetFloat(board, "holdSeconds", 4f);        // (T) counter-hack hold time
        SetFloat(board, "holdSecondsForZero", 3f); // (T) hold time when the player hacks 0

        // Stop button: red block, inactive until the boss unlocks it.
        var button = Block("StopButton", new Color32(0xFF, 0x2A, 0x2A, 0xFF), 1.5f, 1f, 1);
        button.transform.position = new Vector3(26f, 2.5f, 0f);
        var sb = button.AddComponent<StopButton>();
        SetObj(sb, "ending01", AssetDatabase.LoadAssetAtPath<TextAsset>(Ending01));
        SetObj(sb, "ending02", AssetDatabase.LoadAssetAtPath<TextAsset>(Ending02));
        SetObj(sb, "font", font);
        button.SetActive(false);

        // Spawn points
        var spawnL = new GameObject("Spawn-L").transform; spawnL.position = new Vector3(2f, 14f, 0f);
        var spawnR = new GameObject("Spawn-R").transform; spawnR.position = new Vector3(28f, 14f, 0f);

        // Enemy templates (inactive; BossFight clones them).
        var templates = new GameObject("EnemyTemplates").transform;
        var citizen = BuildTemplate(enemy, "Template-Citizen", templates, t => { });
        // STAND-IN for the "AI bug": ranged gold 1 (mirrors EnemyVariantsTestSetup "Ranged").
        var bug = BuildTemplate(enemy, "Template-Bug", templates, t =>
        {
            Tune(t, "kind", 1);                // TestEnemy.Kind.Bug
            Tune(t, "ranged", true);
            Tune(t, "attackRangeX", 9f);       // (T)
            Tune(t, "lastStrikeDelay", 0.5f);  // (T)
            SetEnemyNumber(t, 1, 1, new Color(1f, 0.788f, 0.235f));
            SetVisualColor(t, new Color32(0xE8, 0xD0, 0x5A, 0xFF));
        });
        // STAND-IN for the "android": strong variant (mirrors EnemyVariantsTestSetup "Strong").
        var android = BuildTemplate(enemy, "Template-Android", templates, t =>
        {
            Tune(t, "kind", 2);                // TestEnemy.Kind.Android
            Tune(t, "lastStrikeDelay", 0.5f);  // (T)
            Tune(t, "unparryableNumber", 7);   // (T)
            Tune(t, "maxHp", 5);               // (T)
            SetEnemyNumber(t, 1, 7, null);
            SetVisualColor(t, new Color32(0x9A, 0xA7, 0xB5, 0xFF));
        });

        // Fight + view
        var fightGo = new GameObject("BossFight");
        var fight = fightGo.AddComponent<BossFight>();
        SetObj(fight, "board", board);
        SetObj(fight, "stopButton", button);
        SetObj(fight, "citizenTemplate", citizen);
        SetObj(fight, "bugTemplate", bug);
        SetObj(fight, "androidTemplate", android);
        var fso = new SerializedObject(fight);
        var pts = fso.FindProperty("spawnPoints");
        pts.arraySize = 2;
        pts.GetArrayElementAtIndex(0).objectReferenceValue = spawnL;
        pts.GetArrayElementAtIndex(1).objectReferenceValue = spawnR;
        fso.ApplyModifiedPropertiesWithoutUndo();
        var view = fightGo.AddComponent<BossBoardView>();
        SetObj(view, "fight", fight);
        SetObj(view, "board", board);
        SetObj(view, "message", message);

        BuildCamera();
        new GameObject("RespawnManager").AddComponent<RespawnManager>();
        BuildHud(font);

        // DialoguePlayer (the endings need DialoguePlayer.Instance)
        var dp = new GameObject("DialoguePlayer").AddComponent<DialoguePlayer>();
        SetObj(dp, "speakers", DialogueSetup.EnsureSpeakers());
        SetObj(dp, "font", font);
        StageBuildKit.BuildLighting(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AppendBuildScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[Stg05Builder] Built " + ScenePath + (Todo.Count == 0 ? "" : "\nTODO / skipped:\n - " + string.Join("\n - ", Todo)));
    }

    static void SetupLayers()
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

    // Axis-aligned solid on the Ground layer; color null = invisible.
    static void BuildCollider(string id, float x1, float x2, float y1, float y2, Color? color)
    {
        var go = new GameObject(id) { layer = GroundLayer };
        go.transform.position = new Vector3((x1 + x2) * 0.5f, (y1 + y2) * 0.5f, 0f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(x2 - x1, y2 - y1);
        if (color == null) return;
        var vis = Block(id + " visual", color.Value, x2 - x1, y2 - y1, 0);
        vis.transform.SetParent(go.transform, false);
        vis.transform.localPosition = Vector3.zero;
    }

    // Visible square of the given world size, centered on its position.
    static GameObject Block(string name, Color color, float w, float h, int order)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = order;
        Vector2 b = square.bounds.size;
        go.transform.localScale = new Vector3(w / b.x, h / b.y, 1f);
        return go;
    }

    static TMP_Text WorldText(string name, Transform parent, Vector3 pos, float size, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var t = go.AddComponent<TextMeshPro>();
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.sortingOrder = 10;
        t.rectTransform.sizeDelta = new Vector2(11f, size > 6f ? 4f : 1.5f);
        if (font != null) t.font = font;
        return t;
    }

    static GameObject BuildTemplate(GameObject prefab, string name, Transform parent, Action<GameObject> tune)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        var te = go.GetComponent<TestEnemy>();
        if (HasProp(te, "activationRange")) SetFloat(te, "activationRange", 0f); // 0 = always active
        else Todo.Add(name + ": TestEnemy.activationRange missing");
        if (HasProp(te, "groundMask")) SetInt(te, "groundMask", 1 << GroundLayer);
        tune(go);
        go.SetActive(false);
        return go;
    }

    static void Tune(GameObject go, string prop, bool v)
    {
        var te = go.GetComponent<TestEnemy>();
        if (HasProp(te, prop)) Apply(te, prop, p => p.boolValue = v); else Todo.Add($"{go.name}: TestEnemy.{prop} missing");
    }
    static void Tune(GameObject go, string prop, float v)
    {
        var te = go.GetComponent<TestEnemy>();
        if (HasProp(te, prop)) SetFloat(te, prop, v); else Todo.Add($"{go.name}: TestEnemy.{prop} missing");
    }
    static void Tune(GameObject go, string prop, int v)
    {
        var te = go.GetComponent<TestEnemy>();
        if (HasProp(te, prop)) SetInt(te, prop, v); else Todo.Add($"{go.name}: TestEnemy.{prop} missing");
    }

    static void SetEnemyNumber(GameObject go, int min, int max, Color? color)
    {
        var n = go.GetComponent<EnemyNumber>();
        if (HasProp(n, "minValue") && HasProp(n, "maxValue")) { SetInt(n, "minValue", min); SetInt(n, "maxValue", max); }
        else Todo.Add($"{go.name}: EnemyNumber min/max missing");
        if (color.HasValue)
        {
            if (HasProp(n, "numberColor")) Apply(n, "numberColor", p => p.colorValue = color.Value);
            else Todo.Add($"{go.name}: EnemyNumber.numberColor missing");
        }
    }

    static void SetVisualColor(GameObject go, Color color)
    {
        var sr = go.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) sr.color = color; else Todo.Add(go.name + ": no SpriteRenderer to tint");
    }

    // Appended (not first): STG-05 is the last level. Existing entries keep their order.
    static void AppendBuildScene()
    {
        var list = EditorBuildSettings.scenes.ToList();
        if (list.Any(s => s.path == ScenePath)) return;
        list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    static void BuildHud(TMP_FontAsset font)
    {
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
    }

    // Fixed camera: the whole room fits one screen, no follow.
    static void BuildCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        go.AddComponent<UniversalAdditionalCameraData>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x0B, 0x0F, 0x1E, 0xFF);
        go.transform.position = new Vector3(15f, 8.4375f, -10f);

        var setup = FindType("CameraSetup");
        if (setup != null) setup.GetMethod("Configure", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { go });
        else Todo.Add("Pixel Perfect Camera (CameraSetup) not found - camera is plain orthographic");
    }

    // ------------------------------------------------------------------ helpers

    static Type FindType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.GetName().Name.StartsWith("Assembly-CSharp")) continue;
            var t = asm.GetType(name);
            if (t != null) return t;
        }
        return null;
    }

    static bool HasProp(UnityEngine.Object o, string prop) => o != null && new SerializedObject(o).FindProperty(prop) != null;

    static void SetInt(UnityEngine.Object o, string prop, int v) => Apply(o, prop, p => p.intValue = v);
    static void SetFloat(UnityEngine.Object o, string prop, float v) => Apply(o, prop, p => p.floatValue = v);
    static void SetObj(UnityEngine.Object o, string prop, UnityEngine.Object v) => Apply(o, prop, p => p.objectReferenceValue = v);

    static void Apply(UnityEngine.Object o, string prop, Action<SerializedProperty> set)
    {
        var so = new SerializedObject(o);
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning($"[Stg05Builder] {o.GetType().Name} has no field '{prop}'"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
