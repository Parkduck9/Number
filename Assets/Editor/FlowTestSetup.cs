using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Menu NUMBER/Build Flow Test Scene -> Assets/Scenes/Test_Flow.unity: a stop button (E) with both ending placeholders,
// plus a stage goal on the right. Never opens a dialog: if a scene has unsaved changes it refuses and logs.
public static class FlowTestSetup
{
    const string ScenePath = "Assets/Scenes/Test_Flow.unity";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    const string Ending01 = "Assets/Data/Dialogue/ending_01_placeholder.txt";
    const string Ending02 = "Assets/Data/Dialogue/ending_02_placeholder.txt";

    [MenuItem("NUMBER/Build Flow Test Scene")]
    public static void Build()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("Flow test: a scene has unsaved changes. Save it first, then run the menu again."); return; }

        var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        if (square == null || player == null) { Debug.LogError("Flow test: missing Square sprite or Player prefab (build the prototype scene first)."); return; }
        var font = DialogueSetup.EnsureFont();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f;
        cam.backgroundColor = new Color32(0x0B, 0x0F, 0x1E, 0xFF);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<UniversalAdditionalCameraData>();
        new GameObject("Global Light 2D").AddComponent<Light2D>().lightType = Light2D.LightType.Global;

        var ground = Block("Ground", square, new Color32(0x3A, 0x3F, 0x4B, 0xFF), new Vector3(0f, -7f, 0f), new Vector2(60f, 2f), 0);
        ground.AddComponent<BoxCollider2D>();
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) ground.layer = groundLayer;
        else Debug.LogWarning("Layer 'Ground' missing: build the prototype scene once first.");

        var p = (GameObject)PrefabUtility.InstantiatePrefab(player);
        p.transform.position = new Vector3(-5f, -5f, 0f);

        // Stop button: a red block you walk up to
        var button = Block("StopButton", square, new Color32(0xFF, 0x2A, 0x2A, 0xFF), new Vector3(4f, -5.5f, 0f), new Vector2(1.5f, 1f), 1);
        var sb = button.AddComponent<StopButton>();
        var so = new SerializedObject(sb);
        so.FindProperty("ending01").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Ending01);
        so.FindProperty("ending02").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Ending02);
        so.FindProperty("font").objectReferenceValue = font;
        so.ApplyModifiedProperties();

        // Stage goal: only logs here, since Test_Flow is not in the stage list
        var goal = Block("StageGoal", square, new Color32(0x4C, 0xE0, 0xFF, 0x60), new Vector3(10f, -4f, 0f), new Vector2(1f, 4f), 1);
        goal.AddComponent<BoxCollider2D>();
        goal.AddComponent<StageGoal>();

        // DialoguePlayer (talk mode for the endings)
        var dp = new GameObject("DialoguePlayer").AddComponent<DialoguePlayer>();
        var dso = new SerializedObject(dp);
        dso.FindProperty("speakers").objectReferenceValue = DialogueSetup.EnsureSpeakers();
        dso.FindProperty("font").objectReferenceValue = font;
        dso.ApplyModifiedProperties();

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
}
