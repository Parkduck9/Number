"""DRAFT assets only. Hand-designed pixel masks; no fonts, models or AA.

Run with Python 3.7+; no third-party dependencies. Every drawing coordinate is
native resolution. The tiny PNG codec supports 8-bit RGB/RGBA noninterlaced PNGs.
Nothing is imported into or connected to gameplay by this script.
"""
from pathlib import Path
import struct
import zlib


OUT = Path(__file__).resolve().parents[1]
ROOT = OUT.parents[3]
PALETTE_PATH = ROOT / "Assets/Art/Palette/number_palette.png"
CLEAR = (0, 0, 0, 0)


def read_png(path):
    data = Path(path).read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", path
    pos, compressed, header = 8, bytearray(), None
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        kind = data[pos + 4:pos + 8]
        payload = data[pos + 8:pos + 8 + length]
        crc = struct.unpack(">I", data[pos + 8 + length:pos + 12 + length])[0]
        assert zlib.crc32(kind + payload) & 0xffffffff == crc, path
        if kind == b"IHDR":
            header = struct.unpack(">IIBBBBB", payload)
        elif kind == b"IDAT":
            compressed.extend(payload)
        pos += length + 12
        if kind == b"IEND":
            break
    width, height, depth, mode, comp, filt, interlace = header
    assert depth == 8 and mode in (2, 6) and (comp, filt, interlace) == (0, 0, 0)
    channels = 3 if mode == 2 else 4
    stride = width * channels
    raw = zlib.decompress(compressed)
    assert len(raw) == height * (stride + 1)
    previous, pixels = [0] * stride, []
    for y in range(height):
        offset = y * (stride + 1)
        filter_type = raw[offset]
        assert filter_type in range(5)
        row = list(raw[offset + 1:offset + stride + 1])
        for x in range(stride):
            left = row[x - channels] if x >= channels else 0
            up = previous[x]
            corner = previous[x - channels] if x >= channels else 0
            if filter_type == 1:
                prediction = left
            elif filter_type == 2:
                prediction = up
            elif filter_type == 3:
                prediction = (left + up) // 2
            elif filter_type == 4:
                p = left + up - corner
                distances = (abs(p - left), abs(p - up), abs(p - corner))
                prediction = (left, up, corner)[distances.index(min(distances))]
            else:
                prediction = 0
            row[x] = (row[x] + prediction) & 255
        pixels.append([tuple(row[x:x + channels]) + ((255,) if channels == 3 else ())
                       for x in range(0, stride, channels)])
        previous = row
    return pixels


def write_png(path, pixels):
    width, height = len(pixels[0]), len(pixels)
    assert all(len(row) == width for row in pixels)

    def chunk(kind, payload):
        return (struct.pack(">I", len(payload)) + kind + payload
                + struct.pack(">I", zlib.crc32(kind + payload) & 0xffffffff))

    raw = b"".join(b"\0" + bytes(v for pixel in row for v in pixel) for row in pixels)
    data = (b"\x89PNG\r\n\x1a\n"
            + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))
    Path(path).write_bytes(data)


def blank(width, height, value=None):
    return [[value for _ in range(width)] for _ in range(height)]


def ascii_grid(rows, colors):
    width = len(rows[0])
    assert all(len(row) == width for row in rows), [len(row) for row in rows]
    return [[None if char == "." else colors[char] for char in row] for row in rows]


# O is the single outline ink. H/P are warm paint, never number gold.
# The two-pixel cuts are stencil bridges, not missing key glyphs. All six caps
# intentionally have empty centers; the game's pixel font supplies labels later.
KEY_SQUARE = [
    "................",
    "....OOO..OOO....",
    "...OHHH..PPPO...",
    "..OHOOO..OOPPO..",
    ".OHO........OPO.",
    ".OHO........OPO.",
    ".OHO........OPO.",
    "..HO........OPO.",
    ".OHO........OPO.",
    ".OHO........OP..",
    ".OHO........OPO.",
    ".OHO........OPO.",
    "..OPOOO..OOPPO..",
    "...OPPP..PPPO...",
    "....OOO..OOO....",
    "................",
]
KEY_WIDE = [
    "........................",
    "....OOOOOOO..OOOOOOO....",
    "...OHHHHHHH..PPPPPPPO...",
    "..OHOOOOOOO..OOOOOOPPO..",
    ".OHO................OPO.",
    ".OHO................OPO.",
    ".OHO................OPO.",
    "..HO................OPO.",
    ".OHO................OPO.",
    ".OHO................OP..",
    ".OHO................OPO.",
    ".OHO................OPO.",
    "..OPOOOOOOO..OOOOOOPPO..",
    "...OPPPPPPP..PPPPPPPO...",
    "....OOOOOOO..OOOOOOO....",
    "........................",
]
MOUSE = [
    "................",
    ".....PPP.PP.....",
    "....POOOPOOP....",
    "...POOOOPOOOP...",
    "...POOOOPOOOP...",
    "...POOOOPOOOP...",
    "...PPPPPPPPPP...",
    "...PP......PP...",
    "....P......PP...",
    "...PP......PP...",
    "...PP......PP...",
    "...PP......P....",
    "....PP....PP....",
    "....PPPPPPPP....",
    ".....PPP.PP.....",
    "................",
]

