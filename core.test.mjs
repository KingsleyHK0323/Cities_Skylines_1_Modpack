// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { createRequire } from 'node:module';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';
import { safeRelative, verifyPackage, installationPlan, uninstallPlan, updateStatus, coexistenceWarning, catalogStatus } from './core.mjs';
const bytes = Buffer.from('synthetic fixture, not a game DLL');
const sha256 = createHash('sha256').update(bytes).digest('hex');
const pkg = () => ({ moduleId: 'example', buildId: 'build-a', files: [{ path: 'mod.dll', bytes, sha256 }] });

test('preview selection supports individual and visible bulk selection without execution', () => {
  const { createPreviewModel, sampleModules } = createRequire(import.meta.url)('./preview-model.js');
  const model = createPreviewModel(sampleModules);
  assert.equal(model.selected().length, 0);
  model.select('ai-improve', true);
  assert.equal(model.selected().length, 1);
  model.selectVisible('', 'different-build', true);
  assert.equal(model.selected().length, 2);
  assert.equal(model.visible('CITY TIME', 'all').length, 1);
  assert.equal(model.visible('no results here', 'all').length, 0);
  assert.equal(model.visible('', 'unavailable').length, 1);
  assert.equal(model.selected().length, 2);
  for (const operation of ['install', 'update', 'uninstall']) {
    const plan = model.operationPlan(operation);
    assert.equal(plan.allowed, false); assert.equal(plan.items.length, 2);
    assert.ok(plan.items.every(item => item.allowed === false && item.reason));
  }
  model.clear(); assert.equal(model.selected().length, 0);
  assert.equal(model.operationPlan('install').allowed, false);
  assert.throws(() => model.select('unknown', true));
});
test('preview data is synthetic, detached and never upgrades different-build to newer', () => {
  const { createPreviewModel, sampleModules } = createRequire(import.meta.url)('./preview-model.js');
  const model = createPreviewModel(sampleModules);
  const unavailable = model.visible('', 'unavailable')[0];
  assert.ok(unavailable.reason);
  model.select(unavailable.id, true);
  assert.equal(model.operationPlan('install').items[0].allowed, false);
  const detached = model.visible('', 'all'); detached[0].name = 'tampered';
  assert.notEqual(model.visible('', 'all')[0].name, 'tampered');
  assert.ok(sampleModules.every(item => item.synthetic === true));
  assert.ok(!sampleModules.some(item => item.name === 'AI Gaming'));
  assert.ok(model.visible('', 'all').some(item => item.coexistenceWarning));
  assert.throws(() => model.operationPlan('execute'));
});
test('preview controller preserves selections across filters and keeps actions disabled', () => {
  // A minimal DOM harness exercises event wiring, not browser layout or accessibility.
  class Element {
    constructor(tag = 'div') { this.tag = tag; this.children = []; this.listeners = {}; this.attributes = {}; this.value = ''; }
    append(...nodes) { this.children.push(...nodes); }
    replaceChildren(...nodes) { this.children = nodes; }
    addEventListener(type, handler) { this.listeners[type] = handler; }
    setAttribute(key, value) { this.attributes[key] = value; }
    querySelectorAll() {
      return this.children.flatMap(child => [ ...(child.tag === 'input' && child.type === 'checkbox' ? [child] : []), ...child.querySelectorAll() ]);
    }
    dispatch(type) { this.listeners[type](); }
  }
  const html = readFileSync(new URL('./preview.html', import.meta.url), 'utf8');
  const elements = Object.fromEntries([...html.matchAll(/id="([a-z-]+)"/g)].map(match => [match[1], new Element()]));
  elements['status-filter'].value = 'all';
  const api = createRequire(import.meta.url)('./preview-model.js');
  const document = { getElementById: id => elements[id], createElement: tag => new Element(tag), documentElement: { dataset: {} } };
  vm.runInNewContext(readFileSync(new URL('./preview.js', import.meta.url), 'utf8'), { document, DevManagerPreview: api });
  assert.equal(elements.modules.children.length, 5);
  const first = elements.modules.querySelectorAll()[0]; first.checked = true; first.dispatch('change');
  assert.equal(elements['selection-count'].textContent, '1');
  assert.equal(elements['select-visible'].indeterminate, true);
  elements['status-filter'].value = 'different-build'; elements['status-filter'].dispatch('change');
  elements['select-visible'].checked = true; elements['select-visible'].dispatch('change');
  assert.equal(elements['selection-count'].textContent, '2');
  assert.equal(elements['select-visible'].checked, true);
  elements.review.dispatch('click');
  assert.equal(elements['review-panel'].hidden, false);
  assert.equal(elements['review-items'].children.length, 2);
  assert.equal(elements.review.attributes['aria-expanded'], 'true');
  elements.search.value = 'no match'; elements.search.dispatch('input');
  assert.equal(elements.empty.hidden, false); assert.equal(elements['select-visible'].disabled, true);
  assert.equal(elements['review-panel'].hidden, true);
  assert.equal(elements['selection-count'].textContent, '2');
  elements.clear.dispatch('click'); assert.equal(elements['selection-count'].textContent, '0');
  elements.theme.dispatch('click'); assert.equal(document.documentElement.dataset.theme, 'dark');
  assert.match(html, /role="status" aria-live="polite"/);
  for (const label of ['Install selected', 'Update selected', 'Uninstall selected'])
    assert.ok(html.includes(`<button type="button" disabled>${label}</button>`));
  assert.match(html, /connect-src 'none'/);
});

