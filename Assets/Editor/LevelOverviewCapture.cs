using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu NUMBER/Capture Level Overviews -> docs/qa/level_overview/<tag>/<STG>_seg<i>.png (+ _col.png with Ground colliders outlined,
// green = solid, cyan = one-way, magenta = has a PLACEHOLDER visual) and <STG>_all.png (segments stacked, collider overlay).
// Uses the scene camera offscreen (pixel-perfect off), whole segment rect from SegmentCameraBounds. Editor-time positions (no parallax).
// Batchmode: -executeMethod LevelOverviewCapture.CaptureBatch  (tag from the env var NUMBER_CAPTURE_TAG, default "before").
public static class LevelOverviewCapture
{
    static readonly string[] Stages = { "STG01", "STG02", "STG03", "STG04", "STG05" };
    const float PxPerUnit = 16f; // half of PPU 32: a 76 x 34 u segment = 1215 x 540
    const string OutRoot = "docs/qa/level_overview";

    [MenuItem("NUMBER/Capture Level Overviews")]
    public static void CaptureMenu() => Capture("before");

    public static void CaptureBatch()
    {
        var tag = System.Environment.GetEnvironmentVariable("NUMBER_CAPTURE_TAG");
        Capture(string.IsNullOrEmpty(tag) ? "before" : tag);
    }

    static void Capture(string tag)
    {
        var dir = Path.Combine(OutRoot, tag);
        Directory.CreateDirectory(dir);
        foreach (var stg in Stages)
        {
            var path = $"Assets/Scenes/{stg}.unity";
            if (!File.Exists(path)) { Debug.LogWarning("[Overview] missing " + path); continue; }
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) { Debug.LogWarning("[Overview] no camera in " + stg); continue; }
            foreach (var b in cam.GetComponents<Behaviour>()) if (b != null && b != cam) b.enabled = false; // pixel perfect, follow...

            var rects = SegmentRects(cam, stg);
            var stacked = new List<Texture2D>();
            for (int i = 0; i < rects.Count; i++)
            {
                var tex = Render(cam, rects[i]);
                Save(tex, Path.Combine(dir, $"{stg}_seg{i}.png"));
                DrawColliders(tex, rects[i]);
                Save(tex, Path.Combine(dir, $"{stg}_seg{i}_col.png"));
                stacked.Add(tex);
            }
            if (stacked.Count > 1) Save(Stack(stacked), Path.Combine(dir, $"{stg}_all.png"));
            for (int i = 0; i < rects.Count; i++) Save(Views(cam, rects[i]), Path.Combine(dir, $"{stg}_seg{i}_views.png"));
            Debug.Log($"[Overview] {stg}: {rects.Count} segment(s) -> {dir}");
        }
    }

    // In-game screens (30 x 16.875 u) with Parallax layers moved as at runtime: x = left / middle / right of the camera range,
    // y = bottom / top (rows top to bottom = camera high to low). Grid image, 2 px white gaps.
    static Texture2D Views(Camera cam, Rect seg)
    {
        float hh = 8.4375f, hw = hh * 16f / 9f;
        float[] xs = { seg.xMin + hw, seg.center.x, seg.xMax - hw };
        var ys = seg.height > hh * 2f + 0.1f ? new[] { seg.yMax - hh, seg.yMin + hh } : new[] { seg.yMin + hh };
        var layers = new List<(Transform t, Vector3 basePos, Vector2 f, Vector2 o, Vector2 m)>();
        foreach (var p in Object.FindObjectsByType<Parallax>(FindObjectsSortMode.None))
        {
            var so = new SerializedObject(p);
            layers.Add((p.transform, p.transform.position, so.FindProperty("factor").vector2Value, so.FindProperty("cameraOrigin").vector2Value, so.FindProperty("cameraMax").vector2Value));
        }
        var tiles = new List<Texture2D>();
        foreach (var y in ys)
            foreach (var x in xs)
            {
                foreach (var l in layers)
                {
                    var c = Vector2.Min(Vector2.Max(new Vector2(x, y), l.o), l.m);
                    var d = Vector2.Scale(c - l.o, l.f);
                    l.t.position = new Vector3(l.basePos.x + d.x, l.basePos.y + d.y, l.basePos.z);
                }
                tiles.Add(Render(cam, new Rect(x - hw, y - hh, hw * 2f, hh * 2f)));
            }
        foreach (var l in layers) l.t.position = l.basePos;
        int tw = tiles[0].width, th = tiles[0].height, g = 2, cols = xs.Length, rows = ys.Length;
        var o = new Texture2D(cols * tw + (cols - 1) * g, rows * th + (rows - 1) * g, TextureFormat.RGBA32, false);
        var fill = new Color32[o.width * o.height];
        for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(255, 255, 255, 255);
        o.SetPixels32(fill);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                o.SetPixels(c * (tw + g), o.height - (r + 1) * th - r * g, tw, th, tiles[r * cols + c].GetPixels());
        o.Apply();
        return o;
    }

    static List<Rect> SegmentRects(Camera cam, string stg)
    {
        var list = new List<Rect>();
        var seg = cam.GetComponent("SegmentCameraBounds");
        if (seg != null)
        {
            var arr = new SerializedObject(seg).FindProperty("segments");
            for (int i = 0; arr != null && i < arr.arraySize; i++) list.Add(arr.GetArrayElementAtIndex(i).rectValue);
        }
        if (list.Count == 0) // single-room stages: bounds of every Ground collider
        {
            Bounds? b = null;
            foreach (var c in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
                if (c.gameObject.layer == 6 && !c.isTrigger) { if (b == null) b = c.bounds; else { var x = b.Value; x.Encapsulate(c.bounds); b = x; } }
            var r = b ?? new Bounds(new Vector3(25f, 8.4f), new Vector3(50.625f, 16.875f));
            list.Add(new Rect(r.min.x, r.min.y, r.size.x, Mathf.Max(r.size.y, 16.875f)));
        }
        return list;
    }

    static Texture2D Render(Camera cam, Rect r)
    {
        int w = Mathf.RoundToInt(r.width * PxPerUnit), h = Mathf.RoundToInt(r.height * PxPerUnit);
        var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        cam.orthographic = true;
        cam.orthographicSize = r.height * 0.5f;
        cam.aspect = r.width / r.height;
        cam.transform.position = new Vector3(r.center.x, r.center.y, -10f);
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }

    static void DrawColliders(Texture2D tex, Rect r)
    {
        foreach (var c in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
        {
            if (c.gameObject.layer != 6 || c.isTrigger) continue;
            bool placeholder = false;
            foreach (Transform ch in c.transform) if (ch.name.Contains("PLACEHOLDER") && ch.gameObject.activeSelf) placeholder = true;
            var col = placeholder ? Color.magenta : c.usedByEffector ? Color.cyan : Color.green;
            var b = c.bounds;
            int x0 = Px(b.min.x - r.x), x1 = Px(b.max.x - r.x), y0 = Px(b.min.y - r.y), y1 = Px(b.max.y - r.y);
            for (int x = x0; x <= x1; x++) { Set(tex, x, y0, col); Set(tex, x, y1, col); }
            for (int y = y0; y <= y1; y++) { Set(tex, x0, y, col); Set(tex, x1, y, col); }
        }
        tex.Apply();
    }

    static int Px(float u) => Mathf.RoundToInt(u * PxPerUnit);
    static void Set(Texture2D t, int x, int y, Color c) { if (x >= 0 && y >= 0 && x < t.width && y < t.height) t.SetPixel(x, y, c); }

    static Texture2D Stack(List<Texture2D> parts)
    {
        int w = 0, h = 0, gap = 8;
        foreach (var p in parts) { w = Mathf.Max(w, p.width); h += p.height + gap; }
        var o = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var fill = new Color32[w * h];
        for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(255, 255, 255, 255);
        o.SetPixels32(fill);
        int y = h; // segment 0 on top
        foreach (var p in parts) { y -= p.height + gap; o.SetPixels(0, y + gap, p.width, p.height, p.GetPixels()); }
        o.Apply();
        return o;
    }

    static void Save(Texture2D t, string path) => File.WriteAllBytes(path, t.EncodeToPNG());
}
