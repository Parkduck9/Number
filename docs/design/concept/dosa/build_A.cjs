// Plan A prop overlay. Node standard library only; never writes into Assets.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const {pngRead,pngWrite,image,pixel,set,paste,crop,resize,bbox} = require('./pixels.cjs');
const root = path.resolve(__dirname, '../../../..');
const input = path.join(root, 'Assets/Resources/DraftSprites');
const output = path.join(__dirname, 'A');
fs.mkdirSync(output, {recursive:true});
const rgba = hex => [...Buffer.from(hex,'hex'),255];
const palette = {o:rgba('080b14'),b:rgba('a37b70'),h:rgba('d7b4a0'),s:rgba('7a3a3c'),y:rgba('f2c94c'),r:rgba('7f1a22'),sash:rgba('96363c')};
// Each anchor is the top of the paper pair, followed by gourd top-left.
// These follow the belt in each original cel, without translating the hero.
const poses = {
 idle_4f: [[31,33,26,36],[31,33,26,36],[31,33,26,36],[31,33,26,36]],
 walk_6f: [[30,35,25,38],[30,35,25,38],[30,34,25,38],[30,38,25,39],[30,38,25,39],[31,35,26,39]],
 run_6f: [[31,38,26,39],[31,39,26,40],[32,39,27,40],[31,36,26,37],[31,38,26,39],[32,40,27,40]],
 attack_5f: [[28,36,23,38],[28,36,23,38],[29,37,24,40],[29,37,24,40],[30,35,25,37]],
 jump_4f: [[30,36,25,38],[30,36,25,38],[31,31,26,33],[31,30,26,32]],
 dash_3f: [[32,40,27,42],[32,40,27,42],[28,42,23,43]],
 parry_2f: [[29,34,24,38],[24,33,19,36]],
 hurt_1f: [[32,41,27,43]],
 grapple_1f: [[30,36,25,38]],
};
const srcs={}, dsts={}, report=[];
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
// Resume safely: retain completed PNG files and check they match this build.
function finishPng(file,im){
 if(!fs.existsSync(file)){pngWrite(file,im);return;}
 const existing=pngRead(file);
 if(existing.w!==im.w||existing.h!==im.h||!existing.data.equals(im.data))throw Error('Existing PNG differs: '+file);
}
for(const [name,anchors] of Object.entries(poses)) {
 const filename='hero_'+name+'.png', sourceFile=path.join(input,filename);
 const original=pngRead(sourceFile), dest=crop(original,0,0,original.w,original.h);
 if(original.w!==anchors.length*64||original.h!==64)throw Error('Unexpected frame layout '+name);
 const rows=[];
 anchors.forEach(([tx,ty,gx,gy],f)=>{
  const src=crop(original,f*64,0,64,64), dst=crop(src,0,0,64,64);
  const protect=(x,y)=>{
   const c=pixel(src,x,y), hex=Buffer.from(c.slice(0,3)).toString('hex');
   // Skin stays in front of the hanging props, including grasping hands.
   return ['a37b70','d7b4a0','efd0b6'].includes(hex);
  };
  const ink=(x,y,col)=>{
   if(x<0||x>=64||y<0||y>=64||!pixel(src,x,y)[3]||protect(x,y))return;
   set(dst,x,y,col);
  };
  // Select only the sash-connected muted red cluster, excluding face and hands.
  const candidates=[];
  for(let y=ty-2;y<56;y++)for(let x=8;x<46;x++){
   const c=pixel(src,x,y);
   if(c[3]&&c[0]===122&&c[1]===58&&c[2]===60)candidates.push([x,y]);
  }
  const seen=new Set(),groups=[];
  const points=new Set(candidates.map(([x,y])=>x+64*y));
  for(const [x,y] of candidates){
   const key=x+64*y;if(seen.has(key))continue;
   const stack=[key],group=[];seen.add(key);
   while(stack.length){const k=stack.pop();group.push(k);for(const d of [-65,-64,-63,-1,1,63,64,65])if(points.has(k+d)&&!seen.has(k+d)){seen.add(k+d);stack.push(k+d);}}
   groups.push(group);
  }
  groups.sort((a,b)=>b.length-a.length);
  for(const k of groups[0]||[])set(dst,k%64,Math.floor(k/64),palette.sash);
  // Dark cord and a 5x8 two-lobed gourd, with top-left light shared with skin.
  for(let y=ty;y<gy;y++)ink(gx+2,y,palette.o);
  const gourd=['..s..','.obo.','.hbo.','..o..','.hbbo','hbbso','hbbso','.oso.'];
  gourd.forEach((row,y)=>[...row].forEach((key,x)=>{if(key!=='.')ink(gx+x,gy+y,palette[key]);}));
  // Two 3px-wide papers, separated by a 1px dark gap. Attachment stays fixed.
  const flutter=/^(walk|run)/.test(name)?[0,-1,-1,0,1,1][f]:0;
  for(let t=0;t<2;t++){
   const x0=tx+t*4, y0=ty+(t?1:0), height=t?5:6;
   for(let y=0;y<height;y++){
    const shift=y>=3?flutter:0;
    for(let x=0;x<3;x++)ink(x0+x+shift,y0+y,palette.y);
    if(y===1||y===3)ink(x0+1+shift,y0+y,palette.r);
   }
  }
  let changed=0,alphaMismatch=0; const bounds=[64,64,-1,-1];
  for(let y=0;y<64;y++)for(let x=0;x<64;x++){
   const a=pixel(src,x,y),b=pixel(dst,x,y);
   if(a[3]!==b[3])alphaMismatch++;
   if(b[3]!==0&&b[3]!==255)throw Error('Soft alpha');
   if(a.some((v,i)=>v!==b[i])){changed++;bounds[0]=Math.min(bounds[0],x);bounds[1]=Math.min(bounds[1],y);bounds[2]=Math.max(bounds[2],x);bounds[3]=Math.max(bounds[3],y);}
  }
  if(alphaMismatch)throw Error('Silhouette changed');
  if(bounds[1]<29||bounds[3]>55)throw Error('Edit outside waist/hip/sash');
  paste(dest,dst,f*64,0);
  rows.push({frame:f,changedPixels:changed,editBounds:bounds,alphaMismatch,sourceBounds:bbox(src),outputBounds:bbox(dst),flutter});
 });
 finishPng(path.join(output,filename),dest);
 srcs[name]=original;dsts[name]=dest;
 report.push({filename,dimensions:[dest.w,dest.h],sourceSha256:hash(fs.readFileSync(sourceFile)),outputSha256:hash(fs.readFileSync(path.join(output,filename))),frames:rows});
}
const bg=[39,43,52,255],light=[184,190,200,255];
const font={A:['010','101','111','101','101'],C:['011','100','100','100','011'],D:['110','101','101','101','110'],E:['111','100','110','100','111'],G:['011','100','101','101','011'],I:['111','010','010','010','111'],K:['101','101','110','101','101'],L:['100','100','100','100','111'],N:['101','111','111','111','101'],O:['010','101','101','101','010'],P:['110','101','110','100','100'],R:['110','101','110','101','101'],T:['111','010','010','010','010'],U:['101','101','101','101','111'],W:['101','101','111','111','101']};
function label(im,text,x,y){for(const ch of text){if(font[ch])font[ch].forEach((row,j)=>[...row].forEach((bit,i)=>{if(bit==='1')set(im,x+i,y+j,light);}));x+=4;}}
const preview=image(424,444,bg);
['idle_4f','run_6f','attack_5f'].forEach((name,i)=>{
 const y=i*148;label(preview,name.split('_')[0].toUpperCase(),40,y+2);
 label(preview,'ORIGINAL',2,y+36);label(preview,'PLAN A',2,y+104);
 paste(preview,srcs[name],40,y+12);paste(preview,dsts[name],40,y+80);
});
finishPng(path.join(__dirname,'A_preview_4x.png'),resize(preview,preview.w*4,preview.h*4));
finishPng(path.join(__dirname,'A_preview_1x.png'),preview);
// Three independent cycles; 60 ticks is LCM(4,6,5), so each loops exactly at 10fps.
const gifFrames=[];
for(let tick=0;tick<60;tick++){
 const im=image(232,148,bg);
 label(im,'ORIGINAL',0,38);label(im,'PLAN A',0,108);
 ['idle_4f','run_6f','attack_5f'].forEach((name,col)=>{
  const x=40+col*64,f=tick%(srcs[name].w/64);
  label(im,name.split('_')[0].toUpperCase(),x+4,2);
  paste(im,crop(srcs[name],f*64,0,64,64),x,12);
  paste(im,crop(dsts[name],f*64,0,64,64),x,80);
 });
 gifFrames.push(resize(im,im.w*4,im.h*4));
}
function gifWrite(file,frames){
 const colors=[],map=new Map();
 for(const im of frames)for(let i=0;i<im.data.length;i+=4){const key=im.data.subarray(i,i+3).toString('hex');if(!map.has(key)){map.set(key,colors.length);colors.push([...im.data.subarray(i,i+3)]);}}
 const bits=Math.max(2,Math.ceil(Math.log2(colors.length))),size=1<<bits;
 const u16=n=>Buffer.from([n&255,n>>8]);
 const global=Buffer.alloc(size*3);colors.forEach((c,i)=>global.set(c,i*3));
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
   emit(prefix);
   if(next>(1<<width)-1&&width<12)width++;
   if(next<4096)dict.set(key,next++);else{emit(clear);reset();}
   prefix=k;
  }
  emit(prefix);if(next>(1<<width)-1&&width<12)width++;emit(end);if(count)bytes.push(acc&255);
  chunks.push(Buffer.from([0x21,0xf9,4,4,10,0,0,0,0x2c]),u16(0),u16(0),u16(im.w),u16(im.h),Buffer.from([0,bits]));
  for(let i=0;i<bytes.length;i+=255){const part=bytes.slice(i,i+255);chunks.push(Buffer.from([part.length]),Buffer.from(part));}chunks.push(Buffer.from([0]));
 }
 chunks.push(Buffer.from([0x3b]));fs.writeFileSync(file,Buffer.concat(chunks));
 return {frames:frames.length,dimensions:[frames[0].w,frames[0].h],fps:10,loop:0,palette:colors.length,rgbaSha256:frames.map(im=>hash(im.data))};
}
const animation=gifWrite(path.join(__dirname,'A_preview_4x.gif'),gifFrames);
const sharedColors=new Set();for(const im of Object.values(dsts))for(let i=0;i<im.data.length;i+=4)if(im.data[i+3])sharedColors.add(im.data.subarray(i,i+3).toString('hex'));
fs.writeFileSync(path.join(output,'validation.json'),JSON.stringify({frameSize:[64,64],sharedPalette:[...sharedColors],animation,strips:report},null,2)+'\n');
const {rgbaSha256,...animationSummary}=animation;
console.log(JSON.stringify({strips:report.length,frames:report.reduce((n,r)=>n+r.frames.length,0),sharedPalette:sharedColors.size,animation:animationSummary}));