const catalogFixture = () => ({ schema: 1, moduleId: 'example',
  repository: 'Owner/Repo', branch: 'Example', assembly: 'Example', workflow: 'dev.yml',
  channel: 'development', buildId: 'opaque-b', sourceSha: 'a'.repeat(40), devTool: true,
  release: { id: 42, tag: 'example-dev-b', draft: false, publishedAt: '2026-11-02T00:00:00Z' },
  files: ['Example.dll', 'dependency/api.dll'].map((p, i) => ({ path: p,
    assetName: p.split('/').at(-1), assetId: 100 + i, size: 200, sha256: 'b'.repeat(64),
    url: `https://github.com/Owner/Repo/releases/download/example-dev-b/${p.split('/').at(-1)}` })) });
const catalogInput = (edit = () => {}) => {
  const catalog = catalogFixture(); edit(catalog);
  const data = Buffer.from(JSON.stringify(catalog));
  return { data, policy: { moduleId: 'example', repository: 'Owner/Repo', branch: 'Example',
    assembly: 'Example', workflow: 'dev.yml', requiredFiles: ['Example.dll', 'dependency/api.dll'],
    catalogSha256: createHash('sha256').update(data).digest('hex') } };
};

test('injected download verifies pinned catalog, streamed bytes and exact length', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const payload = Buffer.from('synthetic downloaded bytes');
  const input = catalogInput(c => { c.files[0].size = payload.length; c.files[0].sha256 = createHash('sha256').update(payload).digest('hex'); });
  const calls = [];
  const transport = async request => {
    calls.push(request);
    return { url: request.url, redirected: false, status: 200, headers: { 'content-length': String(payload.length) },
      body: (async function* () { yield payload.subarray(0, 4); yield payload.subarray(4); })() };
  };
  const result = await downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport });
  assert.deepEqual(result.bytes, payload); assert.equal(result.moduleId, 'example');
  assert.equal(calls.length, 1); assert.equal(calls[0].redirect, 'manual'); assert.equal(calls[0].credentials, 'omit');
  assert.equal(calls[0].signal.aborted, true);
  await assert.rejects(downloadCatalogAsset({ ...input, policy: { ...input.policy, catalogSha256: '0'.repeat(64) }, assetPath: 'Example.dll', transport }), { code: 'SOURCE_UNAVAILABLE' });
  assert.equal(calls.length, 1);
});

test('injected download rejects partial, oversized, corrupted and transformed streams', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const payload = Buffer.from('fixture');
  const input = catalogInput(c => { c.files[0].size = payload.length; c.files[0].sha256 = createHash('sha256').update(payload).digest('hex'); });
  const scenarios = [
    { bytes: payload.subarray(1), code: 'LENGTH_MISMATCH' },
    { bytes: Buffer.concat([payload, Buffer.from('x')]), code: 'SIZE_LIMIT' },
    { bytes: Buffer.from('xxxxxxx'), code: 'INTEGRITY_MISMATCH' },
    { bytes: payload, headers: { 'content-length': '999' }, code: 'LENGTH_MISMATCH' },
    { bytes: payload, headers: { 'content-encoding': 'gzip' }, code: 'ENCODING_REFUSED' },
    { bytes: 'fixture', code: 'INVALID_CHUNK' }
  ];
  for (const scenario of scenarios) {
    let signal;
    const transport = async request => {
      signal = request.signal;
      return { url: request.url, redirected: false, status: 200, headers: scenario.headers ?? {},
        body: (async function* () { yield scenario.bytes; })() };
    };
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport }), { code: scenario.code });
    assert.equal(signal.aborted, true);
  }
});

