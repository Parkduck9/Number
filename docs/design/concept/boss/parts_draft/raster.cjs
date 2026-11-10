const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const OUT = __dirname;

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

module.exports={Raster,decode,encode,P,rgba};
