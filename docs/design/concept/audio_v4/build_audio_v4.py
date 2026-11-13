"""NUMBER hit drafts. All writes are beside this script; source files are read-only.

Run: python -s docs/design/concept/audio_v4/build_audio_v4.py [--analyze]
Requires NumPy and soundfile. A ctypes/libsndfile fallback supports the installed
Blender Python when its version differs from the existing soundfile installation.
No downloads, generated tones, random noise, or writes to Assets.
"""
from pathlib import Path
import argparse
import ctypes as ct
import ctypes.util
import hashlib
import json
import math
import site
import sys
import wave

sys.dont_write_bytecode = True
sys.path.append(site.getusersitepackages())
import numpy as np

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
SOURCES = HERE.parent / 'audio_v3' / '_cc0_src'
SR = 44100
TARGET = 10 ** (-1 / 20)
BASELINES = {name: ROOT / 'Assets/Audio/Resources/Sfx' / (name + '.wav')
             for name in ('sfx_hit', 'sfx_parry', 'sfx_hurt')}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


try:
    import soundfile as sf

    def read_audio(path):
        data, sr = sf.read(str(path), dtype='float64', always_2d=True)
        return data.mean(axis=1), sr
except ImportError:
    # Use the already installed decoder, without importing incompatible cp312 cffi.
    candidates = list((Path.home() / 'AppData/Roaming/Python').glob(
        'Python*/site-packages/_soundfile_data/libsndfile_x64.dll'))
    libpath = str(candidates[0]) if candidates else ct.util.find_library('sndfile')
    if not libpath:
        raise RuntimeError('Install soundfile or provide an existing libsndfile.')
    snd = ct.CDLL(libpath)

    class Info(ct.Structure):
        _fields_ = [('frames', ct.c_int64), ('samplerate', ct.c_int),
                    ('channels', ct.c_int), ('format', ct.c_int),
                    ('sections', ct.c_int), ('seekable', ct.c_int)]

    snd.sf_open.argtypes = [ct.c_char_p, ct.c_int, ct.POINTER(Info)]
    snd.sf_open.restype = ct.c_void_p
    snd.sf_readf_double.argtypes = [ct.c_void_p, ct.POINTER(ct.c_double), ct.c_int64]
    snd.sf_readf_double.restype = ct.c_int64
    snd.sf_close.argtypes = [ct.c_void_p]

    def read_audio(path):
        info = Info()
        handle = snd.sf_open(str(path).encode('utf-8'), 0x10, ct.byref(info))
        if not handle:
            raise RuntimeError('Cannot decode ' + str(path))
        try:
            data = np.empty((info.frames, info.channels), dtype=np.float64)
            count = snd.sf_readf_double(handle, data.ctypes.data_as(ct.POINTER(ct.c_double)), info.frames)
            if count != info.frames:
                raise RuntimeError('Incomplete decode: ' + str(path))
            return data.mean(axis=1), info.samplerate
        finally:
            snd.sf_close(handle)


def db(value):
    return float(20 * np.log10(max(float(value), 1e-12)))


def metrics(x, sr):
    energy = x * x
    total = max(float(energy.sum()), 1e-20)
    cumulative = np.cumsum(energy) / total
    # Unwindowed Parseval spectrum: energy fractions refer to the entire event.
    spectrum = np.abs(np.fft.rfft(x)) ** 2
    freq = np.fft.rfftfreq(len(x), 1 / sr)
    norm = max(float(spectrum.sum()), 1e-20)
    def band(a, b):
        return float(spectrum[(freq >= a) & (freq < b)].sum() / norm)
    block = max(1, round(.002 * sr))
    rms = np.sqrt(np.mean(np.pad(energy, (0, (-len(x)) % block)).reshape(-1, block), axis=1))
    onset = int(np.flatnonzero(rms > max(rms.max() * .08, 1e-7))[0]) * block
    return dict(duration_s=len(x) / sr, peak_dbfs=db(np.max(np.abs(x))),
                rms_dbfs=db(np.sqrt(np.mean(energy))), dc=float(x.mean()),
                onset_s=onset / sr, crest_time_s=int(np.argmax(rms)) * block / sr,
                t50_s=int(np.searchsorted(cumulative, .5)) / sr,
                t90_s=int(np.searchsorted(cumulative, .9)) / sr,
                t99_s=int(np.searchsorted(cumulative, .99)) / sr,
                centroid_hz=float(np.sum(spectrum * freq) / norm),
                below_180=band(0, 180), body_180_1500=band(180, 1500),
                edge_1500_6000=band(1500, 6000), above_8000=band(8000, sr / 2 + 1),
                energy_after_120ms=float(energy[round(.12 * sr):].sum() / total))


