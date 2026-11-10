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
        shift=(38-y)//6 if shear else 0
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


# Shoe pixels are copied whole from run 0: white/grey soles are never redrawn.
# (hip, knee, ankle, shoe type, shoe offset). Ankles join the source shoe top.
LEGS=[
 (((33,41),(42,49),(47,56),'flat',(6,0)), ((29,42),(22,49),(13,54),'angle',(-4,-2))),
 (((33,43),(37,51),(33,56),'flat',(-8,0)), ((29,43),(21,47),(18,49),'angle',(1,-7))),
 (((30,42),(24,51),(19,56),'angle',(2,0)), ((34,42),(41,43),(40,50),'flat',(-1,-6))),
 (((34,39),(42,40),(40,49),'flat',(-1,-7)), ((29,39),(23,47),(18,43),'angle',(1,-13))),
]
BOB=(0,2,0,-3)


def leg(spec,near,shoes):
    hip,knee,ankle,kind,offset=spec
    fill=ROBE if near else DEEP
    im=outlined_shape([segment(hip,knee,4.5,4),segment(knee,ankle,4,2.5)],fill)
    hx,hy=hip; kx,ky=knee; ax,ay=ankle
    # Broad fabric fold and darker lower trousers, matching the source's clusters.
    shade_poly(im,[(hx-2,hy),(kx-2,ky),(kx-1,ky-1),(hx-1,hy)],ROBE if near else DEEP)
    if near:
        shade_poly(im,segment(knee,ankle,3,2),DEEP)
    shade_poly(im,[(kx,ky),(ax+2,ay),(ax,ay),(kx-1,ky-1)],DEEP if near else INK)
    # One pixel joint creases keep knees and the robe/pants boundary distinct.
    crease=canvas(64,64)
    line(crease,[(kx-1,ky),(kx+1,ky+1)],DEEP if near else INK)
    line(crease,[(hx-2,hy+3),(hx+1,hy+4)],INK)
    for y,row in enumerate(crease):
        for x,c in enumerate(row):
            if c[3] and im[y][x][3] and im[y][x]!=INK: im[y][x]=c
    # Exact original shoe cutout, moved only by integer translation.
    paste(im,shoes[kind],*offset)
    return im


# Sleeve, cuff and fist clusters are traced directly from run frame 0.
# Source cutouts preserve the visibly bent elbows and clustered hand pixels.
def source_arms(run):
    back_bounds={24:(29,34),25:(27,34),26:(25,34),27:(23,33),28:(22,32),
                 29:(21,31),30:(21,29),31:(22,29),32:(22,29),33:(23,28),
                 34:(24,26),35:(24,26),36:(23,26),37:(23,27),38:(23,26),39:(24,26)}
    front_bounds={25:(45,47),26:(44,48),27:(40,49),28:(39,49),29:(39,49),
                  30:(39,48),31:(38,46),32:(38,45),33:(38,44),34:(39,43),35:(40,42)}
    back=cut_rows(run,back_bounds)
    front=cut_rows(run,front_bounds)
    # Run frame 2 provides the clean passing sleeve, narrow bent cuff and fist.
    passing=cut_rows(crop(run,(128,0,192,64)),
                     {y:(27,35) for y in range(24,39)})
    return {'back':back,'front':front,'pass':passing}


def arm(spec,near,bob,arms):
    kind,dx,dy,tilt=spec
    source=arms[kind]
    out=canvas(64,64)
    darken={LIGHT:ROBE,ROBE:DEEP,WHITE:GREY,SKINHI:SKIN,SKIN:SHADE}
    for y,row in enumerate(source):
        for x,c in enumerate(row):
            if c[3]:
                # Pivot at the shoulder. Entire rows move, no resampling.
                xx=x+dx+(y-26)*tilt//10
                yy=y+dy+bob
                out[yy][xx]=c if near else darken.get(c,c)
    return out


def draw_frame(i,parts,shoes,arms):
    phase=i%4; swap=i>=4; bob=BOB[phase]
    head,torso,sash,hem,weapon=parts
    forward,back=LEGS[phase]
    leg_swap=swap != (phase==3)
    far=leg(forward if leg_swap else back,False,shoes)
    near=leg(back if leg_swap else forward,True,shoes)
    # Full source accessory, with no line-based approximation.
    accessory=transformed(weapon,bob,0)
    # One frame lag follows the preceding phase, separately from torso bob.
    lag=(phase-1)%4
    tail_offsets=((-1,0),(-2,1),(-1,-1),(0,1))
    tx,ty=tail_offsets[lag]
    tail=transformed(hem,bob+ty,tx)
    core=transformed(torso,bob,0,True)
    # Preserve the source knot; trail the ribbon rows with a one-frame lag.
    ribbons=transformed(sash,bob,0,tail=(1,2,3,2)[lag])
    # Original head, hair tie, face and visor are NOT sheared or recoloured.
    face=transformed(head,bob,3)
    # Forward/backward pumping, far arm shaded and behind the core.
    poses=[(('front',0,0,0),('back',0,0,0)),
           (('front',-1,1,0),('back',1,0,1)),
           (('pass',3,1,0),('pass',1,0,0)),
           (('back',1,-1,-1),('front',-1,0,0))]
    front,backarm=poses[phase]
    fararm=arm(front if not swap else backarm,False,bob,arms)
    neararm=arm(backarm if not swap else front,True,bob,arms)
    # Composite the collar and sash last so their source clusters stay readable.
    return composite(accessory,far,fararm,tail,near,neararm,core,ribbons,face)


