# Helper for the mod page's screenshots (docs/screenshots.md, plan docs/plans/2026-11-01-mod-page.md, step P3).
# Run from the repo root in PowerShell. Windows PowerShell 5.1 is enough (System.Drawing).
#
#   .\tools\screenshots.ps1 -Action Install
#       copies tools\studio\macros\*.json into %VINTAGE_STORY_DATA%\Macros (the client must be closed)
#   .\tools\screenshots.ps1 -Action Dialogs -Shot title        (the client must be closed)
#       writes the window positions of that shot from tools\studio\dialog-positions.json into
#       clientsettings.json (dialogPositions); for any other shot name it removes the positions of all shots
#       listed in the file, so that the windows are on "auto" again. Backup: clientsettings.json.bak
#   .\tools\screenshots.ps1 -Action Collect -Shot title -Lang en -Ribbon "Collect them all"
#       takes the newest screenshot of Pictures\Vintagestory (or -Source <file>), checks 1920x1080, draws the ribbon
#       for the shot `title` (only that one), writes docs\images\<shot>.<lang>.<png|jpg> and checks the 2 MB limit
#   .\tools\screenshots.ps1 -Action Check
#       checks every docs\images\*.<lang>.<ext> of the shots: size 1920x1080, at most 2 MB, both languages present
#
# -Format png|jpeg  (default png; P3 decides on a real frame of the full scene, Decisions of the plan "Layout test")
# -Quality          JPEG quality, default 90
# -Corner           corner of the ribbon: TopLeft, TopRight, BottomLeft, BottomRight (default TopLeft)
# -Font             the ribbon's font, a system font (default Georgia)
# -Angle          the ribbon's slope in degrees from the horizontal (default 45; flatter, e.g. 35, to follow a line in the scene)

param(
    [Parameter(Mandatory)] [ValidateSet('Install', 'Dialogs', 'Collect', 'Check')] [string]$Action,
    [string]$Shot,
    [ValidateSet('en', 'uk')] [string]$Lang,
    [string]$Source,
    [string]$Ribbon,
    [ValidateSet('png', 'jpeg')] [string]$Format = 'png',
    [int]$Quality = 90,
    [ValidateSet('TopLeft', 'TopRight', 'BottomLeft', 'BottomRight')] [string]$Corner = 'TopLeft',
    [string]$Font = 'Georgia',
    [double]$Angle = 45
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$shots = 'title', 'album-window', 'album-chest', 'wantlist', 'ledger-intake', 'materials', 'first-ledger'
$langs = 'en', 'uk'
$maxBytes = 2MB
$images = Join-Path $repo 'docs\images'
$pictures = Join-Path ([Environment]::GetFolderPath('MyPictures')) 'Vintagestory'

function Get-DataDir {
    $d = $env:VINTAGE_STORY_DATA
    if (-not $d) { $d = [Environment]::GetEnvironmentVariable('VINTAGE_STORY_DATA', 'User') }
    if (-not $d -or -not (Test-Path $d)) { throw 'VINTAGE_STORY_DATA is not set or the folder does not exist (docs/onboarding.md)' }
    return $d
}

function Assert-ClientClosed {
    # The game writes clientsettings.json on exit and keeps the macros in memory: a running client undoes our edit.
    $p = Get-Process -Name 'Vintagestory' -ErrorAction SilentlyContinue
    if ($p) { throw 'The client is running: close it first (it rewrites its settings and macros on exit).' }
}

function Install-Macros {
    Assert-ClientClosed
    $dest = Join-Path (Get-DataDir) 'Macros'
    if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }
    $files = Get-ChildItem (Join-Path $repo 'tools\studio\macros') -Filter *.json
    foreach ($f in $files) { Copy-Item $f.FullName (Join-Path $dest $f.Name) -Force }
    Write-Host "Installed $($files.Count) macros into $dest"
}

function ConvertTo-PositionsBlock($table) {
    # The block as the game writes it: two-space indent, nested objects with X and Y.
    $names = @($table.Keys | Sort-Object)
    if ($names.Count -eq 0) { return '  "dialogPositions": {}' }
    $lines = foreach ($n in $names) {
        $v = $table[$n]
        if ($null -eq $v) { "    ""$n"": null" }
        else { "    ""$n"": {`r`n      ""X"": $($v.X),`r`n      ""Y"": $($v.Y)`r`n    }" }
    }
    return "  ""dialogPositions"": {`r`n" + ($lines -join ",`r`n") + "`r`n  }"
}

