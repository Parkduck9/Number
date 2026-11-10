# Hero run v5 — longer arm swing and articulated shoes

All outputs are confined to `docs/design/concept/run_v5/`. Existing scripts,
`Assets/`, v4 and root credits were not edited. This is a reviewable concept;
it has not been imported into Unity or marked as user-approved.

## Outputs

- `hero_run_8f.png`: 512x64 RGBA, eight transparent 64x64 cells.
- `hero_run_8f_preview_x4.gif`: 256x256, eight 50 ms frames, 20 fps,
  infinite loop, `#14121c` background, grey ground line at source row 62.
- `hero_run_compare_x4.png`: 2048x888, rows **OLD 6f / V4 / V5**.
- `hero_run_limbs_x8.png`: 2048x1120, complete cells 0–3 above 4–7,
  enlarged exactly 8x with nearest-neighbour integer replication.
- `draw_run.py`: v4 helper revised locally, Python standard library only.

## Preserved from v4

Eight frames, 20 fps, bob `(0,+2,+1,-3)`, fixed shoulders, torso shear
`floor((38-y)*2/9)`, unchanged 278-pixel head translated four pixels forward,
original collar, sash, hem/accessory cutouts and their phase lag. Original
16-colour palette, binary alpha, upper-arm/forearm widths, near sleeve rim,
torso contact shadow, far-arm darkening and cuff orientation are retained.
The far arm remains the opposite near pose shifted `(-2,+1)` using current bob.
Layer order is unchanged; the near fist still masks the sash.

Hips, support-leg poses and non-passing knees/ankles retain v4 coordinates.
Near thighs remain ROBE, near shins DEEP, far trouser cloth DEEP with INK
separation. Far shoes are darker than near shoes.

## Arms and the conflicting length requirements

The requested reach takes priority in this draft. This was stated in chat
after asking which conflicting numeric constraint should take precedence;
no reply had arrived when the draft was completed.

The fixed shoulder `(33,26)` is 17.03 px from the requested front hand
`(50,25)`. Two bones no longer than 8 px cannot reach it even when straight;
an elbow angle of 110–130 degrees needs longer bones. Consequently the
helper explicitly checks upper arms **9–10.5 px** and forearms **7–10.5 px**,
rather than pretending that the requested 7–8 px requirement passes.
Measured upper arms are 9.22–10.30 px; forearms are 7.07–10.44 px.

Before bob, the near back hand moves from v4 `(22,30)` to `(18,28)`;
the front hand moves from `(46,27)` to `(50,25)`. The back elbow is `(24,22)`;
the front elbow is `(42,31)`, giving approximately 116 degrees at full reach.
Far-arm extremes inherit exactly these changes. Shoulders remain fixed.

| Frame | Elbow | Hand centre |
|---|---|---|
| 0 | (24,22) | (18,28) |
| 1 | (23,28) | (30,29) |
| 2 | (31,35) | (41,36) |
| 3 | (37,35) | (47,32) |
| 4 | (42,31) | (50,25) |
| 5 | (34,36) | (41,35) |
| 6 | (24,30) | (28,37) |
| 7 | (23,27) | (21,34) |

Hand travel is monotonic across each half-cycle, with no duplicated/held
endpoint. Consecutive arc distances before bob are 12.04, 13.04, 7.21,
7.62, 13.45, 13.15, 7.62 and 6.71 px: easing at the ends, without v3's
near-stationary holds and 14–17 px jumps. Passing fists are repositioned
outside the thigh silhouettes; both fists remain visible in all eight cells.

## Shoes, ankles and passing legs

Replaced the flat/angled bitmap cutouts with native-grid outlines and
integer sole staircases using the same shoe colours. No bitmap rotation,
resampling, smoothing, downloads or image-generation service was used.

- Contact: front heel lowest, toe lifted about 24 degrees; rear toe-off
  also reaches row 61.
- Down: support sole flat on row 61, recovering foot angled down.
- Passing: support heel lifted with a bend at the ball and toe on row 61;
  swing foot relaxed downward and clearly ahead of the support shoe.
- Flight: rear foot relaxed with toe down, reaching foot slightly down.

The ankle sits three pixels from the rear of a roughly 10–11 pixel shoe,
above its rear third. The final two shin pixels narrow from a four-pixel
interior to three; a continuous outlined ankle joins the upper. Shoes have
one connected silhouette rather than a separate pasted socket.

Only the passing swing leg receives a pose correction: frame 2's far knee
moves from `(35,48)` to `(37,48)`, and ankle from `(30,52)` to `(34,54)`.
Frame 6 inherits the same correction through the existing leg exchange.
This small knee offset and the separated shoe positions expose the swing
leg beyond the support leg; their cloth interiors have a one-pixel INK
boundary. Support poses and all hip anchors stay unchanged.

## Verification

Inspected the final comparison at 4x and every complete frame at 8x, with
particular attention to 2/6, ankle joins, heel strike and toe-off.
The helper exits successfully and asserts:

- Fixed shoulders, opposite arm registration, longer reach, front elbow
  angle, monotonic hand travel and bounded consecutive arc distances.
- Revised bone-length ranges above, original leg lengths/knee bend,
  shoe attachment and four-connected shoe silhouettes.
- Actual heel/toe pixel heights for each phase, flat soles and toe-off
  contact on row 61, no shoe above row 46 or below row 61.
- Inclusive foot spans: **32,27,15,28,28,23,19,32 px**.
- Frame bottoms: **61,61,61,58,61,61,61,58**.
- Passing near fists do not intersect even the expanded thigh mask;
  exposed near/far trouser interiors have INK separation.
- Visible skin counts, near/far: **8/8,8/8,8/4,8/8,8/8,8/8,8/8,8/8**.
- Original palette and binary alpha, unchanged head/collar/unmasked sash,
  PNG pixel round-trip and source/v4 input SHA-256 preservation.
- Independently decoded GIF LZW pixels match all eight staged 4x frames;
  every delay is 50 ms and every cell is 256x256.

Reproduce from the repository root:

```text
python -B docs/design/concept/run_v5/draw_run.py
```

AI-assisted asset revision: **Codex — deterministic local pixel drawing,
masking and compositing**. No external assets, paid services or new licence
dependencies. Existing art attribution remains in root `CREDITS.md`;
this revision's disclosure is here because the requested write scope excludes
root files. Git is not on PATH in this environment; pull/commit could not
be performed. No repository metadata was changed.
