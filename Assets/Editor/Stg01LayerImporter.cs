using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Import rules for the layered STG-01 background parts (Assets/Art/Backgrounds/STG01_Layers, manifest: docs/design/concept/bg_layers/):
// Sprite, PPU 32, Point, uncompressed, FullRect. <name>_<N>f.png = N equal frames side by side (frame names <name>_<i>); pivots from the manifest.
public class Stg01LayerImporter : AssetPostprocessor
{
    public const string Dir = "Assets/Art/Backgrounds/STG01_Layers/";
    // STG-02..05 parts (docs/design/concept/bg_assets/parts_manifest.md): same rules; hanging lanterns / cables pivot at the top; strips over 4096 px are repacked as grids (GridCols).
    public const string PartsDir = "Assets/Art/Backgrounds/STG02_05_Parts/";
    public override uint GetVersion() => 2;

    // Frame columns for strips repacked into a grid to stay within 4096 px (WebGL MAX_TEXTURE_SIZE).
    static readonly System.Collections.Generic.Dictionary<string, int> GridCols = new System.Collections.Generic.Dictionary<string, int>
    {
        { "smoke_stack_6f", 3 }, { "vat_bubbles_6f", 3 }, { "searchlight_sweep_8f", 4 }
    };

    public static bool Handles(string path) => path.StartsWith(Dir) || path.StartsWith(PartsDir);

    static Vector2 PivotFor(string name)
    {
        if (name.StartsWith("lantern_hanji") || name.StartsWith("cable_bundle")) return new Vector2(0.5f, 1f);
        if (name.StartsWith("cloud_") || name.StartsWith("holo_board") || name.StartsWith("holo_ring") || name.StartsWith("drone_")) return new Vector2(0.5f, 0.5f);
        if (name.StartsWith("lantern_red")) return new Vector2(0.5f, 1f); // hangs from its top
        return new Vector2(0.5f, 0f);
    }

    void OnPreprocessTexture()
    {
        if (!Handles(assetPath)) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spritePixelsPerUnit = 32f;
        ti.filterMode = FilterMode.Point;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.maxTextureSize = 4096;

        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        Vector2 pivot = PivotFor(name);
        var st = new TextureImporterSettings();
        ti.ReadTextureSettings(st);
        st.spriteMeshType = SpriteMeshType.FullRect;
        st.spriteExtrude = 0;
        st.spriteAlignment = (int)SpriteAlignment.Custom;
        st.spritePivot = pivot;
        ti.SetTextureSettings(st);

        Match m = Regex.Match(name, @"_(\d+)f$");
        if (!m.Success) { ti.spriteImportMode = SpriteImportMode.Single; return; }
        int count = int.Parse(m.Groups[1].Value);
        ti.GetSourceTextureWidthAndHeight(out int w, out int h);
        // Strips wider than 4096 px are packed as a grid (row-major, top row first); default is a single row.
        int cols = GridCols.TryGetValue(name, out int gc) ? gc : count;
        int rows = (count + cols - 1) / cols;
        float fw = w / (float)cols;
        float fh = h / (float)rows;
        var frames = new SpriteMetaData[count];
        for (int i = 0; i < count; i++)
            frames[i] = new SpriteMetaData
            {
                name = name + "_" + i,
                rect = new Rect((i % cols) * fw, h - (i / cols + 1) * fh, fw, fh),
                alignment = (int)SpriteAlignment.Custom,
                pivot = pivot
            };
        ti.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618 // simple slicing API, same as DraftSpriteImporter
        ti.spritesheet = frames;
#pragma warning restore CS0618
    }
}
