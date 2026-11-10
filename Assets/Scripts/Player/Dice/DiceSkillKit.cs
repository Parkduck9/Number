using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shared helpers for the lucky-dice skills (DECISIONS 18/19, Skill1Blink..Skill6Path, DiceReplay):
//  - flag ownership: EnemiesHeld / InputLocked / Player.SpeedMul / Health.SetInvulnerable are set through here and released only
//    if a skill set them (finally blocks), and again on scene unload + play start (Domain Reload is off; a coroutine killed with
//    its LuckyDice never runs its finally).
//  - targets (on-camera, alive) and strikes (PlayerController.StrikeEnemy, pierce) so sparks / kill bursts / hit-stop come for free.
//  - code-drawn dosa FX (no art): talisman yellow paper with red glyph strokes, ink rim, cyan neon lines, on the FX layer.
//    Scaled time like SpriteFx, so they freeze with the hit-stop and the pause menu.
public static class DiceSkillKit
{
    public const float Px = 1f / 32f; // one game pixel (PPU 32)
    public static readonly Color Yellow = new Color32(0xF2, 0xC9, 0x4C, 0xFF); // talisman paper
    public static readonly Color Red = new Color32(0xD9, 0x3B, 0x2B, 0xFF);    // glyph strokes
    public static readonly Color Ink = new Color32(0x16, 0x13, 0x1C, 0xFF);    // ink rim
    public static readonly Color Cyan = new Color32(0x4F, 0xF0, 0xFF, 0xFF);   // the game's neon (#4FF0FF)
    public static readonly Color Dim = new Color(0.7f, 0.72f, 0.8f);

    public static Color A(Color c, float a) { c.a = a; return c; }

    // ---------------------------------------------------------------- flags

    static bool ownHeld, ownLock;
    static PlayerController speedOwner;
    static float speedPrev = 1f;
    static PlayerHealth invulnOwner;
    static Sprite[] cyanDigits;
    static int fxLayer = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        ownHeld = ownLock = false;
        speedOwner = null; speedPrev = 1f;
        invulnOwner = null;
        cyanDigits = null;
        fxLayer = -1;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    static void OnSceneUnloaded(Scene scene) => ReleaseAll();

    public static void ReleaseAll() { ReleaseHold(); ReleaseLock(); ReleaseSpeed(); ReleaseInvulnerable(); }

    public static void Hold()
    {
        if (GameFlow.EnemiesHeld) return; // someone else holds: leave it to them
        GameFlow.EnemiesHeld = true;
        ownHeld = true;
    }

    public static void ReleaseHold()
    {
        if (!ownHeld) return;
        ownHeld = false;
        GameFlow.EnemiesHeld = false;
    }

    public static void Lock()
    {
        if (GameFlow.InputLocked) return;
        GameFlow.InputLocked = true;
        ownLock = true;
    }

    public static void ReleaseLock()
    {
        if (!ownLock) return;
        ownLock = false;
        GameFlow.InputLocked = false;
    }

    public static void SetSpeed(PlayerController p, float mul)
    {
        if (p == null) return;
        if (speedOwner == null) { speedOwner = p; speedPrev = p.SpeedMul; }
        p.SpeedMul = mul;
    }

    public static void ReleaseSpeed()
    {
        if (speedOwner != null) speedOwner.SpeedMul = speedPrev;
        speedOwner = null;
    }

    // Hard invulnerability (PlayerHealth.SetInvulnerable). Only the dice skills use the hard flag, so owning = we turned it on.
    public static void SetInvulnerable(LuckyDice ctx)
    {
        if (invulnOwner != null || ctx.Health == null) return;
        invulnOwner = ctx.Health;
        invulnOwner.SetInvulnerable(true);
    }

    public static void ReleaseInvulnerable()
    {
        if (invulnOwner != null) invulnOwner.SetInvulnerable(false);
        invulnOwner = null;
    }

    // ---------------------------------------------------------------- targets / strikes

    public static bool PlayerGone(LuckyDice ctx) => ctx == null || ctx.Player == null || GameFlow.IsPlayerDead(ctx.Player);

    public static bool Alive(TestEnemy e) => e != null && e.isActiveAndEnabled && !e.IsDown && !e.IsBossWave;

    public static Vector2 Center(TestEnemy e)
    {
        Collider2D c = e.BodyCollider;
        return c != null ? (Vector2)c.bounds.center : (Vector2)e.transform.position;
    }

