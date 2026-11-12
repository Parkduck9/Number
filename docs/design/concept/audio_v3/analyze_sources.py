"""Offline PCM inspection, not a listening verdict. NumPy + Python standard library."""
from pathlib import Path
import hashlib
import json
import site
import sys
import wave

sys.path.append(site.getusersitepackages())
import numpy as np

ROOT = Path(__file__).resolve().parent


def read_wav(path):
    with wave.open(str(path), 'rb') as w:
        sr, channels, width = w.getframerate(), w.getnchannels(), w.getsampwidth()
        if width != 2:
            raise ValueError(f'Expected PCM16 input: {path} ({width} bytes/sample)')
        x = np.frombuffer(w.readframes(w.getnframes()), '<i2').astype(np.float64) / 32768
    return x.reshape(-1, channels).mean(axis=1), sr, channels


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def metrics(x, sr):
    block = max(1, round(sr * .005))
    env = np.array([np.sqrt(np.mean(x[i:i+block] ** 2)) for i in range(0, len(x), block)])
    active = np.flatnonzero(env > max(env.max() * .025, 1e-5))
    energy = np.cumsum(x*x)
    spectrum = np.abs(np.fft.rfft(x * np.hanning(len(x)))) ** 2
    freq = np.fft.rfftfreq(len(x), 1/sr)
    total = max(float(spectrum.sum()), 1e-30)
    return dict(seconds=round(len(x)/sr, 6), peak_db=round(20*np.log10(max(np.max(np.abs(x)), 1e-12)), 2),
                rms_db=round(10*np.log10(max(np.mean(x*x), 1e-24)), 2),
                onset_s=round(int(active[0])*block/sr, 4) if len(active) else 0,
                end_s=round(min(len(x), (int(active[-1])+1)*block)/sr, 4) if len(active) else 0,
                peak_window_s=round(int(np.argmax(env))*block/sr, 4),
                energy_90_s=round(int(np.searchsorted(energy, .9*energy[-1]))/sr, 4),
                centroid_hz=round(float((freq*spectrum).sum()/total)),
                dominant_hz=round(float(freq[np.argmax(spectrum)])),
                low_pct=round(float(spectrum[freq<250].sum()/total*100), 1),
                high_pct=round(float(spectrum[freq>4000].sum()/total*100), 1),
                envelope_5ms_db=[round(float(20*np.log10(max(v, 1e-9))), 1) for v in env])


def main():
    entries = {}
    for p in sorted((ROOT/'_cc0_src').rglob('*.wav')):
        x, sr, channels = read_wav(p)
        entries[p.relative_to(ROOT/'_cc0_src').as_posix()] = dict(sample_rate=sr, channels=channels, sha256=sha(p), **metrics(x, sr))
    protected = [*sorted((ROOT.parent/'audio_v2').glob('*')),
                 *sorted((ROOT.parents[3]/'Assets/Audio/Resources').rglob('*.wav'))]
    report = dict(method='5 ms RMS envelope; -32 dB relative active threshold; Hann-windowed FFT; no subjective listening.',
                  cc0=entries, protected_sha256={str(p.relative_to(ROOT.parents[3])).replace('\\', '/'):sha(p) for p in protected if p.is_file()})
    (ROOT/'source_analysis.json').write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(f'Analyzed {len(entries)} CC0 WAVs; snapshotted {len(report["protected_sha256"])} protected files.')


if __name__ == '__main__':
    main()
