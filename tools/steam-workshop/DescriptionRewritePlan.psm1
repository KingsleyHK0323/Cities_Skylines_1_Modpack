# Signature: OpenAI-Codex.
Set-StrictMode -Version Latest

function Get-RewriteTextHash {
    param([Parameter(Mandatory = $true)][string]$Text)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text))).Replace('-', '').ToLowerInvariant() }
    finally { $algorithm.Dispose() }
}

function New-RewriteVdf {
    param([Parameter(Mandatory = $true)][string]$WorkshopItemId,
          [Parameter(Mandatory = $true)][string]$Description)
    if ($Description -cmatch '[^\x09\x0A\x0D\x20-\x7E]') { throw 'The description contains unsupported control characters.' }
    if ($WorkshopItemId -cnotmatch '^[1-9][0-9]{9}$' -or [string]::IsNullOrWhiteSpace($Description) -or
        [Text.Encoding]::UTF8.GetByteCount($Description) -gt 8000 -or $Description.Contains('"') -or
        $Description.Contains('\') -or $Description.Contains([char]0) -or $Description -cmatch '[^\x00-\x7F]') {
        throw 'The target or description is invalid for the reviewed VDF convention.'
    }
    return (@('"workshopitem"', '{', '    "appid" "255710"',
        ('    "publishedfileid" "' + $WorkshopItemId + '"'),
        ('    "description" "' + $Description + '"'), '}') -join [Environment]::NewLine) + [Environment]::NewLine
}

function New-RewriteDescriptionPlan {
    param([Parameter(Mandatory = $true)][string]$ManifestJson,
          [Parameter(Mandatory = $true)][string]$ExpectedManifestHash,
          [Parameter(Mandatory = $true)][string]$Draft,
          [Parameter(Mandatory = $true)][string]$ExpectedDraftHash,
          [Parameter(Mandatory = $true)][string]$Module,
          [Parameter(Mandatory = $true)][int]$ExpectedItemCount)
    if ($ExpectedManifestHash -notmatch '^[0-9a-fA-F]{64}$' -or $ExpectedDraftHash -notmatch '^[0-9a-fA-F]{64}$' -or
        (Get-RewriteTextHash $ManifestJson) -cne $ExpectedManifestHash.ToLowerInvariant() -or
        (Get-RewriteTextHash $Draft) -cne $ExpectedDraftHash.ToLowerInvariant()) {
        throw 'The local review files do not match the pinned hashes.'
    }
    $manifest = $ManifestJson | ConvertFrom-Json -Depth 16
    if ([int]$manifest.schemaVersion -ne 1 -or [string]$manifest.status -cne 'draft-not-approved-or-published' -or
        [string]$manifest.preparedBy -cne 'OpenAI-Codex' -or
        [string]$manifest.sourceFile -cne 'Steam_Workshop_Descriptions_Role_Rewrite_2026-11-05_Review.md' -or
        [string]$manifest.sourceFileSha256 -cne $ExpectedDraftHash.ToLowerInvariant() -or
        [string]$manifest.descriptionHashConvention -cne 'UTF-8, LF line endings, exactly one trailing LF') {
        throw 'The review manifest does not match the approved format.'
    }
    $items = @($manifest.items)
    if ($ExpectedItemCount -le 0 -or $items.Count -ne $ExpectedItemCount) { throw 'The manifest item count is invalid.' }
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($item in $items) {
        if ([string]::IsNullOrWhiteSpace([string]$item.module) -or [string]$item.workshopItemId -cnotmatch '^[1-9][0-9]{9}$' -or
            [string]$item.descriptionSha256 -cnotmatch '^[0-9a-f]{64}$' -or [int]$item.descriptionUtf8Bytes -le 0 -or
            [int]$item.descriptionUtf8Bytes -gt 8000 -or -not $names.Add([string]$item.module) -or
            -not $ids.Add([string]$item.workshopItemId)) { throw 'The manifest contains an invalid or duplicate item.' }
    }
    $selected = @($items | Where-Object { [string]$_.module -ceq $Module })
    if ($selected.Count -ne 1) { throw 'The requested module is not uniquely present.' }
    $item = $selected[0]
    $normalized = $Draft.Replace("`r`n", "`n")
    if ($normalized.Contains("`r")) { throw 'The draft has unsupported line endings.' }
    $pattern = '(?ms)^## [0-9]+\. ' + [regex]::Escape($Module) + ' - Workshop ID ' +
        [regex]::Escape([string]$item.workshopItemId) + '\s*\n~~~text\n(.*?)\n~~~\s*$'
    $blocks = [regex]::Matches($normalized, $pattern)
    if ($blocks.Count -ne 1) { throw 'The reviewed description block is missing or duplicated.' }
    $description = $blocks[0].Groups[1].Value + "`n"
    if ((Get-RewriteTextHash $description) -cne [string]$item.descriptionSha256 -or
        [Text.Encoding]::UTF8.GetByteCount($description) -ne [int]$item.descriptionUtf8Bytes) {
        throw 'The selected description does not match the reviewed manifest.'
    }
    $vdf = New-RewriteVdf -WorkshopItemId ([string]$item.workshopItemId) -Description $description
    return [pscustomobject]@{ Module = $Module; WorkshopItemId = [string]$item.workshopItemId; Description = $description; DescriptionHash = [string]$item.descriptionSha256; Vdf = $vdf }
}

function Get-RewriteIssueBody {
    param([Parameter(Mandatory = $true)][string]$Module,
          [Parameter(Mandatory = $true)][string]$ManifestHash,
          [Parameter(Mandatory = $true)][string]$DraftHash,
          [Parameter(Mandatory = $true)][string]$DescriptionHash)
    return @('Apply the reviewed Workshop description rewrite.', ('Module: ' + $Module),
        ('Manifest SHA-256: ' + $ManifestHash.ToLowerInvariant()), ('Draft SHA-256: ' + $DraftHash.ToLowerInvariant()),
        ('Description SHA-256: ' + $DescriptionHash.ToLowerInvariant()),
        'I accept full replacement even if the previous description cannot be retrieved.',
        'Description only; no content folder, title, image, tags or visibility changes.',
        'Prepared by OpenAI-Codex.') -join "`n"
}

function Test-RewriteOwnerIssue {
    param([Parameter(Mandatory = $true)]$Issue,
          [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Events,
          [Parameter(Mandatory = $true)][string]$IssueNumber,
          [Parameter(Mandatory = $true)][string]$Module,
          [Parameter(Mandatory = $true)][string]$ManifestHash,
          [Parameter(Mandatory = $true)][string]$DraftHash,
          [Parameter(Mandatory = $true)][string]$DescriptionHash)
    $expectedBody = Get-RewriteIssueBody -Module $Module -ManifestHash $ManifestHash -DraftHash $DraftHash -DescriptionHash $DescriptionHash
    if ([string]$Issue.repository_url -cne 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack') {
        throw 'The issue belongs to a different repository.'
    }
    if ($IssueNumber -notmatch '^[1-9][0-9]{0,9}$' -or [string]$Issue.number -cne $IssueNumber -or
        $null -ne $Issue.PSObject.Properties['pull_request'] -or [string]$Issue.title -cne ('[Workshop description rewrite] ' + $Module) -or
        [string]$Issue.state -cne 'open' -or [string]$Issue.user.login -ine 'KingsleyHK0323' -or
        ([string]$Issue.body).Replace("`r`n", "`n").Trim() -cne $expectedBody) { throw 'The issue does not match the reviewed owner request.' }
    $label = 'steam-workshop-description-rewrite-approved'
    if (@($Issue.labels | Where-Object { [string]$_.name -ceq $label }).Count -ne 1) { throw 'The owner approval label is missing or duplicated.' }
    $Events = @($Events | ForEach-Object { $_ })
    if ($Events.Count -eq 0 -or $Events.Count -ge 100) { throw 'The approval history is unavailable or may be incomplete.' }
    $previousTime = [DateTimeOffset]::MinValue
    foreach ($entry in $Events) {
        if ($null -eq $entry.PSObject.Properties['label'] -or $null -eq $entry.label) { continue }
        if ([string]$entry.label.name -cne $label -or [string]$entry.event -notin @('labeled', 'unlabeled')) { continue }
        if ([string]$entry.id -notmatch '^[1-9][0-9]*$' -or
            [string]$entry.url -cne ('https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/events/' + [string]$entry.id)) {
            throw 'An approval event is not from the expected repository.'
        }
        $eventTime = ([DateTimeOffset]$entry.created_at).ToUniversalTime()
        if ($eventTime -le $previousTime) { throw 'The approval event order is ambiguous.' }
        $previousTime = $eventTime
    }
    $last = $null
    foreach ($entry in $Events) {
        if ($null -eq $entry.PSObject.Properties['label'] -or $null -eq $entry.label) { continue }
        if ([string]$entry.label.name -ceq $label -and [string]$entry.event -in @('labeled', 'unlabeled')) { $last = $entry }
    }
    if ($null -eq $last -or [string]$last.event -cne 'labeled' -or [string]$last.actor.login -ine 'KingsleyHK0323' -or
        ([DateTimeOffset]$Issue.created_at).ToUniversalTime() -gt ([DateTimeOffset]$last.created_at).ToUniversalTime() -or
        ([DateTimeOffset]$Issue.updated_at).ToUniversalTime() -gt ([DateTimeOffset]$last.created_at).ToUniversalTime()) {
        throw 'The latest label does not approve the current owner issue.'
    }
    if (([DateTimeOffset]$Issue.updated_at).ToUniversalTime() -ge ([DateTimeOffset]$last.created_at).ToUniversalTime() -or
        ([DateTimeOffset]$Issue.created_at).ToUniversalTime() -ge ([DateTimeOffset]$last.created_at).ToUniversalTime()) {
        throw 'The issue snapshot must predate the approval label by at least one second.'
    }
    return $true
}

Export-ModuleMember -Function Get-RewriteTextHash, New-RewriteVdf, New-RewriteDescriptionPlan, Get-RewriteIssueBody, Test-RewriteOwnerIssue

function Test-RewriteRunContext {
    param([Parameter(Mandatory = $true)][Collections.IDictionary]$Context,
          [Parameter(Mandatory = $true)][string]$Mode)
    $expected = @{
        GITHUB_ACTIONS = 'true'; GITHUB_REPOSITORY = 'KingsleyHK0323/Cities_Skylines_1_Modpack'
        GITHUB_ACTOR = 'KingsleyHK0323'; GITHUB_TRIGGERING_ACTOR = 'KingsleyHK0323'
        GITHUB_REF = 'refs/heads/Modpack_Website'; GITHUB_EVENT_NAME = 'workflow_dispatch'
        GITHUB_RUN_ATTEMPT = '1'; WORKSHOP_RELEASE_APPROVAL_READY = 'true'
    }
    if ($Mode -cne 'APPLY REVIEWED DESCRIPTION') { throw 'The explicit apply mode is required.' }
    foreach ($name in $expected.Keys) {
        if (-not $Context.Contains($name) -or [string]$Context[$name] -cne [string]$expected[$name]) {
            throw 'The workflow context does not match the approved owner run.'
        }
    }
    return $true
}

# Display-name claims are retained for the offline fixture only; do not use them for submissions.
function Get-RewriteClaimName {
    param([Parameter(Mandatory = $true)][string]$Module,
          [Parameter(Mandatory = $true)][string]$DescriptionHash)
    if ($Module.Length -gt 120 -or $Module -cnotmatch '^[A-Za-z0-9 ]+$' -or $DescriptionHash -cnotmatch '^[0-9a-f]{64}$') {
        throw 'The one-shot claim identity is invalid.'
    }
    return (Get-RewriteTextHash $Module).Substring(0, 16) + '-' + $DescriptionHash + '.once'
}

Export-ModuleMember -Function Test-RewriteRunContext, Get-RewriteClaimName

function Get-RewriteItemClaimName {
    param([Parameter(Mandatory = $true)][string]$WorkshopItemId,
          [Parameter(Mandatory = $true)][string]$DescriptionHash)
    if ($WorkshopItemId -cnotmatch '^[1-9][0-9]{9}$' -or $DescriptionHash -cnotmatch '^[0-9a-f]{64}$') {
        throw 'The one-shot item identity is invalid.'
    }
    return (Get-RewriteTextHash $WorkshopItemId).Substring(0, 16) + '-' + $DescriptionHash + '.once'
}

function Test-RewritePinnedOwnerIssue {
    param([Parameter(Mandatory = $true)]$Issue,
          [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Events,
          [Parameter(Mandatory = $true)][string]$IssueNumber,
          [Parameter(Mandatory = $true)][string]$Module,
          [Parameter(Mandatory = $true)][string]$ManifestHash,
          [Parameter(Mandatory = $true)][string]$DraftHash,
          [Parameter(Mandatory = $true)][string]$DescriptionHash,
          [Parameter(Mandatory = $true)][string]$ControllerCommit)
    if ($ControllerCommit -cnotmatch '^[0-9a-f]{40}$' -or $null -ne $Issue.PSObject.Properties['pull_request']) {
        throw 'The controller commit or issue type is invalid.'
    }
    $base = Get-RewriteIssueBody -Module $Module -ManifestHash $ManifestHash -DraftHash $DraftHash -DescriptionHash $DescriptionHash
    if (([string]$Issue.body).Replace("`r`n", "`n").Trim() -cne ($base + "`nController commit: " + $ControllerCommit)) {
        throw 'The issue does not approve the running controller commit.'
    }
    $snapshot = [pscustomobject]@{
        number = $Issue.number; title = $Issue.title; state = $Issue.state; user = $Issue.user
        body = $base; created_at = $Issue.created_at; updated_at = $Issue.updated_at
        labels = $Issue.labels; repository_url = $Issue.repository_url
    }
    return Test-RewriteOwnerIssue -Issue $snapshot -Events $Events -IssueNumber $IssueNumber -Module $Module -ManifestHash $ManifestHash -DraftHash $DraftHash -DescriptionHash $DescriptionHash
}

Export-ModuleMember -Function Get-RewriteItemClaimName, Test-RewritePinnedOwnerIssue

function Get-RewriteBatchIssueBody {
    param([Parameter(Mandatory = $true)][string]$ManifestHash,
          [Parameter(Mandatory = $true)][string]$DraftHash,
          [Parameter(Mandatory = $true)][string]$ControllerCommit)
    if ($ManifestHash -cnotmatch '^[0-9a-f]{64}$' -or $DraftHash -cnotmatch '^[0-9a-f]{64}$' -or
        $ControllerCommit -cnotmatch '^[0-9a-f]{40}$') { throw 'The approved batch hashes or commit are invalid.' }
    return @('Apply the reviewed Workshop description rewrite for the 15 modules in the pinned local manifest.',
        ('Manifest SHA-256: ' + $ManifestHash), ('Draft SHA-256: ' + $DraftHash), ('Controller commit: ' + $ControllerCommit),
        '12 items were hidden in the owner screenshots and their current descriptions have not been retrieved.',
        'I accept full replacement even if previous descriptions cannot be retrieved.',
        'Descriptions only; no content folder, title, image, tags or visibility changes.',
        'Prepared by OpenAI-Codex.') -join "`n"
}

function Test-RewriteBatchOwnerIssue {
    param([Parameter(Mandatory = $true)]$Issue,
          [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Events,
          [Parameter(Mandatory = $true)][string]$IssueNumber,
          [Parameter(Mandatory = $true)][string]$ManifestHash,
          [Parameter(Mandatory = $true)][string]$DraftHash,
          [Parameter(Mandatory = $true)][string]$ControllerCommit)
    $expected = Get-RewriteBatchIssueBody -ManifestHash $ManifestHash -DraftHash $DraftHash -ControllerCommit $ControllerCommit
    if ($null -ne $Issue.PSObject.Properties['pull_request'] -or
        [string]$Issue.title -cne '[Workshop description rewrite] All 15 modules' -or
        ([string]$Issue.body).Replace("`r`n", "`n").Trim() -cne $expected) {
        throw 'The issue does not approve this pinned batch and controller commit.'
    }
    $placeholderHash = '0' * 64
    $base = Get-RewriteIssueBody -Module 'Batch Review' -ManifestHash $ManifestHash -DraftHash $DraftHash -DescriptionHash $placeholderHash
    $snapshot = [pscustomobject]@{
        number = $Issue.number; title = '[Workshop description rewrite] Batch Review'; state = $Issue.state
        user = $Issue.user; body = $base; created_at = $Issue.created_at; updated_at = $Issue.updated_at
        labels = $Issue.labels; repository_url = $Issue.repository_url
    }
    return Test-RewriteOwnerIssue -Issue $snapshot -Events $Events -IssueNumber $IssueNumber -Module 'Batch Review' -ManifestHash $ManifestHash -DraftHash $DraftHash -DescriptionHash $placeholderHash
}

Export-ModuleMember -Function Get-RewriteBatchIssueBody, Test-RewriteBatchOwnerIssue
