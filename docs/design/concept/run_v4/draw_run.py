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
# Minimal integer corrections, each coordinate within the requested one-pixel
# tolerance. These reconcile bone length / forward knee bend with the table.
LEG_POSES=[
 (((32,41),(38,47),(42,53),'flat',61,38),((30,42),(26,49),(21,54),'angle',59,None)),
 (((32,43),(36,49),(34,55),'flat',61,30),((30,44),(24,49),(19,51),'angle',56,None)),
 (((32,42),(32,49),(29,55),'flat',61,25),LEG_TABLE[2][1]),
 (((32,38),(28,45),(23,51),'angle',58,None),LEG_TABLE[3][1]),
]
ARM_POSES=list(ARM_TABLE)
ARM_POSES[1]=((29,32),(24,37))
ARM_POSES[2]=((32,33),(34,38))
ARM_POSES[4]=((39,30),(46,27))
ARM_POSES[5]=((37,32),(43,32))
ARM_POSES[6]=((33,33),(35,40))
ARM_POSES[7]=((30,33),(24,36))


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


def make_shoe(spec,source):
    h,k,a,kind,bottom,left=spec
    ax,ay=a
    if kind=='flat':
        left=ax-4 if left is None else left
        rows=source['flat']
        top=bottom-6
    else:
        left=ax-6
        # Use the unchanged angled cutout in flight. In grounded recovery,
        # remove two redundant interior rows, preserving the source sole.
        rows=source['angle']
        height=bottom-ay+1
        if height==6: rows=[rows[j] for j in (0,1,3,4,6,7)]
        assert len(rows)==height,(a,bottom,height)
        # Compact the back edge by three columns, retaining a hard dark edge.
        # This keeps the occupied contact-foot width at 32 pixels inclusive.
        rows=[list(row) for row in rows]
        for row in rows:
            if any(p[3] for p in row[:3]): row[3]=INK
            row[0]=row[1]=row[2]=CLEAR
        top=ay
    im=canvas(64,64)
    paste(im,rows,left,top)
    # Contact's ankle is up to 2 px above the original seven-row shoe.
    # Extend only its existing ankle socket, never the toe or white sole.
    for y in range(ay,top):
        for x in range(ax-1,ax+2): im[y][x]=DEEP if x==ax else INK
    return im


def leg_v4(i,near,shoes):
    spec=leg_pose(i,near)
    hip,knee,ankle,kind,bottom,left=spec
    thigh=tube(hip,knee,6)
    shin=tube(knee,ankle,4)
    joint=disk(knee,2)
    im=paint_mask(thigh|shin|joint,ROBE if near else DEEP)
    if near:
        for x,y in shin|joint: im[y][x]=DEEP
    # Short dark knee crease, rather than a tapered triangular trouser.
    kx,ky=knee
    for x,y in ((kx,ky),(kx+1,ky)):
        if im[y][x]!=INK: im[y][x]=DEEP if near else INK
    shoe=make_shoe(spec,shoes)
    if not near:
        shoe=[[{WHITE:GREY,SKINHI:SKIN,SKIN:SHADE,ROBE:DEEP}.get(p,p)
               for p in row] for row in shoe]
    paste(im,shoe,0,0)
    return im,shoe,spec


def arm_v4(i,near,hand):
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


