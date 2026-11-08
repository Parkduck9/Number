using UnityEditor;
using UnityEngine;

// Sprite sheets under Resources/FX are sliced in code: keep them pixel-exact (no NPOT rescale, no filtering/compression).
public class FxTextureImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/FX/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default; // Resources.Load<Texture2D> works
        ti.filterMode = FilterMode.Point;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
    }
}
