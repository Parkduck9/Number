using UnityEngine;
using UnityEngine.InputSystem;

// Hidden test shortcut (not shown anywhere in the game): hold Ctrl + Shift + Alt and press 0..5 to jump straight to STG00..STG05
// (5 = the boss). One persistent object, created once at startup.
public class StageSkipKeys : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        var go = new GameObject("StageSkipKeys");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<StageSkipKeys>();
    }

    static readonly Key[] Digits = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 }; // Digit0 is not before Digit1 in the Key enum

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.ctrlKey.isPressed || !kb.shiftKey.isPressed || !kb.altKey.isPressed) return;
        if (kb.tKey.wasPressedThisFrame) { StageFlow.LoadStage("TUTORIAL"); return; } // T = the tutorial map
        for (int i = 0; i < Digits.Length; i++) // by name: STG03 is out of the run (11-12) but 3 still opens it for testing
            if (kb[Digits[i]].wasPressedThisFrame) { StageFlow.LoadStage("STG0" + i); return; }
    }
}
