"""Draft-only HUD dice repair. Python + Pillow; all sprite edits are integer pixels."""
from pathlib import Path
import hashlib
import json
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[5]
OUT = Path(__file__).resolve().parent
SRC = ROOT / 'Assets/Resources/DraftHud'
NAMES = [f'dice_face_{n}' for n in range(1, 7)] + ['dice_off'] + [f'dice_roll_{n}' for n in range(4)]
TRANSPARENT = (0, 0, 0, 0)
FRONT = (8, 11, 30, 255)
TOP = (16, 24, 48, 255)
SHADE = (25, 48, 68, 255)
EDGE = (35, 115, 140, 255)
CYAN = (79, 240, 255, 255)
LIGHT = (192, 251, 255, 255)
RED = (242, 45, 82, 255)
DARK_RED = (187, 33, 63, 255)
GREY_DARK = (44, 49, 58, 255)
GREY = (66, 73, 81, 255)
GREY_LIGHT = (107, 120, 124, 255)

# Fixed physical die: front +Z=1, top +Y=2, left -X=3;
# back -Z=6, bottom -Y=5, right +X=4. Only proper rotations allowed.
NORMALS = {1: (0, 0, 1), 2: (0, 1, 0), 3: (-1, 0, 0),
           6: (0, 0, -1), 5: (0, -1, 0), 4: (1, 0, 0)}
REST = {1: (1, 2, 3), 2: (2, 6, 3), 3: (3, 2, 6),
        4: (4, 2, 1), 5: (5, 1, 3), 6: (6, 5, 3)}
ROLL = [(2, 3, 1), (3, 1, 2), (5, 3, 6), REST[1]]

# Pip slots are (upper-left, upper-right, lower-left, lower-right, centre,
# middle-left, middle-right). Top 6 uses two short rows of three;
# side 6 uses two long columns of three, following the original face shears.
TOP_SLOTS = [(7, 4), (15, 4), (9, 6), (17, 6), (12, 5), (11, 4), (13, 6)]
SIDE_SLOTS = [(3, 7), (6, 10), (3, 13), (6, 16), (5, 12), (3, 10), (6, 13)]
PATTERN = {1: [4], 2: [0, 3], 3: [0, 4, 3],
           4: [0, 1, 2, 3], 5: [0, 1, 4, 2, 3], 6: [0, 5, 1, 2, 6, 3]}
# These coordinates preserve the six original front-face pip patterns exactly.
FRONT_SLOTS = [(11, 11), (17, 11), (11, 17), (17, 17), (14, 14), (11, 14), (17, 14)]

sources = {name: Image.open(SRC / (name + '.png')).convert('RGBA') for name in NAMES}
hashes = {name: hashlib.sha256((SRC / (name + '.png')).read_bytes()).hexdigest() for name in NAMES}
records = {}


def cross(a, b):
    return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])


def check_die(triple):
    f, t, s = triple
    assert len(set(triple)) == 3
    assert all(a+b != 7 for a, b in [(f, t), (t, s), (s, f)])
    assert cross(NORMALS[f], NORMALS[t]) == NORMALS[s], triple


def clean(name):
    im = sources[name].copy()
    for y in range(24):
        for x in range(24):
            if im.getpixel((x, y)) in (RED, DARK_RED):
                # Original narrow-face pips replace a single flat face color.
                im.putpixel((x, y), FRONT if x >= 9 and y >= 11 else
                            (TOP if im.getpixel((x, y)) == RED else
                             (TOP if name.startswith('dice_face') or name == 'dice_roll_3' else SHADE)))
    return im


def stamp(im, slots, n, color, size=1):
    points = []
    for index in PATTERN[n]:
        x, y = slots[index]
        for dy in range(size):
            for dx in range(size):
                point = (x+dx, y+dy)
                assert im.getpixel(point)[3] == 255, point
                assert im.getpixel(point) not in (CYAN, EDGE, LIGHT), point
                im.putpixel(point, color)
                points.append(point)
    return points


def save(name, im, triple, regions):
    check_die(triple)
    assert im.size == (24, 24) and im.mode == 'RGBA'
    assert set(im.getchannel('A').get_flattened_data()) == {0, 255}
    assert im.getchannel('A').tobytes() == sources[name].getchannel('A').tobytes()
    im.save(OUT / (name + '.png'))
    records[name] = {'front_top_side': triple, 'pip_pixels': regions}


for n in range(1, 7):
    name = f'dice_face_{n}'
    im = clean(name)
    f, t, s = REST[n]
    regions = {'front': stamp(im, FRONT_SLOTS, f, RED, 2),
               'top': stamp(im, TOP_SLOTS, t, RED),
               'side': stamp(im, SIDE_SLOTS, s, DARK_RED)}
    save(name, im, REST[n], regions)

# Retain the original three opaque greys; dark pips need the middle-grey body.
im = clean('dice_face_1')
grey_map = {TRANSPARENT: TRANSPARENT, FRONT: GREY, TOP: GREY, SHADE: GREY,
            CYAN: GREY_LIGHT, LIGHT: GREY_LIGHT, EDGE: GREY_DARK}
