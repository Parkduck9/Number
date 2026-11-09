"""Deterministic integer-pixel UI staging. Python standard library only.
Mulmaru reference atlas rasterized locally with SingleBitPerPixelGridFit.
Never writes into Assets. Run from repository root.
"""
from pathlib import Path
import struct, zlib, json, hashlib

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[3]
class Img:
    def __init__(self,w,h,c=(0,0,0,0)):
        self.w,self.h=w,h; self.p=[c]*(w*h)
    def dot(self,x,y,c):
        if 0<=x<self.w and 0<=y<self.h: self.p[y*self.w+x]=c
    def rect(self,x,y,w,h,c):
        for yy in range(y,y+h):
            for xx in range(x,x+w): self.dot(xx,yy,c)
    def line(self,x,y,X,Y,c):
        n=max(abs(X-x),abs(Y-y))
        for i in range(n+1): self.dot(x+round((X-x)*i/max(n,1)),y+round((Y-y)*i/max(n,1)),c)
    def crop(self,x,y,w,h):
        a=Img(w,h)
        for yy in range(h):
            for xx in range(w): a.dot(xx,yy,self.p[(y+yy)*self.w+x+xx])
        return a
    def scale(self,w,h):
        a=Img(w,h)
        for y in range(h):
            for x in range(w): a.dot(x,y,self.p[(y*self.h//h)*self.w+x*self.w//w])
        return a
    def paste(self,a,x,y):
        for yy in range(a.h):
            for xx in range(a.w):
                c=a.p[yy*a.w+xx]
                if c[3]: self.dot(x+xx,y+yy,c)
    def save(self,name):
        def chunk(t,b): return struct.pack('>I',len(b))+t+b+struct.pack('>I',zlib.crc32(t+b)&0xffffffff)
        raw=b''.join(b'\0'+bytes(v for c in self.p[y*self.w:(y+1)*self.w] for v in c) for y in range(self.h))
        (OUT/name).write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',self.w,self.h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))

def load(path):
    b=Path(path).read_bytes(); pos=8; data=b''; pal=[]; alpha=[]
    while pos<len(b):
        n=struct.unpack('>I',b[pos:pos+4])[0]; t=b[pos+4:pos+8]; v=b[pos+8:pos+8+n]; pos+=n+12
        if t==b'IHDR': w,h,depth,mode,_,_,inter=struct.unpack('>IIBBBBB',v)
        if t==b'IDAT': data+=v
        if t==b'PLTE': pal=[tuple(v[i:i+3]) for i in range(0,len(v),3)]
        if t==b'tRNS': alpha=list(v)
    assert inter==0 and depth in (1,2,4,8), (path,depth,inter)
    channels={0:1,2:3,3:1,4:2,6:4}[mode]; stride=(w*channels*depth+7)//8; bp=max(1,(channels*depth+7)//8)
    raw=zlib.decompress(data); previous=[0]*stride; a=Img(w,h)
    for y in range(h):
        f=raw[y*(stride+1)]; row=list(raw[y*(stride+1)+1:(y+1)*(stride+1)])
        for x in range(stride):
            l=row[x-bp] if x>=bp else 0; u=previous[x]; ul=previous[x-bp] if x>=bp else 0
            p=l+u-ul; ds=[abs(p-l),abs(p-u),abs(p-ul)]; pa=[l,u,ul][ds.index(min(ds))]
            row[x]=(row[x]+[0,l,u,(l+u)//2,pa][f])%256
        vals=row if depth==8 else [(v>>s)&((1<<depth)-1) for v in row for s in range(8-depth,-1,-depth)]
        for x in range(w):
            c=vals[x*channels:(x+1)*channels]
            if mode==6: col=tuple(c)
            elif mode==2: col=tuple(c)+(255,)
            elif mode==3: col=pal[c[0]]+(alpha[c[0]] if c[0]<len(alpha) else 255,)
            elif mode==0: col=(c[0]*255//((1<<depth)-1),)*3+(255,)
            else: col=(c[0],)*3+(c[1],)
            a.dot(x,y,col)
        previous=row
    return a

def color(s): return tuple(bytes.fromhex(s))+(255,)
NAVY=color('0B0F1E'); BORDER=color('3A3F4B'); DIM=color('8A90A0'); WHITE=color('FFFFFF'); SELECT=color('C8CCD8')
palette=load(ROOT/'Assets/Art/Palette/number_palette.png').p
print('Project palette:', ['#'+bytes(c[:3]).hex() for c in palette])
CYAN=color('4CE8D8'); INK=color('14141F'); IDLE=color('EDF2F7')
# Actual project palette indices are chosen by nearest distance to the reference accents.
def near(s):
    c=color(s); return min(palette,key=lambda p:sum((p[i]-c[i])**2 for i in range(3)))
TEAL=near('42A9BD'); MAG=near('DB508B'); GOLD=near('D7AA54'); ORANGE=color('D77E54'); RED=near('9D353A'); SHADE=near('202436')
MAG=max((p for p in load(ROOT/'Assets/Resources/DraftHud/broadcast_digits_magenta.png').p if p[3]),key=lambda p:p[0]+p[2]-2*p[1])
kit={}; borders={}
def save(name,a,b=None):
    a.save(name+'.png'); kit[name]=a
    if b: borders[name]=b

def frame(w,h,kind='panel'):
    a=Img(w,h,NAVY)
    a.rect(0,0,w,1,BORDER); a.rect(0,h-1,w,1,BORDER); a.rect(0,0,1,h,BORDER); a.rect(w-1,0,1,h,BORDER)
    a.rect(1,1,w-2,1,SELECT); a.rect(1,2,1,h-3,DIM)
    a.rect(2,h-2,w-3,1,SHADE); a.rect(w-2,2,1,h-3,SHADE)
    a.rect(3,3,w-6,1,TEAL); a.rect(3,3,1,h-6,TEAL); a.rect(3,h-4,w-6,1,TEAL); a.rect(w-4,3,1,h-6,TEAL)
    # Small mirrored dancheong meander, stepped eave end and ochre pin.
    for right in (False,True):
        for bottom in (False,True):
            def pt(x,y,c): a.dot(w-1-x if right else x,h-1-y if bottom else y,c)
            for x,y in [(2,2),(3,2),(4,2),(5,2),(6,2),(7,2),(2,3),(2,4),(2,5),(2,6),(3,6),(4,6),(5,6),(5,5),(5,4),(4,4)]: pt(x,y,TEAL)
            pt(3,3,GOLD); pt(3,4,RED); pt(3,5,RED)
            pt(7,1,GOLD); pt(1,7,GOLD)
    if kind=='dialogue':
        # Texture only in fixed top/bottom strips: stable under 9-slice.
        for x in range(11,w-10,5): a.dot(x,5,SHADE); a.dot(x+2,h-6,SHADE)
        a.rect(10,h-3,w-20,1,CYAN); a.rect(w-12,h-3,2,1,MAG)
    return a
save('panel_9s',frame(48,48),(9,9,9,9))
for state in ('normal','hover','pressed'):
    a=Img(32,16,NAVY); edge=CYAN if state=='hover' else BORDER
    for x,y,w,h,c in [(0,0,32,1,edge),(0,15,32,1,edge),(0,0,1,16,edge),(31,0,1,16,edge),(2,2,28,1,TEAL if state=='hover' else SHADE),(2,13,28,1,SHADE)]: a.rect(x,y,w,h,c)
    for x in (2,29): a.rect(x,5,1,6,TEAL); a.dot(x,7,GOLD)
    if state=='pressed':
        a.rect(1,1,30,2,INK); a.rect(1,1,2,14,INK); a.rect(3,3,26,10,NAVY); a.rect(3,12,26,1,BORDER)
    save('button_9s_'+state,a,(5,5,4,4))
save('dialogue_box_9s',frame(64,48,'dialogue'),(10,10,8,8))
a=frame(32,16); a.rect(8,12,16,1,MAG); save('dialogue_nameplate_9s',a,(9,9,7,7))
nextframe=Img(8,8)
for y,n in enumerate((5,3,1)):
    nextframe.rect(1+y,2+y,n,1,CYAN)
save('dialogue_next',nextframe)
a=Img(16,8); a.paste(nextframe,0,0)
for y,n in enumerate((5,3,1)): a.rect(9+y,3+y,n,1,TEAL)
save('dialogue_next_2f',a)
for name,inner in [('portrait_frame',50),('hud_face_bezel',48)]:
    a=frame(56,56); margin=(56-inner)//2; a.rect(margin,margin,inner,inner,(0,0,0,0))
    if name=='hud_face_bezel':
        a.rect(36,0,15,8,INK); a.rect(37,0,13,1,TEAL); a.rect(37,7,13,1,TEAL); a.rect(36,1,1,6,TEAL); a.rect(50,1,1,6,TEAL)
    save(name,a)
a=Img(32,12,NAVY); a.rect(0,0,32,1,BORDER); a.rect(0,11,32,1,BORDER)
for x in (0,31): a.rect(x,1,1,10,TEAL)
for x in range(3,29,4): a.dot(x,2,SHADE); a.dot(x+1,9,SHADE)
save('hint_plate_9s',a,(4,4,3,3))
a=Img(16,16)
mask=['1','11','111','1111','11111','111111','1111111','11111111','111111111','111111','111011','110011','1000011','0000011']
for y,row in enumerate(mask):
    for x,c in enumerate(row):
        if c=='1':
            a.dot(x,y,IDLE if x and y and x<len(row)-1 else INK)
a.line(1,1,1,10,CYAN); save('cursor_arrow',a)
for name,tint in [('crosshair_idle',IDLE),('crosshair_lock',CYAN)]:
    a=Img(16,16); pts=[]
    for i in range(2,5): pts += [(7,i),(7,14-i),(i,7),(14-i,7)]
    for x,y in pts:
        for dx,dy in ((-1,0),(1,0),(0,-1),(0,1)): a.dot(x+dx,y+dy,INK)
    for x,y in pts: a.dot(x,y,tint)
    save(name,a)
a=Img(8,8)
for x in range(1,6): a.rect(x,0,1,3,INK)
for y in range(1,6): a.rect(0,y,3,1,INK)
a.rect(1,1,5,1,CYAN); a.rect(1,1,1,5,CYAN); save('lock_bracket',a)
# White stencil: EnemyPointer applies orange/cyan tint at runtime.
a=Img(9,9)
for x in range(9):
    for y in range(9):
        if abs(y-4)*2<=8-x: a.dot(x,y,WHITE)
# Deliberate dark negative cutout gives the arrow a feather-like stepped shaft.
a.rect(0,3,3,3,(0,0,0,0)); save('enemy_pointer',a)

atlas=load(OUT/'_mulmaru_reference.png'); chars='ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789:.-/'
glyph={}
for j,c in enumerate(chars):
    g=atlas.crop(j*16,0,16,16); pts=[(i%16,i//16) for i,p in enumerate(g.p) if p[3]>127]
    x0=min(x for x,y in pts); x1=max(x for x,y in pts); y0=min(y for x,y in pts); y1=max(y for x,y in pts)
    glyph[c]=g.crop(x0,y0,x1-x0+1,y1-y0+1)
def label(a,s,x,y,c=WHITE,scale=1):
    for ch in s:
        if ch==' ': x+=4*scale; continue
        g=glyph[ch]; t=Img(g.w,g.h)
        for i,p in enumerate(g.p):
            if p[3]>127: t.p[i]=c
        a.paste(t.scale(t.w*scale,t.h*scale),x,y); x+=(g.w+1)*scale

# Pixel lettering: exact Mulmaru glyph grid, enlarged in integers, manually spaced and glitched.
a=Img(160,40); x=10
for j,ch in enumerate('NUMBER'):
    g=glyph[ch]; col=WHITE if j<4 else DIM
    for yy in range(g.h):
        for xx in range(g.w):
            if g.p[yy*g.w+xx][3]>127: a.rect(x+xx*3,7+yy*3,3,3,col)
    if ch in ('N','B'):
        # Two scanline displacements only, one pixel each side.
        row=16 if ch=='N' else 22
        for xx in range(x,x+g.w*3):
            if a.p[row*160+xx][3]: a.dot(xx-1,row,MAG); a.dot(xx+1,row+1,CYAN)
    x+=g.w*3+6
save('logo_number',a); save('logo_number_2x',a.scale(320,80))
digits=load(ROOT/'Assets/Resources/DraftHud/broadcast_digits_magenta.png')
# Existing broadcast strip = 10 cells, 12px cell + 1px spacing. Zero at cell 0.
digit=digits.crop(0,0,12,18)
for name,size,scale in [('app_icon',128,6),('favicon_32',32,1)]:
    a=Img(size,size,NAVY); d=digit.scale(12*scale,18*scale); a.paste(d,(size-d.w)//2,(size-d.h)//2)
    if size==32: a.rect(5,5,1,22,TEAL); a.rect(26,5,1,22,TEAL)
    save(name,a)
for who in ('android','bug'):
    src=load(ROOT/f'Assets/Art/Drafts/Portraits/lineup/portrait_{who}.png')
    a=Img(100,100); a.paste(src.scale(96,96),2,2); save('portrait_'+who,a)

def nine(name,w,h,scale=2):
    a=kit[name]; l,r,t,b=borders[name]; dest=Img(w,h)
    sx=[0,l,a.w-r,a.w]; sy=[0,t,a.h-b,a.h]; dx=[0,l*scale,w-r*scale,w]; dy=[0,t*scale,h-b*scale,h]
    for yy in range(3):
        for xx in range(3):
            patch=a.crop(sx[xx],sy[yy],sx[xx+1]-sx[xx],sy[yy+1]-sy[yy]); dest.paste(patch.scale(dx[xx+1]-dx[xx],dy[yy+1]-dy[yy]),dx[xx],dy[yy])
    return dest

scene=Img(960,540,NAVY)
# Quiet Joseon city silhouettes, stepped tiled eaves. No gradient or synthetic text.
for x,y,w,h in [(0,180,155,260),(175,230,120,210),(660,190,145,250),(820,155,140,285)]:
    scene.rect(x,y,w,h,SHADE)
    for inset,yy in [(0,y-4),(4,y-7),(12,y-10),(24,y-13)]: scene.rect(x+inset,yy,w-2*inset,3,BORDER)
    for wx in range(x+18,x+w-12,24): scene.rect(wx,y+34,4,12,TEAL)
scene.rect(0,368,960,12,INK); scene.rect(0,380,960,160,NAVY)
scene.paste(kit['logo_number'].scale(320,80),320,18)
scene.paste(nine('panel_9s',352,252),304,112); label(scene,'PAUSED',430,138,SELECT,2)
for j,(s,state) in enumerate([('RESUME','normal'),('CONTROLS','hover'),('TITLE','pressed')]):
    y=180+j*52; scene.paste(nine('button_9s_'+state,280,40),340,y); label(scene,s,438 if j!=1 else 426,y+12,WHITE if j==1 else DIM,2)
scene.paste(kit['cursor_arrow'].scale(32,32),608,239)
scene.paste(nine('dialogue_box_9s',896,124),32,386); scene.paste(nine('dialogue_nameplate_9s',150,32),148,358); label(scene,'ANDROID',168,369,SELECT)
# 50x50 portrait area, rendered 2x. 100px staging portrait is shown at native file size.
scene.paste(kit['portrait_android'],44,398); scene.paste(kit['portrait_frame'].scale(112,112),38,392)
label(scene,'BROADCAST SIGNAL LOST.',176,414,WHITE,2); label(scene,'AWAITING THE NEXT NUMBER.',176,446,DIM,2)
scene.paste(kit['dialogue_next'].scale(16,16),888,478)
face=load(ROOT/'Assets/Resources/DraftHud/face_0.png'); scene.paste(face.scale(96,96),32,24); scene.paste(kit['hud_face_bezel'].scale(112,112),24,16); label(scene,'0',104,20,MAG)
scene.paste(kit['portrait_bug'].scale(50,50),850,305); scene.paste(kit['portrait_frame'],847,302)
label(scene,'BUG',858,365,DIM)
scene.paste(kit['crosshair_idle'].scale(32,32),752,235); scene.paste(kit['crosshair_lock'].scale(32,32),861,313)
br=kit['lock_bracket'].scale(16,16)
for rot,pos in [(0,(839,294)),(1,(897,294)),(2,(897,352)),(3,(839,352))]:
    z=br
    for _ in range(rot):
        zz=Img(z.h,z.w)
        for yy in range(z.h):
            for xx in range(z.w): zz.dot(z.h-1-yy,xx,z.p[yy*z.w+xx])
        z=zz
    scene.paste(z,*pos)
arrow=kit['enemy_pointer'].scale(27,27); arrow.p=[ORANGE if p[3] else p for p in arrow.p]; scene.paste(arrow,909,212)
scene.paste(nine('hint_plate_9s',560,24),200,516)
for j,(key,text) in enumerate([('a','MOVE'),('space','JUMP'),('e','INTERACT')]):
    keyimg=load(ROOT/f'Assets/Resources/DraftUi/ctrl_key_{key}.png'); scene.paste(keyimg,214+j*180,518); label(scene,text,256+j*180,524,DIM)
save('mockup_preview',scene); save('mockup_preview_2x',scene.scale(1920,1080))
# Review atlas, source pixels at 4x; captions are outside the actual assets.
sheet=Img(960,840,NAVY); x,y=16,16
for name,a in list(kit.items()):
    if 'mockup' in name or '_2x' in name or name=='app_icon': continue
    tile=a.scale(a.w*4,a.h*4)
    if x+tile.w>944: x=16; y+=238
    if y+tile.h>820: continue
    sheet.paste(tile,x,y+16); label(sheet,name.upper().replace('_',' ').replace('9S','9').replace('2F','2'),x,y,DIM)
    x+=max(tile.w,180)+16
sheet.save('review_4x.png')
sheet2=Img(960,1200,NAVY)
label(sheet2,'NUMBER / ICON / PORTRAITS - 4X',16,10,DIM)
sheet2.paste(kit['logo_number'].scale(640,160),16,30)
sheet2.paste(kit['app_icon'].scale(512,512),16,220)
sheet2.paste(kit['favicon_32'].scale(128,128),560,220)
sheet2.paste(kit['portrait_android'].scale(400,400),544,370)
sheet2.paste(kit['portrait_bug'].scale(400,400),16,760)
sheet2.save('review_4x_2.png')
manifest={}
for name,a in kit.items():
    assert all(p[3] in (0,255) for p in a.p), name
    manifest[name+'.png']={'size':[a.w,a.h],'colors':len(set(p for p in a.p if p[3])),'alpha':[0,255] if any(not p[3] for p in a.p) else [255],'sha256':hashlib.sha256((OUT/(name+'.png')).read_bytes()).hexdigest()}
for name in ('portrait_frame','hud_face_bezel'):
    a=kit[name]; assert a.p[28*a.w+28][3]==0
for name,b in borders.items():
    a=kit[name]; l,r,t,bot=b
    # Stretch region must be uniform; ornaments and hanji dots stay in fixed borders.
    assert len({a.p[y*a.w+x] for y in range(t,a.h-bot) for x in range(l,a.w-r)})==1,name
(OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf8')
(OUT/'borders.json').write_text(json.dumps(borders,indent=2)+'\n',encoding='utf8')
print('Saved',len(kit),'PNGs; alpha, transparent centers and 9-slice interiors verified.')
