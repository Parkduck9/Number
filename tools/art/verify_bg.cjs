// Node 18+, dependency-free artifact checks: node tools/art/verify_bg.cjs
const assert=require('node:assert/strict');
const {readPNG,writePNG,blank,platforms}=require('./pixelize_bg.cjs');
const base='docs/design/concept/stg01/';
const native=readPNG(base+'B_clean_native.png'),hi=readPNG(base+'B_clean_hires.png');
assert.deepEqual([hi.w,hi.h],[3240,1080]);
const mask=readPNG(base+'B_clean_neon_mask.png'),guide=readPNG(base+'B_clean_preview_guides.png');
for(const im of[native,mask,guide,readPNG(base+'B_clean_neon_preview.png')])assert.deepEqual([im.w,im.h],[1620,540]);
const colors=new Set();let opaque=0;
for(let i=0;i<native.data.length;i+=4){
 colors.add(native.data.subarray(i,i+3).join(','));assert.equal(native.data[i+3],255);
 const a=mask.data[i+3];assert.ok(a===0||a===255);
 if(a){opaque++;assert.deepEqual(mask.data.subarray(i,i+3),native.data.subarray(i,i+3));}
 else assert.equal(mask.data[i]+mask.data[i+1]+mask.data[i+2],0);
}
assert.ok(colors.size<=48);assert.ok(opaque>0&&opaque<1620*540*.1);
for(const[x1,x2,height]of platforms){
 const row=540-height;let differences=0,edgePixels=0;
 for(let x=x1;x<=x2;x++){
  const i=(row*1620+x)*4;
  if(native.data.subarray(i,i+3).join(',')!==guide.data.subarray(i,i+3).join(','))differences++;
  if(native.data.subarray(i,i+3).join(',')!==native.data.subarray(i-6480,i-6477).join(','))edgePixels++;
 }
 assert.equal(differences,x2-x1+1,'Guide must cover complete platform span');
 assert.ok(edgePixels/(x2-x1+1)>.9,'Painted seam must contrast with the row immediately above across the span');
 console.log(`Guide x=${x1}..${x2}, row=${row}, bottom-height=${height}`);
}
// Native 4x nearest-neighbor contact sheet for visual checking, no interpolation.
const regions=[[300,440,64,70],[390,330,220,70],[580,290,100,70]],zoom=blank(880,560);
regions.forEach(([sx,sy,w,h],k)=>{for(let y=0;y<h*4;y++)for(let x=0;x<w*4;x++){
 const i=((sy+Math.floor(y/4))*1620+sx+Math.floor(x/4))*4;
 const dx=k===0?x:x,dy=k===0?y:k===1?280+y:y;
 const offset=k===2?400:0;
 // Region 0/2 share the first row, region 1 fills the lower row.
 zoom.data.set(native.data.subarray(i,i+4),((dy)*880+dx+offset)*4);
}});
writePNG(base+'B_clean_QA_4x.png',zoom);
console.log(JSON.stringify({native:[native.w,native.h],colors:colors.size,opaqueMaskPixels:opaque,maskAlpha:[0,255],hires:[hi.w,hi.h]}));
