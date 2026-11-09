using UnityEngine;

// DRAFT: swaps a placeholder square for the Luddite NPC idle strip (2 frames) and loops it. No strip = the square stays.
// The art faces right; the player arrives from the left, so it is flipped. Removal: docs/design/impl/draft-sprites.md.
public class DraftIdleAnim : MonoBehaviour
{
    SpriteRenderer sr;
    Sprite[] frames;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        frames = DraftSprites.Luddite;
        if (!DraftSprites.Setup(sr, frames)) { frames = null; return; }
        sr.flipX = true;
    }

    void Update()
    {
        if (frames != null) sr.sprite = frames[Mathf.FloorToInt(Time.time * DraftSprites.LudditeIdleFps) % frames.Length];
    }
}
