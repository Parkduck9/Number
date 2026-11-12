# NUMBER — Korean instruments + synth / audio v2 DRAFTS

All deliverables stay in this directory. Nothing is imported into Unity; `Assets/` and the root credits are unchanged. Source references read: `Assets/Audio/Source/gen-audio.mjs.txt`, `Assets/Scripts/Audio/SfxId.cs`, and all 14 existing WAVs in the Sfx/Bgm resource folders.

**Listening limitation:** this agent's tool environment does not support audio perception. The original and new files were inspected as PCM signals; a subjective listening pass is still pending. Descriptions below state the intended sound and synthesis design, not a claimed listening verdict. Open **[review.html](review.html)** for old/new playback, SFX over music, and exact-frame loop-boundary audition. Everything runs locally without a server, download, or service. The boundary audition uses a file picker to work around browsers' local-file fetch restrictions.

## Files

All WAVs: mono, 32,000 Hz, signed 16-bit little-endian PCM; measured peak −1.0002 dBFS. SFX have short smooth onset/release fades; BGM has **no endpoint fade**.

| File | Length | Sound / purpose |
|---|---:|---|
| [sfx_swing.wav](sfx_swing.wav) | 0.200 s | Sweeping sword air with a faint, dark plucked-string scrape. |
| [sfx_hit.wav](sfx_hit.wav) | 0.280 s | Janggu **덕** stick crack, inharmonic robot clank and a short noise snap. |
| [sfx_parry.wav](sfx_parry.wav) | 0.700 s | Prominent bright **꽹과리** impact, a compact low punch and pitched bronze ringing tail. |
| [sfx_dash.wav](sfx_dash.wav) | 0.160 s | Air rush plus a short rising, lightly crushed digital zip. |
| [sfx_hurt.wav](sfx_hurt.wav) | 0.300 s | Deep **북** thud underneath a descending crushed robot glitch. |
| [sfx_enemy_tell.wav](sfx_enemy_tell.wav) | 0.200 s | Focused rising robot blip with a terminal ping; kept above most musical fundamentals. |
| [sfx_last_tell.wav](sfx_last_tell.wav) | 0.500 s | Two urgent descending reed/synth stabs, A5 → E5, with a low drum accent. |
| [sfx_stunned.wav](sfx_stunned.wav) | 0.750 s | Pulsing overload fizz plus a strongly falling bent **가야금** string. |
| [sfx_broadcast.wav](sfx_broadcast.wav) | 1.900 s | Four cold, slightly detuned digital-bell notes, A5 → E5 → G5 → A4; an original public-address chime. |
| [sfx_hack_ok.wav](sfx_hack_ok.wav) | 1.000 s | Low **징** bloom and six ascending bright plucks, A4–C5–D5–E5–G5–A5; die face / fortune revealed. |
| [sfx_ui_select.wav](sfx_ui_select.wav) | 0.120 s | Tiny wooden **박** clapper tick with a soft high pluck. |
| [sfx_shockwave.wav](sfx_shockwave.wav) | 1.800 s | Heavy **북**, spreading **징**, filtered rumble and a falling low sweep. |
| [bgm_field.wav](bgm_field.wav) | 48.000 s | 120 BPM, 24 bars: triplet janggu, dark bass, bent pluck motif, sparse neon pad; reed B section. |
| [bgm_boss.wav](bgm_boss.wav) | 40.000 s | 144 BPM, 24 bars: heavier buk/janggu, saturated bass, urgent reed phrases, section gongs and chopped bell/broadcast fragments. |

`SfxId.cs` currently lists 11 SFX; Shockwave is present in the existing audio folder and explicitly requested here, so its WAV is included. No enum or game wiring was changed.

## Arrangement and synthesis

Both tracks use A minor as their center, predominantly A–C–D–E–G melodic vocabulary, with F color on F-harmony phrases. This is an original Korean-inspired game arrangement, not a transcription or a claim to reproduce a particular traditional mode or jangdan exactly.

Field: three 8-bar sections (0–16 s / 16–32 s / 32–48 s). A introduces the bent-string motif; B thins the strings and introduces a formant-shaped reed lead; A′ returns to plucks with low geomungo-like answers. The groove uses 12 subdivisions per four main beats, combining a compound janggu feel with a game-music bass pulse. No constant high-hat wash. Bass and pads stay dark, and the reed leaves phrase gaps for combat.

Boss: three 8-bar sections (0–13⅓ s / 13⅓–26⅔ s / 26⅔–40 s). Double buk accents, more active bass, short reed calls, jing at section starts, then an upper pluck response in the final section. Glitch fragments are deliberately quieter than enemy tells. The suggested “~140 BPM” becomes 144 BPM to produce exactly 40 seconds across 24 whole bars.

