using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Dice face 4, 부동술 time hold (DECISIONS 18/19, 27): everything but the hero stops for 4 s (GameFlow.EnemiesHeld) while the
// hero runs 1.5x. The world is greyed by a blue-grey quad on the FX layer (child of the camera), the enemies (+ their number
// holograms) and enemy shots turn see-through, and a full-colour copy of the hero is drawn above it all, with cyan speed
// afterimages; a 4-3-2-1 countdown sits at the top. Blows on enemies are stored (PlayerController.DeferredHits): a spark now,
// and when the hold ends every stored blow lands at once (one shared hit-stop + shake). No replay (DECISIONS 27).
public static class Skill4Hold
{
    const float Seconds = 4f;         // DECISIONS 18
    const float Speed = 1.5f;         // contract
    const float GhostAlpha = 0.35f;   // (T) enemy alpha during the hold
    const float TrailEvery = 0.05f;   // (T) s between speed afterimages (only while moving)
    const int TintOrder = 60, TrailOrder = 61, MirrorOrder = 62; // FX layer: under the slash (90) and the spark sheets (100+)
    static readonly Color Tint = new Color(0.36f, 0.42f, 0.54f, 0.45f); // blue-grey "stopped world"

    public static IEnumerator Run(LuckyDice ctx)
    {
        if (DiceSkillKit.PlayerGone(ctx)) yield break;
        PlayerController p = ctx.Player;
        Camera cam = ctx.Cam != null ? ctx.Cam : Camera.main;
        SpriteRenderer visual = p.GetComponentInChildren<SpriteRenderer>(); // same lookup PlayerController uses for its visual

        DiceSkillKit.Caption("TIME HOLD", "부동술", Color.white);
        DiceSkillKit.Overlay overlay = DiceSkillKit.Overlay.Get();
        overlay.Flash(new Color(0.75f, 0.85f, 1f), 0.35f, 0.15f);
        Juice.Shake(Juice.ShakeParryAmp, Juice.ShakeParryTime);
        Sfx.Play(SfxId.Parry);

        bool finished = false;
        SpriteRenderer tint = null;
        DiceSkillKit.Mirror mirror = null;
        Ghosts ghosts = null;
        try
        {
            DiceSkillKit.Hold();
            DiceSkillKit.SetSpeed(p, Speed);
            p.DeferredHits = new List<PlayerController.DeferredHit>();
            if (cam != null) tint = BuildTint(cam);
            if (visual != null) mirror = DiceSkillKit.Mirror.Create(visual, MirrorOrder);
            ghosts = Ghosts.Create();

            int shown = -1;
            float trailAt = 0f;
            Vector2 lastTrail = p.Body.position;
            for (float t = 0f; t < Seconds; t += Time.deltaTime)
            {
                if (DiceSkillKit.PlayerGone(ctx)) yield break;
                if (GameFlow.InputLocked && !PauseMenu.IsOpen) break; // dialogue / cutscene took over: end the hold early (the stored blows still land); pause (also locks) just waits
                int n = Mathf.CeilToInt(Seconds - t);
                if (n != shown) { shown = n; overlay.Big(n.ToString(), Color.white); }
                if (tint != null) Fit(tint, cam);
                if (visual != null && t >= trailAt && (p.Body.position - lastTrail).sqrMagnitude > 0.04f)
                {
                    trailAt = t + TrailEvery;
                    lastTrail = p.Body.position;
                    Trail(visual);
                }
                yield return null;
            }
            finished = true;
        }
        finally
        {
            if (ghosts != null) ghosts.Restore(); // every exit path: alpha back before anything else
            DiceSkillKit.ReleaseHold();
            DiceSkillKit.ReleaseSpeed();
            if (overlay != null) overlay.HideBig();
            if (tint != null) Object.Destroy(tint.gameObject);
            if (mirror != null) Object.Destroy(mirror.gameObject);
            if (p != null)
            {
                if (finished && !GameFlow.IsPlayerDead(p)) p.ResolveDeferredHits(); // released: every stored blow lands now
                p.DeferredHits = null; // death / unload / abort: the stored blows are dropped
            }
        }

        // Release: everything held moves again at once, the struck fall together.
        Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime);
        overlay.Flash(Color.white, 0.25f, 0.1f);
    }

    // See-through enemies (DECISIONS 27): every LateUpdate (after TestEnemy / EnemyNumber wrote their colours) each SpriteRenderer,
    // LineRenderer and TMP text under TestEnemy.All and the live EnemyProjectiles gets alpha x GhostAlpha. Remembers the alpha it
    // found (a script that rewrites the colour every frame just gets ghosted again) and puts it back in Restore, only on renderers
    // whose alpha is still the one it wrote. Destroyed renderers are skipped; the object dies with the scene.
    [DefaultExecutionOrder(10000)]
    class Ghosts : MonoBehaviour
    {
        class Entry { public Object r; public float orig, wrote; }
        readonly Dictionary<Object, Entry> map = new Dictionary<Object, Entry>();
        readonly List<Component> roots = new List<Component>();
        readonly List<SpriteRenderer> srs = new List<SpriteRenderer>();
        readonly List<LineRenderer> lrs = new List<LineRenderer>();
        readonly List<TMP_Text> tts = new List<TMP_Text>();

        public static Ghosts Create()
        {
            var g = new GameObject("DiceHoldGhosts").AddComponent<Ghosts>();
            foreach (TestEnemy e in TestEnemy.All) if (e != null) g.roots.Add(e);
            foreach (EnemyProjectile s in FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) g.roots.Add(s);
            g.LateUpdate();
            return g;
        }

        void LateUpdate()
        {
            foreach (Component c in roots)
            {
                if (c == null) continue;
                c.GetComponentsInChildren(true, srs);
                foreach (SpriteRenderer r in srs) { Color k = r.color; k.a = Ghost(r, k.a); r.color = k; }
                c.GetComponentsInChildren(true, lrs);
                foreach (LineRenderer r in lrs)
                {
                    Color s = r.startColor, e = r.endColor;
                    float a = Ghost(r, s.a);
                    e.a = s.a > 0f ? e.a * a / s.a : e.a * GhostAlpha;
                    s.a = a;
                    r.startColor = s; r.endColor = e;
                }
                c.GetComponentsInChildren(true, tts);
                foreach (TMP_Text r in tts) r.alpha = Ghost(r, r.alpha);
            }
        }

        // New alpha for a renderer whose current alpha is `a`.
        float Ghost(Object r, float a)
        {
            if (!map.TryGetValue(r, out Entry en)) map[r] = en = new Entry { r = r, orig = a, wrote = -1f };
            else if (!Mathf.Approximately(a, en.wrote)) en.orig = a; // its script rewrote it this frame
            en.wrote = en.orig * GhostAlpha;
            return en.wrote;
        }

        public void Restore()
        {
            foreach (Entry en in map.Values)
            {
                if (en.r == null) continue;
                switch (en.r)
                {
                    case SpriteRenderer r when Mathf.Approximately(r.color.a, en.wrote):
                        { Color k = r.color; k.a = en.orig; r.color = k; break; }
                    case LineRenderer r when Mathf.Approximately(r.startColor.a, en.wrote):
                        {
                            Color s = r.startColor, e = r.endColor;
                            e.a = en.wrote > 0f ? e.a * en.orig / en.wrote : e.a / GhostAlpha;
                            s.a = en.orig;
                            r.startColor = s; r.endColor = e;
                            break;
                        }
                    case TMP_Text r when Mathf.Approximately(r.alpha, en.wrote):
                        r.alpha = en.orig; break;
                }
            }
            map.Clear();
            enabled = false; // Destroy is end-of-frame: this frame's LateUpdate must not ghost everything again
            Destroy(gameObject);
        }
    }

    // Camera child, so it moves with the follow / shake without lagging a frame.
    static SpriteRenderer BuildTint(Camera cam)
    {
        var sr = new GameObject("DiceHoldTint").AddComponent<SpriteRenderer>();
        sr.sprite = ParryFx.Square;
        sr.color = Tint;
        sr.sortingLayerID = DiceSkillKit.FxLayer;
        sr.sortingOrder = TintOrder;
        sr.transform.SetParent(cam.transform, false);
        sr.transform.localPosition = new Vector3(0f, 0f, Mathf.Max(cam.nearClipPlane + 0.5f, -cam.transform.position.z)); // world z ~0
        Fit(sr, cam);
        return sr;
    }

    static void Fit(SpriteRenderer sr, Camera cam)
    {
        float h = cam.orthographicSize * 2f + 1f; // +1: margin for the punch zoom
        sr.transform.localScale = new Vector3(h * cam.aspect + 1f, h, 1f);
    }

    // Cyan speed afterimage above the tint (SpriteFx.Afterimage would sit on the Characters layer, under the grey).
    static void Trail(SpriteRenderer visual)
    {
        var root = new GameObject("DiceHoldTrail");
        Transform src = visual.transform;
        root.transform.SetPositionAndRotation(src.position, src.rotation);
        root.transform.localScale = src.lossyScale;
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = visual.sprite;
        sr.flipX = visual.flipX;
        sr.color = DiceSkillKit.A(DiceSkillKit.Cyan, 0.45f);
        sr.sortingLayerID = DiceSkillKit.FxLayer;
        sr.sortingOrder = TrailOrder;
        root.AddComponent<DiceSkillKit.Fade>().life = 0.22f;
    }
}
