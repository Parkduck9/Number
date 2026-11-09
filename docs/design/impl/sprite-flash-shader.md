# Sprite hit flash and overload

`Assets/Materials/SpriteFlash.mat` uses `NUMBER/SpriteFlash`. Assign this shared
material to character SpriteRenderers. The shader targets the URP **2D Renderer**
and ordinary SpriteRenderers; it does not add a 3D renderer fallback or SpriteSkin
variants.

Based on the installed URP **17.3.0**
`Shaders/2D/Sprite-Lit-Default.shader` in
`Library/PackageCache/com.unity.render-pipelines.universal@029be7ffa662`.
The Universal2D and NormalsRendering passes reuse URP's vertex, normal and shape
light includes, including all four 2D light blend styles, masks and secondary
normal textures. Transparent blending, depth writes off, vertex color, renderer
tint, flipX/flipY and normal SpriteRenderer sorting follow the stock shader.

| Property | Default | Use |
| --- | --- | --- |
| `_FlashColor` | White | Target RGB; its alpha is ignored. |
| `_FlashAmount` | 0 | 0..1 blend **after** lighting and tint; 1 gives full target RGB even under black lighting. Does not change alpha. |
| `_GlitchAmount` | 0 | 0..1 overload strength: sparse four-source-texel rows shift horizontally up to two texels, changing at 12 Hz. Adds a mild red/cyan split up to one texel. |
| `_MainTex`, `_MaskTex`, `_NormalMap` | White, white, bump | Stock sprite/secondary textures; SpriteRenderer supplies the sprite texture. |
| `_Color`, `_ZWrite` | White, 0 | Stock material tint and depth-write setting; normally leave unchanged. |

Flash is applied last, so full flash also covers the glitch colors. Glitch shifts
the sampled alpha, mask and normal together; the channel split changes RGB only.
The effect stays inside the sprite mesh and does not expand its silhouette.

Create and cache one `MaterialPropertyBlock` per controlling component (or reuse a
scratch block for sequential updates), and cache the three property IDs with
`Shader.PropertyToID` during initialization. With `block`, `spriteRenderer`,
`flashColorId`, `flashAmountId` and `glitchAmountId` already initialized:

```csharp
spriteRenderer.GetPropertyBlock(block);
block.SetColor(flashColorId, Color.white);
block.SetFloat(flashAmountId, Mathf.Clamp01(flash));
block.SetFloat(glitchAmountId, Mathf.Clamp01(overload));
spriteRenderer.SetPropertyBlock(block);
```

Set the amounts back to 0 when finished, including when returning an enemy to a
pool. Reading the block first preserves other systems' overrides. Use the shared
material; accessing `spriteRenderer.material` would create a material copy.

All custom material values have an identical `UnityPerMaterial` constant buffer
layout across both passes, suitable for SRP Batcher compatibility. Unity generally
routes renderers with an MPB outside the SRP Batcher path; shared-material reuse
still avoids per-instance material allocation. Check actual batching in Frame
Debugger rather than assuming MPB draws batch.

## WebGL and texture setup

- Targets shader model 3.0 for WebGL 2.0. Uses ordinary texture samples and scalar
  arithmetic, without compute, extra effect textures or additional render targets.
- At glitch 0, a uniform branch selects the original `CommonLitFragment` and
  `CommonNormalsFragment`: no row hash or extra texture reads. The branch and flash
  lerp remain; driver optimization and GPU timing require a real browser build.
- Active glitch adds two diffuse samples in the lit pass. The normal pass uses
  the same row offset, independent of light render-texture resolution.
- Prefer Point filtering, Clamp wrapping, Full Rect meshes and standalone sprite
  textures with a transparent border of at least three texels for glitch users.
  Offset samples can cross an atlas sprite's UV rectangle; atlases need sufficient
  transparent padding (at least four texels at mip 0), no packing rotation and
  validation with the actual filtering/mip settings. Tight meshes may clip shifts.
- No extra shader keyword is required to activate effects through an MPB. Light
  variants still depend on URP build stripping; include the planned 2D light blend
  styles in build scenes/settings and test those styles in the WebGL player.

## Verification in Unity 6000.3.28f1

Static checks cover package include paths, light variants, shared buffer layout,
defaults and the material-to-shader GUID. Unity import, shader compilation,
visual parity, actual SRP Batcher status and WebGL performance have not been run.

1. With both amounts 0, compare against Sprite-Lit-Default under the same 2D lights;
   verify tint/alpha, flipX/flipY, overlapping sorting layers and secondary maps.
2. Under a black Global Light 2D, set flash to 1: visible sprite pixels should be
   white while transparent pixels remain transparent. At 0.5 expect the RGB blend.
3. On two sprites sharing the material, use an MPB to flash/glitch only one; reset
   both amounts and check that its original rendering returns.
4. Enable normal-mapped 2D lighting and overload: shifted detail and normals should
   align. Check texture edges, atlas neighbors and the maximum effect strength.
5. Build WebGL 2.0 and repeat in a browser; check shader errors, light variants and
   draw cost with the effects both off and on.

The shader `.meta` is the one necessary metadata file: its GUID lets the provided
`.mat` resolve this custom shader immediately on import. Unity can generate the
other metadata files.

Source attribution: Unity Technologies ApS, URP package code under the Unity
Companion License (`LICENSE.md` in the package). Custom effect additions authored
with Codex. No external artwork or paid service was used; CREDITS.md was not edited.
