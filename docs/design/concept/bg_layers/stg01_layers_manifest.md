# STG-01 layered background manifest

Output: `Assets/Art/Backgrounds/STG01_Layers/`  
20 PNG files: 6 far parts, 2 mid parts, 12 props/effects. Total PNG payload: 59,687 bytes. PNG files are RGBA, with alpha exclusively 0 or 255.

## Source grid and finishing

| Source | Source pixels | Measured dominant cell pitch | Recovered grid |
|---|---:|---:|---:|
| stg01_bg_far_gemini.png | 1024 × 338 | X ≈ 2.53 px; Y ≈ 2.52–2.55 px | 405 × 134 |
| stg01_bg_mid_gemini.png | 1024 × 339 | X ≈ 2.53 px; Y ≈ 2.54 px | 405 × 134 |
| stg01_bg_props_gemini.png | 1024 × 1024 | X/Y = 5.12 px | 200 × 200 |

Grid pitch was measured from periodic strong RGB edge energy in both axes. The Gemini images are softened/rescaled, so background pitches are approximate reconstruction measurements, not an assertion of the original authoring canvas. Each cell votes for its dominant 4-bit RGB colour cluster, then takes a modal actual source colour within the winning cluster. No edge averaging, bilinear filtering, or antialiasing was used. Nearby colours are merged to representative source palette entries; no dithering is introduced except deliberately sparse pixels for dissipating steam.

The recovered far/mid grid is enlarged exactly 2× to 810 × 268. This is an integer enlargement of the recovered art, matching the old 810 × 270 plate's width without fractional pixel scaling. Keep transform scale (1,1,1) at PPU 32; the image height is 8.375 Unity units. Align the mid layer 2 px below the old plate's top; do not stretch to 270 px. Stalls and the pole also use 2× enlargement; small props retain their recovered 1× cells. Stalls are 84 px high beside the roughly 44 px hero body, and the pole is 166 px high.

Green cells are removed, remaining green spill is neutralised, and transparent RGB is cleared to zero. Gemini sparkle marks were removed from the mid stone post, distant tower and puff area. The cloud-occluded moon is restored from its measured round silhouette. Sky has five hard bands plus one moon colour. The original empty broadcast frame is preserved. Neon sign glyphs are replaced with isolated geometric strokes; the holo board is blank. There are no new external assets or paid generation calls.

## Files and slicing

Sizes are pixels. Strip dimensions are the complete PNG dimensions; frame dimensions are the grid-slice size. Frames run left to right, equal width, without gutters. All animations loop; do not add a duplicate final frame. FPS values are suggested playback rates, not embedded timing. Pivots are normalised Unity coordinates from bottom-left. Ground props default to bottom-centre (0.5,0); floating exceptions and the lantern hanging anchor are explicitly listed.

