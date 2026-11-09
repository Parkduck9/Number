using UnityEngine;

// Tiny easing helpers for GUI motion (no tween library). Callers pass unscaled-time based t.
public static class UiMotion
{
    // 0..1 in, overshoots ~10% then settles at 1.
    public static float BackOut(float t)
    {
        t = Mathf.Clamp01(t) - 1f;
        return 1f + t * t * (2.70158f * t + 1.70158f);
    }

    // Decaying horizontal shake: integer pixel offset for `elapsed` seconds into a `duration` shake. 0 when finished.
    public static int Shake(float elapsed, float duration, float amplitude)
    {
        if (elapsed < 0f || elapsed >= duration) return 0;
        return Mathf.RoundToInt(Mathf.Sin(elapsed * 80f) * amplitude * (1f - elapsed / duration));
    }
}
