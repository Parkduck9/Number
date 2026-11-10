using UnityEngine;

// 2-4 dust pixels kicked out sideways on every touch-down of the player (the land-dust sheet only plays after a real fall,
// MomentFx.RealFallHeight; this covers the small hops too). Polls PlayerController.Grounded, read only. Added by AmbientAtmosphere.
// Scaled time: the pixels freeze in hit-stop and pause.
public class LandingDust : MonoBehaviour
{
    const float Ppu = 32f;
    const int Pool = 8;
    const float Life = 0.3f;                                  // (T) s
    const float Gravity = 9f;                                 // (T) units/s^2
    static readonly Vector2 Speed = new Vector2(2.2f, 1.6f);  // (T) max sideways / up speed (units/s)
    static readonly Color DustCol = new Color32(0xB8, 0xAE, 0xA0, 0xFF);

    PlayerController player;
    Collider2D body;
    Rigidbody2D rb;
    bool wasGrounded = true; // no puff on the first frame (scene load / respawn)
    float wasVy;
    static Material mat;

    struct Bit { public Transform t; public SpriteRenderer sr; public Vector2 pos, vel; public float life; }
    readonly Bit[] bits = new Bit[Pool];
    int next;

    void Start()
    {
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        for (int i = 0; i < Pool; i++)
        {
            var go = new GameObject("land-dust-px");
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(1f / Ppu, 1f / Ppu, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ParryFx.Square;
            sr.sharedMaterial = mat;
            sr.sortingLayerName = "FX";
            sr.sortingOrder = 102;
            sr.enabled = false;
            bits[i] = new Bit { t = go.transform, sr = sr };
        }
    }

    void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            body = player.GetComponent<Collider2D>();
            rb = player.GetComponent<Rigidbody2D>();
            wasGrounded = true;
        }
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        bool grounded = player.Grounded && player.CurrentState != PlayerController.State.Dead;
        if (grounded && !wasGrounded && wasVy < -1f) Puff(Mathf.Clamp01(-wasVy / 18f));
        wasGrounded = grounded;
        wasVy = rb != null ? rb.linearVelocity.y : 0f;

        for (int i = 0; i < Pool; i++)
        {
            ref Bit b = ref bits[i];
            if (b.life <= 0f) continue;
            b.life -= dt;
            if (b.life <= 0f) { b.sr.enabled = false; continue; }
            b.vel.y -= Gravity * dt;
            b.vel.x *= 1f - 4f * dt; // drag
            b.pos += b.vel * dt;
            // half-pixel offset: a 1 px square centered on a grid point would straddle two screen pixels
            b.t.position = new Vector3(Mathf.Round(b.pos.x * Ppu) / Ppu + 0.5f / Ppu, Mathf.Round(b.pos.y * Ppu) / Ppu + 0.5f / Ppu, 0f);
            Color c = DustCol; c.a = b.life > Life * 0.5f ? 0.8f : 0.4f; // two-step fade
            b.sr.color = c;
        }
    }

    // strength 0..1 (fall speed): 2 pixels for a hop, up to 4 for a hard landing; half go left, half right.
    void Puff(float strength)
    {
        Vector2 feet = body != null ? new Vector2(body.bounds.center.x, body.bounds.min.y) : (Vector2)player.transform.position;
        int n = 2 + Mathf.RoundToInt(strength * 2f);
        for (int k = 0; k < n; k++)
        {
            ref Bit b = ref bits[next];
            next = (next + 1) % Pool;
            float side = k % 2 == 0 ? -1f : 1f;
            b.pos = feet + new Vector2(side * Random.Range(0.1f, 0.3f), 1f / Ppu);
            b.vel = new Vector2(side * Speed.x * Random.Range(0.5f, 1f), Speed.y * Random.Range(0.4f, 1f) * (0.5f + 0.5f * strength));
            b.life = Life * Random.Range(0.8f, 1.1f);
            b.sr.enabled = true;
        }
    }
}
