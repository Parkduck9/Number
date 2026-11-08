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
    [SerializeField] float parryGuardTime = 0.2f; // after a successful parry, further frontal hits are parried too

    [Header("Dash (T)")]
    [SerializeField] float dashSpeed = 24f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float dashCooldown = 2.0f;

    // DECISIONS 11 / docs/design/impl/movement-assist.md
    [Header("Attack approach (T)")]
    [SerializeField] float approachRange = 3.0f;      // search distance ahead (u)
    [SerializeField] float approachMaxDy = 1.0f;
    [SerializeField] float approachStopDist = 1.4f;   // centre-to-centre stop distance (attack box reaches 1.7)
    [SerializeField] float approachMaxSpeed = 40f;
    [SerializeField] float ledgeProbeDepth = 1.5f;    // ground must exist this far below the target feet

    [Header("Dash behind (T)")]
    [SerializeField] float dashBehindRange = 2.0f;    // 1.5 + tolerance
    [SerializeField] float dashBehindMaxDy = 1.0f;
    [SerializeField] float dashBehindClearance = 0.3f; // gap between the two boxes at the landing spot
    [SerializeField] float dashMinDuration = 0.08f;
    [SerializeField] bool dashBehindFaceBack = true;  // end a dash-behind facing the enemy's back

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
    float parryGuardUntil;

    Rigidbody2D rb;
    PlayerInputReader input;
    PlayerHealth health;
    SpriteRenderer visual;
    bool draftSprite; // DraftSprites: hero frames instead of the placeholder square

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
    bool sliding;                       // attack auto-approach in progress (first attackHitDelay of an Attack)
    float slideTargetX, slideSpeed;
    float dashEndsAfter;                // length of the current dash (dashDuration, or shorter/longer for a dash-behind)
    bool dashBehindActive;
    bool pendingParry, pendingDash; // pressed during a hit-stop: replayed on the frame the hit-stop ends (jump/attack use their buffers)
    readonly System.Collections.Generic.HashSet<TestEnemy> swingHits = new System.Collections.Generic.HashSet<TestEnemy>();

    public int Facing { get; private set; } = 1;
    public State CurrentState => state;
    public bool Grounded => grounded;
    public bool ParryActive => state == State.Parry && Time.time - stateEnteredAt < parryActiveTime;
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownUntil - Time.time);
    public Vector2 DashStart { get; private set; } // where the current dash began (TestEnemy: a dash-behind that left the hitbox before the strike still counts as a dash-through)
    public LayerMask EnemyMask => enemyMask;
    bool ParryPressed => input.ParryPressed || pendingParry;
    bool DashPressed => input.DashPressed || pendingDash;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
        health = GetComponent<PlayerHealth>();
        visual = GetComponentInChildren<SpriteRenderer>();
        draftSprite = DraftSprites.Setup(visual, DraftSprites.Hero);
        rb.gravityScale = gravityScale;
        if (GetComponent<Shockwave>() == null) gameObject.AddComponent<Shockwave>(); // upgrade 8-A; players are built by scripts, so add at runtime
    }

    void OnDisable() { EndHitStop(); }

    // Freezes time briefly (real-time length). Never starts during a pause / dialogue, and only ever extends a running hit-stop.
    void StartHitStop(float seconds)
    {
        if (seconds <= 0f || GameFlow.InputLocked || PauseMenu.IsOpen) return;
        hitStopEndsAt = Mathf.Max(hitStopEndsAt, Time.unscaledTime + seconds);
        Time.timeScale = 0f;
    }

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
            else { BufferDuringHitStop(); UpdateVisual(Time.time); return; } // frozen; keep the presses, still show the parry flash
        }
        if (GameFlow.InputLocked) // dialogue / transition / pause: freeze player timers, no jump-cut from the zeroed input
        {
            pendingParry = pendingDash = false;
            float dt = Time.deltaTime;
            stateEnteredAt += dt;
            dashCooldownUntil += dt;
            parryFlashUntil += dt;
            parryGuardUntil += dt;
            jumpBufferedUntil += dt;
            attackBufferedUntil += dt;
            lastGroundedAt += dt;
            health.ShiftInvulnerability(dt);
            if (state == State.Dash) { EnterState(State.Locomotion); rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); } // don't slide through teleports/dialogue
            if (sliding) { sliding = false; rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); } // same for an attack auto-approach (FixedUpdate stops writing it)
            UpdateVisual(Time.time);
            return;
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
            attackBufferedUntil = Mathf.Max(now, StateEndsAt()) + attackBuffer; // must outlive the current swing/parry/dash

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
                if (elapsed >= dashEndsAfter)
                {
                    if (dashBehindActive && dashBehindFaceBack) Facing = -Facing; // a held direction re-faces next frame
                    EnterState(State.Locomotion);
                }
                break;
            case State.Hurt:
                if (elapsed >= hurtDuration) EnterState(health.IsDead ? State.Dead : State.Locomotion);
                break;
        }

        pendingParry = pendingDash = false; // replayed (or ignored by the current state, like a live press) this frame
        if (state == State.Locomotion || state == State.Attack || state == State.Parry) UpdateJumpCut();
        UpdateVisual(now);
    }

    // Hit-stop freezes Time.time, so a buffer set now starts counting when it ends; WasPressedThisFrame is true for this frame only.
    void BufferDuringHitStop()
    {
        if (input.JumpPressed) jumpBufferedUntil = Time.time + jumpBuffer;
        if (input.AttackPressed && state != State.Hurt && state != State.Dead) attackBufferedUntil = Mathf.Max(Time.time, StateEndsAt()) + attackBuffer;
        pendingParry |= input.ParryPressed;
        pendingDash |= input.DashPressed;
    }

    // When the current Attack/Parry/Dash ends by itself (now for any other state).
    float StateEndsAt()
    {
        switch (state)
        {
            case State.Attack: return stateEnteredAt + attackDuration;
            case State.Parry: return stateEnteredAt + parryDuration;
            case State.Dash: return stateEnteredAt + dashEndsAfter;
        }
        return Time.time;
    }

    void UpdateLocomotion(float now)
    {
        if (Mathf.Abs(input.MoveX) > 0.01f) Facing = input.MoveX > 0f ? 1 : -1;

        if (TryDash(now)) return;
        if (ParryPressed) { EnterState(State.Parry); return; }
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
        if (!DashPressed || now < dashCooldownUntil) return false;
        if (Mathf.Abs(input.MoveX) > 0.01f) Facing = input.MoveX > 0f ? 1 : -1; // dash toward the held direction (also cancels an attack/parry)
        dashCooldownUntil = now + dashCooldown;
        EnterState(State.Dash);
        return true;
    }

    void EnterState(State next)
    {
        state = next;
        stateEnteredAt = Time.time;
        rb.gravityScale = gravityScale; // always restore (Dash zeroes it)
        sliding = false;                // any state change ends an auto-approach (dash cancel, hurt, death)

        switch (next)
        {
            case State.Locomotion:
                break;
            case State.Attack:
                attackHitDone = false;
                attackBufferedUntil = -999f;
                if (grounded) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                TryStartApproach();
                break;
            case State.Parry:
                attackBufferedUntil = -999f;
                if (grounded) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            case State.Dash:
                rb.gravityScale = 0f;
                DashStart = rb.position;
                dashBehindActive = TryPlanDashBehind(out float behindDist);
                dashEndsAfter = dashBehindActive ? Mathf.Clamp(behindDist / dashSpeed, dashMinDuration, 0.25f) : dashDuration;
                rb.linearVelocity = new Vector2(Facing * dashSpeed, 0f);
                Sfx.Play(SfxId.Dash);
                break;
            case State.Dead:
                rb.linearVelocity = Vector2.zero; // FixedUpdate does not write velocity in Dead, so the corpse stays put
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
            case State.Attack:
                if (!sliding || GameFlow.InputLocked) break;
                float remain = (slideTargetX - rb.position.x) * Facing;
                if (remain <= 0.02f) { sliding = false; rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); }
                else rb.linearVelocity = new Vector2(Facing * Mathf.Min(slideSpeed, remain / Time.fixedDeltaTime), rb.linearVelocity.y); // last step lands exactly on the target
                break;
        }
    }

    // Nearest enemy on the Facing side within maxDx / maxDy of the player's centre that can still be hit. null = none.
    TestEnemy FindTargetAhead(float maxDx, float maxDy, out Collider2D hit)
    {
        hit = null;
        TestEnemy best = null;
        float bestDx = float.MaxValue;
        Vector2 pos = rb.position;
        Vector2 center = pos + new Vector2(Facing * maxDx * 0.5f, 0f);
        foreach (Collider2D col in Physics2D.OverlapBoxAll(center, new Vector2(maxDx + 1f, maxDy * 2f + 2f), 0f, enemyMask))
        {
            TestEnemy enemy = col.GetComponentInParent<TestEnemy>();
            if (enemy == null || enemy.Current == TestEnemy.State.Stunned || enemy.Current == TestEnemy.State.Frozen || !enemy.gameObject.activeInHierarchy) continue; // downed / vanishing / frozen (STG-00)
            Vector2 d = (Vector2)enemy.transform.position - pos;
            float dx = d.x * Facing;
            if (dx <= 0f || dx > maxDx || Mathf.Abs(d.y) > maxDy || dx >= bestDx) continue;
            bestDx = dx;
            best = enemy;
            hit = col;
        }
        return best;
    }

    // Ground within ledgeProbeDepth under the feet at x (probe starts just above the feet so a floor we stand on counts).
    bool GroundBelow(float x) =>
        Physics2D.Raycast(new Vector2(x, rb.position.y + feetOffset.y + 0.1f), Vector2.down, ledgeProbeDepth, groundMask).collider != null;

    // A) Attack auto-approach: slide to attack reach during attackHitDelay. Grounded only, never off a ledge; walls stop it through physics (Continuous RB).
    void TryStartApproach()
    {
        if (!grounded) return;
        TestEnemy enemy = FindTargetAhead(approachRange, approachMaxDy, out _);
        if (enemy == null) return;
        slideTargetX = enemy.transform.position.x - Facing * approachStopDist;
        float dist = (slideTargetX - rb.position.x) * Facing;
        if (dist <= 0.05f || !GroundBelow(slideTargetX)) return; // already in reach / target over a gap
        slideSpeed = Mathf.Min(dist / attackHitDelay, approachMaxSpeed);
        sliding = true;
    }

    // B) Dash-behind: an enemy very close ahead -> dash to the clear spot just behind it. false = use the fixed-distance dash.
    bool TryPlanDashBehind(out float distance)
    {
        distance = 0f;
        TestEnemy enemy = FindTargetAhead(dashBehindRange, dashBehindMaxDy, out Collider2D col);
        if (enemy == null) return false;
        const float halfW = 0.45f, halfH = 1f; // player box 0.9 x 2 (PrototypeSceneBuilder)
        float targetX = enemy.transform.position.x + Facing * (col.bounds.extents.x + halfW + dashBehindClearance);
        distance = (targetX - rb.position.x) * Facing;
        Vector2 target = new Vector2(targetX, rb.position.y);
        if (Physics2D.OverlapBox(target, new Vector2(halfW * 2f - 0.1f, halfH * 2f - 0.2f), 0f, groundMask) != null) return false; // spot inside a wall
        if (grounded && !GroundBelow(targetX)) return false; // spot over a gap
        return true;
    }

    void DoAttackHit()
    {
        attackHitDone = true;
        if (sliding) { sliding = false; if (grounded) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); } // never keep sliding after the hit
        Sfx.Play(SfxId.Swing);
        ParryFx.Slash(AttackCenter(), visual != null ? visual.transform : null, Facing);
        Collider2D[] cols = Physics2D.OverlapBoxAll(AttackCenter(), attackSize, 0f, enemyMask);
        swingHits.Clear();
        foreach (Collider2D col in cols)
        {
            TestEnemy enemy = col.GetComponentInParent<TestEnemy>();
            if (enemy == null || !swingHits.Add(enemy)) continue; // one hit per enemy per swing
            bool damaged = enemy.ReceivePlayerHit(new HitInfo { Damage = attackDamage, SourcePosition = transform.position, IsSpecial = false });
            if (!damaged) continue;
            Sfx.Play(SfxId.Hit);
            PlayHitSpark(col.ClosestPoint(AttackCenter()), Facing);
            bool down = enemy.Current == TestEnemy.State.Stunned; // the hit that finishes the enemy
            StartHitStop(down ? Juice.HitStopKill : Juice.HitStopHit);
            Juice.Shake(down ? Juice.ShakeKillAmp : Juice.ShakeHitAmp, down ? Juice.ShakeKillTime : Juice.ShakeHitTime);
        }
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
        // projectiles judge by travel direction (a shot spawned inside the player's box has no meaningful position side)
        bool fromFront = !Mathf.Approximately(info.TravelDirX, 0f)
            ? Mathf.Sign(info.TravelDirX) == -Facing
            : Mathf.Approximately(dx, 0f) || Mathf.Sign(dx) == Facing;
        if ((ParryActive || Time.time < parryGuardUntil) && fromFront && !info.Unparryable)
        {
            // No knockback for either side. Hit-stop + flash + parry FX. A guard parry during an attack must not abort the swing.
            if (state == State.Parry) EnterState(State.Locomotion);
            parryFlashUntil = Time.time + parryFlashTime;
            parryGuardUntil = Time.time + parryGuardTime;
            SpriteFx.Play("parry", (Vector2)transform.position + new Vector2(Facing * 0.8f, 0f) - new Vector2(0f, 30f / SpriteFx.Ppu)); // burst centre is 30px above the pivot
            Sfx.Play(SfxId.Parry);
            StartHitStop(hitStopTime);
            Juice.Shake(Juice.ShakeParryAmp, Juice.ShakeParryTime);
            return HitResult.Parried;
        }

        health.TakeDamage(info.Damage);
        Sfx.Play(SfxId.Hurt);
        EnterState(State.Hurt);
        Juice.Shake(Juice.ShakeHurtAmp, Juice.ShakeHurtTime);
        float away = Mathf.Approximately(dx, 0f) ? -Facing : (dx > 0f ? -1f : 1f); // dx==0: knock back behind the player
        PlayHitSpark(transform.position, away); // blow travels away from the source
        rb.linearVelocity = new Vector2(health.IsDead ? 0f : away * knockback.x, knockback.y); // dying: no sideways push (could carry the corpse into an exit)
        return HitResult.Hit;
    }

    void UpdateVisual(float now)
    {
        if (visual == null) return;
        visual.flipX = Facing < 0;
        if (draftSprite) visual.sprite = DraftFrame(now);
        Color c = draftSprite ? Color.white : BaseColor;
        if (state == State.Dash) c = Color.white;
        else if (ParryActive) c = new Color(0.4f, 1f, 0.5f);
        else if (state == State.Parry) c = new Color(0.2f, 0.5f, 0.35f);
        else if (state == State.Dead) c = Color.gray;
        else if (state == State.Hurt) c = new Color(1f, 0.35f, 0.35f); // hurt flash
        else if (health.Invulnerable && Mathf.FloorToInt(now * 20f) % 2 == 0) c.a = 0.3f;
        if (now < parryFlashUntil) c = Color.white;
        visual.color = c;
    }

    // DraftSprites: one hero frame per state (walk strip is the fallback when another strip is missing). Art faces right; flipX handles left.
    Sprite DraftFrame(float now)
    {
        int t = Mathf.FloorToInt((now - stateEnteredAt) / (state == State.Dash ? dashEndsAfter / 2f : attackDuration / 3f));
        switch (state)
        {
            case State.Attack: return DraftSprites.Pick(DraftSprites.HeroAttack, t, DraftSprites.Hero);
            case State.Parry: return DraftSprites.Pick(DraftSprites.HeroParry, ParryActive ? 0 : 1, DraftSprites.HeroAttack ?? DraftSprites.Hero);
            case State.Dash: return DraftSprites.Pick(DraftSprites.HeroDash, t, DraftSprites.Hero);
            case State.Hurt:
            case State.Dead: return DraftSprites.Pick(DraftSprites.HeroHurt, 0, DraftSprites.Hero);
        }
        if (!grounded) return DraftSprites.Pick(DraftSprites.HeroJump, rb.linearVelocity.y > 0.1f ? 0 : 1, DraftSprites.Hero);
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
            return DraftSprites.Hero[Mathf.FloorToInt(now * DraftSprites.WalkFps) % 3];
        return DraftSprites.Pick(DraftSprites.HeroIdle, Mathf.FloorToInt(now * DraftSprites.IdleFps) % 2, DraftSprites.Hero);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(AttackCenter(), attackSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + feetOffset, feetSize);
    }
}
