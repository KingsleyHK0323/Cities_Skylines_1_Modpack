# Signature: OpenAI-Codex.
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'APTPDescriptionUpdate.psm1') -Force

$count = 0
function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:count++
}
function Assert-Rejected([scriptblock]$Action, [string]$Message) {
    try { & $Action | Out-Null } catch { $script:count++; return }
    throw $Message
}

$notePath = 'D:\Code\Game Code\Cities_Skyline_1_Modpack\Resource\Cities_Skyline_1_Modpack_Document\APTP_Steam_Workshop_說明修訂候選_2026-11-05.md'
$note = [IO.File]::ReadAllText($notePath, [Text.UTF8Encoding]::new($false, $true))
$candidateHash = '75489BBBA894C8CD8FC2337BBF4E6615C2D41003D8524EA95C7D1CF6762943EA'
$originalHash = '8719E1A372DD50B137121C632CC75CFFF5890496F8C59E934B6EC0B99BCE370E'
$candidate = Get-APTPCandidateFromNote -Note $note -ExpectedHash $candidateHash
Assert-True ((Get-APTPTextHash $candidate) -ceq $candidateHash) 'The reviewed candidate was not extracted exactly.'
Assert-Rejected { Get-APTPCandidateFromNote -Note ($note + "`n" + ([string][char]96 * 3) + 'text') -ExpectedHash $candidateHash } 'A duplicated candidate block was accepted.'
Assert-Rejected { Get-APTPCandidateFromNote -Note ($note.Replace('Planning a good bus network', 'Modified network text')) -ExpectedHash $candidateHash } 'A modified candidate was accepted.'

$plan = New-APTPDescriptionPlan -Candidate $candidate -ExpectedOriginalHash $originalHash -ExpectedCandidateHash $candidateHash -PublishedFileId '3823739383' -AppId '255710'
Assert-True ((Get-APTPTextHash $plan.Original) -ceq $originalHash) 'The reconstructed historical source differs.'
Assert-True ($plan.Vdf -match '(?m)^\s*"appid" "255710"\r?$') 'The game ID is missing.'
Assert-True ($plan.Vdf -match '(?m)^\s*"publishedfileid" "3823739383"\r?$') 'The item ID is missing.'
Assert-True ($plan.Vdf.Contains($candidate)) 'The reviewed candidate is missing.'
Assert-True (-not ($plan.Vdf -match '(?m)^\s*"(contentfolder|previewfile|visibility|title|tags|changenote)"')) 'An unrelated VDF key was emitted.'
Assert-Rejected { New-APTPDescriptionPlan -Candidate $candidate -ExpectedOriginalHash $originalHash -ExpectedCandidateHash $candidateHash -PublishedFileId '3782858610' -AppId '255710' } 'A different Workshop item was accepted.'
Assert-Rejected { New-APTPDescriptionPlan -Candidate $candidate -ExpectedOriginalHash $originalHash -ExpectedCandidateHash $candidateHash -PublishedFileId '3823739383' -AppId '999' } 'A different game was accepted.'
Assert-Rejected { New-APTPDescriptionPlan -Candidate $candidate -ExpectedOriginalHash ('0' * 64) -ExpectedCandidateHash $candidateHash -PublishedFileId '3823739383' -AppId '255710' } 'A changed historical source was accepted.'
Assert-Rejected { New-APTPDescriptionPlan -Candidate ($candidate + 'changed') -ExpectedOriginalHash $originalHash -ExpectedCandidateHash $candidateHash -PublishedFileId '3823739383' -AppId '255710' } 'An unapproved candidate was accepted.'

