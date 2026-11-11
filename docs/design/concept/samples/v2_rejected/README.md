# Ahn Yudo: replacement pose sample

Concept samples only. No Unity assets or import settings were changed.
AI authored with Codex using PowerShell 5.1 and System.Drawing. No external
art, reference images, paid services or image generation were used. The
rejected v1 artwork was not opened or reused. CREDITS.md was left untouched
as requested; this attribution must accompany any later production adoption.

## Files and reproduction

1. Run `powershell -NoProfile -File build_pose.ps1`: draws the original
   **128x160 flat polygon sketch** to `sketch_pose.png`, then box-averages
   each 4x4 block and quantizes it to `hero_palette.txt`. Coverage below
   50% becomes transparent. Saves `draft_downsample.png` and `.txt`.
2. Run `powershell -NoProfile -File clean_frames.ps1`: applies the manually
   cleaned row runs to that draft. Saves `hero_idle_0.txt`, `_1.txt`, `_2.txt`.
   The text grids are the editable final pixel sources, one character per pixel.
3. Run `powershell -NoProfile -File render_samples.ps1`: validates the grids,
   renders `hero_idle_0.png`, `_1.png`, `_2.png`, reopens the PNGs to check
   palette/alpha, and saves `hero_idle_frames_x4.png` plus `validation.txt`.
   This step can run independently after direct text-grid edits.

The sheet has three 128x160 cells for frames 0,1,2 and one difference cell
for frame 0 versus 2. Background is #0b1030. Difference colors are diagnostic
only: muted blue = unchanged, ochre = frame 0 only, teal = frame 2 only,
sand = changed color. They do not belong to the sprite palette.

Playback: **0,1,2,1**, suggested 180ms per frame for review. Shoulders/head
rise 1px; trailing hem and goreum ends sway 1px at the crest. All visible
trouser, ankle tie and shoe pixels are fixed. Bbox is 32px high at rest and
33px on inhale. Native canvas is always 32x40.

## Visual review and limitations

- Inspected original sketch, native draft, native cleaned frame and 4x sheet.
  Rear-swept hem, bell cuffs, coat slit and distinct foot positions give an
  asymmetric, nonrectangular silhouette. Upper-left contour uses lighter blue;
  bottoms and occluded separations use darkest navy.
- One pale muted cyan lens crack pixel. Skin remains brighter than the crack;
  there is no eye glow. Goreum is the sole strong cyan body feature.
- The orphan check counts opaque pixels with no opaque eight-neighbor.
  The intentional one-pixel lens crack is attached to the head, so is not
  a detached speck. This is a connectivity check, not an automated art judgment.
- At 32px body height the coat hides parts of both knees, so the rear-weighted
  stance is less explicit than in the sketch. The baji ankle gathers and ties
  are only a few pixels; their cultural detail reads better in the sketch.
- Shoulder breathing uses a rigid 1px upper-block rise; it is deliberately
  restrained but slightly mechanical. A readable 22-year-old identity cannot
  be established from this face resolution alone.
- No actual Unity scene/480x270 gameplay composite was reviewed. This remains
  a sample for user art review, not an approved production import.