    // Top of the body (for markers above the head).
    public static Vector2 Head(TestEnemy e)
    {
        Collider2D c = e.BodyCollider;
        return c != null ? new Vector2(c.bounds.center.x, c.bounds.max.y) : (Vector2)e.transform.position + Vector2.up;
    }

    // Alive on-camera enemies, nearest first (a copy: LuckyDice may reuse its list).
    public static List<TestEnemy> Targets(LuckyDice ctx, Vector2 from)
    {
        var list = new List<TestEnemy>();
        foreach (TestEnemy e in ctx.EnemiesOnCamera()) if (Alive(e)) list.Add(e);
        list.Sort((a, b) => (Center(a) - from).sqrMagnitude.CompareTo((Center(b) - from).sqrMagnitude));
        return list;
    }

    // One piercing player blow through the normal hit path (sparks, kill burst, capped hit-stop, shake). True = damaged.
    public static bool Strike(LuckyDice ctx, TestEnemy e)
    {
        if (!Alive(e) || PlayerGone(ctx)) return false;
        return ctx.Player.StrikeEnemy(e, e.BodyCollider, ctx.Player.Body.position, true);
    }

    // Scaled wait (freezes with hit-stop / pause like the rest of the game).
    public static IEnumerator Wait(float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
    }

    // Cyan holo digit sheet 0..9 (Resources/DraftHud/holo_digits_cyan.png, same layout as holo_digits_gold). Null when missing.
    public static Sprite[] CyanDigits()
    {
        if (cyanDigits == null || cyanDigits.Length != 10 || cyanDigits[0] == null)
        {
            Sprite[] s = Resources.LoadAll<Sprite>("DraftHud/holo_digits_cyan").OrderBy(x => x.name).ToArray();
            cyanDigits = s.Length == 10 ? s : null;
        }
        return cyanDigits;
    }

    // ---------------------------------------------------------------- shared moments

    public static void Caption(string en, string ko, Color color) => Overlay.Get().Caption(en, ko, color);

    // No enemy on screen: the roll is spent; a talisman burns away in the hand.
    public static void Fizzle(LuckyDice ctx)
    {
        Caption("No one in sight", "보이는 적이 없다", Dim);
        if (PlayerGone(ctx)) return;
        Fade f = Talisman(ctx.Player.HandPosition, 0.6f);
        f.drift = new Vector2(0f, 0.8f);
        f.grow = 0.3f;
    }

