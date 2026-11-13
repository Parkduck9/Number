// DRAFT ONLY. Source-cell voting and explicit 50x50 pixel cleanup.
// Run from any directory: node docs/design/concept/portraits/benefactor/draw_helper_v4.cjs
// Uses Node's standard library only. Does not write to Assets/ or CREDITS.md.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const OUT = __dirname;
const ROOT = path.resolve(OUT, '../../../../..');
const color = c => Array.isArray(c) ? c : [...Buffer.from(c.slice(1), 'hex'), 255];
class Canvas {
  constructor(w, h, bg) {
    this.w = w; this.h = h; this.data = Buffer.alloc(w * h * 4);
    if (bg) for (let y=0;y<h;y++) for (let x=0;x<w;x++) this.pixel(x,y,bg);
  }
  get(x,y) { return [...this.data.subarray((y*this.w+x)*4,(y*this.w+x)*4+4)]; }
  pixel(x,y,c) {
    assert(x>=0 && y>=0 && x<this.w && y<this.h, `Out of bounds: ${x},${y}`);
    this.data.set(color(c),(y*this.w+x)*4);
  }
  blit(a,dx,dy) {
    for (let y=0;y<a.h;y++) for (let x=0;x<a.w;x++) if(a.get(x,y)[3]) this.pixel(dx+x,dy+y,a.get(x,y));
  }
  scale(n) {
    const a=new Canvas(this.w*n,this.h*n);
    for(let y=0;y<a.h;y++) for(let x=0;x<a.w;x++) a.pixel(x,y,this.get(Math.floor(x/n),Math.floor(y/n)));
    return a;
  }
}
const crcTable=Array.from({length:256},(_,n)=>{
  for(let i=0;i<8;i++) n=n&1 ? 0xedb88320^(n>>>1) : n>>>1;
  return n>>>0;
});
function chunk(type,data) {
  const b=Buffer.alloc(data.length+12); b.writeUInt32BE(data.length); b.write(type,4); data.copy(b,8);
  let c=0xffffffff; for(const v of b.subarray(4,-4)) c=crcTable[(c^v)&255]^(c>>>8);
  b.writeUInt32BE((c^0xffffffff)>>>0,b.length-4); return b;
}
function encode(a) {
  const hdr=Buffer.alloc(13); hdr.writeUInt32BE(a.w); hdr.writeUInt32BE(a.h,4); hdr[8]=8; hdr[9]=6;
  const raw=Buffer.alloc((a.w*4+1)*a.h);
  for(let y=0;y<a.h;y++) a.data.copy(raw,y*(a.w*4+1)+1,y*a.w*4,(y+1)*a.w*4);
  return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',hdr),chunk('IDAT',zlib.deflateSync(raw,{level:9})),chunk('IEND',Buffer.alloc(0))]);
}
function decode(file) {
  const b=fs.readFileSync(file); assert.equal(b[24],8); assert.equal(b[25],6); assert.equal(b[28],0);
  const a=new Canvas(b.readUInt32BE(16),b.readUInt32BE(20)),ids=[];
  for(let p=8;p<b.length;) {
    const n=b.readUInt32BE(p); if(b.toString('ascii',p+4,p+8)==='IDAT') ids.push(b.subarray(p+8,p+8+n)); p+=n+12;
  }
  const raw=zlib.inflateSync(Buffer.concat(ids)),stride=a.w*4;
  for(let y=0;y<a.h;y++) for(let x=0;x<stride;x++) {
    const f=raw[y*(stride+1)],i=y*stride+x,l=x>=4?a.data[i-4]:0,u=y?a.data[i-stride]:0,ul=y&&x>=4?a.data[i-stride-4]:0;
    assert(f<=4); let v=0;
    if(f===1) v=l; else if(f===2) v=u; else if(f===3) v=Math.floor((l+u)/2);
    else if(f===4) {const p=l+u-ul,dl=Math.abs(p-l),du=Math.abs(p-u),dul=Math.abs(p-ul);v=dl<=du&&dl<=dul?l:du<=dul?u:ul;}
    a.data[i]=(raw[y*(stride+1)+1+x]+v)&255;
  }
  return a;
}
function modeCell(a,x0,y0,x1,y1) {
  // Vote in 5-bit RGB bins to absorb Gemini's sub-pixel colour noise.
  // The winning bin's most frequent ORIGINAL RGBA supplies the cell colour.
  const bins=new Map();
  for(let y=y0;y<y1;y++) for(let x=x0;x<x1;x++) {
    const c=a.get(x,y),k=c[3]?`${c[0]>>3},${c[1]>>3},${c[2]>>3}`:'transparent';
    if(!bins.has(k)) bins.set(k,{n:0,exact:new Map()});
    const b=bins.get(k),s=c.join(','); b.n++; b.exact.set(s,(b.exact.get(s)||0)+1);
  }
  const win=[...bins.values()].sort((a,b)=>b.n-a.n)[0];
  return [...win.exact].sort((a,b)=>b[1]-a[1])[0][0].split(',').map(Number);
}
function nativeReference(a) {
  const n=new Canvas(50,50); assert.equal(a.w,100); assert.equal(a.h,100);
  for(let y=0;y<50;y++) for(let x=0;x<50;x++) n.pixel(x,y,modeCell(a,x*2,y*2,x*2+2,y*2+2));
  return n;
}
function bounds(a) {
  let left=a.w,top=a.h,right=-1,bottom=-1;
  for(let y=0;y<a.h;y++) for(let x=0;x<a.w;x++) if(a.get(x,y)[3]) {
    left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x);bottom=Math.max(bottom,y);
  }
  return [left,top,right,bottom];
}
function palette(a) {
  const counts=new Map();
  for(let i=0;i<a.data.length;i+=4) if(a.data[i+3]) {
    const k='#'+a.data.subarray(i,i+3).toString('hex');counts.set(k,(counts.get(k)||0)+1);
  }
  return [...counts].sort((a,b)=>b[1]-a[1]);
}
function detectGrid(a,axis) {
  const length=axis==='x'?a.w:a.h,energy=new Float64Array(length);
  for(let y=1;y<a.h;y++) for(let x=1;x<a.w;x++) {
    const i=(y*a.w+x)*4,j=i-(axis==='x'?4:a.w*4);
    energy[axis==='x'?x:y]+=Math.max(...[0,1,2].map(k=>Math.abs(a.data[i+k]-a.data[j+k])));
  }
  const total=energy.reduce((a,b)=>a+b,0),fits=[];
  // Test both the ~20px grid and its finer subdivisions. Edge concentration
  // must justify the grid; a nominal 50x50 resize is not assumed.
  for(let cells=Math.ceil(length/24);cells<=Math.floor(length/8);cells++) {
    const step=length/cells;
    let best={score:-Infinity};
    for(let phase=0;phase<step;phase+=0.2) {
      let support=0;
      for(let i=1;i<length;i++) if(Math.abs((i-phase)/step-Math.round((i-phase)/step))*step<1.1) support+=energy[i];
      const fraction=support/total,score=fraction-2.2/step;
      if(score>best.score) best={cells,step,phase:phase>step/2?phase-step:phase,fraction,score};
    }
    fits.push(best);
  }
  fits.sort((a,b)=>b.score-a.score);
  const fit=fits[0]; assert(Math.abs(fit.phase)<0.5, 'Source grid origin has moved; re-measure it.');
  return {cells:fit.cells,step:fit.step,origin:0,edgeFraction:+fit.fraction.toFixed(4),
    coarseHarmonic:fits.find(f=>f.cells===fit.cells/2)};
}
function sourceCells(source,gx,gy) {
  const a=new Canvas(gx.cells,gy.cells);
  for(let y=0;y<a.h;y++) for(let x=0;x<a.w;x++) a.pixel(x,y,modeCell(source,
    Math.round(x*gx.step),Math.round(y*gy.step),Math.round((x+1)*gx.step),Math.round((y+1)*gy.step)));
  return a;
}
function components(a) {
  const seen=new Set(),groups=[];
  for(let y=0;y<a.h;y++) for(let x=0;x<a.w;x++) {
    const start=y*a.w+x;if(seen.has(start)||!a.get(x,y)[3])continue;
    const group=[],queue=[start];seen.add(start);
    for(let i=0;i<queue.length;i++) {
      const id=queue[i],x=id%a.w,y=Math.floor(id/a.w);group.push(id);
      for(const [dx,dy] of [[-1,0],[1,0],[0,-1],[0,1]]) {
        const xx=x+dx,yy=y+dy,ii=yy*a.w+xx;
        if(xx<0||yy<0||xx>=a.w||yy>=a.h||seen.has(ii)||!a.get(xx,yy)[3])continue;
        seen.add(ii);queue.push(ii);
      }
    }
    groups.push(group);
  }
  return groups.sort((a,b)=>b.length-a.length);
}
function cutout(a) {
  // Measured navy background: RGB approximately (11,17,39). Hair and ink
  // have much lower blue saturation. Flood only matching exterior navy.
  const navy=c=>c[0]<20&&c[1]<24&&c[2]>=28&&c[2]-c[0]>=12;
  const queue=[],seen=new Set();
  function add(x,y) {
    const id=y*a.w+x;
    if(x<0||y<0||x>=a.w||y>=a.h||seen.has(id)||!navy(a.get(x,y)))return;
    seen.add(id);queue.push(id);
  }
  for(let x=0;x<a.w;x++){add(x,0);add(x,a.h-1);}
  for(let y=0;y<a.h;y++){add(0,y);add(a.w-1,y);}
  // Enclosed gap between ponytail, headset and collar: same measured navy
  // as the exterior, but the surrounding ink prevents an exterior flood.
  add(36,62);
  for(let i=0;i<queue.length;i++) {
    const x=queue[i]%a.w,y=Math.floor(queue[i]/a.w);
    add(x-1,y);add(x+1,y);add(x,y-1);add(x,y+1);
  }
  for(const id of seen)a.pixel(id%a.w,Math.floor(id/a.w),[0,0,0,0]);
  const groups=components(a),keep=new Set(groups[0]);
  // Watermark around (905,905) is disconnected from the character.
  for(const g of groups.slice(1)) for(const id of g)a.pixel(id%a.w,Math.floor(id/a.w),[0,0,0,0]);
  assert(!a.get(88,88)[3]&&!a.get(89,88)[3], 'Watermark survived extraction.');
  return {removedNavyCells:seen.size,keptCells:keep.size,removedComponents:groups.slice(1).map(g=>g.length),bounds:bounds(a)};
}
const P={
  ink:'#000000',deep:'#131424',hairDark:'#1a1c2d',hair:'#252433',
  hairMid:'#3b384e',hairHi:'#555369',coat:'#2d2d44',coatHi:'#333b5f',
  coatDark:'#1d2542',skin:'#f9cbad',skinMid:'#d69a83',skinShade:'#c68e7e',
  skinDark:'#b77569',lip:'#87545f',grey:'#848095',clothShade:'#b6acbd',
  cloth:'#ddd9da',white:'#ffffff',goldDark:'#79512c',gold:'#b8862e',
  goldHi:'#e0b75e',teal:'#64d1d7',cyan:'#7ef9f6',
};
function fitFigure(cells) {
  assert.equal(cells.w,100);assert.equal(cells.h,100);
  const a=new Canvas(50,50),extended=new Canvas(100,108);
  extended.blit(cells,0,0);
  // Source torso is already cropped. Continue its final coat row by four
  // art pixels to put the bust on the lower edge after the head alignment.
  for(let y=100;y<108;y++)for(let x=0;x<100;x++)extended.pixel(x,y,cells.get(x,99));
  const colors=Object.values(P).map(color);
  for(let y=0;y<50;y++)for(let x=0;x<49;x++) {
    const c=modeCell(extended,(x+1)*2,(y+4)*2,(x+2)*2,(y+5)*2);
    if(!c[3])continue;
    const nearest=colors.map(p=>({p,d:p.slice(0,3).reduce((s,v,i)=>s+(v-c[i])**2,0)})).sort((a,b)=>a.d-b.d)[0].p;
    a.pixel(x,y,nearest);
  }
  return a;
}
function cleanAt50(a) {
  const put=(x,y,key)=>a.pixel(x,y,P[key]);
  // Keep the source hair masses, but consolidate the very dark main crown
  // into the reference hair tone. Ink remains the separate outline colour.
  for(let y=7;y<=25;y++)for(let x=19;x<=36;x++) {
    const hex='#'+Buffer.from(a.get(x,y).slice(0,3)).toString('hex');
    if(a.get(x,y)[3]&&hex===P.hairDark)put(x,y,'hair');
  }
  // Two dark, half-lidded eyes at Yudo's eye-line. Each retains one light
  // art pixel; upper lids are y=21 and irises/highlights are y=22.
  for(const x of [24,25,26,27,28,33,34])put(x,20,'skin');
  for(const x of [24,25,26,27,28,33,34,35])put(x,21,'ink');
  put(25,22,'hairDark');put(26,22,'cloth');put(27,22,'ink');put(28,22,'skinMid');
  put(33,22,'hairDark');put(34,22,'cloth');put(35,22,'ink');
  // Restore the cheek where the eye's dark source cells won the vote.
  // The nose is the existing single shadow pixel at (32,24).
  put(28,27,'skin');put(32,27,'skin');put(31,28,'lip');
  // Rim is one art pixel across the outside contour. Replace the isolated
  // source rim on the cheek, then join the crown, side hair and coat edges.
  put(34,26,'skin');
  for(let x=27;x<=31;x++)put(x,7,'cyan');
  put(32,8,'cyan');put(33,8,'cyan');
  const headRight=[34,35,36,36,37,37,37,38,37,37,37,36,36,36,36,36,36,36,36,36,35,35,35,34];
  headRight.forEach((x,i)=>put(x,i+9,'cyan'));
  // Coat edge follows its own contour below the collar.
  const coatRight=[34,35,36,37,37,37,37,38,38,39,39,39,39,39];
  coatRight.forEach((x,i)=>put(x,i+36,'cyan'));
  // Eliminate the old disconnected highlight at the collar/shoulder join.
  put(34,36,'cyan');put(33,36,'coatDark');
  // Continue the cropped coat to the same 34px bust width as Yudo.
  for(let y=47;y<50;y++){put(6,y,'ink');put(7,y,'coat');}
  // Gold ornament: a small bright tip with a darker base, all inside frame.
  put(21,5,'goldHi');put(20,6,'gold');put(19,7,'goldDark');
  return a;
}
function measure(a,chin,skinColors,eyes) {
  const head=new Canvas(50,50),face=new Canvas(50,50),bust=new Canvas(50,50);
  for(let y=0;y<50;y++)for(let x=0;x<50;x++) {
    const c=a.get(x,y),hex='#'+Buffer.from(c.slice(0,3)).toString('hex');
    if(y<=chin)head.pixel(x,y,c);
    if(y<chin&&c[3]&&skinColors.includes(hex))face.pixel(x,y,c);
    if(y>=33)bust.pixel(x,y,c);
  }
  const box=bounds(head),fb=bounds(face),bb=bounds(bust);
  return {headBoundsThroughChin:box,headSize:[box[2]-box[0]+1,box[3]-box[1]+1],
    faceBoundsIncludingEar:fb,faceSize:[fb[2]-fb[0]+1,fb[3]-fb[1]+1],
    eyeRows:eyes,chinRow:chin,bustSpan:[bb[0],bb[2]],bustWidth:bb[2]-bb[0]+1,
    headroom:box[1],bottomRow:bb[3]};
}
function verify(native,saved,comparison,refs) {
  assert.equal(native.w,50);assert.equal(native.h,50);
  assert.equal(saved.w,100);assert.equal(saved.h,100);
  assert.equal(comparison.w,1500);assert.equal(comparison.h,300);
  assert.deepEqual(saved.data,native.scale(2).data,'Saved PNG must contain identical 2x2 cells.');
  assert.deepEqual([...new Set(saved.data.filter((_,i)=>i%4===3))].sort((a,b)=>a-b),[0,255]);
  assert(palette(native).length<=24,'Too many opaque colours.');
  const referenceColors=new Set(refs.flatMap(a=>palette(a).map(([hex])=>hex)));
  for(const [hex] of palette(native))assert(referenceColors.has(hex),'Colour absent from references: '+hex);
  assert.equal(components(native).length,1,'Detached/orphan foreground pixels.');
  assert.deepEqual(bounds(native),[6,4,39,49]);
  for(const [x,y] of [[26,22],[34,22]])assert.deepEqual(native.get(x,y),color(P.cloth),'Eye highlight lost.');
  let mouthPixels=0;
  for(let y=26;y<=30;y++)for(let x=28;x<=33;x++)if(native.get(x,y).slice(0,3).join()===color(P.lip).slice(0,3).join())mouthPixels++;
  assert.equal(mouthPixels,1,'Mouth must be one art pixel.');
  for(let y=0;y<50;y++)for(let x=0;x<50;x++)if(native.get(x,y).join()===color(P.cyan).join()) {
    assert(x>=27,'Cyan rim moved off the right side.');
    assert([[1,0],[0,-1],[0,1]].some(([dx,dy])=>x+dx>=50||y+dy<0||y+dy>=50||!native.get(x+dx,y+dy)[3]),`Interior/thick rim at ${x},${y}`);
  }
}
module.exports={Canvas,encode,decode,modeCell,nativeReference,bounds,palette,
  detectGrid,sourceCells,cutout,fitFigure,cleanAt50,components,measure,P};

