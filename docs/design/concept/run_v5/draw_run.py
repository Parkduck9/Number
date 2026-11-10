"""Pixel cutout revision. Standard library only; writes beside this script only.
Sources are read-only. No interpolation, smoothing, or image-generation API.
"""
from pathlib import Path
import math
import struct
import zlib
import hashlib
import json
OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[3]
REF = ROOT / 'Assets/Resources/DraftSprites'
CLEAR = (0,0,0,0)

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


def color(s):
    return tuple(bytes.fromhex(s))+(255,)


def validate_gif(path,expected):
    # Decode the exported file independently of the writer, including LZW,
    # and compare every pixel against the exact staged 4x frame.
    data=path.read_bytes()
    assert data[:6]==b'GIF89a'
    w,h,flags=struct.unpack('<HHB',data[6:11])
    assert (w,h)==(256,256) and flags&128
    size=2**((flags&7)+1)
    colors=[tuple(data[13+3*j:16+3*j])+(255,) for j in range(size)]
    pos=13+3*size; delay=None; index=0
    def blocks(pos):
        payload=bytearray()
        while data[pos]:
            n=data[pos]; payload.extend(data[pos+1:pos+1+n]); pos+=n+1
        return bytes(payload),pos+1
    while data[pos]!=59:
        tag=data[pos]; pos+=1
        if tag==33:
            kind=data[pos]; pos+=1
            payload,pos=blocks(pos)
            if kind==249:
                delay=struct.unpack('<H',payload[1:3])[0]
            continue
        assert tag==44
        x,y,fw,fh,packed=struct.unpack('<HHHHB',data[pos:pos+9]); pos+=9
        assert (x,y,fw,fh,packed)==(0,0,w,h,0) and delay==5
        minimum=data[pos]; pos+=1
        payload,pos=blocks(pos)
        clear=1<<minimum; end=clear+1; width=minimum+1
        table={j:bytes([j]) for j in range(clear)}
        next_code=end+1; previous=None; bit=0; decoded=bytearray()
        while True:
            start=bit//8; shift=bit%8
            code=(int.from_bytes(payload[start:start+3],'little')>>shift)&((1<<width)-1)
            bit+=width
            if code==clear:
                table={j:bytes([j]) for j in range(clear)}
                next_code=end+1; width=minimum+1; previous=None
                continue
            if code==end: break
            entry=table.get(code)
            if entry is None:
                assert code==next_code and previous is not None
                entry=previous+previous[:1]
            decoded.extend(entry)
            if previous is not None:
                table[next_code]=previous+entry[:1]; next_code+=1
                if next_code==1<<width and width<12: width+=1
            previous=entry
        assert len(decoded)==w*h and index<len(expected)
        assert [colors[j] for j in decoded]==[p for row in expected[index] for p in row]
        index+=1; delay=None
    assert index==len(expected)==8 and pos==len(data)-1
INK=color('080b14'); DEEP=color('1a1d33'); ROBE=color('2d3050')
LIGHT=color('3f4a6e'); GREY=color('62677d'); WHITE=color('d6d7d0')
SKIN=color('d7b4a0'); SHADE=color('a37b70'); SKINHI=color('efd0b6')
GOLD=color('f2c94c'); RED=color('7f1a22'); BG=color('14121c')


def cut_rows(src, spans):
    out=canvas(64,64)
    for y,(a,b) in spans.items():
        for x in range(a,b+1):
            out[y][x]=src[y][x]
    return out


def crop(src, box):
    x0,y0,x1,y1=box
    return [r[x0:x1] for r in src[y0:y1]]


def transformed(src, dy=0, dx=0, shear=False, tail=0):
    out=canvas(64,64)
    for y,row in enumerate(src):
        shift=((38-y)*2)//9 if shear else 0
        shift-=max(0,y-38)*tail//6
        for x,c in enumerate(row):
            if c[3]:
                xx,yy=x+dx+shift,y+dy
                assert 0<=xx<64 and 0<=yy<64
                out[yy][xx]=c
    return out


