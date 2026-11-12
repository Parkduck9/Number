"""NUMBER SFX v3 DRAFTS. Run from any working directory. No Assets/v2 writes.

Dependencies: NumPy (sound I/O uses standard-library wave). No network, PRNG,
external executable, or paid service. Source intervals are seconds in the supplied
WAVs BEFORE varispeed. Gains are peak-relative AFTER EQ/fades, BEFORE master gain.
"""
from pathlib import Path
import html
import json
import math
import site
import sys
import wave

sys.path.append(site.getusersitepackages())
sys.dont_write_bytecode = True
import numpy as np
from analyze_sources import ROOT, read_wav, sha, metrics

SR = 32000
PEAK = 10 ** (-1/20)
REPO = ROOT.parents[3]
PACKS = {
    'sword': ('20 Sword Sound Effects (Attacks and Clashes)', 'StarNinjas', 'https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes'),
    'clash': ('20 Sword Sound Effects (Attacks and Clashes)', 'StarNinjas', 'https://opengameart.org/content/20-sword-sound-effects-attacks-and-clashes'),
    'impact-sounds': ('Impact Sounds', 'Kenney', 'https://kenney.nl/assets/impact-sounds'),
    'interface-sounds': ('Interface Sounds', 'Kenney', 'https://kenney.nl/assets/interface-sounds'),
    'rpg-audio': ('RPG Audio', 'Kenney Vleugels (Kenney)', 'https://kenney.nl/assets/rpg-audio'),
    'sci-fi-sounds': ('Sci-fi Sounds', 'Kenney', 'https://kenney.nl/assets/sci-fi-sounds'),
}


def layer(file, start, end, gain, hp, lp, semitones=0, at=0, attack=.001, release=.012, decay=0):
    return dict(file=file, source_start_s=start, source_end_s=end, gain=gain,
                highpass_hz=hp, lowpass_hz=lp, semitones=semitones,
                at_s=at, attack_s=attack, release_s=release, decay_s=decay)


