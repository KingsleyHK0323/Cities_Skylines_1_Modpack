# Signature: OpenAI-Codex.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot 'DescriptionRewritePlan.psm1'
if (-not (Test-Path -LiteralPath $modulePath -PathType Leaf)) {
    throw 'RED: description rewrite validation is not implemented.'
}
Import-Module $modulePath -Force

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Assert-Rejected([scriptblock]$Action, [string]$Message) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw $Message }
}

$draft = @'
# Review

## 1. Sample Module - Workshop ID 1000000001

~~~text
[h1]Sample Module[/h1]
Test description.
~~~
'@
$draft = $draft.Replace("`r`n", "`n")
$body = "[h1]Sample Module[/h1]`nTest description.`n"
$draftHash = Get-RewriteTextHash $draft
$bodyHash = Get-RewriteTextHash $body
$manifestObject = [ordered]@{
    schemaVersion = 1
    status = 'draft-not-approved-or-published'
    preparedBy = 'OpenAI-Codex'
    sourceFile = 'Steam_Workshop_Descriptions_Role_Rewrite_2026-11-05_Review.md'
    sourceFileSha256 = $draftHash
    descriptionHashConvention = 'UTF-8, LF line endings, exactly one trailing LF'
    mappingEvidence = 'Owner screenshots.'
    items = @([ordered]@{ module = 'Sample Module'; workshopItemId = '1000000001'; descriptionSha256 = $bodyHash; descriptionUtf8Bytes = [Text.Encoding]::UTF8.GetByteCount($body) })
}
$manifest = $manifestObject | ConvertTo-Json -Depth 8
$manifestHash = Get-RewriteTextHash $manifest
$plan = New-RewriteDescriptionPlan -ManifestJson $manifest -ExpectedManifestHash $manifestHash -Draft $draft -ExpectedDraftHash $draftHash -Module 'Sample Module' -ExpectedItemCount 1
Assert-True ($plan.WorkshopItemId -ceq '1000000001') 'The selected item does not match.'
Assert-True ($plan.Description -ceq $body) 'The selected description was changed.'
Assert-True ($plan.Vdf -match '"appid"\s+"255710"') 'The game ID is missing.'
Assert-True ($plan.Vdf -match '"publishedfileid"\s+"1000000001"') 'The target ID is missing.'
Assert-True ($plan.Vdf -notmatch '"(?:contentfolder|previewfile|title|visibility|tags)"') 'An unrelated metadata key was included.'

Assert-Rejected { New-RewriteDescriptionPlan -ManifestJson $manifest -ExpectedManifestHash ('0' * 64) -Draft $draft -ExpectedDraftHash $draftHash -Module 'Sample Module' -ExpectedItemCount 1 } 'Changed manifest accepted.'
Assert-Rejected { New-RewriteDescriptionPlan -ManifestJson $manifest -ExpectedManifestHash $manifestHash -Draft ($draft + 'x') -ExpectedDraftHash $draftHash -Module 'Sample Module' -ExpectedItemCount 1 } 'Changed draft accepted.'
Assert-Rejected { New-RewriteDescriptionPlan -ManifestJson $manifest -ExpectedManifestHash $manifestHash -Draft $draft -ExpectedDraftHash $draftHash -Module 'Other Module' -ExpectedItemCount 1 } 'Unknown module accepted.'
Assert-Rejected { New-RewriteVdf -WorkshopItemId '1000000001' -Description 'bad " quote' } 'Unsafe VDF quote accepted.'
Assert-Rejected { New-RewriteVdf -WorkshopItemId '1000000001' -Description ('x' * 8001) } 'Oversized description accepted.'
Assert-Rejected { New-RewriteVdf -WorkshopItemId '1' -Description 'text' } 'Invalid ID accepted.'

