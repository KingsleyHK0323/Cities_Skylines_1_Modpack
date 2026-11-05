# Signature: OpenAI-Codex.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
function Assert-True([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Assert-Rejected([scriptblock]$Action, [string]$Message) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (-not $rejected) { throw $Message }
}

$workflow = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) '.github/workflows/workshop-description-rewrite-apply.yml'
if (-not (Test-Path -LiteralPath $workflow -PathType Leaf)) { throw 'RED: the pinned in-memory workflow candidate does not exist.' }
$yaml = [IO.File]::ReadAllText($workflow)
$extract = [regex]::Match($yaml, '(?ms)^          # BEGIN OFFLINE SOURCE VALIDATION\r?\n(.*?)^          # END OFFLINE SOURCE VALIDATION\s*$')
Assert-True $extract.Success 'The exact workflow validation function could not be extracted.'
$functionCode = [regex]::Replace($extract.Groups[1].Value, '(?m)^          ', '')
. ([scriptblock]::Create($functionCode))

$expectedPath = 'tools/steam-workshop/test-fixture.ps1'
$helloHash = '5891b5b522d5df086d0ff0b110fbd9d21bb4fc7163af34d08286a2e846f6be03'
$response = [pscustomobject]@{ type='file'; path=$expectedPath; encoding='base64'; size=6; sha='ce013625030ba8dba906f756967f9e9ca394464a'; content='aGVsbG8K' }
Assert-True ((Convert-RewriteSourceContent -Response $response -ExpectedPath $expectedPath -ExpectedSha256 $helloHash) -ceq "hello`n") 'The valid source fixture was rejected.'
Assert-Rejected { Convert-RewriteSourceContent -Response $response -ExpectedPath 'wrong' -ExpectedSha256 $helloHash } 'A wrong source path was accepted.'
Assert-Rejected { Convert-RewriteSourceContent -Response $response -ExpectedPath $expectedPath -ExpectedSha256 ('0' * 64) } 'An unapproved SHA-256 was accepted.'
$response.content = 'd29ybGQK'
$response.sha = 'cc628ccd10742baea8241c5924df992b5c019f71'
Assert-Rejected { Convert-RewriteSourceContent -Response $response -ExpectedPath $expectedPath -ExpectedSha256 $helloHash } 'Tampered source with a matching Git SHA-1 was accepted.'
$response.content = '/w=='
$response.size = 1
$response.sha = 'ce542efaa5124a0437f0c4db329d7ec4b7ba70a7'
Assert-Rejected { Convert-RewriteSourceContent -Response $response -ExpectedPath $expectedPath -ExpectedSha256 'a8100ae6aa1940d0b663bb31cd466142ebbdbd5187131b92d93818987832eb89' } 'Invalid UTF-8 was accepted.'

$planSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'DescriptionRewritePlan.psm1'))
$metadataSource = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'MetadataOnly.psm1'))
$planModule = New-Module -Name 'RewritePlanOfflineTest' -ScriptBlock ([scriptblock]::Create($planSource))
$metadataModule = New-Module -Name 'RewriteMetadataOfflineTest' -ScriptBlock ([scriptblock]::Create($metadataSource))
Import-Module $planModule -Force
Import-Module $metadataModule -Force
Assert-True ([bool](Get-Command New-RewriteDescriptionPlan -ErrorAction SilentlyContinue)) 'The in-memory plan module did not export its command.'
Assert-True ([bool](Get-Command Get-SteamCmdEnvironment -ErrorAction SilentlyContinue)) 'The in-memory metadata module did not export its command.'
$controller = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Invoke-DescriptionRewriteMemory.ps1'))
Assert-True (-not $controller.Contains('$PSScriptRoot')) 'The memory controller depends on a checkout path.'
$tokens = $null; $parseErrors = $null
[void][Management.Automation.Language.Parser]::ParseInput($controller, [ref]$tokens, [ref]$parseErrors)
Assert-True ($parseErrors.Count -eq 0) 'The memory controller does not parse.'
Write-Output 'PASS: pinned-source tamper checks and in-memory module loading; no network or upload.'