def composite(*layers):
    out=canvas(64,64)
    for layer in layers:
        paste(out,layer,0,0)
    return out


def source_parts(run):
    # Hair tie included. Entire head is translated as ONE unchanged cutout.
    head=cut_rows(run,{y:(23,48) for y in range(4,22)})
    # Deliberate row masks isolate the original collar, lapel, belt and robe.
    bounds=[(31,39),(30,39),(29,40),(28,40),(28,40),(29,39),
            (29,39),(29,39),(30,39),(30,38),(30,38),(29,38),
            (28,39),(27,37)]
    torso=cut_rows(run,{y:span for y,span in zip(range(22,36),bounds)})
    # Original gold/red sash is an exact cut, including its dark contour.
    sash=cut_rows(run,{36:(31,37),37:(31,37),38:(30,37),39:(30,38),
                       40:(30,39),41:(29,40),42:(29,40),43:(29,40),44:(29,33)})
    # Short skirt panel is another source cut, not a generated polygon.
    hem=cut_rows(run,{35:(26,33),36:(27,31),37:(27,31),38:(27,30),
                     39:(26,30),40:(25,30),41:(24,29),42:(23,29),
                     43:(22,29),44:(23,29),45:(22,29),46:(21,28)})
    weapon_bounds={34:(19,23),35:(19,23),36:(18,22),37:(18,22),38:(18,22),
                   39:(19,23),40:(20,25),41:(20,24),42:(19,23),43:(18,22),
                   44:(17,21),45:(16,20),46:(15,18),47:(13,17),48:(13,17),49:(14,17)}
    weapon=cut_rows(run,weapon_bounds)
    return head,torso,sash,hem,weapon


def outlined_shape(polys, fill):
    mask=canvas(64,64)
    for pts in polys:
        polygon(mask,pts,fill)
    # One pixel outline, with no antialiasing or stroke-width ambiguity.
    out=canvas(64,64)
    for y in range(64):
        for x in range(64):
            if mask[y][x][3]:
                edge=any(not (0<=x+dx<64 and 0<=y+dy<64 and mask[y+dy][x+dx][3])
                         for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)))
                out[y][x]=INK if edge else fill
    return out


def shade_poly(im,pts,c):
    layer=canvas(64,64)
    polygon(layer,pts,c)
    for y,row in enumerate(layer):
        for x,p in enumerate(row):
            if p[3] and im[y][x][3] and im[y][x]!=INK:
                im[y][x]=c


def segment(a,b,wa,wb):
    vx,vy=b[0]-a[0],b[1]-a[1]
    length=math.hypot(vx,vy)
    nx,ny=-vy/length,vx/length
    return [(round(a[0]+nx*wa),round(a[1]+ny*wa)),
            (round(b[0]+nx*wb),round(b[1]+ny*wb)),
            (round(b[0]-nx*wb),round(b[1]-ny*wb)),
            (round(a[0]-nx*wa),round(a[1]-ny*wa))]


def label(im,text,x,y,c=GREY):
    # Tiny deterministic labels in the review sheets only.
    font={'0':['111','101','101','101','111'],'1':['010','110','010','010','111'],
          '2':['111','001','111','100','111'],'3':['111','001','111','001','111'],
          '4':['101','101','111','001','001'],'5':['111','100','111','001','111'],
          '6':['111','100','111','101','111'],'7':['111','001','010','010','010'],
          '8':['111','101','111','101','111'],'9':['111','101','111','001','111'],
          'V':['101','101','101','101','010'],'O':['111','101','101','101','111'],
          'f':['011','010','111','010','010'],
          'L':['100','100','100','100','111'],'D':['110','101','101','101','110']}
    for char in text:
        if char in font:
            for yy,row in enumerate(font[char]):
                for xx,p in enumerate(row):
                    if p=='1': im[y+yy][x+xx]=c
        x+=4


