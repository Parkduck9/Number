"""Hand-authored HUD drafts. Python 3 + Pillow; no fonts, filters or resampling.

All artwork is a character grid. Helpers only stamp pixels, apply a one-pixel
outline, and assemble at integer coordinates. Run from any working directory.
"""

from pathlib import Path
from PIL import Image

OUT = Path(__file__).resolve().parent.parent
CLEAR = (0, 0, 0, 0)


def rgba(value, alpha=255):
    return tuple(bytes.fromhex(value.lstrip("#"))) + (alpha,)


def grid(text, width, height):
    """Omitted trailing dots are explicitly transparent padding."""
    rows = text.strip("\n").splitlines()
    assert len(rows) == height, (len(rows), height)
    assert all(len(row) <= width for row in rows), [(r, len(r)) for r in rows]
    return tuple(row.ljust(width, ".") for row in rows)


def stamp(image, rows, palette, xy=(0, 0)):
    ox, oy = xy
    for y, row in enumerate(rows):
        for x, char in enumerate(row):
            if char != ".":
                assert 0 <= ox + x < image.width and 0 <= oy + y < image.height
                image.putpixel((ox + x, oy + y), palette[char])


def tile(rows, palette):
    image = Image.new("RGBA", (len(rows[0]), len(rows)), CLEAR)
    stamp(image, rows, palette)
    return image


def copy_pixels(dst, src, xy):
    # Copy authored RGBA verbatim; never alpha-blend semi-transparent fills.
    ox, oy = xy
    for y in range(src.height):
        for x in range(src.width):
            pixel = src.getpixel((x, y))
            if pixel[3]:
                dst.putpixel((ox + x, oy + y), pixel)


def sheet(images, gap=1):
    result = Image.new("RGBA", (sum(i.width for i in images) + gap * (len(images)-1),
                                max(i.height for i in images)), CLEAR)
    x = 0
    for image in images:
        copy_pixels(result, image, (x, 0))
        x += image.width + gap
    return result


def save(image, name):
    image.save(OUT / name)


def pixel_colors(image):
    return {color for count, color in image.getcolors(image.width * image.height)}


# Sixteen shared face colors, all opaque. Light falls from upper left.
FACE = {key: rgba(value) for key, value in {
    "B": "05061A", "b": "0B0E2A", "h": "14162E", "H": "2A2C4A",
    "s": "E0A986", "m": "C07F62", "d": "8A5444", "n": "1C1F4A",
    "N": "2E3366", "w": "D8DCE8", "v": "9097B0", "a": "5A6178",
    "A": "9AA3B8", "c": "3FE6F0", "C": "B8FFFF", "g": "D9A13A",
}.items()}

HEAD = grid("""
.............B
.........BB.BhB....BB
........BhhBhhBB.BhB
......BBhHHhhhhhhhBhBB
.....BhHHHHHhhhhhhhhhhBB
....BhHHHHhhhhhhhhhhhhhhB
...BhHHHhhhhhhhhhhhhhhhhhB
...BhHHhhhhhhhhhHHhhhhhhhhB
..BhhHhhhhhhhhhhhHhhhhhhhhB
..BhhhhhhhhhhhBhHhhhhhhhhhB
..BhhhhhhhhhhBssBhhhhhhhhhB
..BhhhhhhhhBssssssBhhhhhhhB
...BhhhhhBsssssssssBhhhhhB
...BhhhhBsssssssssssBhhhhB
....BhhhBssssssssssssBhhhB
....BhhhBsssssssssssssBhhB
....BhhBssssssssssssssBhhB
....BhhBssssssssssssssBhhB
...BmBhBssssssssssssssBhBmB
...BmsBhssssssssssssssBmdB
...BmsBsssssssssssssssBmdB
...BmmBssssssssssssssssBddB
....BdBssssssssssssssmBdB
.....BsssssssssssssssmmB
.....BsssssssmsmssssmmmB
.....BsssssssmdmsssmmmmB
......BssssssmddssmmmmB
......BssssssssssmmmmB
.......BssssssssmmmmB
........BssssssmmmmB
.........BsssmmmmBB
..........BBmmmBB
............BBB
""", 32, 33)

