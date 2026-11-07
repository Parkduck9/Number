using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Menu NUMBER/Build Prototype Scene. Idempotent: rebuilds layers, scene and prefabs each run.
// See docs/design/PROTOTYPE_ARCH.md section 5.
public static class PrototypeSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Prototype.unity";
    const string PrefabDir = "Assets/Prefabs/Prototype";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string InputAssetPath = "Assets/Settings/InputSystem_Actions.inputactions";

    const int GroundLayer = 6, PlayerLayer = 7, EnemyLayer = 8;

    static Sprite square;

    [MenuItem("NUMBER/Build Prototype Scene")]
    public static void Build()
    {
        // The Prototype scene is regenerated below anyway, so save it silently (no modal when run via MCP).
        var open = EditorSceneManager.GetActiveScene();
        if (open.path == ScenePath && open.isDirty) EditorSceneManager.SaveScene(open);
        else if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SetupLayers();
        ImportTmpIfNeeded();

        square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        if (square == null) { Debug.LogError("Square sprite not found: " + SquarePath); return; }

        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(PrefabDir);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera();
        BuildLight();
        BuildGround();
        BuildPlayer();
        BuildEnemy();
        new GameObject("Debug").AddComponent<PrototypeDebug>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[PrototypeSceneBuilder] Built " + ScenePath);
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
    }

    static void SetLayer(SerializedProperty layers, int index, string name)
    {
        SerializedProperty p = layers.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(p.stringValue)) p.stringValue = name;
        else if (p.stringValue != name) Debug.LogWarning($"Layer {index} already used by '{p.stringValue}', expected '{name}'.");
    }

    static void ImportTmpIfNeeded()
    {
        if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            TMP_PackageResourceImporter.ImportResources(true, false, false);
    }

    static void BuildCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f; // 270px / 16 PPU / 2
        cam.backgroundColor = new Color32(0x0B, 0x0F, 0x1E, 0xFF);
        cam.clearFlags = CameraClearFlags.SolidColor;
        go.transform.position = new Vector3(0f, 0f, -10f);
    }

    static void BuildLight()
    {
        var go = new GameObject("Global Light 2D");
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Global;
        l.intensity = 1f;
    }

    static GameObject BuildSquare(string name, Color color, Vector2 size, int order)
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

    static void BuildGround()
    {
        var go = BuildSquare("Ground", new Color32(0x3A, 0x3F, 0x4B, 0xFF), new Vector2(60f, 2f), 0);
        go.transform.position = new Vector3(0f, -7f, 0f);
        go.layer = GroundLayer;
        go.AddComponent<BoxCollider2D>().size = square.bounds.size; // local size = sprite size; transform scale does the rest
    }

    static void BuildPlayer()
    {
        var root = new GameObject("Player") { layer = PlayerLayer };
        root.transform.position = new Vector3(-5f, -5f, 0f);

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 2f);

        var reader = root.AddComponent<PlayerInputReader>();
        root.AddComponent<PlayerHealth>();
        var ctrl = root.AddComponent<PlayerController>();

        var so = new SerializedObject(reader);
        so.FindProperty("asset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        so.ApplyModifiedProperties();

        so = new SerializedObject(ctrl);
        so.FindProperty("groundMask").intValue = 1 << GroundLayer;
        so.FindProperty("enemyMask").intValue = 1 << EnemyLayer;
        so.ApplyModifiedProperties();

        var visual = BuildSquare("Visual", new Color32(0x4C, 0xE0, 0xFF, 0xFF), new Vector2(1f, 2f), 10);
        visual.transform.SetParent(root.transform, false);
        visual.layer = PlayerLayer;

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabDir + "/Player.prefab", InteractionMode.AutomatedAction);
    }

    static void BuildEnemy()
    {
        var root = new GameObject("TestEnemy") { layer = EnemyLayer };
        root.transform.position = new Vector3(6f, -5f, 0f);

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        root.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 2f);

        var number = root.AddComponent<EnemyNumber>();
        var enemy = root.AddComponent<TestEnemy>();

        var visual = BuildSquare("Visual", new Color32(0xFF, 0x8C, 0x42, 0xFF), new Vector2(1f, 2f), 10);
        visual.transform.SetParent(root.transform, false);
        visual.layer = EnemyLayer;

        var numGo = new GameObject("Number") { layer = EnemyLayer };
        numGo.transform.SetParent(root.transform, false);
        numGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var tmp = numGo.AddComponent<TextMeshPro>();
        tmp.text = "3";
        tmp.fontSize = 6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 20;

        var so = new SerializedObject(number);
        so.FindProperty("label").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();

        so = new SerializedObject(enemy);
        so.FindProperty("playerMask").intValue = 1 << PlayerLayer;
        so.FindProperty("visual").objectReferenceValue = visual.GetComponent<SpriteRenderer>();
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabDir + "/TestEnemy.prefab", InteractionMode.AutomatedAction);
    }

    static void SetBuildScene()
    {
        var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != ScenePath) list.Add(s);
        EditorBuildSettings.scenes = list.ToArray();
    }
}
