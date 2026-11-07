using UnityEngine;

// Test enemy (brainwashed citizen). See docs/design/PROTOTYPE_ARCH.md sections 3-4.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyNumber))]
public class TestEnemy : MonoBehaviour
{
    public enum State { Approach, Windup, Strike, Recover, Turn, Staggered, Open, Stunned, Charge }

    [SerializeField] LayerMask playerMask;
    [SerializeField] SpriteRenderer visual; // optional; found in children if empty

    [Header("Tuning (T)")]
    [SerializeField] int maxHp = 3;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float attackRangeX = 1.8f;
    [SerializeField] float attackRangeY = 1.5f;
    [SerializeField] float windupTime = 0.5f;
    [SerializeField] float recoverTime = 0.35f;
    [SerializeField] float turnTime = 0.35f;
    [SerializeField] float staggerTime = 1f;
    [SerializeField] float openTime = 1f;
    [SerializeField] Vector2 attackOffset = new Vector2(1.0f, 0f);
    [SerializeField] Vector2 attackSize = new Vector2(1.8f, 1.4f);
    [SerializeField] float flashTime = 0.1f;
    [SerializeField] float chargeSpeed = 14f;
    [SerializeField] float chargeMaxTime = 0.6f;

    static readonly Color WindupColor = new Color(1f, 0.8f, 0.5f);
    static readonly Color StaggerColor = new Color(0.7f, 0.7f, 0.75f);
    static readonly Color StunnedColor = new Color(0.4f, 0.4f, 0.4f);

    public State Current => state;
    public int Hp => hp;
    public EnemyNumber Number => number;

    Rigidbody2D rb;
    EnemyNumber number;
    PlayerController player;
    State state;
    float stateEnteredAt;
    int hp;
    int facing = -1;
    Color baseColor = Color.white;
    Color flashColor;
    float flashUntil;

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
    }

    void Update()
    {
        if (player != null && state != State.Stunned) UpdateState();
        ApplyVisual();
    }

    void FixedUpdate()
    {
        float vx = 0f;
        // Recover also closes the gap so the chain keeps landing after the player's hurt knockback.
        // Only toward a player in front: Recover does not re-face, so never walk away from one who got behind.
        if ((state == State.Approach || state == State.Recover) && player != null &&
            (player.transform.position.x - transform.position.x) * facing > attackRangeX)
            vx = facing * moveSpeed; // stop under a jumping player instead of jittering
        if (state == State.Charge) vx = facing * chargeSpeed;
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    void UpdateState()
    {
        float t = Time.time - stateEnteredAt;
        switch (state)
        {
            case State.Approach:
                FacePlayer();
                Vector2 d = player.transform.position - transform.position;
                if (Mathf.Abs(d.x) <= attackRangeX && Mathf.Abs(d.y) <= attackRangeY)
                    EnterState(State.Windup);
                break;
            case State.Windup:
                if (t >= windupTime) EnterState(State.Strike);
                break;
            case State.Strike:
                DoStrike();
                break;
            case State.Recover:
                if (t >= recoverTime) NextAfterChain();
                break;
            case State.Charge:
                Vector2 cd = player.transform.position - transform.position;
                if (Mathf.Abs(cd.x) <= attackRangeX && Mathf.Abs(cd.y) <= attackRangeY) EnterState(State.Strike);
                else if ((cd.x * facing < 0f)) EnterState(State.Turn); // player slipped behind mid-charge
                else if (t >= chargeMaxTime) EnterState(State.Recover);
                break;
            case State.Turn:
                if (t >= turnTime) { FacePlayer(); NextAfterChain(); }
                break;
            case State.Staggered:
                if (t >= staggerTime) FinishReset();
                break;
            case State.Open:
                if (t >= openTime) FinishReset();
                break;
        }
    }

    void NextAfterChain()
    {
        EnterState(number.Value > 0 ? State.Windup : State.Open);
    }

    void FinishReset()
    {
        number.Reroll();
        EnterState(State.Approach);
    }

    void DoStrike()
    {
        bool special = number.Value == 1;
        HitResult result = HitResult.Dodged; // a whiff counts as dodged for the number...
        bool dashedThrough = false;

        Vector2 center = (Vector2)transform.position + new Vector2(attackOffset.x * facing, attackOffset.y);
        Collider2D col = Physics2D.OverlapBox(center, attackSize, 0f, playerMask);
        if (col != null)
        {
            PlayerController pc = col.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                // Dodged also covers post-hit invulnerability/Dead; only an actual dash counts for the stagger.
                dashedThrough = pc.CurrentState == PlayerController.State.Dash;
                result = pc.ReceiveEnemyHit(new HitInfo
                {
                    Damage = 1,
                    SourcePosition = transform.position,
                    IsSpecial = special
                });
            }
        }
        number.Decrement();
        number.SetPulse(false, false);

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
        if (!playerBehind && col == null && dx > attackRangeX && number.Value > 0)
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
            case State.Windup:
                number.SetPulse(true, number.Value == 1);
                break;
            case State.Charge:
                number.SetPulse(true, number.Value == 1); // charge strike at 1 is the special strike too
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
        if (number.Value > 0 && state != State.Turn)
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
}
