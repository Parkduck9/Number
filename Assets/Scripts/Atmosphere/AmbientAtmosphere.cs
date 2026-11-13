using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Air for the stage scenes: drifting dust motes, talisman-yellow embers, falling paper-ash, diagonal rain and dithered mist, plus
// pixel-stepped dark strips at the top / bottom screen edges. Self-installing (no scene edits): one per stage scene (StageFlow.Stages).
// Particles live in a box around the camera and wrap, so the field is endless. depth: 0 = glued to the screen, 1 = fixed in the world,
// > 1 = foreground (slides past faster than the world). All positions snap to the 1/32 grid; sprites are point-filtered squares.
// Scaled time on purpose: the air freezes in hit-stop and pause, like SpriteFx. Unlit (Sprites/Default) so the 2D light setup can't black it out.
[DefaultExecutionOrder(100)] // after CameraFollow2D's LateUpdate
public class AmbientAtmosphere : MonoBehaviour
{
    const float Ppu = 32f;
    const float Margin = 1.5f; // units around the view where particles still live (no pop at the edges while the camera moves)

    // (T) Edge strips: steps from the screen edge inward, each StripStepPx reference pixels tall.
    const int StripStepPx = 4;
    static readonly float[] BottomStrip = { 0.42f, 0.26f, 0.12f };
    static readonly float[] TopStrip = { 0.30f, 0.14f };
    static readonly Color StripColor = new Color32(0x06, 0x05, 0x10, 0xFF); // near-black night navy

    static readonly Color Talisman = new Color32(0xF2, 0xC9, 0x4C, 0xFF); // #F2C94C
    static readonly Color TalismanHot = new Color32(0xFF, 0xE9, 0xA8, 0xFF);
    static readonly Color DustCol = new Color32(0x9A, 0xA6, 0xBE, 0xFF);
    static readonly Color AshCol = new Color32(0x4E, 0x49, 0x58, 0xFF);
    static readonly Color RainCol = new Color32(0x8F, 0xA8, 0xC8, 0xFF);
    static readonly Color MistCol = new Color32(0xB8, 0xC4, 0xD8, 0xFF);

    // Per-stage mix (proposal, see report). Multipliers on the base counts below; rain / mist on or off.
    struct Profile { public float dust, embers, ash, rain, mist; public float wind; }
    static Profile For(string scene)
    {
        switch (scene)
        {
            case "STG00": return new Profile { dust = 1f, embers = 0.4f, ash = 0f, rain = 0f, mist = 0f, wind = 0.1f };  // morning market: calm
            case "STG01": return new Profile { dust = 0.6f, embers = 0.6f, ash = 0f, rain = 1f, mist = 0f, wind = -0.2f }; // neon market: rain
            case "STG02": return new Profile { dust = 0.8f, embers = 1f, ash = 1f, rain = 0f, mist = 0f, wind = -0.6f };  // rooftop rail: wind (mist off, playtest 11-14: the dithered puffs read as floating grey discs)
            case "STG03": return new Profile { dust = 1.5f, embers = 0.5f, ash = 0f, rain = 0f, mist = 0f, wind = 0f };   // hideout: indoor dust
            case "STG04": return new Profile { dust = 0.8f, embers = 1.6f, ash = 1f, rain = 0f, mist = 0f, wind = 0.2f }; // factory: sparks (mist off, same as STG02)
            default:      return new Profile { dust = 0.6f, embers = 0.5f, ash = 0f, rain = 0.7f, mist = 0f, wind = -0.3f }; // STG05 HQ roof
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || !System.Linq.Enumerable.Contains(StageFlow.Stages, scene.name)) return;
        if (FindFirstObjectByType<AmbientAtmosphere>() != null) return;
        var go = new GameObject("Ambient Atmosphere (runtime)");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<AmbientAtmosphere>().profile = For(scene.name);
        go.AddComponent<LandingDust>();
    }

