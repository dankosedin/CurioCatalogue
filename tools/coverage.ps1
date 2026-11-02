# Line coverage of the mod by all four test projects, merged per source file.
#
# Each project runs with coverlet (the collector every test project references). The server and compatibility tests run our code
# in the mod's output folder, where the game's mod loader loads it, not from the test bin, so coverlet is pointed there
# (IncludeDirectory). To instrument curiocatalogue.dll it must resolve VintagestoryAPI, which is not in that folder: the script
# copies it there for the run and removes it afterwards. The game does not load it from there: the mod loader takes only the
# DLL with a ModInfo attribute and a ModSystem (decompiled VintagestoryLib 1.22.7, ModContainer.LoadModInfoFromCode).
#
# Usage: tools/coverage.ps1 [-Out <folder>]   (default: TestResults/coverage, git-ignored). About 2 minutes.
# Writes <Out>/report.md: a table per file (unit = Core.Tests + Tests, server = ServerTests + CompatTests, all = any of them)
# and the lines no test runs. Exit code 1 if a test project fails (the report is still written from what ran).

param([string]$Out)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $Out) { $Out = Join-Path $repoRoot 'TestResults/coverage' }
if (-not $env:VINTAGE_STORY) { throw 'Set VINTAGE_STORY to the game folder (docs/onboarding.md).' }

if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
New-Item -ItemType Directory -Force $Out | Out-Null

Push-Location $repoRoot
try {
    dotnet build curiocatalogue.sln --nologo -v q | Out-Host
    if ($LASTEXITCODE -ne 0) { throw 'The build failed.' }

    $modDir = Join-Path $repoRoot 'curiocatalogue/bin/Debug/Mods/mod'
    $settings = Join-Path $Out 'server.runsettings'
    @"
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>cobertura</Format>
          <Include>[curiocatalogue]*,[CurioCatalogue.Core]*</Include>
          <IncludeDirectory>$modDir</IncludeDirectory>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
"@ | Set-Content -Encoding utf8 $settings

    $failed = $false
    $runs = [ordered]@{
        'core'   = @{ Project = 'CurioCatalogue.Core.Tests'; Group = 'unit'; Server = $false }
        'tests'  = @{ Project = 'CurioCatalogue.Tests'; Group = 'unit'; Server = $false }
        'server' = @{ Project = 'CurioCatalogue.ServerTests'; Group = 'server'; Server = $true }
        'compat' = @{ Project = 'CurioCatalogue.CompatTests'; Group = 'server'; Server = $true }
    }
    $api = Join-Path $modDir 'VintagestoryAPI.dll'
    try {
        foreach ($name in $runs.Keys) {
            $run = $runs[$name]
            $testArgs = @('test', $run.Project, '--no-build', '--nologo', '--results-directory', (Join-Path $Out $name))
            if ($run.Server) {
                Copy-Item (Join-Path $env:VINTAGE_STORY 'VintagestoryAPI.dll') $api
                $testArgs += @('--settings', $settings)
            } else {
                $testArgs += @('--collect', 'XPlat Code Coverage')
            }
            dotnet @testArgs | Select-String -Pattern '^(Passed|Failed)!' | Out-Host
            if ($LASTEXITCODE -ne 0) { $failed = $true }
            if (Test-Path $api) { Remove-Item $api }
        }
    }
    finally {
        if (Test-Path $api) { Remove-Item $api }
    }

    # Merge: per file and line, covered by the unit group, the server group, or any.
    $lines = @{}
    foreach ($name in $runs.Keys) {
        $file = Get-ChildItem -Recurse -Filter coverage.cobertura.xml (Join-Path $Out $name) -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $file) { Write-Warning "no coverage from $name"; continue }
        [xml]$xml = Get-Content -Raw $file.FullName
        $sources = @($xml.coverage.sources.source)
        foreach ($class in $xml.coverage.packages.package.classes.class) {
            $path = $class.filename
            if (-not [IO.Path]::IsPathRooted($path)) {
                foreach ($s in $sources) { $p = Join-Path $s $path; if (Test-Path $p) { $path = $p; break } }
            }
            if ($path.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) { $path = $path.Substring($repoRoot.Length).TrimStart('\', '/') }
            $path = $path -replace '\\', '/'
            foreach ($line in $class.lines.line) {
                $key = "$path|$($line.number)"
                if (-not $lines.ContainsKey($key)) { $lines[$key] = @{ unit = $false; server = $false } }
                if ([int]$line.hits -gt 0) { $lines[$key][$runs[$name].Group] = $true }
            }
        }
    }

    function Percent([int]$part, [int]$whole) { if ($whole -eq 0) { 0 } else { [math]::Round(100 * $part / $whole) } }
    function Ranges([int[]]$numbers) {
        $result = @(); $start = $null; $prev = $null
        foreach ($n in $numbers) {
            if ($null -eq $start) { $start = $n }
            elseif ($n -ne $prev + 1) { $result += $(if ($start -eq $prev) { "$start" } else { "$start-$prev" }); $start = $n }
            $prev = $n
        }
        if ($null -ne $start) { $result += $(if ($start -eq $prev) { "$start" } else { "$start-$prev" }) }
        $result -join ','
    }

    $report = @('| File | Lines | Unit % | Server % | All % | Missed |', '|---|---:|---:|---:|---:|---:|')
    $missed = @()
    foreach ($group in ($lines.GetEnumerator() | Group-Object { $_.Key.Split('|')[0] } | Sort-Object Name)) {
        $n = $group.Count
        $unit = @($group.Group | Where-Object { $_.Value.unit }).Count
        $server = @($group.Group | Where-Object { $_.Value.server }).Count
        $none = @($group.Group | Where-Object { -not ($_.Value.unit -or $_.Value.server) } | ForEach-Object { [int]$_.Key.Split('|')[1] } | Sort-Object)
        $report += "| $($group.Name) | $n | $(Percent $unit $n) | $(Percent $server $n) | $(Percent ($n - $none.Count) $n) | $($none.Count) |"
        if ($none.Count -gt 0) { $missed += "- $($group.Name): $(Ranges $none)" }
    }
    $total = $lines.Count
    $unitAll = @($lines.Values | Where-Object { $_.unit }).Count
    $serverAll = @($lines.Values | Where-Object { $_.server }).Count
    $none = @($lines.Values | Where-Object { -not ($_.unit -or $_.server) }).Count
    $report += "| **Total** | $total | $(Percent $unitAll $total) | $(Percent $serverAll $total) | $(Percent ($total - $none) $total) | $none |"
    $report += '', 'Lines no test runs:', ''
    $report += $missed

    $reportPath = Join-Path $Out 'report.md'
    $report | Set-Content -Encoding utf8 $reportPath
    $report | Out-Host
    Write-Host "Report: $reportPath"
    if ($failed) { exit 1 }
}
finally {
    Pop-Location
}
