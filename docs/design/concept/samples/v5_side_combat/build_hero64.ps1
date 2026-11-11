param([ValidateSet('b')][string]$Best = 'b')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = $PSScriptRoot
# All artwork comes from image_gen. This script only samples, quantizes and translates it.
# Fixed 24-colour material ramps, capped highlights; no dithering or partial alpha.
$hex = @('101321','1c2030','2c3042','414459','565970',
 '171f39','253252','384666','4c5975',
 '694f50','946d61','a78370','b59179',
 '535564','777984','8b918f','989e99',
 '244b59','346675','4f8b96','77a8ad',
 '282633','393643','53505b')
$palette = @($hex | ForEach-Object { [Drawing.ColorTranslator]::FromHtml('#'+$_) })
function New-Bitmap([int]$w,[int]$h) { return [Drawing.Bitmap]::new($w,$h,[Drawing.Imaging.PixelFormat]::Format32bppArgb) }
function Save-Png($b,[string]$name) { $b.Save((Join-Path $root $name),[Drawing.Imaging.ImageFormat]::Png) }
function Is-Background($c) {
 # Navy key deliberately excludes neutral dark hair and outline colours.
 return ($c.A -eq 0 -or ($c.R -lt 23 -and $c.G -lt 28 -and $c.B -ge 32 -and $c.B -le 65 -and ($c.B-$c.G) -ge 17))
}
function Nearest-Colour($c) {
 $bestDistance = [double]::MaxValue; $chosen = $palette[0]
 foreach($p in $palette) {
  $d = 2*[Math]::Pow(($c.R-$p.R),2)+3*[Math]::Pow(($c.G-$p.G),2)+[Math]::Pow(($c.B-$p.B),2)
  if($d -lt $bestDistance) { $bestDistance=$d; $chosen=$p }
 }
 return $chosen
}
function Remove-Orphans($b) {
 $remove = [Collections.Generic.List[Drawing.Point]]::new()
 for($y=0;$y -lt $b.Height;$y++) { for($x=0;$x -lt $b.Width;$x++) {
  if($b.GetPixel($x,$y).A -eq 0) {continue}
  $n=0
  for($dy=-1;$dy -le 1;$dy++) { for($dx=-1;$dx -le 1;$dx++) {
   if(($dx -eq 0 -and $dy -eq 0) -or $x+$dx -lt 0 -or $x+$dx -ge $b.Width -or $y+$dy -lt 0 -or $y+$dy -ge $b.Height) {continue}
   if($b.GetPixel(($x+$dx),($y+$dy)).A -gt 0) {$n++}
  }}
  if($n -eq 0) {$remove.Add([Drawing.Point]::new($x,$y))}
 }}
 foreach($p in $remove) {$b.SetPixel($p.X,$p.Y,[Drawing.Color]::Transparent)}
 return $remove.Count
}
function Make-Native([string]$take) {
 $source = [Drawing.Bitmap]::new((Join-Path $root ('hero_gen64_big_'+$take+'.png')))
 $minX=$source.Width; $minY=$source.Height; $maxX=0; $maxY=0
 # Coarse scan excludes isolated background noise, followed by a padded crop.
 for($y=0;$y -lt $source.Height;$y+=2) { for($x=0;$x -lt $source.Width;$x+=2) {
  if(!(Is-Background ($source.GetPixel($x,$y)))) {
   $minX=[Math]::Min($minX,$x);$maxX=[Math]::Max($maxX,$x)
   $minY=[Math]::Min($minY,$y);$maxY=[Math]::Max($maxY,$y)
  }
 }}
 $height=$maxY-$minY+2; $width=$maxX-$minX+2
 $targetH=64; $targetW=[int][Math]::Round($width*64.0/$height)
 if($targetW -gt 76) {throw 'Sprite crop is too wide; inspect the source/key.'}
 $native=New-Bitmap 80 80; $left=[int][Math]::Floor((80-$targetW)/2); $top=8
 for($y=0;$y -lt $targetH;$y++) { for($x=0;$x -lt $targetW;$x++) {
  # 3x3 stratified majority samples. Select most frequent quantized opaque colour;
  # require majority foreground coverage, so no averaged edge colour/halo is invented.
  $votes=@{}; $opaque=0; $crack=$null
  foreach($sy in @(0.2,0.5,0.8)) {foreach($sx in @(0.2,0.5,0.8)) {
   $ix=[Math]::Min($source.Width-1,[int][Math]::Floor($minX+($x+$sx)*$width/$targetW))
   $iy=[Math]::Min($source.Height-1,[int][Math]::Floor($minY+($y+$sy)*$height/$targetH))
   $c=$source.GetPixel($ix,$iy)
   if(!(Is-Background $c)) {
    $opaque++; $q=(Nearest-Colour $c).ToArgb()
    if($y -lt 22 -and $c.G -gt 100 -and $c.G-$c.R -gt 17 -and $c.B-$c.R -gt 17) {$crack=Nearest-Colour $c}
    if(!$votes.ContainsKey($q)) {$votes[$q]=0};$votes[$q]++
   }
  }}
  if($opaque -ge 5) {
   $winner=$votes.GetEnumerator() | Sort-Object @{Expression='Value';Descending=$true},Name | Select-Object -First 1
   $chosen=[Drawing.Color]::FromArgb([int]$winner.Key)
   if($null -ne $crack) {$chosen=$crack}
   $native.SetPixel(($left+$x),($top+$y),$chosen)
  }
 }}
 $null=Remove-Orphans $native; $source.Dispose(); return $native
}
function Make-Breath($b,[int]$shift) {
 $frame=New-Bitmap 80 80; $moving=[bool[,]]::new(80,80)
 # Take B garment segmentation: upper body, rear glove/flared hem, and cyan ties.
 # These are source-selection masks, not replacement artwork. No leg pixels move.
 for($y=0;$y -lt 80;$y++) {for($x=0;$x -lt 80;$x++) {
  $c=$b.GetPixel($x,$y)
  $tie=($y -lt 55 -and $c.G-$c.R -gt 16 -and $c.B-$c.R -gt 16)
  $moving[$x,$y]=($y -lt 43 -or ($y -lt 50 -and $x -lt 37) -or $tie)
  if(!$moving[$x,$y]) {$frame.SetPixel($x,$y,$c)}
 }}
 # Exposed pixels behind moving ties inherit the nearest stationary source pixel.
 for($y=43;$y -lt 55;$y++) {for($x=0;$x -lt 80;$x++) {
  if(!$moving[$x,$y] -or $b.GetPixel($x,$y).A -eq 0) {continue}
  for($radius=1;$radius -lt 12;$radius++) {
   $found=$false
   foreach($nx in @(($x-$radius),($x+$radius))) {
    if($nx -ge 0 -and $nx -lt 80 -and !$moving[$nx,$y]) {
     $frame.SetPixel($x,$y,$b.GetPixel($nx,$y));$found=$true;break
    }
   }
   if($found){break}
  }
 }}
 for($y=0;$y -lt 79;$y++) {for($x=0;$x -lt 80;$x++) {
  $c=$b.GetPixel($x,$y)
  if($moving[$x,$y] -and $c.A -gt 0 -and $y+$shift -ge 0) {$frame.SetPixel($x,($y+$shift),$c)}
 }}
 # Close only the one-row attachment seam exposed by an upward translation.
 if($shift -lt 0) {for($x=37;$x -lt 80;$x++) {
  if($frame.GetPixel($x,42).A -eq 0 -and $frame.GetPixel($x,41).A -gt 0 -and $frame.GetPixel($x,43).A -gt 0) {
   $frame.SetPixel($x,42,$b.GetPixel($x,43))
  }
 }}
 $null=Remove-Orphans $frame; return $frame
}
function Blit-Nearest($dest,$src,[int]$left,[int]$top,[int]$scale) {
 for($y=0;$y -lt $src.Height;$y++) {for($x=0;$x -lt $src.Width;$x++) {
  $c=$src.GetPixel($x,$y);if($c.A -eq 0){continue}
  for($dy=0;$dy -lt $scale;$dy++){for($dx=0;$dx -lt $scale;$dx++) {
   $dest.SetPixel(($left+$x*$scale+$dx),($top+$y*$scale+$dy),$c)
  }}
 }}
}
function Navy-Canvas([int]$w,[int]$h) {
 $b=New-Bitmap $w $h; $g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.ColorTranslator]::FromHtml('#0b1030'));$g.Dispose();return $b
}
$takes=@{};foreach($take in @('a','b','c')) {$takes[$take]=Make-Native $take}
$frames=@($takes[$Best],(Make-Breath $takes[$Best] -1),(Make-Breath $takes[$Best] 1))
for($i=0;$i -lt 3;$i++) {Save-Png $frames[$i] ('hero_idle_'+$i+'.png')}
$alt=0;foreach($take in @('a','b','c')) {if($take -ne $Best) {Save-Png $takes[$take] ('hero_idle_alt_'+@('a','b')[$alt]+'.png');$alt++}}
foreach($scale in @(4,8)) {
 $preview=Navy-Canvas (240*$scale) (80*$scale)
 for($i=0;$i -lt 3;$i++) {Blit-Nearest $preview $frames[$i] ($i*80*$scale) 0 $scale}
 Save-Png $preview ('hero_idle_frames_x'+$scale+'.png');$preview.Dispose()
}
$strip=Navy-Canvas 480 270;Blit-Nearest $strip $frames[0] 85 140 1;Blit-Nearest $strip $frames[0] 250 68 2
Save-Png $strip 'hero_idle_scale.png';$strip.Dispose()
$report=[Collections.Generic.List[string]]::new()
$report.Add('AI generated (Codex / image_gen)')
$report.Add("Selected source: hero_gen64_big_$Best.png; alternate order: remaining sources alphabetically.")
$report.Add('Canvas 80x80; palette cap 24; binary alpha; majority sampling without dithering.')
$report.Add('Playback: 0,1,2,1. Upper body/hem/tie offsets: 0,-1,+1 px. Fixed lower legs y>=56; upper trousers retain source coordinates underneath translated clothing.')
foreach($name in @('hero_idle_0.png','hero_idle_1.png','hero_idle_2.png','hero_idle_alt_a.png','hero_idle_alt_b.png')) {
 $b=[Drawing.Bitmap]::new((Join-Path $root $name));$colours=@{};$badAlpha=0;$lo=80;$hi=-1
 for($y=0;$y -lt 80;$y++){for($x=0;$x -lt 80;$x++) {
  $c=$b.GetPixel($x,$y);if($c.A -ne 0 -and $c.A -ne 255){$badAlpha++}
  if($c.A -gt 0){$colours[$c.ToArgb()]=1;$lo=[Math]::Min($lo,$y);$hi=[Math]::Max($hi,$y)}
 }}
 $orphans=Remove-Orphans $b
 if($colours.Count -gt 24 -or $badAlpha -ne 0 -or $orphans -ne 0) {throw "Native validation failed: $name"}
 $report.Add("$name : colours=$($colours.Count); nonbinary_alpha=$badAlpha; orphan_8connected=$orphans; height=$($hi-$lo+1)px")
 $b.Dispose()
}
for($i=1;$i -lt 3;$i++) {
 $diff=0;for($y=56;$y -lt 80;$y++){for($x=0;$x -lt 80;$x++){if($frames[0].GetPixel($x,$y).ToArgb() -ne $frames[$i].GetPixel($x,$y).ToArgb()){$diff++}}}
 $report.Add("Frame $i fixed legs/feet changed pixels: $diff")
 if($diff -ne 0){throw 'Leg invariance failed'}
}
$report.Add('Preview panels: x4=960x320; x8=1920x640; scale=480x270 (left 1x, right 2x), background #0b1030.')
$report.Add('Orphan definition: opaque pixel with no opaque 8-connected neighbour. Alpha count excludes fully transparent RGB.')
$report.Add('Visual review: B chosen for narrow profile and asymmetric arms. Rear-knee flexion remains subtle under cargo trousers; shoulder highlight is stronger than ideal. Large generated sources are not mathematically uniform pixel grids; native outputs are exact integer pixels.')
$report | Set-Content -Encoding UTF8 (Join-Path $root 'validation.txt')
$report
foreach($b in $takes.Values){$b.Dispose()};$frames[1].Dispose();$frames[2].Dispose()


