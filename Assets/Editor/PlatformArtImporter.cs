using UnityEditor;
using UnityEngine;

// Platform art under Assets/Art/Platforms/<STAGE>/: single pixel-exact sprites, FullRect mesh so SpriteRenderer Tiled mode works.
public class PlatformArtImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Platforms/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 32f;
        ti.filterMode = FilterMode.Point;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteAlignment = (int)SpriteAlignment.Center;
        ti.SetTextureSettings(s);
        ti.spritePivot = new Vector2(0.5f, 0.5f);

        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        // Border L,B,R,T in px: the end caps stay unstretched when the strip is tiled.
        if (name.StartsWith("ledge_") || name.StartsWith("beam_")) ti.spriteBorder = new Vector4(8f, 0f, 8f, 0f);
        else if (name.StartsWith("tower_roof")) ti.spriteBorder = new Vector4(12f, 0f, 12f, 0f);
        else ti.spriteBorder = Vector4.zero;
    }
}
