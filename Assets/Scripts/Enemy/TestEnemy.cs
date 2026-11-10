using System.Collections.Generic;
using UnityEngine;

// Test enemy (brainwashed citizen). See docs/design/PROTOTYPE_ARCH.md sections 3-4.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyNumber))]
public class TestEnemy : MonoBehaviour, IDuelFoe
{
    public enum State { Approach, Windup, Strike, Recover, Turn, Staggered, Open, Stunned, Charge, Frozen, Special, Overload, RushWindup } // new states go last (BugSwarmVisual switches on them)

    [SerializeField] LayerMask playerMask;
    [SerializeField] SpriteRenderer visual; // optional; found in children if empty

    [Header("Tuning (T)")]
    [SerializeField] int maxHp = 3;                 // unused by Android since gimmick A (blocks until overloaded)
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float attackRangeX = 1.8f;
    [SerializeField] float attackRangeY = 1.5f;
    [SerializeField] float windupTime = 0.5f;       // first strike of a chain only
    [SerializeField] float chainWindupTime = 0.15f; // later strikes
    [SerializeField] float recoverTime = 0.15f;
    [SerializeField] float turnTime = 0.35f;
    [SerializeField] float staggerTime = 1f;
    [SerializeField] float openTime = 1f;
    [SerializeField] Vector2 attackOffset = new Vector2(1.0f, 0f);
    [SerializeField] Vector2 attackSize = new Vector2(1.8f, 1.4f);
    [SerializeField] float flashTime = 0.1f;
    [SerializeField] float chargeSpeed = 14f;
    [SerializeField] float chargeMaxTime = 0.6f;

    public enum Kind { Citizen, Bug, Android, Sniper, Elite } // new kinds go last (serialized as int)

    // ---- Sniper (DECISIONS 15): stands still, laser tracks the player, locks, fires one fast parryable shot along the locked line ----
    const float SniperRange = 19f;       // (T) u from the muzzle, needs a clear line (groundMask): about a whole screen
    const float SniperTrackTime = 1.0f;  // (T) laser follows the player
    const float SniperLockTime = 0.25f;  // (T) laser fixed + flashing, then the shot
    const float SniperShotSpeed = 14f;   // (T) u/s; LMB parry window = (DashRules.ParryShotRange 3 - contact 0.7) / this = 0.16 s (22 gave 0.1 s: too tight, Fable review)
    const float SniperReaimDelay = 0.3f; // (T) s after losing sight before aiming again (no tell flicker at the range / ledge edge)
    static readonly Color SniperTint = new Color(0.72f, 0.8f, 1f); // steel-blue tint on the ranged-citizen frames

    // ---- Elite (user 11-10): never strikes on its own, its attack is the clash duel (EliteDuel). Normal hits work (maxHp hits). ----
    static readonly Color EliteTint = new Color(0.95f, 0.42f, 0.36f); // deep crimson on the android frames until real art exists
    const float EliteScale = 1.25f;
    const float EliteDuelCooldown = 1.5f; // (T) s after a lost duel before it can start another

    // ---- Android guard (DECISIONS 15): frontal hits are blocked; from behind or above it dies in one hit ----
    const float AndroidTurnGrace = 0.35f; // (T) s: a hit this soon after it turned around during a player dash still counts as from behind (Shift dash-behind)
    const float AndroidAboveDy = 1.0f;    // (T) attacker centre at least this far above the android's centre (2.0 was out of reach of every attack; levels/Fable 11-09)
    const float AndroidDescendVy = 0.5f;  // (T) airborne attacker counts as "from above" only while moving down faster than this (u/s)

    [Header("Variants (defaults = melee citizen)")]
    [SerializeField] Kind kind = Kind.Citizen;       // NUM-0 freezes citizens only; Android uses the gold fan special
    [SerializeField] LayerMask groundMask;           // 0 = no edge check
    [SerializeField] float activationRange = 8f;     // idle until the player is this close (spotting range); 0 = always active
    [SerializeField] float edgeProbeForward = 0.7f;  // ground probe ahead of the feet
    [SerializeField] float edgeProbeDepth = 1.5f;
    [SerializeField] float lastStrikeDelay = 0f;     // special enemies: last strike lands this much later (flame tell)
    [SerializeField] int unparryableNumber = 0;      // last strike of a chain with this number cannot be parried (0 = none)
    [SerializeField] bool ranged = false;            // the strike fires a projectile instead of a melee box
    [SerializeField] float keepDistance = 5f;        // ranged: back off while the player is closer
    [SerializeField] float backoffMaxTime = 1f;      // ranged: give up backing off (cornered) and shoot anyway
    [SerializeField] float projectileSpeed = 9f;
    [SerializeField] Vector2 muzzleOffset = new Vector2(0.8f, 0.3f);
    [SerializeField] Color projectileColor = new Color(1f, 0.788f, 0.235f); // gold #FFC93C

    static readonly Color WindupColor = new Color(1f, 0.8f, 0.5f);
    static readonly Color StaggerColor = new Color(0.7f, 0.7f, 0.75f);
    static readonly Color StunnedColor = new Color(0.4f, 0.4f, 0.4f);
    static readonly Color FrozenColor = new Color(0.6f, 0.75f, 0.9f);
    static readonly Color OverloadColor = new Color(0.4f, 0.9f, 1f); // flicker colour while overloaded
    static readonly Color SpecialColor = new Color(1f, 0.788f, 0.235f); // gold tell of the fan special

    public State Current => state;
    public int Hp => hp;
    public EnemyNumber Number => number;
    public int StrikesLeft => strikesLeft;
    // Lucky dice skills: active enemies (OnEnable/OnDisable), down = knocked out or gone.
    public static readonly List<TestEnemy> All = new List<TestEnemy>();
    public bool IsDown => state == State.Stunned || !isActiveAndEnabled;
    public bool IsBossWave { get; set; } // set by BossFight: dice skills skip it
    public bool IsHuman => kind == Kind.Citizen || kind == Kind.Sniper; // DECISIONS 26: the RMB issen kills these in one hit (not bugs / androids / elite)
    public Collider2D BodyCollider { get; private set; }
    public bool InDuel { get; set; } // EliteDuel drives it (AI, hold and visuals skipped here; it poses through DuelPose)
    public SpriteRenderer Visual => visual;
    public bool DuelReady => state == State.Approach; // EliteDuel: anything else (dice freeze / hit) ends the duel
    public int Facing => facing;
    // player lunge stops in front instead of passing through (frontal approach to a guarding Android only)
    public bool BlocksHits => kind == Kind.Android && state != State.Overload && !(player != null && AndroidExposedTo(player.transform.position));

