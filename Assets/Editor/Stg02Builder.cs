using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build STG-02 -> Assets/Scenes/STG02.unity. Idempotent. Layout: docs/design/LEVEL_STG02.md "세로 맵 (DECISIONS 15)".
// SEG-A rooftops (broadcast 3): street -> zigzag shaft -> high roof run -> second shaft -> top walkway -> gate.
// SEG-B elevated rail (broadcast 7): street + low rail -> rising rail ledges with a switchback -> high rail back to the LEFT -> goal.
public static class Stg02Builder
{
    const string Tag = "Stg02Builder";
    const string ScenePath = "Assets/Scenes/STG02.unity";
    const int A = 0, B = 1;
    const float W = WideWidth; // 75.9375 (DECISIONS 12-C)

    // Segments at world x 0 and 110.
    static readonly float[] SegX = { 0f, 110f };
    static readonly float[] SegH = { TallHeight, SegHeight * 1.5f }; // A climbs to y ~22, B to y ~11.5
    static readonly string[] SegId = { "SEG-A", "SEG-B" };

    // Every step is <= StepUp (1.5u) with <= 1u horizontal gap. Nothing solid hangs over a shaft (a beam above a ledge = bonk-lock).
    static readonly Box[] Boxes = new[]
    {
        // ---- SEG-A street (x 0~30): the old opening (low roofs over the street)
        new Box("GROUND-A", A, Kind.Solid, 0f, W, 0f, 1.0f),
        Ledge("ROOF-A1", A, 6f, 16f, 2.5f),
        Ledge("ROOF-A2", A, 18f, 28f, 3.8f),
        // ---- climb 1 (x 30~46): two roofs, a 5-ledge shaft, tower roof 13.0
        Building("BLD-A1", A, 30f, 34f, 2.5f),
        Building("BLD-A2", A, 34f, 42f, 4.0f),
        Building("TOWER-A1", A, 42f, 46f, 13.0f),
        // ---- high roof run (x 46~64, y 13.0) with a ridge
        Beam("ROOF-A3", A, 46f, 64f, 13.0f),
        Ledge("RIDGE-A1", A, 50f, 54f, 14.5f),
        // ---- climb 2 (x 64~76): tower block + shaft, then the top walkway back left to the gate
        Building("TOWER-A2", A, 64f, W, 14.5f),
        Beam("ROOF-A4", A, 56f, 64f, 22.0f), // ends at the shaft edge (x 64)
    }
    .Concat(Zigzag("SH-A1", A, 34f, 42f, 5.5f, 5, StepUp, false))   // 5.5 R, 7 L, 8.5 R, 10 L, 11.5 R -> tower 13.0
    .Concat(Zigzag("SH-A2", A, 64f, W, 16f, 4, StepUp, false))      // 16 R, 17.5 L, 19 R, 20.5 L (flush with ROOF-A4 at x 64) -> 22.0
    .Concat(Walls("A", A, W, TallHeight))
    .Concat(new[]
    {
        // ---- SEG-B street (x 0~44): steps onto the low rail (ends before the NUM-7 group, review 06 #5)
        new Box("GROUND-B", B, Kind.Solid, 0f, W, 0f, 1.0f),
        Ledge("STEP-B1", B, 3f, 6f, 2.5f),
        Ledge("STEP-B2", B, 5f, 8f, 3.8f),
        Ledge("RAIL-B", B, 8f, 28f, 5.0f),
        // ---- rising rail (x 44~67): +1.5 every 6u to the right, then a switchback to the left
        Ledge("RAIL-B1", B, 44f, 49f, 2.5f),
        Ledge("RAIL-B2", B, 50f, 55f, 4.0f),
        Ledge("RAIL-B3", B, 56f, 61f, 5.5f),
        Ledge("RAIL-B4", B, 62f, 67f, 7.0f),
        Ledge("RAIL-B5", B, 56f, 61f, 8.5f),  // switchback (3u above RAIL-B3: one-way, jumped through)
        Ledge("RAIL-B6", B, 50f, 55f, 10.0f),
        // ---- high rail back over the street (x 30~49.5, y 11.5) -> goal at its left end
        Beam("RAIL-B7", B, 30f, 49.5f, 11.5f),
        new Box("END-B", B, Kind.Solid, 29f, 30f, 11.25f, 15f), // stop post: a refused goal (enemies / spawner left) must not drop the player to the street
    })
    .Concat(Walls("B", B, W, SegHeight * 1.5f))
    .ToArray();

    static readonly Pt PlayerStart = new Pt("P-START", A, 3f, 1.0f);
    static readonly Pt Arrival = new Pt("P-B", B, 2f, 1.0f);
    static readonly Pt[] Checkpoints =
    {
        new Pt("CP-A0", A, 3f, 1.0f),
        new Pt("CP-A1", A, 44f, 13.0f),  // top of climb 1
        new Pt("CP-A2", A, 74f, 14.5f),  // foot of climb 2
        new Pt("CP-B0", B, 2f, 1.0f),
        new Pt("CP-B1", B, 48f, 11.5f),  // top of the rising rail
    };

