using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Lighting helpers (docs/design/LIGHTING_DESIGN.md 5.1). Reusable by any scene builder:
//   LightingSetup.BuildInScene(camera) -> creates the "Lighting" root (2 global lights, bloom Volume, controller).
public static class LightingSetup
{
    public const string Dir = "Assets/Data/Lighting";
    public const string NightPath = Dir + "/Night.asset";
    public const string DayPath = Dir + "/Day.asset";
    public const string NightPartsPath = Dir + "/NightParts.asset";
    public const string PostFxPath = Dir + "/NightPostFx.asset";
    public static readonly string[] NewSortingLayers = { "Background", "World", "Characters", "FX" };

    // Adds missing sorting layers; never renames or removes. Also keeps Default just BELOW FX (order: Background, World, Characters,
    // Default, FX): the stage builders put every scene renderer on Background/World/Characters, so only runtime-spawned objects that
    // never set a layer stay on Default (enemy holo numbers, projectiles, swing flashes). They must not vanish behind the backgrounds,
    // and FX (sparks, the number ember drawn over the holo number) must stay on top of them. Default + FX share the white FX light.
    public static void EnsureSortingLayers()
    {
        var tm = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        var layers = tm.FindProperty("m_SortingLayers");
        bool changed = false;
        foreach (var name in NewSortingLayers)
        {
            bool found = false;
            for (int i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) { found = true; break; }
            if (found) continue;
            layers.InsertArrayElementAtIndex(layers.arraySize);
            var e = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            e.FindPropertyRelative("name").stringValue = name;
            e.FindPropertyRelative("uniqueID").uintValue = (uint)Random.Range(1000, int.MaxValue);
            e.FindPropertyRelative("locked").boolValue = false;
            changed = true;
        }
        int def = -1, fxIndex = -1;
        for (int i = 0; i < layers.arraySize; i++)
        {
            var e = layers.GetArrayElementAtIndex(i);
            if (e.FindPropertyRelative("uniqueID").uintValue == 0) def = i;
            if (e.FindPropertyRelative("name").stringValue == "FX") fxIndex = i;
        }
        if (def >= 0 && fxIndex >= 0 && def != fxIndex - 1)
        {
            layers.MoveArrayElement(def, def < fxIndex ? fxIndex - 1 : fxIndex);
            changed = true;
        }
        if (changed) { tm.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
    }

    public static void EnsureProfiles()
    {
        Directory.CreateDirectory(Dir);
        if (AssetDatabase.LoadAssetAtPath<LightingProfile>(NightPath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<LightingProfile>(), NightPath);
        if (AssetDatabase.LoadAssetAtPath<LightingProfile>(DayPath) == null)
        {
            var day = ScriptableObject.CreateInstance<LightingProfile>();
            day.worldLightColor = new Color32(0xFF, 0xF4, 0xE0, 0xFF);
            day.worldLightIntensity = 1f;
            day.characterLightIntensity = 0f;
            day.neonIntensity = 0f;
            day.postFxWeight = 0f;
            AssetDatabase.CreateAsset(day, DayPath);
        }
        EnsurePostFx();
        AssetDatabase.SaveAssets();
    }

    // Night post FX (Volume weight 1 at night, 0 by day). Overrides must be sub-assets of the profile: the first version only called
    // vp.Add, so the saved profile held a null component and no stage had any post FX. Rebuilt when missing or broken.
    // Values are tuning starts (2026-11-10 lighting pass): bloom only past 0.8 linear (unlit numbers > neon > lit, LIGHTING 1.1).
    static void EnsurePostFx()
    {
        var vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostFxPath);
        if (vp != null && vp.components.Count > 0 && !vp.components.Exists(c => c == null) && vp.Has<Vignette>()) return;
        if (vp == null) { vp = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(vp, PostFxPath); }
        foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(PostFxPath)) Object.DestroyImmediate(o, true);
        vp.components.Clear();

        var bloom = vp.Add<Bloom>(true);
        bloom.threshold.Override(0.8f);
        bloom.intensity.Override(0.8f);
        bloom.scatter.Override(0.5f);
        bloom.highQualityFiltering.Override(false);
        bloom.downscale.Override(BloomDownscaleMode.Half);
        bloom.maxIterations.Override(4);
        var vig = vp.Add<Vignette>(true);
        vig.color.Override(new Color(0.03f, 0.03f, 0.10f));
        vig.intensity.Override(0.3f);
        vig.smoothness.Override(0.4f);
        vig.rounded.Override(false);
        var ca = vp.Add<ColorAdjustments>(true);
        ca.contrast.Override(15f);
        var split = vp.Add<SplitToning>(true); // indigo shadows, warm lamp highlights (ART-01)
        split.shadows.Override(new Color(0.44f, 0.45f, 0.58f));
        split.highlights.Override(new Color(0.80f, 0.66f, 0.50f));
        var grain = vp.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.15f);
        grain.response.Override(0.8f);

