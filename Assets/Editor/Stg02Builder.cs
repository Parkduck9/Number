using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build STG-02 -> Assets/Scenes/STG02.unity. Idempotent. Data: docs/design/LEVEL_STG02.md (tables below = that doc).
public static class Stg02Builder
{
    const string Tag = "Stg02Builder";
    const string ScenePath = "Assets/Scenes/STG02.unity";
    const int A = 0, B = 1;
    const float W = WideWidth; // 75.9375 (DECISIONS 12-C: x1.5). Old layout kept at x 0~45, new group + mid platforms at x 46~70, gate/goal moved by +25.3125.
    // Spill lights, px on the background images (LIGHTING_DESIGN 3.1). A: two stone lanterns on the street.
    // B: the lantern by the gate (flickers) + two shop signboards (vermilion, teal) above the shutters.
    static readonly Lamp[] LampsA = { new Lamp(452f, 450f, LampWarm, 4f), new Lamp(990f, 450f, LampWarm, 4f) };
    static readonly Lamp[] LampsB = { new Lamp(1403f, 425f, LampWarm, 4f, 0.2f), new Lamp(410f, 390f, LampRed, 5f), new Lamp(905f, 385f, LampTeal, 5f) };

    // Segments at world x 0 and 110 (>= 101.25 apart: the mirrored background fill reaches 25.3u left of each segment).
    static readonly float[] SegX = { 0f, 110f };
    static readonly string[] SegId = { "SEG-A", "SEG-B" };
    static readonly Color[] SegColor = { new Color32(0x2B, 0x24, 0x40, 0xFF), new Color32(0x1F, 0x2A, 0x3A, 0xFF) };

    // Colliders (all drawn as placeholder squares). OneWay y2 = top surface.
    static readonly Box[] Boxes =
    {
        new Box("GROUND-A", A, Kind.Solid, 0f, W, 0f, 1.0f),
        new Box("ROOF-A1",  A, Kind.OneWay, 6f,  16f,   2.25f, 2.5f),
        new Box("ROOF-A2",  A, Kind.OneWay, 18f, 28f,   3.55f, 3.8f),
        new Box("ROOF-A3",  A, Kind.OneWay, 31f, 40f,   2.25f, 2.5f),
        new Box("ROOF-A4",  A, Kind.OneWay, 46f, 50f,   2.05f, 2.3f), // +1.3 step
        new Box("ROOF-A5",  A, Kind.OneWay, 51f, 57f,   3.35f, 3.6f), // +2.6: diagonal lunge target (ranged shots pass under it)
        new Box("GROUND-B", B, Kind.Solid, 0f, W, 0f, 1.0f),
        new Box("STEP-B1",  B, Kind.OneWay, 3f, 6f,   2.25f, 2.5f),
        new Box("STEP-B2",  B, Kind.OneWay, 5f, 8f,   3.55f, 3.8f),
        new Box("RAIL-B",   B, Kind.OneWay, 8f, 28f,  4.75f, 5.0f), // ends before the x 30-38 group so the rail cannot skip the NUM-7 fight (review 06 #5)
        new Box("PLAT-B1",  B, Kind.OneWay, 49f, 53f, 2.05f, 2.3f), // +1.3 step
        new Box("PLAT-B2",  B, Kind.OneWay, 54f, 60f, 3.35f, 3.6f), // +2.6
    };

    static readonly Pt PlayerStart = new Pt("P-START", A, 3f, 1.0f);
    static readonly Pt Arrival = new Pt("P-B", B, 2f, 1.0f);
    static readonly Pt[] Checkpoints = { new Pt("CP-A0", A, 3f, 1.0f), new Pt("CP-B0", B, 2f, 1.0f) };

