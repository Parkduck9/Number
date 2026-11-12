# HUD dice redraw v4 — review drafts only

AI 생성 (Codex), deterministic local Node.js integer-pixel edits from redraw_v3.
No image-generation API, external assets or paid services. Recorded in CREDITS.md.
Nothing installed in Assets/; v3 remains unchanged.

The same eleven 24×24 RGBA sprites and filename-only preview are retained.
Only old/new top and side pip locations change. Every front pixel (including
the rolling fronts), body shading pixel, outline pixel and alpha value remains
identical to v3. Active auxiliary pips retain v3's #BB213F; inactive auxiliary
pips retain the grey sampled from the inactive front pip.

Counts use xorshift32 with fixed seed **42**, drawing top then side in the order
below. Resting top/side counts are drawn from 1–5 excluding the front value,
and the side also excludes the top value. Inactive uses the same rule with
front 1. Rolling top/side counts are independent draws from 1–5. The instruction
to preserve fronts takes precedence for the rolling frames as well.

| File | Unchanged front | Top | Side |
|---|---:|---:|---:|
| dice_face_1.png | 1 | 2 | 4 |
| dice_face_2.png | 2 | 1 | 5 |
| dice_face_3.png | 3 | 5 | 2 |
| dice_face_4.png | 4 | 5 | 2 |
| dice_face_5.png | 5 | 3 | 1 |
| dice_face_6.png | 6 | 4 | 5 |
| dice_off.png | 1 | 4 | 2 |
| dice_roll_0.png | 1 | 3 | 2 |
| dice_roll_1.png | 2 | 1 | 1 |
| dice_roll_2.png | 3 | 5 | 4 |
| dice_roll_3.png | 1 | 4 | 3 |

Each pip is exactly one pixel. Patterns use the projected centre (1), diagonal
corners (2), diagonal corners plus centre (3), four projected corners (4), or
four corners plus centre (5). Every rendered pip has at least one empty pixel
between it and other pips and at least one interior face pixel before the
outline, including diagonal clearance. No opposite-face adjacency constraint
is imposed in v4.

Reproduce and validate with:

```sh
node docs/design/concept/dice_skills/redraw_v4/build_drafts.cjs
```

`pip_layout.json` records the seed, counts and exact coordinates. The builder
checks dimensions, counts, distinct resting values, spacing, edge clearance,
unchanged pixels/alpha, PNG round trips, unchanged filename headers, and hashes
of all v3 files and the eleven installed sprites. `preview_8x.png` reuses v3's
labels and exact nearest-neighbour 8× layout. Drafts reviewed at native and
enlarged scales; no Unity installation or gameplay test is involved.