test('injected download rejects redirects by default and permits only exact HTTPS targets', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const payload = Buffer.from('fixture');
  const input = catalogInput(c => { c.files[0].size = payload.length; c.files[0].sha256 = createHash('sha256').update(payload).digest('hex'); });
  const target = 'https://cdn.example.test/exact/file.dll?signature=synthetic';
  let calls = 0;
  const transport = async request => {
    calls++;
    return request.url === target ? { url: request.url, redirected: false, status: 200, headers: {},
      body: (async function* () { yield payload; })() } :
      { url: request.url, redirected: false, status: 302, headers: { location: target } };
  };
  await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport }), { code: 'REDIRECT_REFUSED' });
  assert.equal(calls, 1);
  const result = await downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport, redirectUrls: [target] });
  assert.deepEqual(result.bytes, payload);
  await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport, redirectUrls: ['http://cdn.example.test/file'] }), { code: 'INVALID_DOWNLOAD_POLICY' });
});

test('injected download deadline aborts hanging headers and hanging body without a partial result', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const input = catalogInput();
  for (const phase of ['headers', 'body']) {
    let signal;
    const transport = request => {
      signal = request.signal;
      if (phase === 'headers') return new Promise(() => {});
      return { url: request.url, redirected: false, status: 200, headers: {},
        body: { [Symbol.asyncIterator]() { return { next: () => new Promise(() => {}) }; } } };
    };
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport, timeoutMs: 20 }), { code: 'TIMEOUT' });
    assert.equal(signal.aborted, true);
  }
});

test('injected download rejects automatic redirects, loops and excessive hops', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const input = catalogInput();
  for (const variant of ['automatic', 'changed-source', 'loop', 'limit', 'status']) {
    const transport = async request => ({ url: variant === 'changed-source' ? 'https://other.example.test/' : request.url,
      redirected: variant === 'automatic', status: variant === 'status' ? 403 : 302,
      headers: { location: request.url } });
    const code = { automatic: 'SOURCE_CHANGED', 'changed-source': 'SOURCE_CHANGED', loop: 'REDIRECT_LOOP', limit: 'REDIRECT_LIMIT', status: 'HTTP_STATUS' }[variant];
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport,
      redirectUrls: [JSON.parse(input.data).files[0].url], maxRedirects: variant === 'limit' ? 0 : 3 }), { code });
  }
  for (const target of ['https://user:secret@example.test/file', 'https://example.test/a/../file', 'https://example.test/file#fragment'])
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport: () => { throw new Error('must not request'); }, redirectUrls: [target] }), { code: 'INVALID_DOWNLOAD_POLICY' });
});

test('injected download hides transport diagnostics and never returns interrupted bytes', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const input = catalogInput();
  for (const phase of ['headers', 'stream']) {
    const transport = async request => {
      if (phase === 'headers') throw new Error('https://example.test/?secret=PRIVATE');
      return { url: request.url, redirected: false, status: 200, headers: {},
        body: (async function* () { yield Buffer.from('partial'); throw new Error('PRIVATE'); })() };
    };
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport }), error => {
      assert.equal(error.code, 'TRANSPORT_FAILED'); assert.ok(!error.message.includes('PRIVATE'));
      assert.equal(error.bytes, undefined); assert.equal(error.cause, undefined); return true;
    });
  }
});

test('injected download snapshots chunks before an adapter reuses its buffer', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const payload = Buffer.from('fixture');
  const input = catalogInput(c => { c.files[0].size = payload.length; c.files[0].sha256 = createHash('sha256').update(payload).digest('hex'); });
  const transport = async request => ({ url: request.url, redirected: false, status: 200, headers: {},
    body: (async function* () { const chunk = Buffer.from(payload); yield chunk; chunk.fill(0); })() });
  const result = await downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport });
  assert.deepEqual(result.bytes, payload);
});