# Section 2, verbatim reference coordinates. Leg coordinates ALREADY include bob.
BOB=(0,2,1,-3)
LEG_TABLE=[
 (((32,41),(38,46),(42,54),'flat',61,38),((30,42),(25,48),(22,54),'angle',59,None)),
 (((32,43),(36,49),(35,55),'flat',61,30),((30,44),(24,48),(19,51),'angle',56,None)),
 (((32,42),(32,49),(30,55),'flat',61,25),((30,43),(35,48),(30,52),'angle',57,None)),
 (((32,38),(27,45),(23,51),'angle',58,None),((30,39),(38,41),(40,48),'flat',54,36)),
]
ARM_TABLE=[((28,32),(22,30)),((29,33),(24,37)),
           ((32,33),(34,39)),((36,33),(42,32)),
           ((39,31),(46,27)),((37,33),(43,32)),
           ((33,33),(35,39)),((30,33),(25,37))]
# Retain v4's integer joint corrections outside the passing swing leg.
# Passing knee +2x and ankle +4x/+2y expose that leg beyond its support mate.
LEG_POSES=[
 (((32,41),(38,47),(42,53),'flat',61,38),((30,42),(26,49),(21,54),'angle',59,None)),
 (((32,43),(36,49),(34,55),'flat',61,30),((30,44),(24,49),(19,51),'angle',56,None)),
 (((32,42),(32,49),(29,55),'flat',61,25),((30,43),(37,48),(34,54),'angle',57,None)),
 (((32,38),(28,45),(23,51),'angle',58,None),LEG_TABLE[3][1]),
]
# V5 prioritizes the requested +/-4 px reach and open front elbow.
# Those endpoints cannot fit two <=8 px bones from the fixed shoulder.
ARM_POSES=[((24,22),(18,28)),((23,28),(30,29)),
           ((31,35),(41,36)),((37,35),(47,32)),
           ((42,31),(50,25)),((34,36),(41,35)),
           ((24,30),(28,37)),((23,27),(21,34))]


def moved(p,dx,dy):
    return (p[0]+dx,p[1]+dy)


def leg_pose(i,near,reference=False):
    table=LEG_TABLE if reference else LEG_POSES
    phase=i%4
    p=table[phase][0 if near else 1] if i<4 else table[phase][1 if near else 0]
    if i<4:
        return p
    dx,dy=(2,-1) if near else (-2,1)
    h,k,a,kind,bottom,left=p
    # The literal +1 shoe shift would put soles at 62 and flight at 59.
    # Preserve hip/knee exchange. Keep ankle corrections within one pixel
    # of the reference and retain ground=61 / flight clearance at the sole.
    ankle_dy=dy if near or reference or phase==0 else 0
    bottom_dy=dy if near or reference else 0
    return (moved(h,dx,dy),moved(k,dx,dy),moved(a,dx,ankle_dy),kind,
            bottom+bottom_dy,None if left is None else left+dx)


def points(im):
    return {(x,y) for y,row in enumerate(im) for x,p in enumerate(row) if p[3]}


def dilated(mask):
    return {(x+dx,y+dy) for x,y in mask for dx,dy in
            ((0,0),(1,0),(-1,0),(0,1),(0,-1))
            if 0<=x+dx<64 and 0<=y+dy<64}


def tube(a,b,width):
    # Oriented constant-width rectangle. Pixel centres, no antialiasing.
    vx,vy=b[0]-a[0],b[1]-a[1]
    length=math.hypot(vx,vy); ux,uy=vx/length,vy/length
    return {(x,y) for y in range(64) for x in range(64)
            if -.5 <= (x-a[0])*ux+(y-a[1])*uy <= length+.5
            and -width/2 <= -(x-a[0])*uy+(y-a[1])*ux < width/2}


def disk(p,r):
    return {(x,y) for y in range(64) for x in range(64)
            if math.hypot(x-p[0],y-p[1])<=r}


def paint_mask(mask,fill):
    im=canvas(64,64)
    for x,y in dilated(mask): im[y][x]=INK
    for x,y in mask: im[y][x]=fill
    return im