# Physical sources retain their natural envelope. Only +/-1.5 semitone playback
# shifts; no extreme stretching, extra synthesized notes, or new musical layers.
RECIPES = {
    'sfx_swing': dict(seconds=.22, synth_gain=.34, synth_hp=350, synth_lp=2900,
        purpose='Sword air and a dark plucked scrape.',
        synth='Matching v2, low-passed to retain the dark pluck and supporting air.',
        why='Sword 4 has a single rise to a 0.200 s crest; trim its leading silence and retain the sweep, rather than a clash transient.',
        layers=[layer('sword/sword.4.wav', .055, .285, 1, 550, 10200, 1, attack=.003, release=.018)]),
    'sfx_hit': dict(seconds=.28, synth_gain=.78, synth_hp=70, synth_lp=7400,
        purpose='Body/robot hit with a janggu stick crack.',
        synth='Full matching v2 janggu deok/stick attack, robot clank and low buk accent.',
        why='Heavy punch 2 concentrates 90% energy within 80 ms and supplies low body weight; latch supplies a brief physical metal edge.',
        layers=[layer('impact-sounds/impactPunch_heavy_002.wav', 0, .26, .95, 45, 1600, -.5, release=.02),
                layer('rpg-audio/metalLatch.wav', .018, .105, .24, 1100, 7800, -1, at=.003, release=.018)]),
    'sfx_parry': dict(seconds=.76, synth_gain=1, synth_hp=90, synth_lp=9700,
        purpose='Steel block followed by a bright kkwaenggwari ring.',
        synth='Full v2 kkwaenggwari/bronze ringing and low punch; one quiet 60 ms dark reflection extends the tail.',
        reflection=(.06,.10,4100),
        why='Clash 4 is less shrill than alternatives (6.21 kHz centroid versus 7-10 kHz) with a short 40-249 ms active region.',
        layers=[layer('clash/sword_clash.4.wav', .036, .27, .86, 650, 8900, -.5, attack=.0007, release=.025)]),
    'sfx_dash': dict(seconds=.18, synth_gain=.53, synth_hp=350, synth_lp=6500,
        purpose='Fast cloth/air movement with the v2 digital zip.',
        synth='Matching v2 rising crushed zip and air rush.',
        why='Cloth 1 has broadband movement around its 205 ms crest; a short crop gives physical movement without adding a weapon strike.',
        layers=[layer('rpg-audio/cloth1.wav', .145, .32, .85, 240, 6700, 1, attack=.004, release=.018),
                layer('sword/sword.6.wav', .012, .16, .28, 900, 9200, 1, attack=.002, release=.018)]),
    'sfx_hurt': dict(seconds=.3, synth_gain=1, synth_hp=40, synth_lp=5800,
        purpose='Buk-backed bodily impact and falling robot distress.',
        synth='Full matching v2 heavy buk and descending crushed robot voice.',
        why='Heavy punch 3 is a compact low impact with a 25 ms crest and little high-frequency energy, leaving the robot voice clear.',
        layers=[layer('impact-sounds/impactPunch_heavy_003.wav', .005, .26, .85, 45, 1850, -1, release=.025),
                layer('rpg-audio/cloth1.wav', .19, .31, .13, 400, 5000, -.5, at=.008, attack=.002)]),
    'sfx_enemy_tell': dict(seconds=.2, synth_gain=1, synth_hp=100, synth_lp=10000,
        purpose='Readable rising enemy wind-up blip and terminal ping.',
        synth='Full matching v2 rising robot blip and 2350 Hz terminal ping, dominant over a soft mechanical lock.',
        why='Only the latch onset is used; low-pass and a low gain keep recorded steel out of the main warning band.',
        warning=True,
        layers=[layer('rpg-audio/metalLatch.wav', .015, .105, .075, 250, 1100, -1, attack=.0015, release=.018)]),
    'sfx_last_tell': dict(seconds=.5, synth_gain=1, synth_hp=65, synth_lp=9500,
        purpose='Two distinct urgent A5/E5 reed warnings.',
        synth='Full matching v2 two reed stabs and buk; background machine chatter ducks with those stabs.',
        why='Computer noise 2 sits near 900 Hz; low-pass at 520 Hz and low level place its texture below the A5/E5 warning harmonics.',
        warning=True, duck_texture=True,
        layers=[layer('sci-fi-sounds/computerNoise_002.wav', .9, 1.36, .075, 180, 520, -.5, at=.006, attack=.006, release=.03)]),
    'sfx_stunned': dict(seconds=.75, synth_gain=.95, synth_hp=80, synth_lp=7500,
        purpose='Robot overload under a falling gayageum-like bend.',
        synth='Full matching v2 pulsed fizz and falling bent string; physical latch at the onset.',
        why='Computer noise 3 offers a midrange machine texture instead of the near-ultrasonic sci-fi door samples; latch adds a real mechanism.',
        layers=[layer('sci-fi-sounds/computerNoise_003.wav', .9, 1.58, .42, 320, 2900, -1, at=.008, attack=.004, release=.055, decay=.34),
                layer('rpg-audio/metalLatch.wav', .015, .13, .26, 600, 6600, -1.5, release=.022)]),
    'sfx_broadcast': dict(seconds=1.9, synth_gain=1, synth_hp=75, synth_lp=10000,
        purpose='Readable four-note public-address announcement.',
        synth='Full matching v2 A5/E5/G5/A4 chime, unchanged timing/pitch; tiny relay click underneath the first note only.',
        why='Metal click has its main transient at 270 ms; isolate that 65 ms region and keep it quiet so no new warning rhythm is introduced.',
        warning=True,
        layers=[layer('rpg-audio/metalClick.wav', .245, .31, .08, 1400, 6500, 0, attack=.001, release=.014)]),
    'sfx_hack_ok': dict(seconds=1.08, synth_gain=1, synth_hp=80, synth_lp=10000,
        purpose='Dice result: jing bloom, ascending gayageum-like plucks and small metal rattle.',
        synth='Full matching v2 jing and six ascending plucks, plus one quiet 65 ms dark reflection.',
        reflection=(.065,.07,2800),
        why='Coins 2 provides a short tangible rattle at 180 ms, with most energy finished by 232 ms; it supports the roll/result without adding an unrelated pitched UI melody.',
        layers=[layer('rpg-audio/handleCoins2.wav', .02, .305, .3, 1700, 7500, -.5, at=.004, attack=.002, release=.025)]),
    'sfx_ui_select': dict(seconds=.12, synth_gain=.66, synth_hp=180, synth_lp=7200,
        purpose='Single compact physical UI click with a wooden clapper color.',
        synth='Matching v2 wooden bak/clapper tick and soft high pluck.',
        why='Click 2 is a single 10 ms event with an 866 Hz centroid; it avoids the 10.5 kHz brightness of click 4 and double-action switch files.',
        layers=[layer('interface-sounds/click_002.wav', 0, .010023, .9, 220, 5000, 0, attack=.00025, release=.002)]),
    'sfx_shockwave': dict(seconds=1.9, synth_gain=1, synth_hp=35, synth_lp=8200,
        purpose='Heavy buk impulse, jing expansion and a grounded blast tail.',
        synth='Full matching v2 heavy buk, jing and falling low sweep; one quiet 85 ms dark reflection.',
        reflection=(.085,.07,1900),
        why='Crunch 0 has an immediate crest and a 430 ms 90%-energy time, adding impact texture without the delayed attack or near-subsonic bias of other explosions.',
        layers=[layer('sci-fi-sounds/explosionCrunch_000.wav', 0, .75, .72, 55, 4200, -1.5, attack=.001, release=.045),
                layer('impact-sounds/impactSoft_heavy_003.wav', 0, .3, .32, 45, 1000, -.5, release=.03)]),
}


