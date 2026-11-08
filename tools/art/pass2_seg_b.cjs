#!/usr/bin/env node
// Deterministic recolour of the already aligned Pass 1 raster. No geometry resampling.
const fs=require('node:fs'),cp=require('node:child_process');
const {readPNG,writePNG,blank,platforms}=require('./pixelize_bg.cjs');
const root='docs/design/concept/stg01/',dir=root+'neon_B/';
fs.mkdirSync(dir,{recursive:true});
const src=readPNG(root+'B_clean_native.png'),off=blank(1620,540);
const inside=(x,y,r)=>x>=r[0]&&y>=r[1]&&x<r[0]+r[2]&&y<r[1]+r[3];
const lamps=[[23,97,29,39],[132,116,31,40],[79,116,26,40],[494,261,25,38],[549,263,25,38],
 [9,402,28,52],[196,406,28,43],[369,408,30,42],[480,413,22,35],[508,420,28,40],
 [634,408,29,45],[798,409,30,45],[1348,270,28,35],[1408,272,27,35],[1570,272,28,36],
 [1344,388,32,34],[1571,388,30,34],[1442,344,16,27],[1458,339,16,26],[1477,327,23,31]];
const lattice=[[48,289,291,58],[1050,352,300,40],[13,427,103,43],[396,433,83,42],[669,433,148,42]];
const tubeGlass=[[121,408,59,21],[419,413,57,21],[705,410,80,23]];
const clamp=v=>Math.max(0,Math.min(255,Math.round(v)));
// Keep silhouettes and every texture coordinate; only replace RGB values.
for(let y=0;y<540;y++)for(let x=0;x<1620;x++){
 const i=(y*1620+x)*4,[r,g,b]=src.data.subarray(i,i+3),v=Math.max(r,g,b),lo=Math.min(r,g,b);
 const near=y>=300||(x<435&&y>=198)||(x>=1292&&y>=194);
 const cyan=g>r*1.25&&b>r*1.2&&g>140;
 const pink=r>g*1.25&&b>g*1.15;
 const warm=r>g*1.04&&g>b*1.16;
 const lamp=lamps.some(a=>inside(x,y,a));
 const window=lattice.some(a=>inside(x,y,a));
 let c;
 if(lamp&&v>100&&(cyan||warm||lo>100)){
  c=[79+v*.13,77+v*.12,75+v*.11];
 }else if(tubeGlass.some(a=>inside(x,y,a))&&g>r*1.25&&b>r*1.2&&v>95){
  c=[36+v*.08,47+v*.08,59+v*.07];
 }else if(!near){
  // Natural moon remains; every artificial window/panel is dark, including old halos.
  if(Math.hypot(x-1215,y-49)<48&&v>120)c=v>235?[204,228,245]:[v*.72,v*.79,v*.86];
  else if(cyan)c=[24+v*.055,42+v*.075,58+v*.08];
  else if(pink)c=[37+v*.07,37+v*.045,54+v*.065];
  else if(warm)c=[46+v*.085,42+v*.06,49+v*.035];
  else c=[r*.82,g*.8,b*.87];
 }else if(cyan){
  c=[42+v*.06,57+v*.065,66+v*.06];
 }else if(lamp&&(warm||lo>115)){
  // Unlit paper, no white/amber centre; retain ribs through source-value variation.
  c=[83+v*.15,79+v*.125,74+v*.10];
 }else if(window&&warm){
  c=[63+v*.14,59+v*.10,62+v*.08];
 }else if(pink){
  if(x<165&&y<350)c=[60+v*.27,57+v*.235,67+v*.24]; // dusty pink-grey blossom
  else c=[53+v*.23,45+v*.13,65+v*.15]; // wine/indigo cloth
 }else if(warm){
  c=[56+v*.43,51+g*.40,54+b*.36]; // subdued brown wood / food / trim
 }else{
  // Lift near material midtones while retaining structural dark outlines.
  const target=v<55?28+v*.55:60+v*.54;
  const scale=target/Math.max(1,v),grey=(r+g+b)/3;
  c=[(r*.76+grey*.24)*scale,(g*.76+grey*.24)*scale,(b*.76+grey*.24)*scale];
 }
 off.data.set([...c.map(clamp),255],i);
}
function twice(im){const out=blank(im.w*2,im.h*2);for(let y=0;y<out.h;y++)for(let x=0;x<out.w;x++){
 const i=((y>>1)*im.w+(x>>1))*4;out.data.set(im.data.subarray(i,i+4),(y*out.w+x)*4);}return out;}
