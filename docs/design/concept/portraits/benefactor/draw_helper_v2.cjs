// Gwiin portrait DRAFTS. Original integer-pixel drawings by Codex.
// Hand-placed integer pixels at 50x50. No image generation or source downscale.
// DRAFT ONLY. Run: node docs/design/concept/portraits/benefactor/draw_helper_v2.cjs
// Standard library only. Writes the two requested PNGs beside this file.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const OUT = __dirname;
const ROOT = path.resolve(OUT, '../../../../..');
const P = {
  // Darks, skin, collar greys and rim come from Yudo's actual PNG.
  ink: '#000000', deep: '#1a1c2d', hair: '#2d2d44', hairMid: '#4d4c6d', hairHi: '#5d5f7e',
  coatDark: '#1b203c', coat: '#2d345d', coatHi: '#3d5578',
  skinDark: '#b77569', skinMid: '#d69a83', skin: '#f8d0b2', lip: '#87545f',
  clothShade: '#b6acbd', cloth: '#d1ccd2',
  goldDark: '#79512c', gold: '#b8862e', goldHi: '#e0b75e',
  teal: '#64d1d7', cyan: '#7ef9f6',
};
const color = c => Array.isArray(c) ? c : [...Buffer.from((P[c] || c).slice(1), 'hex'), 255];
class Canvas {
  constructor(w = 100, h = 100, bg) {
    this.w = w; this.h = h; this.data = Buffer.alloc(w * h * 4);
    if (bg) this.rect(0, 0, w, h, bg);
  }
  pixel(x, y, c) {
    if (x < 0 || y < 0 || x >= this.w || y >= this.h) return;
    const a = color(c), i = (y * this.w + x) * 4;
    for (let j = 0; j < 4; j++) this.data[i + j] = a[j];
  }
  rect(x, y, w, h, c) { for (let j = y; j < y + h; j++) for (let i = x; i < x + w; i++) this.pixel(i, j, c); }
  line(points, c) {
    for (let i = 1; i < points.length; i++) {
      let [x, y] = points[i - 1]; const [tx, ty] = points[i];
      const dx = Math.abs(tx - x), dy = -Math.abs(ty - y), sx = x < tx ? 1 : -1, sy = y < ty ? 1 : -1;
      let err = dx + dy;
      for (;;) {
        this.pixel(x, y, c); if (x === tx && y === ty) break;
        const e = err * 2; if (e >= dy) { err += dy; x += sx; } if (e <= dx) { err += dx; y += sy; }
      }
    }
  }
  poly(points, fill, outline) {
    const ys = points.map(p => p[1]);
    for (let y = Math.max(0, Math.min(...ys)); y <= Math.min(this.h - 1, Math.max(...ys)); y++) {
      const xs = [];
      for (let i = 0; i < points.length; i++) {
        const a = points[i], b = points[(i + 1) % points.length];
        if ((a[1] <= y && b[1] > y) || (b[1] <= y && a[1] > y)) xs.push(a[0] + (y - a[1]) * (b[0] - a[0]) / (b[1] - a[1]));
      }
      xs.sort((a, b) => a - b);
      for (let i = 0; i + 1 < xs.length; i += 2) for (let x = Math.ceil(xs[i]); x <= Math.floor(xs[i + 1]); x++) this.pixel(x, y, fill);
    }
    if (outline) this.line([...points, points[0]], outline);
  }
  blit(src, x, y) {
    for (let j = 0; j < src.h; j++) for (let i = 0; i < src.w; i++) {
      const k = (j * src.w + i) * 4;
      if (src.data[k + 3]) this.pixel(x + i, y + j, [...src.data.subarray(k, k + 4)]);
    }
  }
  scale(n) {
    const a = new Canvas(this.w * n, this.h * n);
    for (let y = 0; y < a.h; y++) for (let x = 0; x < a.w; x++) {
      const i = (Math.floor(y / n) * this.w + Math.floor(x / n)) * 4;
      a.pixel(x, y, [...this.data.subarray(i, i + 4)]);
    }
    return a;
  }
}
const crcTable = Array.from({ length: 256 }, (_, n) => {
  for (let i = 0; i < 8; i++) n = n & 1 ? 0xedb88320 ^ (n >>> 1) : n >>> 1;
  return n >>> 0;
});
function chunk(type, data) {
  const b = Buffer.alloc(data.length + 12); b.writeUInt32BE(data.length); b.write(type, 4); data.copy(b, 8);
  let c = 0xffffffff; for (const v of b.subarray(4, -4)) c = crcTable[(c ^ v) & 255] ^ (c >>> 8);
  b.writeUInt32BE((c ^ 0xffffffff) >>> 0, b.length - 4); return b;
}
function encode(a) {
  const hdr = Buffer.alloc(13); hdr.writeUInt32BE(a.w); hdr.writeUInt32BE(a.h, 4); hdr[8] = 8; hdr[9] = 6;
  const raw = Buffer.alloc((a.w * 4 + 1) * a.h);
  for (let y = 0; y < a.h; y++) a.data.copy(raw, y * (a.w * 4 + 1) + 1, y * a.w * 4, (y + 1) * a.w * 4);
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', hdr), chunk('IDAT', zlib.deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]);
}
function decode(file) {
  const b = fs.readFileSync(file); assert.equal(b[24], 8); assert.equal(b[25], 6);
  const a = new Canvas(b.readUInt32BE(16), b.readUInt32BE(20)), ids = [];
  for (let p = 8; p < b.length;) {
    const n = b.readUInt32BE(p); if (b.toString('ascii', p + 4, p + 8) === 'IDAT') ids.push(b.subarray(p + 8, p + 8 + n)); p += n + 12;
  }
  const raw = zlib.inflateSync(Buffer.concat(ids)), stride = a.w * 4;
  for (let y = 0; y < a.h; y++) for (let x = 0; x < stride; x++) {
    const f = raw[y * (stride + 1)], i = y * stride + x, l = x >= 4 ? a.data[i - 4] : 0, u = y ? a.data[i - stride] : 0, ul = y && x >= 4 ? a.data[i - stride - 4] : 0;
    let v = 0;
    if (f === 1) v = l; else if (f === 2) v = u; else if (f === 3) v = Math.floor((l + u) / 2);
    else if (f === 4) { const p = l + u - ul, dl = Math.abs(p - l), du = Math.abs(p - u), dul = Math.abs(p - ul); v = dl <= du && dl <= dul ? l : du <= dul ? u : ul; }
    a.data[i] = (raw[y * (stride + 1) + 1 + x] + v) & 255;
  }
  return a;
}

