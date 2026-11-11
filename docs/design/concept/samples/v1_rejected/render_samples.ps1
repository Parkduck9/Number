# PowerShell 5.1; run from any directory. Source grids are the artwork.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$palette = New-Object 'System.Collections.Generic.Dictionary[char,System.Drawing.Color]'
foreach ($line in (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'hero_palette.txt'))) {
    if ($line -match '^(.)\s*->\s*#([0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})\s*$') {
        $key = [char]$Matches[1]
        $hex = $Matches[2]
        if ($palette.ContainsKey($key)) { throw "Duplicate palette index: $key" }
        if ($hex.Length -eq 8) {
            $color = [System.Drawing.Color]::FromArgb([Convert]::ToInt32($hex, 16))
        } else {
            $color = [System.Drawing.ColorTranslator]::FromHtml('#' + $hex)
        }
        if ($color.A -ne 0 -and $color.A -ne 255) { throw 'Nonbinary palette alpha' }
        $palette.Add($key, $color)
    } elseif ($line.Trim().Length -gt 0 -and -not $line.StartsWith('#')) {
        throw "Invalid palette line: $line"
    }
}
$opaquePalette = @($palette.Values | Where-Object { $_.A -eq 255 })
if ($opaquePalette.Count -gt 14) { throw 'Palette exceeds 14 opaque colors' }
if (-not $palette.ContainsKey([char]'.') -or $palette[[char]'.'].A -ne 0) {
    throw 'The dot index must be transparent'
}
Write-Output "Palette: $($opaquePalette.Count) opaque colors + transparent"

function New-NearestPreview([System.Drawing.Bitmap]$Source) {
    $scaled = New-Object System.Drawing.Bitmap ($Source.Width * 4), ($Source.Height * 4), ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($scaled)
    try {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.DrawImage($Source, (New-Object System.Drawing.Rectangle 0, 0, $scaled.Width, $scaled.Height), 0, 0, $Source.Width, $Source.Height, [System.Drawing.GraphicsUnit]::Pixel)
    } finally { $graphics.Dispose() }
    return ,$scaled
}

function Assert-BinaryAlpha([System.Drawing.Bitmap]$Bitmap) {
    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = 0; $x -lt $Bitmap.Width; $x++) {
            $alpha = $Bitmap.GetPixel($x, $y).A
            if ($alpha -ne 0 -and $alpha -ne 255) { throw "Nonbinary alpha at $x,$y" }
        }
    }
}

$previews = @()
try {
    foreach ($sample in @('A', 'B')) {
        $stem = "hero_idle_$sample"
        $rows = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot "$stem.txt"))
        if ($rows.Count -ne 40) { throw "${stem}: expected 40 rows, got $($rows.Count)" }
        $used = New-Object 'System.Collections.Generic.HashSet[int]'
        $minX = 32; $minY = 40; $maxX = -1; $maxY = -1
        $bitmap = New-Object System.Drawing.Bitmap 32, 40, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            for ($y = 0; $y -lt 40; $y++) {
                if ($rows[$y].Length -ne 32) { throw "${stem}: row $y has $($rows[$y].Length) chars; expected 32" }
                for ($x = 0; $x -lt 32; $x++) {
                    $index = $rows[$y][$x]
                    if (-not $palette.ContainsKey($index)) { throw "${stem}: unknown index '$index' at $x,$y" }
                    $color = $palette[$index]
                    $bitmap.SetPixel($x, $y, $color)
                    if ($color.A -eq 255) {
                        [void]$used.Add($color.ToArgb())
                        $minX = [Math]::Min($minX, $x); $maxX = [Math]::Max($maxX, $x)
                        $minY = [Math]::Min($minY, $y); $maxY = [Math]::Max($maxY, $y)
                    }
                }
            }
            if ($maxY -ne 39 -or $minY -lt 2 -or $minX -lt 2 -or $maxX -gt 29) { throw "${stem}: bottom alignment or 2px margin failed" }
            Assert-BinaryAlpha $bitmap
            $bitmap.Save((Join-Path $PSScriptRoot "$stem.png"), [System.Drawing.Imaging.ImageFormat]::Png)
            $preview = New-NearestPreview $bitmap
            Assert-BinaryAlpha $preview
            $preview.Save((Join-Path $PSScriptRoot "${stem}_x4.png"), [System.Drawing.Imaging.ImageFormat]::Png)
            $previews += $preview
            Write-Output "${stem}: PASS; 40 rows x 32 chars; $($used.Count) distinct opaque colors ($($used.Count + 1) including transparent); bounding-box height $($maxY - $minY + 1)px; bounds ($minX,$minY)-($maxX,$maxY); alpha 0/255; 4x 128x160"
        } finally { $bitmap.Dispose() }
    }
    $compare = New-Object System.Drawing.Bitmap 256, 160, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($compare)
    try {
        $graphics.Clear([System.Drawing.ColorTranslator]::FromHtml('#0b1030'))
        $graphics.DrawImageUnscaled($previews[0], 0, 0)
        $graphics.DrawImageUnscaled($previews[1], 128, 0)
        Assert-BinaryAlpha $compare
        $compare.Save((Join-Path $PSScriptRoot 'hero_idle_compare_x4.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Output 'Comparison: PASS; A left / B right; 256x160; background #0b1030'
    } finally { $graphics.Dispose(); $compare.Dispose() }
} finally {
    foreach ($preview in $previews) { $preview.Dispose() }
}
