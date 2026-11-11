using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static StageBuildKit;

// Layered, animated street backdrop (STG-01 SEG-A and STG-00): parts in Assets/Art/Backgrounds/STG01_Layers (manifest:
// docs/design/concept/bg_layers/stg01_layers_manifest.md). Native scale (PPU 32, scale 1), parts are 810 px = 25.3125 u wide.
// Depth: sky (almost pinned to the screen) -> skyline -> mid buildings (+ the broadcast screen) -> props on the street plane (world-fixed).
// Parallax factors (0 = world, 1 = screen): see the consts. Props sit in the gameplay plane, so they do not move against the player.
public static class StreetBackdropKit
{
    const string Dir = Stg01LayerImporter.Dir;
    const float Ppu = 32f;
    const float TileW = 810f / Ppu;            // 25.3125 u
    const float GroundRow = 34f;               // px from the bottom of the 268 px parts to the street surface (stall feet) in the manifest composition
    const float CamHalfW = 15f;                // 960 / 2 / 32
    const float SkylineLift = 82f;             // px: the manifest composition puts the skyline 82 px above the mid layer's bottom (towers stand above the roofs)
    internal static Vector2 camMax; // shared with PartsBackdropKit (STG-02..05)

    // x / y camera-follow fractions. Sky / skyline y 0.8 / 0.7 (map audit #2): they follow a climb in a tall segment instead of sinking out of
    // view (one-screen segments such as STG-00 never move the camera up, so y does not matter there).
    static readonly Vector2 SkyF = new Vector2(0.9f, 0.8f), SkylineF = new Vector2(0.6f, 0.7f), MidF = new Vector2(0.3f, 0.2f);
    // Layer origin on the street: left edge of the 810 px composition (segment-local x) = where the preview composition sits.
    const float LeftEdge = 2.5f;
    // Draw order inside the Background sorting layer (AssignSortingLayers puts every "Background*" object there).
    const int OSkyFill = -112, OSky = -111, OCloud = -110, OSkyline = -109, OTwinkle = -108, OGroundFill = -107, OMid = -106, OGlow = -105, ONumber = -104, OProp = -103, OFx = -102;

