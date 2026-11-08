using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build STG-04 -> Assets/Scenes/STG04.unity. Idempotent. Data: docs/design/LEVEL_STG04.md (tables below = that doc).
public static class Stg04Builder
{
    const string Tag = "Stg04Builder";
    const string ScenePath = "Assets/Scenes/STG04.unity";
    const int A = 0, B = 1;

    // Segments at world x 0 and 60. Solid placeholder backgrounds (real art comes later).
    static readonly float[] SegX = { 0f, 60f };
    static readonly string[] SegId = { "SEG-A", "SEG-B" };
    static readonly Color[] SegColor = { new Color32(0x23, 0x26, 0x2B, 0xFF), new Color32(0x2E, 0x2A, 0x26, 0xFF) };

    // Colliders (all drawn as placeholder squares). OneWay y2 = top surface.
    static readonly Box[] Boxes =
    {
        new Box("GROUND-A", A, Kind.Solid, 0f, 50.625f, 0f, 1.0f),
        new Box("CRATE-A1", A, Kind.OneWay, 7f,  9f,  2.05f, 2.3f),
        new Box("CW-A1",    A, Kind.OneWay, 10f, 22f, 3.35f, 3.6f),
        new Box("CRATE-A2", A, Kind.OneWay, 24f, 26f, 2.05f, 2.3f),
        new Box("CW-A2",    A, Kind.OneWay, 28f, 40f, 3.35f, 3.6f),
        new Box("GROUND-B", B, Kind.Solid, 0f, 50.625f, 0f, 1.0f),
        new Box("CRATE-B1", B, Kind.OneWay, 17f, 19f, 2.05f, 2.3f),
        new Box("CW-B1",    B, Kind.OneWay, 20f, 30f, 3.35f, 3.6f),
    };

    static readonly Pt PlayerStart = new Pt("P-START", A, 3f, 1.0f);
    static readonly Pt Arrival = new Pt("P-B", B, 2f, 1.0f);
    static readonly Pt[] Checkpoints = { new Pt("CP-A0", A, 3f, 1.0f), new Pt("CP-B0", B, 2f, 1.0f) };

    // 7th arg true = android, 8th arg true = gold ranged 1. Every enemy sits in a group or on a catwalk.
    static readonly Foe[] Foes =
    {
        new Foe("E-A1", A, 16f, 1.0f, 0, 0.7f, 8f),
        new Foe("E-A2", A, 19f, 3.6f, 0, 0.7f, 8f), // catwalk, above E-A1
        new Foe("E-A3", A, 33f, 1.0f, 0, 0.7f, 8f),
        new Foe("E-A4", A, 38f, 1.0f, 3, 0f,   8f, true),         // first android: fixed 3 = "parry the 3rd strike" lesson (review 06 #1)
        new Foe("E-A5", A, 44f, 1.0f, 1, 0f,   6f, false, true),  // ranged behind melee + android; wakes only past x 38 so the android is read 1:1 (review 06 #2)
        new Foe("E-B1", B, 15f, 1.0f, 0, 0.8f, 8f),
        new Foe("E-B2", B, 18f, 1.0f, 4, 0f,   8f, true), // androids 3 -> 4 -> 6; no 7 outside the boss (review 06 #3)
        new Foe("E-B3", B, 26f, 3.6f, 0, 0.8f, 8f), // catwalk
        new Foe("E-B4", B, 30f, 1.0f, 6, 0f,   8f, true),
        new Foe("E-B5", B, 34f, 1.0f, 0, 0.8f, 8f),
        new Foe("E-B6", B, 40f, 1.0f, 1, 0f,   12f, false, true), // ranged behind
    };

    // Billboards (segment-local center, within hand reach for hacking): A shows 7, B shows 0. Zone = the whole segment.
    static readonly Vector2 BoardPosA = new Vector2(25f, 4.5f), BoardPosB = new Vector2(10f, 4.5f); // y 4.5: within BroadcastHackUi.interactRange (3u) of a grounded player (center y 2)
    const int BoardA = 7, BoardB = 0;

    static readonly Box GateA = new Box("TR-A-GATE", A, Kind.Solid, 47f, 50.6f, 1f, 5f);
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 46f, 50f, 1f, 5f);

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
            BuildImageBackground(roots[i], i == A ? "Assets/Art/Backgrounds/bg_stg04_segA_production_line.png" : "Assets/Art/Backgrounds/bg_stg04_segB_inspection_hall.png", SegWidth, SegHeight, SegColor[i]);
            foreach (var w in Walls(i == A ? "A" : "B", i)) BuildCollider(roots[i], w, true);
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, true);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(SegX, PlayerStart, 1f);

        foreach (var f in Foes) BuildEnemy(roots[f.seg], f);
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);

        BuildBoard(roots[A], "BOARD-A", BoardPosA, BoardA, new Vector2(SegX[A], SegX[A] + SegWidth));
        BuildBoard(roots[B], "BOARD-B", BoardPosB, BoardB, new Vector2(SegX[B], SegX[B] + SegWidth));

        var arrival = new GameObject("P-B (arrival)").transform;
        arrival.SetParent(roots[Arrival.seg], false);
        arrival.localPosition = new Vector3(Arrival.x, Arrival.y, 0f);
        var gate = BuildTrigger(roots[GateA.seg], GateA).AddComponent<LevelTransition>();
        SetObj(gate, "destination", arrival);
        var goal = BuildTrigger(roots[GoalB.seg], GoalB).AddComponent<LevelTransition>();
        SetInt(goal, "mode", (int)LevelTransition.Mode.Goal);

        BuildLight(1f, Color.white);
        BuildCamera(playerGo.transform, new[] { SegRect(SegX[A]), SegRect(SegX[B]) }, new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud(true);
        SaveScene(scene, Tag, ScenePath);
    }
}
