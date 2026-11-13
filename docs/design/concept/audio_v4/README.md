# NUMBER hit sound v4 — DRAFTS ONLY

Five CC0-source edits for frequent sword hits against robots. No audio is synthesized. No listening was possible: descriptions are design targets inferred from envelopes and spectra, not listening verdicts. All writes are confined to this folder; Assets and root CREDITS.md are unchanged. No commit or git pull was performed because this task permits writes only here.

Open [review.html](http://localhost:8766/docs/design/concept/audio_v4/review.html) through an HTTP server rooted at the repository. Each candidate sits beside the current hit; parry and hurt are separate contrast rows. Rapid fire schedules six hits, 180 ms apart, at playbackRates 1.00, 0.94, 1.08, 0.91, 1.04, 0.98. The single-play rate control also permits the full game range 0.45–1.4. A shared volume slider and Stop all affect native players and Web Audio. Starting any sound cancels the previous audition.

## Signal recipe and measurements

Mono 44,100 Hz, signed 16-bit PCM WAV; final sample peak −1 dBFS. Only the named CC0 recordings below are used; neither the old hit nor its procedural/drum layer is included. All intervals below are original-source seconds, end-exclusive, rounded to nearest original sample. Each crop is mean-centered and resampled with a 65-tap Hann-windowed sinc (anti-alias cutoff min(1, output/input rate)). EQ is a causal two-pole Butterworth high-pass followed by a two-pole low-pass. Optional exponential damping precedes each layer's 2 ms sine-squared attack and 12 ms cosine-squared release. Each layer is peak-normalized after processing and multiplied by the stated linear gain, then mixed at its offset. The master removes residual DC using the fade envelope, applies a 3 ms sine-squared attack / 18 ms cosine-squared release, and peak-normalizes once. Endpoints are exactly zero. No saturation, compressor, reverb, delay, pitch shift, or randomization is baked in.

| Draft | Length | RMS dBFS | Energy centroid | Below 180 Hz | 99% energy by |
|---|---:|---:|---:|---:|---:|
| [hit_a.wav](hit_a.wav) | 140 ms | -19.0 | 2964 Hz | 0.3% | 75.4 ms |
| [hit_b.wav](hit_b.wav) | 130 ms | -18.9 | 1706 Hz | 3.8% | 103.4 ms |
| [hit_c.wav](hit_c.wav) | 175 ms | -19.2 | 2221 Hz | 0.2% | 83.9 ms |
| [hit_d.wav](hit_d.wav) | 155 ms | -21.3 | 3510 Hz | 0.0% | 47.0 ms |
| [hit_e.wav](hit_e.wav) | 145 ms | -18.6 | 1200 Hz | 2.4% | 65.4 ms |

Current hit: 69% energy below 180 Hz, centroid 260 Hz; hurt: 91%, 97 Hz. The drafts move body energy upward. Parry has 31% energy after 120 ms and a 760 ms file; these candidates use much shorter tails and no clash/ringing-reward layers. Spectral separation and short tails support the intended distinction but do not prove perceived metal identity, pleasantness, or lack of fatigue. Equal peaks are not equal loudness; use the common slider and assess the game mixer gain after selecting.

## hit_a — Metallic slice / single source

Dry metal chk; the least elaborate option, retaining the original metal attack.

**Selection basis:** Metal hit 06 has 74% of full-source energy at 1.5-6 kHz, only 2% below 180 Hz, and t90=24 ms. It provides the strongest compact metal-edge/body compromise without layering or a clash recording.

- `oga_bfh_rubberduck/bfh1_metal_hit_06.ogg` — 0.000000–0.140000 s; source 48000 Hz; HP 180 Hz / LP 7000 Hz; gain 1; offset 0 ms; no added decay.
  Author **rubberduck**, [75 CC0 breaking / falling / hit sfx](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

## hit_b — Punch + tiny tick

Firm midrange body with a discrete, very short steel tick.

**Selection basis:** Independent hit14 has a sharp crest at 160 ms and t90=210 ms; isolate that crest, high-pass its low-heavy original. A 36 ms metalClick crop covers the 272 ms click crest without its preceding small actions.

- `oga_hits_independent/hit14.mp3.flac` — 0.150000–0.280000 s; source 44100 Hz; HP 400 Hz / LP 2300 Hz; gain 1; offset 0 ms; no added decay.
  Author **Independent.nu**, [37 hits/punches](https://opengameart.org/content/37-hitspunches), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- `rpg-audio/metalClick.wav` — 0.260000–0.296000 s; source 48000 Hz; HP 2100 Hz / LP 6800 Hz; gain 0.44; offset 0 ms; no added decay.
  Author **Kenney Vleugels (Kenney)**, [RPG Audio](https://kenney.nl/assets/rpg-audio), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

## hit_c — Crunchy robot shell

A metal bite followed by compact irregular mechanical crunch.

**Selection basis:** Metal hit04 concentrates 90% energy in 44 ms; misc03 supplies a broader 82 ms envelope and midrange texture. The short rock transient adds a dry granular edge; all layers are bounded before their long residual tails.

- `oga_bfh_rubberduck/bfh1_metal_hit_04.ogg` — 0.000000–0.160000 s; source 48000 Hz; HP 320 Hz / LP 5500 Hz; gain 1; offset 0 ms; no added decay.
  Author **rubberduck**, [75 CC0 breaking / falling / hit sfx](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- `oga_bfh_rubberduck/bfh1_misc_03.ogg` — 0.012000–0.175000 s; source 48000 Hz; HP 750 Hz / LP 4600 Hz; gain 0.52; offset 6 ms; exponential decay τ=55 ms.
  Author **rubberduck**, [75 CC0 breaking / falling / hit sfx](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- `oga_bfh_rubberduck/bfh1_rock_hit_01.ogg` — 0.000000–0.045000 s; source 48000 Hz; HP 1600 Hz / LP 6200 Hz; gain 0.24; offset 2 ms; no added decay.
  Author **rubberduck**, [75 CC0 breaking / falling / hit sfx](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

## hit_d — Bright short shing

The brightest blade edge, with a light metallic body and no sustained reward ring.

**Selection basis:** Sword06 has its crest at 24 ms, unlike the slow swells of most sword files. Light metal02 has t90=31 ms and 74% energy at 1.5-6 kHz. Exponential damping ends the sword hiss quickly; no parry/clash source is used.

- `sword/sword.6.wav` — 0.016000–0.151000 s; source 44100 Hz; HP 2400 Hz / LP 7800 Hz; gain 0.55; offset 0 ms; exponential decay τ=45 ms.
  Author **StarNinjas**, [20 Sword Sound Effects (Attacks and Clashes)](https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- `impact-sounds/impactMetal_light_002.wav` — 0.000000–0.155000 s; source 44100 Hz; HP 450 Hz / LP 6200 Hz; gain 1; offset 0 ms; exponential decay τ=60 ms.
  Author **Kenney**, [Impact Sounds](https://kenney.nl/assets/impact-sounds), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

## hit_e — Dark steel bite

Lower metallic knock with a restrained cutting edge, avoiding sub-bass weight.

**Selection basis:** Qubodup metal has an immediate crest and t90=33 ms but excess very high and low energy; 280-3400 Hz filtering isolates its short metal body. Hit08 contributes a 41 ms body transient, high-passed to keep it distinct from the hurt sound.

- `oga_impact_qubodup/qubodupImpactMetal.flac` — 0.000000–0.145000 s; source 44100 Hz; HP 280 Hz / LP 3400 Hz; gain 1; offset 0 ms; no added decay.
  Author **qubodup**, [Impact](https://opengameart.org/content/impact), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
- `oga_bfh_rubberduck/bfh1_hit_08.ogg` — 0.008000–0.108000 s; source 48000 Hz; HP 340 Hz / LP 1600 Hz; gain 0.48; offset 2 ms; no added decay.
  Author **rubberduck**, [75 CC0 breaking / falling / hit sfx](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).

## Sources / credits to copy on adoption

Every path above is relative to `../audio_v3/_cc0_src/`. Those supplied raw sources stay gitignored and are required to rebuild. The new OGA packs' author/URL/CC0 attribution follows the supplied source manifest; no external files were downloaded or new license claims inferred. Existing local Kenney RPG Audio license was read; Kenney/StarNinjas credits also appear in audio_v3. Source hashes and native rates are in [source_analysis.json](source_analysis.json) and [validation.json](validation.json).

- 37 hits/punches — Independent.nu. https://opengameart.org/content/37-hitspunches — CC0 1.0. Edited/layered for NUMBER hit v4.
- Impact — qubodup. https://opengameart.org/content/impact — CC0 1.0. Edited/layered for NUMBER hit v4.
- 75 CC0 breaking / falling / hit sfx — rubberduck. https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx — CC0 1.0. Edited/layered for NUMBER hit v4.
- 20 Sword Sound Effects (Attacks and Clashes) — StarNinjas. https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes — CC0 1.0. Edited/layered for NUMBER hit v4.
- Impact Sounds — Kenney. https://kenney.nl/assets/impact-sounds — CC0 1.0. Edited/layered for NUMBER hit v4.
- RPG Audio — Kenney Vleugels (Kenney). https://kenney.nl/assets/rpg-audio — CC0 1.0. Edited/layered for NUMBER hit v4.

**AI disclosure:** Codex / OpenAI GPT selected source intervals using signal analysis and authored the deterministic DSP build script and review page. These are edits of supplied CC0 recordings/designed SFX, not newly AI-synthesized recordings. Record this assistance in project/submission credits when adopting; root credits are intentionally untouched.

## Rebuild / verification

```powershell
python -s docs/design/concept/audio_v4/build_audio_v4.py --analyze
python -s docs/design/concept/audio_v4/build_audio_v4.py
```

Requires NumPy and soundfile; the builder appends `site.getusersitepackages()`. This shell has no `python` on PATH, so these drafts were built using the installed Blender Python and existing libsndfile DLL through ctypes, without opening Blender. Equivalent:

```powershell
& "C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" -s docs/design/concept/audio_v4/build_audio_v4.py
```

Build reopens each PCM file and checks mono/rate/bit depth/frame count, ≤300 ms, −1 dBFS peak, DC, zero endpoints, ≤10 ms onset, <12% bass below 180 Hz, <4% energy above 8 kHz, and t99 <120 ms. It checks source hashes, verifies the used sources and three game references are unchanged during the build, and checks that candidates are not near-identical waveforms. Analysis uses full-event FFT energy, 2 ms block RMS onsets (8% of maximum block RMS), and cumulative squared-sample energy. These are descriptive metrics, not perceptual loudness or fatigue models.

**Delivery checks (2026-11-13):** all five WAVs rebuilt byte-identically; an independent standard-library WAV read verified headers, lengths, zero endpoints and uncompressed PCM. HTTP requests returned the exact draft/page/doc bytes and loaded all three Assets references. JavaScript parsed and a DOM/Web Audio simulation checked the 5+2 rows, all 12 audio paths, exact six-hit start times and pitches, shared gain, single-play pitch, cache reuse, native-player exclusivity, stop during a pending decode, and HTTP failure feedback. A loopback-only HTTP server was started on port 8766 for review. Actual browser rendering/decoding and subjective listening were not tested.

At playbackRate 0.45, durations expand by 2.22× and energy shifts downward; the 300 ms requirement applies to the exported WAVs. Use the rate selector to check overlap and hurt confusion at the low extreme, and harshness at 1.4. Rapid fire is deliberately fixed to the requested 0.9–1.1 range. Human acceptance: listen to all five at the same level, compare to current hit/parry/hurt, run several rapid-fire bursts, then check the selected sound in dense gameplay before integration. No game code or import settings were changed.
