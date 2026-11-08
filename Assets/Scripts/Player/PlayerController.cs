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
    // cooldown: DashRules.ShiftCooldown (DECISIONS 13)

    // DECISIONS 11/13 / docs/design/impl/movement-assist.md (attack auto-approach replaced by the DECISIONS 12 lunge: DashRules, impl/dash-attack.md)
    [Header("Ledge probe (T)")]
    [SerializeField] float ledgeProbeDepth = 1.5f;    // ground must exist this far below the target feet

    [Header("Dash behind (T)")] // search range / dy: DashRules.ShiftAssistRange / ShiftAssistDy
    [SerializeField] float dashBehindClearance = 0.3f; // gap between the two boxes at the landing spot
    [SerializeField] float dashMinDuration = 0.08f;

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
    bool lunging;                       // DECISIONS 12 lunge attack in progress (part of an Attack)
    Vector2 lungeStart, lungeEnd, lungeDir;
    int swingHitStops;                  // hit-stops already played by this swing / lunge (capped at DashRules.LungeMaxHitStops)
    ContactFilter2D enemyFilter;
    readonly Collider2D[] enemyHits = new Collider2D[32];
    float dashEndsAfter;               // length of the current dash (dashDuration, or shorter/longer for a dash-behind)
    bool dashBehindActive;
    TestEnemy dashTarget;               // DECISIONS 13: enemy the Shift dash goes behind and counters at the dash end
    Collider2D dashTargetCol;
    Vector2 dashVelocity;               // Shift dash velocity (horizontal, plus a small vertical step onto the target's ground level)
    bool pendingParry, pendingDash; // pressed during a hit-stop: replayed on the frame the hit-stop ends (jump/attack use their buffers)
    readonly System.Collections.Generic.HashSet<TestEnemy> swingHits = new System.Collections.Generic.HashSet<TestEnemy>();

    public int Facing { get; private set; } = 1;
    public State CurrentState => state;
    public bool Grounded => grounded;
    public bool ParryActive => state == State.Parry && Time.time - stateEnteredAt < parryActiveFor;
    float parryActiveFor; // parryActiveTime, or longer for an LMB parry so it covers the threat it was chosen for (DashRules.ParryCoverMargin)
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
        enemyFilter.SetLayerMask(enemyMask);
        enemyFilter.useTriggers = Physics2D.queriesHitTriggers; // same hits as the old OverlapBoxAll
        if (GetComponent<Shockwave>() == null) gameObject.AddComponent<Shockwave>(); // upgrade 8-A; players are built by scripts, so add at runtime
        MouseCrosshair.Create(this); // DECISIONS 12 aim cursor + lock-on brackets (separate object, runtime like Shockwave)
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
            EndLunge(); // same for a lunge (gravity back, velocity zeroed)
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
                if (lunging) UpdateLunge(now, elapsed);
                else if (!attackHitDone && elapsed >= attackHitDelay) DoAttackHit();
                if (elapsed >= attackHitDelay && TryDash(now)) break;
                if (elapsed >= attackDuration) EnterState(State.Locomotion);
                break;
            case State.Parry:
                if (elapsed >= parryDuration) EnterState(State.Locomotion);
                break;
            case State.Dash:
                SpawnAfterimage(now);
                if (elapsed >= dashEndsAfter)
                {
                    // DECISIONS 13: end turned around (a held direction re-faces next frame); with a target, counter it right away.
                    // Enemy strikes during the dash were already resolved as dodges (stagger / Android overload), so the counter comes after.
                    EnterState(State.Locomotion);
                    if (dashBehindActive) DoShiftCounter();
                    else Facing = -Facing;
                }
                break;
            case State.Hurt:
                if (elapsed >= hurtDuration) EnterState(health.IsDead ? State.Dead : State.Locomotion);
                break;
        }

        pendingParry = pendingDash = false; // replayed (or ignored by the current state, like a live press) this frame
        if (state == State.Locomotion || (state == State.Attack && !lunging) || state == State.Parry) UpdateJumpCut(); // an upward lunge is not a jump
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
        if (ParryPressed) { EnterState(State.Parry); return; } // RMB alias: parry facing as before
        if (input.AttackPressed || now <= attackBufferedUntil)
        {
            // DECISIONS 12-B: LMB = parry when a parryable threat is about to land, else lunge, else in-place swing.
            if (FaceParryThreat(out float eta))
            {
                EnterState(State.Parry);
                parryActiveFor = Mathf.Clamp(eta + DashRules.ParryCoverMargin, parryActiveTime, parryDuration);
            }
            else EnterState(State.Attack);
            return;
        }

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
        dashCooldownUntil = now + DashRules.ShiftCooldown;
        EnterState(State.Dash);
        return true;
    }

    void EnterState(State next)
    {
        EndLunge();                     // any state change ends a lunge (dash cancel, hurt, death)
        state = next;
        stateEnteredAt = Time.time;
        rb.gravityScale = gravityScale; // always restore (Dash / lunge zero it)

        switch (next)
        {
            case State.Locomotion:
                break;
            case State.Attack:
                attackHitDone = false;
                attackBufferedUntil = -999f;
                swingHits.Clear();
                swingHitStops = 0;
                Vector2 aim = MouseCrosshair.MouseWorld(out Vector2 mouse) ? mouse - rb.position : new Vector2(Facing, 0f);
                aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : new Vector2(Facing, 0f);
                if (Mathf.Abs(aim.x) > 0.01f) Facing = aim.x > 0f ? 1 : -1; // in-place swing faces the mouse too
                if (!TryStartLunge(aim) && grounded) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            case State.Parry:
                attackBufferedUntil = -999f;
                parryActiveFor = parryActiveTime;
                if (grounded) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
            case State.Dash:
                rb.gravityScale = 0f;
                DashStart = rb.position;
                dashBehindActive = TryPlanDashBehind(out Vector2 behindOffset);
                if (dashBehindActive)
                {
                    dashEndsAfter = Mathf.Clamp(Mathf.Abs(behindOffset.x) / dashSpeed, dashMinDuration, 0.25f);
                    dashVelocity = behindOffset / dashEndsAfter; // lands on the spot
                }
                else
                {
                    dashTarget = null;
                    dashEndsAfter = dashDuration;
                    dashVelocity = new Vector2(Facing * dashSpeed, 0f);
                }
                rb.linearVelocity = dashVelocity;
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
                rb.linearVelocity = dashVelocity;
                break;
            case State.Attack:
                if (!lunging || GameFlow.InputLocked) break;
                float remain = Vector2.Dot(lungeEnd - rb.position, lungeDir);
                rb.linearVelocity = lungeDir * Mathf.Clamp(remain / Time.fixedDeltaTime, 0f, DashRules.LungeSpeed); // last step lands exactly on the end point
                break;
        }
    }

    // Nearest enemy on the given side (+1 right / -1 left) within maxDx / maxDy of the player's centre that can still be hit. null = none.
    TestEnemy FindTargetAhead(int side, float maxDx, float maxDy, out Collider2D hit)
    {
        hit = null;
        TestEnemy best = null;
        float bestDx = float.MaxValue;
        Vector2 pos = rb.position;
        Vector2 center = pos + new Vector2(side * maxDx * 0.5f, 0f);
        foreach (Collider2D col in Physics2D.OverlapBoxAll(center, new Vector2(maxDx + 1f, maxDy * 2f + 2f), 0f, enemyMask))
        {
            TestEnemy enemy = col.GetComponentInParent<TestEnemy>();
            if (enemy == null || enemy.Current == TestEnemy.State.Stunned || enemy.Current == TestEnemy.State.Frozen || !enemy.gameObject.activeInHierarchy) continue; // downed / vanishing / frozen (STG-00)
            Vector2 d = (Vector2)enemy.transform.position - pos;
            float dx = d.x * side;
            if (dx <= 0f || dx > maxDx || Mathf.Abs(d.y) > maxDy || dx >= bestDx) continue;
            bestDx = dx;
            best = enemy;
            hit = col;
        }
        return best;
    }

    // Ground within ledgeProbeDepth under the feet of a player centred at `at` (probe starts just above the feet so a floor we stand on counts).
    bool GroundBelow(Vector2 at) =>
        Physics2D.Raycast(new Vector2(at.x, at.y + feetOffset.y + 0.1f), Vector2.down, ledgeProbeDepth, groundMask).collider != null;

    // DECISIONS 12 lock-on: nearest hittable enemy within LungeRange + LungeTolerance (centre to centre) inside the +-LungeConeDeg cone around aim.
    // Also used by MouseCrosshair for the lock-on brackets. null = none.
    public TestEnemy FindLungeTarget(Vector2 aim, out Collider2D hit)
    {
        hit = null;
        TestEnemy best = null;
        Vector2 pos = rb.position;
        float range = DashRules.LungeRange + DashRules.LungeTolerance;
        float bestDist = float.MaxValue;
        float minDot = Mathf.Cos(DashRules.LungeConeDeg * Mathf.Deg2Rad);
        int n = Physics2D.OverlapCircle(pos, range, enemyFilter, enemyHits);
        for (int i = 0; i < n; i++)
        {
            Collider2D col = enemyHits[i];
            TestEnemy enemy = col.GetComponentInParent<TestEnemy>();
            if (enemy == null || enemy.Current == TestEnemy.State.Stunned || enemy.Current == TestEnemy.State.Frozen || !enemy.gameObject.activeInHierarchy) continue;
            Vector2 d = (Vector2)enemy.transform.position - pos;
            float dist = d.magnitude;
            if (dist > range || dist >= bestDist || (dist > 0.001f && Vector2.Dot(d / dist, aim) < minDot)) continue;
            bestDist = dist;
            best = enemy;
            hit = col;
        }
        return best;
    }

    // Lunge toward the locked target: through it (end LungeBehind past its centre) or, if it blocks hits (Android), LungeStopInFront before it.
    // Walls clamp the line; on the ground the end must not hang over a pit. false = no target / no room -> in-place swing.
    bool TryStartLunge(Vector2 aim)
    {
        TestEnemy target = FindLungeTarget(aim, out _);
        if (target == null) return false;
        Vector2 pos = rb.position;
        Vector2 d = (Vector2)target.transform.position - pos;
        float tDist = d.magnitude;
        Vector2 dir = tDist > 0.001f ? d / tDist : new Vector2(Facing, 0f);
        float wall = WallDistance(pos, dir, tDist + DashRules.LungeBehind);
        float len = Mathf.Min(target.BlocksHits ? tDist - DashRules.LungeStopInFront : tDist + DashRules.LungeBehind, wall);
        if (grounded && !GroundBelow(pos + dir * len))
        {
            len = Mathf.Min(tDist + 0.3f, wall); // just past it
            if (!GroundBelow(pos + dir * len)) len = Mathf.Min(tDist - DashRules.LungeStopInFront, wall); // stop in front
            if (!GroundBelow(pos + dir * len)) return false;
        }
        if (len < 0.2f) return false; // already there (e.g. an Android right in front): plain swing

        if (Mathf.Abs(dir.x) > 0.01f) Facing = dir.x > 0f ? 1 : -1;
        lunging = true;
        lungeStart = pos;
        lungeDir = dir;
        lungeEnd = pos + dir * len;
        attackHitDone = true; // the sweep replaces the in-place hit box
        nextAfterimageAt = 0f;
        rb.gravityScale = 0f;
        rb.linearVelocity = dir * DashRules.LungeSpeed;
        Sfx.Play(SfxId.Swing);
        return true;
    }

    // Free distance along dir for a slightly slimmer player box (0.8 x 1.8 of 0.9 x 2). One-way platforms are ignored unless the
    // lunge heads downward: the effector blocks a body coming from above, so such a lunge would hang on the platform until LungeMaxTime.
    float WallDistance(Vector2 pos, Vector2 dir, float maxLen)
    {
        float best = maxLen;
        foreach (RaycastHit2D h in Physics2D.BoxCastAll(pos, new Vector2(0.8f, 1.8f), 0f, dir, maxLen, groundMask))
            if ((!h.collider.usedByEffector || dir.y < -0.01f) && h.distance < best) best = h.distance;
        return Mathf.Min(maxLen, best - 0.05f);
    }

    void UpdateLunge(float now, float elapsed)
    {
        SpawnAfterimage(now);
        SweepLunge();
        if (lunging && (Vector2.Dot(lungeEnd - rb.position, lungeDir) <= 0.05f || elapsed >= DashRules.LungeMaxTime)) EndLunge();
    }

    // Kill sweep: everything between the lunge start and LungeTolerance ahead of the body. A blocking enemy (Android) stops the lunge.
    void SweepLunge()
    {
        Vector2 front = rb.position + lungeDir * DashRules.LungeTolerance;
        Vector2 span = front - lungeStart;
        float len = span.magnitude;
        if (len < 0.01f) return;
        float angle = Mathf.Atan2(lungeDir.y, lungeDir.x) * Mathf.Rad2Deg;
        int n = Physics2D.OverlapBox((lungeStart + front) * 0.5f, new Vector2(len, DashRules.LungeSweepWidth), angle, enemyFilter, enemyHits);
        for (int i = 0; i < n; i++)
            if (!HitEnemy(enemyHits[i].GetComponentInParent<TestEnemy>(), enemyHits[i], rb.position, true)) { EndLunge(); return; }
    }

    void EndLunge()
    {
        if (!lunging) return;
        lunging = false;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = Vector2.zero;
    }

    void SpawnAfterimage(float now)
    {
        if (visual == null || now < nextAfterimageAt) return;
        nextAfterimageAt = now + afterimageInterval;
        SpriteFx.Afterimage(visual, AfterimageColor, afterimageLife);
    }

    // DECISIONS 12-B: a parryable shot heading at the player within ParryShotRange, or a melee enemy winding up / striking with the
    // player inside its attack box. Faces the nearest such threat (parry is front-only) and returns true. eta = seconds until it lands.
    bool FaceParryThreat(out float eta)
    {
        Vector2 pos = rb.position;
        float bestDist = float.MaxValue;
        int face = 0;
        eta = 0f;
        foreach (EnemyProjectile p in EnemyProjectile.Active)
        {
            if (p == null || !p.Parryable) continue;
            Vector2 to = pos - (Vector2)p.transform.position;
            float dist = to.magnitude;
            Vector2 v = p.Velocity;
            if (dist > DashRules.ParryShotRange || dist >= bestDist || v.sqrMagnitude < 0.0001f || Vector2.Dot(v, to) <= 0f) continue;
            Vector2 vn = v.normalized;
            if (Mathf.Abs(vn.x * to.y - vn.y * to.x) > DashRules.ParryShotLateral) continue; // passes beside the body
            bestDist = dist;
            face = Mathf.Abs(v.x) > 0.01f ? (v.x > 0f ? -1 : 1) : (to.x < 0f ? 1 : -1); // ReceiveEnemyHit judges shots by travel direction
            eta = Mathf.Max(0f, dist - 0.7f) / v.magnitude; // contact ~0.7u before the centre (player half 0.45 + shot half 0.25)
        }
        int n = Physics2D.OverlapCircle(pos, 4f, enemyFilter, enemyHits);
        for (int i = 0; i < n; i++)
        {
            TestEnemy enemy = enemyHits[i].GetComponentInParent<TestEnemy>();
            if (enemy == null || !enemy.MeleeThreatens(pos, DashRules.ParryMeleeWindow, out float strikeEta)) continue;
            float dist = Vector2.Distance(enemy.transform.position, pos);
            if (dist >= bestDist) continue;
            bestDist = dist;
            eta = strikeEta;
            face = enemy.transform.position.x >= pos.x ? 1 : -1;
        }
        if (face == 0) return false;
        Facing = face;
        return true;
    }

    // DECISIONS 13 Shift: nearest enemy within ShiftAssistRange / ShiftAssistDy, the Facing side (= held direction, set by TryDash)
    // first, then the other side. Plans the dash to the clear spot just behind it and turns Facing toward that side.
    // offset = dash displacement. false = no usable target (none, or every behind spot is in a wall / over a gap / walled off).
    bool TryPlanDashBehind(out Vector2 offset)
    {
        offset = Vector2.zero;
        dashTarget = null;
        int first = Facing;
        for (int k = 0; k < 2; k++)
        {
            int side = k == 0 ? first : -first;
            TestEnemy enemy = FindTargetAhead(side, DashRules.ShiftAssistRange, DashRules.ShiftAssistDy, out Collider2D col);
            if (enemy == null || !PlanBehind(enemy, col, side, out offset)) continue;
            Facing = side;
            dashTarget = enemy;
            dashTargetCol = col;
            return true;
        }
        return false;
    }

    bool PlanBehind(TestEnemy enemy, Collider2D col, int side, out Vector2 offset)
    {
        const float halfW = 0.45f, halfH = 1f; // player box 0.9 x 2 (PrototypeSceneBuilder)
        Vector2 pos = rb.position;
        float targetX = enemy.transform.position.x + side * (col.bounds.extents.x + halfW + dashBehindClearance);
        // small step onto the enemy's ground level (its feet vs ours); a bigger height gap keeps the dash horizontal
        float step = col.bounds.min.y - (pos.y + feetOffset.y);
        Vector2 target = new Vector2(targetX, Mathf.Abs(step) <= DashRules.ShiftStepDy ? pos.y + step : pos.y);
        offset = target - pos;
        float dist = offset.x * side;
        if (dist <= 0.05f) return false;
        if (WallDistance(pos, new Vector2(side, 0f), dist) < dist - 0.01f) return false; // wall between us and the spot
        if (Physics2D.OverlapBox(target, new Vector2(halfW * 2f - 0.1f, halfH * 2f - 0.2f), 0f, groundMask) != null) return false; // spot inside a wall
        if (grounded && !GroundBelow(target)) return false; // spot over a gap
        return true;
    }

    // DECISIONS 13 counter at the end of a Shift dash-behind: turn to the target and strike it (same HitEnemy path as LMB).
    // Normal enemies die in one hit; a non-overloaded Android blocks (white flash + clank). Other enemies in the swing box are hit too.
    void DoShiftCounter()
    {
        TestEnemy target = dashTarget;
        dashTarget = null;
        if (target != null && target.gameObject.activeInHierarchy)
        {
            float dx = target.transform.position.x - rb.position.x;
            if (Mathf.Abs(dx) > 0.01f) Facing = dx > 0f ? 1 : -1;
        }
        else Facing = -Facing;
        swingHits.Clear();
        swingHitStops = 0;
        Sfx.Play(SfxId.Swing);
        ParryFx.Slash(AttackCenter(), visual != null ? visual.transform : null, Facing);
        if (target != null && dashTargetCol != null && !HitEnemy(target, dashTargetCol, rb.position, false)) Sfx.Play(SfxId.Parry, 0.5f); // blocked: clank
        int n = Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyFilter, enemyHits);
        for (int i = 0; i < n; i++)
            HitEnemy(enemyHits[i].GetComponentInParent<TestEnemy>(), enemyHits[i], AttackCenter(), false);
        dashTargetCol = null;
    }

    void DoAttackHit()
    {
        attackHitDone = true;
        Sfx.Play(SfxId.Swing);
        ParryFx.Slash(AttackCenter(), visual != null ? visual.transform : null, Facing);
        int n = Physics2D.OverlapBox(AttackCenter(), attackSize, 0f, enemyFilter, enemyHits);
        for (int i = 0; i < n; i++)
            HitEnemy(enemyHits[i].GetComponentInParent<TestEnemy>(), enemyHits[i], AttackCenter(), false);
    }

    // One blow (in-place swing or lunge sweep). false = the enemy blocked it (Android not overloaded).
    // One hit per enemy per swing; hit-stop + shake only for the first LungeMaxHitStops blows.
    bool HitEnemy(TestEnemy enemy, Collider2D col, Vector2 from, bool lunge)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.Current == TestEnemy.State.Stunned) return true; // already down / vanishing
        if (!swingHits.Add(enemy)) return true;
        bool damaged = enemy.ReceivePlayerHit(new HitInfo { Damage = attackDamage, SourcePosition = transform.position, IsSpecial = false });
        if (!damaged) return false;
        Sfx.Play(SfxId.Hit);
        PlayHitSpark(col.ClosestPoint(from), Facing);
        bool down = enemy.Current == TestEnemy.State.Stunned; // the hit that finishes the enemy
        if (lunge && down)
            for (int i = 0; i < DashRules.LungeKillSparks; i++) // cheap "shatter"
                PlayHitSpark((Vector2)enemy.transform.position + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.5f, 0.6f)), Random.value < 0.5f ? -1f : 1f);
        if (swingHitStops < DashRules.LungeMaxHitStops)
        {
            swingHitStops++;
            StartHitStop(down ? Juice.HitStopKill : Juice.HitStopHit);
            Juice.Shake(down ? Juice.ShakeKillAmp : Juice.ShakeHitAmp, down ? Juice.ShakeKillTime : Juice.ShakeHitTime);
        }
        return true;
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
            StartHitStop(Mathf.Max(hitStopTime, DashRules.ParryHitStop));
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
        if (state == State.Dash || lunging) c = Color.white;
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
