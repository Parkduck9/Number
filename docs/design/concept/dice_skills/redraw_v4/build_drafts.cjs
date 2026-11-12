// Draft-only deterministic pixel edits. Run with Node; no packages or APIs.
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const crypto = require('crypto');
const assert = require('assert/strict');
const OUT = __dirname;
const SRC = path.resolve(OUT, '../redraw_v3');
const ROOT = path.resolve(OUT, '../../../../..');
const NAMES = Array.from({length: 6}, (_, i) => `dice_face_${i + 1}`)
  .concat('dice_off', Array.from({length: 4}, (_, i) => `dice_roll_${i}`));
const C = {
  front: [8,11,30,255], side: [16,24,48,255], top: [25,48,68,255],
  edge: [35,115,140,255], cyan: [79,240,255,255], light: [192,251,255,255],
  red: [242,45,82,255], darkRed: [187,33,63,255],
  grey: [66,73,81,255], greyLight: [107,120,124,255],
};
const eq = (a, b) => a.every((v, i) => v === b[i]);
const pixel = (a, x, y) => [...a.d.subarray((y*a.w+x)*4, (y*a.w+x+1)*4)];
const put = (a, x, y, c) => a.d.set(c, (y*a.w+x)*4);
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');

// Minimal RGBA8 PNG codec: preserves the original integer grid and hard alpha.
function read(file) {
  const b = fs.readFileSync(file), w = b.readUInt32BE(16), h = b.readUInt32BE(20);
  assert.equal(b[24], 8); assert.equal(b[25], 6); assert.equal(b[28], 0);
  let ids = [];
  for (let p=8; p<b.length;) {
    const n=b.readUInt32BE(p);
    if (b.toString('ascii',p+4,p+8)==='IDAT') ids.push(b.subarray(p+8,p+8+n));
    p+=n+12;
  }
  const raw=zlib.inflateSync(Buffer.concat(ids)), d=Buffer.alloc(w*h*4), stride=w*4;
  for(let y=0;y<h;y++) for(let x=0;x<stride;x++) {
    const f=raw[y*(stride+1)], o=y*stride+x;
    const l=x>=4?d[o-4]:0, u=y?d[o-stride]:0, ul=y&&x>=4?d[o-stride-4]:0;
    let v=0;
    assert(f<=4);
    if(f===1) v=l;
    if(f===2) v=u;
    if(f===3) v=Math.floor((l+u)/2);
    if(f===4) { const q=l+u-ul, dl=Math.abs(q-l), du=Math.abs(q-u), dul=Math.abs(q-ul); v=dl<=du&&dl<=dul?l:du<=dul?u:ul; }
    d[o]=(raw[y*(stride+1)+1+x]+v)&255;
  }
  return {w,h,d};
}
const crcTable=Array.from({length:256},(_,n)=>{for(let i=0;i<8;i++)n=n&1?0xedb88320^(n>>>1):n>>>1;return n>>>0;});
function chunk(t,d) {
  const b=Buffer.alloc(d.length+12); b.writeUInt32BE(d.length); b.write(t,4); d.copy(b,8);
  let c=0xffffffff; for(const v of b.subarray(4,-4)) c=crcTable[(c^v)&255]^(c>>>8);
  b.writeUInt32BE((c^0xffffffff)>>>0,b.length-4); return b;
}
function write(file,a) {
  const head=Buffer.alloc(13); head.writeUInt32BE(a.w); head.writeUInt32BE(a.h,4); head[8]=8; head[9]=6;
  const raw=Buffer.alloc((a.w*4+1)*a.h);
  for(let y=0;y<a.h;y++) a.d.copy(raw,y*(a.w*4+1)+1,y*a.w*4,(y+1)*a.w*4);
  fs.writeFileSync(file,Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',head),chunk('IDAT',zlib.deflateSync(raw,{level:9})),chunk('IEND',Buffer.alloc(0))]));
}

// Fixed xorshift32 stream, consumed top then side in NAMES order.
const SEED = 42;
let state = SEED;
function pick(values) {
  state ^= state << 13; state ^= state >>> 17; state ^= state << 5;
  return values[Math.floor((state >>> 0) / 4294967296 * values.length)];
}
const rest = {
  top: {corners:[[8,4],[14,4],[10,6],[16,6]], centre:[12,5]},
  side: {corners:[[4,8],[6,10],[4,14],[6,16]], centre:[5,12]},
};
const layouts = [
  {
    top:{corners:[[10,6],[14,6],[12,10],[16,10]],centre:[13,8]},
    side:{corners:[[4,10],[6,12],[6,14],[8,16]],centre:[6,13]},
  },
  {
    top:{corners:[[8,5],[14,5],[10,9],[16,9]],centre:[12,7]},
    side:{corners:[[4,10],[6,11],[6,14],[8,15]],centre:[6,13]},
  },
  {
    top:{corners:[[8,5],[14,5],[10,7],[16,7]],centre:[12,6]},
    side:{corners:[[4,9],[6,11],[5,13],[7,15]],centre:[6,12]},
  },
  rest,
];
function pattern(n, layout) {
  const [tl,tr,bl,br] = layout.corners, c = layout.centre;
  return {1:[c],2:[tl,br],3:[tl,c,br],4:[tl,tr,bl,br],5:[tl,tr,bl,br,c]}[n];
}
// Flood only the auxiliary face interiors; outlines separate the regions.
function faceMask(a, seed) {
  const mask=new Set(), pending=[seed];
  while(pending.length) {
    const [x,y]=pending.pop(), key=y*a.w+x;
    if(x<0||y<0||x>=a.w||y>=a.h||mask.has(key)) continue;
    const c=pixel(a,x,y);
    if(!c[3]||[C.edge,C.cyan,C.light].some(b=>eq(c,b))) continue;
    mask.add(key);
    pending.push([x-1,y],[x+1,y],[x,y-1],[x,y+1]);
  }
  return mask;
}
function validatePips(a, coords, mask) {
  assert(coords.length>=1&&coords.length<=5);
  assert.equal(new Set(coords.map(p=>p.join(','))).size,coords.length);
  for(const [x,y] of coords) {
    for(let dy=-1;dy<=1;dy++) for(let dx=-1;dx<=1;dx++)
      assert(mask.has((y+dy)*a.w+x+dx),`Pip touches edge: ${x},${y}`);
    for(const [xx,yy] of coords) if(x!==xx||y!==yy)
      assert(Math.max(Math.abs(xx-x),Math.abs(yy-y))>=2,'Pips touch');
  }
}
const protectedFiles=fs.readdirSync(SRC).map(n=>path.join(SRC,n))
  .concat(NAMES.map(n=>path.join(ROOT,'Assets/Resources/DraftHud',n+'.png')));
const before=new Map(protectedFiles.map(file=>[file,hash(file)]));
const sprites=[], report=[];
for(const name of NAMES) {
  const src=read(path.join(SRC,name+'.png'));
  const a={w:src.w,h:src.h,d:Buffer.from(src.d)};
  assert.equal(a.w,24); assert.equal(a.h,24);
  const off=name==='dice_off', rolling=name.startsWith('dice_roll');
  const index=Number(name.slice(-1));
  const front=rolling?[1,2,3,1][index]:off?1:index;
  const options=[1,2,3,4,5].filter(n=>rolling||n!==front);
  const top=pick(options), side=pick(options.filter(n=>rolling||n!==top));
  const layout=rolling?layouts[index]:rest;
  if(!rolling) {
    assert.notEqual(top,front); assert.notEqual(side,front); assert.notEqual(top,side);
  }
  const geometry=off?read(path.join(SRC,'dice_face_1.png')):src;
  const masks={top:faceMask(geometry,layout.top.centre),side:faceMask(geometry,layout.side.centre)};
  assert(masks.top.size>20&&masks.side.size>20);
  assert([...masks.top].every(p=>!masks.side.has(p)));
  const pip=off?pixel(src,14,14):C.darkRed;
  const coords={}, oldCoords={};
  const allowed=new Set();
  for(const face of ['top','side']) {
    coords[face]=pattern(face==='top'?top:side,layout[face]);
    validatePips(a,coords[face],masks[face]);
    oldCoords[face]=[];
    const background=off?(face==='top'?C.greyLight:C.grey):C[face];
    for(const key of masks[face]) {
      const x=key%a.w,y=Math.floor(key/a.w), c=pixel(src,x,y);
      assert(eq(c,background)||eq(c,pip),'Unexpected auxiliary-face color');
      if(eq(c,pip)) {
        oldCoords[face].push([x,y]); put(a,x,y,background); allowed.add(key);
      }
    }
    assert(oldCoords[face].length>=1&&oldCoords[face].length<=3);
    for(const [x,y] of coords[face]) { put(a,x,y,pip); allowed.add(y*a.w+x); }
    const found=[...masks[face]].filter(k=>eq(pixel(a,k%a.w,Math.floor(k/a.w)),pip));
    assert.equal(found.length,coords[face].length);
    assert.deepEqual(new Set(found),new Set(coords[face].map(([x,y])=>y*a.w+x)));
  }
  let changed=0;
  for(let p=0;p<a.w*a.h;p++) {
    assert.equal(a.d[p*4+3],src.d[p*4+3]);
    assert(a.d[p*4+3]===0||a.d[p*4+3]===255);
    if(!a.d.subarray(p*4,p*4+4).equals(src.d.subarray(p*4,p*4+4))) {
      changed++; assert(allowed.has(p),'Changed a body, outline or front pixel');
      assert(masks.top.has(p)||masks.side.has(p));
    }
  }
  const file=path.join(OUT,name+'.png'); write(file,a);
  assert.deepEqual(read(file).d,a.d);
  sprites.push(a);
  report.push({file:name+'.png',front,top,side,changedPixels:changed,pips:coords});
}
// Preserve the v3 filename-only labels and checkerboard, updating only sprites.
const oldPreview=read(path.join(SRC,'preview_8x.png'));
const sheet={w:oldPreview.w,h:oldPreview.h,d:Buffer.from(oldPreview.d)};
assert.equal(sheet.w,224*NAMES.length); assert.equal(sheet.h,232);
for(let i=0;i<NAMES.length;i++) for(let y=0;y<192;y++) for(let x=0;x<192;x++) {
  const c=pixel(sprites[i],Math.floor(x/8),Math.floor(y/8));
  put(sheet,i*224+16+x,32+y,c[3]?c:((Math.floor(x/32)+Math.floor(y/32))%2?[31,38,51,255]:[24,29,40,255]));
}
write(path.join(OUT,'preview_8x.png'),sheet);
assert.deepEqual(sheet.d.subarray(0,sheet.w*32*4),oldPreview.d.subarray(0,sheet.w*32*4));
for(const [file,digest] of before) assert.equal(hash(file),digest,`Source changed: ${file}`);
fs.writeFileSync(path.join(OUT,'pip_layout.json'),JSON.stringify({seed:SEED,prng:'xorshift32',sprites:report},null,2)+'\n');
console.table(report.map(({pips,...row})=>row));
console.log('Verified: seed 42; 11 RGBA 24x24 sprites; 1..5 auxiliary pips; die patterns, single pixels, >=1px gaps and edge margins; all front/body/outline/alpha pixels identical to v3; filename-only 8x preview; v3 and installed sprites unchanged.');

