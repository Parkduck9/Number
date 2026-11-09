using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static StageBuildKit;
using static StreetBackdropKit;

// Layered, animated backdrops for STG-02..05, same system as STG-01 (StreetBackdropKit, DECISIONS 17): parts in
// Assets/Art/Backgrounds/STG02_05_Parts (manifest: docs/design/concept/bg_assets/parts_manifest.md), native scale (PPU 32, scale 1),
// Parallax groups (sky -> skyline -> wall -> buildings) + world-fixed props on the gameplay plane. Plates are only 126-196 px high, so the rest of
// the segment is flat colour sampled from the plates' own edge rows (sky bands go up and down, a fill under each layer), never stretched.
// Every far layer is dimmed (sprite colour multiply) so the player and enemies read in front.
public static class PartsBackdropKit
{
    const string Parts = Stg01LayerImporter.PartsDir;
    const float Ppu = 32f;
    // camera-follow fractions (0 = world, 1 = screen): x like STG-01; y high enough that the layers stay in view while the camera climbs a tall segment
    const float SkyX = 0.9f, SkylineX = 0.6f, WallX = 0.45f, BldX = 0.3f;
    const float FySky = 0.8f, FySkyline = 0.75f, FyWall = 0.6f, FyBld = 0.5f;
    const float DimSky = 0.85f, DimSkyline = 0.7f, DimWall = 0.65f, DimBld = 0.8f, DimProp = 0.9f;
    // draw order inside the Background sorting layer (every "Background*" object goes there, AssignSortingLayers)
    const int OSkyFill = -130, OSky = -129, OFarFx = -128, OSkylineFill = -127, OSkyline = -126, OTwinkle = -125, OCeiling = -124, OFar = -123,
        OWallFill = -122, OWall = -120, OBldFill = -118, OFence = -117, OBld = -115, ORail = -112, OProp = -108;
    const float CompDrop = 6f / Ppu;   // manifest composition: the interior plate ends 6 px below the ground line
    const float FarRise = 142f / Ppu;  // ... and the 128 px far plate starts 142 px above the interior plate's bottom (they overlap)

    struct Part
    {
        public string file, sheet; public float fps; public int gap; // gap = px after the part; sheet = overlay frames at the same spot
        public Part(string file, int gap, string sheet = null, float fps = 0f) { this.file = file; this.gap = gap; this.sheet = sheet; this.fps = fps; }
    }

    static Transform R;
    static float Wd, Top, Y0, CamMin, CamMax, Cx0;
    static Vector2 origin;
    static readonly List<(Transform t, float k)> dims = new List<(Transform, float)>();

    // ------------------------------------------------------------------ stages (segment-local coordinates, Y0 = gameplay ground)