function drawHelper() {
  const a = new Canvas(50, 50);
  // High ponytail: the tie is above the crown; the long tail falls behind
  // the left shoulder. Keep the whole head at Yudo's y=4..31 height.
  a.poly([[16,4],[20,4],[23,6],[24,10],[21,15],[17,20],
    [15,28],[15,34],[12,39],[9,39],[10,32],[9,27],[10,20],
    [12,15],[12,10],[14,6]], 'deep', 'ink');
  a.poly([[16,6],[19,5],[21,7],[21,11],[17,17],[14,23],
    [13,30],[13,35],[11,37],[12,30],[12,25],[14,18],[14,11]], 'hair');
  a.line([[16,8],[18,7],[20,7]], 'hairMid');
  a.line([[17,12],[15,17],[14,22]], 'hairMid');
  a.line([[12,30],[12,34],[11,36]], 'hairMid');
  // Small gold ornament, deliberately subordinate to the face.
  a.line([[16,11],[20,9]], 'goldDark');
  a.line([[16,10],[20,8]], 'gold');
  a.pixel(20,8,'goldHi');

  // Main hair mass, swept towards the right-facing three-quarter face.
  a.poly([[17,12],[20,8],[25,6],[31,6],[35,8],[38,12],
    [39,17],[38,24],[35,29],[31,32],[22,31],[18,28],
    [16,22],[16,16]], 'hair', 'ink');
  a.poly([[17,20],[20,18],[22,24],[22,30],[19,32],[17,30]], 'deep');

  // Low bust: shoulder/bust envelope x=6..39, y=32..49. Shoulder
  // diagonals and the bottom crop follow Yudo rather than the old helper.
  a.poly([[18,33],[23,32],[30,32],[33,34],[35,36],[37,40],
    [39,46],[39,49],[6,49],[7,44],[9,39],[13,36]], 'deep', 'ink');
  a.poly([[10,40],[14,37],[18,36],[22,42],[22,49],[7,49]], 'coatDark');
  a.poly([[31,36],[34,37],[36,41],[38,47],[38,49],[31,49],
    [29,41]], 'coatDark');
  a.poly([[10,41],[13,39],[15,40],[17,46],[16,49],[8,49]], 'hair');
  a.line([[11,41],[14,40],[16,45]], 'hairMid');
  a.line([[34,39],[36,43],[37,47]], 'coat');
  a.line([[14,44],[15,49]], 'ink');

  // Neck, grey white shirt, and two distinct black suit lapels.
  a.poly([[25,29],[32,29],[31,34],[33,36],[28,41],[22,36],
    [25,34]], 'skinMid', 'ink');
  a.poly([[25,31],[30,32],[29,35],[27,36],[25,34]], 'skinDark');
  a.poly([[23,33],[27,36],[31,33],[33,36],[31,44],[31,49],
    [24,49],[24,42],[21,35]], 'clothShade', 'ink');
  a.poly([[27,39],[29,39],[31,38],[30,44],[30,49],[26,49]], 'cloth');
  a.poly([[23,33],[27,36],[26,39],[22,35]], 'cloth', 'ink');
  a.poly([[31,33],[33,35],[29,39],[28,36]], 'cloth', 'ink');
  a.poly([[18,34],[21,33],[25,39],[23,42],[26,49],[21,49],
    [18,43],[16,39]], 'hair', 'ink');
  a.line([[18,37],[20,41],[23,43]], 'hairMid');
  a.poly([[33,34],[35,39],[32,42],[34,44],[30,49],[28,49],
    [31,42]], 'hair', 'ink');
  a.line([[33,38],[32,41]], 'hairMid');

  // Long ponytail overlaps only the rear shoulder, preserving the suit.
  a.poly([[13,25],[16,23],[18,28],[17,34],[15,39],[13,42],
    [11,41],[13,35],[12,31]], 'deep', 'ink');
  a.line([[15,28],[15,33],[14,37],[12,40]], 'hair');

  // Face bounds x=21..37, y=16..31 = 17x16, matching Yudo's
  // exposed skin bounds. Cheek and jaw use Yudo's three skin tones.
  a.poly([[24,16],[29,14],[33,15],[36,18],[36,23],[37,25],
    [36,26],[35,29],[32,31],[29,31],[25,29],[23,26],
    [21,25],[21,22],[23,21]], 'skinMid', 'ink');
  a.poly([[25,18],[29,16],[33,17],[35,19],[35,23],[36,25],
    [35,26],[34,29],[31,30],[28,29],[25,27],[24,23]], 'skin');
  a.line([[23,24],[24,27],[27,29],[29,30]], 'skinDark');
  a.pixel(22,23,'skin');
  a.line([[22,24],[23,25]], 'skinDark');
  a.line([[33,25],[34,26]], 'skinMid');
  // Two-pixel eye clusters with separate eyebrows, no tiny white specks.
  a.line([[26,20],[28,20]], 'hairMid');
  a.line([[33,20],[34,20]], 'hairMid');
  a.rect(26,22,2,1,'ink');
  a.rect(33,22,2,1,'ink');
  a.pixel(28,23,'skinMid');
  // One-pixel mouth: a soft warm mark, with a lifted cheek beside it.
  a.pixel(32,28,'lip');
  a.pixel(33,27,'skinMid');

  // Broad fringe shapes, no strand-by-strand illustration shading.
  a.poly([[17,17],[18,13],[21,10],[26,8],[31,8],[35,10],
    [37,13],[38,18],[36,19],[34,16],[31,14],[28,15],
    [25,18],[23,22],[22,25],[20,26],[19,23]], 'hair', 'ink');
  a.poly([[19,15],[22,12],[26,10],[31,10],[33,11],[28,11],
    [25,13],[22,17],[20,21],[19,22]], 'hairMid');
  a.line([[23,12],[26,10],[29,10]], 'hairHi');
  a.poly([[32,12],[35,13],[36,16],[34,15]], 'hairMid');
  a.line([[22,20],[21,23]], 'deep');

  // Thin headset band and compact boom at the left cheek.
  a.line([[19,18],[20,14],[23,11],[27,9],[31,9]], 'deep');
  a.line([[20,18],[21,14],[24,11]], 'hairHi');
  a.rect(20,22,2,4,'ink');
  a.rect(20,23,1,2,'clothShade');
  a.line([[21,26],[24,28],[27,28]], 'ink');
  a.pixel(27,28,'hairMid');

  // Gold norigae pendant: short cord, small square token and two tassels.
  a.line([[28,42],[28,44]], 'goldDark');
  a.rect(27,44,3,3,'ink');
  a.pixel(28,45,'gold');
  a.pixel(28,44,'goldHi');
  a.line([[27,47],[27,49]], 'goldDark');
  a.line([[29,47],[29,49]], 'gold');

  // One-art-pixel cyan rim on the right silhouette only. Each cyan pixel
  // touches transparency to its right; the inward step is the muted teal.
  a.line([[32,6],[35,8],[38,12],[39,17],[38,23]], 'cyan');
  a.line([[37,25],[36,28],[33,31]], 'cyan');
  a.line([[33,34],[35,36],[37,40],[39,46],[39,49]], 'cyan');
  a.line([[35,10],[37,13],[38,17]], 'teal');
  a.line([[35,39],[37,44]], 'teal');
  return a;
}

