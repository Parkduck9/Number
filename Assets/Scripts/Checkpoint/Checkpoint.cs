using UnityEngine;

// Trigger marker. Touching it makes this the respawn point (last touched wins).
// Touching also restores full HP (confirmed in the GDD).
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] SpriteRenderer marker; // placeholder visual, optional
    [SerializeField] Color inactiveColor = new Color(0.42f, 0.44f, 0.5f);
    [SerializeField] Color activeColor = new Color(1f, 0.85f, 0.3f);

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (marker != null) marker.color = inactiveColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        PlayerHealth hp = pc.GetComponent<PlayerHealth>();
        if (hp.IsDead) return; // a killing knockback must not advance the checkpoint
        RespawnManager.SetCheckpoint(transform.position); // marker position, not wherever the player was mid-air
        hp.RestoreFull(); // GDD: checkpoints heal to full
        if (marker != null) marker.color = activeColor;
    }
}
