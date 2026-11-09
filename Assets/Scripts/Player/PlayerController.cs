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

    [Header("Speed FX (T)")] // docs/design/impl/juice.md "속도감 FX"
    [SerializeField] float runFxSpeedFrac = 0.9f;     // |vx| >= this * moveSpeed on the ground counts as running
    [SerializeField] float runDustInterval = 0.25f;
    [SerializeField] float speedLinesInterval = 0.15f;

    static readonly Color AfterimageColor = new Color(0.298f, 0.909f, 0.847f, 0.6f); // #4CE8D8
    static readonly Color BaseColor = new Color(0.298f, 0.878f, 1f);
    float hitStopEndsAt = -1f; // real time; <0 = no hit-stop running
    float hitStopEndedAt = -10f; // real time the last hit-stop ended (DashRules.HitStopGap)
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
    float nextRunDustAt, nextSpeedLinesAt;
    bool fxWasGrounded = true; // true at start: no spurious landing puff on the first unlocked frame (scene load / after the intro dialogue)
    int fxLastFacing;
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
    bool pendingDash; // pressed during a hit-stop: replayed on the frame the hit-stop ends (jump/attack use their buffers)
    readonly System.Collections.Generic.HashSet<TestEnemy> swingHits = new System.Collections.Generic.HashSet<TestEnemy>();

    // Dev readout (FrameStats, F3): parries tried (Parry state entered) vs. landed, and hits taken while a parry was up.
    public static int ParryTries, ParryCount, ParryFailHits;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetParryStats() => ParryTries = ParryCount = ParryFailHits = 0; // Domain Reload is off

    public int Facing { get; private set; } = 1;
    public State CurrentState => state;
    public bool Grounded => grounded;
    public bool ParryActive => state == State.Parry && Time.time - stateEnteredAt < parryActiveFor;
    float parryActiveFor; // parryActiveTime, or longer for an LMB parry so it covers the threat it was chosen for (DashRules.ParryCoverMargin)
    float ParryLength => Mathf.Max(parryDuration, parryActiveFor); // the Parry state never ends while its active window is still open
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownUntil - Time.time);
    public bool LungingDown => lunging && lungeDir.y < -0.2f; // TestEnemy Android guard: a downward lunge counts as "from above"
    public Vector2 DashStart { get; private set; } // where the current dash began (TestEnemy: a dash-behind that left the hitbox before the strike still counts as a dash-through)
    public LayerMask EnemyMask => enemyMask;
    bool DashPressed => input.DashPressed || pendingDash;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
        health = GetComponent<PlayerHealth>();
        visual = GetComponentInChildren<SpriteRenderer>();
        draftSprite = DraftSprites.Setup(visual, DraftSprites.Hero);
        rb.gravityScale = gravityScale;
        bodyCols = GetComponents<Collider2D>();
        enemyFilter.SetLayerMask(enemyMask);
        enemyFilter.useTriggers = Physics2D.queriesHitTriggers; // same hits as the old OverlapBoxAll
        if (GetComponent<Shockwave>() == null) gameObject.AddComponent<Shockwave>(); // upgrade 8-A; players are built by scripts, so add at runtime
        MouseCrosshair.Create(this); // DECISIONS 12 aim cursor + lock-on brackets (separate object, runtime like Shockwave)
    }

    void OnDisable() { EndHitStop(); RestorePassedPlatforms(true); }

    // Freezes time briefly (real-time length). Never starts during a pause / dialogue, and only ever extends a running hit-stop.
    bool hitStopCountsForGap; // false while only a parry stop runs: the riposte kill right after a parry keeps its freeze

    void StartHitStop(float seconds, bool countsForGap = true)
    {
        if (seconds <= 0f || GameFlow.InputLocked || PauseMenu.IsOpen) return;
        hitStopCountsForGap = (hitStopEndsAt >= 0f && hitStopCountsForGap) || countsForGap;
        hitStopEndsAt = Mathf.Max(hitStopEndsAt, Time.unscaledTime + seconds);
        Time.timeScale = 0f;
    }

    void EndHitStop()
    {
        if (hitStopEndsAt < 0f) return;
        hitStopEndsAt = -1f;
        if (hitStopCountsForGap) hitStopEndedAt = Time.unscaledTime;
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
            pendingDash = false;
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
        if (!lunging && passedPlatforms.Count > 0) RestorePassedPlatforms(false);
        Vector2 feet = (Vector2)transform.position + feetOffset;
        groundFilter.SetLayerMask(groundMask);
        int hitCount = Physics2D.OverlapBox(feet, feetSize, 0f, groundFilter, groundHits);
        grounded = false;
        for (int i = 0; i < hitCount && !grounded; i++) // a platform a down-lunge is still passing through is not ground
            grounded = !groundHits[i].OverlapPoint(feet + Vector2.up * 0.03f) && !passedPlatforms.Contains(groundHits[i]);
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
                if (elapsed >= ParryLength) EnterState(State.Locomotion);
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

        pendingDash = false; // replayed (or ignored by the current state, like a live press) this frame
        if (state == State.Locomotion || (state == State.Attack && !lunging) || state == State.Parry) UpdateJumpCut(); // an upward lunge is not a jump
        UpdateSpeedFx(now);
        UpdateVisual(now);
    }

    // Hit-stop freezes Time.time, so a buffer set now starts counting when it ends; WasPressedThisFrame is true for this frame only.
    void BufferDuringHitStop()
    {
        if (input.JumpPressed) jumpBufferedUntil = Time.time + jumpBuffer;
        if (input.AttackPressed && state != State.Hurt && state != State.Dead) attackBufferedUntil = Mathf.Max(Time.time, StateEndsAt()) + attackBuffer;
        pendingDash |= input.DashPressed;
    }

    // When the current Attack/Parry/Dash ends by itself (now for any other state).
    float StateEndsAt()
    {
        switch (state)
        {
            case State.Attack: return stateEnteredAt + attackDuration;
            case State.Parry: return stateEnteredAt + ParryLength;
            case State.Dash: return stateEnteredAt + dashEndsAfter;
        }
        return Time.time;
    }

    void UpdateLocomotion(float now)
    {
        if (Mathf.Abs(input.MoveX) > 0.01f) Facing = input.MoveX > 0f ? 1 : -1;

        if (TryDash(now)) return;
        if (input.AttackPressed || now <= attackBufferedUntil)
        {
            // DECISIONS 12-B: LMB = parry when a parryable threat is about to land, else lunge, else in-place swing.
            if (FaceParryThreat(out float eta))
            {
                EnterState(State.Parry);
                // No upper cap: a strike seen at eta 0.3 lands up to 2 frames later (Windup->Strike->DoStrike); the Parry state stretches to match.
                // Shots are bounded by their eligibility (ParryShotRange / ParryShotWindow), so the guard always lasts until they land.
                parryActiveFor = Mathf.Max(eta + DashRules.ParryCoverMargin, parryActiveTime);
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
                ParryTries++;
                attackBufferedUntil = -999f;
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
                PlayDashBurst(dashVelocity.x);
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
            if (!ClearLine(pos, enemy.transform.position)) continue; // behind a wall / under a roof: the lunge would only hit the wall
            bestDist = dist;
            best = enemy;
            hit = col;
        }
        return best;
    }

    // Line of sight between centres: no solid ground in between. One-way platforms never block it (a downward lunge passes them, see TryStartLunge).
    bool ClearLine(Vector2 from, Vector2 to)
    {
        foreach (RaycastHit2D h in Physics2D.LinecastAll(from, to, groundMask))
            if (!h.collider.usedByEffector) return false;
        return true;
    }

    // One-way platforms a downward lunge passes through (collision with the body off until the body is clear of them again).
    readonly System.Collections.Generic.List<Collider2D> passedPlatforms = new System.Collections.Generic.List<Collider2D>();
    readonly System.Collections.Generic.List<RaycastHit2D> lungePass = new System.Collections.Generic.List<RaycastHit2D>();
    Collider2D[] bodyCols;

    void SetPlatformPass(Collider2D platform, bool pass)
    {
        foreach (Collider2D b in bodyCols)
            if (b != null && !b.isTrigger) Physics2D.IgnoreCollision(b, platform, pass);
    }

    bool BodyOverlaps(Collider2D platform)
    {
        foreach (Collider2D b in bodyCols)
            if (b != null && !b.isTrigger && b.Distance(platform).isOverlapped) return true;
        return false;
    }

    // After the lunge: give each passed platform its collision back once the body no longer overlaps it (else the effector would pop the body up).
    void RestorePassedPlatforms(bool all)
    {
        for (int i = passedPlatforms.Count - 1; i >= 0; i--)
        {
            Collider2D p = passedPlatforms[i];
            if (p != null && !all && BodyOverlaps(p)) continue;
            if (p != null) SetPlatformPass(p, false);
            passedPlatforms.RemoveAt(i);
        }
    }

    // Lunge toward the locked target: through it (end LungeBehind past its centre) or, if it blocks hits (Android), LungeStopInFront before it.
    // Walls clamp the line; on the ground the end must not hang over a pit. false = no target / no room -> in-place swing.
    bool TryStartLunge(Vector2 aim)
    {
        TestEnemy target = FindLungeTarget(aim, out Collider2D targetCol);
        if (target == null) return false;
        Vector2 pos = rb.position;
        Vector2 d = (Vector2)target.transform.position - pos;
        float tDist = d.magnitude;
        Vector2 dir = tDist > 0.001f ? d / tDist : new Vector2(Facing, 0f);
        // Downward: one-way platforms above the target's feet (the catwalk under the player, any in between) are passed through;
        // the platform the target stands on and solid ground still stop the lunge.
        float passAbove = dir.y < -0.01f ? targetCol.bounds.min.y + 0.05f : float.PositiveInfinity;
        lungePass.Clear();
        float wall = WallDistance(pos, dir, tDist + DashRules.LungeBehind, passAbove, lungePass);
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
        foreach (RaycastHit2D h in lungePass)
            if (h.distance < len && !passedPlatforms.Contains(h.collider)) { SetPlatformPass(h.collider, true); passedPlatforms.Add(h.collider); }
        attackHitDone = true; // the sweep replaces the in-place hit box
        nextAfterimageAt = 0f;
        rb.gravityScale = 0f;
        rb.linearVelocity = dir * DashRules.LungeSpeed;
        PlayDashBurst(dir.x);
        Sfx.Play(SfxId.Swing);
        return true;
    }

    // Free distance along dir for a slightly slimmer player box (0.8 x 1.8 of 0.9 x 2). One-way platforms are ignored unless the
    // lunge heads downward; then those whose top is above passAbove go into pass (TryStartLunge turns their collision off) and the rest
    // (the target's own platform) count like ground.
    // A floor under a shallow down-slanted lunge (or a ceiling over a shallow up-slanted one) is not a wall: the body slides along it,
    // so it must not cut the lunge short (playtest 11-09 "the attack stops at walls when an enemy is there").
    float WallDistance(Vector2 pos, Vector2 dir, float maxLen, float passAbove = float.PositiveInfinity,
        System.Collections.Generic.List<RaycastHit2D> pass = null)
    {
        float best = maxLen;
        foreach (RaycastHit2D h in Physics2D.BoxCastAll(pos, new Vector2(0.8f, 1.8f), 0f, dir, maxLen, groundMask))
        {
            if (h.collider.usedByEffector && dir.y >= -0.01f) continue;
            if (h.collider.usedByEffector && h.collider.bounds.max.y > passAbove) { pass?.Add(h); continue; }
            if ((h.normal.y > 0.7f && dir.y > -0.5f) || (h.normal.y < -0.7f && dir.y < 0.5f)) continue; // slide along floor / ceiling
            if (h.distance < best) best = h.distance;
        }
        return Mathf.Min(maxLen, best - 0.05f);
    }

    void UpdateLunge(float now, float elapsed)
    {
        SpawnAfterimage(now);
        SweepLunge();
        if (!lunging) return;
        float remain = Vector2.Dot(lungeEnd - rb.position, lungeDir);
        // Held up by geometry the cast did not predict (a corner, a step): end where the body stands instead of pushing until LungeMaxTime.
        // Compared with the speed FixedUpdate asked for (it slows down near the end), so the last steps never count as blocked.
        bool blocked = elapsed > 2f * Time.fixedDeltaTime &&
            Vector2.Dot(rb.linearVelocity, lungeDir) < 0.25f * Mathf.Min(DashRules.LungeSpeed, remain / Time.fixedDeltaTime);
        if (remain <= 0.05f || blocked || elapsed >= DashRules.LungeMaxTime) EndLunge();
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

    // Speed FX (all sheets face right; flipX for left). Only called on unfrozen, unlocked frames, so nothing new spawns during
    // hit-stop / dialogue / pause. run-dust at the heel while running full speed (+ once on landing / turning);
    // speed-lines on the body while running, dashing or lunging, rotated to a diagonal lunge.
    void UpdateSpeedFx(float now)
    {
        if (visual == null) return;
        bool running = grounded && state == State.Locomotion && Mathf.Abs(rb.linearVelocity.x) >= moveSpeed * runFxSpeedFrac;
        bool landed = grounded && !fxWasGrounded && state != State.Dead;
        bool turned = running && Facing != fxLastFacing;
        fxWasGrounded = grounded;
        fxLastFacing = Facing;
        if (landed || turned || (running && now >= nextRunDustAt))
        {
            nextRunDustAt = now + runDustInterval;
            SpriteFx.PlayBehind("run-dust", rb.position + feetOffset, Facing < 0, visual);
        }

        if (!(running || state == State.Dash || lunging) || now < nextSpeedLinesAt) return;
        nextSpeedLinesAt = now + speedLinesInterval;
        Vector2 dir = lunging ? lungeDir : state == State.Dash ? dashVelocity : new Vector2(Facing, 0f);
        bool left = Mathf.Abs(dir.x) > 0.01f ? dir.x < 0f : Facing < 0;
        if (left) dir = -dir; // flipped sprite: rotate its mirrored forward onto the move direction
        SpriteFx.PlayBehind("speed-lines", rb.position, left, visual, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, transform);
    }

    // Once at the start of a Shift dash / lunge, at the heel, facing the move direction. Speed lines start the same frame.
    void PlayDashBurst(float dirX)
    {
        nextSpeedLinesAt = 0f;
        if (visual == null) return;
        bool left = Mathf.Abs(dirX) > 0.01f ? dirX < 0f : Facing < 0;
        SpriteFx.PlayBehind("dash-burst", rb.position + feetOffset, left, visual);
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
            if (dist >= bestDist || v.sqrMagnitude < 0.0001f || Vector2.Dot(v, to) <= 0f) continue;
            float shotEta = Mathf.Max(0f, dist - 0.7f) / v.magnitude; // contact ~0.7u before the centre (player half 0.45 + shot half 0.25)
            if (dist > DashRules.ParryShotRange && shotEta > DashRules.ParryShotWindow) continue; // QA R2-04: near OR arriving soon
            Vector2 vn = v.normalized;
            if (Mathf.Abs(vn.x * to.y - vn.y * to.x) > DashRules.ParryShotLateral) continue; // passes beside the body
            if (Physics2D.Raycast(p.transform.position, vn, Mathf.Max(0f, dist - 0.7f), groundMask).collider != null) continue; // dies on a wall first
            bestDist = dist;
            face = Mathf.Abs(v.x) > 0.01f ? (v.x > 0f ? -1 : 1) : (to.x < 0f ? 1 : -1); // ReceiveEnemyHit judges shots by travel direction
            eta = shotEta;
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
        // A lunge hit comes from where the lunge began (the sweep spans from there): the Android "from above / behind" test must not
        // depend on which frame the fast-moving body happened to be sampled at (Fable review 11-09).
        bool damaged = enemy.ReceivePlayerHit(new HitInfo { Damage = attackDamage, SourcePosition = lunge ? lungeStart : (Vector2)transform.position, IsSpecial = false });
        if (!damaged) return false;
        Sfx.Play(SfxId.Hit);
        PlayHitSpark(col.ClosestPoint(from), Facing);
        bool down = enemy.Current == TestEnemy.State.Stunned; // the hit that finishes the enemy
        if (down)
            for (int i = 0; i < Juice.KillSparks; i++) // cheap "shatter"
                PlayHitSpark((Vector2)enemy.transform.position + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.5f, 0.6f)), Random.value < 0.5f ? -1f : 1f);
        if (swingHitStops < DashRules.LungeMaxHitStops)
        {
            swingHitStops++;
            // a blow landing right after a hit-stop ended (2nd lunge kill, chained swings) gets no new freeze: back-to-back stops read as lag
            if (hitStopEndsAt >= 0f || Time.unscaledTime - hitStopEndedAt >= DashRules.HitStopGap)
                StartHitStop(down ? Juice.HitStopKill : Juice.HitStopHit);
            Vector2 blow = (Vector2)enemy.transform.position - rb.position; // player -> enemy (`from` may be the swing box centre, past the enemy)
            if (down) Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime, blow.sqrMagnitude > 0.0001f ? blow : new Vector2(Facing, 0f));
            else Juice.Shake(Juice.ShakeHitAmp, Juice.ShakeHitTime);
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
            ParryCount++;
            Vector2 contact = (Vector2)transform.position + new Vector2(Facing * 0.8f, 0f);
            SpriteFx.Play("parry", contact - new Vector2(0f, 30f / SpriteFx.Ppu)); // burst centre is 30px above the pivot
            ParryFeedback.Show(contact); // screen flash + "PARRY!" popup (playtest 11-09: parries were not readable)
            Sfx.Play(SfxId.Parry);
            StartHitStop(Mathf.Max(hitStopTime, DashRules.ParryHitStop), false);
            Juice.Shake(Juice.ShakeParryAmp, Juice.ShakeParryTime);
            return HitResult.Parried;
        }

        if (state == State.Parry) ParryFailHits++;
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
        SpriteFlash.Apply(visual, c, now < parryFlashUntil, Color.white, Juice.ParryPlayerTint); // flash shader: solid white; else the tint
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
        {
            Sprite[] run = DraftSprites.HeroRun ?? DraftSprites.Hero; // DECISIONS 15: normal movement shows the run cycle (walk = fallback)
            return run[Mathf.FloorToInt(now * (run == DraftSprites.Hero ? DraftSprites.WalkFps : DraftSprites.RunFps)) % run.Length];
        }
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
