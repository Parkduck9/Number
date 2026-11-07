using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Creates the dialogue assets if missing (Mulmaru TMP font, speaker table) and adds the DialoguePlayer to a scene.
public static class DialogueSetup
{
    const string FontSource = "Assets/Fonts/Mulmaru/Mulmaru.ttf";
    const string FontAssetPath = "Assets/Fonts/Mulmaru/Mulmaru SDF.asset";
    const string SpeakersPath = "Assets/Data/Dialogue/Speakers.asset";
    public const string SampleRadio = "Assets/Data/Dialogue/sample_radio.txt";
    public const string SampleTalk = "Assets/Data/Dialogue/sample_talk.txt";

    public static TMP_FontAsset EnsureFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;
        var src = AssetDatabase.LoadAssetAtPath<Font>(FontSource);
        if (src == null) { Debug.LogError("Font not found: " + FontSource); return null; }

        // Dynamic atlas, 24pt, small padding, raster (crisp pixels), Point filter.
        var fa = TMP_FontAsset.CreateFontAsset(src, 24, 2, GlyphRenderMode.RASTER, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fa.name = "Mulmaru SDF";
        AssetDatabase.CreateAsset(fa, FontAssetPath);
        fa.atlasTexture.name = "Mulmaru Atlas";
        fa.atlasTexture.filterMode = FilterMode.Point;
        fa.material.name = "Mulmaru Material";
        AssetDatabase.AddObjectToAsset(fa.atlasTexture, fa);
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    public static DialogueSpeakers EnsureSpeakers()
    {
        var existing = AssetDatabase.LoadAssetAtPath<DialogueSpeakers>(SpeakersPath);
        if (existing != null) return existing;
        Directory.CreateDirectory("Assets/Data/Dialogue");
        var so = ScriptableObject.CreateInstance<DialogueSpeakers>();
        so.speakers = new[]
        {
            new DialogueSpeakers.Speaker { id = "yudo", nameKo = "안유도", nameEn = "Yudo", color = new Color32(0x4C, 0xE0, 0xFF, 0xFF) },
            new DialogueSpeakers.Speaker { id = "broadcast", nameKo = "방송", nameEn = "Broadcast", color = new Color32(0xFF, 0xD2, 0x4A, 0xFF) },
            new DialogueSpeakers.Speaker { id = "citizen", nameKo = "시민", nameEn = "Citizen", color = new Color32(0xFF, 0x8C, 0x42, 0xFF) },
        };
        AssetDatabase.CreateAsset(so, SpeakersPath);
        AssetDatabase.SaveAssets();
        return so;
    }

    // Scene part: the player object plus two sample triggers. Called by PrototypeSceneBuilder.
    public static void BuildInScene()
    {
        var go = new GameObject("DialoguePlayer");
        var dp = go.AddComponent<DialoguePlayer>();
        var so = new SerializedObject(dp);
        so.FindProperty("speakers").objectReferenceValue = EnsureSpeakers();
        so.FindProperty("font").objectReferenceValue = EnsureFont();
        so.ApplyModifiedProperties();

        AddTrigger("Trigger_Radio (sample)", new Vector3(-2f, -5f, 0f), SampleRadio);
        AddTrigger("Trigger_Talk (sample)", new Vector3(14f, -5f, 0f), SampleTalk);
    }

    static void AddTrigger(string name, Vector3 pos, string textPath)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 6f);
        var t = go.AddComponent<DialogueTrigger>();
        var so = new SerializedObject(t);
        so.FindProperty("lines").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(textPath);
        so.ApplyModifiedProperties();
    }
}
