using System.Collections.Generic;
using UnityEngine;

// STG-05 falling roof chunks (DECISIONS 40). Built at runtime by BossFight (no scene objects).
// While the fight is on, a chunk breaks off the top of the room every SpawnEvery s (MaxAlive at once, one per lane), shakes for
// DetachTime, then sinks at FallSpeed. Ground layer + one-way effector: stand on it from above, jump / pass through from below,
// S + Space drops through, the RMB rope hooks it (its raycast uses the Ground mask). No damage. It crumbles (dust) once its bottom
// reaches CrumbleAt, above a standing player's head, so it never squeezes anyone on the bridge. Paused with the boss (input lock,
// dice hold); all crumble when the boss dies.
public class BossDebris : MonoBehaviour
{
    // ---- numbers (T) ----
    const int MaxAlive = 3;
    const float SpawnEvery = 3.4f, FirstAfter = 1.5f, DetachTime = 0.7f, FallSpeed = 1.2f;
    const float CrumbleAt = 4.3f;   // bottom y; standing head top = 4.0 (floor 2 + body 2)
    const float RoomTop = 16.875f, Px = 1f / 32f;
    // lane centres: clear of the ceiling hooks (8.5 / 21.5) and the hatch / core column (x 15) for a 5.4 u wide chunk
    static readonly float[] Lanes = { 4f, 11.8f, 18.2f, 26f };
    const float FallbackW = 5.25f, FallbackH = 0.5f; // code-drawn slab when no art is set

    sealed class Chunk
    {
        public GameObject Go;
        public Rigidbody2D Rb;
        public int Stage;   // 0 off, 1 breaking off, 2 falling
        public int Lane;
        public float T;
    }

    readonly Chunk[] chunks = new Chunk[MaxAlive];
    readonly List<Material> mats = new List<Material>();
    BossFight fight;
    Rigidbody2D playerRb;
    float w, h, spawnT = FirstAfter;
    int lastLane = -1;

    static bool Paused => GameFlow.InputLocked || BossFight.Held;

