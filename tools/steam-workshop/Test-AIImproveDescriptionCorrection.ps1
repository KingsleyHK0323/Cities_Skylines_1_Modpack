# Signature: OpenAI-Codex.
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'AIImproveDescriptionCorrection.psm1') -Force

$count = 0
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:count++
}
function Assert-Rejected([scriptblock]$Action, [string]$Message) {
    try { & $Action | Out-Null } catch { $script:count++; return }
    throw $Message
}

$oldLink = '[*]Want to try changes before they reach this version? See the [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3823575900]Development Version[/url] ' + [char]0x2013 + ' but never subscribe to both.'
$newLink = '[*]Development builds are installed with Dev Manager; see Development version below.'
$oldLicense = 'Source code (GPL-3.0)'
$newLicense = 'Source code (PolyForm Noncommercial License 1.0.0)'
$original = "Header`n$oldLink`n$oldLicense`nFooter"
$corrected = $original.Replace($oldLink, $newLink).Replace($oldLicense, $newLicense)
$case = @{
    Original = $original; Observed = $original; PublishedFileId = '3782858610'
    CreatorAppId = '255710'; ConsumerAppId = '255710'
    ExpectedOriginalHash = Get-CorrectionTextHash $original
    ExpectedCorrectedHash = Get-CorrectionTextHash $corrected
}
$result = New-AIImproveDescriptionCorrection @case
Assert-True ($result.Description -ceq $corrected) 'The corrected description differs from the two approved replacements.'
Assert-True ($result.Vdf -match '(?m)^\s*"appid" "255710"\r?$') 'The app ID is missing.'
Assert-True ($result.Vdf -match '(?m)^\s*"publishedfileid" "3782858610"\r?$') 'The item ID is missing.'
Assert-True ($result.Vdf.Contains($corrected)) 'The corrected description is missing.'
Assert-True (-not ($result.Vdf -match '(?m)^\s*"(contentfolder|previewfile|visibility|title|tags|changenote)"')) 'An unrelated update key was emitted.'

$changed = $case.Clone(); $changed.Observed = 'Different current description.'
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'A changed remote description was accepted.'
$changed = $case.Clone(); $changed.PublishedFileId = '3832298237'
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'A different Workshop item was accepted.'
$changed = $case.Clone(); $changed.CreatorAppId = '999'
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'A different creator app was accepted.'
$changed = $case.Clone(); $changed.ConsumerAppId = '999'
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'A different consumer app was accepted.'
$changed = $case.Clone(); $changed.ExpectedOriginalHash = '0' * 64
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'An incorrect source hash was accepted.'
$changed = $case.Clone(); $changed.ExpectedCorrectedHash = '0' * 64
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'An incorrect corrected hash was accepted.'
$changed = $case.Clone(); $changed.Original = $original + "`n" + $oldLink; $changed.Observed = $changed.Original; $changed.ExpectedOriginalHash = Get-CorrectionTextHash $changed.Original
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'A duplicate old link was accepted.'
$changed = $case.Clone(); $changed.Original = $original.Replace($oldLicense, 'Unknown license'); $changed.Observed = $changed.Original; $changed.ExpectedOriginalHash = Get-CorrectionTextHash $changed.Original
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'A missing old license was accepted.'
$changed = $case.Clone(); $changed.Original = $original + "`n" + $newLink; $changed.Observed = $changed.Original; $changed.ExpectedOriginalHash = Get-CorrectionTextHash $changed.Original
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'An existing replacement was accepted.'
foreach ($unsafe in @('"', '\', [string][char]0)) {
    $changed = $case.Clone(); $changed.Original = $original + $unsafe; $changed.Observed = $changed.Original; $changed.ExpectedOriginalHash = Get-CorrectionTextHash $changed.Original
    $changed.ExpectedCorrectedHash = Get-CorrectionTextHash ($corrected + $unsafe)
    Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'A VDF-unsafe character was accepted.'
}
$changed = $case.Clone(); $changed.Original = $original + ('x' * 8001); $changed.Observed = $changed.Original; $changed.ExpectedOriginalHash = Get-CorrectionTextHash $changed.Original; $changed.ExpectedCorrectedHash = Get-CorrectionTextHash ($corrected + ('x' * 8001))
Assert-Rejected { New-AIImproveDescriptionCorrection @changed } 'An oversized description was accepted.'

$body = @(
    'Correct the AI Improve release description only.'
    'Original SHA-256: 537A05C2C4672BA4C82371047B58924C3BD2A7D38F468A4F50452BC762390629'
    'Corrected SHA-256: CF59F60DDF8765DFAE722AA08775D385C8054CBF80578EBB08EE695CB2B36342'
    'Replace the old Workshop development link with the Dev Manager instruction.'
    'Change the source code label to PolyForm Noncommercial License 1.0.0.'
    'No content, title, preview, tags or visibility changes.'
    'Signature: OpenAI-Codex.'
) -join "`n"
function New-TestIssue {
    return [pscustomobject]@{ number = 2; title = '[Workshop description correction] AI Improve 3782858610'; body = $body; state = 'open';
        created_at = '2026-11-05T01:16:59Z'; updated_at = '2026-11-05T01:16:59Z';
        user = [pscustomobject]@{ login = 'KingsleyHK0323' }; labels = @([pscustomobject]@{ name = 'steam-workshop-description-correction-approved' }) }
}
$event = [pscustomobject]@{ id = 35193015737; created_at = '2026-11-05T01:17:00Z'; event = 'labeled'; actor = [pscustomobject]@{ login = 'KingsleyHK0323' }; label = [pscustomobject]@{ name = 'steam-workshop-description-correction-approved' } }
$issue = New-TestIssue
Assert-True (Test-CorrectionIssueApproved -Issue $issue -Events @($event)) 'The approved owner issue was rejected.'
Assert-True (Test-CorrectionIssueApproved -Issue $issue -Events (,@($event))) 'A wrapped REST event array was rejected.'
$issue = New-TestIssue; $issue.number = 1
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($event) } 'A different issue was accepted.'
$issue = New-TestIssue; $issue.body += ' modified'
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($event) } 'An edited issue body was accepted.'
$issue = New-TestIssue; $issue.user.login = 'someone-else'
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($event) } 'A non-owner issue was accepted.'
$issue = New-TestIssue; $issue.labels = @()
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($event) } 'A missing label was accepted.'
$issue = New-TestIssue; $issue.state = 'closed'
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($event) } 'A closed issue was accepted.'
$issue = New-TestIssue; $issue.updated_at = '2026-11-05T01:17:01Z'
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($event) } 'A later issue change was accepted.'
$issue = New-TestIssue; $issue | Add-Member -NotePropertyName pull_request -NotePropertyValue @{}
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($event) } 'A pull request was accepted.'
$issue = New-TestIssue; $wrong = [pscustomobject]@{ id = 35193015738; created_at = '2026-11-05T01:17:00Z'; event = 'labeled'; actor = [pscustomobject]@{ login = 'KingsleyHK0323' }; label = [pscustomobject]@{ name = 'steam-workshop-description-correction-approved' } }
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($wrong) } 'A different event was accepted.'
$removed = [pscustomobject]@{ event = 'unlabeled'; actor = [pscustomobject]@{ login = 'someone-else' }; label = [pscustomobject]@{ name = 'steam-workshop-description-correction-approved' } }
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @($event, $removed) } 'A removed label was accepted.'
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events @() } 'Missing approval history was accepted.'
Assert-Rejected { Test-CorrectionIssueApproved -Issue $issue -Events (@($event) * 100) } 'Potentially incomplete approval history was accepted.'
Write-Output "Passed $count description-correction checks."
