using UnityEngine;

// Plays a line file once when the player enters this trigger.
[RequireComponent(typeof(BoxCollider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] TextAsset lines;
    bool played;

    void Reset() { GetComponent<BoxCollider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (played || lines == null || other.GetComponentInParent<PlayerController>() == null) return;
        if (DialoguePlayer.Instance == null) return;
        played = true;
        DialoguePlayer.Instance.Play(lines);
    }
}