| File | PNG size | Frame size | Frames | Suggested fps | Pivot | Opaque RGB colours, entire strip |
|---|---:|---:|---:|---:|---|---:|
| [far_sky.png](../../../../Assets/Art/Backgrounds/STG01_Layers/far_sky.png) | 810 × 268 | 810 × 268 | 1 | static | (0.5, 0) | 6 |
| [cloud_a.png](../../../../Assets/Art/Backgrounds/STG01_Layers/cloud_a.png) | 242 × 56 | 242 × 56 | 1 | static | (0.5, 0.5) | 4 |
| [cloud_b.png](../../../../Assets/Art/Backgrounds/STG01_Layers/cloud_b.png) | 164 × 30 | 164 × 30 | 1 | static | (0.5, 0.5) | 4 |
| [cloud_c.png](../../../../Assets/Art/Backgrounds/STG01_Layers/cloud_c.png) | 172 × 48 | 172 × 48 | 1 | static | (0.5, 0.5) | 4 |
| [far_skyline.png](../../../../Assets/Art/Backgrounds/STG01_Layers/far_skyline.png) | 810 × 268 | 810 × 268 | 1 | static | (0.5, 0) | 46 |
| [far_window_twinkle_4f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/far_window_twinkle_4f.png) | 3240 × 268 | 810 × 268 | 4 | 1 | (0.5, 0) | 1 |
| [mid_buildings.png](../../../../Assets/Art/Backgrounds/STG01_Layers/mid_buildings.png) | 810 × 268 | 810 × 268 | 1 | static | (0.5, 0) | 50 |
| [mid_lantern_glow_4f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/mid_lantern_glow_4f.png) | 3240 × 268 | 810 × 268 | 4 | 2 | (0.5, 0) | 4 |
| [stall_magenta.png](../../../../Assets/Art/Backgrounds/STG01_Layers/stall_magenta.png) | 104 × 84 | 104 × 84 | 1 | static | (0.5, 0) | 17 |
| [stall_cyan.png](../../../../Assets/Art/Backgrounds/STG01_Layers/stall_cyan.png) | 110 × 84 | 110 × 84 | 1 | static | (0.5, 0) | 20 |
| [steam_6f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/steam_6f.png) | 168 × 40 | 28 × 40 | 6 | 6 | (0.5, 0) | 6 |
| [lantern_red_4f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/lantern_red_4f.png) | 104 × 42 | 26 × 42 | 4 | 4 | (0.5, 1) | 23 |
| [power_pole.png](../../../../Assets/Art/Backgrounds/STG01_Layers/power_pole.png) | 60 × 166 | 60 × 166 | 1 | static | (0.5, 0) | 12 |
| [neon_sign_4f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/neon_sign_4f.png) | 84 × 31 | 21 × 31 | 4 | 3 | (0.5, 0) | 30 |
| [holo_board_4f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/holo_board_4f.png) | 260 × 34 | 65 × 34 | 4 | 4 | (0.5, 0.5) | 20 |
| [drone_scout_2f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/drone_scout_2f.png) | 72 × 18 | 36 × 18 | 2 | 10 | (0.5, 0.5) | 16 |
| [drone_delivery_2f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/drone_delivery_2f.png) | 84 × 32 | 42 × 32 | 2 | 10 | (0.5, 0.5) | 22 |
| [holo_ring_8f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/holo_ring_8f.png) | 416 × 24 | 52 × 24 | 8 | 8 | (0.5, 0.5) | 2 |
| [stone_lantern_2f.png](../../../../Assets/Art/Backgrounds/STG01_Layers/stone_lantern_2f.png) | 38 × 40 | 19 × 40 | 2 | 3 | (0.5, 0) | 17 |
| [crates.png](../../../../Assets/Art/Backgrounds/STG01_Layers/crates.png) | 41 × 33 | 41 × 33 | 1 | static | (0.5, 0) | 14 |

## Screen inner rectangle

For `mid_buildings.png`, the guaranteed clear axis-aligned inner rectangle is **(x=490, y=166, w=106, h=50)**, measured in output pixels from bottom-left. This is inset within the source's slightly sloping frame so a rectangular number render cannot touch it. Top-left image coordinates: (490,52), size 106 × 50. The rectangle is uniformly RGB (27,36,57), alpha 255. Do not include the bright rim when placing text. The screen remains empty in the preview.

## Registration and animation notes

- **far_sky.png:** Opaque five-band sky; restored moon; no clouds.
- **cloud_a.png:** Drift 0.5–1 native output px/s; no baked sky.
- **cloud_b.png:** Drift 0.5–1 native output px/s; no baked sky.
- **cloud_c.png:** Drift 0.5–1 native output px/s; no baked sky.
- **far_skyline.png:** Repeat X; left/right RGBA columns identical; 268 px full-height registration.
- **far_window_twinkle_4f.png:** Registered to far_skyline; sparse on/off windows.
- **mid_buildings.png:** Screen safe inner rect (bottom-left): (490,166,106,50). Full interior is slightly sloped; use this inset.
- **mid_lantern_glow_4f.png:** Opaque replacement glow pixels over dim base, same registration.
- **steam_6f.png:** Anchor above pot; rise/fade by density, binary alpha; three staggered puffs.
- **lantern_red_4f.png:** Top-centre fixed hanging point; body swings ±2 px.
- **neon_sign_4f.png:** Abstract separated polygon/diamond/dash strokes; no text. Frames on,on,dim,off.
- **holo_board_4f.png:** Floating centre pivot; blank cyan panel, scanline wraps through frame border.
- **drone_scout_2f.png:** Centre pivot; rotor-only changes; body and cargo fixed.
- **drone_delivery_2f.png:** Centre pivot; rotor-only changes; body and cargo fixed.
- **holo_ring_8f.png:** Floating centre pivot; thin ellipse; travelling highlights and cyclic projected height.

