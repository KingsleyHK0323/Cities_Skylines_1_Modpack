# Fresh-process, x86 .NET Framework fixture experiment. No game assembly is referenced or loaded.
# The only Harmony input is the fixed, approved DLL, opened read-only and not copied.
# The caller must bind the authorized DLL path and lock/hash this script before execution.
# Signature: OpenAI-Codex
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$HarmonyDll,
    [Parameter(Mandatory=$true)][string]$OutputRoot
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..\..'))
$module = Join-Path $workspace 'Realistic_City_Time'
$fixture = Join-Path $module 'Diagnostics\CalendarInterception\HarmonyFixture'
if (-not [String]::Equals([IO.Path]::GetFullPath($PSScriptRoot), [IO.Path]::GetFullPath($fixture), [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The launcher must reside in the authorized fixture directory.'
}
$dll = [IO.Path]::GetFullPath($HarmonyDll)
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
$csc = Join-Path $framework 'csc.exe'
$validation = [IO.Path]::GetFullPath($OutputRoot)
$modulePath = [IO.Path]::GetFullPath($module)
if (-not $validation.StartsWith($modulePath + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'The output root must be inside the module.' }
$root = [IO.Path]::GetFullPath($workspace)
$named = [IO.Path]::GetFullPath($dll)
if (-not $named.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The named Harmony file is outside the workspace.'
}
$current = $named
while ($true) {
    $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Reparse point in the named Harmony path.' }
    if ([String]::Equals($current, $root, [StringComparison]::OrdinalIgnoreCase)) { break }
    $current = [IO.Path]::GetDirectoryName($current)
    if ([String]::IsNullOrEmpty($current) -or $current.Length -lt $root.Length) { throw 'Harmony ancestry escaped the workspace.' }
}
$currentOutput = $validation
while ($true) {
    $outputItem = Get-Item -LiteralPath $currentOutput -Force -ErrorAction Stop
    if (($outputItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Reparse point in the output ancestry.' }
    if ([String]::Equals($currentOutput, $modulePath, [StringComparison]::OrdinalIgnoreCase)) { break }
    $parentOutput = [IO.Path]::GetDirectoryName($currentOutput)
    if ([String]::IsNullOrEmpty($parentOutput) -or $parentOutput.Length -lt $modulePath.Length) { throw 'Output ancestry left the module.' }
    $currentOutput = $parentOutput
}
foreach ($directory in @($module, $validation)) {
    $item = Get-Item -LiteralPath $directory -Force -ErrorAction Stop
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Reparse point in the output path.' }
}
$runDir = Join-Path $validation ('harmony-calendar-x86-' + [Guid]::NewGuid().ToString('N'))
if (Test-Path -LiteralPath $runDir) { throw 'Generated output directory already exists.' }
[void][IO.Directory]::CreateDirectory($runDir)
$rawPath = Join-Path $runDir 'raw-evidence.txt'
$rawStream = [IO.File]::Open($rawPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::Read)
$writer = New-Object IO.StreamWriter($rawStream, (New-Object Text.UTF8Encoding($false)))
$writer.AutoFlush = $true
function Record([string] $line) { $writer.WriteLine($line); Write-Output $line }
Record 'Signature: OpenAI-Codex'
$inputs = @(
    [pscustomobject]@{ Name='Harmony DLL'; Path=$dll; Hash='C228FF02A360A48C03A9154F49282DCCDB5809599D5E606CF3840CB4887A5503' },
    [pscustomobject]@{ Name='CalendarIlMatcher.cs'; Path=(Join-Path (Split-Path -Parent $fixture) 'CalendarIlMatcher.cs'); Hash='3D3EAC27FD75F21EC3BD63203E3191BCC64B7EECE665655FA4F28C5183357CAB' },
    [pscustomobject]@{ Name='FakeSimulation.cs'; Path=(Join-Path $fixture 'FakeSimulation.cs'); Hash='018C66B5374EC376F29CB1213BEE1734717ADAC32E3C544AACE5722A6195550D' },
    [pscustomobject]@{ Name='HarmonyFixtureChecks.cs'; Path=(Join-Path $fixture 'HarmonyFixtureChecks.cs'); Hash='F81C4366878FA5679A77489275B5FCF230421497FDCB54B0D1C2D3ADA149A4BE' },
    [pscustomobject]@{ Name='FrameworkFixtureHost.cs'; Path=(Join-Path $fixture 'FrameworkFixtureHost.cs'); Hash='B0EC5A3AC46929AB1FB3D3F01F7AEC7AE23A235E015144359C588796B48950D9' },
    [pscustomobject]@{ Name='csc.exe'; Path=$csc; Hash='012E8CD8ADFF0C439A90FFD22C0E33EFEB6FBC8CF660D52B4858D310649382FA' },
    [pscustomobject]@{ Name='mscorlib.dll'; Path=(Join-Path $framework 'mscorlib.dll'); Hash='1A90940C8FDD4A4EA9615891FE593EB1BAFBD3A57FC27EEFB65757F6B7E2750A' },
    [pscustomobject]@{ Name='System.dll'; Path=(Join-Path $framework 'System.dll'); Hash='2B3C17C6208A0B4B6BEB94E1A066F99BA06CDB2EA919479E99D47E8C6D96DC71' },
    [pscustomobject]@{ Name='System.Core.dll'; Path=(Join-Path $framework 'System.Core.dll'); Hash='8B898BED54AE8CC61F9C58CC3EB062724F8357D2BDDA338B5D363654ECFF333D' }
)
# The host now embeds source attribution in its generated executable.
$inputs[4].Hash = '5D9207ED367F94970685B04CC181EE44CF28084861C590FD1AF7B07FE9558E89'
$streams = New-Object 'System.Collections.Generic.List[System.IO.FileStream]'
$exeStream = $null
$success = $false
$env:TEMP = $runDir
$env:TMP = $runDir
$env:HARMONY_NO_LOG = '1'
try {
    if (-not [String]::IsNullOrEmpty([Environment]::GetEnvironmentVariable('HARMONY_DEBUG'))) { throw 'Harmony debug environment is set.' }
    Record ('OUTPUT_DIR=' + $runDir)
    Record ('START=' + (Get-Date -Format o))
    Record 'Signature: OpenAI-Codex'
    Record 'HANDLE_FINAL_PATH=UNVERIFIED; literal-path ancestry was checked for reparse points'
    foreach ($entry in $inputs) {
        $stream = [IO.File]::Open($entry.Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        $streams.Add($stream)
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') } finally { $sha.Dispose() }
        if ($hash -ne $entry.Hash) { throw ('Input hash mismatch: ' + $entry.Name) }
        $stream.Position = 0
        Record ('INPUT_SHA256 ' + $entry.Name + ' ' + $hash)
    }
    if ($streams[0].Length -ne 2575872) { throw 'Harmony DLL size mismatch.' }
    $exe = Join-Path $runDir 'ClockHarmonyFixture.exe'
    if ((Test-Path -LiteralPath $exe) -or (Test-Path -LiteralPath (Join-Path $runDir '0Harmony.dll')) -or
        (Test-Path -LiteralPath ($exe + '.config'))) { throw 'Unexpected executable or sidecar exists.' }
    $argsForCsc = @('/nologo', '/noconfig', '/nostdlib+', '/target:exe', '/platform:x86', '/optimize+', '/langversion:5',
        '/main:FrameworkFixtureHost', ('/out:' + $exe), ('/reference:' + $inputs[6].Path),
        ('/reference:' + $inputs[7].Path), ('/reference:' + $inputs[8].Path), ('/reference:' + $dll),
        $inputs[1].Path, $inputs[2].Path, $inputs[3].Path, $inputs[4].Path)
    Record 'CSC_ARGUMENTS_BEGIN'
    foreach ($arg in $argsForCsc) { Record $arg }
    Record 'CSC_ARGUMENTS_END'
    Record 'CSC_OUTPUT_BEGIN'
    $compileLines = @(& $csc @argsForCsc 2>&1 | ForEach-Object { $_.ToString() })
    $compileExit = $LASTEXITCODE
    foreach ($line in $compileLines) { Record $line }
    Record 'CSC_OUTPUT_END'
    Record ('CSC_EXIT=' + $compileExit)
    if ($compileExit -ne 0 -or -not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Fixture compilation failed; no test process started.' }
    if ((Test-Path -LiteralPath (Join-Path $runDir '0Harmony.dll')) -or (Test-Path -LiteralPath ($exe + '.config'))) {
        throw 'An unapproved sidecar appeared in the fixture output directory.'
    }
    $exeStream = [IO.File]::Open($exe, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $exeHash = [BitConverter]::ToString($sha.ComputeHash($exeStream)).Replace('-', '') } finally { $sha.Dispose() }
    $exeStream.Position = 0
    Add-Type -AssemblyName System.Reflection.Metadata
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($exeStream, [System.Reflection.PortableExecutable.PEStreamOptions]::LeaveOpen)
    try {
        if ($pe.PEHeaders.CoffHeader.Machine -ne [System.Reflection.PortableExecutable.Machine]::I386 -or
            ($pe.PEHeaders.CorHeader.Flags -band [System.Reflection.PortableExecutable.CorFlags]::Requires32Bit) -eq 0) {
            throw 'Fixture executable is not marked x86.'
        }
        $reader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        $allowed = @('mscorlib', 'System', 'System.Core', '0Harmony')
        $names = New-Object 'System.Collections.Generic.List[string]'
        foreach ($handle in $reader.AssemblyReferences) {
            $reference = $reader.GetAssemblyReference($handle)
            $name = $reader.GetString($reference.Name)
            if ($name -notin $allowed) { throw ('Unexpected executable reference: ' + $name) }
            $names.Add($name)
        }
        if (-not $names.Contains('0Harmony')) { throw 'Expected Harmony reference missing from fixture executable.' }
        Record ('EXE_REFERENCES=' + ($names -join ','))
    } finally { $pe.Dispose() }
    Record ('EXE_SHA256=' + $exeHash)
    Record 'RUN_OUTPUT_BEGIN'
    $runLines = @(& $exe $dll 2>&1 | ForEach-Object { $_.ToString() })
    $runExit = $LASTEXITCODE
    foreach ($line in $runLines) { Record $line }
    Record 'RUN_OUTPUT_END'
    Record ('RUN_EXIT=' + $runExit)
    $joined = $runLines -join [Environment]::NewLine
    if ($runExit -ne 0 -or $joined -notmatch '(?m)^TOTAL: 19\s*$' -or
        -not $joined.Contains('FIXTURE_PATCH_BEGIN') -or -not $joined.Contains('FIXTURE_PATCH_RETURNED') -or
        -not $joined.Contains('FIXTURE_SCOPED_UNPATCH_BEGIN') -or -not $joined.Contains('FIXTURE_SCOPED_UNPATCH_RETURNED') -or
        -not $joined.Contains('PASS: final SHA-256 from the same locked handle') -or
        -not $joined.Contains('PASS: final no named game assemblies')) {
        throw 'Fixture test failed or did not produce all required evidence; do not automatically retry.'
    }
    $exeStream.Position = 0
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $afterExe = [BitConverter]::ToString($sha.ComputeHash($exeStream)).Replace('-', '') } finally { $sha.Dispose() }
    if ($afterExe -ne $exeHash) { throw 'Fixture executable changed during execution.' }
    Record 'EXE_POST_SHA256=PASS'
    $success = $true
}
catch {
    Record ('ERROR=' + $_.Exception.GetType().FullName + ': ' + $_.Exception.Message)
}
finally {
    if ($null -ne $exeStream) { $exeStream.Dispose() }
    for ($i=0; $i -lt $streams.Count; $i++) {
        $stream = $streams[$i]
        try {
            $stream.Position = 0
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-', '') } finally { $sha.Dispose() }
            if ($hash -ne $inputs[$i].Hash) { $success = $false; Record ('POST_HASH_MISMATCH=' + $inputs[$i].Name) }
            else { Record ('POST_HASH_PASS=' + $inputs[$i].Name) }
        } catch { $success = $false; Record ('POST_HASH_ERROR=' + $inputs[$i].Name + ': ' + $_.Exception.Message) }
        finally { $stream.Dispose() }
    }
    Record ('STATUS=' + $(if ($success) { 'PASS' } else { 'FAILED_OR_INCONCLUSIVE' }))
    Record ('END=' + (Get-Date -Format o))
    $writer.Dispose()
}
if (-not $success) { exit 1 }