# Two concentric one-pixel rings and eight directed two-pixel radial ticks.
# Deliberately faceted circles: this is the final 16px grid, not a downsample.
SHOCKWAVE = [
    ".......HH.......",
    ".H.....HH.....H.",
    "..H...HHHH...H..",
    "....HH....CC....",
    "...H........C...",
    "...H..HHHH..C...",
    "..H..H....C..C..",
    "HHH..H....C..CCC",
    "HHH..H....C..CCC",
    "..H..H....C..C..",
    "...C..CCCC..C...",
    "...C........C...",
    "....CC....CC....",
    "..C...CCCC...C..",
    ".C.....CC.....C.",
    ".......CC.......",
]


def ink_outline(grid):
    """One native pixel of #0 behind the stencil/energy mask; no blur."""
    result = blank(len(grid[0]), len(grid))
    for y, row in enumerate(grid):
        for x, value in enumerate(row):
            if value is not None:
                for dx, dy in ((0, 0), (-1, 0), (1, 0), (0, -1), (0, 1)):
                    xx, yy = x + dx, y + dy
                    if 0 <= yy < len(grid) and 0 <= xx < len(row):
                        result[yy][xx] = 0
    for y, row in enumerate(grid):
        for x, value in enumerate(row):
            if value is not None:
                result[y][x] = value
    return result


def span(grid, y, start, end, color):
    """Inclusive runs are a compact hand-designed array, at native resolution."""
    grid[y][start:end + 1] = [color] * (end - start + 1)


def boss_frame(angry=False):
    grid = blank(96, 64)
    # Chamfered heavy casting: outer and inner row-run arrays. Top/left carry
    # the key light, right/bottom stay in the dark metal and navy steps.
    bezel_rows = [(2, 6, 89), (3, 4, 91), (4, 3, 92)]
    bezel_rows += [(y, 2, 93) for y in range(5, 58)]
    bezel_rows += [(58, 3, 92), (59, 4, 91), (60, 6, 89)]
    for y, left, right in bezel_rows:
        span(grid, y, left, right, 0)
        if 3 <= y <= 59:
            span(grid, y, left + 1, right - 1, 6 if y < 54 else 3)
    for y, left, right in [(3, 6, 89), (4, 5, 90), (5, 4, 90), (6, 3, 9)]:
        span(grid, y, left, right, 7)
    span(grid, 3, 7, 67, 8)
    for y in range(6, 55):
        span(grid, y, 3, 4, 7)
        span(grid, y, 90, 92, 3)
    span(grid, 58, 7, 88, 0)
    # Inner gasket and stepped glass corners.
    glass_rows = [(8, 12, 83), (9, 10, 85)]
    glass_rows += [(y, 9, 86) for y in range(10, 52)]
    glass_rows += [(52, 10, 85), (53, 12, 83)]
    for y, left, right in glass_rows:
        span(grid, y, left, right, 0)
    screen_rows = [(10, 13, 82), (11, 11, 84)]
    screen_rows += [(y, 10, 85) for y in range(12, 50)]
    screen_rows += [(50, 11, 84), (51, 13, 82)]
    for y, left, right in screen_rows:
        span(grid, y, left, right, 2 if angry else 1)
    for y in (16, 24, 40, 48):
        span(grid, y, 11, 84, 0 if angry else 2)
    # No HUD glyphs or fake lettering. Just a recessed seam, corner bolts and
    # three lower-bezel ventilation cuts, identical in both states.
    span(grid, 7, 14, 79, 3)
    for x, y in ((6, 6), (87, 55)):
        span(grid, y, x, x + 1, 0)
        span(grid, y + 1, x, x + 1, 3)
    for left in (15, 21, 27):
        span(grid, 56, left, left + 2, 0)
    span(grid, 56, 76, 81, 13 if not angry else 16)

    # One elongated sensor aperture; the black rectangular iris interrupts it.
    # Each pair is the exact inclusive extent of one row, no ellipse rasterizer.
    idle_eye = [(14, 37), (10, 41), (7, 44), (5, 46), (3, 48), (1, 50),
                (0, 51), (1, 50), (3, 48), (5, 46), (7, 44), (10, 41), (14, 37)]
    angry_eye = [(10, 41), (5, 46), (1, 50), (0, 51),
                 (5, 46), (11, 40), (18, 33)]
    eye_rows, top = (angry_eye, 28) if angry else (idle_eye, 25)
    eye = blank(52, len(eye_rows))
    for y, (left, right) in enumerate(eye_rows):
        span(eye, y, left, right, 16 if angry else 13)
        if not angry and y < 6:
            span(eye, y, left, min(right, left + 2), 14)
    if not angry:
        span(eye, 0, 14, 37, 14)
        for y in range(1, 12):
            span(eye, y, 22, 29, 0)
        for y in range(3, 10):
            span(eye, y, 20, 21, 14)
    else:
        for y in range(1, 6):
            span(eye, y, 24, 29, 0)
    eye = ink_outline(eye)
    for y, row in enumerate(eye):
        for x, value in enumerate(row):
            if value is not None:
                grid[top + y][22 + x] = value
    if angry:
        # Three displaced screen-only rows. Bezel geometry stays registered.
        for y, shift in ((29, 3), (30, 3), (34, -2)):
            old = grid[y][10:86]
            shifted = [2] * 76
            for x, value in enumerate(old):
                if 0 <= x + shift < 76:
                    shifted[x + shift] = value
            grid[y][10:86] = shifted
        span(grid, 20, 66, 78, 16)
        span(grid, 43, 16, 27, 16)
    return grid