def analyze():
    paths = []
    for folder in ('oga_hits_independent', 'oga_impact_qubodup', 'oga_bfh_rubberduck', 'sword', 'clash'):
        paths.extend(p for p in (SOURCES / folder).rglob('*') if p.suffix.lower() in ('.wav', '.flac', '.ogg'))
    paths.extend((SOURCES / 'rpg-audio').glob('*metal*'))
    paths.extend((SOURCES / 'impact-sounds').glob('*Metal*'))
    results = {}
    for path in sorted(paths):
        x, sr = read_audio(path)
        m = metrics(x, sr)
        name = path.relative_to(SOURCES).as_posix()
        results[name] = dict(sha256=sha(path), sample_rate=sr, **m)
        print(f"{name:57s} dur={m['duration_s']:.3f} onset={m['onset_s']:.3f} crest={m['crest_time_s']:.3f} "
              f"t90={m['t90_s']:.3f} ctr={m['centroid_hz']:.0f} low={m['below_180']:.2f} edge={m['edge_1500_6000']:.2f}")
    baseline = {}
    for name, path in BASELINES.items():
        x, sr = read_audio(path)
        baseline[name] = dict(sha256=sha(path), sample_rate=sr, **metrics(x, sr))
        print(name, json.dumps(baseline[name]))
    (HERE / 'source_analysis.json').write_text(json.dumps(dict(sources=results, baselines=baseline), indent=2) + '\n', encoding='utf-8')


PACKS = {
    'oga_hits_independent': dict(author='Independent.nu', pack='37 hits/punches', url='https://opengameart.org/content/37-hitspunches'),
    'oga_impact_qubodup': dict(author='qubodup', pack='Impact', url='https://opengameart.org/content/impact'),
    'oga_bfh_rubberduck': dict(author='rubberduck', pack='75 CC0 breaking / falling / hit sfx', url='https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx'),
    'sword': dict(author='StarNinjas', pack='20 Sword Sound Effects (Attacks and Clashes)', url='https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes'),
    'impact-sounds': dict(author='Kenney', pack='Impact Sounds', url='https://kenney.nl/assets/impact-sounds'),
    'rpg-audio': dict(author='Kenney Vleugels (Kenney)', pack='RPG Audio', url='https://kenney.nl/assets/rpg-audio'),
}


def layer(file, start, end, gain, hp, lp, offset=0, decay=0):
    return dict(file=file, source_start_s=start, source_end_s=end, gain=gain,
                hp_hz=hp, lp_hz=lp, offset_s=offset, decay_tau_s=decay)


