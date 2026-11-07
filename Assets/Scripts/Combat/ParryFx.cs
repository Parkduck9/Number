using UnityEngine;

// Placeholder parry-success sparks (code-made pixel squares). Replaced by real art later.
public static class ParryFx
{
    static Sprite square;

    public static void SpawnSparks(Vector2 point, int count, float life, float speed, float size)
    {
        if (square == null)
        {
            Texture2D tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("ParrySpark");
            go.transform.position = point;
            go.transform.localScale = new Vector3(size, size, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = square;
            sr.color = Color.white;
            sr.sortingOrder = 100;
            go.AddComponent<Spark>().velocity = Random.insideUnitCircle.normalized * speed * Random.Range(0.6f, 1f);
            Object.Destroy(go, life);
        }
    }

    class Spark : MonoBehaviour
    {
        public Vector2 velocity;
        void Update() { transform.position += (Vector3)(velocity * Time.deltaTime); }
    }
}