TORSO = grid("""
..................BmmmmmmB
...............BBNmmmmmmnnnNBB
............BBNNNNmmmmmmnnnnnnnBB
.........BBNNNNNNNNmmmmnnnnnnnnnnnBB
.......BBNNNNNNNNNNNnnnnnnnnnnnnnnhhBB
.....BBNNNNNNNNNNNNNnnnnnnnnnnnnnnhhnnBB
....BNNNNNNNNNNNNNNNnnnnnnnnnnnnnnhhnnnnB
...BNNNNNNNNNNNNNNNNnnnnnnnnnnnnnnhhnnnnnB
..BNNNNNNNNNNNNNNNNNnnnnnnnnnnnnnnhhnnnnnnB
.BNNNNNNNNNNNNNNNNNNnnnnnnnnnnnnnnhhnnnnnnnB
BNNNNNNNNNNNNNNNNNNNnnnnnnnnnnnnnnhhnnnnnnnnB
BNNNNNNNNNNNNNNNNNNNnnnnnnnnnnnnnnhhnnnnnnnnB
BNNNNNNNNNNNNNNNNNNNnnnnnnnnnnnnnnhhnnnnnnnnB
""", 46, 13)

# Viewer-right panel tucks under the uninterrupted left-over-right band.
# Two light pixels across, with a one-pixel shadow on the lower edge.
COLLAR_UNDER = grid("""
........wwv
.......wwv
.......wwv
......wwv
......wwv
.....wwv
.....wwv
....wwv
""", 11, 8)
COLLAR_OVER = grid("""
.ww
.vww
..vww
...vww
....vww
.....vww
......vww
.......vww
........vww
.........vww
..........v
""", 12, 11)
OPEN_EYES = grid("""
.........hhh
hhh
............
hhh......hhh
whh......hhw
hhh......hhh
""", 12, 6)
MOUTH = grid("""
dd
..d
""", 3, 2)

# One replacement region per hit, accumulated on the same base image.
EAR = grid("""
.BAAB
BAAaB
BAaaB
BAacB
BAaaB
BAaaB
.BaaB
..BB
""", 5, 8)
NOSE = grid("""
.BAAB
.BAaB
.BAaB
BAcaB
BAaaB
.BBB
""", 5, 6)
EYES = grid("""
BAAAAAAB.BAAAAAAB
BAccccABaBAccccAB
BAccccAB.BAccccAB
.BaaaaB...BaaaaB
""", 17, 4)
JAW = grid("""
.BAAAAAAAAAAAAB
BAAaaaaaaaaaaaB
BAaccccccccaaaB
.BAaaaaaaaaaaB
..BAaaaaaaaaB
...BAaaaaaaB
....BBBBBBB
""", 15, 7)
CHIP = grid("""
gggggggg
gAAAAAag
gABBBBag
gABCCBag
gABccBag
gABBBBag
gaaaaaag
gggggggg
""", 8, 8)
EYE_MARK = grid("""
CCC
ChC
CCC
""", 3, 3)


def faces():
    base = tile(tuple(["B" * 48] + ["B" + "b" * 46 + "B"] * 46 + ["B" * 48]), FACE)
    stamp(base, TORSO, FACE, (1, 34))
    stamp(base, COLLAR_UNDER, FACE, (20, 35))
    stamp(base, COLLAR_OVER, FACE, (17, 35))
    stamp(base, HEAD, FACE, (8, 3))
    stamp(base, OPEN_EYES, FACE, (18, 21))
    stamp(base, MOUTH, FACE, (20, 31))
    frames = [base.copy()]
    steps = [((EAR, (34, 21)),), ((NOSE, (22, 26)),),
             ((EYES, (16, 24)),), ((JAW, (16, 30)),),
             ((CHIP, (21, 14)), (EYE_MARK, (19, 25)), (EYE_MARK, (28, 25)))]
    for edits in steps:
        before = base.copy()
        allowed = set()
        for rows, xy in edits:
            stamp(base, rows, FACE, xy)
            allowed.update((xy[0]+x, xy[1]+y) for y, row in enumerate(rows)
                           for x, char in enumerate(row) if char != ".")
        changed = {(x, y) for y in range(48) for x in range(48)
                   if base.getpixel((x, y)) != before.getpixel((x, y))}
        assert changed and changed <= allowed
        frames.append(base.copy())
    assert len(set().union(*(pixel_colors(f) for f in frames))) <= 16
    for index, frame in enumerate(frames):
        save(frame, f"face_{index}.png")
    save(sheet(frames, gap=0), "face_sheet.png")


