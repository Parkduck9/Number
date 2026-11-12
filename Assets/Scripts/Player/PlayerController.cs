using UnityEngine;

// State machine + movement + attack/parry/dash. See docs/design/PROTOTYPE_ARCH.md section 2/3.
// Update = input, timers, transitions, hit checks. FixedUpdate = velocity writes.
[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInputReader), typeof(PlayerHealth))]
public class PlayerController : MonoBehaviour
{
    public enum State { Locomotion, Attack, Parry, Dash, Hurt, Dead, Issen, Grapple } // Issen: Shift flash strike follow-through (DECISIONS 26); Grapple: RMB rope pull (DECISIONS 16). Dash: Shift with no issen (DECISIONS 35)

    [Header("Masks")]
    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask enemyMask;

    [Header("Movement (T)")]
    [SerializeField] float moveSpeed = 10f; // DECISIONS 22: 8 -> 10
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
    // cooldown: DashRules.ShiftDashCooldown (DECISIONS 35)

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

    [Header("Dash afterimage (T)")]
    [SerializeField] float afterimageInterval = 0.04f;
    [SerializeField] float afterimageLife = 0.2f;

    [Header("Speed FX (T)")] // docs/design/impl/juice.md "속도감 FX"
    [SerializeField] float runFxSpeedFrac = 0.9f;     // |vx| >= this * moveSpeed on the ground counts as running
    [SerializeField] float speedLinesInterval = 0.15f;

    // Run feel (presentation only, never touches the body): forward lean, step bob, footfall dust, start / skid lean.
    const float RunLean = 7f, StartLean = 12f, SkidLean = 6f; // degrees
    const float LeanTime = 0.08f, StartLeanTime = 0.1f, SkidTime = 0.08f; // s

    static readonly Color AfterimageColor = new Color(0.298f, 0.909f, 0.847f, 0.6f); // #4CE8D8
    static readonly Color BaseColor = new Color(0.298f, 0.878f, 1f);
    // DECISIONS 26 hit vs parry readability (T)
    static readonly Color HurtRed = new Color(1f, 0.15f, 0.15f);
    const float HurtBlinkHz = 30f;               // red flash ticks per second while in Hurt
    const float HurtPitch = 0.8f, ParryPitch = 1.3f;
    float hitStopEndsAt = -1f; // real time; <0 = no hit-stop running
    float hitStopEndedAt = -10f; // real time the last hit-stop ended (DashRules.HitStopGap)
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
    int groundHitCount;
    float lastGroundedAt = -999f;
    float jumpBufferedUntil = -999f;
    float attackBufferedUntil = -999f;
    float issenBufferedUntil = -999f;   // Shift pressed during another action: fires when it ends (like the LMB buffer)
    float grappleBufferedUntil = -999f; // RMB pressed during another action: fires when it ends (like the LMB buffer)
    float dashCooldownUntil;
    float nextAfterimageAt;
    float nextSpeedLinesAt;
    int runFrame = -1, lastFootFrame = -1; // run strip frame showing this tick (-1 = not playing) / frame of the last footfall check
    float visualBaseY, leanAngle, startLeanUntil, skidUntil, prevVx; // visual's rest local Y; current lean (z deg)
    int skidDir;
    bool runWasOn;
    bool fxWasGrounded = true; // true at start: no spurious landing puff on the first unlocked frame (scene load / after the intro dialogue)
    int fxLastFacing;
    bool jumpCut;
    bool attackHitDone;
    bool lunging;                       // DECISIONS 12 lunge attack in progress (part of an Attack)
    Vector2 lungeStart, lungeEnd, lungeDir;
    TestEnemy lungeTarget;              // the locked target (HitLungeTargetInReach when a corner holds the body up)
    Collider2D lungeTargetCol;
    int swingHitStops;                  // hit-stops already played by this swing / lunge (capped at DashRules.LungeMaxHitStops)
    ContactFilter2D enemyFilter;
    readonly Collider2D[] enemyHits = new Collider2D[32];
    float dashEndsAfter;               // length of the current dash (dashDuration, or shorter/longer for a dash-behind)
    bool dashBehindActive;
    TestEnemy dashTarget;               // DECISIONS 13: enemy the Shift dash goes behind and counters at the dash end
    Collider2D dashTargetCol;
    Vector2 dashVelocity;               // Shift dash velocity (horizontal, plus a small vertical step onto the target's ground level)
    // DECISIONS 26 issen on Shift (DashRules.Issen*)
    float issenReadyAt;       // cooldown end (Time.time)
    bool pendingIssen;        // pressed during a hit-stop: replayed on the frame the hit-stop ends (jump/attack use their buffers)
    // DECISIONS 16 RMB grapple rope (DashRules.Grapple*), restored by playtest 11-11
    GrappleRope rope;
    Vector2 grappleAnchor;
    bool grappleCeiling;      // anchor on a ceiling (hit normal down): no upward pop into it
    bool grappleAttached;     // the rope reached the anchor (after GrappleExtendTime): pulling
    bool grappleUsed;         // spent this airtime; refilled on landing
    float grappleReadyAt;     // miss cooldown
    float grappleCarryUntil;  // after release: horizontal momentum eases into air control
    bool pendingGrapple;      // pressed during a hit-stop
    // DECISIONS 38 boss-room targets (GrappleAlt): the body is pulled to grapplePull (= grappleAnchor for ground ropes)
    Vector2 grapplePull;
    Transform grappleTarget;  // ground rope: the hit collider's transform; the anchor follows it (a falling boss roof chunk)
    Vector2 grappleLocal;     // anchor in grappleTarget's local space
    bool grappleFollow;       // grappleTarget is set (a destroyed one reads null, so a flag marks "had a target")
    bool grappleHang;        // a ceiling hook (RMB while swinging = a fresh rope from it)
    Vector2 hookLetGo;        // DECISIONS 44: the boss hook last let go of; it can't be grabbed again until hookFreeAt (no endless re-hook hang)
    float hookFreeAt = -999f;
    bool grappleStrike;       // the open hatch: strike on arrival, then bounce off and fall (GrappleStrikeSafe i-frames)
    // DECISIONS 39 rope swing: a hook or a solid ceiling zips to swingLen, then the body swings on the rope (pendulum, gravity, A/D pump)
    bool grappleSwing;        // this rope swings when it reaches swingLen (else the DECISIONS 16 pull-and-pop)
    float swingLen;           // rope length while swinging
    bool hanging;             // swinging on the rope
    float hangSince;
    float grappleCarryAccel = DashRules.GrappleAirAccel; // carried release momentum -> air control rate (a swing release keeps it longer)
    float grappleSafeUntil;   // i-frames after a landed hatch strike
    readonly RaycastHit2D[] grappleHits = new RaycastHit2D[8];
    readonly System.Collections.Generic.HashSet<TestEnemy> swingHits = new System.Collections.Generic.HashSet<TestEnemy>();
    int swingDamaged;                   // blows that did damage since the last reset (StrikeEnemy)

    // DECISIONS 18 lucky dice hooks
    public Rigidbody2D Body => rb;

    // Dev readout (FrameStats, F3): parries tried (Parry state entered) vs. landed, and hits taken while a parry was up.
    public static int ParryTries, ParryCount, ParryFailHits;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetParryStats() { ParryTries = ParryCount = ParryFailHits = 0; HurtFrame = -1; Swung = null; IssenAlt = null; GrappleAlt = null; } // Domain Reload is off

    // In-place LMB swing landed its hit box (centre, size): BossFight hits boss parts / machine props with it (they are not TestEnemies).
    public static event System.Action<Vector2, Vector2> Swung;

    // DECISIONS 31: issen at something that is not a TestEnemy (the boss core). Tried when no enemy is aimed. Plan: mouse and player
    // centre in, the struck point and the landing spot (player centre) out; false = nothing to cut. Hit: the blow landed.
    public interface IIssenTarget { bool PlanIssen(Vector2 mouse, Vector2 from, out Vector2 at, out Vector2 spot); void IssenHit(Vector2 at); }
    public static IIssenTarget IssenAlt;
    // DECISIONS 38: RMB rope targets that are not ground (BossFight: ceiling hooks, the open chest hatch), tried before the ground ray.
    // anchor = where the rope bites, pull = where the body goes; hang = hold there (hook), else GrappleStrike on arrival (false = no hit).
    public interface IGrappleTarget { bool PlanGrapple(Vector2 mouse, Vector2 hand, out Vector2 anchor, out Vector2 pull, out bool hang); bool GrappleStrike(Vector2 at); }
    public static IGrappleTarget GrappleAlt;

