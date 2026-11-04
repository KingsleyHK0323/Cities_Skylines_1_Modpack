# Signature: OpenAI-Codex_workshop_implementation
# This script is intended for a disposable GitHub-hosted Windows runner only.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourcePath,
    [Parameter(Mandatory)][string]$SourceSha,
    [Parameter(Mandatory)][string]$GameRoot,
    [Parameter(Mandatory)][string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -cne 'true' -or $env:RUNNER_ENVIRONMENT -cne 'github-hosted') {
    throw 'Release compilation requires a disposable GitHub-hosted runner.'
}
if ($SourceSha -cnotmatch '^[0-9a-f]{40}$') { throw 'Source SHA is invalid.' }
$source = [IO.Path]::GetFullPath($SourcePath)
$temp = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd([IO.Path]::DirectorySeparatorChar)
$workspace = [IO.Path]::GetFullPath($env:GITHUB_WORKSPACE).TrimEnd([IO.Path]::DirectorySeparatorChar)
$game = [IO.Path]::GetFullPath($GameRoot)
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $source.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    -not $game.StartsWith($temp + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    -not $output.StartsWith($temp + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Source, references and output must be within their expected runner directories.'
}
if (Test-Path -LiteralPath $output) { throw 'Release output already exists; refusing to overwrite.' }
if (-not (Test-Path -LiteralPath (Join-Path $game 'Cities_Data/Managed/ICities.dll') -PathType Leaf)) {
    throw 'Verified game references are missing.'
}
$actualSha = (& git -C $source rev-parse --verify 'HEAD^{commit}').Trim()
if ($LASTEXITCODE -ne 0 -or $actualSha -cne $SourceSha) { throw 'Checked-out source differs from the reviewed SHA.' }
$dirty = & git -C $source status --porcelain=v1 --untracked-files=all
if ($LASTEXITCODE -ne 0 -or -not [string]::IsNullOrWhiteSpace(($dirty -join "`n"))) {
    throw 'The source checkout is not clean.'
}
# Any change to the reviewed tool gate or dev-only sources requires another controller review.
$boundaryBaseline = '0fd60bcdd986e6e2ae40d162c326f1232c1bf639'
& git -C $source merge-base --is-ancestor $boundaryBaseline $SourceSha
if ($LASTEXITCODE -ne 0) { throw 'The source is not descended from the reviewed DevTool boundary.' }
& git -C $source diff --quiet $boundaryBaseline $SourceSha -- AIImprove.csproj AIImproveMod.cs dev/
if ($LASTEXITCODE -ne 0) { throw 'The reviewed DevTool boundary changed and requires review.' }
$modsOutput = Join-Path $temp ('ai-improve-cloud-mods-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $modsOutput -ErrorAction Stop | Out-Null
& dotnet build (Join-Path $source 'AIImprove.csproj') --configuration Release --no-incremental `
    '-p:DevTools=false' "-p:CSInstallPath=$game" "-p:CitiesSkylinesDir=$game" "-p:CSDataPath=$modsOutput"
if ($LASTEXITCODE -ne 0) { throw 'Cloud release compilation failed.' }
$outputFiles = @{
    'AIImprove.dll' = Join-Path $source 'bin/Release/AIImprove.dll'
    'CitiesHarmony.API.dll' = Join-Path $source 'bin/Release/CitiesHarmony.API.dll'
    'CitiesHarmony-LICENSE.txt' = Join-Path $source 'third-party/CitiesHarmony-LICENSE.txt'
    'THIRD-PARTY-NOTICES.md' = Join-Path $source 'third-party/THIRD-PARTY-NOTICES.md'
}
foreach ($item in $outputFiles.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $item.Value -PathType Leaf) -or (Get-Item -LiteralPath $item.Value).Length -le 0) {
        throw "Required release file is missing or empty: $($item.Key)"
    }
}
if ([Reflection.AssemblyName]::GetAssemblyName($outputFiles['AIImprove.dll']).Name -cne 'AIImprove' -or
    [Reflection.AssemblyName]::GetAssemblyName($outputFiles['CitiesHarmony.API.dll']).Name -cne 'CitiesHarmony.API') {
    throw 'Managed assembly identities do not match AI Improve and its required API.'
}
$bytes = [IO.File]::ReadAllBytes($outputFiles['AIImprove.dll'])
$ascii = [Text.Encoding]::ASCII.GetString($bytes)
$unicode = [Text.Encoding]::Unicode.GetString($bytes)
foreach ($marker in @('DevTriggerExtension','LateMergeProbe','SanitationLifecycleProbe',
                      'TryGetAssignedBuilding','This version has DevTool')) {
    if ($ascii.Contains($marker) -or $unicode.Contains($marker)) {
        throw "Release DLL contains a reviewed Development-only marker: $marker"
    }
}
New-Item -ItemType Directory -Path $output -ErrorAction Stop | Out-Null
foreach ($item in $outputFiles.GetEnumerator()) {
    Copy-Item -LiteralPath $item.Value -Destination (Join-Path $output $item.Key) -ErrorAction Stop
}
Write-Output 'Built AI Improve without the reviewed DevTool sources and prepared the exact release dependency set.'
