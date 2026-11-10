# Sniper and projectile sprite conversion

The two supplied Gemini PNGs are preserved as read-only inputs. All seven outputs
are RGBA PNGs in `Assets/Resources/DraftSprites/`, with matching Unity `.meta`
files derived from `citizen_b_ranged_5f.png.meta`.

| File | Frame size | Strip size | Frames / source selection |
|---|---|---|---|
| sniper_aim_5f.png | 64x64 | 320x64 | Source poses 1-5: stand, raise, aim, charge, fire |
| sniper_stunned_1f.png | 64x64 | 64x64 | Source pose 6 |
| sniper_walk_4f.png | 64x64 | 256x64 | Stand torso/rifle with integer-pixel alternating legs and lower-hem edits |
| bullet_sniper_2f.png | 16x16 | 32x16 | Row 1, copies 1 and 2 |
| bullet_android_2f.png | 16x16 | 32x16 | Row 2, copies 1 and 2 (left and middle of three visible diamonds) |
| bullet_citizen_2f.png | 16x16 | 32x16 | Row 3, copies 1 and 4 |
| bullet_bug_2f.png | 16x16 | 32x16 | Row 4, copies 1 and 3 |

Copy numbers count left to right within each row. Citizen copies 1/4 have the
largest remaining shading difference after reduction; the pellet silhouette is
intentionally steady. Bug copies 1/3 differ in the diagonal/vertical hanging tail.
The watermark overlaps bug copy 4, which is excluded completely.

Visible projectile bounds per frame: sniper 12x3, android 8x8 / 9x10,
citizen 8x8, bug 8x9 pixels. All use center pivots; a 16-pixel canvas includes
transparent padding around the 8-12-pixel-wide art (0.25-0.375 world units).

Sniper frames use the citizen's project palette, at most 16 opaque colors;
projectiles use 4-6 opaque colors each. All reduction and preview enlargement use
nearest-neighbour sampling. Alpha is exclusively 0/255, transparent RGB is zero,
and palette mapping removes green-screen edge contamination. No blur, dithering,
or generated image service was used. The fire and stunned poses are separated by
connected-component masks because their source bounding boxes overlap.

The standing sniper and citizen frame 0 are both 59 pixels tall, with visible feet
ending on row 61 (zero-based from the top). Aim/charge source poses are slightly
lower under the same scale; they have not been stretched independently. The fire
forearm/rifle is shifted two integer pixels downward to keep the barrel tip
within two pixels of the aim/charge height while preserving the recoil pose.

Every character rect has custom pivot `(0.5, 0.03125)`; bullet rects use
`(0.5, 0.5)`. Import settings: Multiple sprites, 32 PPU, Point filtering, mipmaps
off, compression off for Default/Standalone/WebGL. Names are `<file_stem>_N`.
Each new texture GUID, spriteID and internalID is unique; the name-to-ID tables
match the sprite entries. Rebuilding preserves the new metadata IDs.

`preview_4x.png` is a dark-background, exact 4x nearest-neighbour sheet containing
all seven strips and citizen/sniper scale comparison. `CREDITS.md` records both
source sheets as `AI 생성 (Google Gemini 원본 → Codex 정리)`.

Rebuild and check from the project root, using Node.js without dependencies:

```powershell
node docs/design/concept/sniper_bullets/build_sprites.cjs --build
node docs/design/concept/sniper_bullets/build_sprites.cjs --verify
```

Verification passed for PNG dimensions, distinct animation frames, binary alpha,
transparent padding, palette limits, green removal, ground alignment, sprite
rects/pivots, name-to-ID tables, IDs unique across existing Assets metadata, and
exact 4x preview pixels. Native-size and 4x sheets were visually inspected.

Unity import / Play mode were not run. In Unity **6000.3.28f1**, check the sliced
frames in the Sprite Editor, then review aim/charge/fire muzzle height and the
four-frame walk over the gameplay background when the code owner connects them.
No `.cs`, scene or prefab files were edited; Git was not used. Nothing was skipped.
