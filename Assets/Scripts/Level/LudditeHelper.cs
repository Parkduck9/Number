using UnityEngine;

// DECISIONS 22: non-combat Luddite helper (the old hero look). Idles, faces the player, and can say a radio line file once when the player walks up.
// Keeps the placeholder square when the helper_idle_4f strip is missing. Placed by the stage builders (StageBuildKit.BuildHelper).
public class LudditeHelper : MonoBehaviour
{
    [SerializeField] TextAsset greeting;    // optional, played once
    [SerializeField] float greetRange = 4f; // (T) units

    SpriteRenderer sr;
    Sprite[] frames;
    PlayerController player;
    bool greeted;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        frames = DraftSprites.HelperIdle;
        if (!DraftSprites.Setup(sr, frames)) frames = null;
    }

    void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        float dx = player.transform.position.x - transform.position.x;
        if (frames != null)
        {
            sr.flipX = dx < 0f; // art faces right
            sr.sprite = frames[Mathf.FloorToInt(Time.time * DraftSprites.IdleFps) % frames.Length];
        }
        if (!greeted && greeting != null && Mathf.Abs(dx) < greetRange && DialoguePlayer.Instance != null)
        {
            greeted = true;
            DialoguePlayer.Instance.Play(greeting);
        }
    }
}