def shoe_phase(i,near):
    # Opposing feet exchange roles after four cells, as do the v4 legs.
    support=near == (i<4)
    return (('strike','flat','off','relax') if support else
            ('off','recover','recover','reach'))[i%4]


def make_shoe(spec,style):
    h,k,a,kind,bottom,left=spec
    ax,ay=a
    if style=='off' and bottom>=58:
        bottom=61
    # Hand-authored native-grid outlines and sole paths. x=0 is heel,
    # x=3 is ankle: attachment is above the rear third, never shoe centre.
    # Upper includes a continuous narrow ankle, not a pasted cutout socket.
    outlines={
      'strike':[(1,-6),(4,-6),(5,-5),(8,-6),(10,-5),(10,-3),(3,0),(0,0),(0,-3)],
      'flat':[(1,-6),(4,-6),(5,-4),(9,-3),(11,-2),(11,0),(0,0),(0,-4)],
      'off':[(0,-6),(3,-7),(5,-5),(6,-3),(10,-2),(11,0),(8,0),(4,-2),(1,-3)],
      'recover':[(0,-6),(3,-6),(5,-4),(6,-2),(9,-1),(10,0),(7,0),(3,-2),(0,-3)],
      'relax':[(1,-6),(4,-6),(5,-4),(6,-2),(9,-1),(10,0),(7,0),(3,-2),(0,-3)],
      'reach':[(1,-6),(4,-6),(5,-4),(8,-3),(10,-1),(10,0),(8,0),(1,-2),(0,-4)]}
    soles={
      'strike':[(1,-1),(3,-1),(9,-4)],
      'flat':[(1,-1),(10,-1)],
      'off':[(2,-4),(5,-3),(8,-1),(10,-1)],
      'recover':[(1,-4),(4,-3),(8,-1)],
      'relax':[(1,-4),(4,-3),(8,-1)],
      'reach':[(1,-3),(4,-2),(9,-1)]}
    ox=ax-3
    # Rasterize the explicit shape, then preserve its one-pixel border.
    im=outlined_shape([[ (ox+x,bottom+y) for x,y in outlines[style] ]],DEEP)
    # Integer sole staircases; no resampling, no extra colours.
    line(im,[(ox+x,bottom+y) for x,y in soles[style]],WHITE)
    # Grey bevel above the white sole gives thickness without a flat slab.
    for x,y in list(points(im)):
        if im[y][x]==WHITE and y>0 and im[y-1][x]==DEEP:
            im[y-1][x]=GREY
    # Three-pixel interior ankle (one narrower than four-pixel shin),
    # blended into the rear of the upper before outlining the union.
    socket={(x,y) for y in range(ay,bottom-3)
            for x in range(ax-1,ax+2)}
    filled={(x,y) for x,y in points(im) if im[y][x]!=INK}|socket
    continuous=paint_mask(filled,DEEP)
    paste(continuous,im,0,0)
    im=continuous
    return im


def leg_v5(i,near):
    spec=leg_pose(i,near)
    hip,knee,ankle,kind,bottom,left=spec
    thigh=tube(hip,knee,6)
    length=math.dist(knee,ankle)
    neck=tuple(round(ankle[j]+(knee[j]-ankle[j])*2/length) for j in (0,1))
    shin=tube(knee,neck,4)|tube(neck,ankle,3)
    joint=disk(knee,2)
    im=paint_mask(thigh|shin|joint,ROBE if near else DEEP)
    if near:
        for x,y in shin|joint: im[y][x]=DEEP
    # Short dark knee crease, rather than a tapered triangular trouser.
    kx,ky=knee
    for x,y in ((kx,ky),(kx+1,ky)):
        if im[y][x]!=INK: im[y][x]=DEEP if near else INK
    shoe=make_shoe(spec,shoe_phase(i,near))
    if not near:
        shoe=[[{WHITE:GREY,GREY:LIGHT,DEEP:INK,SKINHI:SKIN,SKIN:SHADE,ROBE:DEEP}.get(p,p)
               for p in row] for row in shoe]
    paste(im,shoe,0,0)
    return im,shoe,spec


