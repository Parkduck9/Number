/* Draft artwork only. Node built-ins; integer rasterization, no image service. */
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const OUT = __dirname;
const ROUND = Number(process.argv[2] || 5);
const P = {
  ink:'#080b14', night:'#0b1026', dark:'#1a1d33', mid:'#2d3050', tile:'#3f4a6e',
  steel:'#62677d', silver:'#858a9b', edge:'#b8bec8', white:'#d6d7d0',
  coolant:'#226076', cyan:'#42a9bd', red:'#7f1a22', tell:'#f21c2a',
  brass:'#b8862e', yellow:'#ffd43b', warm:'#7a3a3c'
};
const rgba = c => !c ? [0,0,0,0] : [...(P[c]||c).replace('#','').match(/../g).map(x=>parseInt(x,16)),255];
class Raster {
  constructor(w,h,c) { this.w=w;this.h=h;this.d=Buffer.alloc(w*h*4);if(c)this.rect(0,0,w,h,c); }
  px(x,y,c) { x=Math.round(x);y=Math.round(y);if(x<0||y<0||x>=this.w||y>=this.h)return;const a=Array.isArray(c)?c:rgba(c);for(let k=0;k<4;k++)this.d[(y*this.w+x)*4+k]=a[k]; }
  rect(x,y,w,h,c) {const a=rgba(c);for(let j=Math.max(0,y);j<Math.min(this.h,y+h);j++)for(let i=Math.max(0,x);i<Math.min(this.w,x+w);i++)this.px(i,j,a);}
  line(points,c,width=1) {for(let i=1;i<points.length;i++){let [x,y]=points[i-1];const [tx,ty]=points[i];let dx=Math.abs(tx-x),dy=-Math.abs(ty-y),sx=x<tx?1:-1,sy=y<ty?1:-1,err=dx+dy;for(;;){this.rect(x-Math.floor(width/2),y-Math.floor(width/2),width,width,c);if(x===tx&&y===ty)break;const e=2*err;if(e>=dy){err+=dy;x+=sx;}if(e<=dx){err+=dx;y+=sy;}}}}
  poly(points,c,outline) {const ys=points.map(p=>p[1]);for(let y=Math.max(0,Math.min(...ys));y<=Math.min(this.h-1,Math.max(...ys));y++){let xs=[];for(let i=0;i<points.length;i++){const a=points[i],b=points[(i+1)%points.length];if((a[1]<=y&&b[1]>y)||(b[1]<=y&&a[1]>y))xs.push(a[0]+(y-a[1])*(b[0]-a[0])/(b[1]-a[1]));}xs.sort((a,b)=>a-b);for(let i=0;i+1<xs.length;i+=2)for(let x=Math.ceil(xs[i]);x<=Math.floor(xs[i+1]);x++)this.px(x,y,c);}if(outline)this.line([...points,points[0]],outline,2);}
  ellipse(cx,cy,rx,ry,c) {for(let y=-ry;y<=ry;y++){const x=Math.floor(rx*Math.sqrt(1-y*y/(ry*ry)));this.rect(cx-x,cy+y,2*x+1,1,c);}}
  blit(src,x,y,flip=false) {for(let j=0;j<src.h;j++)for(let i=0;i<src.w;i++){const o=(j*src.w+i)*4;if(src.d[o+3])this.px(x+(flip?src.w-1-i:i),y+j,[...src.d.subarray(o,o+4)]);}}
  scale(n) {const a=new Raster(this.w*n,this.h*n);for(let y=0;y<a.h;y++)for(let x=0;x<a.w;x++){const o=(Math.floor(y/n)*this.w+Math.floor(x/n))*4;a.px(x,y,[...this.d.subarray(o,o+4)]);}return a;}
  crop(x,y,w,h) {const a=new Raster(w,h);for(let j=0;j<h;j++)for(let i=0;i<w;i++){const o=((y+j)*this.w+x+i)*4;a.px(i,j,[...this.d.subarray(o,o+4)]);}return a;}
  save(name) {fs.writeFileSync(path.join(OUT,name),encode(this));}
}
const crcTable = Array.from({length:256},(_,n)=>{for(let i=0;i<8;i++)n=n&1?0xedb88320^(n>>>1):n>>>1;return n>>>0;});
function crc(b){let c=0xffffffff;for(const v of b)c=crcTable[(c^v)&255]^(c>>>8);return(c^0xffffffff)>>>0;}
function chunk(t,d){const b=Buffer.alloc(d.length+12);b.writeUInt32BE(d.length);b.write(t,4);d.copy(b,8);b.writeUInt32BE(crc(b.subarray(4,-4)),b.length-4);return b;}
function encode(a){const head=Buffer.alloc(13);head.writeUInt32BE(a.w);head.writeUInt32BE(a.h,4);head[8]=8;head[9]=6;const raw=Buffer.alloc((a.w*4+1)*a.h);for(let y=0;y<a.h;y++)a.d.copy(raw,y*(a.w*4+1)+1,y*a.w*4,(y+1)*a.w*4);return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',head),chunk('IDAT',zlib.deflateSync(raw,{level:9})),chunk('IEND',Buffer.alloc(0))]);}
function decode(file){const b=fs.readFileSync(file),w=b.readUInt32BE(16),h=b.readUInt32BE(20);if(b[24]!==8||b[25]!==6)throw Error('RGBA8 PNG required');let p=8,ids=[];while(p<b.length){const n=b.readUInt32BE(p),t=b.toString('ascii',p+4,p+8);if(t==='IDAT')ids.push(b.subarray(p+8,p+8+n));p+=n+12;}const raw=zlib.inflateSync(Buffer.concat(ids)),a=new Raster(w,h),stride=w*4;for(let y=0;y<h;y++){const f=raw[y*(stride+1)];for(let x=0;x<stride;x++){const o=y*stride+x,l=x>=4?a.d[o-4]:0,u=y?a.d[o-stride]:0,ul=y&&x>=4?a.d[o-stride-4]:0;let v=0;if(f===1)v=l;if(f===2)v=u;if(f===3)v=Math.floor((l+u)/2);if(f===4){const q=l+u-ul,dl=Math.abs(q-l),du=Math.abs(q-u),dul=Math.abs(q-ul);v=dl<=du&&dl<=dul?l:du<=dul?u:ul;}a.d[o]=(raw[y*(stride+1)+1+x]+v)&255;}}return a;}
function tube(a,points,width=9,c='mid'){a.line(points,'ink',width+4);a.line(points,c,width);a.line(points.map(([x,y])=>[x-2,y-2]),'dark',Math.max(1,width-5));}
function stud(a,x,y){a.rect(x,y,4,4,'ink');a.rect(x,y,3,2,'brass');a.px(x,y,'silver');}
function clipped(a,draw,allowed){const b=new Raster(a.w,a.h);draw(b);const keep=allowed.map(c=>rgba(c).slice(0,3).join(','));for(let i=0;i<a.d.length;i+=4)if(a.d[i+3]&&b.d[i+3]&&keep.includes([...a.d.subarray(i,i+3)].join(',')))b.d.copy(a.d,i,i,i+4);}
function refineBody(a){
  // Dark broken equipment in the skirt, varied widths and interrupted bays.
  rack(a,123,323,63,118);rack(a,190,337,47,111);
  rack(a,518,330,52,118);rack(a,581,347,68,101);
  a.poly([[126,324],[157,324],[150,339],[138,342],[144,362],[128,358]],'ink');
  a.poly([[582,351],[614,351],[609,366],[621,376],[614,398],[589,388]],'night');
  // Detached chassis skins, with oil trapped in seams.
  a.poly([[245,332],[271,342],[278,369],[260,399],[239,414],[235,380]],'dark','ink');
  a.poly([[478,341],[500,322],[517,349],[523,391],[500,409],[483,385]],'dark','ink');
  a.line([[246,338],[266,349],[269,368]],'mid',3);
  for(const [x,y]of[[249,356],[497,365],[328,396],[428,414]]){
    a.poly([[x,y],[x+14,y-3],[x+22,y+9],[x+15,y+23],[x+2,y+20]],'dark','ink');
    a.line([[x+2,y+1],[x+12,y-1]],'mid',2);
  }
  const hoses=[[[235,239],[229,263],[231,296],[245,321],[255,350],[254,379],[246,407],[244,447]],[[522,234],[531,259],[528,292],[517,312],[505,335],[510,365],[530,387],[542,412],[543,447]],[[338,353],[330,378],[332,405],[345,422],[366,429],[393,428],[412,419],[420,395]],[[99,295],[87,322],[84,348],[93,370],[108,385],[123,390]]];
  for(const pts of hoses)tube(a,pts,6,'mid');
  clipped(a,b=>{
    // Localized oil and missing corners, rather than random texture noise.
    for(const [x,y]of[[178,76],[213,162],[573,132],[618,223],[69,276],[719,249],[346,190],[445,197],[303,340]]){
      b.poly([[x,y],[x+13,y+3],[x+18,y+14],[x+10,y+17],[x+13,y+27],[x+6,y+40],[x+4,y+21],[x-4,y+13]],'ink');
      b.line([[x+9,y+4],[x+8,y+12],[x+12,y+17]],'coolant',2);
    }
    for(const [x,y]of[[103,100],[169,63],[237,87],[504,62],[579,97],[654,135],[144,356],[533,353]]){
      b.rect(x,y,11,2,'tile');b.rect(x+2,y+2,5,1,'mid');b.rect(x+24,y+29,7,2,'mid');
    }
  },['dark','mid','tile','night']);
  // Small coolant clusters stay much dimmer than an eventual code hologram.
  for(const [x,y]of[[117,192],[599,221],[251,374]]){a.rect(x,y,2,6,'coolant');a.rect(x,y,1,2,'cyan');}
}
function rack(a,x,y,w,h){a.poly([[x,y+5],[x+5,y],[x+w,y],[x+w,y+h],[x,y+h]],'dark','ink');a.rect(x+4,y+4,w-8,3,'mid');a.rect(x+4,y+6,3,h-10,'mid');a.rect(x+w-8,y+6,5,h-7,'night');for(let j=12;j<h-8;j+=21){a.rect(x+10,y+j,w-21,16,'ink');a.rect(x+12,y+j+1,w-25,2,'mid');a.rect(x+13,y+j+5,w-34,2,'dark');a.rect(x+13,y+j+9,w-34,2,'dark');a.rect(x+w-20,y+j+5,3,3,(j%42===12)?'coolant':'dark');a.rect(x+w-15,y+j+5,2,3,'mid');}for(let j=8;j<h;j+=36){a.rect(x+4,y+j,2,2,'tile');}}
function roof(a,x,y,w){
  a.poly([[x,y+4],[x+18,y+12],[x+40,y+10],[x+w-24,y],[x+w-7,y-5],[x+w,y],[x+w-12,y+14],[x+w+9,y+42],[x+w+17,y+47],[x+w-4,y+51],[x+11,y+51],[x-12,y+45],[x,y+34],[x+14,y+16]],'dark','ink');
  a.line([[x-10,y+43],[x+22,y+45],[x+w-10,y+42],[x+w+12,y+46]],'mid',5);
  for(let i=0;i<w-14;i+=13){const ry=y+15-Math.floor(i/26);a.poly([[x+20+i,ry],[x+28+i,ry-1],[x+18+i,y+41],[x+8+i,y+42]],'mid','ink');a.line([[x+21+i,ry+2],[x+11+i,y+38]],'tile',2);a.rect(x+8+i,y+41,8,3,'mid');}
  a.line([[x+2,y+5],[x+20,y+12],[x+w-22,y],[x+w-7,y-5]],'tile',2);
  a.rect(x+15,y+50,w-23,7,'ink');a.rect(x+18,y+51,w-30,2,'coolant');
}
function body(){const a=new Raster(768,448);
  // Hanging conduits connect the crown to an implied machine beyond the canvas.
  for(const [x,dx,h] of [[55,38,210],[124,-12,200],[204,14,180],[274,-32,90],[484,28,94],[552,-18,190],[640,16,180],[718,-26,230]]){
    tube(a,[[x,0],[x+2,34],[x+dx,86],[x+dx, h]],10,'dark');
  }
  a.poly([[36,166],[71,114],[131,105],[166,71],[252,83],[299,116],[317,168],[451,168],[474,110],[535,74],[621,90],[660,127],[732,156],[760,234],[740,330],[686,361],[657,447],[108,447],[76,350],[17,320],[7,234]],'ink');
  a.poly([[72,160],[139,124],[237,133],[306,178],[312,336],[273,398],[249,447],[82,447],[100,323],[38,279]],'night');
  a.poly([[472,177],[531,129],[624,140],[704,188],[725,288],[662,332],[681,447],[501,447],[467,336]],'night');
  // Unequal stacks of server blocks make the fused silhouette architectural.
  rack(a,80,82,76,228);rack(a,156,42,68,206);rack(a,224,70,62,165);
  rack(a,492,42,66,206);rack(a,560,77,80,243);rack(a,643,115,55,199);
  rack(a,28,188,52,145);rack(a,699,206,49,143);
  roof(a,62,76,95);roof(a,556,70,98);
  roof(a,9,255,64);roof(a,670,285,80);
  // Crown of broken, vertical server blades. No helmet or humanoid face.
  rack(a,287,21,44,111);rack(a,337,8,40,67);rack(a,382,17,37,67);rack(a,429,31,46,108);
  a.poly([[294,54],[307,47],[309,64],[329,59],[330,41],[346,41],[347,61],[397,60],[399,47],[415,49],[415,66],[455,62],[467,75],[464,165],[447,181],[315,180],[298,163]],'dark','ink');
  a.line([[300,65],[321,68],[334,64],[393,64]],'mid',3);
  // Blank 144 x 88 hologram recess. Its icon and number belong to code.
  a.rect(305,65,158,102,'ink');a.rect(308,67,151,3,'mid');a.rect(308,69,3,94,'tile');
  a.rect(312,72,144,88,'ink');a.rect(456,74,3,87,'night');a.rect(315,163,136,3,'dark');
  a.rect(300,86,6,14,'mid');a.rect(460,133,7,14,'mid');
  // Shoulder cables branch toward the sternum, in shadow, like veins.
  const cables=[[[181,0],[184,47],[197,83],[201,152],[246,187],[286,210],[306,247]],[[260,0],[256,53],[272,112],[273,158],[302,190],[323,207]],[[536,0],[528,49],[526,126],[501,172],[467,190],[445,221]],[[663,24],[658,104],[630,174],[567,190],[512,217],[476,250]],[[94,20],[96,117],[120,165],[165,185],[209,216]],[[711,60],[723,143],[714,204],[674,241],[627,258]]];
  for(const pts of cables)tube(a,pts,7,'mid');
  // Structural collar and rib cage are heavy, machined, and asymmetrical.
  a.poly([[276,196],[313,180],[353,184],[384,196],[415,184],[455,185],[493,206],[477,229],[447,217],[415,213],[384,224],[352,214],[314,214],[290,227]],'dark','ink');
  a.line([[284,200],[315,187],[352,191],[379,202]],'tile',4);
  a.line([[391,202],[416,192],[452,194],[483,211]],'mid',4);
  a.poly([[307,210],[329,221],[438,220],[461,210],[489,238],[494,290],[477,339],[445,366],[323,365],[285,340],[269,288],[276,237]],'ink');
  for(let i=0;i<5;i++){
    const y=219+i*28,spread=[8,0,0,7,20][i];
    tube(a,[[319,y],[290+spread,y+8],[279+spread,y+20],[289+spread,y+31],[317,y+38],[338,y+37]],13,'mid');
    a.line([[316,y-3],[291+spread,y+5],[283+spread,y+15]],'tile',3);
    tube(a,[[450,y+2],[479-spread,y+9],[489-spread,y+20],[478-spread,y+33],[449,y+38],[430,y+37]],13,'dark');
    a.line([[452,y-1],[477-spread,y+7],[484-spread,y+16]],'mid',3);
  }
  // Solid metal socket surrounding a truly transparent 128x96 opening.
  a.rect(312,222,144,112,'ink');a.rect(315,225,138,106,'dark');a.rect(317,226,134,3,'tile');
  a.rect(320,230,128,96,null);
  for(const x of [308,453])for(const y of [239,274,310]){a.rect(x,y,7,10,'mid');a.rect(x,y,2,7,'tile');}
  // Abdomen is a hanging mass, with voids between cables, rather than legs.
  for(const [x,y,dx]of[[283,349,-31],[312,366,-12],[347,348,14],[389,350,1],[427,358,17],[461,343,38]])tube(a,[[x,y],[x+dx,y+31],[x+dx+9,422],[x+dx+5,447]],12,'dark');
  a.poly([[303,372],[347,385],[398,385],[457,368],[470,410],[457,447],[298,447],[286,416]],'night','ink');
  a.line([[304,375],[348,391],[390,391]],'dark',4);
  // Oil only: dark angular spills and a few cyan coolant drips.
  for(const [x,y]of[[112,161],[238,114],[592,183],[678,262]]){a.poly([[x,y],[x+18,y+3],[x+13,y+16],[x+18,y+30],[x+9,y+45],[x+6,y+22],[x-4,y+15]],'ink');a.line([[x+3,y+10],[x+3,y+24],[x+7,y+29],[x+7,y+38]],'coolant',2);}
  for(const [x,y]of[[167,116],[237,201],[552,161],[676,233],[132,290]])a.rect(x,y,3,2,'red');
  if(ROUND>=2)refineBody(a);
  if(ROUND>=3){
    // Reclaimed android chassis embedded in the mass: dark, recognizable salvage.
    for(const [x,y,flip]of[[49,174,false],[669,165,true],[206,282,false],[531,278,true]]){
      const s=flip?-1:1;
      a.poly([[x,y],[x+s*16,y-8],[x+s*36,y+2],[x+s*43,y+24],[x+s*27,y+42],[x+s*4,y+32]],'dark','ink');
      a.line([[x+s*3,y+1],[x+s*16,y-4],[x+s*31,y+4]],'mid',3);
      a.poly([[x+s*13,y+6],[x+s*29,y+9],[x+s*32,y+22],[x+s*18,y+29],[x+s*10,y+20]],'ink');
      a.line([[x+s*15,y+10],[x+s*25,y+13]],'tile',2);
      tube(a,[[x+s*18,y+36],[x+s*21,y+52],[x+s*9,y+68]],7,'dark');
      a.rect(x+s*18,y+19,2,2,'red');
    }
    // Staggered old tiles over the broad collar: masonry fused into its chest.
    for(const [x,y]of[[300,187],[324,193],[348,198],[419,192],[443,194]]){
      a.poly([[x,y],[x+19,y+5],[x+15,y+15],[x+3,y+14],[x-3,y+8]],'dark','ink');
      a.line([[x,y+1],[x+15,y+5]],'mid',2);
      a.line([[x+5,y+4],[x+2,y+10]],'tile',1);
    }
    // Hardware along the rib edges is deliberately irregular; avoid perfect cloning.
    for(const [x,y]of[[286,246],[288,274],[301,311],[309,348],[477,251],[470,309],[455,345]]){
      a.rect(x,y,7,4,'ink');a.rect(x,y,3,2,'tile');
    }
    // Thin broken cable filaments at the outline, distinct from the big hoses.
    for(const pts of [[[34,212],[20,200],[9,177],[4,152]],[[734,237],[750,229],[760,202],[766,171]],[[170,316],[160,334],[160,357]],[[610,312],[619,324],[617,339]]]){
      a.line(pts,'ink',4);a.line(pts.map(([x,y])=>[x-1,y]),'dark',2);
    }
  }
  if(ROUND>=4){
    // Broken tile ends and cable clamps provide scale without brightening the mass.
    clipped(a,b=>{
      for(const [x,y]of[[84,114],[123,107],[592,108],[629,103],[34,293],[700,324]]){
        b.poly([[x,y],[x+5,y+1],[x+3,y+4],[x+8,y+7],[x+2,y+8]],'ink');
        b.line([[x-2,y-2],[x+1,y-1]],'tile',1);
      }
    },['dark','mid','tile']);
    for(const [x,y]of[[198,142],[270,141],[526,120],[628,177],[246,322],[517,312]]){
      a.rect(x-4,y,9,5,'ink');a.rect(x-3,y,6,2,'mid');a.px(x-3,y,'tile');
    }
    // Unlit connector rails frame the empty monitor without drawing a UI.
    for(const [x,y]of[[300,114],[460,95]]){a.rect(x,y,6,20,'ink');a.rect(x+1,y+2,2,16,'dark');}
  }
  return a;
}
function shell(a,cx,cy,w,h,variant=0){
  const pts=[[cx-w/2,cy+4],[cx-w/2+5,cy-h/3],[cx-8,cy-h/2],[cx+9,cy-h/2+3],[cx+w/2,cy-4],[cx+w/2-6,cy+h/3],[cx+4,cy+h/2],[cx-12,cy+h/2-2]].map(p=>p.map(Math.round));
  a.poly(pts,'red','ink');
  a.poly([[cx+6,cy-h/2+5],[cx+w/2-3,cy],[cx+w/2-8,cy+h/3],[cx+3,cy+h/2-2],[cx-4,cy+3]],'dark');
  a.line([[cx-14,cy-Math.floor(h/2)+6],[cx-24,cy-8],[cx-19,cy+8]],'warm',3);
  a.line([[cx+1,cy-Math.floor(h/2)+3],[cx-6,cy-2],[cx+1,cy+Math.floor(h/2)-1]],'ink',3);
  a.poly([[cx+11,cy-5],[cx+21,cy-9],[cx+25,cy-2],[cx+18,cy+6],[cx+9,cy+4]],'ink');
  a.line([[cx-12,cy+11],[cx-21,cy+16],[cx-20,cy+23]],'ink',2);
  if(variant){a.line([[cx-7,cy-19],[cx-13,cy-9],[cx-7,cy-4]],'tile',2);}
}
function bug(lit=false){const a=new Raster(160,256);
  // Bent arm assembled from distinct empty insect carapaces; small legs remain.
  tube(a,[[111,8],[104,48],[82,90],[62,131],[71,183],[80,234]],26,'dark');
  const shells=[[106,30,66,48],[92,76,69,57],[63,119,66,61],[64,163,66,49],[84,194,64,49]];
  for(let i=0;i<shells.length;i++){
    const [x,y,w,h]=shells[i];
    for(const side of [-1,1])for(let j=0;j<2;j++){
      const x1=x+side*21,y1=y-5+j*14,x2=x+side*(40+j*3),y2=y-18+j*18,x3=x+side*(45+j*3),y3=y-37+j*26;
      a.line([[x1,y1],[x2,y2],[x3,y3]],'ink',7);a.line([[x1-1,y1-2],[x2-1,y2-2],[x3,y3]],'red',3);
    }
    shell(a,x,y,w,h,i%2);
  }
  // Three up-curled claw digits surround the low central palm, with thumb at right.
  for(const pts of [ [[57,208],[34,211],[26,230],[34,245],[47,248],[36,233],[42,223],[58,226]],[[99,208],[126,206],[143,220],[145,235],[136,226],[124,222],[109,232]],[[56,229],[48,245],[57,253],[67,251],[61,240]] ]){
    a.poly(pts,'red','ink');a.line(pts.slice(0,3),'warm',2);
  }
  a.poly([[68,208],[91,206],[110,220],[107,242],[91,255],[68,255],[54,241],[56,221]],'red','ink');
  a.poly([[83,215],[102,222],[101,238],[88,250],[72,249],[62,237],[65,221]],'dark','ink');
  a.poly([[72,221],[88,218],[98,228],[95,241],[81,249],[68,239],[67,229]],lit?'tell':'ink');
  a.line([[71,222],[81,220],[90,223]],lit?'white':'red',3);
  a.line([[72,230],[78,238],[86,229]],lit?'warm':'dark',3);
  if(lit){a.rect(71,225,4,5,'white');a.rect(78,227,5,11,'tell');a.line([[61,218],[66,214],[72,214]],'tell',2);a.line([[100,225],[102,233],[98,241]],'tell',2);}
  a.line([[78,180],[75,186],[77,198],[73,204]],'ink',4);a.line([[78,184],[78,194]],'coolant',2);
  if(ROUND>=2){
    // Elytra ridges and abdominal plates identify dead insect husks, not rocks.
    for(const [cx,cy,w,h]of shells){
      clipped(a,b=>{
        b.poly([[cx-26,cy-11],[cx-17,cy-19],[cx-11,cy-16],[cx-15,cy-6],[cx-13,cy+9],[cx-21,cy+17],[cx-27,cy+6]],'warm');
        b.poly([[cx-22,cy-8],[cx-16,cy-13],[cx-19,cy+5],[cx-23,cy+10]],'red');
        b.line([[cx-17,cy-19],[cx-10,cy-22],[cx-5,cy-17]],'tile',1);
        b.poly([[cx-8,cy+9],[cx-4,cy+5],[cx+1,cy+10],[cx-4,cy+20],[cx-9,cy+22]],'ink');
        for(let j=0;j<3;j++)b.line([[cx+9+j*3,cy+8+j*5],[cx+18+j*3,cy+2+j*5]],'mid',2);
        b.rect(cx-22,cy-1,3,4,'ink');b.rect(cx-20,cy+7,2,3,'ink');
      },['red','dark','warm','mid']);
      a.line([[cx+6,cy+Math.floor(h/2)-4],[cx+15,cy+Math.floor(h/2)+2],[cx+20,cy+Math.floor(h/2)-2]],'ink',4);
    }
    a.line([[126,41],[134,46],[138,58],[131,72]],'ink',5);
    a.line([[125,41],[132,47],[136,58],[130,68]],'warm',2);
    a.poly([[26,121],[16,126],[11,141],[18,151],[15,139],[23,136],[31,139]],'red','ink');
    a.line([[23,128],[18,134]],'warm',2);
    a.line([[59,157],[57,166],[61,173],[60,181]],'ink',3);
    a.line([[60,161],[60,168]],'coolant',2);
    a.line([[68,246],[75,250],[86,251]],lit?'tell':'red',2);
  }
  if(ROUND>=3){
    // A second scale of husks interrupts the repeated big elytra.
    for(const [x,y]of[[126,25],[42,94],[93,147]]){
      a.poly([[x-13,y-5],[x-5,y-12],[x+7,y-10],[x+14,y-2],[x+10,y+9],[x-1,y+13],[x-12,y+7]],'dark','ink');
      a.poly([[x-11,y-4],[x-4,y-9],[x+2,y-6],[x-1,y+9],[x-10,y+5]],'red');
      a.line([[x-9,y-5],[x-5,y-7]],'warm',2);
      a.line([[x+1,y-8],[x-3,y+8]],'ink',2);
      for(let i=0;i<3;i++)a.line([[x+7,y+i*3-3],[x+12,y+i*3-5]],'mid',1);
      a.line([[x-8,y+6],[x-17,y+10],[x-19,y+5]],'red',2);
      a.line([[x+7,y+6],[x+18,y+11],[x+21,y+4]],'red',2);
    }
    // Hollow socket in the upper shell and tiny chips in chitin ridges.
    a.poly([[99,18],[105,15],[111,20],[109,29],[102,32],[97,26]],'ink');
    a.line([[99,18],[104,16]],'warm',1);
    for(const [x,y]of[[83,47],[63,83],[39,116],[43,165],[71,189]]){
      clipped(a,b=>{b.rect(x,y,4,2,'ink');b.rect(x-2,y-2,3,1,'tile');},['red','warm','dark']);
    }
    // Palm reads as an open mechanical swarm port with radial shutters.
    for(const pts of [[[67,229],[74,232]],[[87,221],[83,228]],[[94,236],[87,235]],[[79,247],[80,240]]])a.line(pts,lit?'warm':'mid',2);
  }
  if(ROUND>=4){
    // Claw knuckles and dark segmented undersides, with top-left glints only.
    for(const [x,y]of[[36,220],[126,217],[55,245]]){
      clipped(a,b=>{b.rect(x,y,6,3,'ink');b.rect(x-1,y-2,5,1,'warm');},['red','warm']);
    }
    a.line([[65,212],[70,208],[80,208]],'warm',1);
    a.line([[89,247],[96,243],[101,236]],'ink',2);
  }
  return a;
}
function blade(lit=false){const a=new Raster(160,320);
  tube(a,[[66,5],[75,56],[97,102],[91,141]],25,'dark');
  // Broad riveted textile-covered brigandine sleeve, not stacked samurai lamellae.
  a.poly([[31,5],[71,2],[103,17],[117,50],[109,79],[82,89],[48,71],[32,42]],'ink');
  a.poly([[34,8],[70,5],[100,20],[111,49],[104,73],[82,81],[51,67],[36,40]],'brass');
  a.poly([[39,12],[69,10],[95,23],[105,48],[99,68],[80,75],[55,62],[42,38]],'red','ink');
  a.poly([[84,20],[98,27],[106,48],[100,67],[83,75],[76,64],[83,47]],'dark');
  a.line([[41,15],[64,13],[78,18]],'warm',3);
  for(const [x,y]of[[49,24],[68,25],[86,31],[53,42],[72,43],[91,49],[62,59],[81,62]])stud(a,x,y);
  // Elbow articulation: a flexible dark joint and two hardware straps.
  a.poly([[58,75],[89,86],[108,78],[119,105],[108,128],[78,116]],'dark','ink');
  a.line([[65,79],[87,90],[107,85]],'brass',4);a.line([[73,96],[91,106],[114,100]],'mid',6);
  a.ellipse(100,110,13,13,'ink');a.ellipse(98,108,10,10,'mid');a.ellipse(96,106,4,4,'steel');
  a.poly([[88,115],[113,114],[127,137],[119,164],[99,180],[69,161],[70,136]],'brass','ink');
  a.poly([[89,121],[109,121],[119,139],[113,157],[98,170],[77,157],[77,139]],'red','ink');
  a.poly([[109,129],[119,142],[111,158],[100,167],[95,151]],'dark');
  a.line([[82,136],[90,125],[101,125]],'warm',3);
  for(const [x,y]of[[86,135],[103,138],[83,151],[101,153]])stud(a,x,y);
  // Short straight grip, compact oval guard, single cutting edge and pointed end.
  a.rect(72,147,16,34,'ink');a.rect(75,149,10,31,'warm');
  for(let y=151;y<178;y+=6){a.line([[75,y],[84,y+4]],'ink',2);a.px(75,y,'brass');}
  a.poly([[57,150],[65,142],[78,146],[87,155],[90,173],[83,183],[61,179],[53,167]],'red','ink');
  for(let i=0;i<4;i++){const y=151+i*7;a.poly([[60,y],[70,y-1],[81,y+4],[81,y+9],[69,y+5],[59,y+5]],'red','ink');stud(a,61,y);}
  a.poly([[57,182],[64,177],[96,177],[103,182],[99,189],[61,189]],'brass','ink');
  a.line([[62,180],[94,180]],'silver',2);a.rect(72,189,18,8,'brass');
  // Constant vertical spine x=89. Straight edge x=71 until the final pointed tip.
  a.poly([[71,196],[89,196],[89,299],[80,319],[71,304]],'ink');
  a.poly([[73,197],[87,197],[87,299],[80,316],[73,303]],'steel');
  a.poly([[73,197],[77,197],[77,303],[80,316],[73,303]],lit?'tell':'edge');
  a.rect(80,198,2,103,'mid');a.line([[87,199],[87,299],[80,316]],'silver',1);
  if(lit){a.line([[73,198],[73,302],[80,316]],'white',1);a.line([[76,199],[76,302]],'tell',2);}
  a.px(80,319,'ink');
  if(ROUND>=2){
    // Break the shield-like fill into textile seams over riveted internal plates.
    clipped(a,b=>{
      b.poly([[46,15],[53,18],[56,38],[63,52],[57,60],[48,44]],'warm');
      b.poly([[69,13],[76,20],[72,36],[78,53],[75,70],[67,62],[70,47],[65,28]],'dark');
      b.line([[42,19],[48,40],[55,55]],'ink',2);
      b.line([[55,20],[61,42],[71,62]],'ink',1);
      b.line([[45,36],[69,39],[89,46]],'ink',1);
      b.line([[51,53],[72,57],[91,62]],'ink',1);
      b.poly([[100,33],[106,40],[105,59],[101,62],[98,52]],'ink');
      b.line([[81,131],[86,140],[82,151],[87,162]],'warm',3);
      b.poly([[109,143],[105,151],[110,159],[104,162],[101,151],[105,141]],'ink');
    },['red','warm','dark']);
    for(const [x,y]of[[49,24],[68,25],[86,31],[53,42],[72,43],[91,49],[62,59],[81,62]])stud(a,x,y);
    a.line([[35,8],[53,6],[70,6]],'silver',1);
    a.line([[106,46],[105,53],[102,56]],'coolant',2);
    a.rect(101,57,2,3,'cyan');
    a.line([[82,213],[85,216]],'ink',1);a.line([[82,267],[85,264]],'ink',1);
    a.line([[74,232],[76,230]],lit?'white':'silver',1);
  }
  if(ROUND>=3){
    a.rect(22,0,104,84,null);
    // Wide hanging dujeong-gap sleeve with a blunt cuff, cloth edge and stud rows.
    // The silhouette is a sleeve, not an isolated pointed heraldic shield.
    a.poly([[35,4],[66,2],[89,11],[104,26],[113,54],[110,76],[92,88],[55,75],[39,50],[29,25]],'ink');
    a.poly([[36,8],[65,6],[86,15],[99,29],[107,55],[104,73],[91,81],[59,70],[44,47],[35,25]],'red');
    a.poly([[76,13],[91,23],[102,47],[106,61],[102,73],[92,81],[80,74],[86,53],[80,37]],'dark');
    a.poly([[38,12],[48,14],[52,31],[52,49],[63,64],[57,65],[44,46],[37,26]],'warm');
    a.line([[36,8],[47,5],[65,6],[84,14],[96,25]],'brass',3);
    a.line([[36,12],[36,26],[44,47],[58,70],[91,83],[107,74]],'brass',2);
    a.line([[42,10],[59,9],[70,13]],'silver',1);
    a.line([[46,20],[50,38],[59,56],[66,68]],'ink',1);
    a.line([[63,15],[66,33],[74,51],[79,70]],'ink',2);
    a.line([[39,29],[56,32],[77,36],[98,43]],'ink',1);
    a.line([[47,49],[65,51],[83,58],[106,61]],'ink',1);
    for(const [x,y]of[[43,19],[58,21],[74,26],[89,32],[50,39],[67,41],[84,46],[97,51],[58,58],[75,62],[94,67]])stud(a,x,y);
    // Worn cloth tabs at the cuff and compact metal knuckle fasteners.
    a.poly([[57,74],[65,78],[64,86],[60,84]],'red','ink');
    a.poly([[74,80],[82,84],[80,91],[75,89]],'red','ink');
    a.line([[104,51],[101,57],[103,62],[99,68]],'ink',3);
    a.line([[103,55],[103,59]],'coolant',1);
    a.rect(102,62,1,2,'cyan');
    for(const [x,y]of[[79,124],[91,121],[118,140]]){a.rect(x,y,4,2,'ink');a.px(x,y-1,'silver');}
    a.line([[78,87],[86,91],[98,90]],'tile',2);
  }
  if(ROUND>=4){
    // Small, flat oval hwando-style guard fittings; no katana curvature or hamon.
    a.rect(61,182,3,3,'warm');a.rect(95,182,3,3,'warm');
    a.rect(70,190,3,5,'ink');a.rect(88,190,3,5,'ink');
    a.line([[77,125],[83,120],[90,119]],'silver',1);
    a.line([[110,163],[106,168],[101,171]],'ink',2);
  }
  return a;
}
function hatch(){const a=new Raster(64,96);
  a.poly([[17,0],[63,0],[63,95],[15,95],[3,83],[0,58],[0,29],[7,9]],'ink');
  a.poly([[19,3],[60,3],[60,92],[17,92],[6,80],[3,57],[3,30],[10,12]],'dark');
  for(let i=0;i<5;i++){const y=5+i*18;
    a.poly([[17,y],[59,y+2],[61,y+12],[53,y+15],[17,y+10],[8,y+14],[6,y+8]],'mid','ink');
    a.line([[12,y+3],[19,y+1],[56,y+5]],'steel',2);a.line([[15,y+7],[53,y+11]],'tile',2);
  }
  a.rect(57,2,6,92,'ink');a.rect(58,3,3,88,'tile');
  for(const y of [15,43,71]){a.rect(0,y,9,10,'ink');a.rect(1,y+1,7,7,'tile');a.rect(1,y+1,2,5,'steel');}
  a.line([[26,26],[29,36],[25,44],[27,52]],'ink',4);a.line([[28,37],[28,44]],'coolant',2);
  if(ROUND>=4){
    for(const [x,y]of[[21,13],[36,49],[19,77]]){
      a.rect(x,y,5,2,'ink');a.rect(x-1,y-2,3,1,'steel');
    }
    for(const y of [10,45,80]){a.rect(59,y,3,3,'ink');a.px(59,y,'silver');}
  }
  return a;
}
function core(){const a=new Raster(48,48);
  a.poly([[12,1],[35,1],[46,12],[46,35],[35,46],[12,46],[1,35],[1,12]],'ink');
  a.poly([[13,4],[34,4],[43,13],[43,34],[34,43],[13,43],[4,34],[4,13]],'mid');
  a.line([[5,14],[14,5],[32,5]],'steel',2);
  a.poly([[17,8],[31,8],[39,17],[39,31],[31,39],[17,39],[8,31],[8,17]],'brass','ink');
  a.poly([[18,12],[29,12],[35,18],[35,29],[29,35],[18,35],[12,29],[12,18]],'yellow');
  a.rect(18,16,12,17,'ink');a.rect(20,17,8,13,'brass');a.rect(21,18,5,9,'yellow');a.rect(21,18,3,3,'white');
  for(const [x,y]of[[20,3],[40,20],[20,40],[3,20]])a.rect(x,y,5,5,'ink');
  if(ROUND>=4){
    a.rect(14,14,20,21,'yellow');
    // Three actual muzzle bores replace the accidental numeral-shaped center.
    for(const [x,y]of[[17,18],[27,18],[22,28]]){
      a.rect(x-1,y-1,7,7,'brass');a.rect(x,y,5,5,'ink');a.rect(x+1,y+1,3,3,'dark');a.px(x,y,'white');
    }
    a.line([[12,29],[16,34],[20,36]],'brass',2);
    for(const [x,y]of[[8,14],[34,32]]){a.rect(x,y,5,3,'ink');a.rect(x,y,3,1,'steel');}
    a.rect(19,6,9,3,'yellow');a.rect(6,20,3,8,'brass');a.rect(39,20,3,8,'brass');
  }
  if(ROUND>=5){
    // A single horizontal barrel bank avoids the three-bore accidental face.
    a.rect(13,14,23,21,'yellow');a.rect(11,19,27,13,'brass');
    a.rect(12,20,25,10,'ink');a.rect(12,19,25,1,'white');
    for(const x of [14,20,26,32]){a.rect(x,22,3,6,'dark');a.rect(x,21,3,1,'steel');}
    a.rect(17,33,14,2,'brass');a.rect(18,12,11,3,'yellow');
  }
  return a;
}
function hero(){const ref=decode(path.resolve(OUT,'../../../../../Assets/Resources/DraftSprites/hero_idle_4f.png')).crop(0,0,64,64);for(let i=0;i<ref.d.length;i+=4){const alpha=ref.d[i+3];const c=alpha>=128?rgba('edge'):[0,0,0,0];for(let k=0;k<4;k++)ref.d[i+k]=c[k];}return ref;}
function preview(parts,active=false){const a=new Raster(960,540,'#0b0a14');
  // Sparse arena architecture; all boss parts remain at 1x native pixel density.
  for(const [x,y,w]of[[0,295,68],[74,341,33],[854,278,59],[920,318,40]]){a.rect(x,y,w,205,'night');a.rect(x+8,y+17,6,11,'dark');a.rect(x+w-17,y+42,6,12,'dark');}
  a.blit(parts.boss_body,96,14);
  a.blit(parts.boss_hatch_core,456,268);
  a.blit(parts.boss_hatch_half,416-(active?35:0),244);
  a.blit(parts.boss_hatch_half,480+(active?35:0),244,true);
  a.blit(parts[active?'boss_bug_arm_lit':'boss_bug_arm'],173,152);
  a.blit(parts[active?'boss_blade_arm_lit':'boss_blade_arm'],630,143);
  a.rect(0,466,960,7,'ink');a.rect(0,467,960,3,'steel');a.rect(0,473,960,16,'dark');
  for(let x=0;x<960;x+=48){a.rect(x,473,2,15,'ink');a.rect(x+4,474,38,2,'tile');}
  a.rect(0,489,960,5,'ink');a.rect(0,494,960,10,'night');
  for(const x of [90,250,676,846]){a.poly([[x,497],[x+44,497],[x+37,539],[x+7,539]],'dark','ink');a.rect(x+8,500,4,40,'mid');}
  for(const x of [128,727])tube(a,[[x,503],[x+24,521],[x+84,527],[x+112,503]],3,'mid');
  a.blit(hero(),449,403);
  return a;
}
const parts={boss_body:body(),boss_bug_arm:bug(),boss_bug_arm_lit:bug(true),boss_blade_arm:blade(),boss_blade_arm_lit:blade(true),boss_hatch_half:hatch(),boss_hatch_core:core()};
fs.mkdirSync(path.join(OUT,'review'),{recursive:true});
for(const [name,a]of Object.entries(parts))a.save(name+'.png');
preview(parts).save('preview_1x.png');preview(parts,true).scale(2).save('preview_2x.png');
preview(parts,true).save('review/round_'+ROUND+'_1x.png');
const sheet=new Raster(840,380,'#0b0a14');sheet.blit(parts.boss_bug_arm,10,15);sheet.blit(parts.boss_bug_arm_lit,172,15);sheet.blit(parts.boss_blade_arm,340,15);sheet.blit(parts.boss_blade_arm_lit,502,15);sheet.blit(parts.boss_hatch_half,690,20);sheet.blit(parts.boss_hatch_core,700,145);sheet.save('parts_1x.png');
parts.boss_bug_arm_lit.scale(4).save('review/bug_4x.png');parts.boss_blade_arm_lit.scale(4).save('review/blade_4x.png');parts.boss_hatch_half.scale(4).save('review/hatch_4x.png');parts.boss_body.crop(268,178,232,198).scale(4).save('review/chest_4x.png');
const metrics={round:ROUND,parts:{},palette:[],preview:{size:[1920,1080],background:'#0B0A14',nativeArena:[960,540],nearestNeighbour:2}};
const all=new Set();
for(const [name,a]of Object.entries(parts)){const colors=new Set(),alpha=new Set();let n=0,luma=0;for(let i=0;i<a.d.length;i+=4){alpha.add(a.d[i+3]);if(a.d[i+3]){const hex='#'+a.d.subarray(i,i+3).toString('hex');colors.add(hex);all.add(hex);n++;luma+=(.2126*a.d[i]+.7152*a.d[i+1]+.0722*a.d[i+2])/255;}}
  metrics.parts[name]={size:[a.w,a.h],opaqueColors:colors.size,alpha:[...alpha].sort((a,b)=>a-b),opaquePixels:n,meanLuma:Number((luma/n).toFixed(4)),palette:[...colors].sort()};
  if(colors.size>16||[...alpha].some(x=>x!==0&&x!==255))throw Error('Palette/alpha constraint failed');
  if(!decode(path.join(OUT,name+'.png')).d.equals(a.d))throw Error('PNG roundtrip failed');
}
metrics.palette=[...all].sort();
for(const pair of [['boss_bug_arm','boss_bug_arm_lit'],['boss_blade_arm','boss_blade_arm_lit']]){const a=parts[pair[0]],b=parts[pair[1]];for(let i=3;i<a.d.length;i+=4)if(a.d[i]!==b.d[i])throw Error('Tell silhouette changed');}
// Validate requested recess geometry and authored palette membership.
for(let y=230;y<326;y++)for(let x=320;x<448;x++)if(parts.boss_body.d[(y*768+x)*4+3]!==0)throw Error('Chest aperture obstructed');
for(let y=72;y<160;y++)for(let x=312;x<456;x++){const i=(y*768+x)*4;if(!parts.boss_body.d.subarray(i,i+4).equals(Buffer.from(rgba('ink'))))throw Error('Monitor not blank');}
for(const c of all)if(!Object.values(P).includes(c))throw Error('Unapproved sprite color');
const p1=preview(parts,true),p2=decode(path.join(OUT,'preview_2x.png'));
for(let y=0;y<1080;y++)for(let x=0;x<1920;x++){const i=(y*1920+x)*4,j=(Math.floor(y/2)*960+Math.floor(x/2))*4;if(!p2.d.subarray(i,i+4).equals(p1.d.subarray(j,j+4)))throw Error('Nearest neighbour mismatch');}
const hp=hero();let minY=64,maxY=0;for(let y=0;y<64;y++)for(let x=0;x<64;x++)if(hp.d[(y*64+x)*4+3]){minY=Math.min(minY,y);maxY=Math.max(maxY,y);}
metrics.preview.hero={canvas:[64,64],visibleHeight:maxY-minY+1,source:'Assets/Resources/DraftSprites/hero_idle_4f.png, frame 0 alpha >=128, recolored as silhouette',topLeft:[449,403]};
metrics.checks={rgbaPngRoundTrip:true,hardAlpha:true,allSpritesAtMost16Colors:true,total16Colors:all.size===16,baseAndLitSameAlpha:true,transparentHatch128x96:true,blankHologram144x88:true,nearestNeighbour2x:true};
fs.writeFileSync(path.join(OUT,'metrics.json'),JSON.stringify(metrics,null,2)+'\n');
console.log(JSON.stringify({round:ROUND,parts:Object.fromEntries(Object.entries(metrics.parts).map(([k,v])=>[k,{size:v.size,colors:v.opaqueColors}])),unionColors:all.size},null,2));
