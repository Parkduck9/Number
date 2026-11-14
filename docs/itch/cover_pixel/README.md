# Itch cover pixel redraw - REVIEW DRAFT

Native 315x250 artwork; 630x500 is an exact 2x nearest-neighbour enlargement. Draft only, not installed or published.

Method: local PowerShell/C# with Windows System.Drawing for PNG I/O only. The Gemini source (`../cover_refs/gemini_cover_v7b_full.png`) was inspected as the composition/pose reference. All artwork was redrawn using explicit integer-coordinate polygons, Bresenham outlines, filled clusters and pixel discs on a blank native canvas. No source downscale, palette-quantization filter, antialiasing, blur, alpha glow, image-generation API, packages or paid service.

Palette: 30 colours without logo / 30 with logo (32-colour working palette). Colours come from the fixed project palette, existing key-art magenta/gold accents and Yudo portrait-related skin/hair shades; one pale yellow highlight was added. All pixels are opaque.

Hand-authored cleanup: hair silhouette and interlocking locks; connected 1px cyan rim; ear cartilage; face/profile, nose, mouth and chin; goggle strap, lens border, bridge and single readable eye; neck/collar seam; broad coat folds; finger and nail contours, curled fingers and wrist/cuff overlap; curved giwa eaves and tile courses; abstract red brush strokes replacing the talisman writing; flat-band talisman glow, sparse grouped sparks, pixel-disc bokeh and consistently sloped rain. Source texture/noisy pixels were omitted rather than inherited.

Logo: original 200x44 `Assets/Resources/DraftUi/logo_number.png`, unchanged opaque pixels, native 1x at (57,4). The odd canvas width leaves 57px left / 58px right, the closest possible integer-grid centring. Top sky is deliberately dark.

Rebuild from repository root:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File docs/itch/cover_pixel/generate_cover.ps1
```

Checks: exported dimensions and lossless PNG round trip; original logo pixels and binary source alpha; palette membership; opaque output; every pixel of all 630x500 2x2 blocks equals its native pixel. Visual review at native and integer enlargement.

Provenance: AI generation - Google Gemini source, Codex gpt-6.1-sol local pixel redraw. Original logo reused from the game.

Composite palette: #080B14, #0B1026, #1A1D33, #226076, #2D2D44, #2D3050, #2D345D, #3A0A2E, #3F4A6E, #42A9BD, #4D4C6D, #5D84A0, #6FFBF4, #7A3A3C, #7F1A22, #8D4094, #A37B70, #B77569, #B8862E, #C2AE78, #D69A83, #D6D7D0, #D77E54, #E5FDF5, #F21C2A, #F8D0B2, #FF3FB4, #FFC2E8, #FFD24A, #FFF1A6.
