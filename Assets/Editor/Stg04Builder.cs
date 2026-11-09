using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build STG-04 -> Assets/Scenes/STG04.unity. Idempotent. Data: docs/design/LEVEL_STG04.md "세로 맵" (tables below = that doc).
// DECISIONS 15 vertical map: each segment = factory floor (old layout, x 0~45) -> conveyor shaft (stacked one-way ledges) -> press/tower roof
// (checkpoint) -> high run -> exit on top. SEG-A ends at a sniper tower with the gate, SEG-B on a gantry run with the spawner and the goal.
public static class Stg04Builder
{
    const string Tag = "Stg04Builder";
    const string ScenePath = "Assets/Scenes/STG04.unity";
    const int A = 0, B = 1;
    const float W = WideWidth; // 75.9375 (DECISIONS 12-C)
    const float H = SegHeight * 1.5f; // 25.3125: 1.5 screens, the camera follows y inside the segment
    // Spill lights, px on the background images (LIGHTING_DESIGN 3.1). A: hazard plates of the two robot arms (vermilion) + the scanner screen (teal).
    // B: work lamps on the two ceiling cranes (warm) + the red gate (vermilion).
    static readonly Lamp[] LampsA = { new Lamp(325f, 480f, LampRed, 4f), new Lamp(1090f, 480f, LampRed, 4f), new Lamp(725f, 430f, LampTeal, 5f, 0.15f) };
    static readonly Lamp[] LampsB = { new Lamp(425f, 200f, LampWarm, 5f), new Lamp(995f, 200f, LampWarm, 5f), new Lamp(1440f, 420f, LampRed, 5f) };

    // Segments at world x 0 and 110 (>= 101.25 apart: the mirrored background fill reaches 25.3u left of each segment).
    static readonly float[] SegX = { 0f, 110f };
    static readonly string[] SegId = { "SEG-A", "SEG-B" };
    static readonly Color[] SegColor = { new Color32(0x23, 0x26, 0x2B, 0xFF), new Color32(0x2E, 0x2A, 0x26, 0xFF) };
    // Plain fill above the 16.875u-high images (no new art): average top-row colour of each image.
    static readonly Color[] CeilColor = { new Color32(0x1C, 0x2F, 0x5E, 0xFF), new Color32(0x20, 0x32, 0x6A, 0xFF) };

    // Colliders (all drawn as placeholder squares). OneWay y2 = top surface. Jumps: dy <= 1.5 (StepUp), gaps <= 3u.
    static readonly Box[] Boxes = new[]
    {
        // ---- SEG-A floor (x 0~45, y 1.0): old layout (group + catwalk, first android, board 7 for hacking)
        new Box("GROUND-A", A, Kind.Solid, 0f, W, 0f, 1.0f),
        new Box("CRATE-A1", A, Kind.OneWay, 7f,  9f,  2.05f, 2.3f),
        new Box("CW-A1",    A, Kind.OneWay, 10f, 22f, 3.35f, 3.6f),
        new Box("CRATE-A2", A, Kind.OneWay, 24f, 26f, 2.05f, 2.3f),
        new Box("CW-A2",    A, Kind.OneWay, 28f, 40f, 3.35f, 3.6f),
        // ---- SEG-A climb (x 46~56): stacked conveyor ledges 2.5 L .. 10.0 R, flush with the press roof
        Building("PRESS-A", A, 56f, 60f, 10.0f),                   // checkpoint CP-A1 on top; blocks the floor route
        // ---- SEG-A high run (x 60~72, y 10.0) -> crate stack -> sniper tower with the gate on top
        Beam("HRUN-A", A, 60f, 72f, 10.0f),
        Ledge("STACK-A", A, 69.5f, 71.5f, 11.5f),                  // right of the ranged bug's back-off reach (its shot is at feet +1.05..1.55)
        Building("TOWER-A", A, 72f, W, 13.0f),
    }
    .Concat(Zigzag("CONV-A", A, 46f, 56f, 2.5f, 6))                // 2.5 L, 4.0 R, 5.5 L, 7.0 R, 8.5 L, 10.0 R
    .Concat(new[]
    {
        // ---- SEG-B floor (x 0~45, y 1.0): old layout, board 0 (people freeze, androids move)
        new Box("GROUND-B", B, Kind.Solid, 0f, W, 0f, 1.0f),
        new Box("CRATE-B1", B, Kind.OneWay, 17f, 19f, 2.05f, 2.3f),
        new Box("CW-B1",    B, Kind.OneWay, 20f, 30f, 3.35f, 3.6f),
        // ---- SEG-B climb (x 46~56): inspection lift ledges 2.5 R .. 11.5 R, flush with the tower roof
        Building("TOWER-B", B, 56f, 62f, 11.5f),                   // checkpoint CP-B1 on top
        // ---- SEG-B high run (x 62~76, y 11.5) + gantry for the sniper; goal at the right end
        Beam("HRUN-B", B, 62f, W, 11.5f),
        Ledge("GANTRY-B", B, 69f, 72f, 13.0f),
    })
    .Concat(Zigzag("LIFT-B", B, 46f, 56f, 2.5f, 7, StepUp, false)) // 2.5 R, 4.0 L, 5.5 R, 7.0 L, 8.5 R, 10.0 L, 11.5 R
    .ToArray();

