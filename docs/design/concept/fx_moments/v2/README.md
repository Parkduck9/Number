# NUMBER moment FX — reconstructed exports

All eight requested effects were reconstructed with OpenAI Codex from the original README, exact manifest options, GIFs and contact sheets. These are new procedural sources matching the drafts, not byte-identical recovery of the lost files. The user approved all eight imports for this task. Original draft files remain historical references.

- Game assets: `Assets/Resources/FX/<name>.png`, `.json`, and both `.meta` files.
- Sources: `D:/Git/pixelfx/src/fx/<category>/number-<name>.js` (categories unchanged).
- Each game sheet is native scale 1, transparent, 40 fps, one-shot, with the original frame size and pixel anchor in `meta.pixelfx.pivot`. JSON uses the same Aseprite schema as `goal-open.json`.
- PNG and JSON import metadata are exact copies of the respective goal-open templates except for 16 fresh GUIDs. The PNG template has an empty slicing table, so no slicing entries were added. The JSON grid supplies frame rectangles.
- [overview.gif](overview.gif): OLD / NEW pairs, 3x nearest, on #0B1026. All effects trigger at 0.1 s on a 1.2 s review cycle. OLD includes the original mannequin; NEW shows the exported effect alone.
- Individual GIFs retain every 40 Hz sample, including the terminal blank sample. Alternating 20/30 ms GIF delays preserve speed to centisecond precision, matching the original preview lengths.

| Effect | Frames including blank | Timeline | GIF playback |
|---|---:|---:|---:|
| land-dust | 19 | 0.45 s | 0.48 s |
| grapple-hit | 11 | 0.25 s | 0.28 s |
| spawner-break | 33 | 0.80 s | 0.83 s |
| enemy-warp-in | 31 | 0.75 s | 0.78 s |
| sniper-muzzle | 7 | 0.15 s | 0.18 s |
| checkpoint | 31 | 0.75 s | 0.78 s |
| player-glitch-death | 21 | 0.50 s | 0.53 s |
| hack-pulse | 17 | 0.40 s | 0.43 s |

## Differences from the old drafts

The palettes, seeds, exposed parameters, canvas sizes, anchors, durations and FPS are preserved. Particle placement and intermediate silhouettes are reconstructed rather than recovered: dust clumps are more compact; plasma arc angles and six fragment trajectories differ; muzzle lobes, checkpoint glints and pulse rim gaps differ slightly. Three visual rounds checked all eight contacts, then revised dust decay, plasma arc lengths, checkpoint brightness and particle continuity. Native and 4x samples are included.

The largest intentional difference is actor removal. Warp-in exports the red frame and descending scan lines. Glitch-death exports cyan/magenta signal fragments, with no grey mannequin or filled character silhouette. The JS recipes accept an explicitly supplied `o.actor` for body-aware rendering, but have no automatic stock actor. A sprite sheet alone cannot progressively reveal or dissolve a live Unity character: its visibility must be controlled by the existing gameplay integration. No game code, scenes or prefabs were changed, and Unity Play mode/WebGL integration was not tested.

## Verification and reproduction

[validation.json](validation.json) records `8 pass, 0 fail, 0 with warnings`, plus 120 Hz sampling with the configured seed and seeds 2, 3, 7: zero color spill and zero terminal color/light. It also verifies exact JSON frame grids, pivots, FPS, schema keys and template-identical metadata with fresh GUIDs. [decode_validation.json](decode_validation.json) records independent Windows System.Drawing decoding of all GIF frames and all eight sheet dimensions / transparent terminal frames.

From `D:/Git/pixelfx`, using `C:/Program Files/nodejs/node.exe`:

```text
node records/number-moments-v2/build.mjs --export
node records/number-moments-v2/verify.mjs
```

The build invokes `tools/render.mjs <id> --opts <exact manifest JSON> --sheet --no-actor --scale 1 --out records/number-moments-v2/export` through structured Node argv to avoid PowerShell 5.1 quote loss. It retains existing export GUIDs when rerun. Local decoded old GIF frames in `records/number-moments-v2/old/` supply the comparison; sources and recipes do not depend on them. The local build uses a dependency-free GIF encoder.

The existing CREDITS.md Moment FX row already names all eight effects and goal-open, pixelfx (MIT, Patch / Hugo Duprez), and AI generation with Codex; no duplicate row was added. Recipe-specific credits remain in sources. No paid services were used. Both requested pulls failed because `.git/FETCH_HEAD` is read-only; the existing local checkouts were used. No commit was made.
