# Map naturalness drafts — approval set

Drafts only. Nothing imported into Unity. No Assets, scripts, scenes, prefabs,
project settings, root credits or handoff files were changed.

## Native sizes and placement

Coordinates below use PNG convention: origin at top left, x right, y down.
All parts use PPU 32, scale (1,1,1), Point filtering, no mipmaps, no texture
resizing and lossless alpha. Align origins to the 1/32-unit grid. Detail strokes
primarily use the same two-source-pixel grid as wall_tile and rail_track.

| File | Native pixels | Placement / repetition |
|---|---:|---|
| facade_a.png | 140 × 86 | Transparent wall detail: one warm changho window and one closed/unlit window, restrained green/red lintel. Align its full canvas exactly with one existing wall_tile. |
| facade_b.png | 140 × 86 | Transparent wall detail: AC casing, vent, conduit and return pipe. Same alignment as A. |
| facade_c.png | 140 × 86 | Transparent wall detail: small hanji lantern and closed service panel. Same alignment as A. |
| girder_bracket.png | 96 × 64 | Wall attachment is LEFT, x=0..9; horizontal flange is TOP, y=0..9. Diagonal runs from the wall foot to the outer girder. Overlap flange into girder underside by 8–10 px and wall plate into wall by 8–10 px. Mirror horizontally for a wall on the RIGHT; do not rotate or stretch. |
| girder_chain.png | 128 × 32 | Two chains centred at x≈10 and x≈114. Repeat vertically every 32 px (the link rhythm is 16 px). Top/bottom core pixels join. Hide top 4 px in the carrying girder and bottom 4 px in the suspended girder. Clip the final repeat to the needed length; never stretch. |
| far_towers_strip.png | 960 × 400 | Transparent sky above and between towers; opaque silhouettes below. Repeat horizontally every 960 px (30 world units), with identical left/right edge columns. Bottom must sit behind the existing low skyline. Tallest mast begins at y=10; visible rise is 390 px (12.1875 units), enough to occupy the upper half of the 540-px view. |

The facade files are **transparent overlays**, not opaque replacements. This
keeps the requested transparent backgrounds and preserves every original wall
edge. Leave wall_tile underneath. The outer 8 px are untouched; top/bottom
cornices and horizontal joints come entirely from the original. Any A/B/C/plain
combination therefore inherits the existing wall's joins without introducing
new seams. Existing wall artwork itself is unchanged. If a coder wants one
sprite per tile later, bake `wall_tile + overlay` at 140×86 without resizing.

Keep most wall bays plain. Suggested starting rhythm for a tall column:
plain → A → plain → B → C → plain. Avoid an illuminated window on every bay.
The mockup packs the examples more closely so all three are reviewable.
Overlay must receive the same tint as its underlying wall (the current builder
uses 0.8). Use steel tint consistently with the corresponding rail; the mockup
uses 0.5 as a visual approximation. No lights, blooms or glow textures added.

The bracket and chain are decorative background supports. Draw them behind
walkable caps and actors. They do not imply new colliders. Use a bracket only
where the diagonal ends inside an actual tower wall; use chains only when
their upper end meets an actual carrying girder. The left upper girder in this
crop continues outside the camera, and the lower three hang from it in series.

Far strip order: sky/moon → new towers → existing skyline → buildings/platforms
→ actors/HUD. The source strip has no baked sky, moon, actors or foreground.
Use its supplied dim colours at full opacity; no blur or emissive treatment.
For later parallax integration, start with the existing skyline's horizontal
motion and anchor the bottom to that layer; choose any vertical parallax only
after checking both low and high camera positions. Do not reposition it to the
camera centre every frame. No airship was added; the skyline is the draft's
single focal change in the sky.

## Mockup construction and exact scale

- `mock_towers.png`: BEFORE left / AFTER right; STG01 seg1 bottom-right cell.
  Capture cell rectangle `(964,272)-(1444,542)`, restored to 960×540 with nearest
  neighbour, then native crop `(0,112)-(560,540)`. Final comparison is enlarged
  exactly 2×, 2264×904 px including a 24-native-pixel label band and 12-pixel gap.
- `mock_sky.png`: BEFORE left / AFTER right; STG02 seg1 top-middle cell.
  Capture rectangle `(482,0)-(962,270)`, restored to 960×540, native crop
  `(0,0)-(960,454)`. Final 2× comparison is 3864×956 px including labels/gap.
