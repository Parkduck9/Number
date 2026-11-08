using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu NUMBER/Build Title Scene: creates Assets/Resources/UiTheme.asset (if missing), Assets/Scenes/Title.unity,
// and puts Title at Build Settings index 0 (other entries keep their order).
public static class TitleSceneBuilder
{
    const string ScenePath = "Assets/Scenes/Title.unity";
    const string ThemePath = "Assets/Resources/UiTheme.asset";
    const string FontPath = "Assets/Fonts/Mulmaru/Mulmaru SDF.asset";

    [MenuItem("NUMBER/Build Title Scene")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().isDirty) { Debug.LogError("[TitleSceneBuilder] Save the open scene first (this menu never opens a dialog)."); return; }

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<UiTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) Debug.LogWarning("[TitleSceneBuilder] Mulmaru SDF not found: " + FontPath);
        else if (theme.font == null) { theme.font = font; EditorUtility.SetDirty(theme); }
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = theme.background;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();
        new GameObject("TitleScreen").AddComponent<TitleScreen>();
        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == ScenePath);
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log("[TitleSceneBuilder] Built " + ScenePath + " (Build Settings index 0). Start loads STG00 (or STG01 if missing): make sure it is in Build Settings.");
    }
}
