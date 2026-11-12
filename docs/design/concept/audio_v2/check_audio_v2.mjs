// Independent on-disk PCM audit; no rendering code imported.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../..');
const expected={sfx_swing:.2,sfx_hit:.28,sfx_parry:.7,sfx_dash:.16,sfx_hurt:.3,
  sfx_enemy_tell:.2,sfx_last_tell:.5,sfx_stunned:.75,sfx_broadcast:1.9,
  sfx_hack_ok:1,sfx_ui_select:.12,sfx_shockwave:1.8,bgm_field:48,bgm_boss:40};
function decode(file) {
  const b=fs.readFileSync(file);
  if(b.toString('ascii',0,4)!=='RIFF'||b.toString('ascii',8,12)!=='WAVE') throw Error('Not WAV: '+file);
  let fmt,data;
  for(let p=12;p+8<=b.length;) {
    const id=b.toString('ascii',p,p+4), len=b.readUInt32LE(p+4);
    if(id==='fmt ') fmt={format:b.readUInt16LE(p+8),channels:b.readUInt16LE(p+10),rate:b.readUInt32LE(p+12),bits:b.readUInt16LE(p+22)};
    if(id==='data') data=b.subarray(p+8,p+8+len);
    p+=8+len+(len&1);
  }
  if(!data||fmt?.format!==1||fmt.bits!==16||fmt.channels!==1) throw Error('Expected PCM16 mono: '+file);
  const pcm=new Int16Array(data.length/2);
  for(let i=0;i<pcm.length;i++) pcm[i]=data.readInt16LE(i*2);
  return {b,fmt,pcm};
}
function inspect(file) {
  const {b,fmt,pcm:x}=decode(file), n=x.length;
  let peak=0,sum=0,power=0,steps=0,maxStep=0,clipped=0,early=0,late=0;
  const histogram=new Uint32Array(65536);
  for(let i=0;i<n;i++) {
    const v=x[i]; peak=Math.max(peak,Math.abs(v)); sum+=v; power+=v*v;
    if(Math.abs(v)>=32767) clipped++;
    if(i) { const step=Math.abs(v-x[i-1]); histogram[Math.min(65535,step)]++; steps+=step*step; maxStep=Math.max(maxStep,step); }
    if(i<fmt.rate*.03) early+=v*v;
    if(i>=n-fmt.rate*.03) late+=v*v;
  }
  let count=0,p99Step=0;
  for(let i=0;i<histogram.length;i++) { count+=histogram[i]; if(count>=(n-1)*.99) {p99Step=i;break;} }
  const edge=Array.from(x.slice(-160)).concat(Array.from(x.slice(0,160)));
  let edgePower=0,edgeMax=0;
  for(let i=1;i<edge.length;i++) { const d=edge[i]-edge[i-1];edgePower+=d*d;edgeMax=Math.max(edgeMax,Math.abs(d)); }
  const seam=Math.abs(x[0]-x[n-1]), slopeMismatch=Math.abs((x[1]-x[0])-(x[n-1]-x[n-2]));
  return {file:path.basename(file),sha256:crypto.createHash('sha256').update(b).digest('hex'),
    ...fmt,frames:n,seconds:n/fmt.rate,bytes:b.length,
    peakDbfs:20*Math.log10(peak/32768),rmsDbfs:10*Math.log10(power/n/32768**2),
    dc:sum/n/32768,dcDbfs:20*Math.log10(Math.max(1e-20,Math.abs(sum/n/32768))),
    clippedSamples:clipped,firstSample:x[0],lastSample:x[n-1],
    first30msRmsDbfs:10*Math.log10(early/(fmt.rate*.03)/32768**2),
    last30msRmsDbfs:10*Math.log10(late/(fmt.rate*.03)/32768**2),
    maxAdjacentStep:maxStep/32768,p99AdjacentStep:p99Step/32768,
    adjacentStepRms:Math.sqrt(steps/(n-1))/32768,
    seamStep:seam/32768,seamStepPcm:seam,seamSlopeMismatch:slopeMismatch/32768,
    seamWindowStepRms:Math.sqrt(edgePower/(edge.length-1))/32768,
    seamWindowMaxStep:edgeMax/32768};
}
const report={schema:1,checks:'PCM files re-opened after quantization. Seam values use normalized full scale; 1 LSB = 1/32768.',
  listening:'Audio perception is unavailable to this agent. No subjective listening pass claimed; use review.html for human A/B and seam audition.',
  loopMethod:'24 whole bars, integer loop frames, wrapped event/delay tails, circular filters; no music endpoint fade or splice crossfade.',
  drafts:[],baseline:[],failures:[]};
for(const [name,seconds] of Object.entries(expected)) {
  const r=inspect(path.join(HERE,name+'.wav')), loop=name.startsWith('bgm_');
  const checks={format:r.format===1&&r.channels===1&&r.bits===16&&r.rate===32000,
    length:r.frames===Math.round(seconds*32000),peak:Math.abs(r.peakDbfs+1)<.002,
    dc:Math.abs(r.dc)<.000001,noClipping:r.clippedSamples===0};
  if(loop) {
    r.bpm=name==='bgm_field'?120:144;r.bars=24;r.beatsPerBar=4;
    r.loopStartFrame=0;r.loopEndFrame=r.frames;
    checks.wholeBars=Math.abs(r.seconds*r.bpm/60-r.bars*4)<1e-10;
    checks.size=r.bytes<3.1e6;
    // Neighboring samples need not be equal: a legitimate waveform has slope.
    // Reject a discontinuity larger than normal signal variation or -40 dBFS.
    checks.seam=r.seamStep<=.01&&r.seamStep<=r.p99AdjacentStep;
    checks.seamSlope=r.seamSlopeMismatch<=.015;
  } else checks.fades=r.firstSample===0&&r.lastSample===0;
  r.checks=checks;report.drafts.push(r);
  for(const [key,ok] of Object.entries(checks)) if(!ok)report.failures.push(`${name}: ${key}`);
  const baseline=path.join(ROOT,'Assets/Audio/Resources',loop?'Bgm':'Sfx',name+'.wav');
  if(fs.existsSync(baseline))report.baseline.push(inspect(baseline));
  console.log(`${r.file.padEnd(23)} ${r.seconds.toFixed(3).padStart(6)}s peak ${r.peakDbfs.toFixed(4)} dBFS DC ${r.dc.toExponential(2)}${loop?' seam '+r.seamStep.toFixed(6)+' slope '+r.seamSlopeMismatch.toFixed(6):''} ${Object.values(checks).every(Boolean)?'PASS':'FAIL'}`);
}
fs.writeFileSync(path.join(HERE,'validation.json'),JSON.stringify(report,null,2)+'\n');
if(report.failures.length) { console.error(report.failures);process.exitCode=1; }