    // Neon blink streak: cyan / red chroma split around a white core (skill 1 / 6 reference "afterimage streak").
    public static void Streak(Vector2 a, Vector2 b, float life = 0.22f)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < 0.04f) return;
        Vector2 n = new Vector2(-d.y, d.x).normalized * (2f * Px);
        Line(a + n, b + n, 10f * Px, A(Cyan, 0.45f), life, 95);
        Line(a - n, b - n, 10f * Px, A(Red, 0.4f), life, 95);
        Line(a, b, 3f * Px, A(Color.white, 0.9f), life * 0.7f, 96);
    }

    // 축지법 "folded earth": short yellow ground lines at the feet that pull together and fade.
    public static void Fold(Vector2 feet)
    {
        for (int i = 0; i < 3; i++)
        {
            float w = (1.6f - i * 0.4f), y = i * 3f * Px;
            Fade f = Line(feet + new Vector2(-w * 0.5f, y), feet + new Vector2(w * 0.5f, y), 1f * Px, A(i == 1 ? Red : Yellow, 0.9f), 0.25f, 97);
            f.grow = 0.15f;
        }
    }

    // ---------------------------------------------------------------- code-drawn pieces

    public static int FxLayer => fxLayer >= 0 ? fxLayer : (fxLayer = SortingLayer.NameToID("FX")); // 0 (Default) if missing

    public static SpriteRenderer Quad(Transform parent, Vector2 local, Vector2 size, Color c, int order, float angle = 0f)
    {
        var sr = new GameObject("q").AddComponent<SpriteRenderer>();
        sr.sprite = ParryFx.Square;
        sr.color = c;
        sr.sortingLayerID = FxLayer;
        sr.sortingOrder = order;
        Transform t = sr.transform;
        t.SetParent(parent, false);
        t.localPosition = local;
        t.localRotation = Quaternion.Euler(0f, 0f, angle);
        t.localScale = new Vector3(size.x, size.y, 1f);
        return sr;
    }

    // A fading line from a to b. Tweak the returned Fade's fields in the same frame (it starts on its first Update).
    public static Fade Line(Vector2 a, Vector2 b, float width, Color c, float life, int order = 96)
    {
        var root = new GameObject("DiceLine");
        root.transform.position = (a + b) * 0.5f;
        Vector2 d = b - a;
        Quad(root.transform, Vector2.zero, new Vector2(d.magnitude + width * 0.5f, width), c, order, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        Fade f = root.AddComponent<Fade>();
        f.life = life;
        return f;
    }

    // A fading ring of 20 segments (grow > 1 expands it).
    public static Fade Ring(Vector2 c, float r, float width, Color col, float life, float grow = 1f, int order = 96)
    {
        var root = new GameObject("DiceRing");
        root.transform.position = c;
        const int steps = 20;
        for (int i = 0; i < steps; i++)
        {
            float a0 = i * 2f * Mathf.PI / steps, a1 = (i + 1) * 2f * Mathf.PI / steps;
            Vector2 p0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, p1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r, d = p1 - p0;
            Quad(root.transform, (p0 + p1) * 0.5f, new Vector2(d.magnitude + width * 0.5f, width), col, order, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
        Fade f = root.AddComponent<Fade>();
        f.life = life;
        f.grow = grow;
        return f;
    }

    // 11x17 px talisman (부적): ink rim, yellow paper, red glyph (two bars, a stem, two feet). Fade fields: follow / fly / life.
    public static Fade Talisman(Vector2 pos, float life, int order = 97)
    {
        var root = new GameObject("Talisman");
        root.transform.position = pos;
        Transform t = root.transform;
        Quad(t, Vector2.zero, new Vector2(11f, 17f) * Px, Ink, order);
        Quad(t, Vector2.zero, new Vector2(9f, 15f) * Px, Yellow, order + 1);
        Quad(t, new Vector2(0f, 5f) * Px, new Vector2(7f, 1f) * Px, Red, order + 2);
        Quad(t, new Vector2(0f, 3f) * Px, new Vector2(5f, 1f) * Px, Red, order + 2);
        Quad(t, new Vector2(0f, -0.5f) * Px, new Vector2(1f, 10f) * Px, Red, order + 2);
        Quad(t, new Vector2(-2f, -4f) * Px, new Vector2(3f, 1f) * Px, Red, order + 2);
        Quad(t, new Vector2(2f, -4f) * Px, new Vector2(3f, 1f) * Px, Red, order + 2);
        Fade f = root.AddComponent<Fade>();
        f.life = life;
        return f;
    }

    // Fades every SpriteRenderer under it over `life` (from `fadeAt` seconds on), then destroys itself. Optional: follow a
    // transform (destroyed when that goes inactive), fly in from `flyFrom` over `flyTime` (spinning), drift, grow, pop-in.
    public class Fade : MonoBehaviour
    {
        public float life = 0.3f, fadeAt, grow = 1f, pop;
        public Vector2 drift;
        public Transform follow;
        public Vector3 offset, flyFrom;
        public float flyTime;

        SpriteRenderer[] rs;
        Color[] cols;
        Vector3 baseScale;
        bool following;
        float t;

        void Start()
        {
            rs = GetComponentsInChildren<SpriteRenderer>();
            cols = new Color[rs.Length];
            for (int i = 0; i < rs.Length; i++) cols[i] = rs[i].color;
            baseScale = transform.localScale;
            following = follow != null;
            Apply();
        }

        void Update()
        {
            t += Time.deltaTime;
            if (t >= life) { Destroy(gameObject); return; }
            Apply();
        }

        void Apply()
        {
            if (following)
            {
                if (follow == null || !follow.gameObject.activeInHierarchy) { Destroy(gameObject); return; }
                Vector3 at = follow.position + offset;
                if (t < flyTime)
                {
                    float k = t / flyTime;
                    transform.SetPositionAndRotation(Vector3.Lerp(flyFrom, at, 1f - (1f - k) * (1f - k)), Quaternion.Euler(0f, 0f, (1f - k) * 540f));
                }
                else transform.SetPositionAndRotation(at, Quaternion.identity);
            }
            else transform.position += (Vector3)(drift * Time.deltaTime);

            float s = Mathf.Lerp(1f, grow, t / life);
            if (pop > 0f && t < pop) s *= Mathf.Lerp(1.7f, 1f, t / pop);
            transform.localScale = baseScale * s;
            float a = t <= fadeAt ? 1f : 1f - (t - fadeAt) / Mathf.Max(0.0001f, life - fadeAt);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null) continue;
                Color c = cols[i];
                c.a *= a;
                rs[i].color = c;
            }
        }
    }

    // Per-frame polyline drawer (pool of stretched squares, like GrappleRope). Begin -> Seg/Dot/RingAt -> Flush, every frame.
    public class Segs : MonoBehaviour
    {
        readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        int used;

        public static Segs Create(string name) => new GameObject(name).AddComponent<Segs>();

        public void Begin() => used = 0;

        public SpriteRenderer Seg(Vector2 a, Vector2 b, float width, Color c, int order)
        {
            if (used == pool.Count) pool.Add(Quad(transform, Vector2.zero, Vector2.one, Color.white, 0));
            SpriteRenderer s = pool[used++];
            Vector2 d = b - a;
            s.enabled = true;
            s.sortingOrder = order;
            s.color = c;
            s.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg));
            s.transform.localScale = new Vector3(d.magnitude + width * 0.5f, width, 1f);
            return s;
        }

        public void Dot(Vector2 p, float size, Color c, int order, float angle = 0f)
        {
            SpriteRenderer s = Seg(p, p, size, c, order);
            s.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            s.transform.localScale = new Vector3(size, size, 1f);
        }

        public void RingAt(Vector2 c, float r, float width, Color col, int order, int steps = 16)
        {
            Vector2 prev = c + new Vector2(r, 0f);
            for (int i = 1; i <= steps; i++)
            {
                float a = i * 2f * Mathf.PI / steps;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                Seg(prev, p, width, col, order);
                prev = p;
            }
        }

        public void Flush() { for (int i = used; i < pool.Count; i++) pool[i].enabled = false; }
    }

    // A full-colour copy of `src` on the FX layer (skill 4: the hero stays in colour above the grey tint). LateUpdate = after
    // the player moved and animated this frame.
    public class Mirror : MonoBehaviour
    {
        SpriteRenderer src, sr;

        public static Mirror Create(SpriteRenderer src, int order)
        {
            var m = new GameObject("DiceMirror").AddComponent<Mirror>();
            m.src = src;
            m.sr = m.gameObject.AddComponent<SpriteRenderer>();
            m.sr.sortingLayerID = FxLayer;
            m.sr.sortingOrder = order;
            m.LateUpdate();
            return m;
        }

        void LateUpdate()
        {
            if (src == null) { Destroy(gameObject); return; }
            Transform s = src.transform;
            transform.SetPositionAndRotation(s.position, s.rotation);
            transform.localScale = s.lossyScale;
            sr.sprite = src.sprite;
            sr.flipX = src.flipX;
            sr.flipY = src.flipY;
            sr.color = src.color;
            sr.enabled = src.enabled && src.gameObject.activeInHierarchy;
        }
    }

    // Screen overlay for the skills (one per scene, built on demand): colour flash, glitch bars, a skill-name caption, a big
    // number (skill 4 countdown). Real time like ParryFeedback, frozen while the pause menu is open. Canvas 31: over the parry
    // flash (30), under the control hints (35) and the HUDs.
    public class Overlay : MonoBehaviour
    {
        static Overlay inst;

        Canvas canvas;
        Image flash;
        Image[] bars;
        TMP_Text caption, captionShadow, big, bigShadow;
        Color flashColor;
        float flashLeft, flashTime, glitchLeft, glitchTick, captionLeft, captionTime;

        public static Overlay Get()
        {
            if (inst == null) inst = new GameObject("DiceOverlay").AddComponent<Overlay>();
            return inst;
        }

        void Awake()
        {
            canvas = UiKit.CreateCanvas("DiceCanvas", transform, 31);
            flash = UiKit.FilledImage(UiKit.Stretch("Flash", canvas.transform), Color.clear);
            bars = new Image[8];
            for (int i = 0; i < bars.Length; i++)
            {
                bars[i] = UiKit.FilledImage(UiKit.Rect("GlitchBar", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero), Color.clear);
                bars[i].enabled = false;
            }
            captionShadow = UiKit.Label("CaptionShadow", canvas.transform, new Vector2(0.5f, 1f), new Vector2(2f, -122f), new Vector2(640f, 48f), "", "", 36, Ink);
            caption = UiKit.Label("Caption", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(640f, 48f), "", "", 36, Yellow);
            bigShadow = UiKit.Label("BigShadow", canvas.transform, new Vector2(0.5f, 1f), new Vector2(3f, -201f), new Vector2(240f, 96f), "", "", 72, Ink);
            big = UiKit.Label("Big", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -198f), new Vector2(240f, 96f), "", "", 72, Color.white);
            caption.gameObject.SetActive(false); captionShadow.gameObject.SetActive(false);
            big.gameObject.SetActive(false); bigShadow.gameObject.SetActive(false);
        }

        public void Flash(Color c, float alpha, float time)
        {
            flashColor = A(c, alpha);
            flashTime = flashLeft = time;
            flash.color = flashColor;
        }

        public void Glitch(float time) { glitchLeft = time; glitchTick = 0f; }

        public void Caption(string en, string ko, Color c, float time = 0.9f)
        {
            SetText(caption, en, ko); SetText(captionShadow, en, ko);
            caption.color = c;
            caption.gameObject.SetActive(true); captionShadow.gameObject.SetActive(true);
            captionTime = captionLeft = time;
            ApplyCaption();
        }

        public void Big(string text, Color c)
        {
            SetText(big, text, text); SetText(bigShadow, text, text);
            big.color = c;
            big.gameObject.SetActive(true); bigShadow.gameObject.SetActive(true);
            big.rectTransform.localScale = bigShadow.rectTransform.localScale = Vector3.one * 1.3f; // pops back to 1 in Update
        }

        public void HideBig() { big.gameObject.SetActive(false); bigShadow.gameObject.SetActive(false); }

        void Update()
        {
            if (PauseMenu.IsOpen) return;
            float dt = Time.unscaledDeltaTime;

            if (flashLeft > 0f)
            {
                flashLeft = Mathf.Max(0f, flashLeft - dt);
                flash.color = A(flashColor, flashColor.a * flashLeft / flashTime);
            }

            if (glitchLeft > 0f)
            {
                glitchLeft -= dt;
                glitchTick -= dt;
                if (glitchLeft <= 0f) foreach (Image b in bars) b.enabled = false;
                else if (glitchTick <= 0f)
                {
                    glitchTick = 0.04f;
                    foreach (Image b in bars)
                    {
                        b.enabled = Random.value < 0.8f;
                        RectTransform rt = b.rectTransform;
                        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, Random.value);
                        rt.sizeDelta = new Vector2(Random.Range(300f, 1100f), Random.Range(2, 7) * 4f);
                        rt.anchoredPosition = new Vector2(Random.Range(-240f, 240f), 0f);
                        b.color = A(Random.value < 0.5f ? Cyan : Red, Random.Range(0.25f, 0.5f));
                    }
                }
            }

            if (captionLeft > 0f)
            {
                captionLeft = Mathf.Max(0f, captionLeft - dt);
                ApplyCaption();
                if (captionLeft <= 0f) { caption.gameObject.SetActive(false); captionShadow.gameObject.SetActive(false); }
            }

            if (big.gameObject.activeSelf)
            {
                float s = Mathf.MoveTowards(big.rectTransform.localScale.x, 1f, dt * 3f);
                big.rectTransform.localScale = bigShadow.rectTransform.localScale = Vector3.one * s;
            }
        }

        void ApplyCaption()
        {
            float k = 1f - captionLeft / captionTime; // 0 -> 1
            float a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
            caption.color = A(caption.color, a);
            captionShadow.color = A(Ink, a);
            float s = k < 0.08f ? Mathf.Lerp(1.25f, 1f, k / 0.08f) : 1f;
            caption.rectTransform.localScale = captionShadow.rectTransform.localScale = Vector3.one * s;
        }
    }

    // Labels from UiKit.Label carry a LocalizedText: change text through it so a language switch keeps the new text.
    public static void SetText(TMP_Text label, string en, string ko)
    {
        var loc = label.GetComponent<LocalizedText>();
        if (loc != null) loc.Set(label, en, ko);
        else label.text = Loc.T(en, ko);
    }
}
