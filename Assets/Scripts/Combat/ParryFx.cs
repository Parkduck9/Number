using UnityEngine;

// Placeholder combat FX (code-made pixel squares). Replaced by real art later.
public static class ParryFx
{
    static Sprite square;

    // Shared 1x1 white sprite (scale it to size).
    public static Sprite Square
    {
        get
        {
            if (square == null)
            {
                Texture2D tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return square;
        }
    }

    public static void SpawnSparks(Vector2 point, int count, float life, float speed, float size)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("ParrySpark");
            go.transform.position = point;
            go.transform.localScale = new Vector3(size, size, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Square;
            sr.color = Color.white;
            sr.sortingOrder = 100;
            go.AddComponent<Spark>().velocity = Random.insideUnitCircle.normalized * speed * Random.Range(0.6f, 1f);
            Object.Destroy(go, life);
        }
    }

    // Swing placeholder: short flash over the attack box + attacker's visual steps forward for that moment.
    public static void Swing(Vector2 center, Vector2 size, Color color, Transform attackerVisual, int facing, float time = 0.08f, float nudge = 0.125f)
    {
        GameObject go = new GameObject("SwingFlash");
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square;
        color.a = 0.45f;
        sr.color = color;
        sr.sortingOrder = 90;
        Object.Destroy(go, time);

        if (attackerVisual == null) return;
        Nudge n = attackerVisual.GetComponent<Nudge>();
        if (n == null) n = attackerVisual.gameObject.AddComponent<Nudge>();
        n.Begin(facing * nudge, time);
    }

    class Spark : MonoBehaviour
    {
        public Vector2 velocity;
        void Update() { transform.position += (Vector3)(velocity * Time.deltaTime); }
    }

    class Nudge : MonoBehaviour
    {
        Vector3 basePos;
        float until = -1f;

        void Awake() { basePos = transform.localPosition; }

        public void Begin(float dx, float time)
        {
            transform.localPosition = basePos + new Vector3(dx, 0f, 0f);
            until = Time.time + time;
        }

        void Update()
        {
            if (until >= 0f && Time.time >= until) { transform.localPosition = basePos; until = -1f; }
        }
    }
}
