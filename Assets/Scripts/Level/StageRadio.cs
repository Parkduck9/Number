using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// New story 11-12: a short radio exchange when STG02 / STG04 / STG05 starts (the contact who invited Yudo + Yudo), once per run, so the
// story carries between stages without a hideout stop. Runtime-only (no scene edit). Replaces Stg02DiceGift: Yudo has his die from the start.
public class StageRadio : MonoBehaviour
{
    const float Delay = 1.2f; // s after the stage loads (lets the camera settle / the boss intro start)

    // Same format as the dialogue files (Assets/Data/Dialogue): speaker | Korean | English | seconds. Embedded: those files are not in Resources.
    // Codex concept-only proposal (11-13): P04 after the first auto-roll (STG01 start), P05 rooftops, P06 factory. STG05 has its own
    // boss talk (BossFight), so no radio there.
    static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
    {
        { "STG01",
            "@mode radio\n" +
            "helper | 숫자 안 믿는다며. 주사위는 왜 챙겼어? | You don't trust numbers. Why bring a die? | 4\n" +
            "yudo | 찍어 주는 숫자는. 굴릴 판은 내가 만들지. | Not numbers I'm handed. I earn my rolls. | 4\n" },
        { "STG02",
            "@mode radio\n" +
            "helper | 지붕 위에 저격수다. | Sniper on the roof. | 3\n" +
            "yudo | 봤어. 지붕이 길보다 바쁘네. | I see them. Roofs are busier than the streets. | 3.5\n" },
        { "STG04",
            "@mode radio\n" +
            "helper | 방송을 내보내는 AI는 궁 안에 있어. | The AI behind the broadcast is in the palace. | 4\n" +
            "yudo | 왕 노릇도 하나 보네. | Playing king, too. | 3\n" },
    };

    static readonly HashSet<string> Played = new HashSet<string>(); // once per run: a death reload does not replay it

    // Once per run (cleared on the title like the stage radio): BossFight's intro talk is not replayed after a death reload.
    public static bool FirstTime(string key) => Played.Add(key);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { SceneManager.sceneLoaded -= OnSceneLoaded; Played.Clear(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == StageFlow.TitleScene) { Played.Clear(); return; } // a new run from the title plays them again
        if (!Lines.ContainsKey(scene.name) || Played.Contains(scene.name)) return;
        Played.Add(scene.name);
        var go = new GameObject("StageRadio").AddComponent<StageRadio>();
        SceneManager.MoveGameObjectToScene(go.gameObject, scene);
        go.StartCoroutine(go.Play(scene.name));
    }

    IEnumerator Play(string scene)
    {
        float t = Time.time + Delay;
        while (Time.time < t || GameFlow.InputLocked) yield return null;
        if (DialoguePlayer.Instance != null) DialoguePlayer.Instance.Play(new TextAsset(Lines[scene]));
        else Debug.LogWarning($"[StageRadio] {scene}: no DialoguePlayer in the scene; radio skipped.");
    }
}