function batchFixture() {
  const payload = Buffer.from('batch fixture');
  const entries = ['first', 'second'].map(moduleId => {
    const input = catalogInput(c => {
      c.moduleId = moduleId;
      for (const file of c.files) { file.size = payload.length; file.sha256 = createHash('sha256').update(payload).digest('hex'); }
    });
    input.policy.moduleId = moduleId;
    return { moduleId, ...input, installed: null, synthetic: true };
  });
  return { payload, entries };
}
test('batch assessment feeds the selection model while retaining blocked installation', async () => {
  const { inspectModuleBatch } = await import('./batch.mjs');
  const { entries } = batchFixture();
  entries[1].policy.catalogSha256 = '0'.repeat(64);
  const assessment = inspectModuleBatch(entries);
  assert.equal(assessment.canPrepare, false); assert.equal(assessment.installAllowed, false);
  assert.equal(assessment.previewRecords[0].status, 'not-installed');
  assert.equal(assessment.previewRecords[1].status, 'unavailable');
  const { createPreviewModel } = createRequire(import.meta.url)('./preview-model.js');
  const model = createPreviewModel(assessment.previewRecords);
  model.selectVisible('', 'all', true);
  assert.equal(model.operationPlan('install').allowed, false);
});
test('batch preparation returns only complete verified in-memory module packages', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries, payload } = batchFixture(); let calls = 0;
  const result = await prepareModuleBatch(entries, { transport: async request => {
    calls++;
    return { url: request.url, redirected: false, status: 200, headers: {}, body: (async function* () { yield payload; })() };
  } });
  assert.equal(calls, 4); assert.equal(result.packages.length, 2);
  assert.equal(result.installAllowed, false);
  for (const pkg of result.packages) assert.equal(verifyPackage(pkg).length, 2);
});
test('batch preflight refuses unavailable or duplicate modules before any transfer', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries } = batchFixture(); let calls = 0;
  const transport = () => { calls++; throw new Error('must not request'); };
  await assert.rejects(prepareModuleBatch([entries[0], entries[0]], { transport }), { code: 'BATCH_UNAVAILABLE' });
  entries[1].policy.requiredFiles.push('missing.dll');
  await assert.rejects(prepareModuleBatch(entries, { transport }), { code: 'BATCH_UNAVAILABLE' });
  await assert.rejects(prepareModuleBatch([], { transport }), { code: 'BATCH_UNAVAILABLE' });
  assert.equal(calls, 0);
});
test('batch failure after an earlier module returns no partially prepared packages', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries, payload } = batchFixture(); let calls = 0;
  await assert.rejects(prepareModuleBatch(entries, { transport: async request => {
    calls++;
    return { url: request.url, redirected: false, status: 200, headers: {},
      body: (async function* () { yield calls === 3 ? Buffer.from('corrupt bytes') : payload; })() };
  } }), error => {
    assert.equal(error.code, 'BATCH_ASSET_FAILED'); assert.equal(error.moduleId, 'second');
    assert.equal(error.packages, undefined); assert.equal(error.bytes, undefined); return true;
  });
  assert.equal(calls, 3);
});
test('batch preparation snapshots all source contracts before awaiting transport', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries, payload } = batchFixture(); let calls = 0;
  const result = await prepareModuleBatch(entries, { transport: async request => {
    if (++calls === 1) { entries[1].data.fill(0); entries[1].policy.moduleId = 'tampered'; }
    return { url: request.url, redirected: false, status: 200, headers: {}, body: (async function* () { yield payload; })() };
  } });
  assert.equal(result.packages[1].moduleId, 'second'); assert.equal(calls, 4);
});
test('selection review is explanatory and cannot enable synthetic operations', () => {
  const { createPreviewModel } = createRequire(import.meta.url)('./preview-model.js');
  const model = createPreviewModel(); model.selectVisible('', 'all', true);
  const review = model.reviewSelection();
  assert.equal(review.installAllowed, false); assert.equal(review.items.length, 5);
  assert.ok(review.items.every(item => item.blockers.length > 0));
  assert.ok(review.stages.length >= 3);
});
test('batch bounds and explicit installation records fail closed before transfer', async () => {
  const { inspectModuleBatch, prepareModuleBatch } = await import('./batch.mjs');
  const { entries } = batchFixture(); let calls = 0;
  const transport = () => { calls++; throw new Error('must not request'); };
  delete entries[1].installed;
  assert.equal(inspectModuleBatch(entries).canPrepare, false);
  await assert.rejects(prepareModuleBatch(entries, { transport }), { code: 'BATCH_UNAVAILABLE' });
  const large = ['first', 'second', 'third'].map(moduleId => {
    const input = catalogInput(c => { c.moduleId = moduleId; for (const f of c.files) f.size = 32 * 1024 * 1024; });
    input.policy.moduleId = moduleId; return { moduleId, ...input, installed: null };
  });
  assert.equal(inspectModuleBatch(large).reason, 'BATCH_SIZE_LIMIT');
  await assert.rejects(prepareModuleBatch(large, { transport }), { code: 'BATCH_UNAVAILABLE' });
  await assert.rejects(prepareModuleBatch(batchFixture().entries, { transport, totalTimeoutMs: 30001 }), { code: 'INVALID_BATCH_POLICY' });
  assert.equal(calls, 0);
});
test('batch total deadline aborts a stalled asset and exposes no prepared package', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries } = batchFixture(); let signal;
  await assert.rejects(prepareModuleBatch(entries, { totalTimeoutMs: 25, transport: request => {
    signal = request.signal; return new Promise(() => {});
  } }), error => {
    assert.equal(error.code, 'BATCH_TIMEOUT'); assert.equal(error.packages, undefined); return true;
  });
  assert.equal(signal.aborted, true);
});

