$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$out = $PSScriptRoot
# Deliberate native pixel clusters. No rejected source files are used.
$hex = @('151c30','253249','384b68','566785','505c60','768078','9b9a81','775047','ac7960','c19773','263e45','42636a','71a4a6','79594c','a77c54','b4aa8b')
$pal = @($hex | ForEach-Object { [Drawing.ColorTranslator]::FromHtml('#'+$_) })
function Poly($index, $xy) {
  $points = @(); for($i=0;$i -lt $xy.Count;$i+=2){$points += [Drawing.Point]::new($xy[$i],$xy[$i+1])}
  $brush = [Drawing.SolidBrush]::new($pal[$index]); $g.FillPolygon($brush,[Drawing.Point[]]$points); $brush.Dispose()
}
function Rect($c,$x,$y,$w,$h){$brush=[Drawing.SolidBrush]::new($pal[$c]);$g.FillRectangle($brush,$x,$y,$w,$h);$brush.Dispose()}
function Dot($c,$x,$y){$b.SetPixel($x,$y,$pal[$c])}
function Save($bmp,$name){$bmp.Save((Join-Path $out $name),[Drawing.Imaging.ImageFormat]::Png)}
function Blit($dest,$src,$ox,$oy,$scale){
 for($y=0;$y -lt $src.Height;$y++){for($x=0;$x -lt $src.Width;$x++){
  $c=$src.GetPixel($x,$y);if($c.A -eq 0){continue}
  for($dy=0;$dy -lt $scale;$dy++){for($dx=0;$dx -lt $scale;$dx++){$dest.SetPixel($ox+$x*$scale+$dx,$oy+$y*$scale+$dy,$c)}}
 }}
}
$frames=@()
for($frame=0;$frame -lt 3;$frame++){
 $b=[Drawing.Bitmap]::new(48,48);$g=[Drawing.Graphics]::FromImage($b)
 $g.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::None
 # Fixed broad baji: rear knee bends back; front shin slopes forward.
 Poly 0 @(17,25,24,26,27,30,28,35,32,37,32,39,24,39,22,35,20,31,16,33,15,36,18,37,18,39,9,39,9,37,11,34,10,30)
 Poly 4 @(17,26,22,28,17,31,14,35,11,35,12,31)
 Poly 5 @(17,26,20,28,15,31,13,34,11,34,12,30)
 Poly 6 @(17,27,19,28,14,31,12,32,13,29)
 Poly 4 @(22,27,25,29,27,34,24,35,21,31)
 Poly 5 @(22,27,24,29,26,33,23,33,20,29)
 Rect 13 11 35 4 2;Rect 14 11 35 3 1;Dot 14 10 36
 Rect 13 24 34 4 2;Rect 14 24 34 3 1;Dot 14 28 35
 Rect 1 10 37 7 1;Rect 3 10 37 3 1;Rect 1 25 37 6 1;Rect 3 25 36 3 1
 # Split, below-knee durumagi skirt. Three indigo values with lit left rim.
 $hem=if($frame -eq 2){1}else{0}
 Poly 0 @(16,21,23,21,25,27,29,32,24,34,20,29,17,33,9,34,11,29)
 Poly 1 @(17,22,22,23,21,28,17,32,10,33,13,28)
 Poly 2 @(16,23,20,24,17,29,14,32,10,33,12,29)
 Poly 3 @(15,24,16,25,13,30,10,32,12,28)
 Poly 2 @(22,24,24,28,27,32,24,32,21,28)
 Rect 3 (10+$hem) 32 4 1
 # Pale gathered trousers emerge beneath the split coat, not thin dark sticks.
 Poly 5 @(15,31,18,30,16,33,14,35,11,35,12,33)
 Poly 6 @(15,31,17,31,14,34,12,34)
 Poly 5 @(23,31,25,31,27,34,24,34)
 Dot 6 24 31
 # Upper-body breathing: shoulders drop one pixel on frame 2; head stays stable.
 $s=if($frame -gt 0){1}else{0}
 Poly 0 @(18,(15+$s),24,16,25,21,23,25,16,24,13,20)
 Poly 2 @(18,(16+$s),23,17,23,22,19,24,15,21,15,18)
 Poly 3 @(17,(16+$s),20,16,18,19,15,20,15,18)
 # Pale overlapping collar, side-on (no symmetrical V).
 Poly 6 @(23,16,25,16,23,21,20,23,20,21)
 Poly 15 @(23,16,24,16,22,20,21,20)
 # Far arm forward with bell sleeve and warm fist.
 Poly 0 @(23,(17+$s),26,18,28,17,30,17,31,19,28,22,25,23,23,21)
 Poly 1 @(24,18,26,19,29,18,29,20,26,22,24,21)
 Poly 3 @(25,18,26,19,28,18,28,19,26,20)
 Rect 6 28 18 2 3;Rect 8 30 17 3 2;Dot 9 31 16;Dot 9 32 17;Dot 7 32 19
 # Near arm folds back to hip: broad hanging sleeve, small readable hand.
 Poly 0 @(17,(17+$s),20,19,19,22,17,24,12,23,12,20,14,18)
 Poly 2 @(16,(18+$s),18,19,17,22,13,22,13,20)
 Poly 3 @(15,(18+$s),17,(19+$s),15,(20+$s),13,(21+$s),14,(19+$s))
 Rect 6 13 22 4 1;Rect 4 13 23 3 1
 Rect 8 16 23 3 2;Dot 9 18 23;Dot 7 18 25
 # Ochre goreum contrasts with indigo, with restrained teal secondary thread.
 Rect 13 19 23 5 2;Rect 14 20 23 4 1
 Rect 14 21 25 1 4;Rect 13 (22+$hem) 25 1 5;Dot 12 20 24
 # Hood, true profile: back skull left, one dark lens and nose right.
 Poly 0 @(21,7,27,7,29,9,29,11,31,12,30,15,26,17,21,16,19,13,19,10)
 Poly 1 @(21,8,27,8,28,10,26,13,25,16,21,15,20,12,20,10)
 Poly 2 @(21,8,26,8,27,9,24,9,22,11,21,14,20,12,20,10)
 Rect 3 22 7 4 1;Dot 3 21 8
 # Open warm face: nose, lower lip, jaw, neck.
 Poly 7 @(26,10,29,10,30,12,32,13,30,14,30,16,27,17,25,15)
 Rect 8 27 11 3 4;Rect 9 29 12 2 2;Dot 9 31 13
 Rect 8 27 15 3 1;Rect 7 27 16 2 1;Dot 0 30 14
 Rect 0 24 10 6 2;Rect 10 26 10 4 2;Dot 11 26 10
 Dot 12 28 10;Dot 12 29 11
 Dot 8 25 13;Dot 9 26 13
 $g.Dispose();Save $b ('hero_idle_'+$frame+'.png');$frames+=,$b
}
# Large pixel-source study, not a separately generated concept painting.
$big=[Drawing.Bitmap]::new(768,768);Blit $big $frames[0] 0 0 16;Save $big 'hero_idle_big_source.png';$big.Dispose()
foreach($scale in @(4,8)){
 $width=48*$scale*3;$height=48*$scale+270
 $sheet=[Drawing.Bitmap]::new($width,$height);$sg=[Drawing.Graphics]::FromImage($sheet);$sg.Clear([Drawing.ColorTranslator]::FromHtml('#0b1030'))
 $bg=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#141c35'));$sg.FillRectangle($bg,0,48*$scale,480,270);$bg.Dispose()
 $bg=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#29344a'));$sg.FillRectangle($bg,0,48*$scale+207,480,2);$bg.Dispose();$sg.Dispose()
 for($f=0;$f -lt 3;$f++){Blit $sheet $frames[$f] ($f*48*$scale) 0 $scale}
 Blit $sheet $frames[0] 216 (48*$scale+168) 1
 Save $sheet ('hero_idle_frames_x'+$scale+'.png');$sheet.Dispose()
}
$report=@('AI generated (Codex / PowerShell System.Drawing)','Canvas: 48x48. Playback: 0,1,2,1. Point filter; integer scaling; PPU 16.','Orphan definition: opaque pixel with no opaque neighbor in its 8-neighborhood.','Big source: 16x exact pixel layout of frame 0, generated from the editable drawing instructions above.')
for($f=0;$f -lt 3;$f++){
 $b=$frames[$f];$colors=@{};$alpha=@{};$orphan=0;$minY=48;$maxY=-1;$diff=0
 for($y=0;$y -lt 48;$y++){for($x=0;$x -lt 48;$x++){
  $c=$b.GetPixel($x,$y);$alpha[[int]$c.A]=1
  if($y -ge 34 -and $c.ToArgb() -ne $frames[0].GetPixel($x,$y).ToArgb()){$diff++}
  if($c.A -eq 0){continue};$colors[$c.ToArgb()]=1;$minY=[Math]::Min($minY,$y);$maxY=[Math]::Max($maxY,$y)
  $neighbors=0;foreach($dy in -1..1){foreach($dx in -1..1){if(($dx -eq 0 -and $dy -eq 0) -or $x+$dx -lt 0 -or $x+$dx -ge 48 -or $y+$dy -lt 0 -or $y+$dy -ge 48){continue};if($b.GetPixel($x+$dx,$y+$dy).A -gt 0){$neighbors++}}};if($neighbors -eq 0){$orphan++}
 }}
 $report += "Frame ${f}: opaque colors=$($colors.Count); alpha=$((@($alpha.Keys)|Sort-Object)-join ','); orphan pixels=$orphan; height=$($maxY-$minY+1) px; fixed lower-leg/foot region differences=$diff"
 if($colors.Count -gt 16 -or @($alpha.Keys | Where-Object {$_ -ne 0 -and $_ -ne 255}).Count -gt 0 -or $orphan -gt 0 -or $diff -gt 0){throw 'Validation failed'}
}
for($f=1;$f -lt 3;$f++){
 $changed=0;for($y=0;$y -lt 48;$y++){for($x=0;$x -lt 48;$x++){if($frames[$f].GetPixel($x,$y).ToArgb() -ne $frames[$f-1].GetPixel($x,$y).ToArgb()){$changed++}}}
 $report += "Adjacent frames $($f-1)->${f}: changed pixels=$changed"
 if($changed -eq 0){throw 'Duplicate animation frames'}
}
$report | Set-Content -Encoding UTF8 (Join-Path $out 'validation.txt');$report
$frames | ForEach-Object {$_.Dispose()}
