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

// Menu NUMBER/Build STG-01 -> Assets/Scenes/STG01.unity. Idempotent: regenerates the scene each run.
// Data source: docs/design/LEVEL_STG01.md section 6. Tweak the tables below, then re-run the menu.
// Cards A (Checkpoint/RespawnManager), D (CameraFollow2D/CameraSetup) and E (EnemyNumber/TestEnemy fields)
// are resolved by name/property at build time, so this file compiles even if they are missing; the build log lists what was skipped.
public static class Stg01Builder
{
    const string ScenePath = "Assets/Scenes/STG01.unity";
    const string PlayerPrefab = "Assets/Prefabs/Prototype/Player.prefab";
    const string EnemyPrefab = "Assets/Prefabs/Prototype/TestEnemy.prefab";
    const string SquarePath = "Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png";
    const string PlaceholderDir = "Assets/Art/Placeholder";
    const int GroundLayer = 6, PlayerLayer = 7, EnemyLayer = 8;

    // ------------------------------------------------------------------ DATA (segment-local coordinates, units)

    const float SegWidth = 50.625f, SegHeight = 16.875f;

    // Segments: A at world x 0, B at world x 60. Background = concept image placed bottom-left, scaled to SegWidth.
    // PLACEHOLDER art: replace with the clean 1620x540 backgrounds from Codex (same path or swap the sprite in the scene).
    class Seg
    {
        public string id; public float worldX;
        public string srcImage, dstName; public float ppu; public FilterMode filter;
    }
    static readonly Seg[] Segs =
    {
        // 14 pixel-native is 810x270: PPU 16 -> exactly 50.625u wide.
        new Seg { id = "SEG-A", worldX = 0f,  srcImage = "docs/design/concept/14_bg_stg01_pixel_native.png", dstName = "PLACEHOLDER_stg01_segA_bg.png", ppu = 16f, filter = FilterMode.Point },
        // 13 is a 1600x533 hi-res frame (not pixel art): PPU 32 then scaled x1.0125; bilinear so the odd scale does not shimmer.
        new Seg { id = "SEG-B", worldX = 60f, srcImage = "docs/design/concept/13_style_frame_v2.jpg",          dstName = "PLACEHOLDER_stg01_segB_bg.jpg",  ppu = 32f, filter = FilterMode.Bilinear },
    };

    enum Kind { Solid, OneWay }
    struct Box
    {
        public string id; public int seg; public Kind kind; public float x1, x2, y1, y2;
        public Box(string id, int seg, Kind kind, float x1, float x2, float y1, float y2)
        { this.id = id; this.seg = seg; this.kind = kind; this.x1 = x1; this.x2 = x2; this.y1 = y1; this.y2 = y2; }
    }
    const int A = 0, B = 1;

    // 6.1 colliders (layer Ground, invisible). OneWay = PlatformEffector2D, y2 is the top surface.
    static readonly Box[] Boxes =
    {
        new Box("GROUND-A",  A, Kind.Solid, 0f,      50.625f, 0f,    2.25f),
        new Box("WALL-A-L",  A, Kind.Solid, -1f,     0f,      0f,    16.875f),
        new Box("WALL-A-R",  A, Kind.Solid, 50.625f, 51.625f, 0f,    16.875f),
        new Box("GROUND-B",  B, Kind.Solid, 0f,      50.625f, 0f,    1.0f),
        new Box("WALL-B-L",  B, Kind.Solid, -1f,     0f,      0f,    16.875f),
        new Box("WALL-B-R",  B, Kind.Solid, 50.625f, 51.625f, 0f,    16.875f),
        new Box("BARREL-B",  B, Kind.OneWay, 10.0f,  10.9f,   2.2f,  2.45f),
        new Box("CRATE-B",   B, Kind.OneWay, 10.0f,  11.0f,   3.2f,  3.45f), // extra crate sprite on the barrel [proposal]
        new Box("AW-B1",     B, Kind.OneWay, 0f,     7.3f,    4.95f, 5.2f),
        new Box("AW-B2",     B, Kind.OneWay, 10.8f,  16.6f,   4.55f, 4.8f),
        new Box("WALLTOP-B", B, Kind.OneWay, 0f,     13.0f,   5.75f, 6.0f),
        new Box("CRATE-B3",  B, Kind.OneWay, 17.2f,  19.0f,   2.05f, 2.3f),
        new Box("AW-B3",     B, Kind.OneWay, 19.3f,  26.6f,   4.55f, 4.8f),
        new Box("ROOF-B",    B, Kind.OneWay, 18.4f,  40.8f,   7.25f, 7.5f),
    };

