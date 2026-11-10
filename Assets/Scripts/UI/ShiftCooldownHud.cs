using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Radial cooldown for the Shift dodge-counter (DashRules.ShiftCooldown, DECISIONS 13). Self-bootstrapping: one HUD per scene that has a
// PlayerController (none on Title). Dark overlay shrinks clockwise to empty; when ready a frame pulses once (scale 1 -> 1.6, fade, 0.35 s).
public class ShiftCooldownHud : MonoBehaviour
{
    const float Size = 32f;       // reference px
    const float PulseTime = 0.35f;
    static readonly Color Dark = new Color(0.04f, 0.06f, 0.12f, 0.75f);
    static Sprite whiteSprite;

    PlayerController player;
    Image overlay;
    RectTransform pulse;
    CanvasGroup pulseGroup;
    float pulseT = PulseTime; // >= PulseTime means idle
    bool wasCooling;

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

        // Bottom-left, stacked above the lucky dice HUD (die 16..64 px); the face HUD is not in this corner.
        var canvas = UiKit.CreateCanvas("ShiftCooldownCanvas", transform, 38);
        var t = UiTheme.Current;
        var root = UiKit.Rect("Shift", canvas.transform, Vector2.zero, new Vector2(16f + Size * 0.5f, 80f + Size * 0.5f), new Vector2(Size, Size));
        UiKit.FilledImage(root, t.border);
        var fill = UiKit.Stretch("Fill", root, t.frameWidth);
        var bg = UiKit.FilledImage(fill, t.panelFill);
        Sprite icon = DraftHud.Ui("ctrl_key_shift");
        if (icon != null) { bg.sprite = icon; bg.color = Color.white; bg.preserveAspect = true; }
        UiKit.Label("Key", fill, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Size, Size), "SHIFT", "SHIFT", 9, Color.white);

        overlay = UiKit.FilledImage(UiKit.Stretch("Cooldown", fill), Dark);
        overlay.sprite = whiteSprite;
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = (int)Image.Origin360.Top;
        overlay.fillClockwise = true;
        overlay.fillAmount = 0f;

        // Pulse ring: four thin edges under a scaled, fading group.
        pulse = UiKit.Rect("Pulse", canvas.transform, Vector2.zero, root.anchoredPosition, new Vector2(Size, Size));
        pulseGroup = pulse.gameObject.AddComponent<CanvasGroup>();
        pulseGroup.alpha = 0f;
        pulseGroup.blocksRaycasts = false;
        const float e = 2f;
        Edge(pulse, new Vector2(0.5f, 1f), new Vector2(Size, e));
        Edge(pulse, new Vector2(0.5f, 0f), new Vector2(Size, e));
        Edge(pulse, new Vector2(0f, 0.5f), new Vector2(e, Size));
        Edge(pulse, new Vector2(1f, 0.5f), new Vector2(e, Size));
    }

    static void Edge(RectTransform parent, Vector2 anchor, Vector2 size)
    {
        var rt = UiKit.Rect("Edge", parent, anchor, Vector2.zero, size);
        UiKit.FilledImage(rt, UiTheme.Current.selectedBorder);
    }

    void Update()
    {
        if (player == null) { Destroy(gameObject); return; }

        float remaining = player.DashCooldownRemaining; // same clock as dashCooldownUntil (Time.time)
        bool cooling = remaining > 0f;
        overlay.fillAmount = cooling ? Mathf.Clamp01(remaining / DashRules.ShiftCooldown) : 0f;
        if (wasCooling && !cooling) pulseT = 0f;
        wasCooling = cooling;

        if (pulseT < PulseTime)
        {
            pulseT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(pulseT / PulseTime);
            pulse.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, k);
            pulseGroup.alpha = 1f - k;
        }
    }
}
