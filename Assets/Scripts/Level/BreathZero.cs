using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// "Breath zero" moment (STG-00, DECISIONS 6-A / LIGHTING_DESIGN Q4): the city goes dark and silent, only the number-0 holograms stay lit.
// Two-stage timer: dim every Light2D (+ camera clear color) to dimFactor over dimSeconds, hold, restore over restoreSeconds.
// Unlit things (TMP numbers, neon sprites with Unlit material) are untouched by lights, so the 0 keeps glowing on its own.
// Audio: raises StopRequested (the audio system subscribes with one line: BreathZero.StopRequested += GameAudio.BreathStop)
// and, while muteListener is on, zeroes AudioListener.volume for the hold. The volume is always restored (hold end / OnDestroy).
public class BreathZero : MonoBehaviour
{
    public static event Action<float> StopRequested; // seconds of silence requested

    [SerializeField] float dimSeconds = 0.35f;          // (T)
    [SerializeField, Range(0f, 1f)] float dimFactor = 0.08f; // (T) light intensity multiplier at the bottom
    [SerializeField] float holdSeconds = 2.5f;          // (T) dark + silent
    [SerializeField] float restoreSeconds = 1.5f;       // (T)
    [SerializeField] bool muteListener = true;          // turn off once GameAudio handles the silence itself

    Light2D[] lights;
    float[] baseIntensity;
    Camera cam;
    Color baseBg;
    float prevVolume = 1f;
    bool muted;

    public void Play() { StartCoroutine(Run()); }

    IEnumerator Run()
    {
        lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
        cam = Camera.main;
        if (cam != null) baseBg = cam.backgroundColor;

        StopRequested?.Invoke(holdSeconds);
        if (muteListener) { prevVolume = AudioListener.volume; AudioListener.volume = 0f; muted = true; }

        yield return Fade(1f, dimFactor, dimSeconds);
        yield return new WaitForSeconds(holdSeconds);
        Unmute();
        yield return Fade(dimFactor, 1f, restoreSeconds);
    }

    IEnumerator Fade(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime) // scaled: pause (timeScale 0) stalls it like the rest of the opening
        {
            Apply(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        Apply(to);
    }

    void Apply(float k)
    {
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = baseIntensity[i] * k;
        if (cam != null) cam.backgroundColor = baseBg * k; // the sky is a clear color, no light touches it
    }

    void Unmute()
    {
        if (!muted) return;
        muted = false;
        AudioListener.volume = prevVolume;
    }

    void OnDestroy() { Unmute(); } // scene change / restart / play stop mid-blackout
}