    // Numbers: cap 4 in SEG-A, 5+ allowed in SEG-B (late STG-02). Ranged = gold 1 on the player's surface (shot is horizontal);
    // bug = ranged AI bug (keeps moving on 0, DECISIONS 14), else ranged citizen. sniper = DECISIONS 15 sniper (19u line-of-sight laser).
    static readonly Foe[] Foes =
    {
        new Foe("E-A1",  A, 22f, 3.8f,  0, 0.4f,  8f, cap: 4),               // on the roof above the street
        new Foe("E-A2",  A, 18f, 1.0f,  0, 0.4f,  8f, cap: 4),               // melee + ranged behind (one group with E-A1)
        new Foe("E-A3",  A, 24f, 1.0f,  1, 0f,    12f, ranged: true),
        new Foe("E-A4",  A, 40f, 4.0f,  0, 0.4f,  8f, cap: 4),               // on BLD-A2, guards the shaft entrance
        new Foe("E-A5",  A, 36f, 10.0f, 0, 0.4f,  6f, cap: 4),               // shaft ledge (left)
        new Foe("E-A6",  A, 49f, 13.0f, 0, 0.45f, 8f, cap: 4),               // roof run group: roof + ridge + roof + ranged bug
        new Foe("E-A7",  A, 52f, 14.5f, 0, 0.45f, 8f, cap: 4),
        new Foe("E-A8",  A, 57f, 13.0f, 0, 0.45f, 8f, cap: 4),
        new Foe("E-A9",  A, 62f, 13.0f, 1, 0f,    12f, ranged: true, bug: true), // first ranged AI bug
        new Foe("E-A10", A, 71f, 19.0f, 0, 0.5f,  6f, cap: 4),               // climb 2 ledge (right)
        new Foe("S-A1",  A, 66.5f, 14.5f, 1, 0f,  19f, sniper: true),        // on TOWER-A2, laser along the roof run
        new Foe("E-B1",  B, 20f, 5.0f,  0, 0.6f,  8f, cap: 4),               // on the low rail
        new Foe("E-B2",  B, 30f, 1.0f,  0, 0.6f,  8f),                       // 5+ allowed from here (late STG-02)
        new Foe("E-B3",  B, 33f, 1.0f,  0, 0.6f,  8f),                       // pair with E-B2, ranged E-B5 behind
        new Foe("E-B5",  B, 38f, 1.0f,  1, 0f,    12f, ranged: true),
        new Foe("E-B4",  B, 42f, 1.0f,  0, 0.6f,  8f),
        new Foe("E-B6",  B, 58f, 5.5f,  0, 0.7f,  6f),                       // rising rail
        new Foe("S-B1",  B, 66f, 7.0f,  1, 0f,    19f, sniper: true),        // right end of the rail: covers the rail and the street
        new Foe("E-B7",  B, 43f, 11.5f, 0, 0.7f,  8f),                       // high rail group: 2 melee + ranged bug, spawner behind
        new Foe("E-B8",  B, 40f, 11.5f, 0, 0.7f,  8f),
        new Foe("E-B9",  B, 37f, 11.5f, 1, 0f,    12f, ranged: true, bug: true),
    };

    // Breakable spawner (DECISIONS 15): one per map, at the top of the last climb, watching only SEG-B enemies (doors are one way).
    static readonly Pt Spawner = new Pt("SPAWNER-B", B, 35.5f, 11.5f);

    // Billboards (segment-local centre): A shows 3 (review), B shows 7 (first time). Zone = the whole segment.
    static readonly Vector2 BoardPosA = new Vector2(22f, 8.5f), BoardPosB = new Vector2(16f, 9.5f);
    const int BoardA = 3, BoardB = 7;

    static readonly Box GateA = new Box("TR-A-GATE", A, Kind.Solid, 56f, 58.5f, 22.0f, 25f); // top walkway, left end
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 30f, 33.5f, 11.5f, 15.5f); // high rail, left end

    [MenuItem("NUMBER/Build STG-02")]
    public static void Build()
    {
        if (!Begin(Tag, ScenePath)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var roots = new Transform[SegX.Length];
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i] = new GameObject(SegId[i]).transform;
            roots[i].position = new Vector3(SegX[i], 0f, 0f);
            PartsBackdropKit.Rooftops(roots[i], W, SegH[i], 15f, W - 15f, i == B); // layered parts (DECISIONS 17); camera x range [15, W - 15]
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, !(b.id.StartsWith("GROUND") || b.id.StartsWith("WALL")));

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(SegX, PlayerStart, 1f);

        var enemies = Foes.Select(f => (f, go: BuildEnemy(roots[f.seg], f))).ToList();
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);
        BuildSpawner(roots[Spawner.seg], Spawner.id, new Vector2(Spawner.x, Spawner.y),
            enemies.Where(e => e.f.seg == Spawner.seg).Select(e => e.go).ToList());

        BuildBoard(roots[A], "BOARD-A", BoardPosA, BoardA, new Vector2(SegX[A], SegX[A] + W));
        BuildBoard(roots[B], "BOARD-B", BoardPosB, BoardB, new Vector2(SegX[B], SegX[B] + W));

        var arrival = new GameObject("P-B (arrival)").transform;
        arrival.SetParent(roots[Arrival.seg], false);
        arrival.localPosition = new Vector3(Arrival.x, Arrival.y, 0f);
        SetObj(BuildTrigger(roots[GateA.seg], GateA).AddComponent<LevelTransition>(), "destination", arrival);
        SetInt(BuildTrigger(roots[GoalB.seg], GoalB).AddComponent<LevelTransition>(), "mode", (int)LevelTransition.Mode.Goal);

        BuildCamera(playerGo.transform, Enumerable.Range(0, SegX.Length).Select(i => SegRect(SegX[i], W, SegH[i])).ToArray(), new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud(false); // number HUD shown, no hack dial: the device is obtained in STG-03
        BuildLighting(false);
        SaveScene(scene, Tag, ScenePath);
    }
}
