using UnityEngine;

// Combat FX helpers: shared 1x1 square, attacker nudge, player slash. Sprite-sheet FX live in SpriteFx.
public static class ParryFx
{
    static Sprite square;

    // Shared 1x1 white sprite (scale it to size).
    public static Sprite Square
    {
        get
        {
            if (square == null)
            {
                Texture2D tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }
            return square;
        }
    }

    // Player swing: sword-slash sprite FX with its arc centered on the attack box + the attacker's visual steps forward.
    public static void Slash(Vector2 center, Transform attackerVisual, int facing, float time = 0.08f, float nudge = 0.125f)
    {
        // The arc's center is this far from the sheet pivot (facing right); x mirrors when facing left.
        Vector2 arcCenter = new Vector2(14f, 30f) / SpriteFx.Ppu;
        SpriteFx.Play("sword-slash", center - new Vector2(facing * arcCenter.x, arcCenter.y), facing < 0, 90);
        NudgeVisual(attackerVisual, facing, time, nudge);
    }

    // Successful parry / clang (playtest 11-11, replaces the hero's white flash): the spot where the blow met lights up instead.
    // parry-clash sheet (star burst, pivot = its centre) + a code-drawn white-gold pixel flare: 3 native-size frames that grow while
    // they fade over GlowTime. Scaled time, so it stays bright through the parry hit-stop and fades after.
    const float GlowTime = 0.12f; // (T) s
    static readonly Color32 FlareCore = new Color32(255, 255, 255, 255), FlareArm = new Color32(255, 214, 90, 255), FlareTip = new Color32(255, 240, 190, 255);
    static Sprite[] flare;

    public static void ContactGlow(Vector2 at)
    {
        SpriteFx.Play("parry-clash", at, false, 103);
        if (flare == null || flare[0] == null) flare = MakeFlare();
        var go = new GameObject("ParryGlow");
        go.transform.position = at;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = flare[0];
        sr.sortingLayerName = "FX";
        sr.sortingOrder = 104;
        go.AddComponent<Glow>().sr = sr;
    }

    // 19x19 four-point star per frame: a white diamond core (radius 2 -> 1 -> 0) and gold arms (4 -> 7 -> 9 px) with pale tips.
    static Sprite[] MakeFlare()
    {
        const int n = 19, c = 9;
        var frames = new Sprite[3];
        for (int k = 0; k < 3; k++)
        {
            int core = 2 - k, arm = k == 0 ? 4 : k == 1 ? 7 : 9;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c), d = Mathf.Max(dx, dy);
                    if (dx + dy <= core) px[y * n + x] = FlareCore;
                    else if ((dx == 0 || dy == 0) && d <= arm) px[y * n + x] = d >= arm - 1 ? FlareTip : FlareArm;
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply();
            frames[k] = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), SpriteFx.Ppu);
        }
        return frames;
    }

    class Glow : MonoBehaviour
    {
        public SpriteRenderer sr;
        float t;

        void Update()
        {
            t += Time.deltaTime;
            if (t >= GlowTime) { Destroy(gameObject); return; }
            float u = t / GlowTime;
            sr.sprite = flare[Mathf.Min(2, (int)(u * 3f))];
            sr.color = new Color(1f, 1f, 1f, 1f - u * u);
        }
    }

    // Attacker's visual steps forward for a moment (enemy melee strike; its tell is ParryRing).
    public static void NudgeVisual(Transform attackerVisual, int facing, float time = 0.08f, float nudge = 0.125f)
    {
        if (attackerVisual == null) return;
        Nudge n = attackerVisual.GetComponent<Nudge>();
        if (n == null) n = attackerVisual.gameObject.AddComponent<Nudge>();
        n.Begin(facing * nudge, time);
    }

    class Nudge : MonoBehaviour
    {
        Vector3 basePos;
        float until = -1f;

        void Awake() { basePos = transform.localPosition; }

        public void Begin(float dx, float time)
        {
            transform.localPosition = basePos + new Vector3(dx, 0f, 0f);
            until = Time.time + time;
        }

        void Update()
        {
            if (until >= 0f && Time.time >= until) { transform.localPosition = basePos; until = -1f; }
        }
    }
}
