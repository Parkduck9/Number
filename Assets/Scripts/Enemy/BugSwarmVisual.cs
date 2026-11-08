using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// AI bug look (GDD ART-09): a Kind.Bug TestEnemy hides its placeholder square and is drawn as ~55-70 single-pixel dots
// that gather into a bug silhouette, twitch, stretch toward the player on Windup, burst on Strike and dissolve on death.
// Appearance only: it reads TestEnemy.Current / the hidden SpriteRenderer (flipX = facing, color = flash) and changes nothing.
// One small runtime Texture2D per enemy (Point filter), redrawn from a reused Color32 buffer: no per-frame allocation.
// Attached at runtime by BugSwarmBootstrap below. Removal: delete this file. Doc: docs/design/impl/bug-swarm.md.
[DefaultExecutionOrder(500)] // LateUpdate after TestEnemy.ApplyVisual wrote this frame's flipX / colour
public class BugSwarmVisual : MonoBehaviour
{
    // ---- tunables (T) ----
    const int Size = 96;                  // canvas px (square) centred on the enemy root; 1 px = 1/32 u
    const float Ppu = 32f;
    const int MaxDots = 80;
    const float GatherRadius = 30f;       // px: dots start scattered this far and gather into the silhouette on spawn
    const float Follow = 18f;             // 1/s: how fast a dot homes in on its target
    const float TwitchMin = 0.05f, TwitchMax = 0.14f; // s between twitches of a body dot
    const float WingTwitchMul = 0.35f;    // wings buzz faster
    const float LegTwitchMul = 2.5f;      // legs twitch slower
    const float WindupRamp = 0.3f;        // s to reach the full stretch (first windup is ~0.33 s)
    const float WindupStretch = 0.5f;     // x scale added toward the player at full windup
    const float WindupLead = 6f;          // px forward shift at full windup
    const float ChargeStretch = 0.8f, ChargeLead = 9f;
    const float StrikeBurst = 14f;        // px scatter on a melee strike (ranged: recoil backward)
    const float StrikeDecay = 10f;        // 1/s: scatter settles back
    const float Sag = 4f;                 // px the cluster droops while Staggered / Open
    const float RangedHover = 2f;         // px hover bob of the ranged bug
    const float DeathFade = 0.5f;         // s: mirrors Juice.KillVanishDelay (the body is deactivated after it)
    const float DeathSpeed = 40f, DeathGravity = 90f; // px/s, px/s^2 for the dispersing dots

    // Palette: dark gray-purple body, red accent (melee) or gold accent (ranged). 5 colours per variant.
    static readonly Color32 Dark = new Color32(0x2E, 0x26, 0x38, 255);
    static readonly Color32 Mid = new Color32(0x4A, 0x3F, 0x5C, 255);
    static readonly Color32 Light = new Color32(0x8C, 0x7F, 0xA6, 255);
    static readonly Color32 Red = new Color32(0xFF, 0x2A, 0x2A, 255);
    static readonly Color32 Gold = new Color32(0xFF, 0xC9, 0x3C, 255);
    static readonly Color32 White = new Color32(255, 255, 255, 255);

    // Dot parts: colour + twitch behaviour. Charge = body colour normally, accent while winding up / charging.
    const byte PDark = 0, PMid = 1, PLight = 2, PAccent = 3, PLeg = 4, PWing = 5, PCharge = 6;

    struct Dot
    {
        public Vector2 home, pos, scatter, twitch;
        public byte part;
        public float nextTwitch;
    }

    TestEnemy enemy;
    SpriteRenderer visual;   // the hidden placeholder square: flipX = facing, color = flash
    SpriteRenderer sr;
    Transform swarmTf;
    Texture2D tex;
    Color32[] buf;
    readonly Dot[] dots = new Dot[MaxDots];
    int count;
    bool ranged;
    Color32 accent;
    TestEnemy.State lastState;
    float stateAt;
    int firstFrame;

