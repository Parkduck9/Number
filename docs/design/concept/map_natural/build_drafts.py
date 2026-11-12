"""Draft-only pixel authoring. Run from any directory; never writes into Assets.

Compositing uses Pillow. Pixel primitives are authored in pixels.py.
python build_drafts.py --pass 0  # initial
python build_drafts.py --pass 1  # first visual correction
python build_drafts.py --pass 2  # final visual correction
"""
from pathlib import Path
from collections import Counter
import argparse
import json
from pixels import Raster

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[3]
PARTS = ROOT / 'Assets/Art/Backgrounds/STG02_05_Parts'
CAPS = ROOT / 'docs/qa/level_overview/after3'
args = argparse.ArgumentParser()
args.add_argument('--pass', dest='revision', type=int, default=2)
args.add_argument('--parts-only', action='store_true')
opt = args.parse_args()
REV = opt.revision
T = (0, 0, 0, 0)

def load(name):
    return Raster.open(PARTS / (name+'.png'))

def colors(im):
    return set(tuple(im.data[i:i+4]) for i in range(0, len(im.data), 4) if im.data[i+3])

wall = load('buildings/wall_tile')
rail = load('buildings/rail_track')
far = load('far/rooftops_skyline')
warm_source = load('buildings/shop_2f')
warm_palette = colors(warm_source) | colors(load('props/lantern_hanji'))

def near(target, palette):
    return min(sorted(palette), key=lambda c: sum((a-b)**2 for a,b in zip(c[:3],target)))

# All facade colours are exact RGB samples from the existing wall / steel / shop / lantern.
INK = (0, 6, 25, 255)
WALL = (13, 21, 40, 255)
STONE = (38, 45, 63, 255)
EDGE = (56, 64, 75, 255)
STEEL = (43, 48, 67, 255)
MID = (68, 75, 91, 255)
LIT = (94, 98, 110, 255)
AMBER = near((145, 101, 65), warm_palette)
HONEY = near((178, 130, 80), warm_palette)
PAPER = near((204, 150, 96), warm_palette)
TEAL = (44, 82, 70, 255)
RED = (75, 39, 40, 255)

def frame(im, box, fill, border=INK, width=2):
    x0,y0,x1,y1=box
    im.rect(box,border)
    im.rect((x0+width,y0+width,x1-width,y1-width),fill)

def window(im,x,y,lit=True):
    frame(im,(x,y,x+33,y+31),AMBER if lit else WALL)
    im.rect((x+2,y+2,x+31,y+3),HONEY if lit else STONE)
    # Changho-inspired rectilinear lattice, not faux letters.
    for dx in (10,22):
        im.rect((x+dx,y+2,x+dx+1,y+27),STONE)
    im.rect((x+2,y+14,x+31,y+15),STONE)
    im.rect((x-2,y+30,x+35,y+33),INK)
    im.rect((x,y+30,x+33,y+31),EDGE)

