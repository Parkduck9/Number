# Hero redesign test — Plan B

Concept staging only. The current `Assets/Resources/DraftSprites/` files are unchanged.

| Files | Treatment | Frames |
|---|---|---:|
| `hero_idle_4f.png` | Full outfit redraw; finished from the interrupted run's `idle_generated.png` | 4 |
| `hero_run_6f.png` | Full outfit redraw; finished from `run_generated.png` | 6 |
| `hero_attack_5f.png` | Full outfit redraw; finished from `attack_generated.png` | 5 |
| `hero_walk_6f.png` | Original pose patched with the new outfit and props | 6 |
| `hero_jump_4f.png` | Original pose patched | 4 |
| `hero_dash_3f.png` | Original pose patched | 3 |
| `hero_parry_2f.png` | Original pose patched | 2 |
| `hero_hurt_1f.png` | Original pose patched | 1 |
| `hero_grapple_1f.png` | Original pose patched | 1 |

32 frames total; horizontal strips, 64×64 cells, no padding between frames. Standing idle is 59 pixels tall, from row 3 through row 61 (zero based). Grounded feet end on row 61. Crouched and airborne poses preserve the source height and vertical placement, including airborne run frame 3. The sword, flask and headband can extend sideways beyond the original silhouette.

The main redraws use the existing hero's navy, skin, white and teal color family, with a shared 16-color palette. The yellow paper is exactly `#F2C94C`; the red marks are small pixel marks rather than generated text. Alpha is strictly 0 or 255, with no dithering. Secondary patches keep more of the original anatomy and trouser detail and should be reviewed as patches, not equivalent full redraws.

- `../B_preview_4x.png`: idle, run and attack; original row immediately above each new row. `../B_preview_1x.png` is the native-size companion.
- `../B_preview_4x.gif`: the same three actions side by side, original above new, nearest-neighbor 4×. Each cycle advances one frame every 100ms (10fps). The 60-frame, 6-second GIF loops indefinitely; each action completes a whole number of cycles.
- `secondary_preview_4x.png` / `secondary_preview_1x.png`: old-above-new comparisons for the six patched strips.
- `validation.json`: frame bounds, palette, source/output SHA-256 hashes and GIF verification results.
- `build_B.cjs`: reproducible local export and outfit patching utility; uses the supplied `pixel_io.cjs` and Node standard library. Run from any directory with `node <path-to-this-folder>/build_B.cjs`.

References: `../dosa_concept_ab.png`, right-hand B figure (Google Gemini draft); each corresponding existing hero strip for pose order and proportions. The three generated PNGs from the previous run are retained unchanged. This continuation used local grid/palette cleanup and pixel patches; no new image-generation API call or external asset was used.

Checked at 1× and 4×: frame counts and ordering, grounded foot row, hard alpha, shared palette and all GIF frame pixels. In-game review remains outside this concept-only task.
