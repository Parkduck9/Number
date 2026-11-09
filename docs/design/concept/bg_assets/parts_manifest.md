# STG-02..05 background parts manifest

50 new game PNGs: 34 static parts/plates and 16 animation strips. Source art: supplied Google Gemini drafts; grid recovery, palette cleanup, repairs and animation by Codex using local Python/NumPy/Pillow. No downloads or paid generation calls. CREDITS.md was not edited, as requested.

## Source grid and finishing

| Source | Source pixels | Measured cell pitch | Reconstruction |
|---|---:|---|---|
| b1_far_gemini.png | 1024 x 341 | X 2.553px; Y 2.556px | Each 500px panel: 196 cells wide; upper panels 63 cells high, lower panels 64 |
| b2_buildings_gemini.png | 1024 x 765 | X 2.560px; Y 2.563px | 400 x 299; vertical cell boundary offset approximately 1.08px |
| b3_props_gemini.png | 1024 x 765 | X 2.560px; Y 2.561px | 400 x 299; vertical cell boundary offset approximately 1.08px |
| b4_interiors_gemini.png | 1024 x 765 | No reliable common pixel-cell period | Each 1008 x 245/246 crop reconstructed on a 405 x 98 working grid (X 2.489px, Y 2.500/2.510px) |

**Measurement limitation:** b4 is a softened, irregular-grid illustration. Its 405 x 98 grid is an explicit reconstruction choice, not a measured or recovered original authoring grid. Exact native-grid recovery cannot be claimed for that source. X/Y edge-energy spectra were scanned and the strongest b1/b2/b3 periods identified; local inspection and Fourier phase located the b2/b3 vertical cell boundaries.

Every cell votes for its dominant 4-bit RGB cluster, then chooses the modal actual source RGB within that cluster. Nearby colours merge to frequent representative source entries (no averaging, smoothing or dithering). Explicit repaired regions and animation pixels use discrete palette colours. All game PNGs are RGBA with alpha 0/255 only; transparent RGB is zero.

b1 crops, in source top-left coordinates: rooftops (8,5)-(508,166), factory (516,5)-(1016,166), hideout (8,174)-(508,338), palace (516,174)-(1016,338). Each 196-cell panel is centred at native x=104 within 405 cells, then enlarged exactly 2x. Side roof/wall segments continue by reflection; factory and palace extensions use only peripheral terrain, excluding repeated stack clusters or palace gates. Sky is rebuilt in hard bands and the measured rooftop moon restored as a hard pixel disc. No full plate is stretched. Far output heights are 126px (rooftops/factory) and 128px (hideout/palace), preserving the measured cell pitch rather than forcing 268px.

b4 crops: hideout (8,5)-(1016,251), factory (8,260)-(1016,505), throne (8,514)-(1016,759). Working grids enlarge exactly 2x to 810 x 196. Large buildings use exact 2x; all props retain recovered 1x cells. Keep transform scale (1,1,1), PPU32. Preview includes a 44px ruler for hero-body scale.

Green keys and spill are removed. Green-tinted byeongpung interiors are shifted into cyan before keying while preserving the mountain linework. Talisman pseudo-lettering is replaced by an isolated diamond, dash, square and short geometric strokes. Source sparkle regions are replaced with neighbouring rubble/board/circuitry cells; the far-palace sparkle disappears with reconstructed sky. Tiny disconnected key-fringe fragments are removed.

## Files and slicing

Sizes are pixels. Strip frames run left to right, without gutters. Pivots are normalized Unity coordinates from bottom-left. FPS is a suggested playback rate, not PNG metadata. `shop_2f` means two storeys: it is a static sprite, not a two-frame strip.

