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

    // Segments at world x 0 and 110.
    static readonly float[] SegX = { 0f, 110f };
    static readonly string[] SegId = { "SEG-A", "SEG-B" };

    // Low tier = the drawn surfaces (DECISIONS 41): the building row is world-fixed and PartsBackdropKit puts one-way floors on its roofs,
    // on the fence top between buildings (3.69: the old catwalks CW-A1/A2/B1) and on the machine / crate lids (crate lip 2.06, crate 2.91,
    // server 3.38, scanner 3.66, robot arm 3.56/3.75). Roofs: watchtower 6.31, factory hall 7.13. Placement (part, centre x) on the 1/32 grid.
    static readonly (string part, float x)[][] Row =
    {
        // SEG-A: fence runs x 7.3~23.3 (CW-A1, E-A2) and 26.7~43.7 (CW-A2); crates 8 / 23.5 step onto them. Right half = backdrop behind the shaft / blocks.
        // Nothing behind PRESS-A (x 56~60) / TOWER-A (x 72~) any more (audit #5: half-hidden parts dropped).
        new[] { ("factory_hall", 4f), ("watchtower", 25f), ("factory_hall", 47f), ("watchtower", 52f), ("watchtower", 62f), ("factory_hall", 67f) },
        // SEG-B: fence run x 17.2~31.2 (CW-B1, E-B3); crates 18.5 step onto it. Clear of TOWER-B (x 56~62): watchtower 51.2~54.6 (was a hall cut
        // at x 56), the one behind the tower dropped, the right group starts at x 62 (audit #5).
        new[] { ("watchtower", 3f), ("factory_hall", 10.5f), ("watchtower", 15.5f), ("factory_hall", 34.5f), ("watchtower", 39.5f), ("factory_hall", 44.5f), ("watchtower", 49.5f), ("watchtower", 52.875f), ("factory_hall", 65.3125f), ("watchtower", 70.3125f), ("factory_hall", 75.3125f) },
    };

    // Colliders (high tier drawn by PartsBackdropKit.PlatformArt). OneWay y2 = top surface. Jumps: dy <= 1.5 (StepUp), gaps <= 3u.
    static readonly Box[] Boxes = new[]
    {
        // ---- SEG-A floor (x 0~45, y 1.0): old layout (group + catwalk, first android, board 7 for hacking); crates / catwalks = Row
        new Box("GROUND-A", A, Kind.Solid, 0f, W, 0f, 1.0f),
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
        // ---- SEG-B floor (x 0~45, y 1.0): old layout, board 0 (people freeze, androids move); crate / catwalk = Row
        new Box("GROUND-B", B, Kind.Solid, 0f, W, 0f, 1.0f),
        // ---- SEG-B climb (x 46~56): inspection lift ledges 2.5 R .. 11.5 R, flush with the tower roof
        Building("TOWER-B", B, 56f, 62f, 11.5f),                   // checkpoint CP-B1 on top
        // ---- SEG-B high run (x 62~76, y 11.5) + gantry for the sniper; goal at the right end
        Beam("HRUN-B", B, 62f, W, 11.5f),
        Ledge("GANTRY-B", B, 69f, 72f, 13.0f),
    })
    .Concat(Zigzag("LIFT-B", B, 46f, 56f, 2.5f, 7, StepUp, false)) // 2.5 R, 4.0 L, 5.5 R, 7.0 L, 8.5 R, 10.0 L, 11.5 R
    .ToArray();

    // Platform art supports (PartsBackdropKit.PlatformArt): rail_pillar columns from the floor (1.0) under the high-run walkways.
    static readonly System.Collections.Generic.Dictionary<string, (float floor, float[] xs)> Pillars = new System.Collections.Generic.Dictionary<string, (float, float[])>
    {
        { "HRUN-A", (1.0f, new[] { 63.5f, 68.5f }) },
        { "HRUN-B", (1.0f, new[] { 66f, 72f }) },
    };

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
        new Foe("E-A2", A, 19f, 3.6875f, 0, 0.7f, 8f),                 // fence top (the catwalk), above E-A1
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
        new Foe("E-B3", B, 26f, 3.6875f, 0, 0.8f, 8f),                 // fence top (the catwalk)
        new Foe("E-B4", B, 30f, 1.0f, 6, 0f,   8f, android: true),
        new Foe("E-B5", B, 34f, 1.0f, 0, 0.8f, 8f),
        new Foe("E-B6", B, 40f, 1.0f, 1, 0f,   12f, ranged: true),     // ranged citizen: freezes on 0
        // SEG-B climb + high run
        new Foe("E-B8", B, 48.5f, 7.0f, 0, 0.8f, 6f),                  // lift ledge (left)
        new Foe("E-B9", B, 63f, 11.5f, 1, 0f,  12f, ranged: true, bug: true), // ranged bug: keeps shooting on 0 while the citizens freeze
        new Foe("E-B7", B, 66f, 11.5f, 0, 0.8f, 8f),
        new Foe("S-B1", B, 70.5f, 13.0f, 1, 0f, 19f, sniper: true),    // sniper on the gantry: frozen while the board shows 0, wakes if the player hacks it away
        // Elites (DECISIONS 22), 5~9, on the flat floor, not in the spawner list.
        new Foe("ELITE-A1", A, 13f, 1.0f, 0, 0.7f, 8f, cap: 7, elite: true, minNumber: 5), // under the fence run (overhead one-way, was CW-A1), 5u from the crates at x 8
        new Foe("ELITE-B1", B, 7f, 1.0f, 0, 0.8f, 8f, cap: 7, elite: true, minNumber: 5),  // before the first crate, 5u from the arrival checkpoint
        // Playtest 11-13 (user): more enemies spread over the whole map (7 kills = an auto dice roll, DECISIONS 53).
        new Foe("X-A1", A, 26f, 1.0f, 0, 0.7f, 7f, cap: 4),
        new Foe("X-A2", A, 29f, 1.0f, 0, 0.7f, 7f, cap: 4),
        new Foe("X-A3", A, 48f, 1.0f, 0, 0.7f, 7f, cap: 4),
        new Foe("X-A4", A, 52f, 1.0f, 0, 0.7f, 7f, cap: 4),
        new Foe("X-A5", A, 61.5f, 10.0f, 0, 0.7f, 7f, cap: 4),
        new Foe("X-A6", A, 70f, 10.0f, 0, 0.7f, 7f, cap: 4),
        new Foe("X-B1", B, 19f, 1.0f, 0, 0.8f, 7f, cap: 4),
        new Foe("X-B2", B, 27f, 1.0f, 0, 0.8f, 7f, cap: 4),
        new Foe("X-B3", B, 37f, 1.0f, 0, 0.8f, 7f, cap: 4),
        new Foe("X-B4", B, 44f, 1.0f, 0, 0.8f, 7f, cap: 4),
        new Foe("X-B5", B, 51f, 1.0f, 0, 0.8f, 7f, cap: 4),
        new Foe("X-B6", B, 68f, 11.5f, 0, 0.8f, 7f, cap: 4),
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
            PartsBackdropKit.Factory(roots[i], W, H, 15f, W - 15f, i == B, Row[i]); // layered parts (DECISIONS 17); camera x range [15, W - 15]
            foreach (var w in Walls(i == A ? "A" : "B", i, W, H)) BuildCollider(roots[i], w, false); // outside the camera range
        }
        // the floor matches the drawn ground line; the shaft ledges get prop art instead of placeholder bars (colliders unchanged, audit #4)
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, false); // no placeholder bars: every platform is drawn
        foreach (var b in Boxes.Where(b => !(b.id.StartsWith("GROUND") || b.id.StartsWith("CONV-A") || b.id.StartsWith("LIFT-B"))))
        {
            Pillars.TryGetValue(b.id, out var p);
            PartsBackdropKit.PlatformArt(roots[b.seg], b, p.floor, p.xs); // press / towers, high-run walkways, stack / gantry girders (colliders unchanged)
        }
        foreach (var b in Boxes.Where(b => b.id.StartsWith("CONV-A"))) PartsBackdropKit.LedgeArt(roots[b.seg], "props/conveyor", b.x1, b.x2, b.y2); // static belt (a moving one would read as a conveyor push)
        foreach (var b in Boxes.Where(b => b.id.StartsWith("LIFT-B"))) PartsBackdropKit.LedgeArt(roots[b.seg], "buildings/rail_track", b.x1, b.x2, b.y2); // iron girders
        for (int i = 0; i < roots.Length; i++) // brackets at walls, chains from platforms above (the conveyor belts are not girders)
            PartsBackdropKit.GirderDetails(roots[i], i, Boxes, Boxes.Where(b => b.kind == Kind.OneWay && !b.id.StartsWith("CONV-A")), b => b.id.StartsWith("LIFT-B"));
        // exits (audit #8): the factory scanner gate (pulsing beam) on the tower top / high run, behind the characters, no collider
        PartsBackdropKit.ExitArt(roots[GateA.seg], "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 74.8125f, GateA.y1);
        PartsBackdropKit.ExitArt(roots[GoalB.seg], "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 73.8125f, GoalB.y1);

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
        BuildDialoguePlayer(); // StageRadio's stage-start radio lines (new story 11-12)
        BuildLighting(false);
        SaveScene(scene, Tag, ScenePath);
    }

    // Adds / replaces only the elites in the saved scene (no full rebuild). Build() places them too.
    [MenuItem("NUMBER/STG-04: place elites only")]
    public static void PlaceElites()
    {
        if (!Begin(Tag, ScenePath)) return;
        var scene = OpenAndPlaceElites(Tag, ScenePath, SegId, Foes);
        AssignSortingLayers();
        SaveScene(scene, Tag, ScenePath);
    }
}
