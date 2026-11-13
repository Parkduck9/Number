# HP HUD drafts

Drafts only. Nothing imported into Unity. No game code, Assets files, root credits, or commits changed by this task.

## Options

**A — Paper talismans:** yellow paper with manually placed red seal strokes; lost HP becomes an asymmetric burnt grey stub.
The closest match to the dosa theme and existing checkpoint talisman; tearing removes most of the yellow silhouette.

**B — Red hearts:** solid red hearts become dark hollow hearts, preserving all five countable positions.
The most immediate conventional health symbol; the lose strip cracks the heart and separates its pieces.

**C — Yeopjeon:** brass coins with square holes become grey coins with a broken lower rim.
Connects luck and Joseon material culture; round square-holed silhouettes differ from the cyan diamond dice pips.

## Files and dimensions

| Option | Full | Lost | Lose strip | Mockup |
| --- | --- | --- | --- | --- |
| A | `A_full.png`, 10x16 | `A_empty.png`, 10x16 | `A_lose.png`, 30x16 | `mock_A.png`, 960x432 |
| B | `B_full.png`, 10x16 | `B_empty.png`, 10x16 | `B_lose.png`, 30x16 | `mock_B.png`, 960x432 |
| C | `C_full.png`, 12x16 | `C_empty.png`, 12x16 | `C_lose.png`, 36x16 | `mock_C.png`, 960x432 |

`preview_all.png` (960x1944) combines the three mockups with full/lost samples and each lose strip. `pixels.json` preserves the exact pixel matrices and palette, with `.` meaning transparent. All PNGs use 8-bit RGBA with alpha restricted to 0/255.

Animation: three equal-width cells, left to right, no padding. Frame 0 = crack/tear starts; frame 1 = pieces separate; frame 2 = lost icon, identical to the standalone empty PNG. Suggested review timing: 80 ms / 100 ms / 100 ms, once, then hold the lost icon. This is an animation proposal, not an implementation.

Each icon uses a 16-pixel-high alignment cell; the heart artwork itself is 10x9. Five HP cells use a 15 px horizontal pitch, leaving at least 3 px between silhouettes. Light comes from the upper left. New icons use at most 7 opaque colors and a 1 px dark silhouette outline.

## Mockup scale and placement

Every mockup begins as a 160x72 native-pixel HUD crop on `#0E1428`. Native positions: bezel (8,8), face (12,12), HP icons (72 + 15*i,18), dice pips (72 + 10*i,46). The HP label and 3/5 counter are explicitly draft additions, drawn with a custom 3x5 pixel alphabet; DICE labels the separate charge row.

The 56x56 bezel surrounds the 48x48 `face_0` portrait. HP shows exactly 3 full + 2 lost; dice shows 4 full + 3 empty. Everything scales together by nearest-neighbor 2x to the game HUD size (320x144), then nearest-neighbor 3x for review (960x432). Thus one source pixel is a 6x6 block in each mockup. The preview headers are review annotations.

## Review

Reviewed at native scale, 4x, and in a 960x540 navy canvas with the HUD at game 2x scale. Checked restricted palettes, binary alpha, integral scaling, consistent frame cells, matching final lose/empty frames, exactly five HP positions, and exactly seven original dice pips. No Unity import or Play-mode verification was performed or required for these standalone drafts.

For a future integration, review whether the added HP 3/5 text should remain. The portrait's existing degraded states can still provide secondary feedback. These proposals do not alter the five-hit rule.
