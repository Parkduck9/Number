using UnityEngine;

// Hit / parry flash hook. A sprite tint multiplies, so on the white draft sprites a "white flash" is invisible.
// If the renderer's material has _FlashAmount (the SpriteFlash shader, made by Codex: Assets/Materials/Shaders/SpriteFlash.shader)
// the flash paints the sprite solid flashColor via a MaterialPropertyBlock; otherwise it falls back to tinting with fallbackTint.
public static class SpriteFlash
{
    static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
    static readonly int FlashColor = Shader.PropertyToID("_FlashColor");
    static readonly int GlitchAmount = Shader.PropertyToID("_GlitchAmount");
    static MaterialPropertyBlock block;

    // glitch 0..1: shader row-shift glitch (Android overload); ignored without the shader. Called every frame, so values reset by themselves.
    public static void Apply(SpriteRenderer sr, Color color, bool flashing, Color flashColor, Color fallbackTint, float glitch = 0f)
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
        block.SetFloat(GlitchAmount, glitch);
        sr.SetPropertyBlock(block);
    }
}