- The 12 props/effects are the two stalls, steam, red lantern, power pole, neon sign, holo board, two drones, holo ring, stone lantern and crates. The two redundant ring drafts in the source sheet are represented by one animated ring.
- Stalls contain no baked steam. Steam uses staggered source-puff fragments, integer upward movement, greys and pixel-density fade; its six-frame state wraps directly. Keep its bottom-centre anchor immediately above the pot.
- Mid glow uses four distinct brightness steps on precisely the same pixels; its base layer is dimmed at those pixels so the opaque overlay can both brighten and dim. It is a normal colour overlay, not an additive full-image material.
- Far twinkle is only nine candidate window pixels on the recovered grid (2 × 2 output pixels each), with different windows absent in different phases. The base at those coordinates is dark.
- Lantern: fixed top-centre attachment, ±2 px horizontal body sway. Ring: eight-phase projected ellipse pulse and rotating highlights. Stone lantern: only the flame changes. Drone propellers change while bodies and delivery cargo remain rigid.
- The skyline is horizontally repeatable, with identical first/last RGBA columns and a shared low building at the seam. Sky/mid/stalls are not claimed to be horizontally tileable.

## Preview

[stg01_layers_preview.gif](stg01_layers_preview.gif): **2430 × 810**, 48 frames, 4 seconds, looping, nearest-neighbour 3× enlargement of an 810 × 270 composition. GIF delays repeat 80/80/90 ms, averaging 12 fps. A single 256-colour preview palette avoids palette flicker; use the PNGs for the game. The GIF is documentation only and sits outside Assets.

Composition (coordinates from top-left on the 810 × 270 canvas): sky (0,0), skyline/twinkle (0,-80), mid/glow (0,2); magenta stalls at (164,152) and (477,152), cyan stall at (327,152), pole (625,70). Clouds drift slowly with integer pixel offsets. All 20 parts appear. The old background was used for rough composition and ground height. A separate temporary check placed the existing 64 × 64 hero frame at native size beside the stalls; the hero is not baked into the supplied assets or GIF.

## Unity handoff (settings only; no meta/scene/script edits)

- Texture Type Sprite (2D and UI); PPU **32**; Filter **Point**; Compression **None**; mipmaps off; Non Power of 2 scaling **None**; preserve source alpha.
- Use Sprite Mode Multiple for strips, Grid by Cell Size using the frame dimensions above, with zero offset/padding. Static PNGs use Single. Recommended Mesh Type Full Rect for exact overlay registration.
- Set Max Size **4096** for the two 3240 px-wide overlay strips so Unity does not reduce their size. Other textures need a maximum at least as large as their longest side.
- Repeat X for `far_skyline.png`; other textures Clamp. Repeat the sliced twinkle overlay at the same world offsets, without sampling across strip-frame boundaries. Keep all object positions pixel-aligned at increments of 1/32 world unit.
- Draw order: sky → clouds → skyline → twinkle → buildings → mid glow → props and their local effects. Overlay frame and base must share the same pivot, position and scale. This order and the preview placements are suggestions for the integration owner.

## Verification

Passed for all 20 PNGs: expected names and dimensions; equal-width strips; alpha only 0/255; no green-dominant fringe pixels; transparent RGB zero; fully opaque sky; actual variation in every animated file; skyline seam equality; overlay pixels registered inside their opaque bases; exact screen interior colour/rectangle; invariant drone body/cargo pixels. Static composite reviewed at native size, source-derived art/animation edges at 4×, and hero scale in context. The GIF was decoded using Windows System.Drawing and confirmed as 2430 × 810 / 48 frames, with an interior frame visually inspected.

Unity Play mode and WebGL import/render have not been run: scripts, scenes and .meta files were intentionally not modified. Integration owner should verify Point/PPU32 settings, all loops (including last-to-first transitions), hanging anchor, overlay ordering and horizontal skyline repetition in Play mode.

Git pull was attempted before asset work but .git/FETCH_HEAD was not writable in this session. Commit also requires a session with .git write access. Existing unrelated working-tree files were not changed.
