# Hero cut-out rig experiment

**Experiment only; not used in the game.** Concept: `rig_hero_parts_gemini.png` (Google Gemini). Nine individual drawings made with Codex / built-in OpenAI image generation; local flat-palette cleanup, sizing, joint overlap and compositing. Exact prompts: `generation_prompts.json`; generated inputs: `source/`.

## Deliverables

- `parts/`: 12 transparent, antialiased RGBA PNGs and `rig.json`. Smooth cel art, not pixel art. Native assembled silhouette is 508 px tall (requested approximately 512 px).
- `parts_assembled.png`: native-size assembled character beside the exploded parts, red crosses at pivots. The assembled character is composited from the delivered PNGs using `parts/rig.json`.
- `assembled_transparent.png`: the same assembled character on a transparent 360 x 540 canvas, without labels or pivot markers.

## Coordinate contract

Every `size`, `pivot` and `offset` uses pixels including the 2 px transparent edge padding. The origin is top-left, x increases rightward and y downward. The pelvis is the root (`parent: null`); its offset is its world pivot. Every other offset is the child's pivot location **in the parent's image pixel space**, not a vector from the parent pivot.

For the neutral pose: `child_top_left = parent_top_left + child.offset - child.pivot`. Draw ascending `draw_order`. All delivered parts have `rotation_deg: 0`: limbs straight down and blade pointing right.

With rotations: `child_world_pivot = parent_world_pivot + rotate(parent_world_angle, child.offset - parent.pivot)`; `child_world_angle = parent_world_angle + child.rotation_deg`. Angles are clockwise in this y-down system. Render each image about its own pivot.

Far upper arm, thigh and shin are exact geometry/alpha duplicates of their near counterparts with RGB multiplied by 0.88. Both forearms reuse the same sleeve, with a small cuff-to-hand seam adjustment; the near hand grips and the far hand is open as requested. Far forearm receives the same tint.

### Knife grip occlusion

The blade draws last. Its `occlusion` entry clips it behind skin pixels in the specified rectangle of `forearm_near`, leaving the grip slot visible. This prevents the handle from painting over the fingers while retaining a complete reusable `blade.png`. `render_rig.ps1` implements this rule directly from the JSON. An eventual rig importer must reproduce this clip or supply equivalent finger-over-weapon layering. With rotation, transform the mask with the forearm. No Unity integration is included.

## Reproduce

Windows PowerShell with the built-in .NET System.Drawing library; no packages, network calls or paid API:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File docs/design/concept/rig_test/build_parts.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File docs/design/concept/rig_test/render_rig.ps1
```

The build step reads the saved generation inputs, removes fine tonal noise using shared material colors, retains antialiased contours, guarantees opaque rounded joint disks and creates the far-side copies. The rendering step reads only the final PNGs and JSON; it does not regenerate or independently redraw the assembled hero.

`verification.json` records size/alpha checks for all 12 PNGs, exact far-limb duplication/tint checks, the 508 px visible height, and coincident fully opaque disks of radius 7 px at all eight limb joints (both parent and child). These disks remain overlapping when the child rotates about its specified pivot. This verifies joint-center coverage, not every possible extreme pose silhouette.

Visual approval and animation comparison are still needed before any decision to use this experiment in the game.

## Blender cut-out animation test (`blender/`, `anim/`)

Headless Blender 5.2 build of the rig from `parts/rig.json` plus three keyframed animations, for a side-by-side
look against the 64x64 draft pixel strips. Not game art.

- `blender/rig_and_render.py` (bpy only): one emission/alpha-blended image plane per part, bone per part at the
  rig.json pivots (pelvis root -> torso -> head/arms -> forearms -> blade; pelvis -> thighs -> shins), rigid bone
  parenting, orthographic camera, draw order via depth offsets (blade drawn behind the near fist instead of the
  JSON clip rule). Animations: idle 24 f loop, run 16 f loop, attack 12 f, all 24 fps.
  `blender -b -P blender/rig_and_render.py -- [idle run attack] [--engine EEVEE|CYCLES] [--save-blend]`
- `blender/postprocess.py` (system Python + Pillow): strips, GIFs, contact sheets.
- `anim/<name>/####.png` 704x704 native frames (character 509 px standing, sole on row 660, pelvis on column 352).
- `anim/<name>_512.gif` 24 fps viewing GIF of the native frames.
- `anim/<name>_64_smooth.png` / `_64_hard.png` game-scale strips: 12 fps sampling, character 59 px tall, feet on
  row 61, area downscale; `hard` = alpha thresholded at 50 %.
- `anim/<name>_64x4.gif` 12 fps, 64 px frames upscaled 4x nearest, smooth above hard.
- `anim/compare_<name>.png` draft pixel strip (top) vs rig 64 hard strip (bottom), 4x.
- `anim/hero_rig.blend` the built scene (last rendered animation keyed) for opening in the Blender UI.
