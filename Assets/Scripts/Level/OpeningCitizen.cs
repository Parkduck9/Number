using UnityEngine;

// PLACEHOLDER for the opening only (STG-00): a citizen that idles back and forth until OpeningSequence tells it to freeze.
// Once enemy-rules' NUM-0 citizen freeze is merged, these could be TestEnemy citizens with combat off.
public class OpeningCitizen : MonoBehaviour
{
    [SerializeField] float walkRange = 1.5f; // (T) units each side of the start x
    [SerializeField] float speed = 1f;       // (T) units per second

    static readonly Color FrozenTint = new Color32(0x8C, 0x97, 0xA8, 0xFF);

    SpriteRenderer sr;
    Sprite[] walkFrames; // DraftSprites citizen walk; null = keep the placeholder square
    Color baseColor;
    float startX, phase;
    float freezeAt = float.PositiveInfinity;
    bool frozen;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        walkFrames = DraftSprites.CitizenWalk;
        if (!DraftSprites.Setup(sr, walkFrames)) walkFrames = null;
        if (sr != null) baseColor = sr.color;
        startX = transform.position.x;
        phase = Random.value * 4f * walkRange / speed;
    }

    // Stops moving `delay` seconds from now (game time, so pause stalls it).
    public void Freeze(float delay) { freezeAt = Mathf.Min(freezeAt, Time.time + delay); }

    void Update()
    {
        if (frozen) return;
        if (Time.time >= freezeAt)
        {
            frozen = true;
            if (sr != null) sr.color = FrozenTint;
            if (walkFrames != null) sr.sprite = walkFrames[0]; // hold the stance
            return;
        }
        phase += Time.deltaTime;
        // triangle wave in [-walkRange, walkRange]
        float t = Mathf.PingPong(phase * speed, 2f * walkRange) - walkRange;
        Vector3 p = transform.position;
        float newX = startX + t;
        if (walkFrames != null)
        {
            if (!Mathf.Approximately(newX, p.x)) sr.flipX = newX < p.x; // art faces right
            sr.sprite = walkFrames[Mathf.FloorToInt(Time.time * DraftSprites.WalkFps) % walkFrames.Length];
        }
        p.x = newX;
        transform.position = p;
    }
}
