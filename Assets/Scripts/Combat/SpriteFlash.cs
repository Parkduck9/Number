using UnityEngine;

// Hit / parry flash hook. A sprite tint multiplies, so on the white draft sprites a "white flash" is invisible.
// If the renderer's material has _FlashAmount (the SpriteFlash shader, made by Codex: Assets/Materials/Shaders/SpriteFlash.shader)
// the flash paints the sprite solid flashColor via a MaterialPropertyBlock; otherwise it falls back to tinting with fallbackTint.
public static class SpriteFlash
{
    static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
    static readonly int FlashColor = Shader.PropertyToID("_FlashColor");
    static MaterialPropertyBlock block;

    public static void Apply(SpriteRenderer sr, Color color, bool flashing, Color flashColor, Color fallbackTint)
    {
        Material m = sr.sharedMaterial;
        if (m == null || !m.HasProperty(FlashAmount))
        {
            sr.color = flashing ? fallbackTint : color;
            return;
        }
        sr.color = color;
        if (block == null) block = new MaterialPropertyBlock();
        sr.GetPropertyBlock(block);
        block.SetFloat(FlashAmount, flashing ? 1f : 0f);
        block.SetColor(FlashColor, flashColor);
        sr.SetPropertyBlock(block);
    }
}