    void Start()
    {
        enemy = GetComponent<TestEnemy>();
        if (enemy == null) { enabled = false; return; }
        ranged = BugSwarmBootstrap.IsRanged(enemy);
        accent = ranged ? Gold : Red;
        visual = GetComponentInChildren<SpriteRenderer>();

        tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = "BugSwarm", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        buf = new Color32[Size * Size];
        tex.SetPixels32(buf);
        tex.Apply(false, false);

        var go = new GameObject("Swarm") { layer = gameObject.layer };
        swarmTf = go.transform;
        swarmTf.SetParent(transform, false);
        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
        if (visual != null)
        {
            sr.sortingLayerID = visual.sortingLayerID;
            sr.sortingOrder = visual.sortingOrder;
            visual.enabled = false; // TestEnemy keeps writing flipX/colour to it; we read those
        }

        var rng = new System.Random(GetInstanceID());
        if (ranged) BuildRanged(rng); else BuildMelee(rng);
        lastState = enemy.Current;
        stateAt = Time.time;
        firstFrame = Time.frameCount;
    }

    void OnDestroy()
    {
        if (visual != null) visual.enabled = true;
        if (sr != null && sr.sprite != null) Destroy(sr.sprite);
        if (tex != null) Destroy(tex);
    }

    // ---- silhouettes (facing +x, px from the root centre, y up). Root centre = collider centre; feet at y = -32.

    // Attack bug: wide beetle crouching low, red eyes, three leg pairs, buzzing wings on its back. ~71 dots.
    void BuildMelee(System.Random r)
    {
        Ellipse(r, -9f, -9f, 8f, 5f, 20, PDark, PMid);     // abdomen
        Ellipse(r, 2f, -7f, 5f, 4f, 12, PMid, PDark);       // thorax
        Ellipse(r, 10f, -6f, 3.5f, 3f, 8, PDark, PMid);     // head
        Add(11f, -4f, PAccent); Add(12f, -7f, PAccent);     // eyes
        Add(14f, -8f, PAccent);                             // fang
        Line(r, 12f, -3f, 17f, 3f, 3, PLight);              // antennae
        Line(r, 12f, -4f, 19f, 0f, 3, PLight);
        for (int i = 0; i < 3; i++)                         // legs: rear trails, front leads
        {
            float sx = -4f + 5f * i;
            Line(r, sx, -11f, sx - 6f + 5.5f * i, -21f, 3, PLeg);
        }
        Ellipse(r, -4f, -1f, 9f, 3f, 10, PWing, PWing);     // wings
        Add(4f, -5f, PCharge); Add(0f, -8f, PCharge); Add(-3f, -6f, PCharge); // thorax glows red on windup
    }

    // Ranged bug: slim stinger/dart flying higher (muzzle is at +0.8, +0.3 u), gold tip, swept wings, tucked legs. ~55 dots.
    void BuildRanged(System.Random r)
    {
        Line(r, -14f, 6f, 11f, 6f, 14, PDark);              // shaft core
        Line(r, -13f, 7f, 6f, 7f, 8, PMid);                 // upper edge (tapers toward the tip)
        Line(r, -13f, 5f, 6f, 5f, 8, PMid);                 // lower edge
        Add(12f, 6f, PCharge);                              // sting: gold, longer while winding up
        Add(13f, 6f, PAccent); Add(14f, 6f, PAccent); Add(15f, 6f, PAccent);
        Add(16f, 6f, PCharge);
        Line(r, -14f, 6f, -19f, 10f, 3, PLight);            // tail fins
        Line(r, -14f, 6f, -19f, 2f, 3, PLight);
        Line(r, -2f, 8f, -12f, 14f, 5, PWing);              // swept wings
        Line(r, -1f, 8f, -13f, 11f, 5, PWing);
        Line(r, -5f, 4f, 3f, 3f, 4, PLeg);                  // tucked legs
    }

    void Add(float x, float y, byte part)
    {
        if (count >= MaxDots) return;
        Vector2 start = Random.insideUnitCircle * GatherRadius;
        dots[count++] = new Dot { home = new Vector2(x, y), pos = start, part = part, nextTwitch = Time.time + Random.Range(0f, TwitchMax) };
    }

