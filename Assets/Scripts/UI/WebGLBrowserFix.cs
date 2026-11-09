#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

// Browser-only fixes (Assets/Plugins/WebGL/NumberBrowserFix.jslib):
// - right click must not open the page's context menu;
// - the first click after the page loads must reach the game (canvas focused on load / on press, and the Input System
//   keeps reading input while the page has no focus, so that first press is not dropped as "unfocused").
static class WebGLBrowserFix
{
    [DllImport("__Internal")] static extern void NumberBlockContextMenu();
    [DllImport("__Internal")] static extern void NumberFocusCanvas();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        NumberBlockContextMenu();
        NumberFocusCanvas();
        // Needed on 6000.3 (Input System legacy focus path): otherwise events are flushed until focus and the press that
        // brings focus is dropped. Revisit on 6000.5+ (UNITY_INPUTSYSTEM_SUPPORTS_FOCUS_EVENTS changes this logic).
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        // IgnoreFocus also stops the reset on focus regain: clear keys whose keyup was lost while the tab was away.
        Application.focusChanged += focused => { if (focused && Keyboard.current != null) InputSystem.ResetDevice(Keyboard.current); };
    }
}
#endif
