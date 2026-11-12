# HUD dice redraw v2 — review drafts only

AI 생성 (Codex), deterministic local integer-pixel edits from redraw_v1.
No image-generation API, external assets, or paid services. Same AI provenance
as the existing HUD dice redraw entry in CREDITS.md. Nothing installed in Assets/.

All eleven sprites retain v1's 24×24 canvas, silhouette, outline pixels and binary
alpha. The six numbered front faces and inactive front face are unchanged,
including their original 2×2 pips. Top and side interiors are flat and unmarked.
Active colors: top #193044, side #101830, front #080B1E (existing palette).
Inactive colors: top #6B787C, side #424951; the front remains v1's #424951
to preserve its exact pixels and dark pip. No extra grey was introduced.

The four existing tumble poses have no pips on any face. Their geometry and
outlines are unchanged; only face fills and pip pixels are edited.

preview_8x.png shows all eleven in one horizontal row at exact 8× nearest-neighbour
scale, with filenames only. Run `node build_drafts.cjs` to reproduce and check
dimensions, alpha, palette membership, exact unchanged pixels outside edited face
interiors, flat unmarked faces, and unchanged v1 / installed sprite hashes.

Draft review only; not tested in the running Unity HUD.
