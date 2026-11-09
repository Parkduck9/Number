using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build STG-01 -> Assets/Scenes/STG01.unity. Idempotent: regenerates the scene each run.
// Layout: docs/design/LEVEL_STG01.md "세로 맵 (DECISIONS 15)". Tweak the tables below, then re-run the menu.
// DECISIONS 15 vertical map: street -> climb (shaft) -> high roof run -> climb -> top door / SEG-B: street -> shaft -> high roof -> goal.
// Shared pieces (Building / Ledge / Beam / Zigzag / sky fill / spawner) live in StageBuildKit so STG-02 / STG-04 can reuse them.
public static class Stg01Builder
{
    const string Tag = "Stg01Builder";
    const string ScenePath = "Assets/Scenes/STG01.unity";
    const string PlaceholderDir = "Assets/Art/Placeholder";

    // ------------------------------------------------------------------ DATA (segment-local coordinates, units; y of a Pt / Foe = feet)

    const float W = WideWidth;                    // 75.9375 (DECISIONS 12-C)
    static readonly float[] SegX = { 0f, 110f };  // >= 101.25 apart: the mirrored background fill reaches 25.3u left of a segment
    static readonly float[] SegH = { TallHeight, SegHeight * 1.5f }; // A climbs to y ~26, B to y ~17
    const int A = 0, B = 1;

    // SEG-A background: concept image (810x270 @ PPU 16 = 50.625 x 16.875u), right-aligned + mirrored fill; plain sky above it.
    // SEG-B: no background yet (the 13_style_frame_v2 concept had a fake HUD drawn in), the camera colour shows.
    const string SegASrc = "docs/design/concept/14_bg_stg01_pixel_native.png", SegADst = "PLACEHOLDER_stg01_segA_bg.png";
    static readonly Color SkyA = new Color32(0x2A, 0x2A, 0x55, 0xFF); // the image's top-row sky

    static readonly Box[] Boxes = new[]
    {
        // ---- SEG-A street (x 0~34): tutorial (unchanged roles)
        new Box("GROUND-A", A, Kind.Solid, 0f, W, 0f, 2.25f),      // matches the street in the art (invisible)
        // Low solid step for the first ranged 1: 0.6u above the floor so its horizontal shot still hits a standing player.
        new Box("LEDGE-A1", A, Kind.Solid, 15f, 21f, 2.25f, 2.85f),
        // ---- climb 1 (x 34~50): two roofs, then a zigzag shaft (5 ledges, +1.5 each) up to the tower roof 14.25
        Building("BLD-A1", A, 34f, 38f, 3.75f),
        Building("BLD-A2", A, 38f, 46f, 5.25f),
        Building("TOWER-A1", A, 46f, 50f, 14.25f),
        // ---- high roof run (x 50~66, y 14.25): solid beam (no falling through), raised ridge for attacks from below
        Beam("ROOF-A1", A, 50f, 66f, 14.25f),
        Ledge("RIDGE-A1", A, 55f, 59f, 15.75f),
        // ---- climb 2 (x 66~76): tower block, a second shaft, then a top walkway back to the left with the door
        Building("TOWER-A2", A, 66f, W, 15.75f),
        Beam("ROOF-A2", A, 60f, 66f, 23.25f),      // ends at the shaft: nothing solid above SH-A2 (a beam there made the climb a bonk-lock)
    }
    .Concat(Zigzag("SH-A1", A, 38f, 46f, 6.75f, 5, StepUp, false))   // 6.75 R, 8.25 L, 9.75 R, 11.25 L, 12.75 R -> tower 14.25
    .Concat(Zigzag("SH-A2", A, 66f, W, 17.25f, 4, StepUp, false))    // 17.25 R, 18.75 L, 20.25 R, 21.75 L (flush with ROOF-A2 at x 66) -> 23.25
    .Concat(Walls("A", A, W, TallHeight))
    .Concat(new[]
    {
        // ---- SEG-B street (x 0~41): the old opening of SEG-B (awnings, roof, pincer + ranged)
        new Box("GROUND-B",  B, Kind.Solid, 0f,    W,     0f,    1.0f),
        new Box("BARREL-B",  B, Kind.OneWay, 10.0f, 10.9f, 2.2f,  2.45f),
        new Box("CRATE-B",   B, Kind.OneWay, 10.0f, 11.0f, 3.2f,  3.45f),
        new Box("AW-B1",     B, Kind.OneWay, 0f,    7.3f,  4.95f, 5.2f),
        new Box("AW-B2",     B, Kind.OneWay, 10.8f, 16.6f, 4.55f, 4.8f),
        new Box("WALLTOP-B", B, Kind.OneWay, 0f,    13.0f, 5.75f, 6.0f),
        new Box("CRATE-B3",  B, Kind.OneWay, 17.2f, 19.0f, 2.05f, 2.3f),
        new Box("AW-B3",     B, Kind.OneWay, 19.3f, 26.6f, 4.55f, 4.8f),
        new Box("ROOF-B",    B, Kind.OneWay, 18.4f, 40.8f, 7.25f, 7.5f),
        Ledge("BRIDGE-B", B, 41.5f, 45f, 7.5f),                       // roof runners step on toward the shaft
        // ---- climb (x 46~62): zigzag shaft from the street up to the tower roof 13.0, then the high roof to the goal
        Building("TOWER-B", B, 56f, 62f, 13.0f),
        Beam("ROOF-B2", B, 62f, W, 13.0f),
    })
    .Concat(Zigzag("SH-B", B, 46f, 56f, 2.5f, 8))                    // 2.5 L .. 13.0 R (8 ledges), last one level with the tower roof
    .Concat(Walls("B", B, W, SegHeight * 1.5f))
    .ToArray();

