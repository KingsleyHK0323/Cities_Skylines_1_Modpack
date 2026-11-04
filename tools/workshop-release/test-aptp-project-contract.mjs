// Signature: OpenAI-Codex.
// Exercise the real project-input check with in-memory XML; no build or runner is started.
import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';
import {recipes} from './recipes.mjs';

const directory = fileURLToPath(new URL('.', import.meta.url));
const script = readFileSync(new URL('./Build-APTP.ps1', import.meta.url), 'utf8');
const begin = script.indexOf('$namespaces = [System.Xml.XmlNamespaceManager]');
const end = script.indexOf('$vswhere = ', begin);
assert.ok(begin >= 0 && end > begin, 'The APTP project-input check is missing.');
const contract = script.slice(begin, end);
const recipe = recipes.find(item => item.module === 'Automated_Public_Transport_Planning');
const devSources = [
  'Source/Planning/RouteCreation.cs', 'Source/Planning/RouteEditPlan.cs',
  'Source/Planning/RouteStreetStopRules.cs', 'Source/Planning/RouteManualInput.cs',
  'Source/Planning/RouteEditing.cs', 'Source/Planning/RouteEditExecution.cs',
  'Source/RouteEditingUi.cs', 'Source/Spike/PerformanceProbe.cs', 'Source/Spike/SaveRoundTripSpike.cs',
];

function projectXml({extra = '', devCondition = "'$(DevTools)' == 'true'", codePage = '65001',
  groupCondition = '', referenceCondition = '', extraTop = ''} = {}) {
  const sources = recipe.sources.map(path => `<Compile Include="${path}" />`).join('');
  const dev = devSources.map(path => `<Compile Include="${path}" Condition="${devCondition}" />`).join('');
  const references = recipe.references.map(name => `<Reference Include="${name.slice(0, -4)}"${referenceCondition ? ` Condition="${referenceCondition}"` : ''} />`).join('');
  return `<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">` +
    `<PropertyGroup><CodePage>${codePage}</CodePage><LangVersion>5</LangVersion></PropertyGroup>` +
    `<ItemGroup${groupCondition ? ` Condition="${groupCondition}"` : ''}>${sources}${dev}${extra}</ItemGroup>` +
    `<ItemGroup>${references}</ItemGroup>${extraTop}</Project>`;
}

function checkProject(xml, recipeLanguage = '5') {
  const xmlBase64 = Buffer.from(xml, 'utf8').toString('base64');
  const command = `
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$registry = Get-Content -LiteralPath 'reviewed-recipes.json' -Raw | ConvertFrom-Json
$reviewedCandidates = @($registry.recipes | Where-Object { $_.module -ceq 'Automated_Public_Transport_Planning' })
$reviewedCandidates[0].languageVersion = '${recipeLanguage}'
[xml]$project = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('${xmlBase64}'))
try {
${contract}
  [Console]::Out.WriteLine('accepted')
} catch {
  [Console]::Error.WriteLine($_.Exception.Message)
  exit 1
}
`;
  return spawnSync('pwsh', ['-NoLogo', '-NoProfile', '-NonInteractive', '-EncodedCommand',
    Buffer.from(command, 'utf16le').toString('base64')], {cwd:directory, encoding:'utf8'});
}

test('the APTP project contract accepts the reviewed source and reference shape', () => {
  const result = checkProject(projectXml());
  assert.equal(result.status, 0, result.stderr);
  assert.equal(result.stdout.trim(), 'accepted');
});

test('unexpected conditional sources or item-group conditions are rejected', () => {
  for (const fixture of [
    projectXml({extra:`<Compile Include="Source/Unexpected.cs" Condition="'1' == '1'" />`}),
    projectXml({devCondition:"'1' == '1'"}),
    projectXml({groupCondition:"'1' == '1'"}),
    projectXml({referenceCondition:"'1' == '1'"}),
    projectXml({codePage:'1252'}),
  ]) {
    const result = checkProject(fixture);
    assert.notEqual(result.status, 0, 'The changed project must be rejected.');
  }
  assert.notEqual(checkProject(projectXml(), '7.3').status, 0, 'A changed recipe language must be rejected.');
});

test('Choose and ImportGroup blocks cannot add unchecked MSBuild inputs', () => {
  for (const extraTop of [
    `<Choose><When Condition="'1' == '1'"><ItemGroup><Compile Include="Source/Unexpected.cs" /></ItemGroup></When></Choose>`,
    '<ImportGroup><Import Project="unexpected.targets" /></ImportGroup>',
  ]) {
    const result = checkProject(projectXml({extraTop}));
    assert.notEqual(result.status, 0, 'An unreviewed MSBuild structure must be rejected.');
  }
});

test('the APTP build command explicitly clears extra compilation constants', () => {
  const command = script.match(/& \$msbuild\s+'AutomatedPublicTransportPlanning\.csproj'[\s\S]*?\/verbosity:normal\s+\/nologo/);
  assert.ok(command, 'The APTP MSBuild command is missing.');
  const constants = [...command[0].matchAll(/\/p:DefineConstants=([^\s`]*)/g)].map(match => match[1]);
  const devTools = [...command[0].matchAll(/\/p:DevTools=([^\s`]*)/g)].map(match => match[1]);
  assert.deepEqual(constants, ['']);
  assert.deepEqual(devTools, ['false']);
});
