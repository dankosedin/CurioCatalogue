# Generates the mod's item textures from the pixel grids below.
# Run from the repo root in PowerShell: & .\tools\textures.ps1
#
# Each texture is a plain 32x32 PNG, all drawn here: no vanilla pixels are copied. No "overlays" in item JSON: a texture
# with overlays has no file of its own, and mods that look textures up by file name fail on it. Better Crates 1.10.0
# then draws the crate's content wrongly and breaks the chunk mesh around it (checked in game 27.10.2026).
# A grid is placed at (X, Y); '.' leaves the pixel as it is, other characters are looked up in $palette.

Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot "..\curiocatalogue\assets\curiocatalogue\textures\item"

$palette = @{
    'k' = '5A523E'   # ink, as the text on vanilla crate labels
    'p' = 'C9BE9A'   # paper
    'q' = 'A89C78'   # paper shade
    'g' = '3E7A32'   # green ink
    'r' = '8E2A1C'   # red ink
    'y' = 'C89A3A'   # gilt frame
    'b' = '2F4E86'   # tapestry blue
    'c' = 'A8352A'   # tapestry red
    'o' = 'D8B45A'   # tapestry gold
}

# Paper of an invoice label: random light tones with a darker edge. A fixed seed keeps the file the same on every run.
$paperTones = 'B4A47C', 'B0A077', 'AD9D75', 'A99973', 'B2A279', 'A69770'
$edgeTones = '9C8E6A', '968866', '8F8262'
function Add-Paper($bmp, [int]$x0, [int]$y0, [int]$x1, [int]$y1) {
    $rand = New-Object System.Random -ArgumentList 1022
    for ($y = $y0; $y -le $y1; $y++) {
        for ($x = $x0; $x -le $x1; $x++) {
            $edge = $x -eq $x0 -or $x -eq $x1 -or $y -eq $y0 -or $y -eq $y1
            $tones = if ($edge) { $edgeTones } else { $paperTones }
            $bmp.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml("#" + $tones[$rand.Next($tones.Count)]))
        }
    }
}

# $paper: x0, y0, x1, y1 of the paper to lay first, or $null for a transparent background.
# $turnY >= 0 turns the grids by 180 degrees: pixel (x, y) goes to (31 - x, $turnY - y).
function New-Texture([string]$name, [object[]]$layers, [int[]]$paper = $null, [int]$turnY = -1) {
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList 32, 32
    if ($paper) { Add-Paper $bmp $paper[0] $paper[1] $paper[2] $paper[3] }
    foreach ($layer in $layers) {
        $x0 = $layer[0]; $y0 = $layer[1]; $rows = $layer[2]
        for ($dy = 0; $dy -lt $rows.Count; $dy++) {
            for ($dx = 0; $dx -lt $rows[$dy].Length; $dx++) {
                $ch = [string]$rows[$dy][$dx]
                if ($ch -eq '.') { continue }
                $hex = $palette[$ch]
                if (-not $hex) { throw "$name`: no colour for '$ch'" }
                $x = $x0 + $dx; $y = $y0 + $dy
                if ($turnY -ge 0) { $x = 31 - $x; $y = $turnY - $y }
                $bmp.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml("#$hex"))
            }
        }
    }
    New-Item -ItemType Directory -Force $out | Out-Null
    $bmp.Save((Join-Path $out "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    "  $name.png"
}

# --- Invoices: the whole label for the vanilla shape game:block/wood/crate/label-paper.
# The shape shows pixels x 8..23, y 3..23 and skips row 11; the paper covers exactly that. The inventory icon shows the label turned by 180 degrees
# (checked in game 27.10.2026), so the grids are drawn as the icon shows them and turned when written (turnY 26:
# rows 3..23 map onto each other). Grid row y 15 lands on the skipped row 11 and stays empty.
# The grids start at (8, 3) and are 16 wide.

$lines = @(
    '................',
    '.kkkk.kkk.......',  # y 4
    '................',
    '................',
    '.kkk.kkkk.......',  # y 7
    '................',
    '................',
    '.kkkkk.kkk.kkkk.',  # y 10
    '................',
    '................',
    '.kkk.kkkkk.kk...',  # y 13
    '................',
    '................',  # y 15, not shown
    '................',
    '.kkkkkk.kkk.kkk.',  # y 17
    '................',
    '................',
    '.kk.kkkk.kk.....'   # y 20
)

# Incoming: a green arrow pointing down, into the stock.
$arrowIn = @(
    '..g..',
    '..g..',
    '..g..',
    'ggggg',
    '.ggg.',
    '..g..'
)
# Outgoing: a red arrow pointing up, out of the stock.
$arrowOut = @(
    '..r..',
    '.rrr.',
    'rrrrr',
    '..r..',
    '..r..',
    '..r..'
)

# Want list: tick boxes, one of them ticked, each with a line.
$wantlist = @(
    '................',
    '.kkk.kkkkk.kk...',  # y 4
    '.k.k............',
    '.kkk............',
    '................',
    '.kkk.kkk.kkkk...',  # y 8
    '.kkk............',
    '.kkk............',
    '................',  # y 11
    '.kkk.kkkkkk.kk..',  # y 12
    '.k.k............',
    '.kkk............',
    '................',
    '.kkk.kkkk.kkk...',  # y 16; y 15 above, not shown
    '.k.k............',
    '.kkk............',
    '................',
    '.kkk.kk.kkkkk...',  # y 20
    '.k.k............',
    '.kkk............'
)

# --- Books: the label of our shape curiocatalogue:item/book (the vanilla small book plus a "label" element on the front
# cover, over the vanilla title marks; the cover itself stays the vanilla texture). The element shows pixels 0..4, 0..4.
# Only the front gets a label, as on a real book; the back stays plain, as in vanilla (agreed 27.10.2026).

$ledgerLabel = @(
    'ppppp',
    'pkkkp',
    'ppppp',
    'pkkqp',
    'qpppq'
)
# Bill of materials: counts (red) in front of the lines.
$materialsLabel = @(
    'ppppp',
    'prpkp',
    'ppppp',
    'prpkp',
    'qpppq'
)
# Album: a framed little tapestry.
$albumLabel = @(
    'yyyyy',
    'ycboy',
    'ybcby',
    'yocby',
    'yyyyy'
)

"Writing to $out"
$labelPaper = 8, 3, 23, 23
New-Texture 'invoice-incoming' @(@(8, 3, $lines), @(18, 4, $arrowIn)) -paper $labelPaper -turnY 26
New-Texture 'invoice-outgoing' @(@(8, 3, $lines), @(18, 4, $arrowOut)) -paper $labelPaper -turnY 26
New-Texture 'invoice-wantlist' @(, @(8, 3, $wantlist)) -paper $labelPaper -turnY 26
New-Texture 'book-ledger' @(, @(0, 0, $ledgerLabel))
New-Texture 'book-materials' @(, @(0, 0, $materialsLabel))
New-Texture 'book-album' @(, @(0, 0, $albumLabel))
