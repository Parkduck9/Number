using UnityEngine;

// Placeholder ranged-enemy shot: flies straight, one hit test against the player.
// Parried or hit = destroyed. Dodged (dash/invulnerable) = passes through, never tests again.
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
        transform.position += (Vector3)(velocity * Time.deltaTime);
        Vector2 pos = transform.position;

        if (groundMask.value != 0 && Physics2D.OverlapBox(pos, size, 0f, groundMask) != null)
        {
            Destroy(gameObject);
            return;
        }
        if (passedPlayer) return;
        Collider2D col = Physics2D.OverlapBox(pos, size, 0f, playerMask);
        if (col == null) return;
        PlayerController pc = col.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        info.SourcePosition = pos;
        info.TravelDirX = Mathf.Abs(velocity.x) > 0.01f ? Mathf.Sign(velocity.x) : 0f; // front/back parry test uses travel direction
        HitResult result = pc.ReceiveEnemyHit(info);
        if (result == HitResult.Dodged) passedPlayer = true;
        else Destroy(gameObject);
    }
}