def facades():
    a=Raster(140,86)
    window(a,20,40,True)
    window(a,82,40,REV == 0)
    # Muted green lintel, only two small red end blocks.
    a.rect((18,36,117,37),INK)
    a.rect((20,38,115,39),TEAL)
    for x in (20,112): a.rect((x,38,x+3,39),RED)
    b=Raster(140,86)
    frame(b,(20,42,75,73),MID)
    b.rect((22,44,73,45),LIT)
    frame(b,(24,48,45,67),STEEL)
    # Four squared fan blades and a dark hub, no noisy speckles.
    b.rect((30,50,37,65),EDGE)
    b.rect((26,54,43,61),EDGE)
    b.rect((32,54,37,59),INK)
    for y in (48,54,60,66): b.rect((52,y,69,y+1),STEEL)
    b.rect((26,74,31,77),INK)
    b.rect((64,74,69,77),INK)
    b.line([(76,58),(92,58),(92,74),(116,74)],INK,6)
    b.line([(76,58),(92,58),(92,74),(116,74)],EDGE,2)
    # The upper cable bundle does not cross any tile boundary.
    b.line([(18,32),(88,32),(100,44),(122,44)],INK,4)
    b.line([(18,32),(88,32),(100,44),(122,44)],STEEL,2)
    c=Raster(140,86)
    c.line([(24,34),(24,46),(38,46)],INK,4)
    c.line([(24,34),(24,44),(38,44)],STEEL,2)
    c.line([(38,44),(38,50)],INK,2)
    # Small hanji shade; warm paper stays much darker than gameplay digits.
    c.poly([(30,50),(46,50),(50,56),(50,68),(46,74),(30,74),(26,68),(26,56)],INK)
    c.rect((30,54,45,69),AMBER)
    c.rect((32,54,41,67),HONEY)
    if REV == 0: c.rect((34,56,39,65),PAPER)
    for x in (30,38,46): c.rect((x,54,x+1,69),RED)
    c.rect((30,50,45,53),STEEL)
    c.rect((30,70,45,73),STEEL)
    c.rect((36,74,39,77),RED)
    # A closed service panel gives this bay a different mass from the windows.
    frame(c,(74,42,115,75),WALL)
    c.rect((76,44,113,45),STONE)
    for x in (82,94,106): c.rect((x,50,x+1,67),STONE)
    return {'facade_a':a,'facade_b':b,'facade_c':c}

def supports():
    b=Raster(96,64)
    # Wall left; beam projects right. Upper flange overlaps the girder underside.
    frame(b,(0,0,9,63),STEEL)
    frame(b,(0,0,95,9),STEEL)
    b.line([(8,57),(88,7)],INK,10)
    b.line([(8,55),(86,7)],MID,6)
    b.line([(10,52),(84,6)],LIT,2)
    b.rect((2,2,7,59),MID)
    b.rect((2,2,91,3),LIT)
    for y in (16,48):
        b.rect((2,y,7,y+5),INK)
        b.rect((4,y,5,y+1),LIT)
    # Base plate tying the diagonal into the wall, no ornament on a load path.
    frame(b,(0,52,19,63),STEEL)
    b.rect((4,54,7,57),LIT)
    b.rect((12,54,15,57),MID)
    c=Raster(128,32)
    for x in (10,114):
        c.rect((x-2,0,x+3,31),INK)
        c.rect((x,0,x+1,31),MID)
        for y in (0,16):
            frame(c,(x-4,y+2,x+5,y+13),STEEL)
            c.rect((x-2,y+4,x-1,y+9),LIT)
            c.rect((x,y+4,x+1,y+9),T)
    return {'girder_bracket':b,'girder_chain':c}

