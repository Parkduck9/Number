// Draft-only deterministic pixel edits. Run with Node; no packages or APIs.
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const crypto = require('crypto');
const assert = require('assert/strict');
const OUT = __dirname;
const SRC = path.resolve(OUT, '../redraw_v2');
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

// Only these integer pixels may change. Small face pips are single pixels.
const topPips = {1: [[12,5]], 2: [[9,4],[15,6]]};
const sidePips = {2: [[4,9],[6,15]], 3: [[4,9],[5,12],[6,15]]};
const counts = [[1,2,3],[2,1,3],[3,1,2],[4,1,2],[5,1,3],[6,2,3]];
const square = (x,y) => [[x,y],[x+1,y],[x,y+1],[x+1,y+1]];
const rolls = [
  {top:[[8,6],[12,8],[16,10]], side:[[6,13]], front:square(15,16)},
  {top:[[12,7]], side:[[5,10],[7,15]], front:[...square(13,14),...square(17,17)]},
  {top:[[9,4],[15,7]], side:[[4,9],[6,12],[7,16]], front:[...square(12,12),...square(15,15),...square(18,18)]},
  {top:topPips[2], side:sidePips[2], front:square(14,14)},
];
const boundary = [C.edge,C.cyan,C.light];
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
function isolatedInteriorPips(a,coords,mask) {
  assert(coords.length>=1&&coords.length<=3);
  for(const [x,y] of coords) {
    // Eight-neighbour clearance: no edge contact, even diagonally.
    for(let dy=-1;dy<=1;dy++) for(let dx=-1;dx<=1;dx++)
      assert(mask.has((y+dy)*a.w+x+dx),`Pip touches face edge at ${x},${y}`);
    for(const [xx,yy] of coords) if(x!==xx||y!==yy)
      assert(Math.max(Math.abs(xx-x),Math.abs(yy-y))>=3,'Pips too close');
  }
}
const protectedFiles = fs.readdirSync(SRC).map(n=>path.join(SRC,n))
  .concat(NAMES.map(n=>path.join(ROOT,'Assets/Resources/DraftHud',n+'.png')));
const before = new Map(protectedFiles.map(file=>[file,hash(file)]));
const sprites=[], report=[];
for(const name of NAMES) {
  const src=read(path.join(SRC,name+'.png'));
  const a={w:src.w,h:src.h,d:Buffer.from(src.d)};
  assert.equal(a.w,24); assert.equal(a.h,24);
  const off=name==='dice_off', rolling=name.startsWith('dice_roll');
  const values=counts[off?0:Number(name.slice(-1))-1];
  const regions=rolling?rolls[Number(name.slice(-1))]:{
    top:topPips[values[1]], side:sidePips[values[2]], front:[],
  };
  if(!rolling) {
    for(let i=0;i<3;i++) for(let j=i+1;j<3;j++) {
      assert.notEqual(values[i],values[j]); assert.notEqual(values[i]+values[j],7);
    }
    assert.equal(regions.top.length,values[1]); assert.equal(regions.side.length,values[2]);
  }
  const geometry=off?read(path.join(SRC,'dice_face_1.png')):src;
  const masks={top:faceMask(geometry,regions.top[0]),side:faceMask(geometry,regions.side[0])};
  assert(masks.top.size>20&&masks.side.size>20);
  assert([...masks.top].every(p=>!masks.side.has(p)));
  const greyPip=pixel(src,14,14);
  const changed=new Set();
  for(const face of ['top','side','front']) {
    if(face!=='front') isolatedInteriorPips(a,regions[face],masks[face]);
    for(const [x,y] of regions[face]) {
      const original=pixel(src,x,y);
      assert(eq(original,off?(face==='top'?C.greyLight:C.grey):C[face]));
      put(a,x,y,off?greyPip:face==='front'?C.red:C.darkRed);
      changed.add(y*a.w+x);
    }
  }
  for(let p=0;p<a.w*a.h;p++) {
    assert.equal(a.d[p*4+3],src.d[p*4+3]);
    assert(a.d[p*4+3]===0||a.d[p*4+3]===255);
    if(!changed.has(p)) assert.deepEqual(a.d.subarray(p*4,p*4+4),src.d.subarray(p*4,p*4+4));
    else assert.notDeepEqual(a.d.subarray(p*4,p*4+4),src.d.subarray(p*4,p*4+4));
  }
  write(path.join(OUT,name+'.png'),a);
  assert.deepEqual(read(path.join(OUT,name+'.png')).d,a.d);
  sprites.push(a);
  report.push({file:name+'.png',top:regions.top.length,side:regions.side.length,
    front:rolling?regions.front.length/4:values[0],changedPixels:changed.size});
}

// Reuse the exact filename-only header and layout from v2.
const oldPreview=read(path.join(SRC,'preview_8x.png'));
const sheet={w:oldPreview.w,h:oldPreview.h,d:Buffer.from(oldPreview.d)};
assert.equal(sheet.w,224*NAMES.length); assert.equal(sheet.h,232);
for(let i=0;i<NAMES.length;i++) for(let y=0;y<192;y++) for(let x=0;x<192;x++) {
  const c=pixel(sprites[i],Math.floor(x/8),Math.floor(y/8));
  put(sheet,i*224+16+x,32+y,c[3]?c:((Math.floor(x/32)+Math.floor(y/32))%2?[31,38,51,255]:[24,29,40,255]));
}
write(path.join(OUT,'preview_8x.png'),sheet);
assert.deepEqual(sheet.d.subarray(0,sheet.w*24*4),oldPreview.d.subarray(0,sheet.w*24*4));
for(const [file,digest] of before) assert.equal(hash(file),digest,`Source changed: ${file}`);
console.table(report);
console.log('Verified: 11 sprites at 24x24; single-pixel separated top/side pips with edge clearance; requested legal neighbours; darker red / matching inactive grey; all other v2 pixels and alpha identical; filename-only 8x preview; v2 and installed sprite hashes unchanged.');