    Rigidbody2D rb;
    EnemyNumber number;
    PlayerController player;
    Rigidbody2D playerRb;     // Android guard: is the attacker coming down
    State state;
    float stateEnteredAt;
    int hp;
    int strikesLeft;      // the displayed number never changes; this counts the chain down
    bool chainStarted;    // first strike of a chain gets the long windup
    int facing = -1;
    Color baseColor = Color.white;
    bool draftSprite; // DraftSprites: attack frames (citizen / ranged citizen / android) instead of the placeholder square
    Sprite[] draftFrames, draftWalk, draftStunned; // attack 3f, walk 2f, stunned 1f (walk / stunned may be null = fall back to the attack strip)
    bool sniperArt; // sniper_aim strip in use (own 5-frame mapping in DraftFrame)
    bool flyer; // ranged bug with draft frames: flaps all the time and bobs 1 px (visual only; collider unchanged)
    Color flashColor;
    float flashUntil;
    float backoffSince = -1f; // ranged: when the player got too close (-1 = not too close)
    BroadcastBoard zoneBoard;
    int broadcast = -1;       // number of the zone's broadcast board (-1 = no board)
    int exhaustedCycle = -1;  // NUM-7: index of the dope/exhaust cycle (from the board's change time) already punished
    float nextSpecialAt;      // Android fan cooldown
    float openDuration;       // length of the current Open state
    float nextSparkAt;        // Overload: next spark burst
    bool rushing;             // DECISIONS 12: the current Charge is a rush (RushSpeed, RushMaxDistance)
    float nextRushAt;         // no new rush tell before this
    bool aggro;               // melee: spotted the player once -> stays active (DashRules.MeleeDetectMin)
    float facingChangedAt = -10f; // Android guard: last turn-around (AndroidTurnGrace)
    float nextDuelAt;         // elite: no new duel before this
    SniperSight sight;
    ParryRing ring;           // melee parry timing tell (built on first strike)
    float ringTotal = -1f;    // eta when the current ring started (-1 = not started; reset by EnterState except into Strike)
    Vector2 ringContact;      // target ring centre, fixed for the windup
    ParryHeight strikeHeight; // melee: rolled when a Windup / Charge starts (ring + player parry FX / pose; presentation only)
    Vector2 sniperDir;        // aim (unit), fixed once locked
    bool sniperLocked;
    float nextAimAt;          // sniper: no new aim before this (SniperReaimDelay)
    float frozenUntil;        // dice Overload on a non-android: Frozen until this (0 = board freeze only)
    bool held;                // GameFlow.EnemiesHeld: body parked (no gravity, velocity stored)
    Vector2 heldVelocity;     // restored on release (kill knockback landed during the hold)
    float heldGravity;

