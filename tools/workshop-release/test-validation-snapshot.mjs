// Signature: OpenAI-Codex.
import test from 'node:test';
import assert from 'node:assert/strict';
import {createBuildPlan} from './batch.mjs';
import {requireReady} from './request.mjs';
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

test('the APTP validation fixture is separate and cannot authorize a Steam upload', () => {
  const fixture = validation.createAptpValidationSnapshot();
  assert.equal(fixture.validationOnly, true);
  assert.deepEqual(createBuildPlan(fixture).map(item => item.module), ['Automated_Public_Transport_Planning']);
  assert.throws(() => requireReady(fixture), /validation-only/i);
  assert.throws(() => requireReady({...fixture, validationOnly:false}), /validation-only/i);
});
