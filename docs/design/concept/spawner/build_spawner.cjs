// DRAFT ONLY. Run: node docs/design/concept/spawner/build_spawner.cjs
// Source: spawner_gemini.png, supplied Google Gemini concept.
// No services, packages, Unity imports, or writes outside this directory.
// Pipeline: source-resolution hand mask -> cell dominant vote -> fixed palette
// -> explicit native-pixel cleanup -> integer nearest-neighbour preview.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const OUT = __dirname;
const ROOT = path.resolve(OUT, '../../../..');

function image(w, h, color = [0, 0, 0, 0]) {
  const data = Buffer.alloc(w * h * 4);
  for (let i = 0; i < w * h; i++) data.set(color, i * 4);
  return { w, h, data };
}
function readPNG(file) {
  const b = fs.readFileSync(file);
  let w, h, type, depth, pal, trans, pos = 8;
  const chunks = [];
  while (pos < b.length) {
    const n = b.readUInt32BE(pos), t = b.toString('ascii', pos + 4, pos + 8);
    const d = b.subarray(pos + 8, pos + 8 + n); pos += n + 12;
    if (t === 'IHDR') {
      w = d.readUInt32BE(0); h = d.readUInt32BE(4); depth = d[8]; type = d[9];
      assert.equal(d[12], 0, 'Interlaced PNG unsupported');
    }
    if (t === 'PLTE') pal = d;
    if (t === 'tRNS') trans = d;
    if (t === 'IDAT') chunks.push(d);
  }
  assert.equal(depth, 8, 'Expected 8-bit PNG');
  const channels = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 }[type];
  assert.ok(channels, 'Unsupported PNG colour type');
  const stride = w * channels, raw = zlib.inflateSync(Buffer.concat(chunks));
  const scan = Buffer.alloc(h * stride);
  for (let y = 0; y < h; y++) {
    const filter = raw[y * (stride + 1)]; assert.ok(filter <= 4);
    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? scan[y * stride + x - channels] : 0;
      const b = y ? scan[(y - 1) * stride + x] : 0;
      const c = y && x >= channels ? scan[(y - 1) * stride + x - channels] : 0;
      const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
      const predictor = filter === 0 ? 0 : filter === 1 ? a : filter === 2 ? b : filter === 3 ? (a + b) >> 1 : pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
      scan[y * stride + x] = (raw[y * (stride + 1) + x + 1] + predictor) & 255;
    }
  }
  const im = image(w, h);
  for (let i = 0; i < w * h; i++) {
    const j = i * channels; let r, g, b, a = 255;
    if (type === 2 || type === 6) { r = scan[j]; g = scan[j + 1]; b = scan[j + 2]; if (type === 6) a = scan[j + 3]; }
    else if (type === 3) { const k = scan[j]; r = pal[k * 3]; g = pal[k * 3 + 1]; b = pal[k * 3 + 2]; a = trans && k < trans.length ? trans[k] : 255; }
    else { r = g = b = scan[j]; if (type === 4) a = scan[j + 1]; }
    im.data.set([r, g, b, a], i * 4);
  }
  return im;
}
function crc32(b) {
  let c = 0xffffffff;
  for (const n of b) { c ^= n; for (let k = 0; k < 8; k++) c = (c >>> 1) ^ ((c & 1) ? 0xedb88320 : 0); }
  return (c ^ 0xffffffff) >>> 0;
}
function writePNG(file, im) {
  function chunk(t, d) {
    const tb = Buffer.from(t), n = Buffer.alloc(4), crc = Buffer.alloc(4);
    n.writeUInt32BE(d.length); crc.writeUInt32BE(crc32(Buffer.concat([tb, d])));
    return Buffer.concat([n, tb, d, crc]);
  }
  const ih = Buffer.alloc(13); ih.writeUInt32BE(im.w); ih.writeUInt32BE(im.h, 4); ih[8] = 8; ih[9] = 6;
  const raw = Buffer.alloc((im.w * 4 + 1) * im.h);
  for (let y = 0; y < im.h; y++) im.data.copy(raw, y * (im.w * 4 + 1) + 1, y * im.w * 4, (y + 1) * im.w * 4);
  fs.writeFileSync(file, Buffer.concat([Buffer.from('89504e470d0a1a0a', 'hex'), chunk('IHDR', ih), chunk('IDAT', zlib.deflateSync(raw, { level: 9 })), chunk('IEND', Buffer.alloc(0))]));
}
function pixel(im, x, y) { return Array.from(im.data.subarray((y * im.w + x) * 4, (y * im.w + x) * 4 + 4)); }
function put(im, x, y, c) { if (x >= 0 && x < im.w && y >= 0 && y < im.h) im.data.set(c, (y * im.w + x) * 4); }
function rect(im, x, y, w, h, c) { for (let j = y; j < y + h; j++) for (let i = x; i < x + w; i++) put(im, i, j, c); }
function line(im, points, c) {
  for (let i = 1; i < points.length; i++) {
    let [x, y] = points[i - 1]; const [x1, y1] = points[i];
    const dx = Math.abs(x1 - x), sx = x < x1 ? 1 : -1;
    const dy = -Math.abs(y1 - y), sy = y < y1 ? 1 : -1; let error = dx + dy;
    while (true) { put(im, x, y, c); if (x === x1 && y === y1) break; const e = 2 * error; if (e >= dy) { error += dy; x += sx; } if (e <= dx) { error += dx; y += sy; } }
  }
}
function inside(x, y, points) {
  let result = false;
  for (let i = 0, j = points.length - 1; i < points.length; j = i++) {
    const [xi, yi] = points[i], [xj, yj] = points[j];
    if ((yi > y) !== (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) result = !result;
  }
  return result;
}
function polygon(im, points, c) {
  const xs = points.map(p => p[0]), ys = points.map(p => p[1]);
  for (let y = Math.min(...ys); y <= Math.max(...ys); y++) for (let x = Math.min(...xs); x <= Math.max(...xs); x++) if (inside(x + 0.5, y + 0.5, points)) put(im, x, y, c);
  line(im, [...points, points[0]], c);
}
function paste(dst, src, dx, dy, scale = 1) {
  assert.ok(Number.isInteger(scale) && scale > 0);
  for (let y = 0; y < src.h; y++) for (let x = 0; x < src.w; x++) { const c = pixel(src, x, y); if (c[3]) rect(dst, dx + x * scale, dy + y * scale, scale, scale, c); }
}
function rgb(hex) { return [...Buffer.from(hex.slice(1), 'hex'), 255]; }
const C = {
  O: rgb('#080B14'), N: rgb('#1A1D33'), M: rgb('#2D3050'), S: rgb('#3F4A6E'),
  E: rgb('#62677D'), L: rgb('#858A9B'), G: rgb('#B8862E'), Y: rgb('#FFD43B'),
  D: rgb('#7F1A22'), R: rgb('#F21C2A'), T: rgb('#7A3A3C'), H: rgb('#EFD0B6'),
  // Only two colours outside number_palette.png, both confined to red light.
  A: rgb('#B52A35'), B: rgb('#FF655E'),
};
const CLEAR = [0, 0, 0, 0];
const paletteImage = readPNG(path.join(ROOT, 'Assets/Art/Palette/number_palette.png'));
const projectColors = new Set();
for (let i = 0; i < paletteImage.w * paletteImage.h; i++) projectColors.add(paletteImage.data.subarray(i * 4, i * 4 + 3).toString('hex'));
for (const [key, c] of Object.entries(C)) if (!['A', 'B'].includes(key)) assert.ok(projectColors.has(Buffer.from(c.slice(0, 3)).toString('hex')), `Missing fixed palette colour ${key}`);

// Source blocks are irregular (mostly 2-4 px), without a recoverable uniform
// grid. Use explicit registered cells and redraw unreadable details at native
// size. Each of the three masks excludes its background BEFORE voting.
const states = [
  { name: 'intact', crop: [52, 130, 330, 446], ring: [127, 130, 253, 164],
    body: [[52,421],[61,421],[62,413],[77,413],[77,379],[91,363],[73,344],[65,323],[69,291],[80,268],[94,262],[94,248],[103,237],[106,219],[122,194],[139,185],[141,177],[240,177],[246,188],[262,191],[268,214],[268,146],[276,146],[276,222],[281,238],[291,251],[291,311],[280,314],[278,363],[294,372],[305,387],[305,414],[323,415],[323,422],[330,422],[330,435],[208,435],[208,445],[171,445],[171,435],[52,435]] },
  { name: 'damaged', crop: [374, 130, 652, 446], ring: [448, 130, 575, 164],
    body: [[374,421],[390,421],[401,414],[401,386],[417,369],[405,354],[395,333],[399,304],[415,271],[417,248],[429,237],[432,218],[446,194],[462,184],[464,176],[560,176],[568,189],[585,196],[590,224],[590,157],[597,157],[600,240],[608,247],[608,286],[620,303],[625,330],[620,349],[609,332],[607,312],[601,314],[601,365],[618,378],[625,390],[625,413],[644,415],[644,422],[652,422],[652,435],[531,435],[531,445],[493,445],[493,435],[374,435]] },
  { name: 'broken', crop: [698, 130, 976, 446], ring: null,
    body: [[699,417],[714,407],[718,383],[735,363],[723,347],[725,314],[735,288],[738,254],[744,242],[747,219],[761,198],[773,189],[798,189],[808,197],[834,198],[851,190],[882,190],[900,202],[913,225],[941,174],[948,166],[954,170],[948,178],[925,225],[921,250],[932,256],[932,316],[949,326],[959,346],[955,363],[943,372],[946,394],[966,414],[976,423],[966,435],[944,443],[909,443],[893,437],[863,435],[857,445],[818,445],[815,436],[781,440],[748,446],[718,437],[699,434]] },
];
function maskSource(source, state) {
  const result = image(source.w, source.h);
  const [x0, y0, x1, y1] = state.crop;
  for (let y = y0; y < y1; y++) for (let x = x0; x < x1; x++) {
    const c = pixel(source, x, y);
    let keep = inside(x + 0.5, y + 0.5, state.body);
    if (state.ring) { const [l,t,r,b] = state.ring; if (x >= l && x < r && y >= t && y < b && c[0] > 65 && c[0] > c[1] * 1.3 && c[0] > c[2] * 0.95) keep = true; }
    if (keep) put(result, x, y, [...c.slice(0, 3), 255]);
  }
  return result;
}
function dominantNative(masked, state) {
  const im = image(52, 48), [left, top, right, bottom] = state.crop;
  // 48 occupied columns, x=2..49, same ground y=47 for every state.
  for (let y = 0; y < 48; y++) for (let x = 0; x < 48; x++) {
    const x0 = left + Math.floor(x * (right - left) / 48), x1 = left + Math.floor((x + 1) * (right - left) / 48);
    const y0 = top + Math.floor(y * (bottom - top) / 48), y1 = top + Math.floor((y + 1) * (bottom - top) / 48);
    const votes = new Map(); let opaque = 0, winner = null, count = 0;
    for (let j = y0; j < y1; j++) for (let i = x0; i < x1; i++) {
      const c = pixel(masked, i, j); if (!c[3]) continue; opaque++;
      // Vote by 5-bit RGB bucket to avoid giving every compressed/noisy pixel
      // its own vote. Representative is an actual source pixel, never a mean.
      const key = ((c[0] >> 3) << 10) | ((c[1] >> 3) << 5) | (c[2] >> 3);
      const v = votes.get(key) || { n: 0, color: c }; v.n++; votes.set(key, v);
      if (v.n > count) { count = v.n; winner = v; }
    }
    if (opaque < (x1 - x0) * (y1 - y0) / 2) continue;
    // Palette reduction happens AFTER selecting the source cell's dominant.
    let nearest, distance = Infinity;
    for (const color of Object.values(C)) {
      const d = color.slice(0, 3).reduce((s, v, i) => s + (v - winner.color[i]) ** 2, 0);
      if (d < distance) { distance = d; nearest = color; }
    }
    put(im, x + 2, y, nearest);
  }
  return im;
}

function eyeLogo(im, damaged) {
  rect(im, 21, 34, 11, 6, C.N);
  line(im, [[22,37],[25,35],[27,35],[30,37],[27,39],[25,39],[22,37]], damaged ? C.T : C.A);
  rect(im, 25, 36, 2, 2, damaged ? C.D : C.R);
}
function base(im, broken) {
  polygon(im, [[6,38],[10,36],[42,36],[46,39],[46,45],[49,45],[49,47],[2,47],[2,45],[5,45]], C.O);
  polygon(im, [[7,39],[11,37],[41,37],[45,40],[45,46],[6,46]], C.M);
  line(im, [[7,39],[11,37],[23,37]], C.S);
  for (const y of [39,43]) {
    rect(im, 10, y, 32, 4, C.O);
    for (let j = 0; j < 3; j++) for (let x = 11; x < 42; x++) {
      if ((x + j) % 7 < 3) put(im, x, y + j, broken ? C.G : j === 2 ? C.G : C.Y);
    }
  }
  rect(im, 23, 38, 6, 9, C.O); rect(im, 24, 38, 4, 8, C.M);
  line(im, [[24,38],[27,38]], C.S);
  rect(im, 25, 43, 2, 1, C.O); rect(im, 23, 46, 6, 1, C.S);
  rect(im, 3, 46, 3, 1, C.S); rect(im, 46, 46, 3, 1, C.M);
}
function glass(im, state) {
  const outer = [[17,14],[34,14],[37,17],[37,30],[34,33],[17,33],[14,30],[14,17]];
  polygon(im, outer, C.O);
  polygon(im, [[17,15],[34,15],[36,17],[36,30],[34,32],[17,32],[15,30],[15,17]], C.E);
  polygon(im, [[18,16],[33,16],[35,18],[35,29],[33,31],[18,31],[16,29],[16,18]], state === 'broken' ? C.N : C.D);
  line(im, [[16,29],[16,18],[18,16],[32,16]], C.M);
  if (state === 'intact') {
    polygon(im, [[22,17],[29,17],[32,20],[33,23],[32,27],[29,30],[22,30],[19,27],[18,23],[19,20]], C.A);
    polygon(im, [[23,19],[28,19],[30,21],[31,23],[30,26],[28,28],[23,28],[21,26],[20,23],[21,21]], C.R);
    polygon(im, [[24,21],[27,21],[29,23],[29,25],[27,27],[24,27],[22,25],[22,23]], C.B);
    put(im, 24, 22, C.H); // exactly one pale core highlight
    line(im, [[17,18],[17,27]], C.T);
  } else if (state === 'damaged') {
    polygon(im, [[23,20],[28,20],[30,23],[29,27],[26,29],[22,27],[21,23]], C.A);
    rect(im, 24, 23, 4, 3, C.D);
    // Three deliberate crack paths; no shattered spiderweb noise.
    line(im, [[20,16],[21,20],[24,22],[23,25],[20,28],[18,31]], C.O);
    line(im, [[33,17],[31,20],[30,22],[27,24],[29,27],[30,31]], C.O);
    line(im, [[16,24],[19,23],[22,24]], C.O);
    line(im, [[20,17],[21,20],[23,22]], C.T);
    line(im, [[32,18],[30,21],[28,23]], C.T);
  } else {
    polygon(im, [[20,17],[23,18],[25,17],[28,19],[32,17],[31,21],[34,23],[31,25],[33,29],[29,28],[27,31],[24,28],[20,30],[21,26],[17,24],[21,22]], C.O);
    line(im, [[18,17],[19,20],[21,20]], C.S);
    line(im, [[34,19],[33,21],[31,21]], C.M);
    line(im, [[17,28],[19,28],[20,30]], C.S);
    line(im, [[32,28],[33,30]], C.M);
  }
}
function cleanNative(im, state) {
  const voted = { w: im.w, h: im.h, data: Buffer.from(im.data) };
  // Native-size hand edits, registered across states. The voted source is the
  // layout guide; readable steel shading runs survive in the shoulder/panels.
  // Ambiguous contours, glass, stripes, logo, and cables are explicitly redrawn.
  for (let y = 0; y < 48; y++) for (let x = 0; x < 52; x++) {
    if (y < 7 || x < 6 || x > 45 || y > 36) put(im, x, y, CLEAR);
  }
  polygon(im, [[10,12],[15,8],[36,8],[42,12],[45,19],[44,34],[40,38],[11,38],[7,33],[7,20]], C.O);
  polygon(im, [[11,13],[16,9],[35,9],[41,13],[43,20],[42,34],[39,37],[12,37],[9,33],[9,20]], C.M);
  polygon(im, [[11,14],[16,10],[25,10],[23,13],[17,14],[13,18],[10,18]], C.S);
  polygon(im, [[37,12],[40,14],[42,19],[39,19],[36,15]], C.N);
  rect(im, 10, 21, 4, 12, C.N); rect(im, 38, 21, 4, 13, C.N);
  line(im, [[13,33],[13,36]], C.S);
  line(im, [[36,33],[36,36]], C.N);
  // Keep coherent source-voted steel runs, discarding tiny texture speckles.
  const steel = new Set([C.N,C.M,C.S].map(c=>c.join(',')));
  const panelRuns = [[14,11,35],[14,12,35],[12,33,20],[32,33,39],[12,34,20],[32,34,39],[12,35,20],[32,35,39]];
  for (const [left,y,right] of panelRuns) {
    let x=left;
    while(x<=right) {
      const c=pixel(voted,x,y); let end=x+1;
      while(end<=right && pixel(voted,end,y).join(',')===c.join(',')) end++;
      if(end-x>=2 && steel.has(c.join(','))) rect(im,x,y,end-x,1,c);
      x=end;
    }
  }
  // Clean the top lid rim while retaining its source-voted shoulder below.
  polygon(im, [[17,7],[33,7],[37,9],[35,11],[16,11],[13,9]], C.O);
  line(im, [[16,8],[19,7],[32,7],[35,8]], state === 'intact' ? C.A : state === 'damaged' ? C.T : C.S);
  line(im, [[16,10],[34,10]], C.N);
  line(im, [[19,9],[32,9]], state === 'intact' ? C.D : C.M);
  // Short side housings, matching character/prop 1 px outer contour.
  polygon(im, [[7,18],[10,18],[11,20],[11,29],[9,31],[6,30],[6,20]], C.O);
  rect(im, 7, 20, 3, 9, C.M); line(im, [[7,20],[7,27]], C.S);
  polygon(im, [[41,18],[44,18],[45,20],[45,29],[43,31],[40,30],[40,20]], C.O);
  rect(im, 41, 20, 3, 9, C.N); line(im, [[41,20],[41,27]], C.S);
  for (const x of [8,43]) { rect(im, x, 21, 1, 2, state === 'intact' ? C.R : state === 'damaged' ? C.D : C.O); rect(im, x, 25, 1, 2, state === 'intact' ? C.D : C.O); }
  // Two outlined cable clusters at the left, one on the right.
  line(im, [[6,23],[4,26],[3,30],[3,33],[6,37]], C.O);
  line(im, [[5,25],[4,29],[4,32],[6,35]], C.T);
  line(im, [[6,29],[5,32],[6,35],[8,37]], C.O);
  line(im, [[45,29],[47,32],[47,34],[44,37]], C.O);
  line(im, [[45,30],[46,32],[46,34],[44,36]], C.M);
  // Antenna retained as a simple rooted shape, without a stray cap pixel.
  line(im, [[39,12],[39,4],[40,4],[40,12]], C.O);
  put(im, 39, 5, state === 'intact' ? C.R : C.T);
  glass(im, state);
  eyeLogo(im, state !== 'intact');
  base(im, state === 'broken');
  if (state !== 'broken') {
    const ring = [[20,1],[31,1],[35,2],[37,3],[35,4],[31,5],[20,5],[16,4],[14,3],[16,2],[20,1]];
    line(im, ring, C.O);
    line(im, [[20,2],[31,2],[35,3],[31,4],[20,4],[16,3],[20,2]], state === 'intact' ? C.R : C.D);
    if (state === 'intact') line(im, [[20,2],[29,2]], C.B);
    else { put(im, 33, 2, CLEAR); put(im, 34, 3, CLEAR); }
  } else {
    // Broken lid: remove raised ring and carve a large asymmetric bite.
    rect(im, 16, 6, 21, 5, CLEAR);
    polygon(im, [[13,10],[17,9],[21,11],[26,11],[27,9],[32,10],[37,8],[38,12],[33,13],[19,13]], C.O);
    line(im, [[15,10],[18,10],[21,12],[26,12]], C.S);
    line(im, [[29,11],[32,11],[36,9]], C.E);
    rect(im, 39, 3, 2, 10, CLEAR);
    line(im, [[40,13],[43,9],[46,6]], C.O);
    line(im, [[41,12],[43,10],[45,7]], C.S);
    // Fallen plates and debris attach to the base; no orphan noise or smoke.
    polygon(im, [[12,39],[18,41],[14,46],[8,45]], C.O);
    polygon(im, [[12,40],[16,41],[13,44],[10,44]], C.S);
    polygon(im, [[35,42],[40,41],[44,45],[38,46]], C.O);
    polygon(im, [[37,43],[40,42],[42,44],[38,45]], C.M);
    rect(im, 7, 46, 2, 1, C.E); rect(im, 44, 46, 2, 1, C.S);
    line(im, [[21,34],[24,35],[24,37]], C.O);
  }
  return im;
}

function label(im, text, x, y) {
  const glyphs = {
    A:['010','101','111','101','101'],B:['110','101','110','101','110'],C:['011','100','100','100','011'],D:['110','101','101','101','110'],E:['111','100','110','100','111'],F:['111','100','110','100','100'],G:['011','100','101','101','011'],H:['101','101','111','101','101'],I:['111','010','010','010','111'],K:['101','101','110','101','101'],L:['100','100','100','100','111'],M:['101','111','111','101','101'],N:['101','111','111','111','101'],O:['010','101','101','101','010'],P:['110','101','110','100','100'],R:['110','101','110','101','101'],S:['011','100','010','001','110'],T:['111','010','010','010','010'],U:['101','101','101','101','111'],V:['101','101','101','101','010'],W:['101','101','111','111','101'],X:['101','101','010','101','101'],Y:['101','101','010','010','010'],Z:['111','001','010','100','111'],
    '0':['111','101','101','101','111'],'1':['010','110','010','010','111'],'2':['110','001','010','100','111'],'3':['110','001','010','001','110'],'4':['101','101','111','001','001'],'5':['111','100','110','001','110'],'6':['011','100','111','101','111'],'8':['111','101','111','101','111'],'/':['001','001','010','100','100'],'-':['000','000','111','000','000'],'.':['000','000','000','000','010'],':':['000','010','000','010','000']
  };
  for (const ch of text.toUpperCase()) {
    if (ch !== ' ') { assert.ok(glyphs[ch], `Missing glyph ${ch}`); glyphs[ch].forEach((row, j) => [...row].forEach((v, i) => { if (v === '1') put(im, x + i, y + j, C.L); })); }
    x += 4;
  }
}
function preview(frames) {
  const im = image(704, 360, rgb('#0B1026'));
  label(im, 'DRAFT / USER APPROVAL / 52X48 PX / PPU 32', 16, 12);
  frames.forEach((f, i) => {
    const x = 16 + i * 224;
    label(im, ['INTACT / 0 HITS','DAMAGED / 1 HIT','BROKEN / 2 HITS'][i], x, 28);
    paste(im, f, x, 44, 4);
  });
  label(im, '1X SCALE / ANDROID WALK FRAME 0', 16, 252);
  const android = readPNG(path.join(ROOT, 'Assets/Resources/DraftSprites/android_walk_4f.png'));
  const first = image(64,64);
  for (let y = 0; y < 64; y++) for (let x = 0; x < 64; x++) put(first, x, y, pixel(android, x, y));
  // Align the visible android foot row (61), not the transparent frame edge.
  paste(im, first, 16, 274);
  frames.forEach((f, i) => paste(im, f, 88 + i * 64, 288));
  label(im, 'ANDROID', 28, 344);
  label(im, 'INTACT', 100, 344); label(im, 'DAMAGED', 160, 344); label(im, 'BROKEN', 228, 344);
  label(im, 'RED EXTRAS', 352, 278);
  ['#B52A35','#FF655E'].forEach((hex,i) => {
    rect(im,352,292+i*20,12,12,rgb(hex));
    // Render hex without a hash; preview labels are separate from game art.
    label(im,hex.slice(1),372,296+i*20);
  });
  return im;
}
function verify(im, name) {
  assert.equal(im.w,52); assert.equal(im.h,48);
  const colors = new Set(), allowed = new Set(Object.values(C).map(c => c.join(',')));
  const occupied = [];
  for (let y=0;y<48;y++) for(let x=0;x<52;x++) {
    const c=pixel(im,x,y); assert.ok(c[3]===0||c[3]===255);
    if(c[3]) { assert.ok(allowed.has(c.join(','))); colors.add('#'+Buffer.from(c.slice(0,3)).toString('hex').toUpperCase()); occupied.push([x,y]); }
  }
  const xs=occupied.map(p=>p[0]),ys=occupied.map(p=>p[1]);
  assert.equal(Math.max(...ys),47); assert.equal(Math.min(...xs),2); assert.equal(Math.max(...xs),49);
  assert.equal(pixel(im,2,47)[3],255); assert.equal(pixel(im,49,47)[3],255);
  // Every visible pixel belongs to a cluster, using 8-connected pixel edges.
  for(const [x,y] of occupied) assert.ok(occupied.some(([xx,yy])=>Math.abs(xx-x)<=1&&Math.abs(yy-y)<=1&&(xx!==x||yy!==y)),`${name}: orphan ${x},${y}`);
  console.log(`${name}: 52x48, binary alpha, bounds ${Math.min(...xs)},${Math.min(...ys)}..${Math.max(...xs)},47; ${colors.size} colours`);
  return colors;
}
const source = readPNG(path.join(OUT,'spawner_gemini.png'));
assert.equal(source.w,1024); assert.equal(source.h,572);
const frames=states.map(state=>cleanNative(dominantNative(maskSource(source,state),state),state.name));
const used = new Set();
frames.forEach((im,i)=>{for(const color of verify(im,states[i].name))used.add(color);writePNG(path.join(OUT,`spawner_${states[i].name}.png`),im);});
writePNG(path.join(OUT,'spawner_preview_4x.png'),preview(frames));
console.log('Used: '+[...used].sort().join(', '));
console.log('Extra red light colours: #B52A35, #FF655E');