    static readonly Pt PlayerStart = new Pt("P-START", A, 6f, 2.25f);
    static readonly Pt Arrival = new Pt("P-B", B, 2f, 1.0f);
    static readonly Pt[] Checkpoints =
    {
        new Pt("CP-A0", A, 6f, 2.25f),
        new Pt("CP-A1", A, 48f, 14.25f),  // top of climb 1 (a fall from the roof run restarts here, not at the street)
        new Pt("CP-A2", A, 70f, 15.75f),  // foot of climb 2
        new Pt("CP-B0", B, 2f, 1.0f),
        new Pt("CP-B1", B, 58f, 13.0f),   // top of the SEG-B shaft
    };

    // Placement rule (DECISIONS "Review Top 5" #2 + 15): the only 1:1 flat fight is the tutorial melee E-A2; the rest are groups,
    // enemies on ledges above / below the player (diagonal lunge, 5u reach) and ranged shots along a roof. Ranged stand on a surface
    // the player walks on at the same height (shot is horizontal at feet + 1.3). Numbers capped at 4 (5+ only from late STG-02).
    static readonly Foe[] Foes =
    {
        new Foe("E-A1",  A, 18f, 2.85f, 1, 0f,    10f, ranged: true),       // FIRST enemy on LEDGE-A1 (wakes at player x 8): parry the shot or dash past
        new Foe("E-A2",  A, 27f, 2.25f, 2, 0f,    6f),                      // tutorial melee 1:1, fixed 2
        new Foe("E-A3",  A, 30f, 2.25f, 3, 0f,    9f),                      // fixed 3, wakes with E-A2 -> 2+3 pair
        new Foe("E-A4",  A, 33f, 2.25f, 1, 0f,    8f, ranged: true),        // ranged cover at the foot of the climb
        new Foe("E-A5",  A, 44f, 9.75f, 0, 0.15f, 6f, cap: 4),              // shaft ledge (right): wakes early and may drop onto BLD-A2 (fight it there or from below)
        new Foe("E-A6",  A, 40f, 11.25f, 0, 0.15f, 6f, cap: 4),             // shaft ledge (left), one step higher
        new Foe("E-A7",  A, 55f, 14.25f, 0, 0.15f, 8f, cap: 4),             // roof run group: ground + ridge + ground + ranged
        new Foe("E-A8",  A, 57f, 15.75f, 0, 0.15f, 8f, cap: 4),
        new Foe("E-A9",  A, 61f, 14.25f, 0, 0.15f, 8f, cap: 4),
        new Foe("E-A10", A, 65f, 14.25f, 1, 0f,    12f, ranged: true),
        new Foe("E-A11", A, 73f, 17.25f, 0, 0.2f,  6f, cap: 4),             // climb 2 ledge (right)
        // Sniper (DECISIONS 15) on TOWER-A2 (left of the 17.25 right ledge, so its muzzle line is clear): the laser sweeps the roof run.
        new Foe("S-A1",  A, 68f, 15.75f, 1, 0f,    19f, sniper: true),
        new Foe("E-B3",  B, 22f, 7.5f,  0, 0.3f,  8f, cap: 4),              // on the roof (D2 roof-edge rule)
        // Sniper on the right end of ROOF-B: covers the open shaft (x 46~56) and the street below while the player climbs.
        new Foe("S-B1",  B, 39.5f, 7.5f, 1, 0f,   19f, sniper: true),
        new Foe("E-B1",  B, 30f, 1.0f,  0, 0.3f,  8f, cap: 4),              // pincer pair with E-B2 + ranged E-B4 behind
        new Foe("E-B2",  B, 33f, 1.0f,  0, 0.3f,  8f, cap: 4),
        new Foe("E-B4",  B, 38f, 1.0f,  1, 0f,    12f, ranged: true),
        new Foe("E-B5",  B, 53f, 7.0f,  0, 0.3f,  6f, cap: 4),              // shaft ledge
        new Foe("E-B6",  B, 59f, 13.0f, 0, 0.3f,  8f, cap: 4),              // high roof group
        new Foe("E-B7",  B, 65f, 13.0f, 0, 0.3f,  8f, cap: 4),
        new Foe("E-B8",  B, 68f, 13.0f, 0, 0.3f,  8f, cap: 4),
        new Foe("E-B9",  B, 74f, 13.0f, 1, 0f,    12f, ranged: true),
    };

