using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    // The building row is world-fixed (DECISIONS 41: its roofs are floors), so only the STG-04 hall rail keeps the old 0.3 / 0.5 building parallax
    // (the STG-02 SEG-B rail is world-fixed on its gameplay ledges, RailOnLedges).
    const float SkyX = 0.9f, SkylineX = 0.6f, WallX = 0.45f, BldX = 0.3f;
    const float FySky = 0.8f, FySkyline = 0.75f, FyWall = 0.6f, FyBld = 0.5f;
    const float DimSky = 0.85f, DimSkyline = 0.7f, DimWall = 0.65f, DimBld = 0.8f, DimProp = 0.9f;
    // draw order inside the Background sorting layer (every "Background*" object goes there, AssignSortingLayers).
    // The rail (parallax, farther) draws behind the world-fixed fence / buildings: pillars ORail, track ORail + 2.
    const int OSkyFill = -130, OSky = -129, OFarFx = -128, OSkylineFill = -127, OSkyline = -126, OTwinkle = -125, OCeiling = -124, OFar = -123,
        OWallFill = -122, OWall = -120, OBldFill = -118, ORail = -118, OFence = -117, OBld = -115, OProp = -108;
    // STG-02 SEG-B world-fixed rail (audit #6): in front of the building row, behind the street props; ledge art (audit #4) above the props.
    const int ORailPillar = -113, OGroundFront = -112, ORailTrack = -111, OLedge = -106;

    // Flat roof / lid spans the player stands on (DECISIONS 41), measured from the PNG alpha (top opaque px per column, flat runs only):
    // (first px from the left, last px, height px above the part's bottom). Sloped roofs: only the flat ridge. wall_tile (fence) is handled
    // by Buildings (floor only between buildings).
    static readonly Dictionary<string, (int a, int b, int h)[]> Tops = new Dictionary<string, (int, int, int)[]>
    {
        { "buildings/shop_2f", new[] { (6, 29, 203), (70, 195, 232) } },     // left eave, roof
        { "buildings/house_ac", new[] { (66, 139, 168) } },
        { "buildings/ruin_hanok", new[] { (0, 21, 105), (66, 119, 128) } },  // left eave, roof (126..130 px, sagging)
        { "buildings/factory_hall", new[] { (76, 135, 196) } },
        { "buildings/watchtower", new[] { (16, 45, 170), (70, 97, 170) } },
        { "props/vending_dancheong", new[] { (6, 61, 84) } },
        { "props/crates_neon", new[] { (13, 53, 61) } },                     // lid only (the low lip at +1.06 ate ground-level enemy shots; Fable review)
        { "props/server_cabinet", new[] { (10, 74, 76) } },
        { "props/scanner_gate", new[] { (5, 65, 85) } },
        { "props/robot_arm", new[] { (16, 37, 88), (46, 69, 82) } },
    };
    // Building overlays (looping frames at the same spot).
    static readonly Dictionary<string, (string sheet, float fps)> BuildingFx = new Dictionary<string, (string, float)>
    {
        { "watchtower", ("buildings/watchtower_lamp_blink_2f", 2f) },
    };
    static bool walk; // Rooftops / Factory: world-fixed parts get one-way colliders on their Tops
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
    // row = the building row, (part in buildings/, segment-local centre x), world-fixed with floors on the roofs (DECISIONS 41).
    // railLedges = the gameplay rail ledges (x1, x2, top) drawn as a world-fixed elevated rail (SEG-B); null = no rail (SEG-A).
    public static void Rooftops(Transform root, float width, float top, float camMin, float camMax, (string part, float x)[] row, (float x1, float x2, float top)[] railLedges)
    {
        Begin(root, width, top, 1f, camMin, camMax);
        walk = true;
        bool rail = railLedges != null;
        float ySky = Y0 + 4f;
        Sky("far/rooftops_sky", ySky);
        Skyline("far/rooftops_skyline", null, 0f, "far/rooftop_window_twinkle_4f", 1f, ySky);
        Buildings(Y0, "buildings/wall_tile", row);
        if (rail) RailOnLedges(railLedges, "buildings/wall_tile");
        var p = Props();
        float lamp = Y0 + 4.5f; // hanging lanterns (top pivot)
        if (!rail)
        {
            Put(p, "props/vending_dancheong", null, 0f, 4.5f, Y0, OProp);
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 12.5f, lamp, OProp);
            Put(p, "props/crates_neon", null, 0f, 16.5f, Y0, OProp); // steps onto the fence top (was x 22)
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 28.5f, lamp, OProp);
            Put(p, "props/crates_neon", null, 0f, 29f, Y0, OProp);   // climb 1: crates -> fence -> ruin roof -> shaft
            // segment end (x 30..76) was bare
            Put(p, "props/vending_dancheong", null, 0f, 36f, Y0, OProp);
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 44.5f, lamp, OProp);
            Put(p, "props/crates_neon", null, 0f, 52f, Y0, OProp);
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 60.5f, lamp, OProp);
            Put(p, "props/vending_dancheong", null, 0f, 67f, Y0, OProp);
            Put(p, "props/crates_neon", null, 0f, 73f, Y0, OProp);
        }
        else
        {
            Put(p, "props/crates_neon", null, 0f, 7.5f, Y0, OProp); // steps onto the fence top (replaces STEP-B1/B2)
            Put(p, "props/vending_dancheong", null, 0f, 11.5f, Y0, OProp);
            Put(p, "props/crates_neon", null, 0f, 23f, Y0, OProp);
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 33f, lamp, OProp);
            Put(p, "props/vending_dancheong", null, 0f, 38f, Y0, OProp);
            Put(p, "props/crates_neon", null, 0f, 44.5f, Y0, OProp);
            // segment end (x 46..76) was bare
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 47f, lamp, OProp); // x 51 hung in front of the RAIL-B2 track
            Put(p, "props/vending_dancheong", null, 0f, 57f, Y0, OProp);
            Put(p, "props/crates_neon", null, 0f, 64f, Y0, OProp);
            Put(p, null, "props/lantern_hanji_sway_4f", 3f, 69.5f, lamp, OProp);
            Put(p, "props/vending_dancheong", null, 0f, 74f, Y0, OProp);
        }
        // spill lights (were AddLamps on the old images): the lanterns warm, the vending machines vermilion
        if (!rail)
        {
            Glow(12.5f, lamp - 1f, LampWarm, 4f, 0.2f); Glow(28.5f, lamp - 1f, LampWarm, 4f); Glow(4.5f, Y0 + 1.3f, LampRed, 3f);
            Glow(36f, Y0 + 1.3f, LampTeal, 3f); Glow(44.5f, lamp - 1f, LampWarm, 4f); Glow(60.5f, lamp - 1f, LampWarm, 4f, 0.2f); Glow(67f, Y0 + 1.3f, LampRed, 3f);
        }
        else
        {
            Glow(33f, lamp - 1f, LampWarm, 4f, 0.2f); Glow(11.5f, Y0 + 1.3f, LampRed, 3f); Glow(38f, Y0 + 1.3f, LampTeal, 3f);
            Glow(47f, lamp - 1f, LampWarm, 4f); Glow(57f, Y0 + 1.3f, LampRed, 3f); Glow(69.5f, lamp - 1f, LampWarm, 4f, 0.2f); Glow(74f, Y0 + 1.3f, LampTeal, 3f);
        }
        Finish();
    }

    // STG-04 SEG-A production line / SEG-B inspection hall: smoking stacks beyond the factory wall, halls and watchtowers, machines on the floor.
    // row = the building row, (part in buildings/, segment-local centre x), world-fixed with floors on the roofs (DECISIONS 41).
    public static void Factory(Transform root, float width, float top, float camMin, float camMax, bool hall, (string part, float x)[] row)
    {
        Begin(root, width, top, 1f, camMin, camMax);
        walk = true;
        float ySky = Y0 + 5f;
        Sky("far/factory_sky", ySky);
        Skyline("far/factory_skyline", "far/smoke_stack_6f", 3f, null, 0f, ySky);
        Wall("interiors/factory_wall", Y0 - CompDrop, WallX, FyWall, DimWall, OWallFill, OWall, true,
            ("interiors/red_lamp_blink_2f", 1f), ("interiors/vat_bubbles_6f", 4f));
        Buildings(Y0, "buildings/wall_tile", row);
        if (hall) Rail();
        var p = Props();
        var belt = new[] { new Part("props/conveyor", 0, "props/conveyor_scroll_4f", 5f) };
        float cable = Y0 + 6f; // hangs from the top of the wall
        if (!hall)
        {
            Row(p, belt, Y0, 3f, 6.5f, OProp);                           // ends before the crates (was 3..10)
            Put(p, "props/crates_neon", null, 0f, 8f, Y0, OProp);        // steps onto the fence top (replaces CRATE-A1)
            Put(p, "props/robot_arm", null, 0f, 14f, Y0, OProp);
            Put(p, "props/cable_bundle", null, 0f, 20f, cable, OProp);
            Put(p, "props/crates_neon", null, 0f, 23.5f, Y0, OProp);
            Put(p, "props/robot_arm", null, 0f, 31f, Y0, OProp);
            Put(p, "props/server_cabinet", "props/server_cabinet_lights_4f", 3f, 38f, Y0, OProp);
            Put(p, "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 44f, Y0, OProp);
            // segment end (x 46..76) was bare
            Row(p, belt, Y0, 49f, 56f, OProp);
            Put(p, "props/robot_arm", null, 0f, 60f, Y0, OProp);
            Put(p, "props/cable_bundle", null, 0f, 64f, cable, OProp);
            Put(p, "props/crates_neon", null, 0f, 67f, Y0, OProp);
            Put(p, "props/server_cabinet", "props/server_cabinet_lights_4f", 3f, 73f, Y0, OProp);
        }
        else
        {
            Put(p, "props/server_cabinet", "props/server_cabinet_lights_4f", 3f, 6f, Y0, OProp);
            Put(p, "props/robot_arm", null, 0f, 12f, Y0, OProp);
            Row(p, belt, Y0, 20f, 23.5f, OProp);                         // after the crates (was 15..22)
            Put(p, "props/crates_neon", null, 0f, 18.5f, Y0, OProp);     // steps onto the fence top (replaces CRATE-B1; was x 33)
            Put(p, "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 27f, Y0, OProp);
            Put(p, "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 40f, Y0, OProp);
            Put(p, "props/robot_arm", null, 0f, 44f, Y0, OProp);
            // segment end (x 46..76) was bare
            Put(p, "props/server_cabinet", "props/server_cabinet_lights_4f", 3f, 50f, Y0, OProp);
            Row(p, belt, Y0, 54f, 61f, OProp);
            Put(p, "props/scanner_gate", "props/scanner_gate_beam_pulse_4f", 3f, 65f, Y0, OProp);
            Put(p, "props/cable_bundle", null, 0f, 68.5f, cable, OProp);
            Put(p, "props/robot_arm", null, 0f, 72f, Y0, OProp);
        }
        // spill lights: the scanner gates teal, the robot arms' hazard plates vermilion
        if (!hall)
        {
            Glow(14f, Y0 + 1.5f, LampRed, 4f); Glow(31f, Y0 + 1.5f, LampRed, 4f); Glow(44f, Y0 + 1.3f, LampTeal, 5f, 0.15f);
            Glow(60f, Y0 + 1.5f, LampRed, 4f); Glow(73f, Y0 + 1.2f, LampTeal, 3f);
        }
        else
        {
            Glow(27f, Y0 + 1.3f, LampTeal, 5f, 0.15f); Glow(40f, Y0 + 1.3f, LampTeal, 5f); Glow(44f, Y0 + 1.5f, LampRed, 4f);
            Glow(65f, Y0 + 1.3f, LampTeal, 5f); Glow(72f, Y0 + 1.5f, LampRed, 4f);
        }
        Finish();
    }

    // STG-03 hideout room (one screen): masonry far wall peeking above the interior wall (manifest composition), bulbs and CRTs, old machines and charms.
    public static void Hideout(Transform root, float width, float groundY)
    {
        Begin(root, width, SegHeight, groundY, 15f, 15f);
        float yIn = Y0 - CompDrop, yFar = yIn + FarRise;
        // ceiling (audit #9): the far wall's brick navy instead of its black top row, an iron beam across with hanging lanterns
        Ceiling(new Color32(0x12, 0x1A, 0x2D, 0xFF), yFar + 128f / Ppu);
        var top = Props("Background (ceiling props)");
        var beam = Load("buildings/rail_track");
        float beamTop = yFar + 128f / Ppu + 4.25f; // 13.5 at groundY 1
        if (beam != null) Strip(top, beam, -1f, Wd + 1f, beamTop, OFar);
        float hang = beamTop - (beam != null ? beam.rect.height / Ppu : 0f);
        foreach (float x in new[] { 4f, 14f, 25f }) Put(top, null, "props/lantern_hanji_sway_4f", 3f, x, hang, OProp);
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
        // the skyline sits just under the throne wall's top (audit #10): the palace silhouette and the red horizon show above the wall
        float yIn = Y0 - CompDrop, yFar = yIn + 196f / Ppu - 1f;
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
        R = root; Wd = width; Top = top; Y0 = groundY; CamMin = camMinX; CamMax = camMaxX; walk = false;
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

    static Transform Props(string name = "Background (props)") // gameplay plane: world-fixed, no parallax
    {
        var go = new GameObject(name).transform;
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

    // Spill light at a lit part (segment-local x, y), a light-only NeonSign via LightingSetup.BuildNeon (LIGHTING_DESIGN 3.1), lighting the backdrop too.
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

    // Flat colour above a plate that ends below the top of the segment.
    static void Ceiling(Color c, float yFrom)
    {
        var g = Layer("Background (ceiling)", 0f, FySkyline, DimSkyline);
        AddSquare(g, "Background (ceiling fill)", new Vector2(Wd * 0.5f, (yFrom + Top + 30f) * 0.5f), new Vector2(Wd + 4f, Top + 30f - yFrom), c, OCeiling);
    }

    // World-fixed building row (DECISIONS 41): low fence strip (wall_tile, repeats edge to edge) with the buildings placed by hand in front
    // (part in buildings/, centre x), lit like the props, one-way floors on their roofs (Tops) and on the fence top only in the gaps between
    // buildings (the fence is drawn behind them: a floor there would look like standing inside a wall). Ground-coloured fill under both.
    static void Buildings(float y, string fence, (string part, float x)[] row)
    {
        FillBelow("Background (ground fill)", 0f, DimProp, y, Edge(fence, false), OBldFill);
        var g = Props("Background (buildings)");
        Row(g, new[] { new Part(fence, 0) }, y, -1f, Wd + 1f, OFence);
        var spans = new List<(float l, float r)>();
        foreach (var (part, x) in row)
        {
            string file = "buildings/" + part;
            var s = Load(file);
            if (s == null) { Missing(file); continue; }
            BuildingFx.TryGetValue(part, out var fx);
            Put(g, file, fx.sheet, fx.fps, x, y, OBld);
            float hw = s.rect.width * 0.5f / Ppu, snap = (((int)s.rect.width & 1) == 1) ? 0.5f / Ppu : 0f;
            spans.Add((x + snap - hw, x + snap + hw));
        }
        var f = Load(fence);
        if (f == null) return;
        float from = 0f, fenceTop = y + f.rect.height / Ppu;
        foreach (var (l, r) in spans.OrderBy(sp => sp.l).Append((Wd, Wd)))
        {
            if (l - from >= 0.5f) BuildCollider(R, Ledge($"TOP fence x{from:0.##}-{l:0.##}", 0, from, l, fenceTop), false);
            from = Mathf.Max(from, r);
        }
    }

    // Elevated rail: pillars every second pillar width, the track strip on top of them (the pillar's top sits under the track).
    static void Rail()
    {
        var g = Layer("Background (rail)", BldX, FyBld, DimBld);
        Range(BldX, CamMin, CamMax, out float min, out float max);
        Row(g, new[] { new Part("buildings/rail_pillar", 174) }, Y0, min, max, ORail);
        Row(g, new[] { new Part("buildings/rail_track", 0) }, Y0 + 178f / Ppu, min, max, ORail + 2);
    }

    // World-fixed elevated rail on the gameplay rail ledges (audit #6): a rail_track strip whose top is each ledge top, pillar columns stacked from
    // below the street up to the track (one per ~10 u of track, none where a higher track's column already stands: it reads as holding both),
    // in front of the building row (so no roof pokes through a track), behind the street props. A ground-coloured strip in front of the pillars
    // hides their parts below the street.
    static void RailOnLedges((float x1, float x2, float top)[] ledges, string fence)
    {
        Sprite track = Load("buildings/rail_track"), pillar = Load("buildings/rail_pillar");
        if (track == null || pillar == null) { Missing("buildings/rail_track or rail_pillar"); return; }
        var g = Props("Background (rail)");
        float th = track.rect.height / Ppu, ph = pillar.rect.height / Ppu;
        var cols = new List<float>();
        foreach (var (x1, x2, top) in ledges.OrderByDescending(l => l.top))
        {
            Strip(g, track, x1, x2, top, ORailTrack);
            int n = Mathf.Max(1, Mathf.RoundToInt((x2 - x1) / 10f));
            for (int i = 0; i < n; i++)
            {
                float cx = Mathf.Round((x1 + (i + 0.5f) * (x2 - x1) / n) * Ppu) / Ppu;
                if (cols.Exists(c => Mathf.Abs(c - cx) < 2.5f)) continue;
                cols.Add(cx);
                for (float y = top - th; y > Y0; y -= ph) Sprite1(g, "Background (rail_pillar)", pillar, new Vector2(cx, y - ph), false, ORailPillar);
            }
        }
        AddSquare(g, "Background (ground front)", new Vector2(Wd * 0.5f, (Y0 - 10f) * 0.5f), new Vector2(Wd + 4f, Y0 + 10f), Edge(fence, false), OGroundFront);
    }

    // `s` tiled (SpriteDrawMode.Tiled) over [x1, x2] with its top edge at `top`, world-fixed under `parent`.
    static SpriteRenderer Strip(Transform parent, Sprite s, float x1, float x2, float top, int order)
    {
        float h = s.rect.height / Ppu;
        var t = Sprite1(parent, "Background (" + s.name + ")", s, Vector2.zero, false, order);
        t.localPosition = new Vector3((x1 + x2) * 0.5f, top - h, 0f); // bottom-centre pivot; no odd-width shift (the strip is x2 - x1 wide)
        var sr = t.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(x2 - x1, h);
        return sr;
    }

    // Art on a gameplay ledge (audit #4): `file` (in the parts folder) tiled over the ledge [x1, x2], its top on the ledge top. Visual only;
    // call after the backdrop kit, hide the ledge's placeholder (the collider stays). Lit like the props.
    public static void LedgeArt(Transform root, string file, float x1, float x2, float top)
    {
        var s = Load(file);
        if (s == null) { Missing(file); return; }
        Strip(root, s, x1, x2, top, OLedge).color = new Color(DimProp, DimProp, DimProp, 1f);
    }

    // Art for a gameplay platform from existing parts, replacing its placeholder bar / block (build the collider with visible = false; it is
    // unchanged). Named "Visual (...)" with orders > -100, so AssignSortingLayers puts it on World like the old placeholders (behind the characters).
    //  Building (tower)  = wall_tile facade rows from under a rail_track roof cap down to the segment floor.
    //  Solid (beam)      = rooftop walkway: rail_track cap + one wall_tile row (same bands as a tower roof it meets).
    //  OneWay (ledge)    = thin rail_track girder (reads as jump-through). Order -60: behind the billboard panels (-50).
    // pillarXs = rail_pillar columns (Background, pillarOrder) from the platform's underside down to `floor` (segment-local).
    public static void PlatformArt(Transform root, Box b, float floor = 0f, float[] pillarXs = null, int pillarOrder = ORailPillar)
    {
        Sprite cap = Load("buildings/rail_track"), wall = Load("buildings/wall_tile");
        if (cap == null || wall == null) { Missing("buildings/rail_track or wall_tile"); return; }
        float ch = cap.rect.height / Ppu, wh = wall.rect.height / Ppu;
        float under = b.kind == Kind.Building ? b.y1 : b.kind == Kind.Solid ? b.y2 - ch - wh : b.y2 - ch;
        // facade rows (the last one may run below the segment floor, out of the camera's range)
        if (b.kind != Kind.OneWay)
            for (float t = b.y2 - ch; t > under + 0.001f; t -= wh) Visual(root, wall, b.x1, b.x2, t, 0, DimFacade);
        Visual(root, cap, b.x1, b.x2, b.y2, b.kind == Kind.OneWay ? -60 : 1, DimProp);
        if (pillarXs != null) Pillars(root, under, floor, pillarXs, pillarOrder);
    }

    const float DimFacade = 0.8f;

    static void Visual(Transform root, Sprite s, float x1, float x2, float top, int order, float dim)
    {
        var sr = Strip(root, s, x1, x2, top, order);
        sr.gameObject.name = "Visual (" + s.name + ")";
        sr.color = new Color(dim, dim, dim, 1f);
    }

    // rail_pillar columns at xs, base on `floor`, pieces stacked up; the top piece is lifted so its capital meets `top` (it overlaps the one
    // below and draws in front of it). A span shorter than one piece pokes ph - span (< 0.5 u here) up into the platform, which hides it.
    static void Pillars(Transform root, float top, float floor, float[] xs, int order)
    {
        var p = Load("buildings/rail_pillar");
        if (p == null) { Missing("buildings/rail_pillar"); return; }
        float ph = p.rect.height / Ppu, span = top - floor;
        if (span <= 0f) return;
        int n = Mathf.CeilToInt(span / ph);
        var dim = new Color(DimProp, DimProp, DimProp, 1f);
        foreach (float x in xs)
            for (int i = 0; i < n; i++)
            {
                float y = i == n - 1 ? Mathf.Max(floor, top - ph) : floor + i * ph;
                var t = Sprite1(root, "Background (rail_pillar)", p, new Vector2(x, y), false, order);
                t.localPosition += new Vector3(0f, 0f, -0.01f * i); // upper pieces in front (same sorting order: nearer z wins)
                t.GetComponent<SpriteRenderer>().color = dim;
            }
    }

    // Exit door / gate art at a transition trigger (audit #8): `file` (+ looping overlay `sheet`) bottom-centre at segment-local (x, y),
    // world-fixed, behind the characters, no collider. x on the 1/32 grid.
    public static void ExitArt(Transform root, string file, string sheet, float fps, float x, float y)
    {
        var s = Load(file);
        if (s == null) { Missing(file); return; }
        var dim = new Color(DimProp, DimProp, DimProp, 1f);
        Sprite1(root, "Background (exit " + Path.GetFileName(file) + ")", s, new Vector2(x, y), false, OProp).GetComponent<SpriteRenderer>().color = dim;
        if (sheet == null) return;
        var frames = Frames(sheet, Parts);
        if (frames == null) { Missing(sheet); return; }
        var o = Sprite1(root, "Background (exit " + Path.GetFileName(sheet) + ")", frames[0], new Vector2(x, y), false, OProp + 1);
        o.GetComponent<SpriteRenderer>().color = dim;
        Loop(o.gameObject, frames, fps);
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
            if (walk && parent.GetComponent<Parallax>() == null && Tops.TryGetValue(file, out var spans)) TopColliders(R, file, s, x, y, spans);
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

    // Most common opaque colour of the first / last pixel row (editor only: the PNGs are not readable at runtime). Pure black (outline) pixels
    // are skipped, and a row with nothing else moves one row inward (audit #11: the black outline rows made near-black fill bands).
    static Color32 Edge(string file, bool top)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(Parts + file + ".png"));
        var count = new Dictionary<int, int>();
        int best = 0x14161E, bestN = 0;
        for (int i = 0; i < tex.height && bestN == 0; i++)
        {
            int row = top ? tex.height - 1 - i : i;
            for (int x = 0; x < tex.width; x++)
            {
                Color32 c = tex.GetPixel(x, row);
                int key = (c.r << 16) | (c.g << 8) | c.b;
                if (c.a != 255 || key == 0) continue;
                count.TryGetValue(key, out int n);
                count[key] = ++n;
                if (n > bestN) { bestN = n; best = key; }
            }
        }
        Object.DestroyImmediate(tex);
        return new Color32((byte)(best >> 16), (byte)(best >> 8), (byte)best, 255);
    }
}
