# Signature: OpenAI-Codex.
Set-StrictMode -Version Latest

function Get-APTPTextHash {
    param([Parameter(Mandatory = $true)][string]$Text)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($Text))).Replace('-', '') }
    finally { $algorithm.Dispose() }
}

function Get-APTPCandidateFromNote {
    param([Parameter(Mandatory = $true)][string]$Note, [Parameter(Mandatory = $true)][string]$ExpectedHash)
    $normalized = $Note.Replace("`r`n", "`n")
    $fence = ([string][char]96) * 3
    $opening = $fence + 'text'
    $first = $normalized.IndexOf($opening, [StringComparison]::Ordinal)
    if ($first -lt 0 -or $first -ne $normalized.LastIndexOf($opening, [StringComparison]::Ordinal) -or
        -not $normalized.Substring($first).StartsWith($opening + "`n", [StringComparison]::Ordinal)) {
        throw 'The candidate note does not contain exactly one text block.'
    }
    $start = $first + $opening.Length + 1
    $end = $normalized.IndexOf("`n" + $fence, $start, [StringComparison]::Ordinal)
    if ($end -lt 0) { throw 'The candidate text block has no closing fence.' }
    $candidate = $normalized.Substring($start, $end - $start) + "`n"
    if ((Get-APTPTextHash $candidate) -cne $ExpectedHash) { throw 'The candidate text does not match the reviewed SHA-256.' }
    return $candidate
}

