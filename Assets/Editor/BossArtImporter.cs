using UnityEditor;
using UnityEngine;

// Boss-v2 part art (Assets/Resources/BossArt/, from docs/design/concept/boss/parts_draft/assembly.json):
// PPU 32, point, uncompressed. Pivots: body bottom-left, arms bottom-centre (palm / blade tip), hatch pieces centre.
public class BossArtImporter : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPreprocessTexture()
    {
        if (System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/') != "Assets/Resources/BossArt") return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 32f;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.npotScale = TextureImporterNPOTScale.None;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        Vector2 pivot = name == "boss_body" ? Vector2.zero : name.Contains("_arm") ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(s);
    }
}
