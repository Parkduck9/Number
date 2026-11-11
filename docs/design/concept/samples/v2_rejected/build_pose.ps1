$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = $PSScriptRoot
# Original flat-shape drawing at 128x160. Coordinates are in sketch pixels.
$hex = [ordered]@{
 A='101629'; B='35465B'; C='202B43'; D='354461'; E='53647A';
 F='71818B'; G='424752'; H='777B80'; I='D9AD82'; J='99795D';
 K='202D36'; L='46515A'; M='4EABB1'; N='8BADAA'; O='A2A398'
}
$colors = @{}
foreach ($key in $hex.Keys) { $colors[$key] = [Drawing.ColorTranslator]::FromHtml('#'+$hex[$key]) }
@('# Ahn Yudo concept palette. Dot = transparent; all other entries alpha 255.',
  '# AI authored using Codex; PowerShell 5.1 + System.Drawing; no external assets.') +
  @($hex.Keys | ForEach-Object { "$_ #$($hex[$_])" }) |
  Set-Content -LiteralPath (Join-Path $root 'hero_palette.txt') -Encoding ASCII
$bmp = New-Object Drawing.Bitmap 128,160
$g = [Drawing.Graphics]::FromImage($bmp)
$g.Clear([Drawing.Color]::Transparent)
$g.SmoothingMode = [Drawing.Drawing2D.SmoothingMode]::None
function Poly($c, [string]$vertices) {
 $pts = @($vertices.Split(' ') | ForEach-Object {
   $xy=$_.Split(','); New-Object Drawing.PointF ([single]$xy[0]),([single]$xy[1])
 })
 $brush=New-Object Drawing.SolidBrush $colors[$c]
 $g.FillPolygon($brush,[Drawing.PointF[]]$pts); $brush.Dispose()
}
# Rear leg: folded outward, load above rear ankle; front knee projects right.
Poly A '41,99 64,103 57,119 45,134 46,141 55,143 55,148 29,148 28,142 34,133 34,121'
Poly G '43,105 58,106 51,120 39,135 35,137 37,122'
Poly H '43,109 49,109 43,123 37,130 39,119'
Poly O '33,134 44,135 44,140 32,139'
Poly B '32,143 45,142 49,145 31,145'
Poly A '62,101 76,102 94,118 92,124 83,139 91,140 105,143 106,148 78,148 74,140 78,126 72,122 61,120'
Poly G '66,108 75,108 88,119 87,124 80,137 77,136 81,123 70,119 63,117'
Poly H '70,109 77,114 84,120 80,124 73,121 66,116'
Poly O '77,135 85,137 83,141 75,140'
Poly B '80,143 92,143 101,145 80,145'
# Long coat silhouette, its rear edge flowing left from the forward lean.
Poly A '58,49 71,49 77,59 74,76 70,87 75,103 84,119 74,124 62,121 58,108 51,123 35,128 17,122 30,106 38,88 42,71 48,56'
Poly D '57,52 68,52 72,61 69,76 65,87 70,104 78,117 72,119 63,116 58,101 48,118 34,123 23,120 34,105 42,87 46,70 50,58'
Poly E '51,58 57,56 52,76 48,93 37,113 27,120 23,119 34,104 42,85 46,69'
Poly C '59,80 66,81 63,94 68,111 72,118 63,116 58,101 48,119 42,121 50,106'
Poly B '24,119 35,122 48,118 45,123 34,125 20,122'
# Bent rear elbow and broad bell wrist; opening separates fist from coat.
Poly A '50,54 58,60 51,73 43,79 48,84 55,81 61,85 59,92 51,94 38,91 28,81 30,71 40,60'
Poly D '48,58 52,61 45,73 38,79 43,86 50,86 51,90 40,88 32,80 34,72 42,62'
Poly E '44,61 48,60 42,72 35,79 34,74 38,67'
Poly B '40,83 47,85 48,91 41,89 35,84'
Poly J '50,84 57,82 59,85 57,89 51,89'
Poly I '51,84 56,83 57,85 51,86'
# Cowl and head; face is a strict single right profile, not two visible eyes.
Poly A '60,23 68,20 79,22 85,29 85,39 89,42 86,46 85,51 77,55 65,52 55,55 51,50 55,43 54,32'
Poly B '60,25 68,23 77,24 80,27 65,27 60,33 59,44 55,49 55,40 57,31'
Poly D '65,28 78,27 82,31 80,38 78,45 73,49 64,48 60,51 59,43 60,33'
Poly I '76,29 81,30 83,34 82,39 86,42 83,44 84,48 79,51 73,48 72,38'
Poly J '73,39 77,42 77,47 81,48 78,50 73,47'
Poly A '80,45 84,45 83,47 80,47'
Poly K '73,34 84,34 84,39 74,40 70,37'
Poly L '73,33 82,33 83,35 74,35'
Poly N '78,35 81,36 80,38 78,37'
Poly C '62,48 70,48 77,52 73,57 60,57 53,53 55,49'
Poly E '55,49 60,49 66,53 72,54 69,56 59,54'
# Forward arm reaches down-right, wide sleeve opening exposes a small hand.
Poly A '67,56 74,57 82,65 85,70 98,76 104,87 99,94 88,92 79,85 72,80 64,69'
Poly D '69,59 73,60 78,67 82,74 95,80 99,87 96,90 88,88 80,81 75,79 67,68'
Poly E '68,59 72,59 76,65 80,74 91,79 93,82 80,78 73,71'
Poly B '91,80 96,80 101,87 98,92 93,90 96,86'
Poly J '100,85 106,87 111,93 108,97 102,94 99,91'
Poly I '101,86 105,88 108,92 104,92 101,90'
# Crossed collar and the only strong body cyan: two loose goreum tails.
Poly F '63,56 66,58 60,72 57,74 57,70'
Poly C '66,60 69,64 63,73 58,77 56,74 61,69'
Poly M '59,73 64,73 64,77 60,79 62,89 58,97 55,96 58,87 57,78 54,78 54,75'
Poly M '63,77 66,78 67,86 64,93 62,92 64,85'
$g.Dispose()
$bmp.Save((Join-Path $root 'sketch_pose.png'),[Drawing.Imaging.ImageFormat]::Png)
# Box coverage average, then nearest RGB palette. Transparent when coverage < 50%.
$draft = New-Object Drawing.Bitmap 32,40
$rows = @()
for ($y=0; $y -lt 40; $y++) {
 $row=''
 for ($x=0; $x -lt 32; $x++) {
  $n=0; $r=0; $gg=0; $b=0
  for($dy=0;$dy -lt 4;$dy++){for($dx=0;$dx -lt 4;$dx++){
   $p=$bmp.GetPixel($x*4+$dx,$y*4+$dy)
   if($p.A -gt 0){$n++;$r+=$p.R;$gg+=$p.G;$b+=$p.B}
  }}
  $symbol='.'
  if($n -ge 8){
   $r/=$n;$gg/=$n;$b/=$n;$best=[double]::MaxValue
   foreach($key in $hex.Keys){
    $p=$colors[$key];$d=[math]::Pow($p.R-$r,2)+[math]::Pow($p.G-$gg,2)+[math]::Pow($p.B-$b,2)
    if($d -lt $best){$best=$d;$symbol=$key}
   }
   $draft.SetPixel($x,$y,$colors[$symbol])
  }
  $row+=$symbol
 }
 $rows+=$row
}
$draft.Save((Join-Path $root 'draft_downsample.png'),[Drawing.Imaging.ImageFormat]::Png)
$rows | Set-Content (Join-Path $root 'draft_downsample.txt') -Encoding ASCII
$draft.Dispose();$bmp.Dispose()
Write-Output 'Sketch 128x160 -> box-average + nearest-palette draft 32x40 complete.'
for($i=0;$i -lt $rows.Count;$i++){Write-Output ('{0:D2} {1}' -f $i,$rows[$i])}
