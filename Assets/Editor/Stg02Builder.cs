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

    // Segments at world x 0 and 60. Solid placeholder backgrounds (real art comes later).
    static readonly float[] SegX = { 0f, 60f };
    static readonly string[] SegId = { "SEG-A", "SEG-B" };
    static readonly Color[] SegColor = { new Color32(0x2B, 0x24, 0x40, 0xFF), new Color32(0x1F, 0x2A, 0x3A, 0xFF) };

    // Colliders (all drawn as placeholder squares). OneWay y2 = top surface.
    static readonly Box[] Boxes =
    {
        new Box("GROUND-A", A, Kind.Solid, 0f, 50.625f, 0f, 1.0f),
        new Box("ROOF-A1",  A, Kind.OneWay, 6f,  16f,   2.25f, 2.5f),
        new Box("ROOF-A2",  A, Kind.OneWay, 18f, 28f,   3.55f, 3.8f),
        new Box("ROOF-A3",  A, Kind.OneWay, 31f, 40f,   2.25f, 2.5f),
        new Box("GROUND-B", B, Kind.Solid, 0f, 50.625f, 0f, 1.0f),
        new Box("STEP-B1",  B, Kind.OneWay, 3f, 6f,   2.25f, 2.5f),
        new Box("STEP-B2",  B, Kind.OneWay, 5f, 8f,   3.55f, 3.8f),
        new Box("RAIL-B",   B, Kind.OneWay, 8f, 44f,  4.75f, 5.0f),
    };

    static readonly Pt PlayerStart = new Pt("P-START", A, 3f, 1.0f);
    static readonly Pt Arrival = new Pt("P-B", B, 2f, 1.0f);
    static readonly Pt[] Checkpoints = { new Pt("CP-A0", A, 3f, 1.0f), new Pt("CP-B0", B, 2f, 1.0f) };

    static readonly Foe[] Foes =
    {
        new Foe("E-A1", A, 22f, 3.8f, 0, 0.4f, 8f), // on the roof
        new Foe("E-A2", A, 34f, 1.0f, 0, 0.4f, 8f),
        new Foe("E-A3", A, 37f, 1.0f, 0, 0.4f, 8f), // two at once
        new Foe("E-B1", B, 20f, 5.0f, 0, 0.6f, 8f), // on the rail
        new Foe("E-B2", B, 30f, 1.0f, 0, 0.6f, 8f),
        new Foe("E-B3", B, 34f, 1.0f, 0, 0.6f, 8f),
        new Foe("E-B4", B, 40f, 5.0f, 0, 0.6f, 8f), // on the rail
    };

    // Billboards (segment-local center): A shows 3 (review), B shows 7 (first time). Zone = the whole segment.
    static readonly Vector2 BoardPos = new Vector2(25f, 11f);
    const int BoardA = 3, BoardB = 7;

    static readonly Box GateA = new Box("TR-A-GATE", A, Kind.Solid, 47f, 50.6f, 1f, 5f);
    static readonly Box GoalB = new Box("TR-B-GOAL", B, Kind.Solid, 46f, 50f, 1f, 5f);

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
            BuildSolidBackground(roots[i], SegWidth, SegHeight, SegColor[i]);
            foreach (var w in Walls(i == A ? "A" : "B", i)) BuildCollider(roots[i], w, true);
        }
        foreach (var b in Boxes) BuildCollider(roots[b.seg], b, true);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(SegX, PlayerStart, 1f);

        foreach (var f in Foes) BuildEnemy(roots[f.seg], f);
        foreach (var c in Checkpoints) BuildCheckpoint(roots[c.seg], c);

        BuildBoard(roots[A], "BOARD-A", BoardPos, BoardA, new Vector2(SegX[A], SegX[A] + SegWidth));
        BuildBoard(roots[B], "BOARD-B", BoardPos, BoardB, new Vector2(SegX[B], SegX[B] + SegWidth));

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
        BuildHud(false); // hack tool is obtained in STG-03
        SaveScene(scene, Tag, ScenePath);
    }
}
