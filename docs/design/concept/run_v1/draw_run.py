"""Approval sample only. Standard-library pixel drawing; never writes into Assets/."""
from pathlib import Path
import math
import struct
import zlib

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[3]
REF = ROOT / 'Assets/Resources/DraftSprites'
CLEAR = (0, 0, 0, 0)


def read_png(path):
    raw = path.read_bytes()
    pos, payload = 8, bytearray()
    while pos < len(raw):
        n = struct.unpack('>I', raw[pos:pos+4])[0]
        kind, data = raw[pos+4:pos+8], raw[pos+8:pos+8+n]
        if kind == b'IHDR':
            w, h, depth, mode, _, _, interlace = struct.unpack('>IIBBBBB', data)
        elif kind == b'IDAT':
            payload.extend(data)
        pos += n + 12
    assert depth == 8 and mode == 6 and interlace == 0
    data = zlib.decompress(payload)
    rows, prev, pos = [], bytearray(w*4), 0
    for _ in range(h):
        filt = data[pos]
        row = bytearray(data[pos+1:pos+1+w*4])
        pos += 1+w*4
        for x in range(w*4):
            a, b, c = row[x-4] if x >= 4 else 0, prev[x], prev[x-4] if x >= 4 else 0
            if filt == 1:
                row[x] = (row[x]+a) & 255
            elif filt == 2:
                row[x] = (row[x]+b) & 255
            elif filt == 3:
                row[x] = (row[x]+(a+b)//2) & 255
            elif filt == 4:
                p = a+b-c
                ds = (abs(p-a), abs(p-b), abs(p-c))
                row[x] = (row[x]+(a,b,c)[ds.index(min(ds))]) & 255
        rows.append([tuple(row[x:x+4]) for x in range(0,w*4,4)])
        prev = row
    return rows


def canvas(w, h, color=CLEAR):
    return [[color]*w for _ in range(h)]


def save_png(path, im):
    def chunk(tag, data):
        return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data)&0xffffffff)
    w,h = len(im[0]),len(im)
    data = b''.join(b'\0'+bytes(v for p in row for v in p) for row in im)
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(data,9))+chunk(b'IEND',b''))


def paste(dst, src, x, y):
    for sy,row in enumerate(src):
        for sx,c in enumerate(row):
            if c[3] and 0 <= x+sx < len(dst[0]) and 0 <= y+sy < len(dst):
                dst[y+sy][x+sx] = c


def scale(im, n):
    return [[p for p in row for _ in range(n)] for row in im for _ in range(n)]


def polygon(im, pts, c):
    # Scan integer pixel centres. All edges stay hard and all alpha stays binary.
    for y in range(max(0,min(p[1] for p in pts)),min(len(im)-1,max(p[1] for p in pts))+1):
        for x in range(max(0,min(p[0] for p in pts)),min(len(im[0])-1,max(p[0] for p in pts))+1):
            inside = False
            for i,(ax,ay) in enumerate(pts):
                bx,by = pts[i-1]
                if (ay > y+.5) != (by > y+.5) and x+.5 < (bx-ax)*(y+.5-ay)/(by-ay)+ax:
                    inside = not inside
            if inside:
                im[y][x] = c


