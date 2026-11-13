// Gwiin portrait DRAFTS. Original integer-pixel drawings by Codex.
// No image generation, tracing, external art, filtering or quantization.
// Run from any directory: node path/to/draw_drafts.cjs
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const OUT = __dirname;
const ROOT = path.resolve(OUT, '../../../../..');
const P = {
  ink: '#080a14', deep: '#131424', hair: '#252433', hairMid: '#3b384e', hairHi: '#555369',
  coatDark: '#1a1c2d', coat: '#2d345d', coatHi: '#4d4c6d',
  skinDark: '#b77569', skinMid: '#d69a83', skin: '#f8d0b2', lip: '#87545f',
  clothShade: '#848095', cloth: '#ddd9da',
  goldDark: '#79512c', gold: '#b8862e', goldHi: '#e0b75e',
  teal: '#43b3c0', cyan: '#7ef9f6',
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

function draftA() {
  const a = new Canvas();
  // High ponytail: an off-centre silhouette, not a mirrored head.
  a.poly([[26,15],[28,10],[34,7],[40,9],[43,14],[42,23],[36,34],[32,48],[32,60],[24,68],[17,69],[21,59],[19,48],[21,35],[19,29],[21,21]], 'deep', 'ink');
  a.poly([[25,23],[28,16],[33,12],[38,13],[39,18],[35,29],[28,37],[26,51],[27,59],[23,62],[22,49],[23,36]], 'hair');
  a.poly([[25,25],[29,18],[33,16],[34,18],[30,29],[26,33]], 'hairMid');
  a.line([[34,8],[39,10],[41,13]], 'teal');
  a.line([[23,55],[23,59],[20,65]], 'hairMid');
  a.poly([[30,22],[36,20],[39,23],[37,27],[31,28]], 'goldDark', 'ink');
  a.line([[31,23],[36,22],[37,24]], 'gold');
  // Back of hair and shoulders.
  a.poly([[36,20],[45,14],[58,14],[67,20],[72,30],[73,47],[69,58],[67,70],[55,75],[34,68],[31,48]], 'hair', 'ink');
  a.poly([[32,68],[42,65],[57,67],[67,71],[74,76],[80,82],[83,93],[84,99],[13,99],[17,87],[22,76]], 'coatDark', 'ink');
  a.poly([[22,77],[31,72],[40,78],[42,99],[15,99],[18,88]], 'coat');
  a.poly([[66,73],[74,79],[78,86],[81,99],[68,99],[62,80]], 'coat');
  a.poly([[23,78],[28,76],[34,83],[37,96],[34,96],[29,85]], 'coatHi');
  a.line([[70,77],[75,81],[79,89],[81,99]], 'teal');
  a.line([[69,73],[74,76],[78,81],[80,87],[82,94],[82,99]], 'cyan');
  // Neck, fully closed undercollar, crossed jeogori dongjeong.
  a.poly([[43,57],[59,59],[59,68],[64,72],[54,83],[39,71],[44,67]], 'skinMid', 'ink');
  a.poly([[46,62],[57,64],[57,70],[53,74],[45,68]], 'skinDark');
  a.poly([[41,68],[50,75],[59,70],[64,74],[56,87],[48,88],[36,73]], 'clothShade', 'ink');
  a.poly([[39,70],[43,69],[52,79],[49,84]], 'cloth');
  a.poly([[59,70],[63,72],[56,82],[49,93],[44,99],[39,99],[48,85]], 'cloth');
  a.poly([[34,72],[39,75],[47,84],[43,90],[39,86],[34,83],[30,75]], 'coat', 'ink');
  a.line([[34,74],[40,81],[44,85]], 'coatHi');
  a.poly([[64,73],[67,77],[63,87],[55,94],[52,99],[46,99],[53,86]], 'coat', 'ink');
  a.line([[63,77],[60,86],[54,91]], 'coatHi');
  // Face planes: large lit cheek with one shadow step at the far side.
  a.poly([[41,30],[53,27],[63,29],[67,35],[67,45],[69,49],[69,51],[66,53],[65,59],[60,64],[55,66],[48,63],[41,57],[38,45]], 'skinMid', 'ink');
  a.poly([[45,31],[55,30],[62,32],[64,37],[63,45],[65,49],[64,52],[65,54],[62,59],[56,63],[51,62],[45,58],[41,50],[42,39]], 'skin');
  a.poly([[40,44],[43,47],[44,56],[49,61],[48,63],[42,59],[38,52]], 'skinDark');
  a.poly([[36,43],[40,42],[43,46],[43,53],[40,55],[36,51]], 'skinMid', 'ink');
  a.line([[38,46],[40,46],[40,50],[41,51]], 'skinDark');
  // Quiet eyes, separate brows, asymmetric eye widths in the turn.
  a.line([[46,39],[49,38],[53,39]], 'hairMid');
  a.line([[59,38],[62,38],[64,39]], 'hairMid');
  a.line([[45,43],[48,42],[52,43]], 'ink');
  a.rect(47,43,5,3,'cloth'); a.rect(50,43,2,3,'hair');
  a.line([[47,46],[50,46]], 'skinMid');
  a.line([[59,42],[62,42],[64,43]], 'ink');
  a.rect(59,43,4,2,'cloth'); a.rect(61,43,2,3,'hair');
  a.line([[59,49],[61,50]], 'skinMid');
  a.line([[55,55],[59,55],[61,54]], 'lip');
  a.line([[56,57],[59,57]], 'skinMid');
  // Swept fringe, with three broad hair values rather than individual strands.
  a.poly([[34,28],[38,21],[46,17],[57,17],[65,22],[69,28],[68,36],[64,35],[60,29],[55,31],[48,38],[43,40],[40,46],[36,43],[35,35]], 'hair', 'ink');
  a.poly([[36,28],[41,22],[49,20],[57,20],[63,24],[56,24],[49,28],[44,32],[42,37],[39,39],[40,32]], 'hairMid');
  a.poly([[43,23],[48,21],[54,21],[48,24],[44,27],[41,28]], 'hairHi');
  a.poly([[58,25],[63,25],[66,29],[66,33],[63,30]], 'hairMid');
  a.line([[48,15],[58,15],[64,18],[68,22],[70,28],[71,35]], 'cyan');
  a.line([[58,14],[62,16],[65,18]], 'cyan');
  a.line([[70,28],[70,34]], 'cyan');
  a.line([[64,18],[68,23],[69,29]], 'teal');
  // Radio earpiece and short boom, the key identifying prop.
  a.poly([[33,43],[37,41],[40,43],[41,51],[38,55],[34,53]], 'deep', 'ink');
  a.rect(35,44,3,7,'coatHi'); a.rect(35,45,2,4,'gold');
  a.line([[38,52],[43,56],[49,56]], 'ink');
  a.line([[39,52],[44,55],[48,55]], 'clothShade');
  a.rect(48,54,4,3,'ink'); a.rect(49,54,2,1,'gold');
  a.line([[34,54],[33,62],[35,69]], 'ink');
  // One restrained square luck token on the jacket, not a neon badge.
  a.line([[62,84],[63,89]], 'goldDark');
  a.poly([[62,90],[65,90],[66,93],[63,95],[61,93]], 'gold', 'ink');
  a.pixel(63,92,'goldHi');
  return a;
}

function draftB() {
  const a = new Canvas();
  // Low knotted bun to the right; binyeo through the knot, single coin pendant.
  a.poly([[65,31],[73,29],[79,32],[82,38],[81,47],[77,52],[69,52],[65,46]], 'hair', 'ink');
  a.poly([[73,33],[78,35],[79,40],[76,46],[72,48],[69,45],[74,42]], 'hairMid');
  a.line([[76,31],[79,34],[81,39],[80,45]], 'cyan');
  a.line([[65,34],[82,27]], 'ink'); a.line([[66,33],[81,27]], 'gold');
  a.rect(81,25,3,3,'goldDark'); a.rect(81,25,2,2,'goldHi');
  a.line([[79,30],[80,35],[80,40]], 'goldDark');
  a.poly([[79,40],[82,41],[82,44],[79,45],[78,43]], 'gold', 'ink'); a.pixel(80,42,'deep');
  a.poly([[33,25],[42,16],[56,14],[65,18],[71,26],[73,40],[69,57],[60,67],[43,65],[32,52]], 'hair', 'ink');
  // The torso turns away while the face looks back to the left.
  a.poly([[40,65],[57,65],[67,70],[78,75],[85,83],[89,99],[17,99],[21,86],[28,77]], 'coatDark', 'ink');
  a.poly([[58,72],[66,71],[77,77],[82,83],[86,99],[61,99],[53,83]], 'coat');
  a.poly([[24,85],[31,78],[38,77],[40,89],[34,99],[19,99]], 'coat');
  a.poly([[68,76],[76,80],[80,86],[82,96],[79,96],[75,85]], 'coatHi');
  a.line([[68,71],[77,75],[83,81],[86,89],[88,99]], 'cyan');
  a.line([[70,74],[78,79],[82,85]], 'teal');
  // High neck and a sideways wrapped collar, no exposed chest.
  a.poly([[41,56],[58,57],[57,66],[62,70],[52,80],[36,72],[42,66]], 'skinMid', 'ink');
  a.poly([[45,62],[55,60],[54,69],[48,73],[43,68]], 'skinDark');
  a.poly([[37,68],[43,72],[49,74],[58,68],[63,72],[51,87],[37,79],[33,73]], 'clothShade', 'ink');
  a.poly([[36,69],[40,68],[49,78],[45,82],[38,76]], 'cloth');
  a.poly([[57,68],[61,70],[53,81],[44,92],[39,99],[33,99],[43,85]], 'cloth');
  a.poly([[32,73],[38,78],[44,85],[40,90],[32,83],[27,80]], 'coat', 'ink');
  a.poly([[62,72],[67,75],[61,87],[52,96],[49,99],[41,99],[50,86]], 'coat', 'ink');
  a.line([[64,76],[58,87],[50,95]], 'goldDark');
  a.line([[65,77],[59,88],[52,95]], 'gold');
  // Left-facing face, cheek and jaw deliberately uneven across the turn.
  a.poly([[39,29],[52,26],[62,30],[66,39],[64,51],[59,60],[51,66],[45,65],[38,61],[36,55],[34,53],[34,51],[36,47],[35,36]], 'skinMid', 'ink');
  a.poly([[41,31],[50,29],[57,31],[60,37],[59,49],[56,57],[50,62],[45,62],[39,58],[38,53],[35,52],[38,48],[38,38]], 'skin');
  a.poly([[61,34],[64,40],[62,51],[57,60],[51,65],[48,64],[53,59],[57,51],[58,42]], 'skinDark');
  a.poly([[62,41],[66,40],[68,43],[67,50],[63,53],[61,49]], 'skinMid', 'ink');
  a.line([[64,44],[65,43],[65,48],[63,49]], 'skinDark');
  a.line([[38,39],[41,38],[44,38]], 'hairMid');
  a.line([[49,38],[53,37],[56,38]], 'hairMid');
  a.line([[37,43],[40,42],[43,42]], 'ink');
  a.rect(38,43,4,2,'cloth'); a.rect(38,43,2,3,'hair');
  a.line([[48,42],[51,41],[55,42]], 'ink');
  a.rect(49,42,5,3,'cloth'); a.rect(49,42,2,3,'hair');
  a.line([[49,46],[52,46]], 'skinMid');
  a.line([[40,49],[42,50]], 'skinMid');
  a.line([[42,55],[46,55],[49,54]], 'lip');
  a.line([[44,58],[47,58]], 'skinMid');
  // Smooth swept hair, broken at the temple instead of a helmet outline.
  a.poly([[32,34],[32,29],[36,22],[43,18],[54,16],[63,19],[68,25],[70,34],[68,42],[64,45],[61,40],[60,31],[54,29],[49,30],[43,33],[37,34],[35,41],[33,41]], 'hair', 'ink');
  a.poly([[35,29],[40,23],[47,20],[56,19],[63,22],[65,25],[56,23],[48,25],[43,29],[36,32]], 'hairMid');
  a.poly([[42,23],[48,20],[54,20],[48,23],[44,25],[40,27]], 'hairHi');
  a.poly([[61,26],[65,27],[68,34],[66,39],[64,37]], 'hairMid');
  a.line([[52,15],[58,15],[64,18],[69,24],[71,32]], 'cyan');
  a.line([[58,14],[63,16],[66,19]], 'cyan');
  a.line([[70,28],[70,32]], 'cyan');
  a.line([[61,18],[66,23],[69,29]], 'teal');
  a.line([[64,53],[62,58],[58,62]], 'teal');
  // Throat radio with a cable disappearing into the jacket.
  a.poly([[40,67],[46,71],[53,70],[57,67],[59,69],[54,73],[46,74],[39,70]], 'deep', 'ink');
  a.rect(46,70,7,4,'ink'); a.rect(47,70,5,2,'coatHi'); a.rect(50,70,2,1,'gold');
  a.line([[53,73],[56,78],[57,85]], 'ink');
  a.poly([[60,87],[68,87],[70,91],[69,95],[60,95]], 'coatDark', 'ink');
  a.rect(62,89,5,2,'coatHi'); a.pixel(67,90,'gold');
  return a;
}

function draftC() {
  const a = new Canvas();
  // Long hair, one side tucked, broad falling locks on the opposite shoulder.
  a.poly([[30,30],[35,19],[44,13],[57,12],[66,16],[72,23],[75,37],[73,55],[77,74],[76,86],[69,94],[28,92],[23,84],[27,69],[26,54]], 'deep', 'ink');
  a.poly([[30,45],[34,39],[37,45],[35,61],[34,74],[30,87],[26,85],[30,69]], 'hair');
  a.poly([[65,36],[72,34],[72,54],[75,73],[73,84],[68,88],[68,65]], 'hair');
  a.poly([[70,53],[71,60],[73,74],[71,82],[69,83],[70,71]], 'hairMid');
  a.line([[72,46],[72,55],[74,66],[76,76]], 'teal');
  a.line([[74,44],[74,55],[76,66],[77,74]], 'cyan');
  // Squared flight jacket, contrasting with A's fitted wrapped lapels.
  a.poly([[34,68],[48,67],[60,69],[71,74],[79,81],[84,92],[85,99],[13,99],[17,85],[24,75]], 'coatDark', 'ink');
  a.poly([[23,77],[31,73],[39,79],[36,99],[15,99],[19,85]], 'coat');
  a.poly([[63,75],[71,76],[78,83],[82,94],[83,99],[58,99],[56,86]], 'coat');
  a.poly([[23,79],[28,77],[33,81],[31,85],[26,83],[22,91],[19,91]], 'coatHi');
  a.poly([[70,79],[75,83],[79,91],[78,95],[75,88]], 'coatHi');
  a.line([[26,87],[25,99]], 'ink');
  a.line([[27,87],[26,99]], 'clothShade');
  a.rect(27,87,2,3,'goldDark');
  a.line([[66,84],[73,87]], 'ink');
  a.line([[69,74],[76,79],[81,86],[84,96],[84,99]], 'cyan');
  a.line([[69,77],[74,81],[79,89],[81,97]], 'teal');
  a.poly([[42,58],[57,58],[57,68],[62,73],[51,84],[37,73],[42,68]], 'skinMid', 'ink');
  a.poly([[44,61],[55,63],[55,70],[50,74],[43,69]], 'skinDark');
  a.poly([[38,69],[48,76],[57,70],[62,74],[55,85],[45,88],[33,75]], 'clothShade', 'ink');
  a.poly([[38,69],[42,69],[51,79],[47,84],[39,77]], 'cloth');
  a.poly([[57,70],[61,72],[54,83],[47,92],[44,99],[38,99],[45,86]], 'cloth');
  a.poly([[32,72],[38,77],[45,85],[40,91],[34,84],[29,81]], 'coat', 'ink');
  a.poly([[61,74],[65,77],[59,91],[53,99],[46,99],[52,87]], 'coat', 'ink');
  a.line([[60,79],[55,90],[50,97]], 'coatHi');
  // More frontal three-quarter turn, chin slightly lowered, knowing smile.
  a.poly([[38,29],[50,25],[61,28],[67,34],[67,45],[69,50],[67,53],[64,60],[58,65],[52,67],[45,64],[39,59],[35,49],[35,37]], 'skinMid', 'ink');
  a.poly([[41,31],[51,28],[60,31],[63,36],[62,45],[65,50],[64,54],[60,60],[54,63],[48,61],[42,57],[39,49],[38,39]], 'skin');
  a.poly([[36,42],[40,45],[41,54],[45,60],[50,64],[49,65],[43,62],[38,57],[35,48]], 'skinDark');
  a.poly([[33,43],[37,41],[40,44],[41,51],[38,55],[34,52]], 'skinMid', 'ink');
  a.line([[35,46],[37,45],[38,49],[37,51]], 'skinDark');
  a.line([[43,39],[47,38],[51,39]], 'hairMid');
  a.line([[57,38],[61,37],[64,38]], 'hairMid');
  a.line([[43,43],[46,42],[50,43]], 'ink');
  a.rect(44,43,5,3,'cloth'); a.rect(47,43,2,3,'hair');
  a.line([[44,46],[47,46]], 'skinMid');
  a.line([[57,42],[60,41],[63,42]], 'ink');
  a.rect(58,42,4,3,'cloth'); a.rect(60,42,2,3,'hair');
  a.line([[56,50],[58,51]], 'skinMid');
  a.line([[50,56],[56,56],[58,55]], 'lip');
  a.line([[52,59],[56,59]], 'skinMid');
  // Long side-swept fringe and tucked left temple.
  a.poly([[29,35],[31,26],[36,20],[45,16],[57,15],[65,19],[70,25],[72,34],[69,42],[65,42],[63,34],[58,29],[53,30],[48,35],[43,38],[39,40],[37,47],[33,44],[32,37]], 'hair', 'ink');
  a.poly([[32,32],[35,26],[41,21],[49,19],[58,19],[63,22],[55,22],[48,25],[42,31],[37,34],[34,40],[33,40]], 'hairMid');
  a.poly([[40,25],[45,21],[51,20],[54,20],[47,23],[42,27],[38,29]], 'hairHi');
  a.poly([[60,25],[65,26],[69,32],[69,37],[66,36],[65,31]], 'hairMid');
  a.line([[51,13],[58,13],[65,16],[70,21],[73,28],[74,36]], 'cyan');
  a.line([[58,12],[63,14],[66,16]], 'cyan');
  a.line([[73,29],[73,36]], 'cyan');
  a.line([[64,18],[68,22],[71,28]], 'teal');
  // A small radio behind the tucked ear, fine boom below the cheek.
  a.poly([[31,44],[35,42],[37,44],[38,51],[35,54],[32,53]], 'deep', 'ink');
  a.rect(33,45,2,6,'coatHi'); a.pixel(34,46,'goldHi');
  a.line([[36,52],[41,55],[44,56]], 'ink');
  a.line([[37,52],[41,54],[44,55]], 'clothShade');
  a.rect(44,54,3,3,'ink'); a.pixel(45,54,'gold');
  // Restrained gold binyeo clip and one norigae at the jacket closure.
  a.line([[32,36],[39,31]], 'ink'); a.line([[32,35],[38,31]], 'gold'); a.pixel(38,31,'goldHi');
  a.line([[58,85],[60,89],[60,92]], 'goldDark');
  a.poly([[59,91],[62,91],[63,94],[61,96],[58,94]], 'gold', 'ink'); a.pixel(60,93,'deep');
  a.line([[60,96],[59,99]], 'goldDark'); a.line([[62,96],[63,99]], 'gold');
  // One lock in front of the shoulder, left separate from the headset.
  a.poly([[29,55],[32,53],[34,59],[33,70],[31,78],[32,85],[29,91],[26,88],[28,80],[27,71]], 'hair', 'ink');
  a.line([[31,60],[30,71],[29,77]], 'hairMid');
  return a;
}

function stats(a) {
  const colors = new Set(), opaque = new Set(), alphas = new Set(); let visible = 0;
  let left = a.w, top = a.h, right = 0, bottom = 0;
  for (let i = 0; i < a.data.length; i += 4) {
    const alpha = a.data[i + 3]; colors.add(a.data.subarray(i, i + 4).toString('hex')); alphas.add(alpha);
    if (alpha) { opaque.add('#' + a.data.subarray(i, i + 3).toString('hex')); visible++;
      const x = i / 4 % a.w, y = Math.floor(i / 4 / a.w); left = Math.min(left, x); top = Math.min(top, y); right = Math.max(right, x); bottom = Math.max(bottom, y);
    }
  }
  return { width: a.w, height: a.h, mode: 'RGBA8', totalColorsIncludingTransparent: colors.size, opaqueColors: opaque.size,
    alphaValues: [...alphas].sort((x, y) => x - y), visiblePixels: visible, bounds: [left, top, right, bottom], palette: [...opaque].sort() };
}
const drafts = [draftA(), draftB(), draftC()];
const report = {};
for (let i = 0; i < drafts.length; i++) {
  const name = `benefactor_${'abc'[i]}.png`, file = path.join(OUT, name);
  fs.writeFileSync(file, encode(drafts[i]));
  // Check the saved, decoded PNG rather than only the in-memory drawing.
  const saved = decode(file); assert.deepEqual(saved.data, drafts[i].data);
  const s = stats(saved); assert.equal(s.width, 100); assert.equal(s.height, 100);
  assert(s.totalColorsIncludingTransparent <= 24); assert.deepEqual(s.alphaValues, [0, 255]);
  report[name] = s;
}
// Exactly four adjoining 100px panels, Yudo / A / B / C, no resampling.
const preview = new Canvas(400, 100, '#0E1428');
preview.blit(decode(path.join(ROOT, 'Assets/Resources/Portraits/yudo.png')), 0, 0);
drafts.forEach((a, i) => preview.blit(a, (i + 1) * 100, 0));
fs.writeFileSync(path.join(OUT, 'preview_4x.png'), encode(preview.scale(4)));
fs.writeFileSync(path.join(OUT, 'preview_1x.png'), encode(preview));
fs.writeFileSync(path.join(OUT, 'validation.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
