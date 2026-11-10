# Run v2 — source-cluster revision

Eight 64×64 cells in `hero_run_8f.png` (512×64). Playback: **20 fps**, eight 50 ms frames, 0.4 s per loop. Concept review only; nothing installed into the game.

## Changes from v1

- Replaced v1's newly drawn torso, collar, sash and scabbard with masked **original run-frame pixels**. The original head, hair tie, face and visor are one intact cutout: all 278 opaque head pixels survive unchanged in every frame.
- The collar/lapel/belt torso is tilted by integer row shifts, one pixel per six rows (about 9.5 degrees). The head is translated three pixels forward without shearing, recolouring or changing its shape. No smoothing or resampling is used.
- Replaced the thick line-drawn arms with the original run-0 bent sleeve/cuff/fist clusters, plus the run-2 passing sleeve. Whole-pixel offsets/shears create pumping; the far arm uses the next darker existing shades and sits behind the torso. The near arm cannot obscure the protected collar or sash.
- Redrew only the trouser legs, using separate tapered thigh/calf shapes, one-pixel dark contours and joint creases. Near thigh uses the source robe shade; far thigh is one shade darker. The narrower lower legs and knee bends keep the two silhouettes distinct.
- Copied both shoe silhouettes, including all white/grey sole pixels, directly from run frame 0. No replacement white rectangles.
- Reused the original short robe panel. Its offset follows the previous phase. The original gold/red sash rows trail behind their source knot with the same one-frame lag; no new ribbon colours or isolated detail dots.
- Kept the original scabbard and gourd pixels. They move with the body and sit behind the limbs.

## Timing and ground

Zero-based phases: 0 contact, 1 down, 2 push/passing, 3 flight; frames 4–7 repeat with near/far limb ownership exchanged. Screen-space torso offsets are `[0, +2, 0, -3]`: down is two pixels lower, flight is three pixels higher. Grounded shoe outlines reach row **61**; the preview ground line is row 62. In both flight frames the lowest pixel is row **54**, leaving rows 55–61 empty. The leading knee is raised and the trailing leg folds back.

## Review and checks

- `hero_run_compare_x4.png`: rows **OLD 6f / V1 8f / V2 8f**, with equal 64×64 cells, 4× integer scaling and aligned ground lines. The six-frame strip is centred in the eight-cell row.
- `hero_run_selfcheck_x4.png`: every new frame immediately beside original run frame 0. Inspected all eight pairs at 4× and inspected the finished strip at 1×. Corrected the low forward hand, protected collar/sash overlaps, trouser shading and flight limb order during review.
- Helper assertions verify exact protected head/collar/torso/sash pixel correspondence after the documented integer transforms, the complete head-pixel count, source palette membership, binary alpha, ground contacts and both flight clearances.
- Independently parsed the exported GIF blocks: **256×256, eight images, eight 50 ms delays**, looping. Background `#14121c`, source-grey ground line, integer 4× pixels.
- `validation.json` records dimensions, palette/alpha checks, flight bottoms and SHA-256 hashes of the three source strips.
- No Unity import or Play-mode test: this is a requested concept-only export.

## Reproduce / provenance

Run `python docs/design/concept/run_v2/draw_run.py` from the repository root; Python 3 standard library only, no downloads or installed packages. The helper resolves the repository from its own location and writes only beside itself. Existing scripts are never edited.

Sources inspected: `Assets/Resources/DraftSprites/hero_run_6f.png`, `hero_idle_4f.png`, `hero_attack_5f.png`; v1 `notes.md`, `draw_run.py`, and comparison. The protected visible clusters were deliberately all selected from run 0 for consistent identity; passing arms come from run 2. Idle and attack supply the style/palette cross-check, not newly generated body parts. PNG/GIF codec helpers are copied from v1 into this standalone helper.

AI-assisted revision: **Codex, local deterministic pixel masking/compositing and limb drawing**. No image-generation API, external assets, new colours, paid services or new licensing dependencies. Original hero-art provenance remains in the existing root `CREDITS.md`. This concept's AI disclosure is recorded here because the requested write scope excludes root files.

All output is confined to `docs/design/concept/run_v2/`; no `Assets/`, existing scripts, v1 files, or root credits were changed. Git pull/commit could not be performed: `git` is unavailable in this execution environment. No repository metadata was written.
