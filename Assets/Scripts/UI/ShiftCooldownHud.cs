using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Radial cooldowns for the Shift dodge-counter (DashRules.ShiftCooldown, DECISIONS 13) and the RMB issen (DashRules.IssenCooldown, DECISIONS 26). Self-bootstrapping: one HUD per scene that has a
// PlayerController (none on Title). Dark overlay shrinks clockwise to empty; when ready a frame pulses once (scale 1 -> 1.6, fade, 0.35 s).
public class ShiftCooldownHud : MonoBehaviour
{
    const float Size = 32f;       // reference px
    const float PulseTime = 0.35f;
    static readonly Color Dark = new Color(0.04f, 0.06f, 0.12f, 0.75f);
    static Sprite whiteSprite;

    // One cooldown slot (frame, key icon + label, radial dark overlay, ready pulse).
    class Slot
    {
        public Image overlay;
        public RectTransform pulse;
        public CanvasGroup pulseGroup;
        public float pulseT = PulseTime; // >= PulseTime means idle
        public bool wasCooling;
        public System.Func<float> remaining;
        public float total;
    }

    PlayerController player;
    Slot shift, issen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        var p = FindFirstObjectByType<PlayerController>();
        if (p == null) return;
        new GameObject("ShiftCooldownHud").AddComponent<ShiftCooldownHud>().Build(p);
    }

    void Build(PlayerController p)
    {
        player = p;
        if (whiteSprite == null)
        {
            var tex = new Texture2D(4, 4);
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        // Bottom-left, stacked above the lucky dice HUD (die 16..64 px); the face HUD is not in this corner. RMB issen (DECISIONS 26) to its right.
        var canvas = UiKit.CreateCanvas("ShiftCooldownCanvas", transform, 38);
        shift = MakeSlot(canvas, new Vector2(16f + Size * 0.5f, 80f + Size * 0.5f), "ctrl_key_shift", "SHIFT", () => player.DashCooldownRemaining, DashRules.ShiftCooldown);
        issen = MakeSlot(canvas, new Vector2(16f + Size * 1.5f + 8f, 80f + Size * 0.5f), "ctrl_mouse_right", "RMB", () => player.IssenCooldownRemaining, DashRules.IssenCooldown);
    }

    static Slot MakeSlot(Canvas canvas, Vector2 pos, string iconName, string key, System.Func<float> remaining, float total)
    {
        var s = new Slot { remaining = remaining, total = total };
        var t = UiTheme.Current;
        var root = UiKit.Rect(key, canvas.transform, Vector2.zero, pos, new Vector2(Size, Size));
        UiKit.FilledImage(root, t.border);
        var fill = UiKit.Stretch("Fill", root, t.frameWidth);
        var bg = UiKit.FilledImage(fill, t.panelFill);
        Sprite icon = DraftHud.Ui(iconName);
        if (icon != null) { bg.sprite = icon; bg.color = Color.white; bg.preserveAspect = true; }
        if (icon == null || key == "SHIFT") UiKit.Label("Key", fill, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Size, Size), key, key, 9, Color.white); // the mouse icon needs no letters

        s.overlay = UiKit.FilledImage(UiKit.Stretch("Cooldown", fill), Dark);
        s.overlay.sprite = whiteSprite;
        s.overlay.type = Image.Type.Filled;
        s.overlay.fillMethod = Image.FillMethod.Radial360;
        s.overlay.fillOrigin = (int)Image.Origin360.Top;
        s.overlay.fillClockwise = true;
        s.overlay.fillAmount = 0f;

        // Pulse ring: four thin edges under a scaled, fading group.
        s.pulse = UiKit.Rect("Pulse", canvas.transform, Vector2.zero, root.anchoredPosition, new Vector2(Size, Size));
        s.pulseGroup = s.pulse.gameObject.AddComponent<CanvasGroup>();
        s.pulseGroup.alpha = 0f;
        s.pulseGroup.blocksRaycasts = false;
        const float e = 2f;
        Edge(s.pulse, new Vector2(0.5f, 1f), new Vector2(Size, e));
        Edge(s.pulse, new Vector2(0.5f, 0f), new Vector2(Size, e));
        Edge(s.pulse, new Vector2(0f, 0.5f), new Vector2(e, Size));
        Edge(s.pulse, new Vector2(1f, 0.5f), new Vector2(e, Size));
        return s;
    }

    static void Edge(RectTransform parent, Vector2 anchor, Vector2 size)
    {
        var rt = UiKit.Rect("Edge", parent, anchor, Vector2.zero, size);
        UiKit.FilledImage(rt, UiTheme.Current.selectedBorder);
    }

    void Update()
    {
        if (player == null) { Destroy(gameObject); return; }
        Tick(shift);
        Tick(issen);
    }

    static void Tick(Slot s)
    {
        float remaining = s.remaining(); // same clock as the player's cooldowns (Time.time)
        bool cooling = remaining > 0f;
        s.overlay.fillAmount = cooling ? Mathf.Clamp01(remaining / s.total) : 0f;
        if (s.wasCooling && !cooling) s.pulseT = 0f;
        s.wasCooling = cooling;

        if (s.pulseT < PulseTime)
        {
            s.pulseT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(s.pulseT / PulseTime);
            s.pulse.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, k);
            s.pulseGroup.alpha = 1f - k;
        }
    }
}