# Ten explicitly authored 10x16 silhouettes; 2-pixel strokes, cut corners.
BROADCAST = [grid(text, 10, 16) for text in [
"""
.########
##########
##......##
##......##
##......##
##......##
##......##
##......##
##......##
##......##
##......##
##......##
##......##
##......##
##########
.########
""", """
....##
...###
..####
....##
....##
....##
....##
....##
....##
....##
....##
....##
....##
....##
..######
..######
""", """
.########
##########
........##
........##
........##
........##
........##
.#########
#########
##
##
##
##
##
##########
.#########
""", """
.########
##########
........##
........##
........##
........##
........##
..#######
..########
........##
........##
........##
........##
........##
##########
.########
""", """
##......##
##......##
##......##
##......##
##......##
##......##
##......##
##########
.#########
........##
........##
........##
........##
........##
........##
........##
""", """
.#########
##########
##
##
##
##
##
#########
.#########
........##
........##
........##
........##
........##
##########
.########
""", """
.########
#########
##
##
##
##
##
#########
##########
##......##
##......##
##......##
##......##
##......##
##########
.########
""", """
.#########
##########
........##
........##
.......##
.......##
......##
......##
.....##
.....##
....##
....##
...##
...##
...##
...##
""", """
.########
##########
##......##
##......##
##......##
##......##
##......##
.########
##########
##......##
##......##
##......##
##......##
##......##
##########
.########
""", """
.########
##########
##......##
##......##
##......##
##......##
##......##
##########
.#########
........##
........##
........##
........##
........##
.#########
.########
"""]]

ARC_LEFT = grid("""
..o..m..i
..o..m..i
.o..m..i
.o..m..i
.o..m..i
o..m..i
o..m..i
o..m..i
o..m..i
o..m..i
o..m..i
.o..m..i
.o..m..i
.o..m..i
..o..m..i
..o..m..i
""", 9, 16)
# Keep the original 11px arc cell; the curve occupies its inner nine columns.
ARC_RIGHT = tuple(row[::-1].ljust(11, ".") for row in ARC_LEFT)
ARC_LEFT = tuple(row.rjust(11, ".") for row in ARC_LEFT)


def outlined_digit(rows, colors):
    points = {(x+1, y+1) for y, row in enumerate(rows)
              for x, char in enumerate(row) if char == "#"}
    image = Image.new("RGBA", (12, 18), CLEAR)
    for x, y in points:
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if (x+dx, y+dy) not in points:
                    image.putpixel((x+dx, y+dy), colors["outline"])
    for x, y in points:
        # Only the upper and left silhouette edges catch the light.
        highlight = (x, y-1) not in points or (x-1, y) not in points
        image.putpixel((x, y), colors["highlight" if highlight else "core"])
    return image


def broadcasts():
    variants = {
        "magenta": ("FF3FB4", "FFC2E8", "3A0A2E", "C02C8A", "7A1E5C", 7),
        "cyan": ("3FE6F0", "D0FFFF", "062A33", "2A9CB0", "1A5A70", 3),
    }
    for name, (core, light, dark, middle, outer, example) in variants.items():
        colors = {"core": rgba(core), "highlight": rgba(light), "outline": rgba(dark)}
        digits = [outlined_digit(rows, colors) for rows in BROADCAST]
        palette = {"i": rgba(core), "m": rgba(middle), "o": rgba(outer)}
        left, right = tile(ARC_LEFT, palette), tile(ARC_RIGHT, palette)
        save(sheet(digits), f"broadcast_digits_{name}.png")
        save(sheet([left, right]), f"broadcast_arcs_{name}.png")
        preview = Image.new("RGBA", (40, 18), CLEAR)
        copy_pixels(preview, left, (0, 1))
        copy_pixels(preview, digits[example], (13, 0))
        copy_pixels(preview, right, (27, 1))
        save(preview, f"broadcast_{example}_{name}.png")