- The capture helper renders at **16 px/world unit**, while sprite PPU is 32.
  Its 480×270 camera cells therefore require a 2× nearest-neighbour restoration
  BEFORE native part compositing. Every added part is then placed at **1:1**;
  the final 2× review zoom is separate. No asset PNG is resized.
- `mock_*_after_native.png` show the AFTER crops at native scale. Capture detail
  lost during its original half-resolution render cannot be reconstructed.
- The sky composite uses a hard sky-colour matte, the source gate's alpha and
  the moon's exclusion bounds. The gate, moon, actors, digits and low roofline
  stay in front. This is a flattened-capture placement illustration, not a
  Unity render or play test. A few original sky-edge pixels may retain the
  capture's existing sampling; native parts have hard alpha throughout.
- Facades begin at x=320, row tops y=164,250,336,422 in the restored STG01 cell.
  Three right-wall brackets begin at `(234,250)`, `(234,346)`, `(234,442)` and are
  mirrored. Chains start at `(16,202)`, `(16,298)`, `(16,394)` and repeat/clip down
  to the next girder. The far strip begins at `(0,0)` in the restored STG02 cell.

## Visual review and checks

Initial comparison and two correction passes were rendered and opened:

1. Initial review: sky matte cut the towers at a horizontal line; two warm
   windows on every bay produced a repetitive grid. Pass 1 repaired the matte
   with sky chroma and gate alpha, unlit the second window, removed repeated
   edge windows, and tightened the chain attachment shoes.
2. Pass 1 review: skyline read as a row of outlined boxes with regular window
   dots; lower left girders still had no visible support. Pass 2 broke the long
   outlines into stepped shadow faces, grouped occupied floors, varied service
   shoulders and a relay silhouette, muted mast details, and continued the
   chains through the lower slabs. Final inspection closed a 2-px gap at the
   chain feet by clipping an additional repeat into each lower girder.
3. Final review: opened both BEFORE/AFTER boards, native crops, the 1×/4× part
   sheet, mixed facade tiling sheet and a two-repeat skyline strip. No soft
   edges, smooth gradients, noise texture or fake lettering were introduced.

`checks.json` records dimensions, palette counts and alpha values. Automated
checks require binary alpha, source-sized facades, untouched facade margins,
and identical skyline repeat-edge columns. Steel uses four exact rail colours;
far strip uses four exact colours from rooftops/palace skyline sources. Facade
warm tones come from the existing shop/hanji palette. All part palettes are
eight colours or fewer. Final Pillow validation also confirmed all 7,872 bright
foreground pixels in the sky crop are unchanged from the source capture.

## Source / rerun / handoff

`build_drafts.py` authors pixels using deterministic small shapes and composites
the scene with **Python + Pillow**. `pixels.py` supplies the integer raster
primitives. Rebuild from any directory:

```powershell
& 'C:\Users\User\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' docs/design/concept/map_natural/build_drafts.py --pass 2
```

Requires Pillow (the bundled runtime used here has Pillow 13.0.0). `--pass 0`
and `--pass 1` reproduce review boards in `review/`; default is final pass 2.
Optional `--parts-only` skips scene compositing. Outputs stay in this folder.

Palette/outline references, read only: `buildings/wall_tile.png`, `rail_track.png`,
`rail_pillar.png`, `shop_2f.png`, `gate_scanner.png`, `props/lantern_hanji.png`,
and `far/*_skyline.png` under `Assets/Art/Backgrounds/STG02_05_Parts/`.
STG01/STG02 `*_all.png` and `*_seg0_views.png` were also inspected for context.

AI/tool provenance is in this folder's `CREDITS.md`. Copy the supplied entry to
root CREDITS.md only if these drafts are approved for integration. Root credits
remain untouched because all task outputs are restricted to this directory.

Repository sync/commit was unavailable: `git pull` failed writing the protected
`.git/FETCH_HEAD`; `.git` is read-only in this session. No commit was created.

Suggested future Play-mode checks for the coder: high and low camera positions
in STG01/STG02; no facade seam at 140×86 boundaries; chains attach at both ends;
brackets meet actual walls; skyline repeat at 960 px; skyline remains behind
moon/roofs/actors; gameplay silhouettes stay clear. These were not run here.
