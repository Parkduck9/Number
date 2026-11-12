"""Small deterministic RGBA raster for authoring; no resampling or antialiasing.

Used for pixel primitives and PNG authoring. Scene compositing uses Pillow.
"""
import struct
import zlib
from pathlib import Path


class Raster:
    def __init__(self, w, h, color=(0, 0, 0, 0)):
        self.width, self.height = w, h
        self.size = (w, h)
        self.data = bytearray(color) * (w * h)

    def get(self, x, y):
        k = 4 * (y * self.width + x)
        return tuple(self.data[k:k+4])

    def put(self, x, y, c):
        if 0 <= x < self.width and 0 <= y < self.height:
            k = 4 * (y * self.width + x)
            self.data[k:k+4] = bytes(c if len(c) == 4 else (*c, 255))

    def rect(self, box, c):
        x0, y0, x1, y1 = box
        for y in range(max(0, y0), min(self.height, y1+1)):
            for x in range(max(0, x0), min(self.width, x1+1)):
                self.put(x, y, c)

    def line(self, points, c, width=1):
        for (x0, y0), (x1, y1) in zip(points, points[1:]):
            dx, dy = abs(x1-x0), -abs(y1-y0)
            sx, sy = 1 if x0 < x1 else -1, 1 if y0 < y1 else -1
            err = dx+dy
            while True:
                lo, hi = (width-1)//2, width//2
                self.rect((x0-lo, y0-lo, x0+hi, y0+hi), c)
                if x0 == x1 and y0 == y1:
                    break
                e = 2*err
                if e >= dy:
                    err += dy
                    x0 += sx
                if e <= dx:
                    err += dx
                    y0 += sy

    def poly(self, pts, c):
        for y in range(max(0, min(p[1] for p in pts)), min(self.height, max(p[1] for p in pts)+1)):
            xs = []
            for (x0, y0), (x1, y1) in zip(pts, pts[1:]+pts[:1]):
                if min(y0, y1) <= y < max(y0, y1):
                    xs.append(x0+(y-y0)*(x1-x0)/(y1-y0))
            xs.sort()
            for a, b in zip(xs[::2], xs[1::2]):
                self.rect((int(a+0.999), y, int(b), y), c)
        self.line(pts+pts[:1], c)

    def paste(self, src, xy, mask=True):
        ox, oy = xy
        for y in range(src.height):
            for x in range(src.width):
                c = src.get(x, y)
                if c[3] or not mask:
                    self.put(x+ox, y+oy, c)

    def crop(self, box):
        x0, y0, x1, y1 = box
        out = Raster(x1-x0, y1-y0)
        for y in range(out.height):
            for x in range(out.width):
                out.put(x, y, self.get(x+x0, y+y0))
        return out

    def scale(self, n):
        out = Raster(self.width*n, self.height*n)
        for y in range(out.height):
            for x in range(out.width):
                out.put(x, y, self.get(x//n, y//n))
        return out

    def copy(self):
        out = Raster(*self.size)
        out.data[:] = self.data
        return out

    def dim(self, factor):
        out = self.copy()
        for k in range(0, len(out.data), 4):
            for ch in range(3):
                out.data[k+ch] = round(out.data[k+ch]*factor)
        return out

    def mirror(self):
        out = Raster(*self.size)
        for y in range(self.height):
            for x in range(self.width):
                out.put(x, y, self.get(self.width-1-x, y))
        return out

    def save(self, path):
        def chunk(tag, payload):
            return struct.pack('>I', len(payload))+tag+payload+struct.pack('>I', zlib.crc32(tag+payload))
        scan = b''.join(b'\0'+self.data[y*self.width*4:(y+1)*self.width*4] for y in range(self.height))
        Path(path).write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR', struct.pack('>IIBBBBB', self.width, self.height, 8, 6, 0, 0, 0))+chunk(b'IDAT', zlib.compress(scan, 9))+chunk(b'IEND', b''))

    @classmethod
    def open(cls, path):
        raw = Path(path).read_bytes()
        pos, packed = 8, b''
        while pos < len(raw):
            n = struct.unpack('>I', raw[pos:pos+4])[0]
            tag, content = raw[pos+4:pos+8], raw[pos+8:pos+8+n]
            if tag == b'IHDR':
                w, h, depth, mode, _, _, interlace = struct.unpack('>IIBBBBB', content)
                assert depth == 8 and mode in (2, 6) and interlace == 0
            elif tag == b'IDAT':
                packed += content
            pos += n+12
        scan = zlib.decompress(packed)
        bpp = 4 if mode == 6 else 3
        stride = w*bpp
        prev, ptr = bytearray(stride), 0
        out = cls(w, h)
        for y in range(h):
            filt = scan[ptr]
            ptr += 1
            row = bytearray(scan[ptr:ptr+stride])
            ptr += stride
            for k in range(stride):
                a, b, c = row[k-bpp] if k >= bpp else 0, prev[k], prev[k-bpp] if k >= bpp else 0
                if filt == 1: v = a
                elif filt == 2: v = b
                elif filt == 3: v = (a+b)//2
                elif filt == 4:
                    p = a+b-c
                    distances = (abs(p-a), abs(p-b), abs(p-c))
                    v = (a, b, c)[distances.index(min(distances))]
                else:
                    assert filt == 0
                    v = 0
                row[k] = (row[k]+v) & 255
            for x in range(w):
                c = tuple(row[x*bpp:(x+1)*bpp])
                out.put(x, y, c if bpp == 4 else (*c, 255))
            prev = row
        return out
