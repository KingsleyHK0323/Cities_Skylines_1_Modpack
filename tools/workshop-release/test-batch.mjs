// Signature: OpenAI-Codex.
import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {registry} from './request.mjs';
let batch;
try { batch = await import('./batch.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}

function selection(keys) {
  return {schema:1, repository:'KingsleyHK0323/Cities_Skylines_1_Modpack', issueNumber:17,
    modules:keys.map(key => {
      const item = registry.find(entry => entry.key === key);
      return {key:item.key, branch:item.branch, assembly:item.assembly,
        workshopId:item.workshopId, sourceSha:item.reviewedSource};
    })};
}

test('the batch plan preserves each selected module and its exact reviewed source', () => {
  assert.ok(batch, 'the batch plan is not implemented');
  const result = batch.createBuildPlan(selection(['AI_Improve', 'Wireless_Utilities_And_Global_Coverage']));
  assert.deepEqual(result, [
    {module:'AI_Improve', sourceSha:'0fd60bcdd986e6e2ae40d162c326f1232c1bf639'},
    {module:'Wireless_Utilities_And_Global_Coverage', sourceSha:'fab5d2bfa27ce03981de3a845cf4ee40148a1201'},
  ]);
});

test('a duplicate, changed source, wrong target or unreviewed module blocks the complete batch', () => {
  assert.ok(batch, 'the batch plan is not implemented');
  const original = selection(['AI_Improve', 'Wireless_Utilities_And_Global_Coverage']);
  for (const mutate of [
    x => x.modules.push({...x.modules[0]}),
    x => {x.modules[1].sourceSha = 'a'.repeat(40);},
    x => {x.modules[1].workshopId = x.modules[0].workshopId;},
    x => {x.modules[1].branch = '../unexpected';},
    x => {x.repository = 'someone/else';},
    x => {x.modules = [];},
  ]) {
    const changed = structuredClone(original); mutate(changed);
    assert.throws(() => batch.createBuildPlan(changed));
  }
});

test('approved source history is not fetched shallow because AI Improve derives its version from commit count', () => {
  const script = readFileSync(new URL('./Build-Selected.ps1', import.meta.url), 'utf8');
  const fetchLines = script.split(/\r?\n/).filter(line => /\bgit\b.*\bfetch\b/.test(line));
  assert.equal(fetchLines.length, 1);
  assert.doesNotMatch(fetchLines[0], /--depth|--shallow|--filter=shallow/);
});