function Set-DialogPositions([string]$shotName) {
    Assert-ClientClosed
    $settings = Join-Path (Get-DataDir) 'clientsettings.json'
    $text = [IO.File]::ReadAllText($settings)
    # The top-level block (two-space indent); a nested "dialogPositions" deeper in the file has more indent.
    $rx = [regex]'(?ms)^  "dialogPositions": (\{\s*\}|\{.*?^  \}|\[\s*\])'
    $m = $rx.Match($text)
    if (-not $m.Success) { throw 'No top-level "dialogPositions" in clientsettings.json' }
    $block = '{' + $m.Value.Substring($m.Value.IndexOf('{') + 1)
    if ($m.Value -match '\[\s*\]\s*$') { $block = '{}' }
    $current = @{}
    $parsed = ConvertFrom-Json $block
    foreach ($p in $parsed.PSObject.Properties) { $current[$p.Name] = $p.Value }

    $all = ConvertFrom-Json ([IO.File]::ReadAllText((Join-Path $repo 'tools\studio\dialog-positions.json')))
    foreach ($s in $all.PSObject.Properties) {
        foreach ($d in $s.Value.PSObject.Properties) { $current.Remove($d.Name) }
    }
    $entry = $all.PSObject.Properties | Where-Object { $_.Name -eq $shotName }
    if ($entry) {
        foreach ($d in $entry.Value.PSObject.Properties) { $current[$d.Name] = $d.Value }
        Write-Host "Windows of '$shotName' written: $(($entry.Value.PSObject.Properties.Name) -join ', ')"
    } else {
        Write-Host "No positions for '$shotName': the windows of all shots are on auto."
    }
    Copy-Item $settings "$settings.bak" -Force
    $new = $text.Substring(0, $m.Index) + (ConvertTo-PositionsBlock $current) + $text.Substring($m.Index + $m.Length)
    [IO.File]::WriteAllText($settings, $new, (New-Object Text.UTF8Encoding($false)))
    # Make sure the game can read what we wrote.
    $null = ConvertFrom-Json $new
}

function Add-Ribbon([Drawing.Bitmap]$bmp, [string]$text, [string]$corner, [string]$fontName, [double]$angle) {
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'AntiAliasGridFit'
    $w = $bmp.Width; $h = $bmp.Height
    $d = 560            # the centre line cuts d pixels off the horizontal edge and d*tan(angle) off the vertical one
    $tan = [Math]::Tan($angle * [Math]::PI / 180)
    $a = $d * $tan     # the vertical intercept
    $band = 96          # the ribbon's width
    switch ($corner) {
        'TopLeft'     { $g.TranslateTransform($d / 2, $a / 2); $g.RotateTransform(-$angle) }
        'TopRight'    { $g.TranslateTransform($w - $d / 2, $a / 2); $g.RotateTransform($angle) }
        'BottomLeft'  { $g.TranslateTransform($d / 2, $h - $a / 2); $g.RotateTransform($angle) }
        'BottomRight' { $g.TranslateTransform($w - $d / 2, $h - $a / 2); $g.RotateTransform(-$angle) }
    }
    $len = $d * 3
    $x0 = -$len / 2; $y0 = -$band / 2
    $rect = New-Object Drawing.RectangleF($x0, $y0, $len, $band)
    $fill = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(235, 86, 28, 28))
    $g.FillRectangle($fill, $rect)
    $pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(255, 226, 190, 120), 3)
    $g.DrawLine($pen, $x0, $y0 + 8, -$x0, $y0 + 8)
    $g.DrawLine($pen, $x0, -$y0 - 8, -$x0, -$y0 - 8)

    # The text must fit into the visible part of the ribbon (the chord of the centre line, d*sqrt(2)), with margins.
    $maxWidth = [Math]::Sqrt($d * $d + $a * $a) * 0.72
    $size = 52.0
    do {
        $font = New-Object Drawing.Font($fontName, $size, [Drawing.FontStyle]::Bold, [Drawing.GraphicsUnit]::Pixel)
        $measured = $g.MeasureString($text, $font)
        if ($measured.Width -le $maxWidth) { break }
        $font.Dispose(); $size -= 2
    } while ($size -gt 14)
    $fmt = New-Object Drawing.StringFormat
    $fmt.Alignment = 'Center'; $fmt.LineAlignment = 'Center'
    $textRect = $rect
    $shadowRect = New-Object Drawing.RectangleF(($x0 + 2), ($y0 + 2), $len, $band)
    $g.DrawString($text, $font, (New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(160, 0, 0, 0))), $shadowRect, $fmt)
    $g.DrawString($text, $font, (New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 250, 238, 210))), $textRect, $fmt)
    $g.Dispose()
}

