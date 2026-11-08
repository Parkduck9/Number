using System.Collections;
using UnityEngine;

// STG-00 opening: radio -> Yudo's joke -> the board shows 0 (citizens freeze via the board's listeners) -> exit opens.
// One coroutine, no observers. Phase is public for debugging.
public class OpeningSequence : MonoBehaviour
{
    public enum Phase { Intro, Broadcast, Joke, Freeze, Escape }

    [SerializeField] BroadcastBoard board;
    [SerializeField] GameObject exit;
    [SerializeField] TextAsset line01Radio, line02Talk, line03Radio;
    [SerializeField] float introDelay = 1.5f;   // (T) seconds
    [SerializeField] float broadcastWait = 4f;  // (T) seconds
    [SerializeField] float freezeWait = 1f;     // (T) seconds

    public Phase Current { get; private set; } = Phase.Intro;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(introDelay);

        Current = Phase.Broadcast;
        Play(line01Radio);
        yield return new WaitForSeconds(broadcastWait);

        Current = Phase.Joke;
        Play(line02Talk);
        yield return null; // let the talk box lock input
        while (GameFlow.InputLocked) yield return null;

        Current = Phase.Freeze;
        if (board != null) board.SetAiNumber(0);
        Play(line03Radio);
        yield return new WaitForSeconds(freezeWait);

        Current = Phase.Escape;
        if (exit != null) exit.SetActive(true);
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