RECIPES = {
    'hit_a': dict(title='Metallic slice / single source', seconds=.140,
        intent='Dry metal chk; the least elaborate option, retaining the original metal attack.',
        why='Metal hit 06 has 74% of full-source energy at 1.5-6 kHz, only 2% below 180 Hz, and t90=24 ms. It provides the strongest compact metal-edge/body compromise without layering or a clash recording.',
        layers=[layer('oga_bfh_rubberduck/bfh1_metal_hit_06.ogg', 0, .140, 1, 180, 7000)]),
    'hit_b': dict(title='Punch + tiny tick', seconds=.130,
        intent='Firm midrange body with a discrete, very short steel tick.',
        why='Independent hit14 has a sharp crest at 160 ms and t90=210 ms; isolate that crest, high-pass its low-heavy original. A 36 ms metalClick crop covers the 272 ms click crest without its preceding small actions.',
        layers=[layer('oga_hits_independent/hit14.mp3.flac', .150, .280, 1, 400, 2300),
                layer('rpg-audio/metalClick.wav', .260, .296, .44, 2100, 6800)]),
    'hit_c': dict(title='Crunchy robot shell', seconds=.175,
        intent='A metal bite followed by compact irregular mechanical crunch.',
        why='Metal hit04 concentrates 90% energy in 44 ms; misc03 supplies a broader 82 ms envelope and midrange texture. The short rock transient adds a dry granular edge; all layers are bounded before their long residual tails.',
        layers=[layer('oga_bfh_rubberduck/bfh1_metal_hit_04.ogg', 0, .160, 1, 320, 5500),
                layer('oga_bfh_rubberduck/bfh1_misc_03.ogg', .012, .175, .52, 750, 4600, offset=.006, decay=.055),
                layer('oga_bfh_rubberduck/bfh1_rock_hit_01.ogg', 0, .045, .24, 1600, 6200, offset=.002)]),
    'hit_d': dict(title='Bright short shing', seconds=.155,
        intent='The brightest blade edge, with a light metallic body and no sustained reward ring.',
        why='Sword06 has its crest at 24 ms, unlike the slow swells of most sword files. Light metal02 has t90=31 ms and 74% energy at 1.5-6 kHz. Exponential damping ends the sword hiss quickly; no parry/clash source is used.',
        layers=[layer('sword/sword.6.wav', .016, .151, .55, 2400, 7800, decay=.045),
                layer('impact-sounds/impactMetal_light_002.wav', 0, .155, 1, 450, 6200, offset=0, decay=.060)]),
    'hit_e': dict(title='Dark steel bite', seconds=.145,
        intent='Lower metallic knock with a restrained cutting edge, avoiding sub-bass weight.',
        why='Qubodup metal has an immediate crest and t90=33 ms but excess very high and low energy; 280-3400 Hz filtering isolates its short metal body. Hit08 contributes a 41 ms body transient, high-passed to keep it distinct from the hurt sound.',
        layers=[layer('oga_impact_qubodup/qubodupImpactMetal.flac', 0, .145, 1, 280, 3400),
                layer('oga_bfh_rubberduck/bfh1_hit_08.ogg', .008, .108, .48, 340, 1600, offset=.002)]),
}


def biquad(x, hz, highpass):
    """Causal RBJ two-pole Butterworth (Q=1/sqrt(2)); no pre-ringing."""
    w = 2 * math.pi * hz / SR
    c, s = math.cos(w), math.sin(w)
    alpha = s / math.sqrt(2)
    if highpass:
        b = np.array([(1 + c) / 2, -(1 + c), (1 + c) / 2])
    else:
        b = np.array([(1 - c) / 2, 1 - c, (1 - c) / 2])
    b /= 1 + alpha
    a1, a2 = -2 * c / (1 + alpha), (1 - alpha) / (1 + alpha)
    y = np.zeros_like(x)
    x1 = x2 = y1 = y2 = 0.
    for i, v in enumerate(x):
        out = b[0] * v + b[1] * x1 + b[2] * x2 - a1 * y1 - a2 * y2
        y[i] = out
        x2, x1, y2, y1 = x1, v, y1, out
    return y


def fade_envelope(n, attack, release):
    a, r = round(attack * SR), round(release * SR)
    env = np.ones(n)
    env[:a] *= np.sin(np.linspace(0, math.pi / 2, a)) ** 2
    env[-r:] *= np.cos(np.linspace(0, math.pi / 2, r)) ** 2
    env[0] = env[-1] = 0
    return env


