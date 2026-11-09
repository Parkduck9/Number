using UnityEditor;
using UnityEngine;

// Puts the SpriteFlash material (Codex shader NUMBER/SpriteFlash, docs/design/impl/sprite-flash-shader.md) on the hero and enemy
// "Visual" SpriteRenderers of the Player / TestEnemy prefabs. Every stage instantiates these prefabs without material overrides,
// so no scene rebuild is needed. PrototypeSceneBuilder calls Assign() too, so rebuilding the prefabs keeps it.
// Runtime side: SpriteFlash.Apply (solid flash + Android overload glitch); without this material it falls back to tints.
public static class SpriteFlashSetup
{
    const string MaterialPath = "Assets/Materials/SpriteFlash.mat";
    static readonly string[] Prefabs = { "Assets/Prefabs/Prototype/Player.prefab", "Assets/Prefabs/Prototype/TestEnemy.prefab" };

    [MenuItem("NUMBER/Apply Sprite Flash Material")]
    public static void Apply()
    {
        foreach (string path in Prefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (Assign(root.transform.Find("Visual"))) PrefabUtility.SaveAsPrefabAsset(root, path);
                else Debug.LogError("SpriteFlashSetup: no Visual SpriteRenderer or material for " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Debug.Log("SpriteFlashSetup: material applied to the Player / TestEnemy visuals.");
    }

    public static bool Assign(Transform visual)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var sr = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        if (mat == null || sr == null) return false;
        sr.sharedMaterial = mat;
        return true;
    }
}