    // NUM-7 clock is the board's (shared by every enemy of the zone): dope for Num7DopeSeconds, then exhaustion, repeating.
    float Num7Phase => zoneBoard != null ? Time.time - zoneBoard.NumberChangedAt : 0f;
    static float Num7Cycle => BroadcastRules.Num7DopeSeconds + BroadcastRules.Num7ExhaustSeconds;
    bool Doped => broadcast == 7 && zoneBoard != null && Num7Phase % Num7Cycle < BroadcastRules.Num7DopeSeconds;
    bool Normal => kind != Kind.Android && kind != Kind.Elite;
    float TimeMul => Normal ? Juice.NormalTimeMul : 1f; // Citizen/Bug act faster since they die in one hit
    float Speed => moveSpeed * (Normal ? Juice.NormalSpeedMul : 1f) * (Doped ? BroadcastRules.Num7SpeedMul : 1f);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        number = GetComponent<EnemyNumber>();
        BodyCollider = GetComponent<Collider2D>();
        if (BodyCollider == null) BodyCollider = GetComponentInChildren<Collider2D>();
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        // Bugs: spider (melee) / flyer (ranged) frames; without them BugSwarmVisual draws the dot swarm. Ranged citizens use the
        // hooded ranged-citizen frames (DECISIONS 14). Bugs have no stunned frame: the recover frame, tipped over.
        if (kind == Kind.Sniper) ranged = true; // a sniper is a ranged citizen that never walks (ranged rules: no NUM-3 pin, no rush, no melee parry)
        bool bug = kind == Kind.Bug;
        bool androidArt = kind == Kind.Android || kind == Kind.Elite;
        sniperArt = kind == Kind.Sniper && DraftSprites.SniperAim != null; // missing art: ranged-citizen frames + tint
        draftFrames = sniperArt ? DraftSprites.SniperAim : androidArt ? DraftSprites.Android : bug ? (ranged ? DraftSprites.BugFlyerAttack : DraftSprites.Bug) : ranged ? DraftSprites.CitizenRanged : DraftSprites.Citizen;
        draftSprite = DraftSprites.Setup(visual, draftFrames);
        draftWalk = sniperArt ? DraftSprites.SniperWalk : androidArt ? DraftSprites.AndroidWalk : bug ? (ranged ? DraftSprites.BugFlyer : DraftSprites.BugWalk) : ranged ? DraftSprites.CitizenRangedWalk : DraftSprites.CitizenWalk;
        draftStunned = sniperArt ? DraftSprites.SniperStunned : androidArt ? DraftSprites.AndroidStunned : bug ? null : DraftSprites.CitizenStunned;
        flyer = bug && ranged && draftSprite;
        if (visual != null) baseColor = kind == Kind.Sniper && !draftSprite ? SniperTint : visual.color; // white after the draft sprite swap
        if (kind == Kind.Elite && visual != null)
        {
            baseColor = EliteTint;
            visual.transform.localScale *= EliteScale; // feet pivot: grows upward
        }
        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        hp = maxHp;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player != null) playerRb = player.GetComponent<Rigidbody2D>();
        EnterState(State.Approach);
        zoneBoard = BroadcastBoard.At(transform.position); // zone is fixed: find it once, then react to its changes
        if (zoneBoard != null)
        {
            zoneBoard.NumberChanged += OnBroadcastChanged;
            OnBroadcastChanged(zoneBoard, zoneBoard.Number, -1);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear(); // Domain Reload is off
    void OnEnable() => All.Add(this);
    void OnDisable()
    {
        All.Remove(this);
        if (held) { held = false; rb.gravityScale = heldGravity; } // never leave a parked body without gravity
    }

    void OnDestroy()
    {
        if (zoneBoard != null) zoneBoard.NumberChanged -= OnBroadcastChanged;
    }

    void Update()
    {
        if (InDuel) return; // EliteDuel holds everyone else and poses this one
        if (GameFlow.EnemiesHeld) // dice hold: same freeze as the dialogue lock (Stunned vanish timer included), visuals still update
        {
            stateEnteredAt += Time.deltaTime;
            if (backoffSince >= 0f) backoffSince += Time.deltaTime;
            nextSpecialAt += Time.deltaTime;
            nextSparkAt += Time.deltaTime;
            nextRushAt += Time.deltaTime;
            nextAimAt += Time.deltaTime;
            if (frozenUntil > 0f) frozenUntil += Time.deltaTime;
            if (sight != null && state != State.Windup) sight.Hide(); // knocked out / frozen while held: no laser
            if (ring != null && (player == null || !MeleeStrikeEta(player.transform.position, out _))) ring.Hide(); // same for the parry rings (else they freeze)
            ApplyVisual();
            return;
        }
        if (GameFlow.InputLocked) // blocking dialogue: freeze the AI and push its state timer forward
        {
            stateEnteredAt += Time.deltaTime;
            flashUntil += Time.deltaTime;
            if (backoffSince >= 0f) backoffSince += Time.deltaTime;
            nextSpecialAt += Time.deltaTime;
            nextSparkAt += Time.deltaTime;
            nextRushAt += Time.deltaTime;
            if (frozenUntil > 0f) frozenUntil += Time.deltaTime;
            return;
        }
        // Dice Overload freeze ran out; a zone still on 0 keeps it frozen (the board releases it).
        if (state == State.Frozen && frozenUntil > 0f && Time.time >= frozenUntil && broadcast != 0) { frozenUntil = 0f; FinishReset(); }
        if (state == State.Stunned && Time.time - stateEnteredAt >= Juice.KillVanishDelay)
        {
            if (visual != null) PlayVanishSpark();
            gameObject.SetActive(false); // inactive, not destroyed: BossFight keeps a valid (Stunned) entry, and the board listener stays harmless
            return;
        }
        if (state != State.Stunned) UpdateDope();
        if (player != null && state != State.Stunned) UpdateState();
        if (kind == Kind.Sniper) UpdateSight();
        ApplyVisual();
        UpdateParryRing();
    }

    void FixedUpdate()
    {
        if (GameFlow.EnemiesHeld) // park the body in place; the stored velocity (e.g. kill knockback) comes back on release
        {
            if (!held) { held = true; heldVelocity = rb.linearVelocity; heldGravity = rb.gravityScale; rb.gravityScale = 0f; }
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (held) { held = false; rb.gravityScale = heldGravity; rb.linearVelocity = heldVelocity; }
        if (GameFlow.InputLocked) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }
        if (state == State.Stunned) return; // keep the kill knockback until the body vanishes
        float vx = 0f;
        bool chasing = false; // spotted the player and heading for them: may drop off a roof edge (see DropOk)
        // Recover also closes the gap so the chain keeps landing after the player's hurt knockback.
        // Only toward a player in front: Recover does not re-face, so never walk away from one who got behind.
        if (kind == Kind.Sniper) { } // stands still
        else if ((state == State.Approach || state == State.Recover) && player != null &&
            (player.transform.position.x - transform.position.x) * facing > attackRangeX)
        {
            vx = facing * Speed; // stop under a jumping player instead of jittering
            chasing = state == State.Approach && IsActive();
            if (state == State.Approach && !chasing) vx = 0f; // idle until activated
        }
        else if (state == State.Approach && ranged && IsBackingOff())
            vx = -facing * Speed;
        if (state == State.Charge) { vx = facing * (rushing ? DashRules.RushSpeed : chargeSpeed); chasing = true; }
        if (vx != 0f && !(chasing && DropOk()) && EdgeAhead(vx > 0f ? 1 : -1)) vx = 0f;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    // Zone broadcast rules (DECISIONS 1-A, 7-A, NUM-0). Values in BroadcastRules; delete a block to remove its rule.
    void OnBroadcastChanged(BroadcastBoard board, int n, int old)
    {
        if (state == State.Stunned) return;
        broadcast = n;
        // NUM-3: melee numbers are pinned to 3 while the zone shows 3. Never rerolled mid-chain, else it applies at the next FinishReset.
        number.Pin(n == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3Number : 0);
        if (!ranged && (n == BroadcastRules.Num3Number || old == BroadcastRules.Num3Number) && state == State.Approach && !chainStarted)
        {
            number.Reroll();
            strikesLeft = number.Value;
        }
        // NUM-7: doping starts. Leaving 7 early just clears it (no exhaustion when the number is hacked away).
        if (n == 7) exhaustedCycle = -1;
        // NUM-0: citizens stop breathing; the number changing away releases them.
        if (n == 0 && (kind == Kind.Citizen || kind == Kind.Sniper)) EnterState(State.Frozen); // the sniper is a citizen too
        else if (old == 0 && state == State.Frozen && Time.time >= frozenUntil) { frozenUntil = 0f; FinishReset(); } // a dice freeze still running ends on its own timer
    }

    // NUM-7 timer (time-based, not a board poll).
    void UpdateDope()
    {
        // NUM-7: dope ran out -> exhausted (hittable); a zone that stays on 7 cycles dope / exhaustion.
        if (broadcast != 7 || zoneBoard == null || state == State.Stunned || state == State.Frozen || state == State.Overload) return;
        float phase = Num7Phase;
        int cycle = Mathf.FloorToInt(phase / Num7Cycle);
        float inCycle = phase - cycle * Num7Cycle;
        if (cycle == exhaustedCycle || inCycle < BroadcastRules.Num7DopeSeconds) return;
        exhaustedCycle = cycle;
        strikesLeft = 0;
        number.SetPulse(false);
        EnterState(State.Open);
        openDuration = Num7Cycle - inCycle; // rest of the shared exhaustion window
    }

    bool IsActive()
    {
        if (activationRange <= 0f || aggro) return true;
        float range = ranged ? activationRange : Mathf.Max(activationRange, DashRules.MeleeDetectMin);
        if (Mathf.Abs(player.transform.position.x - transform.position.x) > range) return false; // height ignored
        aggro = !ranged; // melee: once spotted, keep chasing (playtest 11-09)
        return true;
    }

    // Ranged: player closer than keepDistance (same height band) and we have not been backing off too long.
    bool IsBackingOff()
    {
        if (player == null || !IsActive()) return false;
        Vector2 d = player.transform.position - transform.position;
        if (Mathf.Abs(d.x) >= keepDistance || Mathf.Abs(d.y) > attackRangeY) { backoffSince = -1f; return false; }
        if (backoffSince < 0f) backoffSince = Time.time; // timer starts when the player gets too close, not on Approach entry
        return Time.time - backoffSince < backoffMaxTime;
    }

    // Chasing walks off a ledge only toward a player who is below and close (D2 roof drop). Permanent melee aggro
    // (IsActive) otherwise made far-off chasers fall off roofs toward the player (Fable review): they now wait at the edge.
    bool DropOk()
    {
        if (player == null) return false;
        Vector2 d = player.transform.position - transform.position;
        return d.y < -0.5f && Mathf.Abs(d.x) <= DropMaxDistance;
    }
    const float DropMaxDistance = 4f; // (T) x-distance within which a chaser drops toward a lower player

    // No ground just ahead of the feet (needs groundMask).
    bool EdgeAhead(int dir)
    {
        if (groundMask.value == 0) return false;
        Vector2 origin = (Vector2)transform.position + new Vector2(dir * edgeProbeForward, -0.9f);
        return Physics2D.Raycast(origin, Vector2.down, edgeProbeDepth, groundMask).collider == null;
    }

    void UpdateState()
    {
        float t = Time.time - stateEnteredAt;
        switch (state)
        {
            case State.Approach:
                if (kind == Kind.Sniper)
                {
                    if (Time.time >= nextAimAt && SniperSees()) // Windup = aiming
                    {
                        FacePlayer();
                        sniperDir = ((Vector2)player.transform.position - Muzzle).normalized; // this frame's laser already points at the player
                        EnterState(State.Windup);
                    }
                    break;
                }
                if (!IsActive()) break; // idle until the player comes close
                FacePlayer();
                if (kind == Kind.Elite) // its attack is the duel; EliteDuel checks range, height and the player's state
                {
                    if (Time.time >= nextDuelAt) EliteDuel.TryBegin(this, player);
                    break;
                }
                Vector2 d = player.transform.position - transform.position;
                if (Mathf.Abs(d.x) <= attackRangeX && Mathf.Abs(d.y) <= attackRangeY && !(ranged && IsBackingOff()))
                    EnterState(State.Windup);
                else if (Normal && !ranged && Mathf.Abs(d.y) <= attackRangeY && Mathf.Abs(d.x) <= DashRules.RushRange && Time.time >= nextRushAt)
                    EnterState(State.RushWindup); // DECISIONS 12: short tell, then rush at the player
                break;
            case State.RushWindup:
                if (t >= (Doped ? DashRules.RushWindup / BroadcastRules.Num7SpeedMul : DashRules.RushWindup))
                {
                    FacePlayer();
                    rushing = true; // kept by EnterState(Charge)
                    EnterState(State.Charge);
                }
                break;
            case State.Windup:
                if (kind == Kind.Sniper && !UpdateSniperAim(t)) break; // lost sight: back to Approach
                if (t >= WindupWait()) EnterState(State.Strike);
                break;
            case State.Strike:
                DoStrike();
                break;
            case State.Recover:
                if (t >= recoverTime * TimeMul) NextAfterChain();
                break;
            case State.Charge:
                Vector2 cd = player.transform.position - transform.position;
                if (Mathf.Abs(cd.x) <= attackRangeX && Mathf.Abs(cd.y) <= attackRangeY)
                    EnterState(strikesLeft == 1 && lastStrikeDelay > 0f ? State.Windup : State.Strike); // keep the late last strike
                else if ((cd.x * facing < 0f)) EnterState(State.Turn); // player slipped behind mid-charge
                else if (rushing && t >= DashRules.RushMaxDistance / DashRules.RushSpeed)
                {
                    nextRushAt = Time.time + DashRules.RushCooldown; // missed: walk again, no instant re-rush
                    number.SetPulse(false); // RushWindup/Charge turned them on; only a Strike would turn them off
                    number.SetFlame(false);
                    EnterState(State.Approach);
                }
                else if (!rushing && t >= chargeMaxTime) EnterState(State.Recover);
                break;
            case State.Turn:
                if (t >= turnTime) { FacePlayer(); NextAfterChain(); }
                break;
            case State.Staggered:
                if (t >= openDuration) FinishReset(); // NUM-3 melee: 1.5s counter window (set in EnterState)
                break;
            case State.Open:
                if (t >= openDuration) FinishReset();
                break;
            case State.Overload:
                if (Time.time >= nextSparkAt) { PlayOverloadSpark(); nextSparkAt = Time.time + Juice.OverloadSparkInterval; }
                if (t >= Juice.OverloadSeconds) FinishReset(); // unused: back to the normal pattern
                break;
            case State.Special:
                if (t >= BroadcastRules.AndroidFanTell)
                {
                    FireFan();
                    nextSpecialAt = Time.time + BroadcastRules.AndroidFanCooldown;
                    // Continue the chain, but re-check facing and range first (the player may have moved during the tell).
                    FacePlayer();
                    Vector2 sd = player.transform.position - transform.position;
                    bool inRange = Mathf.Abs(sd.x) <= attackRangeX && Mathf.Abs(sd.y) <= attackRangeY;
                    EnterState(inRange ? State.Windup : State.Charge);
                }
                break;
        }
    }

    void NextAfterChain()
    {
        if (kind == Kind.Android && strikesLeft > 0 && Time.time >= nextSpecialAt) EnterState(State.Special);
        else EnterState(strikesLeft > 0 ? State.Windup : State.Open);
    }

    // Gold fan: parryable shots spread around the facing direction.
    void FireFan()
    {
        Vector2 muzzle = (Vector2)transform.position + new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
        var hit = new HitInfo { Damage = 1, SourcePosition = transform.position, IsSpecial = false, Unparryable = false };
        int count = BroadcastRules.AndroidFanCount;
        for (int i = 0; i < count; i++)
        {
            float angle = (i - (count - 1) * 0.5f) * BroadcastRules.AndroidFanSpreadDeg;
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * new Vector2(facing, 0f);
            EnemyProjectile.Spawn(muzzle, dir * BroadcastRules.AndroidFanSpeed, playerMask, groundMask, hit, projectileColor, DraftSprites.BulletAndroid);
        }
    }

    Vector2 Muzzle => (Vector2)transform.position + new Vector2(muzzleOffset.x * facing, muzzleOffset.y);

    // Sniper: player within SniperRange of the muzzle and no ground in between.
    bool SniperSees()
    {
        Vector2 m = Muzzle, p = player.transform.position;
        if ((p - m).sqrMagnitude > SniperRange * SniperRange) return false;
        return groundMask.value == 0 || Physics2D.Linecast(m, p, groundMask).collider == null;
    }

    // Sniper Windup: track for SniperTrackTime (losing sight aborts), then lock the line. false = aborted.
    bool UpdateSniperAim(float t)
    {
        if (t >= SniperTrackTime) { sniperLocked = true; return true; }
        if (!SniperSees())
        {
            number.SetPulse(false);
            number.SetFlame(false);
            nextAimAt = Time.time + SniperReaimDelay;
            EnterState(State.Approach);
            return false;
        }
        FacePlayer();
        Vector2 d = (Vector2)player.transform.position - Muzzle;
        sniperDir = d.sqrMagnitude > 0.0001f ? d.normalized : new Vector2(facing, 0f);
        return true;
    }

    // Laser while aiming (Windup), to the first ground hit or SniperRange.
    void UpdateSight()
    {
        if (sight == null) sight = SniperSight.For(gameObject);
        if (state != State.Windup) { sight.Hide(); return; }
        Vector2 m = Muzzle;
        Vector2 dir = sniperDir.sqrMagnitude > 0f ? sniperDir : new Vector2(facing, 0f);
        float len = SniperRange;
        if (groundMask.value != 0)
        {
            RaycastHit2D h = Physics2D.Raycast(m, dir, SniperRange, groundMask);
            if (h.collider != null) len = h.distance;
        }
        sight.Show(m, m + dir * len, sniperLocked);
    }

    // DECISIONS 25 sniper shot relay: a human going about its pattern (not down, frozen, open, staggered, dueling or a boss wave).
    public bool CanRelay => IsHuman && !IsBossWave && !InDuel && isActiveAndEnabled &&
        (state == State.Approach || state == State.Windup || state == State.Recover || state == State.Turn || state == State.Charge || state == State.RushWindup);

    // Batting the shot spends one strike: the chain (or the next one, from Approach) is one shorter; the displayed number stays.
    // The last strike spent = straight into the counter window, as after a finished chain.
    public void SpendStrikeOnRelay(Vector2 shotPos)
    {
        ParryFx.NudgeVisual(visual != null ? visual.transform : null, shotPos.x >= transform.position.x ? 1 : -1);
        number.Pop();
        strikesLeft--; // Approach: the count was set on entering it and is kept into the Windup
        if (strikesLeft > 0)
        {
            if (state != State.Approach) number.SetFlame(strikesLeft == 1); // Windup / RushWindup light it on entry
            return;
        }
        strikesLeft = 0;
        number.SetPulse(false);
        EnterState(State.Open);
    }

    void FinishReset()
    {
        number.SetBroken(false);
        number.Reroll();
        EnterState(State.Approach);
    }

    void DoStrike()
    {
        bool special = strikesLeft == 1; // last strike of the chain
        HitResult result = HitResult.Dodged; // a whiff counts as dodged for the number...
        bool dashedThrough = false;

        bool unparryable = special && unparryableNumber > 0 && number.Value == unparryableNumber;
        var hit = new HitInfo
        {
            Damage = 1,
            SourcePosition = transform.position,
            IsSpecial = special,
            Unparryable = unparryable,
            Height = ranged ? ParryHeight.Mid : strikeHeight
        };
        Collider2D col = null;
        if (ranged)
        {
            if (kind == Kind.Sniper)
            {
                Vector2 shot = sniperDir.sqrMagnitude > 0f ? sniperDir : new Vector2(facing, 0f);
                EnemyProjectile.Spawn(Muzzle, shot * SniperShotSpeed, playerMask, groundMask, hit, projectileColor, DraftSprites.BulletSniper)
                    .RelayShooter = this; // DECISIONS 25: other humans on its path bat it at the player
                MomentFx.SniperMuzzle(Muzzle, shot);
            }
            else
                EnemyProjectile.Spawn(Muzzle, facing, projectileSpeed, playerMask, groundMask, hit, projectileColor,
                    kind == Kind.Bug ? DraftSprites.BulletBug : DraftSprites.BulletCitizen);
        }
        else
        {
            Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
            ParryFx.NudgeVisual(visual != null ? visual.transform : null, facing); // the rings are the tell now (no red box)
            if (ring != null) ring.Flash(); // rings merge here
            col = Physics2D.OverlapBox(center, attackSize, 0f, playerMask);
            if (col != null)
            {
                PlayerController pc = col.GetComponentInParent<PlayerController>();
                if (pc != null)
                {
                    // Dodged also covers post-hit invulnerability/Dead; only an actual dash counts for the stagger.
                    dashedThrough = pc.CurrentState == PlayerController.State.Dash;
                    result = pc.ReceiveEnemyHit(hit);
                }
            }
            else if (player.CurrentState == PlayerController.State.Dash)
            {
                // Dash-behind (DECISIONS 11): the dash began inside this hitbox and carried the player out before the strike landed.
                // Still a dash-through (last-strike stagger, Android 7th-strike overload); the strike itself is a whiff (Dodged).
                Vector2 s = player.DashStart - center;
                dashedThrough = Mathf.Abs(s.x) <= attackSize.x * 0.5f + 0.45f && Mathf.Abs(s.y) <= attackSize.y * 0.5f + 1f // player box 0.9 x 2
                    && Mathf.Sign(player.transform.position.x - player.DashStart.x) == Mathf.Sign(transform.position.x - player.DashStart.x); // toward/past the enemy only; dashing away stays a Charge trigger
            }
        }
        strikesLeft--;
        chainStarted = true;
        number.SetPulse(false);
        number.Pop();
        number.SetFlame(strikesLeft == 1); // tell: next strike is the last one

        // Android gimmick A: parrying the last strike (or dashing through the unparryable 7th) overloads it.
        if (kind == Kind.Android && special && (result == HitResult.Parried || (unparryable && dashedThrough && result == HitResult.Dodged)))
        {
            if (result == HitResult.Parried) Flash(Juice.ParryEnemyTint, Juice.ParryEnemyTintTime);
            EnterState(State.Overload);
            return;
        }
        // ...but only a real dash-through (player was inside the hitbox) staggers; a plain whiff does not.
        if (special && dashedThrough && result == HitResult.Dodged)
        {
            EnterState(State.Staggered);
            return;
        }
        // Same x counts as in front (Mathf.Sign(0) is 1, which wrongly flagged "behind" when facing left).
        float dx = (player.transform.position.x - transform.position.x) * facing;
        bool playerBehind = dx < 0f;
        // Player dashed away out of range (nothing was hit, not a dash-through): charge after them.
        if (!ranged && !playerBehind && col == null && dx > attackRangeX && strikesLeft > 0)
        {
            EnterState(State.Charge);
            return;
        }
        if (result == HitResult.Parried) Flash(Juice.ParryEnemyTint, Juice.ParryEnemyTintTime);
        EnterState(playerBehind ? State.Turn : State.Recover);
    }

    void EnterState(State next)
    {
        state = next;
        stateEnteredAt = Time.time;
        if (next != State.Charge) rushing = false;
        if (next != State.Strike) ringTotal = -1f; // Windup/Charge -> Strike keeps the merged ring until DoStrike flashes it
        if (next == State.Windup || next == State.Charge) strikeHeight = ParryRing.RandomHeight();
        switch (next)
        {
            case State.Approach:
                strikesLeft = number.Value; // new chain
                chainStarted = false;
                backoffSince = -1f; // each new chain may back off again
                break;
            case State.Windup:
            case State.RushWindup:
                sniperLocked = false;
                number.SetPulse(true);
                number.SetFlame(strikesLeft == 1); // single-strike chain: tell during its windup
                Sfx.Play(strikesLeft == 1 ? SfxId.LastTell : SfxId.EnemyTell);
                break;
            case State.Charge:
                number.SetPulse(true);
                break;
            case State.Open:
            case State.Staggered:
                number.SetFlame(false);
                openDuration = broadcast == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3OpenTime : openTime;
                if (next == State.Open) Sfx.Play(SfxId.Stunned, 0.6f); // counter window opens
                break;
            case State.Frozen:
                number.SetPulse(false);
                number.SetFlame(false);
                break;
            case State.Special:
                FacePlayer();
                number.SetPulse(true);
                number.SetFlame(false); // no last-strike flame during the fan; Windup re-lights it
                break;
            case State.Overload:
                strikesLeft = 0;
                number.SetPulse(false);
                number.SetFlame(false);
                number.SetBroken(true);
                nextSparkAt = Time.time;
                break;
            case State.Stunned:
                number.Hide();
                Sfx.Play(SfxId.Stunned);
                rb.linearVelocity = Vector2.zero;
                if (visual != null && !(draftSprite && draftStunned != null)) visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f * -facing);
                break;
        }
    }

    void FacePlayer()
    {
        if (player == null) return;
        float dx = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) <= 0.01f) return;
        int f = dx > 0f ? 1 : -1;
        if (f == facing) return;
        facing = f;
        // Only a turn made while the player dashes past opens the behind grace (Shift counter), not every turn (Fable review).
        if (player.CurrentState == PlayerController.State.Dash) facingChangedAt = Time.time;
    }

    // Android guard: an attacker at `from` hits its back or comes from above (DECISIONS 15).
    // Behind = on the side it does not face, or it turned around within AndroidTurnGrace (it turns to face a player who just
    // dashed past, before the Shift counter lands). Above = attacker centre >= AndroidAboveDy over its centre AND the player either
    // stands on higher ground or is coming down (falling / downward lunge); a rising or level jump-dash into its face stays blocked.
    bool AndroidExposedTo(Vector2 from)
    {
        bool behind = (from.x - transform.position.x) * facing < 0f || Time.time - facingChangedAt < AndroidTurnGrace;
        bool above = from.y - transform.position.y >= AndroidAboveDy && player != null &&
                     (player.Grounded || player.LungingDown || (playerRb != null && playerRb.linearVelocity.y < -AndroidDescendVy));
        return behind || above;
    }

    float WindupWait()
    {
        if (kind == Kind.Sniper) return SniperTrackTime + SniperLockTime;
        float chainWait = broadcast == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3ChainWindupTime : chainWindupTime;
        float wait = chainStarted ? chainWait * TimeMul : Mathf.Max(windupTime * TimeMul, Normal ? Juice.MinFirstWindup : 0f);
        if (Doped) wait /= BroadcastRules.Num7SpeedMul;
        if (strikesLeft == 1) wait += lastStrikeDelay; // flame is already on for the last strike
        return wait;
    }

    // LMB parry merge (DECISIONS 12-B): a parryable melee strike is about to land (Windup with <= imminentWindow left, or Strike)
    // and its box overlaps a player (0.9 x 2) centred at playerPos. Earlier in the windup LMB stays a lunge/swing (user: "직전에만 패링").
    // eta = seconds until the strike lands (0 in Strike).
    public bool MeleeThreatens(Vector2 playerPos, float imminentWindow, out float eta)
    {
        if (!MeleeStrikeEta(playerPos, out eta) || eta > imminentWindow) return false;
        if (state == State.Charge) return true;
        Vector2 c = (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
        return Mathf.Abs(playerPos.x - c.x) <= attackSize.x * 0.5f + 0.45f && Mathf.Abs(playerPos.y - c.y) <= attackSize.y * 0.5f + 1f;
    }

    // A parryable melee strike is coming (Windup / Charge toward the player) or landing (Strike); eta = seconds until it lands.
    // Shared by the LMB parry (MeleeThreatens) and the timing rings, so the picture and the parry window cannot disagree.
    bool MeleeStrikeEta(Vector2 playerPos, out float eta)
    {
        eta = 0f;
        if (ranged || (state != State.Windup && state != State.Strike && state != State.Charge) || !gameObject.activeInHierarchy) return false;
        if (strikesLeft == 1 && unparryableNumber > 0 && number.Value == unparryableNumber) return false; // unparryable last strike: dash it
        if (state == State.Charge)
        {
            // Charge enters Strike on reaching attackRangeX and strikes the next frame, so without this the only parryable frame
            // was that one Strike frame (Fable review 11-09: an arriving Android got a lunge -> block -> hit). A late last strike
            // goes through Windup first and is caught there.
            if (strikesLeft == 1 && lastStrikeDelay > 0f) return false;
            float ahead = (playerPos.x - transform.position.x) * facing;
            if (ahead < 0f || Mathf.Abs(playerPos.y - transform.position.y) > attackRangeY) return false;
            eta = Mathf.Max(0f, ahead - attackRangeX) / (rushing ? DashRules.RushSpeed : chargeSpeed);
            return true;
        }
        if (state == State.Windup) eta = Mathf.Max(0f, WindupWait() - (Time.time - stateEnteredAt));
        return true;
    }

    // Melee parry timing rings (user 11-10): shown while MeleeStrikeEta holds, merged at eta 0, flashed by DoStrike.
    // Not called while EnemiesHeld / InputLocked (Update returns early), and hit-stop stops Time.time: the rings freeze there.
    void UpdateParryRing()
    {
        if (ranged || (ring != null && ring.Flashing)) return;
        if (player == null || !MeleeStrikeEta(player.transform.position, out float eta))
        {
            if (ring != null) ring.Hide(); // stunned / frozen / staggered / killed / recovered / unparryable: gone
            ringTotal = -1f;
            return;
        }
        if (ring == null) ring = ParryRing.For(gameObject);
        if (ringTotal < 0f || state == State.Charge) ringContact = ParryContact(); // fixed during a windup; follows the player during a charge
        if (ringTotal < 0f) ringTotal = eta;
        ring.Show(ringContact, facing, ringTotal, eta, MeleeThreatens(player.transform.position, DashRules.ParryMeleeWindow, out _)); // armed = LMB really parries (box check too)
    }

    // Where the blow meets the player: the player's front edge (body half-width 0.45) at the attack box's height, kept inside the
    // attack box during a windup so a player standing deep in it or at its far edge still gets a ring on the box.
    // The strike height moves it up / down, kept inside the box (1.4 tall: High +0.75 lands on its top edge, +0.7).
    Vector2 ParryContact()
    {
        Vector2 c = (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
        float x = player.transform.position.x - facing * 0.45f;
        if (state != State.Charge) x = Mathf.Clamp(x, c.x - attackSize.x * 0.5f, c.x + attackSize.x * 0.5f);
        float half = attackSize.y * 0.5f;
        return new Vector2(x, c.y + Mathf.Clamp(ParryRing.Offset(strikeHeight), -half, half));
    }

    // true = damage applied
    public bool ReceivePlayerHit(HitInfo info)
    {
        if (state == State.Stunned) return false;
        if (kind == Kind.Android && !info.Pierce && state != State.Overload && !AndroidExposedTo(info.SourcePosition)) // frontal hit on a guarding Android
        {
            Flash(Juice.BlockTint); // white was invisible on the (white) draft sprite
            Vector2 p = (Vector2)transform.position + new Vector2(0.45f * facing, 0.3f); // deflect spark on its front
            SpriteFx.Play("hit-spark", p - new Vector2(facing * 12f, 16f) / SpriteFx.Ppu, facing < 0);
            Sfx.Play(SfxId.Parry, 0.5f);
            return false;
        }
        // Citizen/Bug/Sniper in any state; Android overloaded, or hit from behind / above: one hit knocks it down. Elite: maxHp hits.
        Flash(Color.red);
        if (kind == Kind.Elite && --hp > 0) return true;
        hp = 0;
        EnterState(State.Stunned);
        if (ring != null) ring.Hide(); // killed under InputLocked (dice skill 6): Update won't run to hide them
        float dir = Mathf.Approximately(info.SourcePosition.x, transform.position.x) ? -facing : Mathf.Sign(transform.position.x - info.SourcePosition.x);
        rb.linearVelocity = new Vector2(dir * Juice.KillKnockback.x, Juice.KillKnockback.y);
        if (held) heldVelocity = rb.linearVelocity; // killed during a dice hold: fly off on release
        return true;
    }

    // Overload sparks: existing hit-spark FX at a random spot on the body.
    void PlayOverloadSpark()
    {
        Vector2 p = (Vector2)transform.position + new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-0.5f, 0.6f));
        SpriteFx.Play("hit-spark", p - new Vector2(12f, 16f) / SpriteFx.Ppu, Random.value < 0.5f);
    }

    void PlayVanishSpark()
    {
        if (!visual.enabled) return; // shattered elite: nothing left to spark
        SpriteFx.Play("hit-spark", (Vector2)transform.position - new Vector2(12f, 16f) / SpriteFx.Ppu);
    }

    void Flash(Color c) => Flash(c, flashTime);

    void Flash(Color c, float seconds)
    {
        flashColor = c;
        flashUntil = Time.time + seconds;
    }

    // Stunned / Overload = stunned frame; Approach = walk frames while moving; windup (and the android's fan tell) / strike / recover = attack frames 0 / 1 / 2; the rest rests on the recover pose.
    Sprite DraftFrame()
    {
        if ((state == State.Stunned || (state == State.Overload && kind == Kind.Android)) && draftStunned != null) return draftStunned[0];
        if (flyer && draftWalk != null && state != State.Windup && state != State.Strike && state != State.Recover && state != State.Stunned)
            return draftWalk[FlapFrame()]; // flyer: flap loop except while shooting (charge / fire / recover)
        if (state == State.Approach && draftWalk != null)
            return draftWalk[Mathf.Abs(rb.linearVelocity.x) > 0.1f ? Mathf.FloorToInt(Time.time * DraftSprites.WalkFps) % draftWalk.Length : 0];
        // 5-frame attack strips: keys 0 windup / 2 strike / 4 recover, 1 = last 0.1 s of the windup, 3 = first half of the recover.
        if (draftFrames.Length < 5) return draftFrames[state == State.Windup || state == State.Special || state == State.RushWindup ? 0 : state == State.Strike ? 1 : 2];
        float el = Time.time - stateEnteredAt;
        if (sniperArt)
        {
            // Sniper: 1 raise, 2 aim (tracking), 3 charge (locked), 4 fire / recoil, then back to 1; any other state stands (0, no muzzle flash).
            if (state == State.Windup) return draftFrames[sniperLocked ? 3 : el < 0.12f ? 1 : 2];
            if (state == State.Strike) return draftFrames[4];
            if (state == State.Recover) return draftFrames[el < recoverTime * TimeMul * 0.5f ? 4 : 1];
            return draftFrames[0];
        }
        if (state == State.Windup) return draftFrames[WindupWait() - el <= 0.1f ? 1 : 0];
        if (state == State.Special || state == State.RushWindup) return draftFrames[0];
        if (state == State.Strike) return draftFrames[2];
        return draftFrames[state == State.Recover && el < recoverTime * TimeMul * 0.5f ? 3 : 4];
    }

    int FlapFrame() => Mathf.FloorToInt(Time.time * DraftSprites.WalkFps) % draftWalk.Length;

    // Flyer hover: lifted FlyerLift so the art's gold mouth sits at the muzzle height (muzzleOffset y 0.3; the shot height itself must
    // not change: STG-02 shots pass under a roof), plus 1 px up on every other flap frame. Knocked out: back on the ground (-1 = Setup feet offset).
    const float FlyerLift = 0.3125f; // 10 px
    void BobFlyer()
    {
        Transform v = visual.transform;
        Vector3 p = v.localPosition;
        p.y = state == State.Stunned ? -1f : -1f + FlyerLift + (draftWalk != null && FlapFrame() == 0 ? 1f / 32f : 0f);
        v.localPosition = p;
    }

    void ApplyVisual()
    {
        if (visual == null) return;
        visual.flipX = facing < 0;
        if (draftSprite) visual.sprite = DraftFrame();
        if (flyer) BobFlyer();
        Color c = baseColor;
        switch (state)
        {
            case State.Windup:
            case State.RushWindup: c = WindupColor; break;
            case State.Staggered: c = Mathf.PingPong(Time.time * 10f, 1f) > 0.5f ? StaggerColor : baseColor; break;
            case State.Stunned: c = StunnedColor; break;
            case State.Frozen: c = FrozenColor; break;
            case State.Special: c = SpecialColor; break;
            case State.Overload: c = Mathf.FloorToInt(Time.time * 16f) % 2 == 0 ? OverloadColor : baseColor; break;
        }
        // Overload: small flickery glitch (shader only), on for 2 of every 3 ticks of OverloadGlitchHz.
        float glitch = state == State.Overload && Mathf.FloorToInt(Time.time * Juice.OverloadGlitchHz) % 3 != 0 ? Juice.OverloadGlitch : 0f;
        SpriteFlash.Apply(visual, c, Time.time < flashUntil, Color.white, flashColor, glitch); // flash shader: solid white; else the tint
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 p = transform.position;
        Gizmos.DrawWireCube(p + new Vector3(attackOffset.x * facing, attackOffset.y, 0f), attackSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(p, new Vector3(attackRangeX * 2f, attackRangeY * 2f, 0f));
    }

    // Upgrade 8-A shockwave: interrupt the chain and enter the existing Staggered state for `seconds`.
    // strikesLeft = 0 opens the punish window, same as the dash-through stagger.
    public void ForceStagger(float seconds)
    {
        if (state == State.Stunned || state == State.Frozen || state == State.Overload) return; // overload window stays
        // Never shorten a counter window that is already open (DECISIONS, QA #11): keep the longer remaining time.
        if ((state == State.Open || state == State.Staggered) && openDuration - (Time.time - stateEnteredAt) >= seconds) return;
        strikesLeft = 0;
        number.SetPulse(false);
        EnterState(State.Staggered);
        stateEnteredAt = Time.time - (openDuration - seconds); // Staggered ends at openDuration; shift so it lasts `seconds`
    }

    // Dice skill 3: androids overload for `seconds` (counter window); citizens / bugs / snipers freeze for `seconds`.
    public void Overload(float seconds)
    {
        if (state == State.Stunned) return;
        if (kind == Kind.Android)
        {
            if (state == State.Overload && Juice.OverloadSeconds - (Time.time - stateEnteredAt) >= seconds) return; // keep the longer window
            if (state != State.Overload) EnterState(State.Overload);
            stateEnteredAt = Time.time - (Juice.OverloadSeconds - seconds); // Overload ends at OverloadSeconds; shift so it lasts `seconds`
            return;
        }
        EnterState(State.Frozen);
        frozenUntil = Mathf.Max(frozenUntil, Time.time + seconds);
    }

    // ---- Elite clash duel (EliteDuel) ----
    // One frame of the attack strip (0 windup, 1 late windup, 2 strike, 3 recover) with the parry flash; called every duel frame.
    public void DuelPose(int frame, bool flash)
    {
        if (visual == null) return;
        visual.flipX = facing < 0;
        if (draftSprite) visual.sprite = draftFrames[Mathf.Clamp(frame, 0, draftFrames.Length - 1)];
        SpriteFlash.Apply(visual, baseColor, flash, Color.white, Juice.ParryEnemyTint);
    }

    // Lost duel: back to the normal AI with the same number (no reroll), a new duel only after the cooldown.
    public void EndDuel()
    {
        InDuel = false;
        nextDuelAt = Time.time + EliteDuelCooldown;
        EnterState(State.Approach);
    }

    // Won duel: a kill through the normal down state (StageClear / pointer count it), the body is already shattered (ShatterFx).
    public void DuelFinish()
    {
        InDuel = false;
        hp = 0;
        EnterState(State.Stunned);
        if (held) heldVelocity = Vector2.zero; // don't slide the hidden body on release
        if (ring != null) ring.Hide();
        if (visual != null) visual.enabled = false;
    }

    // Dice skill 5: show a countdown value in another digit sheet over the enemy's own number.
    public void SetNumberOverride(int value, Sprite[] digits) => number.SetOverride(value, digits);
    public void ClearNumberOverride() => number.ClearOverride();
}
