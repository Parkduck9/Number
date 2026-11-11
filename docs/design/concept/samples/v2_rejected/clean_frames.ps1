$ErrorActionPreference='Stop'
# Hand cleanup AFTER inspecting sketch_pose.png and draft_downsample.png.
# Each run below is a manually edited row of the downsample, not a new raster drawing.
# Retains the gesture, bell cuffs and swept hem of the large sketch.
$edits=@{
5='16:BBB';
6='15:BEDDA';
7='14:BEDDIIA';
8='14:BDDLKNA';
9='14:BDDDKIA';
10='14:BDDDJIIA';
11='13:BCDDDJIA';
12='13:BEDCCJJA';
13='13:BCEDCCA';
14='11:BBCDFCA';
15='10:BEDACFEBA';
16='9:BEDCADFCDEBA';
17='8:BEDBACEFBCEDA';
18='7:BEDACEDMBCDEBAA';
19='7:BDABEDMMDACDEDBAA';
20='7:BCABEDMDMAAACDEDB';
21='8:BCBJIADMA..ACDEDBA';
22='9:AAJJAEDMA...ABDBII';
23='9:BECDCMDA......AJJA';
24='9:BEDDCMDA';
25='8:BEDDDCCDBA';
26='8:BEDDDCACDBAA';
27='7:BEDDDCAAACDDAA';
28='6:BEDDDCCAAACDDAGA';
29='5:BEDDDDCAAAACCAAHGA';
30='5:BBBBBCAA..AAACAGGA';
31='8:AHGGA......AGGA';
32='8:AHGA.......GGA';
33='8:HGA.......AGA';
34='8:OOA.......AOOA';
35='7:ABBB......AGGDBBA';
36='7:AAAAAAA...AAAAAAA';
}
$rows=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'draft_downsample.txt'))
foreach($y in $edits.Keys){
 $parts=$edits[$y].Split(':');$x=[int]$parts[0]
 $rows[$y]=('.' * $x + $parts[1]).PadRight(32,'.')
}
$rows | Set-Content (Join-Path $PSScriptRoot 'hero_idle_0.txt') -Encoding ASCII
# Fixed lower-body layer. Shoulders/head breathe upward by one pixel.
# Hem is y>=25; it sways horizontally by one pixel, without moving any trousers.
# To keep exact layer identity, only the coat pixels A..F in x<=17 are shifted.
function Breathe([int]$phase){
 $out=@($rows)
 if($phase -eq 1){
  for($y=4;$y -le 17;$y++){$out[$y]=$rows[$y+1]}
  $out[18]=$rows[18]
 }
 if($phase -eq 2){
  # At the crest the raised shoulder block is identical to phase 1.
  for($y=4;$y -le 17;$y++){$out[$y]=$rows[$y+1]}
 }
 # Tail ends sway right at crest; only 1px translation of existing cyan pixels.
 if($phase -eq 2){
  foreach($y in 23,24){
   $a=$out[$y].ToCharArray()
   for($x=0;$x -lt 32;$x++){if($a[$x] -eq 'M'){$a[$x]='D';$a[$x+1]='M';break}}
   $out[$y]=-join $a
  }
  # Trailing hem-only swatch shifts right, leaving the pants layer untouched.
  foreach($y in 28,29,30){
   $a=$out[$y].ToCharArray();$first=$rows[$y].IndexOf('B');$end=13
   for($x=$end;$x -ge $first;$x--){$a[$x+1]=$rows[$y][$x]}
   $a[$first]='.';$out[$y]=-join $a
  }
 }
 return $out
}
Breathe 1 | Set-Content (Join-Path $PSScriptRoot 'hero_idle_1.txt') -Encoding ASCII
Breathe 2 | Set-Content (Join-Path $PSScriptRoot 'hero_idle_2.txt') -Encoding ASCII
Write-Output 'Hand-cleaned frames written. Playback order: 0,1,2,1.'