    public int Facing { get; private set; } = 1;
    public State CurrentState => state;
    public bool Grounded => grounded;
    public bool ParryActive => state == State.Parry && Time.time - stateEnteredAt < parryActiveFor;
    int pressSpentFrame = -1; // DECISIONS 44: frame whose LMB press already parried inside ReceiveEnemyHit
    public static int HurtFrame = -1; // frame the player last took damage (ParryRing turns a merge flash red on it)
    float parryActiveFor; // parryActiveTime, or longer for an LMB parry so it covers the threat it was chosen for (DashRules.ParryCoverMargin)
    float ParryLength => Mathf.Max(parryDuration, parryActiveFor); // the Parry state never ends while its active window is still open
    public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownUntil - Time.time);
    public float IssenCooldownRemaining => Mathf.Max(0f, issenReadyAt - Time.time);
    public bool LungingDown => lunging && lungeDir.y < -0.2f; // TestEnemy Android guard: a downward lunge counts as "from above"
    public Vector2 DashStart { get; private set; } // where the current dash began (TestEnemy: a dash-behind that left the hitbox before the strike still counts as a dash-through)
    public LayerMask EnemyMask => enemyMask;
    public Vector2 HandPosition => (Vector2)transform.position + new Vector2(Facing * 0.35f, 0.25f); // dice skill FX / grapple shot ray origin
    // Visual rope start = the front hand, from the 64px hero frames (PPU 32, feet pivot 2px up, sprite 1u below the root): grapple pose fist
    // at px (55,19) = (0.72, 0.34); firing pose (jump/run frame 0 front fist ~ px (43..46, 28..31)) = (0.40, 0.0). x mirrors with Facing.
    public Vector2 RopeHandPosition => (Vector2)transform.position + (state == State.Grapple ? new Vector2(Facing * 0.72f, 0.34f) : new Vector2(Facing * 0.40f, 0f));

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
        health = GetComponent<PlayerHealth>();
        visual = GetComponentInChildren<SpriteRenderer>();
        draftSprite = DraftSprites.Setup(visual, DraftSprites.Hero);
        if (visual != null) visualBaseY = visual.transform.localPosition.y;
        rb.gravityScale = gravityScale;
        bodyCols = GetComponents<Collider2D>();
        // Zero friction: velocity is written directly, so ground grip isn't needed, and walls must not hold the player up.
        PhysicsMaterial2D slick = null;
        foreach (Collider2D b in bodyCols)
        {
            if (b.isTrigger || b.sharedMaterial != null) continue;
            if (slick == null) slick = new PhysicsMaterial2D("PlayerSlick") { friction = 0f, bounciness = 0f };
            b.sharedMaterial = slick;
        }
        enemyFilter.SetLayerMask(enemyMask);
        enemyFilter.useTriggers = Physics2D.queriesHitTriggers; // same hits as the old OverlapBoxAll
        if (GetComponent<LuckyDice>() == null) gameObject.AddComponent<LuckyDice>(); // DECISIONS 18 / 31 (E); players are built by scripts, so add at runtime
        MouseCrosshair.Create(this); // DECISIONS 12 aim cursor + lock-on brackets (separate object, runtime like LuckyDice)
        if (GetComponent<AimLine>() == null) gameObject.AddComponent<AimLine>(); // DECISIONS 22 aim line
        rope = GrappleRope.Create(this); // DECISIONS 16
    }

    void OnDisable() { EndHitStop(); RestorePassedPlatforms(true); if (rope != null) rope.Hide(); }

    // Freezes time briefly (real-time length). Never starts during a pause / dialogue, and only ever extends a running hit-stop.
    bool hitStopCountsForGap; // false while only a parry stop runs: the riposte kill right after a parry keeps its freeze

    void StartHitStop(float seconds, bool countsForGap = true, bool evenLocked = false)
    {
        if (seconds <= 0f || (GameFlow.InputLocked && !evenLocked) || PauseMenu.IsOpen) return;
        hitStopCountsForGap = (hitStopEndsAt >= 0f && hitStopCountsForGap) || countsForGap;
        hitStopEndsAt = Mathf.Max(hitStopEndsAt, Time.unscaledTime + seconds);
        Time.timeScale = 0f;
    }

    // Elite clash duel (EliteDuel): input is locked, so the duel judges LMB itself. Begin drops any parry / guard / lunge (a missed duel
    // strike must land as a hit) and faces the elite; Clash shows one clang pose (parry, or the hero's own counter swing) + the contact
    // glow (DECISIONS 35). contact = where the blades met; null = the hero's front edge (Facing) at the strike height.
    Sprite duelPose;
    float duelPoseUntil = -1f;
    public void DuelBegin(int face)
    {
        if (state != State.Locomotion) EnterState(State.Locomotion);
        Facing = face;
        parryGuardUntil = parryPoseUntil = -999f;
    }
    public void DuelClash(bool counter, ParryHeight height, Vector2? contact = null)
    {
        ParryFx.ContactGlow(contact ?? (Vector2)transform.position + new Vector2(Facing * 0.45f, ParryRing.Offset(height)));
        if (!counter)
        {
            duelPoseUntil = -1f; // a counter pose still on screen must not hide the parry pose
            ShowParryPose(height, 0.15f, true);
            return;
        }
        duelPose = DraftSprites.Pick(DraftSprites.HeroAttack, 2, DraftSprites.Hero);
        duelPoseUntil = Time.time + 0.15f;
    }

    // Successful-parry pose (user 11-10): one of three strips for the strike's height (never the same strip twice in a row),
    // frame 0 at impact, frame 1 after ParryPoseKey. Scaled time, so the parry hit-stop holds frame 0. Shown while in Locomotion
    // (a swing / issen takes over); duel and boss clangs force it like the old duel pose (the boss slam's LMB also starts a swing).
    const float ParryPoseKey = 0.08f, ParryPoseTime = 0.25f; // (T) s
    Sprite[] parryPose;
    float parryPoseAt, parryPoseUntil = -999f;
    bool parryPoseForced;
    void ShowParryPose(ParryHeight height, float time, bool forced)
    {
        parryPose = DraftSprites.HeroParryPose(height, parryPose);
        parryPoseAt = Time.time;
        parryPoseUntil = Time.time + time;
        parryPoseForced = forced;
    }

    // Playtest 11-11 (replaces the DECISIONS 30 post-parry riposte): an LMB press that becomes a parry swings right away, presentation
    // only (the parry window and its rules are unchanged): the HeroAttack strip over ParrySwingTime + the slash FX on the press frame,
    // then the parry stance for the rest of the Parry state.
    const float ParrySwingTime = 0.18f; // (T) s
    public void DuelHitStop(float seconds) => StartHitStop(seconds, false, true); // finisher: runs although the duel still holds the input lock
    // DECISIONS 32 dice faces 3 / 4: show one hero frame for `time` s (visual only, scaled time; the duel pose slot).
    public void HoldPose(Sprite pose, float time)
    {
        if (pose == null) return;
        duelPose = pose;
        duelPoseUntil = Time.time + time;
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
            pendingIssen = pendingGrapple = false;
            issenBufferedUntil = grappleBufferedUntil = -999f;
            float dt = Time.deltaTime;
            stateEnteredAt += dt;
            dashCooldownUntil += dt;
            parryGuardUntil += dt;
            jumpBufferedUntil += dt;
            attackBufferedUntil += dt;
            lastGroundedAt += dt;
            issenReadyAt += dt;
            grappleReadyAt += dt;
            grappleCarryUntil += dt;
            grappleSafeUntil += dt;
            hookFreeAt += dt;
            health.ShiftInvulnerability(dt);
            if (state == State.Dash) { EnterState(State.Locomotion); rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); } // don't slide through teleports/dialogue
            EndLunge(); // same for a lunge (gravity back, velocity zeroed)
            if (state == State.Issen) EnterState(State.Locomotion); // and for the issen pose (no hanging in dialogue / blackout)
            if (state == State.Grapple) { EnterState(State.Locomotion); rb.linearVelocity = Vector2.zero; } // and for the rope
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
        int hitCount = groundHitCount = Physics2D.OverlapBox(feet, feetSize, 0f, groundFilter, groundHits);
        grounded = false;
        for (int i = 0; i < hitCount && !grounded; i++) // a platform a down-lunge is still passing through is not ground
            grounded = !groundHits[i].OverlapPoint(feet + Vector2.up * 0.03f) && !passedPlatforms.Contains(groundHits[i]);
        if (grounded && rb.linearVelocity.y <= 0.1f) lastGroundedAt = now;
        if (grounded && rb.linearVelocity.y <= 0.1f && state != State.Grapple) grappleUsed = false; // one rope per airtime (a rope fired from the ground is still this airtime's)

        if (input.JumpPressed) jumpBufferedUntil = now + jumpBuffer;
        if (input.AttackPressed && state != State.Locomotion && state != State.Hurt && state != State.Dead)
            attackBufferedUntil = Mathf.Max(now, StateEndsAt()) + attackBuffer; // must outlive the current swing/parry/dash
        if (input.IssenPressed && state != State.Locomotion && state != State.Issen && state != State.Hurt && state != State.Dead)
            issenBufferedUntil = Mathf.Max(now, StateEndsAt()) + attackBuffer;
        if (input.GrapplePressed && state != State.Locomotion && state != State.Grapple && state != State.Hurt && state != State.Dead)
            grappleBufferedUntil = Mathf.Max(now, StateEndsAt()) + attackBuffer;

        switch (state)
        {
            case State.Locomotion: UpdateLocomotion(now); break;
            case State.Attack:
                // DECISIONS 44: an on-time LMB during an in-place swing (an early press swung) parries at once instead of waiting
                // in the buffer until the swing ends (by then the blow had landed).
                if (!lunging && input.AttackPressed && TryEnterParry()) break;
                if (lunging) UpdateLunge(now, elapsed);
                else if (!attackHitDone && elapsed >= attackHitDelay) DoAttackHit();
                if (!lunging) TryJump(now); // playtest 11-11: jump out of a swing; the swing's hit and length are unchanged
                if (elapsed >= attackDuration) EnterState(State.Locomotion);
                break;
            case State.Parry:
                TryJump(now); // same for a parry (its window is unchanged)
                if (elapsed >= ParryLength) EnterState(State.Locomotion);
                break;
            case State.Dash:
                SpawnAfterimage(now);
                if (elapsed >= dashEndsAfter) EnterState(State.Locomotion); // DECISIONS 35: keeps facing the dash direction (13's turn-around and counter are off)
                break;
            case State.Hurt:
                if (elapsed >= hurtDuration) EnterState(health.IsDead ? State.Dead : State.Locomotion);
                break;
            case State.Issen:
                if (elapsed >= DashRules.IssenPoseTime) EnterState(State.Locomotion);
                break;
            case State.Grapple: UpdateGrapple(now, elapsed); break;
        }

        pendingIssen = pendingGrapple = false; // replayed (or ignored by the current state, like a live press) this frame
        if (now < grappleSafeUntil) SpawnAfterimage(now); // hatch strike fall: the i-frames show as the dash trail
        if (state == State.Locomotion || (state == State.Attack && !lunging) || state == State.Parry) UpdateJumpCut(); // an upward lunge is not a jump
        UpdateSpeedFx(now);
        UpdateVisual(now);
    }

    // Hit-stop freezes Time.time, so a buffer set now starts counting when it ends; WasPressedThisFrame is true for this frame only.
    void BufferDuringHitStop()
    {
        if (input.JumpPressed) jumpBufferedUntil = Time.time + jumpBuffer;
        if (input.AttackPressed && Time.frameCount != pressSpentFrame && state != State.Hurt && state != State.Dead) attackBufferedUntil = Mathf.Max(Time.time, StateEndsAt()) + attackBuffer;
        pendingIssen |= input.IssenPressed;
        if (input.IssenPressed && state != State.Locomotion && state != State.Issen && state != State.Hurt && state != State.Dead)
            issenBufferedUntil = Mathf.Max(Time.time, StateEndsAt()) + attackBuffer;
        pendingGrapple |= input.GrapplePressed;
        if (input.GrapplePressed && state != State.Locomotion && state != State.Grapple && state != State.Hurt && state != State.Dead)
            grappleBufferedUntil = Mathf.Max(Time.time, StateEndsAt()) + attackBuffer;
    }

    // When the current Attack/Parry/Dash ends by itself (now for any other state).
    float StateEndsAt()
    {
        switch (state)
        {
            case State.Attack: return stateEnteredAt + attackDuration;
            case State.Parry: return stateEnteredAt + ParryLength;
            case State.Dash: return stateEnteredAt + dashEndsAfter;
            case State.Issen: return stateEnteredAt + DashRules.IssenPoseTime;
        }
        return Time.time;
    }

    void UpdateLocomotion(float now)
    {
        if (Mathf.Abs(input.MoveX) > 0.01f) Facing = input.MoveX > 0f ? 1 : -1;

        if (TryShift(now)) return;
        if (TryGrapple(now)) return;
        if (input.AttackPressed || now <= attackBufferedUntil)
        {
            // DECISIONS 12-B: LMB = parry when a parryable threat is about to land, else lunge, else in-place swing.
            if (!TryEnterParry()) EnterState(State.Attack);
            return;
        }
        TryJump(now);
    }

    bool TryEnterParry()
    {
        if (!FaceParryThreat(out float eta)) return false;
        EnterState(State.Parry);
        // No upper cap: a strike seen at eta 0.3 lands up to 2 frames later (Windup->Strike->DoStrike); the Parry state stretches to match.
        // Shots are bounded by their eligibility (ParryShotRange / ParryShotWindow), so the guard always lasts until they land.
        parryActiveFor = Mathf.Max(eta + DashRules.ParryCoverMargin, parryActiveTime);
        return true;
    }

    // jump with coyote + buffer
    void TryJump(float now)
    {
        if (now > jumpBufferedUntil || now - lastGroundedAt > coyoteTime) return;
        if (input.DownHeld && TryDropThrough()) { jumpBufferedUntil = -999f; lastGroundedAt = -999f; return; }
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
        jumpBufferedUntil = -999f;
        lastGroundedAt = -999f;
        jumpCut = false;
    }

    // S + Space on a one-way ledge: drop through it. Only when every ground under the feet is one-way (solid ground / a building = normal jump).
    // Reuses the down-lunge pass-through: collision is given back by RestorePassedPlatforms once the body no longer overlaps the ledge.
    bool TryDropThrough()
    {
        if (!grounded || groundHitCount == 0) return false;
        for (int i = 0; i < groundHitCount; i++)
        {
            Collider2D c = groundHits[i];
            if (!c.usedByEffector) return false;
            PlatformEffector2D fx = c.GetComponent<PlatformEffector2D>();
            if (fx == null || !fx.useOneWay) return false;
        }
        for (int i = 0; i < groundHitCount; i++)
            if (!passedPlatforms.Contains(groundHits[i])) { SetPlatformPass(groundHits[i], true); passedPlatforms.Add(groundHits[i]); }
        rb.position += Vector2.down * 0.1f; // sink into the ledge so the overlap guard holds the pass until the body is below it
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, -1f));
        grounded = false;
        return true;
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

    void EnterState(State next)
    {
        EndLunge();                     // any state change ends a lunge (dash cancel, hurt, death)
        if (state == State.Grapple && rope != null) rope.Hide(); // ...and an attached rope (release, hurt, dialogue)
        if (state == State.Grapple && grappleHang) { hookLetGo = grappleAnchor; hookFreeAt = Time.time + DashRules.GrappleHookRegrab; }
        state = next;
        if (next == State.Dead) MomentFx.PlayerDeath(rb.position + feetOffset);
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
                Sfx.Play(SfxId.Swing, 0.6f); // the press swings at once (ParrySwingTime)
                ParryFx.Slash(AttackCenter(), visual != null ? visual.transform : null, Facing);
                break;
            case State.Dash:
                rb.gravityScale = 0f;
                DashStart = rb.position;
                // DECISIONS 35 plain dash (Shift with no issen): Facing direction, horizontal, walls stop the body. No dash-behind counter.
                dashEndsAfter = dashDuration;
                dashVelocity = new Vector2(Facing * dashSpeed, 0f);
                rb.linearVelocity = dashVelocity;
                PlayDashBurst(dashVelocity.x);
                Sfx.Play(SfxId.Dash);
                break;
            case State.Dead:
                rb.linearVelocity = Vector2.zero; // FixedUpdate does not write velocity in Dead, so the corpse stays put
                break;
            case State.Issen:
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero; // holds the follow-through still (FixedUpdate keeps it still)
                grappleCarryUntil = 0f;
                break;
            case State.Grapple:
                rb.gravityScale = 0f;
                rb.linearVelocity = Vector2.zero; // hangs still while the rope flies out, then the pull (FixedUpdate)
                grappleAttached = hanging = false;
                grappleCarryAccel = DashRules.GrappleAirAccel; // SwingRelease overrides it after the release
                break;
        }
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Locomotion:
                float vx = input.MoveX * moveSpeed;
                if (Time.time < grappleCarryUntil && !grounded) // rope release: keep the swing, blend into air control
                    vx = Mathf.MoveTowards(rb.linearVelocity.x, vx, grappleCarryAccel * Time.fixedDeltaTime);
                if (!grounded && vx != 0f && PushingIntoWall(vx > 0f ? 1 : -1)) vx = 0f; // airborne against a wall face: no push, so gravity slides us down
                rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
                break;
            case State.Issen:
                rb.linearVelocity = Vector2.zero;
                break;
            case State.Grapple:
                FollowGrappleTarget();
                if (!grappleAttached) { rb.linearVelocity = Vector2.zero; break; }
                if (hanging) { SwingStep(); break; }
                Vector2 toAnchor = grapplePull - rb.position;
                rb.linearVelocity = toAnchor.sqrMagnitude > 0.0001f ? toAnchor.normalized * DashRules.GrapplePullSpeed : Vector2.zero; // full speed to the end (release keeps it)
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
            if (enemy == null || enemy.Current == TestEnemy.State.Stunned || !enemy.gameObject.activeInHierarchy) continue; // downed / vanishing (NUM-0 frozen citizens stay targetable)
            Vector2 d = (Vector2)enemy.transform.position - pos;
            float dx = d.x * side;
            if (dx <= 0f || dx > maxDx || Mathf.Abs(d.y) > maxDy || dx >= bestDx) continue;
            bestDx = dx;
            best = enemy;
            hit = col;
        }
        return best;
    }

    // True when a solid (non one-way) ground collider is right next to the body on side `dir` (+1 / -1).
    readonly RaycastHit2D[] wallHits = new RaycastHit2D[4];
    ContactFilter2D wallFilter;
    bool PushingIntoWall(int dir)
    {
        wallFilter.SetLayerMask(groundMask);
        wallFilter.useTriggers = false;
        foreach (Collider2D b in bodyCols)
        {
            if (b == null || b.isTrigger) continue;
            int n = b.Cast(new Vector2(dir, 0f), wallFilter, wallHits, 0.06f);
            for (int i = 0; i < n; i++)
                if (!wallHits[i].collider.usedByEffector && Mathf.Abs(wallHits[i].normal.y) < 0.5f) return true;
        }
        return false;
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
            if (enemy == null || enemy.Current == TestEnemy.State.Stunned || !enemy.gameObject.activeInHierarchy) continue;
            Vector2 d = (Vector2)enemy.transform.position - pos;
            float dist = d.magnitude;
            if (dist > range || dist >= bestDist || (dist > 0.001f && Vector2.Dot(d / dist, aim) < minDot)) continue;
            if (!ReachLine(pos, enemy.transform.position)) continue; // behind a wall / under a roof: the lunge would only hit the wall
            bestDist = dist;
            best = enemy;
            hit = col;
        }
        return best;
    }

    // Line of sight between centres: no solid ground in between. One-way platforms never block it (a downward lunge passes them, see TryStartLunge).
    ContactFilter2D losFilter; // own filter: FindLungeTarget also runs from the crosshair's LateUpdate, outside Update's groundFilter setup
    readonly RaycastHit2D[] losHits = new RaycastHit2D[16];
    bool ClearLine(Vector2 from, Vector2 to)
    {
        losFilter.SetLayerMask(groundMask);
        losFilter.useTriggers = Physics2D.queriesHitTriggers; // same hits as the old LinecastAll
        int n = Physics2D.Linecast(from, to, losFilter, losHits);
        for (int i = 0; i < n; i++)
            if (!losHits[i].collider.usedByEffector) return false;
        return true;
    }

    // Reachable: the centre line, or a parallel line 0.4 to either side (inside both bodies), is clear. A corner or step edge clips
    // only one side of the path and the lunge slides past it (playtest 11-10 "diagonal attacks get blocked at walls").
    // A wall fully between the two still blocks all three lines.
    bool ReachLine(Vector2 from, Vector2 to)
    {
        if (ClearLine(from, to)) return true;
        Vector2 d = to - from;
        if (d.sqrMagnitude < 0.0001f) return false;
        Vector2 side = new Vector2(-d.y, d.x).normalized * 0.4f;
        return ClearLine(from + side, to + side) || ClearLine(from - side, to - side);
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
        // Corners before the target do not stop it (the body slides past); the target's own floor / a wall at it still does.
        float wall = WallDistance(pos, dir, tDist + DashRules.LungeBehind, passAbove, lungePass, tDist - DashRules.LungeBehind);
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
        lungeTarget = target;
        lungeTargetCol = targetCol;
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
    // Hits closer than slideBefore that the body can slide along (a slanted hit, not head-on) are skipped too: a corner or step
    // edge on a diagonal lunge, or the wall the player leans on (playtest 11-10). UpdateLunge ends it if the slide really sticks.
    float WallDistance(Vector2 pos, Vector2 dir, float maxLen, float passAbove = float.PositiveInfinity,
        System.Collections.Generic.List<RaycastHit2D> pass = null, float slideBefore = 0f)
    {
        float best = maxLen;
        foreach (RaycastHit2D h in Physics2D.BoxCastAll(pos, new Vector2(0.8f, 1.8f), 0f, dir, maxLen, groundMask))
        {
            if (h.collider.usedByEffector && dir.y >= -0.01f) continue;
            if (h.collider.usedByEffector && h.collider.bounds.max.y > passAbove) { pass?.Add(h); continue; }
            if ((h.normal.y > 0.7f && dir.y > -0.5f) || (h.normal.y < -0.7f && dir.y < 0.5f)) continue; // slide along floor / ceiling
            if (h.distance < slideBefore && Mathf.Abs(Vector2.Dot(h.normal, dir)) < 0.9f) continue; // slide past a corner (keeps >= 19% of the speed)
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
        // 0.15: a slide past a corner (WallDistance lets |normal . dir| < 0.9 through) keeps at least 0.19 of it.
        bool blocked = elapsed > 2f * Time.fixedDeltaTime &&
            Vector2.Dot(rb.linearVelocity, lungeDir) < 0.15f * Mathf.Min(DashRules.LungeSpeed, remain / Time.fixedDeltaTime);
        if (remain <= 0.05f || blocked || elapsed >= DashRules.LungeMaxTime) { HitLungeTargetInReach(); EndLunge(); } // no-op once the sweep hit it
    }

    // Lunge held up short of the target (a corner the slide could not pass): the locked target still takes the blow when it is
    // within the sweep's reach (LungeTolerance ahead + half its width, to the collider) with a clear line.
    void HitLungeTargetInReach()
    {
        if (lungeTarget == null || lungeTargetCol == null || swingHits.Contains(lungeTarget)) return;
        Vector2 pos = rb.position;
        if (Vector2.Distance(lungeTargetCol.ClosestPoint(pos), pos) > DashRules.LungeTolerance + 0.5f * DashRules.LungeSweepWidth) return;
        if (ReachLine(pos, lungeTarget.transform.position)) HitEnemy(lungeTarget, lungeTargetCol, pos, true);
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

    // DECISIONS 35: a Shift press (live, buffered or held over a hit-stop) is an issen when one can land, else a plain dash in the
    // facing (= held) direction (DashRules.ShiftDashCooldown; the old DECISIONS 13 dash-behind counter stays off). Both on cooldown: a faint swish.
    bool TryShift(float now)
    {
        if (!(input.IssenPressed || pendingIssen || now <= issenBufferedUntil)) return false;
        issenBufferedUntil = -999f;
        if (now >= issenReadyAt && TryIssen(now)) return true;
        if (now < dashCooldownUntil)
        {
            Sfx.Play(SfxId.Swing, 0.3f); // fail cue
            return false;
        }
        dashCooldownUntil = now + DashRules.ShiftDashCooldown;
        EnterState(State.Dash);
        return true;
    }

    // DECISIONS 26 issen (Shift since playtest 11-11): flash to just behind the aimed enemy (FindIssenTarget) in one frame, leaving a ghost streak, and cut it.
    // Humans (citizen / sniper) die in one hit; androids, the elite and bugs take nothing (just passed). Nothing else on the path is hit.
    // Then the Issen pose (DashRules.IssenPoseTime). false = no target / no free spot (TryShift dashes instead).
    bool TryIssen(float now)
    {
        Collider2D col = null;
        bool aimed = MouseCrosshair.MouseWorld(out Vector2 mouse);
        TestEnemy target = aimed ? FindIssenTarget(mouse, out col) : null;
        Vector2 spot = default, at = default;
        bool alt = false;
        if (target == null || !PlanIssen(target, col, out spot))
        {
            alt = aimed && IssenAlt != null && IssenAlt.PlanIssen(mouse, rb.position, out at, out spot);
            if (!alt) return false;
        }
        else at = target.transform.position;
        issenReadyAt = now + DashRules.IssenCooldown;
        Vector2 from = rb.position, path = spot - from;
        int travel = Mathf.Abs(path.x) > 0.01f ? (path.x > 0f ? 1 : -1) : Facing;
        PlayDashBurst(travel); // at the heel where we left
        EnterState(State.Issen); // ends a lunge / dash / parry; gravity off, still
        if (visual != null) // ghost streak: afterimages along the path, the ones near the start fade first
        {
            Transform v = visual.transform;
            Vector3 keep = v.position;
            visual.flipX = travel < 0;
            for (int i = 0; i < DashRules.IssenGhosts; i++)
            {
                float u = i / (float)DashRules.IssenGhosts;
                v.position = keep + (Vector3)(path * u);
                SpriteFx.Afterimage(visual, AfterimageColor, afterimageLife * (0.5f + u));
            }
            v.position = keep;
        }
        DashStart = rb.position; // TestEnemy: an issen out of a strike box counts as a dash-through (Android overload)
        rb.position = spot;
        transform.position = spot; // same-frame queries (HitInfo.SourcePosition) see the new spot
        float dx = at.x - spot.x;
        Facing = Mathf.Abs(dx) > 0.01f ? (dx > 0f ? 1 : -1) : -travel; // ends facing the enemy
        Sfx.Play(SfxId.Dash);
        ParryFx.Slash(at, null, travel);
        if (alt) IssenAlt.IssenHit(at);
        else if (target.IsAndroid && target.Current != TestEnemy.State.Overload) // playtest 11-12: the cut overloads an android (stunned look, next hit kills)
        {
            target.Overload(DiceSkillKit.AndroidOverloadSeconds); // 3 s like the dice overload: the issen cooldown (5 s) leaves LMB to finish it
            LuckyDice.AddHitCharge(); // a landed cut counts toward the dice (7 hits)
            MomentFx.HackPulse(target.BodyCollider != null ? (Vector2)target.BodyCollider.bounds.center : (Vector2)target.transform.position);
            Sfx.Play(SfxId.Stunned);
        }
        else if (!((target.IsHuman || target.IsAndroid) && StrikeEnemy(target, col, from, true))) Sfx.Play(SfxId.Parry, 0.4f, 0.7f); // passed untouched: dull tick
        StartHitStop(DashRules.IssenHitStop); // only extends a kill's longer stop
        Juice.Shake(Juice.ShakeHitAmp, Juice.ShakeHitTime, path);
        return true;
    }

    // The crosshair's lunge lock (FindLungeTarget: what the brackets show) if any; else the enemy collider nearest the mouse within
    // IssenMouseRadius whose centre is within IssenRange of the player with a clear line. col = its body collider. null = none.
    // Playtest 11-11 "1자": only enemies roughly level with the player (centres within DashRules.IssenMaxDy).
    public TestEnemy FindIssenTarget(Vector2 mouse, out Collider2D col)
    {
        Vector2 pos = rb.position, aim = mouse - pos;
        col = null;
        TestEnemy best = aim.sqrMagnitude > 0.0001f ? FindLungeTarget(aim.normalized, out col) : null;
        if (best != null && Mathf.Abs(best.transform.position.y - pos.y) <= DashRules.IssenMaxDy) return best;
        best = null;
        col = null;
        float bestD = float.MaxValue;
        int n = Physics2D.OverlapCircle(mouse, DashRules.IssenMouseRadius, enemyFilter, enemyHits);
        for (int i = 0; i < n; i++)
        {
            TestEnemy e = enemyHits[i].GetComponentInParent<TestEnemy>();
            if (e == null || e.IsDown || (e.BodyCollider != null && enemyHits[i] != e.BodyCollider)) continue;
            float d = Vector2.Distance(enemyHits[i].ClosestPoint(mouse), mouse);
            if (d >= bestD || Mathf.Abs(e.transform.position.y - pos.y) > DashRules.IssenMaxDy
                || Vector2.Distance(e.transform.position, pos) > DashRules.IssenRange || !ReachLine(pos, e.transform.position)) continue;
            bestD = d;
            best = e;
            col = enemyHits[i];
        }
        return best;
    }

    // Landing spot just behind the target (far side from the player), on its feet level; if that is in a wall or walled off from the
    // target, the near side (stop short in front of it). false = both blocked (cancel).
    bool PlanIssen(TestEnemy target, Collider2D col, out Vector2 spot)
    {
        spot = default;
        if (target.BodyCollider != null) col = target.BodyCollider;
        if (col == null) return false;
        const float halfW = 0.45f, halfH = 1f; // player box 0.9 x 2 (same as PlanBehind)
        Vector2 at = target.transform.position;
        float dx = at.x - rb.position.x;
        int first = Mathf.Abs(dx) > 0.01f ? (dx > 0f ? 1 : -1) : Facing;
        for (int k = 0; k < 2; k++)
        {
            int side = k == 0 ? first : -first;
            Vector2 s = new Vector2(at.x + side * (col.bounds.extents.x + halfW + dashBehindClearance), col.bounds.min.y - feetOffset.y + 0.02f);
            if (BlinkSpotBlocked(s, new Vector2(halfW * 2f - 0.1f, halfH * 2f - 0.2f)) || !ClearLine(at, s)) continue;
            spot = s;
            return true;
        }
        return false;
    }

    // DECISIONS 16: RMB fires the rope at the mouse. Hit (ground / wall / ceiling / one-way platform within GrappleRange) -> Grapple state;
    // miss -> the rope shoots out and snaps back, short cooldown. One attached rope per airtime.
    bool TryGrapple(float now)
    {
        if (!(input.GrapplePressed || pendingGrapple || now <= grappleBufferedUntil) || now < grappleReadyAt || grappleUsed) return false;
        grappleBufferedUntil = -999f;
        Vector2 hand = HandPosition;
        bool aimed = MouseCrosshair.MouseWorld(out Vector2 mouse);
        Vector2 aim = aimed ? mouse - hand : new Vector2(Facing, 0f);
        aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : new Vector2(Facing, 0f);
        int face = Mathf.Abs(aim.x) > 0.01f ? (aim.x > 0f ? 1 : -1) : Facing; // applied on a hit only (Facing is the parry front)
        hand = (Vector2)transform.position + new Vector2(face * 0.35f, 0.25f);
        if (aimed && GrappleAlt != null && GrappleAlt.PlanGrapple(mouse, hand, out Vector2 altAnchor, out Vector2 altPull, out bool hang) // DECISIONS 38
            && !(hang && now < hookFreeAt && (altAnchor - hookLetGo).sqrMagnitude < 0.01f)) // DECISIONS 44: not the hook just let go of
        {
            Facing = face;
            grappleAnchor = altAnchor;
            grapplePull = hang ? altAnchor : altPull; // a hook: zip toward the hook itself, swing at the old hang depth (DECISIONS 39)
            grappleCeiling = false;
            grappleFollow = false;
            grappleHang = grappleSwing = hang;
            swingLen = Vector2.Distance(altAnchor, altPull);
            grappleStrike = !hang;
            grappleUsed = true;
            EnterState(State.Grapple);
            rope.Shoot(grappleAnchor, true);
            Sfx.Play(SfxId.Dash);
            return true;
        }
        RaycastHit2D best = default;
        int n = Physics2D.Raycast(hand, aim, groundFilter, grappleHits, DashRules.GrappleRange); // groundFilter: groundMask, set every Update
        for (int i = 0; i < n; i++) // skip a collider the hand starts inside (rising through a one-way platform)
            if (grappleHits[i].distance > 0.01f && (best.collider == null || grappleHits[i].distance < best.distance)) best = grappleHits[i];
        if (best.collider == null)
        {
            rope.Shoot(hand + aim * DashRules.GrappleRange, false);
            grappleReadyAt = now + DashRules.GrappleMissCooldown;
            Sfx.Play(SfxId.Swing, 0.5f);
            return false; // a miss does not interrupt movement
        }
        Facing = face;
        grappleAnchor = best.point;
        grappleCeiling = best.normal.y < -0.7f;
        // A wall just below a ledge top: anchor on the corner instead, so the pull ends level with the top and the pop lands on it.
        if (Mathf.Abs(best.normal.x) > 0.7f)
        {
            Vector2 probe = best.point - best.normal * 0.1f + Vector2.up * DashRules.GrappleLedgeSnap;
            RaycastHit2D top = Physics2D.Raycast(probe, Vector2.down, DashRules.GrappleLedgeSnap, groundMask);
            if (top.collider != null && top.distance > 0.01f && top.normal.y > 0.7f) grappleAnchor = new Vector2(best.point.x, top.point.y + 0.1f);
        }
        grapplePull = grappleAnchor;
        grappleTarget = best.collider.transform;
        grappleLocal = grappleTarget.InverseTransformPoint(grappleAnchor);
        grappleFollow = true;
        grappleHang = grappleStrike = false;
        grappleSwing = grappleCeiling && !best.collider.usedByEffector; // solid ceiling / overhang swings; a one-way platform keeps the pop through it
        swingLen = DashRules.GrappleSwingLen;
        grappleUsed = true;
        EnterState(State.Grapple);
        rope.Shoot(grappleAnchor, true);
        Sfx.Play(SfxId.Dash);
        return true;
    }

    void UpdateGrapple(float now, float elapsed)
    {
        if (grappleFollow && (grappleTarget == null || !grappleTarget.gameObject.activeInHierarchy)) // the hit chunk crumbled / was disabled
        {
            ReleaseGrapple(false);
            return;
        }
        // Jump = early release + a normal jump. LMB = release into a swing (UpdateLocomotion runs it this frame). Shift stays buffered until the pull ends.
        if (now <= jumpBufferedUntil && grappleAttached && hanging) // swinging: let go with the swing (slingshot)
        {
            SwingRelease();
            jumpBufferedUntil = -999f;
            return;
        }
        if (now <= jumpBufferedUntil && grappleAttached) // a (buffered) press while pulling (not during the extend: no free air jump)
        {
            ReleaseGrapple(false);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, jumpSpeed));
            jumpBufferedUntil = -999f;
            jumpCut = false;
            return;
        }
        if (input.AttackPressed || now <= attackBufferedUntil)
        {
            ReleaseGrapple(false);
            UpdateLocomotion(now);
            return;
        }
        if (!grappleAttached)
        {
            if (elapsed < DashRules.GrappleExtendTime) return;
            grappleAttached = true; // rope reached the anchor: spark there, start pulling
            PlayHitSpark(grappleAnchor, Mathf.Sign(grappleAnchor.x - rb.position.x));
            Juice.Shake(Juice.ShakeHitAmp, Juice.ShakeHitTime);
            return;
        }
        if (hanging) // DECISIONS 39 swing (FixedUpdate SwingStep): S, landing or the time limit lets go keeping the swing velocity
        {
            if (input.GrapplePressed) // RMB again: let go with the swing; a hook also allows a fresh rope (the open hatch, a wall), a miss just flies on
            {
                SwingRelease();
                if (!grappleHang) return;
                grappleUsed = false;
                grappleReadyAt = now;
                TryGrapple(now);
            }
            else if (input.DownHeld || now - hangSince >= DashRules.GrappleHangMax || (grounded && rb.linearVelocity.y <= 0.1f)) ReleaseGrapple(false);
            return;
        }
        SpawnAfterimage(now);
        Vector2 toAnchor = grapplePull - rb.position;
        float dist = toAnchor.magnitude;
        float pulled = elapsed - DashRules.GrappleExtendTime;
        float wanted = DashRules.GrapplePullSpeed;
        bool blocked = pulled > 2f * Time.fixedDeltaTime && dist > 0.001f &&
            Vector2.Dot(rb.linearVelocity, toAnchor / dist) < 0.25f * wanted; // stuck on a corner / ceiling: let go
        float step = DashRules.GrapplePullSpeed * Time.fixedDeltaTime;
        if (grappleSwing && (dist <= swingLen + step || blocked || pulled >= DashRules.GrappleMaxPull)) // rope length reached: swing from here
        {
            hanging = true;
            hangSince = now;
            // The rope is as long as it is now (closer than the min: falls slack until taut). Never shorter than dist: SwingStep would
            // yank the overshoot back in one step (up to max speed toward the anchor, kept afterwards = a random bounce / a shove into a blocker).
            swingLen = Mathf.Max(dist, DashRules.GrappleSwingMin);
            // The pull is purely radial, so dropping the radial part left ~0 speed (playtest: sluggish). Redirect the zip speed along the
            // arc instead, toward the horizontal travel (else held A/D, else facing): grabbing flings straight into a swing.
            if (dist > 0.001f)
            {
                Vector2 v = rb.linearVelocity, r = toAnchor / dist, tan = new Vector2(-r.y, r.x);
                float dir = Mathf.Abs(v.x) > 0.5f ? Mathf.Sign(v.x) : (input.MoveX != 0f ? Mathf.Sign(input.MoveX) : Facing);
                if (tan.x * dir < 0f) tan = -tan;
                if (Mathf.Abs(tan.x) < 0.2f && tan.y > 0f) tan = -tan; // level with the anchor: swing down, not up into it
                float speed = Mathf.Clamp(v.magnitude * DashRules.GrappleSwingKeep, DashRules.GrappleSwingEntryMin, DashRules.GrappleSwingMaxSpeed);
                // Scaled by how far off straight-below the grab is (full from 30 deg): a straight-up grab starts gently (EntryMin)
                // instead of being flung to ~80 deg, an angled one keeps its momentum. Anchor not above: EntryMin.
                float side = r.y > 0f ? Mathf.Clamp01(Mathf.Abs(r.x) * 2f) : 0f;
                rb.linearVelocity = tan * Mathf.Lerp(DashRules.GrappleSwingEntryMin, speed, side);
            }
            rope.Taut = true;
            return;
        }
        if (grappleStrike && dist <= DashRules.GrappleReleaseDist + step) { HatchStrike(now); return; }
        if ((!grappleHang && dist <= DashRules.GrappleReleaseDist + step) || blocked || pulled >= DashRules.GrappleMaxPull)
            ReleaseGrapple(!(blocked && grappleCeiling) && !grappleHang && !grappleStrike); // no pop into a ceiling; boss targets never pop
    }

    // Moves the ground anchor (and the pull point and rope tip) with the collider it hit; zero shift on static terrain.
    void FollowGrappleTarget()
    {
        if (!grappleFollow || grappleTarget == null) return;
        Vector2 delta = (Vector2)grappleTarget.TransformPoint(grappleLocal) - grappleAnchor;
        // Inactive, or a jump no mover makes in one step (a pooled chunk crumbled and respawned elsewhere between checks): drop it,
        // UpdateGrapple then releases (null target) even if the pooled object is active again.
        if (!grappleTarget.gameObject.activeInHierarchy || delta.sqrMagnitude > 0.25f) { grappleTarget = null; return; }
        if (delta.sqrMagnitude < 1e-8f) return;
        grappleAnchor += delta;
        grapplePull += delta;
        rope.MoveTarget(grappleAnchor);
    }

    // DECISIONS 38: the rope reached the open hatch. The boss takes the hit (GrappleStrike); the hero bounces back the way it came and
    // falls with GrappleStrikeSafe i-frames. A hatch that shut meanwhile (false): the same bounce, no hit, no i-frames.
    void HatchStrike(float now)
    {
        bool struck = GrappleAlt != null && GrappleAlt.GrappleStrike(grappleAnchor);
        EnterState(State.Locomotion);
        rb.linearVelocity = new Vector2(-Facing * DashRules.GrappleStrikeBounceX, DashRules.GrappleStrikeBounceY);
        jumpCut = true;
        grappleCarryUntil = now + DashRules.GrappleCarryTime;
        grappleReadyAt = now + DashRules.GrappleCooldown;
        if (!struck) return;
        grappleSafeUntil = now + DashRules.GrappleStrikeSafe;
        HoldPose(DraftSprites.Pick(DraftSprites.HeroAttack, 2, DraftSprites.Hero), 0.2f);
        ParryFx.Slash(AttackCenter(), visual != null ? visual.transform : null, Facing);
        Sfx.Play(SfxId.Swing);
    }

    // Back to Locomotion keeping the pull velocity. pop: anchor above -> at least GrapplePopFrac of a jump upward (slingshot onto the ledge).
    void ReleaseGrapple(bool pop)
    {
        Vector2 v = grappleAttached ? rb.linearVelocity : Vector2.zero;
        bool above = grappleAnchor.y > rb.position.y + 0.1f;
        EnterState(State.Locomotion);
        if (pop && above) v.y = Mathf.Max(v.y, DashRules.GrapplePopFrac * jumpSpeed);
        rb.linearVelocity = v;
        jumpCut = true; // the pop is not a held jump: releasing Space must not halve it
        grappleCarryUntil = Time.time + DashRules.GrappleCarryTime;
        grappleReadyAt = Time.time + DashRules.GrappleCooldown; // ground ropes refill at once: no spammed wall zip
    }

    // DECISIONS 39: Space / RMB off a swing. The swing velocity x GrappleSwingBoost plus a small lift; the momentum eases into air control slowly.
    void SwingRelease()
    {
        Vector2 v = rb.linearVelocity * DashRules.GrappleSwingBoost + Vector2.up * (DashRules.GrappleSwingLiftFrac * jumpSpeed);
        ReleaseGrapple(false);
        rb.linearVelocity = v;
        grappleCarryUntil = Time.time + DashRules.GrappleSwingCarryTime;
        grappleCarryAccel = DashRules.GrappleSwingAirAccel;
    }

    // FixedUpdate while swinging (gravityScale stays 0 in Grapple; gravity is added here): a one-sided rope of swingLen from grappleAnchor.
    void SwingStep()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 v = rb.linearVelocity + Physics2D.gravity * (gravityScale * dt);
        Vector2 off = rb.position - grappleAnchor;
        float d = off.magnitude;
        if (d > 0.001f)
        {
            Vector2 r = off / d, tan = new Vector2(-r.y, r.x);
            if (tan.x < 0f) tan = -tan; // D pumps along the arc to the right, A to the left
            // Pump only below the energy of max speed at the bottom of the arc: the speed cap alone let a held key keep pushing on
            // the way up (pump 22: over the top of a hook, hard into a ceiling). Pumping against the motion (braking) always works.
            float pump = input.MoveX * DashRules.GrappleSwingPump * dt;
            float g = -Physics2D.gravity.y * gravityScale, max = DashRules.GrappleSwingMaxSpeed;
            if (pump * Vector2.Dot(v, tan) <= 0f || v.sqrMagnitude + 2f * g * (swingLen + off.y) < max * max) v += tan * pump;
            if (d >= swingLen) // taut: no outward speed, and back onto the circle this step
            {
                float outV = Vector2.Dot(v, r);
                if (outV > 0f) v -= r * outV;
                v -= r * ((d - swingLen) / dt);
            }
        }
        rb.linearVelocity = Vector2.ClampMagnitude(v, DashRules.GrappleSwingMaxSpeed);
    }

    void SpawnAfterimage(float now)
    {
        if (visual == null || now < nextAfterimageAt) return;
        nextAfterimageAt = now + afterimageInterval;
        SpriteFx.Afterimage(visual, AfterimageColor, afterimageLife);
    }

    // Speed FX (all sheets face right; flipX for left). Only called on unfrozen, unlocked frames, so nothing new spawns during
    // hit-stop / dialogue / pause. run-dust at the heel on each footfall (run frame 0 and N/2) while running full speed (+ once on landing / turning);
    // speed-lines on the body while running, dashing or lunging, rotated to a diagonal lunge.
    void UpdateSpeedFx(float now)
    {
        if (visual == null) return;
        bool running = grounded && state == State.Locomotion && Mathf.Abs(rb.linearVelocity.x) >= moveSpeed * runFxSpeedFrac;
        bool landed = grounded && !fxWasGrounded && state != State.Dead;
        bool turned = running && Facing != fxLastFacing;
        fxWasGrounded = grounded;
        MomentFx.TrackLand(grounded && state != State.Dead, rb.position + feetOffset);
        fxLastFacing = Facing;
        int frame = running ? runFrame : -1; // runFrame is from the last UpdateVisual
        bool footfall = frame >= 0 && frame != lastFootFrame && (frame == 0 || frame == DraftSprites.HeroRun.Length / 2);
        lastFootFrame = frame;
        if (landed || turned || footfall)
            SpriteFx.PlayBehind("run-dust", rb.position + feetOffset, Facing < 0, visual);

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
            if (p == null || !p.Parryable || GameFlow.EnemiesHeld) continue; // dice hold: frozen shots are no threat, LMB attacks
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
        swingDamaged = 0;
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
        Swung?.Invoke(AttackCenter(), attackSize);
    }

    // One blow (in-place swing or lunge sweep). false = the enemy blocked it (Android not overloaded).
    // One hit per enemy per swing; hit-stop + shake only for the first LungeMaxHitStops blows.
    bool HitEnemy(TestEnemy enemy, Collider2D col, Vector2 from, bool lunge, bool pierce = false)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.Current == TestEnemy.State.Stunned) return true; // already down / vanishing
        if (!swingHits.Add(enemy)) return true;
        // A lunge hit comes from where the lunge began (the sweep spans from there): the Android "from above / behind" test must not
        // depend on which frame the fast-moving body happened to be sampled at (Fable review 11-09).
        var info = new HitInfo { Damage = attackDamage, SourcePosition = lunge ? lungeStart : (Vector2)transform.position, IsSpecial = false, Pierce = pierce };
        EnemyStruck?.Invoke(enemy);
        return LandHit(enemy, col, from, info);
    }

    // Every blow that reaches an enemy (damaging or blocked). DECISIONS 27 dice face 5 listens; do not hit enemies from a handler.
    public event System.Action<TestEnemy> EnemyStruck;

    bool LandHit(TestEnemy enemy, Collider2D col, Vector2 from, HitInfo info)
    {
        bool damaged = enemy.ReceivePlayerHit(info);
        if (!damaged) return false;
        swingDamaged++;
        LuckyDice.AddHitCharge(); // lucky dice: +1 per landed hit (skill strikes are filtered inside)
        Sfx.Play(SfxId.Hit);
        Vector2 contactPoint = col.ClosestPoint(from);
        PlayHitSpark(contactPoint, Facing);
        SpriteFx.Play("impact-ring", contactPoint, false, 101); // pivot = ring centre
        bool down = enemy.Current == TestEnemy.State.Stunned; // the hit that finishes the enemy
        if (down) SpriteFx.Play("kill-burst", enemy.transform.position, Facing < 0, 102); // pivot = burst origin, flips with the blow
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

    // DECISIONS 18 dice skills: one blow through the normal hit path (sparks, kill burst, capped hit-stop, shake).
    // Its own hit set each call. pierce = ignore the Android front block. true = it did damage.
    public bool StrikeEnemy(TestEnemy e, Collider2D col, Vector2 from, bool pierce)
    {
        swingHits.Clear();
        swingHitStops = 0;
        swingDamaged = 0;
        HitEnemy(e, col, from, false, pierce);
        return swingDamaged > 0;
    }

    // DECISIONS 18 skill 1 / 6: teleport to a free spot just behind e (the far side from the player, else the near side), on its
    // feet level, facing it. Cancels dash / lunge / rope. false = dead player, no enemy collider, or both spots blocked by walls.
    public bool TryBlinkBehind(TestEnemy e)
    {
        if (state == State.Dead || e == null || e.BodyCollider == null) return false;
        Collider2D col = e.BodyCollider;
        float dx = e.transform.position.x - rb.position.x;
        int first = Mathf.Abs(dx) > 0.01f ? (dx > 0f ? 1 : -1) : Facing;
        const float halfW = 0.45f, halfH = 1f; // player box 0.9 x 2 (same as PlanBehind)
        for (int k = 0; k < 2; k++)
        {
            int side = k == 0 ? first : -first;
            Vector2 spot = new Vector2(e.transform.position.x + side * (col.bounds.extents.x + halfW + dashBehindClearance),
                                       col.bounds.min.y - feetOffset.y + 0.02f);
            if (BlinkSpotBlocked(spot, new Vector2(halfW * 2f - 0.1f, halfH * 2f - 0.2f))) continue;
            if (visual != null) SpriteFx.Afterimage(visual, AfterimageColor, afterimageLife); // ghost where we left
            if (state != State.Locomotion) EnterState(State.Locomotion); // ends lunge / dash / rope / parry
            dashTarget = null;
            dashTargetCol = null;
            dashBehindActive = false;
            grappleCarryUntil = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.position = spot;
            transform.position = spot; // same-frame queries (AttackCenter, HitInfo.SourcePosition) see the new spot
            Facing = -side;
            PlayDashBurst(side); // burst points the way we travelled
            Sfx.Play(SfxId.Dash);
            return true;
        }
        return false;
    }

    // DECISIONS 44 dice face 6 in the boss fight: a free standing spot on the ground `off` u beside `at` (the hero's side first, then
    // the far side) with a clear line from the hero. Ground = the first surface under (spot x, at.y); none within 10 u, a wall / solid
    // in the body box, or a wall between = that side is out. false = both out.
    public bool PlanSpotBeside(Vector2 at, float off, out Vector2 spot)
    {
        spot = default;
        if (state == State.Dead) return false;
        const float halfW = 0.45f, halfH = 1f; // player box 0.9 x 2 (same as PlanBehind)
        int first = rb.position.x <= at.x ? -1 : 1;
        for (int k = 0; k < 2; k++)
        {
            float x = at.x + (k == 0 ? first : -first) * off;
            RaycastHit2D g = Physics2D.Raycast(new Vector2(x, at.y), Vector2.down, 10f, groundMask);
            if (g.collider == null || g.distance < 0.01f || g.normal.y < 0.7f) continue; // nothing below / starts inside a solid
            Vector2 s = new Vector2(x, g.point.y - feetOffset.y + 0.02f);
            if (BlinkSpotBlocked(s, new Vector2(halfW * 2f - 0.1f, halfH * 2f - 0.2f)) || !ClearLine(rb.position, s)) continue;
            spot = s;
            return true;
        }
        return false;
    }

    // DECISIONS 44 dice face 6 boss dash, one frame of it (LuckyDice drives it under its input lock + i-frames): the body sits at `pos`
    // (still, so gravity can't drift it), faces `face`, and leaves a dash afterimage. first = the take-off (rope / swing dropped, burst, whoosh).
    public void DiceDashStep(Vector2 pos, int face, bool first)
    {
        if (state == State.Dead) return;
        if (first)
        {
            if (state != State.Locomotion) EnterState(State.Locomotion); // ends a rope / swing / parry
            grappleCarryUntil = 0f;
            PlayDashBurst(face);
            Sfx.Play(SfxId.Dash);
        }
        rb.linearVelocity = Vector2.zero;
        rb.position = pos;
        transform.position = pos;
        Facing = face;
        if (visual == null) return;
        visual.flipX = face < 0; // UpdateVisual already ran this frame with the old facing
        SpriteFx.Afterimage(visual, AfterimageColor, afterimageLife);
    }

    // Solid ground / walls in the box (one-way platforms do not count: the body can stand in one and drops or rises through).
    bool BlinkSpotBlocked(Vector2 at, Vector2 size)
    {
        foreach (Collider2D c in Physics2D.OverlapBoxAll(at, size, 0f, groundMask))
            if (!c.isTrigger && !c.usedByEffector) return true;
        return false;
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
        if (state == State.Dash || state == State.Issen) return HitResult.Dodged;
        if (health.Invulnerable || Time.time < grappleSafeUntil) return HitResult.Dodged; // grappleSafeUntil: hatch strike fall (DECISIONS 38)

        float dx = info.SourcePosition.x - transform.position.x;
        // DECISIONS 44: LMB pressed on this very frame, but the blow resolved before this Update read the press (script order is not
        // fixed): it parries, turned to the blow like FaceParryThreat would have. PlayerInputReader runs first (-100), so the press is known.
        bool pressedNow = input.AttackPressed && !info.Unparryable && !lunging && (state == State.Locomotion || state == State.Attack);
        if (pressedNow)
        {
            if (!Mathf.Approximately(info.TravelDirX, 0f)) Facing = info.TravelDirX > 0f ? -1 : 1;
            else if (!Mathf.Approximately(dx, 0f)) Facing = dx > 0f ? 1 : -1;
            ParryTries++;
        }
        // projectiles judge by travel direction (a shot spawned inside the player's box has no meaningful position side)
        bool fromFront = !Mathf.Approximately(info.TravelDirX, 0f)
            ? Mathf.Sign(info.TravelDirX) == -Facing
            : Mathf.Approximately(dx, 0f) || Mathf.Sign(dx) == Facing;
        // An LMB lunge parries what it runs into (playtest 11-10: a shot met on the way in counted as a hit). The lunge keeps going.
        if ((ParryActive || Time.time < parryGuardUntil || lunging || pressedNow) && fromFront && !info.Unparryable)
        {
            // No knockback for either side. Hit-stop + flash + parry FX. A guard parry during an attack must not abort the swing.
            if (state == State.Parry) EnterState(State.Locomotion);
            if (pressedNow) { pressSpentFrame = Time.frameCount; attackBufferedUntil = -999f; } // that press is spent: no swing buffered behind the parry hit-stop (or already buffered by this frame's Update, mid-swing)
            parryGuardUntil = Time.time + parryGuardTime;
            ParryCount++;
            // Shot: where the bullet is. Melee: the hero's front edge at the strike height, where the ring merges (TestEnemy.ParryContact).
            Vector2 contact = !Mathf.Approximately(info.TravelDirX, 0f) ? info.SourcePosition
                : (Vector2)transform.position + new Vector2(Facing * 0.45f, ParryRing.Offset(info.Height));
            SpriteFx.Play("parry_talisman", contact, Facing < 0, 102); // pivot = contact point; art takes a blow from the right
            ParryFx.ContactGlow(contact); // playtest 11-11: the contact glows, the hero no longer flashes white (DECISIONS 35)
            ShowParryPose(info.Height, ParryPoseTime, false);
            Sfx.Play(SfxId.Parry, 1f, ParryPitch); // DECISIONS 26: high clang (hurt is low)
            StartHitStop(Mathf.Max(hitStopTime, DashRules.ParryHitStop), false);
            Juice.Shake(Juice.ShakeParryAmp, Juice.ShakeParryTime);
            return HitResult.Parried;
        }

        if (state == State.Parry) ParryFailHits++;
        HurtFrame = Time.frameCount;
        health.TakeDamage(info.Damage);
        Sfx.Play(SfxId.Hurt, 1f, HurtPitch); // DECISIONS 26: duller, lower than the parry clang
        ScreenFlash.Hurt();
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
        runFrame = -1; // DraftFrame sets it when the run strip is what shows
        if (draftSprite)
            visual.sprite = now < duelPoseUntil ? duelPose
                : now < parryPoseUntil && (parryPoseForced || state == State.Locomotion) ? DraftSprites.Pick(parryPose, now - parryPoseAt < ParryPoseKey ? 0 : 1, DraftSprites.HeroAttack ?? DraftSprites.Hero)
                : DraftFrame(now);
        UpdateRunFeel(now);
        Color c = draftSprite ? Color.white : BaseColor;
        bool hurtBlink = state == State.Hurt && Mathf.FloorToInt(now * HurtBlinkHz) % 2 == 0; // DECISIONS 26: solid red on alternate ticks
        if (state == State.Dash || lunging || state == State.Issen) c = Color.white;
        else if (ParryActive) c = new Color(0.4f, 1f, 0.5f);
        else if (state == State.Parry) c = new Color(0.2f, 0.5f, 0.35f);
        else if (state == State.Dead) c = Color.clear; // the player-glitch-death FX takes the body away (MomentFx)
        else if (state == State.Hurt) c = new Color(1f, 0.35f, 0.35f); // hurt flash
        else if (health.HurtInvulnerable && Mathf.FloorToInt(now * 20f) % 2 == 0) c.a = 0.3f;
        if (hurtBlink) SpriteFlash.Apply(visual, c, true, HurtRed, HurtRed);
        else SpriteFlash.Apply(visual, c, false, Color.white, c); // no parry flash on the hero (DECISIONS 35); resets the shader flash
    }

    // DraftSprites: one hero frame per state (walk strip is the fallback when another strip is missing). Art faces right; flipX handles left.
    Sprite DraftFrame(float now)
    {
        // Strips have 2x frames (attack 5f / dash 3f, keys at 0/2/4 and 0/2): index = old third (half) * 2, so the strike key shows at the same moment as before.
        int t = Mathf.FloorToInt((now - stateEnteredAt) / (state == State.Dash ? dashEndsAfter / 2f : attackDuration / 3f) * 2f);
        switch (state)
        {
            case State.Attack: return DraftSprites.Pick(DraftSprites.HeroAttack, t, DraftSprites.Hero);
            case State.Parry:
                float swing = (now - stateEnteredAt) / ParrySwingTime; // the press swings first (presentation only)
                if (swing < 1f && DraftSprites.HeroAttack != null) return DraftSprites.Pick(DraftSprites.HeroAttack, (int)(swing * DraftSprites.HeroAttack.Length), DraftSprites.Hero);
                return DraftSprites.Pick(DraftSprites.HeroParry, ParryActive ? 0 : 1, DraftSprites.HeroAttack ?? DraftSprites.Hero);
            case State.Dash: return DraftSprites.Pick(DraftSprites.HeroDash, t, DraftSprites.Hero);
            case State.Issen: return DraftSprites.Pick(DraftSprites.HeroAttack, 4, DraftSprites.Hero); // follow-through key
            case State.Grapple: return DraftSprites.HeroGrapple != null ? DraftSprites.HeroGrapple[0] : DraftSprites.Pick(DraftSprites.HeroJump, 0, DraftSprites.Hero);
            case State.Hurt:
            case State.Dead: return DraftSprites.Pick(DraftSprites.HeroHurt, 0, DraftSprites.Hero);
        }
        if (!grounded)
        {
            float vy = rb.linearVelocity.y; // jump strip: 0 rise, 1 rise->apex, 2 apex->fall, 3 fall
            return DraftSprites.Pick(DraftSprites.HeroJump, vy > 2f ? 0 : vy > 0.1f ? 1 : vy > -2f ? 2 : 3, DraftSprites.Hero);
        }
        if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
        {
            Sprite[] run = DraftSprites.HeroRun ?? DraftSprites.Hero; // DECISIONS 15: normal movement shows the run cycle (walk = fallback)
            int f = Mathf.FloorToInt(now * (run == DraftSprites.Hero ? DraftSprites.WalkFps : DraftSprites.RunFps)) % run.Length;
            if (run != DraftSprites.Hero) runFrame = f;
            return run[f];
        }
        return DraftSprites.Pick(DraftSprites.HeroIdle, Mathf.FloorToInt(now * DraftSprites.IdleFps) % (DraftSprites.HeroIdle?.Length ?? 1), DraftSprites.Hero);
    }

    // Run feel on the visual child only (local rotation = lean, local Y = step bob; local X stays with ParryFx's nudge). Lean: forward 7 deg
    // while the run strip plays, 12 deg for the first 0.1 s from standstill, 6 deg backward for 0.08 s after a fast run is released / reversed
    // (+ a dust puff on release). Everything eases back to rest whenever the run strip is not showing (pose override, air, dash, attack, death).
    void UpdateRunFeel(float now)
    {
        float vx = rb.linearVelocity.x;
        bool running = runFrame >= 0;
        if (running && !runWasOn && Mathf.Abs(prevVx) < 0.1f) startLeanUntil = now + StartLeanTime;
        runWasOn = running;
        if (grounded && state == State.Locomotion && Mathf.Abs(prevVx) >= moveSpeed * runFxSpeedFrac && (Mathf.Abs(vx) < 0.1f || vx * prevVx < 0f))
        {
            skidUntil = now + SkidTime;
            skidDir = prevVx > 0f ? 1 : -1;
            if (Mathf.Abs(vx) < 0.1f) SpriteFx.PlayBehind("run-dust", rb.position + feetOffset, skidDir < 0, visual); // a reversal already puffs (turned)
        }
        prevVx = vx;

        float target = 0f;
        if (state == State.Dead) leanAngle = 0f;
        else if (now < skidUntil && grounded && state == State.Locomotion) target = skidDir * SkidLean; // back = against the travel direction (+z = counter-clockwise)
        else if (running) target = -Facing * (now < startLeanUntil ? StartLean : RunLean);
        leanAngle = Mathf.Lerp(leanAngle, target, 1f - Mathf.Exp(-Time.deltaTime / LeanTime));

        int bob = 0; // px: down on the contact frame, up through the flight frames
        if (running && state != State.Dead)
        {
            int half = DraftSprites.HeroRun.Length / 2;
            float u = (runFrame % half) / (float)half;
            bob = u == 0f ? -1 : u < 0.6f ? 1 : 0;
        }
        Transform v = visual.transform;
        v.localRotation = Quaternion.Euler(0f, 0f, leanAngle);
        Vector3 p = v.localPosition;
        p.y = visualBaseY + bob / SpriteFx.Ppu;
        v.localPosition = p;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(AttackCenter(), attackSize);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + feetOffset, feetSize);
    }
}
