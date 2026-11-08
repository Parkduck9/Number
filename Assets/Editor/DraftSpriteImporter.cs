using UnityEditor;
using UnityEngine;

// DRAFT art import rules. See docs/design/impl/draft-sprites.md.
//  Assets/Resources/DraftSprites/<name>_<N>f.png : N frames of 64x64, pivot bottom-center (feet), PPU 32.
//  Assets/Resources/DraftHud/*_digits_*.png      : 10 digit glyphs (broadcast 12x18 + 1px gap, holo 7x11 + 1px gap), pivot center.
//  Assets/Resources/DraftHud/*_arcs_*.png        : 2 broadcast waves (left, right) 11x16 + 1px gap, pivot center.
//  Assets/Resources/DraftHud|DraftUi/*.png       : single sprite, pivot center.
public class DraftSpriteImporter : AssetPostprocessor
{
    public override uint GetVersion() => 2;

    void OnPreprocessTexture()
    {
        string dir = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
        bool strip = dir == "Assets/Resources/DraftSprites";
        if (!strip && dir != "Assets/Resources/DraftHud" && dir != "Assets/Resources/DraftUi") return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spritePixelsPerUnit = 32f;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;

        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool digits = name.Contains("_digits_"), arcs = name.Contains("_arcs_");
        if (!strip && !digits && !arcs)
        {
            ti.spriteImportMode = SpriteImportMode.Single; // default pivot = center
            return;
        }

        int count; float w, h, step;
        if (strip)
        {
            string tail = name.Substring(name.LastIndexOf('_') + 1); // "3f"
            count = int.TryParse(tail.TrimEnd('f'), out int n) ? n : 1;
            w = h = 64f; step = 64f;
        }
        else if (arcs) // left + right wave, 11x16 each, 1px gap
        {
            count = 2; w = 11f; h = 16f; step = 12f;
        }
        else
        {
            count = 10;
            bool broadcast = name.StartsWith("broadcast");
            w = broadcast ? 12f : 7f; h = broadcast ? 18f : 11f; step = w + 1f;
        }
        var frames = new SpriteMetaData[count];
        for (int i = 0; i < count; i++)
            frames[i] = new SpriteMetaData
            {
                name = name + "_" + i,
                rect = new Rect(i * step, 0f, w, h),
                alignment = (int)(strip ? SpriteAlignment.BottomCenter : SpriteAlignment.Center),
                pivot = strip ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f)
            };
        ti.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618 // simple slicing API; fine for a draft
        ti.spritesheet = frames;
#pragma warning restore CS0618
    }
}

// Final background art (Assets/Art/Backgrounds): Sprite(Single), PPU 32, Pivot Bottom Left, Point, no compression, no mipmaps. See docs/design/impl/bg-art.md.
public class BackgroundImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Backgrounds/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 32f;
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteAlignment = (int)SpriteAlignment.BottomLeft;
        ti.SetTextureSettings(st);
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.maxTextureSize = 2048;
        ti.npotScale = TextureImporterNPOTScale.None;
    }
}
