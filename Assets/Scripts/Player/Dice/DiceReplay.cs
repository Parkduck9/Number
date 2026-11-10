using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Skill 4 replay (DECISIONS 18: "the hero's 4 s are recorded and played back at 2x while the game stops").
// Simple version: the Recorder stores the hero's visual every frame (position relative to the camera, sprite, flipX);
// Play locks input and re-traces those moves on the current screen as a ghost at 2x (~2 s), with a VHS look: chroma-split
// ghost (cyan / red copies), scanlines, a rolling tracking bar, a corner frame, "REPLAY" with a blinking red dot, a
// timecode. Enemies and hits are not re-enacted (the kills already happened on release).
public static class DiceReplay
{
    const float Rate = 2f;        // contract: 2x
    const int MaxFrames = 1200;   // 4 s at up to 300 fps
    static readonly Color GhostColor = new Color(0.85f, 0.95f, 1f, 0.9f);
    static Texture2D scanTex;

    public struct Frame
    {
        public float t;
        public Vector2 offset; // visual position - camera position
        public Sprite sprite;
        public bool flipX;
    }

    public class Recorder : MonoBehaviour
    {
        public readonly List<Frame> Frames = new List<Frame>();
        public Vector3 Scale { get; private set; } = Vector3.one;
        public float Duration => Frames.Count > 0 ? Frames[Frames.Count - 1].t : 0f;

        SpriteRenderer src;
        Camera cam;
        float t;

        public static Recorder Begin(SpriteRenderer src, Camera cam)
        {
            var r = new GameObject("DiceReplayRecorder").AddComponent<Recorder>();
            r.src = src;
            r.cam = cam;
            return r;
        }

        public void Stop() => enabled = false;

        void LateUpdate() // after the hero moved and animated
        {
            if (src == null || Frames.Count >= MaxFrames) return;
            if (Frames.Count > 0) t += Time.deltaTime;
            Vector2 c = cam != null ? (Vector2)cam.transform.position : Vector2.zero;
            Frames.Add(new Frame { t = t, offset = (Vector2)src.transform.position - c, sprite = src.sprite, flipX = src.flipX });
            Scale = src.transform.lossyScale;
        }
    }