    class Layer
    {
        public int count;
        public float depth;
        public string sortLayer; public int order;
        public Sprite sprite; public Vector2 sizePx = Vector2.one;
        public Color colA, colB; public float alphaMin, alphaMax;
        public Vector2 velMin, velMax;
        public float sway, swayHz;  // sideways wobble (units, Hz)
        public float lifeMin, lifeMax; // 0 = endless (wraps); > 0 = fades in / out and respawns somewhere else
        public float flicker;       // 0..1 alpha flicker depth
        public bool lowerHalf;      // spawn only in the lower half of the view (mist)
        public Vector2 snapOff;     // half a pixel on odd-sized axes, so a centered sprite covers whole screen pixels
        public P[] ps;
    }

    struct P
    {
        public Vector2 local, vel;
        public float life, maxLife, phase, alpha;
        public Color col;
        public Transform t;
        public SpriteRenderer sr;
    }

    Profile profile;
    readonly List<Layer> layers = new List<Layer>();
    readonly List<SpriteRenderer> strips = new List<SpriteRenderer>();
    Camera cam;
    PixelPerfectCamera ppc;
    Vector2 lastCam;
    Vector2 half; // half view size (units)
    static Material mat;
    static Sprite rainSprite, mistSprite;

    void Start()
    {
        cam = Camera.main;
        if (cam == null) { enabled = false; return; }
        ppc = cam.GetComponent<PixelPerfectCamera>();
        half = ViewHalf();
        lastCam = cam.transform.position;
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        Profile p = profile;
        float w = p.wind;

        // Far dust motes: 1 px, behind the world, slow drift.
        Add(new Layer { count = N(48, p.dust), depth = 0.55f, sortLayer = "Background", order = -101, sprite = ParryFx.Square,
            colA = DustCol, colB = Color.white, alphaMin = 0.12f, alphaMax = 0.35f,
            velMin = new Vector2(w - 0.12f, -0.06f), velMax = new Vector2(w + 0.12f, 0.1f), sway = 0.15f, swayHz = 0.25f, flicker = 0.3f });
        // Near dust: 2 px, in front of everything, dim, slides past faster than the world.
        Add(new Layer { count = N(8, p.dust), depth = 1.3f, sortLayer = "FX", order = 250, sprite = ParryFx.Square, sizePx = new Vector2(2f, 2f),
            colA = DustCol, colB = DustCol, alphaMin = 0.1f, alphaMax = 0.22f,
            velMin = new Vector2(w * 1.5f - 0.2f, -0.1f), velMax = new Vector2(w * 1.5f + 0.2f, 0.1f), sway = 0.2f, swayHz = 0.2f });
        // Embers: talisman-yellow pixels rising with a wobble, flickering, short lives.
        Add(new Layer { count = N(14, p.embers), depth = 0.9f, sortLayer = "FX", order = 40, sprite = ParryFx.Square,
            colA = Talisman, colB = TalismanHot, alphaMin = 0.55f, alphaMax = 0.95f,
            velMin = new Vector2(w - 0.25f, 0.35f), velMax = new Vector2(w + 0.25f, 0.9f), sway = 0.35f, swayHz = 0.8f,
            lifeMin = 1.8f, lifeMax = 4f, flicker = 0.6f });
        // Paper-ash: dark 2x1 flakes falling with a flutter.
        Add(new Layer { count = N(8, p.ash), depth = 0.95f, sortLayer = "FX", order = 39, sprite = ParryFx.Square, sizePx = new Vector2(2f, 1f),
            colA = AshCol, colB = AshCol * 1.3f, alphaMin = 0.6f, alphaMax = 0.85f,
            velMin = new Vector2(w * 2f - 0.2f, -0.7f), velMax = new Vector2(w * 2f + 0.2f, -0.35f), sway = 0.4f, swayHz = 1.1f,
            lifeMin = 3f, lifeMax = 5f, flicker = 0.25f });
        if (p.rain > 0f)
        {
            if (rainSprite == null) rainSprite = MakeRainSprite();
            Vector2 rv = new Vector2(-3.2f, -16f); // down-left, like the sprite's slant
            Add(new Layer { count = N(55, p.rain), depth = 0.7f, sortLayer = "Background", order = -101, sprite = rainSprite,
                colA = RainCol, colB = RainCol, alphaMin = 0.18f, alphaMax = 0.32f, velMin = rv * 0.85f, velMax = rv });
            Add(new Layer { count = N(14, p.rain), depth = 1.25f, sortLayer = "FX", order = 251, sprite = rainSprite,
                colA = RainCol, colB = Color.white, alphaMin = 0.25f, alphaMax = 0.4f, velMin = rv * 1.1f, velMax = rv * 1.25f });
        }
        if (p.mist > 0f)
        {
            if (mistSprite == null) mistSprite = MakeMistSprite();
            Add(new Layer { count = N(6, p.mist), depth = 0.45f, sortLayer = "Background", order = -101, sprite = mistSprite,
                colA = MistCol, colB = MistCol, alphaMin = 0.06f, alphaMax = 0.11f,
                velMin = new Vector2(w * 0.5f - 0.15f, 0f), velMax = new Vector2(w * 0.5f + 0.15f, 0f), lowerHalf = true });
        }
        BuildStrips();
    }

