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
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostFxPath) == null)
        {
            var vp = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = vp.Add<Bloom>(true);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.45f);
            bloom.highQualityFiltering.Override(false);
            bloom.downscale.Override(BloomDownscaleMode.Half);
            bloom.maxIterations.Override(4);
            AssetDatabase.CreateAsset(vp, PostFxPath);
        }
        AssetDatabase.SaveAssets();
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
        l.intensity = 0.4f;
        l.pointLightInnerRadius = 0.5f;
        l.pointLightOuterRadius = outerRadius;
        l.falloffIntensity = 0.6f;
        l.blendStyleIndex = 0;
        l.shadowsEnabled = false;
        l.targetSortingLayers = new[] { LayerId("World"), LayerId("Characters") };

        var n = go.AddComponent<NeonSign>();
        var so = new SerializedObject(n);
        so.FindProperty("sprite").objectReferenceValue = sr;
        so.FindProperty("signLight").objectReferenceValue = l;
        so.FindProperty("flicker").floatValue = flicker;
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
