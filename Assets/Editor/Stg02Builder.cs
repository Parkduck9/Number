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

    // Low tier (y < ~8.5) = the drawn surfaces (DECISIONS 41): the building row is world-fixed and PartsBackdropKit puts one-way floors on
    // its roofs, on the fence top between buildings and on the vending / crate lids. Top heights (Y0 1.0): crate lip 2.06, crate 2.91,
    // vending 3.63, fence 3.69, ruin eave 4.28, ruin 5.0, house 6.25, shop eave 7.34, shop 8.25.
    // Placement (part, centre x): x must stay on the 1/32 grid.
    static readonly (string part, float x)[][] Row =
    {
        // SEG-A: fence run x 16~34.75 (E-A1), then crates 29 -> fence -> ruin eave / vending -> ruin roof 5.0 (x 36.8~38.5) -> SH-A1 5.5.
        // Between the towers (x 46~64) only what fits: house 46.0~53.0, shop 53.0~61.4 (audit #5: nothing half-hidden behind TOWER-A1/A2).
        new[] { ("shop_2f", 4.5f), ("house_ac", 12.5f), ("ruin_hanok", 38.0625f), ("house_ac", 49.5f), ("shop_2f", 57.1875f) },
        // SEG-B: crates 7.5 -> fence run x 7~28.4 (E-B1, the old low rail) -> ruin roof 5.0; the rest is backdrop behind the rising rail.
        // No building under RAIL-B3/B4 (x 56~67): their roofs (5.0 / 6.25) were floors just under the tracks (audit #6).
        new[] { ("house_ac", 3.5f), ("ruin_hanok", 31.75f), ("shop_2f", 40.25f), ("house_ac", 49.5f), ("shop_2f", 72.65625f) },
    };

    // Every step is <= StepUp (1.5u) with <= 1u horizontal gap. Nothing solid hangs over a shaft (a beam above a ledge = bonk-lock).
    static readonly Box[] Boxes = new[]
    {
        // ---- SEG-A street (x 0~30): the old opening; the low roofs (ROOF-A1/A2) are now the drawn fence / roofs (Row)
        new Box("GROUND-A", A, Kind.Solid, 0f, W, 0f, 1.0f),
        // ---- climb 1 (x 30~46): drawn steps (Row; BLD-A1/A2 removed), a 5-ledge shaft, tower roof 13.0
        Building("TOWER-A1", A, 42f, 46f, 13.0f),
        // ---- high roof run (x 46~64, y 13.0) with a ridge
        Beam("ROOF-A3", A, 46f, 64f, 13.0f),
        Ledge("RIDGE-A1", A, 50f, 54f, 14.5f),
        // ---- climb 2 (x 64~76): tower block + shaft, then the top walkway back left to the gate
        Building("TOWER-A2", A, 64f, W, 14.5f),
        Beam("ROOF-A4", A, 53.75f, 64f, 22.0f), // ends at the shaft edge (x 64); from x 53.75 so the gate art (x 53.8~60.7) stands on it
    }
    .Concat(Zigzag("SH-A1", A, 34f, 42f, 5.5f, 5, StepUp, false))   // 5.5 R, 7 L, 8.5 R, 10 L, 11.5 R -> tower 13.0
    .Concat(Zigzag("SH-A2", A, 64f, W, 16f, 4, StepUp, false))      // 16 R, 17.5 L, 19 R, 20.5 L (flush with ROOF-A4 at x 64) -> 22.0
    .Concat(Walls("A", A, W, TallHeight))
    .Concat(new[]
    {
        // ---- SEG-B street (x 0~44): crates onto the drawn fence run (Row; STEP-B1/B2 + RAIL-B removed), ends before the NUM-7 group (review 06 #5)
        new Box("GROUND-B", B, Kind.Solid, 0f, W, 0f, 1.0f),
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
        new Foe("E-A1",  A, 22f, 3.6875f, 0, 0.4f, 8f, cap: 4),              // on the fence top above the street (was ROOF-A2 3.8)
        new Foe("E-A2",  A, 18f, 1.0f,  0, 0.4f,  8f, cap: 4),               // melee + ranged behind (one group with E-A1)
        new Foe("E-A3",  A, 24f, 1.0f,  1, 0f,    12f, ranged: true),
        new Foe("E-A4",  A, 37.5f, 5.0f, 0, 0.4f, 8f, cap: 4),               // on the ruin roof (was BLD-A2 x 40), guards the shaft entrance
        new Foe("E-A5",  A, 36f, 10.0f, 0, 0.4f,  6f, cap: 4),               // shaft ledge (left)
        new Foe("E-A6",  A, 49f, 13.0f, 0, 0.45f, 8f, cap: 4),               // roof run group: roof + ridge + roof + ranged bug
        new Foe("E-A7",  A, 52f, 14.5f, 0, 0.45f, 8f, cap: 4),
        new Foe("E-A8",  A, 57f, 13.0f, 0, 0.45f, 8f, cap: 4),
        new Foe("E-A9",  A, 62f, 13.0f, 1, 0f,    12f, ranged: true, bug: true), // first ranged AI bug
        new Foe("E-A10", A, 71f, 19.0f, 0, 0.5f,  6f, cap: 4),               // climb 2 ledge (right)
        new Foe("S-A1",  A, 66.5f, 14.5f, 1, 0f,  19f, sniper: true),        // on TOWER-A2, laser along the roof run
        new Foe("E-B1",  B, 20f, 3.6875f, 0, 0.6f, 8f, cap: 4),              // on the fence top (was the low rail RAIL-B 5.0)
        new Foe("E-B2",  B, 30f, 1.0f,  0, 0.6f,  8f),                       // 5+ allowed from here (late STG-02)
        new Foe("E-B3",  B, 33f, 1.0f,  0, 0.6f,  8f),                       // pair with E-B2, ranged E-B5 behind
        new Foe("E-B5",  B, 38f, 1.0f,  1, 0f,    12f, ranged: true),
        new Foe("E-B4",  B, 42f, 1.0f,  0, 0.6f,  8f),
        new Foe("E-B6",  B, 58f, 5.5f,  0, 0.7f,  6f),                       // rising rail
        new Foe("S-B1",  B, 66f, 7.0f,  1, 0f,    19f, sniper: true),        // right end of the rail: covers the rail and the street
        new Foe("E-B7",  B, 43f, 11.5f, 0, 0.7f,  8f),                       // high rail group: 2 melee + ranged bug, spawner behind
        new Foe("E-B8",  B, 40f, 11.5f, 0, 0.7f,  8f),
        new Foe("E-B9",  B, 37f, 11.5f, 1, 0f,    12f, ranged: true, bug: true),
        new Foe("ELITE-B1", B, 60f, 1.0f, 0, 0.7f, 8f, elite: true),           // clash-duel elite on the open street under the rising rail (not respawned)
        new Foe("ELITE-B2", B, 16f, 1.0f, 0, 0.4f, 8f, cap: 6, elite: true, minNumber: 3), // second elite (DECISIONS 22): early street below the fence run, 3~6, flat 8u each side
    };

    // Breakable spawner (DECISIONS 15): one per map, at the top of the last climb, watching only SEG-B enemies (doors are one way).
    static readonly Pt Spawner = new Pt("SPAWNER-B", B, 35.5f, 11.5f);

    // Billboards (segment-local centre): A shows 3 (review), B shows 7 (first time). Zone = the whole segment.
    static readonly Vector2 BoardPosA = new Vector2(4.5f, 9.5f), BoardPosB = new Vector2(16f, 9.5f); // A stands on the shop_2f roof (8.25), was in the sky at (22, 8.5)
    const int BoardA = 3, BoardB = 7;

    static readonly Box GateA = new Box("TR-A-GATE", A, Kind.Solid, 56f, 58.5f, 22.0f, 25f); // top walkway, left end
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 30f, 33.5f, 11.5f, 15.5f); // high rail, left end

    // Ledges drawn by the backdrop (no placeholder bar; colliders stay): the rail (world-fixed track on each RAIL-B ledge), the shaft girders,
    // and the END-B stop post behind the goal gate's left wing.
    static bool HasArt(Box b) => b.id.StartsWith("RAIL-B") || b.id.StartsWith("SH-A") || b.id == "END-B";

    // Platform art supports (PartsBackdropKit.PlatformArt): rail_pillar columns under the roof walkways, (floor they stand on, x list).
    // x 60 is shared so the ROOF-A4 column continues the ROOF-A3 one.
    static readonly System.Collections.Generic.Dictionary<string, (float floor, float[] xs)> Pillars = new System.Collections.Generic.Dictionary<string, (float, float[])>
    {
        { "ROOF-A3", (1.0f, new[] { 50f, 60f }) },
        { "ROOF-A4", (13.0f, new[] { 60f }) }, // stands on ROOF-A3
    };

    // Rail art spans = the RAIL-B ledges (x1, x2, top); RAIL-B7's track runs on under the END-B post to the gate's left edge (x 29).
    static (float x1, float x2, float top)[] RailLedges() =>
        Boxes.Where(b => b.id.StartsWith("RAIL-B")).Select(b => (b.id == "RAIL-B7" ? b.x1 - 1f : b.x1, b.x2, b.y2)).ToArray();

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
            PartsBackdropKit.Rooftops(roots[i], W, SegH[i], 15f, W - 15f, Row[i], i == B ? RailLedges() : null); // layered parts (DECISIONS 17); camera x range [15, W - 15]
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, false); // no placeholder bars: every platform is drawn
        foreach (var b in Boxes.Where(b => b.id.StartsWith("SH-A"))) PartsBackdropKit.LedgeArt(roots[b.seg], "buildings/rail_track", b.x1, b.x2, b.y2); // shaft ledges = iron girders
        foreach (var b in Boxes.Where(b => !(b.id.StartsWith("GROUND") || b.id.StartsWith("WALL") || HasArt(b))))
        {
            Pillars.TryGetValue(b.id, out var p);
            PartsBackdropKit.PlatformArt(roots[b.seg], b, p.floor, p.xs); // towers, roof walkways, ridge girder (colliders unchanged)
        }
        // exits (audit #8): the hanok scanner gate, door centred on the trigger (B: left wing over the END-B stop post, which it hides)
        PartsBackdropKit.ExitArt(roots[GateA.seg], "buildings/gate_scanner", null, 0f, 57.25f, GateA.y1);
        PartsBackdropKit.ExitArt(roots[GoalB.seg], "buildings/gate_scanner", null, 0f, 32.4375f, GoalB.y1);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(SegX, PlayerStart, 1f);

        var enemies = Foes.Select(f => (f, go: BuildEnemy(roots[f.seg], f))).ToList();
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);
        BuildSpawner(roots[Spawner.seg], Spawner.id, new Vector2(Spawner.x, Spawner.y),
            enemies.Where(e => e.f.seg == Spawner.seg && !e.f.elite).Select(e => e.go).ToList());

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

    // Adds / replaces only the elites in the saved scene (no full rebuild, keeps anything edited since). Build() places them too.
    [MenuItem("NUMBER/STG-02: place elite only")]
    public static void PlaceElite()
    {
        if (!Begin(Tag, ScenePath)) return;
        var scene = OpenAndPlaceElites(Tag, ScenePath, SegId, Foes);
        AssignSortingLayers();
        SaveScene(scene, Tag, ScenePath);
    }
}
