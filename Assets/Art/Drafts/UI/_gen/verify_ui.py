"""Check files on disk, including the complete 4x composited preview pixels.

Python 3.7+, stdlib only. Exit nonzero on any failure. Run after gen_ui_icons.py.
"""
from pathlib import Path
import sys

from gen_ui_icons import read_png


OUT = Path(__file__).resolve().parents[1]
ROOT = OUT.parents[3]


def main():
    palette_image = read_png(ROOT / "Assets/Art/Palette/number_palette.png")
    assert (len(palette_image[0]), len(palette_image)) == (24, 1)
    palette = palette_image[0]
    allowed = set(palette)
    reserved = {palette[i] for i in (15, 17, 19)}
    names = ("mouse_left", "mouse_right", "key_shift", "key_e", "key_space", "key_w", "key_a", "key_d")
    expected = {}
    for name in names:
        width = 24 if name in ("key_shift", "key_space") else 16
        expected["ctrl_" + name + ".png"] = (width, 16)
        expected["ctrl_" + name + "_preview4x.png"] = (width * 4, 64)
    expected.update({
        "ctrl_icons_strip.png": (158, 16),
        "ctrl_icons_strip_preview4x.png": (632, 64),
        "shockwave_icon.png": (16, 16),
        "shockwave_icon_cooldown.png": (16, 16),
        "shockwave_icons_preview4x.png": (136, 64),
        "boss_embector_idle.png": (96, 64),
        "boss_embector_angry.png": (96, 64),
        "boss_embector_preview4x.png": (776, 256),
    })
    actual = {p.name for p in OUT.glob("*.png")}
    assert actual == set(expected), ("Missing", set(expected) - actual, "Unexpected", actual - set(expected))
    images = {}
    for name, size in expected.items():
        pixels = read_png(OUT / name)
        images[name] = pixels
        assert (len(pixels[0]), len(pixels)) == size, (name, "size")
        colors = {pixel for row in pixels for pixel in row}
        assert all(pixel[3] in (0, 255) for pixel in colors), (name, "alpha")
        assert all(pixel[3] or pixel == (0, 0, 0, 0) for pixel in colors), (name, "transparent RGB")
        opaque = {pixel for pixel in colors if pixel[3]}
        assert opaque <= allowed, (name, "not in palette", opaque - allowed)
        assert len(opaque) <= 16, (name, "color budget", len(opaque))
        # Only the ready shockwave tile may have reserved cyan, also in preview.
        for y, row in enumerate(pixels):
            for x, pixel in enumerate(row):
                if pixel in reserved:
                    assert pixel == palette[15], (name, "red/gold reserved")
                    assert name == "shockwave_icon.png" or (name == "shockwave_icons_preview4x.png" and x < 64), (name, x, y, "reserved")
        print("PASS {:43s} {:3d}x{:<3d} colors={:2d}".format(name, *size, len(opaque)))

    # Independently construct combined source rows and expected preview pixels.
    def combine(sources):
        height = len(sources[0])
        return [sum((source[y] + ([(0, 0, 0, 0)] * 2 if i < len(sources) - 1 else [])
                     for i, source in enumerate(sources)), []) for y in range(height)]

    ctrl_sources = [images["ctrl_" + name + ".png"] for name in names]
    combined_controls = combine(ctrl_sources)
    assert images["ctrl_icons_strip.png"] == combined_controls, "Strip order / transparent 2px gaps"
    preview_sources = [("ctrl_" + name + "_preview4x.png", source) for name, source in zip(names, ctrl_sources)]
    preview_sources += [
        ("ctrl_icons_strip_preview4x.png", combined_controls),
        ("shockwave_icons_preview4x.png", combine([images["shockwave_icon.png"], images["shockwave_icon_cooldown.png"]])),
        ("boss_embector_preview4x.png", combine([images["boss_embector_idle.png"], images["boss_embector_angry.png"]])),
    ]
    for name, source in preview_sources:
        for y, row in enumerate(images[name]):
            for x, pixel in enumerate(row):
                original = source[y // 4][x // 4]
                assert pixel == (original if original[3] else palette[1]), (name, "4x nearest / background", x, y)
    cooldown_colors = {pixel for row in images["shockwave_icon_cooldown.png"] for pixel in row if pixel[3]}
    assert cooldown_colors <= {palette[i] for i in (0, 6, 13)}, "Cooldown has a bright color"
    for a, b in zip(images["shockwave_icon.png"], images["shockwave_icon_cooldown.png"]):
        assert [p[3] for p in a] == [p[3] for p in b], "Cooldown silhouette mismatch"
    for source in ctrl_sources:
        assert len({p for row in source for p in row if p[3]}) <= 3, "Stencil exceeds 3 inks"
    # Required blank overlay regions in every key cap: no generated glyphs.
    for name, source in zip(names[2:], ctrl_sources[2:]):
        for row in source[4:12]:
            assert all(p == (0, 0, 0, 0) for p in row[4:-4]), (name, "key center not blank")
    for y in range(64):
        for x in range(96):
            if not (10 <= x <= 85 and 10 <= y <= 51) and not (76 <= x <= 81 and y == 56):
                assert images["boss_embector_idle.png"][y][x] == images["boss_embector_angry.png"][y][x], "Bezel registration"
    print("PASS 11 previews: exact opaque #0b1026 composite, 4x nearest-neighbor")
    print("PASS strip order / 2px gaps; 3-ink stencils / blank key centers")
    print("PASS cooldown ramp / identical alpha; boss bezel registration")
    print("PASS 24/24 PNGs: sizes, palette, binary alpha, <=16 colors, reserved-color restrictions")


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, OSError, ValueError) as error:
        print("FAIL", error)
        sys.exit(1)
