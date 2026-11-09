using UnityEditor;
using UnityEngine;

// Dialogue portraits (Assets/Resources/Portraits/<speaker id>.png, 48x48): single sprite, pixel-exact.
public class PortraitImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Portraits/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 48f;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
    }
}