Plucks use fractional-delay Karplus–Strong feedback with bridge damping, pitch relaxation, subtle nonghyeon-like oscillation, and a small nonlinear bridge buzz. Darker damping produces the geomungo-like register. Janggu separates **쿵** (low skin), **덕** (sharp stick), and **덩** (both). Gongs use beating inharmonic modal banks; the jing blooms more slowly than the small gong. Reed uses band-limited additive saw/pulse harmonics weighted around two formants, breath noise and delayed vibrato. Robot voices use locally synthesized sample/hold and quantization, followed by filtering. These are procedural instrument approximations, not recordings.

Peak normalization is intentionally shared across files as requested; it is not loudness matching. The review player's starting gains (music 0.28, SFX 0.65) are audition conveniences, not changes to the game's mixer. Final relative gain and combat masking need human review in Play mode before adoption.

## Reproduce and verify

Run from the repository root with Node.js 18 or later, no npm packages:

```powershell
node docs/design/concept/audio_v2/gen_audio_v2.mjs
node docs/design/concept/audio_v2/check_audio_v2.mjs
```

The generator always writes beside its own source, never to Assets. A fixed seeded PRNG makes renders repeatable. The independent checker re-opens the on-disk WAVs, validates PCM headers, exact frame counts, peaks, DC, clipping and SFX endpoint fades; it also measures the BGM seam step and slope. Its nonzero exit code reports a failed check. Full measurements and SHA-256 hashes for every draft and original are in [validation.json](validation.json).

Latest verification: **14/14 passed**. No clipped samples. Maximum absolute mean DC across all drafts is below 0.00000025 full scale (about −132 dBFS); PCM quantization can leave a sub-LSB mean. Every SFX starts and ends at exactly zero. Music endpoints contain continuous running tails rather than silence.

| Loop | Exact frames / region | Peak | RMS | Last → first step | Slope mismatch | WAV size |
|---|---|---:|---:|---:|---:|---:|
| Field | 1,536,000 / [0, 1,536,000) | −1.0002 dBFS | −17.65 dBFS | 54 LSB / 0.001648 FS | 8 LSB | 3,072,044 B / 2.930 MiB |
| Boss | 1,280,000 / [0, 1,280,000) | −1.0002 dBFS | −14.84 dBFS | 10 LSB / 0.000305 FS | 8 LSB | 2,560,044 B / 2.441 MiB |

A continuous waveform need not have identical adjacent samples. Both seam steps are below −55 dBFS and below the respective track's 99th-percentile ordinary adjacent step (0.03500 / 0.03046 FS); both pass the checker limits of 0.01 FS seam step and 0.015 FS slope mismatch. Events and their release tails wrap modulo the exact loop frame count; short dark room reflections also wrap, and filters warm their state on a complete prior loop. There is no fade or splice crossfade. Set loop start to frame 0 and loop end to the exclusive frame above. The field WAV is approximately 3 MB in decimal units; both are below 3 MiB. All fourteen WAVs total 6,138,856 B (5.854 MiB).

Baseline PCM inspection found field 27.428594 s and boss 19.200 s, with seam steps 0.06369 and 0.06299 FS respectively. Several original SFX had nonzero mean DC. These measurements motivated explicit circular filtering/tail wrapping and final DC correction. They are signal findings, not listening judgments.

## Human listening / adoption checklist

- A/B every file in review.html; confirm intended timbre, especially parry's satisfying bronze ring and the two distinct warning cues.
- Audition both complete loops repeatedly and use “Boundary ×2” (join at two seconds in each pass); check for a click, gap, rhythmic hesitation, or awkward harmonic return.
- Keep either music playing and trigger hit, parry, enemy tell and last tell; all cues should remain readable. Check several fast repetitions of swing, hit and UI select for fatigue.
- In Unity Play mode after approval: test dense combat and simultaneous jing / shockwave / die confirmation, then test the WebGL build for imported compression loop behavior and polyphonic headroom.
- Copy the draft AI/provenance entry from [CREDITS.md](CREDITS.md) into root credits and the jam disclosure when the team adopts these files. No approval, import, mixer change or integration is implied by this draft delivery.

No downloaded samples, online services, paid tools or external dependencies were used. Git pull was attempted before work but could not write `.git/FETCH_HEAD` under the current filesystem permissions. Staging was also attempted, but `.git/index.lock` could not be created; no commit was possible. Unrelated pre-existing worktree changes were left alone.

Re-rendering produced byte-identical SHA-256 hashes for all 14 drafts. All 14 original audio hashes remained unchanged. The review player's JavaScript passed a syntax check; browser playback and subjective listening remain for human review.
