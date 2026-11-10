# Run v3 — larger motion, separated arms

Concept-only revision of v2. All files are in this directory; no existing scripts, `Assets/`, root credits, or earlier concepts were changed.

## Deliverables

- `hero_run_8f.png`: 512×64 transparent strip; eight 64×64 cells, left to right.
- `hero_run_8f_preview_x4.gif`: 256×256, 20 fps (eight 50 ms frames), infinite loop, background `#14121c`, ground line at source row 62.
- `hero_run_compare_x4.png`: 2048×888, rows **OLD 6f / V2 / V3**, aligned ground lines. Original six-frame row is centred.
- `hero_run_arms_x8.png`: 1536×800; frames 0–3 above 4–7. Each panel is an 8× crop of source columns 12–59 and rows 0–44, including both hands/cuffs and the sash.
- `draw_run.py`: standalone reproduction helper, adapted from v2; Python standard library only.
- `validation.json`: export properties, motion measurements, and read-only source hashes.

## Changes

**Arms:** Replaced v2's sheared sleeve cutouts with continuous sleeve tubes: three-pixel interiors and a one-pixel dark perimeter. Each pose has an explicit shoulder, bent elbow, wrist and fist. Near sleeves composite over the torso, retaining their separating outline. Far sleeves use the next darker existing shades and sit behind the torso; rear elbows tuck closer to the body to reduce their exposed area. Hand cutouts copy the source run-0 fist, including its skin shading and stepped outline. The cuffs use the existing white/grey palette as compact clusters. Sleeves have solid fills, with no dithering or checker texture.

Near-hand horizontal centres are `[26, 29, 43, 46, 49, 46, 29, 28]`: **23 px** travel. The forward fist rises to row 24 near the chin; the rear elbow reaches column 21 at row 28. Front/back arms oppose the corresponding legs. Passing poses 2/6 place one hand on each side of the torso instead of stacking them. Shoulder placement in 4/5 was adjusted to retain every source white collar pixel without cutting into the sleeve outline.

**Motion:** Torso row shifts are `floor((38-y)*2/9)`, a 1 px step per 4–5 rows, approximately **12.53°**. The original head moves forward four pixels as an intact cutout; no rotation, shearing, resampling or recolouring. Contact shoes extend three pixels farther at each end than v2: **6 px wider** overall. Push has a longer rear reach. Flight has a nearly horizontal leading thigh and a folded trailing leg with the heel raised toward the hip.

| Phase | Frames | Body screen offset | Foot contact |
|---|---|---|---|
| Contact | 0, 4 | 0 | Sole outline at row 61 |
| Down | 1, 5 | +2 (lower) | Sole outline at row 61 |
| Push/passing | 2, 6 | 0 | Sole outline at row 61 |
| Flight | 3, 7 | -4 (higher) | Lowest opaque pixel at row 52 |

The source short robe panel trails 2–5 pixels farther left, depending on phase. Source sash rows trail farther behind their knot. Both follow the preceding phase, preserving v2's one-frame lag. The source scabbard/gourd and original shoe cutouts remain in use. Collar, sash and head keep their source palette and pixel clusters.

## Self-check

Inspected the complete eight-frame strip at native scale and the OLD/V2/V3 comparison at 4×. Inspected all eight upper-body crops at 8×. Corrected passing-arm placement, reduced the far rear sleeve, expanded the arm-review crop to include every cuff, and adjusted the forward shoulder to protect the collar. Two hands and two leg poses remain distinguishable in each frame; far limbs are intentionally partly occluded by the torso/near limbs.

The helper asserts:

- Exactly 278 unchanged source head pixels in every frame, translated only.
- Every source white collar pixel and every transformed sash pixel survives compositing.
- All opaque colours belong to the original 16-colour palette; alpha is only 0 or 255.
- Every frame has one connected opaque silhouette (8-neighbour connectivity), with no detached stray pixels.
- At least 18 px of near-hand travel; grounded phases reach row 61; no pixels below row 61; flight leaves rows 55–63 empty.
- GIF block structure contains eight 50 ms delays and eight 256×256 images.

Independently opened and decoded all eight exported GIF frames through Windows System.Drawing, confirming 256×256, eight frames and 5-centisecond timing. This is a concept export; no Unity import or Play-mode test was performed.

## Reproduce and provenance

From the repository root: `python -B docs/design/concept/run_v3/draw_run.py`. The helper resolves all paths relative to itself and writes only to this directory. It reads the original run/idle/attack strips and v2 comparison strip; original source clusters and codec helpers come from the inspected v2 helper. No packages, downloads or image-generation API are required.

AI-assisted asset revision: **Codex — local deterministic pixel drawing, masking and compositing**. No external assets, new colours, paid services or licensing dependencies. Original hero-art attribution remains in the root `CREDITS.md`; this revision's AI disclosure is recorded here because the requested output scope excludes root files. Git pull/commit was unavailable because `git` is not callable in this environment. No repository metadata was written.
