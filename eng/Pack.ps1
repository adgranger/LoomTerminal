<#
.SYNOPSIS
    Packs the four LoomTerminal libraries into artifacts/packages and checks the content of every package.
.DESCRIPTION
    Not published anywhere: publishing to nuget.org is a manual decision. Fails with the list of all problems found.
#>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'artifacts/packages'
$projects = 'Core', 'Pty', 'Presentation', 'WinUI'
$sourceLinkUrl = 'https://raw.githubusercontent.com/adgranger/LoomTerminal/'

New-Item -ItemType Directory -Force $out | Out-Null
Get-ChildItem $out -Include '*.nupkg', '*.snupkg' -Recurse | Remove-Item -Force

foreach ($p in $projects) {
    dotnet pack (Join-Path $root "src/LoomTerminal.$p") -c Release -o $out -warnaserror
    if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed for LoomTerminal.$p" }
}

$version = (dotnet msbuild (Join-Path $root 'src/LoomTerminal.Core') -getProperty:PackageVersion).Trim()
$problems = [System.Collections.Generic.List[string]]::new()

# Expected dependency ids per package; LoomTerminal ids must have the package version.
$dependencies = @{
    Core         = @()
    Pty          = @()
    Presentation = @('LoomTerminal.Core', 'LoomTerminal.Pty')
    WinUI        = @('LoomTerminal.Presentation', 'Microsoft.WindowsAppSDK', 'Microsoft.Graphics.Win2D')
}

function Read-Entry($entry) {
    $reader = [System.IO.StreamReader]::new($entry.Open())
    try { $reader.ReadToEnd() } finally { $reader.Dispose() }
}

foreach ($ext in 'nupkg', 'snupkg') {
    $files = @(Get-ChildItem $out -Filter "*.$ext")
    if ($files.Count -ne 4) { $problems.Add("expected 4 .$ext files, found $($files.Count)") }
}

foreach ($p in $projects) {
    $id = "LoomTerminal.$p"
    foreach ($ext in 'nupkg', 'snupkg') {
        $path = Join-Path $out "$id.$version.$ext"
        if (-not (Test-Path $path)) { $problems.Add("$id.$version.$ext is missing"); continue }
        $zip = [System.IO.Compression.ZipFile]::OpenRead($path)
        try {
            $names = @($zip.Entries | ForEach-Object FullName)
            $tfmDirs = @($names | Where-Object { $_ -match '^lib/[^/]+/' } | ForEach-Object { ($_ -split '/')[1] } | Sort-Object -Unique)
            if ($tfmDirs.Count -eq 0) { $problems.Add("$id.$ext has no lib/<tfm>/ folder"); continue }
            $tfm = $tfmDirs[0]
            $need = if ($ext -eq 'nupkg') {
                @('README.md', 'NOTICE.md', "lib/$tfm/$id.dll", "lib/$tfm/$id.xml")
            } else {
                @("lib/$tfm/$id.pdb")
            }
            foreach ($n in $need) {
                if ($n -notin $names) { $problems.Add("$id.$ext misses $n") }
            }

            if ($ext -eq 'snupkg') {
                $pdb = $zip.GetEntry("lib/$tfm/$id.pdb")
                if ($pdb) {
                    $ms = [System.IO.MemoryStream]::new()
                    $s = $pdb.Open(); $s.CopyTo($ms); $s.Dispose()
                    $text = [System.Text.Encoding]::Latin1.GetString($ms.ToArray())
                    if (-not $text.Contains($sourceLinkUrl)) { $problems.Add("$id.pdb has no SourceLink map ($sourceLinkUrl)") }
                }
                continue
            }

            $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
            if (-not $nuspecEntry) { $problems.Add("$id.nupkg has no .nuspec"); continue }
            $nuspec = [xml](Read-Entry $nuspecEntry)
            $meta = $nuspec.package.metadata
            if ($meta.license.type -ne 'expression' -or $meta.license.'#text' -ne 'MIT') {
                $problems.Add("$id.nuspec: license is not the MIT expression")
            }
            if (-not $meta.repository -or [string]::IsNullOrWhiteSpace($meta.repository.commit)) {
                $problems.Add("$id.nuspec: repository has no commit")
            }

            $found = @{}
            foreach ($group in @($meta.dependencies.group)) {
                foreach ($d in @($group.dependency)) { if ($d) { $found[$d.id] = $d.version } }
            }
            foreach ($d in $dependencies[$p]) {
                if (-not $found.ContainsKey($d)) { $problems.Add("$id.nuspec: dependency $d is missing"); continue }
                if ($d -like 'LoomTerminal.*' -and $found[$d] -notmatch "^\[?$([regex]::Escape($version))") {
                    $problems.Add("$id.nuspec: dependency $d is $($found[$d]), expected $version")
                }
            }
            foreach ($d in $found.Keys) {
                if ($d -notin $dependencies[$p]) { $problems.Add("$id.nuspec: unexpected dependency $d") }
            }

            if ($p -eq 'WinUI') {
                foreach ($n in 'buildTransitive/LoomTerminal.WinUI.targets', "lib/$tfm/LoomTerminal.WinUI.pri") {
                    if ($n -notin $names) { $problems.Add("$id.nupkg misses $n") }
                }
                if (-not ($names | Where-Object { $_ -like '*Themes/Generic.xaml' -or $_ -like '*Themes/Generic.xbf' })) {
                    $problems.Add("$id.nupkg misses Themes/Generic.xaml (or .xbf)")
                }
            }
        } finally { $zip.Dispose() }
    }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    throw "$($problems.Count) package problem(s) found"
}

Write-Host "Packed and checked: $version"
Get-ChildItem $out | ForEach-Object { Write-Host $_.Name }