def rgba(grid, palette):
    return [[CLEAR if value is None else palette[value] for value in row] for row in grid]


def strip(images, gap=2):
    width = sum(len(im[0]) for im in images) + gap * (len(images) - 1)
    result, left = blank(width, len(images[0]), CLEAR), 0
    for im in images:
        assert len(im) == len(result)
        for y, row in enumerate(im):
            result[y][left:left + len(row)] = row
        left += len(im[0]) + gap
    return result


def preview(pixels, background):
    # Exact nearest-neighbor replication after binary-alpha compositing.
    opaque = [[pixel if pixel[3] else background for pixel in row] for row in pixels]
    return [[pixel for pixel in row for _ in range(4)] for row in opaque for _ in range(4)]


def main():
    palette_rows = read_png(PALETTE_PATH)
    assert len(palette_rows) == 1 and len(palette_rows[0]) == 24
    palette = palette_rows[0]
    assert palette[0] == (8, 11, 20, 255)
    assert palette[1] == (11, 16, 38, 255)
    OUT.mkdir(parents=True, exist_ok=True)
    controls = []
    for name in ("mouse_left", "mouse_right", "key_shift", "key_e", "key_space", "key_w", "key_a", "key_d"):
        if name.startswith("mouse"):
            grid = ascii_grid(MOUSE, {"P": 18, "O": 0})
            # Selected button is solid warm paint; the other stays dark.
            left, right = (4, 6) if name == "mouse_left" else (9, 11)
            for y in range(2, 6):
                span(grid, y, left, right, 21)
            # A single dry-spray chip, with the click target still contiguous.
            grid[5][left] = 0
            grid = ink_outline(grid)
        else:
            rows = KEY_WIDE if name in ("key_shift", "key_space") else KEY_SQUARE
            grid = ascii_grid(rows, {"O": 0, "H": 21, "P": 18})
        pixels = rgba(grid, palette)
        controls.append(pixels)
        write_png(OUT / ("ctrl_" + name + ".png"), pixels)
        write_png(OUT / ("ctrl_" + name + "_preview4x.png"), preview(pixels, palette[1]))
    control_strip = strip(controls)
    write_png(OUT / "ctrl_icons_strip.png", control_strip)
    write_png(OUT / "ctrl_icons_strip_preview4x.png", preview(control_strip, palette[1]))

    shockwaves = []
    for name, colors in (("shockwave_icon", {"H": 15, "C": 14}),
                         ("shockwave_icon_cooldown", {"H": 6, "C": 13})):
        pixels = rgba(ink_outline(ascii_grid(SHOCKWAVE, colors)), palette)
        shockwaves.append(pixels)
        write_png(OUT / (name + ".png"), pixels)
    write_png(OUT / "shockwave_icons_preview4x.png", preview(strip(shockwaves), palette[1]))

    bosses = []
    for name, angry in (("idle", False), ("angry", True)):
        pixels = rgba(boss_frame(angry), palette)
        bosses.append(pixels)
        write_png(OUT / ("boss_embector_" + name + ".png"), pixels)
    write_png(OUT / "boss_embector_preview4x.png", preview(strip(bosses), palette[1]))
    print("Generated 24 PNGs in " + str(OUT))
    print("Controls strip order: mouse_left, mouse_right, shift, e, space, w, a, d")
    print("Combined preview order: ready / cooldown; idle / angry. Gaps: 2 native pixels.")


if __name__ == "__main__":
    main()
