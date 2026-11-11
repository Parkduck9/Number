using UnityEngine;

// Trigger marker. Touching it makes this the respawn point (last touched wins).
// Touching also restores full HP (confirmed in the GDD).
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] SpriteRenderer marker; // placeholder visual, optional
    [SerializeField] Color inactiveColor = new Color(0.42f, 0.44f, 0.5f);
    [SerializeField] Color activeColor = new Color(1f, 0.85f, 0.3f);
    Sprite off, on; // Luddite mark art (Resources/DraftUi/checkpoint_off|on, 16x40 px, centre pivot); null = the colour-tinted placeholder
    bool lit; // touched since this scene load (IsRecordedAt only answers during the respawn reload, so it cannot gate the FX later)

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        // After a respawn reload the recorded checkpoint is this one: keep it lit.
        lit = RespawnManager.IsRecordedAt(transform.position);
        off = Resources.Load<Sprite>("DraftUi/checkpoint_off");
        on = Resources.Load<Sprite>("DraftUi/checkpoint_on");
        if (marker != null && off != null && on != null)
        {
            marker.transform.localScale = Vector3.one; // the placeholder square was scaled to 0.3 x 1
            marker.transform.localPosition = new Vector3(0f, -1f + off.bounds.extents.y, 0f); // feet line = transform y - 1
        }
        Show();
    }

    void Show()
    {
        if (marker == null) return;
        if (off != null && on != null) { marker.sprite = lit ? on : off; marker.color = Color.white; }
        else marker.color = lit ? activeColor : inactiveColor;
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
        Show();
    }
}
