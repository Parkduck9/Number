using UnityEngine;

// One look (night / day / interior). Values per docs/design/LIGHTING_DESIGN.md 4.2.
[CreateAssetMenu(menuName = "NUMBER/Lighting Profile")]
public class LightingProfile : ScriptableObject
{
    public Color worldLightColor = new Color32(0xB4, 0xBC, 0xE6, 0xFF);
    [Range(0f, 1.5f)] public float worldLightIntensity = 0.7f;
    [Range(0f, 1f)] public float characterLightIntensity = 0.2f; // white, Characters layer only
    [Range(0f, 1f)] public float neonIntensity = 0.95f;          // 0 = every sign and its light off
    [Range(0f, 1f)] public float postFxWeight = 1f;              // Volume.weight (bloom)
}

