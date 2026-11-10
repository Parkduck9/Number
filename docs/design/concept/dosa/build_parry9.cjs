// Asset-only authoring utility; never touches code, scenes, prefabs or metadata.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const io=require('./B/pixel_io.cjs');
const {pngRead,pngWrite,image,pixel,set,paste,crop,resize,bbox,components}=io;
const root=path.resolve(__dirname,'../../../..');
const palette=['080b14','1a1d33','2d3050','3f4a6e','62677d','d6d7d0','a37b70','d7b4a0','efd0b6','226076','42a9bd','7f1a22','b94445','f2c94c','9b7043','c49a5c'].map(h=>[...Buffer.from(h,'hex'),255]);
const bg=[11,10,20,255],clear=[0,0,0,0];
const stage=Number(process.argv[2]||0),source=pngRead(path.join(__dirname,'parry9_generated.png'));
function nearest(c){let best=Infinity,idx=0;palette.forEach((p,i)=>{const d=(p[0]-c[0])**2*.3+(p[1]-c[1])**2*.4+(p[2]-c[2])**2*.3;if(d<best){best=d;idx=i;}});return idx;}
function reduce(src,w,h){
 const dst=image(w,h);
 for(let y=0;y<h;y++)for(let x=0;x<w;x++){
  let count=0,total=0;const hist=new Uint16Array(16);
  for(let j=Math.floor(y*src.h/h);j<Math.floor((y+1)*src.h/h);j++)for(let i=Math.floor(x*src.w/w);i<Math.floor((x+1)*src.w/w);i++){
   const c=pixel(src,i,j);total++;if(c[3]>=160){count++;hist[nearest(c)]++;}
  }
  if(count/total>=.42){let k=0;for(let i=1;i<16;i++)if(hist[i]>hist[k])k=i;set(dst,x,y,palette[k]);}
 }
 return dst;
}
function line(im,x0,y0,x1,y1,col,width=1){
 const dx=Math.abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.abs(y1-y0),sy=y0<y1?1:-1;let e=dx+dy;
 for(;;){for(let w=0;w<width;w++)set(im,x0,y0+w,col);if(x0===x1&&y0===y1)break;const e2=2*e;if(e2>=dy){e+=dy;x0+=sx;}if(e2<=dx){e+=dx;y0+=sy;}}
}
const font={H:['101','101','111','101','101'],I:['111','010','010','010','111'],M:['101','111','111','101','101'],D:['110','101','101','101','110'],L:['100','100','100','100','111'],O:['111','101','101','101','111'],W:['101','101','111','111','101'],'1':['010','110','010','010','111'],'2':['110','001','010','100','111'],'3':['110','001','010','001','110']};
function label(im,text,x,y){for(const c of text){if(font[c])font[c].forEach((r,j)=>[...r].forEach((v,i)=>{if(v==='1')set(im,x+i,y+j,palette[5]);}));x+=4;}}
const strips=[],records=[];
for(let row=0;row<3;row++)for(let col=0;col<3;col++){
 const sx=Math.floor(col*source.w/3),sy=Math.floor(row*source.h/3);
 const part=crop(source,sx,sy,Math.floor((col+1)*source.w/3)-sx,Math.floor((row+1)*source.h/3)-sy);
 for(let i=3;i<part.data.length;i+=4)if(part.data[i]<160)part.data.fill(0,i-3,i+1);
 const b=bbox(part),scale=stage===2&&row===0&&col===1?6.1:5.8,w=Math.round((b[2]-b[0])/scale),h=Math.round((b[3]-b[1])/scale);
 const small=reduce(crop(part,b[0],b[1],b[2]-b[0],b[3]-b[1]),w,h),cel=image(64,64);
 paste(cel,small,Math.floor((64-w)/2),62-h);
 // Look-and-fix round 1: strengthen the blade/ward and recover top margin.
 if(stage===1){
  if(row===0&&col===1){
   // Erase only the old sword area, then shorten its upward extension.
   for(let y=0;y<20;y++)for(let x=32;x<59;x++)set(cel,x,y,clear);
  }
  const blades={
   '0,0':[27,10,50,10], '0,1':[34,19,50,3],
   '1,0':[39,31,40,10], '1,1':[37,30,51,15],
   '2,0':[37,48,57,48], '2,1':[42,51,59,60],
  };
  const blade=blades[`${row},${col}`];
  if(blade){const [x0,y0,x1,y1]=blade;line(cel,x0,y0-1,x1,y1-1,palette[0],4);line(cel,x0,y0,x1,y1,palette[4],2);line(cel,x0,y0,x1,y1,palette[5]);}
  if(row===1&&col===2){
   for(let y=21;y<=29;y++)for(let x=50;x<=53;x++)set(cel,x,y,palette[x===50||x===53||y===21||y===29?0:13]);
   set(cel,51,24,palette[11]);set(cel,52,26,palette[11]);
  }
  // Restore a continuous one-pixel silhouette contour after majority reduction.
  const rim=image(64,64);
  for(let y=0;y<=61;y++)for(let x=1;x<63;x++)if(pixel(cel,x,y)[3]){
   for(const [dx,dy] of [[-1,0],[1,0],[0,-1],[0,1]])if(y+dy>=1&&y+dy<=61&&!pixel(cel,x+dx,y+dy)[3])set(rim,x+dx,y+dy,palette[0]);
  }
  paste(rim,cel,0,0);cel.data.set(rim.data);
 }
 // Look-and-fix round 2: retain the source's single connected blade contour.
 // The first fix doubled some edges and erased HI2's wrist; rebuild from source.
 if(stage===2){
  const bladeRegions={
   '0,0':[26,8,53,13], '0,1':[35,2,54,13],
   '1,0':[38,8,43,31], '1,1':[40,10,54,26],
   '2,0':[39,45,59,49], '2,1':[44,49,62,59],
   '2,2':[43,15,52,35],
  };
  const br=bladeRegions[`${row},${col}`];
  if(br)for(let y=br[1];y<br[3];y++)for(let x=br[0];x<br[2];x++){
   const c=pixel(cel,x,y);if(c[3]&&[6,7,8].some(k=>c.every((v,i)=>v===palette[k][i])))set(cel,x,y,palette[4]);
  }
  if(row===1&&col===2){
   for(let y=20;y<=27;y++)for(let x=51;x<=53;x++)set(cel,x,y,palette[13]);
   set(cel,52,22,palette[11]);set(cel,52,25,palette[11]);
  }
  if(row===2&&col===2){
   for(let y=54;y<=59;y++)for(let x=37;x<=40;x++)set(cel,x,y,palette[13]);
   set(cel,38,55,palette[11]);set(cel,39,57,palette[11]);
  }
  // One exterior contour, clipped above the grounded baseline.
  const rim=image(64,64);
  for(let y=0;y<=61;y++)for(let x=1;x<63;x++)if(pixel(cel,x,y)[3]){
   for(const [dx,dy] of [[-1,0],[1,0],[0,-1],[0,1]])if(y+dy>=1&&y+dy<=61&&!pixel(cel,x+dx,y+dy)[3])set(rim,x+dx,y+dy,palette[0]);
  }
  paste(rim,cel,0,0);cel.data.set(rim.data);
 }
 const recoil=image(64,64),hinge=row===2?49:44;
 for(let y=0;y<64;y++)for(let x=0;x<64;x++){
  const c=pixel(cel,x,y);if(c[3])set(recoil,x-(y<hinge?1:0),y+(y<hinge?1:0),c);
 }
 const strip=image(128,64);paste(strip,cel,0,0);paste(strip,recoil,64,0);
 const file=`hero_parry_${['hi','mid','lo'][row]}${col+1}_2f.png`;
 const frames=[cel,recoil].map((im,i)=>({frame:i,bounds:bbox(im),colors:[...new Set(Array.from({length:4096},(_,k)=>im.data[k*4+3]?im.data.subarray(k*4,k*4+3).toString('hex'):null).filter(Boolean))].length}));
 records.push({file,sourceBounds:[sx+b[0],sy+b[1],sx+b[2],sy+b[3]],frames});strips.push(strip);
 if(stage===2){
  assert.equal(strip.w,128);assert.equal(strip.h,64);
  for(const im of [cel,recoil]){
   assert.equal(bbox(im)[3],62,'Grounded baseline must be row 61');
   for(let k=0;k<im.data.length;k+=4){const c=Array.from(im.data.subarray(k,k+4));assert.ok(c[3]===0||c[3]===255);if(c[3])assert.ok(palette.some(p=>p.every((v,i)=>v===c[i])));}
  }
  assert.ok(!cel.data.equals(recoil.data),'Impact/recoil must differ');
  for(let y=59;y<64;y++)assert.ok(cel.data.subarray(y*256,(y+1)*256).equals(recoil.data.subarray(y*256,(y+1)*256)),'Shoes must remain planted');
  pngWrite(path.join(root,'Assets/Resources/DraftSprites',file),strip);
  records.at(-1).sha256=crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'Assets/Resources/DraftSprites',file))).digest('hex');
 }
}
const preview=image(444,246,bg);
strips.forEach((strip,i)=>{const row=Math.floor(i/3),col=i%3;const x=12+col*144,y=8+row*80;label(preview,['HI','MID','LOW'][row]+(col+1),x,y);paste(preview,strip,x,y+8);});
const suffix=stage===2?'':`_round${stage+1}`;
pngWrite(path.join(__dirname,`parry9_preview${suffix}_1x.png`),preview);
pngWrite(path.join(__dirname,`parry9_preview${suffix}_4x.png`),resize(preview,preview.w*4,preview.h*4));
fs.writeFileSync(path.join(__dirname,`parry9_validation${suffix}.json`),JSON.stringify({stage,frameSize:[64,64],stripSize:[128,64],feetRow:61,hardAlpha:true,background:'#0B0A14',reviewRounds:stage===2?2:stage,palette:palette.map(c=>Buffer.from(c.slice(0,3)).toString('hex')),records},null,2)+'\n');
console.log(JSON.stringify({stage,records}));
