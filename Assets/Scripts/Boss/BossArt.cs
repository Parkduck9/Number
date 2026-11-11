using UnityEngine;

// TEMPORARY code-drawn art for the boss-v2 fused background boss (docs/design/impl/boss-v2.md) until Codex art drops into
// BossFight's override slots: dark navy blocky body (+ two crack overlays), bug-shell arm (dark red), elite blade arm (crimson + gold,
// straight sword), chest hatch half + its glowing inside, the cyan hologram window (redrawn only when its icon / number changes),
// ending machine props, one swarm bug. Point filter, 32 px per unit, pivots on pixel corners. Built once per play session.
public static class BossArt
{
    public const float Ppu = 32f;
    public const int Normal = 0, Lit = 1, Flash = 2; // variant index of the part sprites
    public const int IconBug = 0, IconBlade = 1, IconAim = 2;

    public static Sprite Body, BodyFlash, Crack1, Crack2, HatchInside, Bug, Holo;
    public static readonly Sprite[] BugArm = new Sprite[3], BladeArm = new Sprite[3], Hatch = new Sprite[3];
    public static readonly Sprite[] Props = new Sprite[3]; // rack, screen, cables

    static Texture2D holoTex;
    static Color32[] holoBuf;
    static int holoIcon = -2, holoN = -2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Body = null; holoTex = null; holoBuf = null; holoIcon = holoN = -2; } // Domain Reload is off

    static Color32 C(int rgb, int a = 255) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)a);

    // palette
    static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    static readonly Color32 Navy0 = C(0x0A0E1A), Navy1 = C(0x121A2C), Navy2 = C(0x1A2440), Navy3 = C(0x26304F);
    static readonly Color32 EyeDim = C(0x6A1A24), EyeHot = C(0xFF3B3B), Coolant = C(0x2A8C9A), Teal = C(0x3AA895);
    static readonly Color32 Gold = C(0xD9A64A), GoldHi = C(0xF2C94C), Pale = C(0xFFF1A0), Cyan = C(0x4CE0FF), CyanHi = C(0x8FF4FF);

    public static void Ensure()
    {
        if (Body != null) return;
        BuildBody();
        Variants(BugArm, BugArmPix(false), BugArmPix(true), new Vector2(0.5f, 0f));
        Variants(BladeArm, BladePix(false), BladePix(true), new Vector2(0.5f, 0f));
        Variants(Hatch, HatchPix(false), HatchPix(true), new Vector2(0.5f, 0.5f));
        HatchInside = HatchInsidePix().Make(new Vector2(0.5f, 0.5f));
        // Drawn props (Resources/BossArt/prop_*.png, bottom-centre pivot) win; the code drawings stay as the fallback.
        Props[0] = Resources.Load<Sprite>("BossArt/prop_rack") ?? RackPix().Make(new Vector2(0.5f, 0f));
        Props[1] = Resources.Load<Sprite>("BossArt/prop_screen") ?? ScreenPix().Make(new Vector2(0.5f, 0f));
        Props[2] = Resources.Load<Sprite>("BossArt/prop_cables") ?? CablesPix().Make(new Vector2(0.5f, 0f));
        var bug = new Pix(3, 3); // . R .  /  R R R  /  r . r
        bug.Dot(1, 2, C(0xD8343A)); bug.Rect(0, 1, 3, 1, C(0xD8343A)); bug.Dot(0, 0, C(0x7A1018)); bug.Dot(2, 0, C(0x7A1018));
        Bug = bug.Make(new Vector2(0f, 0f));
        holoTex = null;
        holoIcon = holoN = -2;
        holoBuf = new Color32[HoloW * HoloH];
        holoTex = new Texture2D(HoloW, HoloH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        Holo = Sprite.Create(holoTex, new Rect(0, 0, HoloW, HoloH), new Vector2(0.5f, 0.5f), Ppu, 0, SpriteMeshType.FullRect);
        DrawHolo(-1, -1);
    }

    static void Variants(Sprite[] dst, Pix normal, Pix lit, Vector2 pivot)
    {
        dst[Normal] = normal.Make(pivot);
        dst[Lit] = lit.Make(pivot);
        dst[Flash] = normal.Lerp(new Color32(255, 255, 255, 255), 0.6f).Make(pivot);
    }

    // ------------------------------------------------------------------ body (24 x 14 u, pivot bottom-left)
    public const int BodyW = 768, BodyH = 448;

    static void BuildBody()
    {
        var r = new System.Random(23);
        var c = new Pix(BodyW, BodyH);
        c.Rect(0, 0, 176, 224, Navy1); c.Rect(32, 224, 144, 48, Navy1);       // left server stack
        c.Rect(592, 0, 176, 224, Navy1); c.Rect(592, 224, 144, 48, Navy1);    // right server stack
        c.Rect(160, 0, 448, 352, Navy1);                                      // torso
        c.Rect(64, 288, 224, 112, Navy1); c.Rect(480, 288, 224, 112, Navy1);  // shoulders
        c.Rect(288, 352, 192, 96, Navy1); c.Rect(352, 432, 64, 16, Clear);     // crown with a notch
        c.Rect(160, 0, 448, 96, Navy2);                                       // lower torso band
        c.Outline(Navy0, 2);
        for (int y = 40; y < BodyH - 4; y += 40)                              // panel seams + a lit edge under each
            for (int x = 0; x < BodyW; x++)
                if (c.Inside(x, y, 3)) { c.Dot(x, y, Navy0); c.Dot(x, y - 1, Navy3); }
        for (int x = 192; x < 600; x += 64)
            for (int y = 0; y < BodyH; y++)
                if (c.Inside(x, y, 3)) c.Dot(x, y, Navy0);
        foreach (int sx in new[] { 0, 592 })                                  // vents + server LEDs (dim)
            for (int y = 12; y < 208; y += 16)
            {
                c.Rect(sx + 14, y, 148, 4, Navy0);
                if (r.Next(3) == 0) c.Rect(sx + 18 + r.Next(130), y + 6, 2, 1, r.Next(4) == 0 ? EyeDim : C(0x1E5A58));
            }
        foreach (int cx in new[] { 200, 252, 318, 446, 512, 566 })            // cables down the torso (vein-like)
        {
            int x = cx;
            for (int y = 340; y >= 0; y -= 8)
            {
                if (r.Next(3) == 0) x += r.Next(3) - 1;
                c.Rect(x, y, 5, 8, Navy0); c.Rect(x + 1, y, 1, 8, C(0x1E2A44));
            }
        }
        for (int i = 0; i < 4; i++)                                            // coolant drips (no blood: JAM_RULES)
        {
            int x = 180 + r.Next(400), y = 40 * (2 + r.Next(6)) - 2;
            for (int k = 0, len = 10 + r.Next(30); k < len; k++) if (c.Inside(x, y - k, 1)) c.Dot(x, y - k, Coolant);
        }
        c.Rect(376, 408, 6, 3, EyeDim); c.Rect(386, 408, 6, 3, EyeDim); c.Dot(378, 409, EyeHot); c.Dot(388, 409, EyeHot); // eyes in the dark
        for (int i = 0; i < 8; i++)
        {
            int x = r.Next(BodyW), y = 120 + r.Next(300);
            if (c.Inside(x, y, 4)) { c.Rect(x, y, 2, 2, EyeDim); if (i % 3 == 0) c.Dot(x, y, EyeHot); }
        }
        c.Rect(304, 136, 160, 80, Navy0); c.Rect(310, 142, 148, 68, C(0x05060A)); // chest hatch recess (hatch centre 384,176)
        c.Rect(304, 262, 160, 100, Navy0);                                       // hologram mount (window centre 384,312)
        Body = c.Make(Vector2.zero);
        BodyFlash = c.Lerp(new Color32(255, 255, 255, 255), 0.35f).Make(Vector2.zero);
        Crack1 = Cracks(c, r, 7).Make(Vector2.zero);
        Crack2 = Cracks(c, r, 10).Make(Vector2.zero);
    }

    static Pix Cracks(Pix body, System.Random r, int count)
    {
        var c = new Pix(BodyW, BodyH);
        for (int i = 0; i < count; i++)
        {
            int x = 120 + r.Next(528), y = 60 + r.Next(360), dx = r.Next(2) == 0 ? -1 : 1;
            for (int k = 0, len = 40 + r.Next(50); k < len; k++)
            {
                if (!body.Inside(x, y, 2)) break;
                c.Dot(x, y, C(0x03040A)); c.Dot(x + 1, y, Navy0);
                if (k % 9 == 4) c.Dot(x - 1, y, Coolant);
                y -= 1;
                if (r.Next(3) == 0) x += dx;
                if (r.Next(12) == 0) dx = -dx;
            }
        }
        return c;
    }

    // ------------------------------------------------------------------ bug-shell arm (3 x 10 u, pivot bottom-centre = the palm)
    static Pix BugArmPix(bool lit)
    {
        Color32 o = C(0x2A0A0E), plate = lit ? C(0x8A2A26) : C(0x4E1218), hi = lit ? C(0xB84A3A) : C(0x7A2420), dark = C(0x300A0E);
        var c = new Pix(96, 320);
        for (int y = 52, i = 0; y < 320; y += 34, i++)
        {
            int w = i % 2 == 0 ? 64 : 72, x0 = 48 - w / 2;
            c.Rect(x0, y, w, 30, plate); c.Rect(x0 + 2, y + 25, w - 4, 3, hi); c.Rect(x0 + 4, y + 4, w - 8, 2, dark);
            c.Rect(x0 + 10, y + 12, 3, 3, C(0x1A0608)); c.Rect(x0 + w - 13, y + 14, 3, 3, C(0x1A0608)); // dead bug eyes
            for (int k = 0; k < 4; k++) { c.Dot(x0 - 1 - k, y + 18 - k * 2, o); c.Dot(x0 + w + k, y + 18 - k * 2, o); } // bug legs
        }
        c.Rect(4, 10, 88, 44, plate); c.Rect(6, 50, 84, 3, hi);                 // palm
        for (int k = 0; k < 3; k++) c.Rect(8 + k * 34, 0, 12, 12, plate);      // claws
        c.Rect(28, 18, 40, 20, lit ? C(0xFF4A3A) : C(0x120406));               // palm opening (glows when lit)
        if (lit) c.Rect(38, 23, 20, 10, C(0xFFB0A0));
        c.Outline(o, 1);
        return c;
    }

    // ------------------------------------------------------------------ elite blade arm (2.5 x 11 u, pivot bottom-centre = the tip)
    static Pix BladePix(bool lit)
    {
        Color32 o = C(0x3E0A14), red = lit ? C(0xB8283C) : C(0x7A1626), redHi = lit ? C(0xE04050) : C(0xA02234);
        Color32 gold = lit ? GoldHi : Gold, goldHi = lit ? Pale : GoldHi, steel = lit ? C(0xC8D0DC) : C(0x9AA4B4), edge = lit ? C(0xFFFFFF) : C(0xDDE4EE);
        var c = new Pix(80, 352);
        for (int y = 0; y < 16; y++) c.Rect(40 - 1 - y * 10 / 16, y, 2 + y * 20 / 16, 1, steel); // tip
        c.Rect(30, 16, 20, 146, steel); c.Rect(30, 16, 2, 146, edge); c.Rect(48, 16, 2, 146, edge);
        c.Rect(38, 24, 4, 128, lit ? C(0xFF3B3B) : C(0x5A6272));             // fuller
        c.Rect(8, 162, 64, 14, gold); c.Rect(8, 174, 64, 2, goldHi);            // crossguard
        c.Rect(16, 176, 48, 32, red); c.Rect(20, 200, 40, 3, redHi);            // fist
        c.Rect(12, 208, 56, 144, red); c.Rect(12, 208, 3, 144, redHi);          // forearm
        for (int y = 224; y < 352; y += 40) { c.Rect(12, y, 56, 4, gold); c.Rect(12, y + 3, 56, 1, goldHi); }
        c.Rect(36, 230, 8, 8, gold); c.Rect(36, 310, 8, 8, gold);
        c.Outline(o, 1);
        return c;
    }

    // ------------------------------------------------------------------ chest hatch half (2 x 2 u, centre pivot; the right half is flipX)
    static Pix HatchPix(bool lit)
    {
        var c = new Pix(64, 64);
        c.Rect(0, 0, 64, 64, lit ? GoldHi : C(0x0E1220));                      // rib gaps (warning light leaks when lit)
        for (int y = 2; y < 62; y += 12) { c.Rect(0, y, 64, 8, C(0x1E2640)); c.Rect(0, y + 7, 64, 1, C(0x2E3A5C)); c.Rect(54, y + 2, 3, 3, Gold); }
        c.Outline(Navy0, 1);
        return c;
    }

    static Pix HatchInsidePix()
    {
        var c = new Pix(128, 64);
        c.Rect(0, 0, 128, 64, C(0x05060A));
        c.Rect(24, 12, 80, 40, C(0x3A2A08)); c.Rect(40, 20, 48, 24, C(0x8A6410)); c.Rect(52, 26, 24, 12, GoldHi); c.Rect(58, 29, 12, 6, Pale);
        return c;
    }

    // ------------------------------------------------------------------ ending props (pivot bottom-centre, stand on the floor)
    static Pix RackPix()
    {
        var r = new System.Random(5);
        var c = new Pix(48, 96);
        c.Rect(0, 0, 48, 96, C(0x1A2030));
        for (int y = 6; y < 90; y += 12)
        {
            c.Rect(3, y, 42, 1, C(0x10141E));
            c.Rect(6, y + 4, 2, 1, r.Next(3) == 0 ? EyeHot : r.Next(2) == 0 ? Teal : GoldHi);
            c.Rect(12, y + 4, 28, 3, C(0x0C1018));
        }
        c.Rect(0, 90, 48, 6, C(0x262E44));
        c.Outline(Navy0, 1);
        return c;
    }

    static Pix ScreenPix()
    {
        var r = new System.Random(9);
        var c = new Pix(64, 80);
        c.Rect(28, 0, 8, 40, C(0x1A2030)); c.Rect(12, 0, 40, 6, C(0x1A2030));
        c.Rect(0, 40, 64, 40, C(0x1A2030)); c.Rect(4, 44, 56, 32, C(0x0B2A3A));
        for (int y = 48; y < 72; y += 4) c.Rect(8, y, 6 + r.Next(30), 1, r.Next(4) == 0 ? EyeHot : Cyan);
        c.Outline(Navy0, 1);
        return c;
    }

    static Pix CablesPix()
    {
        var r = new System.Random(13);
        var c = new Pix(80, 40);
        Color32[] cols = { C(0x0E1220), C(0x1E2A44), C(0x6A1A24), C(0x0E1220), Coolant, C(0x1E2A44) };
        for (int i = 0; i < cols.Length; i++)
        {
            int y = 4 + r.Next(26);
            for (int x = 2; x < 78; x += 2) { if (r.Next(3) == 0) y = Mathf.Clamp(y + r.Next(5) - 2, 2, 34); c.Rect(x, y, 2, 4, cols[i]); }
        }
        c.Rect(0, 0, 80, 4, C(0x1A2030));
        c.Outline(Navy0, 1);
        return c;
    }

    // ------------------------------------------------------------------ hologram window (4.5 x 2.75 u): icon + number N only, no face
    public const int HoloW = 144, HoloH = 88;

    static readonly string[] Icons = // 7x7, top row first
    {
        ".#...#...#.#...#####.##.#.##.#####.#.###.#.#...#.", // bug
        "...#.....###..#######..###....###....###.....#...", // straight sword, point down
        "...#....##.##..#...#.###.###.#...#..##.##....#...", // aim
    };
    static readonly string[] Digits = // 3x5, top row first
    {
        "####.##.##.####", ".#.##..#..#.###", "###..#####..###", "###..####..####", "#.##.####..#..#",
        "####..###..####", "####..####.####", "###..#..#..#..#", "####.#####.####", "####.####..####",
    };

    // icon -1 / n -1 = blank window. Rewrites the texture only when something changed.
    public static void DrawHolo(int icon, int n)
    {
        if (holoTex == null || (icon == holoIcon && n == holoN)) return;
        holoIcon = icon; holoN = n;
        Color32 fill = C(0x0B2A3A, 190), scan = C(0x0F3A4E, 200);
        for (int y = 0; y < HoloH; y++)
            for (int x = 0; x < HoloW; x++)
            {
                bool border = x < 2 || y < 2 || x >= HoloW - 2 || y >= HoloH - 2;
                bool corner = (x < 4 || x >= HoloW - 4) && (y < 4 || y >= HoloH - 4);
                holoBuf[y * HoloW + x] = corner ? Clear : border ? Cyan : y % 3 == 0 ? scan : fill;
            }
        if (icon >= 0) Glyph(Icons[icon], 7, 7, 5, 18, 26, CyanHi);
        if (n >= 0) Glyph(Digits[n % 10], 3, 5, 9, 86, 22, CyanHi);
        holoTex.SetPixels32(holoBuf);
        holoTex.Apply();
    }

    static void Glyph(string bits, int w, int h, int scale, int x0, int y0, Color32 col)
    {
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                if (bits[j * w + i] != '#') continue;
                int py = y0 + (h - 1 - j) * scale, px = x0 + i * scale;
                for (int yy = 0; yy < scale; yy++)
                    for (int xx = 0; xx < scale; xx++) holoBuf[(py + yy) * HoloW + px + xx] = col;
            }
    }

    // ------------------------------------------------------------------ tiny pixel canvas
    sealed class Pix
    {
        readonly int w, h;
        readonly Color32[] p;

        public Pix(int w, int h) { this.w = w; this.h = h; p = new Color32[w * h]; }

        public void Dot(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) p[y * w + x] = c; }
        public void Rect(int x, int y, int rw, int rh, Color32 c)
        {
            for (int j = Mathf.Max(0, y); j < Mathf.Min(h, y + rh); j++)
                for (int i = Mathf.Max(0, x); i < Mathf.Min(w, x + rw); i++) p[j * w + i] = c;
        }
        bool Has(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && p[y * w + x].a > 0;
        // Opaque with an opaque square of radius m around it (keeps details off the silhouette outline).
        public bool Inside(int x, int y, int m) => Has(x - m, y) && Has(x + m, y) && Has(x, y - m) && Has(x, y + m);

        // Opaque pixels within `thick` px of a clear one become c.
        public void Outline(Color32 c, int thick)
        {
            var src = (Color32[])p.Clone();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (src[y * w + x].a == 0) continue;
                    for (int k = 1; k <= thick; k++)
                        if (!SrcHas(src, x - k, y) || !SrcHas(src, x + k, y) || !SrcHas(src, x, y - k) || !SrcHas(src, x, y + k)) { p[y * w + x] = c; break; }
                }
        }
        bool SrcHas(Color32[] s, int x, int y) => x >= 0 && y >= 0 && x < w && y < h && s[y * w + x].a > 0;

        public Pix Lerp(Color32 to, float k)
        {
            var o = new Pix(w, h);
            for (int i = 0; i < p.Length; i++) { Color32 c = Color32.Lerp(p[i], to, k); c.a = p[i].a; o.p[i] = c; }
            return o;
        }

        public Sprite Make(Vector2 pivot)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(p);
            tex.Apply();
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, w, h), pivot, Ppu, 0, SpriteMeshType.FullRect);
        }
    }
}
