// NUMBER audio v2 DRAFT. Offline Node.js 18+, standard library only.
// Run: node docs/design/concept/audio_v2/gen_audio_v2.mjs
// All instruments are procedural approximations, not sampled instruments.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const OUT = path.dirname(fileURLToPath(import.meta.url));
const SR = 32000, TAU = Math.PI * 2, PEAK = 10 ** (-1 / 20);
let seed = 0x4e554d32;
const rnd = () => ((seed = (1664525 * seed + 1013904223) >>> 0) / 4294967296);
const noise = () => rnd() * 2 - 1;
const buffer = sec => new Float64Array(Math.round(sec * SR));
const hz = m => 440 * 2 ** ((m - 69) / 12);
const clamp = (x, lo, hi) => Math.max(lo, Math.min(hi, x));
const smooth = x => { x = clamp(x, 0, 1); return x * x * (3 - 2 * x); };

function unit(x) {
  let p = 0;
  for (const v of x) p = Math.max(p, Math.abs(v));
  if (p) for (let i = 0; i < x.length; i++) x[i] /= p;
  return x;
}
function fade(x, attack = .001, release = .02) {
  const a = Math.max(2, Math.round(attack * SR)), r = Math.max(2, Math.round(release * SR));
  for (let i = 0; i < Math.min(a, x.length); i++) x[i] *= smooth(i / (a - 1));
  for (let i = 0; i < Math.min(r, x.length); i++) x[x.length - 1 - i] *= smooth(i / (r - 1));
  return x;
}
function add(dst, src, at = 0, gain = 1, wrap = false) {
  const start = Math.round(at * SR), n = dst.length;
  for (let i = 0; i < src.length; i++) {
    let j = start + i;
    if (wrap) j = ((j % n) + n) % n;
    if (j >= 0 && j < n) dst[j] += src[i] * gain;
  }
  return dst;
}
function lp(x, cutoff, circular = false) {
  const a = 1 - Math.exp(-TAU * cutoff / SR);
  let z = 0;
  if (circular) for (const v of x) z += a * (v - z);
  for (let i = 0; i < x.length; i++) { z += a * (x[i] - z); x[i] = z; }
  return x;
}
function hp(x, cutoff, circular = false) {
  const low = lp(Float64Array.from(x), cutoff, circular);
  for (let i = 0; i < x.length; i++) x[i] -= low[i];
  return x;
}
function band(x, f, q = 1) {
  let x1 = 0, x2 = 0, y1 = 0, y2 = 0;
  for (let i = 0; i < x.length; i++) {
    const w = TAU * (typeof f === 'function' ? f(i / SR) : f) / SR;
    const al = Math.sin(w) / (2 * q), a0 = 1 + al;
    const y = al / a0 * (x[i] - x2) + 2 * Math.cos(w) / a0 * y1 - (1 - al) / a0 * y2;
    x2 = x1; x1 = x[i]; y2 = y1; y1 = y; x[i] = y;
  }
  return x;
}
function hiss(sec, decay, cutoff = 6000) {
  const x = buffer(sec);
  for (let i = 0; i < x.length; i++) x[i] = noise() * Math.exp(-i / SR / decay);
  return fade(lp(hp(x, 150), cutoff), .0006, .012);
}
function tone(sec, freq, decay, shape = 'sine') {
  const x = buffer(sec); let phase = 0;
  for (let i = 0; i < x.length; i++) {
    const t = i / SR, f = typeof freq === 'function' ? freq(t) : freq;
    phase += TAU * f / SR;
    let v = Math.sin(phase);
    if (shape === 'buzz') v = .75 * v + .19 * Math.sin(2 * phase) + .1 * Math.sin(3 * phase);
    x[i] = v * Math.exp(-t / decay);
  }
  return fade(x, .001, .02);
}
function crush(x, bits = 5, hold = 5) {
  let v = 0; const q = 2 ** (bits - 1);
  for (let i = 0; i < x.length; i++) { if (i % hold === 0) v = Math.round(x[i] * q) / q; x[i] = v; }
  return lp(x, 4800);
}
function room(x, wet = .12, wrap = false) {
  // Finite, dark, irregular reflections; wrap all music tails exactly.
  const out = Float64Array.from(x);
  for (const [sec, gain] of [[.037,.48],[.071,.32],[.113,.23],[.173,.14],[.239,.07]])
    add(out, lp(Float64Array.from(x), 3100), sec, wet * gain, wrap);
  return out;
}