# 7x11 final glyph bounds: two-pixel near-white strokes and a one-pixel
# bright exterior edge. Small counters stay clear, with no interior outline.
HOLO = [grid(text, 7, 11) for text in [
"""
.eeeee
eccccce
eccccce
ecc.cce
ecc.cce
ecc.cce
ecc.cce
ecc.cce
eccccce
eccccce
.eeeee
""", """
...ee
..ecce
.ecce
..ecce
..ecce
..ecce
..ecce
..ecce
.ecccce
.ecccce
.eeeeee
""", """
.eeeee
eccccce
eccccce
...ecce
...ecce
eccccce
eccccce
ecce
ecce
eccccce
.eeeee
""", """
.eeeee
eccccce
eccccce
...ecce
...ecce
.ecccce
.ecccce
...ecce
...ecce
eccccce
.eeeee
""", """
.ee.ee
ecc.cce
ecc.cce
ecc.cce
ecc.cce
eccccce
eccccce
.eeecce
...ecce
...ecce
....ee
""", """
.eeeee
eccccce
eccccce
ecce
ecce
eccccce
eccccce
...ecce
...ecce
eccccce
.eeeee
""", """
.eeeee
eccccce
eccccce
ecce
ecce
eccccce
eccccce
ecc.cce
ecc.cce
eccccce
.eeeee
""", """
.eeeee
eccccce
eccccce
...ecce
...ecce
..ecce
..ecce
.ecce
.ecce
.ecce
..ee
""", """
.eeeee
eccccce
eccccce
ecc.cce
ecc.cce
eccccce
eccccce
ecc.cce
ecc.cce
eccccce
.eeeee
""", """
.eeeee
eccccce
eccccce
ecc.cce
ecc.cce
eccccce
eccccce
...ecce
...ecce
eccccce
.eeeee
"""]]

WINDOW = grid("""
LLLbbbbbbbbbbbbbbLLL
LffffffffffffffffffL
LffffffffffffffffffL
bssssssssssssssssssb
bffffffffffffffffffb
bffffffffffffffffffb
bssssssssssssssssssb
bffffffffffffffffffb
bffffffffffffffffffb
bssssssssssssssssssb
bffffffffffffffffffb
bffffffffffffffffffb
bssssssssssssssssssb
LffffffffffffffffffL
LffffffffffffffffffL
LLLbbbbbbbbbbbbbbLLL
""", 20, 16)


def holograms():
    variants = {
        "gold": ("FFD23F", "E0A020", "FFF6C8", "FFB000", (3,)),
        "red": ("FF3B3B", "C81E1E", "FFE6E6", "FF2020", (0, 7)),
    }
    for name, (border, scan, core, fill, examples) in variants.items():
        palette = {"b": rgba(scan), "L": rgba(border),
                   "f": rgba(fill, 70), "s": rgba(scan, 110),
                   "c": rgba(core), "e": rgba(border)}
        window = tile(WINDOW, palette)
        digits = [tile(rows, palette) for rows in HOLO]
        save(window, f"holo_window_{name}.png")
        save(sheet(digits), f"holo_digits_{name}.png")
        for digit in examples:
            preview = window.copy()
            # Odd-sized digit in even-sized frame: upper-left tie break (6,2).
            copy_pixels(preview, digits[digit], (6, 2))
            save(preview, f"holo_{name}_{digit}.png")


def verify_and_report():
    paths = sorted(OUT.glob("*.png"))
    assert len(paths) == 20
    for path in paths:
        with Image.open(path) as image:
            colors = pixel_colors(image)
            alphas = {p[3] for p in colors}
            if path.name.startswith("holo_window") or path.name in {
                "holo_gold_3.png", "holo_red_0.png", "holo_red_7.png"
            }:
                assert alphas <= {70, 110, 255}, path.name
                for x in range(20):
                    assert image.getpixel((x, 0))[3] == image.getpixel((x, 15))[3] == 255
                for y in range(16):
                    assert image.getpixel((0, y))[3] == image.getpixel((19, y))[3] == 255
            else:
                assert alphas <= {0, 255}, path.name
            print(f"{path.name:34} {image.width:3}x{image.height:<3}  {len(colors):2} RGBA colors")


if __name__ == "__main__":
    OUT.mkdir(parents=True, exist_ok=True)
    faces()
    broadcasts()
    holograms()
    verify_and_report()
