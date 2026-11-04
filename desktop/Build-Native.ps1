# Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
# Developer-only native-shell build. Identity-only mode never compiles or writes.
param([switch]$IdentityOnly, [string]$IdentitySourcePath, [string]$IdentityIconPath, [string]$OutputDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
if (-not $IdentityOnly -and ($IdentitySourcePath -or $IdentityIconPath)) { throw 'Alternate inputs are permitted only for an identity-only check.' }
$source = if ($IdentityOnly -and $IdentitySourcePath) { $IdentitySourcePath } else { Join-Path $PSScriptRoot 'DevManager.cs' }
$icon = if ($IdentityOnly -and $IdentityIconPath) { $IdentityIconPath } else { Join-Path $PSScriptRoot 'DevManager.ico' }
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw 'Native source file is missing.' }
if (-not (Test-Path -LiteralPath $icon -PathType Leaf)) { throw 'Required app icon is missing: desktop/DevManager.ico.' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) { throw 'The existing Windows .NET Framework compiler is required.' }
function Get-StreamHash([IO.Stream]$stream) {
  $stream.Position = 0
  $value = (Get-FileHash -InputStream $stream -Algorithm SHA256).Hash
  $stream.Position = 0
  return $value
}
$sourceLease = $null
$iconLease = $null
try {
# Keep both inputs read-locked until cache selection or compilation completes.
$sourceLease = [IO.File]::Open($source,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
$iconLease = [IO.File]::Open($icon,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
$sourceHash = Get-StreamHash $sourceLease
$iconHash = Get-StreamHash $iconLease
$scriptHash = (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
$compilerHash = (Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash
$identityInput = [Text.Encoding]::UTF8.GetBytes("$sourceHash|$iconHash|$scriptHash|$compilerHash")
$sha = [Security.Cryptography.SHA256]::Create()
try { $identity = -join ($sha.ComputeHash($identityInput) | ForEach-Object { $_.ToString('x2') }) } finally { $sha.Dispose() }
$root = [IO.Path]::GetFullPath($OutputDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar,[IO.Path]::AltDirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $root -PathType Container)) { throw 'Native output directory must already exist.' }
$output = [IO.Path]::GetFullPath((Join-Path $root "DevManager-dev-$identity.exe"))
if (-not [StringComparer]::OrdinalIgnoreCase.Equals([IO.Path]::GetDirectoryName($output),$root)) { throw 'Invalid native output location.' }
if ($IdentityOnly) { $output; return }
if (Test-Path -LiteralPath $output) {
  $cached = Get-Item -LiteralPath $output -Force
  if ($cached.PSIsContainer -or $cached.Length -eq 0 -or ($cached.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Native cache output is not a regular nonempty file.' }
  $output; return
}
$staging = [IO.Path]::GetFullPath((Join-Path $root ('DevManager-build-' + [Guid]::NewGuid().ToString('N') + '.exe')))
if (-not [StringComparer]::OrdinalIgnoreCase.Equals([IO.Path]::GetDirectoryName($staging),$root)) { throw 'Invalid native staging location.' }
& $compiler /nologo /codepage:65001 /target:winexe /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll "/win32icon:$icon" "/out:$staging" $source
if ($LASTEXITCODE -ne 0) { throw 'Native build failed; any new staging output is retained for inspection.' }
if ((Get-StreamHash $sourceLease) -ne $sourceHash -or
    (Get-StreamHash $iconLease) -ne $iconHash -or
    (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash -ne $scriptHash -or
    (Get-FileHash -LiteralPath $compiler -Algorithm SHA256).Hash -ne $compilerHash) {
  throw 'Native build inputs changed; staging output is retained and cannot enter the cache.'
}
[IO.File]::Move($staging,$output)
$output
} finally {
  if ($iconLease) { $iconLease.Dispose() }
  if ($sourceLease) { $sourceLease.Dispose() }
}