writePNG(root+'B_base_off_hires.png',twice(off));
cp.execFileSync(process.execPath,['tools/art/pixelize_bg.cjs',root+'B_base_off_hires.png',root+'B_base_off_native.png','--colors','48','--no-moon-anchor'],{stdio:'pipe'});
const base=readPNG(root+'B_base_off_native.png');
// Deliver hires as an exact integer 2x master of the final palette, no invented detail.
writePNG(root+'B_base_off_hires.png',twice(base));
const half={...base,data:base.data.slice()},composite={...base,data:base.data.slice()};
for(let i=0;i<half.data.length;i+=4)for(let c=0;c<3;c++)half.data[i+c]=Math.round(half.data[i+c]*.5);
writePNG(root+'B_base_off_preview_50pct.png',half);
// Masks follow the original lit glass pixels, ensuring each tube has an OFF counterpart.
const teal=[60,255,219],red=[255,74,60],gold=[255,179,71],white=[255,248,232];
const specs=[
 ['Skyline left billboard',[429,111,142,49],'cyan',teal,192,.3,'#8AE0C8','outline'],
 ['Skyline right billboard',[1499,102,74,61],'warm',gold,192,0,'#FFD0A0','outline'],
 ['Skyline middle panel',[788,240,105,14],'cyan',red,128,0,'#FF9B87','solid'],
 ['Left post glass',[80,119,24,36],'cyan',teal,80,0,'#8AE0C8','outline'],
 ['Middle post glass',[495,262,24,36],'cyan',teal,80,0,'#8AE0C8','outline'],
 ['Stall 1 edge tube',[124,412,53,13],'cyan',teal,80,0,'#8AE0C8','solid'],
 ['Stall 2 edge tube',[423,417,49,13],'cyan',teal,80,.3,'#8AE0C8','solid'],
 ['Stall 3 edge tube',[709,414,72,15],'cyan',teal,96,0,'#8AE0C8','solid'],
 ['Skyline strip left',[276,73,21,104],'cyan',gold,0,0,'#FFD0A0','solid'],
 ['Skyline strip centre',[738,128,21,88],'cyan',teal,0,0,'#8AE0C8','solid'],
 ['Skyline strip right',[1335,164,67,14],'pink',red,0,0,'#FF9B87','solid']
];
const bayer=[[0,8,2,10],[12,4,14,6],[3,11,1,9],[15,7,13,5]],records=[];
for(let n=0;n<specs.length;n++){
 const [label,rect,kind,tube,radius,flicker,light,mode]=specs[n];
 const [sx,sy,w,h]=rect,pad=4,W=w+pad*2,H=h+pad*2;
 const glass=new Uint8Array(W*H),seed=new Uint8Array(W*H);
 for(let y=0;y<h;y++)for(let x=0;x<w;x++){
  const i=((sy+y)*1620+sx+x)*4,[r,g,b]=src.data.subarray(i,i+3);
  const hit=kind==='cyan'?g>=165&&b>=160&&g>r*1.35:kind==='warm'?r>=195&&g>=125&&g>b*1.3:r>150&&b>100&&r>g*1.3;
  if(hit)glass[(y+pad)*W+x+pad]=1;
 }
 for(let y=pad;y<H-pad;y++)for(let x=pad;x<W-pad;x++)if(glass[y*W+x]){
  const edge=!glass[y*W+x-1]||!glass[y*W+x+1]||!glass[(y-1)*W+x]||!glass[(y+1)*W+x];
  if(mode==='solid'||edge)seed[y*W+x]=1;
 }
 const im=blank(W,H),dist=new Uint8Array(W*H);dist.fill(255);
 for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(seed[y*W+x])for(let dy=-4;dy<=4;dy++)for(let dx=-4;dx<=4;dx++){
  const xx=x+dx,yy=y+dy;if(xx>=0&&xx<W&&yy>=0&&yy<H){const j=yy*W+xx;dist[j]=Math.min(dist[j],Math.max(Math.abs(dx),Math.abs(dy)));}}
 for(let y=0;y<H;y++)for(let x=0;x<W;x++){
  const j=y*W+x,d=dist[j],threshold=bayer[(y+sy-pad)&3][(x+sx-pad)&3];let color;
  if(d===0){
   // Sparse one-pixel core; saturated tube remains dominant.
   color=seed[j-1]&&seed[j+1]&&seed[j-W]&&seed[j+W]&&((x+y)%4===0)?white:tube;
  }else if(d===1&&threshold<12)color=tube.map(v=>Math.round(v*.43));
  else if(d===2&&threshold<8)color=tube.map(v=>Math.round(v*.25));
  else if(d>=3&&d<=4&&threshold<4)color=tube.map(v=>Math.round(v*.14));
  if(color)im.data.set([...color,255],j*4);
 }
 let x1=W,y1=H,x2=-1,y2=-1;
 for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(im.data[(y*W+x)*4+3]){x1=Math.min(x1,x);x2=Math.max(x2,x);y1=Math.min(y1,y);y2=Math.max(y2,y);}
 if(x2<0)throw Error('Empty tube '+label);
 const crop=blank(x2-x1+1,y2-y1+1),ox=sx-pad+x1,oy=sy-pad+y1;
 for(let y=0;y<crop.h;y++)for(let x=0;x<crop.w;x++){
  const p=((y+y1)*W+x+x1)*4,q=(y*crop.w+x)*4;crop.data.set(im.data.subarray(p,p+4),q);
  if(crop.data[q+3])composite.data.set(crop.data.subarray(q,q+4),((oy+y)*1620+ox+x)*4);
 }
 const file=`neon_B_${String(n+1).padStart(2,'0')}.png`;
 writePNG(dir+file,crop);records.push({file,x:ox,y:oy,w:crop.w,h:crop.h,label,radius,flicker,light,tube});
}
fs.writeFileSync(dir+'neon_list.csv','layer,file,x_px,y_px,light_hex,light_radius_px,flicker\n'+records.map(r=>`near,${r.file},${r.x},${r.y},${r.light},${r.radius},${r.flicker}`).join('\n')+'\n');
fs.writeFileSync(dir+'neon_manifest.json',JSON.stringify(records,null,2)+'\n');
writePNG(root+'B_lit_composite_preview.png',composite);
console.log('Created',records.length,'cropped binary-alpha neon sprites.');