    // Numbers: cap 4 until late SEG-B (5+ allowed from E-B2 on). Ranged = gold 1 (7th arg android=false, 8th ranged=true, 10th bug=true = ranged AI bug, else ranged citizen).
    // Groups of 3-4, each with a ranged 1 or a platform enemy. Ranged stand on the ground: their shot flies at feet + 1.3.
    static readonly Foe[] Foes =
    {
        new Foe("E-A1", A, 22f, 3.8f, 0, 0.4f, 8f, false, false, 4), // on the roof (above the path)
        new Foe("E-A2", A, 18f, 1.0f, 0, 0.4f, 8f, false, false, 4), // melee with a gold ranged 1 behind it (wakes at x 10, ranged at 12, roof at 14 = one group)
        new Foe("E-A3", A, 24f, 1.0f, 1, 0f,   12f, false, true),
        new Foe("E-A4", A, 34f, 1.0f, 0, 0.4f, 8f, false, false, 4), // group of 3 with E-A5 and E-A6 (roof)
        new Foe("E-A5", A, 37f, 1.0f, 0, 0.4f, 8f, false, false, 4),
        new Foe("E-A6", A, 38f, 2.5f, 0, 0.4f, 8f, false, false, 4), // on ROOF-A3
        new Foe("E-A7", A, 53f, 1.0f, 0, 0.45f, 8f, false, false, 4), // new group (wakes at x 45): ground + ROOF-A5 + ranged
        new Foe("E-A8", A, 55f, 3.6f, 0, 0.45f, 8f, false, false, 4),
        new Foe("E-A9", A, 62f, 1.0f, 1, 0f,   12f, false, true, 0, true), // first ranged AI bug (swarm; DECISIONS 14)
        new Foe("E-B1", B, 20f, 5.0f, 0, 0.6f, 8f, false, false, 4), // on the rail
        new Foe("E-B2", B, 30f, 1.0f, 0, 0.6f, 8f),                  // 5+ allowed from here (late STG-02)
        new Foe("E-B3", B, 33f, 1.0f, 0, 0.6f, 8f),                  // pair with E-B2, ranged E-B5 behind
        new Foe("E-B5", B, 38f, 1.0f, 1, 0f,   12f, false, true),
        new Foe("E-B4", B, 43f, 1.0f, 0, 0.6f, 8f),                  // ground, behind ranged E-B5 (was on the rail; review 06 #5)
        new Foe("E-B6", B, 56f, 1.0f, 0, 0.7f, 8f),                  // new group of 4 (wakes at x 48): ground + PLAT-B2 + ground + ranged
        new Foe("E-B7", B, 58f, 3.6f, 0, 0.7f, 8f),
        new Foe("E-B8", B, 61f, 1.0f, 0, 0.7f, 8f),
        new Foe("E-B9", B, 66f, 1.0f, 1, 0f,   12f, false, true, 0, true), // ranged AI bug
    };

    // Billboards (segment-local center): A shows 3 (review), B shows 7 (first time). Zone = the whole segment.
    // x 50.3125 = old 25 + 25.3125: the empty sky in the middle of the (right-aligned) background image.
    static readonly Vector2 BoardPos = new Vector2(50.3125f, 11f);
    const int BoardA = 3, BoardB = 7;

    static readonly Box GateA = new Box("TR-A-GATE", A, Kind.Solid, 72.3125f, 75.9125f, 1f, 5f);
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 71.3125f, 75.3125f, 1f, 5f);

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
            BuildImageBackground(roots[i], i == A ? "Assets/Art/Backgrounds/bg_stg02_segA_rooftops.png" : "Assets/Art/Backgrounds/bg_stg02_segB_elevated_rail.png", W, SegHeight, SegColor[i]);
            foreach (var w in Walls(i == A ? "A" : "B", i, W)) BuildCollider(roots[i], w, true);
            AddLamps(roots[i], W, i == A ? LampsA : LampsB);
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, true);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(SegX, PlayerStart, 1f);

        foreach (var f in Foes) BuildEnemy(roots[f.seg], f);
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);

        BuildBoard(roots[A], "BOARD-A", BoardPos, BoardA, new Vector2(SegX[A], SegX[A] + W));
        BuildBoard(roots[B], "BOARD-B", BoardPos, BoardB, new Vector2(SegX[B], SegX[B] + W));

        var arrival = new GameObject("P-B (arrival)").transform;
        arrival.SetParent(roots[Arrival.seg], false);
        arrival.localPosition = new Vector3(Arrival.x, Arrival.y, 0f);
        var gate = BuildTrigger(roots[GateA.seg], GateA).AddComponent<LevelTransition>();
        SetObj(gate, "destination", arrival);
        var goal = BuildTrigger(roots[GoalB.seg], GoalB).AddComponent<LevelTransition>();
        SetInt(goal, "mode", (int)LevelTransition.Mode.Goal);

        BuildCamera(playerGo.transform, new[] { SegRect(SegX[A], W), SegRect(SegX[B], W) }, new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud(false); // number HUD shown, no hack dial: the device is obtained in STG-03
        BuildLighting(false);
        SaveScene(scene, Tag, ScenePath);
    }
}
