using UnityEngine;

// Goal trigger: the player touching it clears the stage and loads the next one (StageFlow).
[RequireComponent(typeof(Collider2D))]
public class StageGoal : MonoBehaviour
{
    [SerializeField] float delay = 0.5f; // (T) seconds of locked input before the switch

    float loadAt = -1f, nextCheckAt, nextOpenCheckAt;
    PlayerController player;
    GameObject openFx; // looping pad effect, shown once the exit is open (all enemies dead + spawners broken)

    void Awake() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other) => Touch(other, true);

    // Standing in the goal after a refusal: re-check every 0.2 s (the last enemy may die / the spawner break meanwhile), no hint spam.
    void OnTriggerStay2D(Collider2D other)
    {
        if (Time.unscaledTime < nextCheckAt) return;
        nextCheckAt = Time.unscaledTime + 0.2f;
        Touch(other, false);
    }

    void Touch(Collider2D other, bool hint)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (loadAt >= 0f || pc == null || GameFlow.IsPlayerDead(pc)) return;
        if (!StageClear.TryClear(transform.position, hint)) return; // enemies left / spawner unbroken
        player = pc;
        GameFlow.InputLocked = true;
        loadAt = Time.unscaledTime + delay;
    }

    void Update()
    {
        if (openFx == null && Time.time >= nextOpenCheckAt)
        {
            nextOpenCheckAt = Time.time + 0.5f;
            Bounds b = GetComponent<Collider2D>().bounds;
            if (StageClear.TryClear(transform.position, false)) openFx = MomentFx.GoalOpen(new Vector2(b.center.x, b.min.y));
        }
        if (loadAt < 0f) return;
        if (GameFlow.IsPlayerDead(player)) { loadAt = -1f; GameFlow.InputLocked = false; return; } // died during the delay: the respawn reload takes over
        if (Time.unscaledTime < loadAt) return;
        loadAt = -1f;
        if (!StageFlow.LoadNext()) GameFlow.InputLocked = false; // nothing to load: give control back
    }
}
