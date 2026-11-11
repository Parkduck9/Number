Pass 2 for the SEG-B clean plate in docs/design/concept/stg01/ (read B_clean_notes.md, tools/art/*.cjs, docs/design/LIGHTING_DESIGN.md sections 1.3 and 2.3, docs/design/ANTI_SLOP.md). Platform geometry from pass 1 is GOOD and must not move (same 8 measured heights/spans).

The game lights this plate at runtime (Unity URP Light2D multiplies everything by ~0.5), so the plate must change:

1. BASE PLATE, neon OFF + "unlit dusk" values:
   - Every neon tube / glowing sign / billboard / lit window panel is painted in the OFF state: dark glass tubes, dark sign frames, blank dim panels (no readable text, no painted glow, no halo). Lanterns: keep paper lantern bodies but unlit/dim (no bright amber core).
   - Value rule: sky and far skyline may stay dark navy, but mid/near layers (walls, roof tiles, ground, stalls) in mid-dark values, roughly 40-60% brightness, so shapes still read when the image is darkened to 50%. Verify by exporting a 50%-brightness preview and checking forms read.
   - Reduce magenta further (stall awnings can become deep wine/indigo-red); keep palette cool-indigo + muted teal + warm brown wood. No cherry-blossom neon pink blob (make it muted dusty pink-grey or darker).
   - Produce B_base_off_hires.png (3240x1080) and B_base_off_native.png (1620x540) via the existing pipeline (pixelize_bg.cjs, 48 colors, integer 2x). Keep geometry identical to pass 1 (re-use align script; if you regenerate with ImageGen, re-align).
2. NEON SIGN PNGs (lit state), cropped to bounding box, in docs/design/concept/stg01/neon_B/ :
   - one PNG per sign: the lit tube pixels + a pixel halo; tube pixels have at least one channel at 255 (saturated), optional 1px bright core #FFF8E8; halo = darker steps of the same hue, 2-3 levels, ORDERED DITHER; alpha only 0 or 255; no blur.
   - Neon palette (use these defaults): vermilion #FF4A3C, teal #2EC4A8, desaturated orange-gold #FFB347-ish; white core. Max 3 colors + white accent. No pink+cyan mixing.
   - Cover: the 2-3 big skyline billboards/panels, the lantern-post glass tube(s) (x~90 and x~505), stall awning edge tubes (the 3 cyan strips -> use teal), a few skyline window strips, gate lanterns (optional, warm gold, small). Name files neon_B_01.png ... Place each exactly where its OFF counterpart is painted in B_base_off_native.png (pixel-aligned: pasting the PNG at x_px,y_px over the base must give the lit look).
3. neon_B/neon_list.csv with header: layer,file,x_px,y_px,light_hex,light_radius_px,flicker  (layer = "near" for everything since there is only one composite layer for now; x_px,y_px = top-left offset in the 1620x540 image; light_hex = light color slightly less saturated than tube; radius 128-224 px for big signs, 64-96 for small, 0 radius allowed for tiny window strips; flicker 0 except 1-2 signs at 0.3).
4. Preview images: B_lit_composite_preview.png (base + all neon PNGs pasted, 1620x540) and B_base_off_preview_50pct.png.
5. Update B_clean_notes.md (add a Pass 2 section; list the neon files and anything approximate). Remove outdated full-size neon mask outputs from the deliverable list (can keep files but mark obsolete).
Only touch docs/design/concept/stg01/ and tools/art/. Do not edit CREDITS.md, Assets/, C#. Do not commit. Honest notes about limits.
