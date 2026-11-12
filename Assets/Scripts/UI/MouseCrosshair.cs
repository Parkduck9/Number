using UnityEngine;
using UnityEngine.InputSystem;

// DECISIONS 12: pixel crosshair at the mouse + lock-on corner brackets around the lunge target (docs/design/impl/dash-attack.md).
// Created at runtime by PlayerController.Awake as its own object (not a child: the player flips / moves).
// The OS cursor is hidden during play and shown in dialogue / pause. Runs after the camera follow so it never lags a frame.
[DefaultExecutionOrder(1000)]
public class MouseCrosshair : MonoBehaviour
{
    const float Ppu = SpriteFx.Ppu;
    static readonly Color Ink = new Color(0.08f, 0.08f, 0.12f);              // 1 px outline (readable on any background)
    static readonly Color IdleTint = new Color(0.93f, 0.95f, 0.97f);         // off-white
    static readonly Color LockTint = new Color(0.298f, 0.909f, 0.847f);      // hero cyan #4CE8D8 = "the lunge will hit this"

    static readonly Color IssenTint = new Color32(0xF2, 0xC9, 0x4C, 0xFF);   // talisman yellow #F2C94C = "Shift would issen right now"

    PlayerController player;
    bool bracketsPrecolored;
    SpriteRenderer cross;
    Sprite idleArt, lockArt;
    readonly SpriteRenderer[] brackets = new SpriteRenderer[4];

    public static void Create(PlayerController player)
    {
        var go = new GameObject("MouseCrosshair");
        var c = go.AddComponent<MouseCrosshair>();
        c.player = player;
        // Kit art (16x16 crosshair, 8x8 bracket) is pre-colored: white renderer + sprite swap. Missing art = the code-baked sprites + tint.
        c.idleArt = DraftHud.Ui("crosshair_idle"); c.lockArt = DraftHud.Ui("crosshair_lock");
        Sprite bracketArt = DraftHud.Ui("lock_bracket");
        if (c.idleArt == null || c.lockArt == null) c.idleArt = c.lockArt = null;
        c.cross = MakeRenderer(go.transform, c.idleArt != null ? c.idleArt : CrossSprite());
        c.bracketsPrecolored = bracketArt != null;
        Sprite corner = bracketArt != null ? bracketArt : CornerSprite();
        for (int i = 0; i < 4; i++)
        {
            var b = new GameObject("LockBracket" + i);
            b.transform.SetParent(go.transform, false);
            // corner art is the top-left L; mirror it for the other corners (scale flips around the corner pivot)
            b.transform.localScale = new Vector3(i % 2 == 0 ? 1f : -1f, i < 2 ? 1f : -1f, 1f);
            c.brackets[i] = MakeRenderer(b.transform, corner);
            c.brackets[i].color = bracketArt != null ? Color.white : LockTint;
            c.brackets[i].enabled = false;
        }
    }

    // Mouse position in world space. false = no mouse / camera (callers fall back to the facing direction).
    public static bool MouseWorld(out Vector2 world)
    {
        world = default;
        Camera cam = Camera.main;
        if (Mouse.current == null || cam == null) return false;
        Vector3 s = Mouse.current.position.ReadValue();
        s.z = -cam.transform.position.z;
        world = cam.ScreenToWorldPoint(s);
        return true;
    }

    void LateUpdate()
    {
        bool ui = GameFlow.InputLocked || PauseMenu.IsOpen;
        if (Cursor.visible != ui) Cursor.visible = ui; // set only on change: an OS cursor call every frame
        Vector2 mouse = default;
        bool show = !ui && player != null && player.isActiveAndEnabled && MouseWorld(out mouse);
        cross.enabled = show;
        if (!show) { SetBrackets(false); return; }
        transform.position = new Vector3((Mathf.Floor(mouse.x * Ppu) + 0.5f) / Ppu, (Mathf.Floor(mouse.y * Ppu) + 0.5f) / Ppu, 0f); // odd-sized sprite: centre on a pixel centre

        Vector2 aim = mouse - (Vector2)player.transform.position;
        Collider2D col = null;
        bool locked = aim.sqrMagnitude > 0.0001f && player.CurrentState != PlayerController.State.Dead
            && player.FindLungeTarget(aim.normalized, out col) != null;
        bool issen = player.CanIssenNow(); // once per frame: Shift would issen -> talisman yellow cue
        if (idleArt != null) cross.sprite = locked ? lockArt : idleArt;
        else cross.color = locked ? LockTint : IdleTint;
        if (issen) cross.color = IssenTint;
        else if (idleArt != null) cross.color = Color.white;
        Color bracketBase = bracketsPrecolored ? Color.white : LockTint;
        foreach (SpriteRenderer br in brackets) br.color = issen ? IssenTint : bracketBase;
        SetBrackets(locked);
        if (!locked) return;
        Bounds b = col.bounds;
        float pad = 2f / Ppu;
        float l = Snap(b.min.x - pad), r = Snap(b.max.x + pad), bot = Snap(b.min.y - pad), top = Snap(b.max.y + pad);
        brackets[0].transform.position = new Vector3(l, top, 0f);
        brackets[1].transform.position = new Vector3(r, top, 0f);
        brackets[2].transform.position = new Vector3(l, bot, 0f);
        brackets[3].transform.position = new Vector3(r, bot, 0f);
    }

    static float Snap(float v) => Mathf.Round(v * Ppu) / Ppu;

    void SetBrackets(bool on)
    {
        foreach (SpriteRenderer b in brackets) b.enabled = on;
    }

    void OnDisable() { Cursor.visible = true; }
    void OnDestroy() { Cursor.visible = true; }

    static SpriteRenderer MakeRenderer(Transform t, Sprite s)
    {
        var sr = t.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingLayerName = "FX";
        sr.sortingOrder = 1000;
        return sr;
    }

    // 11 x 11 plus with a hollow centre: 3 px arms, 1 px ink outline. White core so the renderer colour tints it.
    static Sprite CrossSprite()
    {
        const int n = 11;
        var core = new bool[n, n];
        for (int i = 1; i <= 3; i++)
        {
            core[5, i] = core[5, n - 1 - i] = true;
            core[i, 5] = core[n - 1 - i, 5] = true;
        }
        return Bake(core, n, new Vector2(0.5f, 0.5f), "Crosshair");
    }

    // 5 x 5 top-left corner L (3 px arms) with outline; pivot at the top-left corner.
    static Sprite CornerSprite()
    {
        const int n = 5;
        var core = new bool[n, n];
        for (int i = 1; i <= 3; i++) { core[i, 3] = true; core[1, 4 - i] = true; }
        return Bake(core, n, new Vector2(0f, 1f), "LockBracket");
    }

    static Sprite Bake(bool[,] core, int n, Vector2 pivot, string name)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                if (core[x, y]) { px[y * n + x] = Color.white; continue; }
                bool edge = (x > 0 && core[x - 1, y]) || (x < n - 1 && core[x + 1, y]) || (y > 0 && core[x, y - 1]) || (y < n - 1 && core[x, y + 1]);
                px[y * n + x] = edge ? Ink : Color.clear;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, n, n), pivot, Ppu, 0, SpriteMeshType.FullRect);
    }
}
