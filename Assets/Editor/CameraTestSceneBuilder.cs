using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Menu NUMBER/Build Camera Test Scene -> Assets/Scenes/Test_Camera.unity (not added to Build Settings).
public static class CameraTestSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Test_Camera.unity";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";

    [MenuItem("NUMBER/Build Camera Test Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        if (square == null) { Debug.LogError("Square sprite not found: " + SquarePath); return; }

        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Level: x -20..40, y -6..18 (matches CameraFollow2D default bounds).
        Block(square, "Ground", new Vector2(10f, -3.5f), new Vector2(60f, 1f), new Color(0.25f, 0.25f, 0.3f), 0);
        Block(square, "Wall L", new Vector2(-19.5f, 6f), new Vector2(1f, 24f), new Color(0.35f, 0.25f, 0.25f), 0);
        Block(square, "Wall R", new Vector2(39.5f, 6f), new Vector2(1f, 24f), new Color(0.35f, 0.25f, 0.25f), 0);
        for (int i = 0; i < 11; i++) // thin marks every 5 units to read scrolling
            Block(square, "Mark " + i, new Vector2(-15f + i * 5f, -2.5f), new Vector2(0.25f, 1f), Color.white, 1);

        var far = new GameObject("Parallax Far (0.8)").AddComponent<ParallaxLayer>();
        SetFloat(far, "factor", 0.8f);
        for (int i = 0; i < 6; i++) Block(square, "Far " + i, new Vector2(-10f + i * 10f, 4f), new Vector2(3f, 3f), new Color(0.15f, 0.2f, 0.35f), -2).transform.SetParent(far.transform);
        var mid = new GameObject("Parallax Mid (0.4)").AddComponent<ParallaxLayer>();
        SetFloat(mid, "factor", 0.4f);
        for (int i = 0; i < 8; i++) Block(square, "Mid " + i, new Vector2(-12f + i * 7f, 0f), new Vector2(2f, 4f), new Color(0.2f, 0.3f, 0.3f), -1).transform.SetParent(mid.transform);

        // 64px hero = 2 world units.
        var player = Block(square, "Player (arrows / WASD)", new Vector2(0f, -2f), new Vector2(1f, 2f), new Color(0.9f, 0.7f, 0.2f), 2);
        player.AddComponent<CameraTestMover>();

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f; // 540 / 2 / PPU 32 (Pixel Perfect Camera overrides it anyway)
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.07f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<UniversalAdditionalCameraData>();
        CameraSetup.Configure(camGo);
        var follow = camGo.AddComponent<CameraFollow2D>();
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[CameraTestSceneBuilder] Built " + ScenePath);
    }

    static GameObject Block(Sprite sprite, string name, Vector2 pos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = size;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
        return go;
    }

    static void SetFloat(Object o, string prop, float v)
    {
        var so = new SerializedObject(o);
        so.FindProperty(prop).floatValue = v;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}

// Shared by the test scene builder; other scene builders can call it to get the same camera setup.
public static class CameraSetup
{
    // Reference 960x540, PPU 32 -> exactly x2 at 1920x1080 (orthographic size 8.4375).
    // Upscale Render Texture + Stretch Fill: renders at 960x540, then point-upscales to fill the window
    // (integer at 1920x1080; slightly uneven pixels at other sizes, but no black bars on 16:9).
    public static PixelPerfectCamera Configure(GameObject cameraGo)
    {
        var ppc = cameraGo.GetComponent<PixelPerfectCamera>();
        if (ppc == null) ppc = cameraGo.AddComponent<PixelPerfectCamera>();
        ppc.assetsPPU = 32;
        ppc.refResolutionX = 960;
        ppc.refResolutionY = 540;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
        return ppc;
    }
}
