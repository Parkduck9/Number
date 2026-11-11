// DRAFT Luddite checkpoint. AI-assisted pixel design: Codex (OpenAI).
// Run: node docs/design/concept/checkpoint/build_checkpoint.cjs
// No generated art service, external assets, fonts, randomness, or dependencies.
// Only the two PNGs beside this script are written. Source images are read only.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../../../..');

// Minimal 8-bit RGB/RGBA PNG reader, supporting all five PNG row filters.
function readPNG(file) {
  const b = fs.readFileSync(file), chunks = [];
  const w = b.readUInt32BE(16), h = b.readUInt32BE(20);
  assert.equal(b[24], 8);
  assert.ok(b[25] === 2 || b[25] === 6);
  assert.equal(b[28], 0);
  const channels = b[25] === 6 ? 4 : 3, stride = w * channels;
  for (let p = 8; p < b.length;) {
    const n = b.readUInt32BE(p);
    if (b.toString('ascii', p + 4, p + 8) === 'IDAT') chunks.push(b.subarray(p + 8, p + 8 + n));
    p += n + 12;
  }
  const raw = zlib.inflateSync(Buffer.concat(chunks));
  const decoded = Buffer.alloc(h * stride), pixels = Buffer.alloc(w * h * 4);
  const paeth = (a, b, c) => {
    const p = a + b - c, x = Math.abs(p - a), y = Math.abs(p - b), z = Math.abs(p - c);
    return x <= y && x <= z ? a : y <= z ? b : c;
  };
  for (let y = 0; y < h; y++) {
    const filter = raw[y * (stride + 1)];
    assert.ok(filter <= 4);
    for (let x = 0; x < stride; x++) {
      const i = y * stride + x;
      const a = x >= channels ? decoded[i - channels] : 0;
      const b = y ? decoded[i - stride] : 0;
      const c = y && x >= channels ? decoded[i - stride - channels] : 0;
      const predictor = [0, a, b, (a + b) >> 1, paeth(a, b, c)][filter];
      decoded[i] = (raw[y * (stride + 1) + x + 1] + predictor) & 255;
    }
  }
  for (let i = 0; i < w * h; i++) {
    decoded.copy(pixels, i * 4, i * channels, i * channels + 3);
    pixels[i * 4 + 3] = channels === 4 ? decoded[i * channels + 3] : 255;
  }
  return { w, h, pixels };
}

function pngChunk(type, data) {
  const content = Buffer.concat([Buffer.from(type), data]);
  let crc = 0xffffffff;
  for (const byte of content) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0);
  }
  const out = Buffer.alloc(data.length + 12);
  out.writeUInt32BE(data.length, 0);
  content.copy(out, 4);
  out.writeUInt32BE((crc ^ 0xffffffff) >>> 0, out.length - 4);
  return out;
}

function writePNG(file, w, h, pixels) {
  const header = Buffer.alloc(13);
  header.writeUInt32BE(w, 0); header.writeUInt32BE(h, 4);
  header[8] = 8; header[9] = 6;
  const rows = Buffer.alloc(h * (w * 4 + 1));
  for (let y = 0; y < h; y++) pixels.copy(rows, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4);
  fs.writeFileSync(file, Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    pngChunk('IHDR', header), pngChunk('IDAT', zlib.deflateSync(rows, { level: 9 })), pngChunk('IEND', Buffer.alloc(0)),
  ]));
}