$pageTitle = 'Steam Workshop::Automated Public Transport Planning: Release Version'
Assert-True (Test-APTPFinalUri -Url 'https://steamcommunity.com/sharedfiles/filedetails/?id=3823739383') 'The expected final URL was rejected.'
Assert-Rejected { Test-APTPFinalUri -Url 'https://steamcommunity.com/sharedfiles/filedetails/?id=999' } 'A redirected item was accepted.'
Assert-Rejected { Test-APTPFinalUri -Url 'https://steamcommunity.com.example.invalid/sharedfiles/filedetails/?id=3823739383' } 'A different host was accepted.'
Assert-Rejected { Test-APTPFinalUri -Url 'http://steamcommunity.com/sharedfiles/filedetails/?id=3823739383' } 'An insecure scheme was accepted.'
Assert-Rejected { Test-APTPFinalUri -Url 'https://steamcommunity.com/sharedfiles/filedetails/?id=3823739383&other=1' } 'An unexpected final query was accepted.'
$sample = '<html><title>' + $pageTitle + '</title><div class="workshopItemDescription" id="highlightContent"><div class="bb_h1">Example</div><b>Header.</b><ul class="bb_ul"><li><a class="bb_link" href="https://steamcommunity.com/linkfilter/?u=https%3A%2F%2Fexample.com%2Fpage">Link</a><span class="bb_link_host">[example.com]</span></li></ul></div></html>'
$sample = $sample.Replace('<html>', '<html><a href="https://steamcommunity.com/app/255710/workshop/">Workshop</a><div data-appid="255710"></div><a href="https://steamcommunity.com/sharedfiles/filedetails/?id=3823739383">Item</a>')
$sampleBBCode = "[h1]Example[/h1]`n[b]Header.[/b]`n[list]`n[*][url=https://example.com/page]Link[/url]`n[/list]`n"
$inner = Get-APTPDescriptionInnerHtml -Page $sample
Assert-True ($inner.Contains('<div class="bb_h1">Example</div>')) 'The nested description was not extracted.'
Assert-True (Test-APTPRenderedDescription -InnerHtml $inner -Candidate $sampleBBCode) 'The equivalent rendered description was rejected.'
Assert-Rejected { Get-APTPDescriptionInnerHtml -Page $sample.Replace($pageTitle, 'Wrong title') } 'A different item title was accepted.'
Assert-Rejected { Get-APTPDescriptionInnerHtml -Page $sample.Replace('/app/255710/workshop', '/app/999/workshop') } 'A different game workshop link was accepted.'
Assert-Rejected { Get-APTPDescriptionInnerHtml -Page $sample.Replace('data-appid="255710"', 'data-appid="999"') } 'A different app attribute was accepted.'
Assert-Rejected { Get-APTPDescriptionInnerHtml -Page $sample.Replace('filedetails/?id=3823739383', 'filedetails/?id=123') } 'A different page item ID was accepted.'
Assert-Rejected { Get-APTPDescriptionInnerHtml -Page $sample.Replace('id="highlightContent"', 'id="other"') } 'A missing container was accepted.'
Assert-Rejected { Get-APTPDescriptionInnerHtml -Page ($sample + $sample) } 'Duplicate description containers were accepted.'
Assert-Rejected { Get-APTPDescriptionInnerHtml -Page $sample.Replace('</div></html>', '</html>') } 'An unclosed description was accepted.'
Assert-Rejected { Test-APTPRenderedDescription -InnerHtml $inner.Replace('Header.', 'Changed.') -Candidate $sampleBBCode } 'Different visible text was accepted.'
Assert-Rejected { Test-APTPRenderedDescription -InnerHtml $inner.Replace('example.com%2Fpage', 'example.com%2Fother') -Candidate $sampleBBCode } 'A different destination was accepted.'
Assert-Rejected { Test-APTPRenderedDescription -InnerHtml $inner.Replace('<b>Header.</b>', '<em>Header.</em>') -Candidate $sampleBBCode } 'Different visible formatting was accepted.'
$multiBBCode = "[list]`n[*][url=https://example.com/first]First[/url]`n[*][url=https://example.org/second]Second[/url]`n[/list]`n"
$multiHtml = '<ul class="bb_ul"><li><a class="bb_link" href="https://steamcommunity.com/linkfilter/?u=https%3A%2F%2Fexample.com%2Ffirst">First</a><span class="bb_link_host">[example.com]</span></li><li><a class="bb_link" href="https://steamcommunity.com/linkfilter/?u=https%3A%2F%2Fexample.org%2Fsecond">Second</a><span class="bb_link_host">[example.org]</span></li></ul>'
Assert-True (Test-APTPRenderedDescription -InnerHtml $multiHtml -Candidate $multiBBCode) 'Equivalent multiple rendered links were rejected.'
Assert-Rejected { Test-APTPRenderedDescription -InnerHtml $multiHtml.Replace('example.com%2Ffirst', 'example.org%2Fsecond').Replace('example.org%2Fsecond">Second', 'example.com%2Ffirst">Second') -Candidate $multiBBCode } 'Swapped link destinations were accepted.'

