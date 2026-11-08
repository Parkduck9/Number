using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Menu NUMBER/Lighting/Build Test Scene -> Assets/Scenes/Test_Lighting.unity (own scene).
// Placeholders only: Square sprites + code-generated pixel neon tubes. Prototype prefabs are used read-only.
public static class LightingTestSetup
{
    const string ScenePath = "Assets/Scenes/Test_Lighting.unity";
    const string TexDir = "Assets/Scenes/Test_Lighting_Assets";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    const string EnemyPrefab = "Assets/Prefabs/Prototype/TestEnemy.prefab";

    [MenuItem("NUMBER/Lighting/Build Test Scene")]
    public static void Build()
    {
        var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        if (square == null || player == null || enemy == null) { Debug.LogError("Lighting test: missing Square sprite or Prototype prefabs."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        LightingSetup.EnsureSortingLayers();
        LightingSetup.EnsureProfiles();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x0B, 0x0F, 0x1E, 0xFF);
        camGo.transform.position = new Vector3(2f, 0f, -10f);

        LightingSetup.BuildInScene(cam);

        Block("Backdrop", square, new Color32(0x4A, 0x52, 0x7A, 0xFF), new Vector3(0f, 1.5f, 0f), new Vector2(60f, 17f), -5, "World");
        var ground = Block("Ground", square, new Color32(0x5A, 0x60, 0x70, 0xFF), new Vector3(0f, -7f, 0f), new Vector2(60f, 2f), 0, "World");
        ground.AddComponent<BoxCollider2D>();
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) ground.layer = groundLayer;

        var signs = new GameObject("Signs").transform;
        var red = MakeNeon("neon_red", new Color32(0xFF, 0x4A, 0x3C, 0xFF), 0);
        var teal = MakeNeon("neon_teal", new Color32(0x2E, 0xC4, 0xA8, 0xFF), 1);
        var gold = MakeNeon("neon_gold", new Color32(0xFF, 0xB4, 0x3C, 0xFF), 2);
        LightingSetup.BuildNeon("Neon_Red", signs, new Vector3(-8f, -2f, 0f), red, new Color32(0xE0, 0x60, 0x4A, 0xFF), 5f, 0f, 5, "World");
        LightingSetup.BuildNeon("Neon_Teal", signs, new Vector3(2f, 0f, 0f), teal, new Color32(0x3A, 0xA8, 0x95, 0xFF), 5f, 0.3f, 5, "World");
        LightingSetup.BuildNeon("Neon_Gold", signs, new Vector3(11f, -1f, 0f), gold, new Color32(0xD9, 0xA6, 0x4A, 0xFF), 6f, 0f, 5, "World");
        LightingSetup.BuildFloorLight(signs, new Vector3(11f, -5.7f, 0f), new Color32(0xD9, 0xA6, 0x4A, 0xFF));

        Spawn(player, new Vector3(-6f, -5f, 0f));
        Spawn(enemy, new Vector3(3f, -5f, 0f));

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Built " + ScenePath + " (N key toggles day/night in Play; menu NUMBER/Lighting/Toggle Day-Night in edit mode)");
    }

    static GameObject Block(string name, Sprite sprite, Color color, Vector3 pos, Vector2 size, int order, string layer)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        return go;
    }

    static void Spawn(GameObject prefab, Vector3 pos)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = pos;
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingLayerName = "Characters"; // scene override only
    }

    // Pixel neon tube: 2px colour outline + 1px bright seam inside, 2-step dithered halo outside. Alpha 0 or 255 only.
    static Sprite MakeNeon(string name, Color32 col, int variant)
    {
        const int w = 96, h = 40, m = 12; // total size, halo margin
        var px = new Color32[w * h];
        Color32 seam = new Color32(0xFF, 0xF8, 0xE8, 0xFF);
        Color32 Dim(float f) => new Color32((byte)(col.r * f), (byte)(col.g * f), (byte)(col.b * f), 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Max(m - x, x - (w - 1 - m), 0), dy = Mathf.Max(m - y, y - (h - 1 - m), 0);
                int d = Mathf.Max(dx, dy); // distance outside body (Chebyshev)
                int ix = Mathf.Min(x - m, w - 1 - m - x), iy = Mathf.Min(y - m, h - 1 - m - y);
                int inset = Mathf.Min(ix, iy); // distance inside body edge
                Color32 c = new Color32(0, 0, 0, 0);
                if (d == 0)
                {
                    bool bar = (variant == 1 && Mathf.Abs(y - h / 2) <= 1) ||
                               (variant == 2 && (Mathf.Abs(x - w / 3) <= 1 || Mathf.Abs(x - 2 * w / 3) <= 1));
                    if (inset == 2 || (bar && inset > 2)) c = seam;
                    else if (inset < 2 || bar) c = col;
                }
                else if (d <= 4) { if (((x + y) & 1) == 0) c = Dim(0.5f); }
                else if (d <= 9) { if ((x & 1) == 0 && (y & 1) == 0) c = Dim(0.3f); }
                px[y * w + x] = c;
            }
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        Directory.CreateDirectory(TexDir);
        string path = TexDir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = 32;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
