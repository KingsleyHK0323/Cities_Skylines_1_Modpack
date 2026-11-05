# Signature: OpenAI-Codex.
[CmdletBinding()]
param(
    [ValidateSet('PREVIEW', 'APPLY AI IMPROVE DESCRIPTION CORRECTION')]
    [string]$Mode = 'PREVIEW'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'AIImproveDescriptionCorrection.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'MetadataOnly.psm1') -Force

$originalHash = '537A05C2C4672BA4C82371047B58924C3BD2A7D38F468A4F50452BC762390629'
$correctedHash = 'CF59F60DDF8765DFAE722AA08775D385C8054CBF80578EBB08EE695CB2B36342'
$expectedRoot = [IO.Path]::GetFullPath('D:\Code\Game Code\Cities_Skyline_1_Modpack').TrimEnd('\')
if ([string]::IsNullOrWhiteSpace($env:MODPACK_ROOT) -or
    [IO.Path]::GetFullPath($env:MODPACK_ROOT).TrimEnd('\') -ine $expectedRoot) {
    throw 'MODPACK_ROOT does not identify the reviewed project directory.'
}
$stage = Join-Path $expectedRoot 'Resource\Workshop_Metadata_Staging_2026-11-05'
$encoding = [Text.UTF8Encoding]::new($false, $true)
$original = [IO.File]::ReadAllText((Join-Path $stage 'AI_Improve_proposed.txt'), $encoding)

function Assert-CorrectionOwnerIssue {
    if ([string]::IsNullOrWhiteSpace($env:GH_TOKEN)) { throw 'A repository token is required.' }
    $headers = @{ Authorization = 'Bearer ' + $env:GH_TOKEN; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
    $url = 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/2'
    $issue = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -TimeoutSec 20
    $events = @(Invoke-RestMethod -Method Get -Uri ($url + '/events?per_page=100') -Headers $headers -TimeoutSec 20)
    Test-CorrectionIssueApproved -Issue $issue -Events $events | Out-Null
}

function Get-CorrectionWorkshopItem {
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

if ($Mode -ceq 'APPLY AI IMPROVE DESCRIPTION CORRECTION') { Assert-CorrectionOwnerIssue }
$item = Get-CorrectionWorkshopItem
$correction = New-AIImproveDescriptionCorrection -Original $original -Observed ([string]$item.description) `
    -PublishedFileId ([string]$item.publishedfileid) -CreatorAppId ([string]$item.creator_app_id) `
    -ConsumerAppId ([string]$item.consumer_app_id) -ExpectedOriginalHash $originalHash -ExpectedCorrectedHash $correctedHash
if ($Mode -ceq 'PREVIEW') {
    Write-Output 'Preview passed: only the two reviewed description replacements were prepared. No Steam update was submitted.'
    return
}

foreach ($name in @('STEAMCMD_EXE', 'STEAMCMD_USERNAME', 'RUNNER_TEMP')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) { throw "Required runner configuration is missing: $name" }
}
if ($env:STEAMCMD_USERNAME -match '\s') { throw 'The configured SteamCMD username is invalid.' }
$steamCmd = [IO.Path]::GetFullPath($env:STEAMCMD_EXE)
if (-not (Test-Path -LiteralPath $steamCmd -PathType Leaf)) { throw 'The configured SteamCMD executable was not found.' }
$steamCmdRoot = Split-Path -Parent $steamCmd
if (-not (Test-Path -LiteralPath (Join-Path $steamCmdRoot 'config\config.vdf') -PathType Leaf)) { throw 'SteamCMD has no saved login configuration.' }
$runnerTemp = [IO.Path]::GetFullPath($env:RUNNER_TEMP)
if (-not (Test-Path -LiteralPath $runnerTemp -PathType Container)) { throw 'RUNNER_TEMP is not an existing directory.' }
$claimPath = Join-Path $stage 'AI_Improve_description_correction.once'
if (Test-Path -LiteralPath $claimPath) { throw 'The correction was already attempted; inspect Steam before any retry.' }

$vdfPath = Join-Path $runnerTemp ('ai-improve-correction-' + [Guid]::NewGuid().ToString('N') + '.vdf')
$vdfStream = [IO.File]::Open($vdfPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try { $bytes = $encoding.GetBytes($correction.Vdf); $vdfStream.Write($bytes, 0, $bytes.Length) } finally { $vdfStream.Dispose() }

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

Assert-CorrectionOwnerIssue
$latest = Get-CorrectionWorkshopItem
if ([string]$latest.description -cne $original) { throw 'The Steam description changed before submission.' }
if ([IO.File]::ReadAllText($vdfPath, $encoding) -cne $correction.Vdf) { throw 'The generated VDF changed before submission.' }

# An exclusive local claim survives an uncertain submission and prevents automatic repeats.
$claim = [IO.File]::Open($claimPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $claimBytes = $encoding.GetBytes('AI Improve description correction attempted. Review Steam before any retry. Signature: OpenAI-Codex.')
    $claim.Write($claimBytes, 0, $claimBytes.Length)
} finally { $claim.Dispose() }

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

$after = Get-CorrectionWorkshopItem
if ([string]$after.description -cne $correction.Description) {
    throw 'Steam did not immediately confirm the corrected description. Inspect before any retry.'
}
Write-Output 'Steam returned the expected AI Improve description correction. The update contained no content folder, image, title, tags or visibility key.'
