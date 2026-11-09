# NUMBER FX moments — DRAFTS ONLY

Nine review drafts. None is approved for use in the game. No Assets/, game scripts, scenes, prefabs or CREDITS.md were written.

Open [overview.gif](overview.gif) for the numbered 3x grid. Numbering follows the request, left to right, top to bottom. Each individual `<name>.gif` has a matching `<name>_contact.png`.

| # | Moment | Library id / new file | Native size | Duration | FPS | Mode |
|---|---|---|---|---|---|---|
| 1 | [land-dust](land-dust.gif) | `impacts/number-land-dust` | 96x64 | 0.45 s | 40 | one-shot |
| 2 | [grapple-hit](grapple-hit.gif) | `impacts/number-grapple-hit` | 64x64 | 0.25 s | 40 | one-shot |
| 3 | [spawner-break](spawner-break.gif) | `explosions/number-spawner-break` | 128x96 | 0.8 s | 40 | one-shot |
| 4 | [enemy-warp-in](enemy-warp-in.gif) | `scifi/number-enemy-warp-in` | 80x80 | 0.75 s | 40 | one-shot |
| 5 | [sniper-muzzle](sniper-muzzle.gif) | `projectiles/number-sniper-muzzle` | 64x64 | 0.15 s | 40 | one-shot |
| 6 | [checkpoint](checkpoint.gif) | `scifi/number-checkpoint` | 96x80 | 0.75 s | 40 | one-shot |
| 7 | [player-glitch-death](player-glitch-death.gif) | `scifi/number-player-glitch-death` | 112x80 | 0.5 s | 40 | one-shot |
| 8 | [hack-pulse](hack-pulse.gif) | `scifi/number-hack-pulse` | 112x96 | 0.4 s | 40 | one-shot |
| 9 | [goal-open](goal-open.gif) | `scifi/teleporter-pad` | 96x104 | 2.4 s | 30 | seamless loop |

## Preview conventions

Native pixels use PPU 32 as the scale reference. All delivered previews use nearest-neighbour 3x scaling on #0B1026. Contact sheets sample up to 12 frames; GIFs contain every frame. Source color marks have hard pixel edges, with restrained additive light. The goal column deliberately uses the stock translucent volume. No paid services, downloads or image generation APIs were used.

Actor previews use the library neutral mannequin, resampled once to 28x44 pixels as a scale guide. It is not a NUMBER character or a proposed replacement. Enemy warp-in leaves the completed mannequin visible; player death hides it entirely. Neither actor image has been copied into the game.

The overview is 1296x1152 (432x384 at 3x), with labels 1–9 only. Its clock is 2.4 s at 40 fps: one-shots trigger at 0.1 s and 1.3 s; goal-open runs one continuous 2.4 s cycle. The 30 fps goal is held/repeated on the overview clock without changing its speed.

One-shot duration is the effect timeline in the table. Each GIF also holds its terminal sample for one frame; GIF centisecond rounding gives preview lengths of land-dust: 0.48 s, grapple-hit: 0.28 s, spawner-break: 0.83 s, enemy-warp-in: 0.78 s, sniper-muzzle: 0.18 s, checkpoint: 0.78 s, player-glitch-death: 0.53 s, hack-pulse: 0.43 s. All remain within the requested caps. GIFs repeat only to make review convenient; the eight source effects remain one-shots.

## Exact options and selection

Options below are the complete --opts objects, including defaults made explicit for reproduction. Light is 0–0.30 for new effects, and 0.18 for goal-open. New sources are in D:/Git/pixelfx/src/fx/<id>.js. No existing effect source was edited.

### 1. land-dust

New file: heavy-landing hardcodes 1.6 s, floor cracks and pebbles. This draft retains only the low bilateral dust gesture.

Source: `D:/Git/pixelfx/src/fx/impacts/number-land-dust.js`. Anchor (native pixels): `[48, 54]`. [Contact sheet](land-dust_contact.png).

```json
{
  "palette": [
    "#25283B",
    "#41465B",
    "#626B81",
    "#8793A4"
  ],
  "seed": 21,
  "spread": 30,
  "light": 0
}
```

