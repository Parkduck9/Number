// Deterministic local pixel cleanup; no generation service or external packages.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../../../..');
const out = path.join(root, 'Assets/Resources/DraftSprites');
function pngRead(file) {
  const b = fs.readFileSync(file); let w, h, type, depth, pal, trans, pos = 8, chunks = [];
  while (pos < b.length) {
    const n = b.readUInt32BE(pos), t = b.toString('ascii', pos + 4, pos + 8), d = b.subarray(pos + 8, pos + 8 + n); pos += n + 12;
    if (t === 'IHDR') { w = d.readUInt32BE(0); h = d.readUInt32BE(4); depth = d[8]; type = d[9]; if (d[12]) throw Error('Interlaced PNG'); }
    if (t === 'PLTE') pal = d;
    if (t === 'tRNS') trans = d;
    if (t === 'IDAT') chunks.push(d);
  }
  if (depth !== 8) throw Error(`Unsupported PNG depth ${depth}: ${file}`);
  const channels = ({0:1,2:3,3:1,4:2,6:4})[type], stride = w * channels;
  const raw = zlib.inflateSync(Buffer.concat(chunks)), scan = Buffer.alloc(h * stride);
  for (let y = 0; y < h; y++) {
    const filter = raw[y * (stride + 1)];
    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? scan[y * stride + x - channels] : 0, b = y ? scan[(y - 1) * stride + x] : 0, c = y && x >= channels ? scan[(y - 1) * stride + x - channels] : 0;
      const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
      const v = filter === 0 ? 0 : filter === 1 ? a : filter === 2 ? b : filter === 3 ? Math.floor((a+b)/2) : pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
      scan[y*stride+x] = (raw[y*(stride+1)+x+1] + v) & 255;
    }
  }
  const im = image(w,h);
  for (let i=0; i<w*h; i++) {
    const j=i*channels;
    let r,g,b,a=255;
    if (type===6 || type===2) { r=scan[j]; g=scan[j+1]; b=scan[j+2]; if(type===6)a=scan[j+3]; }
    else if(type===3) { const k=scan[j]; r=pal[k*3];g=pal[k*3+1];b=pal[k*3+2];a=trans && k<trans.length ? trans[k]:255; }
    else {r=g=b=scan[j];if(type===4)a=scan[j+1];}
    im.data.set([r,g,b,a],i*4);
  }
  return im;
}
function image(w,h,color=[0,0,0,0]) { const data=Buffer.alloc(w*h*4); for(let i=0;i<w*h;i++)data.set(color,i*4);return {w,h,data}; }
function crc32(b) {let c=0xffffffff;for(const n of b){c^=n;for(let k=0;k<8;k++)c=(c>>>1)^((c&1)?0xedb88320:0);}return(c^0xffffffff)>>>0;}
function pngWrite(file,im) {
  const chunk=(t,d)=>{const tb=Buffer.from(t),n=Buffer.alloc(4),crc=Buffer.alloc(4);n.writeUInt32BE(d.length);crc.writeUInt32BE(crc32(Buffer.concat([tb,d])));return Buffer.concat([n,tb,d,crc]);};
  const ih=Buffer.alloc(13);ih.writeUInt32BE(im.w);ih.writeUInt32BE(im.h,4);ih[8]=8;ih[9]=6;
  const raw=Buffer.alloc((im.w*4+1)*im.h);for(let y=0;y<im.h;y++)im.data.copy(raw,y*(im.w*4+1)+1,y*im.w*4,(y+1)*im.w*4);
  fs.writeFileSync(file,Buffer.concat([Buffer.from('89504e470d0a1a0a','hex'),chunk('IHDR',ih),chunk('IDAT',zlib.deflateSync(raw,{level:9})),chunk('IEND',Buffer.alloc(0))]));
}
function pixel(im,x,y) {return Array.from(im.data.subarray((y*im.w+x)*4,(y*im.w+x)*4+4));}
function set(im,x,y,c) {if(x>=0&&x<im.w&&y>=0&&y<im.h)im.data.set(c,(y*im.w+x)*4);}
function paste(dst,src,dx,dy) {for(let y=0;y<src.h;y++)for(let x=0;x<src.w;x++){const c=pixel(src,x,y);if(c[3])set(dst,x+dx,y+dy,c);}}
function crop(src,x,y,w,h) {const im=image(w,h);for(let j=0;j<h;j++)for(let i=0;i<w;i++)set(im,i,j,pixel(src,i+x,j+y));return im;}
function resize(src,w,h) {const im=image(w,h);for(let y=0;y<h;y++)for(let x=0;x<w;x++)set(im,x,y,pixel(src,Math.min(src.w-1,Math.floor((x+.5)*src.w/w)),Math.min(src.h-1,Math.floor((y+.5)*src.h/h))));return im;}
function bbox(im) {let l=im.w,t=im.h,r=-1,b=-1;for(let y=0;y<im.h;y++)for(let x=0;x<im.w;x++)if(pixel(im,x,y)[3]){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}return [l,t,r+1,b+1];}
function components(im) {
  const seen=new Uint8Array(im.w*im.h),cs=[];
  for(let i=0;i<seen.length;i++)if(!seen[i]&&im.data[i*4+3]) {
    const todo=[i],ps=[];seen[i]=1;let l=im.w,t=im.h,r=0,b=0;
    while(todo.length){const j=todo.pop(),x=j%im.w,y=Math.floor(j/im.w);ps.push(j);l=Math.min(l,x);r=Math.max(r,x);t=Math.min(t,y);b=Math.max(b,y);
      for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const nx=x+dx,ny=y+dy,k=ny*im.w+nx;if(nx>=0&&nx<im.w&&ny>=0&&ny<im.h&&!seen[k]&&im.data[k*4+3]){seen[k]=1;todo.push(k);}}
    }
    cs.push({bounds:[l,t,r+1,b+1],size:ps.length,ps});
  }
  return cs.sort((a,b)=>b.size-a.size);
}
function removeGreen(src) {
  const im=image(src.w,src.h);
  for(let y=0;y<src.h;y++)for(let x=0;x<src.w;x++){
    let [r,g,b,a]=pixel(src,x,y);
    // The source uses a pure bright-green screen. Cut mixed edge pixels too;
    // retained pixels are remapped to the shared palette below.
    if(!a||g>Math.max(r,b)+14)continue;
    set(im,x,y,[r,g,b,255]);
  }
  return im;
}
function trim(src) {const [l,t,r,b]=bbox(src);return crop(src,l,t,r-l,b-t);}
function quantize(src,palette) {
  const im=image(src.w,src.h);
  for(let y=0;y<src.h;y++)for(let x=0;x<src.w;x++) {
    const c=pixel(src,x,y); if(!c[3])continue;
    let best=Infinity,col;
    for(const p of palette) {const d=p.reduce((s,v,i)=>s+(v-c[i])**2,0);if(d<best){best=d;col=p;}}
    set(im,x,y,[...col,255]);
  }
  return im;
}
const colors={
  character:[[8,11,20],[11,16,38],[26,29,51],[45,48,80],[63,74,110],[98,103,125],[133,138,155],[184,190,200],[214,215,208],[163,123,112],[215,180,160],[239,208,182],[127,26,34],[242,28,42],[184,134,46],[255,212,59]],
  red:[[70,17,25],[127,26,34],[202,24,32],[242,28,42],[255,99,92],[255,175,165],[255,247,235]],
  amber:[[65,37,19],[127,65,16],[184,99,16],[232,141,18],[255,180,38],[255,215,100],[255,247,220]],
  magenta:[[39,13,43],[70,15,75],[112,17,117],[163,22,166],[207,31,205],[239,110,226],[255,186,240]]
};
function makeStrip(frames) {const im=image(frames[0].w*frames.length,frames[0].h);frames.forEach((f,i)=>paste(im,f,i*f.w,0));return im;}
function indexedRows(im,x,y,rows) {rows.forEach((r,j)=>[...r].forEach((v,i)=>set(im,x+i,y+j,v==='.'?[0,0,0,0]:[...colors.character[parseInt(v,16)],255])));}
function walkFrames(stand) {
  // Keep the approved stand torso and rifle. Only the lower coat, knees and
  // boots change; far legs use navy shadows and near legs use the coat light.
  const strides=[
    ['....02220....1220.......','....02220....1220.......','...02210.....01220......','...02210......01220.....','..02210........01220....','.023320.........023322..','.033330.........0333330.','.000000.........0000000.'],
    ['....02220....1220.......','....02220....1220.......','.....0220...01220.......','.....02210..1220........','......02210.23322.......','.....023220.333330......','.....033320.000000......','.....000000.............'],
    ['....02220....1220.......','....02220....1220.......','.....01220..02330.......','......01220.02330.......','.......0122003330.......','.......023322033322.....','.......0333330333330....','.......0000000000000....'],
    ['....02220....1220.......','....02220....1220.......','....02210....01220......','....02210...01220.......','....23320..01220........','...033330..023322.......','...000000..0333330......','...........0000000......']
  ];
  return strides.map((rows,i)=>{
    const f=crop(stand,0,0,64,64);
    for(let y=56;y<62;y++)for(let x=0;x<64;x++)set(f,x,y,[0,0,0,0]);
    indexedRows(f,12,56,rows.slice(2));
    // Shift one hem corner by a pixel over the bent knee, keeping the outline.
    if(i===1){set(f,16,54,[0,0,0,0]);set(f,17,54,[8,11,20,255]);}
    if(i===3){set(f,33,54,[0,0,0,0]);set(f,32,54,[8,11,20,255]);}
    for(const c of components(f).slice(1))for(const p of c.ps)f.data.fill(0,p*4,p*4+4);
    return f;
  });
}
function metaWrite(name,frames) {
  const file=path.join(out,name+'.png.meta');
  // Retain IDs when rebuilding an already-created strip, so Unity references
  // stay stable. Initial IDs are cryptographically random and unique.
  const existing=fs.existsSync(file)?fs.readFileSync(file,'utf8'):'';
  const existingNames=[...existing.matchAll(/^      name: (.+)$/gm)].map(m=>m[1]);
  const prior=existingNames.length===frames.length&&existingNames.every((n,i)=>n===`${name}_${i}`)?existing:'';
  const template=fs.readFileSync(path.join(out,'citizen_b_ranged_5f.png.meta'),'utf8').replace(/\r\n/g,'\n');
  const start=template.indexOf('    - serializedVersion: 2\n'),next=template.indexOf('\n    - serializedVersion: 2\n',start)+1,end=template.indexOf('\n    outline: []',start)+1;
  const frameBlock=template.slice(start,next);
  const oldIDs=[...prior.matchAll(/      internalID: (-?\d+)/g)].map(m=>m[1]);
  const oldSpriteIDs=[...prior.matchAll(/      spriteID: ([a-f0-9]+)/g)].map(m=>m[1]);
  const ids=frames.map((_,i)=>oldIDs[i]||String(crypto.randomBytes(8).readBigUInt64LE()&0x7fffffffffffffffn));
  const blocks=frames.map((f,i)=>frameBlock
    .replace(/name: .*/,`name: ${name}_${i}`)
    .replace(/        x: \d+/,`        x: ${i*f.w}`)
    .replace(/        width: \d+/,`        width: ${f.w}`)
    .replace(/        height: \d+/,`        height: ${f.h}`)
    .replace(/      pivot: .*/,`      pivot: {x: 0.5, y: ${name.startsWith('bullet')?'0.5':'0.03125'}}`)
    .replace(/      spriteID: .*/,`      spriteID: ${oldSpriteIDs[i]||crypto.randomUUID().replaceAll('-','')}`)
    .replace(/      internalID: .*/,`      internalID: ${ids[i]}`)).join('');
  const guid=prior.match(/^guid: (\w+)/m)?.[1]||crypto.randomUUID().replaceAll('-','');
  const table=frames.map((_,i)=>`      ${name}_${i}: ${ids[i]}\n`).join('');
  const text=(template.slice(0,start)+blocks+template.slice(end)).replace(/^guid: .*/m,`guid: ${guid}`)
    .replace(/textureCompression: \d+/g,'textureCompression: 0')
    .replace(/    nameFileIdTable:\r?\n[\s\S]*?(?=  mipmapLimitGroupName:)/,`    nameFileIdTable:\n${table}`);
  fs.writeFileSync(file,text.replace(/\r?\n/g,'\n'));
}
function font(im,txt,x,y,color=[200,210,226,255]) {
  const glyphs={
    A:['010','101','111','101','101'],B:['110','101','110','101','110'],C:['011','100','100','100','011'],D:['110','101','101','101','110'],E:['111','100','110','100','111'],F:['111','100','110','100','100'],G:['011','100','101','101','011'],H:['101','101','111','101','101'],I:['111','010','010','010','111'],J:['001','001','001','101','010'],K:['101','101','110','101','101'],L:['100','100','100','100','111'],M:['101','111','111','101','101'],N:['101','111','111','111','101'],O:['010','101','101','101','010'],P:['110','101','110','100','100'],Q:['010','101','101','011','001'],R:['110','101','110','101','101'],S:['011','100','010','001','110'],T:['111','010','010','010','010'],U:['101','101','101','101','111'],V:['101','101','101','101','010'],W:['101','101','111','111','101'],X:['101','101','010','101','101'],Y:['101','101','010','010','010'],Z:['111','001','010','100','111'],
    '0':['111','101','101','101','111'],'1':['010','110','010','010','111'],'2':['110','001','010','100','111'],'3':['110','001','010','001','110'],'4':['101','101','111','001','001'],'5':['111','100','110','001','110'],'6':['011','100','111','101','111'],'7':['111','001','010','010','010'],'8':['111','101','111','101','111'],'9':['111','101','111','001','110'],'/':['001','001','010','100','100'],'-':['000','000','111','000','000'],'.':['000','000','000','000','010'],':':['000','010','000','010','000']
  };
  for(const c of txt.toUpperCase()){const g=glyphs[c];if(g)g.forEach((r,j)=>[...r].forEach((v,i)=>{if(v==='1')set(im,x+i,y+j,color);}));x+=4;}
}
function preview(assets,sniper,citizen) {
  const im=image(352,480,[23,27,37,255]);
  font(im,'SNIPER / 64X64 / FEET ROW 61',12,9);
  let y=24;
  for(const [name,frames] of Object.entries(assets)) {
    if(!name.startsWith('sniper'))continue;
    font(im,name.replaceAll('_',' '),12,y);y+=10;
    frames.forEach((f,i)=>{
      paste(im,image(f.w,f.h,[31,37,49,255]),12+i*f.w,y);
      paste(im,f,12+i*f.w,y);
    });
    y+=70;
  }
  font(im,'BULLETS / 16X16 / SOURCE COPIES',12,y);y+=12;
  const names=['bullet_sniper_2f','bullet_android_2f','bullet_citizen_2f','bullet_bug_2f'];
  names.forEach((name,i)=>{const x=12+i*84;font(im,name.split('_')[1],x,y);paste(im,makeStrip(assets[name]),x,y+10);});
  y+=38;
  font(im,'SCALE / CITIZEN - SNIPER',12,y);y+=10;
  for(let x=12;x<140;x++)set(im,x,y+62,[75,84,104,255]);
  paste(im,citizen,12,y);paste(im,sniper,76,y);
  // Crop to content before the integer-only enlargement.
  const native=crop(im,0,0,352,y+72);
  pngWrite(path.join(__dirname,'preview_4x.png'),resize(native,native.w*4,native.h*4));
}
const files = {
  sniper: path.join(__dirname,'sniper_6f_gemini.png'), bullets:path.join(__dirname,'bullets_gemini.png'),
  citizen:path.join(out,'citizen_b_ranged_5f.png'), stunned:path.join(out,'citizen_stunned_1f.png')
};
if(process.argv.includes('--inspect')) {
  for(const [n,p] of Object.entries(files)) {
    const src=pngRead(p),im=n==='sniper'||n==='bullets'?removeGreen(src):src;
    console.log(n,src.w,src.h,'bounds',bbox(im),'components',components(im).slice(0,24).map(c=>({bounds:c.bounds,size:c.size})));
    if(n==='citizen')console.log('citizen frame 0',bbox(crop(im,0,0,64,64)),'colors',new Set(Array.from({length:im.w*im.h},(_,i)=>im.data.subarray(i*4,i*4+4).toString('hex'))).size);
  }
  const pal=pngRead(path.join(root,'Assets/Art/Palette/number_palette.png'));
  console.log('project palette',Array.from(new Set(Array.from({length:pal.w*pal.h},(_,i)=>pixel(pal,i%pal.w,Math.floor(i/pal.w)).slice(0,3).join(',')))));
  const citizen=pngRead(files.citizen);
  console.log('citizen palette',Array.from(new Set(Array.from({length:citizen.w*citizen.h},(_,i)=>pixel(citizen,i%citizen.w,Math.floor(i/citizen.w)).join(',')))));
}
if(process.argv.includes('--build')) {
  const assets={};
  const src=removeGreen(pngRead(files.sniper));
  const bounds=[[30,11,168,199],[196,11,342,199],[363,17,531,199],[548,17,711,199],[740,14,900,199],[898,25,1005,199]];
  const anchors=[70,241,417,602,778,947],scale=59/188;
  const bodyComponents=components(src).filter(c=>c.size>8000).sort((a,b)=>a.bounds[0]-b.bounds[0]);
  const sniper=bounds.map(([l,t,r,b],i)=>{
    // Component masks separate the overlapping bounding boxes of fire/stunned.
    const isolated=image(src.w,src.h);
    for(const p of bodyComponents[i].ps)isolated.data.set(src.data.subarray(p*4,p*4+4),p*4);
    const small=quantize(resize(crop(isolated,l,t,r-l,b-t),Math.round((r-l)*scale),Math.round((b-t)*scale)),colors.character);
    const f=image(64,64);paste(f,small,25-Math.round((anchors[i]-l)*scale),62-small.h);return f;
  });
  // Lower the recoiling forearm/rifle by two integer pixels so the projectile
  // origin remains at the aim/charge barrel height. Keep the original recoil.
  const fire=crop(sniper[4],0,0,64,64);
  for(let y=14;y<=30;y++)for(let x=35;x<64;x++)set(sniper[4],x,y,[0,0,0,0]);
  for(let y=14;y<=28;y++)for(let x=35;x<64;x++){const c=pixel(fire,x,y);if(c[3])set(sniper[4],x,y+2,c);}
  set(sniper[4],34,20,[98,103,125,255]);set(sniper[4],35,20,[133,138,155,255]);
  // A hot flash center reads as a flash rather than a red decorative dot.
  set(sniper[4],58,19,[255,212,59,255]);set(sniper[4],58,20,[214,215,208,255]);
  assets.sniper_aim_5f=sniper.slice(0,5);assets.sniper_stunned_1f=[sniper[5]];
  assets.sniper_walk_4f=walkFrames(sniper[0]);
  const bsrc=removeGreen(pngRead(files.bullets));
  function bullet(rect,w,h,palette) {const [l,t,r,b]=rect,small=quantize(resize(crop(bsrc,l,t,r-l,b-t),w,h),palette),f=image(16,16);paste(f,small,Math.floor((16-w)/2),Math.floor((16-h)/2));return f;}
  assets.bullet_sniper_2f=[bullet([84,41,435,100],12,3,colors.red),bullet([572,41,955,100],12,3,colors.red)];
  assets.bullet_android_2f=[bullet([196,160,307,271],10,10,colors.red),bullet([580,160,700,271],11,10,colors.red)];
  // Citizen copies differ subtly in shading. Copies 1/4 have the largest
  // pixel difference after reduction (33 channel differences vs. 12-24).
  const pellets=Array.from({length:4},(_,i)=>bullet([94+i*256,323,162+i*256,390],8,8,colors.amber));
  assets.bullet_citizen_2f=[pellets[0],pellets[3]];
  assets.bullet_bug_2f=[bullet([86,451,170,544],8,9,colors.magenta),bullet([598,451,682,544],8,9,colors.magenta)];
  for(const [name,frames]of Object.entries(assets)) {pngWrite(path.join(out,name+'.png'),makeStrip(frames));metaWrite(name,frames);}
  preview(assets,sniper[0],crop(pngRead(files.citizen),0,0,64,64));
  for(const [n,frames]of Object.entries(assets))console.log(n,frames.map(f=>bbox(f)));
  if(process.argv.includes('--pixels')) {
    for(const [label,f,rect]of [['stand',sniper[0],[12,42,36,62]],['fire',sniper[4],[26,14,64,30]],['aim',sniper[2],[26,14,64,30]]]) {
      console.log(label,'palette 0123456789ABCDEF');const[l,t,r,b]=rect;
      for(let y=t;y<b;y++){let line='';for(let x=l;x<r;x++){const c=pixel(f,x,y);line+=c[3]?colors.character.findIndex(p=>p.every((v,i)=>v===c[i])).toString(16).toUpperCase():'.';}console.log(y,line);}
    }
  }
}
if(process.argv.includes('--verify')) {
  const assert=require('node:assert/strict');
  const specs={sniper_aim_5f:[5,64,64],sniper_stunned_1f:[1,64,64],sniper_walk_4f:[4,64,64],bullet_sniper_2f:[2,16,16],bullet_android_2f:[2,16,16],bullet_citizen_2f:[2,16,16],bullet_bug_2f:[2,16,16]};
  const guids=new Set(),ids=new Set(),spriteIDs=new Set(),generatedMetas=new Set();
  for(const [name,[n,w,h]]of Object.entries(specs)) {
    const im=pngRead(path.join(out,name+'.png')),mf=path.join(out,name+'.png.meta'),m=fs.readFileSync(mf,'utf8').replace(/\r\n/g,'\n');generatedMetas.add(mf);
    assert.equal(im.w,w*n);assert.equal(im.h,h);
    let count=0;const palette=new Set();
    for(let i=0;i<im.w*im.h;i++) {
      const [r,g,b,a]=im.data.subarray(i*4,i*4+4);assert.ok(a===0||a===255,'hard alpha');
      if(!a)assert.equal(r+g+b,0,'transparent RGB must be zero');
      else {count++;palette.add(`${r},${g},${b}`);assert.ok(g<=Math.max(r,b)+4,'green spill');}
    }
    assert.ok(count);assert.ok(palette.size<=(name.startsWith('sniper')?16:7));
    const frameHashes=[];
    for(let i=0;i<n;i++) {
      const f=crop(im,i*w,0,w,h),bb=bbox(f);assert.ok(bb[0]>0&&bb[2]<w,'horizontal transparent padding');
      if(w===64)assert.equal(bb[3],62,'feet on row 61');
      else assert.ok(bb[2]-bb[0]>=6&&bb[2]-bb[0]<=12,'bullet visual width');
      frameHashes.push(crypto.createHash('sha256').update(f.data).digest('hex'));
    }
    assert.equal(new Set(frameHashes).size,n,'distinct animation frames');
    const guid=m.match(/^guid: ([a-f0-9]{32})$/m)?.[1];assert.ok(guid);assert.ok(!guids.has(guid));guids.add(guid);
    assert.match(m,/^    filterMode: 0$/m);assert.match(m,/^  enableMipMap: 0$|^    enableMipMap: 0$/m);
    assert.match(m,/^  spriteMode: 2$/m);assert.match(m,/^  spritePixelsToUnits: 32$/m);
    assert.equal([...m.matchAll(/^    textureCompression: 0$/gm)].length,3);
    const frameNames=[...m.matchAll(/^      name: (.+)$/gm)].map(m=>m[1]);assert.deepEqual(frameNames,Array.from({length:n},(_,i)=>`${name}_${i}`));
    const frameIds=[...m.matchAll(/^      internalID: (-?\d+)$/gm)].map(m=>m[1]);assert.equal(frameIds.length,n);
    const frameSIds=[...m.matchAll(/^      spriteID: ([a-f0-9]{32})$/gm)].map(m=>m[1]);assert.equal(frameSIds.length,n);
    const pivot=name.startsWith('bullet')?'0.5':'0.03125';
    assert.equal(m.split(`      pivot: {x: 0.5, y: ${pivot}}`).length-1,n);
    for(let i=0;i<n;i++) {
      assert.ok(!ids.has(frameIds[i]));ids.add(frameIds[i]);assert.ok(BigInt(frameIds[i])>0n&&BigInt(frameIds[i])<2n**63n);
      assert.ok(!spriteIDs.has(frameSIds[i]));spriteIDs.add(frameSIds[i]);
      assert.ok(m.includes(`      ${name}_${i}: ${frameIds[i]}\n`),'name table ID');
      const rect=`      name: ${name}_${i}\n      rect:\n        serializedVersion: 2\n        x: ${i*w}\n        y: 0\n        width: ${w}\n        height: ${h}\n`;
      assert.ok(m.includes(rect),'slice rect');
    }
    console.log('PASS',name,`${im.w}x${im.h}`,`${n} frames`,`${palette.size} colors`);
  }
  function otherMeta(dir) {
    for(const e of fs.readdirSync(dir,{withFileTypes:true})) {
      const p=path.join(dir,e.name);if(e.isDirectory())otherMeta(p);
      else if(e.name.endsWith('.meta')&&!generatedMetas.has(p)) {
        const m=fs.readFileSync(p,'utf8');
        for(const match of m.matchAll(/^guid: ([a-f0-9]+)/gm))assert.ok(!guids.has(match[1]),`GUID collision ${p}`);
        for(const match of m.matchAll(/^\s+internalID: (-?\d+)/gm))assert.ok(!ids.has(match[1]),`ID collision ${p}`);
        for(const match of m.matchAll(/^\s+spriteID: ([a-f0-9]+)/gm))assert.ok(!spriteIDs.has(match[1]),`sprite ID collision ${p}`);
      }
    }
  }
  otherMeta(path.join(root,'Assets'));
  const p=pngRead(path.join(__dirname,'preview_4x.png'));assert.equal(p.w%4,0);assert.equal(p.h%4,0);
  for(let y=0;y<p.h;y+=4)for(let x=0;x<p.w;x+=4)for(let dy=0;dy<4;dy++)for(let dx=0;dx<4;dx++)assert.deepEqual(pixel(p,x,y),pixel(p,x+dx,y+dy),'integer preview pixels');
  console.log('PASS unique GUIDs/internalIDs/spriteIDs across Assets; exact 4x nearest-neighbour preview');
  for(const [name,p]of Object.entries(files))console.log('READ-ONLY SHA256',name,crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex'));
}