function modes(sec, f, partials, swell = .001) {
  const x = buffer(sec);
  for (const [ratio, gain, decay] of partials) {
    const p = rnd() * TAU;
    for (let i = 0; i < x.length; i++) {
      const t = i / SR;
      const bend = .0018 * Math.sin(TAU * 5.1 * t) * Math.exp(-t * 4);
      x[i] += gain * Math.sin(TAU * f * ratio * t + bend * ratio + p) * Math.exp(-t / decay) * smooth(t / swell);
    }
  }
  return fade(x, .0007, Math.min(.1, sec * .18));
}
function buk(sec = .7, deep = false) {
  const x = tone(sec, t => (deep ? 49 : 70) + 75 * Math.exp(-t / .016), deep ? .23 : .135);
  add(x, modes(sec, deep ? 62 : 94, [[1,.25,.14],[1.59,.14,.085],[2.13,.1,.06]], .001), 0, .65);
  add(x, band(hiss(.055,.012), 690, .8), 0, .24);
  return unit(lp(x, 2600));
}
function janggu(kind = 'deok') {
  // Different resonances, attacks, and registers for stick, low head, both heads.
  const x = buffer(.34);
  if (kind !== 'deok') {
    add(x, tone(.34,t => 112 + 72 * Math.exp(-t / .012),.075),0,.9);
    add(x,modes(.28,173,[[1,.28,.055],[1.61,.18,.042],[2.23,.13,.032]]),0,.5);
  }
  if (kind !== 'kung') {
    add(x,modes(.18,385,[[1,1,.032],[1.52,.65,.024],[2.65,.22,.015]]),.001,.7);
    add(x,band(hiss(.08,.01),2200,1.1),.001,1.2);
  }
  return unit(lp(x, 6100));
}
function gong(sec, small = false, bloom = .01) {
  const parts = small
    ? [[1,.8,.27],[1.47,.6,.21],[2.12,.43,.15],[2.78,.25,.11],[3.89,.16,.055],[5.11,.08,.03]]
    : [[1,1,1.12],[1.41,.65,.93],[1.94,.48,.78],[2.72,.32,.6],[3.49,.15,.42],[4.87,.08,.22]];
  const f = small ? 1110 : 126;
  const x = modes(sec,f,parts,bloom);
  add(x,modes(sec,f * 1.006,parts.map(([r,g,d])=>[r,g * .34,d * .8]),bloom),0,1);
  add(x,band(hiss(.08,.018),small ? 3800 : 900,.7),0,small ? .2 : .09);
  return unit(lp(x,small ? 9500 : 4300));
}
function pluck(sec, m, dark = false, bend = .035) {
  // Fractional-delay Karplus-Strong, damped bridge reflection and bridge buzz.
  // The delay includes the averaging filter's half-sample group delay.
  const x = buffer(sec), base = SR / hz(m) - .5;
  const maxDelay = Math.ceil(base * 1.6 + 8), line = new Float64Array(maxDelay);
  const exc = buffer((base + 4) / SR);
  for (let i = 0; i < exc.length; i++) exc[i] = noise();
  lp(exc, dark ? 1900 : 5800); unit(exc);
  let write = 0, prev = 0;
  for (let i = 0; i < x.length; i++) {
    const t = i / SR;
    // Start above target; relax with slight left-hand oscillation / nonghyeon.
    const shift = bend * Math.exp(-t / .075) + .0038 * Math.sin(TAU * 5.2 * t) * smooth(t / .11);
    const delay = base / (1 + shift);
    const rd = (write - delay + maxDelay * 2) % maxDelay, j = Math.floor(rd), frac = rd - j;
    const s = line[j] * (1 - frac) + line[(j + 1) % maxDelay] * frac;
    const feedback = (s + prev) * .5 * (dark ? .9958 : .9972);
    prev = s;
    const v = feedback + (i < exc.length ? exc[i] * .65 : 0);
    line[write] = v; write = (write + 1) % maxDelay;
    const bridge = .035 * Math.tanh(9 * v) * Math.exp(-t / .1);
    x[i] = (v + bridge) * Math.exp(-t / (dark ? .45 : 1.3));
  }
  return unit(fade(hp(lp(x,dark ? 2900 : 6500),75),.0008,Math.min(.12,sec / 5)));
}
function reed(sec, m, urgent = false) {
  const x = buffer(sec), f0 = hz(m); let ph = 0;
  const weights = [];
  for (let h = 1; h * f0 < 7600; h++) {
    const f = f0 * h;
    const formant = .18 + 1.6 * Math.exp(-(((f-1050)/550)**2)) + .9 * Math.exp(-(((f-2350)/800)**2));
    const pulse = .6 + .4 * Math.sin(Math.PI * h * .32);
    weights.push([h,formant * pulse / h]);
  }
  for (let i = 0; i < x.length; i++) {
    const t = i / SR;
    const f = f0 * (1 - .027 * Math.exp(-t/.023) + .009 * Math.sin(TAU*5.7*t)*smooth(t/.14));
    ph += TAU * f / SR;
    let v = 0;
    for (const [h,g] of weights) v += Math.sin(ph*h) * g;
    const env = smooth(t/(urgent ? .009 : .027)) * smooth((sec-t)/.07) * (.9 + .1*Math.sin(TAU*2.6*t));
    x[i] = (v + noise() * .028) * env;
  }
  return unit(fade(lp(hp(x,200),urgent ? 4200 : 3300),.002,.02));
}
function bass(sec,m,heavy=false) {
  const x = buffer(sec); let ph = 0;
  for (let i=0;i<x.length;i++) {
    const t=i/SR; ph+=TAU*hz(m)/SR;
    const v=Math.sin(ph)+.3*Math.sin(2*ph)+.17*Math.sin(3*ph)+.08*Math.sin(4*ph);
    x[i]=Math.tanh(v*(heavy?2.6:1.2))*smooth(t/.008)*smooth((sec-t)/.035)*Math.exp(-t/(heavy?.45:.32));
  }
  return unit(lp(x,heavy?1100:680));
}
function pad(sec, notes) {
  const x=buffer(sec);
  for (const m of notes) for (const det of [-.045,.045]) {
    const f=hz(m+det), p=rnd()*TAU;
    for(let i=0;i<x.length;i++) {
      const t=i/SR;
      x[i]+=(Math.sin(TAU*f*t+p)+.18*Math.sin(TAU*2*f*t+p))*smooth(t/.35)*smooth((sec-t)/.6);
    }
  }
  return unit(lp(x,1500));
}
function digitalBell(sec,m) {
  const x=modes(sec,hz(m),[[1,1,.29],[2.005,.36,.19],[2.753,.14,.12],[4.016,.045,.08]],.003);
  add(x,modes(sec,hz(m+.065),[[1,.25,.24],[2.01,.08,.16]],.003));
  return unit(lp(x,6900));
}

