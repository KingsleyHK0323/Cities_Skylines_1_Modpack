# Signature: OpenAI-Codex.
[CmdletBinding()]
param(
    [ValidateSet('PREVIEW', 'APPLY APTP DESCRIPTION UPDATE')]
    [string]$Mode = 'PREVIEW'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'APTPDescriptionUpdate.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'MetadataOnly.psm1') -Force

$originalHash = '8719E1A372DD50B137121C632CC75CFFF5890496F8C59E934B6EC0B99BCE370E'
$candidateHash = '75489BBBA894C8CD8FC2337BBF4E6615C2D41003D8524EA95C7D1CF6762943EA'
$observedHtmlHash = '7982E44A961BFE4A6C34CBC52F81E04D53342E0D887FEDCDD2F1E6EB2E850E2E'
$expectedRoot = [IO.Path]::GetFullPath('D:\Code\Game Code\Cities_Skyline_1_Modpack').TrimEnd('\')
if ([string]::IsNullOrWhiteSpace($env:MODPACK_ROOT) -or
    [IO.Path]::GetFullPath($env:MODPACK_ROOT).TrimEnd('\') -ine $expectedRoot) {
    throw 'MODPACK_ROOT does not identify the reviewed project directory.'
}
$encoding = [Text.UTF8Encoding]::new($false, $true)
$notePath = Join-Path $expectedRoot 'Resource\Cities_Skyline_1_Modpack_Document\APTP_Steam_Workshop_說明修訂候選_2026-11-05.md'
$note = [IO.File]::ReadAllText($notePath, $encoding)
$candidate = Get-APTPCandidateFromNote -Note $note -ExpectedHash $candidateHash
$plan = New-APTPDescriptionPlan -Candidate $candidate -ExpectedOriginalHash $originalHash `
    -ExpectedCandidateHash $candidateHash -PublishedFileId '3823739383' -AppId '255710'

function Assert-APTPOwnerIssue {
    if ([string]::IsNullOrWhiteSpace($env:GH_TOKEN) -or $env:APTP_ISSUE_NUMBER -notmatch '^[1-9]\d{0,9}$') {
        throw 'A repository token and numeric owner issue number are required.'
    }
    $headers = @{ Authorization = 'Bearer ' + $env:GH_TOKEN; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
    $url = 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/' + $env:APTP_ISSUE_NUMBER
    $issue = Invoke-RestMethod -Method Get -Uri $url -Headers $headers -TimeoutSec 20
    $events = @(Invoke-RestMethod -Method Get -Uri ($url + '/events?per_page=100') -Headers $headers -TimeoutSec 20)
    Test-APTPOwnerIssue -Issue $issue -Events $events -RequestedIssueNumber $env:APTP_ISSUE_NUMBER | Out-Null
}

function Get-APTPWorkshopInnerHtml {
    $response = Invoke-WebRequest -Uri 'https://steamcommunity.com/sharedfiles/filedetails/?id=3823739383' -MaximumRedirection 0 -TimeoutSec 20
    if ([int]$response.StatusCode -ne 200) { throw 'Steam did not return an accessible APTP page.' }
    Test-APTPFinalUri -Url ([string]$response.BaseResponse.RequestMessage.RequestUri.AbsoluteUri) | Out-Null
    return Get-APTPDescriptionInnerHtml -Page ([string]$response.Content)
}

function Assert-APTPBeforeSubmission {
    $before = Get-APTPWorkshopInnerHtml
    if ((Get-APTPTextHash $before) -cne $observedHtmlHash) { throw 'The Steam description changed after the reviewed snapshot.' }
    Test-APTPRenderedDescription -InnerHtml $before -Candidate $plan.Original | Out-Null
}

if ($Mode -ceq 'PREVIEW') {
    Assert-APTPBeforeSubmission
    Write-Output 'Preview passed: the visible Steam description matches the reviewed historical source. No update was submitted.'
    return
}
Assert-APTPOwnerIssue
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
$stage = Join-Path $expectedRoot 'Resource\Workshop_Metadata_Staging_2026-11-05'
$claimPath = Join-Path $stage 'APTP_description_update.once'
if (Test-Path -LiteralPath $claimPath) { throw 'The APTP update was already attempted; inspect Steam before any retry.' }

$vdfPath = Join-Path $runnerTemp ('aptp-description-' + [Guid]::NewGuid().ToString('N') + '.vdf')
$vdfStream = [IO.File]::Open($vdfPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try { $bytes = $encoding.GetBytes($plan.Vdf); $vdfStream.Write($bytes, 0, $bytes.Length) } finally { $vdfStream.Dispose() }

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

Assert-APTPOwnerIssue
Assert-APTPBeforeSubmission
if ([IO.File]::ReadAllText($vdfPath, $encoding) -cne $plan.Vdf) { throw 'The generated VDF changed before submission.' }

# An exclusive local claim survives an uncertain submission and prevents automatic repeats.
$claim = [IO.File]::Open($claimPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $claimBytes = $encoding.GetBytes('APTP description update attempted. Review Steam before any retry. Signature: OpenAI-Codex.')
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

$after = Get-APTPWorkshopInnerHtml
Test-APTPRenderedDescription -InnerHtml $after -Candidate $plan.Candidate | Out-Null
Write-Output 'Steam rendered the reviewed APTP description and links. The update contained no content folder, image, title, tags or visibility key.'