def fade(x, attack=.001, release=.012):
    x = x.copy()
    for n, reverse in [(min(len(x), max(2, round(attack*SR))), False),
                       (min(len(x), max(2, round(release*SR))), True)]:
        t = np.linspace(0, 1, n)
        ramp = t*t*(3-2*t)
        if reverse:
            x[-n:] *= ramp[::-1]
        else:
            x[:n] *= ramp
    return x


def biquad(x, cutoff, high=False):
    """Causal 2-pole Butterworth; no circular EQ or pre-ringing."""
    w = 2*np.pi*cutoff/SR
    c, s = math.cos(w), math.sin(w)
    alpha = s/math.sqrt(2)
    a0 = 1+alpha
    b0 = ((1+c) if high else (1-c))/2/a0
    b1 = (-(1+c) if high else (1-c))/a0
    b2 = b0
    a1, a2 = -2*c/a0, (1-alpha)/a0
    y = np.zeros_like(x)
    z1 = z2 = 0.
    for i, v in enumerate(x):
        out = b0*v+z1
        z1 = b1*v-a1*out+z2
        z2 = b2*v-a2*out
        y[i] = out
    return y


def eq(x, hp, lp):
    return biquad(biquad(x, hp, True), lp)


def varispeed(x, source_sr, semitones):
    """96-tap, Blackman-windowed sinc, with anti-alias cutoff when downsampling.

    Playback-rate shift couples pitch/time; +1 st shortens by 5.6%, -1.5 st
    lengthens by 9.1%. No invented claim of independent pitch-preserving stretch.
    """
    step = source_sr/SR * 2**(semitones/12)
    if abs(step-1) < 1e-12:
        return x.copy()
    count = round(len(x)/step)
    y = np.zeros(count)
    radius = 48
    cutoff = .94*min(1., 1/step)
    offsets = np.arange(-radius+1, radius+1)
    for start in range(0, count, 4096):
        pos = np.arange(start, min(start+4096, count))*step
        indices = np.floor(pos).astype(int)[:, None]+offsets[None, :]
        distance = pos[:, None]-indices
        window = .42+.5*np.cos(np.pi*distance/radius)+.08*np.cos(2*np.pi*distance/radius)
        window[np.abs(distance)>=radius] = 0
        weights = cutoff*np.sinc(cutoff*distance)*window
        weights /= weights.sum(axis=1)[:, None]
        samples = x[np.clip(indices, 0, len(x)-1)]
        samples[(indices<0)|(indices>=len(x))] = 0
        y[start:start+len(pos)] = (samples*weights).sum(axis=1)
    return y


def unit(x):
    return x/max(np.max(np.abs(x)), 1e-12)


def put(dst, src, at=0, gain=1):
    start = round(at*SR)
    count = min(len(src), len(dst)-start)
    dst[start:start+count] += src[:count]*gain


def prepare(item):
    path = ROOT/'_cc0_src'/item['file']
    x, sr, _ = read_wav(path)
    start, end = round(item['source_start_s']*sr), round(item['source_end_s']*sr)
    if start < 0 or end > len(x)+1 or end <= start:
        raise ValueError(f'Invalid source region: {item}')
    x = varispeed(x[start:end], sr, item['semitones'])
    x = eq(x, item['highpass_hz'], item['lowpass_hz'])
    if item['decay_s']:
        x *= np.exp(-np.arange(len(x))/SR/item['decay_s'])
    return unit(fade(x, item['attack_s'], item['release_s']))


