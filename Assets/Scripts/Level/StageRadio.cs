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
            "helper | 지붕 위로 올라가요! 저격수 붉은 선을 읽어요. 선이 굳으면 쏩니다. | Get up on the roofs! Read the snipers' red lines. When a line locks, it fires. | 6\n" +
            "yudo | 읽고, 쌓고, 굴린다. 점괘 보는 거랑 똑같네. | Read, build up, roll. Same as reading a fortune. | 4\n" },
        { "STG04",
            "@mode radio\n" +
            "helper | 여기가 매일 아침 행운의 숫자를 찍어 내는 공장이에요. 방송 장치는 궁 안 왕좌실에 있어요. | This is the factory that stamps out the lucky number every morning. The broadcast device is in the palace throne room. | 6\n" +
            "yudo | 숫자를 찍어 낸다라… 운도 공장에서 나오는 줄은 몰랐네. | Stamping out numbers... didn't know luck came off a production line. | 5\n" },
        { "STG05",
            "@mode radio\n" +
            "helper | 저게 도시의 운을 혼자 쥔 기계예요. 코어가 열릴 때를 노려요! | That machine holds the whole city's luck. Strike when the core opens! | 5\n" +
            "yudo | 네 숫자는 다 읽었어. 이제 내가 굴릴 차례야. | I've read all your numbers. Now it's my turn to roll. | 4\n" },
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
