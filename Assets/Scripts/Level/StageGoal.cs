using UnityEngine;

// Goal trigger: the player touching it clears the stage and loads the next one (StageFlow).
[RequireComponent(typeof(Collider2D))]
public class StageGoal : MonoBehaviour
{
    [SerializeField] float delay = 0.5f; // (T) seconds of locked input before the switch

    float loadAt = -1f;

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (loadAt >= 0f || other.GetComponentInParent<PlayerController>() == null) return;
        GameFlow.InputLocked = true;
        loadAt = Time.unscaledTime + delay;
    }

    void Update()
    {
        if (loadAt < 0f || Time.unscaledTime < loadAt) return;
        loadAt = -1f;
        if (!StageFlow.LoadNext()) GameFlow.InputLocked = false; // nothing to load: give control back
    }
}