function stats(a) {
  const colors = new Set(), alpha = new Set();
  let left=a.w, top=a.h, right=-1, bottom=-1;
  for (let y=0;y<a.h;y++) for (let x=0;x<a.w;x++) {
    const i=(y*a.w+x)*4;
    alpha.add(a.data[i+3]);
    if (!a.data[i+3]) continue;
    colors.add('#'+a.data.subarray(i,i+3).toString('hex'));
    left=Math.min(left,x); top=Math.min(top,y);
    right=Math.max(right,x); bottom=Math.max(bottom,y);
  }
  return {size:[a.w,a.h],opaqueColors:colors.size,
    colorsIncludingTransparency:colors.size+1,alpha:[...alpha].sort((a,b)=>a-b),
    bounds:[left,top,right,bottom],palette:[...colors].sort()};
}

// Measurements are zero-based, inclusive, in 50x50 art coordinates.
// Measured reference landmarks, not uniform downscales used as drawing input:
//                        Yudo             Citizen          helper_v2
// head silhouette*       x9..40 y4..31     x3..45 y4..35    x9..39 y4..31
// head width x height    32 x 28           43 x 32          31 x 28
// eye-line (eye cluster)  y21..23           y26..27          y22
// chin/jaw outline       y31               y35              y31
// shoulder/bust envelope x6..39            x7..41           x6..39
// shoulder/bust width    34                35               34
// top of hair/headwear   y4                y4 (hat)          y4
// whole horizontal span  x6..40            x3..45 (hat)      x6..39
// * Includes hair/hat and rim, excludes neck and the hanging ponytail.
// Yudo has some 1-display-pixel edits; 50-grid measurements use the top-left
// representative of each 2x2 cell. Citizen is exactly aligned to the 2x2 grid.
// Citizen's hat obscures the crown, so its anatomical head height cannot be
// measured from this PNG. The references differ by >2px at eyes/chin: matching
// both within 1px is impossible. Yudo is the requested head-size authority.

