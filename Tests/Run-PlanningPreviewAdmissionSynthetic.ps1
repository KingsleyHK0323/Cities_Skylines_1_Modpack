# Signature: OpenAI-Codex_aptp_release_boundary.
# Compiles the actual request admission and ownership classes in memory; no game API is used.
$ErrorActionPreference = 'Stop'
$checkout = Split-Path -Parent $PSScriptRoot
$sourcePaths = @(
    (Join-Path $checkout 'Source/Planning/RouteEditRequestGate.cs'),
    (Join-Path $checkout 'Source/Planning/PlanningPreviewAdmission.cs'),
    (Join-Path $PSScriptRoot 'PlanningPreviewAdmissionChecks.cs')
)
$sources = $sourcePaths | ForEach-Object { Get-Content -LiteralPath $_ -Raw }
$imports = "using System;`nusing System.Threading;`nusing AutomatedPublicTransportPlanning.Planning;`n"
$combined = $imports + (($sources | ForEach-Object { [regex]::Replace($_, '(?m)^using [^\r\n]+;\r?$', '') }) -join "`n")
Add-Type -TypeDefinition $combined -ErrorAction Stop
$assertions = [PlanningPreviewAdmissionChecks]::Run()
Write-Output "PASS actual preview admission/ownership: $assertions assertions (controlled interleaving)."
