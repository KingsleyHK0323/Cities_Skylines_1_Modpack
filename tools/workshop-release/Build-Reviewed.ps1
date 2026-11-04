# Signature: OpenAI-Codex_workshop_implementation
# Updated by: OpenAI-Codex.
# Compiles only an exact reviewed commit and explicit sources on a GitHub-hosted runner.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_]{1,100}$')][string]$Module,
    [Parameter(Mandatory)][string]$SourcePath,
    [Parameter(Mandatory)][string]$SourceSha,
    [Parameter(Mandatory)][string]$GameRoot,
    [Parameter(Mandatory)][string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -cne 'true' -or $env:RUNNER_ENVIRONMENT -cne 'github-hosted') {
    throw 'Release compilation requires a disposable GitHub-hosted runner.'
}
$registry = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviewed-recipes.json') -Raw | ConvertFrom-Json
$matches = @($registry.recipes | Where-Object { $_.module -ceq $Module })
if ($registry.schema -ne 1 -or $registry.signature -cne 'OpenAI-Codex_workshop_implementation' -or
    $matches.Count -ne 1 -or $matches[0].sourceSha -cne $SourceSha -or $SourceSha -cnotmatch '^[0-9a-f]{40}$') {
    throw 'The module source does not match a unique reviewed recipe.'
}
$recipe = $matches[0]
if ($recipe.assembly -cnotmatch '^[A-Za-z][A-Za-z0-9]{1,100}$' -or $recipe.languageVersion -cnotin @('3','5','7.3')) {
    throw 'The reviewed assembly name or language version is invalid.'
}
$source = [IO.Path]::GetFullPath($SourcePath)
$temp = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd([IO.Path]::DirectorySeparatorChar)
$workspace = [IO.Path]::GetFullPath($env:GITHUB_WORKSPACE).TrimEnd([IO.Path]::DirectorySeparatorChar)
$game = [IO.Path]::GetFullPath($GameRoot)
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $source.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    -not $game.StartsWith($temp + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    -not $output.StartsWith($temp + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Source, references and output must be within their expected runner directories.'
}
if (Test-Path -LiteralPath $output) { throw 'Release output already exists; refusing to overwrite.' }
$actualSha = (& git -C $source rev-parse --verify 'HEAD^{commit}').Trim()
if ($LASTEXITCODE -ne 0 -or $actualSha -cne $SourceSha) { throw 'Checked-out source differs from the reviewed SHA.' }
$dirty = & git -C $source status --porcelain=v1 --untracked-files=all
if ($LASTEXITCODE -ne 0 -or -not [string]::IsNullOrWhiteSpace(($dirty -join "`n"))) {
    throw 'The source checkout is not clean.'
}
$sdks = @(dotnet --list-sdks)
if ($LASTEXITCODE -ne 0) { throw 'Could not list hosted SDKs.' }
$selected = $sdks | ForEach-Object {
    if ($_ -match '^(8\.0\.[0-9]+) \[(.+)\]$') { [pscustomobject]@{Version=[version]$Matches[1]; Root=$Matches[2]} }
} | Sort-Object Version -Descending | Select-Object -First 1
if (-not $selected) { throw 'No stable .NET 8 SDK is installed.' }
$compiler = Join-Path $selected.Root "$($selected.Version)/Roslyn/bincore/csc.dll"
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) { throw 'The expected compiler is unavailable.' }
$allowedReferences = @('mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll','UnityEngine.dll',
                       'ICities.dll','ColossalManaged.dll','Assembly-CSharp.dll')
$arguments = @('/noconfig','/nostdlib+','/target:library',"/langversion:$($recipe.languageVersion)",'/optimize+','/deterministic+')
if ($recipe.PSObject.Properties.Name -contains 'compilerOptions') {
    foreach ($option in @($recipe.compilerOptions)) {
        if ($option -cnotin @('/warnaserror+','/checked+')) { throw 'A compiler option is not approved.' }
        $arguments += $option
    }
}
$managed = Join-Path $game 'Cities_Data/Managed'
foreach ($name in @($recipe.references)) {
    if ($name -cnotin $allowedReferences) { throw 'A game reference is not approved.' }
    $path = Join-Path $managed $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required reference missing: $name" }
    $arguments += "/reference:$path"
}
foreach ($relative in @($recipe.sources)) {
    if ($relative -cmatch '\.\.' -or $relative -cnotmatch '^(?:Properties/AssemblyInfo\.cs|(?:Source|Game)/[A-Za-z0-9_./-]+\.cs)$') {
        throw 'An unsafe source path was rejected.'
    }
    $path = Join-Path $source $relative
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Reviewed source is missing: $relative" }
    $arguments += $path
}
if (@($recipe.sources).Count -lt 1) { throw 'The source list is empty.' }
New-Item -ItemType Directory -Path $output -ErrorAction Stop | Out-Null
$dll = Join-Path $output "$($recipe.assembly).dll"
$arguments += "/out:$dll"
& dotnet $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Cloud release compilation failed: $Module" }
if (-not (Test-Path -LiteralPath $dll -PathType Leaf) -or (Get-Item -LiteralPath $dll).Length -le 0 -or
    [Reflection.AssemblyName]::GetAssemblyName($dll).Name -cne $recipe.assembly) {
    throw "Compiled assembly identity is invalid: $Module"
}
Write-Output "Built reviewed no-DevTool source for $Module. Game behavior remains unverified."