        foreach (var c in vp.components)
        {
            c.name = c.GetType().Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, vp);
        }
        EditorUtility.SetDirty(vp);
    }

    static int LayerId(string name)
    {
        foreach (var l in SortingLayer.layers) if (l.name == name) return l.id;
        return 0;
    }

    static Light2D NewGlobal(string name, Transform parent, int[] layerIds)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var l = go.AddComponent<Light2D>();
        l.targetSortingLayers = layerIds; // before the type: a global light that briefly targets every layer logs "more than one global light"
        l.lightType = Light2D.LightType.Global;
        return l;
    }

    // Camera is optional: if given, post-processing is turned on and AA off. day = start on the Day profile (STG-00, LIGHTING Q7).
    public static LightingController BuildInScene(Camera cam, bool startDay = false)
    {
        EnsureSortingLayers();
        EnsureProfiles();
        var night = AssetDatabase.LoadAssetAtPath<LightingProfile>(NightPath);
        var day = AssetDatabase.LoadAssetAtPath<LightingProfile>(DayPath);
        var start = startDay ? day : night;

        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
        }

        // One global light per layer: World light = Background/World; Characters get their own (world look + lift);
        // FX and Default (runtime sparks, numbers, shots) a constant white light so effects keep their own colours at night.
        var all = new System.Collections.Generic.List<int>();
        foreach (var sl in SortingLayer.layers) if (sl.name != "Characters" && sl.name != "FX" && sl.id != 0) all.Add(sl.id);

        var root = new GameObject("Lighting");
        var world = NewGlobal("Global Light (World)", root.transform, all.ToArray());
        var chars = NewGlobal("Global Light (Characters)", root.transform, new[] { LayerId("Characters") });
        var fx = NewGlobal("Global Light (FX)", root.transform, new[] { LayerId("FX"), 0 });
        fx.color = Color.white;
        fx.intensity = 1f;

        var volGo = new GameObject("PostFx Volume");
        volGo.transform.SetParent(root.transform, false);
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.weight = 1f;
        vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostFxPath);

        var ctrlGo = new GameObject("LightingController");
        ctrlGo.transform.SetParent(root.transform, false);
        var ctrl = ctrlGo.AddComponent<LightingController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("worldLight").objectReferenceValue = world;
        so.FindProperty("characterLight").objectReferenceValue = chars;
        so.FindProperty("volume").objectReferenceValue = vol;
        so.FindProperty("profile").objectReferenceValue = start;
        so.FindProperty("dayProfile").objectReferenceValue = day;
        so.FindProperty("nightProfile").objectReferenceValue = night;
        so.ApplyModifiedProperties();
        ctrl.Apply(start, 0f);
        return ctrl;
    }

    // Spill strength (2026-11-10 lighting pass, was 0.4 / radius as given): with the night world light at 0.4 a lamp at 0.8 lifts the art
    // around it to full albedo (light texture clamps at 1, HDR emulation off), so pools read ~2.5x brighter than the street between lamps.
    public const float LampIntensity = 0.8f;
    public const float LampRadiusScale = 1.5f;

    // Creates a NeonSign: Unlit sprite + child Point Light2D (+ optional flat floor light).
    public static NeonSign BuildNeon(string name, Transform parent, Vector3 pos, Sprite sprite, Color lightColor,
        float outerRadius, float flicker, int order, string sortingLayer)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>(); // sprite may be null: a light-only lamp until the neon PNG exists
        sr.sprite = sprite;
        if (mat != null) sr.sharedMaterial = mat;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;

        var lgo = new GameObject("Light");
        lgo.transform.SetParent(go.transform, false);
        lgo.transform.localPosition = new Vector3(0f, -0.2f, 0f);
        var l = lgo.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = lightColor;
        l.intensity = LampIntensity;
        l.pointLightInnerRadius = 0.5f;
        l.pointLightOuterRadius = outerRadius * LampRadiusScale;
        l.falloffIntensity = 0.5f;
        l.blendStyleIndex = 0;
        l.shadowsEnabled = false;
        l.targetSortingLayers = new[] { LayerId("World"), LayerId("Characters") };

        var n = go.AddComponent<NeonSign>();
        var so = new SerializedObject(n);
        so.FindProperty("sprite").objectReferenceValue = sr;
        so.FindProperty("signLight").objectReferenceValue = l;
        so.FindProperty("flicker").floatValue = flicker;
        so.FindProperty("lightIntensityOn").floatValue = LampIntensity;
        so.ApplyModifiedProperties();
        return n;
    }

    public static Light2D BuildFloorLight(Transform parent, Vector3 pos, Color color)
    {
        var go = new GameObject("Floor Light");
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(3f, 0.5f, 1f);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = 0.25f;
        l.pointLightInnerRadius = 0.5f;
        l.pointLightOuterRadius = 3f;
        l.falloffIntensity = 0.6f;
        l.shadowsEnabled = false;
        l.targetSortingLayers = new[] { LayerId("World") };
        return l;
    }

    // ------------------------------------------------------------------ emissive pass (2026-11-10 lighting pass)

    // Background parts that ARE light sources (lit windows, lanterns, neon, holo screens, blinking lamps): Unlit so the night light
    // does not dim them, at 0.9 so they stay under the enemy numbers in the bloom (LIGHTING 1.1). Matched by object-name substring.
    static readonly string[] EmissiveKeys =
    {
        "window twinkle", "window_twinkle", "lantern glow", "lantern_red", "lantern_hanji", "neon_sign", "holo", "lamp_blink",
        "cabinet_lights", "screen_faces", "ledwall", "beam_pulse", "searchlight",
    };

    // Props that get a spill light (StreetBackdropKit street props; the parts kits place their own Glow lamps). Radius before LampRadiusScale.
    static readonly (string key, Color color, float radius)[] SpillProps =
    {
        ("stall_magenta", StageBuildKit.LampRed, 4f), ("stall_cyan", StageBuildKit.LampTeal, 4f), ("neon_sign", StageBuildKit.LampRed, 3f),
        ("holo_board", StageBuildKit.LampTeal, 4f), ("lantern_red", StageBuildKit.LampWarm, 4f), ("stone_lantern", StageBuildKit.LampWarm, 2.5f),
    };
    const string SpillName = "Spill (auto)";

    // Idempotent: emissive parts -> Unlit, spill lights at the SpillProps (skipped where a lamp already lights that spot),
    // and every existing NeonSign raised to LampIntensity. Called by StageBuildKit.BuildLighting and the menu below.
    public static void DecorateEmissives()
    {
        var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        foreach (var sign in Object.FindObjectsByType<NeonSign>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sign.name == SpillName) { Object.DestroyImmediate(sign.gameObject); continue; }
            var so = new SerializedObject(sign);
            so.FindProperty("lightIntensityOn").floatValue = LampIntensity;
            so.ApplyModifiedPropertiesWithoutUndo();
            var old = so.FindProperty("signLight").objectReferenceValue as Light2D;
            if (old != null && old.falloffIntensity > 0.55f) // built before LampRadiusScale (falloff 0.6): widen once
            {
                old.pointLightOuterRadius *= LampRadiusScale;
                old.falloffIntensity = 0.5f;
            }
        }

        var lamps = new System.Collections.Generic.List<Vector2>();
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Point) lamps.Add(l.transform.position);
        int[] layers = { LayerId("Background"), LayerId("World"), LayerId("Characters") };

        foreach (var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = r.name;
            if (unlit != null && r.sharedMaterial != unlit && System.Array.Exists(EmissiveKeys, k => n.Contains(k)))
            {
                r.sharedMaterial = unlit;
                var c = r.color;
                r.color = new Color(c.r * 0.9f, c.g * 0.9f, c.b * 0.9f, c.a);
            }
            foreach (var (key, color, radius) in SpillProps)
            {
                if (!n.Contains(key)) continue;
                var b = r.bounds;
                var pos = new Vector3(b.center.x, b.center.y - b.extents.y * 0.3f, 0f); // lower third: the light falls on the street
                if (!lamps.Exists(p => Vector2.Distance(p, pos) < 1.5f))
                {
                    var sign = BuildNeon(SpillName, r.transform, pos, null, color, radius, 0f, 0, "World");
                    sign.GetComponentInChildren<Light2D>().targetSortingLayers = layers; // the street is the background image
                    lamps.Add(pos);
                }
                break;
            }
        }

        // Parts-kit stages (STG-02..05, marked by their "Lamps (light only)" group) have darker, pre-dimmed art: at the street's 0.4 they go
        // murky, so they run on NightParts (world 0.6). The street backdrop (STG-01) keeps Night 0.4 for stronger light pools. Day is untouched.
        var ctrl = Object.FindFirstObjectByType<LightingController>();
        var night = AssetDatabase.LoadAssetAtPath<LightingProfile>(NightPath);
        if (ctrl != null && GameObject.Find("Lamps (light only)") != null && (ctrl.profile == night || ctrl.profile == null))
        {
            var parts = AssetDatabase.LoadAssetAtPath<LightingProfile>(NightPartsPath);
            if (parts == null)
            {
                parts = Object.Instantiate(night);
                parts.worldLightIntensity = 0.6f;
                parts.characterLightIntensity = 0.3f;
                AssetDatabase.CreateAsset(parts, NightPartsPath);
            }
            ctrl.nightProfile = parts;
            ctrl.Apply(parts, 0f);
            EditorUtility.SetDirty(ctrl);
        }
    }

    // Applies the emissive pass to STG-00..05 in place (no full rebuild) and saves them. Rebuilding a stage with its builder does the same.
    [MenuItem("NUMBER/Lighting/Apply Emissive Pass To Stages")]
    public static void ApplyEmissivePassToStages()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EnsureProfiles();
        for (int i = 0; i <= 5; i++)
        {
            string path = "Assets/Scenes/STG0" + i + ".unity";
            if (!File.Exists(path)) continue;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
            DecorateEmissives();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("Emissive pass applied to STG00-05.");
    }

    [MenuItem("NUMBER/Lighting/Toggle Day-Night (Edit Mode)")]
    public static void ToggleDayNight()
    {
        var c = Object.FindFirstObjectByType<LightingController>();
        if (c == null) { Debug.LogWarning("No LightingController in the open scene."); return; }
        var so = new SerializedObject(c);
        var cur = so.FindProperty("profile").objectReferenceValue;
        var night = so.FindProperty("nightProfile").objectReferenceValue as LightingProfile;
        var day = so.FindProperty("dayProfile").objectReferenceValue as LightingProfile;
        c.Apply(cur == night ? day : night, 0f);
        EditorUtility.SetDirty(c);
        SceneView.RepaintAll();
        Debug.Log("Lighting preview: " + c.profile.name);
    }
}
