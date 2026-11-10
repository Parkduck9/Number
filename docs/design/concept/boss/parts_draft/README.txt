FINAL BOSS PARTS - DRAFT, 2026-11-10

Created entirely by generate.cjs using Node.js built-ins. No paid service, image
generation API, image editor, antialiasing, gradients, or downloaded artwork.
All generated files are confined to this directory. No Git operations were used.

DELIVERABLES (opaque RGB count; transparency excluded)
boss_body.png            768 x 448    8 colors
boss_bug_arm.png         160 x 256    7 colors
boss_bug_arm_lit.png     160 x 256    9 colors
boss_blade_arm.png       160 x 320   12 colors
boss_blade_arm_lit.png   160 x 320   13 colors
boss_hatch_half.png       64 x  96    7 colors
boss_hatch_core.png       48 x  48    7 colors
preview_2x.png          1920 x 1080   2x nearest-neighbor of a 960 x 540 arena

The seven parts share exactly 16 opaque colors from docs/design/PALETTE.md.
Transparent pixels are RGBA 0,0,0,0. Every opaque alpha is 255.
Preview background #0B0A14 is the user-requested additional preview-only color.
Reserved bright red/yellow are limited to attack tells and the active gun core.
The dim background body uses 8 colors; average encoded luma is 0.0928 (0-1).
Upper-left material lighting is used; active parts add local emissive pixels.

PREVIEWS
preview_2x.png shows both lit arms and open hatch so the core is visible.
preview_1x.png shows idle arms and a closed hatch.
parts_1x.png places all seven-part variants except the large body on one board.
review/round_1_1x.png through round_5_1x.png preserve the review sequence.
review/*_4x.png are enlarged checks of the arms, hatch, and chest region.
The hero uses the first actual hero_idle_4f frame's alpha, thresholded at 128
and recolored to a silhouette. Its 64x64 canvas has a 59-pixel visible height.

VISUAL REVIEW AND REFINEMENTS
1. Generated and inspected assembled native-size image, parts board, and 4x
   chest. Found repeated polygonal bug shells, shield-like arm, and empty lower
   mass. Retained dark body and clearly separated interactive parts.
2. Added shell ridges/empty cavities, segmented insect undersides, cloth seams,
   oil/coolant, lower server salvage and draped hoses. Inspected 1x assembly and
   4x arms. Needed more variety and a less shield-shaped shoulder.
3. Added smaller overlapping husks, dark salvaged machine modules, broken collar
   tiles and thin cables. Redrew shoulder as a broad studded sleeve with cloth
   tabs. Inspected 1x, 2x assembly and 4x hatch. Core resembled a numeral zero.
4. Added small tile chips, cable clamps, claw knuckles, hatch fasteners and guard
   fittings. Reworked core into gun bores. Inspected 1x idle/active, 4x parts and
   core. The three bores accidentally suggested a face, which was rejected.
5. Replaced the core center with one horizontal four-barrel bank. Inspected
   final native assembly and core. No facial layout or baked hologram glyphs.

AUTOMATED CHECKS (all passed; see metrics.json)
- Exact part dimensions and RGBA PNG decode/encode pixel identity.
- Binary alpha; <=16 opaque colors per part; exactly 16 across the set.
- Every sprite color belongs to the selected documented project palette.
- Idle/lit variants have identical alpha masks and registration.
- Body hatch rectangle (320,230,128,96) is completely transparent.
- Hologram rectangle (312,72,144,88) is uniformly dark and empty.
- Every pixel of preview_2x.png matches the corresponding native pixel exactly.
- Existing hero visible silhouette height is 59 pixels.

REPRODUCE
From the project root:
  node docs/design/concept/boss/parts_draft/generate.cjs
The default is final round 5. Supplying 1 through 4 regenerates an earlier
revision and overwrites the current deliverables; rerun with 5 to restore final.
Node built-ins only; no package install required.

HANDOFF / LIMITATIONS
assembly.json records normalized Unity pivots, 32 PPU, body-local rectangles,
assembly positions, and optional import settings. PNG files cannot themselves
apply Unity pivots; .meta settings and game integration remain unperformed.
The bug anchor is the lower central edge of the palm, as requested. The sword
anchor is directly below its bottommost tip pixel. Hatch right half is a flip.
No Unity editor, Play mode or WebGL integration was tested. No .cs, scenes,
prefabs, Assets files or root CREDITS.md were changed. The task's output-only
constraint takes precedence; copy the local CREDITS.txt note into root credits
when these drafts are accepted. Final style approval remains with the art owner.
