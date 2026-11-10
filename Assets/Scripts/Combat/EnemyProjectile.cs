using UnityEngine;

// Placeholder ranged-enemy shot: flies straight, one hit test against the player.
// Hit = destroyed. Parried = flies back harmlessly for a moment. Dodged (dash/invulnerable) = passes through, never tests again.
public class EnemyProjectile : MonoBehaviour
{
    const float Lifetime = 3f;
    const float ReflectGravity = 18f;   // u/s^2: a parried shot has lost its power and drops (user 11-10)
    const float ReflectSpeedLoss = 0.45f; // fraction of its speed it keeps on the way back

    Vector2 velocity;
    Vector2 size = new Vector2(0.5f, 0.5f);
    LayerMask playerMask;
    LayerMask groundMask;
    HitInfo info;
    float dieAt;
    bool passedPlayer;
    Sprite[] frames; // null = placeholder square
    SpriteRenderer sr;
    const float FrameFps = 12f;

    // Live shots, for the LMB parry merge (PlayerController.FaceParryThreat).
    public static readonly System.Collections.Generic.List<EnemyProjectile> Active = new System.Collections.Generic.List<EnemyProjectile>();
    public Vector2 Velocity => velocity;
    public bool Parryable => !info.Unparryable && !passedPlayer;
    public bool Reflected { get; private set; }        // parried back (BossFight: a reflected shot that reaches the chest hatch hurts it)
    [System.NonSerialized] public float ReflectLife = Juice.ReflectLife; // s a parried shot keeps flying (the boss sets it longer)
    [System.NonSerialized] public Vector2? ReflectTarget;                 // boss hatch: the weakened shot lobs back into it instead of just dropping

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Active.Clear(); // Domain Reload is off
    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    // frames (optional, 2-frame loop pointing +x): art instead of the coloured square; `color` is the fallback when null.
    public static void Spawn(Vector2 position, int dir, float speed, LayerMask playerMask, LayerMask groundMask, HitInfo info, Color color, Sprite[] frames = null)
    {
        Spawn(position, new Vector2(dir * speed, 0f), playerMask, groundMask, info, color, frames);
    }

    public static EnemyProjectile Spawn(Vector2 position, Vector2 velocity, LayerMask playerMask, LayerMask groundMask, HitInfo info, Color color, Sprite[] frames = null)
    {
        GameObject go = new GameObject("EnemyProjectile");
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ParryFx.Square;
        sr.color = color;
        sr.sortingOrder = 50;
        EnemyProjectile p = go.AddComponent<EnemyProjectile>();
        if (frames != null && frames.Length > 0)
        {
            p.frames = frames;
            sr.sprite = frames[0];
            sr.color = Color.white;
            go.transform.localScale = Vector3.one;
            p.AlignToVelocity(velocity);
        }
        p.velocity = velocity;
        p.playerMask = playerMask;
        p.groundMask = groundMask;
        p.info = info;
        p.dieAt = Time.time + Lifetime;
        return p;
    }

    void Update()
    {
        if (GameFlow.InputLocked || GameFlow.EnemiesHeld) { dieAt += Time.deltaTime; return; } // dice hold freezes shots too
        if (Time.time >= dieAt) { Destroy(gameObject); return; }
        if (frames != null) Rend.sprite = frames[Mathf.FloorToInt(Time.time * FrameFps) % frames.Length];
        Vector2 prev = transform.position;
        if (Reflected)
        {
            velocity.y -= ReflectGravity * Time.deltaTime; // falls as it goes back
            if (frames != null) AlignToVelocity(velocity);
        }
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

    // Parried: weakened, it pops back and falls (or lobs into ReflectTarget), white, harmless (never tests the player again), and vanishes soon.
    void Reflect()
    {
        Vector2 pos = transform.position;
        float speed = Mathf.Max(velocity.magnitude * ReflectSpeedLoss, 4f);
        if (ReflectTarget.HasValue)
        {
            // weakened lob that lands in the target: flight time from the reduced speed, gravity compensated on y
            Vector2 d = ReflectTarget.Value - pos;
            float t = Mathf.Clamp(d.magnitude / speed, 0.35f, 1.5f); // cap: from far away it must still land in time and stay on screen
            velocity = new Vector2(d.x / t, d.y / t + 0.5f * ReflectGravity * t);
        }
        else
        {
            velocity = -velocity.normalized * speed + Vector2.up * 3f; // a small pop up, then it drops
            if (velocity.y < 0f) velocity.y = Mathf.Abs(velocity.y) * 0.3f; // a shot fired up at us must not vanish into the floor at once
        }
        passedPlayer = true;
        Reflected = true;
        dieAt = Time.time + ReflectLife;
        if (frames != null)
        {
            AlignToVelocity(velocity);
            Rend.color = new Color(1f, 1f, 1f, 0.85f); // pale: still reads as reflected
        }
        else Rend.color = Color.white;
    }

    SpriteRenderer Rend => sr != null ? sr : sr = GetComponent<SpriteRenderer>();

    // Art points +x: rotate it along the flight direction.
    void AlignToVelocity(Vector2 v)
    {
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
    }
}
