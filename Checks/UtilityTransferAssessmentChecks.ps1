# Signature: OpenAI-Codex_Wireless_Utilities_And_Global_Coverage
# Synthetic inputs only. Compiles the actual isolated production model in memory.
param([switch]$ProbeOmittedOfferGuard)

$ErrorActionPreference = 'Stop'
$modelPath = Join-Path $PSScriptRoot '..\Source\UtilityTransferAssessment.cs'
if (-not (Test-Path -LiteralPath $modelPath -PathType Leaf)) {
    throw 'FAIL: a known electricity offer of 100, demand of 70 and acceptance of 40 requires the missing production assessment.'
}
$modelSource = Get-Content -LiteralPath $modelPath -Raw
if ($ProbeOmittedOfferGuard) {
    # Mutate only the in-memory source to demonstrate the over-offer assertion fails.
    $original = 'if (delivery > offer)'
    if (([regex]::Matches($modelSource, [regex]::Escape($original))).Count -ne 1) {
        throw 'FAIL: expected one production offer guard for the synthetic mutation.'
    }
    $modelSource = $modelSource.Replace($original, 'if (delivery < 0 && delivery > offer)')
}
Add-Type -TypeDefinition $modelSource -CompilerOptions '/langversion:3' -ErrorAction Stop

$resourceType = [WirelessUtilitiesAndGlobalCoverage.UtilityResource]
$quantityType = [WirelessUtilitiesAndGlobalCoverage.UtilityQuantity]
$assessmentType = [WirelessUtilitiesAndGlobalCoverage.UtilityTransferAssessment]
$session = [Guid]'aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb'
$otherSession = [Guid]'aaaaaaaa-1111-2222-3333-cccccccccccc'
$count = 0

function Resource([string]$name) {
    return [Enum]::Parse($resourceType, $name)
}

function Known($kind, [Guid]$city, [long]$epoch, [AllowNull()][string]$unit, [long]$value) {
    return $quantityType::Known($kind, $city, $epoch, $unit, $value)
}

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw ('FAIL: ' + $message) }
    $script:count++
}

function AssertStatus($result, [string]$expected, [string]$case) {
    Assert ($result.Status.ToString() -ceq $expected) ($case + ': expected ' + $expected + ', got ' + $result.Status)
    if ($expected -cne 'Consistent') {
        Assert ($null -eq $result.OfferRemaining -and $null -eq $result.UnmetDemand) ($case + ': rejected input produced a numeric result')
    }
}

$electricity = Resource 'Electricity'
$water = Resource 'Water'
$sewage = Resource 'Sewage'
$heating = Resource 'Heating'

foreach ($kind in @($electricity, $water, $sewage, $heating)) {
    $result = $assessmentType::Assess((Known $kind $session 7 'native-units' 100), (Known $kind $session 7 'native-units' 70), (Known $kind $session 7 'native-units' 40))
    AssertStatus $result 'Consistent' ('partial transfer for ' + $kind)
    Assert ($result.OfferRemaining -eq 60 -and $result.UnmetDemand -eq 30) ('partial amounts for ' + $kind)
}

$zero = Known $water $session 8 'water-unit' 0
$result = $assessmentType::Assess($zero, $zero, $zero)
AssertStatus $result 'Consistent' 'known zero'
Assert ($result.OfferRemaining -eq 0 -and $result.UnmetDemand -eq 0) 'known zero must not become unknown'

$max = Known $sewage $session 9 'sewage-unit' ([long]::MaxValue)
$result = $assessmentType::Assess($max, $max, $max)
AssertStatus $result 'Consistent' 'maximum signed quantity'
Assert ($result.OfferRemaining -eq 0 -and $result.UnmetDemand -eq 0) 'maximum values must not overflow'

$offer = Known $water $session 10 'water-unit' 100
$demand = Known $water $session 10 'water-unit' 70
$accepted = Known $water $session 10 'water-unit' 40
$unknown = $quantityType::Unknown($water, $session, 10)
$unsupported = $quantityType::Unsupported($water, $session, 10)
AssertStatus ($assessmentType::Assess($unknown, $demand, $accepted)) 'Unobserved' 'unknown offer'
AssertStatus ($assessmentType::Assess($offer, $unsupported, $accepted)) 'Unsupported' 'unsupported demand'
AssertStatus ($assessmentType::Assess($null, $demand, $accepted)) 'Unobserved' 'missing observation'
AssertStatus ($assessmentType::Assess($offer, (Known $sewage $session 10 'water-unit' 70), $accepted)) 'ContextMismatch' 'water and sewage cannot be mixed'
AssertStatus ($assessmentType::Assess($offer, (Known $water $otherSession 10 'water-unit' 70), $accepted)) 'ContextMismatch' 'different city sessions'
AssertStatus ($assessmentType::Assess($offer, (Known $water $session 11 'water-unit' 70), $accepted)) 'ContextMismatch' 'different observation epochs'
AssertStatus ($assessmentType::Assess((Known ([Enum]::ToObject($resourceType, 99)) $session 10 'water-unit' 100), $demand, $accepted)) 'InvalidContext' 'unknown resource enum'
AssertStatus ($assessmentType::Assess((Known $water ([Guid]::Empty) 10 'water-unit' 100), $demand, $accepted)) 'InvalidContext' 'empty session'
AssertStatus ($assessmentType::Assess((Known $water $session -1 'water-unit' 100), $demand, $accepted)) 'InvalidContext' 'negative epoch'

foreach ($position in @('offer', 'demand', 'accepted')) {
    $bad = Known $water $session 10 'water-unit' -1
    $a = $offer; $b = $demand; $c = $accepted
    switch ($position) { 'offer' { $a = $bad }; 'demand' { $b = $bad }; 'accepted' { $c = $bad } }
    AssertStatus ($assessmentType::Assess($a, $b, $c)) 'NegativeQuantity' ('negative ' + $position)
}

AssertStatus ($assessmentType::Assess((Known $water $session 10 ' ' 100), $demand, $accepted)) 'InvalidUnit' 'blank unit'
AssertStatus ($assessmentType::Assess((Known $water $session 10 ' water-unit' 100), $demand, $accepted)) 'InvalidUnit' 'unit with surrounding whitespace'
AssertStatus ($assessmentType::Assess($offer, (Known $water $session 10 'Water-unit' 70), $accepted)) 'UnitMismatch' 'unit identity is case sensitive'
AssertStatus ($assessmentType::Assess($offer, (Known $water $session 10 'water-unit' 200), (Known $water $session 10 'water-unit' 101))) 'ExceedsOffer' 'over-offer even when demand is larger'
AssertStatus ($assessmentType::Assess($offer, $demand, (Known $water $session 10 'water-unit' 101))) 'ExceedsOffer' 'accepted more than offered'
AssertStatus ($assessmentType::Assess($offer, $demand, (Known $water $session 10 'water-unit' 71))) 'ExceedsDemand' 'accepted more than requested'

$first = $assessmentType::Assess($offer, $demand, $accepted)
$second = $assessmentType::Assess($offer, $demand, $accepted)
Assert ($first.Status.ToString() -ceq 'Consistent' -and $second.OfferRemaining -eq 60) 'the pure assessment must not mutate input state'
Assert ($offer.Amount -eq 100 -and $demand.Amount -eq 70 -and $accepted.Amount -eq 40) 'input quantities must remain unchanged'

Write-Output ('PASS: ' + $count + ' synthetic assertions. This is not a native/gameplay test or a transaction ledger.')
