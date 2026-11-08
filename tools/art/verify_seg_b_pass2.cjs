#!/usr/bin/env node
const assert=require('node:assert/strict'),fs=require('node:fs'),cp=require('node:child_process');
const {readPNG,writePNG,blank,platforms}=require('./pixelize_bg.cjs');
const p='docs/design/concept/stg01/',base=readPNG(p+'B_base_off_native.png'),hi=readPNG(p+'B_base_off_hires.png');
const half=readPNG(p+'B_base_off_preview_50pct.png'),lit=readPNG(p+'B_lit_composite_preview.png');
assert.deepEqual([hi.w,hi.h],[3240,1080]);
for(const im of[base,half,lit])assert.deepEqual([im.w,im.h],[1620,540]);
const colors=new Set();
for(let y=0;y<540;y++)for(let x=0;x<1620;x++){
 const i=(y*1620+x)*4;colors.add(base.data.subarray(i,i+3).join(','));assert.equal(base.data[i+3],255);
 for(let c=0;c<3;c++)assert.equal(half.data[i+c],Math.round(base.data[i+c]*.5));
 assert.equal(half.data[i+3],255);
 for(let dy=0;dy<2;dy++)for(let dx=0;dx<2;dx++)assert.deepEqual(hi.data.subarray(((y*2+dy)*3240+x*2+dx)*4,((y*2+dy)*3240+x*2+dx)*4+4),base.data.subarray(i,i+4));
}
assert.equal(colors.size,48,'Exactly 48 used RGB colours');
// Re-run the Pass 1 aligner, verifying the inherited geometry source byte-for-byte.
const check=p+'_pass2_alignment_check.png';
try{
 cp.execFileSync(process.execPath,['tools/art/align_seg_b.cjs',p+'B_clean_generated.png',check],{stdio:'pipe'});
 assert.deepEqual(readPNG(check).data,readPNG(p+'B_clean_hires.png').data,'Pass 1 alignment source unchanged');
}finally{if(fs.existsSync(check))fs.unlinkSync(check);}
const seams=[];
for(const[x1,x2,h]of platforms){let contrast=0;
 for(let x=x1;x<=x2;x++){const i=((540-h)*1620+x)*4;
  if(base.data.subarray(i,i+3).join()!==base.data.subarray(i-6480,i-6477).join())contrast++;}
 assert.ok(contrast/(x2-x1+1)>.9,'Platform seam lost');
 seams.push({x1,x2,row:540-h,height:h,contrastFraction:contrast/(x2-x1+1)});
}
const rows=fs.readFileSync(p+'neon_B/neon_list.csv','utf8').trim().split(/\r?\n/);
assert.equal(rows.shift(),'layer,file,x_px,y_px,light_hex,light_radius_px,flicker');
const rebuilt={...base,data:base.data.slice()},signs=[];let flickers=0;
for(const row of rows){const[layer,file,xs,ys,light,rs,fl]=row.split(','),x=+xs,y=+ys,radius=+rs,flicker=+fl;
 assert.equal(layer,'near');assert.match(file,/^neon_B_\d\d\.png$/);assert.match(light,/^#[0-9A-F]{6}$/);
 assert.ok(radius===0||(radius>=64&&radius<=224));assert.ok(flicker===0||flicker===.3);if(flicker)flickers++;
 const im=readPNG(p+'neon_B/'+file);assert.ok(x>=0&&y>=0&&x+im.w<=1620&&y+im.h<=540);
 let opaque=0,tubes=0;const palette=new Set();let edges=[false,false,false,false];
 for(let yy=0;yy<im.h;yy++)for(let xx=0;xx<im.w;xx++){
  const i=(yy*im.w+xx)*4,a=im.data[i+3];assert.ok(a===0||a===255);
  if(a){opaque++;palette.add(im.data.subarray(i,i+3).join(','));if(Math.max(...im.data.subarray(i,i+3))===255)tubes++;
   rebuilt.data.set(im.data.subarray(i,i+4),((y+yy)*1620+x+xx)*4);
   if(xx===0)edges[0]=true;if(yy===0)edges[1]=true;if(xx===im.w-1)edges[2]=true;if(yy===im.h-1)edges[3]=true;
  }else assert.equal(im.data[i]+im.data[i+1]+im.data[i+2],0);
 }
 assert.ok(edges.every(Boolean),'Sprite is not tightly cropped');assert.ok(tubes>0&&opaque>tubes,'Require tubes and darker pixel halo');
 assert.ok(palette.size>=4&&palette.size<=5,'One tube hue, three halo levels, optional white core');
 signs.push({file,x,y,width:im.w,height:im.h,opaque,tubes,colors:palette.size});
}
assert.ok(flickers>=1&&flickers<=2);assert.deepEqual(rebuilt.data,lit.data,'CSV reconstruction must match lit preview exactly');
// Native nearest-neighbour 4x crops: sign/halo, stall tubes, barrel and roof seam.
const crops=[[418,104,164,65],[118,403,70,45],[310,451,53,65],[577,289,99,53]],qa=blank(960,540);
crops.forEach(([sx,sy,w,h],k)=>{const dx=k===1?680:k===2?680:0,dy=k<2?0:270;
 const source=k<2?lit:half;
 for(let yy=0;yy<h*4;yy++)for(let xx=0;xx<w*4;xx++){
  const i=((sy+(yy>>2))*1620+sx+(xx>>2))*4;qa.data.set(source.data.subarray(i,i+4),((dy+yy)*qa.w+dx+xx)*4);}});
writePNG(p+'B_pass2_QA_4x.png',qa);
const regions={wall:[900,400,300,100],roof:[600,306,600,36],ground:[0,511,1620,29],stalls:[15,430,810,75]};
const values={};
for(const[name,[x,y,w,h]]of Object.entries(regions)){
 const a=[];for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++){const i=(yy*1620+xx)*4;a.push(Math.max(...base.data.subarray(i,i+3))/255);}
 a.sort((a,b)=>a-b);values[name]={medianMaxChannel:a[Math.floor(a.length*.5)],p25:a[Math.floor(a.length*.25)],p75:a[Math.floor(a.length*.75)]};
}
const report={native:[1620,540],hires:[3240,1080],colors:colors.size,integer2x:true,exact50pct:true,pass1AlignmentReproduced:true,seams,signs,flickers,valueSamples:values,compositeCSVReconstruction:'exact',alpha:[0,255],limits:'Value statistic uses max sRGB channel (HSV V); regional samples include outlines/objects. No Unity runtime test.'};
fs.writeFileSync(p+'B_pass2_verification.json',JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