def master(x):
    x = biquad(x, 20, True)
    # Remove residual DC under a smooth window without changing zero endpoints.
    x = fade(x, .0007, .018)
    window = np.sin(np.linspace(0, np.pi, len(x)))**2
    x -= window*(x.sum()/window.sum())
    return x * (PEAK/np.max(np.abs(x)))


def make_docs(report):
    lines = ['# NUMBER — SFX v3 hybrid DRAFTS', '',
        'Twelve mono 32,000 Hz / signed 16-bit PCM drafts, sample peaks approximately -1 dBFS. '
        'No BGM generated. Assets and audio_v2 are read-only inputs; nothing is imported into Unity.', '',
        '**Listening limitation:** these choices were made from length, 5 ms RMS envelopes and Hann-windowed FFT spectra. '
        'No subjective listening was performed or claimed. Open [review.html](review.html) for human A/B/C audition. '
        'CC0 files are the supplied converted WAVs; their filenames and hashes identify the exact local inputs. '
        'Physical knife/cloth/coin/latch recordings add tangible texture; Kenney sci-fi/interface files are designed sounds, '
        'and are not claimed to be acoustic recordings of real robots.', '',
        '## Source packs and license', '',
        'All external layers are CC0 1.0. Supplied Kenney `License.txt` files were read, and the official pack pages and '
        'StarNinjas listing were checked. No additional assets were downloaded. '
        'Raw `_cc0_src/` is git-ignored and must not be committed. Its source analysis is retained as numbers/hashes, not audio.', '']
    for key in ['sword','impact-sounds','interface-sounds','rpg-audio','sci-fi-sounds']:
        title, author, url = PACKS[key]
        lines.append(f'- [{title}]({url}) — {author}, [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).')
    lines += ['', '## Per-file recipe and exact attribution', '',
              'Every output uses `../audio_v2/<same filename>.wav` (AI-generated procedural synthesis: Codex / OpenAI GPT). '
              'The complete v2 is retained unless the filtering below says otherwise; no isolated traditional-instrument recording is implied. '
              'EQ uses causal two-pole Butterworth high/low-pass filters. Source intervals below are before varispeed; '
              'all shifts are coupled playback speed/pitch. Gains are peak-relative after per-layer EQ/fades and before final normalization. '
              'Layer and master fades use smooth ramps; final PCM endpoints are exactly zero.', '']
    for name, recipe in RECIPES.items():
        m = report['outputs'][name]
        lines += [f'### [{name}.wav]({name}.wav) — {recipe["seconds"]:.3f} s', '', recipe['purpose'], '',
                  f'- v2 layer (gain {recipe["synth_gain"]}): {recipe["synth"]} EQ {recipe["synth_hp"]}–{recipe["synth_lp"]} Hz.']
        for item in recipe['layers']:
            key, file = item['file'].split('/', 1)
            title, author, url = PACKS[key]
            lines.append(f'- CC0: [{title}]({url}) / `{item["file"]}` — **{author}**. '
                f'Source {item["source_start_s"]:.6f}–{item["source_end_s"]:.6f} s; '
                f'{item["semitones"]:+g} semitones; EQ {item["highpass_hz"]}–{item["lowpass_hz"]} Hz; '
                f'offset {item["at_s"]*1000:g} ms; gain {item["gain"]}; fades {item["attack_s"]*1000:g}/{item["release_s"]*1000:g} ms'
                +(f'; exponential decay {item["decay_s"]:g} s.' if item['decay_s'] else '.'))
        lines += [f'- Selection basis: {recipe["why"]}',
                  f'- Measured: {m["peak_db"]:.2f} dBFS peak; {m["rms_db"]:.2f} dBFS RMS; '
                  f'{m["cc0_to_synth_energy_db"]:+.2f} dB CC0/v2 energy before mastering.']
        if recipe.get('reflection'):
            delay, gain, lp = recipe['reflection']
            lines.append(f'- Tail reflection: v2 only, delay {delay*1000:g} ms, gain {gain}, low-pass {lp} Hz.')
        if recipe.get('warning'):
            lines.append(f'- Warning band check (700–4000 Hz): v2 exceeds CC0 by {m["warning_band_synth_over_cc0_db"]:.2f} dB. '
                         'Original v2 note times and pitch are retained; gameplay masking still needs human listening.')
        if recipe.get('duck_texture'):
            lines.append('- Machine texture is attenuated by up to 65% following the 5 ms v2 RMS envelope, with a 6 ms smoother.')
        lines.append('')
    lines += ['## Build and verification', '',
        'Requires Python 3.10+ and NumPy. Audio I/O uses `wave` from the standard library; no SciPy or soundfile is required. '
        'The script appends the user site-packages path as requested. On this machine the PATH Python launcher was broken; '
        'the installed Blender Python 3.13 with its bundled NumPy was used without opening Blender.', '',
        '```powershell', 'python docs/design/concept/audio_v3/analyze_sources.py',
        'python docs/design/concept/audio_v3/build_audio_v3.py', '```', '',
        'Machine-specific equivalent: `& "C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" '
        'docs/design/concept/audio_v3/build_audio_v3.py`.', '',
        'Keep the supplied `_cc0_src/` tree in place for rebuilds. The builder validates input hashes against '
        '[source_analysis.json](source_analysis.json), rejects changed protected inputs, reopens all written WAVs, '
        'and verifies headers, frame counts, peak, clipping, DC, zero endpoints and edge slopes. '
        '[validation.json](validation.json) stores per-layer processing, source hashes, output hashes, spectral/envelope '
        'measurements, warning-band ratios and the untouched v2/current-game input hashes. '
        'The build fails on violated checks. It does not write source files.', '',
        'Delivery QA (2026-11-12): an independent standard-library WAV reader verified all 12 PCM headers, '
        'frame counts, peaks and zero endpoints. A second render produced 12/12 byte-identical WAVs. '
        'All 36 review audio paths resolved; JavaScript parsed, and a DOM simulation verified initial gain, '
        'slider updates, exclusive playback and Stop all. Browser decoding, layout and subjective listening were not tested.', '',
        'Peak normalization is not loudness matching: compare at a comfortable slider setting, then assess relative game gains. '
        'The three warnings deliberately favor v2; numeric band dominance is not a guarantee of perceptual audibility.', '',
        '## Human review', '',
        '- Open review.html locally. Each row has v3, v2 and the current game file. The global SFX volume slider affects every player. '
        'Starting another player stops the previous player for clean comparison; missing paths show an error.',
        '- Compare rapid swing/hit/UI repetition for harshness; parry must read as steel plus bronze ring, distinct from hit.',
        '- Check dash movement, hurt weight and stunned servo texture without masking the Korean-inspired layers.',
        '- In a later authorized game integration, check enemy/last/broadcast warnings during dense combat and BGM. '
        'Confirm jing/plucks still lead the dice result and buk still anchors shockwave; verify WebGL compression/polyphony.', '',
        '## Draft credits / AI disclosure', '',
        'See [CREDITS.md](CREDITS.md) for copy-ready attribution and AI disclosure. Root credits were not modified because '
        'this task permits writes only in audio_v3. Copy these credits into the project/submission when adopting.', '',
        'Git pull was attempted but `.git/FETCH_HEAD` writes are denied by the workspace permissions. '
        'Staging was also attempted, but `.git/index.lock` could not be created; no commit was possible. '
        'No existing unrelated changes were edited.']
    (ROOT/'README.md').write_text('\n'.join(lines)+'\n', encoding='utf-8')
    credits = ['# Audio v3 draft credits', '', 'External audio layers: CC0 1.0; edited/cropped/EQ/varispeed and mixed into drafts.', '']
    for key in ['sword','impact-sounds','interface-sounds','rpg-audio','sci-fi-sounds']:
        title, author, url = PACKS[key]
        files = sorted({i['file'] for r in RECIPES.values() for i in r['layers'] if i['file'].split('/')[0] == key or (key=='sword' and i['file'].startswith('clash/'))})
        credits += [f'- [{title}]({url}) — {author}, CC0 1.0: '+', '.join(f'`{f}`' for f in files)+'.']
    credits += ['', 'StarNinjas creator profile: https://opengameart.org/users/starninjas', '',
        'AI 생성 (Codex / OpenAI GPT): v2 Korean-inspired procedural synthesis retained and offline DSP/build/analysis/review code authored '
        'to select and layer supplied CC0 samples. No generative audio model, paid API, BGM generation, or new service was used. '
        'Kkwaenggwari, jing, janggu, buk and gayageum names describe synthetic approximations, not authentic instrument recordings.', '',
        'Exact per-output attribution is in README.md; hashes and intervals are in validation.json. '
        'Raw CC0 inputs remain ignored. Copy this disclosure and the source list into root CREDITS.md and jam submission if adopted.']
    (ROOT/'CREDITS.md').write_text('\n'.join(credits)+'\n', encoding='utf-8')


