using System.Collections;
using UnityEngine;

// STG-03: walk up to the Luddite, talk (blocking), the exit door opens. The lucky dice now come at the STG-02 start (Stg02DiceGift). Once only.
[RequireComponent(typeof(BoxCollider2D))]
public class LudditeMeeting : MonoBehaviour
{
    [SerializeField] TextAsset talk;
    [SerializeField] GameObject exit;

    bool done;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (done || other.GetComponentInParent<PlayerController>() == null) return;
        done = true;
        if (DialoguePlayer.Instance != null && talk != null) DialoguePlayer.Instance.Play(talk);
        else Debug.LogWarning("[LudditeMeeting] No DialoguePlayer or talk file; talk skipped.");
        StartCoroutine(Finish());
    }

    IEnumerator Finish()
    {
        yield return null; // let the talk box lock input
        while (GameFlow.InputLocked) yield return null;
        if (!LuckyDice.Unlocked)
        {
            LuckyDice.SetUnlocked(true); // safety net when STG-03 is entered directly (debug / skipped STG-02)
            ControlHints.Show("LuckyDice");
        }
        if (exit != null) exit.SetActive(true);
    }
}