def draw_v4(i,parts,shoes,hand):
    phase=i%4; bob=BOB[phase]
    head,torso,sash,hem,weapon=parts
    fararm,fh,fc,fj,fi=arm_v4(i,False,hand)
    neararm,nh,nc,nj,ni=arm_v4(i,True,hand)
    farleg,fs,flj=leg_v4(i,False,shoes)
    nearleg,ns,nlj=leg_v4(i,True,shoes)
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
            expected=[moved((33,26),dx,dy)]+[moved(p,dx,dy) for p in ARM_TABLE[j]]
            joints=d[name+'_arm_joints']
            for actual,ref in zip(joints,expected):
                assert max(abs(a-b) for a,b in zip(actual,ref))<=1,(i,name,'arm joint',actual,ref)
            assert 6<=math.dist(joints[0],joints[1])<=8,(i,name,'upper arm',joints)
            assert 5<=math.dist(joints[1],joints[2])<=8,(i,name,'forearm',joints)
            ref=leg_pose(i,near,True)
            joints=d[name+'_leg_joints']
            for actual,expected in zip(joints[:3],ref[:3]):
                assert max(abs(a-b) for a,b in zip(actual,expected))<=1,(i,name,'leg joint',actual,expected)
            h,k,a=joints[:3]
            assert 7<=math.dist(h,k)<=9,(i,name,'thigh',h,k)
            assert 5<=math.dist(k,a)<=8,(i,name,'shin',k,a)
            # Knee projects forward of the hip/ankle line: genuine bend.
            intercept=h[0]+(a[0]-h[0])*(k[1]-h[1])/(a[1]-h[1])
            assert k[0]>intercept,(i,name,'straight/backward knee',k,intercept)
            shoe=points(d[name+'_shoe'])
            assert min(y for x,y in shoe)>=46,(i,name,'high rear shoe')
            assert max(y for x,y in shoe)<=61
            # All cuff pixels are on the elbow-facing half of the hand.
            s,e,h=d[name+'_arm_joints']
            for x,y in d[name+'_cuff']:
                assert (x-h[0])*(e[0]-h[0])+(y-h[1])*(e[1]-h[1])>0
        # Far pose uses CURRENT frame bob, never the opposite frame's bob.
        _,_,_,opposite,_=arm_v4((i+4)%8,True,HAND)
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
        # Track hand visibility honestly. This is a visual diagnostic rather
        # than a waived geometric assertion; see the unresolved note for 2/6.
        visible=[]
        for name in ('near','far'):
            hand=d[name+'_hand']
            count=sum(f[y][x]==hand[y][x] and hand[y][x] in (SKINHI,SKIN,SHADE)
                      for x,y in points(hand))
            visible.append(count)
        measurements.append(dict(frame=i,bob=bob,foot_span=span,bottom=bottom,
                                 visible_skin_pixels=visible,
                                 near_arm=d['near_arm_joints'],far_arm=d['far_arm_joints'],
                                 near_leg=d['near_leg_joints'][:3],far_leg=d['far_leg_joints'][:3]))
    return measurements


def main():
    global HAND
    source_paths=[REF/name for name in ('hero_run_6f.png','hero_idle_4f.png','hero_attack_5f.png')]
    source_paths += [OUT.parent/'run_v3'/name for name in ('draw_run.py','notes.md','hero_run_8f.png')]
    source_paths += [OUT/'FABLE_SPEC.md']
    before={p:hashlib.sha256(p.read_bytes()).digest() for p in source_paths}
    # Read-only reuse of v3 cutout definitions / codecs copied above.
    run=read_png(REF/'hero_run_6f.png')
    parts=source_parts(run)
    shoes={'flat':crop(run,(40,55,52,62)), 'angle':crop(run,(15,54,26,62))}
    # Source fist narrowed from 6 to 5 columns, retaining both dark boundaries.
    source=crop(cut_rows(run,{25:(45,47),26:(44,48),27:(44,49),
                              28:(44,49),29:(44,48),30:(44,47)}),(44,25,50,31))
    HAND=[[INK if row[x]==DEEP else row[x] for x in (0,1,2,4,5)] for row in source]
    rendered=[draw_v4(i,parts,shoes,HAND) for i in range(8)]
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
    v3=read_png(OUT.parent/'run_v3/hero_run_8f.png')
    compare=canvas(512,222,BG)
    for row,(name,src,x) in enumerate([('OLD 6f',run,64),('V3',v3,0),('V4',strip,0)]):
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
    assert all(hashlib.sha256(p.read_bytes()).digest()==digest for p,digest in before.items())
    print(json.dumps(dict(numeric_checks='PASS',strip=[512,64],fps=20,
                         foot_spans=[d['foot_span'] for d in report],
                         frame_bottoms=[d['bottom'] for d in report],
                         hand_visibility=[d['visible_skin_pixels'] for d in report],
                         visual_status='INCOMPLETE: passing-frame far hands occluded (2, 6)',
                         source_hashes_unchanged=True),indent=2))
    # Do not turn the known visual failure into a passing asset check.
    failures=[d['frame'] for d in report if min(d['visible_skin_pixels'])<2]
    assert not failures,('visual acceptance failed: far fist occluded',failures)


if __name__=='__main__':
    main()


