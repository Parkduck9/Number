# Hero run v4 — spec implementation, visual acceptance incomplete

All work is confined to this directory. `Assets/`, gameplay/editor scripts,
v3 files, root credits and `FABLE_SPEC.md` were not modified.

**Status:** the required numeric assertions pass. The full visual acceptance
check does **not** pass: frames **2 and 6** still do not read as two clearly
separated arms, and their overlapping legs remain difficult to distinguish
at 4x. This is a reviewable concept, not an approved replacement sprite.
The helper deliberately exits with an assertion for the hidden far fists,
after writing all requested images and reporting the numeric results.

## Outputs

- `hero_run_8f.png`: 512x64 RGBA, eight 64x64 cells, transparent background.
- `hero_run_8f_preview_x4.gif`: 256x256, eight 50 ms frames, 20 fps,
  infinite loop, `#14121c` background, grey ground line at source row 62.
- `hero_run_compare_x4.png`: 2048x888, rows **OLD 6f / V3 / V4**.
  The six original cells are centered; all ground lines align within a row.
- `hero_run_limbs_x8.png`: 2048x1120, frames 0–3 above 4–7. Every panel
  contains the entire 64x64 cell enlarged exactly 8x, plus its frame number.
- `draw_run.py`: standalone Python standard-library helper, adapted from
  v3's inspected codec, cutout, transform and labeling functions.

## Changes versus v3

Replaced the old leg and arm pose tables with section 2 of `FABLE_SPEC.md`.
Hip/shoulder anchors, opposing arm selection `(i+4)%8`, far-arm translation
`(-2,+1)`, and bob `(0,+2,+1,-3)` now follow the spec. The leg reference
coordinates already include bob and are not shifted a second time.

Retained the v3 source head, collar, torso, sash, hem and accessory cutouts.
The whole 278-pixel head is translated forward four pixels, unchanged.
Torso shear remains `floor((38-y)*2/9)` (about 12.53 degrees). Hem and sash
retain v3's preceding-phase lag and integer tail offsets, with the new bob.
The near fist masks the later sash layer, as explicitly required by v4.

Sleeves now have five-pixel upper-arm interiors, a seven-pixel outlined
shoulder cap, and four-pixel forearm interiors. Near sleeves use ROBE with
a one-pixel LIGHT rim on their upper edges and a DEEP contact shadow on
the torso. Far sleeves use DEEP without a rim, grey cuffs, and darkened
skin. Foreground outlines and back-side INK edges separate exposed limbs.

The fist is a 5x6 adaptation of the source cutout: one redundant interior
column is removed while preserving both dark sides. A two-pixel cuff band
is oriented perpendicular to the forearm, on the elbow side of the fist.
This removes v3's fixed white block hanging below downward-pointing fists.

Trouser interiors are six pixels at the thigh and four at the shin, with
joint-only changes in thickness. Near thighs use ROBE, near shins DEEP;
far cloth is DEEP throughout. Shoes use the original flat/angled pixels,
with an ankle socket extension where needed. Grounded angled shoes select
six source rows; flight retains eight. Three rear-edge columns of the
angle cutout are cleared and the new edge outlined to keep the occupied
foot span at most 32 pixels **including both endpoints**.

Layer order is accessory, far arm, far leg, hem, near leg, torso, near arm,
collar, sash, head. The accessory remains behind the specified stack.

## Joint tolerance adjustments

Every rendered joint remains within one pixel **per coordinate** of its
reference table, including the derived far poses. Shoulders and hips are
exact. Small corrections reconcile the length/bend requirements with the
integer table; the reference values remain separately encoded in the helper.

Before bob, near-arm changes from the table are:

| Frame | Adjustment |
|---|---|
| 1 | Elbow `(29,32)` instead of `(29,33)` |
| 2 | Hand `(34,38)` instead of `(34,39)` |
| 4 | Elbow `(39,30)` instead of `(39,31)` |
| 5 | Elbow `(37,32)` instead of `(37,33)` |
| 6 | Hand `(35,40)` instead of `(35,39)` |
| 7 | Hand `(24,36)` instead of `(25,37)` |

For base leg phases 0–3 (coordinates already include bob):

| Frame | Adjustment |
|---|---|
| 0 near | Knee `(38,47)`, ankle `(42,53)` |
| 0 far | Knee `(26,49)`, ankle `(21,54)` |
| 1 near | Ankle `(34,55)` |
| 1 far | Knee `(24,49)` |
| 2 near | Ankle `(29,55)` |
| 3 near | Knee `(28,45)` |

Frames 4–7 exchange those leg columns with the specified hip/knee offsets.
Their far ankles use the permitted one-pixel vertical tolerance to retain
the ground and flight limits. The helper checks each against the literal
derived reference rather than against the corrected pose.

