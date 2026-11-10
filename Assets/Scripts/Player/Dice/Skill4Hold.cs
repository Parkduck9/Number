using System.Collections;
using UnityEngine;

// Dice face 4, 부동술 time hold (DECISIONS 18/19): everything but the hero stops for 4 s (GameFlow.EnemiesHeld) while the
// hero runs 1.5x. The world is greyed by a blue-grey quad on the FX layer (child of the camera) and a full-colour copy of
// the hero is drawn above it, with cyan speed afterimages; a 4-3-2-1 countdown sits at the top. Kills land normally and the
// held bodies all fly / vanish on release. Then DiceReplay plays the recorded 4 s back once at 2x.
public static class Skill4Hold
{
    const float Seconds = 4f;         // DECISIONS 18
    const float Speed = 1.5f;         // contract
    const float ReplayDelay = 0.45f;  // (T) s after the release so the held kills fly before the replay
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

        DiceReplay.Recorder rec = null;
        try
        {
            bool finished = false;
            SpriteRenderer tint = null;
            DiceSkillKit.Mirror mirror = null;
            try
            {
                DiceSkillKit.Hold();
                DiceSkillKit.SetSpeed(p, Speed);
                if (cam != null) tint = BuildTint(cam);
                if (visual != null)
                {
                    mirror = DiceSkillKit.Mirror.Create(visual, MirrorOrder);
                    rec = DiceReplay.Recorder.Begin(visual, cam);
                }

                int shown = -1;
                float trailAt = 0f;
                Vector2 lastTrail = p.Body.position;
                for (float t = 0f; t < Seconds; t += Time.deltaTime)
                {
                    if (DiceSkillKit.PlayerGone(ctx)) yield break;
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
                DiceSkillKit.ReleaseHold();
                DiceSkillKit.ReleaseSpeed();
                if (overlay != null) overlay.HideBig();
                if (tint != null) Object.Destroy(tint.gameObject);
                if (mirror != null) Object.Destroy(mirror.gameObject);
                if (rec != null) rec.Stop();
            }
            if (!finished) yield break;

            // Release: everything held moves again at once.
            Juice.Shake(Juice.ShakeKillAmp, Juice.ShakeKillTime);
            overlay.Flash(Color.white, 0.25f, 0.1f);
            yield return DiceSkillKit.Wait(ReplayDelay);
            if (DiceSkillKit.PlayerGone(ctx) || rec == null) yield break;
            yield return DiceReplay.Play(ctx, rec);
        }
        finally
        {
            if (rec != null) Object.Destroy(rec.gameObject);
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
