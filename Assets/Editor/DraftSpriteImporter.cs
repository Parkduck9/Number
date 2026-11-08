using UnityEditor;
using UnityEngine;

// DRAFT sprite strips (Assets/Resources/DraftSprites): three 64x64 frames, pivot bottom-center (feet), PPU 32. See docs/design/impl/draft-sprites.md.
public class DraftSpriteImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/DraftSprites/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = 32f;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;

        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        var frames = new SpriteMetaData[3];
        for (int i = 0; i < 3; i++)
            frames[i] = new SpriteMetaData
            {
                name = name + "_" + i,
                rect = new Rect(i * 64f, 0f, 64f, 64f),
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
#pragma warning disable CS0618 // simple slicing API; fine for a draft
        ti.spritesheet = frames;
#pragma warning restore CS0618
    }
}
