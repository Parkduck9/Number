using UnityEngine;

// Pixel shield arc in front of an enemy whose frontal hits are blocked (guarding Android, tutorial Guarded dummy).
// Code-drawn like the dice FX (DiceSkillKit.Quad); hidden while the block is off (overloaded, down, attacker behind/above).
public class FrontShieldFx : MonoBehaviour
{
    const int Segs = 8;
    TestEnemy enemy;
    Transform root;
    SpriteRenderer[] parts;
    float[] baseAlpha;
    float flashUntil;

    public static void Attach(TestEnemy e)
    {
        var fx = e.GetComponent<FrontShieldFx>(); // a spawner template clone carries the component without its (private) enemy link
        if (fx == null) fx = e.gameObject.AddComponent<FrontShieldFx>();
        fx.enemy = e;
    }

    // A frontal hit was blocked: flash brighter.
    public void Flash() => flashUntil = Time.time + 0.18f;

    void Build()
    {
        root = new GameObject("FrontShield").transform;
        root.SetParent(transform, false);
        parts = new SpriteRenderer[Segs + 1];
        baseAlpha = new float[Segs + 1];
        const float rx = 0.3f, ry = 0.95f, cx = 0.45f;
        Vector2 prev = Vector2.zero;
        for (int i = 0; i <= Segs; i++)
        {
            float a = Mathf.Lerp(-70f, 70f, i / (float)Segs) * Mathf.Deg2Rad;
            Vector2 p = new Vector2(cx + Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
            p = new Vector2(Mathf.Round(p.x / DiceSkillKit.Px), Mathf.Round(p.y / DiceSkillKit.Px)) * DiceSkillKit.Px; // snap to the pixel grid
            if (i > 0)
            {
                Vector2 d = p - prev;
                parts[i - 1] = DiceSkillKit.Quad(root, (p + prev) * 0.5f, new Vector2(d.magnitude + 2f * DiceSkillKit.Px, 2f * DiceSkillKit.Px), DiceSkillKit.Cyan, 95, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                baseAlpha[i - 1] = 0.85f;
            }
            prev = p;
        }
        // faint fill between the arc and the body
        parts[Segs] = DiceSkillKit.Quad(root, new Vector2(cx + 0.05f, 0f), new Vector2(0.3f, ry * 1.5f), DiceSkillKit.Cyan, 94);
        baseAlpha[Segs] = 0.14f;
    }

    void Update()
    {
        bool on = enemy != null && !enemy.IsDown && (enemy.Guarded || enemy.BlocksHits);
        if (!on) { if (root != null && root.gameObject.activeSelf) root.gameObject.SetActive(false); return; }
        if (root == null) Build();
        if (!root.gameObject.activeSelf) root.gameObject.SetActive(true);
        Vector2 c = enemy.BodyCollider != null ? (Vector2)(enemy.BodyCollider.bounds.center - transform.position) : Vector2.zero;
        root.localPosition = new Vector3(0f, c.y, 0f);
        root.localScale = new Vector3(enemy.Facing >= 0 ? 1f : -1f, 1f, 1f);
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 6f);
        float flash = Time.time < flashUntil ? 1f : 0f;
        for (int i = 0; i < parts.Length; i++)
        {
            Color col = Color.Lerp(DiceSkillKit.Cyan, Color.white, flash);
            col.a = Mathf.Min(1f, baseAlpha[i] * pulse + flash * 0.6f);
            parts[i].color = col;
        }
    }
}
