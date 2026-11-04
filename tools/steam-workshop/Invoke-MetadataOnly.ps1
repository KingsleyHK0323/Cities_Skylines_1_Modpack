# Signature: OpenAI-Codex.
[CmdletBinding()]
param(
    [ValidateSet('PREVIEW', 'UPDATE AI IMPROVE DESCRIPTION ONLY')]
    [string]$Mode = 'PREVIEW'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'MetadataOnly.psm1') -Force

$originalHash = 'E5D53622EE0841720F72C254C5FA8C2990685A7EFDE129212B796F46CD640485'
$proposedHash = '537A05C2C4672BA4C82371047B58924C3BD2A7D38F468A4F50452BC762390629'
$expectedRoot = [IO.Path]::GetFullPath('D:\Code\Game Code\Cities_Skyline_1_Modpack').TrimEnd('\')
if ([string]::IsNullOrWhiteSpace($env:MODPACK_ROOT) -or
    [IO.Path]::GetFullPath($env:MODPACK_ROOT).TrimEnd('\') -ine $expectedRoot) {
    throw 'MODPACK_ROOT does not identify the reviewed project directory.'
}
$stage = Join-Path $expectedRoot 'Resource\Workshop_Metadata_Staging_2026-11-05'
$originalPath = Join-Path $stage 'AI_Improve_original.txt'
$proposedPath = Join-Path $stage 'AI_Improve_proposed.txt'
$encoding = [Text.UTF8Encoding]::new($false, $true)
$original = [IO.File]::ReadAllText($originalPath, $encoding)
$proposed = [IO.File]::ReadAllText($proposedPath, $encoding)

function Assert-OwnerIssue {
    if ([string]::IsNullOrWhiteSpace($env:GH_TOKEN) -or $env:METADATA_ISSUE_NUMBER -notmatch '^\d{1,10}$') {
        throw 'A repository token and numeric owner issue number are required.'
    }
    $headers = @{ Authorization = 'Bearer ' + $env:GH_TOKEN; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
    $url = 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/' + $env:METADATA_ISSUE_NUMBER
    $issue = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -TimeoutSec 20
    Test-MetadataIssueApproved $issue | Out-Null
    $events = @(Invoke-RestMethod -Method Get -Uri ($url + '/events?per_page=100') -Headers $headers -TimeoutSec 20)
    Test-MetadataApprovalEvents $events | Out-Null
}

function Get-CurrentWorkshopItem {
    $body = @{ itemcount = '1'; 'publishedfileids[0]' = '3782858610' }
    $response = Invoke-RestMethod -Method Post -Uri 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/' -Body $body -TimeoutSec 20
    $items = @($response.response.publishedfiledetails)
    if ($items.Count -ne 1 -or $items[0].result -ne 1 -or
        [string]$items[0].publishedfileid -cne '3782858610' -or
        [string]$items[0].title -cne 'AI Improve: Release Version') {
        throw 'Steam did not return the expected AI Improve release item.'
    }
    return $items[0]
}

if ($Mode -ceq 'UPDATE AI IMPROVE DESCRIPTION ONLY') { Assert-OwnerIssue }
$item = Get-CurrentWorkshopItem
$vdf = New-MetadataOnlyVdf -Original $original -Observed ([string]$item.description) `
    -Proposed $proposed -PublishedFileId ([string]$item.publishedfileid) `
    -CreatorAppId ([string]$item.creator_app_id) -ConsumerAppId ([string]$item.consumer_app_id) `
    -ExpectedOriginalHash $originalHash -ExpectedProposedHash $proposedHash

if ($Mode -ceq 'PREVIEW') {
    Write-Output 'Preview passed: the current description matches the reviewed snapshot. No Steam update was submitted.'
    return
}

foreach ($name in @('STEAMCMD_EXE', 'STEAMCMD_USERNAME', 'RUNNER_TEMP')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        throw "Required runner configuration is missing: $name"
    }
}
if ($env:STEAMCMD_USERNAME -match '\s') { throw 'The configured SteamCMD username is invalid.' }
$steamCmd = [IO.Path]::GetFullPath($env:STEAMCMD_EXE)
if (-not (Test-Path -LiteralPath $steamCmd -PathType Leaf)) { throw 'The configured SteamCMD executable was not found.' }
$steamCmdRoot = Split-Path -Parent $steamCmd
if (-not (Test-Path -LiteralPath (Join-Path $steamCmdRoot 'config\config.vdf') -PathType Leaf)) {
    throw 'SteamCMD has no saved login configuration.'
}
$runnerTemp = [IO.Path]::GetFullPath($env:RUNNER_TEMP)
if (-not (Test-Path -LiteralPath $runnerTemp -PathType Container)) { throw 'RUNNER_TEMP is not an existing directory.' }

# An exclusive local claim survives an uncertain submission and prevents automatic repeats.
$claimPath = Join-Path $stage 'AI_Improve_description_update.once'
$claim = [IO.File]::Open($claimPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $claimText = 'AI Improve metadata update attempted. Review Steam before any retry. Signature: OpenAI-Codex.'
    $claimBytes = $encoding.GetBytes($claimText)
    $claim.Write($claimBytes, 0, $claimBytes.Length)
} finally { $claim.Dispose() }

$vdfPath = Join-Path $runnerTemp ('ai-improve-metadata-' + [Guid]::NewGuid().ToString('N') + '.vdf')
$vdfStream = [IO.File]::Open($vdfPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try { $bytes = $encoding.GetBytes($vdf); $vdfStream.Write($bytes, 0, $bytes.Length) } finally { $vdfStream.Dispose() }

Assert-OwnerIssue
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = $steamCmd
$start.WorkingDirectory = $steamCmdRoot
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$childEnvironment = Get-SteamCmdEnvironment ([Environment]::GetEnvironmentVariables())
$start.Environment.Clear()
foreach ($name in $childEnvironment.Keys) { $start.Environment[$name] = [string]$childEnvironment[$name] }
$start.ArgumentList.Add('+login')
$start.ArgumentList.Add($env:STEAMCMD_USERNAME)
$start.ArgumentList.Add('+workshop_build_item')
$start.ArgumentList.Add($vdfPath)
$start.ArgumentList.Add('+quit')
$latest = Get-CurrentWorkshopItem
if ([string]$latest.description -cne $original) {
    throw 'The Steam description changed before submission. No automatic retry is allowed.'
}
if ([IO.File]::ReadAllText($vdfPath, $encoding) -cne $vdf) {
    throw 'The generated VDF changed before submission. No automatic retry is allowed.'
}
$process = [Diagnostics.Process]::Start($start)
$stdout = $process.StandardOutput.ReadToEndAsync()
$stderr = $process.StandardError.ReadToEndAsync()
if (-not $process.WaitForExit(180000)) {
    try { $process.Kill() } catch { }
    throw 'SteamCMD timed out. The result is unknown; inspect the item before any retry.'
}
$stdout.GetAwaiter().GetResult() | Out-Null
$stderr.GetAwaiter().GetResult() | Out-Null
if ($process.ExitCode -ne 0) { throw 'SteamCMD failed. The result is unknown; inspect the item before any retry.' }

$after = Get-CurrentWorkshopItem
if ([string]$after.description -cne $proposed) {
    throw 'Steam did not immediately confirm the proposed description. The result is unknown; inspect before any retry.'
}
Write-Output 'Steam returned the expected AI Improve description. The update contained no content folder, image, title, tags or visibility key.'
