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

    static readonly Color BgColor = new Color32(0x2A, 0x2F, 0x24, 0xFF);
    static readonly Color NpcColor = new Color32(0x8F, 0xBF, 0x6A, 0xFF);
    static readonly Color DoorColor = new Color32(0x5A, 0x4A, 0x3A, 0xFF);

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
    static readonly Box Talk = new Box("TR-TALK", 0, Kind.Solid, 15f, 20f, 1f, 5f);
    static readonly Box Exit = new Box("TR-EXIT", 0, Kind.Solid, 27f, 29.5f, 1f, 5f);

    [MenuItem("NUMBER/Build STG-03")]
    public static void Build()
    {
        if (!Begin(Tag, ScenePath, false)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("ROOM").transform;
        BuildImageBackground(root, "Assets/Art/Backgrounds/bg_stg03_hideout_room.png", RoomWidth, SegHeight, BgColor);
        foreach (var b in Boxes) BuildCollider(root, b, true);
        // px on bg_stg03_hideout_room.png: brazier (warm, flickers), the two old monitors on the left shelf (teal)
        AddLamps(root, RoomWidth, new Lamp(430f, 445f, LampWarm, 5f, 0.25f), new Lamp(105f, 235f, LampTeal, 3f), new Lamp(105f, 360f, LampTeal, 3f));

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(new[] { 0f }, PlayerStart, 1f);
        BuildCheckpoint(root, Checkpoint);

        var luddite = new GameObject("LUDDITE").transform; // DraftIdleAnim swaps the square for the draft idle art (feet at the floor)
        luddite.SetParent(root, false);
        luddite.localPosition = new Vector3(LudditeX, 2f, 0f);
        AddSquare(luddite, "LUDDITE (PLACEHOLDER)", Vector2.zero, new Vector2(1f, 2f), NpcColor, 2);
        luddite.gameObject.AddComponent<DraftIdleAnim>();
        AddSquare(root, "Door (PLACEHOLDER)", new Vector2((Exit.x1 + Exit.x2) * 0.5f, 2.5f), new Vector2(Exit.x2 - Exit.x1, 3f), DoorColor, 1);

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
}
