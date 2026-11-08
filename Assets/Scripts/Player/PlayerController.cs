using UnityEngine;

// State machine + movement + attack/parry/dash. See docs/design/PROTOTYPE_ARCH.md section 2/3.
// Update = input, timers, transitions, hit checks. FixedUpdate = velocity writes.
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader), typeof(PlayerHealth))]
public class PlayerController : MonoBehaviour
{
    public enum State { Locomotion, Attack, Parry, Dash, Hurt, Dead }

    [Header("Masks")]
    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask enemyMask;

    [Header("Movement (T)")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float gravityScale = 4f;
    [SerializeField] float jumpSpeed = 14f;
    [SerializeField] float coyoteTime = 0.10f;
    [SerializeField] float jumpBuffer = 0.10f;
    [SerializeField] Vector2 feetOffset = new Vector2(0f, -1.0f);
    [SerializeField] Vector2 feetSize = new Vector2(0.8f, 0.1f);

    [Header("Attack (T)")]
    [SerializeField] float attackHitDelay = 0.08f;
    [SerializeField] float attackDuration = 0.30f;
    [SerializeField] float attackBuffer = 0.15f;
    [SerializeField] Vector2 attackOffset = new Vector2(0.9f, 0f);
    [SerializeField] Vector2 attackSize = new Vector2(1.6f, 1.4f);
    [SerializeField] int attackDamage = 1;

    [Header("Parry (T)")]
    [SerializeField] float parryActiveTime = 0.20f;
    [SerializeField] float parryDuration = 0.35f;

    [Header("Dash (T)")]
    [SerializeField] float dashSpeed = 24f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 2.0f;

    [Header("Hurt (T)")]
    [SerializeField] Vector2 knockback = new Vector2(5f, 4f);
    [SerializeField] float hurtDuration = 0.25f;

    [Header("Parry FX (T)")]
    [SerializeField] float hitStopTime = 0.06f;
    [SerializeField] float parryFlashTime = 0.1f;

    [Header("Dash afterimage (T)")]
    [SerializeField] float afterimageInterval = 0.04f;
    [SerializeField] float afterimageLife = 0.2f;

    static readonly Color AfterimageColor = new Color(0.298f, 0.909f, 0.847f, 0.6f); // #4CE8D8
    static readonly Color BaseColor = new Color(0.298f, 0.878f, 1f);
    float hitStopEndsAt = -1f; // real time; <0 = no hit-stop running
    float parryFlashUntil;

    Rigidbody2D rb;
    PlayerInputReader input;
    PlayerHealth health;
    SpriteRenderer visual;

    State state;
    float stateEnteredAt;
    bool grounded;
    ContactFilter2D groundFilter;
    readonly Collider2D[] groundHits = new Collider2D[8];
    float lastGroundedAt = -999f;
    float jumpBufferedUntil = -999f;
    float attackBufferedUntil = -999f;
    float dashCooldownUntil;
    float nextAfterimageAt;
    bool jumpCut;
    bool attackHitDone;

    public int Facing { get; private set; } = 1;
    public State CurrentState => state;
    public bool Grounded => grounded;
    public bool ParryActive => state == State.Parry && Time.time - stateEnteredAt < parryActiveTime;
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownUntil - Time.time);
    public LayerMask EnemyMask => enemyMask;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
        health = GetComponent<PlayerHealth>();
        visual = GetComponentInChildren<SpriteRenderer>();
        rb.gravityScale = gravityScale;
        if (GetComponent<Shockwave>() == null) gameObject.AddComponent<Shockwave>(); // upgrade 8-A; players are built by scripts, so add at runtime
    }

    void OnDisable() { EndHitStop(); }

    void EndHitStop()
    {
        if (hitStopEndsAt < 0f) return;
        hitStopEndsAt = -1f;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (hitStopEndsAt >= 0f)
        {
            if (Time.unscaledTime >= hitStopEndsAt) EndHitStop();
            else { UpdateVisual(Time.time); return; } // frozen; still show the parry flash
        }
        float now = Time.time;
        float elapsed = now - stateEnteredAt;

        // ground check (ignore while rising so a jump doesn't refresh coyote time)
        // A ground collider that already contains a point just above the feet means the feet are embedded in it
        // (jump apex inside a one-way platform), so it is not something we stand on.
        Vector2 feet = (Vector2)transform.position + feetOffset;
        groundFilter.SetLayerMask(groundMask);
        int hitCount = Physics2D.OverlapBox(feet, feetSize, 0f, groundFilter, groundHits);
        grounded = false;
        for (int i = 0; i < hitCount && !grounded; i++)
            grounded = !groundHits[i].OverlapPoint(feet + Vector2.up * 0.03f);
        if (grounded && rb.linearVelocity.y <= 0.1f) lastGroundedAt = now;

        if (input.JumpPressed) jumpBufferedUntil = now + jumpBuffer;
        if (input.AttackPressed && state != State.Locomotion && state != State.Hurt && state != State.Dead)
            attackBufferedUntil = now + attackBuffer;

        switch (state)
        {
            case State.Locomotion: UpdateLocomotion(now); break;
            case State.Attack:
                if (!attackHitDone && elapsed >= attackHitDelay) DoAttackHit();
                if (elapsed >= attackHitDelay && TryDash(now)) break;
                if (elapsed >= attackDuration) EnterState(State.Locomotion);
                break;
            case State.Parry:
                if (elapsed >= parryDuration) EnterState(State.Locomotion);
                break;
            case State.Dash:
                if (visual != null && now >= nextAfterimageAt)
                {
                    nextAfterimageAt = now + afterimageInterval;
                    SpriteFx.Afterimage(visual, AfterimageColor, afterimageLife);
                }
                if (elapsed >= dashDuration) EnterState(State.Locomotion);
                break;
            case State.Hurt:
                if (elapsed >= hurtDuration) EnterState(health.IsDead ? State.Dead : State.Locomotion);
                break;
        }

        if (state == State.Locomotion || state == State.Attack || state == State.Parry) UpdateJumpCut();
        UpdateVisual(now);
    }

    void UpdateLocomotion(float now)
    {
        if (Mathf.Abs(input.MoveX) > 0.01f) Facing = input.MoveX > 0f ? 1 : -1;

        if (TryDash(now)) return;
        if (input.ParryPressed) { EnterState(State.Parry); return; }
        if (input.AttackPressed || now <= attackBufferedUntil) { EnterState(State.Attack); return; }

        // jump with coyote + buffer
        if (now <= jumpBufferedUntil && now - lastGroundedAt <= coyoteTime)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            jumpBufferedUntil = -999f;
            lastGroundedAt = -999f;
            jumpCut = false;
        }
    }

    // variable jump: release while rising halves the upward speed once (any state that keeps gravity)
    void UpdateJumpCut()
    {
        if (!jumpCut && rb.linearVelocity.y > 0.1f && !input.JumpHeld)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            jumpCut = true;
        }
        if (rb.linearVelocity.y <= 0.1f) jumpCut = false;
    }

    bool TryDash(float now)
    {
        if (!input.DashPressed || now < dashCooldownUntil) return false;
        dashCooldownUntil = now + dashCooldown;
        EnterState(State.Dash);
        return true;
    }

    void EnterState(State next)
    {
        state = next;
        stateEnteredAt = Time.time;
        rb.gravityScale = gravityScale; // always restore (Dash zeroes it)

        switch (next)
        {
            case State.Locomotion:
                break;
            case State.Attack:
                attackHitDone = false;
                attackBufferedUntil = -999f;
                if (grounded) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            case State.Parry:
                attackBufferedUntil = -999f;
                if (grounded) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            case State.Dash:
                rb.gravityScale = 0f;
                rb.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
                break;
            case State.Dead:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Locomotion:
                rb.linearVelocity = new Vector2(input.MoveX * moveSpeed, rb.linearVelocity.y);
                break;
            case State.Dash:
                rb.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
                break;
        }
    }

    void DoAttackHit()
    {
        attackHitDone = true;
        ParryFx.Slash(AttackCenter(), visual != null ? visual.transform : null, Facing);
        Collider2D col = Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyMask);
        if (col == null) return;
        TestEnemy enemy = col.GetComponentInParent<TestEnemy>();
        if (enemy == null) return;
        bool damaged = enemy.ReceivePlayerHit(new HitInfo { Damage = attackDamage, SourcePosition = transform.position, IsSpecial = false });
        if (damaged) PlayHitSpark(col.ClosestPoint(AttackCenter()), Facing);
    }

    // hit-spark's pivot is a feet point: shift so its core (+12,+16 px) lands on the contact point. dir = blow direction.
    static void PlayHitSpark(Vector2 point, float dir)
    {
        SpriteFx.Play("hit-spark", point - new Vector2(dir * 12f, 16f) / SpriteFx.Ppu, dir < 0f);
    }

    Vector2 AttackCenter() => (Vector2)transform.position + new Vector2(Facing * attackOffset.x, attackOffset.y);

    // Called by the enemy at its strike moment. Priority: Dash > post-hit invulnerability > Parry (front only) > Hit.
    public HitResult ReceiveEnemyHit(HitInfo info)
    {
        if (state == State.Dead) return HitResult.Dodged;
        if (state == State.Dash) return HitResult.Dodged;
        if (health.Invulnerable) return HitResult.Dodged;

        float dx = info.SourcePosition.x - transform.position.x;
        bool fromFront = Mathf.Approximately(dx, 0f) || Mathf.Sign(dx) == Facing;
        if (ParryActive && fromFront && !info.Unparryable)
        {
            // No knockback for either side. Hit-stop + flash + parry FX.
            EnterState(State.Locomotion);
            parryFlashUntil = Time.time + parryFlashTime;
            SpriteFx.Play("parry", (Vector2)transform.position + new Vector2(Facing * 0.8f, 0f) - new Vector2(0f, 30f / SpriteFx.Ppu)); // burst centre is 30px above the pivot
            if (hitStopTime > 0f)
            {
                hitStopEndsAt = Time.unscaledTime + hitStopTime;
                Time.timeScale = 0f;
            }
            return HitResult.Parried;
        }

        health.TakeDamage(info.Damage);
        EnterState(State.Hurt);
        float away = dx > 0f ? -1f : 1f;
        PlayHitSpark(transform.position, away); // blow travels away from the source
        rb.linearVelocity = new Vector2(away * knockback.x, knockback.y);
        return HitResult.Hit;
    }

    void UpdateVisual(float now)
    {
        if (visual == null) return;
        Color c = BaseColor;
        if (state == State.Dash) c = Color.white;
        else if (ParryActive) c = new Color(0.4f, 1f, 0.5f);
        else if (state == State.Parry) c = new Color(0.2f, 0.5f, 0.35f);
        else if (state == State.Dead) c = Color.gray;
        else if (health.Invulnerable && Mathf.FloorToInt(now * 20f) % 2 == 0) c.a = 0.3f;
        if (now < parryFlashUntil) c = Color.white;
        visual.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(AttackCenter(), attackSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + feetOffset, feetSize);
    }
}