def render_layer(spec):
    path = SOURCES / spec['file']
    x, sr = read_audio(path)
    i, j = round(spec['source_start_s'] * sr), round(spec['source_end_s'] * sr)
    assert 0 <= i < j <= len(x), path
    x = x[i:j]
    x -= np.mean(x)
    # Windowed-sinc resampling, with anti-aliasing when downsampling. Unity
    # playback shifts are auditioned in the browser, not baked into the files.
    n = round(len(x) * SR / sr)
    pos = np.arange(n) * sr / SR
    centers = np.floor(pos).astype(int)
    taps = np.arange(-32, 33)
    idx = centers[:, None] + taps
    delta = pos[:, None] - idx
    cutoff = min(1, SR / sr)
    kernel = cutoff * np.sinc(cutoff * delta) * np.where(np.abs(delta) <= 32, .5 + .5 * np.cos(math.pi * delta / 32), 0)
    kernel /= np.sum(kernel, axis=1, keepdims=True)
    x = np.sum(x[np.clip(idx, 0, len(x) - 1)] * kernel, axis=1)
    x = biquad(biquad(x, spec['hp_hz'], True), spec['lp_hz'], False)
    if spec['decay_tau_s']:
        x *= np.exp(-np.arange(len(x)) / (SR * spec['decay_tau_s']))
    x *= fade_envelope(len(x), .002, .012)
    x *= spec['gain'] / max(np.max(np.abs(x)), 1e-12)
    return x


