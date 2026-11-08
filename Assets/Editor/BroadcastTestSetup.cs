using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Menu NUMBER/Build Broadcast Test Scene -> Assets/Scenes/Test_Broadcast.unity (own scene; does not touch Prototype.unity).
// Needs the Prototype player prefab (run NUMBER/Build Prototype Scene once first) and the Mulmaru TMP font.
public static class BroadcastTestSetup
{
    const string ScenePath = "Assets/Scenes/Test_Broadcast.unity";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    const string FontPath = "Assets/Fonts/Mulmaru/Mulmaru SDF.asset";

    [MenuItem("NUMBER/Build Broadcast Test Scene")]
    public static void Build()
    {
        var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (square == null || player == null) { Debug.LogError("Broadcast test: missing Square sprite or Player prefab (build the prototype scene first)."); return; }
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) // never raise a modal dialog
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("Broadcast test: a scene has unsaved changes. Save it first, then run the menu again."); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f;
        cam.backgroundColor = new Color32(0x0B, 0x0F, 0x1E, 0xFF);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<UniversalAdditionalCameraData>();

        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;

        var ground = Block("Ground", square, new Color32(0x3A, 0x3F, 0x4B, 0xFF), new Vector3(0f, -7f, 0f), new Vector2(60f, 2f), 0);
        ground.AddComponent<BoxCollider2D>();
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) ground.layer = groundLayer;
        else Debug.LogWarning("Layer 'Ground' missing: build the prototype scene once first.");

        var p = (GameObject)PrefabUtility.InstantiatePrefab(player);
        p.transform.position = new Vector3(-5f, -5f, 0f);

        BuildBoard("Board A", square, font, new Vector3(5f, -4.5f, 0f), 3);
        BuildBoard("Board B", square, font, new Vector3(-11f, -4.5f, 0f), 7);

        var ui = new GameObject("BroadcastHackUi").AddComponent<BroadcastHackUi>();
        var so = new SerializedObject(ui);
        so.FindProperty("font").objectReferenceValue = font;
        so.ApplyModifiedProperties();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Built " + ScenePath);
    }

    static GameObject Block(string name, Sprite sprite, Color color, Vector3 pos, Vector2 size, int order)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    static void BuildBoard(string name, Sprite square, TMP_FontAsset font, Vector3 pos, int aiNumber)
    {
        var board = Block(name, square, new Color32(0x1A, 0x1F, 0x33, 0xFF), pos, new Vector2(3f, 3f), 1);

        var textGo = new GameObject("Number");
        textGo.transform.SetParent(board.transform, false);
        textGo.transform.localScale = new Vector3(1f / 3f, 1f / 3f, 1f); // undo the board's scale
        var tmp = textGo.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.text = aiNumber.ToString();
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color32(0xFF, 0xD2, 0x4A, 0xFF);
        tmp.sortingOrder = 5;

        var b = board.AddComponent<BroadcastBoard>();
        var so = new SerializedObject(b);
        so.FindProperty("aiNumber").intValue = aiNumber;
        so.FindProperty("label").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();
    }
}