### 2. grapple-hit

New file: bullet-impact hardcodes smoke, chips and a 1.4 s decay. The earlier parry draft has a fixed 22 px ring; this contact uses a 9 px ring.

Source: `D:/Git/pixelfx/src/fx/impacts/number-grapple-hit.js`. Anchor (native pixels): `[24, 32]`. [Contact sheet](grapple-hit_contact.png).

```json
{
  "palette": [
    "#16425B",
    "#20758B",
    "#4FF0FF",
    "#C7FCFF",
    "#FFFFFF"
  ],
  "seed": 23,
  "radius": 9,
  "sparks": 5,
  "light": 0.25
}
```

### 3. spawner-break

New file: plasma-explosion hardcodes a 224x200 canvas and 1.7 s timeline, with no metal bits. This draft adds six small metal fragments inside 128x96.

Source: `D:/Git/pixelfx/src/fx/explosions/number-spawner-break.js`. Anchor (native pixels): `[64, 56]`. [Contact sheet](spawner-break_contact.png).

```json
{
  "palette": [
    "#123449",
    "#20758B",
    "#25A7BF",
    "#4FF0FF",
    "#C7FCFF",
    "#FFFFFF"
  ],
  "accent": [
    "#521A54",
    "#AF298F",
    "#FF3FD0",
    "#FFA6EB"
  ],
  "metal": [
    "#25283B",
    "#41465B",
    "#8793A4",
    "#C0CDD8"
  ],
  "seed": 31,
  "radius": 30,
  "light": 0.3
}
```

### 4. enemy-warp-in

New file: warp-in is 3 s and respawn-beam is 2.5 s, with fixed tall columns. This paints a 44 px figure in 0.75 s.

Source: `D:/Git/pixelfx/src/fx/scifi/number-enemy-warp-in.js`. Anchor (native pixels): `[40, 64]`. [Contact sheet](enemy-warp-in_contact.png).

```json
{
  "palette": [
    "#351420",
    "#741E31",
    "#BE2635",
    "#FF2A2A",
    "#FF9999",
    "#FFF4F4"
  ],
  "seed": 37,
  "height": 44,
  "width": 15,
  "light": 0.2
}
```

### 5. sniper-muzzle

New file: muzzle-pistol has mandatory casing and smoke, without disable options. Reuses its existing flashShape/flash helpers for the flash alone.

Source: `D:/Git/pixelfx/src/fx/projectiles/number-sniper-muzzle.js`. Anchor (native pixels): `[16, 32]`. [Contact sheet](sniper-muzzle_contact.png).

```json
{
  "palette": [
    "#741E31",
    "#BE2635",
    "#FF2A2A",
    "#FFA6A6",
    "#FFFFFF"
  ],
  "seed": 41,
  "size": 0.8,
  "light": 0.25
}
```

### 6. checkpoint

New file: heal includes hearts and a 1.9 s timeline; teleporter-pad is a persistent loop. This is a single 0.75 s ring activation.

Source: `D:/Git/pixelfx/src/fx/scifi/number-checkpoint.js`. Anchor (native pixels): `[48, 67]`. [Contact sheet](checkpoint_contact.png).

```json
{
  "palette": [
    "#16425B",
    "#20758B",
    "#25A7BF",
    "#4FF0FF",
    "#97F8FF",
    "#DFFFFF"
  ],
  "seed": 43,
  "radius": 21,
  "height": 39,
  "light": 0.2
}
```

### 7. player-glitch-death

New file: glitch loops and restores the actor; warp-out uses a long extraction beam. This one-shot loses the actor completely in 0.5 s.

Source: `D:/Git/pixelfx/src/fx/scifi/number-player-glitch-death.js`. Anchor (native pixels): `[56, 64]`. [Contact sheet](player-glitch-death_contact.png).

```json
{
  "palette": [
    "#173447",
    "#20758B",
    "#4FF0FF",
    "#FF3FD0",
    "#C7FCFF",
    "#FFFFFF"
  ],
  "seed": 47,
  "height": 44,
  "spread": 26,
  "light": 0
}
```

