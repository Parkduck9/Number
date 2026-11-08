using UnityEngine;

// Test enemy (brainwashed citizen). See docs/design/PROTOTYPE_ARCH.md sections 3-4.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyNumber))]
public class TestEnemy : MonoBehaviour
{
    public enum State { Approach, Windup, Strike, Recover, Turn, Staggered, Open, Stunned, Charge, Frozen, Special }

    [SerializeField] LayerMask playerMask;
    [SerializeField] SpriteRenderer visual; // optional; found in children if empty

    [Header("Tuning (T)")]
    [SerializeField] int maxHp = 3;
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

    public enum Kind { Citizen, Bug, Android }

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
    static readonly Color SpecialColor = new Color(1f, 0.788f, 0.235f); // gold tell of the fan special

    public State Current => state;
    public int Hp => hp;
    public EnemyNumber Number => number;
    public int StrikesLeft => strikesLeft;

    Rigidbody2D rb;
    EnemyNumber number;
    PlayerController player;
    State state;
    float stateEnteredAt;
    int hp;
    int strikesLeft;      // the displayed number never changes; this counts the chain down
    bool chainStarted;    // first strike of a chain gets the long windup
    int facing = -1;
    Color baseColor = Color.white;
    Color flashColor;
    float flashUntil;
    float backoffSince = -1f; // ranged: when the player got too close (-1 = not too close)
    BroadcastBoard zoneBoard;
    int broadcast = -1;       // number of the zone's broadcast board (-1 = no board)
    int exhaustedCycle = -1;  // NUM-7: index of the dope/exhaust cycle (from the board's change time) already punished
    float nextSpecialAt;      // Android fan cooldown
    float openDuration;       // length of the current Open state

