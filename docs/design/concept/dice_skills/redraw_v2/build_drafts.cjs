// Draft-only deterministic pixel edits. Run with Node; no packages or APIs.
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const crypto = require('crypto');
const assert = require('assert/strict');
const OUT = __dirname;
const SRC = path.resolve(OUT, '../redraw_v1');
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

// Record every v1 file and the corresponding installed sprites before editing.
const protectedFiles = fs.readdirSync(SRC).map(n=>path.join(SRC,n))
  .concat(NAMES.map(n=>path.join(ROOT,'Assets/Resources/DraftHud',n+'.png')));
const before = new Map(protectedFiles.map(file=>[file,hash(file)]));
const layout=JSON.parse(fs.readFileSync(path.join(SRC,'face_layout.json'),'utf8'));
const boundary=[C.edge,C.cyan,C.light];
function faceMask(a,seed) {
  const result=new Set(), pending=[seed];
  while(pending.length) {
    const [x,y]=pending.pop(), key=y*a.w+x;
    if(x<0||y<0||x>=a.w||y>=a.h||result.has(key)) continue;
    const c=pixel(a,x,y);
    if(!c[3]||boundary.some(b=>eq(c,b))) continue;
    result.add(key);
    pending.push([x-1,y],[x+1,y],[x,y-1],[x,y+1]);
  }
  return result;
}
const sprites=[];
for(const name of NAMES) {
  const src=read(path.join(SRC,name+'.png'));
  const a={w:src.w,h:src.h,d:Buffer.from(src.d)};
  assert.equal(a.w,24); assert.equal(a.h,24);
  // The inactive die has the identical rest geometry, but grey outlines.
  const geometry=name==='dice_off'?read(path.join(SRC,'dice_face_1.png')):src;
  const regions=layout[name].pip_pixels;
  const top=faceMask(geometry,regions.top[0]);
  const side=faceMask(geometry,regions.side[0]);
  assert(top.size>20&&side.size>20);
  assert([...top].every(p=>!side.has(p)));
  const frontPips=new Set(regions.front.map(([x,y])=>y*a.w+x));
  assert([...frontPips].every(p=>!top.has(p)&&!side.has(p)));
  for(const p of top) a.d.set(name==='dice_off'?C.greyLight:C.top,p*4);
  for(const p of side) a.d.set(name==='dice_off'?C.grey:C.side,p*4);
  const rolling=name.startsWith('dice_roll');
  if(rolling) for(const [x,y] of regions.front) put(a,x,y,C.front);

  const palette=new Set();
  for(let p=0;p<src.w*src.h;p++) palette.add([...src.d.subarray(p*4,p*4+4)].join(','));
  for(let p=0;p<a.w*a.h;p++) {
    const c=[...a.d.subarray(p*4,p*4+4)];
    assert(palette.has(c.join(',')),`${name}: new palette color`);
    assert.equal(c[3],src.d[p*4+3]);
    assert(c[3]===0||c[3]===255);
    if(!top.has(p)&&!side.has(p)&&!(rolling&&frontPips.has(p)))
      assert.deepEqual(a.d.subarray(p*4,p*4+4),src.d.subarray(p*4,p*4+4));
    if(name!=='dice_off'&&(eq(c,C.red)||eq(c,C.darkRed)))
      assert(!rolling&&frontPips.has(p));
  }
  for(const p of top) assert(eq([...a.d.subarray(p*4,p*4+4)],name==='dice_off'?C.greyLight:C.top));
  for(const p of side) assert(eq([...a.d.subarray(p*4,p*4+4)],name==='dice_off'?C.grey:C.side));
  write(path.join(OUT,name+'.png'),a);
  assert.deepEqual(read(path.join(OUT,name+'.png')).d,a.d);
  sprites.push(a);
}

// Reuse only the filename label strip from v1; omit its face-value captions.
const oldPreview=read(path.join(SRC,'preview_8x.png'));
const sheet={w:224*NAMES.length,h:232,d:Buffer.alloc(224*NAMES.length*232*4)};
for(let y=0;y<sheet.h;y++) for(let x=0;x<sheet.w;x++) put(sheet,x,y,[24,29,40,255]);
for(let y=0;y<24;y++) oldPreview.d.copy(sheet.d,y*sheet.w*4,y*oldPreview.w*4,(y*oldPreview.w+sheet.w)*4);
for(let i=0;i<NAMES.length;i++) for(let y=0;y<192;y++) for(let x=0;x<192;x++) {
  const c=pixel(sprites[i],Math.floor(x/8),Math.floor(y/8));
  put(sheet,i*224+16+x,32+y,c[3]?c:((Math.floor(x/32)+Math.floor(y/32))%2?[31,38,51,255]:[24,29,40,255]));
}
write(path.join(OUT,'preview_8x.png'),sheet);
for(const [file,digest] of before) assert.equal(hash(file),digest,`Source changed: ${file}`);
console.log('Verified 11 sprites: 24x24, binary alpha, v1 palettes/silhouettes/outlines, unchanged rest front faces, flat unmarked top/side faces, unmarked tumbles, filename-only 8x nearest preview; v1 and installed sprite hashes unchanged.');
