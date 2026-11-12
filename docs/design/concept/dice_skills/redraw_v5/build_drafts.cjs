// Draft-only deterministic pixel edits. Run with Node; no packages or APIs.
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const crypto = require('crypto');
const assert = require('assert/strict');
const OUT = __dirname;
const SRC = path.resolve(OUT, '../redraw_v4');
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

// High camera: a nearly square TOP, with two thin vertical face strips.
// Points are back-left, back-right, front-right, front-left; fifth point is
// the common extrusion vector for front/left strip thickness.
const REST = [[3,3],[18,1],[22,16],[7,18],[-2,5]];
const ROLLS = [
  [[5,1],[19,5],[21,14],[7,10],[-4,9]],
  [[2,8],[14,1],[22,10],[10,17],[-1,6]],
  [[3,3],[18,2],[21,12],[6,13],[-2,10]],
  [[4,3],[19,1],[22,13],[7,16],[-2,7]],
];
const SEED = 42;
const OFF_PIP = pixel(read(path.join(SRC,'dice_off.png')),14,14);
let state = SEED;
function pick(values) {
  state ^= state << 13; state ^= state >>> 17; state ^= state << 5;
  return values[Math.floor((state >>> 0) / 4294967296 * values.length)];
}
const add = (a,b) => a.map((v,i)=>v+b[i]);
function projection(q,u,v) {
  // Bilinear mapping also accommodates the slight roll_3 perspective skew.
  return [0,1].map(i=>(1-u)*(1-v)*q[0][i]+u*(1-v)*q[1][i]+u*v*q[2][i]+(1-u)*v*q[3][i]);
}
function line(a,p,q,c) {
  let [x,y]=p, [xx,yy]=q;
  const dx=Math.abs(xx-x),dy=-Math.abs(yy-y),sx=x<xx?1:-1,sy=y<yy?1:-1;
  let error=dx+dy;
  for(;;) {
    assert(x>=0&&x<24&&y>=0&&y<24); put(a,x,y,c);
    if(x===xx&&y===yy) break;
    const e=2*error;
    if(e>=dy) {error+=dy;x+=sx;} if(e<=dx) {error+=dx;y+=sy;}
  }
}
function inside(q,x,y) {
  let positive=false,negative=false;
  for(let i=0;i<q.length;i++) {
    const p=q[i],r=q[(i+1)%q.length], cross=(r[0]-p[0])*(y-p[1])-(r[1]-p[1])*(x-p[0]);
    positive ||= cross>0; negative ||= cross<0;
  }
  return !(positive&&negative);
}
function fill(a,q,c) {
  for(let y=0;y<24;y++) for(let x=0;x<24;x++) if(inside(q,x,y)) put(a,x,y,c);
}
function drawBody(geometry,off) {
  const [A,B,Cc,D,E]=geometry, Ae=add(A,E),Ce=add(Cc,E),De=add(D,E);
  const a={w:24,h:24,d:Buffer.alloc(24*24*4)};
  const faces={top:[A,B,Cc,D],front:[D,Cc,Ce,De],side:[A,D,De,Ae]};
  const colors=off?{top:C.greyLight,front:C.grey,side:C.grey,edge:C.grey,cyan:C.greyLight,light:C.greyLight}:C;
  for(const face of ['side','front','top']) fill(a,faces[face],colors[face]);
  for(const [p,q] of [[A,Ae],[Ae,De],[De,Ce]]) line(a,p,q,colors.edge);
  for(const [p,q] of [[A,B],[B,Cc],[Cc,Ce],[A,D],[D,Cc],[D,De]]) line(a,p,q,colors.cyan);
  line(a,A,add(A,[1,1]),colors.light);
  line(a,D,add(D,[2,0]),colors.light);
  // Exact allowed pixels: never paint over a contour or outside the die.
  const masks={};
  for(const face of Object.keys(faces)) {
    masks[face]=new Set();
    for(let y=0;y<24;y++) for(let x=0;x<24;x++)
      if(inside(faces[face],x,y)&&eq(pixel(a,x,y),colors[face])) masks[face].add(y*24+x);
  }
  // Grey body and grey contour share a palette entry; use the active geometry
  // mask to keep inactive pips off those otherwise indistinguishable edges.
  return {a,faces,masks:off?drawBody(geometry,false).masks:masks};
}
function pattern(n) {
  const tl=[.25,.25],tr=[.75,.25],bl=[.25,.75],br=[.75,.75],c=[.5,.5];
  return {1:[c],2:[tl,br],3:[tl,c,br],4:[tl,tr,bl,br],5:[tl,tr,bl,br,c],6:[tl,tr,[.25,.5],[.75,.5],bl,br]}[n];
}
function pips(a,face,mask,n,color,size=1) {
  const coords=[];
  for(const uv of pattern(n)) {
    const projected=projection(face,...uv);
    let p=projected.map(v=>Math.round(v-(size-1)/2));
    if(size===1) {
      // Snap compressed strip pips to the nearest interior integer pixel.
      const candidates=[...mask].map(k=>[k%24,Math.floor(k/24)])
        .filter(q=>coords.every(group=>group.every(r=>Math.max(Math.abs(q[0]-r[0]),Math.abs(q[1]-r[1]))>=2)))
        .sort((q,r)=>Math.hypot(q[0]-projected[0],q[1]-projected[1])-Math.hypot(r[0]-projected[0],r[1]-projected[1]));
      assert(candidates.length,`No pip room: n=${n} face=${JSON.stringify(face)} mask=${mask.size}`); p=candidates[0];
      assert(Math.hypot(p[0]-projected[0],p[1]-projected[1])<=1.6,'Excessive strip-pip displacement');
    }
    const pixels=[];
    for(let dy=0;dy<size;dy++) for(let dx=0;dx<size;dx++) {
      const x=p[0]+dx,y=p[1]+dy;
      assert(mask.has(y*24+x),`Pip on boundary: ${x},${y}`);
      put(a,x,y,color); pixels.push([x,y]);
    }
    coords.push(pixels);
  }
  for(let i=0;i<coords.length;i++) for(let j=i+1;j<coords.length;j++)
    for(const p of coords[i]) for(const q of coords[j]) assert(Math.max(Math.abs(p[0]-q[0]),Math.abs(p[1]-q[1]))>=2,'Pips merge');
  return coords;
}
function tumblePips(a,face,mask,index) {
  // Oblique fragments, deliberately outside a complete result pattern.
  // Edge-clipped marks and oblique, incomplete arrangements cannot read as a
  // settled 1..6 pattern, particularly in the final near-rest roll frame.
  const uvSets=[[[.2,.2],[.7,.45],[.82,.8]],[[.1,.4],[.5,.5],[.9,.6]],[[.25,.15],[.75,.45],[.4,.9]],[[.15,.12],[.72,.25],[.85,.88]]];
  const coords=[];
  for(const uv of uvSets[index]) {
    const [x,y]=projection(face,...uv).map(Math.round), pixels=[];
    for(const [dx,dy] of [[0,0],[1,0],[0,1],[1,1]]) {
      if(mask.has((y+dy)*24+x+dx)) {put(a,x+dx,y+dy,C.darkRed);pixels.push([x+dx,y+dy]);}
    }
    assert(pixels.length,`Empty tumble pip: frame=${index}, uv=${uv}`); coords.push(pixels);
  }
  return coords;
}
const protectedFiles=fs.readdirSync(SRC).map(n=>path.join(SRC,n))
  .concat(NAMES.map(n=>path.join(ROOT,'Assets/Resources/DraftHud',n+'.png')));