if(require.main===module) {
  const source=decode(path.join(OUT,'benefactor_gemini2.png'));
  assert.deepEqual([source.w,source.h],[1024,1024]);
  const gx=detectGrid(source,'x'),gy=detectGrid(source,'y');
  const cells=sourceCells(source,gx,gy),extraction=cutout(cells);
  const native=cleanAt50(fitFigure(cells));
  fs.writeFileSync(path.join(OUT,'helper_v4.png'),encode(native.scale(2)));
  const names=['yudo','citizen','luddite','helper'];
  const refs=names.map(n=>decode(path.join(ROOT,'Assets/Resources/Portraits',n+'.png')));
  const comparison=new Canvas(500,100,'#0B1026');
  [...refs,native.scale(2)].forEach((a,i)=>comparison.blit(a,i*100,0));
  fs.writeFileSync(path.join(OUT,'helper_v4_compare_3x.png'),encode(comparison.scale(3)));
  const saved=decode(path.join(OUT,'helper_v4.png')),
    savedComparison=decode(path.join(OUT,'helper_v4_compare_3x.png'));
  verify(native,saved,savedComparison,refs);
  assert.deepEqual(savedComparison.data,comparison.scale(3).data);
  // Coordinates are zero-based and inclusive, in ART pixels. The head
  // envelope includes hair/hat/ponytail above the annotated jaw outline.
  // Skin bounds exclude the neck. Eye/jaw annotations were visually checked
  // against enlarged reference pixels; Citizen's hat lowers the face.
  const measurements={
    yudo:measure(nativeReference(refs[0]),31,['#f8d0b2','#d69a83','#b77569'],[21,23]),
    citizen:measure(nativeReference(refs[1]),35,['#f9cbad','#c68e7e'],[26,27]),
    helper_v4:measure(native,31,[P.skin,P.skinMid,P.skinShade,P.skinDark],[21,22]),
  };
  console.log(JSON.stringify({draftOnly:true,grid:{x:gx,y:gy},extraction,measurements,
    native:{size:[50,50],bounds:bounds(native),opaqueColors:palette(native).length,
      colorsIncludingTransparency:palette(native).length+1,palette:palette(native)},
    comparison:{size:[1500,300],order:[...names,'helper_v4'],background:'#0B1026'},
    checks:['PNG round-trip','exact 2x nearest cells','binary alpha','<=24 opaque colours',
      'all colours from references/current helper','one foreground component',
      'two eye highlights','one-pixel mouth','one-pixel exterior right rim','nearest 3x comparison']},null,2));
}