const native = drawHelper();
const candidate = native.scale(2);
const outputFile = path.join(OUT,'helper_v2.png');
fs.writeFileSync(outputFile,encode(candidate));
const saved = decode(outputFile);
assert.deepEqual(saved.data,candidate.data);
assert.deepEqual([saved.w,saved.h],[100,100]);
const report=stats(saved);
assert.deepEqual(report.alpha,[0,255]);
assert(report.opaqueColors<=24);
for (let y=0;y<100;y+=2) for (let x=0;x<100;x+=2) {
  const i=(y*100+x)*4;
  for (let dy=0;dy<2;dy++) for (let dx=0;dx<2;dx++) {
    const j=((y+dy)*100+x+dx)*4;
    assert.deepEqual(saved.data.subarray(i,i+4),saved.data.subarray(j,j+4));
  }
}
const referenceNames=['yudo','citizen','helper'];
const references=referenceNames.map(n=>decode(path.join(ROOT,'Assets/Resources/Portraits',n+'.png')));
const sourcePalette=new Set();
for (const a of references) for (let i=0;i<a.data.length;i+=4)
  if (a.data[i+3]) sourcePalette.add('#'+a.data.subarray(i,i+3).toString('hex'));
for (const c of report.palette) assert(sourcePalette.has(c),'New color: '+c);
const comparison = new Canvas(400,100,'#0B1026');
references.forEach((a,i)=>comparison.blit(a,i*100,0));
comparison.blit(saved,300,0);
const compareFile=path.join(OUT,'helper_v2_compare_3x.png');
fs.writeFileSync(compareFile,encode(comparison.scale(3)));
assert.deepEqual(decode(compareFile).data,comparison.scale(3).data);
console.log(JSON.stringify({candidate:report,native:stats(native),
  comparison:{size:[1200,300],order:[...referenceNames,'helper_v2'],background:'#0B1026'},
  checks:['100x100','exact 2x2 grid','binary alpha','<=24 opaque colors',
    'all colors reused from reference PNGs','saved PNG round-trip','nearest 3x comparison']},null,2));