function sfx() {
  const out={}; let x;
  x=buffer(.2);
  const sw=band(hiss(.2,.12),t=>1100+2400*Math.sin(Math.PI*t/.2),.8);
  for(let i=0;i<sw.length;i++) sw[i]*=Math.sin(Math.PI*i/(sw.length-1))**1.3;
  add(x,unit(sw),0,.85); add(x,pluck(.18,76,true,.12),.008,.13); out.sfx_swing=x;

  x=buffer(.28); add(x,janggu('deok'),0,1);
  add(x,modes(.26,610,[[1,.8,.045],[1.73,.6,.035],[2.61,.25,.022]]),.003,.6);
  add(x,buk(.25),0,.2); add(x,hiss(.04,.007),0,.3); out.sfx_hit=x;

  x=buffer(.7); add(x,gong(.7,true),0,1);
  add(x,tone(.095,t=>290+120*Math.exp(-t/.008),.027),0,.35);
  add(x,modes(.63,hz(93),[[1,.32,.25],[1.503,.12,.17]],.002),.009,.45);
  add(x,band(hiss(.025,.004),4800,.8),0,.25); out.sfx_parry=room(x,.16);

  x=buffer(.16); add(x,unit(band(hiss(.16,.06),t=>1600+2000*t/.16,.7)),0,.85);
  add(x,crush(tone(.12,t=>720*2**(t/.07),.038,'buzz'),6,3),.004,.22); out.sfx_dash=x;

  x=buffer(.3); add(x,buk(.3,true),0,1);
  add(x,crush(tone(.24,t=>260*Math.exp(-t*9)+65,.065,'buzz'),4,11),.012,.55);
  add(x,band(hiss(.07,.014),1700,.8),.008,.2); out.sfx_hurt=x;

  x=buffer(.2); add(x,tone(.11,t=>1560+520*smooth(t/.1),.065,'buzz'),.004,.7);
  add(x,tone(.055,2350,.024),.11,.45); out.sfx_enemy_tell=x;

  x=buffer(.5); add(x,reed(.205,81,true),0,.85); add(x,reed(.25,76,true),.225,1);
  add(x,buk(.4),0,.36); add(x,crush(tone(.11,330,.05,'buzz'),5,5),.23,.25); out.sfx_last_tell=x;

  x=buffer(.75); const fizz=hiss(.65,.24);
  for(let i=0;i<fizz.length;i++) fizz[i]*=.15+.85*(.5+.5*Math.sin(TAU*(31*i/SR+14*(i/SR)**2)));
  add(x,crush(band(fizz,t=>3400-2600*t/.65,.7),5,4),0,.75);
  add(x,pluck(.72,76,false,.5),.015,.8); out.sfx_stunned=x;

  x=buffer(1.9);
  [81,76,79,69].forEach((m,k)=>add(x,digitalBell(.95,m),k*.285,.64));
  out.sfx_broadcast=room(x,.13);

  x=buffer(1); add(x,gong(1,false,.04),0,.38);
  [69,72,74,76,79,81].forEach((m,k)=>add(x,pluck(.8-k*.055,m,false,.045),.028+k*.069,.62));
  out.sfx_hack_ok=room(x,.14);

  x=buffer(.12); add(x,modes(.1,950,[[1,1,.012],[1.87,.45,.008],[2.7,.15,.006]]),0,.8);
  add(x,band(hiss(.025,.003),1800,1),0,.5); add(x,pluck(.11,81,true),.003,.22); out.sfx_ui_select=x;

  x=buffer(1.8); add(x,buk(.9,true),0,1); add(x,gong(1.8,false,.018),.009,.65);
  add(x,tone(1.4,t=>32+70*Math.exp(-t/ .15),.39),.015,.72);
  add(x,lp(hiss(1.2,.28),220),0,.65); out.sfx_shockwave=room(x,.15);
  return out;
}

