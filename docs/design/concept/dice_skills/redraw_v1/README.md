# HUD dice redraw v1 — review drafts only

AI 생성 (Codex), authored pixel by pixel with Python + Pillow from the existing
project HUD sprites. No image generation API, new external assets, or paid service.
No Assets files changed. Existing art provenance is recorded in the Dice HUD row
of the repository CREDITS.md (Gemini source, Codex cleanup).

All eleven sprites are 24×24 RGBA, use the exact existing palettes and silhouettes,
and have binary alpha. The six rest sprites retain the original 2×2 front pips.
Top/left pips are separated single pixels, foreshortened to the existing planes.
The inactive die uses the original three greys: middle-grey faces, dark-grey pips.

| File | Front | Top | Left side |
|---|---:|---:|---:|
| dice_face_1.png | 1 | 2 | 3 |
| dice_face_2.png | 2 | 6 | 3 |
| dice_face_3.png | 3 | 2 | 6 |
| dice_face_4.png | 4 | 2 | 1 |
| dice_face_5.png | 5 | 1 | 3 |
| dice_face_6.png | 6 | 5 | 3 |
| dice_off.png | 1 | 2 | 3 |
| dice_roll_0.png | 2 | 3 | 1 |
| dice_roll_1.png | 3 | 1 | 2 |
| dice_roll_2.png | 5 | 3 | 6 |
| dice_roll_3.png | 1 | 2 | 3 |

Fixed die normals: front +Z=1, top +Y=2, left −X=3, back −Z=6,
bottom −Y=5, right +X=4. Each visible triple passes the handedness check
`cross(front_normal, top_normal) == left_normal`; opposites sum to seven.
Rolling frames preserve the existing four tilted silhouettes and finish at rest.

`preview_8x.png` shows all eleven sprites in one horizontal row, enlarged with
nearest-neighbour sampling. Labels include each filename and front/top/left values.
`face_layout.json` records the exact pip pixel positions. Run `build_drafts.py`
from any working directory to reproduce the drafts and validate dimensions,
alpha, palettes, silhouettes, pip counts, handedness, and unchanged source hashes.

These files have only been reviewed as standalone pixel art. They are not installed
in Unity and have not been tested in the running HUD.
