# Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
# Requires an externally supplied official Node runtime; verifies it without installation.
param([string]$OutputDirectory, [string]$NodePath = (Get-Command node -ErrorAction Stop).Source, [string]$RuntimeCachePath)
$ErrorActionPreference = 'Stop'
$appRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $PSScriptRoot ('portable-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,6)) }
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Output already exists; choose a new directory. Existing packages are preserved.' }
$runtime = Join-Path $output 'runtime'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) { throw 'The existing Windows .NET Framework compiler is required for this developer build.' }
$expectedRuntime = '1c331340fa6cac5e4294725959070a143fa0507195a78ad77eed7943f719aac9'
if ((Get-FileHash -LiteralPath $NodePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedRuntime) { throw 'Runtime does not match the official Node win-x64 checksum.' }
$version = (& $NodePath --version).Trim()
if ($LASTEXITCODE -ne 0 -or $version -ne 'v24.22.2') { throw 'This portable recipe requires the official Node.js v24.22.2 win-x64 runtime.' }
if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'Node-v24.22.2-LICENSE.txt') -Algorithm SHA256).Hash.ToLowerInvariant() -ne '5888dbb9a1d2b18f2c3e6c5f6af1b39de658372b402a0577b002777f14c62ace') { throw 'Node license does not match the verified official text.' }
$checksums = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Node-v24.22.2-SHASUMS256.txt'), [Text.Encoding]::UTF8)
if ($checksums -notmatch ('(?m)^' + $expectedRuntime + '  win-x64/node\.exe\r?$')) { throw 'Official checksum reference does not match the pinned runtime.' }
New-Item -ItemType Directory -Force -Path $runtime | Out-Null
$exe = Join-Path $output 'DevManager.exe'
& $compiler /nologo /codepage:65001 /target:winexe /optimize+ /platform:anycpu /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll "/out:$exe" (Join-Path $PSScriptRoot 'DevManager.cs')
if ($LASTEXITCODE -ne 0) { throw 'Desktop compilation failed.' }
if ($RuntimeCachePath) {
  if ((Get-FileHash -LiteralPath $RuntimeCachePath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedRuntime) { throw 'Unverified runtime cache.' }
  New-Item -ItemType HardLink -Path (Join-Path $runtime 'node.exe') -Target $RuntimeCachePath | Out-Null
} else { Copy-Item -LiteralPath $NodePath -Destination (Join-Path $runtime 'node.exe') }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Node-v24.22.2-LICENSE.txt') -Destination (Join-Path $runtime 'LICENSE_NODE.txt') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'THIRD-PARTY.md') -Destination (Join-Path $output 'THIRD-PARTY.md') -Force
$sources = @('core.mjs','filesystem.mjs','github-source.mjs','download.mjs','https-source.mjs','module-policy.mjs','release-index.mjs','bundle-source.mjs','update-order.mjs','asset-policy.mjs','desktop-service.mjs','desktop-backend.mjs','registry.mjs','LICENSE','README.md','USER-GUIDE.md','PUBLISHER-CONTRACT.md','SOURCE-CONTRACT.md','Change Log.md')
$sources += 'artifact-source.mjs'
$sources += 'ARTIFACT-CONTRACT.md'
$sources += @('preferences.mjs','localization.mjs','translations.json')
$sources += 'settings-store.mjs'
$sources += 'TEST-READINESS.md'
foreach ($name in $sources) { Copy-Item -LiteralPath (Join-Path $appRoot $name) -Destination (Join-Path $output $name) -Force }
$manifest = [ordered]@{ schema=1; runtime=$version; runtimeSHA256=(Get-FileHash (Join-Path $runtime 'node.exe') -Algorithm SHA256).Hash; nativeSHA256=(Get-FileHash $exe -Algorithm SHA256).Hash; signature='OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager'; files=@{} }
foreach ($name in $sources) { $manifest.files[$name] = (Get-FileHash (Join-Path $output $name) -Algorithm SHA256).Hash }
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'package-manifest.json') -Encoding utf8
'Portable folder ready: ' + $output
'Open DevManager.exe directly; no separate Node installation is required on the destination Windows PC.'
