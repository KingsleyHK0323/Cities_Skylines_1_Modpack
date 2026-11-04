# Signature: OpenAI-Codex.
# Compiles a validated selection only on a disposable GitHub-hosted runner.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$GameRoot,
    [Parameter(Mandatory)][string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -cne 'true' -or $env:RUNNER_ENVIRONMENT -cne 'github-hosted') {
    throw 'Batch compilation requires a GitHub-hosted runner.'
}
$workspace = [IO.Path]::GetFullPath($env:GITHUB_WORKSPACE).TrimEnd([IO.Path]::DirectorySeparatorChar)
$temp = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd([IO.Path]::DirectorySeparatorChar)
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $output.StartsWith($temp + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Batch output must be inside runner temporary storage.'
}
$planText = & node (Join-Path $PSScriptRoot 'batch.mjs')
if ($LASTEXITCODE -ne 0) { throw 'The approved module plan was rejected.' }
$plan = @($planText | ConvertFrom-Json)
if ($plan.Count -lt 1 -or $plan.Count -gt 15) { throw 'The module count is invalid.' }
$sources = Join-Path $workspace 'workshop-module-sources'
if ((Test-Path -LiteralPath $sources) -or (Test-Path -LiteralPath $output)) {
    throw 'A batch destination already exists; refusing to overwrite.'
}
New-Item -ItemType Directory -Path $sources -ErrorAction Stop | Out-Null
New-Item -ItemType Directory -Path $output -ErrorAction Stop | Out-Null
foreach ($entry in $plan) {
    $module = [string]$entry.module
    $sourceSha = [string]$entry.sourceSha
    if ($module -cnotmatch '^[A-Za-z0-9_]{1,100}$' -or $sourceSha -cnotmatch '^[0-9a-f]{40}$') {
        throw 'The validated plan contains an invalid module or source SHA.'
    }
    $source = Join-Path $sources $module
    $package = Join-Path $output $module
    New-Item -ItemType Directory -Path $source -ErrorAction Stop | Out-Null
    New-Item -ItemType Directory -Path $package -ErrorAction Stop | Out-Null
    & git -C $source init --quiet
    if ($LASTEXITCODE -ne 0) { throw "Source repository initialization failed: $module" }
    & git -C $source remote add origin 'https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack.git'
    if ($LASTEXITCODE -ne 0) { throw "Source remote configuration failed: $module" }
    # Preserve history: AI Improve derives part of its assembly version from the commit count.
    & git -C $source fetch --no-tags origin $sourceSha
    if ($LASTEXITCODE -ne 0) { throw "The approved source could not be fetched: $module" }
    & git -C $source checkout --detach --quiet FETCH_HEAD
    if ($LASTEXITCODE -ne 0) { throw "The approved source could not be checked out: $module" }
    $env:WORKSHOP_MODULE_KEY = $module
    $content = Join-Path $package 'content'
    if ($module -ceq 'AI_Improve') {
        & (Join-Path $PSScriptRoot 'Build-AIImprove.ps1') -SourcePath $source -SourceSha $sourceSha -GameRoot $GameRoot -OutputDirectory $content
    } else {
        & (Join-Path $PSScriptRoot 'Build-Reviewed.ps1') -Module $module -SourcePath $source -SourceSha $sourceSha -GameRoot $GameRoot -OutputDirectory $content
    }
    & node (Join-Path $PSScriptRoot 'package.mjs') create $content (Join-Path $package 'manifest.json')
    if ($LASTEXITCODE -ne 0) { throw "Release package validation failed: $module" }
}
Write-Output 'Each selected reviewed module was built and packaged; upload has not started.'
