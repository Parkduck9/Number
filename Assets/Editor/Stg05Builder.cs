using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Menu NUMBER/Build STG-05 -> Assets/Scenes/STG05.unity (final boss room, one fixed screen 30 x 16.875 u, origin bottom-left).
// Idempotent: regenerates the scene each run. Also callable in batchmode: -executeMethod Stg05Builder.Build
public static class Stg05Builder
{
    const string ScenePath = "Assets/Scenes/STG05.unity";
    const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string Ending01 = "Assets/Data/Dialogue/ending_01_placeholder.txt";
    const string DebrisArt = "Assets/Art/Backgrounds/STG02_05_Parts/buildings/rail_track.png";
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
        if (square == null || player == null)
        {
            Debug.LogError("[Stg05Builder] Missing Square sprite / Player prefab. Run NUMBER/Build Prototype Scene first.");
            return;
        }
        var font = DialogueSetup.EnsureFont();

        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Layered background parts (DECISIONS 17, 30 x 16.875 u one fixed screen, floor top y = 2); the kit reports missing parts through StageBuildKit.Todo.
        StageBuildKit.Square = square;
        StageBuildKit.Todo.Clear();
        PartsBackdropKit.Palace(new GameObject("Background (boss room)").transform, RoomW, 2f);
        Todo.AddRange(StageBuildKit.Todo);

        // Room: ground + walls (layer Ground; walls invisible, visible floor strip so the room reads).
        BuildCollider("GROUND", 0f, RoomW, 0f, 2f, new Color32(0x3A, 0x3F, 0x4B, 0xFF));
        BuildCollider("WALL-L", -1f, 0f, 0f, RoomH, null);
        BuildCollider("WALL-R", RoomW, RoomW + 1f, 0f, RoomH, null);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(player);
        playerGo.transform.position = new Vector3(4f, 3f, 0f); // feet (4, 2), player pivot = feet + 1

        // Final boss v2 (DECISIONS 23): the fused background boss builds its own (temporary code-drawn) parts at runtime.
        // No stop button / END-02 dial any more (DECISIONS 21): the ending is smashing the machines, then END-01.
        var fight = new GameObject("BossFight").AddComponent<BossFight>();
        SetObj(fight, "ending", AssetDatabase.LoadAssetAtPath<TextAsset>(Ending01));
        SetObj(fight, "font", font);
        // DECISIONS 40 falling roof chunks: the rail girder for now (empty = code-drawn slab). TODO art: a proper broken roof chunk.
        SetObj(fight, "debrisArt", AssetDatabase.LoadAssetAtPath<Sprite>(DebrisArt));

        // Room camera: CameraFollow2D clamped to the one-screen room (fixed view; shakes and the duel zoom need the follow).
        StageBuildKit.BuildCamera(playerGo.transform, new[] { new Rect(0f, 0f, RoomW, RoomH) }, new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        new GameObject("RespawnManager").AddComponent<RespawnManager>();
        BuildHud(font);

        // DialoguePlayer (the ending needs DialoguePlayer.Instance)
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

    // ------------------------------------------------------------------ helpers

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