$runMatch = [regex]::Match($yaml, '(?ms)^        run: \|\r?\n((?:          .*\r?\n?)*)')
Assert-True $runMatch.Success 'The workflow script block could not be extracted.'
$runCode = [regex]::Replace($runMatch.Groups[1].Value, '(?m)^          ', '')
$workflowTokens = $null; $workflowErrors = $null
[void][Management.Automation.Language.Parser]::ParseInput($runCode, [ref]$workflowTokens, [ref]$workflowErrors)
Assert-True ($workflowErrors.Count -eq 0) 'The workflow PowerShell block does not parse.'
Assert-True ($yaml.Contains('pwsh -NoLogo -NoProfile -NonInteractive -File "{0}"')) 'The custom shell does not disable profiles.'
Assert-True (-not ($yaml -match 'actions/checkout|git clean|git reset')) 'The workflow includes a checkout or destructive Git operation.'
foreach ($entry in @(
    @('DescriptionRewritePlan.psm1', 'bbe4bb0c0096eb7ecf39bedadb1d9802060d8864a85ef9c42925a3568fdf44ec'),
    @('MetadataOnly.psm1', '5c3ee3aa9b9d71fbcb1219a071ffbb18ed0bd24ea16fb209a74e04fa036e645b'),
    @('Invoke-DescriptionRewriteMemory.ps1', '073ada0caad57638d07980e78e63b9a99efbb170f97844c3b57af2819d85c604')
)) {
    $name = [string]$entry[0]; $expectedHash = [string]$entry[1]
    Assert-True ($yaml.Contains("'tools/steam-workshop/$name' = '$expectedHash'")) 'A source hash is not pinned in the workflow.'
    Assert-True ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $name) -Algorithm SHA256).Hash.ToLowerInvariant() -ceq $expectedHash) 'A local source does not match its pinned hash.'
}
$filtered = Get-SteamCmdEnvironment @{ SystemRoot='test'; GH_TOKEN='secret-fixture'; STEAMCMD_USERNAME='user-fixture'; PATH='test' }
Assert-True (-not $filtered.ContainsKey('GH_TOKEN') -and -not $filtered.ContainsKey('STEAMCMD_USERNAME')) 'The child environment retained unapproved credentials.'
Write-Output 'PASS: offline workflow script, pinned hashes and child-environment isolation checks.'

Assert-True ($yaml.Contains('7baf77927639793e11b2a02ec2fd6d9114c95432be2f23a3225e274732255cf4')) 'The reviewed SteamCMD hash is not pinned.'
Assert-True ($yaml.Contains('Get-AuthenticodeSignature') -and $yaml.Contains('[IO.FileAttributes]::ReparsePoint')) 'The SteamCMD signature or path checks are missing.'
Write-Output 'PASS: offline checks for the pinned SteamCMD verification requirements.'

Assert-True ($yaml -notmatch '(?s)description-rewrite:\s*\r?\n\s*if:\s*>-\s*\r?\n\s*false\s*&&' -and $yaml.Contains("github.actor == github.repository_owner") -and $yaml.Contains("inputs.confirm_update == 'APPLY REVIEWED DESCRIPTION'")) 'The reviewed manual approval guards are missing.'
Write-Output 'PASS: offline check of manual owner approval guards.'

$controllerAst = [Management.Automation.Language.Parser]::ParseInput($controller, [ref]$tokens, [ref]$parseErrors)
$directoryFunction = $controllerAst.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -ceq 'Assert-RewriteProjectDirectory' }, $true)
Assert-True ($null -ne $directoryFunction) 'The project-directory guard is missing.'
. ([scriptblock]::Create($directoryFunction.Extent.Text))
$expectedRoot = [IO.Path]::GetFullPath('D:\Code\Game Code\Cities_Skyline_1_Modpack').TrimEnd('\')
Assert-RewriteProjectDirectory $expectedRoot
Assert-RewriteProjectDirectory (Join-Path $expectedRoot 'Resource')
Assert-Rejected { Assert-RewriteProjectDirectory ($expectedRoot + '-outside') } 'A similarly named outside directory was accepted.'
Assert-True ($yaml.Contains('[IO.File]::Open($expectedSteam, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)')) 'The SteamCMD read-sharing lock is missing.'
Write-Output 'PASS: offline project-root guard and executable-lock presence checks.'