def line(im, pts, c, width=1):
    for (ax,ay),(bx,by) in zip(pts,pts[1:]):
        count = max(abs(bx-ax),abs(by-ay),1)
        for i in range(count+1):
            x,y = round(ax+(bx-ax)*i/count),round(ay+(by-ay)*i/count)
            for dy in range(-(width//2),width-width//2):
                for dx in range(-(width//2),width-width//2):
                    if 0 <= x+dx < len(im[0]) and 0 <= y+dy < len(im):
                        im[y+dy][x+dx] = c


def save_gif(path, frames, palette):
    # Literal LZW groups with explicit clears keep the code width at 9 bits.
    # GIF delay is 5 centiseconds = 20 fps, with an infinite loop.
    w,h = len(frames[0][0]),len(frames[0])
    rgb = [c[:3] for c in palette]
    indices = {c:i for i,c in enumerate(palette)}
    table = bytes(v for c in rgb+[(0,0,0)]*(256-len(rgb)) for v in c)
    buf = bytearray(b'GIF89a'+struct.pack('<HHBBB',w,h,0xf7,0,0)+table)
    buf.extend(b'!\xff\x0bNETSCAPE2.0\x03\x01\x00\x00\x00')
    for im in frames:
        values = [indices[p] for row in im for p in row]
        codes = []
        for i in range(0,len(values),200):
            codes.extend([256]+values[i:i+200])
        codes.append(257)
        packed, bits, acc = bytearray(),0,0
        for code in codes:
            acc |= code << bits
            bits += 9
            while bits >= 8:
                packed.append(acc & 255)
                acc >>= 8
                bits -= 8
        if bits:
            packed.append(acc & 255)
        buf.extend(b'!\xf9\x04\x04\x05\x00\x00\x00')
        buf.extend(b','+struct.pack('<HHHHB',0,0,w,h,0)+b'\x08')
        for i in range(0,len(packed),255):
            block = packed[i:i+255]
            buf.extend(bytes([len(block)])+block)
        buf.extend(b'\0')
    buf.extend(b';')
    path.write_bytes(buf)


def color(h):
    return tuple(bytes.fromhex(h))+(255,)


INK = color('080b14')
DEEP = color('1a1d33')
ROBE = color('2d3050')
LIGHT = color('3f4a6e')
GREY = color('62677d')
WHITE = color('d6d7d0')
SKIN = color('d7b4a0')
SHADE = color('a37b70')
SKINHI = color('efd0b6')
GOLD = color('f2c94c')
OCHRE = color('9b7043')
RED = color('7f1a22')
REDHI = color('b94445')
BG = color('14121c')


def limb(im, hip, knee, ankle, near, shoe):
    # Narrow wrapped trousers leave negative space between both running legs.
    line(im,[hip,knee],INK,8)
    line(im,[knee,ankle],INK,6)
    line(im,[hip,knee],ROBE if near else DEEP,5)
    line(im,[knee,ankle],ROBE if near else DEEP,4)
    line(im,[(hip[0]-1,hip[1]),(knee[0]-1,knee[1])],LIGHT if near else ROBE,2)
    line(im,[(knee[0],knee[1]+1),(ankle[0],ankle[1]-2)],DEEP,3)
    ax,ay = ankle
    line(im,[(ax,ay-1),(ax,ay+1)],SKIN if near else SHADE,3)
    outline = [(ax-3,ay),(ax+2,ay),(ax+shoe[0],ay+shoe[1]),(ax+shoe[0],ay+shoe[1]+3),(ax-3,ay+3)]
    polygon(im,outline,INK)
    line(im,outline+[outline[0]],INK)
    line(im,[(ax-1,ay+1),(ax+shoe[0]-1,ay+shoe[1]+1)],WHITE if near else GREY,2)
    line(im,[(ax-2,ay+2),(ax+shoe[0]-1,ay+shoe[1]+2)],GREY if near else ROBE)


def arm(im, shoulder, elbow, hand, near):
    line(im,[shoulder,elbow,hand],INK,8)
    line(im,[shoulder,elbow,hand],ROBE if near else DEEP,5)
    line(im,[(shoulder[0]-1,shoulder[1]-1),(elbow[0]-1,elbow[1]-1)],LIGHT if near else ROBE,2)
    hx,hy = hand
    # White cuff and a compact closed fist, using the existing hand palette.
    ex,ey = elbow
    cx,cy = round(hx+(ex-hx)*.26),round(hy+(ey-hy)*.26)
    line(im,[(cx,cy),(hx,hy)],WHITE if near else GREY,4)
    polygon(im,[(hx-2,hy-3),(hx+2,hy-3),(hx+3,hy-1),(hx+2,hy+2),(hx-2,hy+2)],INK)
    line(im,[(hx-1,hy-2),(hx+1,hy-2),(hx+1,hy)],SKINHI if near else SKIN,2)
    line(im,[(hx-1,hy+1),(hx+1,hy+1)],SHADE)


# Contact, down, push/passing, flight. The second half swaps anatomical depth;
# the hero keeps facing right rather than mirroring the whole character.
BOB = [0,2,0,-3]
LEAN = [12,15,12,12]
LEGS = [
    # front knee/ankle and rear knee/ankle; heel first at y=61.
    (((41,49),(49,58),(5,-2)), ((21,49),(12,54),(5,-2))),
    (((34,53),(31,58),(6,0)), ((19,45),(16,49),(5,-2))),
    (((23,51),(15,56),(5,2)), ((38,44),(36,51),(6,0))),
    (((22,49),(16,44),(5,-2)), ((41,40),(42,48),(6,0))),
]
# Hand positions relative to an unbobbed body: 28 px pumping excursion.
ARMS = [
    (((35,30),(45,36),(47,29)), ((31,31),(19,36),(20,42))),
    (((35,30),(44,38),(44,33)), ((31,31),(20,35),(23,40))),
    (((35,30),(34,40),(30,37)), ((31,31),(29,40),(34,35))),
    (((35,30),(23,36),(19,31)), ((31,31),(43,36),(45,29))),
]


def draw_frame(i, reference):
    phase, swapped = i%4, i>=4
    bob = BOB[phase]
    im = canvas(64,64)
    def shifted(p):
        return p[0],p[1]+bob
    def poly(pts,c):
        polygon(im,[shifted(p) for p in pts],c)
    def stroke(pts,c,w=1):
        line(im,[shifted(p) for p in pts],c,w)

    # Sheathed accessory from the source silhouette, pointing behind the hip.
    stroke([(29,39),(13,51)],INK,4)
    stroke([(28,40),(13,51)],DEEP,2)
    stroke([(15,50),(12,52)],RED,2)
    stroke([(13,51),(12,52)],REDHI)

    front, rear = LEGS[phase]
    def leg(spec,near):
        knee,ankle,shoe = spec
        # Grounded feet do not inherit torso bob; flight feet already float.
        limb(im,shifted((30,42)),knee,ankle,near,shoe)
    leg(front if swapped else rear,False)

    forward,backward = ARMS[phase]
    far_arm = backward if swapped else forward
    arm(im,*(shifted(p) for p in far_arm),False)

    # Split, trailing robe hem. Tail shape uses the previous phase, cyclically.
    lag = (phase-1)%4
    tail_y = [46,48,46,43][lag]
    poly([(30,37),(34,42),(24,tail_y+2),(17,tail_y),(23,40)],INK)
    poly([(28,39),(31,42),(24,tail_y),(20,tail_y-1),(25,41)],ROBE)
    stroke([(25,42),(21,tail_y-1)],LIGHT)

    leg(rear if swapped else front,True)

    # The torso is drawn in a vertical local space and sheared exactly at its
    # integer rows. Hip stays at x=30; neck shifts forward 4-5 pixels.
    torso = canvas(64,64)
    polygon(torso,[(26,26),(34,26),(37,32),(35,39),(37,44),(29,46),(23,43),(25,35)],INK)
    polygon(torso,[(27,27),(33,27),(35,32),(33,39),(35,43),(29,44),(25,42),(27,34)],ROBE)
    polygon(torso,[(26,31),(29,30),(28,39),(26,42),(25,40)],DEEP)
    line(torso,[(28,28),(27,31),(30,34),(33,29)],WHITE,2)
    line(torso,[(29,32),(31,34),(31,37)],INK,2)
    line(torso,[(28,36),(31,34),(33,34)],LIGHT)
    line(torso,[(25,39),(34,39)],INK,3)
    line(torso,[(26,39),(33,39)],OCHRE)
    for y,row in enumerate(torso):
        dx = round((42-y)*math.tan(math.radians(LEAN[phase])))
        for x,c in enumerate(row):
            if c[3] and 0 <= x+dx < 64 and 0 <= y+bob < 64:
                im[y+bob][x+dx] = c

    # Source head/face/visor pixels are kept intact: no identity redraw.
    head_dx = 2 if phase != 1 else 3
    for y in range(3,27):
        for x in range(21,46):
            c = reference[y][x]
            if c[3]:
                im[y+bob][x+head_dx] = c

    # Belt knot and gold/red ribbons trail left, with the same one-frame lag.
    tx,ty = [(22,46),(20,43),(23,42),(24,47)][lag]
    stroke([(33,40),(tx,ty)],INK,4)
    stroke([(33,40),(tx,ty)],GOLD,2)
    stroke([(32,41),(tx+1,ty)],RED)
    stroke([(32,41),(tx+2,ty+4)],INK,3)
    stroke([(32,41),(tx+2,ty+3)],GOLD)
    stroke([(32,40),(33,40)],GOLD,2)

    near_arm = forward if swapped else backward
    arm(im,*(shifted(p) for p in near_arm),True)
    return im


def main():
    old = read_png(REF/'hero_run_6f.png')
    frames = [draw_frame(i,old) for i in range(8)]
    strip = canvas(512,64)
    for i,frame in enumerate(frames):
        paste(strip,frame,i*64,0)
    save_png(OUT/'hero_run_8f.png',strip)

    preview = []
    for frame in frames:
        stage = canvas(64,64,BG)
        line(stage,[(0,62),(63,62)],GREY)
        paste(stage,frame,0,0)
        preview.append(scale(stage,4))
    palette = sorted({p for frame in preview for row in frame for p in row})
    save_gif(OUT/'hero_run_8f_preview_x4.gif',preview,palette)

    # Each row is a frame strip at the same scale, centred on a 512 px canvas.
    comparison = canvas(512,152,BG)
    line(comparison,[(0,70),(511,70)],GREY)
    line(comparison,[(0,146),(511,146)],GREY)
    paste(comparison,old,64,8)
    paste(comparison,strip,0,84)
    save_png(OUT/'hero_run_compare_x4.png',scale(comparison,4))

    source_colors = {p for row in old for p in row if p[3]}
    new_colors = {p for row in strip for p in row if p[3]}
    assert new_colors <= source_colors, new_colors-source_colors
    assert {p[3] for row in strip for p in row} == {0,255}
    assert all(not any(p[3] for p in row) for frame in (frames[3],frames[7]) for row in frame[55:])
    assert all(any(p[3] for p in frame[61]) for frame in (frames[0],frames[1],frames[2],frames[4],frames[5],frames[6]))
    print('Verified: 512x64; 8 frames; 50 ms/frame; original palette only; binary alpha; flight frames clear below y=54; grounded soles y=61.')
    print('Frame bounds:',[(i,min(x for row in f for x,p in enumerate(row) if p[3]),max(y for y,row in enumerate(f) if any(p[3] for p in row))) for i,f in enumerate(frames)])


if __name__ == '__main__':
    main()
