#!/usr/bin/env node
/* Node 18+, no dependencies. Scene-specific pixel finishing; run from repo root:
 * node tools/art/align_seg_b.cjs docs/design/concept/stg01/B_clean_generated.png docs/design/concept/stg01/B_clean_hires.png
 * Then run pixelize_bg.cjs on B_clean_hires.png. Coordinates below use the
 * native 1620x540 grid; edits are performed at 2x in the high-res plate.
 */
const {readPNG,writePNG,resizeLanczos,crop3}=require('./pixelize_bg.cjs');
const src=resizeLanczos(crop3(readPNG(process.argv[2])),3240,1080);
const out={...src,data:src.data.slice()};
function rect(x,y,w,h,c){for(let yy=y*2;yy<(y+h)*2;yy++)for(let xx=x*2;xx<(x+w)*2;xx++)out.data.set([...c,255],(yy*out.w+xx)*4);}
function copy(sx,sy,sw,sh,dx,dy,dw=sw,dh=sh){for(let y=0;y<dh*2;y++)for(let x=0;x<dw*2;x++){const a=((sy*2+Math.floor(y*sh/dh))*src.w+sx*2+Math.floor(x*sw/dw))*4;out.data.set(src.data.subarray(a,a+4),((dy*2+y)*out.w+dx*2+x)*4);}}
const cap=[147,162,174],dark=[29,39,55],stone=[99,108,128],wood=[170,105,59],pink=[134,49,101];
// Shorten generated roof to collider's left end using adjacent skyline.
copy(550,299,20,49,570,299,19,49);
copy(570,302,736,84,589,300,718,86);
rect(589,300,718,2,cap);rect(589,302,718,1,dark);
for(let x=601;x<1307;x+=16)rect(x,300,1,2,stone);
rect(589,303,2,43,dark);
// High wall stops at x416. Low foliage behind stall 2 replaces overrun.
copy(375,294,120,51,417,345,172,41);
copy(0,342,416,3,0,345);
copy(0,345,416,29,0,348,416,26);
rect(0,348,417,2,stone);rect(0,350,417,1,dark);
for(let x=18;x<417;x+=33)rect(x,348,1,2,dark);
rect(415,351,2,23,dark);
// Retain cloth folds; add precise upper seams.
rect(0,374,235,2,pink);rect(0,376,235,1,[184,77,131]);
rect(346,386,186,2,pink);rect(346,388,186,1,[184,77,131]);
rect(618,386,234,2,pink);rect(618,388,234,1,[184,77,131]);
// Raise existing barrel, preserve nearby lanterns.
copy(319,467,29,40,320,462,30,45);
rect(320,462,30,2,dark);rect(322,462,26,1,wood);
rect(550,466,59,2,dark);rect(551,466,57,1,wood);
// Coping marks full-width ground in front of background stairs.
rect(0,507,1620,1,dark);
rect(0,508,1620,2,[148,151,168]);rect(0,510,1620,1,[76,83,104]);
for(let x=22;x<1620;x+=34)rect(x,508,1,3,dark);
// Match the garden opening's dark skyline, leaving the foliage intact.
for(let y=345*2;y<386*2;y++)for(let x=417*2;x<589*2;x++){
 const i=(y*out.w+x)*4,r=out.data[i],g=out.data[i+1],b=out.data[i+2];
 if(b>g*1.2&&g>r*2)out.data.set([10,55,94,255],i);
}
writePNG(process.argv[3],out);
console.log('Aligned high-res: 3240x1080. Surface rows: 508,348,374,462,386,466,386,300.');
