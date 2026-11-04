// Signature: OpenAI-Codex_workshop_implementation
import test from 'node:test';
import assert from 'node:assert/strict';
let upload;
try { upload = await import('./upload.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}
test('the upload sequencer is implemented', () => {
  assert.ok(upload, 'the upload sequencer has not been implemented');
});

test('checks the package and authorization twice around one Steam owner preflight', async () => {
  const calls = [];
  const result = await upload.submitAfterChecks({
    requireReady:async () => true,
    verifyPackage:async () => {calls.push('package'); return 'a'.repeat(64);},
    recheck:async () => {calls.push('approval'); return true;},
    steamPreflight:async () => {calls.push('steam'); return true;},
    prepare:async () => {calls.push('prepare'); return 'ready';},
    submit:async prepared => {assert.equal(prepared, 'ready'); calls.push('submit'); return 0;},
  });
  assert.deepEqual(calls, ['approval','package','steam','approval','package','prepare','submit']);
  assert.equal(result.status, 'unknown');
  assert.equal(result.attempted, true);
});

test('a stale authorization, unverifiable Steam item or changed package never submits', async () => {
  for (const phase of ['firstApproval','firstPackage','steam','secondApproval','secondPackage']) {
    let approvalCount = 0, packageCount = 0, submitCount = 0;
    const deps = {
      requireReady:async () => true,
      recheck:async () => {approvalCount++; return phase === (approvalCount === 1 ? 'firstApproval' : 'secondApproval') ? false : true;},
      verifyPackage:async () => {packageCount++; return phase === (packageCount === 1 ? 'firstPackage' : 'secondPackage') ? 'b'.repeat(64) : 'a'.repeat(64);},
      steamPreflight:async () => phase !== 'steam',
      prepare:async () => true,
      submit:async () => {submitCount++; return 0;},
    };
    await assert.rejects(upload.submitAfterChecks(deps));
    assert.equal(submitCount, 0);
  }
});

test('a failed or interrupted submit is unknown and is not retried', async () => {
  let count = 0;
  const result = await upload.submitAfterChecks({
    requireReady:async () => true,
    verifyPackage:async () => 'a'.repeat(64), recheck:async () => true, steamPreflight:async () => true,
    prepare:async () => true,
    submit:async () => {count++; throw new Error('connection interrupted');},
  });
  assert.equal(result.status, 'unknown');
  assert.equal(count, 1);
});

test('a configuration or VDF preparation failure is known to precede submission', async () => {
  let count = 0;
  await assert.rejects(upload.submitAfterChecks({
    requireReady:async () => true,
    verifyPackage:async () => 'a'.repeat(64), recheck:async () => true, steamPreflight:async () => true,
    prepare:async () => {throw new Error('invalid SteamCMD configuration');},
    submit:async () => {count++;},
  }), /before SteamCMD/i);
  assert.equal(count, 0);
});

test('SteamCMD does not inherit GitHub tokens or unrelated runner environment', () => {
  const env = upload.steamProcessEnvironment({SystemRoot:'C:\\Windows',PATH:'C:\\Windows',
    GH_TOKEN:'secret',GITHUB_TOKEN:'secret',CS1_GAME_REFERENCES_KEY:'secret',
    STEAMCMD_USERNAME:'private',RANDOM_VAR:'do not pass'});
  assert.deepEqual(env, {SystemRoot:'C:\\Windows',PATH:'C:\\Windows'});
});

test('an unready selection stops before any external check or submission', async () => {
  let touched = false;
  await assert.rejects(upload.submitAfterChecks({
    requireReady:async () => false,
    recheck:async () => {touched = true; return true;},
    verifyPackage:async () => 'a'.repeat(64), steamPreflight:async () => true,
    prepare:async () => true, submit:async () => true,
  }), /not ready/i);
  assert.equal(touched, false);
});

test('Steam VDF uses the selected registered ID and rejects cross-module targets', () => {
  const ai = upload.renderSteamVdf('AI_Improve', '3782858610', 'D:\\temp\\ai', 'Update');
  const wireless = upload.renderSteamVdf('Wireless_Utilities_And_Global_Coverage', '3832301115', 'D:\\temp\\wireless', 'Update');
  assert.match(ai, /"publishedfileid" "3782858610"/);
  assert.match(wireless, /"publishedfileid" "3832301115"/);
  assert.throws(() => upload.renderSteamVdf('Wireless_Utilities_And_Global_Coverage', '3782858610', 'D:\\temp\\wireless', 'Update'));
  assert.throws(() => upload.renderSteamVdf('AI_Gaming', '3823740106', 'D:\\temp\\gaming', 'Update'));
});

// Additional validation: OpenAI-Codex.