$body = @(
    'Update the Automated Public Transport Planning release description only.'
    'Historical source SHA-256: 8719E1A372DD50B137121C632CC75CFFF5890496F8C59E934B6EC0B99BCE370E'
    'Reviewed candidate SHA-256: 75489BBBA894C8CD8FC2337BBF4E6615C2D41003D8524EA95C7D1CF6762943EA'
    'Observed description HTML SHA-256: 7982E44A961BFE4A6C34CBC52F81E04D53342E0D887FEDCDD2F1E6EB2E850E2E'
    'Visible content was compared to the historical source; the raw BBCode could not be verified.'
    'No content folder, title, image, tags or visibility changes.'
    'Signature: OpenAI-Codex.'
) -join "`n"
function New-TestIssue {
    return [pscustomobject]@{ number = 42; title = '[Workshop metadata] APTP 3823739383'; body = $body; state = 'open';
        created_at = '2026-11-05T01:00:00Z'; updated_at = '2026-11-05T01:01:00Z';
        user = [pscustomobject]@{ login = 'KingsleyHK0323' }; labels = @([pscustomobject]@{ name = 'steam-workshop-aptp-description-approved' }) }
}
$event = [pscustomobject]@{ id = 42; created_at = '2026-11-05T01:01:00Z'; event = 'labeled'; actor = [pscustomobject]@{ login = 'KingsleyHK0323' }; label = [pscustomobject]@{ name = 'steam-workshop-aptp-description-approved' } }
$issue = New-TestIssue
Assert-True (Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42') 'The owner approval was rejected.'
Assert-True (Test-APTPOwnerIssue -Issue $issue -Events (,@($event)) -RequestedIssueNumber '42') 'A nested REST event list was rejected.'
$issue = New-TestIssue; $issue.number = 43
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42' } 'A different issue was accepted.'
$issue = New-TestIssue; $issue.body += ' edited'
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42' } 'An edited issue body was accepted.'
$issue = New-TestIssue; $issue.user.login = 'someone-else'
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42' } 'A non-owner issue was accepted.'
$issue = New-TestIssue; $issue.title = 'Different item'
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42' } 'A different issue title was accepted.'
$issue = New-TestIssue; $issue.state = 'closed'
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42' } 'A closed issue was accepted.'
$issue = New-TestIssue; $issue | Add-Member -NotePropertyName pull_request -NotePropertyValue @{}
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42' } 'A pull request was accepted.'
$issue = New-TestIssue; $issue.updated_at = '2026-11-05T01:02:00Z'
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42' } 'An issue changed after approval was accepted.'
$issue = New-TestIssue; $issue.labels = @()
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber '42' } 'A missing approval label was accepted.'
$issue = New-TestIssue; $removed = [pscustomobject]@{ event = 'unlabeled'; actor = [pscustomobject]@{ login = 'someone-else' }; label = [pscustomobject]@{ name = 'steam-workshop-aptp-description-approved' } }
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event, $removed) -RequestedIssueNumber '42' } 'A removed approval was accepted.'
$other = [pscustomobject]@{ event = 'labeled'; created_at = '2026-11-05T01:01:00Z'; actor = [pscustomobject]@{ login = 'someone-else' }; label = [pscustomobject]@{ name = 'steam-workshop-aptp-description-approved' } }
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event, $other) -RequestedIssueNumber '42' } 'A non-owner label event was accepted.'
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @() -RequestedIssueNumber '42' } 'Missing approval history was accepted.'
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events (@($event) * 100) -RequestedIssueNumber '42' } 'Potentially incomplete event history was accepted.'
Assert-Rejected { Test-APTPOwnerIssue -Issue $issue -Events @($event) -RequestedIssueNumber 'not-a-number' } 'A nonnumeric issue number was accepted.'
Write-Output "Passed $count APTP description-update checks."
