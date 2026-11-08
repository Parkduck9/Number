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
    const float W = WideWidth; // 75.9375 (DECISIONS 12-C: x1.5). Old layout kept at x 0~45, new group + crate/catwalk at x 48~66, gate/goal moved by +25.3125.

    // Segments at world x 0 and 110 (>= 101.25 apart: the mirrored background fill reaches 25.3u left of each segment).
    static readonly float[] SegX = { 0f, 110f };
    static readonly string[] SegId = { "SEG-A", "SEG-B" };
    static readonly Color[] SegColor = { new Color32(0x23, 0x26, 0x2B, 0xFF), new Color32(0x2E, 0x2A, 0x26, 0xFF) };

    // Colliders (all drawn as placeholder squares). OneWay y2 = top surface.
    static readonly Box[] Boxes =
    {
        new Box("GROUND-A", A, Kind.Solid, 0f, W, 0f, 1.0f),
        new Box("CRATE-A1", A, Kind.OneWay, 7f,  9f,  2.05f, 2.3f),
        new Box("CW-A1",    A, Kind.OneWay, 10f, 22f, 3.35f, 3.6f),
        new Box("CRATE-A2", A, Kind.OneWay, 24f, 26f, 2.05f, 2.3f),
        new Box("CW-A2",    A, Kind.OneWay, 28f, 40f, 3.35f, 3.6f),
        new Box("CRATE-A3", A, Kind.OneWay, 49f, 52f, 2.05f, 2.3f), // +1.3 step
        new Box("CW-A3",    A, Kind.OneWay, 53f, 61f, 3.35f, 3.6f), // +2.6: diagonal lunge target
        new Box("GROUND-B", B, Kind.Solid, 0f, W, 0f, 1.0f),
        new Box("CRATE-B1", B, Kind.OneWay, 17f, 19f, 2.05f, 2.3f),
        new Box("CW-B1",    B, Kind.OneWay, 20f, 30f, 3.35f, 3.6f),
        new Box("CRATE-B2", B, Kind.OneWay, 48f, 51f, 2.05f, 2.3f),
        new Box("CW-B2",    B, Kind.OneWay, 52f, 60f, 3.35f, 3.6f),
    };

    static readonly Pt PlayerStart = new Pt("P-START", A, 3f, 1.0f);
    static readonly Pt Arrival = new Pt("P-B", B, 2f, 1.0f);
    static readonly Pt[] Checkpoints = { new Pt("CP-A0", A, 3f, 1.0f), new Pt("CP-B0", B, 2f, 1.0f) };

    // 7th arg true = android, 8th arg true = gold ranged 1 (10th arg true = ranged AI bug, else ranged citizen). Every enemy sits in a group or on a catwalk.
    static readonly Foe[] Foes =
    {
        new Foe("E-A1", A, 16f, 1.0f, 0, 0.7f, 8f),
        new Foe("E-A2", A, 19f, 3.6f, 0, 0.7f, 8f), // catwalk, above E-A1
        new Foe("E-A6", A, 21f, 1.0f, 0, 0.7f, 8f), // third of the E-A1/E-A2 group (under the catwalk end)
        new Foe("E-A3", A, 33f, 1.0f, 0, 0.7f, 8f),
        new Foe("E-A4", A, 38f, 1.0f, 3, 0f,   8f, true),         // first android: fixed 3 = "parry the 3rd strike" lesson (review 06 #1)
        new Foe("E-A5", A, 44f, 1.0f, 1, 0f,   6f, false, true),  // ranged behind melee + android; wakes only past x 38 so the android is read 1:1 (review 06 #2)
        new Foe("E-A7", A, 55f, 1.0f, 0, 0.7f, 8f),               // new group (wakes at x 47): ground + catwalk + ranged
        new Foe("E-A8", A, 58f, 3.6f, 0, 0.7f, 8f),
        new Foe("E-A9", A, 65f, 1.0f, 1, 0f,   12f, false, true, 0, true), // ranged AI bug on the ground (shot at feet + 1.3 passes under CW-A3)
        new Foe("E-B1", B, 15f, 1.0f, 0, 0.8f, 8f),
        new Foe("E-B2", B, 18f, 1.0f, 4, 0f,   8f, true), // androids 3 -> 4 -> 6; no 7 outside the boss (review 06 #3)
        new Foe("E-B3", B, 26f, 3.6f, 0, 0.8f, 8f), // catwalk
        new Foe("E-B4", B, 30f, 1.0f, 6, 0f,   8f, true),
        new Foe("E-B5", B, 34f, 1.0f, 0, 0.8f, 8f),
        new Foe("E-B6", B, 40f, 1.0f, 1, 0f,   12f, false, true), // ranged behind
        new Foe("E-B7", B, 54f, 1.0f, 0, 0.8f, 8f),               // new group (wakes at x 46): ground + catwalk + ranged
        new Foe("E-B8", B, 57f, 3.6f, 0, 0.8f, 8f),
        new Foe("E-B9", B, 64f, 1.0f, 1, 0f,   12f, false, true, 0, true), // ranged AI bug: keeps shooting on 0 while E-B6 (ranged citizen) freezes
    };

    // Billboards (segment-local center, within hand reach for hacking): A shows 7, B shows 0. Zone = the whole segment.
    static readonly Vector2 BoardPosA = new Vector2(25f, 4.5f), BoardPosB = new Vector2(10f, 4.5f); // y 4.5: within BroadcastHackUi.interactRange (3u) of a grounded player (center y 2)
    const int BoardA = 7, BoardB = 0;

    static readonly Box GateA = new Box("TR-A-GATE", A, Kind.Solid, 72.3125f, 75.9125f, 1f, 5f);
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 71.3125f, 75.3125f, 1f, 5f);

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
            foreach (var w in Walls(i == A ? "A" : "B", i, W)) BuildCollider(roots[i], w, true);
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, true);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(SegX, PlayerStart, 1f);

        foreach (var f in Foes) BuildEnemy(roots[f.seg], f);
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);

        BuildBoard(roots[A], "BOARD-A", BoardPosA, BoardA, new Vector2(SegX[A], SegX[A] + W));
        BuildBoard(roots[B], "BOARD-B", BoardPosB, BoardB, new Vector2(SegX[B], SegX[B] + W));

        var arrival = new GameObject("P-B (arrival)").transform;
        arrival.SetParent(roots[Arrival.seg], false);
        arrival.localPosition = new Vector3(Arrival.x, Arrival.y, 0f);
        var gate = BuildTrigger(roots[GateA.seg], GateA).AddComponent<LevelTransition>();
        SetObj(gate, "destination", arrival);
        var goal = BuildTrigger(roots[GoalB.seg], GoalB).AddComponent<LevelTransition>();
        SetInt(goal, "mode", (int)LevelTransition.Mode.Goal);

        BuildLight(1f, Color.white);
        BuildCamera(playerGo.transform, new[] { SegRect(SegX[A], W), SegRect(SegX[B], W) }, new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud(true);
        SaveScene(scene, Tag, ScenePath);
    }
}