    static int N(int baseCount, float mul) => Mathf.RoundToInt(baseCount * mul);

    Vector2 ViewHalf()
    {
        if (ppc != null && ppc.assetsPPU > 0) return new Vector2(ppc.refResolutionX, ppc.refResolutionY) * 0.5f / ppc.assetsPPU;
        return new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
    }

    void Add(Layer l)
    {
        if (l.count <= 0) return;
        l.ps = new P[l.count];
        Vector2 dims = l.sprite == ParryFx.Square ? l.sizePx : l.sprite.rect.size;
        l.snapOff = new Vector2(Mathf.RoundToInt(dims.x) % 2 == 1 ? 0.5f / Ppu : 0f, Mathf.RoundToInt(dims.y) % 2 == 1 ? 0.5f / Ppu : 0f);
        for (int i = 0; i < l.count; i++)
        {
            var go = new GameObject("air");
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(l.sizePx.x / (l.sprite == ParryFx.Square ? Ppu : 1f), l.sizePx.y / (l.sprite == ParryFx.Square ? Ppu : 1f), 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = l.sprite;
            sr.sharedMaterial = mat;
            sr.sortingLayerName = l.sortLayer;
            sr.sortingOrder = l.order;
            l.ps[i] = new P { t = go.transform, sr = sr };
            Respawn(l, ref l.ps[i], true);
        }
        layers.Add(l);
    }

    void Respawn(Layer l, ref P p, bool anywhere)
    {
        Vector2 box = half + Vector2.one * Margin;
        p.local = new Vector2(Random.Range(-box.x, box.x), l.lowerHalf ? Random.Range(-half.y, 0f) : Random.Range(-box.y, box.y));
        p.vel = new Vector2(Random.Range(l.velMin.x, l.velMax.x), Random.Range(l.velMin.y, l.velMax.y));
        p.maxLife = l.lifeMax > 0f ? Random.Range(l.lifeMin, l.lifeMax) : 0f;
        p.life = anywhere && p.maxLife > 0f ? Random.Range(0f, p.maxLife) : p.maxLife;
        p.phase = Random.Range(0f, 100f);
        p.alpha = Random.Range(l.alphaMin, l.alphaMax);
        p.col = Color.Lerp(l.colA, l.colB, Random.value * 0.5f);
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        Vector2 c = cam.transform.position;
        Vector2 camDelta = c - lastCam;
        lastCam = c;
        Vector2 box = half + Vector2.one * Margin;
        float time = Time.time;
        foreach (var l in layers)
        {
            for (int i = 0; i < l.ps.Length; i++)
            {
                ref P p = ref l.ps[i];
                if (dt > 0f)
                {
                    p.local += p.vel * dt - camDelta * l.depth;
                    if (p.maxLife > 0f)
                    {
                        p.life -= dt;
                        if (p.life <= 0f) Respawn(l, ref p, false);
                    }
                }
                else p.local -= camDelta * l.depth; // frozen in time, still fixed in its depth while the camera moves
                p.local.x = Wrap(p.local.x, box.x);
                p.local.y = l.lowerHalf ? Mathf.Repeat(p.local.y + box.y, box.y) - box.y : Wrap(p.local.y, box.y); // mist stays in the lower half
                float sway = l.sway > 0f ? Mathf.Sin((time + p.phase) * l.swayHz * 6.2832f) * l.sway : 0f;
                Vector2 w = c + p.local + new Vector2(sway, 0f);
                p.t.position = new Vector3(Mathf.Round(w.x * Ppu) / Ppu + l.snapOff.x, Mathf.Round(w.y * Ppu) / Ppu + l.snapOff.y, 0f);
                float a = p.alpha;
                if (l.flicker > 0f) a *= 1f - l.flicker * (0.5f + 0.5f * Mathf.Sin((time + p.phase) * 7.3f));
                if (p.maxLife > 0f) a *= Mathf.Clamp01(Mathf.Min(p.life, p.maxLife - p.life) / 0.4f); // fade in / out
                Color col = p.col; col.a = Quantize(a);
                p.sr.color = col;
            }
        }
        PlaceStrips(c);
    }

    static float Wrap(float v, float h) => Mathf.Repeat(v + h, 2f * h) - h; // any distance (a segment cut snaps the camera far)
    static float Quantize(float a) => Mathf.Round(a * 16f) / 16f; // stepped alpha: no smooth fades on the pixels

    // Edge strips: full-width bands stacked from each screen edge, glued to the (snapped) camera, in front of the world, under UI canvases.
    void BuildStrips()
    {
        foreach (float a in BottomStrip) strips.Add(Strip(a));
        foreach (float a in TopStrip) strips.Add(Strip(a));
    }

    SpriteRenderer Strip(float alpha)
    {
        var go = new GameObject("edge-shade");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ParryFx.Square;
        sr.sharedMaterial = mat;
        sr.sortingLayerName = "FX";
        sr.sortingOrder = 260;
        Color col = StripColor; col.a = alpha;
        sr.color = col;
        return sr;
    }

    void PlaceStrips(Vector2 c)
    {
        Vector2 h = ViewHalf(); // follows the punch zoom, so the bands stay on the edge
        float step = StripStepPx / (ppc != null && ppc.assetsPPU > 0 ? (float)ppc.assetsPPU : Ppu);
        float width = h.x * 2f + 2f;
        int k = 0;
        for (int i = 0; i < BottomStrip.Length; i++, k++) Place(strips[k], c.x, c.y - h.y + step * (i + 0.5f), width, step);
        for (int i = 0; i < TopStrip.Length; i++, k++) Place(strips[k], c.x, c.y + h.y - step * (i + 0.5f), width, step);
    }

    static void Place(SpriteRenderer sr, float x, float y, float w, float h)
    {
        sr.transform.position = new Vector3(x, y, 0f);
        sr.transform.localScale = new Vector3(w, h, 1f);
    }

    // 2x8 px stepped slant (top-right to bottom-left), PPU 32: falls down-left like the rain velocity.
    static Sprite MakeRainSprite()
    {
        var tex = new Texture2D(2, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[16];
        for (int y = 0; y < 8; y++)
        {
            px[y * 2 + (y >= 4 ? 1 : 0)] = new Color(1f, 1f, 1f, 1f - 0.6f * y / 7f); // head (bottom) bright, tail (top) dim
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 8), new Vector2(0.5f, 0.5f), Ppu);
    }

    // 96x20 px blob, two dither levels (solid core, checkerboard rim), PPU 32. No soft gradients.
    static Sprite MakeMistSprite()
    {
        const int W = 96, H = 20;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x + 0.5f - W * 0.5f) / (W * 0.5f), dy = (y + 0.5f - H * 0.5f) / (H * 0.5f);
                float d = dx * dx + dy * dy;
                bool on = d < 0.35f || (d < 1f && ((x + y) & 1) == 0);
                px[y * W + x] = on ? Color.white : Color.clear;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), Ppu);
    }
}