$issueBody = Get-RewriteIssueBody -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash
$issue = [pscustomobject]@{
    number = 7; title = '[Workshop description rewrite] Sample Module'; state = 'open'
    user = [pscustomobject]@{ login = 'KingsleyHK0323' }
    body = $issueBody; created_at = '2026-11-05T00:00:00Z'; updated_at = '2026-11-05T00:01:00Z'
    labels = @([pscustomobject]@{ name = 'steam-workshop-description-rewrite-approved' })
}
$event = [pscustomobject]@{ event = 'labeled'; actor = [pscustomobject]@{ login = 'KingsleyHK0323' }; label = [pscustomobject]@{ name = 'steam-workshop-description-rewrite-approved' }; created_at = '2026-11-05T00:01:00Z' }
$issue | Add-Member -NotePropertyName repository_url -NotePropertyValue 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack'
$issue.updated_at = '2026-11-05T00:00:30Z'
$event | Add-Member -NotePropertyName id -NotePropertyValue 123
$event | Add-Member -NotePropertyName url -NotePropertyValue 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/events/123'
Assert-True (Test-RewriteOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash) 'Owner issue was rejected.'
$issue.body += ' altered'
Assert-Rejected { Test-RewriteOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash } 'Changed issue accepted.'
$issue.body = $issueBody
$event.actor.login = 'other'
Assert-Rejected { Test-RewriteOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash } 'Non-owner label accepted.'
Write-Output 'PASS: offline target, hash, VDF and owner-issue checks.'

Assert-True ([bool](Get-Command Test-RewriteRunContext -ErrorAction SilentlyContinue)) 'RED: the workflow context guard is not implemented.'
Assert-True ([bool](Get-Command Get-RewriteClaimName -ErrorAction SilentlyContinue)) 'RED: the one-shot claim name is not implemented.'
$context = @{
    GITHUB_ACTIONS = 'true'; GITHUB_REPOSITORY = 'KingsleyHK0323/Cities_Skylines_1_Modpack'
    GITHUB_ACTOR = 'KingsleyHK0323'; GITHUB_TRIGGERING_ACTOR = 'KingsleyHK0323'
    GITHUB_REF = 'refs/heads/Modpack_Website'; GITHUB_EVENT_NAME = 'workflow_dispatch'
    GITHUB_RUN_ATTEMPT = '1'; WORKSHOP_RELEASE_APPROVAL_READY = 'true'
}
Assert-True (Test-RewriteRunContext -Context $context -Mode 'APPLY REVIEWED DESCRIPTION') 'An authorized context was rejected.'
$context.GITHUB_ACTOR = 'other'
Assert-Rejected { Test-RewriteRunContext -Context $context -Mode 'APPLY REVIEWED DESCRIPTION' } 'A non-owner context was accepted.'
$context.GITHUB_ACTOR = 'KingsleyHK0323'
Assert-Rejected { Test-RewriteRunContext -Context $context -Mode 'PREVIEW' } 'The default mode allowed an update.'
$context.GITHUB_RUN_ATTEMPT = '2'
Assert-Rejected { Test-RewriteRunContext -Context $context -Mode 'APPLY REVIEWED DESCRIPTION' } 'A rerun was accepted.'
$claimName = Get-RewriteClaimName -Module 'Sample Module' -DescriptionHash $bodyHash
Assert-True ($claimName -match '^[a-f0-9]{16}-[a-f0-9]{64}\.once$') 'The claim filename is invalid.'
Assert-True ($claimName -cne (Get-RewriteClaimName -Module 'Another Module' -DescriptionHash $bodyHash)) 'Different modules share a claim filename.'
Write-Output 'PASS: offline context and one-shot claim identity checks.'

