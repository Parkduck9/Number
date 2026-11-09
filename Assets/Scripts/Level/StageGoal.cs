using UnityEngine;

// Goal trigger: the player touching it clears the stage and loads the next one (StageFlow).
[RequireComponent(typeof(Collider2D))]
public class StageGoal : MonoBehaviour
{
    [SerializeField] float delay = 0.5f; // (T) seconds of locked input before the switch

    float loadAt = -1f;
    PlayerController player;

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (loadAt >= 0f || pc == null || GameFlow.IsPlayerDead(pc)) return;
        if (!StageClear.TryClear()) return; // enemies left / spawner unbroken
        player = pc;
        GameFlow.InputLocked = true;
        loadAt = Time.unscaledTime + delay;
    }

    void Update()
    {
        if (loadAt < 0f) return;
        if (GameFlow.IsPlayerDead(player)) { loadAt = -1f; GameFlow.InputLocked = false; return; } // died during the delay: the respawn reload takes over
        if (Time.unscaledTime < loadAt) return;
        loadAt = -1f;
        if (!StageFlow.LoadNext()) GameFlow.InputLocked = false; // nothing to load: give control back
    }
}