    // Breakable spawner (DECISIONS 15): one per map, in the LAST segment, watching only that segment's enemies.
    static readonly Pt Spawner = new Pt("SPAWNER-B", B, 66.5f, 13.0f); // between E-B7 and E-B8, outside the goal trigger
    static readonly Box DoorA = new Box("TR-A-DOOR", A, Kind.Solid, 60f, 62.5f, 23.25f, 26f); // top walkway, left end
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 70f, 75f, 13.0f, 17f);
    static readonly Box HintA = new Box("TR-A-HINT", A, Kind.Solid, 7f, 8f, 2.25f, 5f);     // radio hint before E-A1 wakes
    const string HintLines = "Assets/Data/Dialogue/stg01_hint_combat.txt";

    // DECISIONS 5-B: SEG-B has one broadcast board "3" (SEG-A none); its zone is the whole segment.
    static readonly Vector2 BoardPos = new Vector2(6f, 3.5f); // near the arrival point so it reads at once

    // ------------------------------------------------------------------ BUILD

    [MenuItem("NUMBER/Build STG-01")]
    public static void Build()
    {
        if (!Begin(Tag, ScenePath)) return;
        var bgA = ImportBackground();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var roots = new Transform[SegX.Length];
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i] = new GameObject(i == A ? "SEG-A" : "SEG-B").transform;
            roots[i].position = new Vector3(SegX[i], 0f, 0f);
        }
        if (bgA != null)
        {
            BuildWideBackground(roots[A], bgA, W); // native scale, right-aligned + mirrored fill
            BuildSkyFill(roots[A], W, bgA.bounds.size.y, SegH[A], SkyA);
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, !(b.id.StartsWith("GROUND") || b.id.StartsWith("WALL")));

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(SegX, PlayerStart, 1f);

        var enemies = Foes.Select(f => (f, go: BuildEnemy(roots[f.seg], f))).ToList();
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);
        BuildSpawner(roots[Spawner.seg], Spawner.id, new Vector2(Spawner.x, Spawner.y),
            enemies.Where(e => e.f.seg == Spawner.seg).Select(e => e.go).ToList());

        var arrival = new GameObject("P-B (arrival)").transform;
        arrival.SetParent(roots[Arrival.seg], false);
        arrival.localPosition = new Vector3(Arrival.x, Arrival.y, 0f);
        SetObj(BuildTrigger(roots[DoorA.seg], DoorA).AddComponent<LevelTransition>(), "destination", arrival);
        SetInt(BuildTrigger(roots[GoalB.seg], GoalB).AddComponent<LevelTransition>(), "mode", (int)LevelTransition.Mode.Goal);

        BuildCamera(playerGo.transform, Enumerable.Range(0, SegX.Length).Select(i => SegRect(SegX[i], W, SegH[i])).ToArray(), new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud(false); // HUD only: the hack device is obtained in STG-03 (01-fixes #5)
        BuildBoard(roots[B], "BOARD-B", BoardPos, 3, new Vector2(SegX[B], SegX[B] + W));
        BuildDialoguePlayer(); // radio hint needs DialoguePlayer.Instance
        BuildHint(roots[HintA.seg]);
        BuildLighting(false); // night (no lamps: the SEG-A background is a concept placeholder)
        SaveScene(scene, Tag, ScenePath);
    }

    // Copies the concept image into Assets/Art/Placeholder (if not there yet) and applies the import settings.
    static Sprite ImportBackground()
    {
        Directory.CreateDirectory(PlaceholderDir);
        string dst = PlaceholderDir + "/" + SegADst;
        if (!File.Exists(dst))
        {
            string src = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SegASrc));
            if (!File.Exists(src)) { Todo.Add("Background missing for SEG-A: " + SegASrc); return null; }
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
        ti.spritePixelsPerUnit = 16f; // 810 px -> 50.625u
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = false;
        ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
    }

    static void BuildHint(Transform root)
    {
        var go = BuildTrigger(root, HintA);
        var lines = AssetDatabase.LoadAssetAtPath<TextAsset>(HintLines);
        if (lines == null) { Todo.Add("hint line file missing: " + HintLines); return; }
        SetObj(go.AddComponent<DialogueTrigger>(), "lines", lines);
    }

    // Scene-view aid: always show Ground-layer colliders (green = solid, cyan = one-way) so the layout can be checked over the backgrounds.
    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
    static void DrawGround(BoxCollider2D c, GizmoType t)
    {
        if (c.gameObject.layer != 6 || c.isTrigger) return; // 6 = Ground
        Gizmos.matrix = c.transform.localToWorldMatrix;
        Gizmos.color = c.usedByEffector ? Color.cyan : Color.green;
        Gizmos.DrawWireCube(c.offset, c.size);
    }
}
