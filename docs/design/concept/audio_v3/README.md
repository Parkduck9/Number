# NUMBER — SFX v3 hybrid DRAFTS

Twelve mono 32,000 Hz / signed 16-bit PCM drafts, sample peaks approximately -1 dBFS. No BGM generated. Assets and audio_v2 are read-only inputs; nothing is imported into Unity.

**Listening limitation:** these choices were made from length, 5 ms RMS envelopes and Hann-windowed FFT spectra. No subjective listening was performed or claimed. Open [review.html](review.html) for human A/B/C audition. CC0 files are the supplied converted WAVs; their filenames and hashes identify the exact local inputs. Physical knife/cloth/coin/latch recordings add tangible texture; Kenney sci-fi/interface files are designed sounds, and are not claimed to be acoustic recordings of real robots.

## Source packs and license

All external layers are CC0 1.0. Supplied Kenney `License.txt` files were read, and the official pack pages and StarNinjas listing were checked. No additional assets were downloaded. Raw `_cc0_src/` is git-ignored and must not be committed. Its source analysis is retained as numbers/hashes, not audio.

- [20 Sword Sound Effects (Attacks and Clashes)](https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes) — StarNinjas, [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- [Impact Sounds](https://kenney.nl/assets/impact-sounds) — Kenney, [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- [Interface Sounds](https://kenney.nl/assets/interface-sounds) — Kenney, [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- [RPG Audio](https://kenney.nl/assets/rpg-audio) — Kenney Vleugels (Kenney), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) — Kenney, [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

## Per-file recipe and exact attribution

Every output uses `../audio_v2/<same filename>.wav` (AI-generated procedural synthesis: Codex / OpenAI GPT). The complete v2 is retained unless the filtering below says otherwise; no isolated traditional-instrument recording is implied. EQ uses causal two-pole Butterworth high/low-pass filters. Source intervals below are before varispeed; all shifts are coupled playback speed/pitch. Gains are peak-relative after per-layer EQ/fades and before final normalization. Layer and master fades use smooth ramps; final PCM endpoints are exactly zero.

### [sfx_swing.wav](sfx_swing.wav) — 0.220 s

Sword air and a dark plucked scrape.

- v2 layer (gain 0.34): Matching v2, low-passed to retain the dark pluck and supporting air. EQ 350–2900 Hz.
- CC0: [20 Sword Sound Effects (Attacks and Clashes)](https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes) / `sword/sword.4.wav` — **StarNinjas**. Source 0.055000–0.285000 s; +1 semitones; EQ 550–10200 Hz; offset 0 ms; gain 1; fades 3/18 ms.
- Selection basis: Sword 4 has a single rise to a 0.200 s crest; trim its leading silence and retain the sweep, rather than a clash transient.
- Measured: -1.00 dBFS peak; -15.03 dBFS RMS; +13.49 dB CC0/v2 energy before mastering.

### [sfx_hit.wav](sfx_hit.wav) — 0.280 s

Body/robot hit with a janggu stick crack.

- v2 layer (gain 0.78): Full matching v2 janggu deok/stick attack, robot clank and low buk accent. EQ 70–7400 Hz.
- CC0: [Impact Sounds](https://kenney.nl/assets/impact-sounds) / `impact-sounds/impactPunch_heavy_002.wav` — **Kenney**. Source 0.000000–0.260000 s; -0.5 semitones; EQ 45–1600 Hz; offset 0 ms; gain 0.95; fades 1/20 ms.
- CC0: [RPG Audio](https://kenney.nl/assets/rpg-audio) / `rpg-audio/metalLatch.wav` — **Kenney Vleugels (Kenney)**. Source 0.018000–0.105000 s; -1 semitones; EQ 1100–7800 Hz; offset 3 ms; gain 0.24; fades 1/18 ms.
- Selection basis: Heavy punch 2 concentrates 90% energy within 80 ms and supplies low body weight; latch supplies a brief physical metal edge.
- Measured: -1.00 dBFS peak; -17.24 dBFS RMS; +6.42 dB CC0/v2 energy before mastering.

### [sfx_parry.wav](sfx_parry.wav) — 0.760 s

Steel block followed by a bright kkwaenggwari ring.

- v2 layer (gain 1): Full v2 kkwaenggwari/bronze ringing and low punch; one quiet 60 ms dark reflection extends the tail. EQ 90–9700 Hz.
- CC0: [20 Sword Sound Effects (Attacks and Clashes)](https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes) / `clash/sword_clash.4.wav` — **StarNinjas**. Source 0.036000–0.270000 s; -0.5 semitones; EQ 650–8900 Hz; offset 0 ms; gain 0.86; fades 0.7/25 ms.
- Selection basis: Clash 4 is less shrill than alternatives (6.21 kHz centroid versus 7-10 kHz) with a short 40-249 ms active region.
- Measured: -1.00 dBFS peak; -22.05 dBFS RMS; -5.40 dB CC0/v2 energy before mastering.
- Tail reflection: v2 only, delay 60 ms, gain 0.1, low-pass 4100 Hz.

### [sfx_dash.wav](sfx_dash.wav) — 0.180 s

Fast cloth/air movement with the v2 digital zip.

- v2 layer (gain 0.53): Matching v2 rising crushed zip and air rush. EQ 350–6500 Hz.
- CC0: [RPG Audio](https://kenney.nl/assets/rpg-audio) / `rpg-audio/cloth1.wav` — **Kenney Vleugels (Kenney)**. Source 0.145000–0.320000 s; +1 semitones; EQ 240–6700 Hz; offset 0 ms; gain 0.85; fades 4/18 ms.
- CC0: [20 Sword Sound Effects (Attacks and Clashes)](https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes) / `sword/sword.6.wav` — **StarNinjas**. Source 0.012000–0.160000 s; +1 semitones; EQ 900–9200 Hz; offset 0 ms; gain 0.28; fades 2/18 ms.
- Selection basis: Cloth 1 has broadband movement around its 205 ms crest; a short crop gives physical movement without adding a weapon strike.
- Measured: -1.00 dBFS peak; -18.77 dBFS RMS; +1.32 dB CC0/v2 energy before mastering.

### [sfx_hurt.wav](sfx_hurt.wav) — 0.300 s

Buk-backed bodily impact and falling robot distress.

- v2 layer (gain 1): Full matching v2 heavy buk and descending crushed robot voice. EQ 40–5800 Hz.
- CC0: [Impact Sounds](https://kenney.nl/assets/impact-sounds) / `impact-sounds/impactPunch_heavy_003.wav` — **Kenney**. Source 0.005000–0.260000 s; -1 semitones; EQ 45–1850 Hz; offset 0 ms; gain 0.85; fades 1/25 ms.
- CC0: [RPG Audio](https://kenney.nl/assets/rpg-audio) / `rpg-audio/cloth1.wav` — **Kenney Vleugels (Kenney)**. Source 0.190000–0.310000 s; -0.5 semitones; EQ 400–5000 Hz; offset 8 ms; gain 0.13; fades 2/12 ms.
- Selection basis: Heavy punch 3 is a compact low impact with a 25 ms crest and little high-frequency energy, leaving the robot voice clear.
- Measured: -1.00 dBFS peak; -14.82 dBFS RMS; -4.56 dB CC0/v2 energy before mastering.

### [sfx_enemy_tell.wav](sfx_enemy_tell.wav) — 0.200 s

Readable rising enemy wind-up blip and terminal ping.

- v2 layer (gain 1): Full matching v2 rising robot blip and 2350 Hz terminal ping, dominant over a soft mechanical lock. EQ 100–10000 Hz.
- CC0: [RPG Audio](https://kenney.nl/assets/rpg-audio) / `rpg-audio/metalLatch.wav` — **Kenney Vleugels (Kenney)**. Source 0.015000–0.105000 s; -1 semitones; EQ 250–1100 Hz; offset 0 ms; gain 0.075; fades 1.5/18 ms.
- Selection basis: Only the latch onset is used; low-pass and a low gain keep recorded steel out of the main warning band.
- Measured: -1.00 dBFS peak; -11.89 dBFS RMS; -31.62 dB CC0/v2 energy before mastering.
- Warning band check (700–4000 Hz): v2 exceeds CC0 by 33.60 dB. Original v2 note times and pitch are retained; gameplay masking still needs human listening.

### [sfx_last_tell.wav](sfx_last_tell.wav) — 0.500 s

Two distinct urgent A5/E5 reed warnings.

- v2 layer (gain 1): Full matching v2 two reed stabs and buk; background machine chatter ducks with those stabs. EQ 65–9500 Hz.
- CC0: [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) / `sci-fi-sounds/computerNoise_002.wav` — **Kenney**. Source 0.900000–1.360000 s; -0.5 semitones; EQ 180–520 Hz; offset 6 ms; gain 0.075; fades 6/30 ms.
- Selection basis: Computer noise 2 sits near 900 Hz; low-pass at 520 Hz and low level place its texture below the A5/E5 warning harmonics.
- Measured: -1.00 dBFS peak; -9.72 dBFS RMS; -27.53 dB CC0/v2 energy before mastering.
- Warning band check (700–4000 Hz): v2 exceeds CC0 by 28.13 dB. Original v2 note times and pitch are retained; gameplay masking still needs human listening.
- Machine texture is attenuated by up to 65% following the 5 ms v2 RMS envelope, with a 6 ms smoother.

### [sfx_stunned.wav](sfx_stunned.wav) — 0.750 s

Robot overload under a falling gayageum-like bend.

- v2 layer (gain 0.95): Full matching v2 pulsed fizz and falling bent string; physical latch at the onset. EQ 80–7500 Hz.
- CC0: [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) / `sci-fi-sounds/computerNoise_003.wav` — **Kenney**. Source 0.900000–1.580000 s; -1 semitones; EQ 320–2900 Hz; offset 8 ms; gain 0.42; fades 4/55 ms; exponential decay 0.34 s.
- CC0: [RPG Audio](https://kenney.nl/assets/rpg-audio) / `rpg-audio/metalLatch.wav` — **Kenney Vleugels (Kenney)**. Source 0.015000–0.130000 s; -1.5 semitones; EQ 600–6600 Hz; offset 0 ms; gain 0.26; fades 1/22 ms.
- Selection basis: Computer noise 3 offers a midrange machine texture instead of the near-ultrasonic sci-fi door samples; latch adds a real mechanism.
- Measured: -1.00 dBFS peak; -18.56 dBFS RMS; +2.12 dB CC0/v2 energy before mastering.

### [sfx_broadcast.wav](sfx_broadcast.wav) — 1.900 s

Readable four-note public-address announcement.

- v2 layer (gain 1): Full matching v2 A5/E5/G5/A4 chime, unchanged timing/pitch; tiny relay click underneath the first note only. EQ 75–10000 Hz.
- CC0: [RPG Audio](https://kenney.nl/assets/rpg-audio) / `rpg-audio/metalClick.wav` — **Kenney Vleugels (Kenney)**. Source 0.245000–0.310000 s; +0 semitones; EQ 1400–6500 Hz; offset 0 ms; gain 0.08; fades 1/14 ms.
- Selection basis: Metal click has its main transient at 270 ms; isolate that 65 ms region and keep it quiet so no new warning rhythm is introduced.
- Measured: -1.00 dBFS peak; -13.84 dBFS RMS; -42.74 dB CC0/v2 energy before mastering.
- Warning band check (700–4000 Hz): v2 exceeds CC0 by 41.67 dB. Original v2 note times and pitch are retained; gameplay masking still needs human listening.

### [sfx_hack_ok.wav](sfx_hack_ok.wav) — 1.080 s

Dice result: jing bloom, ascending gayageum-like plucks and small metal rattle.

- v2 layer (gain 1): Full matching v2 jing and six ascending plucks, plus one quiet 65 ms dark reflection. EQ 80–10000 Hz.
- CC0: [RPG Audio](https://kenney.nl/assets/rpg-audio) / `rpg-audio/handleCoins2.wav` — **Kenney Vleugels (Kenney)**. Source 0.020000–0.305000 s; -0.5 semitones; EQ 1700–7500 Hz; offset 4 ms; gain 0.3; fades 2/25 ms.
- Selection basis: Coins 2 provides a short tangible rattle at 180 ms, with most energy finished by 232 ms; it supports the roll/result without adding an unrelated pitched UI melody.
- Measured: -1.00 dBFS peak; -18.35 dBFS RMS; -20.40 dB CC0/v2 energy before mastering.
- Tail reflection: v2 only, delay 65 ms, gain 0.07, low-pass 2800 Hz.

### [sfx_ui_select.wav](sfx_ui_select.wav) — 0.120 s

Single compact physical UI click with a wooden clapper color.

- v2 layer (gain 0.66): Matching v2 wooden bak/clapper tick and soft high pluck. EQ 180–7200 Hz.
- CC0: [Interface Sounds](https://kenney.nl/assets/interface-sounds) / `interface-sounds/click_002.wav` — **Kenney**. Source 0.000000–0.010023 s; +0 semitones; EQ 220–5000 Hz; offset 0 ms; gain 0.9; fades 0.25/2 ms.
- Selection basis: Click 2 is a single 10 ms event with an 866 Hz centroid; it avoids the 10.5 kHz brightness of click 4 and double-action switch files.
- Measured: -1.00 dBFS peak; -21.14 dBFS RMS; -2.52 dB CC0/v2 energy before mastering.

### [sfx_shockwave.wav](sfx_shockwave.wav) — 1.900 s

Heavy buk impulse, jing expansion and a grounded blast tail.

- v2 layer (gain 1): Full matching v2 heavy buk, jing and falling low sweep; one quiet 85 ms dark reflection. EQ 35–8200 Hz.
- CC0: [Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) / `sci-fi-sounds/explosionCrunch_000.wav` — **Kenney**. Source 0.000000–0.750000 s; -1.5 semitones; EQ 55–4200 Hz; offset 0 ms; gain 0.72; fades 1/45 ms.
- CC0: [Impact Sounds](https://kenney.nl/assets/impact-sounds) / `impact-sounds/impactSoft_heavy_003.wav` — **Kenney**. Source 0.000000–0.300000 s; -0.5 semitones; EQ 45–1000 Hz; offset 0 ms; gain 0.32; fades 1/30 ms.
- Selection basis: Crunch 0 has an immediate crest and a 430 ms 90%-energy time, adding impact texture without the delayed attack or near-subsonic bias of other explosions.
- Measured: -1.00 dBFS peak; -17.25 dBFS RMS; -2.64 dB CC0/v2 energy before mastering.
- Tail reflection: v2 only, delay 85 ms, gain 0.07, low-pass 1900 Hz.

## Build and verification

Requires Python 3.10+ and NumPy. Audio I/O uses `wave` from the standard library; no SciPy or soundfile is required. The script appends the user site-packages path as requested. On this machine the PATH Python launcher was broken; the installed Blender Python 3.13 with its bundled NumPy was used without opening Blender.

```powershell
python docs/design/concept/audio_v3/analyze_sources.py
python docs/design/concept/audio_v3/build_audio_v3.py
```

Machine-specific equivalent: `& "C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" docs/design/concept/audio_v3/build_audio_v3.py`.

Keep the supplied `_cc0_src/` tree in place for rebuilds. The builder validates input hashes against [source_analysis.json](source_analysis.json), rejects changed protected inputs, reopens all written WAVs, and verifies headers, frame counts, peak, clipping, DC, zero endpoints and edge slopes. [validation.json](validation.json) stores per-layer processing, source hashes, output hashes, spectral/envelope measurements, warning-band ratios and the untouched v2/current-game input hashes. The build fails on violated checks. It does not write source files.

Delivery QA (2026-11-12): an independent standard-library WAV reader verified all 12 PCM headers, frame counts, peaks and zero endpoints. A second render produced 12/12 byte-identical WAVs. All 36 review audio paths resolved; JavaScript parsed, and a DOM simulation verified initial gain, slider updates, exclusive playback and Stop all. Browser decoding, layout and subjective listening were not tested.

Peak normalization is not loudness matching: compare at a comfortable slider setting, then assess relative game gains. The three warnings deliberately favor v2; numeric band dominance is not a guarantee of perceptual audibility.

## Human review

- Open review.html locally. Each row has v3, v2 and the current game file. The global SFX volume slider affects every player. Starting another player stops the previous player for clean comparison; missing paths show an error.
- Compare rapid swing/hit/UI repetition for harshness; parry must read as steel plus bronze ring, distinct from hit.
- Check dash movement, hurt weight and stunned servo texture without masking the Korean-inspired layers.
- In a later authorized game integration, check enemy/last/broadcast warnings during dense combat and BGM. Confirm jing/plucks still lead the dice result and buk still anchors shockwave; verify WebGL compression/polyphony.

## Draft credits / AI disclosure

See [CREDITS.md](CREDITS.md) for copy-ready attribution and AI disclosure. Root credits were not modified because this task permits writes only in audio_v3. Copy these credits into the project/submission when adopting.

Git pull was attempted but `.git/FETCH_HEAD` writes are denied by the workspace permissions. Staging was also attempted, but `.git/index.lock` could not be created; no commit was possible. No existing unrelated changes were edited.
