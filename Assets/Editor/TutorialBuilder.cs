using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build TUTORIAL -> Assets/Scenes/TUTORIAL.unity. Idempotent. Playtest 11-12 (user): a tutorial-only map between STG-00 and STG-01.
// One flat street, laid out in the order Stg01Tutorial teaches: move/jump (crates) -> strike -> parry -> Shift -> rope (a tower too high
// to jump) -> dice (a pair behind the tower). The exit opens when the tutorial ends (or is skipped with Esc).
public static class TutorialBuilder
{
    const string Tag = "TutorialBuilder";
    const string ScenePath = "Assets/Scenes/TUTORIAL.unity";
    const string Lines = "Assets/Data/Dialogue/stg01_tutorial.txt";
    const string ExitArt = "Assets/Art/Backgrounds/STG02_05_Parts/buildings/gate_scanner.png"; // same hanok gate as STG-01
    const int PillarOrder = -104; // as Stg01Builder

    const float W = WideWidth;
    const float Ground = 2.25f; // StreetBackdropKit street surface

    static readonly (string file, float x)[] Steps = { ("crates", 12f) }; // step 1: something to jump on
    static readonly Box[] Boxes =
    {
        new Box("GROUND-T", 0, Kind.Solid, 0f, W, 0f, Ground),
        Building("TOWER-T", 0, 50f, 57f, 9.25f), // step 5: 7u over the street, too high to jump: rope up, drop down the far side
    };
    static readonly Pt PlayerStart = new Pt("P-START", 0, 4f, Ground);
    static readonly Pt[] Checkpoints = { new Pt("CP-T0", 0, 4f, Ground), new Pt("CP-T1", 0, 46f, Ground), new Pt("CP-T2", 0, 60f, Ground) };
    static readonly Foe[] Foes =
    {
        new Foe("E-T1", 0, 21f, Ground, 2, 0f, 5f), // step 2: strike
        new Foe("E-T2", 0, 31f, Ground, 3, 0f, 6f), // step 3: parry (3 beats to read)
        new Foe("E-T3", 0, 41f, Ground, 3, 0f, 7f), // step 4: Shift issen
        new Foe("E-T4", 0, 63f, Ground, 0, 0f, 8f, cap: 3), // step 6: dice pair
        new Foe("E-T5", 0, 66f, Ground, 0, 0f, 8f, cap: 3),
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
        foreach (var f in Foes) BuildEnemy(root, f);
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

        BuildLighting(false);
        SaveScene(scene, Tag, ScenePath);
    }
}
