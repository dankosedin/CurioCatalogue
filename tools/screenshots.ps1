# Helper for the mod page's screenshots: the steps for a person are in tools/studio/README.md, the reasons in
# docs/screenshots.md, the plan in docs/plans/2026-11-01-mod-page.md (P3).
# Run from the repo root in PowerShell. Windows PowerShell 5.1 is enough (System.Drawing).
#
#   .\tools\screenshots.ps1 -Action Prepare -Lang uk         (the client must be closed)
#       copies the macros (tools\studio\macros) and the studio (tools\studio\studio.json) into the data folder,
#       writes the window positions of tools\studio\dialog-positions.json and the client's language into
#       clientsettings.json (backup: clientsettings.json.bak)
#   .\tools\screenshots.ps1 -Action Collect -Lang uk
#       takes the newest seven frames of Pictures\Vintagestory, in the order of the shot list (title first), checks
#       1920x1080, draws the ribbon (the text of the language from tools\studio\ribbons.json; -Ribbon overrides it) on title and writes docs\images\<shot>.<lang>.<png|jpg>; title is a JPEG, the others
#       PNG (decided in P3: with the ribbon a PNG title is over 2 MB). Prints which frame went to which shot.
#   .\tools\screenshots.ps1 -Action Check
#       checks every docs\images\<shot>.<lang>.<ext>: 1920x1080, at most 2 MB, both languages present
#
# The ribbon: -Corner (default BottomRight), -Angle in degrees (default 25, the slope of the floor boards),
# -Font (a system font, default Georgia). -Quality: JPEG quality, default 90. -Data, -Pictures, -Images: other folders, for a test run.

param(
    [Parameter(Mandatory)] [ValidateSet('Prepare', 'Collect', 'Check')] [string]$Action,
    [ValidateSet('en', 'uk')] [string]$Lang,
    [string]$Ribbon,
    [ValidateSet('TopLeft', 'TopRight', 'BottomLeft', 'BottomRight')] [string]$Corner = 'BottomRight',
    [double]$Angle = 25,
    [string]$Font = 'Georgia',
    [int]$Quality = 90,
    [string]$Data,
    [string]$Pictures,
    [string]$Images
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
# The order of the shot list in docs/screenshots.md and of the keys Alt+1 ... Alt+7: Collect relies on it.
$shots = 'title', 'album-window', 'album-chest', 'wantlist', 'ledger-intake', 'materials', 'first-ledger'
$langs = 'en', 'uk'
$maxBytes = 2MB
$images = if ($Images) { $Images } else { Join-Path $repo 'docs\images' }
$pictures = if ($Pictures) { $Pictures } else { Join-Path ([Environment]::GetFolderPath('MyPictures')) 'Vintagestory' }

function Get-DataDir {
    $d = $Data
    if (-not $d) { $d = $env:VINTAGE_STORY_DATA }
    if (-not $d) { $d = [Environment]::GetEnvironmentVariable('VINTAGE_STORY_DATA', 'User') }
    if (-not $d -or -not (Test-Path $d)) { throw 'VINTAGE_STORY_DATA is not set or the folder does not exist (docs/onboarding.md)' }
    return $d
}

function Assert-ClientClosed {
    # The game writes clientsettings.json on exit and keeps the macros in memory: a running client undoes our edit.
    $p = Get-Process -Name 'Vintagestory' -ErrorAction SilentlyContinue
    if ($p) { throw 'The client is running: close it first (it rewrites its settings and macros on exit).' }
}

function Copy-Files([string]$from, [string]$filter, [string]$to) {
    if (-not (Test-Path $to)) { New-Item -ItemType Directory -Path $to | Out-Null }
    $files = Get-ChildItem $from -Filter $filter
    foreach ($f in $files) { Copy-Item $f.FullName (Join-Path $to $f.Name) -Force }
    Write-Host "Copied $($files.Count) file(s) into $to"
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

function Set-ClientSettings([string]$language) {
    $settings = Join-Path (Get-DataDir) 'clientsettings.json'
    $text = [IO.File]::ReadAllText($settings)
    # The top-level block (two-space indent); a nested "dialogPositions" deeper in the file has more indent.
    $rx = [regex]'(?ms)^  "dialogPositions": (\{\s*\}|\{.*?^  \}|\[\s*\])'
    $m = $rx.Match($text)
    if (-not $m.Success) { throw 'No top-level "dialogPositions" in clientsettings.json' }
    $current = @{}
    if ($m.Value -notmatch '\[\s*\]\s*$') {
        $parsed = ConvertFrom-Json ('{' + $m.Value.Substring($m.Value.IndexOf('{') + 1))
        foreach ($p in $parsed.PSObject.Properties) { $current[$p.Name] = $p.Value }
    }
    # Every window of every shot: the album's for title, the inventory for first-ledger. The album's window goes back
    # to "auto" in game after title (the steps in tools/studio/README.md).
    $all = ConvertFrom-Json ([IO.File]::ReadAllText((Join-Path $repo 'tools\studio\dialog-positions.json')))
    foreach ($s in $all.PSObject.Properties) {
        foreach ($d in $s.Value.PSObject.Properties) { $current[$d.Name] = $d.Value; Write-Host "Window $($d.Name) ($($s.Name)): $($d.Value.X), $($d.Value.Y)" }
    }
    $new = $text.Substring(0, $m.Index) + (ConvertTo-PositionsBlock $current) + $text.Substring($m.Index + $m.Length)

    $lx = [regex]'"language": "[a-z-]+"'
    if (-not $lx.IsMatch($new)) { throw 'No "language" in clientsettings.json' }
    $new = $lx.Replace($new, """language"": ""$language""", 1)
    Write-Host "Client language: $language"

    $null = ConvertFrom-Json $new    # make sure the game can read what we write
    Copy-Item $settings "$settings.bak" -Force
    [IO.File]::WriteAllText($settings, $new, (New-Object Text.UTF8Encoding($false)))
}

function Invoke-Prepare {
    if (-not $Lang) { throw '-Lang is required (en or uk)' }
    Assert-ClientClosed
    $dataDir = Get-DataDir
    Copy-Files (Join-Path $repo 'tools\studio\macros') '*.json' (Join-Path $dataDir 'Macros')
    Copy-Files (Join-Path $repo 'tools\studio') 'studio.json' (Join-Path $dataDir 'WorldEdit')
    Set-ClientSettings $Lang
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

    # The text must fit into the visible part of the ribbon (the chord of the centre line), with margins.
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
    $shadowRect = New-Object Drawing.RectangleF(($x0 + 2), ($y0 + 2), $len, $band)
    $g.DrawString($text, $font, (New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(160, 0, 0, 0))), $shadowRect, $fmt)
    $g.DrawString($text, $font, (New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 250, 238, 210))), $rect, $fmt)
    $g.Dispose()
}