    public void Setup(BossFight owner, Sprite art)
    {
        fight = owner;
        w = art != null ? art.bounds.size.x : FallbackW;
        h = art != null ? art.bounds.size.y : FallbackH;
        int ground = LayerMask.NameToLayer("Ground");
        Shader unlit = Shader.Find("Sprites/Default");
        for (int i = 0; i < chunks.Length; i++)
        {
            var go = new GameObject("RoofDebris") { layer = ground };
            go.transform.SetParent(transform, false);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<BoxCollider2D>();
            col.usedByEffector = true;
            col.size = new Vector2(w - 0.2f, 0.4f);
            col.offset = new Vector2(0f, h - 0.2f); // top of the collider = top of the art
            var fx = go.AddComponent<PlatformEffector2D>();
            fx.useOneWay = true;
            fx.surfaceArc = 160f; // side bumps pass through too

            var vis = new GameObject("Art");
            vis.transform.SetParent(go.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            if (art != null)
            {
                sr.sprite = art;
                vis.transform.localPosition = new Vector3(-art.bounds.center.x, -art.bounds.min.y, 0f); // bottom-centre on the body
            }
            else
            {
                sr.sprite = ParryFx.Square; // TODO art: placeholder slab (dark steel, light top edge)
                sr.color = new Color32(0x3A, 0x3F, 0x4B, 0xFF);
                vis.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
                vis.transform.localScale = new Vector3(w, h, 1f);
                var edge = new GameObject("Edge").AddComponent<SpriteRenderer>();
                edge.transform.SetParent(go.transform, false);
                edge.transform.localPosition = new Vector3(0f, h - Px, 0f);
                edge.transform.localScale = new Vector3(w, 2f * Px, 1f);
                edge.sprite = ParryFx.Square;
                edge.color = new Color32(0x8A, 0x93, 0xA6, 0xFF);
                Unlit(edge, unlit, 1);
            }
            Unlit(sr, unlit, 0);
            go.SetActive(false);
            chunks[i] = new Chunk { Go = go, Rb = rb };
        }
    }

    void Unlit(SpriteRenderer sr, Shader shader, int order)
    {
        var m = new Material(shader);
        mats.Add(m);
        sr.sharedMaterial = m;
        sr.sortingLayerName = "World";
        sr.sortingOrder = order;
    }

    void Start()
    {
        PlayerController p = FindFirstObjectByType<PlayerController>();
        if (p != null) playerRb = p.GetComponent<Rigidbody2D>();
    }

    void OnDestroy()
    {
        foreach (Material m in mats) Destroy(m);
    }

    void Update()
    {
        if (fight == null || !fight.Started || fight.Dead || Paused) return;
        spawnT -= Time.deltaTime;
        if (spawnT > 0f) return;
        spawnT = SpawnEvery;
        Spawn();
    }

    void Spawn()
    {
        Chunk c = null;
        foreach (Chunk k in chunks) if (k.Stage == 0) { c = k; break; }
        if (c == null) return;
        int start = Random.Range(0, Lanes.Length), lane = -1;
        for (int i = 0; i < Lanes.Length && lane < 0; i++)
        {
            int l = (start + i) % Lanes.Length;
            if (l != lastLane && !LaneUsed(l)) lane = l;
        }
        if (lane < 0) return;
        lastLane = lane;
        c.Lane = lane;
        c.Stage = 1;
        c.T = 0f;
        Vector2 at = new Vector2(Lanes[lane], RoomTop - h + 0.1f); // hangs just under the top edge while it breaks off
        c.Go.transform.position = at;
        c.Go.SetActive(true);
        c.Rb.position = at;
        SpriteFx.Play("land-dust", at + new Vector2(-w * 0.3f, h), true, 101);
        SpriteFx.Play("land-dust", at + new Vector2(w * 0.3f, h), false, 101);
        Sfx.Play(SfxId.Hit, 0.2f, 0.45f); // low creak
    }

    bool LaneUsed(int lane)
    {
        foreach (Chunk k in chunks) if (k.Stage != 0 && k.Lane == lane) return true;
        return false;
    }

    void FixedUpdate()
    {
        if (Paused) return; // kinematic: no MovePosition = it stays put
        float dt = Time.fixedDeltaTime;
        foreach (Chunk c in chunks)
        {
            if (c.Stage == 0) continue;
            c.T += dt;
            float x = Lanes[c.Lane];
            if (c.Stage == 1) // breaking off: 1 px shiver
            {
                c.Rb.MovePosition(new Vector2(x + (Mathf.FloorToInt(c.T * 20f) % 2 == 0 ? Px : -Px), c.Rb.position.y));
                if (c.T >= DetachTime) { c.Stage = 2; c.Rb.MovePosition(new Vector2(x, c.Rb.position.y)); }
                continue;
            }
            Vector2 p = new Vector2(x, c.Rb.position.y - FallSpeed * dt);
            if (p.y <= CrumbleAt) { Crumble(c); continue; }
            c.Rb.MovePosition(p);
            Carry(c);
        }
    }

    // A player standing on the chunk sinks with it at the same speed (no fall-and-land jitter every step). Only with gravity on
    // (rope / dash / issen own the velocity) and not while rising (a jump off it).
    void Carry(Chunk c)
    {
        if (playerRb == null || playerRb.gravityScale <= 0f) return;
        Vector2 pp = playerRb.position, at = c.Rb.position; // player centre = feet + 1
        if (playerRb.linearVelocity.y > 0.05f || Mathf.Abs(pp.y - 1f - (at.y + h)) > 0.12f || Mathf.Abs(pp.x - at.x) > w * 0.5f + 0.35f) return;
        playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, -FallSpeed);
    }

    void Crumble(Chunk c)
    {
        Vector2 at = c.Rb.position;
        c.Stage = 0;
        c.Go.SetActive(false);
        SpriteFx.Play("land-dust", at + new Vector2(-w * 0.3f, h * 0.5f), true, 101);
        SpriteFx.Play("land-dust", at + new Vector2(0f, h * 0.5f), false, 101);
        SpriteFx.Play("land-dust", at + new Vector2(w * 0.3f, h * 0.5f), false, 101);
        Sfx.Play(SfxId.Hit, 0.3f, 0.6f);
    }

    // The boss died: every chunk crumbles where it is (the ending room stays still)
    public void CrumbleAll()
    {
        foreach (Chunk c in chunks) if (c.Stage != 0) Crumble(c);
    }
}