def make_review():
    rows = []
    for name, r in RECIPES.items():
        cells = []
        for label, path in [('v3 draft', name+'.wav'), ('v2', '../audio_v2/'+name+'.wav'),
                            ('current game', '../../../../Assets/Audio/Resources/Sfx/'+name+'.wav')]:
            cells.append(f'<td><audio controls preload="none" aria-label="{name} {label}" src="{path}"></audio>'
                         f'<a href="{path}">WAV</a><span class="error" role="status"></span></td>')
        rows.append(f'<tr><th scope="row"><code>{name}</code><small>{html.escape(r["purpose"])}</small></th>'+''.join(cells)+'</tr>')
    page = '''<!doctype html>
<html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>NUMBER · SFX v3 draft review</title>
<style>
:root{color-scheme:dark;font-family:system-ui,sans-serif;background:#151719;color:#edece8}
body{max-width:1360px;margin:auto;padding:24px}h1{font-size:26px;margin:0 0 10px}
p{max-width:940px;color:#bfc4c8;line-height:1.5}a{color:#a5d9df}label{display:inline-flex;gap:12px;align-items:center}
.controls{position:sticky;top:0;background:#151719;padding:15px 0;border-block:1px solid #43494d;z-index:1}
button{font:inherit;margin-left:18px;padding:6px 14px;background:#323b40;color:white;border:1px solid #69747b;border-radius:4px}
.table-scroll{overflow:auto}table{width:100%;border-collapse:collapse;margin-top:16px}th,td{padding:14px 10px;text-align:left;border-bottom:1px solid #3d4347;vertical-align:top}
thead th{color:#a5d9df}tbody th{min-width:190px;font-weight:500}small{display:block;margin-top:7px;font-size:12px;line-height:1.4;color:#aeb6bc;max-width:240px}
audio{display:block;width:100%;min-width:235px;max-width:340px;height:36px;margin-bottom:6px}td a{font-size:12px}.error{display:block;color:#ffb7a4;font-size:12px}
input{width:210px;accent-color:#a5d9df}output{min-width:3ch}
</style>
<h1>NUMBER · SFX v3 drafts</h1>
<p>CC0 texture + v2 Korean-inspired synthesis. Signal analysis completed; human listening pending.
Compare each row at a comfortable level. Starting playback stops the previous player. No BGM changes or Unity imports.</p>
<p><a href="README.md">Recipes and source credits</a> · <a href="validation.json">Signal validation</a>.
The current-game column reads the live Assets path and may match v2 until adoption.</p>
<div class="controls"><label for="volume">SFX volume <input id="volume" type="range" min="0" max="1" step="0.01" value="0.65"><output id="level" for="volume">65%</output></label><button id="stop" type="button">Stop all</button></div>
<div class="table-scroll"><table><thead><tr><th scope="col">Sound / purpose</th><th scope="col">v3 draft</th><th scope="col">v2</th><th scope="col">Current game</th></tr></thead><tbody>
'''+ '\n'.join(rows)+'''
</tbody></table></div>
<script>
'use strict';
const players=Array.from(document.querySelectorAll('audio'));
const volume=document.getElementById('volume'),level=document.getElementById('level');
function updateVolume(){const gain=Number(volume.value);for(const p of players)p.volume=gain;level.value=Math.round(gain*100)+'%';}
volume.addEventListener('input',updateVolume);updateVolume();
function stopAll(except){for(const p of players)if(p!==except){p.pause();if(p.readyState>0)p.currentTime=0;}}
document.getElementById('stop').addEventListener('click',()=>stopAll(null));
for(const p of players){p.addEventListener('play',()=>stopAll(p));p.addEventListener('ended',()=>{p.currentTime=0;});
p.addEventListener('error',()=>{p.parentElement.querySelector('.error').textContent='Audio unavailable. Check the local WAV path.';});}
</script></html>
'''
    (ROOT/'review.html').write_text(page, encoding='utf-8')