def arm_v5(i,near,hand):
    phase=i%4; bob=BOB[phase]
    j=i if near else (i+4)%8
    dx,dy=(0,bob) if near else (-2,bob+1)
    shoulder=moved((33,26),dx,dy)
    elbow,fist=(moved(p,dx,dy) for p in ARM_POSES[j])
    upper=tube(shoulder,elbow,5)
    fore=tube(elbow,fist,4)
    interior=upper|fore|disk(elbow,2)
    sx,sy=shoulder
    # Seven pixels including outline; five-pixel deltoid interior for 3 rows.
    interior|={(x,y) for y in range(sy,sy+3) for x in range(sx-2,sx+3)}
    im=paint_mask(interior,ROBE if near else DEEP)
    if near:
        for segment_mask in (upper,fore):
            for x,y in segment_mask:
                if (x,y-1) not in segment_mask and im[y][x]==ROBE:
                    im[y][x]=LIGHT
    hx,hy=fist
    vx,vy=hx-elbow[0],hy-elbow[1]
    length=math.hypot(vx,vy); ux,uy=vx/length,vy/length
    # Intersection of the elbow-facing ray and the 5x6 fist rectangle.
    reach=min(2.5/abs(ux) if ux else 1e6,3/abs(uy) if uy else 1e6)
    cuff={(x,y) for y in range(64) for x in range(64)
          if reach <= -((x-hx)*ux+(y-hy)*uy) < reach+2
          and -2.5 <= -(x-hx)*uy+(y-hy)*ux < 2.5}
    for x,y in dilated(cuff): im[y][x]=INK
    for x,y in cuff: im[y][x]=WHITE if near and x>=hx else GREY
    cluster=hand
    if not near:
        cluster=[[{SKINHI:SKIN,SKIN:SHADE}.get(p,p) for p in row] for row in hand]
    hand_layer=canvas(64,64)
    paste(hand_layer,cluster,hx-2,hy-2)
    paste(im,hand_layer,0,0)
    return im,hand_layer,cuff,(shoulder,elbow,fist),interior


def separate_under(back,front):
    # Only darken BACK pixels adjacent to a foreground limb/torso. No changes
    # to anchors, no widening, and no clearing anatomical connections.
    # A foreground INK contour already supplies the one-pixel gap. Add a
    # back-side gap only where a reused cutout has an unoutlined cloth edge.
    edge=dilated({(x,y) for y,row in enumerate(front) for x,p in enumerate(row)
                  if p[3] and p!=INK})
    return [[INK if p in (ROBE,DEEP,LIGHT) and (x,y) in edge else p
             for x,p in enumerate(row)] for y,row in enumerate(back)]


