# Signature: OpenAI-Codex.
Set-StrictMode -Version Latest

function Get-MetadataTextHash {
    param([Parameter(Mandatory = $true)][string]$Text)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
        return [BitConverter]::ToString($algorithm.ComputeHash($bytes)).Replace('-', '')
    } finally { $algorithm.Dispose() }
}

function New-MetadataOnlyVdf {
    param(
        [Parameter(Mandatory = $true)][string]$Original,
        [Parameter(Mandatory = $true)][string]$Observed,
        [Parameter(Mandatory = $true)][string]$Proposed,
        [Parameter(Mandatory = $true)][string]$PublishedFileId,
        [Parameter(Mandatory = $true)][string]$CreatorAppId,
        [Parameter(Mandatory = $true)][string]$ConsumerAppId,
        [Parameter(Mandatory = $true)][string]$ExpectedOriginalHash,
        [Parameter(Mandatory = $true)][string]$ExpectedProposedHash
    )
    if ($PublishedFileId -cne '3782858610' -or $CreatorAppId -cne '255710' -or $ConsumerAppId -cne '255710') {
        throw 'The observed item or game does not match the approved AI Improve target.'
    }
    if ([string]::IsNullOrWhiteSpace($Original) -or $Original -cne $Observed) {
        throw 'The existing description is unavailable or changed after the snapshot.'
    }
    if ((Get-MetadataTextHash $Original) -cne $ExpectedOriginalHash -or
        (Get-MetadataTextHash $Proposed) -cne $ExpectedProposedHash) {
        throw 'A staged description does not match its reviewed SHA-256.'
    }
    if ($Proposed.Length -le $Original.Length -or $Proposed.Length -gt 8000 -or
        -not $Proposed.StartsWith($Original, [StringComparison]::Ordinal)) {
        throw 'The proposal must retain the complete existing description and append text.'
    }
    if ($Proposed.Contains('"') -or $Proposed.Contains('\') -or $Proposed.Contains([char]0)) {
        throw 'The proposal contains a character not supported by the reviewed VDF convention.'
    }
    $newline = [Environment]::NewLine
    return (@(
        '"workshopitem"'
        '{'
        '    "appid" "255710"'
        '    "publishedfileid" "3782858610"'
        ('    "description" "' + $Proposed + '"')
        '}'
    ) -join $newline) + $newline
}

function Test-MetadataIssueApproved {
    param([Parameter(Mandatory = $true)]$Issue)
    $body = @(
        'Update the AI Improve release description only.'
        'Original SHA-256: E5D53622EE0841720F72C254C5FA8C2990685A7EFDE129212B796F46CD640485'
        'Proposed SHA-256: 537A05C2C4672BA4C82371047B58924C3BD2A7D38F468A4F50452BC762390629'
        'No content, title, preview, tags or visibility changes.'
    ) -join "`n"
    if ($null -ne $Issue.PSObject.Properties['pull_request'] -or
        [string]$Issue.title -cne '[Workshop metadata] AI Improve 3782858610' -or
        [string]$Issue.state -cne 'open' -or [string]$Issue.user.login -ine 'KingsleyHK0323' -or
        ([string]$Issue.body).Replace("`r`n", "`n").Trim() -cne $body) {
        throw 'The issue does not match the reviewed owner request.'
    }
    $approved = @($Issue.labels | Where-Object { [string]$_.name -ceq 'steam-workshop-metadata-approved' })
    if ($approved.Count -ne 1) { throw 'The owner approval label is not present exactly once.' }
    return $true
}

function Test-MetadataApprovalEvents {
    param([Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Events)
    if ($Events.Count -eq 0 -or $Events.Count -ge 100) { throw 'Approval history is missing or may be incomplete.' }
    $last = $null
    foreach ($entry in $Events) {
        if ($null -eq $entry.PSObject.Properties['label'] -or $null -eq $entry.label) { continue }
        if ([string]$entry.label.name -ceq 'steam-workshop-metadata-approved' -and
            [string]$entry.event -in @('labeled', 'unlabeled')) { $last = $entry }
    }
    if ($null -eq $last -or [string]$last.event -cne 'labeled' -or
        [string]$last.actor.login -ine 'KingsleyHK0323') {
        throw 'The most recent metadata approval label was not applied by the owner.'
    }
    return $true
}

function Get-SteamCmdEnvironment {
    param([Parameter(Mandatory = $true)][Collections.IDictionary]$Environment)
    $allowed = @{}
    foreach ($name in @('SystemRoot', 'windir', 'SystemDrive', 'COMSPEC', 'PATH', 'PATHEXT',
        'TEMP', 'TMP', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'PROGRAMDATA',
        'PROGRAMFILES', 'PROGRAMFILES(X86)', 'HOMEDRIVE', 'HOMEPATH')) {
        if ($Environment.Contains($name) -and -not [string]::IsNullOrEmpty([string]$Environment[$name])) {
            $allowed[$name] = [string]$Environment[$name]
        }
    }
    return $allowed
}

Export-ModuleMember -Function Get-MetadataTextHash, New-MetadataOnlyVdf, Test-MetadataIssueApproved, Test-MetadataApprovalEvents, Get-SteamCmdEnvironment