| File | PNG size | Frame size | Frames | Suggested fps | Pivot | Opaque RGB colours, entire strip | Stage |
|---|---:|---:|---:|---:|---|---:|---|
| [buildings/factory_hall.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/factory_hall.png) | 212 x 196 | 212 x 196 | 1 | static | (0.5, 0) | 12 | STG-04 |
| [buildings/gate_scanner.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/gate_scanner.png) | 220 x 140 | 220 x 140 | 1 | static | (0.5, 0) | 22 | STG-02, STG-05 |
| [buildings/house_ac.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/house_ac.png) | 222 x 202 | 222 x 202 | 1 | static | (0.5, 0) | 12 | STG-02, STG-03 |
| [buildings/rail_pillar.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/rail_pillar.png) | 174 x 178 | 174 x 178 | 1 | static | (0.5, 0) | 36 | STG-02, STG-04 |
| [buildings/rail_track.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/rail_track.png) | 174 x 38 | 174 x 38 | 1 | static | (0.5, 0) | 14 | STG-02, STG-04 |
| [buildings/ruin_hanok.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/ruin_hanok.png) | 212 x 138 | 212 x 138 | 1 | static | (0.5, 0) | 12 | STG-02, STG-03 |
| [buildings/shop_2f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/shop_2f.png) | 268 x 236 | 268 x 236 | 1 | static | (0.5, 0) | 43 | STG-02 |
| [buildings/wall_tile.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/wall_tile.png) | 140 x 86 | 140 x 86 | 1 | static | (0.5, 0) | 7 | STG-02, STG-03, STG-05 |
| [buildings/watchtower.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/watchtower.png) | 108 x 190 | 108 x 190 | 1 | static | (0.5, 0) | 28 | STG-04, STG-05 |
| [buildings/watchtower_lamp_blink_2f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/buildings/watchtower_lamp_blink_2f.png) | 216 x 190 | 108 x 190 | 2 | 2 | (0.5, 0) | 2 | STG-04, STG-05 |
| [far/factory_plate.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/factory_plate.png) | 810 x 126 | 810 x 126 | 1 | static | (0.5, 0) | 25 | STG-04 |
| [far/factory_sky.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/factory_sky.png) | 810 x 126 | 810 x 126 | 1 | static | (0.5, 0) | 4 | STG-04 |
| [far/factory_skyline.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/factory_skyline.png) | 810 x 126 | 810 x 126 | 1 | static | (0.5, 0) | 22 | STG-04 |
| [far/hideout_wall.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/hideout_wall.png) | 810 x 128 | 810 x 128 | 1 | static | (0.5, 0) | 21 | STG-03 |
| [far/palace_plate.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/palace_plate.png) | 810 x 128 | 810 x 128 | 1 | static | (0.5, 0) | 21 | STG-05 |
| [far/palace_sky.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/palace_sky.png) | 810 x 128 | 810 x 128 | 1 | static | (0.5, 0) | 5 | STG-05 |
| [far/palace_skyline.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/palace_skyline.png) | 810 x 128 | 810 x 128 | 1 | static | (0.5, 0) | 16 | STG-05 |
| [far/rooftop_window_twinkle_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/rooftop_window_twinkle_4f.png) | 3240 x 126 | 810 x 126 | 4 | 1 | (0.5, 0) | 4 | STG-02 |
| [far/rooftops_plate.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/rooftops_plate.png) | 810 x 126 | 810 x 126 | 1 | static | (0.5, 0) | 25 | STG-02 |
| [far/rooftops_sky.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/rooftops_sky.png) | 810 x 126 | 810 x 126 | 1 | static | (0.5, 0) | 5 | STG-02 |
| [far/rooftops_skyline.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/rooftops_skyline.png) | 810 x 126 | 810 x 126 | 1 | static | (0.5, 0) | 21 | STG-02 |
| [far/searchlight_sweep_8f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/searchlight_sweep_8f.png) | 6480 x 128 | 810 x 128 | 8 | 2 | (0.5, 0) | 1 | STG-05 |
| [far/smoke_stack_6f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/far/smoke_stack_6f.png) | 4860 x 126 | 810 x 126 | 6 | 3 | (0.5, 0) | 3 | STG-04 |
| [interiors/bulb_flicker_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/interiors/bulb_flicker_4f.png) | 3240 x 196 | 810 x 196 | 4 | 3 | (0.5, 0) | 4 | STG-03 |
| [interiors/crt_static_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/interiors/crt_static_4f.png) | 3240 x 196 | 810 x 196 | 4 | 6 | (0.5, 0) | 4 | STG-03 |
| [interiors/factory_wall.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/interiors/factory_wall.png) | 810 x 196 | 810 x 196 | 1 | static | (0.5, 0) | 57 | STG-04 |
| [interiors/hideout_wall.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/interiors/hideout_wall.png) | 810 x 196 | 810 x 196 | 1 | static | (0.5, 0) | 38 | STG-03 |
| [interiors/ledwall_shimmer_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/interiors/ledwall_shimmer_4f.png) | 3240 x 196 | 810 x 196 | 4 | 2 | (0.5, 0) | 9 | STG-05 |
| [interiors/red_lamp_blink_2f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/interiors/red_lamp_blink_2f.png) | 1620 x 196 | 810 x 196 | 2 | 1 | (0.5, 0) | 2 | STG-04 |
| [interiors/throne_wall.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/interiors/throne_wall.png) | 810 x 196 | 810 x 196 | 1 | static | (0.5, 0) | 56 | STG-05 |
| [interiors/vat_bubbles_6f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/interiors/vat_bubbles_6f.png) | 4860 x 196 | 810 x 196 | 6 | 4 | (0.5, 0) | 1 | STG-04 |
| [props/byeongpung_holo.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/byeongpung_holo.png) | 92 x 74 | 92 x 74 | 1 | static | (0.5, 0) | 35 | STG-03, STG-05 |
| [props/cable_bundle.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/cable_bundle.png) | 49 x 72 | 49 x 72 | 1 | static | (0.5, 1) | 16 | STG-02, STG-03, STG-04 |
| [props/conveyor.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/conveyor.png) | 57 x 19 | 57 x 19 | 1 | static | (0.5, 0) | 10 | STG-04 |
| [props/conveyor_scroll_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/conveyor_scroll_4f.png) | 228 x 19 | 57 x 19 | 4 | 5 | (0.5, 0) | 2 | STG-04 |
| [props/crates_neon.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/crates_neon.png) | 65 x 61 | 65 x 61 | 1 | static | (0.5, 0) | 36 | STG-02, STG-03, STG-04 |
| [props/haetae.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/haetae.png) | 69 x 64 | 69 x 64 | 1 | static | (0.5, 0) | 16 | STG-05 |
| [props/haetae_eyes_blink_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/haetae_eyes_blink_4f.png) | 276 x 64 | 69 x 64 | 4 | 3 | (0.5, 0) | 3 | STG-05 |
| [props/jangseung_pair.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/jangseung_pair.png) | 97 x 91 | 97 x 91 | 1 | static | (0.5, 0) | 27 | STG-03, STG-05 |
| [props/jangseung_screen_faces_blink_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/jangseung_screen_faces_blink_4f.png) | 388 x 91 | 97 x 91 | 4 | 3 | (0.5, 0) | 4 | STG-03, STG-05 |
| [props/lantern_hanji.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/lantern_hanji.png) | 45 x 66 | 45 x 66 | 1 | static | (0.5, 1) | 20 | STG-02, STG-03, STG-05 |
| [props/lantern_hanji_sway_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/lantern_hanji_sway_4f.png) | 196 x 66 | 49 x 66 | 4 | 3 | (0.5, 1) | 21 | STG-02, STG-03, STG-05 |
| [props/robot_arm.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/robot_arm.png) | 88 x 88 | 88 x 88 | 1 | static | (0.5, 0) | 27 | STG-04 |
| [props/scanner_gate.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/scanner_gate.png) | 71 x 85 | 71 x 85 | 1 | static | (0.5, 0) | 26 | STG-04, STG-05 |
| [props/scanner_gate_beam_pulse_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/scanner_gate_beam_pulse_4f.png) | 284 x 85 | 71 x 85 | 4 | 3 | (0.5, 0) | 4 | STG-04, STG-05 |
| [props/server_cabinet.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/server_cabinet.png) | 85 x 76 | 85 x 76 | 1 | static | (0.5, 0) | 28 | STG-03, STG-04 |
| [props/server_cabinet_lights_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/server_cabinet_lights_4f.png) | 340 x 76 | 85 x 76 | 4 | 3 | (0.5, 0) | 4 | STG-03, STG-04 |
| [props/talisman_holo.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/talisman_holo.png) | 32 x 77 | 32 x 77 | 1 | static | (0.5, 0) | 20 | STG-03, STG-05 |
| [props/talisman_holo_flicker_4f.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/talisman_holo_flicker_4f.png) | 128 x 77 | 32 x 77 | 4 | 3 | (0.5, 0) | 4 | STG-03, STG-05 |
| [props/vending_dancheong.png](../../../../Assets/Art/Backgrounds/STG02_05_Parts/props/vending_dancheong.png) | 71 x 84 | 71 x 84 | 1 | static | (0.5, 0) | 37 | STG-02 |

