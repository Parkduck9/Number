# HUD dice redraw v5 — review drafts only

AI 생성 (Codex), deterministic local Node.js pixel authoring from the v4
palette and editable builder. No image-generation API, external assets or
paid services. Recorded in the root CREDITS.md. Nothing installed in Assets/.

Eleven transparent 24×24 RGBA sprites, plus `preview_8x.png` with the same
filename-only labels as v4. The high 3/4 camera gives the TOP an almost square,
dominant face. Front and left side are narrow shaded strips. Main top pips
are 2×2 pixels, matching the previous front pip size; auxiliary pips are
single pixels in the same dark red. Cyan outlines and edge highlights reuse
the exact v4 palette. The grey inactive die has a dim top 1 and grey strip pips.

Every sprite reaches row 23, with transparent surrounding pixels and canvas
centre x=11.5, suitable for a future bottom-centre pivot. No import settings
or game files are changed.

Seeded xorshift32 (seed 42), consumed front then side in file order. Resting
strip counts are 1–5, exclude the top result, and differ from each other.

| File | Top result | Front | Side |
|---|---:|---:|---:|
| dice_face_1.png | 1 | 2 | 4 |
| dice_face_2.png | 2 | 1 | 5 |
| dice_face_3.png | 3 | 5 | 2 |
| dice_face_4.png | 4 | 5 | 2 |
| dice_face_5.png | 5 | 3 | 1 |
| dice_face_6.png | 6 | 4 | 5 |
| dice_off.png | 1 (dim) | 4 | 2 |
| dice_roll_0.png | tumbling fragments | 3 | 2 |
| dice_roll_1.png | tumbling fragments | 1 | 2 |
| dice_roll_2.png | tumbling fragments | 5 | 3 |
| dice_roll_3.png | tumbling fragments | 4 | 3 |

Rolling frames change the projected cube geometry. Top marks are oblique,
partly edge-clipped, incomplete arrangements rather than a result pattern.
The last frame approaches the rest pose with a shortened, tilted top and a
deeper front strip; it retains an irregular partial pip arrangement.

Reproduce and validate:

```sh
node docs/design/concept/dice_skills/redraw_v5/build_drafts.cjs
```

`pip_layout.json` records counts, exact painted pixels and face geometry.
The builder checks palette membership, hard alpha, sprite dimensions, bottom
row, separated pips inside each face, resting auxiliary count restrictions,
dominant top area, PNG round trips, unchanged preview labels, and SHA-256
hashes of all v4 files and the eleven installed dice sprites.

Visually reviewed at native 1×, 4× and 8× against a dark background. At 1×,
the six resting results have distinct standard die patterns on the large top
face. This is a draft image review; no Unity gameplay or import test was run.