    // NUM-7 clock is the board's (shared by every enemy of the zone): dope for Num7DopeSeconds, then exhaustion, repeating.
    float Num7Phase => zoneBoard != null ? Time.time - zoneBoard.NumberChangedAt : 0f;
    static float Num7Cycle => BroadcastRules.Num7DopeSeconds + BroadcastRules.Num7ExhaustSeconds;
    bool Doped => broadcast == 7 && zoneBoard != null && Num7Phase % Num7Cycle < BroadcastRules.Num7DopeSeconds;
    float Speed => moveSpeed * (Doped ? BroadcastRules.Num7SpeedMul : 1f);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        number = GetComponent<EnemyNumber>();
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null) baseColor = visual.color;
        rb.gravityScale = 4f;
        rb.freezeRotation = true;
        hp = maxHp;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        EnterState(State.Approach);
        zoneBoard = BroadcastBoard.At(transform.position); // zone is fixed: find it once, then react to its changes
        if (zoneBoard != null)
        {
            zoneBoard.NumberChanged += OnBroadcastChanged;
            OnBroadcastChanged(zoneBoard, zoneBoard.Number, -1);
        }
    }

    void OnDestroy()
    {
        if (zoneBoard != null) zoneBoard.NumberChanged -= OnBroadcastChanged;
    }

    void Update()
    {
        if (GameFlow.InputLocked) // blocking dialogue: freeze the AI and push its state timer forward
        {
            stateEnteredAt += Time.deltaTime;
            flashUntil += Time.deltaTime;
            if (backoffSince >= 0f) backoffSince += Time.deltaTime;
            nextSpecialAt += Time.deltaTime;
            return;
        }
        if (state != State.Stunned) UpdateDope();
        if (player != null && state != State.Stunned) UpdateState();
        ApplyVisual();
    }

    void FixedUpdate()
    {
        if (GameFlow.InputLocked) { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); return; }
        float vx = 0f;
        bool chasing = false; // spotted the player and heading for them: drops off roof edges instead of stopping
        // Recover also closes the gap so the chain keeps landing after the player's hurt knockback.
        // Only toward a player in front: Recover does not re-face, so never walk away from one who got behind.
        if ((state == State.Approach || state == State.Recover) && player != null &&
            (player.transform.position.x - transform.position.x) * facing > attackRangeX)
        {
            vx = facing * Speed; // stop under a jumping player instead of jittering
            chasing = state == State.Approach && IsActive();
            if (state == State.Approach && !chasing) vx = 0f; // idle until activated
        }
        else if (state == State.Approach && ranged && IsBackingOff())
            vx = -facing * Speed;
        if (state == State.Charge) { vx = facing * chargeSpeed; chasing = true; }
        if (vx != 0f && !chasing && EdgeAhead(vx > 0f ? 1 : -1)) vx = 0f;
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
        if (n == 0 && kind == Kind.Citizen) EnterState(State.Frozen);
        else if (old == 0 && state == State.Frozen) FinishReset();
    }

    // NUM-7 timer (time-based, not a board poll).
    void UpdateDope()
    {
        // NUM-7: dope ran out -> exhausted (hittable); a zone that stays on 7 cycles dope / exhaustion.
        if (broadcast != 7 || zoneBoard == null || state == State.Stunned || state == State.Frozen) return;
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
        return activationRange <= 0f || Mathf.Abs(player.transform.position.x - transform.position.x) <= activationRange; // height ignored
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
                if (!IsActive()) break; // idle until the player comes close
                FacePlayer();
                Vector2 d = player.transform.position - transform.position;
                if (Mathf.Abs(d.x) <= attackRangeX && Mathf.Abs(d.y) <= attackRangeY && !(ranged && IsBackingOff()))
                    EnterState(State.Windup);
                break;
            case State.Windup:
                float chainWait = broadcast == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3ChainWindupTime : chainWindupTime;
                float wait = chainStarted ? chainWait : windupTime;
                if (Doped) wait /= BroadcastRules.Num7SpeedMul;
                if (strikesLeft == 1) wait += lastStrikeDelay; // flame is already on for the last strike
                if (t >= wait) EnterState(State.Strike);
                break;
            case State.Strike:
                DoStrike();
                break;
            case State.Recover:
                if (t >= recoverTime) NextAfterChain();
                break;
            case State.Charge:
                Vector2 cd = player.transform.position - transform.position;
                if (Mathf.Abs(cd.x) <= attackRangeX && Mathf.Abs(cd.y) <= attackRangeY)
                    EnterState(strikesLeft == 1 && lastStrikeDelay > 0f ? State.Windup : State.Strike); // keep the late last strike
                else if ((cd.x * facing < 0f)) EnterState(State.Turn); // player slipped behind mid-charge
                else if (t >= chargeMaxTime) EnterState(State.Recover);
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
            EnemyProjectile.Spawn(muzzle, dir * BroadcastRules.AndroidFanSpeed, playerMask, groundMask, hit, projectileColor);
        }
    }

    void FinishReset()
    {
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
            Unparryable = unparryable
        };
        Collider2D col = null;
        if (ranged)
        {
            Vector2 muzzle = (Vector2)transform.position + new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
            EnemyProjectile.Spawn(muzzle, facing, projectileSpeed, playerMask, groundMask, hit, projectileColor);
        }
        else
        {
            Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
            ParryFx.Swing(center, attackSize, Color.red, visual != null ? visual.transform : null, facing);
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
        }
        strikesLeft--;
        chainStarted = true;
        number.SetPulse(false);
        number.SetFlame(strikesLeft == 1); // tell: next strike is the last one

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
        if (result == HitResult.Parried) Flash(Color.white);
        EnterState(playerBehind ? State.Turn : State.Recover);
    }

    void EnterState(State next)
    {
        state = next;
        stateEnteredAt = Time.time;
        switch (next)
        {
            case State.Approach:
                strikesLeft = number.Value; // new chain
                chainStarted = false;
                backoffSince = -1f; // each new chain may back off again
                break;
            case State.Windup:
                number.SetPulse(true);
                number.SetFlame(strikesLeft == 1); // single-strike chain: tell during its windup
                break;
            case State.Charge:
                number.SetPulse(true);
                break;
            case State.Open:
            case State.Staggered:
                number.SetFlame(false);
                openDuration = broadcast == BroadcastRules.Num3Number && !ranged ? BroadcastRules.Num3OpenTime : openTime;
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
            case State.Stunned:
                number.Hide();
                rb.linearVelocity = Vector2.zero;
                if (visual != null) visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f * -facing);
                break;
        }
    }

    void FacePlayer()
    {
        if (player == null) return;
        float dx = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.01f) facing = dx > 0f ? 1 : -1;
    }

    // true = damage applied
    public bool ReceivePlayerHit(HitInfo info)
    {
        if (state == State.Stunned) return false;
        if (strikesLeft > 0 && state != State.Turn && state != State.Frozen) // frozen: damage always applies
        {
            Flash(Color.white);
            return false;
        }
        hp -= info.Damage;
        Flash(Color.red);
        if (hp <= 0) EnterState(State.Stunned);
        return true;
    }

    void Flash(Color c)
    {
        flashColor = c;
        flashUntil = Time.time + flashTime;
    }

    void ApplyVisual()
    {
        if (visual == null) return;
        visual.flipX = facing < 0;
        Color c = baseColor;
        switch (state)
        {
            case State.Windup: c = WindupColor; break;
            case State.Staggered: c = Mathf.PingPong(Time.time * 10f, 1f) > 0.5f ? StaggerColor : baseColor; break;
            case State.Stunned: c = StunnedColor; break;
            case State.Frozen: c = FrozenColor; break;
            case State.Special: c = SpecialColor; break;
        }
        if (Time.time < flashUntil) c = flashColor;
        visual.color = c;
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
        if (state == State.Stunned || state == State.Frozen) return;
        strikesLeft = 0;
        number.SetPulse(false);
        EnterState(State.Staggered);
        stateEnteredAt = Time.time - (openDuration - seconds); // Staggered ends at openDuration; shift so it lasts `seconds`
    }
}
