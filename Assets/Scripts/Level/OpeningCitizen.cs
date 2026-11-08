using UnityEngine;

// PLACEHOLDER for the opening only (STG-00): a citizen that idles back and forth and freezes when a board shows 0.
// Once enemy-rules' NUM-0 citizen freeze is merged, these could be TestEnemy citizens with combat off.
public class OpeningCitizen : MonoBehaviour, IBroadcastListener
{
    [SerializeField] float walkRange = 1.5f; // (T) units each side of the start x
    [SerializeField] float speed = 1f;       // (T) units per second

    static readonly Color FrozenTint = new Color32(0x8C, 0x97, 0xA8, 0xFF);

    SpriteRenderer sr;
    Color baseColor;
    float startX, phase;
    bool frozen;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        startX = transform.position.x;
        phase = Random.value * 4f * walkRange / speed;
        foreach (var b in BroadcastBoard.All) b.AddListener(this);
    }

    void OnDestroy()
    {
        foreach (var b in BroadcastBoard.All) b.RemoveListener(this);
    }

    void Update()
    {
        if (frozen) return;
        phase += Time.deltaTime;
        // triangle wave in [-walkRange, walkRange]
        float t = Mathf.PingPong(phase * speed, 2f * walkRange) - walkRange;
        Vector3 p = transform.position;
        p.x = startX + t;
        transform.position = p;
    }

    public void OnBroadcastChanged(BroadcastBoard board, int newNumber, int oldNumber)
    {
        frozen = newNumber == 0;
        if (sr != null) sr.color = frozen ? FrozenTint : baseColor;
    }
}
