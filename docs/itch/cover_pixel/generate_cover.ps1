# DRAFT ONLY. Run from the repository root:
# powershell -NoProfile -ExecutionPolicy Bypass -File docs/itch/cover_pixel/generate_cover.ps1
# Windows / System.Drawing only. No packages, network, image-generation API or Assets writes.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$source = @'
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

public static class CoverPixelDraft {
  const int W=315,H=250;
  // Project palette, existing key-art accent colours, and portrait-related shades.
  static readonly Dictionary<string,int> P = new Dictionary<string,int>();
  static void Swatch(string name,string hex){P[name]=unchecked((int)0xff000000)|Convert.ToInt32(hex,16);}
  sealed class Canvas {
    public int Width,Height; public int[] Pixels;
    public Canvas(int w,int h,int c){Width=w;Height=h;Pixels=Enumerable.Repeat(c,w*h).ToArray();}
    public void Dot(int x,int y,string c){Put(x,y,P[c]);}
    public void Put(int x,int y,int c){if(x>=0&&x<Width&&y>=0&&y<Height)Pixels[y*Width+x]=c;}
    public int Get(int x,int y){return Pixels[y*Width+x];}
    public Canvas Copy(){var n=new Canvas(Width,Height,0);Array.Copy(Pixels,n.Pixels,Pixels.Length);return n;}
    public void Rect(string c,int x,int y,int w,int h){for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)Dot(xx,yy,c);}
    // Integer Bresenham lines and scan-line polygons: never a Graphics interpolation call.
    public void Line(string c,int x,int y,int xx,int yy){int dx=Math.Abs(xx-x),sx=x<xx?1:-1,dy=-Math.Abs(yy-y),sy=y<yy?1:-1,e=dx+dy;while(true){Dot(x,y,c);if(x==xx&&y==yy)break;int ee=e*2;if(ee>=dy){e+=dy;x+=sx;}if(ee<=dx){e+=dx;y+=sy;}}}
    public void Path(string c,params int[] a){for(int i=2;i<a.Length;i+=2)Line(c,a[i-2],a[i-1],a[i],a[i+1]);}
    public void Poly(string c,params int[] a){
      int n=a.Length/2,lo=Height,hi=0;for(int i=0;i<n;i++){lo=Math.Min(lo,a[2*i+1]);hi=Math.Max(hi,a[2*i+1]);}
      for(int y=Math.Max(0,lo);y<=Math.Min(Height-1,hi);y++)for(int x=0;x<Width;x++){
        bool inside=false;for(int i=0,j=n-1;i<n;j=i++)if((a[2*i+1]>y)!=(a[2*j+1]>y)&&x<(double)(a[2*j]-a[2*i])*(y-a[2*i+1])/(a[2*j+1]-a[2*i+1])+a[2*i])inside=!inside;
        if(inside)Dot(x,y,c);
      }
      for(int i=0;i<n;i++)Line(c,a[2*i],a[2*i+1],a[2*((i+1)%n)],a[2*((i+1)%n)+1]);
    }
    public void Disc(string c,int cx,int cy,int r){for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++)if(x*x+y*y<=r*r)Dot(cx+x,cy+y,c);}
    public Canvas Scale(int s){var n=new Canvas(Width*s,Height*s,0);for(int y=0;y<n.Height;y++)for(int x=0;x<n.Width;x++)n.Put(x,y,Get(x/s,y/s));return n;}
    public void Save(string path){using(var b=new Bitmap(Width,Height,PixelFormat.Format32bppArgb)){for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)b.SetPixel(x,y,Color.FromArgb(Get(x,y)));b.Save(path,ImageFormat.Png);}}
  }
  static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
  static void Palette(){
    Swatch("ink","080B14");Swatch("deep","0B1026");Swatch("night","1A1D33");
    Swatch("indigo","2D3050");Swatch("slate","3F4A6E");Swatch("edge","62677D");
    Swatch("gray","858A9B");Swatch("linen","D6D7D0");
    Swatch("skinDark","7A3A3C");Swatch("skinShade","A37B70");
    Swatch("skinRose","B77569");Swatch("skin","D69A83");Swatch("skinLight","F8D0B2");
    Swatch("tealDark","226076");Swatch("teal","42A9BD");Swatch("cyan","6FFBF4");Swatch("ice","E5FDF5");
    Swatch("ochre","B8862E");Swatch("goldShade","C2AE78");Swatch("gold","FFD24A");Swatch("light","FFF1A6");
    Swatch("red","F21C2A");Swatch("redDark","7F1A22");
    Swatch("plum","3A0A2E");Swatch("violet","8D4094");Swatch("pink","FF3FB4");Swatch("pinkLight","FFC2E8");
    Swatch("hair","2D2D44");Swatch("hairLight","4D4C6D");Swatch("cloth","2D345D");
    Swatch("steel","5D84A0");Swatch("warm","D77E54");
  }
  static void Background(Canvas a){
    // Large discrete sky planes replace the source's soft atmospheric gradient.
    a.Rect("deep",0,0,W,H);
    a.Poly("night",0,40,58,40,89,51,161,52,192,38,238,38,265,51,315,51,315,151,0,151);
    a.Poly("indigo",0,83,35,83,56,96,107,96,128,86,191,86,213,93,249,86,315,86,315,140,0,140);
    a.Poly("night",0,122,48,122,73,135,148,135,173,126,207,126,235,116,315,116,315,250,0,250);
    // Distant windows and luminous bokeh: hard pixel discs, a maximum of two bands.
    a.Rect("tealDark",223,83,10,22);a.Rect("teal",225,84,6,17);a.Rect("cyan",226,85,4,14);
    a.Rect("plum",35,134,11,14);a.Rect("violet",38,138,5,8);
    a.Rect("tealDark",188,136,17,14);a.Rect("teal",190,138,13,9);a.Rect("cyan",193,139,9,7);
    a.Rect("tealDark",269,58,4,17);a.Rect("tealDark",300,52,4,23);
    int[,] discs={{5,94,5,0},{42,112,6,0},{57,127,7,0},{206,90,8,0},{206,30,6,1},{197,71,3,1},{207,139,5,1},{196,138,6,0},{264,64,8,2},{280,67,5,3},{265,2,7,3},{280,7,5,3},{228,28,7,3},{29,96,3,3},{225,141,4,3}};
    for(int i=0;i<discs.GetLength(0);i++){
      int x=discs[i,0],y=discs[i,1],r=discs[i,2],type=discs[i,3];
      string outer=type<2?"plum":"tealDark",inner=type==0?"pink":type==1?"violet":type==2?"cyan":"teal";
      a.Disc(outer,x,y,r+1);a.Disc(inner,x,y,r-1);
      if(type==0&&r>5)a.Line("pinkLight",x-2,y-2,x+1,y-2);
    }
    // Left distant hanok and continuous foreground ridge, no pseudo-signage.
    a.Poly("deep",0,101,21,112,43,118,58,117,67,114,66,121,57,126,41,127,18,120,0,117);
    a.Path("slate",0,103,23,115,42,121,56,121,64,119);
    a.Rect("deep",0,125,31,24);a.Rect("ink",4,130,18,12);
    a.Line("indigo",8,131,8,141);a.Line("indigo",15,133,15,142);
    // Small upper-right eave; source composition retained behind the logo pocket.
    a.Poly("ink",239,37,248,29,258,22,280,17,315,12,315,50,287,48,266,43,245,43);
    a.Poly("night",247,33,262,24,288,20,315,18,315,29,285,29,262,34);
    a.Path("tealDark",244,34,261,26,289,22,315,21);
    a.Path("indigo",242,39,264,37,286,40,315,40);
    for(int x=263;x<316;x+=13){a.Line("deep",x,25,x-7,32);a.Rect("night",x,41,3,5);}
    // Main right roof: curved giwa eave, restrained bands, readable tile courses.
    a.Poly("ink",194,105,198,110,199,115,212,115,231,106,249,92,260,79,262,75,265,78,288,76,315,70,315,132,290,138,263,139,238,136,216,131,201,121);
    a.Poly("night",201,116,215,116,234,107,252,93,264,80,288,80,315,74,315,104,296,115,274,127,247,130,225,126,210,122);
    a.Poly("indigo",217,117,238,107,253,96,264,84,288,84,315,78,315,86,287,91,270,103,250,119,233,122);
    a.Path("tealDark",200,115,212,117,230,109,250,95,262,79,288,78,315,72);
    a.Path("slate",219,118,237,111,255,98,266,84,287,84,315,78);
    a.Path("ink",206,120,227,127,247,131,275,129,300,117,315,108);
    a.Path("slate",209,122,229,129,248,133,269,132);
    a.Path("indigo",213,126,236,134,260,137,284,134,311,120);
    int[,] tiles={{259,91,243,112},{270,88,255,111},{283,86,270,109},{296,83,284,105},{310,80,299,100}};
    for(int i=0;i<tiles.GetLength(0);i++){a.Line("ink",tiles[i,0],tiles[i,1],tiles[i,2],tiles[i,3]);a.Line("tealDark",tiles[i,0]+2,tiles[i,1],tiles[i,2]+2,tiles[i,3]);}
    a.Path("deep",248,101,267,105,291,99,315,91);
    a.Path("slate",242,109,257,114,270,113,293,106,315,97);
    for(int x=228;x<280;x+=9)a.Rect("night",x,132,3,4);
    a.Poly("deep",257,140,277,139,278,190,315,191,315,250,257,250);
    a.Rect("ink",262,144,8,106);a.Rect("indigo",271,147,3,103);
    a.Rect("ink",281,151,26,19);a.Rect("night",284,154,20,13);
    a.Rect("ink",280,198,14,27);a.Rect("indigo",281,200,9,4);a.Rect("deep",293,219,22,31);
    // Lower left roof and wall are deliberately low-contrast behind the hero.
    a.Rect("deep",0,154,264,96);a.Rect("night",0,161,264,5);
    a.Poly("ink",0,149,79,150,112,148,186,148,215,145,257,146,260,152,218,155,180,157,119,155,82,157,0,157);
    a.Path("tealDark",0,150,76,151,112,150,184,150,214,147,257,148);
    a.Path("indigo",0,154,81,155,119,153,181,155,219,153,259,151);
    for(int x=8;x<256;x+=12){a.Poly("night",x,159,x+9,159,x+6,173,x-5,173);a.Line("indigo",x+1,160,x-3,170);}
    a.Rect("ink",0,175,263,5);a.Line("indigo",0,179,261,179);
    a.Rect("night",6,183,17,43);a.Rect("ink",10,186,11,24);
    a.Rect("night",238,181,15,69);a.Rect("ink",243,185,5,65);
    a.Line("indigo",254,182,254,249);
    // Authored rain follows one consistent down-left slope; no random speckle field.
    int[,] rain={{25,0,22,17},{81,8,78,25},{124,0,121,18},{175,22,172,40},{235,0,232,16},{310,5,307,22},{14,53,11,70},{29,70,27,81},{213,49,210,66},{218,110,216,123},{308,136,305,153},{291,173,288,191},{283,222,280,240},{222,225,219,245},{8,111,6,123},{21,21,20,31}};
    for(int i=0;i<rain.GetLength(0);i++)a.Line(i%4==0?"tealDark":"indigo",rain[i,0],rain[i,1],rain[i,2],rain[i,3]);
    // Quiet top sky is a flat plane rather than a dark alpha overlay.
    a.Poly("deep",0,0,249,0,244,14,234,26,231,39,199,39,190,44,150,44,125,39,82,39,65,34,0,34);
    a.Line("night",23,0,20,15);a.Line("night",82,4,80,19);a.Line("night",171,19,168,37);
  }
  static void Talisman(Canvas a){
    // Opaque concentric colour bands, then a clean paper silhouette. No blur.
    a.Poly("ochre",213,160,226,144,246,135,263,122,279,102,286,116,301,125,293,140,272,154,256,167,240,182,230,171);
    a.Poly("gold",216,160,228,147,250,137,267,123,279,106,285,118,298,126,291,137,271,150,254,162,239,178,231,168);
    a.Poly("light",218,159,230,147,251,138,268,124,279,108,283,119,295,127,289,136,269,148,251,160,239,175,233,166);
    a.Poly("gold",219,161,231,158,252,147,269,137,286,130,295,127,289,137,270,150,252,163,239,178,233,168);
    a.Path("light",218,160,230,164,239,175);
    a.Path("ochre",239,178,252,163,270,150,290,138);
    // Abstract brush flicks: explicitly no traced source glyphs, letters, or symbols.
    a.Poly("red",228,154,239,149,249,147,261,139,258,142,249,150,237,154,226,158);
    a.Poly("red",242,151,249,148,247,155,243,160,245,153,238,159);
    a.Poly("redDark",250,144,259,138,268,137,264,140,258,141,252,146);
    a.Poly("red",264,130,269,132,275,130,279,129,276,133,270,135,265,133);
    a.Poly("red",279,117,281,121,288,125,287,127,280,124,277,125,278,121);
    a.Path("warm",254,152,262,148,268,142);
    // A few grouped sparks replace hundreds of isolated source noise dots.
    a.Rect("goldShade",258,115,2,3);a.Rect("gold",260,114,1,2);
    a.Rect("light",287,105,2,2);a.Rect("ochre",301,115,2,3);
    a.Rect("gold",294,146,2,2);a.Line("goldShade",275,158,279,156);
    a.Rect("gold",307,136,2,1);a.Rect("goldShade",246,126,2,2);
    a.Rect("ochre",213,168,2,2);a.Line("gold",222,175,225,172);
    a.Line("goldShade",286,151,289,150);a.Rect("light",297,100,1,2);
  }
  static void Body(Canvas a){
    // Coat silhouette: one-pixel contour with separate cyan and talisman-lit edges.
    a.Poly("ink",9,249,7,230,7,215,11,201,17,194,29,186,50,178,65,170,73,156,84,148,95,157,112,170,129,177,138,176,146,180,161,185,177,192,182,199,184,210,187,225,197,233,205,249);
    a.Poly("night",13,249,10,231,10,216,14,202,22,195,36,188,58,180,69,174,76,158,85,152,95,163,111,177,128,185,139,180,160,187,177,195,180,203,183,220,191,238,197,249);
    a.Poly("hair",14,214,22,201,31,198,42,202,54,218,66,249,15,249,11,235);
    a.Poly("cloth",16,202,29,192,48,185,60,181,69,190,78,211,76,225,68,231,58,214,43,202,30,198);
    a.Poly("indigo",14,207,21,203,29,203,41,214,48,228,59,242,62,249,45,249,36,237,23,232,16,219);
    a.Poly("ink",32,207,38,209,43,216,47,224,40,220,36,214);
    a.Poly("night",19,223,24,227,34,230,44,240,53,244,57,249,30,249,19,239);
    a.Path("slate",18,202,23,199,30,199,36,201);
    a.Poly("night",68,180,78,171,87,170,107,182,125,198,109,212,99,224,91,230,85,216,77,198);
    a.Poly("cloth",76,184,83,183,100,195,107,203,100,211,92,217,88,207);
    a.Poly("ink",61,183,66,183,78,205,88,228,90,249,83,249,80,227,69,204);
    a.Path("indigo",65,186,73,201,82,222,86,245);
    a.Poly("hair",138,182,148,185,145,192,145,209,158,216,140,226,119,239,102,249,90,249,104,233,116,214,126,194);
    a.Poly("cloth",142,185,149,189,158,191,168,196,172,207,166,212,157,213,147,207,146,195);
    a.Poly("ink",141,186,145,192,144,203,150,215,142,217,139,210,138,196);
    a.Poly("night",112,231,132,219,147,217,153,220,136,230,121,235,100,249,96,249);
    a.Path("ink",126,222,146,215,159,216,165,219);
    // Deliberate contour correction: cyan light is 1px, with 2px corner clusters.
    a.Path("cyan",12,249,10,242,9,230,9,216,12,204,16,197,27,190,46,182,63,174,69,165,74,155,83,150);
    a.Path("teal",15,240,13,230,13,216,16,205,22,198,32,193);
    // Remove the duplicate bright shoulder run; one cyan edge, a muted inner seam.
    a.Path("teal",34,190,42,188,50,185);
    a.Path("gold",139,178,145,182,161,187,176,194,180,199);
    a.Path("light",142,181,159,186,170,191);
    a.Path("gold",145,188,149,195,152,198);
    a.Path("ochre",148,187,154,196,159,199);
    a.Path("gold",199,231,202,239,205,249);a.Path("light",199,235,201,244);
  }
  static void NeckAndFace(Canvas a){
    a.Poly("ink",86,137,109,134,121,148,136,163,133,175,127,185,119,199,107,193,92,177,83,163,81,151);
    a.Poly("skinShade",88,140,107,137,120,150,131,163,130,176,120,196,108,187,96,175,85,161,84,152);
    a.Poly("skinRose",88,149,96,142,108,144,113,157,124,170,124,183,119,190,113,183,105,173,99,161,94,158);
    a.Poly("skinDark",108,143,120,153,131,165,129,177,125,181,124,171,115,160);
    a.Poly("skin",86,154,91,153,94,163,101,173,106,187,96,177,86,163);
    a.Path("skinLight",86,153,84,158,86,164);
    // Neck contour and collar overlap replace the source's fuzzy double boundaries.
    a.Path("skinDark",96,155,95,163,99,178,106,187);
    a.Path("skinShade",106,177,109,188,116,194);
    a.Poly("ink",78,150,82,153,85,166,98,181,117,196,121,199,117,206,101,197,88,184,78,172,75,161);
    a.Poly("linen",79,155,82,155,85,168,97,182,117,198,118,201,115,204,100,193,87,180,79,169,77,162);
    a.Poly("steel",78,155,80,158,81,169,91,182,101,191,108,198,99,192,86,179,78,168);
    a.Path("ice",80,158,83,168,97,183,117,199);
    a.Poly("ink",132,168,137,174,138,185,133,199,122,211,114,223,108,225,105,222,110,216,119,203,127,186);
    a.Poly("linen",133,172,135,177,134,189,128,200,118,211,113,222,109,223,107,221,113,213,123,199,130,183);
    a.Poly("steel",133,180,134,189,128,200,119,210,114,218,113,221,111,222,113,215,124,200,130,188);
    a.Path("gold",133,170,134,177,132,183);
    // Face is a new native polygon drawing, not a posterized skin sample.
    a.Poly("ink",101,105,112,108,122,103,135,99,148,94,160,87,164,90,165,98,171,108,172,119,168,125,169,130,172,136,173,138,171,140,164,142,165,147,162,150,162,155,156,166,153,168,148,168,138,163,126,154,118,147,113,139,109,131,103,131,98,126,95,119,95,112);
    a.Poly("skin",102,107,112,111,125,106,138,103,151,96,161,90,162,99,168,108,170,120,166,125,167,131,171,137,170,139,162,141,163,147,160,150,161,154,155,165,151,166,140,161,128,152,120,145,115,138,111,129,104,129,100,124,97,118,97,113);
    a.Poly("skinLight",123,113,143,105,158,101,165,110,167,122,164,126,166,132,170,137,161,140,161,146,158,150,159,155,154,163,150,164,141,160,132,154,126,146,123,136);
    a.Poly("skinShade",114,121,120,124,121,138,126,149,138,160,148,167,139,162,126,153,119,147,114,138,111,130,107,129,108,121);
    a.Poly("skinRose",159,92,161,91,162,99,164,104,159,103,153,102);
    a.Poly("skinShade",160,138,166,138,168,139,163,140,161,142,158,141);
    a.Line("skinDark",160,138,167,139);a.Line("warm",163,140,166,140);
    // Mouth and chin: a clean one-pixel profile, dark upper lip, small lit lower lip.
    a.Path("skinDark",149,149,153,148,161,148);
    a.Line("ink",152,148,161,148);a.Line("skinLight",154,150,159,150);
    a.Path("skinShade",160,152,158,156,155,158);
    a.Path("gold",168,128,171,136,171,138,164,141);
    a.Path("light",165,143,164,147);a.Path("gold",159,156,155,164,151,166);
  }
  static void Ear(Canvas a){
    // Draw above the swept hair mass; keep short locks over the top/right edge.
    // Ear follows the portrait's simple stepped cartilage, without photographic texture.
    a.Poly("ink",98,103,103,103,108,106,112,113,112,120,111,128,108,132,103,130,98,125,94,117,94,111);
    a.Poly("skin",99,105,103,105,107,108,110,114,110,122,108,129,104,128,100,123,97,117,97,111);
    a.Poly("skinRose",99,110,102,108,105,110,107,115,107,121,105,122,106,126,103,124,100,120,99,116);
    a.Path("skinDark",99,114,101,110,103,110,106,114,106,117,104,119,105,122);
    a.Path("skinLight",98,108,101,106,104,107,107,111);
    a.Path("skinShade",98,119,101,124,105,128,108,129);
    a.Poly("ink",103,103,109,105,112,109,113,118,111,124,110,116,108,110);
    a.Path("hair",108,105,110,108,111,113);
  }
  static void Hair(Canvas a){
    // Traced silhouette, then explicitly placed interlocking locks and shade clusters.
    a.Poly("ink",56,69,67,67,80,60,90,54,100,50,107,49,100,45,117,46,132,45,134,39,141,47,154,51,168,57,172,63,175,61,174,54,179,62,179,67,176,70,184,76,188,84,190,93,190,104,185,116,180,117,179,109,173,111,172,103,167,109,160,109,163,98,156,105,150,102,142,103,137,107,126,109,118,114,115,125,113,118,111,115,109,128,107,140,103,143,101,138,98,146,94,140,91,148,89,140,84,145,88,134,81,137,82,131,75,132,81,125,70,125,73,121,63,119,69,116,59,114,65,110,56,111,62,106,52,106,61,101,50,98,44,94,59,94,56,91,63,87,54,87,62,81,70,76,56,77,64,74);
    a.Poly("hair",58,69,70,67,85,58,100,53,115,49,131,48,141,51,155,55,167,61,171,66,159,62,144,58,123,57,108,61,90,65,77,71,64,73);
    a.Poly("hairLight",68,68,85,60,103,54,120,51,132,51,116,54,103,58,88,62,77,68);
    a.Poly("indigo",95,65,112,59,132,58,147,61,132,61,115,64,105,69,92,73,78,76);
    a.Poly("hair",58,84,70,77,91,70,108,66,128,62,145,63,155,65,136,65,119,71,107,78,86,85,70,87,64,87);
    a.Poly("hairLight",68,80,83,75,102,69,116,66,128,65,115,69,103,74,83,80,76,83,65,84);
    a.Poly("night",75,86,95,81,110,76,126,70,140,67,153,67,159,69,143,70,128,77,115,83,95,90,76,94,60,94,53,94,69,91);
    a.Poly("hair",59,98,75,96,94,92,115,86,130,79,144,73,160,71,165,73,151,76,141,83,128,90,110,96,94,99,75,101,65,101);
    a.Poly("hairLight",82,95,96,92,115,86,130,80,143,76,138,80,129,86,116,92,102,96,89,97);
    a.Poly("night",66,107,77,102,95,101,114,96,133,89,145,82,157,77,166,74,168,77,155,82,146,91,131,99,117,102,102,101,93,103,79,110);
    a.Poly("hair",104,101,116,100,133,94,147,86,160,79,167,78,163,84,156,91,149,97,137,102,125,105,117,109,114,113,111,109);
    a.Poly("hairLight",121,101,134,96,147,89,155,83,159,82,151,91,142,98,134,101,125,103);
    a.Poly("hair",78,112,91,106,97,106,94,110,91,118,86,127,83,127,85,119,78,123,76,121,80,116,73,117);
    a.Poly("night",90,126,94,128,100,134,99,139,96,137,92,143,92,135,88,138);
    a.Poly("hair",112,111,117,108,118,112,116,118,115,114,114,117);
    a.Poly("hair",166,81,169,79,172,82,171,94,167,103,163,106,166,96);
    a.Poly("hairLight",168,84,169,83,169,91,166,100,167,92);
    a.Poly("night",175,76,181,81,185,89,187,98,186,107,182,113,182,104,180,93);
    a.Poly("hair",178,79,183,85,186,94,186,102,183,107,183,96);
    a.Path("ochre",176,84,179,91,180,101,178,108);
    a.Path("gold",180,84,183,91,184,99,183,104);
    a.Path("goldShade",171,82,174,91,173,99);
    // Silhouette cleanup: fixed runs, connected cyan stair steps, no random flyaways.
    a.Path("cyan",56,69,62,70,70,69,84,62,97,55,111,51,120,50);
    a.Path("teal",64,72,73,72,85,66,98,60);
    a.Path("cyan",55,86,63,85,72,80);a.Path("teal",62,88,72,85,80,82);
    a.Path("cyan",45,94,52,96,61,97,67,96);
    a.Path("teal",50,99,59,101,63,100);
    a.Path("cyan",55,106,62,106,65,105);a.Path("teal",60,111,67,110);
    a.Path("cyan",64,115,71,116,74,115);
    a.Path("cyan",73,122,79,122,77,125);a.Path("teal",81,130,85,127);
    a.Path("cyan",85,136,89,132,88,139);a.Path("teal",91,143,93,137);
  }
  static void Goggles(Canvas a){
    // Strap and a single readable side lens, shaded in the portrait's teal family.
    a.Poly("ink",118,105,127,102,136,103,141,107,139,114,129,113,120,111);
    a.Poly("tealDark",121,105,128,104,134,104,137,107,136,111,127,110,121,109);
    a.Line("teal",124,105,127,105);a.Line("cyan",123,107,127,107);a.Line("teal",123,109,126,109);
    a.Poly("ink",135,102,145,102,157,105,161,108,165,107,172,109,176,113,176,121,172,128,167,131,161,129,157,128,153,130,144,130,136,127,131,122,130,113,132,106);
    a.Poly("tealDark",135,104,144,104,156,107,161,111,165,109,171,111,174,114,174,121,171,126,167,129,160,126,155,128,144,128,137,125,133,121,132,113,133,108);
    a.Poly("teal",136,105,144,105,155,108,162,113,166,111,171,113,172,120,169,126,166,127,160,124,154,127,144,127,138,124,135,120,134,113);
    a.Poly("cyan",136,106,144,106,155,109,162,114,166,112,171,114,169,116,166,115,163,117,160,116,153,112,144,109,138,109,137,113,137,120,140,123,149,125,155,125,154,127,144,127,138,124,135,120,134,113);
    a.Poly("tealDark",139,111,145,111,153,114,159,117,156,121,152,124,146,123,141,120,139,117);
    a.Poly("teal",140,114,145,114,153,116,157,119,153,122,148,121,144,119);
    // One sharp eye and brow, no luminous eye or smudged photographic detail.
    a.Path("ink",141,112,146,113,152,116,156,117);
    a.Path("slate",145,115,148,119,152,121,155,121);
    a.Path("ice",146,116,150,118,154,119);
    a.Rect("ink",151,117,1,3);a.Dot(152,120,"ink");
    a.Poly("ice",137,106,142,106,151,109,151,110,141,108,138,109,137,115,136,115);
    a.Path("cyan",138,121,141,124,148,126,154,126);
    a.Path("ice",167,112,171,114,170,119,168,123);
    a.Path("cyan",173,115,173,121,170,126);
    // Exact pixel cleanup of the bridge and nose-facing rim, one connected silhouette.
    a.Rect("ink",160,111,2,2);a.Dot(162,112,"cyan");a.Dot(163,113,"cyan");
    a.Line("tealDark",165,119,164,123);a.Dot(165,124,"cyan");
  }
  static void HandAndSleeve(Canvas a){
    // Index/middle fingers form a clean flicking gesture; three curled silhouettes.
    a.Poly("ink",165,217,175,207,184,195,189,186,193,179,193,168,195,166,199,167,201,171,201,178,199,181,206,177,215,174,225,165,233,157,239,151,242,149,245,150,247,153,245,158,240,165,233,175,223,188,232,189,236,191,238,195,237,200,238,204,236,210,232,216,228,218,224,217,223,214,227,209,220,211,214,216,207,219,199,224,191,236,184,232,177,227);
    a.Poly("skin",168,217,177,207,186,195,191,185,195,179,195,169,196,168,198,169,199,173,199,179,196,185,207,180,216,177,227,166,235,157,241,152,243,151,245,153,243,158,238,165,231,175,220,189,230,191,234,192,236,195,235,200,236,205,233,211,229,215,226,216,225,214,229,208,222,209,216,214,207,217,199,223,191,233,185,230,179,225);
    a.Poly("skinLight",174,215,182,205,190,196,199,187,211,181,221,176,231,166,239,157,243,152,244,154,240,161,233,171,220,186,218,191,222,192,231,193,234,196,233,198,225,197,215,194,207,198,198,207,191,219,185,226);
    a.Poly("skinShade",190,231,199,221,208,215,217,211,224,205,230,204,234,206,232,212,228,215,226,214,230,208,221,211,215,216,207,219,199,224,192,234);
    a.Poly("skinRose",194,225,199,219,206,215,215,210,221,204,226,202,231,202,233,204,225,204,218,210,209,214,201,222,195,229);
    a.Poly("skinLight",195,170,197,170,198,174,198,180,195,183,196,177);
    a.Path("skinShade",191,194,199,185,207,182,216,178);
    a.Path("skinRose",198,196,203,191,211,187,217,180);
    a.Path("skinShade",207,189,218,181,228,171,237,160);
    a.Path("skinDark",221,190,226,192,234,193);
    a.Path("skinDark",218,198,222,201,229,202,234,203);
    a.Path("skinShade",204,202,209,201,212,201);
    a.Path("skinRose",210,198,214,198);a.Line("skinLight",225,193,231,194);
    a.Path("light",236,160,241,153,243,152);a.Path("gold",225,173,232,165);
    a.Path("gold",198,168,199,171,199,178);a.Path("light",219,190,229,191,234,193);
    // Nail cluster follows the index direction, no extra finger tips.
    a.Poly("skinShade",239,153,242,151,244,152,244,154,241,158,239,158);
    a.Path("skinLight",240,153,242,152,243,153,242,155);
    // The cuff overlaps the wrist with a continuous one-pixel dark boundary.
    a.Poly("ink",134,249,135,237,145,228,157,222,165,216,172,218,181,226,187,235,193,249);
    a.Poly("night",138,249,138,238,148,230,160,224,166,219,171,221,179,228,184,236,190,249);
    a.Poly("hair",140,239,149,232,161,226,168,223,172,225,176,231,168,236,157,240,147,247,144,249,137,249);
    a.Poly("indigo",145,240,154,234,164,229,169,226,172,229,164,234,154,238,144,246,140,249,138,249);
    a.Path("ink",166,218,171,220,179,227,185,235,191,249);
    a.Path("slate",170,223,177,229,181,235);
    a.Path("skinDark",173,219,179,224,184,229,190,233);
  }
  public static void Run(string root){
    Palette();Check(P.Count==32,"Expected a 32-colour working palette");
    string dir=Path.Combine(root,"docs/itch/cover_pixel");Directory.CreateDirectory(dir);
    string sourcePath=Path.Combine(root,"docs/itch/cover_refs/gemini_cover_v7b_full.png");
    using(var source=new Bitmap(sourcePath))Check(source.Width==1024&&source.Height==825,"Unexpected reference dimensions");
    var a=new Canvas(W,H,P["deep"]);
    Background(a);Talisman(a);Body(a);NeckAndFace(a);Hair(a);Ear(a);Goggles(a);HandAndSleeve(a);
    // This deliberately contains no resize/quantize step: every visible artwork pixel
    // comes from the integer contours, fills and coordinate edits above. Gemini is
    // the composition/pose reference, not a raster fed through a conversion filter.
    a.Save(Path.Combine(dir,"cover_pixel_native_nologo_315x250.png"));
    var logoVersion=a.Copy();
    using(var logo=new Bitmap(Path.Combine(root,"Assets/Resources/DraftUi/logo_number.png"))){
      Check(logo.Width==200&&logo.Height==44,"Unexpected logo dimensions");
      const int ox=(W-200)/2,oy=4; // odd 115px remainder: 57px left, 58px right.
      for(int y=0;y<logo.Height;y++)for(int x=0;x<logo.Width;x++){
        var c=logo.GetPixel(x,y);Check(c.A==0||c.A==255,"Logo alpha must be binary");
        if(c.A==255){Check(P.Values.Contains(c.ToArgb()),"Unlisted logo colour");logoVersion.Put(ox+x,oy+y,c.ToArgb());}
      }
      for(int y=0;y<logo.Height;y++)for(int x=0;x<logo.Width;x++)if(logo.GetPixel(x,y).A==255)Check(logoVersion.Get(ox+x,oy+y)==logo.GetPixel(x,y).ToArgb(),"Logo pixels changed");
    }
    var scaled=logoVersion.Scale(2);
    logoVersion.Save(Path.Combine(dir,"cover_pixel_native_315x250.png"));
    scaled.Save(Path.Combine(dir,"cover_pixel_630x500.png"));
    var allowed=new HashSet<int>(P.Values);
    foreach(var pic in new[]{a,logoVersion,scaled})foreach(int c in pic.Pixels){Check((uint)c>>24==255,"Non-opaque output");Check(allowed.Contains(c),"Off-palette pixel");}
    Check(scaled.Width==630&&scaled.Height==500,"Wrong 2x dimensions");
    for(int y=0;y<500;y++)for(int x=0;x<630;x++)Check(scaled.Get(x,y)==logoVersion.Get(x/2,y/2),"Non-nearest pixel");
    // Round-trip file checks verify the exported PNGs rather than only memory buffers.
    string[] names={"cover_pixel_native_nologo_315x250.png","cover_pixel_native_315x250.png","cover_pixel_630x500.png"};
    Canvas[] pics={a,logoVersion,scaled};
    for(int i=0;i<names.Length;i++)using(var b=new Bitmap(Path.Combine(dir,names[i]))){Check(b.Width==pics[i].Width&&b.Height==pics[i].Height,"Export dimensions");for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)Check(b.GetPixel(x,y).ToArgb()==pics[i].Get(x,y),"PNG round-trip mismatch");}
    int count=a.Pixels.Distinct().Count(),full=logoVersion.Pixels.Distinct().Count();
    string palette=String.Join(", ",logoVersion.Pixels.Distinct().OrderBy(c=>(uint)c).Select(c=>"#"+(c&0xffffff).ToString("X6")).ToArray());
    string readme="# Itch cover pixel redraw - REVIEW DRAFT\n\n"+
      "Native 315x250 artwork; 630x500 is an exact 2x nearest-neighbour enlargement. Draft only, not installed or published.\n\n"+
      "Method: local PowerShell/C# with Windows System.Drawing for PNG I/O only. The Gemini source (`../cover_refs/gemini_cover_v7b_full.png`) was inspected as the composition/pose reference. All artwork was redrawn using explicit integer-coordinate polygons, Bresenham outlines, filled clusters and pixel discs on a blank native canvas. No source downscale, palette-quantization filter, antialiasing, blur, alpha glow, image-generation API, packages or paid service.\n\n"+
      "Palette: "+count+" colours without logo / "+full+" with logo (32-colour working palette). Colours come from the fixed project palette, existing key-art magenta/gold accents and Yudo portrait-related skin/hair shades; one pale yellow highlight was added. All pixels are opaque.\n\n"+
      "Hand-authored cleanup: hair silhouette and interlocking locks; connected 1px cyan rim; ear cartilage; face/profile, nose, mouth and chin; goggle strap, lens border, bridge and single readable eye; neck/collar seam; broad coat folds; finger and nail contours, curled fingers and wrist/cuff overlap; curved giwa eaves and tile courses; abstract red brush strokes replacing the talisman writing; flat-band talisman glow, sparse grouped sparks, pixel-disc bokeh and consistently sloped rain. Source texture/noisy pixels were omitted rather than inherited.\n\n"+
      "Logo: original 200x44 `Assets/Resources/DraftUi/logo_number.png`, unchanged opaque pixels, native 1x at (57,4). The odd canvas width leaves 57px left / 58px right, the closest possible integer-grid centring. Top sky is deliberately dark.\n\n"+
      "Rebuild from repository root:\n```powershell\npowershell -NoProfile -ExecutionPolicy Bypass -File docs/itch/cover_pixel/generate_cover.ps1\n```\n\n"+
      "Checks: exported dimensions and lossless PNG round trip; original logo pixels and binary source alpha; palette membership; opaque output; every pixel of all 630x500 2x2 blocks equals its native pixel. Visual review at native and integer enlargement.\n\n"+
      "Provenance: AI generation - Google Gemini source, Codex gpt-6.1-sol local pixel redraw. Original logo reused from the game.\n\n"+
      "Composite palette: "+palette+".\n";
    File.WriteAllText(Path.Combine(dir,"README.md"),readme,new UTF8Encoding(false));
    Console.WriteLine("PASS: 315x250 native, 630x500 exact 2x, unchanged 1x logo at (57,4), opaque; palette "+count+" / "+full+" colours; exported PNGs verified.");
  }
}
'@
Add-Type -TypeDefinition $source -ReferencedAssemblies System.Drawing
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
[CoverPixelDraft]::Run($repoRoot)
