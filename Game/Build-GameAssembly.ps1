# Signature: OpenAI-Codex_Realistic_City_Time
# Update signature: OpenAI-Codex
# Update signature: OpenAI-Codex_boundary_host
# Cloud-only compiler entry point. This script does not obtain or publish reference assemblies.
param(
    [Parameter(Mandatory=$true)][string]$CompilerPath,
    [Parameter(Mandatory=$true)][string]$ReferenceDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$Channel = 'Unspecified'
)
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'This compiler entry point is reserved for approved GitHub-hosted validation.' }
$channelDefine = $null
if ([string]::Equals($Channel, 'Development', [StringComparison]::Ordinal)) {
    $channelDefine = '/define:RCT_CHANNEL_DEVELOPMENT'
} elseif ([string]::Equals($Channel, 'Release', [StringComparison]::Ordinal)) {
    $channelDefine = '/define:RCT_CHANNEL_RELEASE'
} elseif (-not [string]::Equals($Channel, 'Unspecified', [StringComparison]::Ordinal)) {
    throw 'Invalid build channel.'
}
if (-not (Test-Path -LiteralPath $CompilerPath -PathType Leaf)) { throw 'Compiler path does not exist.' }
$references = @('mscorlib.dll','System.dll','UnityEngine.dll','ColossalManaged.dll','ICities.dll','Assembly-CSharp.dll')
$compilerArgs = @('/noconfig','/nostdlib+','/target:library','/langversion:7.3','/optimize+','/deterministic+')
if ($null -ne $channelDefine) { $compilerArgs += $channelDefine }
foreach ($name in $references) {
    $path = Join-Path $ReferenceDirectory $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing approved reference: $name" }
    $compilerArgs += "/reference:$path"
}
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$output = Join-Path $OutputDirectory 'RealisticCityTime.dll'
if (Test-Path -LiteralPath $output) { throw 'Output already exists; use a fresh build directory.' }
$compilerArgs += "/out:$output"
$compilerArgs += (Join-Path $PSScriptRoot 'GameClockState.cs'), (Join-Path $PSScriptRoot 'GameMod.cs')
$compilerArgs += (Join-Path $PSScriptRoot 'ClockControlBoundary.cs')
$compilerArgs += (Join-Path $PSScriptRoot 'PortableClockScaler.cs')
$compilerArgs += (Join-Path $PSScriptRoot 'ClockRateSettings.cs')
$compilerArgs += (Join-Path $PSScriptRoot 'ClockRateInput.cs')
$compilerArgs += (Join-Path $PSScriptRoot 'ClockRateSettingsUi.cs')
& dotnet $CompilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "Game assembly compilation failed: $LASTEXITCODE" }
if (-not (Test-Path -LiteralPath $output -PathType Leaf)) { throw 'Compiler produced no game assembly.' }
Get-FileHash -LiteralPath $output -Algorithm SHA256 | Select-Object Hash