function music(boss=false) {
  // 24 complete 4-beat bars. Field triplet janggu: 12/8 / 4/4 hybrid.
  // Integer loop frames define all event positions; no accumulating beat error.
  const bpm=boss?144:120, seconds=boss?40:48;
  const x=buffer(seconds), beat=seconds/96, bar=beat*4;
  const stems=Object.fromEntries(['drums','bass','strings','reed','pad','metal','glitch'].map(k=>[k,buffer(seconds)]));
  const put=(lane,sound,b,off,gain)=>add(stems[lane],sound,(b*4+off)*beat,gain,true);
  const D=janggu('deong'), K=janggu('kung'), d=janggu('deok'), B=buk(.8,boss);
  const roots=[45,45,43,43,41,41,43,45,45,48,43,43,41,41,43,45,45,45,43,48,41,43,43,45];
  const motifs=[
    [[0,69],[.667,72],[1.667,74],[2.667,76],[3.333,74]],
    [[.333,72],[1.333,69],[2.333,67]],
    [[0,67],[1,69],[1.667,72],[3,74]],
    [[0,72],[1.333,69],[2.667,67]],
    [[0,65],[.667,67],[2,69],[3.333,72]],
    [[.333,74],[1.333,72],[2.333,69]],
    [[0,67],[1,69],[2.333,72],[3,67]],
    [[0,69],[1.667,72],[2.667,69]],
  ];
  const lead=[
    [[0,81,1.25],[1.667,79,.65],[2.667,76,.95]],
    [[.333,74,1.1],[2,76,1.2]],
    [[0,79,1.1],[1.667,76,.65],[2.667,74,.95]],
    [[0,72,1.35],[2,74,1.3]],
    [[0,77,.95],[1.333,76,.65],[2.333,72,1.2]],
    [[.333,74,1],[2,72,.7],[3,69,.65]],
    [[0,74,1],[1.333,76,.65],[2.333,79,.95]],
    [[0,76,1.1],[1.667,72,.65],[2.667,69,.95]],
  ];
  for(let b=0;b<24;b++) {
    const section=Math.floor(b/8), r=roots[b], fill=b%4===3, thin=!boss && b===15;
    // Controlled accents; no continuous hi-hat or cymbal wash over combat cues.
    for(const [step,kind,gain] of [[0,'D',.58],[2,'d',.24],[3,'K',.39],[5,'d',.29],[6,'D',.48],[8,'d',.22],[9,'K',.37],[11,'d',.3]]) {
      if(thin && step>6) continue;
      put('drums',kind==='D'?D:kind==='K'?K:d,b,step/3,gain*(boss?1.1:1));
    }
    if(fill) for(const step of boss?[9.5,10,10.5,11.5]:[10,11.5]) put('drums',d,b,step/3,.18);
    put('drums',B,b,0,boss?.8:.33);
    if(boss) { put('drums',B,b,2,.62); if(fill) put('drums',B,b,3.5,.48); }
    const bassEvents=boss?[[0,0],[.75,0],[1.5,0],[2,0],[2.75,7],[3.5,12]]:[[0,0],[1.5,7],[2.5,0],[3.333,12]];
    for(const [at,offset] of bassEvents) if(!thin || at<2) put('bass',bass(beat*(boss?.57:.58),r-12+offset,boss),b,at,boss?.40:.34);
    const motif=motifs[b%8];
    for(const [at,m] of motif) {
      if(!boss && section===1 && at>1) continue;
      put('strings',pluck(boss?.7:1.05,m+(boss && section===2?12:0),false,.028),b,at,boss?.145:.23);
      if(!boss && section!==1) put('strings',pluck(.7,m,true,.015),b,at+.75,.032);
    }
    // Geomungo answers an octave lower, left space around reed phrases.
    if(b%2===0) put('strings',pluck(1.2,r+12,true,.025),b,2.333,boss?.10:.17);
    if((!boss && section===1) || (boss && (b%8<6 || section===2))) {
      for(const [at,m,len] of lead[b%8]) put('reed',reed(len*beat,m,boss),b,at,boss?.20:.155);
    }
    if(b%2===0) {
      // Pentatonic open voicings; F bars use F-A-C instead of a major seventh.
      const chord=r===41?[53,57,60]:r===43?[55,60,62]:r===48?[60,64,67]:[57,60,64];
      put('pad',pad(bar*2+.5,chord),b,0,boss?.038:.067);
    }
    if(b%8===0) put('metal',gong(boss?3.2:2.8,false,.055),b,0,boss?.32:.115);
    if(boss) {
      // Broadcast fragments remain soft and low; warnings own the 1.5-3 kHz slot.
      if(b%2===1) for(const [at,m] of [[1.5,69],[1.75,64],[3.25,67]])
        put('glitch',crush(digitalBell(.16,m),5,8),b,at,.075);
      if(b%4===2) put('metal',gong(.25,true),b,3.5,.055);
    }
  }
  for(const [name,stem] of Object.entries(stems)) {
    const processed=['strings','reed','pad','metal'].includes(name)?room(stem,name==='reed'?.17:.12,true):stem;
    add(x,processed);
  }
  // Circular filter state, folded delays and event tails: no endpoint fades.
  hp(x,28,true); lp(x,boss?5600:4900,true);
  return {x,bpm,bars:24,meter:'4-beat bars; triplet janggu (12/8 / 4/4 hybrid)'};
}