## Registration and animation notes

Use either an opaque `*_plate` OR its separate sky/skyline pair. The opaque plates are static convenience composites; for moving searchlights use separated layers, so beams pass behind the gate. Hideout far wall has no meaningful sky split.

- **buildings/rail_track.png:** Repeat X; matching 3-cell seam.
- **buildings/wall_tile.png:** Repeat X; matching 3-cell seam.
- **buildings/watchtower_lamp_blink_2f.png:** Registered to watchtower; lamp only.
- **far/factory_plate.png:** Opaque static convenience plate; alternative to sky+skyline, not an additional layer.
- **far/factory_sky.png:** Opaque hard-band sky; full registration. Layer 0.
- **far/factory_skyline.png:** Alpha silhouette; original centred at native x=104. Side terrain reflected; no fractional stretch.
- **far/hideout_wall.png:** Opaque wall; reflection-continued side masonry.
- **far/palace_plate.png:** Opaque static convenience plate; alternative to sky+skyline, not an additional layer.
- **far/palace_sky.png:** Opaque hard-band sky; full registration. Layer 0.
- **far/palace_skyline.png:** Alpha silhouette; original centred at native x=104. Side terrain reflected; no fractional stretch.
- **far/rooftop_window_twinkle_4f.png:** Registered skyline replacement pixels; draw above skyline.
- **far/rooftops_plate.png:** Opaque static convenience plate; alternative to sky+skyline, not an additional layer.
- **far/rooftops_sky.png:** Opaque hard-band sky; full registration. Layer 0.
- **far/rooftops_skyline.png:** Alpha silhouette; original centred at native x=104. Side terrain reflected; no fractional stretch.
- **far/searchlight_sweep_8f.png:** Draw above sky, BELOW skyline; stepped opaque beams, no additive haze.
- **far/smoke_stack_6f.png:** Draw over sky, behind skyline; four stack origins; density/position wrap.
- **interiors/bulb_flicker_4f.png:** Five hanging bulb cores only; full plate registration.
- **interiors/crt_static_4f.png:** Six CRT interiors only; full plate registration.
- **interiors/factory_wall.png:** Opaque 810x196 plate. Source is irregular-grid illustration: modal reconstruction at 405x98, not a recovered authoring canvas.
- **interiors/hideout_wall.png:** Opaque 810x196 plate. Source is irregular-grid illustration: modal reconstruction at 405x98, not a recovered authoring canvas.
- **interiors/ledwall_shimmer_4f.png:** Sparse LED pixels and sun/moon cores only; preserves mountain composition.
- **interiors/red_lamp_blink_2f.png:** Seven red lamp cores only.
- **interiors/throne_wall.png:** Opaque 810x196 plate. Source is irregular-grid illustration: modal reconstruction at 405x98, not a recovered authoring canvas.
- **interiors/vat_bubbles_6f.png:** Cyan fluid cores only; source bubbles removed from core regions.
- **props/conveyor.png:** Repeat X; matching 2-cell seam.
- **props/conveyor_scroll_4f.png:** Belt rows only; repeat X with static conveyor.
- **props/haetae_eyes_blink_4f.png:** Colour replacement overlay; same origin/pivot as static prop.
- **props/jangseung_screen_faces_blink_4f.png:** Colour replacement overlay; same origin/pivot as static prop.
- **props/lantern_hanji_sway_4f.png:** Complete replacement sprite, not additive overlay; padded 2px each side; fixed top anchor.
- **props/scanner_gate_beam_pulse_4f.png:** Beam only; shared full-frame registration.
- **props/server_cabinet_lights_4f.png:** Colour replacement overlay; same origin/pivot as static prop.
- **props/talisman_holo_flicker_4f.png:** Colour replacement overlay; same origin/pivot as static prop.