    static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Dir + file + ".png");
    internal static Sprite[] Frames(string file, string dir = Dir)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(dir + file + ".png").OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        return all.Length > 0 ? all : null;
    }

    // Flat stall roofs / crate lid (DECISIONS 41): (first px from the left, last px, height px above the part's bottom), from the PNG alpha.
    static readonly Dictionary<string, (int a, int b, int h)[]> Tops = new Dictionary<string, (int, int, int)[]>
    {
        { "stall_magenta", new[] { (22, 73, 80) } },
        { "stall_cyan", new[] { (22, 79, 80) } },
        { "crates", new[] { (18, 34, 32) } },
    };
    static Transform walkRoot; // segment root that gets the one-way colliders (null = backdrop only, STG-00)

    // root = segment root (world origin of the segment must be x = root.position.x), width = segment width, top = segment height,
    // groundY = gameplay ground height (segment-local), camMinX..camMaxX = camera x range (segment-local) for the tile coverage.
    // steps (STG-01 SEG-A, DECISIONS 41): extra stalls / crates (file, segment-local x) on the street; when given, every stall roof and crate lid
    // in the segment also gets a one-way collider (low tier platforms = the drawn surfaces). Null = backdrop only (STG-00).
    // hideTwinTower: cut the broadcast-screen tower out of the mirrored mid tiles (map audit #7), so only the real one shows.
    public static void Build(Transform root, float width, float top, float groundY, float camMinX, float camMaxX, (string file, float x)[] steps = null, bool hideTwinTower = false)
    {
        walkRoot = steps != null ? root : null;
        foreach (string f in Directory.GetFiles(Dir, "*.png")) AssetDatabase.ImportAsset(f.Replace('\\', '/'), ImportAssetOptions.ForceSynchronousImport); // settings applied before loading
        var sky = Load("far_sky");
        var skyline = Load("far_skyline");
        var mid = Load("mid_buildings");
        if (sky == null || skyline == null || mid == null) { Todo.Add("STG01_Layers parts missing: layered background skipped"); return; }

        float y0 = groundY - GroundRow / Ppu;                     // bottom of the 268 px parts
        float cx0 = LeftEdge + TileW * 0.5f;                      // centre x of the composition tile (real mid tile with the broadcast screen)
        Vector2 camOrigin = new Vector2(root.position.x + camMinX, SegHeight * 0.5f);
        camMax = new Vector2(root.position.x + camMaxX, Mathf.Max(top - SegHeight * 0.5f, SegHeight * 0.5f));

        // ---- sky: flat bands (sampled from the sprite, run the whole segment and above/below the art) + the sprite (moon) + drifting clouds
        var skyFill = Group(root, "Background (sky fill)", new Vector2(0f, SkyF.y), camOrigin);
        foreach (var (rowStart, rowEnd, c) in Runs(Dir + "far_sky.png", false))
        {
            float yb = y0 + rowStart / Ppu, yt = y0 + rowEnd / Ppu;
            if (rowStart == 0) yb = -10f;                          // lowest band continues below the street strip
            if (rowEnd == (int)sky.rect.height) yt = top + 4f;     // highest band continues up the tall segment
            AddSquare(skyFill, "Background (sky band)", new Vector2(width * 0.5f, (yb + yt) * 0.5f), new Vector2(width + 4f, yt - yb), c, OSkyFill);
        }
        var skyG = Group(root, "Background (sky)", SkyF, camOrigin);
        Sprite1(skyG, "Background (far sky)", sky, new Vector2(cx0, y0), false, OSky);
        float skyL = -5f, skyR = (camMaxX - camMinX) * (1f - SkyF.x) + 2f * CamHalfW + 5f; // clouds wrap just outside the visible range of the sky group
        // (canvas px from the left / from the bottom of the composition) per cloud; native drift 0.5-1 px/s
        Cloud(skyG, "cloud_a", 120f, 235f, false, 0.5f, y0, cx0, skyL, skyR);
        Cloud(skyG, "cloud_c", 330f, 205f, false, 0.75f, y0, cx0, skyL, skyR);
        Cloud(skyG, "cloud_b", 505f, 240f, false, 1f, y0, cx0, skyL, skyR);
        Cloud(skyG, "cloud_a", 690f, 215f, true, 0.6f, y0, cx0, skyL, skyR);
        Cloud(skyG, "cloud_b", 790f, 170f, false, 0.9f, y0, cx0, skyL, skyR);

        // ---- skyline (+ window twinkle), repeated across the segment
        var skylineG = Group(root, "Background (skyline)", SkylineF, camOrigin);
        Range(SkylineF.x, camMinX, camMaxX, out float sMin, out float sMax);
        // fill under the skyline (its bottom-row colour): it rises faster than the mid layer, so a climb would show the sky below it.
        // Hidden behind the mid layer while the camera is low (the mid art is opaque up to 130 px, the skyline starts at 82 px).
        float ySkyline = y0 + SkylineLift / Ppu;
        var skylineBottom = Runs(Dir + "far_skyline.png", true).OrderByDescending(r => r.end - r.start).First();
        AddSquare(skylineG, "Background (skyline fill)", new Vector2(width * 0.5f, (ySkyline - 10f) * 0.5f), new Vector2(width + 4f, ySkyline + 10f), skylineBottom.c, OSkyline);
        Tiles(skylineG, "Background (far skyline)", skyline, null, 0f, cx0, y0 + SkylineLift / Ppu, sMin, sMax, false, OSkyline);
        Tiles(skylineG, "Background (window twinkle)", null, Frames("far_window_twinkle_4f"), 1f, cx0, y0 + SkylineLift / Ppu, sMin, sMax, false, OTwinkle);

        // ---- street ground fill under the mid layer, then the mid buildings (+ lantern glow); tile 0 carries the broadcast screen
        var groundG = Group(root, "Background (ground fill)", new Vector2(0f, MidF.y), camOrigin);
        var bottom = Runs(Dir + "mid_buildings.png", true).OrderByDescending(r => r.end - r.start).First(); // dominant colour of the bottom row, as (start,end,colour) runs along x
        AddSquare(groundG, "Background (ground fill)", new Vector2(width * 0.5f, (y0 - 10f) * 0.5f + 0.5f / Ppu), new Vector2(width + 4f, y0 + 10f + 1f / Ppu), bottom.c, OGroundFill);
        var midG = Group(root, "Background (mid)", MidF, camOrigin);
        Range(MidF.x, camMinX, camMaxX, out float mMin, out float mMax);
        Transform tile0 = Tiles(midG, "Background (mid buildings)", mid, null, 0f, cx0, y0, mMin, mMax, true, OMid);
        Tiles(midG, "Background (lantern glow)", null, Frames("mid_lantern_glow_4f"), 2f, cx0, y0, mMin, mMax, true, OGlow);
        if (tile0 != null) BroadcastNumber(tile0);
        if (hideTwinTower) HideTwinTowers(midG, mid);

        // ---- props on the street plane (world-fixed), positions from the preview composition
        var props = new GameObject("Background (props)").transform;
        props.SetParent(root, false);
        float px(float c) => LeftEdge + c / Ppu;
        float py(float fromBottom) => y0 + fromBottom / Ppu;
        Prop(props, "stall_magenta", null, 0, px(216f), py(GroundRow), OProp);
        Prop(props, "stall_cyan", null, 0, px(382f), py(GroundRow), OProp);
        Prop(props, "stall_magenta", null, 0, px(529f), py(GroundRow), OProp);
        Prop(props, "steam_6f", "steam_6f", 6, px(210f), py(70f), OFx);
        Prop(props, "steam_6f", "steam_6f", 6, px(376f), py(70f), OFx);
        Prop(props, "steam_6f", "steam_6f", 6, px(523f), py(70f), OFx);
        Prop(props, "power_pole", null, 0, px(655f), py(GroundRow), OProp);
        Prop(props, "lantern_red_4f", "lantern_red_4f", 4, px(675f), py(133f), OFx);
        Prop(props, "neon_sign_4f", "neon_sign_4f", 3, px(311f), py(89f), OFx);
        Prop(props, "holo_board_4f", "holo_board_4f", 4, px(209f), py(147f), OFx);
        Prop(props, "holo_ring_8f", "holo_ring_8f", 8, px(121f), py(149f), OFx);
        Prop(props, "stone_lantern_2f", "stone_lantern_2f", 3, px(759f), py(GroundRow), OProp);
        Prop(props, "crates", null, 0, px(610f), py(GroundRow), OProp);
        if (steps != null) foreach (var (file, x) in steps) Prop(props, file, null, 0, x, groundY, OProp);
        // drones cross the whole segment and wrap, bobbing 1 px
        Drone(props, "drone_scout_2f", 10f, px(110f), py(185f), 1.2f, width, OFx);
        Drone(props, "drone_delivery_2f", 10f, px(445f), py(178f), -0.8f, width, OFx);
        // segments taller than a screen: a drone every 8 u further up, so the climb does not look into an empty sky (map audit #2)
        if (top > SegHeight)
        {
            int n = 0;
            for (float hy = py(185f) + 8f; hy < top - 2f; hy += 8f, n++)
                Drone(props, n % 2 == 0 ? "drone_delivery_2f" : "drone_scout_2f", 10f, px(n % 2 == 0 ? 600f : 250f), hy, n % 2 == 0 ? 0.9f : -1.1f, width, OFx);
        }
    }

    // ------------------------------------------------------------------ pieces

    internal static Transform Group(Transform root, string name, Vector2 factor, Vector2 camOrigin)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        var p = go.AddComponent<Parallax>();
        var so = new SerializedObject(p);
        so.FindProperty("factor").vector2Value = factor;
        so.FindProperty("cameraOrigin").vector2Value = camOrigin;
        so.FindProperty("cameraMax").vector2Value = camMax;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go.transform;
    }

    internal static Transform Sprite1(Transform parent, string name, Sprite s, Vector2 pos, bool flip, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (((int)s.rect.width & 1) == 1) pos.x += 0.5f / Ppu; // odd width, pivot at the centre: shift half a pixel so the edges land on whole pixels
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(flip ? -1f : 1f, 1f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        return go.transform;
    }

    internal static void Loop(GameObject go, Sprite[] frames, float fps)
    {
        var loop = go.AddComponent<SpriteLoop>();
        var so = new SerializedObject(loop);
        var arr = so.FindProperty("frames");
        arr.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        so.FindProperty("fps").floatValue = fps;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Tile centres cx0 + k * TileW covering the segment-local range [min, max]. mirror: odd tiles are flipped (pixel-continuous seams for art
    // that does not tile); the overlay copies flip with their base. Returns tile 0 (k = 0). frames != null: animated overlay at fps.
    internal static Transform Tiles(Transform parent, string name, Sprite single, Sprite[] frames, float fps, float cx0, float y0, float min, float max, bool mirror, int order)
    {
        if (single == null && frames == null) return null;
        int kMin = Mathf.FloorToInt((min - cx0) / TileW + 0.5f), kMax = Mathf.CeilToInt((max - cx0) / TileW - 0.5f);
        Transform tile0 = null;
        for (int k = kMin; k <= kMax; k++)
        {
            bool flip = mirror && (((k % 2) + 2) % 2) == 1;
            Transform t = Sprite1(parent, name, single != null ? single : frames[0], new Vector2(cx0 + k * TileW, y0), flip, order);
            if (frames != null) Loop(t.gameObject, frames, fps);
            if (k == 0) tile0 = t;
        }
        return tile0;
    }

    // Segment-local x range a layer with horizontal factor f must cover so the 30 u view is always full while the camera moves camMin..camMax
    // (the layer sits at its authored place when the camera is at camMin and drifts by f * camera travel).
    internal static void Range(float f, float camMin, float camMax, out float min, out float max)
    {
        min = camMin - CamHalfW - 1f;
        max = camMax * (1f - f) + camMin * f + CamHalfW + 1f;
    }

    static void Cloud(Transform parent, string file, float canvasX, float fromBottom, bool flip, float pxPerSecond, float y0, float cx0, float minX, float maxX)
    {
        var s = Load(file);
        if (s == null) return;
        Transform t = Sprite1(parent, "Background (" + file + ")", s, new Vector2(LeftEdge + canvasX / Ppu, y0 + fromBottom / Ppu), flip, OCloud);
        var d = t.gameObject.AddComponent<Drifter>();
        var so = new SerializedObject(d);
        so.FindProperty("speed").floatValue = pxPerSecond / Ppu;
        so.FindProperty("minX").floatValue = minX;
        so.FindProperty("maxX").floatValue = maxX;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // sheet = animation file (null = static sprite `file`), fps = loop rate
    static void Prop(Transform parent, string file, string sheet, float fps, float x, float y, int order)
    {
        Sprite s = sheet == null ? Load(file) : null;
        Sprite[] frames = sheet != null ? Frames(sheet) : null;
        if (s == null && frames == null) { Todo.Add("STG01_Layers part missing: " + file); return; }
        Transform t = Sprite1(parent, "Background (" + file + ")", s != null ? s : frames[0], new Vector2(x, y), false, order);
        if (frames != null) Loop(t.gameObject, frames, fps);
        if (walkRoot != null && s != null && Tops.TryGetValue(file, out var spans)) TopColliders(walkRoot, file, s, x, y, spans);
    }

    // One-way colliders (same as StageBuildKit.Ledge) on the flat spans of a world-fixed part placed bottom-centre at segment-local (x, y),
    // with Sprite1's pixel snap, so the floor sits exactly on the drawn roof line. spans = (first px, last px, height px) from the PNG.
    internal static void TopColliders(Transform root, string file, Sprite s, float x, float y, (int a, int b, int h)[] spans)
    {
        int w = (int)s.rect.width;
        float left = x + ((w & 1) == 1 ? 0.5f / Ppu : 0f) - w * 0.5f / Ppu;
        foreach (var (a, b, h) in spans)
            BuildCollider(root, Ledge($"TOP {Path.GetFileName(file)} x{x:0.##} px{a}", 0, left + a / Ppu, left + (b + 1) / Ppu, y + h / Ppu), false);
    }

    static void Drone(Transform parent, string sheet, float fps, float x, float y, float speed, float width, int order)
    {
        var frames = Frames(sheet);
        if (frames == null) { Todo.Add("STG01_Layers part missing: " + sheet); return; }
        Transform t = Sprite1(parent, "Background (" + sheet + ")", frames[0], new Vector2(x, y), false, order);
        Loop(t.gameObject, frames, fps);
        var d = t.gameObject.AddComponent<Drifter>();
        var so = new SerializedObject(d);
        so.FindProperty("speed").floatValue = speed;
        so.FindProperty("minX").floatValue = -4f;
        so.FindProperty("maxX").floatValue = width + 4f;
        so.FindProperty("bob").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // mid_buildings.png: broadcast screen inner rect (490,166,106,50) from the bottom-left; digits at 2x (the mid art is a 2 px grid).
    static void BroadcastNumber(Transform tile0)
    {
        var go = new GameObject("Background (broadcast number)");
        go.transform.SetParent(tile0, false);
        go.transform.localPosition = new Vector3((490f + 53f - 405f) / Ppu, (166f + 25f) / Ppu, 0f);
        go.transform.localScale = new Vector3(2f, 2f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = ONumber;
        go.AddComponent<BroadcastScreen>();
    }

    // mid_buildings.png: the broadcast-screen tower as px rects (x, y from the bottom, w, h), stepped to hug the roofs in front of its base.
    static readonly RectInt[] TowerPx =
    {
        new RectInt(452, 180, 204, 88), new RectInt(474, 167, 166, 13), new RectInt(474, 158, 138, 9),
        new RectInt(474, 146, 116, 12), new RectInt(476, 141, 116, 5), new RectInt(494, 131, 98, 10),
    };

    // A flipped mid tile repeats the tower (blank screen) 16 u from the real one (map audit #7). Flipped mid tiles and their glow overlays
    // render outside SpriteMasks laid over their own tower (children of the flipped tile, so the rects mirror with it); the skyline shows through.
    static void HideTwinTowers(Transform midG, Sprite mid)
    {
        float half = mid.rect.width * 0.5f;
        foreach (Transform t in midG)
        {
            if (t.localScale.x > 0f) continue;
            t.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
            if (!t.name.EndsWith("(mid buildings)")) continue;
            foreach (var r in TowerPx)
            {
                var go = new GameObject("Background (twin tower mask)");
                go.transform.SetParent(t, false);
                go.transform.localPosition = new Vector3((r.x + r.width * 0.5f - half) / Ppu, (r.y + r.height * 0.5f) / Ppu, 0f);
                go.transform.localScale = new Vector3(r.width / Ppu, r.height / Ppu, 1f) / Square.bounds.size.x; // as AddSquare
                go.AddComponent<SpriteMask>().sprite = Square;
            }
        }
    }

    // ------------------------------------------------------------------ pixel sampling (editor only; the PNGs are not readable at runtime)

    // Runs of equal colour along one column (column 0, bottom -> top: rows) or along the bottom row (left -> right: columns).
    // Returned as (start, end, colour); end exclusive.
    internal static List<(int start, int end, Color32 c)> Runs(string path, bool bottomRow)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        var runs = new List<(int, int, Color32)>();
        int n = bottomRow ? tex.width : tex.height;
        Color32 prev = default;
        int start = 0;
        for (int i = 0; i < n; i++)
        {
            Color32 c = bottomRow ? tex.GetPixel(i, 0) : tex.GetPixel(0, i);
            if (i > 0 && !c.Equals(prev)) { runs.Add((start, i, prev)); start = i; }
            prev = c;
        }
        runs.Add((start, n, prev));
        Object.DestroyImmediate(tex);
        return runs;
    }
}
