using UnityEngine;
using UnityEngine.UI;

// Title hologram flicker: alpha of the given graphics steps between a few fixed levels.
// Mostly steady; about a third of the 1-second windows are a "stutter" where the level
// changes at StepsPerSecond, picked by an integer hash (irregular but repeatable, not a sine).
// Unscaled time, no per-frame allocations.
public class HologramFlicker : MonoBehaviour
{
    const float StepsPerSecond = 24f;  // (T) how often the level may change inside a stutter window
    const float StutterChance = 0.35f; // (T) share of 1-second windows that stutter
    const float DarkLevel = 0.2f, DimLevel = 0.6f; // (T) alpha multipliers; the rest of the steps are full
    const float DarkShare = 0.25f, DimShare = 0.3f; // (T) share of stutter steps at each level

    Graphic[] targets;
    Color[] baseColors;
    int lastStep = -1;

    public void Init(params Graphic[] graphics)
    {
        targets = graphics;
        baseColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) baseColors[i] = graphics[i].color;
    }

    void Update()
    {
        if (targets == null) return;
        float t = Time.unscaledTime;
        int step = Mathf.FloorToInt(t * StepsPerSecond);
        if (step == lastStep) return;
        lastStep = step;

        float level = 1f;
        if (Hash(Mathf.FloorToInt(t)) < StutterChance)
        {
            float h = Hash(step);
            level = h < DarkShare ? DarkLevel : h < DarkShare + DimShare ? DimLevel : 1f;
        }
        for (int i = 0; i < targets.Length; i++)
        {
            var c = baseColors[i];
            c.a *= level;
            targets[i].color = c;
        }
    }

    // Integer hash -> [0, 1]. Same input, same output: the flicker pattern is repeatable.
    static float Hash(int n)
    {
        uint x = (uint)n * 2654435761u;
        x ^= x >> 15; x *= 2246822519u; x ^= x >> 13;
        return (x & 0xFFFF) / 65535f;
    }
}
