using UnityEngine;

// Placeholder ranged-enemy shot: flies straight, one hit test against the player.
// Hit = destroyed. Parried = flies back harmlessly for a moment. Dodged (dash/invulnerable) = passes through, never tests again.
public class EnemyProjectile : MonoBehaviour
{
    const float Lifetime = 3f;

    Vector2 velocity;
    Vector2 size = new Vector2(0.5f, 0.5f);
    LayerMask playerMask;
    LayerMask groundMask;
    HitInfo info;
    float dieAt;
    bool passedPlayer;

    // Live shots, for the LMB parry merge (PlayerController.FaceParryThreat).
    public static readonly System.Collections.Generic.List<EnemyProjectile> Active = new System.Collections.Generic.List<EnemyProjectile>();
    public Vector2 Velocity => velocity;
    public bool Parryable => !info.Unparryable && !passedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Active.Clear(); // Domain Reload is off
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    public static void Spawn(Vector2 position, int dir, float speed, LayerMask playerMask, LayerMask groundMask, HitInfo info, Color color)
    {
        Spawn(position, new Vector2(dir * speed, 0f), playerMask, groundMask, info, color);
    }

    public static void Spawn(Vector2 position, Vector2 velocity, LayerMask playerMask, LayerMask groundMask, HitInfo info, Color color)
    {
        GameObject go = new GameObject("EnemyProjectile");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ParryFx.Square;
        sr.color = color;
        sr.sortingOrder = 50;
        EnemyProjectile p = go.AddComponent<EnemyProjectile>();
        p.velocity = velocity;
        p.playerMask = playerMask;
        p.groundMask = groundMask;
        p.info = info;
        p.dieAt = Time.time + Lifetime;
    }

    void Update()
    {
        if (GameFlow.InputLocked) { dieAt += Time.deltaTime; return; }
        if (Time.time >= dieAt) { Destroy(gameObject); return; }
        Vector2 prev = transform.position;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        Vector2 pos = transform.position;
        // Sweep from the old to the new position (a long frame must not tunnel through ground or the player).
        Vector2 step = pos - prev;
        float dist = step.magnitude;
        Vector2 dir = dist > 0f ? step / dist : Vector2.right;

        if (groundMask.value != 0 && Physics2D.BoxCast(prev, size, 0f, dir, dist, groundMask).collider != null)
        {
            Destroy(gameObject);
            return;
        }
        if (passedPlayer) return;
        Collider2D col = Physics2D.BoxCast(prev, size, 0f, dir, dist, playerMask).collider;
        if (col == null) return;
        PlayerController pc = col.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        info.SourcePosition = pos;
        info.TravelDirX = Mathf.Abs(velocity.x) > 0.01f ? Mathf.Sign(velocity.x) : 0f; // front/back parry test uses travel direction
        HitResult result = pc.ReceiveEnemyHit(info);
        if (result == HitResult.Dodged) passedPlayer = true;
        else if (result == HitResult.Parried) Reflect();
        else Destroy(gameObject);
    }

    // Parried: fly back the way it came, white, harmless (never tests the player again), and vanish soon.
    void Reflect()
    {
        velocity = -velocity * Juice.ReflectSpeedMul;
        passedPlayer = true;
        dieAt = Time.time + Juice.ReflectLife;
        GetComponent<SpriteRenderer>().color = Color.white;
    }
}
