using System.Collections.Generic;
using UnityEngine;

// Elite duel focus (DECISIONS 49): everything but the two fighters and the duel UI goes out of focus (light blur + darken).
// The 2D Renderer copies the screen into _CameraSortingLayerTexture right after the World layer (Renderer2D "Foremost Sorting
// Layer" = World). A camera-sized quad on the DuelBlur layer (right after World) draws that copy blurred (Number/DuelFocusBlur).
// Instead of lifting the fighters above the quad (a new layer would lose their 2D lights), every OTHER renderer above DuelBlur
// (other enemies, their numbers, projectiles, ambient FX, ...) is lowered onto the top of World for the duel, keeping its stacking
// (order + LayerStep per layer it came from), and put back once the fade-out ends. The fighters' hierarchies (hero + children,
// elite + number + ring) and the duel's own objects (pips) keep their layers; FX spawned during the duel stay sharp.
// Fallback: no shader support = a plain dark overlay. Missing DuelBlur layer = no effect. Real time, frozen while paused.
public class DuelFocusFx : MonoBehaviour
{
    const string LayerName = "DuelBlur";
    const float FadeIn = 0.2f, FadeOut = 0.25f; // (T) s
    const float FallbackAlpha = 0.55f;          // (T) plain dark overlay
    const float Oversize = 1.6f;                // quad vs view (camera shake; the PPU 1.5x zoom-out lags a frame)
    const int LayerStep = 4000;                 // order offset per sorting layer above World when lowered

    static readonly int AmountId = Shader.PropertyToID("_Amount");

    readonly List<Renderer> lowered = new List<Renderer>(256);
    readonly List<int> oldLayer = new List<int>(256), oldOrder = new List<int>(256);
    Camera cam;
    SpriteRenderer quad;
    Material mat;
    bool shaderOk;
    int worldId, worldValue, blurValue;
    float k, target;

    public static DuelFocusFx Create(Transform parent)
    {
        var go = new GameObject("DuelFocusFx");
        go.transform.SetParent(parent, false);
        return go.AddComponent<DuelFocusFx>();
    }

    // keep*: roots whose renderers stay sharp (null = none).
    public void Begin(Transform keepA, Transform keepB, Transform keepC)
    {
        if (!Ready()) return;
        Restore(); // a previous duel still fading out
        int blurId = quad.sortingLayerID;
        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) // once per duel
        {
            int v = SortingLayer.GetLayerValueFromID(r.sortingLayerID);
            if (v <= blurValue || r.sortingLayerID == blurId || r is SpriteMask) continue; // already under the overlay / masks stay put
            Transform t = r.transform;
            if ((keepA != null && t.IsChildOf(keepA)) || (keepB != null && t.IsChildOf(keepB)) || (keepC != null && t.IsChildOf(keepC))) continue;
            lowered.Add(r);
            oldLayer.Add(r.sortingLayerID);
            oldOrder.Add(r.sortingOrder);
            r.sortingLayerID = worldId;
            r.sortingOrder = Mathf.Clamp(r.sortingOrder + (v - worldValue) * LayerStep, short.MinValue, short.MaxValue);
        }
        target = 1f;
    }

    public void End() => target = 0f;

    // Quad on the main camera (rebuilt after a scene change). False = no DuelBlur layer / no camera: the duel runs without it.
    bool Ready()
    {
        if (quad != null && cam != null) return true;
        int blurId = SortingLayer.NameToID(LayerName);
        if (SortingLayer.IDToName(blurId) != LayerName) { Debug.LogWarning("DuelFocusFx: sorting layer '" + LayerName + "' missing"); return false; }
        cam = Camera.main;
        if (cam == null) return false;
        worldId = SortingLayer.NameToID("World");
        worldValue = SortingLayer.GetLayerValueFromID(worldId);
        blurValue = SortingLayer.GetLayerValueFromID(blurId);
        if (mat == null)
        {
            Shader s = Shader.Find("Number/DuelFocusBlur");
            shaderOk = s != null && s.isSupported;
            mat = new Material(shaderOk ? s : Shader.Find("Sprites/Default"));
        }
        var go = new GameObject("DuelFocusQuad");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 1f);
        quad = go.AddComponent<SpriteRenderer>();
        quad.sprite = ParryFx.Square;
        quad.sharedMaterial = mat;
        quad.sortingLayerID = blurId;
        quad.sortingOrder = short.MinValue;
        quad.enabled = false;
        k = 0f;
        return true;
    }

    void LateUpdate()
    {
        if (quad == null || cam == null) return;
        if (!PauseMenu.IsOpen) k = Mathf.MoveTowards(k, target, Time.unscaledDeltaTime / (target > k ? FadeIn : FadeOut));
        quad.enabled = k > 0f;
        if (k <= 0f) { if (lowered.Count > 0) Restore(); return; }
        float h = cam.orthographicSize * 2f * Oversize;
        quad.transform.localScale = new Vector3(h * cam.aspect, h, 1f);
        float a = k * k * (3f - 2f * k);
        if (shaderOk) mat.SetFloat(AmountId, a);
        else quad.color = new Color(0f, 0f, 0f, FallbackAlpha * a);
    }

    void Restore()
    {
        for (int i = 0; i < lowered.Count; i++)
        {
            if (lowered[i] == null) continue;
            lowered[i].sortingLayerID = oldLayer[i];
            lowered[i].sortingOrder = oldOrder[i];
        }
        lowered.Clear();
        oldLayer.Clear();
        oldOrder.Clear();
    }

    void OnDestroy()
    {
        Restore();
        if (quad != null) Destroy(quad.gameObject);
        if (mat != null) Destroy(mat);
    }
}
