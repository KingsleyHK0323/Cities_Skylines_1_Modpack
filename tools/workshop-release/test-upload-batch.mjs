// Signature: OpenAI-Codex.
import test from 'node:test';
import assert from 'node:assert/strict';
let batch;
try { batch = await import('./upload-batch.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}

const selected = ['AI_Improve','Wireless_Utilities_And_Global_Coverage'];

test('every selected package and Steam target is checked before any submit', async () => {
  assert.ok(batch, 'the batch uploader is not implemented');
  const events = [];
  const result = await batch.uploadSelected({modules:selected, requireReady:async () => true,
    recheck:async () => true,
    verify:async key => {events.push(`verify:${key}`); return key === selected[0] ? 'a'.repeat(64) : 'b'.repeat(64);},
    preflight:async key => {events.push(`steam:${key}`); return true;},
    submit:async key => {events.push(`submit:${key}`); return {status:'unknown'};}});
  assert.deepEqual(events.slice(0, 4), [`verify:${selected[0]}`, `steam:${selected[0]}`, `verify:${selected[1]}`, `steam:${selected[1]}`]);
  assert.equal(events.filter(item => item.startsWith('submit:')).length, 1);
  assert.deepEqual(result, [{module:selected[0],status:'unknown'}, {module:selected[1],status:'not-attempted'}]);
});

test('an invalid package, failed preflight, stale approval or unready module submits nothing', async () => {
  assert.ok(batch, 'the batch uploader is not implemented');
  for (const failure of ['ready','approval','package','steam']) {
    let submitted = 0;
    await assert.rejects(batch.uploadSelected({modules:selected,
      requireReady:async () => failure !== 'ready', recheck:async () => failure !== 'approval',
      verify:async key => failure === 'package' && key === selected[1] ? 'bad' : 'a'.repeat(64),
      preflight:async key => !(failure === 'steam' && key === selected[1]),
      submit:async () => {submitted++; return {status:'unknown'};}}));
    assert.equal(submitted, 0);
  }
});

test('a changed package blocks submission after the initial batch preflight', async () => {
  assert.ok(batch, 'the batch uploader is not implemented');
  let checks = 0, submitted = 0;
  await assert.rejects(batch.uploadSelected({modules:[selected[0]], requireReady:async () => true,
    recheck:async () => true,
    verify:async () => (++checks === 1 ? 'a' : 'b').repeat(64), preflight:async () => true,
    submit:async () => {submitted++; return {status:'unknown'};}}));
  assert.equal(submitted, 0);
});
