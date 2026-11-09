using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Import rules for the layered STG-01 background parts (Assets/Art/Backgrounds/STG01_Layers, manifest: docs/design/concept/bg_layers/):
// Sprite, PPU 32, Point, uncompressed, FullRect. <name>_<N>f.png = N equal frames side by side (frame names <name>_<i>); pivots from the manifest.
public class Stg01LayerImporter : AssetPostprocessor
{
    public const string Dir = "Assets/Art/Backgrounds/STG01_Layers/";
    public override uint GetVersion() => 1;

    static Vector2 PivotFor(string name)
    {
        if (name.StartsWith("cloud_") || name.StartsWith("holo_board") || name.StartsWith("holo_ring") || name.StartsWith("drone_")) return new Vector2(0.5f, 0.5f);
        if (name.StartsWith("lantern_red")) return new Vector2(0.5f, 1f); // hangs from its top
        return new Vector2(0.5f, 0f);
    }

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Dir)) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spritePixelsPerUnit = 32f;
        ti.filterMode = FilterMode.Point;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.maxTextureSize = 4096; // the 3240 px overlay strips

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
        float fw = w / (float)count;
        var frames = new SpriteMetaData[count];
        for (int i = 0; i < count; i++)
            frames[i] = new SpriteMetaData
            {
                name = name + "_" + i,
                rect = new Rect(i * fw, 0f, fw, h),
                alignment = (int)SpriteAlignment.Custom,
                pivot = pivot
            };
        ti.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618 // simple slicing API, same as DraftSpriteImporter
        ti.spritesheet = frames;
#pragma warning restore CS0618
    }
}