function Save-Frame([Drawing.Bitmap]$bmp, [string]$path, [bool]$jpeg) {
    if ($jpeg) {
        $codec = [Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
        $params = New-Object Drawing.Imaging.EncoderParameters 1
        $params.Param[0] = New-Object Drawing.Imaging.EncoderParameter ([Drawing.Imaging.Encoder]::Quality), ([long]$Quality)
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

function Invoke-Collect {
    if (-not $Lang) { throw '-Lang is required (en or uk)' }
    if (-not $Ribbon) {
        $ribbons = Get-Content (Join-Path $PSScriptRoot "studio\ribbons.json") -Raw -Encoding UTF8 | ConvertFrom-Json
        $Ribbon = $ribbons.$Lang
        if (-not $Ribbon) { throw "tools/studio/ribbons.json has no text for '$Lang': add one" }
    }
    $frames = @(Get-ChildItem $pictures -Filter *.png | Sort-Object LastWriteTime | Select-Object -Last $shots.Count)
    if ($frames.Count -lt $shots.Count) { throw "Only $($frames.Count) screenshots in $pictures, the list has $($shots.Count)" }
    # Check every frame before writing any, so that a wrong frame leaves docs\images as it was.
    foreach ($f in $frames) {
        $img = [Drawing.Image]::FromFile($f.FullName)
        $size = "$($img.Width)x$($img.Height)"; $img.Dispose()
        if ($size -ne '1920x1080') { throw "$($f.Name) is $size, not 1920x1080: a frame taken without the shot's macro? Shoot the list again." }
    }
    if (-not (Test-Path $images)) { New-Item -ItemType Directory -Path $images | Out-Null }
    $bad = 0
    for ($i = 0; $i -lt $shots.Count; $i++) {
        $shot = $shots[$i]; $src = $frames[$i]
        $raw = [Drawing.Image]::FromFile($src.FullName)
        $bmp = New-Object Drawing.Bitmap 1920, 1080, ([Drawing.Imaging.PixelFormat]::Format24bppRgb)
        $g = [Drawing.Graphics]::FromImage($bmp); $g.DrawImage($raw, 0, 0, 1920, 1080); $g.Dispose(); $raw.Dispose()
        $jpeg = $shot -eq 'title'
        if ($jpeg) { Add-Ribbon $bmp $Ribbon $Corner $Font $Angle }
        $ext = if ($jpeg) { 'jpg' } else { 'png' }
        $out = Join-Path $images "$shot.$Lang.$ext"
        Save-Frame $bmp $out $jpeg
        $bmp.Dispose()
        $other = Join-Path $images "$shot.$Lang.$(if ($jpeg) { 'png' } else { 'jpg' })"
        if (Test-Path $other) { Remove-Item $other }
        $problems = Test-Frame $out
        Write-Host ('{0,-14} <- {1}  ({2:N2} MB)' -f $shot, $src.Name, ((Get-Item $out).Length / 1MB))
        if ($problems) { $bad++; $problems | ForEach-Object { Write-Warning "$shot`: $_" } }
    }
    Write-Host 'Look at the files in docs\images: each shot must show what the list says.'
    if ($bad) { exit 1 }
}

function Invoke-Check {
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
    'Prepare' { Invoke-Prepare }
    'Collect' { Invoke-Collect }
    'Check'   { Invoke-Check }
}