### 8. hack-pulse

New file: emp-blast hardcodes smoke and a 3.2 s timeline. This retains the scanline/rim motif without a filled dome or smoke.

Source: `D:/Git/pixelfx/src/fx/scifi/number-hack-pulse.js`. Anchor (native pixels): `[56, 48]`. [Contact sheet](hack-pulse_contact.png).

```json
{
  "palette": [
    "#351833",
    "#70225F",
    "#AF298F",
    "#FF3FD0",
    "#FFA6EB",
    "#FFF0FC"
  ],
  "seed": 53,
  "radius": 36,
  "light": 0.2
}
```

### 9. goal-open

Existing library file, unchanged. Palette, metal colors, seed and light are supplied through --opts.

Source: `D:/Git/pixelfx/src/fx/scifi/teleporter-pad.js`. Anchor (native pixels): `[48, 92]`. [Contact sheet](goal-open_contact.png).

```json
{
  "palette": [
    "#123449",
    "#16425B",
    "#20758B",
    "#25A7BF",
    "#4FF0FF",
    "#78D6E0",
    "#A7E9EF",
    "#DFFFFF"
  ],
  "metal": [
    "#0B1026",
    "#141C30",
    "#252E43",
    "#37465C",
    "#506980",
    "#7895A4"
  ],
  "seed": 59,
  "light": 0.18
}
```

## Rendering and verification

- Three render/look rounds were completed: first pass, slice and grid adjustments, then final asymmetric plasma rim refinement. Inspected contacts and 1x/4x peak grids. Actor act sheets were also rendered and inspected.
- Ran `node tools/check.mjs <id>` for every selected id: 9 pass, 0 failures, 0 warnings. Re-ran during final export.
- Ran `node tools/render.mjs <id> --opts <JSON above> --gif --scale 3 --zoom 1 --max 12` for each id. ffmpeg is absent, so the CLI produced its contacts and the local dependency-free encoder generated the GIFs at their real frame timings. The final review renderer recomposites on the requested navy backdrop and substitutes the 44 px mannequin for actor previews.
- Checked the actual configured options at 120 Hz with seeds [chosen, 2, 3, 7], including the 44 px actor: no color pixels outside the canvas, and no color/light left at the end of any one-shot. See [validation.json](validation.json).
- Independently decoded every frame of all 10 delivered GIFs with Windows System.Drawing. Frame counts, dimensions and centisecond playback lengths match the manifest; the goal loop is exactly 2.40 s. Inspected a decoded overview image for corruption and clipping.
- Full generation settings, commands and GIF frame counts are in [manifest.json](manifest.json). The build and GIF encoder are kept in `D:/Git/pixelfx/records/number-moments/`; no game code was added.
- These are standalone draft previews; no Unity gameplay or WebGL integration was performed. Approval is required before any game import or connection.

Rebuild all review files from the pixelfx checkout:

```powershell
& "C:/Program Files/nodejs/node.exe" records/number-moments/build.mjs C:/Unity/Number/docs/design/concept/fx_moments
& "C:/Program Files/nodejs/node.exe" records/number-moments/verify.mjs
```

## Draft attribution

pixelfx by Patch, MIT; includes techniques/code adapted from Hugo Duprez / Sprite Fusion, MIT. Repository: https://github.com/patch-gg/pixelfx . Reference collection: https://www.spritefusion.com/pixel-art-vfx . Copyright (c) 2026 Patch and Copyright (c) 2026 Hugo Duprez. The complete license is preserved in [LICENSE.pixelfx.txt](LICENSE.pixelfx.txt). Specific recipe attributions are retained in the new source files.

New draft choreography and the preview assembly were authored with OpenAI Codex. This record is for draft provenance only; CREDITS.md remains untouched as requested. No Patch Pixel font asset from gallery/fonts was used; labels use the library core bitmap text renderer.

Both requested git pulls failed because this session cannot write .git/FETCH_HEAD; work used the current local checkouts. Existing unrelated changes were left alone. No commit was made because .git is read-only in this session.