    // STG-02 SEG-A rooftops / SEG-B elevated rail: night city, buildings + vending / crates / lanterns on the street, rail on SEG-B.
    public static void Rooftops(Transform root, float width, float top, float camMin, float camMax, bool rail)
    {
        Begin(root, width, top, 1f, camMin, camMax);
        float ySky = Y0 + 4f;
        Sky("far/rooftops_sky", ySky);
        Skyline("far/rooftops_skyline", null, 0f, "far/rooftop_window_twinkle_4f", 1f, ySky);
        var shop = new Part("buildings/shop_2f", 48); var house = new Part("buildings/house_ac", 32); var ruin = new Part("buildings/ruin_hanok", 56);
        Buildings(Y0, "buildings/wall_tile", rail ? new[] { ruin, house, ruin, shop } : new[] { shop, house, ruin, house });
        if (rail) Rail();
        var p = Props();
        float lamp = Y0 + 4.5f; // hanging lanterns (top pivot)
        if (!rail)
        {
            Put(p, "props/vending_dancheong", null, 0f, 4.5f, Y0, OProp);
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 12.5f, lamp, OProp);
            Put(p, "props/crates_neon", null, 0f, 22f, Y0, OProp);
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 28.5f, lamp, OProp);
        }
        else
        {
            Put(p, "props/vending_dancheong", null, 0f, 11.5f, Y0, OProp);
            Put(p, "props/crates_neon", null, 0f, 23f, Y0, OProp);
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 33f, lamp, OProp);
            Put(p, "props/vending_dancheong", null, 0f, 38f, Y0, OProp);
            Put(p, "props/crates_neon", null, 0f, 44.5f, Y0, OProp);
        }
        // spill lights (were AddLamps on the old images): the lanterns warm, the vending machines vermilion
        if (!rail) { Glow(12.5f, lamp - 1f, LampWarm, 4f, 0.2f); Glow(28.5f, lamp - 1f, LampWarm, 4f); Glow(4.5f, Y0 + 1.3f, LampRed, 3f); }
        else { Glow(33f, lamp - 1f, LampWarm, 4f, 0.2f); Glow(11.5f, Y0 + 1.3f, LampRed, 3f); Glow(38f, Y0 + 1.3f, LampTeal, 3f); }
        Finish();
    }

    // STG-04 SEG-A production line / SEG-B inspection hall: smoking stacks beyond the factory wall, halls and watchtowers, machines on the floor.
    public static void Factory(Transform root, float width, float top, float camMin, float camMax, bool hall)
    {
        Begin(root, width, top, 1f, camMin, camMax);
        float ySky = Y0 + 5f;
        Sky("far/factory_sky", ySky);
        Skyline("far/factory_skyline", "far/smoke_stack_6f", 3f, null, 0f, ySky);
        Wall("interiors/factory_wall", Y0 - CompDrop, WallX, FyWall, DimWall, OWallFill, OWall, true,
            ("interiors/red_lamp_blink_2f", 1f), ("interiors/vat_bubbles_6f", 4f));
        var factory = new Part("buildings/factory_hall", 64); var tower = new Part("buildings/watchtower", 96, "buildings/watchtower_lamp_blink_2f", 2f);
        Buildings(Y0, "buildings/wall_tile", hall ? new[] { tower, factory, tower } : new[] { factory, tower, factory });
        if (hall) Rail();
        var p = Props();
        var belt = new[] { new Part("props/conveyor", 0, "props/conveyor_scroll_4f", 5f) };
        float cable = Y0 + 6f; // hangs from the top of the wall
        if (!hall)
        {
            Row(p, belt, Y0, 3f, 10f, OProp);
            Put(p, "props/robot_arm", null, 0f, 14f, Y0, OProp);
            Put(p, "props/cable_bundle", null, 0f, 20f, cable, OProp);
            Put(p, "props/crates_neon", null, 0f, 23.5f, Y0, OProp);
            Put(p, "props/robot_arm", null, 0f, 31f, Y0, OProp);
            Put(p, "props/server_cabinet", "props/server_cabinet_lights_4f", 3f, 38f, Y0, OProp);
            Put(p, "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 44f, Y0, OProp);
        }
        else
        {
            Put(p, "props/server_cabinet", "props/server_cabinet_lights_4f", 3f, 6f, Y0, OProp);
            Put(p, "props/robot_arm", null, 0f, 12f, Y0, OProp);
            Row(p, belt, Y0, 15f, 22f, OProp);
            Put(p, "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 27f, Y0, OProp);
            Put(p, "props/crates_neon", null, 0f, 33f, Y0, OProp);
            Put(p, "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 40f, Y0, OProp);
            Put(p, "props/robot_arm", null, 0f, 44f, Y0, OProp);
        }
        // spill lights: the scanner gates teal, the robot arms' hazard plates vermilion
        if (!hall) { Glow(14f, Y0 + 1.5f, LampRed, 4f); Glow(31f, Y0 + 1.5f, LampRed, 4f); Glow(44f, Y0 + 1.3f, LampTeal, 5f, 0.15f); }
        else { Glow(27f, Y0 + 1.3f, LampTeal, 5f, 0.15f); Glow(40f, Y0 + 1.3f, LampTeal, 5f); Glow(44f, Y0 + 1.5f, LampRed, 4f); }
        Finish();
    }

    // STG-03 hideout room (one screen): masonry far wall peeking above the interior wall (manifest composition), bulbs and CRTs, old machines and charms.
    public static void Hideout(Transform root, float width, float groundY)
    {
        Begin(root, width, SegHeight, groundY, 15f, 15f);
        float yIn = Y0 - CompDrop, yFar = yIn + FarRise;
        Ceiling("far/hideout_wall", yFar + 128f / Ppu);
        Wall("far/hideout_wall", yFar, SkylineX, FySkyline, DimSkyline, OFar, OFar, false);
        Wall("interiors/hideout_wall", yIn, WallX, FyWall, DimWall, OWallFill, OWall, true,
            ("interiors/bulb_flicker_4f", 3f), ("interiors/crt_static_4f", 6f));
        var p = Props();
        Put(p, "props/server_cabinet", "props/server_cabinet_lights_4f", 3f, 5.5f, Y0, OProp);
        Put(p, null, "props/lantern_hanji_sway_4f", 3f, 8.5f, Y0 + 5.5f, OProp);
        Put(p, "props/byeongpung_holo", null, 0f, 11.5f, Y0, OProp);
        Put(p, "props/talisman_holo", "props/talisman_holo_flicker_4f", 3f, 14.5f, Y0 + 1.5f, OProp);
        Put(p, "props/cable_bundle", null, 0f, 16.5f, Y0 + 6f, OProp);
        Put(p, "props/jangseung_pair", "props/jangseung_screen_faces_blink_4f", 3f, 23f, Y0, OProp);
        Glow(8.5f, Y0 + 4.5f, LampWarm, 5f, 0.25f); // the hanging lantern (flickers)
        Glow(5.5f, Y0 + 1.2f, LampTeal, 3f);        // server cabinet lights
        Finish();
    }

    // STG-05 throne room (one screen): palace sky with searchlights behind the skyline, throne wall with the LED screen, haetae and jangseung.
    public static void Palace(Transform root, float width, float groundY)
    {
        Begin(root, width, SegHeight, groundY, 15f, 15f);
        float yIn = Y0 - CompDrop, yFar = yIn + FarRise;
        Sky("far/palace_sky", yFar);
        Skyline("far/palace_skyline", "far/searchlight_sweep_8f", 2f, null, 0f, yFar);
        Wall("interiors/throne_wall", yIn, WallX, FyWall, DimWall, OWallFill, OWall, true, ("interiors/ledwall_shimmer_4f", 2f));
        var bld = Layer("Background (buildings)", BldX, FyBld, DimBld);
        Put(bld, "buildings/gate_scanner", null, 0f, 15f, Y0, OBld);
        var p = Props();
        Put(p, "props/byeongpung_holo", null, 0f, 3.5f, Y0, OProp);
        Put(p, "props/haetae", "props/haetae_eyes_blink_4f", 3f, 8f, Y0, OProp);
        Put(p, "props/jangseung_pair", "props/jangseung_screen_faces_blink_4f", 3f, 22.5f, Y0, OProp);
        Glow(15f, Y0 + 4f, LampWarm, 8f, 0.1f); // the throne wall's LED screen
        Glow(8f, Y0 + 1f, LampWarm, 4f);        // haetae
        Finish();
    }

    // ------------------------------------------------------------------ layers

    static void Begin(Transform root, float width, float top, float groundY, float camMinX, float camMaxX)
    {
        R = root; Wd = width; Top = top; Y0 = groundY; CamMin = camMinX; CamMax = camMaxX;
        Cx0 = camMinX; // the centre tile sits under the camera when it starts
        origin = new Vector2(root.position.x + camMinX, SegHeight * 0.5f);
        StreetBackdropKit.camMax = new Vector2(root.position.x + camMaxX, Mathf.Max(top - SegHeight * 0.5f, SegHeight * 0.5f));
        dims.Clear();
        EnsureImported();
    }

    // Parts imported before the importer rule existed (or with defaults) are reimported so the sprites exist with PPU 32 / Point / slices.
    static void EnsureImported()
    {
        if (!Directory.Exists(Parts)) return;
        foreach (string f in Directory.GetFiles(Parts, "*.png", SearchOption.AllDirectories))
        {
            string p = f.Replace('\\', '/');
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null || ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 32f || ti.filterMode != FilterMode.Point)
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }

    static Transform Layer(string name, float fx, float fy, float dim)
    {
        var g = Group(R, name, new Vector2(fx, fy), origin);
        dims.Add((g, dim));
        return g;
    }

    static Transform Props() // gameplay plane: world-fixed, no parallax
    {
        var go = new GameObject("Background (props)").transform;
        go.SetParent(R, false);
        dims.Add((go, DimProp));
        return go;
    }

    static void Finish()
    {
        foreach (var (t, k) in dims)
            foreach (var sr in t.GetComponentsInChildren<SpriteRenderer>(true))
                sr.color = new Color(sr.color.r * k, sr.color.g * k, sr.color.b * k, sr.color.a);
    }

    static void Missing(string file) => Todo.Add("STG02_05_Parts part missing: " + file);

    // Spill light at a lit part (segment-local x, y), same light-only NeonSign as StageBuildKit.AddLamps (LIGHTING_DESIGN 3.1), lighting the backdrop too.
    static void Glow(float x, float y, Color c, float radius, float flicker = 0f)
    {
        var parent = R.Find("Lamps (light only)");
        if (parent == null) { parent = new GameObject("Lamps (light only)").transform; parent.SetParent(R, false); }
        var n = LightingSetup.BuildNeon("Lamp", parent, R.TransformPoint(new Vector3(x, y, 0f)), null, c, radius, flicker, 0, "World");
        n.GetComponentInChildren<Light2D>().targetSortingLayers = new[] { SortingLayer.NameToID("Background"), SortingLayer.NameToID("World"), SortingLayer.NameToID("Characters") };
    }

    // Hard-band sky: flat bands sampled from the sprite's left column continue below and above the art, the sprite sits on top (tiles mirrored).
    static void Sky(string file, float ySky)
    {
        var sprite = Load(file);
        if (sprite == null) { Missing(file); return; }
        var fill = Layer("Background (sky fill)", 0f, FySky, DimSky);
        foreach (var (rs, re, c) in Runs(Parts + file + ".png", false))
        {
            float yb = rs == 0 ? -10f : ySky + rs / Ppu, yt = re >= (int)sprite.rect.height ? Top + 30f : ySky + re / Ppu;
            AddSquare(fill, "Background (sky band)", new Vector2(Wd * 0.5f, (yb + yt) * 0.5f), new Vector2(Wd + 4f, yt - yb), c, OSkyFill);
        }
        var g = Layer("Background (sky)", SkyX, FySky, DimSky);
        Range(SkyX, CamMin, CamMax, out float min, out float max);
        Tiles(g, "Background (sky)", sprite, null, 0f, Cx0, ySky, min, max, true, OSky);
    }

    // Skyline silhouette (+ optional smoke / searchlight strip behind it, window twinkle on it), registered to the sky at ySky; solid fill below it.
    static void Skyline(string file, string behind, float behindFps, string twinkle, float twinkleFps, float ySky)
    {
        var sprite = Load(file);
        if (sprite == null) { Missing(file); return; }
        FillBelow("Background (skyline fill)", FySkyline, DimSkyline, ySky, Edge(file, false), OSkylineFill);
        var g = Layer("Background (skyline)", SkylineX, FySkyline, DimSkyline);
        Range(SkylineX, CamMin, CamMax, out float min, out float max);
        Overlay(g, behind, behindFps, ySky, min, max, OFarFx);
        Tiles(g, "Background (skyline)", sprite, null, 0f, Cx0, ySky, min, max, true, OSkyline);
        Overlay(g, twinkle, twinkleFps, ySky, min, max, OTwinkle);
    }

    static void Overlay(Transform g, string sheet, float fps, float y, float min, float max, int order)
    {
        if (sheet == null) return;
        var frames = Frames(sheet, Parts);
        if (frames == null) { Missing(sheet); return; }
        Tiles(g, "Background (" + Path.GetFileName(sheet) + ")", null, frames, fps, Cx0, y, min, max, true, order);
    }

    // Opaque 810 px plate (interior wall, far wall) tiled mirrored with its registered overlays; flat fill below when `fill`.
    static void Wall(string file, float y, float fx, float fy, float dim, int fillOrder, int order, bool fill, params (string sheet, float fps)[] overlays)
    {
        var sprite = Load(file);
        if (sprite == null) { Missing(file); return; }
        if (fill) FillBelow("Background (wall fill)", fy, dim, y, Edge(file, false), fillOrder);
        var g = Layer("Background (" + Path.GetFileName(file) + ")", fx, fy, dim);
        Range(fx, CamMin, CamMax, out float min, out float max);
        Tiles(g, "Background (" + Path.GetFileName(file) + ")", sprite, null, 0f, Cx0, y, min, max, true, order);
        foreach (var o in overlays) Overlay(g, o.sheet, o.fps, y, min, max, order + 1);
    }

    // Flat colour above a plate that ends below the top of the segment (the plate's own top-row colour).
    static void Ceiling(string file, float yFrom)
    {
        var g = Layer("Background (ceiling)", 0f, FySkyline, DimSkyline);
        AddSquare(g, "Background (ceiling fill)", new Vector2(Wd * 0.5f, (yFrom + Top + 30f) * 0.5f), new Vector2(Wd + 4f, Top + 30f - yFrom), Edge(file, true), OCeiling);
    }

    // Low fence strip (wall_tile, repeats edge to edge) with the building row in front, ground-coloured fill under both.
    static Transform Buildings(float y, string fence, params Part[] row)
    {
        FillBelow("Background (ground fill)", FyBld, DimBld, y, Edge(fence, false), OBldFill);
        var g = Layer("Background (buildings)", BldX, FyBld, DimBld);
        Range(BldX, CamMin, CamMax, out float min, out float max);
        Row(g, new[] { new Part(fence, 0) }, y, min, max, OFence);
        Row(g, row, y, min, max, OBld);
        return g;
    }

    // Elevated rail: pillars every second pillar width, the track strip on top of them (the pillar's top sits under the track).
    static void Rail()
    {
        var g = Layer("Background (rail)", BldX, FyBld, DimBld);
        Range(BldX, CamMin, CamMax, out float min, out float max);
        Row(g, new[] { new Part("buildings/rail_pillar", 174) }, Y0, min, max, ORail);
        Row(g, new[] { new Part("buildings/rail_track", 0) }, Y0 + 178f / Ppu, min, max, ORail + 2);
    }

    static void FillBelow(string name, float fy, float dim, float toY, Color c, int order)
    {
        var g = Layer(name, 0f, fy, dim);
        AddSquare(g, name + " square", new Vector2(Wd * 0.5f, (toY - 10f) * 0.5f), new Vector2(Wd + 4f, toY + 10f), c, order);
    }

    // ------------------------------------------------------------------ pieces

    static Sprite Load(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Parts + file + ".png");

    // One part, bottom-centre (top-centre for hanging parts) at (x, y): static sprite `file`, and/or looping frames `sheet` at the same spot
    // (colour-replacement overlay above the static sprite, or the replacement sprite itself when file is null).
    static void Put(Transform parent, string file, string sheet, float fps, float x, float y, int order)
    {
        if (file != null)
        {
            var s = Load(file);
            if (s == null) { Missing(file); return; }
            Sprite1(parent, "Background (" + Path.GetFileName(file) + ")", s, new Vector2(x, y), false, order);
        }
        if (sheet == null) return;
        var frames = Frames(sheet, Parts);
        if (frames == null) { Missing(sheet); return; }
        var t = Sprite1(parent, "Background (" + Path.GetFileName(sheet) + ")", frames[0], new Vector2(x, y), false, file != null ? order + 1 : order);
        Loop(t.gameObject, frames, fps);
    }

    // Parts left to right (cycling) over [min, max], `gap` px after each; pixel aligned. A strip of one part with gap 0 repeats edge to edge.
    static void Row(Transform parent, Part[] parts, float y, float min, float max, int order)
    {
        if (parts.Length == 0) return;
        float x = Mathf.Floor(min * Ppu) / Ppu;
        for (int i = 0; x < max; i++)
        {
            var p = parts[i % parts.Length];
            var s = Load(p.file);
            if (s == null) { Missing(p.file); return; }
            float w = s.rect.width / Ppu;
            Put(parent, p.file, p.sheet, p.fps, x + w * 0.5f, y, order);
            x += w + p.gap / Ppu;
        }
    }

    // Most common opaque colour of the first / last pixel row (editor only: the PNGs are not readable at runtime).
    static Color32 Edge(string file, bool top)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(Parts + file + ".png"));
        var count = new Dictionary<int, int>();
        int best = 0x14161E, bestN = 0, row = top ? tex.height - 1 : 0;
        for (int x = 0; x < tex.width; x++)
        {
            Color32 c = tex.GetPixel(x, row);
            if (c.a != 255) continue;
            int key = (c.r << 16) | (c.g << 8) | c.b;
            count.TryGetValue(key, out int n);
            count[key] = ++n;
            if (n > bestN) { bestN = n; best = key; }
        }
        Object.DestroyImmediate(tex);
        return new Color32((byte)(best >> 16), (byte)(best >> 8), (byte)best, 255);
    }
}