def draw_v5(i,parts,hand):
    phase=i%4; bob=BOB[phase]
    head,torso,sash,hem,weapon=parts
    fararm,fh,fc,fj,fi=arm_v5(i,False,hand)
    neararm,nh,nc,nj,ni=arm_v5(i,True,hand)
    farleg,fs,flj=leg_v5(i,False)
    nearleg,ns,nlj=leg_v5(i,True)
    lag=(phase-1)%4
    tx,ty=((-3,0),(-5,1),(-4,-1),(-2,1))[lag]
    tail=transformed(hem,bob+ty,tx)
    core=transformed(torso,bob,0,True)
    ribbons=transformed(sash,bob,0,tail=(3,4,5,4)[lag])
    face=transformed(head,bob,4)
    collar=cut_rows(core,{y+bob:(33+((38-y)*2)//9,39+((38-y)*2)//9)
                          for y in range(22,27)})
    # Required torso contact shadow immediately below/rear of near sleeve.
    sleeve=points(neararm)
    shadow={(x+dx,y+dy) for x,y in sleeve for dx,dy in ((-1,0),(0,1))}
    for x,y in shadow-sleeve:
        if 0<=x<64 and 0<=y<64 and core[y][x][3] and core[y][x]!=INK:
            core[y][x]=DEEP
    fararm=separate_under(fararm,composite(core,neararm))
    farleg=separate_under(farleg,composite(tail,nearleg,core))
    # Sash occupies its specified layer, with a fist-shaped occlusion mask.
    for x,y in points(nh): ribbons[y][x]=CLEAR
    accessory=transformed(weapon,bob)
    frame=composite(accessory,fararm,farleg,tail,nearleg,core,neararm,
                    collar,ribbons,face)
    return frame,dict(near_arm=neararm,far_arm=fararm,near_hand=nh,far_hand=fh,
                     near_cuff=nc,far_cuff=fc,near_arm_joints=nj,far_arm_joints=fj,
                     near_leg=nearleg,far_leg=farleg,near_shoe=ns,far_shoe=fs,
                     near_leg_joints=nlj,far_leg_joints=flj,head=face,
                     collar=collar,sash=ribbons)


def validate(frames,details,palette):
    measurements=[]
    # Longer arc: monotonic horizontal travel in each half-cycle, without
    # a held endpoint followed by a long jump. Measure BEFORE bob.
    hands=[p[1] for p in ARM_POSES]
    steps=[math.dist(hands[i],hands[(i+1)%8]) for i in range(8)]
    assert all(6<=d<=14 for d in steps) and max(steps)/min(steps)<=2.1,steps
    assert all(hands[j][0]<hands[j+1][0] for j in range(4))
    assert all(hands[j][0]>hands[(j+1)%8][0] for j in range(4,8))
    assert hands[0][0]==ARM_TABLE[0][1][0]-4
    assert hands[4]==(ARM_TABLE[4][1][0]+4,ARM_TABLE[4][1][1]-2)
    e,h=ARM_POSES[4]; s=(33,26)
    cosine=sum((s[k]-e[k])*(h[k]-e[k]) for k in (0,1))/(math.dist(s,e)*math.dist(e,h))
    assert 110<=math.degrees(math.acos(cosine))<=130
    for i,(f,d) in enumerate(zip(frames,details)):
        bob=BOB[i%4]
        assert {p[3] for row in f for p in row}<={0,255}
        assert {p for row in f for p in row if p[3]}<=palette
        assert d['near_arm_joints'][0]==(33,26+bob),'shoulder fixed'
        assert d['far_arm_joints'][0]==(31,27+bob),'far shoulder fixed'
        for near in (True,False):
            name='near' if near else 'far'
            j=i if near else (i+4)%8
            dx,dy=(0,bob) if near else (-2,bob+1)
            expected=[moved((33,26),dx,dy)]+[moved(p,dx,dy) for p in ARM_POSES[j]]
            joints=d[name+'_arm_joints']
            for actual,ref in zip(joints,expected):
                assert max(abs(a-b) for a,b in zip(actual,ref))<=1,(i,name,'arm joint',actual,ref)
            assert 9<=math.dist(joints[0],joints[1])<=10.5,(i,name,'upper arm',joints)
            assert 7<=math.dist(joints[1],joints[2])<=10.5,(i,name,'forearm',joints)
            ref=leg_pose(i,near,True)
            joints=d[name+'_leg_joints']
            for actual,expected in zip(joints[:2],ref[:2]):
                tolerance=2 if i%4==2 and near==(i>=4) else 1
                assert max(abs(a-b) for a,b in zip(actual,expected))<=tolerance,(i,name,'leg joint',actual,expected)
            h,k,a=joints[:3]
            assert 7<=math.dist(h,k)<=9,(i,name,'thigh',h,k)
            assert 5<=math.dist(k,a)<=8,(i,name,'shin',k,a)
            # Knee projects forward of the hip/ankle line: genuine bend.
            intercept=h[0]+(a[0]-h[0])*(k[1]-h[1])/(a[1]-h[1])
            assert k[0]>intercept,(i,name,'straight/backward knee',k,intercept)
            shoe=points(d[name+'_shoe'])
            assert min(y for x,y in shoe)>=46,(i,name,'high rear shoe')
            assert max(y for x,y in shoe)<=61
            style=shoe_phase(i,near)
            # Verify rendered heel/toe heights, not merely the phase label.
            ax,ay=a
            heel=[y for x,y in shoe if x<=ax]
            toe=[y for x,y in shoe if x>=ax+4]
            if style=='strike':
                assert max(heel)>=max(toe)+2,(i,name,'toe must lift at heel strike')
            elif style=='flat':
                assert max(heel)==max(toe)==61,(i,name,'flat contact')
            elif style in ('off','recover','relax','reach'):
                assert max(toe)>max(heel),(i,name,'toe must be below heel',style)
            if style=='off':
                assert max(toe)==61,(i,name,'toe-off contact row')
            # Shoe remains attached to the leg and is one 4-connected shape.
            assert shoe & tube(k,a,3),(i,name,'shoe detached from shin')
            reached={next(iter(shoe))}
            while True:
                grown=dilated(reached)&shoe
                if grown==reached: break
                reached=grown
            assert reached==shoe,(i,name,'disconnected shoe pixels')
            # All cuff pixels are on the elbow-facing half of the hand.
            s,e,h=d[name+'_arm_joints']
            for x,y in d[name+'_cuff']:
                assert (x-h[0])*(e[0]-h[0])+(y-h[1])*(e[1]-h[1])>0
        # Far pose uses CURRENT frame bob, never the opposite frame's bob.
        _,_,_,opposite,_=arm_v5((i+4)%8,True,HAND)
        shift=(-2,1+bob-BOB[(i+4)%4])
        assert d['far_arm_joints']==tuple(moved(p,*shift) for p in opposite)
        shoes=points(d['near_shoe'])|points(d['far_shoe'])
        span=max(x for x,y in shoes)-min(x for x,y in shoes)+1
        assert span<=32,(i,'foot span',span)
        bottom=max(y for x,y in points(f))
        if i%4==3: assert 54<=bottom<=58,(i,'flight bottom',bottom)
        else: assert bottom==61,(i,'ground bottom',bottom)
        # Every original head pixel survives, translated only.
        assert len(points(d['head']))==278
        for x,y in points(d['head']): assert f[y][x]==d['head'][y][x]
        for x,y in points(d['collar']):
            assert f[y][x]==d['collar'][y][x],(i,'collar overwritten',x,y)
        for x,y in points(d['sash']):
            assert f[y][x]==d['sash'][y][x],(i,'sash overwritten',x,y)
        for x,y in points(d['near_hand']):
            assert f[y][x]==d['near_hand'][y][x],(i,'sash over near fist',x,y)
        # Require visible fists in the final composite, including 2/6.
        visible=[]
        for name in ('near','far'):
            hand=d[name+'_hand']
            count=sum(f[y][x]==hand[y][x] and hand[y][x] in (SKINHI,SKIN,SHADE)
                      for x,y in points(hand))
            visible.append(count)
        assert min(visible)>=4,(i,'hidden fist',visible)
        if i in (2,6):
            # Near fist never sits on the thigh, even at its outline.
            h,k,a=d['near_leg_joints'][:3]
            assert not (points(d['near_hand']) & dilated(tube(h,k,6))), (i,'fist on thigh')
            # Compare the two leg layers before torso/hem occlusion.
            # Their visible cloth interiors must have an INK boundary.
            front=d['near_leg']; back=d['far_leg']
            nearcloth={(x,y) for x,y in points(front) if front[y][x] in (ROBE,DEEP)}
            farcloth={(x,y) for x,y in points(back) if back[y][x]==DEEP and not front[y][x][3]}
            assert len(farcloth)>=8,(i,'far thigh hidden')
            assert not (dilated(nearcloth)&farcloth),(i,'missing INK separation')
            assert d['near_leg_joints'][2][0] != d['far_leg_joints'][2][0]
        measurements.append(dict(frame=i,bob=bob,foot_span=span,bottom=bottom,
                                 visible_skin_pixels=visible,
                                 near_arm=d['near_arm_joints'],far_arm=d['far_arm_joints'],
                                 near_leg=d['near_leg_joints'][:3],far_leg=d['far_leg_joints'][:3]))
    return measurements


def main():
    global HAND
    source_paths=[REF/name for name in ('hero_run_6f.png','hero_idle_4f.png','hero_attack_5f.png')]
    source_paths += [OUT.parent/'run_v4'/name for name in
                     ('draw_run.py','notes.md','FABLE_SPEC.md','hero_run_8f.png')]
    before={p:hashlib.sha256(p.read_bytes()).digest() for p in source_paths}
    # Read-only reuse of v3 cutout definitions / codecs copied above.
    run=read_png(REF/'hero_run_6f.png')
    parts=source_parts(run)
    # Source fist narrowed from 6 to 5 columns, retaining both dark boundaries.
    source=crop(cut_rows(run,{25:(45,47),26:(44,48),27:(44,49),
                              28:(44,49),29:(44,48),30:(44,47)}),(44,25,50,31))
    HAND=[[INK if row[x]==DEEP else row[x] for x in (0,1,2,4,5)] for row in source]
    rendered=[draw_v5(i,parts,HAND) for i in range(8)]
    frames,details=map(list,zip(*rendered))
    strip=canvas(512,64)
    for i,f in enumerate(frames):paste(strip,f,64*i,0)
    save_png(OUT/'hero_run_8f.png',strip)
    preview=[]
    for f in frames:
        stage=canvas(64,64,BG)
        line(stage,[(0,62),(63,62)],GREY)
        paste(stage,f,0,0)
        preview.append(scale(stage,4))
    save_gif(OUT/'hero_run_8f_preview_x4.gif',preview,
             sorted({p for f in preview for row in f for p in row}))
    v4=read_png(OUT.parent/'run_v4/hero_run_8f.png')
    compare=canvas(512,222,BG)
    for row,(name,src,x) in enumerate([('OLD 6f',run,64),('V4',v4,0),('V5',strip,0)]):
        top=row*74
        label(compare,name,2,top+2)
        line(compare,[(0,top+70),(511,top+70)],GREY)
        paste(compare,src,x,top+8)
    save_png(OUT/'hero_run_compare_x4.png',scale(compare,4))
    review=canvas(64*4,70*2,BG)
    for i,f in enumerate(frames):
        x,y=(i%4)*64,(i//4)*70
        label(review,str(i),x+2,y+1)
        line(review,[(x,y+68),(x+63,y+68)],GREY)
        paste(review,f,x,y+6)
    save_png(OUT/'hero_run_limbs_x8.png',scale(review,8))
    source_colors={p for name in ('hero_run_6f.png','hero_idle_4f.png','hero_attack_5f.png')
                   for row in read_png(REF/name) for p in row if p[3]}
    assert len(source_colors)==16
    assert {p[3] for row in strip for p in row}=={0,255}
    report=validate(frames,details,source_colors)
    assert read_png(OUT/'hero_run_8f.png')==strip
    gif=(OUT/'hero_run_8f_preview_x4.gif').read_bytes()
    assert gif[:6]==b'GIF89a' and struct.unpack('<HH',gif[6:10])==(256,256)
    assert gif.count(b'!\xf9\x04\x04\x05\x00\x00\x00')==8
    validate_gif(OUT/'hero_run_8f_preview_x4.gif',preview)
    assert all(hashlib.sha256(p.read_bytes()).digest()==digest for p,digest in before.items())
    print(json.dumps(dict(numeric_checks='PASS',strip=[512,64],fps=20,
                         foot_spans=[d['foot_span'] for d in report],
                         frame_bottoms=[d['bottom'] for d in report],
                         hand_visibility=[d['visible_skin_pixels'] for d in report],
                         visual_status='Inspect comparison at 4x and limbs at 8x',
                         source_hashes_unchanged=True),indent=2))


if __name__=='__main__':
    main()


