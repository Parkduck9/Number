
## Enemies

Each horizontal strip uses 64 x 64 frames. Key indices below are zero-based and preserve source RGBA pixels exactly. In-betweens are manually specified integer-pixel limb/cloth edits and plotted contours; no crossfade, resampling, antialiasing or new strip colors.

| New file | Frames | Target fps | Original key indices in new strip | Lowest opaque row (top = 0) |
|---|---:|---:|---|---:|
| `citizen_walk_4f.png` | 4 | 16 | 0, 2 | 61 |
| `citizen_b_walk_4f.png` | 4 | 16 | 0, 2 | 61 |
| `android_walk_4f.png` | 4 | 16 | 0, 2 | 61 |
| `bug_walk_4f.png` | 4 | 16 | 0, 2 | 61 |
| `bug_flyer_4f.png` | 4 | 16 | 0, 2 | 41 |
| `citizen_attack_5f.png` | 5 | 15 | 0, 2, 4 | 61 |
| `citizen_b_ranged_5f.png` | 5 | 15 | 0, 2, 4 | 61 |
| `android_attack_5f.png` | 5 | 15 | 0, 2, 4 | 61 |
| `bug_attack_5f.png` | 5 | 15 | 0, 2, 4 | 61 |
| `bug_flyer_attack_5f.png` | 5 | 15 | 0, 2, 4 | 41 |

All ten source `.meta` files specify PPU 32 and per-sprite custom pivot (0.5, 0.03125). Preserve these values when importing/slicing these staging PNGs; PNG itself does not store Unity pivots. No asset or meta was modified.

Matching `_preview.gif` files show OLD left and NEW right, 4x nearest-neighbour, on #0B1026. Both sides advance at the listed target fps. GIF centisecond timing alternates 60/70 ms to approximate 16 fps, and 60/70/70 ms for 15 fps. Walk/fly cycles repeat continuously; attack previews repeat each complete one-shot in original wind-up, strike, recovery order (no added recovery-to-wind-up tween). Independent old/new cycles span their least common multiple in the GIF.

Motion notes: citizen legs pass inward with alternating raised feet and a small sleeve swing; android retains rigid joints and planted ground height; crawler alternates leg groups; flyer has explicitly drawn downstroke/upstroke wing silhouettes. Attacks preserve the strike at index 2 and add an intermediate extension and settling pose.

Validation: all 10 strips checked for exact retained keys, original palette only, binary alpha, requested dimensions, unique added poses, and retained lowest opaque row. Enlarged contact sheets inspected for contour and joint continuity. Unity Play mode was not run; staging only.

AI assistance: Codex integer-pixel editing using local Python standard-library PNG/GIF tooling. No external asset or paid generation service used. This separate Enemies document avoids the concurrent Hero README write.