function New-APTPDescriptionPlan {
    param([Parameter(Mandatory = $true)][string]$Candidate,
          [Parameter(Mandatory = $true)][string]$ExpectedOriginalHash,
          [Parameter(Mandatory = $true)][string]$ExpectedCandidateHash,
          [Parameter(Mandatory = $true)][string]$PublishedFileId,
          [Parameter(Mandatory = $true)][string]$AppId)
    if ($PublishedFileId -cne '3823739383' -or $AppId -cne '255710') { throw 'The item or game does not match APTP.' }
    if ((Get-APTPTextHash $Candidate) -cne $ExpectedCandidateHash -or $Candidate.Length -gt 8000 -or
        $Candidate.Contains('"') -or $Candidate.Contains('\') -or $Candidate.Contains([char]0)) {
        throw 'The candidate is not the reviewed, VDF-safe description.'
    }
    $oldDev = '[*]Testing builds are published as the [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3823739653]Development Version[/url] ' + [char]0x2013 + ' never subscribe to both.'
    $newDev = '[*]To install an available development version, use the separate [url=https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases]Dev Manager installer[/url]. Availability varies by module. Avoid enabling the release and development versions of this module at the same time.'
    $oldSite = 'https://spacesquare640.github.io/Cities_Skylines_1_Modpack/'
    $newSite = 'https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/'
    $oldGithub = 'https://github.com/SpaceSquare640/Cities_Skylines_1_Modpack'
    $newGithub = 'https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack'
    $oldLabel = 'Source code (GPL-3.0)'
    $newLabel = 'Source code (PolyForm Noncommercial License 1.0.0)'
    $licenseLine = '[*][url=https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/Automated_Public_Transport_Planning/LICENSE]LICENSE[/url]'
    foreach ($entry in @(@($newDev, 1), @($newSite, 2), @($newGithub, 4), @($newLabel, 1), @($licenseLine, 1))) {
        if ([regex]::Matches($Candidate, [regex]::Escape([string]$entry[0])).Count -ne [int]$entry[1]) {
            throw 'A reviewed replacement is missing or not unique.'
        }
    }
    $original = $Candidate.Replace($licenseLine + "`n", '').Replace($newDev, $oldDev).Replace($newSite, $oldSite).Replace($newGithub, $oldGithub).Replace($newLabel, $oldLabel)
    if ((Get-APTPTextHash $original) -cne $ExpectedOriginalHash) { throw 'The reconstructed historical description does not match its SHA-256.' }
    $vdf = (@('"workshopitem"', '{', '    "appid" "255710"', '    "publishedfileid" "3823739383"', ('    "description" "' + $Candidate + '"'), '}') -join [Environment]::NewLine) + [Environment]::NewLine
    return [pscustomobject]@{ Original = $original; Candidate = $Candidate; Vdf = $vdf }
}

function Get-APTPDescriptionInnerHtml {
    param([Parameter(Mandatory = $true)][string]$Page)
    if ($Page.Length -gt 5000000) { throw 'The Steam page is unexpectedly large.' }
    $title = [regex]::Match($Page, '(?is)<title>\s*(.*?)\s*</title>')
    if (-not $title.Success -or [Net.WebUtility]::HtmlDecode($title.Groups[1].Value) -cne 'Steam Workshop::Automated Public Transport Planning: Release Version') {
        throw 'Steam did not return the expected APTP page.'
    }
    if (-not [regex]::IsMatch($Page, '(?i)https?://steamcommunity\.com/app/255710/workshop(?:[/?"#]|$)') -or
        -not [regex]::IsMatch($Page, '(?i)(?:data-appid|data-app-id)="255710"') -or
        -not [regex]::IsMatch($Page, '(?i)filedetails/\?id=3823739383(?:[&"#]|$)')) {
        throw 'The Steam page does not identify the expected item and game.'
    }
    $openings = [regex]::Matches($Page, '(?i)<div\s+class="workshopItemDescription"\s+id="highlightContent"\s*>')
    if ($openings.Count -ne 1) { throw 'The description container is missing or duplicated.' }
    $open = $openings[0]
    $tags = [regex]::new('(?i)<(/?)div\b[^>]*>')
    $depth = 0
    foreach ($tag in $tags.Matches($Page, $open.Index)) {
        if ($tag.Groups[1].Value -eq '') { $depth++ } else { $depth-- }
        if ($depth -eq 0) {
            $start = $open.Index + $open.Length
            $inner = $Page.Substring($start, $tag.Index - $start)
            if ([string]::IsNullOrWhiteSpace($inner)) { throw 'The description is empty or unavailable.' }
            return $inner
        }
    }
    throw 'The description container is not closed.'
}

function Test-APTPFinalUri {
    param([Parameter(Mandatory = $true)][string]$Url)
    if ($Url -cne 'https://steamcommunity.com/sharedfiles/filedetails/?id=3823739383') {
        throw 'The final Steam URL is not the approved APTP item.'
    }
    return $true
}

function Test-APTPRenderedDescription {
    param([Parameter(Mandatory = $true)][string]$InnerHtml, [Parameter(Mandatory = $true)][string]$Candidate)
    $htmlText = [regex]::Replace($InnerHtml, '(?is)<span\s+class="bb_link_host">.*?</span>', ' ')
    $htmlText = [regex]::Replace($htmlText, '(?is)<[^>]*>', ' ')
    $bbText = [regex]::Replace($Candidate, '(?i)\[(?:/?(?:h1|h2|b|list)|\*|url=[^\]]+|/url)\]', ' ')
    $htmlText = [regex]::Replace([Net.WebUtility]::HtmlDecode($htmlText), '\s+', ' ').Trim()
    $bbText = [regex]::Replace([Net.WebUtility]::HtmlDecode($bbText), '\s+', ' ').Trim()
    if ($htmlText -cne $bbText) { throw 'The visible description text does not match.' }
    $expectedLinks = @([regex]::Matches($Candidate, '(?i)\[url=([^\]]+)\]') | ForEach-Object { $_.Groups[1].Value })
    $actualLinks = @([regex]::Matches($InnerHtml, '(?i)<a\b[^>]*class="bb_link"[^>]*href="([^"]+)"') | ForEach-Object {
        $url = [Net.WebUtility]::HtmlDecode($_.Groups[1].Value)
        $filter = 'https://steamcommunity.com/linkfilter/?u='
        if ($url.StartsWith($filter, [StringComparison]::OrdinalIgnoreCase)) { [Net.WebUtility]::UrlDecode($url.Substring($filter.Length)) } else { $url }
    })
    if ($expectedLinks.Count -ne $actualLinks.Count) { throw 'The number of rendered links differs.' }
    for ($i = 0; $i -lt $expectedLinks.Count; $i++) {
        if ($expectedLinks[$i] -cne $actualLinks[$i]) { throw 'A rendered link differs from the reviewed destination.' }
    }
    foreach ($pair in @(
        @('(?i)\[h1\]', '(?i)<div\s+class="bb_h1"'),
        @('(?i)\[h2\]', '(?i)<div\s+class="bb_h2"'),
        @('(?i)\[list\]', '(?i)<ul\s+class="bb_ul"'),
        @('\[\*\]', '(?i)<li\b'),
        @('(?i)\[b\]', '(?i)<b\s*>')
    )) {
        if ([regex]::Matches($Candidate, [string]$pair[0]).Count -ne [regex]::Matches($InnerHtml, [string]$pair[1]).Count) {
            throw 'The visible description formatting differs.'
        }
    }
    return $true
}

function Test-APTPOwnerIssue {
    param([Parameter(Mandatory = $true)]$Issue,
          [Parameter(Mandatory = $true)][AllowEmptyCollection()][object[]]$Events,
          [Parameter(Mandatory = $true)][string]$RequestedIssueNumber)
    $body = @(
        'Update the Automated Public Transport Planning release description only.'
        'Historical source SHA-256: 8719E1A372DD50B137121C632CC75CFFF5890496F8C59E934B6EC0B99BCE370E'
        'Reviewed candidate SHA-256: 75489BBBA894C8CD8FC2337BBF4E6615C2D41003D8524EA95C7D1CF6762943EA'
        'Observed description HTML SHA-256: 7982E44A961BFE4A6C34CBC52F81E04D53342E0D887FEDCDD2F1E6EB2E850E2E'
        'Visible content was compared to the historical source; the raw BBCode could not be verified.'
        'No content folder, title, image, tags or visibility changes.'
        'Signature: OpenAI-Codex.'
    ) -join "`n"
    if ($RequestedIssueNumber -notmatch '^[1-9]\d{0,9}$' -or [string]$Issue.number -cne $RequestedIssueNumber -or
        $null -ne $Issue.PSObject.Properties['pull_request'] -or
        [string]$Issue.title -cne '[Workshop metadata] APTP 3823739383' -or
        [string]$Issue.state -cne 'open' -or [string]$Issue.user.login -ine 'KingsleyHK0323' -or
        ([string]$Issue.body).Replace("`r`n", "`n").Trim() -cne $body) {
        throw 'The issue does not match the reviewed owner request.'
    }
    $label = 'steam-workshop-aptp-description-approved'
    if (@($Issue.labels | Where-Object { [string]$_.name -ceq $label }).Count -ne 1) { throw 'The APTP approval label is not present exactly once.' }
    $Events = @($Events | ForEach-Object { $_ })
    if ($Events.Count -eq 0 -or $Events.Count -ge 100) { throw 'Approval history is missing or may be incomplete.' }
    $last = $null
    foreach ($entry in $Events) {
        if ($null -eq $entry.PSObject.Properties['label'] -or $null -eq $entry.label) { continue }
        if ([string]$entry.label.name -ceq $label -and [string]$entry.event -in @('labeled', 'unlabeled')) { $last = $entry }
    }
    if ($null -eq $last -or [string]$last.event -cne 'labeled' -or [string]$last.actor.login -ine 'KingsleyHK0323' -or
        ([DateTimeOffset]$Issue.created_at).ToUniversalTime() -gt ([DateTimeOffset]$last.created_at).ToUniversalTime() -or
        ([DateTimeOffset]$Issue.updated_at).ToUniversalTime() -gt ([DateTimeOffset]$last.created_at).ToUniversalTime()) {
        throw 'The most recent owner label does not approve the current issue snapshot.'
    }
    return $true
}

Export-ModuleMember -Function Get-APTPTextHash, Get-APTPCandidateFromNote, New-APTPDescriptionPlan, Get-APTPDescriptionInnerHtml, Test-APTPRenderedDescription, Test-APTPOwnerIssue
Export-ModuleMember -Function Test-APTPFinalUri
