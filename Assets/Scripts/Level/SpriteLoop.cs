using UnityEngine;

// Looping sprite-sheet frames on a SpriteRenderer (ambient background animation). Random start offset so copies don't sync.
// Scaled time: freezes with a hit-stop / pause like the other FX.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLoop : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 4f;

    SpriteRenderer sr;
    float offset;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0 && fps > 0f) offset = Random.value * frames.Length / fps;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || fps <= 0f) return;
        sr.sprite = frames[(int)((Time.time + offset) * fps) % frames.Length];
    }
}
