# SEG-B clean background plate

**Current deliverables: Pass 2 below.** The original `B_clean_*` plate is the retained Pass 1 source, not the runtime OFF base. `B_clean_neon_mask.png` and `B_clean_neon_preview.png` are **OBSOLETE** full-size lighting outputs, retained for history only; do not import them for the Pass 2 lighting setup.

Built-in OpenAI ImageGen, two passes using `13_style_frame_v2.jpg`: clean/style pass, then cleanup of gate/skyline/barrel compositing artifacts. No separately billed API or external asset downloads. AI disclosure for Claude: **AI-generated background (OpenAI ImageGen), geometry finishing and palette processing (Codex/Node.js)**. CREDITS.md was not edited.

Removed characters, red number panels, bugs, HUD, broadcast, lettering and face billboard. Replaced stacked pagodas with single low giwa roof caps; retained dancheong, lattice windows, stalls, stone walls and front-facing gate stairs. Indigo/blue shadows, amber lanterns, cyan panels and smaller magenta accents.

The generated image was 2048×683, not the requested high resolution. A centered 2046×682 crop preserves 3:1 without stretching; Lanczos enlargement produces **3240×1080**. This does not add original detail. Scene-specific strip adjustments and pixel cap painting are reproducible in `tools/art/align_seg_b.cjs`. Final reduction uses exact **2×2 box averages**, OKLab median-cut plus 12 weighted k-means refinements, **48 RGB colors**, no dithering, no final resampling. A cool near-white palette entry preserves moonlight.

## Platform measurements

Coordinates are native pixels; top-origin row = 540 − bottom height. Measurements refer to the finished horizontal upper cap/seam, not a sloping cloth hem or lower eave. Thin outlines and retained texture can occupy adjacent pixels. The guide file marks only the requested spans.

| Surface | x span | Top-origin cap row | Height from bottom | Target deviation |
|---|---:|---:|---:|---:|
| Street coping | 0–1619 | 508 | 32 | 0 |
| Left stone wall cap | 0–416 | 348 | 192 | 0 |
| Stall 1 upper seam | 0–234 | 374 | 166 | 0 |
| Barrel lid | 320–349 | 462 | 78 | 0 |
| Stall 2 upper seam | 346–531 | 386 | 154 | 0 |
| Stall 3 crate lid | 550–608 | 466 | 74 | 0 |
| Stall 3 upper seam | 618–851 | 386 | 154 | 0 |
| Continuous roof cap | 589–1306 | 300 | 240 | 0 |

Leading outline/texture pixels are not all at the cap row: ground coping begins around 34px from bottom (+2), wall perimeter around 195px (+3), and crate perimeter around 75px (+1). These are within the requested ±3px tolerance; the table gives the deliberate horizontal contact seams. Use the native image and guide together when placing colliders.

## Limits and verification

- Model geometry alone was insufficient. Roof start, wall end, cloth seams, barrel/crate lids and ground coping needed local corrections. The wall extension is replaced by a small foliage opening; its rectangular join remains somewhat visible. This is a finished generated plate with local pixel corrections, not a fully hand-authored tile set.
- Beyond x1306, a short background roof continuation recedes behind the gate, around row302 (238px from bottom). It is **not part of the measured walkable span**. The gate/stairs footprint is approximately x1265–1620 rather than starting precisely at1225. Left upper roof is approximately 340px above the bottom; gate roof and skyline are background only.
- **Obsolete Pass 1 mask method (not a current deliverable):** brightness ≥170/255, saturation ≥0.40, cyan/magenta/amber hue checks plus explicit emissive-region rectangles. Binary alpha only; no blur/bloom. Excludes most flowers, cloth and food. It is a color/region selection, not ground-truth lighting segmentation: some bright trim/window-frame pixels can remain, while pale low-saturation lamp cores and occluded lights can be omitted. Preview overlays these pixels over a 25%-brightness plate.
- `verify_bg.cjs` passed: hires 3240×1080, all native outputs 1620×540, 48 colors, opaque native alpha, mask alpha only 0/255, **31,438 mask pixels**, exact RGB agreement with native, complete guide coverage, and painted contact seams contrasting with the preceding raster row across over 90% of each span. Reviewed native, neon preview and 4× nearest-neighbor crops. These checks do not replace visual silhouette inspection.
- No Unity Play-mode or collider check was performed. No Assets/, C#, CREDITS.md or commits were changed. `git pull` could not run: git was absent from PATH and access to the GitHub Desktop installation was denied.

## Reproduce (PowerShell, repository root)