def label(im,text,x,y,c=GREY):
    # Tiny deterministic labels in the review sheets only.
    font={'0':['111','101','101','101','111'],'1':['010','110','010','010','111'],
          '2':['111','001','111','100','111'],'3':['111','001','111','001','111'],
          '4':['101','101','111','001','001'],'5':['111','100','111','001','111'],
          '6':['111','100','111','101','111'],'7':['111','001','010','010','010'],
          '8':['111','101','111','101','111'],'9':['111','101','111','001','111'],
          'V':['101','101','101','101','010'],'O':['111','101','101','101','111'],
          'L':['100','100','100','100','111'],'D':['110','101','101','101','110']}
    for char in text:
        if char in font:
            for yy,row in enumerate(font[char]):
                for xx,p in enumerate(row):
                    if p=='1': im[y+yy][x+xx]=c
        x+=4


def main():
    run=read_png(REF/'hero_run_6f.png')
    idle=read_png(REF/'hero_idle_4f.png')
    attack=read_png(REF/'hero_attack_5f.png')
    parts=source_parts(run)
    assert sum(bool(c[3]) for r in parts[0] for c in r)==sum(bool(c[3]) for r in run[4:22] for c in r[:64])==278
    shoes={'flat':cut_rows(run,{y:(40,51) for y in range(55,62)}),
           'angle':cut_rows(run,{y:(15,25) for y in range(54,62)})}
    arms=source_arms(run)
    frames=[draw_frame(i,parts,shoes,arms) for i in range(8)]
    strip=canvas(512,64)
    for i,f in enumerate(frames): paste(strip,f,64*i,0)
    save_png(OUT/'hero_run_8f.png',strip)
    preview=[]
    for f in frames:
        stage=canvas(64,64,BG)
        line(stage,[(0,62),(63,62)],GREY)
        paste(stage,f,0,0)
        preview.append(scale(stage,4))
    palette=sorted({p for f in preview for row in f for p in row})
    save_gif(OUT/'hero_run_8f_preview_x4.gif',preview,palette)
    v1=read_png(OUT.parent/'run_v1/hero_run_8f.png')
    compare=canvas(512,222,BG)
    for row,(name,src,x) in enumerate([('OLD',run,64),('V1',v1,0),('V2',strip,0)]):
        top=row*74
        label(compare,name,2,top+2)
        line(compare,[(0,top+70),(511,top+70)],GREY)
        paste(compare,src,x,top+8)
    save_png(OUT/'hero_run_compare_x4.png',scale(compare,4))
    # User-requested per-frame self-check, every v2 frame beside old run frame 0.
    check=canvas(128*4,74*2,BG)
    old0=crop(run,(0,0,64,64))
    for i,f in enumerate(frames):
        x,y=(i%4)*128,(i//4)*74
        label(check,'OLD',x+2,y+1)
        label(check,'V2 '+str(i),x+66,y+1)
        line(check,[(x,y+70),(x+127,y+70)],GREY)
        paste(check,old0,x,y+8); paste(check,f,x+64,y+8)
    save_png(OUT/'hero_run_selfcheck_x4.png',scale(check,4))
    source_colors={p for s in (run,idle,attack) for row in s for p in row if p[3]}
    assert len(source_colors)==16
    assert {p for row in strip for p in row if p[3]}<=source_colors
    assert {p[3] for row in strip for p in row}=={0,255}
    # Verify every protected source pixel survives compositing unchanged.
    for i,f in enumerate(frames):
        for p,dx,shear in ((parts[0],3,False),(parts[1],0,True),(parts[2],0,False)):
            expected=transformed(p,BOB[i%4],dx,shear,tail=(1,2,3,2)[(i%4-1)%4] if p is parts[2] else 0)
            for y,row in enumerate(expected):
                for x,c in enumerate(row):
                    if c[3]: assert f[y][x]==c,(i,x,y,'protected cluster overwritten')
    for i in (0,1,2,4,5,6): assert any(p[3] for p in frames[i][61])
    for i in (3,7): assert all(not p[3] for row in frames[i][55:] for p in row)
    assert all(not p[3] for f in frames for row in f[62:] for p in row)
    # Parse GIF block structure: independently verify all eight frame delays.
    gif=(OUT/'hero_run_8f_preview_x4.gif').read_bytes()
    assert gif[:6]==b'GIF89a' and struct.unpack('<HH',gif[6:10])==(256,256)
    pos=13+768; delays=[]; images=0
    while gif[pos]!=0x3b:
        tag=gif[pos]; pos+=1
        if tag==0x21:
            ext=gif[pos];pos+=1
            if ext==0xf9:
                assert gif[pos]==4
                delays.append(struct.unpack('<H',gif[pos+2:pos+4])[0]*10)
            while gif[pos]:pos+=1+gif[pos]
            pos+=1
        elif tag==0x2c:
            packed=gif[pos+8];pos+=9
            if packed&128:pos+=3*(2**((packed&7)+1))
            pos+=1
            while gif[pos]:pos+=1+gif[pos]
            pos+=1;images+=1
        else:raise AssertionError(('GIF block',tag,pos))
    assert images==8 and delays==[50]*8
    report={'strip':[512,64], 'cells':[64,64], 'frames':8,'fps':20,'delay_ms':50,
            'palette':len(source_colors),'alpha':[0,255],
            'body_screen_bob':BOB,'flight_bottoms':[max(y for y,r in enumerate(frames[i]) if any(p[3] for p in r)) for i in (3,7)],
            'source_sha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in
              [REF/'hero_run_6f.png',REF/'hero_idle_4f.png',REF/'hero_attack_5f.png']}}
    (OUT/'validation.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(report,indent=2))

if __name__=='__main__':
    main()