    void Line(System.Random r, float x0, float y0, float x1, float y1, int n, byte part)
    {
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : (float)i / (n - 1);
            Add(Mathf.Lerp(x0, x1, t) + Jit(r), Mathf.Lerp(y0, y1, t) + Jit(r), part);
        }
    }

    // Uniform fill of an ellipse; roughly 70% partA, 30% partB.
    void Ellipse(System.Random r, float cx, float cy, float rx, float ry, int n, byte partA, byte partB)
    {
        for (int i = 0; i < n; i++)
        {
            float a = (float)r.NextDouble() * Mathf.PI * 2f;
            float d = Mathf.Sqrt((float)r.NextDouble());
            Add(cx + Mathf.Cos(a) * rx * d, cy + Mathf.Sin(a) * ry * d, r.NextDouble() < 0.7 ? partA : partB);
        }
    }

    static float Jit(System.Random r) => ((float)r.NextDouble() - 0.5f) * 0.8f;

    // ---- per frame

    void LateUpdate()
    {
        TestEnemy.State s = enemy.Current;
        if (s != lastState) { Enter(s); lastState = s; stateAt = Time.time; }
        if (visual != null) swarmTf.localPosition = visual.transform.localPosition; // inherit ParryFx's strike nudge
        if (!sr.isVisible && Time.frameCount > firstFrame + 1) return; // off-screen: keep state tracking, skip simulation + upload
        float t = Time.time - stateAt;
        Simulate(s, t, Time.deltaTime);
        Draw(s, t);
    }

    void Enter(TestEnemy.State s)
    {
        int facing = Facing();
        switch (s)
        {
            case TestEnemy.State.Strike: // melee: burst forward; ranged: recoil backward
                for (int i = 0; i < count; i++)
                {
                    Vector2 dir = ranged
                        ? new Vector2(-facing * Random.Range(0.5f, 1f), Random.Range(-0.3f, 0.3f))
                        : new Vector2(facing * Random.Range(0.2f, 1f), Random.Range(-0.6f, 0.6f));
                    dots[i].scatter += dir.normalized * Random.Range(0.5f, 1f) * StrikeBurst;
                }
                break;
            case TestEnemy.State.Stunned: // knocked out: the swarm loses cohesion, dots fly apart and fall
                for (int i = 0; i < count; i++)
                {
                    Vector2 v = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * DeathSpeed;
                    v.y += DeathSpeed * 0.5f;
                    dots[i].scatter = v; // reused as velocity while dead
                }
                break;
        }
    }

    int Facing() => visual != null && visual.flipX ? -1 : 1;

    void Simulate(TestEnemy.State s, float t, float dt)
    {
        if (s == TestEnemy.State.Stunned)
        {
            for (int i = 0; i < count; i++)
            {
                dots[i].scatter.y -= DeathGravity * dt;
                dots[i].pos += dots[i].scatter * dt;
            }
            return;
        }
        int facing = Facing();
        float stretch = 0f, lead = 0f, sag = 0f, jit = 1f;
        switch (s)
        {
            case TestEnemy.State.Windup:
            case TestEnemy.State.RushWindup:
                float k = Mathf.Clamp01(t / WindupRamp);
                stretch = WindupStretch * k; lead = WindupLead * k; jit = 1f - 0.5f * k; // tenses up
                break;
            case TestEnemy.State.Charge: stretch = ChargeStretch; lead = ChargeLead; break;
            case TestEnemy.State.Turn: jit = 2f; break;
            case TestEnemy.State.Staggered:
            case TestEnemy.State.Open: sag = Sag; jit = 1.8f; break;
        }
        // Hover: melee 1 px; the ranged dart floats in a slower, deeper 2 px bob so it reads as a hovering shooter. Legs stay put.
        float bob = ranged ? Mathf.Round(Mathf.Sin(Time.time * 3f) * RangedHover) : Mathf.Round(Mathf.Sin(Time.time * 5f));
        float follow = Mathf.Min(1f, Follow * dt);
        float decay = Mathf.Exp(-StrikeDecay * dt);
        float now = Time.time;
        for (int i = 0; i < count; i++)
        {
            ref Dot d = ref dots[i];
            Vector2 target = new Vector2((d.home.x * (1f + stretch) + lead) * facing, d.home.y - sag + (d.part == PLeg ? 0f : bob));
            d.pos += (target - d.pos) * follow;
            d.scatter *= decay;
            if (now < d.nextTwitch) continue;
            float wait = Random.Range(TwitchMin, TwitchMax) / jit;
            switch (d.part)
            {
                case PWing: d.twitch = new Vector2(0f, Random.value < 0.5f ? -2f : 2f); wait *= WingTwitchMul; break;
                case PLeg: d.twitch = new Vector2(Random.Range(-1f, 1f) * jit, 0f); wait *= LegTwitchMul; break;
                case PAccent: d.twitch = Vector2.zero; break; // eyes / sting hold still: the focal point
                default: d.twitch = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * jit; break;
            }
            d.nextTwitch = now + wait;
        }
    }

    void Draw(TestEnemy.State s, float t)
    {
        System.Array.Clear(buf, 0, buf.Length);
        bool dead = s == TestEnemy.State.Stunned;
        bool charge = s == TestEnemy.State.Windup || s == TestEnemy.State.RushWindup || s == TestEnemy.State.Charge;
        bool accentOff = (s == TestEnemy.State.Staggered || s == TestEnemy.State.Open) && Mathf.PingPong(Time.time * 8f, 1f) > 0.5f; // eyes blink while open
        bool flashWhite = visual != null && visual.color == Color.white; // parry flash
        bool flashRed = visual != null && visual.color == Color.red;     // kill flash
        int alive = dead ? Mathf.CeilToInt(count * (1f - Mathf.Clamp01(t / DeathFade))) : count;
        const int Half = Size / 2;
        for (int i = 0; i < count; i++)
        {
            if (dead && (i * 37) % count >= alive) continue; // dissolve spread over the body, not front to back
            ref Dot d = ref dots[i];
            Vector2 p = d.pos + d.twitch + d.scatter;
            int px = Half + Mathf.RoundToInt(p.x), py = Half + Mathf.RoundToInt(p.y);
            if ((uint)px >= Size || (uint)py >= Size) continue;
            buf[py * Size + px] = flashWhite ? White : flashRed ? accent : ColorOf(d.part, charge, accentOff);
        }
        tex.SetPixels32(buf);
        tex.Apply(false, false);
    }

    Color32 ColorOf(byte part, bool charge, bool accentOff)
    {
        switch (part)
        {
            case PMid: return Mid;
            case PLight: case PWing: return Light;
            case PAccent: return accentOff ? Mid : accent;
            case PCharge: return charge ? accent : Mid;
            default: return Dark;
        }
    }
}