All colour-changing overlays cover their active pixels with an opaque colour in every phase, so they can darken a bright static base without additive blending. Smoke and bubble overlays use transparent inactive cells. Sky reconstruction removes baked smoke/searchlights; factory fluid cores are repainted before adding moving bubbles. Lantern sway replaces the static lantern; do not draw both. It has a fixed upper stem and one-native-cell lateral body motion, with 2px horizontal padding. A repeated central pose inside the four-frame sway is intentional; the final frame does not duplicate the first.

Wall, rail-track and conveyor horizontal border columns match exactly. Place separate repeated sprites edge-to-edge. For conveyor strips, repeat sliced frames rather than wrapping the complete strip texture. The rail pillar begins beneath the separated 38px-high output rail track; attach its top under the track. Far plates are extended compositions, not guaranteed seamless repeating tiles.

## Suggested stage layers and parallax

Parallax values are camera-motion multipliers (0 fixed to camera, 1 world-locked). These are integration suggestions, not scene changes.

| Order | Parts | Parallax |
|---:|---|---:|
| 0 | Rooftop/factory/palace sky | 0.05 |
| 1 | Smoke or searchlight overlay, registered to its sky | 0.15 |
| 2 | Far skyline and rooftop twinkle | 0.15 |
| 2 | Hideout far masonry wall | 0.20 |
| 3 | Hideout/factory/throne interior wall and its registered CRT/bulb/vat/red-lamp/LED overlays | 0.45 |
| 4 | Buildings, wall tiles, rail track and pillars | 0.75 |
| 5 | Watchtower lamp overlay, same parent transform as tower | 0.75 |
| 6 | Ground props and their local overlays | 1.00 |

