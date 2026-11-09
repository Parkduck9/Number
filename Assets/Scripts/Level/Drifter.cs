using UnityEngine;

// Slow ambient drift for background pieces (clouds ~0.5-1 px/s, drones a few u/s): moves along local x, wraps from one end of
// [minX, maxX] to the other (the ends are off-screen), optional 1 px bob. Position snaps to the 1/32 u pixel grid.
public class Drifter : MonoBehaviour
{
    [SerializeField] float speed = 0.02f;   // u/s along +x (negative = left)
    [SerializeField] float minX, maxX;      // local x wrap range
    [SerializeField] bool bob;              // 1 px up/down
    [SerializeField] float bobHz = 0.5f;

    float x, y, offset;

    void Awake()
    {
        x = transform.localPosition.x;
        y = transform.localPosition.y;
        offset = Random.value * 10f;
    }

    void Update()
    {
        x += speed * Time.deltaTime;
        if (x > maxX) x = minX;
        else if (x < minX) x = maxX;
        float by = bob && Mathf.Sin((Time.time + offset) * bobHz * Mathf.PI * 2f) > 0f ? 1f / 32f : 0f;
        transform.localPosition = new Vector3(Mathf.Round(x * 32f) / 32f, y + by, transform.localPosition.z);
    }
}
