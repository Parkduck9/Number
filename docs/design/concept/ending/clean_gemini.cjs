// Node.js only; lossless RGBA pixel cloning, with no image dependencies.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');

function readPng(file) {
  const b = fs.readFileSync(file), parts = [];
  let w, h;
  for (let p = 8; p < b.length;) {
    const n = b.readUInt32BE(p), type = b.toString('ascii', p + 4, p + 8);
    const data = b.subarray(p + 8, p + 8 + n);
    if (type === 'IHDR') {
      w = data.readUInt32BE(0); h = data.readUInt32BE(4);
      if (data[8] !== 8 || data[9] !== 6 || data[12] !== 0)
        throw new Error('Expected noninterlaced 8-bit RGBA PNG');
    }
    if (type === 'IDAT') parts.push(data);
    p += n + 12;
  }
  const raw = zlib.inflateSync(Buffer.concat(parts)), pixels = Buffer.alloc(w * h * 4);
  const stride = w * 4;
  for (let y = 0; y < h; y++) {
    const filter = raw[y * (stride + 1)];
    for (let x = 0; x < stride; x++) {
      const i = y * stride + x, a = x >= 4 ? pixels[i - 4] : 0;
      const b = y ? pixels[i - stride] : 0, c = y && x >= 4 ? pixels[i - stride - 4] : 0;
      const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
      const predictors = [0, a, b, Math.floor((a + b) / 2), pa <= pb && pa <= pc ? a : pb <= pc ? b : c];
      if (filter > 4) throw new Error('Invalid PNG filter');
      pixels[i] = (raw[y * (stride + 1) + x + 1] + predictors[filter]) & 255;
    }
  }
  return { w, h, pixels };
}

function png(w, h, pixels) {
  function chunk(type, data) {
    const name = Buffer.from(type), body = Buffer.concat([name, data]);
    let crc = 0xffffffff;
    for (const byte of body) {
      crc ^= byte;
      for (let k = 0; k < 8; k++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0);
    }
    const result = Buffer.alloc(data.length + 12);
    result.writeUInt32BE(data.length); body.copy(result, 4);
    result.writeUInt32BE((crc ^ 0xffffffff) >>> 0, result.length - 4);
    return result;
  }
  const header = Buffer.alloc(13), rows = Buffer.alloc(h * (w * 4 + 1));
  header.writeUInt32BE(w); header.writeUInt32BE(h, 4); header[8] = 8; header[9] = 6;
  for (let y = 0; y < h; y++) pixels.copy(rows, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4);
  return Buffer.concat([Buffer.from('89504e470d0a1a0a', 'hex'), chunk('IHDR', header),
    chunk('IDAT', zlib.deflateSync(rows)), chunk('IEND', Buffer.alloc(0))]);
}

function clean() {
  const source = readPng(path.join(__dirname, 'ending_fresh_gemini.png'));
  const { w, h, pixels } = source;
  if (w !== 1024 || h !== 572) throw new Error('Expected 1024x572 source');
  const output = Buffer.alloc(w * 576 * 4);
  pixels.copy(output);
  // Trace the grey sparkle, including a one-pixel antialias margin.
  // Clone unmarked rock 7 pixels right and 56 down; do not blend colours.
  for (let y = 463; y <= 501; y++) {
    let left = w, right = -1;
    for (let x = 916; x <= 954; x++) {
      const i = (y * w + x) * 4, r = pixels[i], g = pixels[i + 1], b = pixels[i + 2];
      if (r >= 30 && g >= 30 && b >= 30 && r < g * 1.3) {
        left = Math.min(left, x); right = Math.max(right, x);
      }
    }
    if (right >= left) for (let yy = y - 1; yy <= y + 1; yy++)
      for (let x = left - 1; x <= right + 1; x++) {
        const from = ((yy + 56) * w + x + 7) * 4, to = (yy * w + x) * 4;
        pixels.copy(output, to, from, from + 4);
      }
  }
  // Repeat the final source row four times, with no resizing.
  for (let y = h; y < 576; y++) output.copy(output, y * w * 4, (h - 1) * w * 4, h * w * 4);
  const encoded = png(w, 576, output);
  fs.writeFileSync(path.join(__dirname, 'ending_gemini_clean_1024x576.png'), encoded);
  fs.copyFileSync(path.join(__dirname, 'ending_gemini_clean_1024x576.png'),
    path.resolve(__dirname, '../../../../Assets/Resources/DraftUi/ending_illustration.png'));
  // Before | after, 80x72 source crops, each enlarged 4x by pixel repetition.
  const crop = { x: 902, y: 450, w: 80, h: 72 }, scale = 4;
  const pw = crop.w * scale * 2, ph = crop.h * scale, preview = Buffer.alloc(pw * ph * 4);
  for (let y = 0; y < ph; y++) for (let x = 0; x < pw; x++) {
    const after = x >= pw / 2, localX = x % (pw / 2);
    const from = ((crop.y + Math.floor(y / scale)) * w + crop.x + Math.floor(localX / scale)) * 4;
    (after ? output : pixels).copy(preview, (y * pw + x) * 4, from, from + 4);
  }
  fs.writeFileSync(path.join(__dirname, 'watermark_before_after_4x.png'), png(pw, ph, preview));
}

if (require.main === module) clean();
module.exports = { readPng, png };
