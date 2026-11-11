using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// STG-02 start: the Luddite helper waits a few units ahead of the player and hands over the lucky dice (was the STG-03 hideout talk).
// Runtime-only (no scene edit): spawned when STG02 loads while the dice are still locked. LuckyDice.Unlocked is static, so a death reload never repeats it.
public class Stg02DiceGift : MonoBehaviour
{
    const string SceneName = "STG02";
    const float AheadX = 6f, TalkRange = 2.5f;

    // Same format as the dialogue files (Assets/Data/Dialogue); embedded because those files are not in Resources.
    const string Talk =
        "@mode talk\n" +
        "helper | 거기 서. 나도 한때 숫자를 쫓던 사람이야. 이걸 가져가. | Hold on. I chased numbers once myself. Take this.\n" +
        "helper | 행운의 주사위야. 적을 일곱 번 베면 한 번 굴릴 수 있어. | A lucky die. Seven hits on enemies charge one roll.\n" +
        "helper | E를 누르면 굴러. 나온 눈이 곧 네 도술이야. | Press E to roll. The face you roll is the spell you cast.\n" +
        "yudo | 숫자를 직접 굴린다라. 마음에 들어요. | Rolling my own number. I like that.\n";

    PlayerController player;
    bool talked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => SceneManager.sceneLoaded -= OnSceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName || LuckyDice.Unlocked || FindFirstObjectByType<Stg02DiceGift>() != null) return; // Init + sceneLoaded can both see the first scene
        SceneManager.MoveGameObjectToScene(new GameObject("Stg02DiceGift").AddComponent<Stg02DiceGift>().gameObject, scene);
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player != null) SpawnHelper(player.transform.position);
            return;
        }
        if (talked) return;
        var helper = transform.GetChild(0);
        if (Mathf.Abs(player.transform.position.x - helper.position.x) > TalkRange) return;
        talked = true;
        StartCoroutine(Gift());
    }

    void SpawnHelper(Vector3 playerPos)
    {
        float x = playerPos.x + AheadX, floor = playerPos.y - 1f; // fallback: roughly the player's feet
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, playerPos.y + 3f), Vector2.down, 20f, LayerMask.GetMask("Ground"));
        if (hit.collider != null) floor = hit.point.y;

        var go = new GameObject("LUDDITE-HELPER");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x, floor + 1f, 0f);
        var art = new GameObject("Art (PLACEHOLDER)").AddComponent<SpriteRenderer>(); // LudditeHelper swaps in the idle strip (positions it at the feet)
        art.transform.SetParent(go.transform, false);
        art.sortingLayerName = "Characters";
        art.sortingOrder = 2;
        go.AddComponent<LudditeHelper>();
    }

    IEnumerator Gift()
    {
        if (DialoguePlayer.Instance != null) DialoguePlayer.Instance.Play(new TextAsset(Talk));
        else Debug.LogWarning("[Stg02DiceGift] No DialoguePlayer; talk skipped.");
        yield return null; // let the talk box lock input
        while (GameFlow.InputLocked) yield return null;
        LuckyDice.SetUnlocked(true);
        ControlHints.Show("LuckyDice"); // E icon + what it does, a few playable seconds
    }
}
