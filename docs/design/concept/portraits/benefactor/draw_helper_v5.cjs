// DRAFT ONLY: face/mic cleanup on helper_v4's exact native 50x50 grid.
// Run: node docs/design/concept/portraits/benefactor/draw_helper_v5.cjs
// Uses the v4 PNG helpers; never regenerates or overwrites the v4 drawing.
// Writes only the three v5 review PNGs alongside this script.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { Canvas, decode, encode, palette, bounds, components, P } = require('./draw_helper_v4.cjs');
const OUT = __dirname;
const ROOT = path.resolve(OUT, '../../../../..');
const rgba = key => [...Buffer.from(P[key].slice(1), 'hex'), 255];

function native50(image) {
  assert.deepEqual([image.w, image.h], [100, 100]);
  const native = new Canvas(50, 50);
  for (let y = 0; y < 50; y++) for (let x = 0; x < 50; x++) {
    const cell = image.get(x * 2, y * 2);
    for (let dy = 0; dy < 2; dy++) for (let dx = 0; dx < 2; dx++) {
      assert.deepEqual(image.get(x * 2 + dx, y * 2 + dy), cell,
        `V4 contains a broken 2x cell at ${x},${y}`);
    }
    native.pixel(x, y, cell);
  }
  return native;
}

// Inclusive face spans, measured on v4. The hairline splits the forehead.
// Hair, ear, headset root, neck, clothes, rim and alpha remain untouched.
const FACE = [
  [19, 24, 28], [19, 31, 34],
  [20, 23, 34], [21, 23, 34], [22, 23, 34], [23, 23, 34],
  [24, 23, 34], [25, 23, 34], [26, 23, 34],
  [27, 24, 33], [28, 24, 33], [29, 25, 32], [30, 26, 31],
];
const OUTLINE = [[23, 28], [26, 31]];
const allowed = new Set(FACE.flatMap(([y, left, right]) =>
  Array.from({ length: right - left + 1 }, (_, i) => `${left + i},${y}`)));
OUTLINE.forEach(([x, y]) => allowed.add(`${x},${y}`));

function redrawFace(source) {
  const face = new Canvas(50, 50);
  source.data.copy(face.data);
  const put = (x, y, key) => {
    assert(allowed.has(`${x},${y}`), `Edit outside face: ${x},${y}`);
    face.pixel(x, y, P[key]);
  };
  // Three flat skin planes from v4's palette: warm base, broad light,
  // and one shadow restricted to the left edge and under the jaw.
  for (const [y, left, right] of FACE) {
    for (let x = left; x <= right; x++) put(x, y, 'skinMid');
    if (y >= 20 && y <= 29) {
      put(left, y, 'skinShade');
      for (let x = left + 2; x <= right; x++) put(x, y, 'skin');
    }
    if (y === 30) for (let x = left; x <= right; x++) put(x, y, 'skinShade');
  }

  // One-pixel brows, a clear row between brow and lid, equal eye height.
  // Near eye: two dark cells plus one light; far eye: one dark plus light.
  // Highlights sit left of the dark shapes to direct the gaze right.
  put(26, 19, 'hairDark'); put(33, 19, 'hairDark');
  for (const x of [25, 26, 27, 32, 33]) put(x, 21, 'ink');
  put(25, 22, 'cloth'); put(26, 22, 'hairDark'); put(27, 22, 'hairDark');
  put(32, 22, 'cloth'); put(33, 22, 'hairDark');

  // The only interior skin-shadow pixel is the small reference-style nose.
  put(31, 24, 'skinShade');
  // Two-cell quiet smile, slightly darker than the skin shadow.
  put(30, 28, 'skinDark'); put(31, 28, 'skinDark');
  // A short, single-row boom ends on the cheek above/left of the lips.
  for (const x of [23, 24, 25]) put(x, 26, 'hairDark');
  put(26, 26, 'ink');
  // Join the lower-left jaw staircase without the old skin-coloured notch.
  for (const [x, y] of OUTLINE) put(x, y, 'ink');
  return face;
}

function cropFace(image) {
  // Same fixed native-art-pixel window for both versions; includes headset
  // root, eyes, cheek, lips and complete chin. Enlargement is native x8.
  const crop = new Canvas(20, 20, '#0B1026');
  for (let y = 0; y < 20; y++) for (let x = 0; x < 20; x++) {
    const pixel = image.get(x + 18, y + 14);
    if (pixel[3]) crop.pixel(x, y, pixel);
  }
  return crop;
}

