using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static StageBuildKit;

// Menu NUMBER/Build Sandbox scene -> Assets/Scenes/Sandbox.unity (not added to Build Settings).
// One flat 160u floor, six labelled stations left to right (F1..F6 teleport, see SandboxHarness). Edit the tables and re-run.
public static class SandboxBuilder
{
    const string Tag = "SandboxBuilder";
    const string ScenePath = "Assets/Scenes/Sandbox.unity";
    const float W = 160f, FloorTop = 1f;

    static readonly Box[] Boxes = new[] { new Box("GROUND", 0, Kind.Solid, 0f, W, 0f, FloorTop) }.Concat(Walls("S", 0, W, SegHeight)).ToArray();

    // Station signs: x = centre, EN / KO.
    static readonly (float x, string en, string ko)[] Signs =
    {
        (22f, "1  BASICS", "기초"),
        (50f, "2  SNIPERS + RELAY", "저격수 + 중계"),
        (74f, "3  ANDROIDS + BOARD", "안드로이드 + 방송판"),
        (100f, "4  ELITE CLASH", "엘리트 대결"),
        (122f, "5  RMB TARGETS", "우클릭 표적"),
        (146f, "6  DICE CROWD", "주사위 군중"),
    };

    // Foe(id, seg, x, feetY, fixedNumber, progress, detect, android, ranged, cap, bug, sniper, elite, minNumber)
    static readonly Foe[] Foes =
    {
        // 1 basics: melee citizens
        new Foe("C1-1", 0, 16f, FloorTop, 2, 0f, 14f),
        new Foe("C1-2", 0, 21f, FloorTop, 3, 0f, 14f),
        new Foe("C1-3", 0, 26f, FloorTop, 4, 0f, 14f),
        // 2 snipers + relay: citizens stand on the shot path between the snipers (right) and the player (F2 at x 36)
        new Foe("S2-1", 0, 58f, FloorTop, 1, 0f, 25f, sniper: true),
        new Foe("S2-2", 0, 63f, FloorTop, 1, 0f, 25f, sniper: true),
        new Foe("C2-1", 0, 46f, FloorTop, 3, 0f, 6f),
        new Foe("C2-2", 0, 51f, FloorTop, 3, 0f, 6f),
        // 3 androids (+ board at x 66)
        new Foe("A3-1", 0, 76f, FloorTop, 3, 0f, 14f, android: true),
        new Foe("A3-2", 0, 81f, FloorTop, 7, 0f, 14f, android: true),
        new Foe("C3-1", 0, 71f, FloorTop, 0, 0f, 12f, cap: 4),
        // 4 elite clash
        new Foe("E4-1", 0, 100f, FloorTop, 0, 0f, 16f, cap: 5, elite: true, minNumber: 5),
        // 5 RMB issen targets: row of humans + one android
        new Foe("T5-1", 0, 112f, FloorTop, 3, 0f, 2f),
        new Foe("T5-2", 0, 116f, FloorTop, 3, 0f, 2f),
        new Foe("T5-3", 0, 120f, FloorTop, 3, 0f, 2f),
        new Foe("T5-4", 0, 124f, FloorTop, 3, 0f, 2f),
        new Foe("T5-5", 0, 128f, FloorTop, 3, 0f, 2f),
        new Foe("T5-A", 0, 132f, FloorTop, 3, 0f, 2f, android: true),
        // 6 dice crowd (view x 125..155 when teleported to x 140, plus the right end)
        new Foe("D6-1", 0, 131f, FloorTop, 3, 0f, 20f),
        new Foe("D6-2", 0, 135f, FloorTop, 3, 0f, 20f),
        new Foe("D6-3", 0, 138f, FloorTop, 1, 0f, 20f, ranged: true),
        new Foe("D6-4", 0, 143f, FloorTop, 3, 0f, 20f, android: true),
        new Foe("D6-5", 0, 146f, FloorTop, 1, 0f, 20f, ranged: true, bug: true),
        new Foe("D6-6", 0, 149f, FloorTop, 3, 0f, 20f),
        new Foe("D6-7", 0, 152f, FloorTop, 1, 0f, 25f, sniper: true),
        new Foe("D6-8", 0, 155f, FloorTop, 0, 0f, 20f, cap: 5, elite: true, minNumber: 4),
    };

    [MenuItem("NUMBER/Build Sandbox scene")]
    public static void Build()
    {
        if (!Begin(Tag, ScenePath)) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("SANDBOX").transform;

        foreach (var b in Boxes) BuildCollider(root, b, b.id == "GROUND");

        var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(Player);
        playerGo.transform.position = new Vector3(6f, FloorTop + 1f, 0f);
        foreach (var f in Foes) BuildEnemy(root, f);

        BuildBoard(root, "BOARD-3", new Vector2(66f, 3.5f), 3, new Vector2(58f, 88f));
        foreach (var s in Signs) BuildSign(root, s.x, s.en + "  /  " + s.ko);

        BuildCamera(playerGo.transform, new[] { SegRect(0f, W, SegHeight) }, new Color32(0x0B, 0x0F, 0x1E, 0xFF));
        AddByName("RespawnManager", new GameObject("RespawnManager"), "RespawnManager");
        BuildHud();
        new GameObject("SandboxHarness").AddComponent<SandboxHarness>();
        BuildLighting(false);
        SaveScene(scene, Tag, ScenePath);
    }

    static void BuildSign(Transform root, float x, string text)
    {
        var go = new GameObject("SIGN " + text);
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(x, 7.5f, 0f);
        var t = go.AddComponent<TextMeshPro>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Mulmaru/Mulmaru SDF.asset");
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = 4f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color32(0xE8, 0xD0, 0x5A, 0xFF);
        t.sortingOrder = -40;
        ((RectTransform)go.transform).sizeDelta = new Vector2(20f, 2f);
    }
}
