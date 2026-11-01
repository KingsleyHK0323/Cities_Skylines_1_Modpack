# Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
$ErrorActionPreference = 'Stop'
# Signature: OpenAI-Codex_Automated_Public_Transport_Planning
foreach ($key in @('GITHUB_SHA','GITHUB_RUN_ID','GITHUB_RUN_ATTEMPT','GITHUB_REPOSITORY','GITHUB_REF')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key))) { throw "Missing build identity: $key" }
}
$out = Join-Path $env:RUNNER_TEMP 'aptp-release-assets'
New-Item -ItemType Directory -Path $out -ErrorAction Stop | Out-Null
Copy-Item -LiteralPath 'bin/Release/AutomatedPublicTransportPlanning.dll' -Destination $out
Copy-Item -LiteralPath '.github/scripts/Development-README.md' -Destination (Join-Path $out 'README.md')
Copy-Item -LiteralPath 'LICENSE' -Destination $out
if ((Get-FileHash LICENSE).Hash -ne (Get-FileHash (Join-Path $out 'LICENSE')).Hash) { throw 'License mismatch' }
@("sourceSHA=$env:GITHUB_SHA", 'sharedrefsSHA=7b8a5e7c10f2db84388e8dd8af2084f239957b13', "runid=$env:GITHUB_RUN_ID", "runattempt=$env:GITHUB_RUN_ATTEMPT", 'DevTools=true', 'Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning') | Set-Content (Join-Path $out 'build-info.txt') -Encoding utf8
@("repository=$env:GITHUB_REPOSITORY", "ref=$env:GITHUB_REF", 'Signature: OpenAI-Codex_Automated_Public_Transport_Planning') | Add-Content -LiteralPath (Join-Path $out 'build-info.txt') -Encoding utf8
@('Stable Workshop links recorded in source; not a live Steam status check.', 'AI Improve: https://steamcommunity.com/sharedfiles/filedetails/?id=3782858610', 'APTP: https://steamcommunity.com/sharedfiles/filedetails/?id=3823739383', 'AI Gaming: https://steamcommunity.com/sharedfiles/filedetails/?id=3823740106', 'Harmony (for mods requiring it): https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402', 'Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning') | Set-Content (Join-Path $out 'Steam_Workshop_Link.txt') -Encoding utf8
$allowed = @('AutomatedPublicTransportPlanning.dll','README.md','LICENSE','build-info.txt','Steam_Workshop_Link.txt')
$lines = foreach ($name in $allowed) {
    $f = Get-Item -LiteralPath (Join-Path $out $name)
    if ($f.Length -le 0) { throw "Empty file: $name" }
    "$((Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant())  $name"
}
$lines | Set-Content (Join-Path $out 'SHA256SUMS') -Encoding ascii
if (@(Get-ChildItem -LiteralPath $out -File).Count -ne 6) { throw 'Unexpected asset count' }