function verify(source, edited, saved, comparison, faces) {
  assert.deepEqual(saved.data, edited.scale(2).data, 'PNG must preserve exact 2x2 cells');
  assert.deepEqual([saved.w, saved.h], [100, 100]);
  assert.deepEqual([comparison.w, comparison.h], [1200, 300]);
  assert.deepEqual([faces.w, faces.h], [320, 160]);
  const sourcePalette = new Set(palette(source).map(([hex]) => hex));
  assert(palette(edited).length <= 24, 'More than 24 opaque colours');
  for (const [hex] of palette(edited)) assert(sourcePalette.has(hex), `New colour: ${hex}`);
  const changed = [];
  for (let y = 0; y < 50; y++) for (let x = 0; x < 50; x++) {
    const before = source.get(x, y), after = edited.get(x, y);
    assert([0, 255].includes(after[3]), 'Non-binary alpha');
    assert.equal(after[3], before[3], `Silhouette changed: ${x},${y}`);
    if (before.join() !== after.join()) {
      assert(allowed.has(`${x},${y}`), `Changed approved area: ${x},${y}`);
      changed.push([x, y]);
    }
  }
  assert(changed.length > 0);
  assert.deepEqual(bounds(edited), bounds(source));
  assert.equal(components(edited).length, 1, 'Detached foreground');
  const features = new Map([
    ['26,19', 'hairDark'], ['33,19', 'hairDark'],
    ...[25, 26, 27, 32, 33].map(x => [`${x},21`, 'ink']),
    ['25,22', 'cloth'], ['26,22', 'hairDark'], ['27,22', 'hairDark'],
    ['32,22', 'cloth'], ['33,22', 'hairDark'], ['31,24', 'skinShade'],
    ['30,28', 'skinDark'], ['31,28', 'skinDark'],
    ...[23, 24, 25].map(x => [`${x},26`, 'hairDark']), ['26,26', 'ink'],
  ]);
  const skin = ['skinMid', 'skin', 'skinShade'].map(key => rgba(key).join());
  for (const [y, left, right] of FACE) for (let x = left; x <= right; x++) {
    const feature = features.get(`${x},${y}`), pixel = edited.get(x, y);
    if (feature) assert.deepEqual(pixel, rgba(feature));
    else {
      assert(skin.includes(pixel.join()), `Stray face pixel: ${x},${y}`);
      if (pixel.join() === rgba('skinShade').join()) {
        assert(x === left || y === 30, `Interior shadow noise: ${x},${y}`);
      }
    }
  }
  // Keep a skin-only space between the new mic tip and the mouth.
  for (let y = 26; y <= 28; y++) for (let x = 27; x <= 29; x++) {
    assert(skin.includes(edited.get(x, y).join()), 'Mic touches lips');
  }
  return { changedArtPixels: changed.length, opaqueColours: palette(edited).length,
    unchangedOutsideFace: true, unchangedAlpha: true, exact2xGrid: true,
    faceSkinColours: ['skinMid', 'skin', 'skinShade'].map(key => P[key]) };
}

const v4 = decode(path.join(OUT, 'helper_v4.png'));
const original = native50(v4), edited = redrawFace(original);
const v5 = edited.scale(2);
const comparison = new Canvas(400, 100, '#0B1026');
const portraits = ['yudo', 'citizen'].map(name =>
  decode(path.join(ROOT, 'Assets/Resources/Portraits', name + '.png')));
[...portraits, v4, v5].forEach((portrait, i) => {
  assert.deepEqual([portrait.w, portrait.h], [100, 100]);
  comparison.blit(portrait, i * 100, 0);
});
const faces = new Canvas(40, 20, '#0B1026');
faces.blit(cropFace(original), 0, 0);
faces.blit(cropFace(edited), 20, 0);
const outputs = [
  ['helper_v5.png', v5],
  ['helper_v5_compare_3x.png', comparison.scale(3)],
  ['helper_v5_face_8x.png', faces.scale(8)],
];
for (const [name, image] of outputs) fs.writeFileSync(path.join(OUT, name), encode(image));
const saved = outputs.map(([name]) => decode(path.join(OUT, name)));
saved.forEach((image, i) => assert.deepEqual(image.data, outputs[i][1].data, 'PNG round-trip'));
console.log(JSON.stringify({ draftOnly: true,
  ...verify(original, edited, ...saved),
  comparisonOrder: ['yudo', 'citizen', 'helper_v4', 'helper_v5'],
  faceOrder: ['helper_v4', 'helper_v5'],
  faceCropArtPixels: { x: 18, y: 14, width: 20, height: 20 },
  previewBackground: '#0B1026',
}, null, 2));
