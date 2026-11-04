# Signature: OpenAI-Codex.
# Reuses the reviewed APTP Release project build on a disposable GitHub-hosted runner.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourcePath,
    [Parameter(Mandatory)][string]$SourceSha,
    [Parameter(Mandatory)][string]$GameRoot,
    [Parameter(Mandatory)][string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -cne 'true' -or $env:RUNNER_ENVIRONMENT -cne 'github-hosted') {
    throw 'APTP release compilation requires a disposable GitHub-hosted runner.'
}
$reviewedSha = 'f2c97c858bcae95f99c7bc1b634b7178a07f3f97'
$registry = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviewed-recipes.json') -Raw | ConvertFrom-Json
$reviewedCandidates = @($registry.recipes | Where-Object { $_.module -ceq 'Automated_Public_Transport_Planning' })
if ($registry.schema -ne 1 -or $registry.signature -cne 'OpenAI-Codex_workshop_implementation' -or
    $reviewedCandidates.Count -ne 1 -or $reviewedCandidates[0].sourceSha -cne $reviewedSha -or
    $reviewedCandidates[0].assembly -cne 'AutomatedPublicTransportPlanning' -or
    $reviewedCandidates[0].builder -cne 'msbuild' -or $SourceSha -cne $reviewedSha) {
    throw 'APTP source does not match the reviewed MSBuild recipe.'
}
$source = [IO.Path]::GetFullPath($SourcePath)
$temp = [IO.Path]::GetFullPath($env:RUNNER_TEMP).TrimEnd([IO.Path]::DirectorySeparatorChar)
$workspace = [IO.Path]::GetFullPath($env:GITHUB_WORKSPACE).TrimEnd([IO.Path]::DirectorySeparatorChar)
$game = [IO.Path]::GetFullPath($GameRoot)
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (-not $source.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    -not $game.StartsWith($temp + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    -not $output.StartsWith($temp + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'APTP source, references and output must be within their expected runner directories.'
}
if ((Test-Path -LiteralPath $output) -or (Test-Path -LiteralPath (Join-Path $source 'Local.props'))) {
    throw 'APTP output or local project overrides already exist.'
}
if (-not (Test-Path -LiteralPath (Join-Path $game 'Cities_Data/Managed/ICities.dll') -PathType Leaf)) {
    throw 'Verified game references are missing.'
}
$actualSha = (& git -C $source rev-parse --verify 'HEAD^{commit}').Trim()
if ($LASTEXITCODE -ne 0 -or $actualSha -cne $reviewedSha) { throw 'APTP source differs from the reviewed SHA.' }
$dirty = & git -C $source status --porcelain=v1 --untracked-files=all
if ($LASTEXITCODE -ne 0 -or -not [string]::IsNullOrWhiteSpace(($dirty -join "`n"))) {
    throw 'The APTP source checkout is not clean.'
}
$projectPath = Join-Path $source 'AutomatedPublicTransportPlanning.csproj'
[xml]$project = Get-Content -LiteralPath $projectPath -Raw -Encoding UTF8
$namespaces = [System.Xml.XmlNamespaceManager]::new($project.NameTable)
$namespaces.AddNamespace('m', 'http://schemas.microsoft.com/developer/msbuild/2003')
$projectSources = @($project.SelectNodes('/m:Project/m:ItemGroup/m:Compile[not(@Condition)]', $namespaces) |
    ForEach-Object { $_.GetAttribute('Include').Replace('\', '/') })
$projectReferences = @($project.SelectNodes('/m:Project/m:ItemGroup/m:Reference', $namespaces) |
    ForEach-Object { ($_.GetAttribute('Include') -split ',')[0].Trim() + '.dll' })
$conditionalSources = @($project.SelectNodes('/m:Project/m:ItemGroup/m:Compile[@Condition]', $namespaces))
$conditionalNames = @($conditionalSources | ForEach-Object { $_.GetAttribute('Include').Replace('\', '/') })
$expectedDevelopmentSources = @(
    'Source/Planning/RouteCreation.cs', 'Source/Planning/RouteEditPlan.cs',
    'Source/Planning/RouteStreetStopRules.cs', 'Source/Planning/RouteManualInput.cs',
    'Source/Planning/RouteEditing.cs', 'Source/Planning/RouteEditExecution.cs',
    'Source/RouteEditingUi.cs', 'Source/Spike/PerformanceProbe.cs', 'Source/Spike/SaveRoundTripSpike.cs'
)
$unexpectedConditions = @($conditionalSources | Where-Object {
    ($_.GetAttribute('Condition') -replace '\s+', '') -cne "'`$(DevTools)'=='true'"
})
$conditionalGroups = @($project.SelectNodes('/m:Project/m:ItemGroup[@Condition][m:Compile or m:Reference]', $namespaces))
$conditionalReferences = @($project.SelectNodes('/m:Project/m:ItemGroup/m:Reference[@Condition]', $namespaces))
$unsupportedStructures = @($project.SelectNodes('//m:Choose | //m:ImportGroup', $namespaces))
$codePages = @($project.SelectNodes('/m:Project/m:PropertyGroup/m:CodePage', $namespaces))
$languages = @($project.SelectNodes('/m:Project/m:PropertyGroup/m:LangVersion', $namespaces))
if ($projectSources.Count -ne @($reviewedCandidates[0].sources).Count -or
    $projectReferences.Count -ne @($reviewedCandidates[0].references).Count -or
    $conditionalNames.Count -ne $expectedDevelopmentSources.Count -or
    $unexpectedConditions.Count -ne 0 -or $conditionalGroups.Count -ne 0 -or $conditionalReferences.Count -ne 0 -or
    $unsupportedStructures.Count -ne 0 -or
    @(Compare-Object $expectedDevelopmentSources $conditionalNames -CaseSensitive).Count -ne 0 -or
    @(Compare-Object @($reviewedCandidates[0].sources) $projectSources -CaseSensitive).Count -ne 0 -or
    @(Compare-Object @($reviewedCandidates[0].references) $projectReferences -CaseSensitive).Count -ne 0 -or
    $codePages.Count -ne 1 -or $codePages[0].InnerText -cne '65001' -or
    $languages.Count -ne 1 -or $languages[0].InnerText -cne '5' -or $reviewedCandidates[0].languageVersion -cne '5') {
    throw 'APTP project inputs, UTF-8 encoding or language version differ from the reviewed recipe.'
}
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) { throw 'Visual Studio locator is unavailable.' }
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild/**/Bin/MSBuild.exe' | Select-Object -First 1
if ($LASTEXITCODE -ne 0 -or -not $msbuild -or -not (Test-Path -LiteralPath $msbuild -PathType Leaf)) {
    throw 'Visual Studio MSBuild is unavailable.'
}
$buildOutput = Join-Path $temp ('aptp-workshop-release-' + [Guid]::NewGuid().ToString('N'))
$intermediate = Join-Path $temp ('aptp-workshop-intermediate-' + [Guid]::NewGuid().ToString('N'))
Push-Location $source
try {
    & node --test Tests/ReleasePreviewBoundary.test.mjs
    if ($LASTEXITCODE -ne 0) { throw 'The APTP release preview boundary check failed.' }
    & ./Tests/Run-PlanningPreviewAdmissionSynthetic.ps1
    & $msbuild 'AutomatedPublicTransportPlanning.csproj' /t:Rebuild /p:Configuration=Release /p:DevTools=false /p:DefineConstants= /p:ModsDir= `
        "/p:OutputPath=$buildOutput/" "/p:IntermediateOutputPath=$intermediate/" `
        "/p:CitiesSkylinesDir=$game" "/p:CSInstallPath=$game" /verbosity:normal /nologo
    if ($LASTEXITCODE -ne 0) { throw 'APTP cloud release compilation failed.' }
} finally {
    Pop-Location
}
$dll = Join-Path $buildOutput 'AutomatedPublicTransportPlanning.dll'
if (-not (Test-Path -LiteralPath $dll -PathType Leaf) -or (Get-Item -LiteralPath $dll).Length -le 0 -or
    [Reflection.AssemblyName]::GetAssemblyName($dll).Name -cne 'AutomatedPublicTransportPlanning') {
    throw 'The APTP Release DLL is missing or has the wrong managed assembly identity.'
}
$bytes = [IO.File]::ReadAllBytes($dll)
$ascii = [Text.Encoding]::ASCII.GetString($bytes)
$unicode = [Text.Encoding]::Unicode.GetString($bytes)
foreach ($marker in @('RouteCreationController', 'RouteEditingController', 'SaveRoundTripSpike', 'PerformanceProbe', 'OnDiagnosticsButton')) {
    if ($ascii.Contains($marker) -or $unicode.Contains($marker)) {
        throw 'The APTP Release DLL contains a reviewed Development-only marker.'
    }
}
New-Item -ItemType Directory -Path $output -ErrorAction Stop | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $output 'AutomatedPublicTransportPlanning.dll') -ErrorAction Stop
Write-Output 'Built the reviewed APTP Release project without DevTools. Game behavior remains unverified.'
