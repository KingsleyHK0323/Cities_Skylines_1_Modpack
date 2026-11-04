// Signature: OpenAI-Codex_aptp_release_boundary.
// Static source-boundary checks; these do not compile the mod or run the game.
import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {dirname, join} from 'node:path';

const root = dirname(dirname(fileURLToPath(import.meta.url)));
const read = path => readFileSync(join(root, path), 'utf8');

function conditionalProjection(source, enabled) {
  const active = [true];
  const out = [];
  for (const line of source.split(/\r?\n/)) {
    const directive = line.trim();
    if (directive === '#if DEVTOOLS') { active.push(active.at(-1) && enabled); continue; }
    if (directive === '#else') {
      assert.ok(active.length > 1, 'unmatched #else');
      const prior = active.pop();
      active.push(active.at(-1) && !prior);
      continue;
    }
    if (directive === '#endif') { assert.ok(active.length > 1, 'unmatched #endif'); active.pop(); continue; }
    assert.doesNotMatch(directive, /^#if\b/, 'unsupported conditional in source projection');
    if (active.at(-1)) out.push(line);
  }
  assert.equal(active.length, 1, 'unclosed conditional');
  return out.join('\n');
}

function compiledSources(devtools) {
  const project = read('AutomatedPublicTransportPlanning.csproj');
  const entries = [...project.matchAll(/<Compile\s+Include="([^"]+)"([^>]*?)\s*\/>/g)];
  assert.ok(entries.length > 0, 'missing project compile entries');
  return entries.filter(([, , attrs]) => !attrs.includes('Condition=') || devtools)
    .map(([, name]) => name.replaceAll('\\', '/'));
}

test('release includes the read-only preview pipeline and excludes write-capable development controls', () => {
  const release = compiledSources(false);
  for (const path of ['Source/Planning/CitizenUnitDemand.cs','Source/Planning/PlanningSnapshot.cs',
    'Source/Planning/PlanningEngine.cs','Source/Planning/PlanningCapture.cs',
    'Source/Planning/PlanningPreviewAdmission.cs',
    'Source/Planning/PlanningThreadingExtension.cs','Source/Planning/RouteStopPreview.cs',
    'Source/Planning/RouteEditRequestGate.cs']) assert.ok(release.includes(path), path);
  for (const path of ['Source/Planning/RouteCreation.cs','Source/Planning/RouteEditing.cs',
    'Source/RouteEditingUi.cs','Source/Spike/SaveRoundTripSpike.cs',
    'Source/Spike/PerformanceProbe.cs']) assert.ok(!release.includes(path), path);
});

test('release exposes and updates the read-only preview without route-writing controls', () => {
  const mod = conditionalProjection(read('Source/Mod.cs'), false);
  const bridge = conditionalProjection(read('Source/Planning/PlanningThreadingExtension.cs'), false);
  const loader = conditionalProjection(read('Source/Loader.cs'), false);
  assert.match(mod, /PlanningCapture\.RequestPreview\(\)/);
  assert.match(mod, /internal static void UpdatePlanningStatus\(string text\)/);
  assert.match(bridge, /PlanningCapture\.OnAfterSimulationFrame\(\)/);
  assert.match(bridge, /Mod\.UpdatePlanningStatus\(PlanningCapture\.DisplayText\)/);
  assert.match(loader, /PlanningCapture\.CancelForLevelChange\(\)/);
  assert.doesNotMatch(loader, /\b(RunSurvey|ReportTransportPrefabs|ReportBusDepots|ReportStopCapableSegments)\b/);
  for (const name of ['RouteCreationController','RouteEditingController','RouteEditingUi','SaveRoundTripSpike',
    'PerformanceProbe','OnDiagnosticsButton','DumpBusPrefabs']) {
    assert.doesNotMatch(mod + bridge + loader, new RegExp('\\b' + name + '\\b'));
  }
});

test('development keeps its existing guarded controls and risk description', () => {
  const mod = conditionalProjection(read('Source/Mod.cs'), true);
  const bridge = conditionalProjection(read('Source/Planning/PlanningThreadingExtension.cs'), true);
  const loader = conditionalProjection(read('Source/Loader.cs'), true);
  assert.match(mod, /RouteEditingUi\.Build\(helper\)/);
  assert.match(mod, /RouteCreationController\.RequestCreate/);
  assert.match(mod, /SaveRoundTripSpike\.CreateTestLine/);
  assert.match(mod, /PerformanceProbe\.Run/);
  assert.match(mod, /BusPrefabResolver\.DumpBusPrefabs/);
  assert.match(mod, /This version has DevTool/);
  assert.match(bridge, /RouteCreationController\.OnAfterSimulationFrame/);
  assert.match(bridge, /RouteEditingController\.OnAfterSimulationFrame/);
  assert.match(loader, /RunSurvey\(loadedAs\)/);
  assert.match(loader, /ReportTransportPrefabs\(\)/);
  assert.match(loader, /ReportBusDepots\(\)/);
  assert.match(loader, /ReportStopCapableSegments\(\)/);
});

test('the preview labels remain available in all eleven release language tables', () => {
  const strings = conditionalProjection(read('Source/Util/Strings.cs'), false);
  const languages = ['English','TraditionalChinese','SimplifiedChinese','Japanese','Korean',
    'German','French','Spanish','Portuguese','Russian','Polish'];
  for (const key of ['ButtonPlanningPreview','LabelPlanningStatus','SnapshotStaleNote','DemandProxyNote']) {
    assert.equal((strings.match(new RegExp('d\\[' + key + '\\]', 'g')) || []).length, 11, key);
    assert.match(strings, new RegExp('public const string ' + key + '\\b'));
    for (const language of languages) {
      const start = strings.indexOf('private static Dictionary<string, string> ' + language + '()');
      assert.notEqual(start, -1, language);
      const next = strings.indexOf('private static Dictionary<string, string> ', start + 1);
      const table = strings.slice(start, next === -1 ? undefined : next);
      assert.equal((table.match(new RegExp('d\\[' + key + '\\]', 'g')) || []).length, 1,
        language + ' ' + key);
    }
  }
  assert.doesNotMatch(strings, /public const string GroupSpike\b/);
});

test('capacity proxy is distinguished from actual people or ridership', () => {
  const engine = read('Source/Planning/PlanningEngine.cs');
  const mod = conditionalProjection(read('Source/Mod.cs'), false);
  assert.match(engine, /not actual people or ridership/);
  assert.doesNotMatch(engine, /Demand \(people\):/);
  assert.match(mod, /Loc\.Get\(Strings\.DemandProxyNote\)/);
});

test('preview admission prepares ticket before publication and invalidates it on cancellation', () => {
  const capture = read('Source/Planning/PlanningCapture.cs');
  assert.match(capture, /s_admission\.TryQueue\(delegate/);
  assert.match(capture, /s_admission\.TryTake\(\)/);
  assert.match(capture, /s_admission\.Cancel\(delegate/);
  assert.match(capture, /s_admission\.TryResetCancelled\(delegate/);
  assert.match(capture, /s_workerGate\.TryPublish\(workerTicket, delegate \{ s_admission\.Complete\(\); \}\)/);
  assert.match(capture, /private static void UpdateDisplay\(string text\)[\s\S]*?s_workerGate\.TryPublish\(s_captureTicket, delegate/);
  assert.doesNotMatch(capture, /\bs_request\b|\bs_cancel\b|\bs_busy\b/);
});

test('the proposed preview sources contain no known direct route-mutation calls', () => {
  const files = ['Source/Planning/CitizenUnitDemand.cs','Source/Planning/PlanningSnapshot.cs',
    'Source/Planning/PlanningEngine.cs','Source/Planning/PlanningCapture.cs',
    'Source/Planning/RouteStopPreview.cs','Source/Planning/RouteEditRequestGate.cs'];
  for (const path of files) {
    assert.doesNotMatch(read(path), /\b(CreateLine|ReleaseLine|AddStop|MoveStop|SetStop|RemoveStop)\s*\(/, path);
  }
});
