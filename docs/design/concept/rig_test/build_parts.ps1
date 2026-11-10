$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class RigArt {
 public static Color C(string hex) { return ColorTranslator.FromHtml(hex); }
 public static Rectangle Bounds(Bitmap b) {
  int l=b.Width,t=b.Height,r=-1,d=-1;
  for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>127){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);d=Math.Max(d,y);}
  return Rectangle.FromLTRB(l,t,r+1,d+1);
 }
 public static Bitmap Part(string file,int w,int h,string[] colors) {
  using(var src=new Bitmap(file)) {
   Rectangle box=Bounds(src); var hi=new Bitmap((w-4)*4,(h-4)*4,PixelFormat.Format32bppArgb);
   using(var g=Graphics.FromImage(hi)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(src,new Rectangle(0,0,hi.Width,hi.Height),box,GraphicsUnit.Pixel);}
   Color[] p=Array.ConvertAll(colors,C);
   for(int y=0;y<hi.Height;y++)for(int x=0;x<hi.Width;x++){
    Color c=hi.GetPixel(x,y); if(c.A<100){hi.SetPixel(x,y,Color.Transparent);continue;}
    double best=Double.MaxValue;Color match=p[0];
    foreach(Color q in p){double dist=(c.R-q.R)*(c.R-q.R)+(c.G-q.G)*(c.G-q.G)+(c.B-q.B)*(c.B-q.B);if(dist<best){best=dist;match=q;}}
    hi.SetPixel(x,y,match);
   }
   var dst=new Bitmap(w,h,PixelFormat.Format32bppArgb);
   using(var g=Graphics.FromImage(dst)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(hi,new Rectangle(2,2,w-4,h-4));}
   hi.Dispose();return dst;
  }
 }
 public static Bitmap Tint(Bitmap src,double f){var b=new Bitmap(src.Width,src.Height,PixelFormat.Format32bppArgb);for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var c=src.GetPixel(x,y);b.SetPixel(x,y,Color.FromArgb(c.A,(int)Math.Round(c.R*f),(int)Math.Round(c.G*f),(int)Math.Round(c.B*f)));}return b;}
 public static void Save(Bitmap b,string path){b.Save(path,ImageFormat.Png);}
 public static void Joint(Bitmap b,int cx,int cy,int r,string baseColor,string shadeColor){
  // Guarantee a filled circular overlap around every rotating joint.
  using(var disk=new Bitmap(b.Width,b.Height,PixelFormat.Format32bppArgb)){
   using(var g=Graphics.FromImage(disk)){
    g.SmoothingMode=SmoothingMode.AntiAlias;
    using(var brush=new SolidBrush(C(baseColor)))g.FillEllipse(brush,cx-r,cy-r,2*r,2*r);
    using(var brush=new SolidBrush(C(shadeColor)))g.FillPie(brush,cx-r,cy-r,2*r,2*r,90,90);
    using(var pen=new Pen(C("#12161e"),1.2f))g.DrawEllipse(pen,cx-r,cy-r,2*r,2*r);
    g.DrawImageUnscaled(b,0,0);
   }
   using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.DrawImageUnscaled(disk,0,0);}
  }
 }
}
'@
$here = $PSScriptRoot
$out = Join-Path $here 'parts'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$ink='#12161e'; $grey=@($ink,'#646e73','#3f4856'); $navy=@($ink,'#232b3f','#171d2e')
$skin=@('#daaa8d','#956f65'); $steel=@('#b7c4ca','#76868f'); $white=@('#e5e8e8','#9da7b5')
$palettes=@{
 head=@($ink,'#262b31','#4fa6a6','#307077')+$skin+$grey[1..2]
 torso=$grey
 pelvis=$navy
 upper_arm_near=$grey
 forearm_near=$grey+$skin
 forearm_open=$grey+$skin
 thigh_near=$navy
 shin_near=$navy+$white+$skin
 blade=$grey+$steel
}
$sizes=@{head=@(94,110);torso=@(84,158);pelvis=@(76,65);upper_arm_near=@(46,112);forearm_near=@(38,124);forearm_open=@(38,132);thigh_near=@(57,142);shin_near=@(64,154);blade=@(109,20)}
$parts=@{}
foreach($name in $sizes.Keys){
 $s=$sizes[$name]
 $parts[$name]=[RigArt]::Part((Join-Path $here "source/$name.png"),$s[0],$s[1],$palettes[$name])
}
[RigArt]::Joint($parts.pelvis,34,49,12,'#232b3f','#171d2e')
[RigArt]::Joint($parts.pelvis,56,49,12,'#232b3f','#171d2e')
[RigArt]::Joint($parts.upper_arm_near,23,22,19,'#646e73','#3f4856')
[RigArt]::Joint($parts.upper_arm_near,23,94,17,'#646e73','#3f4856')
[RigArt]::Joint($parts.forearm_near,19,18,15,'#646e73','#3f4856')
[RigArt]::Joint($parts.thigh_near,28,26,23,'#232b3f','#171d2e')
[RigArt]::Joint($parts.thigh_near,28,122,17,'#232b3f','#171d2e')
[RigArt]::Joint($parts.shin_near,19,19,16,'#232b3f','#171d2e')
# Same sleeve geometry and colors on both forearms; only the hand is different.
$open=$parts.forearm_open
$farBase=[System.Drawing.Bitmap]::new(38,132)
$g=[System.Drawing.Graphics]::FromImage($farBase)
$g.DrawImage($parts.forearm_near,[System.Drawing.Rectangle]::new(0,0,38,97),[System.Drawing.Rectangle]::new(0,0,38,97),[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($open,[System.Drawing.Rectangle]::new(0,95,38,37),[System.Drawing.Rectangle]::new(0,95,38,37),[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$parts.forearm_far=[RigArt]::Tint($farBase,0.88)
foreach($stem in @('upper_arm','thigh','shin')){$parts["${stem}_far"]=[RigArt]::Tint($parts["${stem}_near"],0.88)}
foreach($name in $parts.Keys){if($name -ne 'forearm_open'){[RigArt]::Save($parts[$name],(Join-Path $out "$name.png"))}}
foreach($b in $parts.Values){$b.Dispose()}
$farBase.Dispose()
Write-Output '12 transparent parts written.'
