using System.Collections;
using UnityEngine;

// STG-00 opening: radio shows 0 -> Yudo's joke -> short silence -> BREATH ZERO (lights + sound cut, citizens freeze one beat apart) -> exit opens.
// One coroutine, no observers. Phase is public for debugging.
public class OpeningSequence : MonoBehaviour
{
    public enum Phase { Intro, Broadcast, Joke, Freeze, Escape }

    [SerializeField] BroadcastBoard board;
    [SerializeField] GameObject exit;
    [SerializeField] TextAsset line01Radio, line02Talk, line03Radio;
    [SerializeField] float introDelay = 1.5f;   // (T) seconds
    [SerializeField] float broadcastWait = 4f;  // (T) seconds
    [SerializeField] float silenceDelay = 0.3f; // (T) seconds between the joke closing and the cut
    [SerializeField] float freezeStagger = 0.25f; // (T) seconds between one citizen freezing and the next (nearest to the board first)
    [SerializeField] float freezeWait = 1f;     // (T) seconds from the cut to the exit opening
    [SerializeField] bool breathZero = true;    // false = no blackout / mute (citizens still freeze)

    public Phase Current { get; private set; } = Phase.Intro;

    IEnumerator Start()
    {
        // The radio's 0 would trigger GameAudio's own breath stop (chime, then silence) before the joke; the cut belongs to BreathZero.
        if (breathZero) GameAudio.BreathStopOnZero = false;
        yield return new WaitForSeconds(introDelay);

        Current = Phase.Broadcast;
        if (board != null) board.SetAiNumber(0); // the 0 is on screen before the lights go: it is what stays lit
        Play(line01Radio);
        yield return new WaitForSeconds(broadcastWait);
        while (DialoguePlayer.Instance != null && DialoguePlayer.Instance.RadioBusy) yield return null; // 11-13: the broadcast is two lines now

        Current = Phase.Joke;
        Play(line02Talk);
        yield return null; // let the talk box lock input
        while (GameFlow.InputLocked) yield return null;
        yield return new WaitForSeconds(silenceDelay);

        Current = Phase.Freeze;
        if (breathZero) gameObject.AddComponent<BreathZero>().Play(); // silence hook: BreathZero calls GameAudio.BreathStop
        GameAudio.BreathStopOnZero = true;
        FreezeCitizens();
        Play(line03Radio);
        yield return new WaitForSeconds(freezeWait);

        Current = Phase.Escape;
        if (exit != null) exit.SetActive(true);
    }

    void OnDestroy() { GameAudio.BreathStopOnZero = true; } // left the scene mid-opening

    // Citizens freeze in a wave spreading from the board.
    void FreezeCitizens()
    {
        var citizens = FindObjectsByType<OpeningCitizen>(FindObjectsSortMode.None);
        float bx = board != null ? board.transform.position.x : 0f;
        System.Array.Sort(citizens, (a, b) => Mathf.Abs(a.transform.position.x - bx).CompareTo(Mathf.Abs(b.transform.position.x - bx)));
        for (int i = 0; i < citizens.Length; i++) citizens[i].Freeze(i * freezeStagger);
    }

    static void Play(TextAsset file)
    {
        if (DialoguePlayer.Instance == null || file == null)
        {
            Debug.LogWarning("[OpeningSequence] No DialoguePlayer or line file; line skipped.");
            return;
        }
        DialoguePlayer.Instance.Play(file);
    }
}
