# Signature: OpenAI-Codex.
[CmdletBinding()]
param(
    [ValidateSet('DO NOT RUN', 'APPLY REVIEWED DESCRIPTION')]
    [string]$Mode = 'DO NOT RUN',
    [Parameter(Mandatory = $true)][System.Management.Automation.PSModuleInfo]$PlanModule,
    [Parameter(Mandatory = $true)][System.Management.Automation.PSModuleInfo]$MetadataModule
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module $PlanModule -Force
Import-Module $MetadataModule -Force
Test-RewriteRunContext -Context ([Environment]::GetEnvironmentVariables()) -Mode $Mode | Out-Null

$manifestHash = '801d699e71a0ca1b907cf4d997709678737a7e9589564420f4fbd1cb4d9fc9dd'
$draftHash = 'd71cdd62f2e1b389c828f7d9d1d774453c7175b2e7f43817cd3bd5d2583c2380'
$expectedRoot = [IO.Path]::GetFullPath('D:\Code\Game Code\Cities_Skyline_1_Modpack').TrimEnd('\')
if ([string]::IsNullOrWhiteSpace($env:MODPACK_ROOT) -or
    [IO.Path]::GetFullPath($env:MODPACK_ROOT).TrimEnd('\') -ine $expectedRoot) { throw 'The configured project root is not the reviewed root.' }
if ([string]::IsNullOrWhiteSpace($env:REWRITE_MODULE) -or $env:REWRITE_ISSUE_NUMBER -notmatch '^[1-9][0-9]{0,9}$' -or
    $env:GITHUB_SHA -cnotmatch '^[0-9a-f]{40}$' -or [string]::IsNullOrWhiteSpace($env:GH_TOKEN)) {
    throw 'The selected module, owner issue, controller commit or token is invalid.'
}
$encoding = [Text.UTF8Encoding]::new($false, $true)
$resourceRoot = Join-Path $expectedRoot 'Resource'
function Assert-RewriteProjectDirectory([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path).TrimEnd('\')
    if ($full -ine $expectedRoot -and -not $full.StartsWith($expectedRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'A required local directory is outside the approved project root.'
    }
    $current = $full
    while ($true) {
        $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
        if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw 'A required local directory is unavailable or is a reparse point.'
        }
        if ($current -ieq $expectedRoot) { break }
        $parent = Split-Path -Parent $current
        if ([string]::IsNullOrWhiteSpace($parent) -or $parent -ieq $current) { throw 'The directory ancestry is invalid.' }
        $current = $parent.TrimEnd('\')
    }
}
Assert-RewriteProjectDirectory $resourceRoot
$manifestPath = Join-Path $resourceRoot 'Steam_Workshop_Descriptions_Role_Rewrite_2026-11-05_Review_Manifest.json'
$draftPath = Join-Path $resourceRoot 'Steam_Workshop_Descriptions_Role_Rewrite_2026-11-05_Review.md'
foreach ($path in @($manifestPath, $draftPath)) {
    $file = Get-Item -LiteralPath $path -ErrorAction Stop
    if ($file.PSIsContainer -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'A reviewed input is not a regular local file.' }
}

function Get-CurrentRewritePlan {
    $manifestText = [IO.File]::ReadAllText($manifestPath, $encoding)
    $draftText = [IO.File]::ReadAllText($draftPath, $encoding)
    return New-RewriteDescriptionPlan -ManifestJson $manifestText -ExpectedManifestHash $manifestHash -Draft $draftText -ExpectedDraftHash $draftHash -Module $env:REWRITE_MODULE -ExpectedItemCount 15
}
function Assert-CurrentRewriteIssue($Plan) {
    $headers = @{ Authorization = 'Bearer ' + $env:GH_TOKEN; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
    $url = 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/' + $env:REWRITE_ISSUE_NUMBER
    $issue = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -MaximumRedirection 0 -TimeoutSec 20
    $events = @(Invoke-RestMethod -Method Get -Uri ($url + '/events?per_page=100') -Headers $headers -MaximumRedirection 0 -TimeoutSec 20)
    Test-RewriteBatchOwnerIssue -Issue $issue -Events $events -IssueNumber $env:REWRITE_ISSUE_NUMBER -ManifestHash $manifestHash -DraftHash $draftHash -ControllerCommit $env:GITHUB_SHA | Out-Null
}

$plan = Get-CurrentRewritePlan
Assert-CurrentRewriteIssue $plan
foreach ($name in @('STEAMCMD_EXE', 'STEAMCMD_USERNAME', 'RUNNER_TEMP')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) { throw 'The existing SteamCMD runner configuration is incomplete.' }
}
if ($env:STEAMCMD_USERNAME -match '\s') { throw 'The configured SteamCMD username is invalid.' }
$steamCmd = [IO.Path]::GetFullPath($env:STEAMCMD_EXE)
if (-not (Test-Path -LiteralPath $steamCmd -PathType Leaf)) { throw 'The configured SteamCMD executable was not found.' }
$steamCmdRoot = Split-Path -Parent $steamCmd
if (-not (Test-Path -LiteralPath (Join-Path $steamCmdRoot 'config\config.vdf') -PathType Leaf)) { throw 'SteamCMD has no saved login configuration.' }
$runnerTemp = [IO.Path]::GetFullPath($env:RUNNER_TEMP)
if (-not (Test-Path -LiteralPath $runnerTemp -PathType Container)) { throw 'RUNNER_TEMP is not an existing directory.' }
Assert-RewriteProjectDirectory $runnerTemp
$stage = Join-Path $resourceRoot 'Workshop_Metadata_Staging_2026-11-05'
if (-not (Test-Path -LiteralPath $stage -PathType Container)) { throw 'The approved claim directory does not exist.' }
Assert-RewriteProjectDirectory $stage
$claimName = 'rewrite-' + (Get-RewriteItemClaimName -WorkshopItemId $plan.WorkshopItemId -DescriptionHash $plan.DescriptionHash)
$claimPath = Join-Path $stage $claimName
if (Test-Path -LiteralPath $claimPath) { throw 'This item and description were already attempted; inspect Steam before any retry.' }

$vdfPath = Join-Path $runnerTemp ('workshop-description-rewrite-' + [Guid]::NewGuid().ToString('N') + '.vdf')
$vdfStream = [IO.File]::Open($vdfPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try { $bytes = $encoding.GetBytes($plan.Vdf); $vdfStream.Write($bytes, 0, $bytes.Length) } finally { $vdfStream.Dispose() }
$vdfLock = [IO.File]::Open($vdfPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = $steamCmd
$start.WorkingDirectory = $steamCmdRoot
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$start.Environment.Clear()
$childEnvironment = Get-SteamCmdEnvironment ([Environment]::GetEnvironmentVariables())
foreach ($name in $childEnvironment.Keys) { $start.Environment[$name] = [string]$childEnvironment[$name] }
$start.ArgumentList.Add('+login')
$start.ArgumentList.Add($env:STEAMCMD_USERNAME)
$start.ArgumentList.Add('+workshop_build_item')
$start.ArgumentList.Add($vdfPath)
$start.ArgumentList.Add('+quit')

Assert-CurrentRewriteIssue $plan
$rechecked = Get-CurrentRewritePlan
if ($rechecked.DescriptionHash -cne $plan.DescriptionHash -or $rechecked.WorkshopItemId -cne $plan.WorkshopItemId -or
    $rechecked.Vdf -cne $plan.Vdf -or [IO.File]::ReadAllText($vdfPath, $encoding) -cne $plan.Vdf) {
    throw 'The reviewed plan or VDF changed before submission.'
}

# A claim is created before SteamCMD starts and is never removed automatically.
$claim = [IO.File]::Open($claimPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $claimBytes = $encoding.GetBytes('Description rewrite attempted. The Steam result is unverified; inspect the item before retrying. Signature: OpenAI-Codex.')
    $claim.Write($claimBytes, 0, $claimBytes.Length)
    $claim.Flush($true)
} finally { $claim.Dispose() }
try { $process = [Diagnostics.Process]::Start($start) } catch { throw 'SteamCMD could not be started. The claim is retained; inspect the item before any retry.' }
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
if (-not $process.WaitForExit(180000)) {
    try { $process.Kill() } catch { }
    throw 'SteamCMD timed out. The submission outcome is unknown; inspect the item before any retry.'
}
$stdout.GetAwaiter().GetResult() | Out-Null
$stderr.GetAwaiter().GetResult() | Out-Null
if ($process.ExitCode -ne 0) { throw 'SteamCMD exited with an error. The submission outcome is unknown; inspect the item before any retry.' }
throw 'SteamCMD returned, but Steam acceptance and the final description are unverified. Inspect the item before any retry.'
} finally { $vdfLock.Dispose() }
