# Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
# Developer-only native-shell build; reuses a previously verified local runtime cache.
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'DevManager.cs'
$hash = (Get-FileHash $source -Algorithm SHA256).Hash.Substring(0,12)
$output = Join-Path $PSScriptRoot "DevManager-dev-$hash.exe"
if (-not (Test-Path -LiteralPath $output)) {
  $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
  & $compiler /nologo /codepage:65001 /target:winexe /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll "/out:$output" $source
  if ($LASTEXITCODE -ne 0) { throw 'Native build failed.' }
}
$output
