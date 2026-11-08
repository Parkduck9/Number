using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Menu NUMBER/Build Checkpoint Test Scene -> Assets/Scenes/Test_Checkpoint.unity (separate from Prototype.unity).
// Needs the Player/TestEnemy prefabs made by NUMBER/Build Prototype Scene.
public static class CheckpointTestSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Test_Checkpoint.unity";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    const string EnemyPrefab = "Assets/Prefabs/Prototype/TestEnemy.prefab";
    const int GroundLayer = 6;

    static Sprite square;

    [MenuItem("NUMBER/Build Checkpoint Test Scene")]
    public static void Build()
    {
        square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        if (square == null || player == null || enemy == null)
        {
            Debug.LogError("Missing square sprite or Player/TestEnemy prefab. Run NUMBER/Build Prototype Scene first.");
            return;
        }

        var open = EditorSceneManager.GetActiveScene();
        if (open.path == ScenePath && open.isDirty) EditorSceneManager.SaveScene(open);
        else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x0B, 0x0F, 0x1E, 0xFF);
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        new GameObject("Global Light 2D").AddComponent<Light2D>().lightType = Light2D.LightType.Global;

        var ground = Square("Ground", new Color32(0x3A, 0x3F, 0x4B, 0xFF), new Vector2(60f, 2f), 0);
        ground.transform.position = new Vector3(0f, -7f, 0f);
        ground.layer = GroundLayer;
        ground.AddComponent<BoxCollider2D>().size = square.bounds.size;

        var p = (GameObject)PrefabUtility.InstantiatePrefab(player);
        p.transform.position = new Vector3(-5f, -5f, 0f);

        var e = (GameObject)PrefabUtility.InstantiatePrefab(enemy);
        e.transform.position = new Vector3(13f, -5f, 0f);

        // Static camera sees x in [-15, 15] (16:9), so keep everything inside that range.
        BuildCheckpoint("Checkpoint A", 2f);
        BuildCheckpoint("Checkpoint B", 9f);

        new GameObject("Respawn").AddComponent<RespawnManager>();
        new GameObject("Debug").AddComponent<PrototypeDebug>();
        BuildHud();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[CheckpointTestSceneBuilder] Built " + ScenePath);
    }

    static GameObject Square(string name, Color color, Vector2 size, int order)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        sr.sortingOrder = order;
        Vector2 b = square.bounds.size;
        go.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1f);
        return go;
    }

    static void BuildCheckpoint(string name, float x)
    {
        var root = new GameObject(name);
        root.transform.position = new Vector3(x, -5f, 0f);
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 3f);

        var marker = Square("Marker", Color.gray, new Vector2(0.3f, 2f), 5);
        marker.transform.SetParent(root.transform, false);

        var cp = root.AddComponent<Checkpoint>();
        var so = new SerializedObject(cp);
        so.FindProperty("marker").objectReferenceValue = marker.GetComponent<SpriteRenderer>();
        so.ApplyModifiedProperties();
    }

    static void BuildHud()
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

        var hud = canvasGo.AddComponent<FaceHealthHud>();
        var so = new SerializedObject(hud);
        so.FindProperty("face").objectReferenceValue = img;
        so.FindProperty("label").objectReferenceValue = label;
        so.ApplyModifiedProperties();
    }

    static void AddToBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list) if (s.path == ScenePath) return;
        list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