    // 6.2 player start, checkpoints (touch = heal), enemies. y = feet position.
    struct Pt { public string id; public int seg; public float x, y; public Pt(string id, int seg, float x, float y) { this.id = id; this.seg = seg; this.x = x; this.y = y; } }
    static readonly Pt PlayerStart = new Pt("P-START", A, 6f, 2.25f);
    static readonly Pt Arrival = new Pt("P-B", B, 2f, 1.0f);
    static readonly Pt[] Checkpoints = { new Pt("CP-A0", A, 6f, 2.25f), new Pt("CP-B0", B, 2f, 1.0f) };

    // fixedNumber 0 = reroll by progress. detect = activation range (u). progress = reroll weight position.
    struct Foe { public string id; public int seg; public float x, y; public int fixedNumber; public float progress, detect;
        public Foe(string id, int seg, float x, float y, int fixedNumber, float progress, float detect)
        { this.id = id; this.seg = seg; this.x = x; this.y = y; this.fixedNumber = fixedNumber; this.progress = progress; this.detect = detect; } }
    static readonly Foe[] Foes =
    {
        new Foe("E-A1", A, 20f, 2.25f, 2, 0f,   8f), // D4: fixed 2
        new Foe("E-A2", A, 38f, 2.25f, 3, 0f,   8f), // D4: fixed 3, charge spot
        new Foe("E-B3", B, 22f, 7.5f,  0, 0.3f, 8f), // on the roof (D2 roof-edge rule)
        new Foe("E-B1", B, 30f, 1.0f,  0, 0.3f, 8f),
        new Foe("E-B2", B, 33f, 1.0f,  0, 0.3f, 8f),
    };