    static readonly Pt PlayerStart = new Pt("P-START", A, 3f, 1.0f);
    static readonly Pt Arrival = new Pt("P-B", B, 2f, 1.0f);
    static readonly Pt[] Checkpoints =
    {
        new Pt("CP-A0", A, 3f, 1.0f),
        new Pt("CP-A1", A, 56.5f, 10.0f),  // top of the conveyor shaft
        new Pt("CP-B0", B, 2f, 1.0f),
        new Pt("CP-B1", B, 59f, 11.5f),  // top of the lift shaft
    };

    // Androids (DECISIONS 15): frontal hits blocked, one hit from behind (Shift counter) or after the overload (parry the last strike).
    // Ranged stand at the player's height on the same surface (shot is horizontal). Snipers on high perches (Kind.Sniper, gold 1).
    static readonly Foe[] Foes =
    {
        // SEG-A floor
        new Foe("E-A1", A, 16f, 1.0f, 0, 0.7f, 8f),
        new Foe("E-A2", A, 19f, 3.6f, 0, 0.7f, 8f),                    // catwalk, above E-A1
        new Foe("E-A6", A, 21f, 1.0f, 0, 0.7f, 8f),                    // third of the E-A1/E-A2 group
        new Foe("E-A3", A, 33f, 1.0f, 0, 0.7f, 8f),
        new Foe("E-A4", A, 38f, 1.0f, 3, 0f,   8f, android: true),     // first android, fixed 3: Shift dash-behind or parry the 3rd strike (from-above kill needs TestEnemy.AndroidAboveDy 2.0 -> 1.0, see LEVEL_STG04)
        new Foe("E-A5", A, 44f, 1.0f, 1, 0f,   6f, ranged: true),      // ranged citizen at the foot of the shaft; wakes only past x 38
        // SEG-A climb + high run
        new Foe("E-A10", A, 53.5f, 7.0f, 0, 0.7f, 6f),                 // shaft ledge (right): fight it from the ledge below / above
        new Foe("E-A9", A, 63f, 10.0f, 1, 0f,  12f, ranged: true, bug: true), // ranged bug facing the shaft top (nothing above it to eat its shots)
        new Foe("E-A7", A, 65.5f, 10.0f, 0, 0.7f, 8f),
        new Foe("E-A8", A, 68f, 10.0f, 0, 0.7f, 8f),
        new Foe("S-A1", A, 73f, 13.0f, 1, 0f,  19f, sniper: true),     // sniper on the gate tower: its laser sweeps the high run
        // SEG-B floor
        new Foe("E-B1", B, 15f, 1.0f, 0, 0.8f, 8f),
        new Foe("E-B2", B, 23f, 1.0f, 4, 0f,   8f, android: true),     // android 4 (androids 3 -> 4 -> 6, no 7 outside the boss)
        new Foe("E-B3", B, 26f, 3.6f, 0, 0.8f, 8f),                    // catwalk
        new Foe("E-B4", B, 30f, 1.0f, 6, 0f,   8f, android: true),
        new Foe("E-B5", B, 34f, 1.0f, 0, 0.8f, 8f),
        new Foe("E-B6", B, 40f, 1.0f, 1, 0f,   12f, ranged: true),     // ranged citizen: freezes on 0
        // SEG-B climb + high run
        new Foe("E-B8", B, 48.5f, 7.0f, 0, 0.8f, 6f),                  // lift ledge (left)
        new Foe("E-B9", B, 63f, 11.5f, 1, 0f,  12f, ranged: true, bug: true), // ranged bug: keeps shooting on 0 while the citizens freeze
        new Foe("E-B7", B, 66f, 11.5f, 0, 0.8f, 8f),
        new Foe("S-B1", B, 70.5f, 13.0f, 1, 0f, 19f, sniper: true),    // sniper on the gantry: frozen while the board shows 0, wakes if the player hacks it away
    };

