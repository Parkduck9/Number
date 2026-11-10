param([string]$RigFile = (Join-Path $PSScriptRoot 'parts/rig.json'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$rig=Get-Content -LiteralPath $RigFile -Raw -Encoding UTF8 | ConvertFrom-Json
$dir=Split-Path $RigFile
$byName=@{}; $origins=@{}; $images=@{}
foreach($p in $rig.parts){$byName[$p.name]=$p; $images[$p.name]=[System.Drawing.Bitmap]::new((Join-Path $dir $p.file)); if($images[$p.name].Width -ne $p.size[0] -or $images[$p.name].Height -ne $p.size[1]){throw "Size mismatch: $($p.name)"}}
function Origin($name){
 if($origins.ContainsKey($name)){return ,$origins[$name]}
 $p=$byName[$name];$o=@(0,0)
 if($null -ne $p.parent){$o=Origin $p.parent}
 $pos=@(($o[0]+$p.offset[0]-$p.pivot[0]),($o[1]+$p.offset[1]-$p.pivot[1]))
 $origins[$name]=$pos; return ,$pos
}
$assembled=[System.Drawing.Bitmap]::new(360,540)
$g=[System.Drawing.Graphics]::FromImage($assembled)
foreach($p in ($rig.parts | Sort-Object draw_order)){
 $o=Origin $p.name
 if($p.occlusion){
  $clip=$p.occlusion;$hand=$images[$clip.part];$ho=Origin $clip.part
  $sprite=$images[$p.name].Clone()
  for($y=0;$y -lt $sprite.Height;$y++){for($x=0;$x -lt $sprite.Width;$x++){
   $hx=$o[0]+$x-$ho[0];$hy=$o[1]+$y-$ho[1]
   if($hx -lt $clip.rect[0] -or $hy -lt $clip.rect[1] -or $hx -ge ($clip.rect[0]+$clip.rect[2]) -or $hy -ge ($clip.rect[1]+$clip.rect[3])){continue}
   $c=$hand.GetPixel($hx,$hy)
   foreach($hex in $clip.skin_colors){$s=[System.Drawing.ColorTranslator]::FromHtml($hex);$dist=[Math]::Sqrt([Math]::Pow($c.R-$s.R,2)+[Math]::Pow($c.G-$s.G,2)+[Math]::Pow($c.B-$s.B,2));if($c.A -gt 128 -and $dist -lt $clip.color_tolerance){$sprite.SetPixel($x,$y,[System.Drawing.Color]::Transparent);break}}
  }}
  $g.DrawImageUnscaled($sprite,$o[0],$o[1]);$sprite.Dispose()
 }else{$g.DrawImageUnscaled($images[$p.name],$o[0],$o[1])}
}
$g.Dispose()
$assembled.Save((Join-Path $PSScriptRoot 'assembled_transparent.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$board=[System.Drawing.Bitmap]::new(1200,650)
$g=[System.Drawing.Graphics]::FromImage($board)
$g.Clear([System.Drawing.ColorTranslator]::FromHtml('#e8e8e2'))
$ink=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#252d36'))
$muted=[System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#5b6670'))
$font=[System.Drawing.Font]::new('Consolas',11)
$title=[System.Drawing.Font]::new('Consolas',16,[System.Drawing.FontStyle]::Bold)
$g.DrawString('RIG STUDY / RIGHT PROFILE',$title,$ink,24,20)
$g.DrawString('ASSEMBLED FROM rig.json',$font,$muted,24,54)
$g.DrawImageUnscaled($assembled,-40,84)
$pen=[System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#bdc5c7'),1)
$g.DrawLine($pen,316,75,316,620)
$g.DrawString('EXPLODED / NATIVE PARTS',$title,$ink,350,20)
$g.DrawString('Cross = rotation pivot  |  All limbs neutral, straight down',$font,$muted,350,54)
$locations=@{
 head=@(370,110);torso=@(550,110);pelvis=@(745,130);blade=@(950,150)
 upper_arm_far=@(375,330);forearm_far=@(450,330);upper_arm_near=@(570,330);forearm_near=@(645,330)
 thigh_far=@(770,330);shin_far=@(845,330);thigh_near=@(970,330);shin_near=@(1045,330)
}
$cross=[System.Drawing.Pen]::new([System.Drawing.ColorTranslator]::FromHtml('#c0443d'),1)
foreach($p in $rig.parts){
 $xy=$locations[$p.name];$g.DrawImageUnscaled($images[$p.name],$xy[0],$xy[1]);$x=$xy[0]+$p.pivot[0];$y=$xy[1]+$p.pivot[1]
 $g.DrawLine($cross,$x-4,$y,$x+4,$y);$g.DrawLine($cross,$x,$y-4,$x,$y+4)
 $label=$p.name.Replace('_',"`n")
 $g.DrawString($label,$font,$ink,$xy[0]-8,$xy[1]+$p.size[1]+10)
}
$g.DrawString('EXPERIMENT / NOT USED IN GAME',$font,$muted,350,604)
$g.Dispose()
$board.Save((Join-Path $PSScriptRoot 'parts_assembled.png'),[System.Drawing.Imaging.ImageFormat]::Png)
foreach($im in $images.Values){$im.Dispose()};$assembled.Dispose();$board.Dispose();$font.Dispose();$title.Dispose();$ink.Dispose();$muted.Dispose();$pen.Dispose();$cross.Dispose()
Write-Output 'Rendered assembled PNG and side-by-side exploded preview from rig.json.'
