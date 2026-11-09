using UnityEngine;

// Trigger marker. Touching it makes this the respawn point (last touched wins).
// Touching also restores full HP (confirmed in the GDD).
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] SpriteRenderer marker; // placeholder visual, optional
    [SerializeField] Color inactiveColor = new Color(0.42f, 0.44f, 0.5f);
    [SerializeField] Color activeColor = new Color(1f, 0.85f, 0.3f);
    bool lit; // touched since this scene load (IsRecordedAt only answers during the respawn reload, so it cannot gate the FX later)

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        // After a respawn reload the recorded checkpoint is this one: keep it lit.
        lit = RespawnManager.IsRecordedAt(transform.position);
        if (marker != null) marker.color = lit ? activeColor : inactiveColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        PlayerHealth hp = pc.GetComponent<PlayerHealth>();
        if (hp.IsDead) return; // a killing knockback must not advance the checkpoint
        if (!lit) MomentFx.Checkpoint(transform.position); // only when newly activated
        lit = true;
        RespawnManager.SetCheckpoint(transform.position); // marker position, not wherever the player was mid-air
        hp.RestoreFull(); // GDD: checkpoints heal to full
        if (marker != null) marker.color = activeColor;
    }
}
