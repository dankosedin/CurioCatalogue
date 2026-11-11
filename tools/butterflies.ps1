# Generates the butterflies' collection definition from the game's assets (L4a of docs/plans/2026-11-09-album-polish.md).
# Run from the repo root in PowerShell: & .\tools\butterflies.ps1
#
# Reads, from the game copy in $env:VINTAGE_STORY:
#   - assets/survival/entities/animal/invertebrate/butterfly.json, "chanceByType": the types and their steps of spawn chance;
#   - assets/game/lang/en.json, "itemdesc-creature-butterfly-{type}": each type's Latin name.
# Writes curiocatalogue/assets/curiocatalogue/config/curiocatalogue/collections/butterflies.json and prints the species' lang
# lines (the same text in en.json and uk.json). Run it again after a game update; ButterfliesTests (server tests) fail when the
# game moves a type to another species or step of chance.
#
# What the game does not give is written below by hand: the family of each genus, the families' order, the words of the steps,
# and the sample butterflies drawn as icons of tabs and layouts.

$ErrorActionPreference = 'Stop'
$game = $env:VINTAGE_STORY
if (-not $game) { throw "VINTAGE_STORY is not set (docs/onboarding.md)" }
$out = Join-Path $PSScriptRoot "..\curiocatalogue\assets\curiocatalogue\config\curiocatalogue\collections\butterflies.json"

# --- The game's tables ---

$entity = Get-Content -Raw -Encoding UTF8 (Join-Path $game "assets\survival\entities\animal\invertebrate\butterfly.json")
$chanceLine = ($entity -split "`n" | Where-Object { $_ -match 'chanceByType' } | Select-Object -First 1)
$chance = [ordered]@{}
foreach ($m in [regex]::Matches($chanceLine, '"\*-([a-z0-9]+)":\s*([0-9.Ee+-]+)')) {
    $chance[$m.Groups[1].Value] = [double]::Parse($m.Groups[2].Value, [Globalization.CultureInfo]::InvariantCulture)
}
$types = @($chance.Keys)   # the game's order
if ($types.Count -eq 0) { throw "No chanceByType in butterfly.json" }

$lang = Get-Content -Raw -Encoding UTF8 (Join-Path $game "assets\game\lang\en.json")
$latin = @{}
foreach ($m in [regex]::Matches($lang, '"itemdesc-creature-butterfly-([a-z0-9]+)":\s*"([^"]*)"')) { $latin[$m.Groups[1].Value] = $m.Groups[2].Value }

# --- By hand: families by genus, in the usual order of butterflies, moths last (L4a) ---

$families = [ordered]@{
    'papilionidae' = 'graphium ornithoptera papilio parnassius troides'
    'hesperiidae'  = 'astraptes'
    'pieridae'     = 'anteos anthocharis appias catopsilia colias delias eroessa gonepteryx hesperocharis leptidea phoebis pieris pontia zerene'
    'riodinidae'   = 'mesosemia thisbe'
    'lycaenidae'   = 'aricia callophrys cupidopsis glaucopsyche lycaena maculinea plebejus strymon thecla'
    'nymphalidae'  = 'acraea aglais apatura araschnia asterope batesia callicore charaxes coenonympha cupha cymothoe cyrestis danaus dryadula erebia euphaedra euphydryas fountainea haetera hipparchia inachis junonia kaniska maniola mestra morpho nymphalis oeneis parantica polygonia siproeta taenaris vanessa'
    # Saturniidae, Erebidae, Sphingidae, Uraniidae, Geometridae, Noctuidae as one
    'moths'        = 'arctia attacus chrysiridia daphnis mimas niceteria nyctemera plataea psychomorpha thalaina'
}
$familyOfGenus = @{}
foreach ($f in $families.Keys) { foreach ($g in ($families[$f] -split ' ')) { $familyOfGenus[$g] = $f } }

# The steps of chance, rarest first (drafts of the words in lang, reviewed at release).
$steps = 'legendary', 'veryrare', 'rare', 'scarce', 'uncommon', 'common'

$familyIcons = [ordered]@{
    'papilionidae' = 'commonyellowswallowtailmale'; 'hesperiidae' = 'twobarredflasher'; 'pieridae' = 'commonbrimstonemale'
    'riodinidae' = 'ireniametalmark'; 'lycaenidae' = 'largebluefemale'; 'nymphalidae' = 'peacock'; 'moths' = 'atlasmothmale'
}
$stepIcons = [ordered]@{
    'legendary' = 'queenvictoriasbirdwingmale'; 'veryrare' = 'purplespottedswallowtail'; 'rare' = 'mourningcloak'
    'scarce' = 'morphoportis'; 'uncommon' = 'peacock'; 'common' = 'monarch'
}
$layoutIcons = @{ 'byfamily' = 'commonyellowswallowtailmale'; 'byrarity' = 'queenvictoriasbirdwingmale' }

# --- Derived: species by genus and species of the Latin name; family by genus; step by chance ---

$speciesOf = [ordered]@{}   # species code -> types
$speciesName = @{}          # species code -> "Genus species"
foreach ($t in $types) {
    if (-not $latin.ContainsKey($t)) { throw "No Latin name for '$t' in the game's en.json" }
    $words = @($latin[$t].Trim() -split '\s+')
    if ($words.Count -lt 2) { throw "The Latin name of '$t' is not a binomial: '$($latin[$t])'" }
    $genus = $words[0].ToLowerInvariant(); $epithet = $words[1].ToLowerInvariant()
    $code = "$genus-$epithet"
    if (-not $speciesOf.Contains($code)) { $speciesOf[$code] = New-Object System.Collections.ArrayList }
    [void]$speciesOf[$code].Add($t)
    $speciesName[$code] = $genus.Substring(0, 1).ToUpperInvariant() + $genus.Substring(1) + " " + $epithet
}
$species = @($speciesOf.Keys | Sort-Object { $_ } -CaseSensitive)

