#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

// Dev-only frame readout (F3): FPS, average / worst frame time over 0.5 s, and a HIT-STOP tag while the game is frozen on purpose.
// A hitch shows as a worst-frame spike; a hit-stop keeps FPS normal and only lights the tag. Not compiled into release builds.
public class FrameStats : MonoBehaviour
{
    const float Window = 0.5f;
    const float SpikeMs = 33.4f; // slower than 30 fps

    bool show;
    float acc, worst;
    int frames, spikes, hitStops;
    bool wasFrozen;
    string text = "";
    GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (FindFirstObjectByType<FrameStats>() != null) return;
        var go = new GameObject("FrameStats");
        DontDestroyOnLoad(go);
        go.AddComponent<FrameStats>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) show = !show;
        if (!show) return;
        float dt = Time.unscaledDeltaTime;
        acc += dt;
        frames++;
        if (dt > worst) worst = dt;
        if (dt * 1000f > SpikeMs) spikes++;
        bool frozen = Time.timeScale == 0f && !PauseMenu.IsOpen;
        if (frozen && !wasFrozen) hitStops++;
        wasFrozen = frozen;
        if (acc < Window) return;
        text = string.Format("{0:0} FPS  avg {1:0.0} ms  worst {2:0.0} ms  spikes(>33ms) {3}  hit-stops {4}",
                             frames / acc, acc * 1000f / frames, worst * 1000f, spikes, hitStops);
        acc = worst = 0f;
        frames = 0;
    }

    void OnGUI()
    {
        if (!show) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.yellow } };
        bool hitStop = Time.timeScale == 0f && !PauseMenu.IsOpen;
        GUI.Label(new Rect(8f, 8f, 700f, 24f), text, style);
        if (hitStop) GUI.Label(new Rect(8f, 30f, 200f, 24f), "HIT-STOP", style);
    }
}
#endif
