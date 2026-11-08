using System.Collections;
using UnityEngine;

// STG-03: walk up to the Luddite, talk (blocking), receive the shockwave device, the exit door opens. Once only.
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
        Shockwave.SetUnlocked(true);
        ControlHints.Show("Shockwave"); // Space icon + what it does, a few playable seconds
        if (exit != null) exit.SetActive(true);
    }
}