// Adds BugSwarmVisual to every Kind.Bug TestEnemy in each loaded scene, inactive ones included (the boss scene's
// Template-Bug is inactive and cloned per wave, so the clones inherit the component).
public static class BugSwarmBootstrap
{
    static FieldInfo kindField, rangedField;
    static bool resolved;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachAll();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachAll();

    public static void AttachAll()
    {
        foreach (TestEnemy e in Object.FindObjectsByType<TestEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)) TryAttach(e);
    }

    // For enemies created at runtime outside the template path.
    public static void TryAttach(TestEnemy e)
    {
        if (e.GetComponent<BugSwarmVisual>() != null || !IsBug(e)) return; // ranged bugs get the swarm too; ranged citizens are Kind.Citizen (DECISIONS 14)
        e.gameObject.AddComponent<BugSwarmVisual>();
    }

    // TestEnemy.kind / ranged are private serialized fields: read once via reflection, cached.
    static void Resolve()
    {
        if (resolved) return;
        resolved = true;
        const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        kindField = typeof(TestEnemy).GetField("kind", F);
        rangedField = typeof(TestEnemy).GetField("ranged", F);
        if (kindField == null || rangedField == null)
            Debug.LogWarning("BugSwarm: TestEnemy.kind / ranged not found (renamed?). Add `public Kind EnemyKind => kind; public bool Ranged => ranged;` and read them in BugSwarmBootstrap.");
    }

    public static bool IsBug(TestEnemy e)
    {
        Resolve();
        return kindField != null && (TestEnemy.Kind)kindField.GetValue(e) == TestEnemy.Kind.Bug;
    }

    public static bool IsRanged(TestEnemy e)
    {
        Resolve();
        return rangedField != null && (bool)rangedField.GetValue(e);
    }
}