def main():
    analysis = json.loads((ROOT/'source_analysis.json').read_text(encoding='utf-8'))
    for rel, digest in analysis['protected_sha256'].items():
        if sha(REPO/rel) != digest:
            raise ValueError(f'Protected input changed since analysis: {rel}')
    report = dict(format='mono 32000 Hz PCM_16', peak_target_dbfs=-1,
        listening='Signal analysis only. Human audition pending.', outputs={},
        protected_inputs_sha256=analysis['protected_sha256'], recipes=RECIPES)
    for name, recipe in RECIPES.items():
        n = round(recipe['seconds']*SR)
        synth = np.zeros(n)
        x, sr, _ = read_wav(ROOT.parent/'audio_v2'/(name+'.wav'))
        assert sr == SR
        x = eq(x, recipe['synth_hp'], recipe['synth_lp'])
        put(synth, x, gain=recipe['synth_gain'])
        if recipe.get('reflection'):
            delay, gain, lp = recipe['reflection']
            put(synth, biquad(x, lp), delay, gain*recipe['synth_gain'])
        texture = np.zeros(n)
        sources = []
        for item in recipe['layers']:
            p = ROOT/'_cc0_src'/item['file']
            digest = sha(p)
            assert digest == analysis['cc0'][item['file']]['sha256'], f'Source changed: {p}'
            part = prepare(item)
            put(texture, part, item['at_s'], item['gain'])
            key = item['file'].split('/')[0]
            title, author, url = PACKS[key]
            sources.append(dict(**item, pack=title, author=author, license='CC0-1.0', url=url, sha256=digest,
                                rendered_layer_seconds=round(len(part)/SR,6)))
        if recipe.get('duck_texture'):
            # Follow a block RMS envelope; convolution smooths its boundaries.
            size = 160
            env = np.repeat([np.sqrt(np.mean(synth[i:i+size]**2)) for i in range(0,n,size)],size)[:n]
            env = np.convolve(env, np.ones(193)/193, mode='same')
            texture *= 1-.65*env/max(env.max(),1e-12)
        pre_master = synth+texture
        x = master(pre_master)
        pcm = np.rint(x*32768).astype('<i2')
        p = ROOT/(name+'.wav')
        with wave.open(str(p), 'wb') as w:
            w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
        y, sr, channels = read_wav(p)
        m = metrics(y, sr)
        m.pop('envelope_5ms_db')
        m.update(frames=len(y), sample_rate=sr, channels=channels, subtype='PCM_16', sha256=sha(p),
                 dc_mean=float(y.mean()), first_sample=int(pcm[0]), last_sample=int(pcm[-1]),
                 first_step_lsb=int(abs(int(pcm[1])-int(pcm[0]))),
                 last_step_lsb=int(abs(int(pcm[-1])-int(pcm[-2]))),
                 clipped_samples=int(np.count_nonzero(np.abs(y)>=.9999)),
                 cc0_to_synth_energy_db=round(float(10*np.log10(np.sum(texture**2)/np.sum(synth**2))),2),
                 cc0_sources=sources)
        if recipe.get('warning'):
            freq = np.fft.rfftfreq(n, 1/SR)
            band = (freq>=700)&(freq<=4000)
            energies = [np.sum(np.abs(np.fft.rfft(v))[band]**2) for v in (synth,texture)]
            m['warning_band_synth_over_cc0_db'] = round(float(10*np.log10(energies[0]/energies[1])),2)
            assert m['warning_band_synth_over_cc0_db'] > 18
        assert sr == SR and channels == 1 and len(y) == n
        assert abs(m['peak_db']+1) < .01 and m['clipped_samples'] == 0
        assert abs(m['dc_mean']) < 1e-5 and pcm[0] == pcm[-1] == 0
        assert m['first_step_lsb'] <= 3 and m['last_step_lsb'] <= 3
        report['outputs'][name] = m
        print(f'{name}.wav: {len(y)/SR:.3f}s, peak {m["peak_db"]:.2f} dBFS, RMS {m["rms_db"]:.2f} dBFS')
    for rel, digest in analysis['protected_sha256'].items():
        assert sha(REPO/rel) == digest, f'Protected input changed: {rel}'
    report['all_checks_passed'] = True
    (ROOT/'validation.json').write_text(json.dumps(report, indent=2)+'\n',encoding='utf-8')
    make_docs(report)
    make_review()
    print('12/12 passed; protected v2 and game audio hashes unchanged.')


if __name__ == '__main__':
    main()
