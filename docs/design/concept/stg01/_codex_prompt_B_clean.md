You are the ASSET artist (see AGENTS.md; read docs/design/ANTI_SLOP.md and docs/design/LEVEL_STG01.md section 2 SEG-B + section 5 first).

GOAL: produce a CLEAN background plate for level segment B of the game "NUMBER" (Joseon-dynasty cyberpunk 2D pixel side-scroller). Reference composition: docs/design/concept/13_style_frame_v2.jpg (1600x533). Keep the same overall composition (left building with stalls and stone wall, long central tile roof, right roof, big gate with stairs at right, night city skyline behind, stone-paved street at bottom) but FIX the following:

REMOVE completely: the hero, all 3 enemy characters and their red number windows, the drone bugs, the top "3" broadcast, the face HUD at top-left, ALL readable text/letters (the "EMBECTOR" sign becomes an abstract glyph-free neon panel), the holographic face billboard (replace with abstract light panel). No characters, no UI, no text anywhere.

STYLE FIXES:
- Replace the Chinese-style multi-tier pagoda towers with Joseon hanok roofs: gentle curved eaves, grey giwa tiles, dancheong (green/red/blue bracket painting) under eaves, wooden lattice windows. Single low hanok roofs only, NO multi-tier pagodas, no upturned Chinese corners.
- Tone down neon: sky/background in deep indigo and teal-blue night; warm amber lantern light as the main accent; magenta/pink only as small accents (a few stall awnings, a few skyline panels), cyan as secondary. No pink/purple wash over everything. The skyline = futuristic towers with glyph-free neon panels, restrained.
- Keep it a believable pixel-art look (crisp pixel clusters, limited palette, no blur, no painterly gradients, no soft AI mush). Anti-slop checklist applies.

PLATFORM HEIGHTS (hard constraint — the game's colliders are placed from these). Final canvas is 1620 x 540 px; measure from the BOTTOM edge of the canvas; 32 px = 1 game unit. The walkable top edge of each element must be a clear, flat, horizontal, readable line at these heights (+-3 px):
  - Street ground top surface: y = 32 px (stone paving, flat, full width).
  - Left stone wall top (left building lower tier), x 0..416: y = 192 px.
  - Stall 1 awning top, x 0..234: y = 166 px.
  - Barrel at stall 2 front, x 320..349: top y = 78 px.
  - Stall 2 awning top, x 346..531: y = 154 px.
  - Stall 3 crate at x 550..608: top y = 74 px. Stall 3 awning top, x 618..851: y = 154 px.
  - CENTRAL+RIGHT LONG ROOF RIDGE: ONE continuous flat-looking ridge line at y = 240 px from x = 589 to x = 1306 (draw the ridge cap tiles as a bright thin highlighted line, the roof body slopes down behind/below it; the left end of the roof at x=589 must have a clear end (eave edge) the enemy can jump down from). No second ridge step on the right — keep both roofs at the same ridge height.
  - Right gate (daemun) + stairs occupy x ~1225..1620, stairs front-facing, gate roof is background-only.
  - Left building roof (upper) around y 340 px is background only.
  - Nothing else tall in the walkable zone between y=32 and y=240 except what's listed (decor like hanging lanterns, banners, steam, potted plants may overlap but must not look like walls the player can't pass; keep street level mostly clear and readable).

DELIVERABLES (save in docs/design/concept/stg01/, do not touch Assets/ or any C# scripts):
1. B_clean_hires.png — the high-res clean plate, 3:1 aspect, at least 3240x1080 (generate with your image generation ability at the largest native size, then crop/resize to exactly 3:1 without distorting; if the model gives smaller, upscale to 3240x1080 with Lanczos).
2. A reusable script tools/art/pixelize_bg.* (use whatever runs in this environment WITHOUT paid services; Node 18+ is installed at C:\Program Files\nodejs\node.exe; Python is NOT installed on the system, but if you have a Python with Pillow/numpy bundled you may use it — document how to run it at the top of the script). It must: take a hi-res image, resize to exactly 540 px height (width 1620 for 3:1) using an area/box average that is INTEGER-friendly (hi-res height should be an integer multiple of 540, e.g. 1080 -> factor 2, or 2160 -> 4; if not, first resize hi-res to 1080 or 2160 high), reduce to a limited palette (about 32-48 colors, median-cut or k-means in a perceptual space, no dithering or very light ordered dithering), output a native PNG at exactly 1620x540, hard pixel edges, no anti-aliased resample at the end. Also generate (--neon-mask) a second PNG of the same size containing ONLY the neon/emissive pixels (bright saturated cyan/magenta/amber light pixels: lanterns, signs, windows glow) on transparent background, all other pixels fully transparent, threshold chosen by brightness+saturation; and a 'neon preview' overlay.
3. B_clean_native.png — the pixelized result, exactly 1620x540.
4. B_clean_neon_mask.png — the neon-only mask, exactly 1620x540, transparent elsewhere.
5. B_clean_preview_guides.png — 1620x540 copy of native with thin colored horizontal guide lines drawn at the platform heights above and labels-free, so we can verify alignment (this is a debug image only).
6. A short notes file docs/design/concept/stg01/B_clean_notes.md: what you did, measured platform heights actually achieved in the native image (list y from bottom for each item) and where they deviate from the spec, what the model could not do.

If the painted heights are off by more than 3 px, redo or adjust (e.g. repaint or shift layers) until they match. Be honest in the notes about what is approximate.

Do NOT edit CREDITS.md (Claude will). Do NOT commit. Work only in docs/design/concept/stg01/ and tools/art/.