```powershell
$node = 'C:/Program Files/nodejs/node.exe'
& $node tools/art/align_seg_b.cjs docs/design/concept/stg01/B_clean_generated.png docs/design/concept/stg01/B_clean_hires.png
& $node tools/art/pixelize_bg.cjs docs/design/concept/stg01/B_clean_hires.png docs/design/concept/stg01/B_clean_native.png --colors 48 --neon-mask --neon-regions tools/art/B_neon_regions.json --guides docs/design/concept/stg01/B_clean_preview_guides.png
& $node tools/art/verify_bg.cjs
```

For another background, use `pixelize_bg.cjs` directly; omit the SEG-B alignment and region file. Input: 8-bit noninterlaced PNG. `--hires PATH.png` optionally saves the normalized plate. No npm packages, Python or network required.

## Pass 2 — OFF dusk base and individual neon sprites

2026-11-08. Deterministic Codex/Node.js pixel recolouring and sprite extraction; **no new ImageGen invocation, paid API, external assets or downloads**. Original plate provenance remains the ImageGen disclosure above. Read LIGHTING_DESIGN 1.3/2.3 and ANTI_SLOP before processing. This pass is explicitly confined to `docs/design/concept/stg01/` and `tools/art/`; no CREDITS.md, Assets/, C#, scene, prefab or commit changes. Git is unavailable on PATH, so pull/status could not be run.

### Current deliverables

| File | Purpose |
|---|---|
| `B_base_off_native.png` | 1620×540, opaque, exactly 48 used RGB colours; current OFF plate |
| `B_base_off_hires.png` | 3240×1080; exact nearest-neighbour integer 2× of final native, not new high-resolution detail |
| `B_base_off_preview_50pct.png` | Native OFF plate with each sRGB channel multiplied by 0.5 and rounded |
| `B_lit_composite_preview.png` | Native OFF plate plus every neon PNG at its CSV offset; normal binary-alpha paste, no simulated Light2D spill |
| `neon_B/neon_B_01.png` … `neon_B_11.png` | Tightly cropped individual neon sprites with binary alpha and pixel halos |
| `neon_B/neon_list.csv` | Import/placement contract; native top-left offsets, all `layer=near` |
| `neon_B/neon_manifest.json` | Descriptive labels, dimensions, tube colours and light metadata for auditing |
| `B_pass2_QA_4x.png` | Integer 4× inspection crops: billboard, stall tube, 50% roof and barrel |
| `B_pass2_verification.json` | Automated verification results, original eight spans and measured regional values |

### Geometry, palette and OFF treatment

The already aligned `B_clean_native.png` is the sole pixel-layout source. No pixels are translated, cropped, warped or rescaled before recolouring. The eight spans and cap rows in the platform table above are unchanged. Verification reruns **the existing `align_seg_b.cjs`** from the generated source and compares its high-resolution raster byte-for-byte with `B_clean_hires.png`; the temporary check image is removed. It does not apply alignment twice to an already aligned plate.

Artificial cyan/teal and gold/magenta window/panel pixels are reduced to dim glass/brown/indigo surfaces. Paper lantern bodies remain with compressed neutral paper values and no bright amber centre. Stall edge tubes have dark glass and subdued frames. Cloth is deep wine/indigo-red; blossom is dusty pink-grey; wood is muted brown. Natural moon highlights remain, with their former cyan shifted towards neutral moonlight. The scene has no new text or invented lettering.

Near/mid material values were raised while retaining dark outlines and mortar. Representative median max-sRGB-channel values (HSV V, **not linear luminance**) are wall **49.4%**, roof **40.4%**, ground **47.5%**, stalls **38.8%**. The stall rectangle includes shadows, metal and dark glass, so its overall median sits just below 40%; lit-material wood/food highlights remain distinct. These are broad regional statistics rather than a claim that every pixel is 40–60%. Visually inspected native, 50% preview and 4× crops: roof-tile rows, stone courses, coping, barrel, cloth edges and stair treads still read after halving.

Processing: native recolour → exact nearest-neighbour 2× staging master → existing `pixelize_bg.cjs --colors 48 --no-moon-anchor` (2×2 box reduction, OKLab median-cut and 12 k-means refinements) → native output → exact 2× final master. The new optional `--no-moon-anchor` avoids forcing a near-white centre into an OFF palette. If histogram lookup leaves a centre unused, it reassigns the highest weighted-error shared colour bin to an original measured RGB value until 48 colours are used. Default Pass 1 pipeline behaviour is unchanged.

### Neon inventory

