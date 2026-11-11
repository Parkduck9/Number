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

    // DECISIONS 25 sniper shot relay: a human enemy on the path bats the shot at the player once (spends one of its strikes).
    const float RelayRadius = 0.6f;      // (T) u from an enemy body that counts as reaching it
    const float RelayTellTime = 0.25f;   // (T) s before contact: the batter's number pops
    const float RelaySpeedMul = 1.1f;
    const float RelayLineLength = 1.5f, RelayLineTime = 0.15f;
    static readonly Color RelayColor = new Color(1f, 0.353f, 0.235f); // #FF5A3C
    // Set by a sniper: the shot is relayable, never by its own shooter.
    public TestEnemy RelayShooter { get => shooter; set { shooter = value; relayable = true; } }
    TestEnemy shooter;
    bool relayable, relayed;
    TestEnemy relayTold; // whose number already popped for this shot
    static readonly RaycastHit2D[] RelayHits = new RaycastHit2D[8];
    static ContactFilter2D relayFilter;
    static bool relayFilterReady;
    static readonly RaycastHit2D[] GroundHits = new RaycastHit2D[16]; // one step's ground hits (reflected shots skip one-way ones, so keep several)
    static ContactFilter2D groundFilter; // struct: only its layer mask is set per cast

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active.Clear(); relayFilterReady = false; } // Domain Reload is off
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

        if (groundMask.value != 0)
        {
            groundFilter.SetLayerMask(groundMask);
            groundFilter.useTriggers = Physics2D.queriesHitTriggers;
            int n = Physics2D.BoxCast(prev, size, 0f, dir, groundFilter, GroundHits, dist);
            for (int i = 0; i < n; i++)
            {
                if (Reflected && GroundHits[i].collider.usedByEffector) continue; // a parried shot flies through one-way platforms (boss roof chunks must not block the lob to the hatch)
                Destroy(gameObject); // any other hit along the whole step (a solid wall behind a one-way platform too)
                return;
            }
        }
        if (relayable && !relayed && !Reflected && TryRelay(prev, dir, dist)) return; // batted: tests the player from the next frame
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

    // This frame's sweep reached a relayer: bat. Else pop the number of the one it reaches within RelayTellTime (tell).
    bool TryRelay(Vector2 prev, Vector2 dir, float dist)
    {
        TestEnemy e = FirstRelayer(prev, dir, dist);
        if (e != null) { Relay(e); return true; }
        e = FirstRelayer(transform.position, dir, velocity.magnitude * RelayTellTime);
        if (e != null && e != relayTold) { relayTold = e; e.Number.Pop(); }
        return false;
    }

    TestEnemy FirstRelayer(Vector2 from, Vector2 dir, float dist)
    {
        if (!relayFilterReady)
        {
            relayFilter = new ContactFilter2D();
            relayFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
            relayFilter.useTriggers = Physics2D.queriesHitTriggers;
            relayFilterReady = true;
        }
        int n = Physics2D.CircleCast(from, RelayRadius, dir, relayFilter, RelayHits, dist);
        TestEnemy best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            if (RelayHits[i].distance >= bestDist) continue;
            TestEnemy e = RelayHits[i].collider.GetComponentInParent<TestEnemy>();
            if (e == null || e == shooter || !e.CanRelay) continue;
            best = e;
            bestDist = RelayHits[i].distance;
        }
        return best;
    }

    // Re-aimed at the player's centre, faster, red-orange, with a short aim line. Still parryable (Reflect); never relayed again.
    void Relay(TestEnemy e)
    {
        relayed = true;
        Vector2 pos = transform.position;
        e.SpendStrikeOnRelay(pos);
        PlayerController pc = FindFirstObjectByType<PlayerController>(); // rare event: no cache
        Vector2 to = pc != null ? (Vector2)pc.transform.position - pos : -velocity;
        float speed = velocity.magnitude * RelaySpeedMul;
        Vector2 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
        velocity = dir * speed;
        passedPlayer = false; // a new threat, even if it was dodged before
        dieAt = Time.time + Lifetime;
        Rend.color = frames != null ? Color.Lerp(Color.white, RelayColor, 0.7f) : RelayColor;
        if (frames != null) AlignToVelocity(velocity);
        Sfx.Play(SfxId.Parry, 0.35f);

        var line = new GameObject("RelayLine");
        line.transform.position = pos + dir * (RelayLineLength * 0.5f);
        line.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        line.transform.localScale = new Vector3(RelayLineLength, 1f / 32f, 1f); // 1 px thin
        var lsr = line.AddComponent<SpriteRenderer>();
        lsr.sprite = ParryFx.Square;
        lsr.color = new Color(RelayColor.r, RelayColor.g, RelayColor.b, 0.45f);
        lsr.sortingOrder = 49;
        Destroy(line, RelayLineTime);
    }

    SpriteRenderer Rend => sr != null ? sr : sr = GetComponent<SpriteRenderer>();

    // Art points +x: rotate it along the flight direction.
    void AlignToVelocity(Vector2 v)
    {
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);
    }
}
