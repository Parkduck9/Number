# Plan B installation and nine parry guards

Installed 2026-11-10. Asset-only task: no Git commands, C# changes, scenes, prefabs, or hand-authored Unity metadata.

The nine previous hero strips were copied unchanged to matching `helper_*` names before installing Plan B. Every pre-existing hero strip had a B counterpart; missing counterparts: **none**. The old `hero_parry_2f.png` remains present and is the exact Plan B version. `hero_install_validation.json` records the original/helper and Plan B SHA-256 hashes.

New guards are right-facing horizontal strips, each 128x64 with two 64x64 frames and no spacing. Frame 0 is the impact guard; frame 1 moves the upper body one pixel back/down for recoil while leaving both shoes planted. Feet end on zero-based row 61. All colors belong to the existing 16-color Plan B palette; alpha is only 0 or 255.

| Height | Pose 1 | Pose 2 | Pose 3 |
|---|---|---|---|
| HIGH | Horizontal sword above head, raised elbows | Diagonal sword above brow, turned torso | Raised paper ward above forehead |
| MID | Upright sword, hands braced at chest | Diagonal sword with crossed arms | Extended chest-height paper ward, wide stance |
| LOW | Crouched horizontal sword at knee height | Low lunge, sword down and forward | Kneeling guard, ward beside front knee |

`parry9_preview_4x.png` is 1776x984, three rows (HIGH, MID, LOW), three pose groups per row, impact then recoil in each group. Background is exactly #0B0A14; enlargement is exact 4x nearest-neighbour. `parry9_preview_1x.png` is its native-size companion.

Two look-and-fix rounds were completed at 1x and 4x:

1. Initial reduction showed a crowded HI2 sword tip and thin sword/ward detail. Strengthened contours, blades and MID3 ward; saved the result as `parry9_preview_round2_*` after inspecting `round1`.
2. Inspection of that revision found doubled blade edges and an over-erased HI2 wrist. Restored the source's connected blade shapes, corrected warm blade pixels to gray, gave HI2 top clearance, filled the MID3/LOW3 wards with yellow and small red pixel marks, and retained a continuous exterior contour. The final preview was inspected again at both scales.

Validation passed: nine exact original/helper copies, nine exact B installations with original dimensions/frame counts, nine distinct new impact silhouettes, 18 different impact/recoil frames, 16 shared opaque colors, hard alpha, feet row 61, fixed shoe pixels, and exact nearest-neighbour preview scaling. This is image/file validation; Unity Play mode and gameplay selection/wiring were not exercised.

The source pose sheet was made with built-in OpenAI image generation, using the NEW B rows of the approved Plan B preview as reference. `parry9_prompt.txt` contains the complete prompt. `build_parry9.cjs` uses Node standard library and the existing `B/pixel_io.cjs` for palette/grid cleanup, local pixel corrections and recoil/export. Run `node docs/design/concept/dosa/build_parry9.cjs 2` to reproduce only the nine new strips and final previews; it does not rerun the installation or overwrite helpers.

## Files written

All 27 game PNGs below are in `Assets/Resources/DraftSprites/`:

| Preserved old hero (new file) | Installed B (overwritten file) |
|---|---|
| helper_attack_5f.png | hero_attack_5f.png |
| helper_dash_3f.png | hero_dash_3f.png |
| helper_grapple_1f.png | hero_grapple_1f.png |
| helper_hurt_1f.png | hero_hurt_1f.png |
| helper_idle_4f.png | hero_idle_4f.png |
| helper_jump_4f.png | hero_jump_4f.png |
| helper_parry_2f.png | hero_parry_2f.png |
| helper_run_6f.png | hero_run_6f.png |
| helper_walk_6f.png | hero_walk_6f.png |

New strips:

- hero_parry_hi1_2f.png
- hero_parry_hi2_2f.png
- hero_parry_hi3_2f.png
- hero_parry_mid1_2f.png
- hero_parry_mid2_2f.png
- hero_parry_mid3_2f.png
- hero_parry_lo1_2f.png
- hero_parry_lo2_2f.png
- hero_parry_lo3_2f.png

Additional written files, relative to the repository root:

- CREDITS.md
- docs/design/concept/dosa/B/README.md
- docs/design/concept/dosa/hero_install_validation.json
- docs/design/concept/dosa/build_parry9.cjs
- docs/design/concept/dosa/parry9_generated.png
- docs/design/concept/dosa/parry9_prompt.txt
- docs/design/concept/dosa/parry9_README.md
- docs/design/concept/dosa/parry9_preview_1x.png
- docs/design/concept/dosa/parry9_preview_4x.png
- docs/design/concept/dosa/parry9_preview_round1_1x.png
- docs/design/concept/dosa/parry9_preview_round1_4x.png
- docs/design/concept/dosa/parry9_preview_round2_1x.png
- docs/design/concept/dosa/parry9_preview_round2_4x.png
- docs/design/concept/dosa/parry9_validation.json
- docs/design/concept/dosa/parry9_validation_round1.json
- docs/design/concept/dosa/parry9_validation_round2.json
