using UnityEngine;
using UnityEngine.UI;

// DECISIONS 26 hit vs parry readability, code-drawn (no asset): hurt = red vignette on the screen edges fading out over HurtTime;
// (the parry full-screen flash was removed in playtest 11-11, DECISIONS 35: the contact point glows instead, ParryFx.ContactGlow).
// Unscaled time, so it plays through hit-stops.
// One per scene, made on first use (PlayerController.ReceiveEnemyHit): one overlay canvas, one raycast-off Image, disabled when idle.
public class ScreenFlash : MonoBehaviour
{
    const float HurtTime = 0.25f, HurtAlpha = 0.6f;    // (T)
    const float VignetteInner = 0.6f;                  // (T) 0 = centre, 1 = edge: the red starts here
    const int VignetteRes = 64;
    static readonly Color HurtColor = new Color(0.9f, 0.08f, 0.12f);

    static ScreenFlash inst;
    static Sprite vignette;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { inst = null; vignette = null; } // Domain Reload is off

    Image hurt;
    float hurtT = HurtTime;

    public static void Hurt() { Get().hurtT = 0f; inst.Tick(0f); }

    static ScreenFlash Get()
    {
        if (inst == null) inst = new GameObject("ScreenFlash").AddComponent<ScreenFlash>().Build(); // scene load destroys the old one
        return inst;
    }

    ScreenFlash Build()
    {
        var canvas = UiKit.CreateCanvas("ScreenFlashCanvas", transform, 30); // under the HUD (35+) and menus
        if (vignette == null) vignette = MakeVignette();
        hurt = UiKit.FilledImage(UiKit.Stretch("Hurt", canvas.transform), HurtColor);
        hurt.sprite = vignette;
        hurt.enabled = false;
        return this;
    }

    void Update() => Tick(Time.unscaledDeltaTime);

    void Tick(float dt)
    {
        hurtT += dt;
        Fade(hurt, HurtColor, HurtAlpha * (1f - hurtT / HurtTime));
    }

    static void Fade(Image img, Color c, float a)
    {
        bool on = a > 0.001f;
        if (img.enabled != on) img.enabled = on;
        if (!on) return;
        c.a = a;
        img.color = c;
    }

    // White, alpha 0 inside VignetteInner rising to 1 at the edges (each axis separately, so it hugs a 16:9 screen; corners strongest).
    static Sprite MakeVignette()
    {
        const int n = VignetteRes;
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f), v = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                float a = Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(VignetteInner, 1f, u))
                                      + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(VignetteInner, 1f, v)));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "Vignette", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