function finish(x,isLoop=false) {
  if(!isLoop) { hp(x,28); fade(x,.0015,Math.min(.09,x.length/SR*.15)); }
  let sum=0,weight=0;
  const dcWindow=isLoop?null:fade(new Float64Array(x.length).fill(1),.0015,Math.min(.09,x.length/SR*.15));
  for(let i=0;i<x.length;i++) { sum+=x[i]; weight+=dcWindow?dcWindow[i]:1; }
  const dc=sum/weight;
  for(let i=0;i<x.length;i++) x[i]-=dc*(dcWindow?dcWindow[i]:1);
  unit(x);
  for(let i=0;i<x.length;i++) x[i]*=PEAK;
  return x;
}
function write(name,x) {
  const b=Buffer.alloc(44+x.length*2);
  b.write('RIFF'); b.writeUInt32LE(b.length-8,4); b.write('WAVEfmt ',8); b.writeUInt32LE(16,16);
  b.writeUInt16LE(1,20); b.writeUInt16LE(1,22); b.writeUInt32LE(SR,24); b.writeUInt32LE(SR*2,28);
  b.writeUInt16LE(2,32); b.writeUInt16LE(16,34); b.write('data',36); b.writeUInt32LE(x.length*2,40);
  for(let i=0;i<x.length;i++) b.writeInt16LE(Math.round(x[i]*32767),44+i*2);
  fs.writeFileSync(path.join(OUT,name+'.wav'),b);
  console.log(`${name}.wav ${(x.length/SR).toFixed(3)}s ${b.length} bytes`);
}
for(const [name,x] of Object.entries(sfx())) write(name,finish(x));
for(const boss of [false,true]) { const {x}=music(boss); write(boss?'bgm_boss':'bgm_field',finish(x,true)); }
