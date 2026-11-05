# Signature: OpenAI-Codex.
Set-StrictMode -Version Latest

function Get-CorrectionTextHash {
    param([Parameter(Mandatory = $true)][string]$Text)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        return [BitConverter]::ToString($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text))).Replace('-', '')
    } finally { $algorithm.Dispose() }
}

function New-AIImproveDescriptionCorrection {
    param(
        [Parameter(Mandatory = $true)][string]$Original,
        [Parameter(Mandatory = $true)][string]$Observed,
        [Parameter(Mandatory = $true)][string]$PublishedFileId,
        [Parameter(Mandatory = $true)][string]$CreatorAppId,
        [Parameter(Mandatory = $true)][string]$ConsumerAppId,
        [Parameter(Mandatory = $true)][string]$ExpectedOriginalHash,
        [Parameter(Mandatory = $true)][string]$ExpectedCorrectedHash
    )
    if ($PublishedFileId -cne '3782858610' -or $CreatorAppId -cne '255710' -or $ConsumerAppId -cne '255710') {
        throw 'The observed item or game does not match AI Improve.'
    }
    if ([string]::IsNullOrWhiteSpace($Original) -or $Original -cne $Observed -or
        (Get-CorrectionTextHash $Original) -cne $ExpectedOriginalHash) {
        throw 'The current description does not match the reviewed snapshot.'
    }
    $oldLink = '[*]Want to try changes before they reach this version? See the [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3823575900]Development Version[/url] ' + [char]0x2013 + ' but never subscribe to both.'
    $newLink = '[*]Development builds are installed with Dev Manager; see Development version below.'
    $oldLicense = 'Source code (GPL-3.0)'
    $newLicense = 'Source code (PolyForm Noncommercial License 1.0.0)'
    foreach ($value in @($oldLink, $oldLicense)) {
        $first = $Original.IndexOf($value, [StringComparison]::Ordinal)
        if ($first -lt 0 -or $first -ne $Original.LastIndexOf($value, [StringComparison]::Ordinal)) {
            throw 'An approved original phrase is missing or not unique.'
        }
    }
    foreach ($value in @($newLink, $newLicense)) {
        if ($Original.Contains($value)) { throw 'An approved replacement is already present.' }
    }
    $corrected = $Original.Replace($oldLink, $newLink).Replace($oldLicense, $newLicense)
    if ((Get-CorrectionTextHash $corrected) -cne $ExpectedCorrectedHash -or
        $corrected.Length -gt 8000 -or $corrected.Contains('"') -or
        $corrected.Contains('\') -or $corrected.Contains([char]0)) {
        throw 'The corrected description is not the reviewed, VDF-safe content.'
    }
    $vdf = (@(
        '"workshopitem"'
        '{'
        '    "appid" "255710"'
        '    "publishedfileid" "3782858610"'
        ('    "description" "' + $corrected + '"')
        '}'
    ) -join [Environment]::NewLine) + [Environment]::NewLine
    return [pscustomobject]@{ Description = $corrected; Vdf = $vdf }
}

function Test-CorrectionIssueApproved {
    param([Parameter(Mandatory = $true)]$Issue,
          [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Events)
    $body = @(
        'Correct the AI Improve release description only.'
        'Original SHA-256: 537A05C2C4672BA4C82371047B58924C3BD2A7D38F468A4F50452BC762390629'
        'Corrected SHA-256: CF59F60DDF8765DFAE722AA08775D385C8054CBF80578EBB08EE695CB2B36342'
        'Replace the old Workshop development link with the Dev Manager instruction.'
        'Change the source code label to PolyForm Noncommercial License 1.0.0.'
        'No content, title, preview, tags or visibility changes.'
        'Signature: OpenAI-Codex.'
    ) -join "`n"
    if ($null -ne $Issue.PSObject.Properties['pull_request'] -or [int]$Issue.number -ne 2 -or
        [string]$Issue.title -cne '[Workshop description correction] AI Improve 3782858610' -or
        [string]$Issue.state -cne 'open' -or [string]$Issue.user.login -ine 'KingsleyHK0323' -or
        ([DateTimeOffset]$Issue.created_at).ToUniversalTime() -ne [DateTimeOffset]::Parse('2026-11-05T01:16:59Z') -or
        ([DateTimeOffset]$Issue.updated_at).ToUniversalTime() -ne [DateTimeOffset]::Parse('2026-11-05T01:16:59Z') -or
        ([string]$Issue.body).Replace("`r`n", "`n").Trim() -cne $body) {
        throw 'The issue does not match the approved correction.'
    }
    $labelName = 'steam-workshop-description-correction-approved'
    if (@($Issue.labels | Where-Object { [string]$_.name -ceq $labelName }).Count -ne 1) {
        throw 'The correction approval label is not present exactly once.'
    }
    $Events = @($Events | ForEach-Object { $_ })
    if ($Events.Count -eq 0 -or $Events.Count -ge 100) { throw 'Approval history is missing or may be incomplete.' }
    $last = $null
    foreach ($entry in $Events) {
        if ($null -eq $entry.PSObject.Properties['label'] -or $null -eq $entry.label) { continue }
        if ([string]$entry.label.name -ceq $labelName -and [string]$entry.event -in @('labeled', 'unlabeled')) { $last = $entry }
    }
    if ($null -eq $last -or [string]$last.event -cne 'labeled' -or
        [string]$last.actor.login -ine 'KingsleyHK0323' -or [string]$last.id -cne '35193015737' -or
        ([DateTimeOffset]$last.created_at).ToUniversalTime() -ne [DateTimeOffset]::Parse('2026-11-05T01:17:00Z')) {
        throw 'The specific owner approval event is missing or no longer current.'
    }
    return $true
}

Export-ModuleMember -Function Get-CorrectionTextHash, New-AIImproveDescriptionCorrection, Test-CorrectionIssueApproved
