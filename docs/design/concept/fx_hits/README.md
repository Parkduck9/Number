# Combat hit drafts — approval required

Review only. This run does not modify `Assets/`, `CREDITS.md`, Unity scripts, or scenes. Game integration requires user approval.

[All nine candidates](all_variants_contact.png) compares two recolored library effects (A/B) and the selected compact draft (C) for each request. Stock canvases are fitted to a 96px comparison box with nearest-neighbor sampling; chosen effects retain their target pixel size. Each chosen contact sheet shows every frame at 4x and samples at native 1x.

| Selected draft | pixelfx ID / source file under `D:/Git/pixelfx/src/fx/` | Size | Frames / fps | First empty frame |
|---|---|---|---|---|
| [kill-burst](kill-burst.gif) / [frames](kill-burst_contact.png) | `impacts/number-kill-burst.js` | 96 x 96 | 18 / 50 | 0.34 s |
| [impact-ring](impact-ring.gif) / [frames](impact-ring_contact.png) | `impacts/number-impact-ring.js` | 64 x 64 | 10 / 50 | 0.18 s |
| [parry-clash](parry-clash.gif) / [frames](parry-clash_contact.png) | `impacts/number-parry-clash.js` | 80 x 80 | 15 / 50 | 0.28 s |

Frame counts include the empty endpoint. With a normal 20ms endpoint, clips occupy 0.36 / 0.20 / 0.30 seconds. GIFs use exact 20ms active frames, nearest-neighbor 4x enlargement, navy `#0b1026`, and a **600ms empty pause** before repeating for review. Effects themselves are one-shots. White is confined to the contact flash/core. Kill flashes white for exactly one 20ms frame. Its four falling data cells are geometric squares, not text. Parry assumes the attacker is to the right; sparks reflect right, while its cross silhouette distinguishes it from a hit.

## Recipes and alternatives

All chosen drafts use their source file's default options, `seed: 11`, and no option overrides. Common cyan ramp: `#102039 #16425b #20758b #25a7bf #4ff0ff #97f8ff #c7fcff #ffffff`. Kill ramp: `#24182e #432135 #773145 #ad384d #f24f60 #ff7d82 #ffc6be #ffffff`.

- Kill: `spread: 1.2, fragments: 7, sparks: 12`; origin `[28,42]`. A: `impacts/crit-hit`, B: `scifi/emp-blast`, recolored red. Rejected for the long shard tail and the delayed dome/strobe; neither supplied compact mechanical debris plus falling cells.
- Impact: `radius: 26, rays: 8`; origin `[32,32]`. A: `pickups/collect-burst`, B: `explosions/plasma-explosion`, recolored cyan. Rejected for pickup twinkles/double rings and the large plasma core.
- Parry: `sparks: 10, fan: 0.95, arm: 29`; origin `[32,40]`. A: `impacts/parry`, B: `impacts/shield-block`, recolored cyan. Rejected for the symmetric lingering spray and shield lattice.

A/B render options: the corresponding ramp above, `seed:11, light:0.5`, no preview actor; EMP `fringe:'#f24f60'`, plasma `accent` set to the cyan ramp, shield `sparks:['#c7fcff','#4ff0ff','#25a7bf','#20758b']`. Other source defaults remain unchanged. These are unmodified library modules.

Three inspected rounds: initial draft; refinement 1 separated debris, shortened ticks, and directed parry sparks; refinement 2 tightened the post-flash frame, data cells, ring decay, and parry cooling. Local iteration evidence and `export.mjs` remain in `D:/Git/pixelfx/records/number-hit-drafts/`. With ffmpeg unavailable, a local GIF encoder exported the pixelfx buffers; Windows' GIF decoder verified all frames and delays.

Validation: `node tools/check.mjs impacts/number-kill-burst impacts/number-impact-ring impacts/number-parry-clash` — **3 pass, 0 fail, 0 warnings**. Ends, seeds, and bounds pass. Unity placement/layering is untested. Git pull was blocked by `.git` write restrictions; no commit was made.

Provenance: AI-authored with Codex using the local [pixelfx](https://github.com/patch-gg/pixelfx) library, MIT, Copyright (c) 2026 Patch and Hugo Duprez. Chosen choreography is new; stock comparison modules retain their Sprite Fusion credits. Full library license is included in [LICENSE.pixelfx.txt](LICENSE.pixelfx.txt). This draft record does not change game credits; approval and any later import require the normal credits update.