def build():
    analysis_path = HERE / 'source_analysis.json'
    if not analysis_path.exists():
        analyze()
    analysis = json.loads(analysis_path.read_text(encoding='utf-8'))
    protected = {p: sha(p) for p in BASELINES.values()}
    for name, path in BASELINES.items():
        assert protected[path] == analysis['baselines'][name]['sha256'], 'Reference changed; rerun --analyze: ' + str(path)
    report = dict(sample_rate=SR, subtype='PCM_16', listening_performed=False,
                  metrics_note='Full-event energy spectrum; t90/t99 measured from the start of each file.',
                  baselines=analysis['baselines'], candidates={})
    for name, recipe in RECIPES.items():
        n = round(recipe['seconds'] * SR)
        mix = np.zeros(n)
        details = []
        for spec in recipe['layers']:
            path = SOURCES / spec['file']
            assert sha(path) == analysis['sources'][spec['file']]['sha256'], 'Source changed: ' + str(path)
            protected[path] = sha(path)
            x = render_layer(spec)
            start = round(spec['offset_s'] * SR)
            count = min(len(x), n - start)
            mix[start:start + count] += x[:count]
            details.append(dict(**spec, sha256=sha(path), source_rate=analysis['sources'][spec['file']]['sample_rate'],
                                **PACKS[Path(spec['file']).parts[0]], license='CC0 1.0'))
        env = fade_envelope(n, .003, .018)
        # Remove residual mean while preserving fade shape and zero endpoints.
        mix = (mix - np.sum(mix * env) / np.sum(env)) * env
        mix *= TARGET / np.max(np.abs(mix))
        pcm = np.rint(mix * 32768).astype('<i2')
        path = HERE / (name + '.wav')
        with wave.open(str(path), 'wb') as out:
            out.setparams((1, 2, SR, n, 'NONE', 'not compressed'))
            out.writeframes(pcm.tobytes())
        with wave.open(str(path), 'rb') as out:
            assert (out.getnchannels(), out.getsampwidth(), out.getframerate(), out.getnframes()) == (1, 2, SR, n)
            decoded = np.frombuffer(out.readframes(n), dtype='<i2') / 32768.
        m = metrics(decoded, SR)
        assert len(decoded) / SR <= .3
        assert -.01 < m['dc'] < .01
        assert abs(m['dc']) < 2e-5
        assert -1.02 <= m['peak_dbfs'] <= -.98
        assert decoded[0] == decoded[-1] == 0
        assert m['onset_s'] <= .010
        assert m['below_180'] < .12, 'Excess bass'
        assert m['above_8000'] < .04, 'Excess high hiss'
        assert m['t99_s'] < .120, 'Tail too long for repeated cuts'
        report['candidates'][name] = dict(title=recipe['title'], sha256=sha(path), bytes=path.stat().st_size,
            master_attack_s=.003, master_release_s=.018, layer_attack_s=.002, layer_release_s=.012,
            layers=details, **m, duration_at_rate_045_s=m['duration_s'] / .45,
            t99_at_rate_045_s=m['t99_s'] / .45)
        print(name, json.dumps(m))
    for path, digest in protected.items():
        assert sha(path) == digest, 'Read-only input changed: ' + str(path)
    # Catch duplicate/superficially rescaled drafts: compare normalized waveforms.
    similarities = {}
    names = list(RECIPES)
    for a, name in enumerate(names):
        x, _ = read_audio(HERE / (name + '.wav'))
        for other in names[a + 1:]:
            y, _ = read_audio(HERE / (other + '.wav'))
            n = max(len(x), len(y))
            corr = float(np.dot(np.pad(x, (0, n - len(x))), np.pad(y, (0, n - len(y)))) / np.sqrt(np.dot(x, x) * np.dot(y, y)))
            similarities[name + '/' + other] = corr
            assert abs(corr) < .90
    report['pairwise_waveform_cosine'] = similarities
    report['protected_input_hashes_unchanged'] = True
    (HERE / 'validation.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    write_readme(report)


def write_readme(report):
    lines = ['# NUMBER hit sound v4 — DRAFTS ONLY', '',
        'Five CC0-source edits for frequent sword hits against robots. No audio is synthesized. '
        'No listening was possible: descriptions are design targets inferred from envelopes and spectra, not listening verdicts. '
        'All writes are confined to this folder; Assets and root CREDITS.md are unchanged. No commit or git pull was performed because this task permits writes only here.', '',
        'Open [review.html](http://localhost:8766/docs/design/concept/audio_v4/review.html) through an HTTP server rooted at the repository. '
        'Each candidate sits beside the current hit; parry and hurt are separate contrast rows. '
        'Rapid fire schedules six hits, 180 ms apart, at playbackRates 1.00, 0.94, 1.08, 0.91, 1.04, 0.98. '
        'The single-play rate control also permits the full game range 0.45–1.4. '
        'A shared volume slider and Stop all affect native players and Web Audio. Starting any sound cancels the previous audition.', '',
        '## Signal recipe and measurements', '',
        'Mono 44,100 Hz, signed 16-bit PCM WAV; final sample peak −1 dBFS. '
        'Only the named CC0 recordings below are used; neither the old hit nor its procedural/drum layer is included. '
        'All intervals below are original-source seconds, end-exclusive, rounded to nearest original sample. '
        'Each crop is mean-centered and resampled with a 65-tap Hann-windowed sinc (anti-alias cutoff min(1, output/input rate)). '
        'EQ is a causal two-pole Butterworth high-pass followed by a two-pole low-pass. '
        'Optional exponential damping precedes each layer\'s 2 ms sine-squared attack and 12 ms cosine-squared release. '
        'Each layer is peak-normalized after processing and multiplied by the stated linear gain, then mixed at its offset. '
        'The master removes residual DC using the fade envelope, applies a 3 ms sine-squared attack / 18 ms cosine-squared release, '
        'and peak-normalizes once. Endpoints are exactly zero. No saturation, compressor, reverb, delay, pitch shift, or randomization is baked in.', '',
        '| Draft | Length | RMS dBFS | Energy centroid | Below 180 Hz | 99% energy by |',
        '|---|---:|---:|---:|---:|---:|']
    for name, m in report['candidates'].items():
        lines.append(f"| [{name}.wav]({name}.wav) | {m['duration_s']*1000:.0f} ms | {m['rms_dbfs']:.1f} | {m['centroid_hz']:.0f} Hz | {m['below_180']*100:.1f}% | {m['t99_s']*1000:.1f} ms |")
    lines += ['', 'Current hit: 69% energy below 180 Hz, centroid 260 Hz; hurt: 91%, 97 Hz. '
        'The drafts move body energy upward. Parry has 31% energy after 120 ms and a 760 ms file; '
        'these candidates use much shorter tails and no clash/ringing-reward layers. '
        'Spectral separation and short tails support the intended distinction but do not prove perceived metal identity, pleasantness, or lack of fatigue. '
        'Equal peaks are not equal loudness; use the common slider and assess the game mixer gain after selecting.', '']
    for name, recipe in RECIPES.items():
        lines += [f"## {name} — {recipe['title']}", '', recipe['intent'], '', '**Selection basis:** ' + recipe['why'], '']
        for spec in report['candidates'][name]['layers']:
            lines += [f"- `{spec['file']}` — {spec['source_start_s']:.6f}–{spec['source_end_s']:.6f} s; "
                      f"source {spec['source_rate']} Hz; HP {spec['hp_hz']} Hz / LP {spec['lp_hz']} Hz; "
                      f"gain {spec['gain']}; offset {spec['offset_s']*1000:g} ms; "
                      + (f"exponential decay τ={spec['decay_tau_s']*1000:g} ms." if spec['decay_tau_s'] else 'no added decay.'),
                      f"  Author **{spec['author']}**, [{spec['pack']}]({spec['url']}), [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)."]
        lines += ['']
    lines += ['## Sources / credits to copy on adoption', '',
        'Every path above is relative to `../audio_v3/_cc0_src/`. Those supplied raw sources stay gitignored and are required to rebuild. '
        'The new OGA packs\' author/URL/CC0 attribution follows the supplied source manifest; no external files were downloaded or new license claims inferred. '
        'Existing local Kenney RPG Audio license was read; Kenney/StarNinjas credits also appear in audio_v3. '
        'Source hashes and native rates are in [source_analysis.json](source_analysis.json) and [validation.json](validation.json).', '']
    for pack in PACKS.values():
        lines.append(f"- {pack['pack']} — {pack['author']}. {pack['url']} — CC0 1.0. Edited/layered for NUMBER hit v4.")
    lines += ['', '**AI disclosure:** Codex / OpenAI GPT selected source intervals using signal analysis and authored the deterministic DSP build script and review page. '
        'These are edits of supplied CC0 recordings/designed SFX, not newly AI-synthesized recordings. Record this assistance in project/submission credits when adopting; root credits are intentionally untouched.', '',
        '## Rebuild / verification', '', '```powershell',
        'python -s docs/design/concept/audio_v4/build_audio_v4.py --analyze',
        'python -s docs/design/concept/audio_v4/build_audio_v4.py', '```', '',
        'Requires NumPy and soundfile; the builder appends `site.getusersitepackages()`. '
        'This shell has no `python` on PATH, so these drafts were built using the installed Blender Python and existing libsndfile DLL through ctypes, without opening Blender. Equivalent:', '',
        '```powershell',
        '& "C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" -s docs/design/concept/audio_v4/build_audio_v4.py', '```', '',
        'Build reopens each PCM file and checks mono/rate/bit depth/frame count, ≤300 ms, −1 dBFS peak, DC, zero endpoints, '
        '≤10 ms onset, <12% bass below 180 Hz, <4% energy above 8 kHz, and t99 <120 ms. '
        'It checks source hashes, verifies the used sources and three game references are unchanged during the build, '
        'and checks that candidates are not near-identical waveforms. '
        'Analysis uses full-event FFT energy, 2 ms block RMS onsets (8% of maximum block RMS), and cumulative squared-sample energy. '
        'These are descriptive metrics, not perceptual loudness or fatigue models.', '',
        '**Delivery checks (2026-11-13):** all five WAVs rebuilt byte-identically; an independent standard-library WAV read verified '
        'headers, lengths, zero endpoints and uncompressed PCM. HTTP requests returned the exact draft/page/doc bytes and loaded all three Assets references. '
        'JavaScript parsed and a DOM/Web Audio simulation checked the 5+2 rows, all 12 audio paths, exact six-hit start times and pitches, '
        'shared gain, single-play pitch, cache reuse, native-player exclusivity, stop during a pending decode, and HTTP failure feedback. '
        'A loopback-only HTTP server was started on port 8766 for review. Actual browser rendering/decoding and subjective listening were not tested.', '',
        'At playbackRate 0.45, durations expand by 2.22× and energy shifts downward; the 300 ms requirement applies to the exported WAVs. '
        'Use the rate selector to check overlap and hurt confusion at the low extreme, and harshness at 1.4. '
        'Rapid fire is deliberately fixed to the requested 0.9–1.1 range. '
        'Human acceptance: listen to all five at the same level, compare to current hit/parry/hurt, run several rapid-fire bursts, '
        'then check the selected sound in dense gameplay before integration. No game code or import settings were changed.', '']
    (HERE / 'README.md').write_text('\n'.join(lines), encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--analyze', action='store_true')
    args = parser.parse_args()
    if args.analyze:
        analyze()
    else:
        build()
