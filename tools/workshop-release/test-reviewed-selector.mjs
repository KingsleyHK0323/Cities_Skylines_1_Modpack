// Signature: OpenAI-Codex.
// Exercise the real PowerShell recipe-selection boundary without starting a build or runner.
import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const directory = fileURLToPath(new URL('.', import.meta.url));
const script = readFileSync(new URL('./Build-Reviewed.ps1', import.meta.url), 'utf8');
const begin = script.indexOf('$registry = Get-Content');
const end = script.indexOf('$source = [IO.Path]', begin);
assert.ok(begin >= 0 && end > begin, 'The reviewed recipe-selection boundary is missing.');
const selection = script.slice(begin, end);
const inspector = readFileSync(new URL('./Inspect-Assemblies.ps1', import.meta.url), 'utf8');
const inspectBegin = inspector.indexOf('$registry = Get-Content');
const inspectEnd = inspector.indexOf('if ([IO.Path]::GetFileName($ModuleDll)', inspectBegin);
assert.ok(inspectBegin >= 0 && inspectEnd > inspectBegin, 'The assembly recipe-selection boundary is missing.');
const inspectionSelection = inspector.slice(inspectBegin, inspectEnd);
const validSha = 'fab5d2bfa27ce03981de3a845cf4ee40148a1201';

function runSelection(module, sha, segment, outputVariable) {
  assert.match(module, /^[A-Za-z0-9_]+$/);
  assert.match(sha, /^[a-z0-9]+$/);
  const command = `
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$PSScriptRoot = (Get-Location).Path
$Module = '${module}'
$SourceSha = '${sha}'
try {
${segment}
  [Console]::Out.WriteLine(${outputVariable})
} catch {
  [Console]::Error.WriteLine($_.Exception.Message)
  exit 1
}
`;
  return spawnSync('pwsh', ['-NoLogo', '-NoProfile', '-NonInteractive', '-EncodedCommand',
    Buffer.from(command, 'utf16le').toString('base64')], {cwd:directory, encoding:'utf8'});
}

test('the actual PowerShell selector retains the reviewed Wireless recipe after SHA validation', () => {
  const result = runSelection('Wireless_Utilities_And_Global_Coverage', validSha, selection, '$recipe.assembly');
  assert.equal(result.status, 0, result.stderr);
  assert.equal(result.stdout.trim(), 'WirelessUtilitiesAndGlobalCoverage');
});

test('the actual PowerShell selector rejects an unreviewed SHA or module', () => {
  for (const [module, sha] of [
    ['Wireless_Utilities_And_Global_Coverage', 'a'.repeat(40)],
    ['Unknown_Module', validSha],
  ]) {
    const result = runSelection(module, sha, selection, '$recipe.assembly');
    assert.notEqual(result.status, 0);
    assert.match(result.stderr, /unique reviewed recipe/i);
  }
});

test('the actual assembly inspector retains the reviewed filename after name validation', () => {
  const result = runSelection('Wireless_Utilities_And_Global_Coverage', validSha, inspectionSelection, '$filename');
  assert.equal(result.status, 0, result.stderr);
  assert.equal(result.stdout.trim(), 'WirelessUtilitiesAndGlobalCoverage.dll');
});
