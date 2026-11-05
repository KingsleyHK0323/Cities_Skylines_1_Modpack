# Signature: OpenAI-Codex.
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'MetadataOnly.psm1') -Force

$count = 0
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:count++
}
function Assert-Rejected([scriptblock]$Action, [string]$Message) {
    try { & $Action | Out-Null } catch { $script:count++; return }
    throw $Message
}

$original = 'Existing public description with a website link.'
$proposed = $original + "`n`n" + '[b]Development version[/b] Use the installer.'
$expectedOriginal = Get-MetadataTextHash $original
$expectedProposed = Get-MetadataTextHash $proposed
$testCase = @{
    Original = $original; Observed = $original; Proposed = $proposed
    PublishedFileId = '3782858610'; CreatorAppId = '255710'; ConsumerAppId = '255710'
    ExpectedOriginalHash = $expectedOriginal; ExpectedProposedHash = $expectedProposed
}
$vdf = New-MetadataOnlyVdf @testCase
Assert-True ($vdf -match '(?m)^\s*"appid" "255710"\r?$') 'The app ID is missing.'
Assert-True ($vdf -match '(?m)^\s*"publishedfileid" "3782858610"\r?$') 'The item ID is missing.'
Assert-True ($vdf.Contains($proposed)) 'The proposed text is missing.'
Assert-True (-not ($vdf -match '(?m)^\s*"(contentfolder|previewfile|visibility|title|tags|changenote)"')) 'An unrelated update key was emitted.'
$changed = $testCase.Clone(); $changed.Observed = 'Changed remotely.'
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A changed current description was accepted.'
$changed = $testCase.Clone(); $changed.PublishedFileId = '3832298237'
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A different item was accepted.'
$changed = $testCase.Clone(); $changed.ConsumerAppId = '999'
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A different game was accepted.'
$changed = $testCase.Clone(); $changed.Proposed = 'Replacement only.'; $changed.ExpectedProposedHash = Get-MetadataTextHash $changed.Proposed
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A replacement without the original text was accepted.'
$changed = $testCase.Clone(); $changed.ExpectedOriginalHash = ('0' * 64)
Assert-Rejected { New-MetadataOnlyVdf @changed } 'An incorrect original hash was accepted.'
$changed = $testCase.Clone(); $changed.ExpectedProposedHash = ('0' * 64)
Assert-Rejected { New-MetadataOnlyVdf @changed } 'An incorrect proposed hash was accepted.'
$changed = $testCase.Clone(); $changed.Proposed = $proposed + '"'; $changed.ExpectedProposedHash = Get-MetadataTextHash $changed.Proposed
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A quoted VDF value was accepted.'
$changed = $testCase.Clone(); $changed.Proposed = $proposed + '\'; $changed.ExpectedProposedHash = Get-MetadataTextHash $changed.Proposed
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A backslash VDF value was accepted.'
$changed = $testCase.Clone(); $changed.Proposed = $proposed + [char]0; $changed.ExpectedProposedHash = Get-MetadataTextHash $changed.Proposed
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A NUL in the VDF value was accepted.'
$changed = $testCase.Clone(); $changed.Proposed = $original; $changed.ExpectedProposedHash = Get-MetadataTextHash $changed.Proposed
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A proposal without an addition was accepted.'
$changed = $testCase.Clone(); $changed.Proposed = $original + ('x' * 8001); $changed.ExpectedProposedHash = Get-MetadataTextHash $changed.Proposed
Assert-Rejected { New-MetadataOnlyVdf @changed } 'An oversized description was accepted.'
$changed = $testCase.Clone(); $changed.CreatorAppId = '999'
Assert-Rejected { New-MetadataOnlyVdf @changed } 'A different creator app was accepted.'
$body = @(
    'Update the AI Improve release description only.'
    'Original SHA-256: E5D53622EE0841720F72C254C5FA8C2990685A7EFDE129212B796F46CD640485'
    'Proposed SHA-256: 537A05C2C4672BA4C82371047B58924C3BD2A7D38F468A4F50452BC762390629'
    'No content, title, preview, tags or visibility changes.'
) -join "`n"
function New-TestIssue {
    return [pscustomobject]@{ title = '[Workshop metadata] AI Improve 3782858610'; body = $body; state = 'open';
        created_at = '2026-11-04T23:12:17Z'; updated_at = '2026-11-04T23:12:32Z';
        user = [pscustomobject]@{ login = 'KingsleyHK0323' }; labels = @([pscustomobject]@{ name = 'steam-workshop-metadata-approved' }) }
}
$issue = New-TestIssue
Assert-True (Test-MetadataIssueApproved $issue) 'The reviewed owner issue was rejected.'
$issue = New-TestIssue; $issue.body += ' changed'
Assert-Rejected { Test-MetadataIssueApproved $issue } 'An edited issue was accepted.'
$issue = New-TestIssue; $issue.user.login = 'someone-else'
Assert-Rejected { Test-MetadataIssueApproved $issue } 'A non-owner issue was accepted.'
$issue = New-TestIssue; $issue.labels = @()
Assert-Rejected { Test-MetadataIssueApproved $issue } 'An unlabelled issue was accepted.'
$issue = New-TestIssue; $issue.state = 'closed'
Assert-Rejected { Test-MetadataIssueApproved $issue } 'A closed issue was accepted.'
$issue = New-TestIssue; $issue.title = 'Another target'
Assert-Rejected { Test-MetadataIssueApproved $issue } 'An issue with the wrong title was accepted.'
$issue = New-TestIssue; $issue | Add-Member -NotePropertyName pull_request -NotePropertyValue @{}
Assert-Rejected { Test-MetadataIssueApproved $issue } 'A pull request was accepted as an issue.'
$issue = New-TestIssue; $issue.updated_at = '2026-11-04T23:12:33Z'
Assert-Rejected { Test-MetadataIssueApproved $issue } 'An issue changed after owner approval was accepted.'
$issue = New-TestIssue; $issue.created_at = '2026-11-04T23:12:16Z'
Assert-Rejected { Test-MetadataIssueApproved $issue } 'An unexpected issue creation time was accepted.'
$ownerEvent = [pscustomobject]@{ id = 35184966638; created_at = '2026-11-04T23:12:32Z'; event = 'labeled'; actor = [pscustomobject]@{ login = 'KingsleyHK0323' }; label = [pscustomobject]@{ name = 'steam-workshop-metadata-approved' } }
$removedEvent = [pscustomobject]@{ event = 'unlabeled'; actor = [pscustomobject]@{ login = 'someone-else' }; label = [pscustomobject]@{ name = 'steam-workshop-metadata-approved' } }
$otherEvent = [pscustomobject]@{ event = 'labeled'; actor = [pscustomobject]@{ login = 'someone-else' }; label = [pscustomobject]@{ name = 'steam-workshop-metadata-approved' } }
Assert-True (Test-MetadataApprovalEvents @($ownerEvent)) 'The owner approval event was rejected.'
$wrongEvent = [pscustomobject]@{ id = 35184966639; created_at = '2026-11-04T23:12:32Z'; event = 'labeled'; actor = [pscustomobject]@{ login = 'KingsleyHK0323' }; label = [pscustomobject]@{ name = 'steam-workshop-metadata-approved' } }
Assert-Rejected { Test-MetadataApprovalEvents @($wrongEvent) } 'A different owner approval event was accepted.'
Assert-Rejected { Test-MetadataApprovalEvents @($ownerEvent, $removedEvent) } 'A removed approval was accepted.'
Assert-Rejected { Test-MetadataApprovalEvents @($ownerEvent, $otherEvent) } 'An approval added by someone else was accepted.'
Assert-Rejected { Test-MetadataApprovalEvents @() } 'Missing approval history was accepted.'
$manyEvents = @($ownerEvent) * 100
Assert-Rejected { Test-MetadataApprovalEvents $manyEvents } 'Incomplete approval history was accepted.'
$wrappedOwner = ,@($ownerEvent)
Assert-True (Test-MetadataApprovalEvents $wrappedOwner) 'A REST response containing a nested owner-event array was rejected.'
$wrappedRemoved = ,@($ownerEvent, $removedEvent)
Assert-Rejected { Test-MetadataApprovalEvents $wrappedRemoved } 'A nested response with removed approval was accepted.'
$wrappedMany = ,$manyEvents
Assert-Rejected { Test-MetadataApprovalEvents $wrappedMany } 'A nested response with incomplete event history was accepted.'
$environment = @{ SystemRoot = 'C:\Windows'; PATH = 'C:\Windows'; USERPROFILE = 'C:\User'; GH_TOKEN = 'secret';
    GITHUB_TOKEN = 'secret'; ACTIONS_RUNTIME_TOKEN = 'secret'; STEAM_PASSWORD = 'secret'; STEAMCMD_USERNAME = 'secret' }
$childEnvironment = Get-SteamCmdEnvironment $environment
Assert-True ($childEnvironment.ContainsKey('SystemRoot') -and $childEnvironment.ContainsKey('PATH') -and $childEnvironment.ContainsKey('USERPROFILE')) 'Required Windows environment entries were not retained.'
Assert-True (-not $childEnvironment.ContainsKey('GH_TOKEN') -and -not $childEnvironment.ContainsKey('GITHUB_TOKEN') -and
    -not $childEnvironment.ContainsKey('ACTIONS_RUNTIME_TOKEN') -and -not $childEnvironment.ContainsKey('STEAM_PASSWORD') -and
    -not $childEnvironment.ContainsKey('STEAMCMD_USERNAME')) 'A credential-related variable was inherited by SteamCMD.'
Write-Output "Passed $count metadata-only checks."
