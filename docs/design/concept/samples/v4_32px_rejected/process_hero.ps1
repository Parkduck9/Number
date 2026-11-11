param([int]$CharacterHeight = 32, [int]$KeyTolerance = 22)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
# Processing only: no character drawing, local retouching, or synthetic animation.
# Global nearest-palette conversion intentionally caps light cloth/skin values.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
public static class HeroProcessor {
 static Color[] palette = Array.ConvertAll(new string[]{
 "101019","20202e","303044","45465b","202747","30385a",
 "46516a","666779","898990","aaa8a2","785452","a77969",
 "b49b87","305562","40818e","64afb7"}, s=>ColorTranslator.FromHtml("#"+s));
 static int Dist(Color a, Color b) { int r=a.R-b.R,g=a.G-b.G,c=a.B-b.B; return r*r+g*g+c*c; }
 static Color Quant(Color c) { int best=int.MaxValue; Color result=palette[0]; foreach(Color p in palette) {int d=Dist(c,p);if(d<best){best=d;result=p;}}return result; }
 public static string Process(string input,string output,int height,int tolerance) {
 using(Bitmap original=new Bitmap(input))
 using(Bitmap src=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb)) {
 using(Graphics g=Graphics.FromImage(src))g.DrawImageUnscaled(original,0,0);
 Color key=src.GetPixel(0,0); int x0=src.Width,y0=src.Height,x1=-1,y1=-1;
 // Color key applies throughout image, including gaps between limbs.
 for(int y=0;y<src.Height;y++)for(int x=0;x<src.Width;x++) {
 Color c=src.GetPixel(x,y); bool keep=c.A>=128 && Dist(c,key)>tolerance*tolerance;
 if(!keep)src.SetPixel(x,y,Color.Transparent);
 else {x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
 }
 if(x1<x0)throw new Exception("No foreground: "+input);
 int w=(int)Math.Round((x1-x0+1)*height/(double)(y1-y0+1));
 if(w>46 || height>46)throw new Exception("Character exceeds canvas");
 using(Bitmap dst=new Bitmap(48,48,PixelFormat.Format32bppArgb)) {
 int ox=(48-w)/2,oy=40-height;
 for(int y=0;y<height;y++)for(int x=0;x<w;x++) {
 int sx=x0+Math.Min(x1-x0,(int)((x+0.5)*(x1-x0+1)/w));
 int sy=y0+Math.Min(y1-y0,(int)((y+0.5)*(y1-y0+1)/height));
 Color c=src.GetPixel(sx,sy);if(c.A>=128)dst.SetPixel(ox+x,oy+y,Quant(c));
 }
 // Remove isolated single-pixel components; diagonal attachment is retained.
 var remove=new List<Point>();
 for(int y=0;y<48;y++)for(int x=0;x<48;x++)if(dst.GetPixel(x,y).A>0){
 int neighbors=0;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
 int nx=x+dx,ny=y+dy;if((dx!=0||dy!=0)&&nx>=0&&nx<48&&ny>=0&&ny<48&&dst.GetPixel(nx,ny).A>0)neighbors++;
 }if(neighbors==0)remove.Add(new Point(x,y));}
 foreach(Point p in remove)dst.SetPixel(p.X,p.Y,Color.Transparent);
 dst.Save(output,ImageFormat.Png);
 var colors=new HashSet<int>();int partial=0,count=0,minY=48,maxY=-1;
 for(int y=0;y<48;y++)for(int x=0;x<48;x++){
 Color c=dst.GetPixel(x,y);if(c.A!=0&&c.A!=255)partial++;
 if(c.A==255){colors.Add(c.ToArgb());count++;minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}}
 if(colors.Count>16||partial>0||count==0)throw new Exception("Validation failed");
 return Path.GetFileName(output)+": canvas=48x48; visible height="+(maxY-minY+1)+"; opaque colors="+colors.Count+"; partial alpha="+partial+"; isolated pixels removed="+remove.Count+"; opaque pixels="+count+"; crop="+x0+","+y0+","+(x1-x0+1)+","+(y1-y0+1);
 }} }
 public static void Preview(string[] files,string output,int scale,bool strip) {
 using(Bitmap dst=new Bitmap(strip?480:48*files.Length*scale,strip?270:48*scale)) {
 using(Graphics g=Graphics.FromImage(dst))g.Clear(ColorTranslator.FromHtml("#0b1030"));
 for(int i=0;i<(strip?1:files.Length);i++)using(Bitmap src=new Bitmap(files[i])) {
 int ox=strip?216:i*48*scale,oy=strip?111:0;
 for(int y=0;y<48;y++)for(int x=0;x<48;x++)if(src.GetPixel(x,y).A==255)
 for(int dy=0;dy<scale;dy++)for(int dx=0;dx<scale;dx++)dst.SetPixel(ox+x*scale+dx,oy+y*scale+dy,src.GetPixel(x,y));
 }dst.Save(output,ImageFormat.Png);
 }}
}
'@
$pairs = @(
 @('hero_gen_big_0.png','hero_idle_0.png'),
 @('hero_gen_big_alt_1.png','hero_idle_alt_1.png'),
 @('hero_gen_big_alt_2.png','hero_idle_alt_2.png')
)
$report = @('AI generated (Codex / image_gen).', 'Frame 0 only. No breathing animation: consistency not established.', 'Preview order: base, alternative 1, alternative 2. These are alternatives, NOT animation frames.')
$files = @()
foreach ($pair in $pairs) {
 $inputFile = Join-Path $PSScriptRoot $pair[0]
 $outputFile = Join-Path $PSScriptRoot $pair[1]
 $report += [HeroProcessor]::Process($inputFile,$outputFile,$CharacterHeight,$KeyTolerance)
 $files += $outputFile
}
[HeroProcessor]::Preview($files,(Join-Path $PSScriptRoot 'hero_idle_frames_x4.png'),4,$false)
[HeroProcessor]::Preview($files,(Join-Path $PSScriptRoot 'hero_idle_frames_x8.png'),8,$false)
[HeroProcessor]::Preview($files,(Join-Path $PSScriptRoot 'hero_idle_scale_1x.png'),1,$true)
$report += @('Processing: corner-color key, tight crop, center-sampled nearest-neighbor resize, fixed shared 16-color palette, binary alpha, isolated-pixel removal.', 'Light source requested: top left. No per-pixel manual edits.', 'Weaknesses: generated large images contain excess detail; 32px reduction loses glove/collar/cargo details. Alternatives 1 and 2 retain a partially frontal torso; alternative 2 has an unwanted sleeve motif. Base is the closest side view, but rear shoe and weight distribution remain imperfect.', 'Native outputs are review samples, not approved production sprites. No enemy hologram comparison was performed.')
$report | Set-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'validation.txt')
$report