const before=new Map(protectedFiles.map(file=>[file,hash(file)]));
const palette=new Set();
for(const name of NAMES) {const a=read(path.join(SRC,name+'.png'));for(let p=0;p<a.w*a.h;p++) palette.add(pixel(a,p%a.w,Math.floor(p/a.w)).join(','));}
const sprites=[],report=[];
for(const name of NAMES) {
  const rolling=name.startsWith('dice_roll'), off=name==='dice_off', index=Number(name.slice(-1));
  const top=rolling?null:off?1:index;
  const options=[1,2,3,4,5].filter(n=>n!==top);
  const front=pick(options),side=pick(options.filter(n=>n!==front));
  const {a,faces,masks}=drawBody(rolling?ROLLS[index]:REST,off);
  const coords={};
  coords.top=rolling?tumblePips(a,faces.top,masks.top,index):pips(a,faces.top,masks.top,top,off?OFF_PIP:C.red,2);
  for(const face of ['front','side']) coords[face]=pips(a,faces[face],masks[face],face==='front'?front:side,off?OFF_PIP:C.darkRed);
  let bottom=-1;
  for(let y=0;y<24;y++) for(let x=0;x<24;x++) {
    const c=pixel(a,x,y); assert(c[3]===0||c[3]===255);
    assert(palette.has(c.join(',')),`New palette color ${c}`);
    if(c[3]) bottom=y;
  }
  assert.equal(bottom,23,'Bottom-centre canvas anchoring');
  if(!rolling) {
    assert.notEqual(front,top); assert.notEqual(side,top); assert.notEqual(front,side);
    assert(masks.top.size>masks.front.size+masks.side.size,'Top must dominate');
  }
  write(path.join(OUT,name+'.png'),a); assert.deepEqual(read(path.join(OUT,name+'.png')).d,a.d);
  sprites.push(a);report.push({file:name+'.png',top,front,side,bottomRow:bottom,pips:coords,geometry:rolling?ROLLS[index]:REST});
}
// Reuse the exact v4 filename-only headers and 8x checkerboard layout.
const oldPreview=read(path.join(SRC,'preview_8x.png'));
const sheet={w:oldPreview.w,h:oldPreview.h,d:Buffer.from(oldPreview.d)};
assert.equal(sheet.w,224*NAMES.length);assert.equal(sheet.h,232);
for(let i=0;i<NAMES.length;i++) for(let y=0;y<192;y++) for(let x=0;x<192;x++) {
  const c=pixel(sprites[i],Math.floor(x/8),Math.floor(y/8));
  put(sheet,i*224+16+x,32+y,c[3]?c:((Math.floor(x/32)+Math.floor(y/32))%2?[31,38,51,255]:[24,29,40,255]));
}
write(path.join(OUT,'preview_8x.png'),sheet);
assert.deepEqual(sheet.d.subarray(0,sheet.w*32*4),oldPreview.d.subarray(0,sheet.w*32*4));
for(const [file,digest] of before) assert.equal(hash(file),digest,`Source changed: ${file}`);
fs.writeFileSync(path.join(OUT,'pip_layout.json'),JSON.stringify({seed:SEED,prng:'xorshift32',sprites:report},null,2)+'\n');
console.table(report.map(({pips,geometry,...row})=>row));
console.log('Verified: eleven 24x24 RGBA sprites; v4 palette only; hard alpha; bottom row 23; 2x2 resting top pips; separate seeded 1..5 strip pips; dominant top; unchanged v4 and installed sprites; filename-only nearest-neighbour 8x preview.');

