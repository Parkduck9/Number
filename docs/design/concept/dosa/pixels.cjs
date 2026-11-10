const fs=require('node:fs'),zlib=require('node:zlib');
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

module.exports={pngRead,pngWrite,image,pixel,set,paste,crop,resize,bbox};