    // Trigger boxes (segment-local x1..x2, y1..y2).
    static readonly Box DoorA = new Box("TR-A-DOOR", A, Kind.Solid, 43.2f, 46.8f, 2.25f, 5f);
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 42f, 47f, 1.0f, 5f);

    // DECISIONS 5-B: SEG-B has one broadcast board (SEG-A none). Its zone is the whole segment, so the HUD "3" appears on arrival through the door.
    const int BoardSeg = B, BoardNumber = 3;
    const float BoardSize = 2.5f;
    static readonly Vector2 BoardPos = new Vector2(6f, 3.5f); // segment-local centre: feet y 1.0 + 2.5u, near the arrival point P-B (2, 1) so it can be hacked at once

    // ------------------------------------------------------------------ BUILD

    static readonly List<string> Todo = new List<string>();
    static Sprite square;

    [MenuItem("NUMBER/Build STG-01")]
    public static void Build()
    {
        // Never raise a modal (this runs via MCP too): only STG01 itself is discarded silently, any other unsaved scene aborts.
        foreach (var s in Enumerable.Range(0, EditorSceneManager.sceneCount).Select(EditorSceneManager.GetSceneAt))
            if (s.isDirty && s.path != ScenePath)
            {
                Debug.LogError($"[Stg01Builder] Scene '{s.name}' has unsaved changes. Save or discard it, then run NUMBER/Build STG-01 again.");
                return;
            }

        Todo.Clear();
        SetupLayers();
        square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab);
        if (square == null || player == null || enemy == null)
        {
            Debug.LogError("[Stg01Builder] Missing Square sprite / Player / TestEnemy prefab. Run NUMBER/Build Prototype Scene first.");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");
        var bgSprites = new Sprite[Segs.Length];
        for (int i = 0; i < Segs.Length; i++) bgSprites[i] = ImportBackground(Segs[i]);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var roots = new Transform[Segs.Length];
        for (int i = 0; i < Segs.Length; i++)
        {
            roots[i] = new GameObject(Segs[i].id).transform;
            roots[i].position = new Vector3(Segs[i].worldX, 0f, 0f);
            BuildBackground(roots[i], Segs[i], bgSprites[i]);
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(player);
        playerGo.transform.position = World(PlayerStart, 1f);

        foreach (var f in Foes) BuildEnemy(roots[f.seg], enemy, f);
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);

        var arrival = new GameObject("P-B (arrival)").transform;
        arrival.SetParent(roots[Arrival.seg], false);
        arrival.localPosition = new Vector3(Arrival.x, Arrival.y, 0f);
        var door = BuildTrigger(roots[DoorA.seg], DoorA);
        var doorLt = door.AddComponent<LevelTransition>();
        SetObj(doorLt, "destination", arrival);
        var goal = BuildTrigger(roots[GoalB.seg], GoalB);
        var goalLt = goal.AddComponent<LevelTransition>();
        SetInt(goalLt, "mode", (int)LevelTransition.Mode.Goal);

        BuildLight();
        var cam = BuildCamera(playerGo.transform, roots);
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud();
        BuildBoard(roots[BoardSeg]); // SEG-B only (5-B); the HUD (BuildHud) shows the board of the player's zone

        EditorSceneManager.SaveScene(scene, ScenePath);
        StageBuildKit.SyncBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("[Stg01Builder] Built " + ScenePath + (Todo.Count == 0 ? "" : "\nTODO / skipped:\n - " + string.Join("\n - ", Todo)));
    }

    static Vector3 World(Pt p, float footToCenter) => new Vector3(Segs[p.seg].worldX + p.x, p.y + footToCenter, 0f);

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

    // Copies the concept image into Assets/Art/Placeholder (if not there yet) and applies the import settings.
    static Sprite ImportBackground(Seg s)
    {
        Directory.CreateDirectory(PlaceholderDir);
        string dst = PlaceholderDir + "/" + s.dstName;
        if (!File.Exists(dst))
        {
            string src = Path.GetFullPath(Path.Combine(Application.dataPath, "..", s.srcImage));
            if (!File.Exists(src)) { Todo.Add("Background missing for " + s.id + ": " + s.srcImage); return null; }
            File.Copy(src, dst);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceSynchronousImport);
        }
        var ti = (TextureImporter)AssetImporter.GetAtPath(dst);
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.BottomLeft;
        ti.SetTextureSettings(st);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = s.ppu;
        ti.filterMode = s.filter;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = false;
        ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
    }

    static void BuildBackground(Transform root, Seg s, Sprite sprite)
    {
        if (sprite == null) return;
        var go = new GameObject("Background (PLACEHOLDER concept image)");
        go.transform.SetParent(root, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -100;
        float k = SegWidth / sprite.bounds.size.x; // 1.0 for A, 1.0125 for B
        go.transform.localScale = new Vector3(k, k, 1f);
    }

    static GameObject BuildCollider(Transform root, Box b)
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
        if (b.id == "CRATE-B") // the one extra visible prop: crate sprite on the barrel (placeholder square)
        {
            var vis = new GameObject("CRATE-B sprite (PLACEHOLDER)");
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = new Vector3(0f, -0.5f + 0.125f, 0f); // 1x1 crate whose top = collider top
            vis.transform.localScale = new Vector3(1f, 1f, 1f) / square.bounds.size.x;
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = new Color32(0x8A, 0x5A, 0x32, 0xFF);
            sr.sortingOrder = 5;
        }
        return go;
    }

    static GameObject BuildTrigger(Transform root, Box b)
    {
        var go = new GameObject(b.id);
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3((b.x1 + b.x2) * 0.5f, (b.y1 + b.y2) * 0.5f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(b.x2 - b.x1, b.y2 - b.y1);
        col.isTrigger = true;
        return go;
    }

    static void BuildCheckpoint(Transform root, Pt c)
    {
        var go = new GameObject(c.id);
        go.transform.SetParent(root, false);
        // Pivot at player-center height (feet + 1): Checkpoint records transform.position as the respawn point.
        go.transform.localPosition = new Vector3(c.x, c.y + 1f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.5f, 3f);
        col.offset = new Vector2(0f, 0.5f); // trigger spans feet .. feet + 3
        col.isTrigger = true;

        var marker = new GameObject("Marker (placeholder)");
        marker.transform.SetParent(go.transform, false);
        marker.transform.localPosition = new Vector3(0f, -0.5f, 0f); // 1u post standing on the feet line
        marker.transform.localScale = new Vector3(0.3f, 1f, 1f) / square.bounds.size.x;
        var sr = marker.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.sortingOrder = 3;

        var cp = AddByName("Checkpoint", go, "Card A Checkpoint (" + c.id + ")");
        if (cp != null) SetObj(cp, "marker", sr);
    }

    static void BuildEnemy(Transform root, GameObject prefab, Foe f)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = f.id;
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(f.x, f.y + 1f, 0f);

        var number = go.GetComponent<EnemyNumber>();
        var te = go.GetComponent<TestEnemy>();
        if (f.progress > 0f) SetFloat(number, "progress", f.progress);

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
    }

    // Face-portrait health HUD (Card A, top-left) + broadcast display UI (Card B, builds its own UI at runtime).
    static void BuildHud()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Mulmaru/Mulmaru SDF.asset");
        if (font == null) Todo.Add("Mulmaru SDF font missing - HUD uses the default TMP font (run the dialogue setup menu first)");

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

    static void BuildBoard(Transform root)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Mulmaru/Mulmaru SDF.asset");
        var go = new GameObject("BroadcastBoard");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(BoardPos.x, BoardPos.y, 0f);
        go.transform.localScale = new Vector3(BoardSize, BoardSize, 1f) / square.bounds.size.x;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = new Color32(0x1A, 0x1F, 0x33, 0xFF);
        sr.sortingOrder = 1;

        var textGo = new GameObject("Number");
        textGo.transform.SetParent(go.transform, false);
        textGo.transform.localScale = new Vector3(1f / go.transform.localScale.x, 1f / go.transform.localScale.y, 1f); // undo the board's scale
        var tmp = textGo.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.text = BoardNumber.ToString();
        tmp.fontSize = 18f * BoardSize / 3f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color32(0xFF, 0xD2, 0x4A, 0xFF);
        tmp.sortingOrder = 5;

        var board = go.AddComponent<BroadcastBoard>();
        SetInt(board, "aiNumber", BoardNumber);
        SetObj(board, "label", tmp);
        Apply(board, "zoneX", p => p.vector2Value = new Vector2(Segs[BoardSeg].worldX, Segs[BoardSeg].worldX + SegWidth));
    }

    static void BuildLight()
    {
        var go = new GameObject("Global Light 2D");
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Global;
        l.intensity = 1f;
    }

    static GameObject BuildCamera(Transform player, Transform[] roots)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = go.AddComponent<Camera>();
        go.AddComponent<AudioListener>();
        go.AddComponent<UniversalAdditionalCameraData>();
        cam.orthographic = true;
        cam.orthographicSize = 8.4375f; // 540 / 2 / PPU 32
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x0B, 0x0F, 0x1E, 0xFF);
        go.transform.position = new Vector3(Segs[0].worldX + 15f, SegHeight * 0.5f, -10f);

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
        arr.arraySize = Segs.Length;
        for (int i = 0; i < Segs.Length; i++)
            arr.GetArrayElementAtIndex(i).rectValue = new Rect(Segs[i].worldX, 0f, SegWidth, SegHeight);
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
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

    static Component AddByName(string typeName, GameObject go, string what)
    {
        var t = FindType(typeName);
        if (t != null) return go.AddComponent(t);
        Todo.Add(what + " not found (type " + typeName + ")");
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
        if (p == null) { Debug.LogWarning($"[Stg01Builder] {o.GetType().Name} has no field '{prop}'"); return; }
        set(p);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Scene-view aid: always show Ground-layer colliders (green = solid, cyan = one-way) so the layout can be checked over the backgrounds.
    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
    static void DrawGround(BoxCollider2D c, GizmoType t)
    {
        if (c.gameObject.layer != GroundLayer || c.isTrigger) return;
        Gizmos.matrix = c.transform.localToWorldMatrix;
        Gizmos.color = c.usedByEffector ? Color.cyan : Color.green;
        Gizmos.DrawWireCube(c.offset, c.size);
    }
}
