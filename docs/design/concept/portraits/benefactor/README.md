# Gwiin / Benefactor — dialogue portrait drafts

Review only. No game assets, scenes, import settings or gameplay scripts were changed.

- **A:** High ponytail, right-facing three-quarter view, calm smile. Radio headset with a short boom; wrapped jeogori collar under a dark lapelled jacket, gold hair tie and small luck token.
- **B:** Low knotted bun, left-facing three-quarter view over a turned shoulder. Gold binyeo with a hanging coin, throat radio and gold-edged diagonal jacket closure.
- **C:** Long hair with one tucked side, a slightly lowered chin and a knowing smile. Compact headset, hair clip, norigae at the collar closure and a jacket zipper.

`preview_4x.png`: Yudo / A / B / C, left to right. Four adjoining 100x100 panels on `#0E1428`, enlarged to 1600x400 using exact nearest-neighbour copying. `preview_1x.png` is the same comparison at native size.

Each draft is 100x100 RGBA8 with **19 opaque colours + 1 transparent colour**, alpha values **0 and 255 only**, one-pixel ink contours and selective one/two-pixel top/right cyan rims. Broad flat colour planes use the existing portrait palette; gold is `#B8862E` with two supporting shades. Facial expressions and radio equipment were visually checked at 1x and 4x. These are original scripted pixel drawings, without an image-generation API or external traced art.

Regenerate with Node.js (standard library only):

```powershell
node docs/design/concept/portraits/benefactor/draw_drafts.cjs
```

The script writes only inside this folder. It reads the existing Yudo PNG for the comparison, checks decoded output pixels, dimensions, colour count and hard alpha, and saves `validation.json`. AI disclosure is recorded in the root `CREDITS.md`.
