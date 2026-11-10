"""
Assemble the Blender frame renders (anim/<name>/####.png, 704x704, ground on row 660, pelvis on col 352)
into game-scale strips, GIFs and contact sheets. Needs Pillow (system Python, not Blender's).

  python postprocess.py [idle run attack]

Per animation:
  anim/<name>_512.gif         full render, 24 fps (over a flat grey background)
  anim/<name>_64_smooth.png   64x64 frames, 12 fps sampling, area downscale, soft alpha
  anim/<name>_64_hard.png     same, alpha thresholded at 50% (pixel-art-like edge)
  anim/<name>_64x4.gif        64 px frames upscaled 4x nearest, 12 fps; smooth on top, hard below
  anim/<name>_sheet.png       contact sheet of every 24 fps frame (for review)
  anim/compare_<name>.png     draft pixel strip (top) vs rig 64 hard strip (bottom), both 4x nearest
"""
import os
import sys
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RIG_DIR = os.path.dirname(HERE)
ANIM_DIR = os.path.join(RIG_DIR, "anim")
DRAFT_DIR = r"C:\Unity\Number\Assets\Resources\DraftSprites"

GROUND_ROW = 660.0     # render row of the standing sole
CENTER_COL = 352.0     # render column of the pelvis pivot
STAND_HEIGHT = 509.0   # standing height in render px
TARGET_HEIGHT = 59.0   # standing height in the 64x64 frame
FEET_ROW = 61          # lowest foot pixel row in the 64x64 frame
BG = (72, 74, 84, 255)
DRAFT_STRIP = {"idle": "hero_idle_4f.png", "run": "hero_run_6f.png", "attack": "hero_attack_5f.png"}
DRAFT_FPS_MS = 90      # ~11 fps, what the game plays draft strips at


def load_frames(name):
    d = os.path.join(ANIM_DIR, name)
    files = sorted(f for f in os.listdir(d) if f.endswith(".png"))
    return [Image.open(os.path.join(d, f)).convert("RGBA") for f in files]


def on_bg(im, bg=BG):
    out = Image.new("RGBA", im.size, bg)
    out.alpha_composite(im)
    return out.convert("RGB")


def to_64(frame, center_shift=0.0):
    """Area-downscale so the standing height is TARGET_HEIGHT, crop 64x64 with the sole on FEET_ROW."""
    s = TARGET_HEIGHT / STAND_HEIGHT
    win = 64.0 / s                              # source window size in render px
    top = GROUND_ROW - (FEET_ROW + 1) / s       # sole sits at the bottom edge of FEET_ROW
    left = CENTER_COL + center_shift - 32.0 / s
    box = (left, top, left + win, top + win)
    # premultiplied resize avoids dark fringes
    return frame.convert("RGBa").resize((64, 64), Image.BOX, box=box).convert("RGBA")


def hard_alpha(im, threshold=128):
    r, g, b, a = im.split()
    a = a.point(lambda v: 255 if v >= threshold else 0)
    out = Image.merge("RGBA", (r, g, b, a))
    return out


def strip(frames):
    w, h = frames[0].size
    out = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        out.paste(f, (i * w, 0))
    return out


def upscale(im, k):
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def contact_sheet(frames, cols, scale, label_h=14):
    w = int(frames[0].width * scale)
    h = int(frames[0].height * scale)
    rows = (len(frames) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * w, rows * h), (40, 40, 48))
    for i, f in enumerate(frames):
        small = on_bg(f).resize((w, h), Image.LANCZOS if scale < 1 else Image.NEAREST)
        x, y = (i % cols) * w, (i // cols) * h
        sheet.paste(small, (x, y))
        # frame marker: 1 px white tick per frame index along the top edge
        for t in range(i + 1):
            sheet.putpixel((x + 2 + t * 3, y + 2), (255, 255, 255))
            sheet.putpixel((x + 3 + t * 3, y + 2), (255, 255, 255))
    return sheet


def save_gif(frames_rgb, path, ms):
    frames_rgb[0].save(path, save_all=True, append_images=frames_rgb[1:], duration=ms, loop=0,
                       optimize=False, disposal=1)


def compare_image(name, hard_frames):
    draft = Image.open(os.path.join(DRAFT_DIR, DRAFT_STRIP[name])).convert("RGBA")
    rig = strip(hard_frames)
    w = max(draft.width, rig.width)
    out = Image.new("RGBA", (w, 64 * 2 + 4), BG)
    out.alpha_composite(draft, (0, 0))
    out.alpha_composite(rig, (0, 64 + 4))
    return upscale(out.convert("RGB"), 4)


def process(name, center_shift=0.0):
    frames = load_frames(name)
    print(f"{name}: {len(frames)} frames")
    # 24 fps GIF of the full render
    save_gif([on_bg(f) for f in frames], os.path.join(ANIM_DIR, f"{name}_512.gif"), 1000 // 24)
    # contact sheet of all frames
    contact_sheet(frames, cols=8, scale=0.35).save(os.path.join(ANIM_DIR, f"{name}_sheet.png"))
    # 12 fps sampling: every other frame starting at frame 1
    sampled = frames[::2]
    smooth = [to_64(f, center_shift) for f in sampled]
    hard = [hard_alpha(f) for f in smooth]
    strip(smooth).save(os.path.join(ANIM_DIR, f"{name}_64_smooth.png"))
    strip(hard).save(os.path.join(ANIM_DIR, f"{name}_64_hard.png"))
    gif_frames = []
    for s, h in zip(smooth, hard):
        both = Image.new("RGBA", (64, 128), BG)
        both.alpha_composite(s, (0, 0))
        both.alpha_composite(h, (0, 64))
        gif_frames.append(upscale(both.convert("RGB"), 4))
    save_gif(gif_frames, os.path.join(ANIM_DIR, f"{name}_64x4.gif"), 1000 // 12)
    compare_image(name, hard).save(os.path.join(ANIM_DIR, f"compare_{name}.png"))
    # review sheet of the 64 hard frames at 4x
    contact_sheet(hard, cols=len(hard), scale=4).save(os.path.join(ANIM_DIR, f"{name}_64_sheet.png"))


if __name__ == "__main__":
    names = sys.argv[1:] or ["idle", "run", "attack"]
    for n in names:
        process(n, center_shift=4.0 if n == "attack" else 0.0)
