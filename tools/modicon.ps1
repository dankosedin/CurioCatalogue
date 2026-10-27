# Makes the mod icon from an in-game screenshot: crops a square, shrinks it to a pixel grid, optionally reduces the
# colours, and scales it back up without smoothing so the pixels stay sharp.
# Run from the repo root in PowerShell. The current icon (a vanilla room at night, lanterns, our album on a table):
#   & .\tools\modicon.ps1 -Source tools\modicon-source.png -X 500 -Y 300 -Size 820 -Neutral 0.4 -Saturation 1.4 -Contrast 1.1 -Brightness 1.1
# The grey-world balance (-Neutral) overshoots on a scene of mostly warm wood: at 1 the white slab turns blue.
#
# -X, -Y, -Size  the square to crop, in screenshot pixels (default: the largest centred square)
# -Pixels        pixels per side of the result grid (32 or 64 divide 512 evenly)
# -Neutral       white balance, 0 (as shot) to 1 (the average colour becomes grey); takes the warm cast of lanterns away
# -Saturation, -Contrast, -Brightness   1 keeps the picture as it is
# -Colors        reduce to this many colours with k-means (0 keeps all colours)
# -Out           output file; the icon is 512x512 like those of other mods (Better Crates 512, Carry On 300)

param(
    [Parameter(Mandatory)] [string]$Source,
    [int]$X = -1, [int]$Y = -1, [int]$Size = -1,
    [int]$Pixels = 64,
    [int]$Colors = 0,
    [double]$Neutral = 0, [double]$Saturation = 1, [double]$Contrast = 1, [double]$Brightness = 1,
    [string]$Out = (Join-Path $PSScriptRoot "..\curiocatalogue\modicon.png")
)

Add-Type -AssemblyName System.Drawing
$iconSize = 512

$src = [System.Drawing.Image]::FromFile((Resolve-Path $Source))
if ($Size -lt 0) { $Size = [Math]::Min($src.Width, $src.Height) }
if ($X -lt 0) { $X = [int](($src.Width - $Size) / 2) }
if ($Y -lt 0) { $Y = [int](($src.Height - $Size) / 2) }
if ($X + $Size -gt $src.Width -or $Y + $Size -gt $src.Height) { throw "Crop $X,$Y +$Size is outside the $($src.Width)x$($src.Height) screenshot" }

# Shrink by halves: GDI+ samples only a few source pixels per target pixel, so one big step (say 600 -> 32) skips
# most of the picture and gives blotches; halving each step averages them all.
function Resize($img, [int]$sx, [int]$sy, [int]$size, [int]$to) {
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList $to, $to
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $wrap = New-Object System.Drawing.Imaging.ImageAttributes
    $wrap.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)   # no dark fringe at the edges
    $g.DrawImage($img, (New-Object System.Drawing.Rectangle 0, 0, $to, $to), $sx, $sy, $size, $size, [System.Drawing.GraphicsUnit]::Pixel, $wrap)
    $g.Dispose(); $wrap.Dispose()
    $bmp
}
$grid = Resize $src $X $Y $Size ([Math]::Max($Pixels, [int]($Size / 2)))
$src.Dispose()
while ($grid.Width -gt $Pixels) {
    $next = Resize $grid 0 0 $grid.Width ([Math]::Max($Pixels, [int]($grid.Width / 2)))
    $grid.Dispose(); $grid = $next
}

if ($Neutral -ne 0 -or $Saturation -ne 1 -or $Contrast -ne 1 -or $Brightness -ne 1) {
    # White balance by the grey-world rule: the average colour of the picture is taken as grey, so a warm cast from
    # lanterns goes away. -Neutral blends between the original (0) and fully balanced (1).
    $sum = @(0.0, 0.0, 0.0)
    for ($j = 0; $j -lt $Pixels; $j++) { for ($i = 0; $i -lt $Pixels; $i++) { $c = $grid.GetPixel($i, $j); $sum[0] += $c.R; $sum[1] += $c.G; $sum[2] += $c.B } }
    $grey = ($sum[0] + $sum[1] + $sum[2]) / 3
    $gain = @(0..2 | ForEach-Object { 1 + $Neutral * ($grey / [Math]::Max(1, $sum[$_]) - 1) })
    for ($j = 0; $j -lt $Pixels; $j++) {
        for ($i = 0; $i -lt $Pixels; $i++) {
            $c = $grid.GetPixel($i, $j)
            $v = @(($c.R * $gain[0] * $Brightness), ($c.G * $gain[1] * $Brightness), ($c.B * $gain[2] * $Brightness))
            $luma = 0.299 * $v[0] + 0.587 * $v[1] + 0.114 * $v[2]
            $v = @($v | ForEach-Object { $s = $luma + ($_ - $luma) * $Saturation; 128 + ($s - 128) * $Contrast })
            $v = @($v | ForEach-Object { [int][Math]::Min(255, [Math]::Max(0, $_)) })
            $grid.SetPixel($i, $j, [System.Drawing.Color]::FromArgb($v[0], $v[1], $v[2]))
        }
    }
}

if ($Colors -gt 0) {
    # k-means over the grid's colours; a fixed seed keeps the result the same on every run.
    $px = for ($j = 0; $j -lt $Pixels; $j++) { for ($i = 0; $i -lt $Pixels; $i++) { $c = $grid.GetPixel($i, $j); , @($c.R, $c.G, $c.B) } }
    $rand = New-Object System.Random -ArgumentList 1022
    $centres = @(1..$Colors | ForEach-Object { , @($px[$rand.Next($px.Count)]) })
    $assign = New-Object int[] $px.Count
    for ($round = 0; $round -lt 20; $round++) {
        for ($k = 0; $k -lt $px.Count; $k++) {
            $best = 0; $bestD = [double]::MaxValue
            for ($c = 0; $c -lt $Colors; $c++) {
                $d = 0; for ($ch = 0; $ch -lt 3; $ch++) { $e = $px[$k][$ch] - $centres[$c][$ch]; $d += $e * $e }
                if ($d -lt $bestD) { $bestD = $d; $best = $c }
            }
            $assign[$k] = $best
        }
        for ($c = 0; $c -lt $Colors; $c++) {
            $sum = @(0, 0, 0); $n = 0
            for ($k = 0; $k -lt $px.Count; $k++) { if ($assign[$k] -eq $c) { for ($ch = 0; $ch -lt 3; $ch++) { $sum[$ch] += $px[$k][$ch] }; $n++ } }
            if ($n -gt 0) { $centres[$c] = @($sum | ForEach-Object { [int]($_ / $n) }) }
        }
    }
    for ($k = 0; $k -lt $px.Count; $k++) {
        $c = $centres[$assign[$k]]
        $grid.SetPixel($k % $Pixels, [Math]::Floor($k / $Pixels), [System.Drawing.Color]::FromArgb($c[0], $c[1], $c[2]))
    }
}

# Scale up: nearest neighbour, with half-pixel offset so every grid pixel becomes an even block.
$icon = New-Object System.Drawing.Bitmap -ArgumentList $iconSize, $iconSize
$g = [System.Drawing.Graphics]::FromImage($icon)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
$g.DrawImage($grid, 0, 0, $iconSize, $iconSize)
$g.Dispose(); $grid.Dispose()

$icon.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$icon.Dispose()
"Saved $Out"
