# Signature: OpenAI-Codex_workshop_implementation
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_]{1,100}$')][string]$Module,
    [Parameter(Mandatory)][string]$ModuleDll,
    [string]$DependencyDll = ''
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# AssemblyName reads managed metadata without loading or executing the assembly.
$result = [ordered]@{}
if ($Module -ceq 'AI_Improve') {
    if ([string]::IsNullOrWhiteSpace($DependencyDll)) { throw 'The required dependency is missing.' }
    $result['AIImprove.dll'] = [Reflection.AssemblyName]::GetAssemblyName([IO.Path]::GetFullPath($ModuleDll)).Name
    $result['CitiesHarmony.API.dll'] = [Reflection.AssemblyName]::GetAssemblyName([IO.Path]::GetFullPath($DependencyDll)).Name
} else {
    if (-not [string]::IsNullOrWhiteSpace($DependencyDll)) { throw 'This reviewed recipe has no approved packaged dependency.' }
    $registry = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'reviewed-recipes.json') -Raw | ConvertFrom-Json
    $matches = @($registry.recipes | Where-Object { $_.module -ceq $Module })
    if ($registry.schema -ne 1 -or $matches.Count -ne 1 -or $matches[0].assembly -cnotmatch '^[A-Za-z][A-Za-z0-9]{1,100}$') {
        throw 'A unique reviewed assembly recipe was not found.'
    }
    $filename = "$($matches[0].assembly).dll"
    if ([IO.Path]::GetFileName($ModuleDll) -cne $filename) { throw 'The assembly filename differs from its recipe.' }
    $result[$filename] = [Reflection.AssemblyName]::GetAssemblyName([IO.Path]::GetFullPath($ModuleDll)).Name
}
$result | ConvertTo-Json -Compress