Offsets include the halo bounding box. Do not centre the cropped PNG on the offset. Paste its **top-left pixel** at `(x_px,y_px)`. Left/top coordinates refer to the 1620×540 image. File paths below are relative to `neon_B/`.

| File | Subject | x, y | PNG size | Radius | Flicker |
|---|---|---|---|---:|---:|
| `neon_B_01.png` | Left skyline billboard perimeter, teal | 428, 110 | 143×51 | 192 | 0.3 |
| `neon_B_02.png` | Right skyline billboard perimeter, orange-gold | 1502, 104 | 67×55 | 192 | 0 |
| `neon_B_03.png` | Central horizontal skyline panel, vermilion | 790, 240 | 101×15 | 128 | 0 |
| `neon_B_04.png` | Glass tube at left lantern post, teal | 80, 116 | 27×41 | 80 | 0 |
| `neon_B_05.png` | Glass tube at middle lantern post, teal | 494, 262 | 25×33 | 80 | 0 |
| `neon_B_06.png` | Stall 1 edge strip, teal | 126, 410 | 47×15 | 80 | 0 |
| `neon_B_07.png` | Stall 2 edge strip, teal | 424, 418 | 45×13 | 80 | 0.3 |
| `neon_B_08.png` | Stall 3 edge strip, teal | 708, 410 | 71×19 | 96 | 0 |
| `neon_B_09.png` | Left skyline window strip, orange-gold | 278, 72 | 17×103 | 0 | 0 |
| `neon_B_10.png` | Central skyline window strip, teal | 740, 126 | 17×93 | 0 | 0 |
| `neon_B_11.png` | Right skyline window strip, vermilion | 1336, 166 | 67×11 | 0 | 0 |

Tube hues: vermilion `#FF4A3C`, orange-gold `#FFB347`, teal **`#3CFFDB`**. Requested default teal `#2EC4A8` has no 255 channel, so it is normalized approximately to preserve hue while satisfying the saturated-channel requirement. Optional sparse 1px core is `#FFF8E8`. The three halo shades are rounded 43%, 25%, 14% of each tube RGB, with a globally aligned 4×4 Bayer ordered-dither pattern over distances 1, 2 and 3–4 native pixels. Each sprite uses its one tube hue, three darker shades and optional white; no smooth blur, partial alpha, pink or cyan/pink mixing. Light colours are less saturated `#8AE0C8`, `#FF9B87`, `#FFD0A0`.

### Verification and limits

- `verify_seg_b_pass2.cjs` passes: dimensions, exactly 48 colours, opaque base, exact 2× replication, exact rounded 50% preview, inherited alignment, all eight cap spans, all sprite alpha values 0/255, fully transparent RGB zero, tight bounding boxes, saturated tubes, darker halos, CSV bounds and **exact pixel reconstruction** of the lit composite. Contact seams contrast with the row above over 93.2–100% of each span. Two sprites flicker at 0.3; all others 0. Radius 0 on the three skyline strips avoids unnecessary lights.
- Recolouring and tube selection are heuristic, constrained by known regions and the original 48-colour raster; this is not a semantic repaint of every tiny material. Existing decorative trim, paper ribs and distant window outlines remain as nonemissive texture. Some warm trim can still read as an ambient highlight; no intentional amber lamp core remains.
- Large billboards light their **perimeters**, leaving blank dim centres rather than large solid luminous rectangles. The third panel is an existing horizontal skyline strip, not an invented new sign. Tube contours follow the old raster and can inherit small irregularities/gaps; the third stall strip retains its original separated left segment. Optional gate lantern neon was not added; their OFF bodies remain.
- The high-resolution output contains no additional detail beyond native. Halo is a dark pixel overlay, not additive illumination. The lit composite is at full base value as requested; it does not preview Unity's colour space, light falloff, bloom, sorting or import settings. A 50% sRGB image is a readability proxy, not a URP rendering-equivalence claim.
- No Unity editor, runtime, collider, WebGL build or actual 960×540 gameplay-camera test was performed. Geometry is verified at the raster seams only. Pass 1's generated detail, foliage join and cultural/architectural ambiguities remain; this pass does not certify the scene as a hand-authored tile set.

### Reproduce Pass 2 (PowerShell, repository root)

```powershell
$node = 'C:/Program Files/nodejs/node.exe'
& $node tools/art/pass2_seg_b.cjs
& $node tools/art/verify_seg_b_pass2.cjs
```

Both scripts use built-in Node modules only. Pass 1 files remain untouched. For Unity integration later: use native plate, individual PNGs and CSV; Point filtering, no compression, PPU 32, Sprite-Unlit for neon. Those import settings were not applied in this scoped concept-art task.
