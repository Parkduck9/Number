$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=$PSScriptRoot
$palette=@{'.'=[Drawing.Color]::Transparent}
foreach($line in Get-Content (Join-Path $root 'hero_palette.txt')){
 if($line -match '^([A-O]) #(\w{6})$'){$palette[$matches[1]]=[Drawing.ColorTranslator]::FromHtml('#'+$matches[2])}
}
if($palette.Count-1 -gt 15){throw 'Palette exceeds 15 opaque colors'}
$report=New-Object 'System.Collections.Generic.List[string]'
$report.Add('Ahn Yudo | 32x40 | loop 0,1,2,1 | upper-left light')
$report.Add(('Palette: {0} opaque colors + transparent' -f ($palette.Count-1)))
$frames=@();$grids=@()
for($f=0;$f -lt 3;$f++){
 $rows=@(Get-Content (Join-Path $root "hero_idle_$f.txt"))
 if($rows.Count -ne 40){throw "frame $f row count $($rows.Count)"}
 $bmp=New-Object Drawing.Bitmap 32,40
 $used=@{};$minX=32;$minY=40;$maxX=-1;$maxY=-1;$orphan=0;$highlights=0
 for($y=0;$y -lt 40;$y++){
  if($rows[$y].Length -ne 32){throw "frame $f row $y width $($rows[$y].Length)"}
  for($x=0;$x -lt 32;$x++){
   $key=[string]$rows[$y][$x]
   if(-not $palette.ContainsKey($key)){throw "Unknown palette char $key"}
   $color=$palette[$key]
   if($color.A -ne 0 -and $color.A -ne 255){throw 'Nonbinary alpha'}
   $bmp.SetPixel($x,$y,$color)
   if($key -eq '.'){continue}
   $used[$key]=$true
   $minX=[math]::Min($minX,$x);$maxX=[math]::Max($maxX,$x)
   $minY=[math]::Min($minY,$y);$maxY=[math]::Max($maxY,$y)
   # Eight-neighbor opaque connectivity: detached specks are orphans.
   $neighbors=0
   for($dy=-1;$dy -le 1;$dy++){for($dx=-1;$dx -le 1;$dx++){
    $nx=$x+$dx;$ny=$y+$dy
    if(($dx -ne 0 -or $dy -ne 0) -and $nx -ge 0 -and $nx -lt 32 -and $ny -ge 0 -and $ny -lt 40){
     if($rows[$ny][$nx] -ne '.'){$neighbors++}
    }
   }}
   if($neighbors -eq 0){$orphan++}
   if($key -eq 'N'){$highlights++}
  }
 }
 if($used.Count -gt 15 -or $orphan -gt 0){throw "Frame $f has too many colors or orphan pixels"}
 $bmp.Save((Join-Path $root "hero_idle_$f.png"),[Drawing.Imaging.ImageFormat]::Png)
 # Verify actual encoded PNG pixels, not only the text input.
 $encoded=[Drawing.Bitmap]::FromFile((Join-Path $root "hero_idle_$f.png"))
 for($vy=0;$vy -lt 40;$vy++){for($vx=0;$vx -lt 32;$vx++){
  $actual=$encoded.GetPixel($vx,$vy)
  if($actual.A -ne 0 -and $actual.A -ne 255){throw 'PNG contains partial alpha'}
  if($actual.A -eq 255 -and $actual.ToArgb() -ne $palette[[string]$rows[$vy][$vx]].ToArgb()){
   throw 'Encoded PNG palette mismatch'
  }
 }}
 $encoded.Dispose()
 $frames+=,$bmp;$grids+=,$rows
 $report.Add(('idle_{0}: rows=40 widths=32 palette=PASS colors={1} alpha=0/255 orphan=0 crack={2}px bbox=({3},{4})-({5},{6}) height={7}px' -f $f,$used.Count,$highlights,$minX,$minY,$maxX,$maxY,($maxY-$minY+1)))
}
# Compare the entire visible trouser/tie/foot region (y>=31), plus projecting knee.
$lowerChanges=0
for($f=1;$f -lt 3;$f++){
 for($y=25;$y -lt 40;$y++){for($x=0;$x -lt 32;$x++){
  if($y -ge 31 -or $x -ge 18){
   if($grids[0][$y][$x] -ne $grids[$f][$y][$x]){$lowerChanges++}
  }
 }}
}
if($lowerChanges -ne 0){throw "Lower body changed $lowerChanges pixels"}
$report.Add('Lower body/feet: identical across all frames (y>=31 and x>=18,y>=25); changed pixels=0')
for($f=1;$f -lt 3;$f++){
 for($y=4;$y -le 17;$y++){
  if($grids[$f][$y] -ne $grids[0][$y+1]){throw 'Shoulder motion exceeds one pixel'}
 }
}
foreach($y in 23,24){
 if($grids[2][$y].IndexOf('M') -ne $grids[0][$y].IndexOf('M')+1){throw 'Goreum motion mismatch'}
}
foreach($y in 28,29,30){
 if($grids[2][$y].IndexOf('B') -ne $grids[0][$y].IndexOf('B')+1){throw 'Hem motion mismatch'}
}
$report.Add('Motion: shoulders/head up 1px in frames 1/2; hem and goreum right 1px in frame 2; PASS')
$report.Add('Encoded native PNGs: pixel palette and binary alpha rechecked; PASS')
$sheet=New-Object Drawing.Bitmap 512,160
$bg=[Drawing.ColorTranslator]::FromHtml('#0b1030')
# Explicit integer pixel expansion: no filtering or half-pixel sampling.
for($y=0;$y -lt 40;$y++){for($x=0;$x -lt 128;$x++){
 $cell=[int][math]::Floor($x/32);$sx=$x%32;$c=$bg
 if($cell -lt 3){$p=$frames[$cell].GetPixel($sx,$y);if($p.A -eq 255){$c=$p}}
 else{
  $p=$frames[0].GetPixel($sx,$y);$q=$frames[2].GetPixel($sx,$y)
  if($p.ToArgb() -eq $q.ToArgb()){if($p.A -eq 255){$c=[Drawing.Color]::FromArgb(255,47,57,76)}}
  elseif($p.A -eq 255 -and $q.A -eq 255){$c=[Drawing.Color]::FromArgb(255,235,205,123)}
  elseif($p.A -eq 255){$c=[Drawing.Color]::FromArgb(255,199,126,77)}
  else{$c=[Drawing.Color]::FromArgb(255,98,184,174)}
 }
 for($dy=0;$dy -lt 4;$dy++){for($dx=0;$dx -lt 4;$dx++){$sheet.SetPixel($x*4+$dx,$y*4+$dy,$c)}}
}}
$sheet.Save((Join-Path $root 'hero_idle_frames_x4.png'),[Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose();foreach($bmp in $frames){$bmp.Dispose()}
$report.Add('Sheet: 512x160; 4x nearest; cells 0,1,2,difference(0 vs 2); background #0b1030')
$report.Add('Difference key: muted=unchanged, ochre=frame0 only, teal=frame2 only, sand=changed color')
$report | Set-Content (Join-Path $root 'validation.txt') -Encoding ASCII
$report | Write-Output
