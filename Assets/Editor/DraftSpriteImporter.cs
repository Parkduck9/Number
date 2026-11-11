using UnityEditor;
using UnityEngine;

// DRAFT art import rules. See docs/design/impl/draft-sprites.md.
//  Assets/Resources/DraftSprites/<name>_<N>f.png : N frames of 64x64, pivot on the feet line (2 px above the frame bottom), PPU 32.
//  Assets/Resources/DraftHud/*_digits_*.png      : 10 digit glyphs (broadcast 12x18 + 1px gap, holo 7x11 + 1px gap), pivot center.
//  Assets/Resources/DraftHud/*_arcs_*.png        : 2 broadcast waves (left, right) 11x16 + 1px gap, pivot center.
//  Assets/Resources/DraftHud|DraftUi/*.png       : single sprite, pivot center.
public class DraftSpriteImporter : AssetPostprocessor
{
    const float StripFeetGap = 2f; // px: every 64x64 strip has its feet on row 61, two empty rows below
    public override uint GetVersion() => 6; // 6: ending_illustration bilinear; 4: UI kit 9-slice borders, crosshair pivots, dialogue_next_2f frames; 5: bullet_*_2f 16x16 center

    // UI kit 9-slice borders (docs/design/concept/ui_kit/borders.json: left, right, top, bottom in source px) as Unity spriteBorder (x=left, y=bottom, z=right, w=top).
    static readonly System.Collections.Generic.Dictionary<string, Vector4> NineSlice = new System.Collections.Generic.Dictionary<string, Vector4>
    {
        { "panel_9s", new Vector4(9, 9, 9, 9) },
        { "button_9s_normal", new Vector4(5, 4, 5, 4) },
        { "button_9s_hover", new Vector4(5, 4, 5, 4) },
        { "button_9s_pressed", new Vector4(5, 4, 5, 4) },
        { "dialogue_box_9s", new Vector4(10, 8, 10, 8) },
        { "dialogue_nameplate_9s", new Vector4(9, 7, 9, 7) },
        { "hint_plate_9s", new Vector4(4, 3, 4, 3) },
    };

    // crosshair art: ink centre is pixel (7,7) from the top-left of the 16x16 sheet = pivot (7.5/16, 8.5/16); lock bracket: top-left corner.
    static bool CrosshairPivot(string name, out Vector2 pivot)
    {
        pivot = name.StartsWith("crosshair_") ? new Vector2(7.5f / 16f, 8.5f / 16f) : new Vector2(0f, 1f);
        return name.StartsWith("crosshair_") || name == "lock_bracket";
    }

    void OnPreprocessTexture()
    {
        string dir = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
        bool strip = dir == "Assets/Resources/DraftSprites";
        if (!strip && dir != "Assets/Resources/DraftHud" && dir != "Assets/Resources/DraftUi") return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spritePixelsPerUnit = 32f;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        ti.filterMode = name == "ending_illustration" ? FilterMode.Bilinear : FilterMode.Point; // painterly 1024x576 image, not pixel art
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;

        bool digits = name.Contains("_digits_"), arcs = name.Contains("_arcs_"), next2 = name == "dialogue_next_2f";
        if (!strip && !digits && !arcs && !next2)
        {
            ti.spriteImportMode = SpriteImportMode.Single; // default pivot = center
            if (NineSlice.TryGetValue(name, out Vector4 border)) ti.spriteBorder = border; // UI kit 9-slice
            if (CrosshairPivot(name, out Vector2 pivot))
            {
                var st = new TextureImporterSettings();
                ti.ReadTextureSettings(st);
                st.spriteAlignment = (int)SpriteAlignment.Custom;
                st.spritePivot = pivot;
                ti.SetTextureSettings(st);
            }
            return;
        }

        int count; float w, h, step;
        if (next2) // UI kit blinking arrow: two 8x8 frames side by side, no gap
        {
            count = 2; w = h = step = 8f;
        }
        else if (strip)
        {
            string tail = name.Substring(name.LastIndexOf('_') + 1); // "3f"
            count = int.TryParse(tail.TrimEnd('f'), out int n) ? n : 1;
            w = h = step = name.StartsWith("bullet_") ? 16f : 64f; // enemy shots: 16x16 frames, pivot center (below)
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
        bool feet = strip && !name.StartsWith("bullet_");
        var frames = new SpriteMetaData[count];
        for (int i = 0; i < count; i++)
            frames[i] = new SpriteMetaData
            {
                name = name + "_" + i,
                rect = new Rect(i * step, 0f, w, h),
                alignment = (int)(feet ? SpriteAlignment.Custom : SpriteAlignment.Center),
                pivot = feet ? new Vector2(0.5f, StripFeetGap / 64f) : new Vector2(0.5f, 0.5f) // strips: pivot on the feet line, not the frame bottom
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
        if (!assetPath.StartsWith("Assets/Art/Backgrounds/") || Stg01LayerImporter.Handles(assetPath)) return; // the layered parts have their own importer
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