## Spec conflicts and unmet points

1. **Literal table versus bone lengths.** For example, frame 0's nominal
   near shin is `sqrt(80) = 8.94 px`, above the permitted 5–8 px range.
   Frame 1/5 upper arms are `sqrt(65) = 8.06 px`, above 6–8 px. Frame 4's
   forearm is also 8.06 px. The one-pixel changes above bring all lengths
   into their specified ranges. All knees project forward of the
   corresponding hip–ankle line; none are collinear.

2. **Exchanged soles versus ground/flight.** Moving the complete near
   column by `(-2,+1)` would put grounded soles on row 62 and the second
   flight sole on row 59. Those conflict with ground row 61 and flight
   rows 54–58. The export holds soles at 61 / 58 and adjusts the associated
   ankle within the allowed tolerance. Literal translated shoe bottoms
   are therefore not met in frames 4–7.

3. **Ankle registration versus unchanged source shoes.** The source flat
   shoe is seven rows tall and the angle shoe eight; the table requests
   flat heights of both seven and eight rows and angle heights of both
   six and eight. Socket extension / source-row selection resolves this;
   v3's shoes are consequently not immutable cutouts in this revision.
   The nominal global columns 17–49 cannot describe every table pose with
   the source angle's seventh-column ankle registration. The asserted
   constraint is the requested per-frame occupied span, at most 32 pixels.

4. **Two separated arms/legs in every frame remains unmet.** Nominal frame
   2 hands, after bob, are near `(34,40)` and far `(33,41)`: only one pixel
   apart on both axes, despite 5x6 fists and six-pixel outlined forearms.
   The far hand sits behind the near arm, near leg and sash. The related
   frame 6 pose has the same problem. Tried all 2,550 length-valid integer
   elbow/hand pose pairs within the one-pixel tolerance for phases 2/6
   using this fist cutout and required stack; none exposed both far fists.
   The final render retains the table tolerance and layer order rather
   than silently moving hands beyond them. Visible far skin counts are
   zero in both frames. The passing leg shoes also stack closely (foot
   spans 12 / 13 px); their knees have distinct outlines, but the full
   two-leg read is not clear enough at 4x. A revised passing-pose table or
   an explicit occlusion/layer exception is needed to finish this check.
   This is an observed limitation of this implementation, not a claim
   that every possible pixel interpretation of the spec is impossible.

## Verification

Inspected the final transparent strip at native size, the comparison at
4x, and all eight full-body panels at 8x. Corrected cuff direction,
shoulder registration, frame 3's previously hidden rear hand, rear-knee
bend, shoe width and ground clearance, then rendered and inspected again.
Exposed sleeves retain their outline/contact separation and no misplaced
white cuff block remains below a downward fist. The passing poses are
explicitly rejected by the visual check described above.

Required numeric assertions all pass:

- Every arm/leg joint is within one pixel of the original spec coordinates.
- Upper arms 6–8 px, forearms 5–8 px, thighs 7–9 px, shins 5–8 px.
- Shoulders fixed relative to bob; far arm exactly the opposite near pose
  shifted `(-2,+1)`, using the current phase bob.
- Inclusive foot spans: **32, 26, 12, 28, 28, 22, 13, 32 px**.
- Frame bottoms: **61, 61, 61, 58, 61, 61, 61, 58**.
- Neither shoe begins above row 46; no shoe extends below row 61.
- Every opaque sprite pixel belongs to the original 16-color palette;
  alpha is exclusively 0 or 255. Preview background is excluded from this
  sprite-palette check.
- All original head pixels, the reused collar and unmasked sash survive.
  Sash never overwrites the near fist.
- PNG round-trip matches; GIF has eight 50 ms delays and 256x256 cells.
- Source-art, v3 input and spec SHA-256 hashes are unchanged after export.

Independently decoded the GIF through Windows System.Drawing and compared
**every pixel in all eight 256x256 frames** against the strip at 4x plus
the specified background/ground. All pixels, frame counts and 50 ms delays
matched. No Unity import, script compilation or Play-mode change was made.

## Reproduction and provenance

From the repository root:

```text
python -B docs/design/concept/run_v4/draw_run.py
```

No packages, downloads, paid tools or image-generation API are used. The
helper writes only its four image exports beside itself. The final strict
visual assertion intentionally fails for frames 2/6 after reporting the
passing numeric checks; do not interpret the exported files as full approval.

AI-assisted asset revision: **Codex — local deterministic pixel drawing,
masking and compositing**. No external assets or new licensing dependencies.
Existing art attribution remains in root `CREDITS.md`; this revision's AI
record is here because the requested write scope excludes root files.
Git was not callable in this execution environment, so pull/commit could
not be performed. No repository metadata was changed.
