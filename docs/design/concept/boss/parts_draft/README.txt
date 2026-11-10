FINAL BOSS PARTS - CONCEPT REVISION v2, 2026-11-10

Reference: ../boss_concept_4.png. This revision directly reuses the supplied
concept pixels for the roofed shoulders, fused shells, claw and armored arm.
The cute face and its halo are removed. The window is uniformly dark and empty.
The body is a continuous dark hunch with roofed server shoulders, curved ribs,
vein-like cables and dim red machine lights. The straight sword points down;
its lit edge is cyan/white. Liquid marks are black oil or cyan coolant.

FILES / UNCHANGED SPRITE DIMENSIONS AND UNITY PIVOTS
boss_body.png           768x448    pivot 0,0
boss_bug_arm.png        200x320    pivot 0.5,0 (lower central palm)
boss_bug_arm_lit.png    200x320    pivot 0.5,0
boss_blade_arm.png      288x576    pivot 0.5,0 (sword tip)
boss_blade_arm_lit.png  288x576    pivot 0.5,0
boss_hatch_half.png      64x96     pivot 0.5,0.5 (flip for right half)
boss_hatch_core.png      48x48     pivot 0.5,0.5 (retained yellow gun bank)

All seven parts share the EXACT original 16 opaque palette colors. Alpha is
0 or 255, and all transparent pixels are zero RGBA. Six redrawn parts use
an exact 2x grid; the retained 48x48 core keeps its previous native pixels.
Base/lit alpha masks match. Non-emissive material highlights face upper left.
Window rectangle: [312,72,144,88]. Transparent hatch: [320,230,128,96].

PREVIEWS
preview_1x.png: 960x640 idle assembly, hatch closed; every part at native size.
preview_2x.png: 1920x1280 exact 2x nearest-neighbour; both lit arms and open hatch
               shown together for art review, not proposed attack timing.
preview_full_1x.png: native active assembly (960x640).
parts_1x.png: both arm variants, hatch half and core, fully uncropped.
compare_concept.png: 1944x672, source concept on the left, new idle assembly on
                    the right. BOTH use one source pixel per output pixel.
                    Source crop [0,0,756,572] excludes the attack-card sidebar.
                    The requested larger down-pointing blade extends lower
                    than the concept's original upward-pointing sword.
review_v2/arena_960x540.png: native-resolution viewport check. The top 62 rows of
                    the huge blade arm continue above the viewport, while its
                    tip ends at y514, above the bridge at y516. No resizing.
review_v2/*_4x.png: chest, hatch, bug and blade detail checks.

The proof canvas is taller than v1's 960x540 to show the full 576px arm without
clipping its shoulder or shrinking its pixels. All sprite dimensions are
unchanged. assembly.json records both proof and viewport positions.
The preview hero is the actual 64x64 hero_idle_4f frame-0 alpha silhouette,
thresholded to hard alpha; 59 pixels tall. Preview scenery is review-only.

FOUR LOOK / FIX ROUNDS (actual intermediate images in review_v2/)
1. Extracted masked concept shoulders, shell arm and three armor segments;
   rebuilt the empty window, ribs, claw, grip and down-pointing sword. Inspected
   source comparison, native parts and 4x chest. Found residual source-arm
   pixels on the body, a generic claw, overly dark material planes and an
   unsuitable fractional-scale blade presentation.
2. Removed residual source colors from the lower body, added broad deltoid
   shadows and lower tendons, replaced the claw with an inverted concept-hand
   cutout around the bottom palm, thickened the armored elbow. Changed the
   proof to fully native arm pixels. Inspected comparison and parts board.
   Found dark burgundy planes quantizing into blue, and comparison crop cutting
   the outer armor. The source-hand silhouette was also too dark to read.
3. Preserved the source burgundy color ramp before palette matching; restored
   readable shell/cloth planes, cleaned the hand connection and lower ribs,
   and removed the comparison crop. Inspected native assembly, parts and 4x
   bug. Found a residual non-bug fragment at its left edge and unnecessary
   empty space above the proof; needed a separate actual-size viewport check.
4. Removed the stray bug fragment, refined roof/rack glints and lower cables,
   removed the last crimson source drip beside the sword collar, and tightened
   the proof to 960x640. Inspected comparison, active 2x assembly, 4x blade,
   hatch and 960x540 viewport. Final tests pass (metrics.json).

Review snapshots: round_1_1x.png through round_4_1x.png and each matching
round_N_compare.png. Early rounds preserve their experimental canvas sizes;
only round 4 is the delivered composition. The copied v1/review/ is separate.

REPRODUCE / VALIDATE
  node docs/design/concept/boss/parts_draft/generate.cjs
Default round: 4. Node.js built-ins only. raster.cjs contains the previous
integer rasterizer and RGBA PNG codec. All source images are local.
Numeric rounds 1-4 recreate the recorded revision layers, with outputs written
to this directory. Run the default again after inspecting an earlier round.

Checks run by the generator:
- RGBA PNG encode/decode pixel identity; alpha only 0/255.
- Original shared 16-color palette; every part <=16 colors.
- Exact sprite sizes and unchanged normalized pivots against v1/assembly.json.
- Same base/lit alpha masks; visible palm/tip at bottom-center registration.
- Blank 144x88 window; transparent 128x96 hatch hole.
- No red, burgundy or bright red pixels in the blade/collar region.
- preview_2x is pixel-identical to an exact integer enlargement.
- Full proof uses native sprite sizes without scaling any individual part.

PRESERVATION / HANDOFF
v1/ is the complete previous directory snapshot, including original PNGs,
generator, assembly, metrics, notes and review images. It is not overwritten.
No git operations, .cs, .unity, .prefab, Assets files or Unity imports were used.
Root CREDITS.md and local CREDITS.txt disclose source-pixel reuse and AI editing.
PNG does not embed Unity PPU/pivots: use assembly.json when integrating.
No Unity Play/WebGL or attack-motion test has been performed. Visual acceptance
remains with the user; these are reviewable draft assets, not game integration.
