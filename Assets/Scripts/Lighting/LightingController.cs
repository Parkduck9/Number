using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One per scene. Applies a LightingProfile (instantly or faded) to the two global lights,
// the bloom Volume weight and every NeonSign. Edit mode applies once in OnEnable/OnValidate only.
[ExecuteAlways]
public class LightingController : MonoBehaviour
{
    public static LightingController Current;

    [SerializeField] Light2D worldLight;
    [SerializeField] Light2D characterLight;
    [SerializeField] Volume volume;
    public LightingProfile profile;
    public LightingProfile dayProfile;
    public LightingProfile nightProfile;
    [SerializeField] bool debugToggleKey = false; // N key; editor / development builds only

    // current / fade source / fade target: world color + 4 floats
    struct Look { public Color world; public float worldI, charI, neon, post; }
    Look cur, from, to;
    float fadeT = 1f, fadeLen;

    public float CurrentNeon => cur.neon;

    void OnEnable() { Current = this; ApplyNow(); }
    void OnDisable() { if (Current == this) Current = null; }
    void OnValidate()
    {
#if UNITY_EDITOR
        // defer: toggling Light2D.enabled inside OnValidate raises SendMessage warnings
        UnityEditor.EditorApplication.delayCall += () => { if (this) ApplyNow(); };
#endif
    }

    void ApplyNow() { if (profile != null) Apply(profile, 0f); }

    public void Apply(LightingProfile p, float seconds = 0f)
    {
        if (p == null) return;
        profile = p;
        to = new Look { world = p.worldLightColor, worldI = p.worldLightIntensity, charI = p.characterLightIntensity, neon = p.neonIntensity, post = p.postFxWeight };
        if (seconds <= 0f || !Application.isPlaying) { cur = to; fadeT = 1f; Push(); return; }
        from = cur; fadeLen = seconds; fadeT = 0f;
    }

    void Update()
    {
        if (!Application.isPlaying) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var kb = Keyboard.current;
        if (debugToggleKey && kb != null && kb.nKey.wasPressedThisFrame && dayProfile != null && nightProfile != null)
            Apply(profile == nightProfile ? dayProfile : nightProfile, 0.5f);
#endif

        if (fadeT >= 1f) return;
        fadeT = Mathf.Min(1f, fadeT + Time.deltaTime / fadeLen);
        cur = new Look
        {
            world = Color.Lerp(from.world, to.world, fadeT),
            worldI = Mathf.Lerp(from.worldI, to.worldI, fadeT),
            charI = Mathf.Lerp(from.charI, to.charI, fadeT),
            neon = Mathf.Lerp(from.neon, to.neon, fadeT),
            post = Mathf.Lerp(from.post, to.post, fadeT),
        };
        Push();
    }

    void Push()
    {
        if (worldLight != null) { worldLight.color = cur.world; worldLight.intensity = cur.worldI; }
        if (characterLight != null) { characterLight.color = cur.world; characterLight.intensity = cur.worldI + cur.charI; } // own global light on Characters = world look + lift
        if (volume != null) volume.weight = cur.post;
        var all = NeonSign.All;
        for (int i = 0; i < all.Count; i++) all[i].SetIntensity(cur.neon);
    }
}