for y in range(24):
    for x in range(24):
        im.putpixel((x, y), grey_map[im.getpixel((x, y))])
regions = {'front': stamp(im, FRONT_SLOTS, 1, GREY_DARK, 2),
           'top': stamp(im, TOP_SLOTS, 2, GREY_DARK),
           'side': stamp(im, SIDE_SLOTS, 3, GREY_DARK)}
save('dice_off', im, REST[1], regions)

# Existing silhouette keyframes already tumble from a tilted pose into rest.
# Each frame gets complete, isolated pip patterns on its three visible planes.
ROLL_TOP = [
    [(8, 6), (14, 5), (11, 11), (17, 10), (12, 8), (11, 5), (14, 10)],
    [(7, 5), (14, 4), (11, 10), (18, 9), (12, 7), (10, 4), (14, 9)],
    [(7, 4), (14, 4), (10, 8), (17, 7), (12, 6), (10, 4), (13, 8)],
    TOP_SLOTS,
]
ROLL_SIDE = [
    [(4, 10), (7, 13), (6, 14), (8, 17), (6, 12), (5, 12), (8, 15)],
    [(4, 9), (7, 12), (6, 15), (8, 17), (6, 13), (4, 12), (8, 15)],
    [(3, 8), (7, 11), (4, 13), (7, 17), (5, 12), (3, 10), (7, 14)],
    SIDE_SLOTS,
]
ROLL_FRONT = [
    [(12, 15), (17, 14), (12, 18), (17, 17), (15, 16), (12, 16), (17, 15)],
    [(12, 14), (18, 13), (12, 18), (18, 17), (15, 16), (12, 16), (18, 15)],
    [(12, 13), (18, 12), (12, 17), (18, 17), (15, 15), (12, 15), (18, 15)],
    FRONT_SLOTS,
]
for frame in range(4):
    name = f'dice_roll_{frame}'
    im = clean(name)
    f, t, s = ROLL[frame]
    regions = {'front': stamp(im, ROLL_FRONT[frame], f, RED, 2),
               'top': stamp(im, ROLL_TOP[frame], t, RED),
               'side': stamp(im, ROLL_SIDE[frame], s, DARK_RED)}
    save(name, im, ROLL[frame], regions)

# Review-only bitmap labels, nearest-neighbour enlargement with no font AA.
font = ImageFont.load_default(size=12)
sheet = Image.new('RGBA', (224 * len(NAMES), 248), (24, 29, 40, 255))
draw = ImageDraw.Draw(sheet)
for i, name in enumerate(NAMES):
    x = 224*i
    for yy in range(24):
        for xx in range(24):
            c = (31, 38, 51, 255) if (xx//4+yy//4)%2 else (24, 29, 40, 255)
            draw.rectangle((x+16+xx*8, 32+yy*8, x+23+xx*8, 39+yy*8), fill=c)
    im = Image.open(OUT / (name + '.png'))
    sheet.alpha_composite(im.resize((192, 192), Image.Resampling.NEAREST), (x+16, 32))
    # Render text to a 1-bit mask so preview labels are hard pixels too.
    label = Image.new('1', (224, 24))
    ImageDraw.Draw(label).text((8, 4), name + '.png', font=font, fill=1)
    sheet.paste((218, 231, 239, 255), (x, 0), label)
    triple = records[name]['front_top_side']
    label = Image.new('1', (224, 24))
    ImageDraw.Draw(label).text((8, 3), 'F / T / L: ' + ' / '.join(map(str, triple)), font=font, fill=1)
    sheet.paste((218, 231, 239, 255), (x, 224), label)
sheet.save(OUT / 'preview_8x.png')

# Validate output counts and make the source-preservation check reproducible.
for name, rec in records.items():
    im = Image.open(OUT / (name + '.png'))
    for region, n in zip(('front', 'top', 'side'), rec['front_top_side']):
        pixels = rec['pip_pixels'][region]
        assert len(set(map(tuple, pixels))) == n * (4 if region == 'front' else 1)
    if name != 'dice_off':
        expected = {tuple(p) for pixels in rec['pip_pixels'].values() for p in pixels}
        actual = {(x, y) for y in range(24) for x in range(24)
                  if im.getpixel((x, y)) in (RED, DARK_RED)}
        assert actual == expected, (name, actual ^ expected)
    assert hashlib.sha256((SRC / (name + '.png')).read_bytes()).hexdigest() == hashes[name]
    source_palette = set(sources['dice_off' if name == 'dice_off' else 'dice_face_1'].get_flattened_data())
    assert set(im.get_flattened_data()) <= source_palette

(OUT / 'face_layout.json').write_text(json.dumps(records, indent=2) + '\n')
print('Verified 11 RGBA sprites: 24x24, hard alpha, exact source silhouettes/palettes, legal handedness and pip counts; source hashes unchanged.')
