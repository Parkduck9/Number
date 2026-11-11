using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build STG-03 -> Assets/Scenes/STG03.unity. Idempotent. One room (30 x 16.875), dialogue only: meet the Luddite, get the device, exit.
public static class Stg03Builder
{
    const string Tag = "Stg03Builder";
    const string ScenePath = "Assets/Scenes/STG03.unity";
    const string TalkPath = "Assets/Data/Dialogue/stg03_luddite_placeholder.txt";
    const float RoomWidth = 30f;

    // Room-local coordinates (world x 0). The room is narrower than a normal segment.
    static readonly Box[] Boxes =
    {
        new Box("GROUND-A", 0, Kind.Solid, 0f, RoomWidth, 0f, 1f),
        new Box("WALL-A-L", 0, Kind.Solid, -1f, 0f, 0f, SegHeight),
        new Box("WALL-A-R", 0, Kind.Solid, RoomWidth, RoomWidth + 1f, 0f, SegHeight),
    };
    static readonly Pt PlayerStart = new Pt("P-START", 0, 3f, 1f);
    static readonly Pt Checkpoint = new Pt("CP-A0", 0, 3f, 1f);
    const float LudditeX = 18f;
    static readonly string[] SegId = { "ROOM" };
    // DECISIONS 22: one elite before the meeting (4~7). The exit stays closed until it is broken (Goal rule: all enemies dead).
    static readonly Foe[] Foes = { new Foe("ELITE-A1", 0, 10f, 1.0f, 0, 0.5f, 6f, cap: 7, elite: true, minNumber: 4) };
    static readonly Box Talk = new Box("TR-TALK", 0, Kind.Solid, 15f, 20f, 1f, 5f);
    static readonly Box Exit = new Box("TR-EXIT", 0, Kind.Solid, 27f, 29.5f, 1f, 5f);

    [MenuItem("NUMBER/Build STG-03")]
    public static void Build()
    {
        if (!Begin(Tag, ScenePath)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("ROOM").transform;
        PartsBackdropKit.Hideout(root, RoomWidth, 1f); // layered parts (DECISIONS 17); one fixed screen
        foreach (var b in Boxes) BuildCollider(root, b, b.id != "GROUND-A"); // floor: the wall plate's dark floor + its fill below (audit #9)

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(new[] { 0f }, PlayerStart, 1f);
        BuildCheckpoint(root, Checkpoint);

        BuildHelper(root, "LUDDITE", LudditeX, 1f); // LudditeHelper swaps the square for the helper idle art (feet at the floor)
        foreach (var f in Foes) BuildEnemy(root, f);
        // exit door (audit #9): the scanner gate, doors on the trigger (x 28.25); its right wing runs behind the room wall, off screen
        PartsBackdropKit.ExitArt(root, "buildings/gate_scanner", null, 0f, (Exit.x1 + Exit.x2) * 0.5f, Exit.y1);

        var exit = BuildTrigger(root, Exit);
        SetInt(exit.AddComponent<LevelTransition>(), "mode", (int)LevelTransition.Mode.Goal);
        exit.SetActive(false); // LudditeMeeting opens it

        var meeting = BuildTrigger(root, Talk).AddComponent<LudditeMeeting>();
        var talk = AssetDatabase.LoadAssetAtPath<TextAsset>(TalkPath);
        if (talk == null) Todo.Add("Talk file missing: " + TalkPath);
        SetObj(meeting, "talk", talk);
        SetObj(meeting, "exit", exit);

        BuildCamera(playerGo.transform, new[] { new Rect(0f, 0f, RoomWidth, SegHeight) }, new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "Card A RespawnManager");
        BuildHud(false);
        BuildDialoguePlayer();
        BuildLighting(false);
        SaveScene(scene, Tag, ScenePath);
    }

    // Replaces the old Luddite square with the helper NPC and adds the elite in the saved scene (no full rebuild).
    [MenuItem("NUMBER/STG-03: place helper + elite only")]
    public static void PlaceHelperAndElite()
    {
        if (!Begin(Tag, ScenePath)) return;
        var scene = OpenAndPlaceElites(Tag, ScenePath, SegId, Foes);
        var root = GameObject.Find("ROOM").transform;
        var old = root.Find("LUDDITE");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        BuildHelper(root, "LUDDITE", LudditeX, 1f);
        AssignSortingLayers();
        SaveScene(scene, Tag, ScenePath);
    }
}