const palette = readPNG(path.join(root, 'Assets/Art/Palette/number_palette.png'));
const hero = readPNG(path.join(root, 'Assets/Resources/DraftSprites/hero_idle_4f.png'));
const sample = i => [...palette.pixels.subarray(i * 4, i * 4 + 4)];
// Ten colors across BOTH states, sampled directly from the project palette.
const colors = {
  '#': sample(0), s: sample(20), w: sample(10), h: sample(11),
  g: sample(7), p: sample(8), r: sample(16),
  a: sample(18), y: sample(19), o: sample(21),
};
const frames = [];
for (const lit of [false, true]) {
  const grid = Array.from({ length: 40 }, () => Array(16).fill('.'));
  function stamp(x, y, rows) {
    assert.ok(rows.every(row => row.length === rows[0].length));
    rows.forEach((row, dy) => [...row].forEach((v, dx) => {
      assert.ok(x + dx < 16 && y + dy < 40);
      if (v !== '.') grid[y + dy][x + dx] = v;
    }));
  }
  // Rough-cut stake: broad top-left plane, narrow dark right plane.
  stamp(5, 4, [
    '.####.', '#hhws#', '#hwws#', '#hwws#', '#hwws#', '#hwws#',
    '#hwws#', '#hwws#', '#hwws#', '#hwws#', '#hwws#', '#hwws#',
    '#hwws#', '#hwws#', '#hwws#', '#hwws#', '#hwws#', '#hwws#',
    '#hwws#', '#hwws#', '#hwws#', '#hwws#', '#hwws#', '#hwws#',
    '#hwws#', '#hwss#', '#hwss#', '#hwss#', '#hwss#', '#hwss#',
    '#hwss#', '#hwss#', '#hwss#', '#hwss#',
  ]);
  // Small hand-hewn crosspiece, cord, and lantern suspension.
  stamp(1, 7, ['.###########.', '#hhwwwwwwsss#', '.##########s#', '.........#s#.']);
  // Rectangular folded-paper lantern; no round festival-lantern silhouette.
  stamp(10, 11, ['.###.', '#sss#', '#ggs#', '#gps#', '#gps#', '#ggs#', '#sss#', '.###.']);
  // Yellow paper is bound to the stake by a plain cord, not a character/glyph.
  // Red marks form an irregular, nonlinguistic forked brush gesture.
  stamp(2, 10, [
    '######', '#hhhh#', '#pppg#', '#prpg#', '#prpg#', '#prrg#',
    '#pprg#', '#prrg#', '#rrpg#', '#prpg#', '#prpg#', '#pppg#',
    '#pppg#', '#ppg##', '####..',
  ]);
  stamp(4, 37, ['.######.', '#hwwsss#', '########']);
  const state = { ...colors };
  if (lit) {
    // Discrete gold clusters touch the paper outline; no detached sparkle/noise.
    stamp(1, 13, ['a', 'a']);
    stamp(1, 20, ['a', 'a']);
    stamp(8, 15, ['a', 'a']);
    state.p = colors.y; state.g = colors.a;
    stamp(10, 11, ['.###.', '#aaa#', '#oyo#', '#yyo#', '#yoo#', '#ooo#', '#aaa#', '.###.']);
  } else {
    state.r = colors.s;
  }
  const pixels = Buffer.alloc(16 * 40 * 4);
  grid.forEach((row, y) => row.forEach((symbol, x) => {
    if (symbol !== '.') pixels.set(state[symbol], (y * 16 + x) * 4);
  }));
  // Check dimensions, hard alpha, centered silhouette, ground contact, and no
  // detached single pixels (all visible pixels form one 4-connected component).
  const visible = new Set();
  for (let i = 0; i < 640; i++) {
    assert.ok(pixels[i * 4 + 3] === 0 || pixels[i * 4 + 3] === 255);
    if (pixels[i * 4 + 3]) visible.add(i);
  }
  assert.ok([...visible].some(i => Math.floor(i / 16) === 39));
  const xs = [...visible].map(i => i % 16);
  assert.equal(Math.min(...xs) + Math.max(...xs), 15);
  const visited = new Set(), pending = [visible.values().next().value];
  while (pending.length) {
    const i = pending.pop();
    if (visited.has(i) || !visible.has(i)) continue;
    visited.add(i);
    for (const n of [i % 16 ? i - 1 : -1, i % 16 < 15 ? i + 1 : -1, i - 16, i + 16]) pending.push(n);
  }
  assert.equal(visited.size, visible.size);
  frames.push(pixels);
}
const strip = Buffer.alloc(32 * 40 * 4);
frames.forEach((frame, f) => {
  for (let y = 0; y < 40; y++) frame.copy(strip, (y * 32 + f * 16) * 4, y * 64, (y + 1) * 64);
});
const used = new Set();
for (let i = 0; i < strip.length; i += 4) if (strip[i + 3]) used.add(strip.subarray(i, i + 4).toString('hex'));
assert.ok(used.size <= 10);
const allowed = new Set(Object.values(colors).map(c => Buffer.from(c).toString('hex')));
assert.ok([...used].every(c => allowed.has(c)));
writePNG(path.join(__dirname, 'checkpoint_2f.png'), 32, 40, strip);

// Preview, left to right: UNLIT, LIT, hero idle cell 0. Align actual opaque
// foot pixels, preserving the full 64x64 hero cell and the original pixel scale.
let heroBottom = 0;
for (let y = 0; y < 64; y++) for (let x = 0; x < 64; x++) {
  if (hero.pixels[(y * hero.w + x) * 4 + 3]) heroBottom = y;
}
const pw = 128, ph = 72, scene = Buffer.alloc(pw * ph * 4);
for (let i = 0; i < pw * ph; i++) scene.set([16, 20, 42, 255], i * 4);
function composite(src, sw, sh, stride, x0, y0) {
  for (let y = 0; y < sh; y++) for (let x = 0; x < sw; x++) {
    const i = (y * stride + x) * 4, j = ((y0 + y) * pw + x0 + x) * 4;
    const a = src[i + 3] / 255;
    for (let c = 0; c < 3; c++) scene[j + c] = Math.round(src[i + c] * a + scene[j + c] * (1 - a));
  }
}
frames.forEach((frame, f) => composite(frame, 16, 40, 16, 12 + f * 28, 24));
composite(hero.pixels, 64, 64, hero.w, 60, 63 - heroBottom);
const zoom = Buffer.alloc(pw * ph * 16 * 4);
for (let y = 0; y < ph * 4; y++) for (let x = 0; x < pw * 4; x++) {
  const i = (Math.floor(y / 4) * pw + Math.floor(x / 4)) * 4;
  scene.copy(zoom, (y * pw * 4 + x) * 4, i, i + 4);
}
writePNG(path.join(__dirname, 'preview_4x.png'), pw * 4, ph * 4, zoom);
console.log(`Wrote checkpoint_2f.png (32x40, ${used.size} opaque palette colors) and preview_4x.png (512x288).`);