$speciesOfFamily = [ordered]@{}
foreach ($f in $families.Keys) { $speciesOfFamily[$f] = New-Object System.Collections.ArrayList }
foreach ($s in $species) {
    $genus = $s.Split('-')[0]
    if (-not $familyOfGenus.ContainsKey($genus)) { throw "No family for the genus '$genus' ($s): add it above" }
    [void]$speciesOfFamily[$familyOfGenus[$genus]].Add($s)
}

$chances = @($chance.Values | Sort-Object -Unique)
if ($chances.Count -ne $steps.Count) { throw "The game has $($chances.Count) steps of chance, the words are for $($steps.Count)" }
$typesOfStep = [ordered]@{}
for ($i = 0; $i -lt $steps.Count; $i++) {
    $typesOfStep[$steps[$i]] = @($types | Where-Object { $chance[$_] -eq $chances[$i] })
}

# --- Writing, wrapped at about 120 characters as the other definitions ---

function Quote($s) { '"' + $s + '"' }
function Wrap([string[]]$items, [string]$indent) {
    $lines = New-Object System.Collections.ArrayList; $line = ''
    foreach ($q in ($items | ForEach-Object { Quote $_ })) {
        $next = if ($line) { "$line, $q" } else { $q }
        if (($indent.Length + $next.Length) -gt 118 -and $line) { [void]$lines.Add($line + ','); $line = $q } else { $line = $next }
    }
    if ($line) { [void]$lines.Add($line) }
    ($lines | ForEach-Object { $indent + $_ }) -join "`n"
}
function MapLines($map, [string]$indent) {
    $keys = @($map.Keys)
    $rows = for ($i = 0; $i -lt $keys.Count; $i++) {
        $sep = if ($i -lt $keys.Count - 1) { ',' } else { '' }
        $one = "$indent$(Quote $keys[$i]): [" + (($map[$keys[$i]] | ForEach-Object { Quote $_ }) -join ', ') + "]$sep"
        if ($one.Length -le 120) { $one }
        else { "$indent$(Quote $keys[$i]): [`n" + (Wrap $map[$keys[$i]] ($indent + '  ')) + "`n$indent]$sep" }
    }
    $rows -join "`n"
}
function IconLines($map, [string]$indent) {
    $keys = @($map.Keys)
    $rows = for ($i = 0; $i -lt $keys.Count; $i++) {
        $sep = if ($i -lt $keys.Count - 1) { ',' } else { '' }
        "$indent$(Quote $keys[$i]): $(Quote ('game:butterfly-dead-' + $map[$keys[$i]]))$sep"
    }
    $rows -join "`n"
}

$json = @"
{
  "nameKey": "curiocatalogue:collection-butterflies",
  "albumRecipe": "curiocatalogue:ledger-album-butterflies",
  "source": { "kind": "variants", "code": "game:butterfly-dead-{type}" },
  "dimensions": [
    {
      "code": "type",
      "values": [
$(Wrap $types '        ')
      ]
    },
    {
      "code": "species",
      "from": "type",
      "nameKey": "curiocatalogue:butterflies-species-{value}",
      "values": [
$(Wrap $species '        ')
      ],
      "map": {
$(MapLines $speciesOf '        ')
      }
    },
    {
      "code": "family",
      "from": "species",
      "nameKey": "curiocatalogue:butterflies-family-{value}",
      "icons": {
$(IconLines $familyIcons '        ')
      },
      "values": [$((@($families.Keys) | ForEach-Object { Quote $_ }) -join ', ')],
      "map": {
$(MapLines $speciesOfFamily '        ')
      }
    },
    {
      "code": "rarity",
      "from": "type",
      "nameKey": "curiocatalogue:butterflies-rarity-{value}",
      "icons": {
$(IconLines $stepIcons '        ')
      },
      "values": [$(($steps | ForEach-Object { Quote $_ }) -join ', ')],
      "map": {
$(MapLines $typesOfStep '        ')
      }
    }
  ],
  "levels": [
    {
      "id": "all",
      "progressKey": "curiocatalogue:butterflies-progress",
      "keep": ["type"],
      "drop": {},
      "layouts": [
        { "id": "byfamily", "icon": "game:butterfly-dead-$($layoutIcons.byfamily)", "nameKey": "curiocatalogue:butterflies-layout-byfamily", "groupBy": ["family", "species"] },
        { "id": "byrarity", "icon": "game:butterfly-dead-$($layoutIcons.byrarity)", "nameKey": "curiocatalogue:butterflies-layout-byrarity", "groupBy": ["rarity"] }
      ]
    }
  ],
  "exclude": []
}
"@

$full = [IO.Path]::GetFullPath($out)
[IO.File]::WriteAllText($full, ($json -replace "`r`n", "`n") + "`n", (New-Object Text.UTF8Encoding $false))
Write-Host "Wrote $full`: $($types.Count) types, $($species.Count) species, $($families.Count) families, $($steps.Count) steps"
foreach ($f in $families.Keys) { Write-Host ("  {0}: {1} species" -f $f, $speciesOfFamily[$f].Count) }
foreach ($s in $steps) { Write-Host ("  {0}: {1} types" -f $s, $typesOfStep[$s].Count) }
Write-Host "`nLang lines of the species (en.json and uk.json alike):"
foreach ($s in $species) { Write-Host ("  ""curiocatalogue:butterflies-species-{0}"": ""{1}""," -f $s, $speciesName[$s]) }
