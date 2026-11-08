#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine;

// Calls Assets/Plugins/WebGL/NumberBrowserFix.jslib once at start (no right-click context menu on the canvas).
static class WebGLBrowserFix
{
    [DllImport("__Internal")] static extern void NumberBlockContextMenu();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init() => NumberBlockContextMenu();
}
#endif