$event.actor.login = 'KingsleyHK0323'
$issue.updated_at = '2026-11-05T00:01:00Z'
Assert-Rejected { Test-RewriteOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash } 'Same-second issue update accepted.'
$issue.updated_at = '2026-11-05T00:00:30Z'
$issue.repository_url = 'https://api.github.com/repos/KingsleyHK0323/other'
Assert-Rejected { Test-RewriteOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash } 'Another repository was accepted.'
$issue.repository_url = 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack'
$event.url = 'https://api.github.com/repos/KingsleyHK0323/other/issues/events/123'
Assert-Rejected { Test-RewriteOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash } 'Another repository event was accepted.'
$event.url = 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/events/123'
$laterRemoval = [pscustomobject]@{ id = 124; url = 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/events/124'; event = 'unlabeled'; actor = [pscustomobject]@{ login = 'other' }; label = [pscustomobject]@{ name = 'steam-workshop-description-rewrite-approved' }; created_at = '2026-11-05T00:02:00Z' }
Assert-Rejected { Test-RewriteOwnerIssue -Issue $issue -Events @($laterRemoval, $event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash } 'Out-of-order label history was accepted.'
Assert-Rejected { New-RewriteVdf -WorkshopItemId '1000000001' -Description ('text' + [char]1) } 'A control character was accepted.'
Assert-True ([bool](Get-Command Test-RewritePinnedOwnerIssue -ErrorAction SilentlyContinue)) 'RED: the controller commit guard is not implemented.'
$commit = 'a' * 40
$issue.body = $issueBody + "`nController commit: " + $commit
Assert-True (Test-RewritePinnedOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash -ControllerCommit $commit) 'A pinned owner issue was rejected.'
Assert-Rejected { Test-RewritePinnedOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -Module 'Sample Module' -ManifestHash $manifestHash -DraftHash $draftHash -DescriptionHash $bodyHash -ControllerCommit ('b' * 40) } 'Another controller commit was accepted.'
Write-Output 'PASS: offline repository, ordering, VDF controls and pinned-controller checks.'

Assert-True ([bool](Get-Command Get-RewriteItemClaimName -ErrorAction SilentlyContinue)) 'RED: item-based one-shot identity is not implemented.'
$itemClaim = Get-RewriteItemClaimName -WorkshopItemId '1000000001' -DescriptionHash $bodyHash
Assert-True ($itemClaim -ceq (Get-RewriteItemClaimName -WorkshopItemId '1000000001' -DescriptionHash $bodyHash)) 'The same item and description changed its claim identity.'
Assert-True ($itemClaim -cne (Get-RewriteItemClaimName -WorkshopItemId '1000000002' -DescriptionHash $bodyHash)) 'Different items share a claim identity.'
Assert-True (-not $itemClaim.Contains('1000000001')) 'The private item ID appears in the claim name.'
Write-Output 'PASS: offline item-based one-shot identity checks.'

Assert-True ([bool](Get-Command Get-RewriteBatchIssueBody -ErrorAction SilentlyContinue)) 'RED: the batch owner issue body is not implemented.'
Assert-True ([bool](Get-Command Test-RewriteBatchOwnerIssue -ErrorAction SilentlyContinue)) 'RED: the batch owner issue guard is not implemented.'
$issue.title = '[Workshop description rewrite] All 15 modules'
$issue.body = Get-RewriteBatchIssueBody -ManifestHash $manifestHash -DraftHash $draftHash -ControllerCommit $commit
Assert-True ($issue.body.Contains('12 items were hidden in the owner screenshots')) 'The unavailable private descriptions were not disclosed.'
Assert-True (Test-RewriteBatchOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -ManifestHash $manifestHash -DraftHash $draftHash -ControllerCommit $commit) 'A pinned batch owner issue was rejected.'
Assert-Rejected { Test-RewriteBatchOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -ManifestHash $manifestHash -DraftHash $draftHash -ControllerCommit ('b' * 40) } 'Another controller commit was accepted for the batch.'
$issue.body += ' altered'
Assert-Rejected { Test-RewriteBatchOwnerIssue -Issue $issue -Events @($event) -IssueNumber '7' -ManifestHash $manifestHash -DraftHash $draftHash -ControllerCommit $commit } 'An altered batch issue was accepted.'
Write-Output 'PASS: offline pinned batch issue checks.'

# Verify Windows sharing behavior using this existing test file; no bytes are written.
$lockFixture = $PSCommandPath
$beforeLockHash = (Get-FileHash -LiteralPath $lockFixture -Algorithm SHA256).Hash
$readLock = [IO.File]::Open($lockFixture, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    $fixtureText = [IO.File]::ReadAllText($lockFixture)
    Assert-True ($fixtureText.Length -gt 0) 'A concurrent read was blocked.'
    $writeRejected = $false
    try {
        $unexpectedWriter = [IO.File]::Open($lockFixture, [IO.FileMode]::Open, [IO.FileAccess]::Write, [IO.FileShare]::ReadWrite)
        $unexpectedWriter.Dispose()
    } catch { $writeRejected = $true }
    Assert-True $writeRejected 'The read-sharing lock allowed another write handle.'
} finally { $readLock.Dispose() }
Assert-True ($beforeLockHash -ceq (Get-FileHash -LiteralPath $lockFixture -Algorithm SHA256).Hash) 'The lock fixture changed.'
Write-Output 'PASS: offline read-sharing lock check; no file data was changed.'