An overlay and its base must share the same transform, scale, pivot and parallax. Sky/skyline use one far-stage parent; do not apply different motion to registered smoke/beam origins. If the sky itself is independently scrolling, put the smoke/beam layer on the skyline parent instead. Hanging lantern and cable pivots are top-centre. Draw props behind the hero unless intentionally used as foreground occluders.

## Preview

[parts_preview.gif](parts_preview.gif): 3240 x 1080, 72 frames, 12 seconds, looping. Four simultaneous 810 x 270 stage compositions in a 2 x 2 grid, enlarged exactly 2x with nearest-neighbour. Frame delays repeat 160/170/170ms (6fps average); animation phases use the per-part FPS above. One shared 256-colour palette; no preview dithering. The preview is documentation only.

[parts_sheet_4x.png](parts_sheet_4x.png): 6480 x 10892, RGBA, exact 4x first-frame contact sheet of all 50 PNGs. Sparse overlays are cropped to first-frame alpha bounds for readability; each label records that top-left crop rectangle and the original frame dimensions. The game PNGs retain full registration.

Preview placement uses top-left pixel coordinates. All far parts start at (0,0); all interior plates and their overlays start at (0,74). Ground objects align at y=264, except walls at y=258 and the lantern bottom at y=150. STG-02: shop x20, house x410, wall tiles x300/382, vending x295, crates x710, lantern x361. STG-03: ruin x5, server x270, byeongpung x670, talisman x770, cable x220. STG-04: hall x10, robot x310, conveyors x415/472/529, scanner x630, tower x704. STG-05: gate x12, haetae x262, jangseung x690, byeongpung x350. A 44px reference ruler and stage labels appear only in documentation. The stacked far/interior presentation is a cutaway for inspection, not a prescribed final scene layout.

## Unity handoff

- Sprite (2D and UI); PPU **32**; Filter **Point**; Compression **None**; mipmaps off; NPOT scaling **None**; Mesh Type **Full Rect**; preserve alpha.
- Use Multiple and Grid by Cell Size for strips, using table frame sizes; zero padding and offset. Static PNGs use Single.
- Max Size **8192** is required for 6480px searchlights and 4860px smoke/vat strips; use at least 4096 for 3240px strips. Unity must not downscale these textures.
- Use Clamp for strips. Repeat sliced sprites at exact frame-width world intervals where tileability is indicated.
- Align positions to 1/32 world units. Do not stretch 126/128/196px-high plates to STG-01 height; extend scene coverage with placement or separate geometry.

## Verification and limits

Reopened all 50 serialized game PNGs and verified RGBA, exact recorded dimensions, alpha exclusively 0/255, transparent RGB zero, integer frame-width division, variation in all animation strips, and non-identical first/last frames. Checked all opaque plates are 810px wide and fully opaque; wall/rail/conveyor first and last RGBA columns match. Eye/face/server/talisman/tower/twinkle overlays remain within their opaque bases. Scanner beams intentionally occupy the open gate aperture.

Reviewed recovered object sheet, background sheet, native composite and a decoded interior GIF frame. GIF decode verified 3240 x 1080 / 72 frames, with different first and last frames. Contact sheet is RGBA with opaque alpha. No scripts, scenes, .meta files, existing assets, or CREDITS.md were edited; no git commands were used. All processing files stayed in the external OS temp directory.

**Unity Play mode / WebGL import and rendering were not run.** Unity generates .meta files and may initially use default texture settings; apply the above settings during integration. Check last-to-first loop transitions, registration, tile repetition, hanging anchors and hero scale in Play mode. The b4 grid-recovery limitation above remains material.
