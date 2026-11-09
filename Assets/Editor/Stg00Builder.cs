using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build STG-00 -> Assets/Scenes/STG00.unity. Idempotent. Opening: one segment, no combat, scripted by OpeningSequence.
// Reuses the layered STG-01 street backdrop (StreetBackdropKit).
public static class Stg00Builder
{
    const string Tag = "Stg00Builder";
    const string ScenePath = "Assets/Scenes/STG00.unity";
    const string DialogueDir = "Assets/Data/Dialogue/";

    // (T) day profile, LIGHTING_DESIGN Q7
    static readonly Color SkyColor = new Color32(0x9F, 0xC6, 0xE0, 0xFF);

    // Same colliders as STG-01 SEG-A, invisible (the background image is the ground).
    static readonly Box Ground = new Box("GROUND-A", 0, Kind.Solid, 0f, SegWidth, 0f, 2.25f);
    static readonly Pt PlayerStart = new Pt("P-START", 0, 6f, 2.25f);
    static readonly Vector2 BoardPos = new Vector2(33.7f, 11.8f);
    static readonly float[] CitizenX = { 12f, 16f, 22f, 27f, 31f, 40f };
    static readonly Color CitizenColor = new Color32(0xC9, 0xA2, 0x7E, 0xFF);
    static readonly Box Exit = new Box("TR-EXIT", 0, Kind.Solid, 47.5f, SegWidth, 2.25f, 6f);

    [MenuItem("NUMBER/Build STG-00")]
    public static void Build()
    {
        if (!Begin(Tag, ScenePath, false)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("SEG-A").transform;
        StreetBackdropKit.Build(root, SegWidth, SegHeight, 2.25f, 15f, SegWidth - 15f); // one screen tall: the camera never moves up
        BuildCollider(root, Ground, false);
        foreach (var w in Walls("A", 0)) BuildCollider(root, w, false);

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = World(new[] { 0f }, PlayerStart, 1f);

        var board = BuildBoard(root, "BOARD-A", BoardPos, 1, new Vector2(0f, SegWidth));
        foreach (float x in CitizenX)
        {
            var c = new GameObject("CITIZEN").transform; // OpeningCitizen swaps the square child for the draft art (feet at the ground)
            c.SetParent(root, false);
            c.localPosition = new Vector3(x, 2.25f + 1f, 0f);
            AddSquare(c, "CITIZEN (PLACEHOLDER)", Vector2.zero, new Vector2(1f, 2f), CitizenColor, 2);
            c.gameObject.AddComponent<OpeningCitizen>();
        }

        var exit = BuildTrigger(root, Exit);
        SetInt(exit.AddComponent<LevelTransition>(), "mode", (int)LevelTransition.Mode.Goal);
        exit.SetActive(false); // OpeningSequence opens it

        var seq = new GameObject("OpeningSequence").AddComponent<OpeningSequence>();
        SetObj(seq, "board", board);
        SetObj(seq, "exit", exit);
        SetObj(seq, "line01Radio", LoadLine("stg00_line01_placeholder.txt"));
        SetObj(seq, "line02Talk", LoadLine("stg00_line02_placeholder.txt"));
        SetObj(seq, "line03Radio", LoadLine("stg00_line03_placeholder.txt"));

        BuildCamera(playerGo.transform, new[] { SegRect(0f) }, SkyColor);
        BuildDialoguePlayer(); // no HUD, no RespawnManager, no checkpoints: nothing can hurt the player here
        BuildLighting(true); // the only daytime stage (LIGHTING Q7): Day profile, neon off
        SaveScene(scene, Tag, ScenePath);
    }

    static TextAsset LoadLine(string file)
    {
        var t = AssetDatabase.LoadAssetAtPath<TextAsset>(DialogueDir + file);
        if (t == null) Todo.Add("Dialogue file missing: " + DialogueDir + file);
        return t;
    }
}
