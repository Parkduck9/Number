using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build TUTORIAL -> Assets/Scenes/TUTORIAL.unity. Idempotent. Playtest 11-12 (user): a tutorial-only map between STG-00 and STG-01.
// One flat street, laid out in the order Stg01Tutorial teaches (see the data below). The exit opens when the tutorial ends (or Esc skips it).
public static class TutorialBuilder
{
    const string Tag = "TutorialBuilder";
    const string ScenePath = "Assets/Scenes/TUTORIAL.unity";
    const string Lines = "Assets/Data/Dialogue/stg01_tutorial.txt";
    const string ExitArt = "Assets/Art/Backgrounds/STG02_05_Parts/buildings/gate_scanner.png"; // same hanok gate as STG-01
    const int PillarOrder = -104; // as Stg01Builder

    const float W = WideWidth;
    const float Ground = 2.25f; // StreetBackdropKit street surface

    // Playtest 11-12 (user): one enemy per step, every move forced. Each step's area is closed by a barrier (GATE-n) that opens when the
    // step is done; the step's enemy is Guarded (Stg01Tutorial) so only the taught move beats it.
    //   1 move (crates) | 2 strike: plain enemy | 3 Shift issen: plain enemy | 4 parry: android | 5 rope: tower | 6 dice: one enemy -> exit
    static readonly (string file, float x)[] Steps = { ("crates", 10f) }; // step 1: something to jump on
    static readonly Box[] Boxes =
    {
        new Box("GROUND-T", 0, Kind.Solid, 0f, W, 0f, Ground),
        Building("TOWER-T", 0, 51f, 58f, 9.25f), // step 5: 7u over the street, too high to jump: rope up, drop down the far side
    };
    static readonly float[] GateX = { 15f, 26f, 37f, 48f }; // after steps 1..4 (step 5 = the tower itself, step 6 = the exit)
    static readonly Color GateColor = new Color(0.3f, 0.88f, 1f, 0.45f); // placeholder barrier
    static readonly Pt PlayerStart = new Pt("P-START", 0, 4f, Ground);
    static readonly Pt[] Checkpoints =
    {
        new Pt("CP-T0", 0, 4f, Ground), new Pt("CP-T1", 0, 17f, Ground), new Pt("CP-T2", 0, 28f, Ground),
        new Pt("CP-T3", 0, 39f, Ground), new Pt("CP-T4", 0, 50f, Ground), new Pt("CP-T5", 0, 60f, Ground),
    };
    // index = step (0-based); null = that step has no enemy
    static readonly Foe?[] Foes =
    {
        null,
        new Foe("E-T1", 0, 21f, Ground, 2, 0f, 5f),                 // strike
        new Foe("E-T2", 0, 32f, Ground, 3, 0f, 5f),                 // Shift issen
        new Foe("A-T3", 0, 43f, Ground, 3, 0f, 5f, android: true),  // parry (3 beats to read)
        null,
        new Foe("E-T5", 0, 65f, Ground, 3, 0f, 7f),                 // dice
    };
    static readonly Box Exit = new Box("TR-EXIT", 0, Kind.Solid, 70f, 73f, Ground, 6f);

    [MenuItem("NUMBER/Build TUTORIAL")]
    public static void Build()
    {
        if (!Begin(Tag, ScenePath)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("SEG-T").transform;
        StreetBackdropKit.Build(root, W, SegHeight, Ground, 15f, W - 15f, Steps, hideTwinTower: true);
        foreach (var b in Boxes) BuildCollider(root, b, false);
        foreach (var w in Walls("T", 0, W)) BuildCollider(root, w, false);
        PartsBackdropKit.PlatformArt(root, Boxes[1], 0f, null, PillarOrder);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(new[] { 0f }, PlayerStart, 1f);
        var targets = new TestEnemy[Foes.Length];
        for (int k = 0; k < Foes.Length; k++) if (Foes[k].HasValue) targets[k] = BuildEnemy(root, Foes[k].Value).GetComponent<TestEnemy>();
        var gates = new GameObject[GateX.Length];
        for (int k = 0; k < GateX.Length; k++)
        {
            gates[k] = BuildCollider(root, new Box("GATE-" + (k + 1), 0, Kind.Solid, GateX[k] - 0.15f, GateX[k] + 0.15f, Ground, SegHeight + 2f), false);
            AddSquare(gates[k].transform, "Barrier (PLACEHOLDER)", Vector2.zero, new Vector2(0.25f, SegHeight + 2f - Ground), GateColor, 5);
        }
        foreach (var c in Checkpoints) BuildCheckpoint(root, c);

        var exit = BuildTrigger(root, Exit);
        SetInt(exit.AddComponent<LevelTransition>(), "mode", (int)LevelTransition.Mode.Goal);
        exit.SetActive(false); // Stg01Tutorial opens it
        var gate = AssetDatabase.LoadAssetAtPath<Sprite>(ExitArt);
        if (gate != null)
        {
            var g = new GameObject("TR-EXIT (gate art)");
            g.transform.SetParent(root, false);
            g.transform.localPosition = new Vector3((Exit.x1 + Exit.x2) * 0.5f, Exit.y1, 0f);
            var sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = gate;
            sr.sortingOrder = -1;
        }
        else Todo.Add("exit gate sprite missing: " + ExitArt);

        BuildCamera(playerGo.transform, new[] { SegRect(0f, W) }, new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud(false);
        BuildDialoguePlayer();

        var tut = new GameObject("TUTORIAL").AddComponent<Stg01Tutorial>();
        var lines = AssetDatabase.LoadAssetAtPath<TextAsset>(Lines);
        if (lines == null) Todo.Add("tutorial line file missing: " + Lines);
        SetObj(tut, "lines", lines);
        SetObj(tut, "exit", exit);
        var so = new SerializedObject(tut);
        var gp = so.FindProperty("gates");
        gp.arraySize = gates.Length;
        for (int k = 0; k < gates.Length; k++) gp.GetArrayElementAtIndex(k).objectReferenceValue = gates[k];
        var tp = so.FindProperty("targets");
        tp.arraySize = targets.Length;
        for (int k = 0; k < targets.Length; k++) tp.GetArrayElementAtIndex(k).objectReferenceValue = targets[k];
        so.ApplyModifiedPropertiesWithoutUndo();

        BuildLighting(false);
        SaveScene(scene, Tag, ScenePath);
    }
}