    public static IEnumerator Play(LuckyDice ctx, Recorder rec)
    {
        List<Frame> frames = rec.Frames;
        if (frames.Count < 2 || rec.Duration <= 0f) yield break;
        rec.Stop();
        Camera cam = ctx.Cam != null ? ctx.Cam : Camera.main;
        float dur = rec.Duration / Rate;

        GameObject ui = null, ghosts = null;
        try
        {
            DiceSkillKit.Lock();
            ui = BuildVhs(out RawImage scan, out Image track, out Image dot, out TMP_Text timer);
            ghosts = new GameObject("DiceReplayGhost");
            ghosts.transform.localScale = rec.Scale;
            SpriteRenderer red = Ghost(ghosts.transform, DiceSkillKit.A(DiceSkillKit.Red, 0.45f), 93);
            SpriteRenderer cyan = Ghost(ghosts.transform, DiceSkillKit.A(DiceSkillKit.Cyan, 0.45f), 94);
            SpriteRenderer main = Ghost(ghosts.transform, GhostColor, 95);
            Sfx.Play(SfxId.UiSelect);

            int i = 0, shownSec = -1;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                if (DiceSkillKit.PlayerGone(ctx)) yield break;
                float rt = t * Rate;
                while (i < frames.Count - 1 && frames[i + 1].t <= rt) i++;
                Frame f = frames[i];
                Vector2 pos = (cam != null ? (Vector2)cam.transform.position : Vector2.zero) + f.offset;
                float jitter = Random.value < 0.08f ? Random.Range(-2, 3) * DiceSkillKit.Px : 0f; // tape wobble
                Show(main, f, pos + new Vector2(jitter, 0f));
                Show(cyan, f, pos + new Vector2(jitter - 1.5f * DiceSkillKit.Px, 0f));
                Show(red, f, pos + new Vector2(jitter + 1.5f * DiceSkillKit.Px, 0f));

                int sec = Mathf.Min(4, (int)rt);
                if (sec != shownSec) { shownSec = sec; DiceSkillKit.SetText(timer, "0:00:0" + sec, "0:00:0" + sec); }
                scan.uvRect = new Rect(0f, -t * 0.6f, 1f, 90f); // 90 periods of 3 texels over the 540-high canvas = 2 px lines
                RectTransform tr = track.rectTransform;
                tr.anchorMin = new Vector2(0f, 1f - Mathf.Repeat(t * 0.7f, 1f));
                tr.anchorMax = new Vector2(1f, tr.anchorMin.y);
                dot.enabled = ((int)(t * 3f) & 1) == 0;
                yield return null;
            }
        }
        finally
        {
            DiceSkillKit.ReleaseLock();
            if (ui != null) Object.Destroy(ui);
            if (ghosts != null) Object.Destroy(ghosts);
        }
    }

    static SpriteRenderer Ghost(Transform parent, Color c, int order)
    {
        var sr = new GameObject("Ghost").AddComponent<SpriteRenderer>();
        sr.transform.SetParent(parent, false);
        sr.color = c;
        sr.sortingLayerID = DiceSkillKit.FxLayer;
        sr.sortingOrder = order;
        return sr;
    }

    static void Show(SpriteRenderer sr, Frame f, Vector2 pos)
    {
        sr.transform.position = pos;
        sr.sprite = f.sprite;
        sr.flipX = f.flipX;
    }

    // Overlay canvas 34 (over the skill overlay 31, under the HUDs).
    static GameObject BuildVhs(out RawImage scan, out Image track, out Image dot, out TMP_Text timer)
    {
        Canvas canvas = UiKit.CreateCanvas("DiceReplayVhs", null, 34);
        Transform root = canvas.transform;
        UiKit.FilledImage(UiKit.Stretch("Tint", root), new Color(0.08f, 0.12f, 0.22f, 0.22f));

        if (scanTex == null)
        {
            scanTex = new Texture2D(1, 3) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            scanTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.35f));
            scanTex.SetPixel(0, 1, Color.clear);
            scanTex.SetPixel(0, 2, Color.clear);
            scanTex.Apply();
        }
        RectTransform srt = UiKit.Stretch("Scanlines", root);
        scan = srt.gameObject.AddComponent<RawImage>();
        scan.texture = scanTex;
        scan.raycastTarget = false;

        RectTransform trt = UiKit.Rect("Tracking", root, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 10f));
        trt.anchorMax = new Vector2(1f, 1f);
        track = UiKit.FilledImage(trt, new Color(1f, 1f, 1f, 0.12f));

        // Camcorder corner brackets, 2 px lines inset 20 px.
        Color frame = new Color(1f, 1f, 1f, 0.7f);
        for (int cx = 0; cx < 2; cx++)
            for (int cy = 0; cy < 2; cy++)
            {
                Vector2 anchor = new Vector2(cx, cy);
                float sx = cx == 0 ? 1f : -1f, sy = cy == 0 ? 1f : -1f;
                UiKit.FilledImage(UiKit.Rect("Corner", root, anchor, new Vector2(sx * (20f + 18f), sy * 21f), new Vector2(36f, 2f)), frame);
                UiKit.FilledImage(UiKit.Rect("Corner", root, anchor, new Vector2(sx * 21f, sy * (20f + 18f)), new Vector2(2f, 36f)), frame);
            }

        dot = UiKit.FilledImage(UiKit.Rect("RecDot", root, new Vector2(0f, 1f), new Vector2(52f, -52f), new Vector2(12f, 12f)), DiceSkillKit.Red);
        UiKit.Label("Replay", root, new Vector2(0f, 1f), new Vector2(130f, -52f), new Vector2(140f, 36f), "REPLAY", "REPLAY", 24, Color.white);
        UiKit.Label("Vhs", root, new Vector2(1f, 1f), new Vector2(-80f, -52f), new Vector2(100f, 36f), "VHS", "VHS", 24, Color.white);
        timer = UiKit.Label("Timecode", root, new Vector2(1f, 0f), new Vector2(-120f, 52f), new Vector2(180f, 36f), "0:00:00", "0:00:00", 24, Color.white);
        return canvas.gameObject;
    }
}
