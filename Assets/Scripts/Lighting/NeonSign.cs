using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Unlit sprite sign + optional child Point Light2D. Intensity is driven by LightingController.
// [ExecuteAlways] so the edit-mode day/night preview reaches the signs.
[ExecuteAlways]
public class NeonSign : MonoBehaviour
{
    public static readonly List<NeonSign> All = new List<NeonSign>();

    // domain reload is off: clear leftovers between editor play sessions
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => All.Clear();

    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Light2D signLight;
    [SerializeField, Range(0f, 1f)] float flicker = 0f;   // 0 = steady, 0.3 = occasional dips
    [SerializeField] float lightIntensityOn = 0.4f;

    float level = 1f; // last value from LightingController
    public bool On { get; private set; } = true;

    void OnEnable()
    {
        All.Add(this);
        var c = LightingController.Current;
        Apply(c != null ? c.CurrentNeon : 1f, 1f);
    }

    void OnDisable() { All.Remove(this); }

    public void SetOn(bool on)
    {
        On = on;
        Apply(level, 1f);
    }

    public void SetIntensity(float k)
    {
        level = k;
        Apply(k, 1f);
    }

    void Update()
    {
        if (!Application.isPlaying || flicker <= 0f) return;
        // stepped hash flicker, 12 steps per second (no smooth sine: keeps the pixel feel)
        int step = (int)(Time.time * 12f);
        uint h = (uint)(step * 73856093 ^ (GetInstanceID() * 19349663));
        float r = (h % 1000u) / 1000f;
        Apply(level, 1f - flicker * (r > 0.7f ? r : 0f));
    }

    void Apply(float k, float mul)
    {
        float v = On ? Mathf.Clamp01(k) * mul : 0f;
        if (sprite != null)
        {
            sprite.color = new Color(v, v, v, v);
            sprite.enabled = v > 0.001f;
        }
        if (signLight != null)
        {
            signLight.intensity = lightIntensityOn * v;
            signLight.enabled = v > 0.01f;
        }
    }
}