def skyline():
    im=Raster(960,400)
    pal=colors(far) | colors(load('far/palace_skyline')) | colors(load('far/factory_skyline'))
    body=(11,18,37,255)
    face=(24,38,65,255)
    lit=(37,52,83,255)
    bright=(52,71,101,255)
    recess=(4,10,26,255)
    # Samples remain unmodified: depth is created by palette choice, never alpha/glow.
    def tower(x,w,top,steps=False,light=True):
        im.rect((x,top,x+w-1,399),body)
        if REV>=2:
            im.rect((x+w-14,top+2,x+w-1,399),recess)
            im.rect((x+2,top,x+w-17,top+1),face)
        else:
            im.rect((x,top,x+3,399),face)
            im.rect((x,top,x+w-1,top+3),face)
        if steps:
            im.rect((x+8,top-16,x+w-9,top-1),body)
            im.rect((x+8,top-16,x+w-9,top-15),face)
        if REV<2: im.rect((x+w-9,top+4,x+w-8,399),face)
        if light and REV>=2:
            # Three deliberate occupied-floor clusters; no random scattered dots.
            for i,dy in enumerate((30,104,186)):
                yy=top+dy
                xx=x+12+(14 if (x//10+i)%2 else 0)
                if yy<370:
                    for row in range(2 if i==1 else 1):
                        im.rect((xx,yy+row*8,xx+5,yy+row*8+1),face)
                        if w>=76 and i!=1: im.rect((xx+14,yy,xx+23,yy+1),face)
            im.rect((x+w-20,top+22,x+w-19,top+64),face)
        elif light:
            for y in range(top+26,380,32):
                for col in range(2):
                    xx=x+14+col*18
                    if xx+7<x+w-12:
                        im.rect((xx,y,xx+7,y+3),lit)
            for y in range(top+58,370,96): im.rect((x+14,y,x+21,y+3),bright if REV == 0 else lit)
    # One building crosses the repeat boundary: identical material and height on both sides.
    im.rect((0,274,39,399),body)
    im.rect((906,274,959,399),body)
    im.rect((0,274,39,277),face)
    im.rect((906,274,959,277),face)
    for x in (12,928):
        for y in (300,332,364): im.rect((x,y,x+7,y+3),lit)
    tower(46,94 if REV>=2 else 76,178 if REV>=2 else 152,True)
    tower(128,58,236)
    tower(194,86,102,True)
    tower(268,48,214,light=False)
    tower(328,76,190,True)
    tower(414,54,258,light=False)
    tower(486,84,130,True)
    tower(586,60,224)
    tower(658,94,78,True)
    tower(770,66,194,True)
    tower(846,46,244,light=False)
    if REV>=2:
        # Unequal service shoulders and a narrow tapered relay silhouette.
        im.rect((164,244,193,399),body)
        im.rect((164,244,185,245),face)
        im.poly([(294,218),(300,158),(312,158),(318,218),(318,399),(294,399)],recess)
        im.rect((304,136,307,157),body)
        im.rect((300,158,311,159),face)
        im.rect((744,278,769,399),body)
        im.rect((744,278,761,279),face)
    # Tall communications mast with one broad, shallow hanok roof and dancheong eaves.
    # Long shoulders and restrained corner lift avoid a pagoda silhouette.
    sx=532
    im.rect((sx-5,18,sx+4,132),body)
    im.rect((sx-3,20,sx-2,130),face)
    im.rect((sx-20,66,sx+19,67 if REV>=2 else 69),face)
    im.rect((sx-14,50,sx+13,51 if REV>=2 else 53),face if REV>=2 else lit)
    im.rect((sx-1,10,sx,19),lit)
    def roof(cx,cy,half):
        p=[(cx-half,cy-8),(cx-half+10,cy-2),(cx-24,cy-14),(cx+24,cy-14),(cx+half-10,cy-2),(cx+half,cy-8),(cx+half-4,cy+6),(cx-half+4,cy+6)]
        im.poly(p,body)
        im.line(p[:6],face,2)
        im.rect((cx-half+10,cy+6,cx+half-11,cy+9),face)
        im.rect((cx-28,cy+10,cx+27,cy+13),lit if REV==0 else face)
        for xx in range(cx-half+20,cx+half-18,16 if REV>=2 else 12):
            im.line([(xx,cy-2),(xx-2,cy+3)],face,2)
    roof(sx,112,70)
    im.rect((sx-26,126,sx+25,156),body)
    for x in (sx-20,sx+17): im.rect((x,126,x+3,155),face)
    im.rect((sx-12,132,sx+11,133 if REV>=2 else 135),face if REV>=2 else lit)
    # Hologram tower: outlined screen on a physical mounting frame. A broadcast
    # pictogram (nested square) instead of unreadable invented text.
    bx,by=686,114
    frame(im,(bx,by,bx+37,by+72),body,face)
    im.rect((bx+4,by+4,bx+33,by+5),face if REV>=2 else lit)
    im.rect((bx+8,by+18,bx+29,by+49),face)
    im.rect((bx+12,by+22,bx+25,by+45),body)
    im.rect((bx+16,by+26,bx+21,by+41),lit if REV==0 else face)
    for y in (by+58,by+64): im.rect((bx+8,y,bx+27,y+1),face if REV>=2 else lit)
    # Scattered rooftop plant, not random texture or window speckle.
    for x,y in [(60,146),(210,94),(346,184),(786,188)]:
        im.rect((x,y-10,x+13,y-1),body)
        im.rect((x,y-10,x+13,y-9),face)
    assert colors(im) <= pal
    assert all(im.get(0,y)==im.get(959,y) for y in range(400))
    return im

parts={**facades(),**supports(),'far_towers_strip':skyline()}
for n,im in parts.items():
    im.save(OUT/(n+'.png'))
    assert set(im.data[3::4]) <= {0,255}
for name in ('facade_a','facade_b','facade_c'):
    im=parts[name]
    assert im.size==wall.size
    assert all(im.get(x,y)[3]==0 for x in range(140) for y in range(86) if x<8 or x>=132 or y<8 or y>=78)

# Contact sheet: true-size parts on the actual wall / sky, plus four-times detail.
sheet=Raster(1760,800,(7,12,27,255))
for j,n in enumerate(('facade_a','facade_b','facade_c')):
    tile=wall.copy()
    tile.paste(parts[n],(0,0))
    sheet.paste(tile,(20+j*160,20))
    sheet.paste(tile.scale(4),(20+j*576,130))
sheet.paste(parts['girder_bracket'].scale(4),(20,510))
sheet.paste(parts['girder_chain'].scale(4),(440,510))
sheet.save(OUT/'parts_review_1x_4x.png')

# Mixed 3x3 repeats demonstrate untouched boundaries against the original wall.
repeat=Raster(420,258)
for y in range(3):
    for x in range(3):
        repeat.paste(wall,(x*140,y*86))
        name=[None,'facade_a','facade_b','facade_c'][((x+y*2)%4)]
        if name: repeat.paste(parts[name],(x*140,y*86))
repeat.scale(2).save(OUT/'facade_tiling_2x.png')
far_review=Raster(1920,400,(5,10,23,255))
far_review.paste(parts['far_towers_strip'],(0,0))
far_review.paste(parts['far_towers_strip'],(960,0))
far_review.save(OUT/'far_tiling_1x.png')

if opt.parts_only:
    print('Authored six transparent parts; compositing not run.')
    raise SystemExit

# Compositing at native PPU 32: capture cells are 480x270 at PPU 16, so restore
# their sampling to 960x540 using nearest neighbour BEFORE adding native parts.
# Assets themselves are never resized. Final cropped comparison is enlarged 2x.
from PIL import Image
def native(stage,box):
    return Image.open(CAPS/(stage+'_seg1_views.png')).convert('RGBA').crop(box).resize((960,540),Image.Resampling.NEAREST)
def convert(im): return Image.frombytes('RGBA',im.size,bytes(im.data))
def overlay(dst,src,xy): dst.alpha_composite(convert(src),xy)
def to_raster(im):
    r=Raster(*im.size); r.data[:]=im.tobytes(); return r

t0=native('STG01',(964,272,1444,542))
t=t0.copy()
# Tower is x320..511 in the native restored capture. Keep decoration on its
# tiled grid, clip the second tile exactly to the wall's right edge.
for row,(y,name) in enumerate([(164,'facade_a'),(250,'facade_b'),(336,'facade_c'),(422,'facade_a')]):
    tile=parts[name].dim(.8)
    overlay(t,tile,(320,y))
    if REV == 0:
        overlay(t,parts['facade_a'].dim(.8).crop((0,0,52,86)),(460,y))
    elif row==1:
        overlay(t,parts['facade_c'].dim(.8).crop((0,0,52,86)),(460,y))
# Brackets on right-attached ledges at y222,318,414. End plate lies on wall.
for y in (250,346,442):
    overlay(t,parts['girder_bracket'].mirror().dim(.5),(234,y))
# Cable pair: from the underside of the upper left slab (y206) down to the
# next left slab (y270), tucked behind both flanges; all endpoints meet steel.
chain=parts['girder_chain'].dim(.5)
for start in ((202,298,394) if REV>=2 else (202,)):
    for y in range(start,start+64,32): overlay(t,chain,(16,y))
    if REV>=2: overlay(t,chain.crop((0,0,128,6)),(16,start+64))
if REV>=1:
    # Connector shoes only in the mockup, built from the existing rail pixels;
    # notes explain equivalent 4px overlap, so no extra runtime piece is needed.
    for start in ((202,298,394) if REV>=2 else (202,)):
        for x in (24,128):
            shoe=Raster(10,4,STEEL).dim(.5)
            overlay(t,shoe,(x,start))
            overlay(t,shoe,(x,start+66))

s0=native('STG02',(482,0,962,270))
s=s0.copy()
strip=parts['far_towers_strip']
# Sky-only matte from source capture, preserving moon, rooflines, platforms,
# actors, and all existing scenic pixels. No scene repaint or sky replacement.
matte=Raster(960,540)
orig=to_raster(s0)
gate_mask=load('buildings/gate_scanner')
for y in range(400):
    for x in range(960):
        bg=orig.get(x,y)
        # Native sky in this capture is a near-black blue field. Existing
        # skyline/roof/props are excluded by their stronger local contrast.
        sky_pixel=(2<=bg[0]<=10 and 6<=bg[1]<=17 and 22<=bg[2]<=39
                   and 3<=bg[1]-bg[0]<=10 and 4<=bg[2]-2*bg[1]<=12)
        # The gate is the original sprite at native scale, source alpha supplies
        # its silhouette. Restore the same two-pixel capture sampling for alpha.
        gx,gy=(x-192)//2*2,(y-302)//2*2
        gate_pixel=(0<=gx<220 and 0<=gy<140 and gate_mask.get(gx,gy)[3]>0)
        if (y<300 if REV==0 else sky_pixel and not gate_pixel and (y<378 or bg[1]>=10)) or (REV==0 and bg[0]<=10 and bg[1]<=16 and 14<=bg[2]<=31):
            c=strip.get(x,y)
            # Preserve the moon explicitly (source bounds include hard edge).
            if not (448<=x<488 and 312<=y<352): matte.put(x,y,c)
overlay(s,matte,(0,0))

# Small hand-coded 3x5 review labels, outside the unchanged capture crops.
FONT={'B':['110','101','110','101','110'],'E':['111','100','110','100','111'],
      'F':['111','100','110','100','100'],'O':['010','101','101','101','010'],
      'R':['110','101','110','101','101'],'A':['010','101','111','101','101'],
      'T':['111','010','010','010','010']}
def label(im,text,x,y):
    for ch in text:
        for j,row in enumerate(FONT[ch]):
            for i,v in enumerate(row):
                if v=='1': im.rect((x+i*2,y+j*2,x+i*2+1,y+j*2+1),(136,148,164,255))
        x+=8
def compare(before,after,box,name):
    before,after=to_raster(before).crop(box),to_raster(after).crop(box)
    w,h=before.size
    board=Raster(w*2+12,h+24,(7,12,25,255))
    board.paste(before,(0,24)); board.paste(after,(w+12,24))
    label(board,'BEFORE',8,6); label(board,'AFTER',w+20,6)
    board.scale(2).save(OUT/(name+'.png'))
    after.save(OUT/(name+'_after_native.png'))
    if REV<2:
        (OUT/'review').mkdir(exist_ok=True)
        board.scale(2).save(OUT/'review'/(name+'_pass'+str(REV)+'.png'))
compare(t0,t,(0,112,560,540),'mock_towers')
compare(s0,s,(0,0,960,454),'mock_sky')

manifest={n:{'size_px':im.size,'opaque_palette_count':len(colors(im)), 'alpha_values':sorted(set(im.data[3::4]))} for n,im in parts.items()}
manifest['compositor']='Python / Pillow'
manifest['revision']=REV
(OUT/'checks.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Drafts generated; hard alpha, facade margins, exact source palette and skyline repeat edges checked.')