function Save-Frame([Drawing.Bitmap]$bmp, [string]$path, [string]$format, [int]$quality) {
    if ($format -eq 'jpeg') {
        $codec = [Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
        $params = New-Object Drawing.Imaging.EncoderParameters 1
        $params.Param[0] = New-Object Drawing.Imaging.EncoderParameter ([Drawing.Imaging.Encoder]::Quality), ([long]$quality)
        $bmp.Save($path, $codec, $params)
    } else {
        $bmp.Save($path, [Drawing.Imaging.ImageFormat]::Png)
    }
}

function Test-Frame([string]$path) {
    # Returns a list of problems (empty: the file is fine for ModDB).
    $problems = @()
    $img = [Drawing.Image]::FromFile($path)
    try {
        if ($img.Width -ne 1920 -or $img.Height -ne 1080) { $problems += "size $($img.Width)x$($img.Height), not 1920x1080" }
    } finally { $img.Dispose() }
    $bytes = (Get-Item $path).Length
    if ($bytes -gt $maxBytes) { $problems += ('{0:N2} MB, over the 2 MB limit' -f ($bytes / 1MB)) }
    return $problems
}

function Collect-Frame {
    if ($shots -notcontains $Shot) { throw "-Shot must be one of: $($shots -join ', ')" }
    if (-not $Lang) { throw '-Lang is required (en or uk)' }
    if ($Shot -eq 'title' -and -not $Ribbon) { throw 'The shot title needs -Ribbon "<text>" (the text is decided in step P5)' }
    if (-not $Source) {
        $newest = Get-ChildItem $pictures -Filter *.png | Sort-Object LastWriteTime | Select-Object -Last 1
        if (-not $newest) { throw "No screenshots in $pictures" }
        $Source = $newest.FullName
    }
    Write-Host "Source: $Source ($((Get-Item $Source).LastWriteTime))"
    # Load through a copy so that the source file is not locked and the result is a plain 32-bit bitmap.
    $raw = [Drawing.Image]::FromFile((Resolve-Path $Source))
    if ($raw.Width -ne 1920 -or $raw.Height -ne 1080) {
        $s = "$($raw.Width)x$($raw.Height)"; $raw.Dispose()
        throw "The frame is $s, not 1920x1080: run `.resolution 1080p` (the macro does) and shoot again."
    }
    $bmp = New-Object Drawing.Bitmap 1920, 1080, ([Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [Drawing.Graphics]::FromImage($bmp); $g.DrawImage($raw, 0, 0, 1920, 1080); $g.Dispose(); $raw.Dispose()
    if ($Shot -eq 'title') { Add-Ribbon $bmp $Ribbon $Corner $Font $Angle }
    if (-not (Test-Path $images)) { New-Item -ItemType Directory -Path $images | Out-Null }
    $ext = if ($Format -eq 'jpeg') { 'jpg' } else { 'png' }
    $out = Join-Path $images "$Shot.$Lang.$ext"
    Save-Frame $bmp $out $Format $Quality
    $bmp.Dispose()
    # A frame of the other format for the same shot would be stale.
    $other = Join-Path $images "$Shot.$Lang.$(if ($ext -eq 'png') { 'jpg' } else { 'png' })"
    if (Test-Path $other) { Remove-Item $other; Write-Host "Removed the stale $other" }
    $problems = Test-Frame $out
    Write-Host ("Wrote {0} ({1:N2} MB)" -f $out, ((Get-Item $out).Length / 1MB))
    if ($problems) { $problems | ForEach-Object { Write-Warning $_ }; exit 1 }
}

function Check-Frames {
    $bad = 0
    foreach ($s in $shots) {
        foreach ($l in $langs) {
            $f = Get-ChildItem $images -Filter "$s.$l.*" -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in '.png', '.jpg' }
            if (-not $f) { Write-Warning "missing: $s.$l"; $bad++; continue }
            if (@($f).Count -gt 1) { Write-Warning "two files for $s.$l"; $bad++ }
            foreach ($file in $f) {
                $p = Test-Frame $file.FullName
                if ($p) { $bad++; Write-Warning "$($file.Name): $($p -join '; ')" } else { Write-Host "ok  $($file.Name)" }
            }
        }
    }
    if ($bad) { exit 1 }
}

switch ($Action) {
    'Install' { Install-Macros }
    'Dialogs' { if (-not $Shot) { throw '-Shot is required' }; Set-DialogPositions $Shot }
    'Collect' { Collect-Frame }
    'Check'   { Check-Frames }
}
