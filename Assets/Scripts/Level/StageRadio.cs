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
    static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
    {
        { "STG02",
            "@mode radio\n" +
            "helper | 지붕으로 올라가요! 저격수의 붉은 선이 굳으면 쏴요. | Get on the roofs! When a sniper's red line locks, it fires. | 5\n" +
            "yudo | 읽고, 쌓고, 굴린다. 점보는 거랑 같네. | Read, build, roll. Just like telling fortunes. | 4\n" },
        { "STG04",
            "@mode radio\n" +
            "helper | 매일 아침 행운의 숫자를 찍어 내는 공장이에요. 방송 장치는 궁 왕좌실에 있어요. | This factory stamps out the lucky number every morning. The broadcaster is in the throne room. | 6\n" +
            "yudo | 운도 공장에서 찍어 내는군. | So luck comes off a production line. | 4\n" },
        { "STG05",
            "@mode radio\n" +
            "helper | 저 기계가 도시의 운을 쥐고 있어요. 코어가 열리면 쳐요! | That machine holds the city's luck. Hit the core when it opens! | 5\n" +
            "yudo | 네 숫자는 다 읽었어. 이제 내가 굴린다. | I've read all your numbers. My roll now. | 4\n" },
    };

    static readonly HashSet<string> Played = new HashSet<string>(); // once per run: a death reload does not replay it

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
