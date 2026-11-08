using System.IO;
using UnityEditor;
using UnityEngine;

// NUMBER/Quantize Selected To Palette: rewrites the selected PNG textures in place so every opaque pixel is the nearest
// color of Assets/Art/Palette/number_palette.png (distance in OKLab) and alpha is strictly 0 or 255 (alpha < 128 -> 0).
// Reads/writes the PNG files directly, so texture import settings do not affect the result. See docs/design/PALETTE.md.
public static class PaletteQuantizer
{
    const string PalettePath = "Assets/Art/Palette/number_palette.png";
    const int AlphaCutoff = 128;

    [MenuItem("NUMBER/Quantize Selected To Palette")]
    static void QuantizeSelected()
    {
        var targets = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
        if (targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Quantize", "Select one or more PNG textures in the Project window.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Quantize To Palette",
                $"Overwrite {targets.Length} PNG file(s) in place with palette colors (alpha -> 0/255)?\nUse git to revert.",
                "Quantize", "Cancel"))
            return;

        var palette = LoadPng(PalettePath).GetPixels32();
        var paletteLab = new Vector3[palette.Length];
        for (int i = 0; i < palette.Length; i++) paletteLab[i] = ToOklab(palette[i]);

        int done = 0;
        foreach (var tex in targets)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            if (path == PalettePath || !path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"PaletteQuantizer: skipped {path} (not a PNG, or the palette itself)");
                continue;
            }
            var src = LoadPng(path);
            var px = src.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                if (px[i].a < AlphaCutoff) { px[i] = new Color32(0, 0, 0, 0); continue; }
                var c = palette[Nearest(ToOklab(px[i]), paletteLab)];
                px[i] = new Color32(c.r, c.g, c.b, 255);
            }
            src.SetPixels32(px);
            File.WriteAllBytes(path, src.EncodeToPNG());
            Object.DestroyImmediate(src);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            done++;
        }
        Debug.Log($"PaletteQuantizer: quantized {done} texture(s) to {palette.Length} colors ({PalettePath}).");
    }

    static Texture2D LoadPng(string path)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(path))) throw new IOException("PaletteQuantizer: cannot load " + path);
        return tex;
    }

    static int Nearest(Vector3 lab, Vector3[] paletteLab)
    {
        int best = 0; float bestD = float.MaxValue;
        for (int i = 0; i < paletteLab.Length; i++)
        {
            float d = (paletteLab[i] - lab).sqrMagnitude;
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    static float SrgbToLinear(byte v)
    {
        float c = v / 255f;
        return c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    static float Cbrt(float x) => x < 0f ? -Mathf.Pow(-x, 1f / 3f) : Mathf.Pow(x, 1f / 3f);

    // Bjorn Ottosson's OKLab (same constants as the Python preview script).
    static Vector3 ToOklab(Color32 c)
    {
        float r = SrgbToLinear(c.r), g = SrgbToLinear(c.g), b = SrgbToLinear(c.b);
        float l = Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b);
        float m = Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b);
        float s = Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);
        return new Vector3(
            0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
            1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
            0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s);
    }
}

// Palette strip and previews must stay pixel-exact in the editor (Point filter, no compression).
public class PaletteTextureImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/Palette/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.npotScale = TextureImporterNPOTScale.None;
    }
}