    // Breakable spawner (DECISIONS 15): one per map, in the LAST segment, watching only the climb + high-run group (E-B8, E-B9, E-B7, S-B1): watching the floor too would force a second descent (Fable review).
    static readonly Pt Spawner = new Pt("SPAWNER-B", B, 68f, 11.5f); // on the high run between E-B7 and the gantry, outside the goal trigger

    // Billboards (segment-local center, within hand reach for hacking from the floor): A shows 7, B shows 0. Zone = the whole segment.
    static readonly Vector2 BoardPosA = new Vector2(25f, 4.5f), BoardPosB = new Vector2(10f, 4.5f); // y 4.5: within BroadcastHackUi.interactRange (3u) of a grounded player (center y 2)
    const int BoardA = 7, BoardB = 0;

    static readonly Box GateA = new Box("TR-A-GATE", A, Kind.Solid, 74.3f, W, 13.0f, 17.0f); // right end of the gate tower (the sniper stands left of it)
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 72.3125f, 75.3125f, 11.5f, 15.5f); // right end of the high run

    [MenuItem("NUMBER/Build STG-04")]
    public static void Build()
    {
        if (!Begin(Tag, ScenePath)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var roots = new Transform[SegX.Length];
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i] = new GameObject(SegId[i]).transform;
            roots[i].position = new Vector3(SegX[i], 0f, 0f);
            BuildImageBackground(roots[i], i == A ? "Assets/Art/Backgrounds/bg_stg04_segA_production_line.png" : "Assets/Art/Backgrounds/bg_stg04_segB_inspection_hall.png", W, SegHeight, SegColor[i]);
            BuildSkyFill(roots[i], W, SegHeight, H, CeilColor[i]);
            foreach (var w in Walls(i == A ? "A" : "B", i, W, H)) BuildCollider(roots[i], w, true);
            AddLamps(roots[i], W, i == A ? LampsA : LampsB);
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, true);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(SegX, PlayerStart, 1f);

        var enemies = Foes.Select(f => (f, go: BuildEnemy(roots[f.seg], f))).ToList();
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);
        BuildSpawner(roots[Spawner.seg], Spawner.id, new Vector2(Spawner.x, Spawner.y),
            enemies.Where(e => e.f.seg == Spawner.seg && (e.f.y >= 11.5f || e.f.id == "E-B8")).Select(e => e.go).ToList());

        BuildBoard(roots[A], "BOARD-A", BoardPosA, BoardA, new Vector2(SegX[A], SegX[A] + W));
        BuildBoard(roots[B], "BOARD-B", BoardPosB, BoardB, new Vector2(SegX[B], SegX[B] + W));

        var arrival = new GameObject("P-B (arrival)").transform;
        arrival.SetParent(roots[Arrival.seg], false);
        arrival.localPosition = new Vector3(Arrival.x, Arrival.y, 0f);
        var gate = BuildTrigger(roots[GateA.seg], GateA).AddComponent<LevelTransition>();
        SetObj(gate, "destination", arrival);
        var goal = BuildTrigger(roots[GoalB.seg], GoalB).AddComponent<LevelTransition>();
        SetInt(goal, "mode", (int)LevelTransition.Mode.Goal);

        BuildCamera(playerGo.transform, new[] { SegRect(SegX[A], W, H), SegRect(SegX[B], W, H) }, new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud(true);
        BuildLighting(false);
        SaveScene(scene, Tag, ScenePath);
    }
}