function planningFixture() {
  const { entries, payload } = batchFixture();
  return { requests: entries.map(entry => ({ operation: 'install', moduleId: entry.moduleId, source: entry,
    package: { moduleId: entry.moduleId, buildId: JSON.parse(entry.data).buildId,
      files: JSON.parse(entry.data).files.map(f => ({ path: f.path, sha256: f.sha256, bytes: Buffer.from(payload) })) } })),
    observation: { schema: 1, snapshotId: 'synthetic-observation', complete: true, rootSafe: true, gameRunning: false,
      modules: entries.map(entry => ({ moduleId: entry.moduleId, directoryPresent: false,
        reparsePoint: false, ownership: null, files: [], stableInstalled: false })) },
    rules: { complete: true, conflicts: [], requires: [] } };
}
function ownPlanningModule(fixture, index) {
  const pkg = fixture.requests[index].package; const observed = fixture.observation.modules[index];
  observed.directoryPresent = true;
  observed.ownership = { moduleId: pkg.moduleId, buildId: pkg.buildId, files: pkg.files.map(({ path, sha256 }) => ({ path, sha256 })) };
  observed.files = [{ path: 'dependency', kind: 'directory', reparsePoint: false },
    ...pkg.files.map(({ path, sha256 }) => ({ path, sha256, kind: 'file', hardLink: false, reparsePoint: false }))];
}
test('operation planner checks complete synthetic packages but never authorizes execution', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); const plan = planModuleOperations(f);
  assert.equal(plan.modelReady, true); assert.equal(plan.executionAllowed, false);
  assert.equal(plan.steps.length, 2); assert.ok(plan.warnings.includes('DLL_IDENTITY_UNVERIFIED'));
  assert.equal(f.observation.modules[0].directoryPresent, false);
});
test('operation planner refuses unavailable sources, mismatched packages and unknown targets', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  for (const edit of [f => { f.requests[0].source.policy.catalogSha256 = '0'.repeat(64); },
    f => { f.requests[0].package.buildId = 'wrong'; }, f => { f.requests[0].package.files.pop(); },
    f => { f.observation.modules[0].directoryPresent = true; },
    f => { f.observation.complete = false; }, f => { f.observation.gameRunning = true; },
    f => { f.rules.complete = false; }, f => { f.requests.push(f.requests[0]); }]) {
    const f = planningFixture(); edit(f); const plan = planModuleOperations(f);
    assert.equal(plan.modelReady, false, String(edit)); assert.equal(plan.steps.length, 0);
  }
});
test('uninstall planner preserves modified, unknown, missing and linked content', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  for (const edit of [f => { f.observation.modules[0].files[1].sha256 = '0'.repeat(64); },
    f => { f.observation.modules[0].files[1].hardLink = true; },
    f => { f.observation.modules[0].reparsePoint = true; },
    f => { f.observation.modules[0].files.push({ path: 'personal', kind: 'directory', reparsePoint: false }); },
    f => { f.observation.modules[0].files.pop(); }, f => { f.observation.modules[0].ownership = null; }]) {
    const f = planningFixture(); ownPlanningModule(f, 0);
    f.requests = [{ operation: 'uninstall', moduleId: 'first' }]; edit(f);
    const plan = planModuleOperations(f); assert.equal(plan.modelReady, false, String(edit));
    assert.equal(plan.executionAllowed, false); assert.deepEqual(plan.steps, []);
  }
});
test('uninstall planning is quarantine-only and coexistence never changes subscriptions', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); ownPlanningModule(f, 0); f.observation.modules[0].stableInstalled = true;
  f.requests = [{ operation: 'uninstall', moduleId: 'first' }];
  const plan = planModuleOperations(f);
  assert.equal(plan.modelReady, true); assert.equal(plan.steps[0].mode, 'quarantine-owned-directory');
  assert.ok(plan.warnings.includes('COEXISTENCE:first')); assert.equal(plan.executionAllowed, false);
});
test('first development install warns about an existing stable copy without blocking the model', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); f.observation.modules[0].stableInstalled = true;
  const plan = planModuleOperations(f);
  assert.equal(plan.modelReady, true);
  assert.ok(plan.warnings.includes('COEXISTENCE:first'));
  assert.equal(plan.warnings.filter(w => w === 'COEXISTENCE:first').length, 1);
  assert.equal(plan.executionAllowed, false);
  assert.equal(f.observation.modules[0].directoryPresent, false);
});
test('operation planner detects explicit conflicts and protects required modules', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); f.rules.conflicts.push(['first', 'second']);
  assert.equal(planModuleOperations(f).modelReady, false);
  const removal = planningFixture(); ownPlanningModule(removal, 0); ownPlanningModule(removal, 1);
  removal.rules.requires.push({ moduleId: 'second', dependencyId: 'first' });
  removal.requests = [{ operation: 'uninstall', moduleId: 'first' }];
  assert.ok(planModuleOperations(removal).blockers.some(b => b.code === 'DEPENDENCY_MISSING'));
});
test('operation planner orders dependencies and rejects cycles without partial steps', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); f.rules.requires.push({ moduleId: 'second', dependencyId: 'first' });
  f.requests.reverse();
  assert.deepEqual(planModuleOperations(f).steps.map(s => s.moduleId), ['first', 'second']);
  f.rules.requires.push({ moduleId: 'first', dependencyId: 'second' });
  assert.equal(planModuleOperations(f).steps.length, 0);
  const removal = planningFixture(); ownPlanningModule(removal, 0); ownPlanningModule(removal, 1);
  removal.rules.requires.push({ moduleId: 'second', dependencyId: 'first' });
  removal.requests = [{ operation: 'uninstall', moduleId: 'first' }, { operation: 'uninstall', moduleId: 'second' }];
  assert.deepEqual(planModuleOperations(removal).steps.map(s => s.moduleId), ['second', 'first']);
});
test('prepared batch feeds planning but source changes still invalidate installation plans', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); const { payload } = batchFixture();
  const prepared = await prepareModuleBatch(f.requests.map(r => r.source), { transport: async request =>
    ({ url: request.url, redirected: false, status: 200, headers: {}, body: (async function* () { yield payload; })() }) });
  for (const request of f.requests) request.package = prepared.packages.find(pkg => pkg.moduleId === request.moduleId);
  assert.equal(planModuleOperations(f).modelReady, true);
  f.requests[1].source.policy.catalogSha256 = '0'.repeat(64);
  assert.equal(planModuleOperations(f).modelReady, false);
  assert.equal(planModuleOperations(f).executionAllowed, false);
});
test('planner rejects unsafe paths, missing rule observations and inconsistent snapshots', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  for (const edit of [f => { f.observation.modules[0].files.push({ path: '../outside', kind: 'directory', reparsePoint: false }); },
    f => { f.rules.conflicts.push(['first', 'unknown']); },
    f => { f.observation.modules.push(f.observation.modules[0]); },
    f => { f.observation.rootSafe = false; },
    f => { f.requests[0].operation = 'delete'; },
    f => { f.observation.modules[0].files.push({ path: 'extra', kind: 'directory', reparsePoint: false }); }]) {
    const f = planningFixture(); edit(f); const plan = planModuleOperations(f);
    assert.equal(plan.modelReady, false, String(edit)); assert.equal(plan.steps.length, 0);
  }
});
test('pinned per-module catalog compares opaque builds without inferring ordering', () => {
  const { data, policy } = catalogInput();
  assert.equal(catalogStatus(null, policy, data).status, 'not-installed');
  assert.equal(catalogStatus({ moduleId: 'example', buildId: 'opaque-b' }, policy, data).status, 'current');
  assert.equal(catalogStatus({ moduleId: 'example', buildId: 'opaque-z' }, policy, data).status, 'different-build');
  assert.equal(catalogStatus({ moduleId: 'other', buildId: 'opaque-b' }, policy, data).status, 'unavailable');
  const withFlag = catalogInput(c => { c.release.prerelease = false; });
  assert.equal(catalogStatus(null, withFlag.policy, withFlag.data).status, 'not-installed');
});
test('catalog trust anchor is external and exact bytes must match', () => {
  const { data, policy } = catalogInput();
  assert.equal(catalogStatus(null, { ...policy, catalogSha256: null }, data).status, 'unavailable');
  assert.equal(catalogStatus(null, policy, Buffer.concat([data, Buffer.from(' ')])).status, 'unavailable');
  assert.equal(catalogStatus(null, policy, Buffer.alloc(1024 * 1024 + 1)).status, 'unavailable');
});
test('catalog refuses repository and release dot segments before URL normalization', () => {
  for (const repository of ['./Repo', '../Repo', 'Owner/.', 'Owner/..']) {
    const input = catalogInput(c => {
      c.repository = repository;
      for (const f of c.files) f.url = `https://github.com/${repository}/releases/download/${encodeURIComponent(c.release.tag)}/${encodeURIComponent(f.assetName)}`;
    });
    input.policy.repository = repository;
    assert.equal(catalogStatus(null, input.policy, input.data).status, 'unavailable', repository);
  }
  for (const tag of ['.', '..']) {
    const input = catalogInput(c => {
      c.release.tag = tag;
      for (const f of c.files) f.url = `https://github.com/${c.repository}/releases/download/${encodeURIComponent(tag)}/${encodeURIComponent(f.assetName)}`;
    });
    assert.equal(catalogStatus(null, input.policy, input.data).status, 'unavailable', tag);
  }
});
test('catalog retains canonical dotted repository names and encoded tag segments', () => {
  for (const tag of ['v1.2.3', 'feature/v1.2', '%2e%2e']) {
    const input = catalogInput(c => {
      c.repository = 'Owner.Name/Repo.Name'; c.release.tag = tag;
      for (const f of c.files) f.url = `https://github.com/${c.repository}/releases/download/${encodeURIComponent(tag)}/${encodeURIComponent(f.assetName)}`;
    });
    input.policy.repository = 'Owner.Name/Repo.Name';
    assert.equal(catalogStatus(null, input.policy, input.data).status, 'not-installed', tag);
    const parsed = new URL(JSON.parse(input.data).files[0].url);
    assert.equal(parsed.pathname.split('/').length, 7);
  }
});
test('module selection remains independent and invalid sources expose no package', () => {
  const first = catalogInput();
  const second = catalogInput(c => { c.moduleId = 'second'; c.branch = 'Second'; c.buildId = 'second-only'; });
  second.policy.moduleId = 'second'; second.policy.branch = 'Second';
  assert.equal(catalogStatus(null, second.policy, second.data).selectedBuildId, 'second-only');
  const wrong = catalogStatus(null, first.policy, second.data);
  assert.equal(wrong.status, 'unavailable'); assert.equal(wrong.catalog, undefined);
  assert.equal(catalogStatus(null, first.policy, first.data).selectedBuildId, 'opaque-b');
  const bad = Buffer.from('{');
  assert.equal(catalogStatus(null, { ...first.policy,
    catalogSha256: createHash('sha256').update(bad).digest('hex') }, bad).reason, 'invalid-catalog');
  assert.equal(catalogStatus(null, first.policy, first.data).catalog.files.length, 2);
});
test('catalog rejects incomplete, unpublished, mismatched and unsafe sources', () => {
  const edits = [c => { c.schema = 2; }, c => { c.moduleId = 'other'; },
    c => { c.repository = 'Other/Repo'; }, c => { c.branch = 'Other'; },
    c => { c.assembly = 'Other'; }, c => { c.workflow = 'other.yml'; },
    c => { c.channel = 'stable'; }, c => { c.devTool = false; },
    c => { c.release.draft = true; }, c => { c.release.publishedAt = null; },
    c => { c.sourceSha = 'latest'; }, c => { c.buildId = ''; },
    c => { c.files.pop(); }, c => { c.files.push({ ...c.files[0], path: 'extra.dll' }); },
    c => { c.files[1].path = 'EXAMPLE.dll'; }, c => { c.files[0].sha256 = ''; },
    c => { c.files[0].size = 0; }, c => { c.files[0].url += '?token=secret'; },
    c => { c.files[0].url = 'https://evil.example/Example.dll'; },
    c => { c.files[0].url = c.files[0].url.replace('example-dev-b', 'other'); },
    c => { c.files[0].assetId = -1; }, c => { c.release.id = 0; },
    c => { c.release.draft = undefined; }, c => { c.release.publishedAt = '2026-02-30T00:00:00Z'; },
    c => { c.files[0].size = 32 * 1024 * 1024 + 1; },
    c => { c.files[1].assetId = c.files[0].assetId; },
    c => { c.files[0].path = '../Example.dll'; }];
  for (const edit of edits) {
    const { data, policy } = catalogInput(edit);
    assert.equal(catalogStatus(null, policy, data).status, 'unavailable', String(edit));
  }
});
test('rejects traversal, Windows devices, alternate streams and ambiguous names', () => {
  for (const p of ['../x', '/x', 'C:/x', 'a\\b', 'x:stream', 'NUL.dll', 'a/../x', 'x.', 'x ', 'a//b'])
    assert.throws(() => safeRelative(p));
});
test('verifies all files and detects tampering', () => {
  assert.equal(verifyPackage(pkg()).length, 1);
  const p = pkg(); p.files[0].bytes = Buffer.from('changed');
  assert.throws(() => verifyPackage(p));
});
test('rejects case-insensitive duplicate destinations', () => {
  const p = pkg(); p.files.push({ ...p.files[0], path: 'MOD.dll' });
  assert.throws(() => verifyPackage(p));
});
test('does not overwrite a pre-existing user file', () => {
  assert.throws(() => installationPlan('D:\\Mods', pkg(), ['example/MOD.dll']));
  assert.equal(installationPlan('D:\\Mods', pkg())[0].destination, 'D:\\Mods\\example\\mod.dll');
});
test('uninstall retains modified files and rejects reparse points', () => {
  const record = { moduleId: 'example', files: [{ relative: 'example/mod.dll', sha256 }] };
  assert.deepEqual(uninstallPlan(record, { 'example/mod.dll': { sha256 } }), ['example/mod.dll']);
  assert.throws(() => uninstallPlan(record, { 'example/mod.dll': { sha256: 'changed' } }));
  assert.throws(() => uninstallPlan(record, { 'example/mod.dll': { sha256, reparsePoint: true } }));
  assert.throws(() => uninstallPlan({ ...record, moduleId: 'other' }, {}));
});
test('versions are compared per module without assuming numeric ordering', () => {
  assert.equal(updateStatus(pkg(), pkg()), 'current');
  assert.equal(updateStatus(pkg(), { moduleId: 'example', buildId: 'build-b' }), 'different-build');
  assert.throws(() => updateStatus(pkg(), { moduleId: 'other', buildId: 'a' }));
});
test('coexistence produces only a warning', () => {
  assert.equal(typeof coexistenceWarning(true, true), 'string');
  assert.equal(coexistenceWarning(true, false), null);
});
test('module identifiers cannot be Windows devices or ambiguous names', () => {
  for (const moduleId of ['CON', 'nul', 'com1', 'LPT9', 'a.', 'a ', 'a/b'])
    assert.throws(() => verifyPackage({ ...pkg(), moduleId }), moduleId);
});
test('rejects file versus directory overlap in either order', () => {
  for (const paths of [['a', 'a/b.dll'], ['A/b.dll', 'a']]) {
    const p = pkg(); p.files = paths.map(p => ({ path: p, bytes, sha256 }));
    assert.throws(() => verifyPackage(p));
  }
});
test('rejects invalid Windows characters and reserved device variants', () => {
  for (const name of ['a?.dll', 'a*.dll', 'a|b', 'a<b', 'COM¹.dll', 'CON .txt'])
    assert.throws(() => safeRelative(name), name);
});
test('ownership plans reject nested module identities and malformed hashes', () => {
  assert.throws(() => uninstallPlan({ moduleId: 'example/sub', files: [
    { relative: 'example/sub/mod.dll', sha256 }] }, { 'example/sub/mod.dll': { sha256 } }));
  assert.throws(() => uninstallPlan({ moduleId: 'example', files: [
    { relative: 'example/mod.dll', sha256: 'not-a-hash' }] },
  { 'example/mod.dll': { sha256: 'not-a-hash' } }));
});
