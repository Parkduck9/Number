using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Menu NUMBER/Build Enemy Variants Test Scene -> Assets/Scenes/Test_Enemies.unity (separate from Prototype.unity).
// Player + melee citizen, ranged (gold 1), strong (1-7, 7th strike unparryable) + a roof ledge with a citizen on it.
public static class EnemyVariantsTestSetup
{
    const string ScenePath = "Assets/Scenes/Test_Enemies.unity";
    const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const int GroundLayer = 6, PlayerLayer = 7, EnemyLayer = 8;

    static Sprite square;

    [MenuItem("NUMBER/Build Enemy Variants Test Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        var playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        if (square == null || playerAsset == null)
        {
            Debug.LogError("Missing square sprite or Player prefab. Run NUMBER/Build Prototype Scene once first.");
            return;
        }
        SetupLayers();
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera") { tag = "MainCamera" };
        var c = cam.AddComponent<Camera>();
        cam.AddComponent<AudioListener>();
        c.orthographic = true;
        c.orthographicSize = 8.4375f;
        c.backgroundColor = new Color32(0x0B, 0x0F, 0x1E, 0xFF);
        c.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        var follow = cam.AddComponent<CameraFollow2D>();

        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;

        Block("Ground", new Vector2(0f, -7f), new Vector2(80f, 2f), new Color32(0x3A, 0x3F, 0x4B, 0xFF));
        Block("Roof", new Vector2(12f, -4.5f), new Vector2(8f, 1f), new Color32(0x5A, 0x4F, 0x6B, 0xFF)); // top y = -4, x 8..16

        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerAsset);
        player.transform.position = new Vector3(-5f, -5f, 0f);
        var fso = new SerializedObject(follow);
        fso.FindProperty("target").objectReferenceValue = player.transform;
        fso.FindProperty("useBounds").boolValue = false;
        fso.ApplyModifiedProperties();

        // 1. Melee citizen (unchanged behavior; activation range only).
        Enemy("Citizen", new Vector2(2f, -5f), new Color32(0xFF, 0x8C, 0x42, 0xFF), 2, 5, new Color(1f, 0.1647f, 0.1647f),
            so => { so.FindProperty("groundMask").intValue = 1 << GroundLayer; so.FindProperty("activationRange").floatValue = 12f; });

        // 2. Ranged: gold 1, one telegraphed shot, keeps distance.
        Enemy("Ranged", new Vector2(24f, -5f), new Color32(0xE8, 0xD0, 0x5A, 0xFF), 1, 1, new Color(1f, 0.788f, 0.235f),
            so =>
            {
                so.FindProperty("groundMask").intValue = 1 << GroundLayer;
                so.FindProperty("activationRange").floatValue = 14f;
                so.FindProperty("ranged").boolValue = true;
                so.FindProperty("attackRangeX").floatValue = 9f;
                so.FindProperty("lastStrikeDelay").floatValue = 0.5f;
            });

        // 3. Strong (android placeholder): 1-7, last strike +0.5s with flame, number 7 -> last strike unparryable.
        Enemy("Strong", new Vector2(-18f, -5f), new Color32(0x9A, 0xA7, 0xB5, 0xFF), 1, 7, new Color(1f, 0.1647f, 0.1647f),
            so =>
            {
                so.FindProperty("groundMask").intValue = 1 << GroundLayer;
                so.FindProperty("activationRange").floatValue = 14f;
                so.FindProperty("lastStrikeDelay").floatValue = 0.5f;
                so.FindProperty("unparryableNumber").intValue = 7;
                so.FindProperty("maxHp").intValue = 5;
            });

        // 4. Citizen on the roof: chases the player off the edge when spotted.
        Enemy("RoofCitizen", new Vector2(10f, -3f), new Color32(0xFF, 0x8C, 0x42, 0xFF), 2, 3, new Color(1f, 0.1647f, 0.1647f),
            so => { so.FindProperty("groundMask").intValue = 1 << GroundLayer; so.FindProperty("activationRange").floatValue = 8f; });

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[EnemyVariantsTestSetup] Built " + ScenePath);
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

    static void Block(string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = Square(name, color, size, 0);
        go.transform.position = pos;
        go.layer = GroundLayer;
        go.AddComponent<BoxCollider2D>().size = square.bounds.size;
    }

    static void Enemy(string name, Vector2 pos, Color visualColor, int min, int max, Color numColor, System.Action<SerializedObject> tune)
    {
        var root = new GameObject(name) { layer = EnemyLayer };
        root.transform.position = pos;
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        root.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 2f);

        var number = root.AddComponent<EnemyNumber>();
        var enemy = root.AddComponent<TestEnemy>();

        var visual = Square("Visual", visualColor, new Vector2(1f, 2f), 10);
        visual.transform.SetParent(root.transform, false);
        visual.layer = EnemyLayer;

        var numGo = new GameObject("Number") { layer = EnemyLayer };
        numGo.transform.SetParent(root.transform, false);
        numGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var tmp = numGo.AddComponent<TextMeshPro>();
        tmp.text = max.ToString();
        tmp.fontSize = 6f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 20;

        var so = new SerializedObject(number);
        so.FindProperty("label").objectReferenceValue = tmp;
        so.FindProperty("minValue").intValue = min;
        so.FindProperty("maxValue").intValue = max;
        so.FindProperty("numberColor").colorValue = numColor;
        so.ApplyModifiedProperties();

        so = new SerializedObject(enemy);
        so.FindProperty("playerMask").intValue = 1 << PlayerLayer;
        so.FindProperty("visual").objectReferenceValue = visual.GetComponent<SpriteRenderer>();
        tune(so);
        so.ApplyModifiedProperties();
    }
}
