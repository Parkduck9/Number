# HUD dice redraw v3 — review drafts only

AI 생성 (Codex), deterministic local Node.js integer-pixel edits from redraw_v2.
No image-generation API, external assets, or paid services. Recorded in CREDITS.md.
Nothing installed in Assets/; v2 remains unchanged.

All eleven sprites retain the 24×24 canvas and binary alpha. Only added pip
pixels differ from v2; silhouettes, outlines, body shading and the seven resting
front faces are pixel-identical. Top/side pips are single pixels, separated by
at least two empty pixels, with at least one face pixel between each pip and
the boundary (including diagonally). Active auxiliary pips use #BB213F, darker
than the unchanged front pips (#F22D52). Inactive auxiliary pips use the exact
grey sampled from the existing inactive front pip.

| File | Front | Top | Side |
|---|---:|---:|---:|
| dice_face_1.png | 1 | 2 | 3 |
| dice_face_2.png | 2 | 1 | 3 |
| dice_face_3.png | 3 | 1 | 2 |
| dice_face_4.png | 4 | 1 | 2 |
| dice_face_5.png | 5 | 1 | 3 |
| dice_face_6.png | 6 | 2 | 3 |
| dice_off.png | 1 | 2 | 3 |
| dice_roll_0.png | 1 | 3 | 1 |
| dice_roll_1.png | 2 | 1 | 2 |
| dice_roll_2.png | 3 | 2 | 3 |
| dice_roll_3.png | 1 | 2 | 2 |

Resting triples contain neither repeated nor opposite faces (opposites sum to 7).
Motion counts are intentionally unconstrained. Tumble front pips use 2×2 bright
red squares like the resting fronts; auxiliary pips remain single shaded pixels.

preview_8x.png uses exact 8× nearest-neighbour enlargement and the unchanged
filename-only header from v2. No face-value captions were added.

Run `node docs/design/concept/dice_skills/redraw_v3/build_drafts.cjs` from the
repository root to reproduce all PNGs and validate pip counts, adjacency,
clearance, separation, exact unchanged pixels, dimensions and alpha. It also
checks that every v2 file and the corresponding eleven installed sprites keep
their original SHA-256 hashes. Visually reviewed at native and enlarged scales;
not tested in the running Unity HUD.
