# Checks references into the game's source code against the game version the mod targets.
#
# A reference looks like `Collectible.cs:89#HeldPriorityInteract` or `Block.cs:2195-2207#interactionsIndex`: a file name,
# a line or a range, and a symbol that must appear there. It holds for the game version in curiocatalogue/modinfo.json
# (dependencies.game) of the same commit. Each of vsapi, vssurvivalmod and vsessentialsmod has one commit per version,
# "Updated to Version X"; files are read from that commit with `git show`, so the clones' working copies are not touched.
#
# Usage: tools/check-refs.ps1 [-SourceRoot <folder with the three clones>] [-GameVersion <x.y.z>]   (default: $env:VINTAGE_STORY_SRC)
# Exit code 1 if a reference fails. References without a symbol are listed as warnings; references to our own files are skipped.

param(
    [string]$SourceRoot = $env:VINTAGE_STORY_SRC,
    # Check against another version, e.g. before moving to it (default: the one in modinfo.json).
    [string]$GameVersion
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$repos = 'vsapi', 'vssurvivalmod', 'vsessentialsmod'
# Old Ukrainian docs keep their references as they were written.
$excluded = '^(docs[\\/]archive[\\/]|(DESIGN|RESEARCH|PLAN|REVIEW|SETUP)\.md$)'

if (-not $SourceRoot) { throw 'Set VINTAGE_STORY_SRC (or pass -SourceRoot) to the folder with the vsapi, vssurvivalmod and vsessentialsmod clones.' }

$modinfo = Get-Content -Raw -Encoding UTF8 (Join-Path $repoRoot 'curiocatalogue/modinfo.json') | ConvertFrom-Json
$version = if ($GameVersion) { $GameVersion } else { $modinfo.dependencies.game }
Write-Host "Game version $version"

# file name -> list of @{ Repo; Commit; Path }
$index = @{}
foreach ($repo in $repos) {
    $dir = Join-Path $SourceRoot $repo
    $commit = git -C $dir log --format=%H -n 1 --fixed-strings --grep "Updated to Version $version" | Select-Object -First 1
    if (-not $commit) { throw "$repo has no commit 'Updated to Version $version'. Run: git -C $dir fetch" }
    Write-Host ("  {0} {1}" -f $repo, $commit.Substring(0, 7))
    foreach ($path in (git -C $dir ls-tree -r --name-only $commit)) {
        if ($path -notlike '*.cs') { continue }
        $name = Split-Path -Leaf $path
        if (-not $index.ContainsKey($name)) { $index[$name] = New-Object System.Collections.ArrayList }
        [void]$index[$name].Add(@{ Repo = $dir; Commit = $commit; Path = $path })
    }
}

$cache = @{}
function Get-SourceLines($candidate) {
    $key = $candidate.Repo + '|' + $candidate.Path
    if (-not $cache.ContainsKey($key)) {
        $text = git -C $candidate.Repo -c core.quotepath=off show ("{0}:{1}" -f $candidate.Commit, $candidate.Path)
        $cache[$key] = @($text)
    }
    return $cache[$key]
}

$ownFiles = @{}
Get-ChildItem -Path $repoRoot -Recurse -Filter *.cs -File | ForEach-Object { $ownFiles[$_.Name] = $true }

$pattern = '(?<file>[A-Za-z][A-Za-z0-9_.]*\.cs):(?<from>\d+)(?:-(?<to>\d+))?(?:#(?<sym>[A-Za-z_][A-Za-z0-9_.]*))?'
$failed = 0; $checked = 0; $warnings = 0

$files = Get-ChildItem -Path $repoRoot -Recurse -File -Include *.cs, *.md |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|notes|\.git)[\\/]' }

foreach ($file in $files) {
    $relative = $file.FullName.Substring($repoRoot.Length + 1)
    if ($relative -match $excluded) { continue }
    $lineNo = 0
    foreach ($line in (Get-Content -Encoding UTF8 $file.FullName)) {
        $lineNo++
        foreach ($m in [regex]::Matches($line, $pattern)) {
            $name = $m.Groups['file'].Value
            $where = "${relative}:$lineNo  $($m.Value)"
            # Pinned to another version on purpose: `vsapi@1.21.6 Collectible.cs:89#...`
            if ($line.Substring(0, $m.Index) -match '@\d+(\.\d+)+\S*\s+$') { continue }
            if (-not $index.ContainsKey($name)) {
                if ($ownFiles.ContainsKey($name)) { continue }
                Write-Host "FAIL  $where  -- no such file in the game source" -ForegroundColor Red; $failed++; continue
            }
            if (-not $m.Groups['sym'].Success) {
                Write-Host "WARN  $where  -- no #symbol" -ForegroundColor Yellow; $warnings++; continue
            }
            $from = [int]$m.Groups['from'].Value
            $to = if ($m.Groups['to'].Success) { [int]$m.Groups['to'].Value } else { $from }
            $symbol = $m.Groups['sym'].Value
            $checked++
            $found = $false
            foreach ($candidate in $index[$name]) {
                $source = Get-SourceLines $candidate
                for ($i = $from; $i -le $to -and $i -le $source.Count; $i++) {
                    if ($source[$i - 1].Contains($symbol)) { $found = $true; break }
                }
                if ($found) { break }
            }
            if (-not $found) {
                $candidate = $index[$name][0]
                $source = Get-SourceLines $candidate
                $actual = if ($from -le $source.Count) { $source[$from - 1].Trim() } else { '<past the end>' }
                if ($actual.Length -gt 100) { $actual = $actual.Substring(0, 100) + '...' }
                Write-Host "FAIL  $where  -- line $from of $($candidate.Path): $actual" -ForegroundColor Red
                $failed++
            }
        }
    }
}

Write-Host "$checked checked, $failed failed, $warnings without a symbol"
if ($failed -gt 0) { exit 1 }
