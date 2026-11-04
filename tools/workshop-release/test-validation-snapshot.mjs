// Signature: OpenAI-Codex.
import test from 'node:test';
import assert from 'node:assert/strict';
import {createBuildPlan} from './batch.mjs';
let validation;
try { validation = await import('./validation-snapshot.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}

test('the cloud-only validation fixture selects two reviewed modules without authorizing an upload', () => {
  assert.ok(validation, 'the validation snapshot is not implemented');
  const fixture = validation.createValidationSnapshot();
  assert.equal(fixture.validationOnly, true);
  assert.deepEqual(createBuildPlan(fixture).map(item => item.module),
    ['AI_Improve','Wireless_Utilities_And_Global_Coverage']);
});
