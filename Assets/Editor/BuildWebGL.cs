using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.WebGL;
using UnityEngine;

// Menus: NUMBER/Apply WebGL Settings (settings only), NUMBER/Build WebGL (settings + build to Build/WebGL).
// Only changes a setting when it differs, and logs every change. Do not run Build while the editor is in use:
// it switches the active platform to WebGL (reimports assets) and blocks the editor for minutes.
public static class BuildWebGL
{
    const string OutputDir = "Build/WebGL";

    [MenuItem("NUMBER/Apply WebGL Settings")]
    public static void ApplySettings()
    {
        var changes = new List<string>();

        // Brotli on itch.io (https). Decompression Fallback keeps it loading if the host omits Content-Encoding.
        Set(changes, "compressionFormat", PlayerSettings.WebGL.compressionFormat, WebGLCompressionFormat.Brotli, v => PlayerSettings.WebGL.compressionFormat = v);
        Set(changes, "decompressionFallback", PlayerSettings.WebGL.decompressionFallback, true, v => PlayerSettings.WebGL.decompressionFallback = v);
        // Threads are not allowed on itch.io (no cross-origin isolation).
        Set(changes, "threadsSupport", PlayerSettings.WebGL.threadsSupport, false, v => PlayerSettings.WebGL.threadsSupport = v);
        // Default template (has a fullscreen button). The canvas is a fixed WxH px, so set the itch.io viewport to 960x540 + fullscreen button.
        Set(changes, "template", PlayerSettings.WebGL.template, "APPLICATION:Default", v => PlayerSettings.WebGL.template = v);
        // Default canvas 960x540 (16:9, matches the UI reference; fits an itch.io embed, fullscreen button stays).
        Set(changes, "defaultWebScreenWidth", PlayerSettings.defaultWebScreenWidth, 960, v => PlayerSettings.defaultWebScreenWidth = v);
        Set(changes, "defaultWebScreenHeight", PlayerSettings.defaultWebScreenHeight, 540, v => PlayerSettings.defaultWebScreenHeight = v);
        // Exceptions: full without stacktrace, so null refs / range errors are catchable instead of corrupting memory (small size cost; stacktraces would be bigger/slower).
        Set(changes, "exceptionSupport", PlayerSettings.WebGL.exceptionSupport, WebGLExceptionSupport.FullWithoutStacktrace, v => PlayerSettings.WebGL.exceptionSupport = v);
        // Smaller download over faster build / peak speed.
        Set(changes, "codeOptimization", UserBuildSettings.codeOptimization, WasmCodeOptimization.DiskSize, v => UserBuildSettings.codeOptimization = v);
        // Managed stripping: Medium (High risks stripping reflection users such as TMP/Input System).
        var target = NamedBuildTarget.WebGL;
        Set(changes, "managedStrippingLevel", PlayerSettings.GetManagedStrippingLevel(target), ManagedStrippingLevel.Medium, v => PlayerSettings.SetManagedStrippingLevel(target, v));

        if (changes.Count == 0) Debug.Log("[BuildWebGL] WebGL settings already as required, nothing changed.");
        else { AssetDatabase.SaveAssets(); Debug.Log("[BuildWebGL] Changed WebGL settings:\n" + string.Join("\n", changes)); }
    }

    [MenuItem("NUMBER/Build WebGL")]
    public static void Build()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) { Debug.LogError("[BuildWebGL] No enabled scenes in Build Settings."); return; }

        ApplySettings();
        Directory.CreateDirectory(OutputDir);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;
        if (s.result == BuildResult.Succeeded) Debug.Log($"[BuildWebGL] Succeeded: {OutputDir} ({s.totalSize / (1024f * 1024f):F1} MB, {s.totalTime.TotalSeconds:F0}s). Zip the CONTENTS of the folder (index.html at the zip root) for itch.io.");
        else Debug.LogError($"[BuildWebGL] {s.result}: {s.totalErrors} errors.");
    }

    static void Set<T>(List<string> changes, string name, T current, T wanted, System.Action<T> apply)
    {
        if (EqualityComparer<T>.Default.Equals(current, wanted)) return;
        apply(wanted);
        changes.Add($"  {name}: {current} -> {wanted}");
    }
}
