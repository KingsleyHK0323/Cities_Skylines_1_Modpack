# Signature: OpenAI-Codex_lifecycle_scope_review
# Independent source list. This script does not launch the game, Steam, a runner, or a workflow.
param(
    [Parameter(Mandatory=$true)][string]$CompilerPath,
    [Parameter(Mandatory=$true)][string]$ReferenceDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $CompilerPath -PathType Leaf)) { throw 'Compiler path does not exist.' }
if (-not (Test-Path -LiteralPath $OutputDirectory -PathType Container)) { throw 'Output directory must already exist.' }
$moduleDirectory = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$validationDirectory = Join-Path $moduleDirectory 'Local_Validation_20261113_CityTime'
if (-not (Test-Path -LiteralPath $validationDirectory -PathType Container)) { throw 'Authorized validation directory does not exist.' }
$validationPath = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $validationDirectory).ProviderPath).TrimEnd([char[]]@('\','/'))
$resolvedOutputPath = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $OutputDirectory).ProviderPath).TrimEnd([char[]]@('\','/'))
$allowedPrefix = $validationPath + [System.IO.Path]::DirectorySeparatorChar
if (-not [String]::Equals($resolvedOutputPath, $validationPath, [StringComparison]::OrdinalIgnoreCase) -and
    -not $resolvedOutputPath.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Output directory must remain inside the authorized validation directory.'
}
$checkedPath = $resolvedOutputPath
while ($true) {
    $entry = Get-Item -LiteralPath $checkedPath -Force
    if (($entry.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'Output directory and its ancestors must not contain reparse points.'
    }
    $parent = [System.IO.Directory]::GetParent($entry.FullName)
    if ($null -eq $parent) { break }
    $checkedPath = $parent.FullName
}
# These checks cannot prevent another process from changing the filesystem after validation.
$output = Join-Path $OutputDirectory 'ControlledReadOnlyClockDiagnostic.dll'
if (Test-Path -LiteralPath $output) { throw 'Output already exists; use a fresh output directory.' }
$compilerArgs = @('/noconfig','/nostdlib+','/target:library','/langversion:5','/optimize+')
$compilerArgs += '/debug-'
foreach ($name in @('mscorlib.dll','System.dll','UnityEngine.dll','ColossalManaged.dll','ICities.dll','Assembly-CSharp.dll')) {
    $path = Join-Path $ReferenceDirectory $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing approved reference: $name" }
    $compilerArgs += "/reference:$path"
}
$diagnosticsDirectory = Split-Path -Parent $PSScriptRoot
$compilerArgs += "/out:$output"
$compilerArgs += (Join-Path $diagnosticsDirectory 'ClockDiagnostics.cs')
$compilerArgs += (Join-Path $diagnosticsDirectory 'ClockDiagnosticManual.cs')
$compilerArgs += (Join-Path $PSScriptRoot 'ControlledReadOnlyHost.cs')
$compilerArgs += (Join-Path $PSScriptRoot 'ControlledDiagnosticUi.cs')
$compilerArgs += (Join-Path $PSScriptRoot 'ControlledReadOnlyGameEntry.cs')
& $CompilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "Independent diagnostic compilation failed: $LASTEXITCODE" }
if (-not (Test-Path -LiteralPath $output -PathType Leaf)) { throw 'Compiler produced no diagnostic assembly.' }
Get-FileHash -LiteralPath $output -Algorithm SHA256 | Select-Object Hash
