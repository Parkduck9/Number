// Resume Plan B from the supplied generated drawings. Node standard library only.
// Authoring/export utility: writes only concept staging files, never Assets.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const {pngRead,pngWrite,image,pixel,set,paste,crop,resize,bbox,components} = require('./pixel_io.cjs');
const out = __dirname, root = path.resolve(out, '../../../../..');
const input = path.join(root, 'Assets/Resources/DraftSprites');
const rgba = h => [...Buffer.from(h,'hex'),255];
const hexes = ['080b14','1a1d33','2d3050','3f4a6e','62677d','d6d7d0','a37b70','d7b4a0','efd0b6','226076','42a9bd','7f1a22','b94445','f2c94c','9b7043','c49a5c'];
const colors = hexes.map(rgba);
const transparent = [0,0,0,0];
const sources={}, finals={}, records=[];
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
function nearest(c){
 let best=Infinity,index=0;
 colors.forEach((p,i)=>{const d=(c[0]-p[0])**2*.3+(c[1]-p[1])**2*.4+(c[2]-p[2])**2*.3;if(d<best){best=d;index=i;}});
 return index;
}
function mainComponent(im){
 for(let i=0;i<im.data.length;i+=4)if(im.data[i+3]<128)im.data.fill(0,i,i+4);else im.data[i+3]=255;
 const cs=components(im), clean=image(im.w,im.h);
 for(const k of cs[0].ps)clean.data.set(im.data.subarray(k*4,k*4+4),k*4);
 return clean;
}
// Each target pixel selects one actual palette color from its source cell.
// Majority coverage controls alpha; no interpolation, dithering, or soft edges.
function reduce(im,w,h){
 const dst=image(w,h);
 for(let y=0;y<h;y++)for(let x=0;x<w;x++){
  const x0=Math.floor(x*im.w/w),x1=Math.floor((x+1)*im.w/w),y0=Math.floor(y*im.h/h),y1=Math.floor((y+1)*im.h/h);
  const hist=new Uint32Array(colors.length);let opaque=0,total=0;
  for(let sy=y0;sy<y1;sy++)for(let sx=x0;sx<x1;sx++){
   const c=pixel(im,sx,sy);total++;if(c[3]){hist[nearest(c)]++;opaque++;}
  }
  if(opaque/total>=.46){let index=0;for(let k=1;k<hist.length;k++)if(hist[k]>hist[index])index=k;set(dst,x,y,colors[index]);}
 }
 return dst;
}
function finishDetails(cel,waist){
 // Keep warm flask colors out of skin; the hood/head shares the original skin ramp.
 for(let y=0;y<waist-4;y++)for(let x=0;x<64;x++){
  const c=pixel(cel,x,y),h=Buffer.from(c.slice(0,3)).toString('hex');
  if(c[3]&&['9b7043','c49a5c'].includes(h))set(cel,x,y,colors[6]);
 }
 const mask=image(64,64);
 for(let y=waist-2;y<57;y++)for(let x=0;x<64;x++)if(pixel(cel,x,y)[3]&&pixel(cel,x,y).slice(0,3).every((v,i)=>v===colors[13][i]))set(mask,x,y,colors[13]);
 for(const c of components(mask)){
  if(c.size<4)continue;
  const [l,t,r,b]=c.bounds;
  for(let y=t+1;y<b-1;y+=3){
   const xs=[];for(let x=l;x<r;x++)if(pixel(mask,x,y)[3])xs.push(x);
   if(xs.length>=2)set(cel,xs[Math.floor(xs.length/2)],y,colors[11]);
  }
 }
 // A single dark stepped fracture stays readable in a 2-3px-tall teal lens.
 const lens=[];
 for(let y=4;y<waist-7;y++)for(let x=0;x<64;x++){
  const c=pixel(cel,x,y);if(c[3]&&c[0]===66&&c[1]===169&&c[2]===189)lens.push([x,y]);
 }
 if(lens.length){
  const l=Math.min(...lens.map(p=>p[0])),r=Math.max(...lens.map(p=>p[0])),t=Math.min(...lens.map(p=>p[1]));
  const x=Math.round((l+r)/2);
  for(const [dx,dy] of [[0,0],[1,1]])if(pixel(cel,x+dx,t+dy)[3]&&pixel(cel,x+dx,t+dy)[1]>=96)set(cel,x+dx,t+dy,colors[9]);
 }
}
const core={idle_4f:'idle_generated.png',run_6f:'run_generated.png',attack_5f:'attack_generated.png'};
for(const [name,generatedFile] of Object.entries(core)){
 const file='hero_'+name+'.png',source=pngRead(path.join(input,file)),generated=pngRead(path.join(out,generatedFile)),dest=image(source.w,64),frames=[];
 const n=source.w/64;
 for(let f=0;f<n;f++){
  const original=crop(source,f*64,0,64,64),target=bbox(original);
  const segment=crop(generated,Math.floor(f*generated.w/n),0,Math.floor((f+1)*generated.w/n)-Math.floor(f*generated.w/n),generated.h);
  const clean=mainComponent(segment),b=bbox(clean),height=target[3]-target[1],width=Math.round((b[2]-b[0])*height/(b[3]-b[1]));
  const reduced=reduce(crop(clean,b[0],b[1],b[2]-b[0],b[3]-b[1]),width,height);
  const x=Math.round((target[0]+target[2]-width)/2),cel=image(64,64);
  paste(cel,reduced,x,target[1]);
  finishDetails(cel,name==='idle_4f'?34:name==='run_6f'?38:36);
  paste(dest,cel,f*64,0);
  frames.push({frame:f,sourceBounds:target,outputBounds:bbox(cel),generatedBounds:[b[0]+Math.floor(f*generated.w/n),b[1],b[2]+Math.floor(f*generated.w/n),b[3]]});
 }
 pngWrite(path.join(out,file),dest);sources[name]=source;finals[name]=dest;
 records.push({filename:file,method:'full redraw (resumed generated artwork, palette and grid cleanup)',sourceSha256:hash(fs.readFileSync(path.join(input,file))),frames});
}
// Secondary poses retain the original anatomy and motion. Outfit patches are
// intentionally identified separately from the three new drawings above.
const extra={
 walk_6f:[[30,35,25,38,30,15,8],[30,35,25,38,29,15,8],[30,34,25,38,29,14,8],[30,38,25,39,30,15,8],[30,38,25,39,30,15,8],[31,35,26,39,29,14,8]],
 jump_4f:[[30,36,25,38,32,20,8],[30,36,25,38,32,18,8],[31,31,26,33,32,14,8],[31,30,26,32,32,12,8]],
 dash_3f:[[32,40,27,42,44,24,9],[32,40,27,42,46,26,9],[28,42,23,43,46,29,10]],
 parry_2f:[[29,34,24,38,28,14,8],[24,33,19,36,25,14,8]],
 hurt_1f:[[32,41,27,43,24,21,8]],
 grapple_1f:[[30,36,25,38,37,20,8]],
};
function line(im,x0,y0,x1,y1,col,width=1){
 const dx=Math.abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.abs(y1-y0),sy=y0<y1?1:-1;let err=dx+dy;
 for(;;){for(let j=0;j<width;j++)set(im,x0+j,y0,col);if(x0===x1&&y0===y1)break;const e=2*err;if(e>=dy){err+=dy;x0+=sx;}if(e<=dx){err+=dx;y0+=sy;}}
}
function polygon(im,pts,col,allowed){
 for(let y=Math.min(...pts.map(p=>p[1]));y<=Math.max(...pts.map(p=>p[1]));y++)for(let x=Math.min(...pts.map(p=>p[0]));x<=Math.max(...pts.map(p=>p[0]));x++){
  let inside=false;
  for(let i=0,j=pts.length-1;i<pts.length;j=i++)if((pts[i][1]>y)!==(pts[j][1]>y)&&x<(pts[j][0]-pts[i][0])*(y-pts[i][1])/(pts[j][1]-pts[i][1])+pts[i][0])inside=!inside;
  if(inside&&(!allowed||allowed(x,y)))set(im,x,y,col);
 }
}
for(const [name,anchors] of Object.entries(extra)){
 const file='hero_'+name+'.png',source=pngRead(path.join(input,file)),dest=image(source.w,64),frames=[];
 anchors.forEach(([bx,by,gx,gy,ex,ey,ew],f)=>{
  const src=crop(source,f*64,0,64,64),cel=image(64,64);
  // Headband ends and sheathed sword sit behind the original pose.
  line(cel,ex-3,ey+1,ex-8,ey+3,colors[1],2);
  line(cel,ex-3,ey+2,ex-7,ey+5,colors[2],2);
  const sx=bx-12,sy=Math.min(bbox(src)[3]-4,by+15);
  line(cel,bx-2,by+1,sx,sy,colors[0],3);
  line(cel,bx-1,by+2,sx+1,sy-1,colors[3]);
  line(cel,sx+1,sy,sx,Math.min(60,sy+3),colors[11],2);
  for(let y=0;y<64;y++)for(let x=0;x<64;x++){
   const c=pixel(src,x,y);if(!c[3])continue;
   let index=nearest(c);
   const h=Buffer.from(c.slice(0,3)).toString('hex');
   // Remove the technical cyan shoulder patch and mute sleeve armor.
   if(y>ey+6&&y<by&&['42a9bd','226076'].includes(h))index=3;
   if(y>ey+7&&y<by-2&&['858a9b','b8bec8','62677d'].includes(h))index=3;
   // The long red sash becomes the dark pants/short coat beneath the belt.
   if(y>=by-2&&['7a3a3c','7f1a22'].includes(h))index=y<by+8?2:1;
   set(cel,x,y,colors[index]);
  }
  const protect=(x,y)=>{
   const c=pixel(src,x,y),h=Buffer.from(c.slice(0,3)).toString('hex');
   return ['a37b70','d7b4a0','efd0b6'].includes(h);
  };
  const cloth=(x,y)=>pixel(src,x,y)[3]&&!protect(x,y);
  // Two short overlap panels replace the long light undershirt and sash.
  polygon(cel,[[bx-5,by],[bx+3,by],[bx+7,by+7],[bx+3,by+9],[bx,by+5],[bx-3,by+9],[bx-8,by+7]],colors[2],cloth);
  polygon(cel,[[bx-5,by+1],[bx-1,by+1],[bx-3,by+7],[bx-7,by+6]],colors[3],cloth);
  line(cel,bx,by+3,bx-2,by+8,colors[1]);
  // Belt is a flat dark tie; source hands stay in front.
  for(let y=by-1;y<=by;y++)for(let x=bx-5;x<bx+7;x++)if(cloth(x,y))set(cel,x,y,colors[y===by?1:4]);
  const flask=['..b..','.hbo.','..o..','.hbbo','hbbbo','hbbbo','.ooo.'];
  const key={s:colors[1],b:colors[14],h:colors[15],o:colors[0]};
  flask.forEach((row,y)=>[...row].forEach((c,x)=>{if(c!=='.'&&!protect(gx-3+x,gy-1+y))set(cel,gx-3+x,gy-1+y,key[c]);}));
  for(let paper=0;paper<2;paper++)for(let y=0;y<7-paper;y++){
   const dx=paper*4+(y>3&&/walk|dash/.test(name)?[0,1,0,-1,0,1][f]:0),dy=paper;
   for(let x=0;x<3;x++)if(!protect(bx+x+dx,by+1+y+dy))set(cel,bx+x+dx,by+1+y+dy,colors[13]);
  }
  // An 8-10px AR visor follows each original face angle; same stepped crack.
  for(let y=0;y<3;y++)for(let x=0;x<ew;x++)if(pixel(src,ex+x,ey+y)[3])set(cel,ex+x,ey+y,colors[y===0||x===0||x===ew-1?9:10]);
  set(cel,ex+Math.floor(ew/2),ey+1,colors[9]);
  set(cel,ex+Math.floor(ew/2)+1,ey+2,colors[9]);
  // Restore original hands over the attached belt props.
  for(let y=by-3;y<Math.min(64,by+12);y++)for(let x=0;x<64;x++)if(protect(x,y))set(cel,x,y,colors[nearest(pixel(src,x,y))]);
  finishDetails(cel,by);
  paste(dest,cel,f*64,0);
  frames.push({frame:f,sourceBounds:bbox(src),outputBounds:bbox(cel)});
 });
 pngWrite(path.join(out,file),dest);sources[name]=source;finals[name]=dest;
 records.push({filename:file,method:'patched original (coat panels, sleeves, goggles/headband, belt props, sword/tassel)',sourceSha256:hash(fs.readFileSync(path.join(input,file))),frames});
}
const bg=[39,41,52,255];
const font={A:['010','101','111','101','101'],B:['110','101','110','101','110'],C:['011','100','100','100','011'],D:['110','101','101','101','110'],E:['111','100','110','100','111'],G:['011','100','101','101','011'],H:['101','101','111','101','101'],I:['111','010','010','010','111'],J:['001','001','001','101','010'],K:['101','101','110','101','101'],L:['100','100','100','100','111'],N:['101','111','111','111','101'],O:['010','101','101','101','010'],P:['110','101','110','100','100'],R:['110','101','110','101','101'],T:['111','010','010','010','010'],U:['101','101','101','101','111'],W:['101','101','111','111','101'],Y:['101','101','010','010','010']};
font.M=['101','111','111','101','101'];font.S=['011','100','010','001','110'];
function label(im,text,x,y){
 for(const ch of text){if(font[ch])font[ch].forEach((row,j)=>[...row].forEach((bit,i)=>{if(bit==='1')set(im,x+i,y+j,colors[5]);}));x+=4;}
}
function preview(names,file){
 const im=image(424,names.length*148,bg);
 names.forEach((key,row)=>{
  const y=row*148;label(im,key.split('_')[0].toUpperCase(),40,y+2);
  label(im,'OLD',4,y+37);label(im,'NEW B',4,y+105);
  paste(im,sources[key],40,y+12);paste(im,finals[key],40,y+80);
 });
 pngWrite(file.replace('_4x','_1x'),im);
 pngWrite(file,resize(im,im.w*4,im.h*4));
}
preview(Object.keys(core),path.join(out,'../B_preview_4x.png'));
preview(Object.keys(extra),path.join(out,'secondary_preview_4x.png'));
// All cycles play concurrently for LCM(4,6,5)=60 ticks, with no duplicated holds.
const gifFrames=[];
for(let tick=0;tick<60;tick++){
 const im=image(232,148,bg);label(im,'OLD',4,37);label(im,'NEW B',4,105);
 Object.keys(core).forEach((key,col)=>{
  const x=40+64*col,frame=tick%(finals[key].w/64);
  label(im,key.split('_')[0].toUpperCase(),x+4,2);
  paste(im,crop(sources[key],frame*64,0,64,64),x,12);
  paste(im,crop(finals[key],frame*64,0,64,64),x,80);
 });
 gifFrames.push(resize(im,im.w*4,im.h*4));
}
function gifWrite(file,frames){
 const palette=[],map=new Map();
 for(const im of frames)for(let i=0;i<im.data.length;i+=4){const key=im.data.subarray(i,i+3).toString('hex');if(!map.has(key)){map.set(key,palette.length);palette.push([...im.data.subarray(i,i+3)]);}}
 const bits=Math.max(2,Math.ceil(Math.log2(palette.length))),size=1<<bits;
 if(bits>8)throw Error('GIF palette overflow');
 const u16=n=>Buffer.from([n&255,n>>8]);
 const global=Buffer.alloc(size*3);palette.forEach((c,i)=>global.set(c,i*3));
 const chunks=[Buffer.from('GIF89a'),u16(frames[0].w),u16(frames[0].h),Buffer.from([0x80|((bits-1)<<4)|(bits-1),0,0]),global,Buffer.from([0x21,0xff,0x0b]),Buffer.from('NETSCAPE2.0'),Buffer.from([3,1,0,0,0])];
 for(const im of frames){
  const clear=1<<bits,end=clear+1;let dict=new Map(),next=end+1,width=bits+1,acc=0,count=0,bytes=[];
  function emit(code){acc|=code<<count;count+=width;while(count>=8){bytes.push(acc&255);acc>>>=8;count-=8;}}
  function reset(){dict=new Map();next=end+1;width=bits+1;}
  const indices=new Uint8Array(im.w*im.h);for(let i=0;i<indices.length;i++)indices[i]=map.get(im.data.subarray(i*4,i*4+3).toString('hex'));
  emit(clear);let prefix=indices[0];
  for(let i=1;i<indices.length;i++){
   const k=indices[i],key=prefix*256+k;
   if(dict.has(key)){prefix=dict.get(key);continue;}
   emit(prefix);if(next>(1<<width)-1&&width<12)width++;
   if(next<4096)dict.set(key,next++);else{emit(clear);reset();}prefix=k;
  }
  emit(prefix);if(next>(1<<width)-1&&width<12)width++;emit(end);if(count)bytes.push(acc&255);
  chunks.push(Buffer.from([0x21,0xf9,4,4,10,0,0,0,0x2c]),u16(0),u16(0),u16(im.w),u16(im.h),Buffer.from([0,bits]));
  for(let i=0;i<bytes.length;i+=255){const part=bytes.slice(i,i+255);chunks.push(Buffer.from([part.length]),Buffer.from(part));}chunks.push(Buffer.from([0]));
 }
 chunks.push(Buffer.from([0x3b]));fs.writeFileSync(file,Buffer.concat(chunks));
 return {frames:frames.length,dimensions:[frames[0].w,frames[0].h],fps:10,delayMilliseconds:100,loop:0,seconds:6,paletteColors:palette.length,decodedRgbaSha256:frames.map(im=>hash(im.data))};
}
const animation=gifWrite(path.join(out,'../B_preview_4x.gif'),gifFrames);
for(const r of records){
 const im=finals[r.filename.replace(/^hero_/,'').replace(/\.png$/,'')],used=new Set();
 for(let i=0;i<im.data.length;i+=4){
  const alpha=im.data[i+3];if(alpha!==0&&alpha!==255)throw Error('Soft alpha '+r.filename);
  if(alpha){const h=im.data.subarray(i,i+3).toString('hex');if(!hexes.includes(h))throw Error('Off palette '+h);used.add(h);}
 }
 if(im.w!==r.frames.length*64||im.h!==64)throw Error('Incorrect strip dimensions');
 if(hash(fs.readFileSync(path.join(input,r.filename)))!==r.sourceSha256)throw Error('Source changed during export');
 r.dimensions=[im.w,im.h];r.opaqueColors=used.size;r.outputSha256=hash(fs.readFileSync(path.join(out,r.filename)));
}
const validation={frameSize:[64,64],idleHeightPixels:59,groundedFootRow:61,airbornePlacement:'Original per-frame height and vertical placement retained; run frame 3 remains airborne.',palette:hexes,alpha:[0,255],animation,strips:records};
fs.writeFileSync(path.join(out,'validation.json'),JSON.stringify(validation,null,2)+'\n');
console.log(JSON.stringify({strips:records.map(r=>({file:r.filename,method:r.method,dimensions:r.dimensions,colors:r.opaqueColors})),animation:{frames:60,fps:10,loop:0}}));
