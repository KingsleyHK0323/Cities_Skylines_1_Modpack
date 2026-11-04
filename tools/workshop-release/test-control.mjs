// Signature: OpenAI-Codex_workshop_implementation
import test from 'node:test';
import assert from 'node:assert/strict';
let control;
try { control = await import('./control.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}
test('workflow control is implemented', () => {
  assert.ok(control, 'workflow control has not been implemented');
});

test('reports a blocked build as not uploaded', () => {
  const report = control.formatReport({modules:[{key:'AI_Improve'}]},
    {authorization:'success',build:'failure',upload:'skipped'});
  assert.match(report, /AI_Improve/);
  assert.match(report, /not submitted/i);
  assert.doesNotMatch(report, /upload succeeded/i);
});

test('a started or interrupted Steam submission is unknown, never an automatic retry', () => {
  for (const status of ['failure','cancelled','success']) {
    const report = control.formatReport({modules:[{key:'AI_Improve'}]},
      {authorization:'success',build:'success',upload:status});
    assert.match(report, /unknown/i);
    assert.match(report, /do not retry/i);
    assert.doesNotMatch(report, /upload succeeded/i);
  }
});

test('reports cannot include unregistered issue-provided module text', () => {
  assert.throws(() => control.formatReport({modules:[{key:'AI_Improve\nmalicious'}]},
    {authorization:'success',build:'success',upload:'success'}));
});

test('a skipped batch does not falsely say a reviewed module has no recipe', () => {
  const report = control.formatReport({modules:[{key:'Wireless_Utilities_And_Global_Coverage'}]},
    {authorization:'success',build:'skipped',upload:'skipped'});
  assert.match(report, /not submitted/i);
  assert.doesNotMatch(report, /no reviewed cloud release recipe/i);
});

test('the report distinguishes an unknown submission from modules not attempted', () => {
  const snapshot = {modules:[{key:'AI_Improve'},{key:'Wireless_Utilities_And_Global_Coverage'}]};
  const report = control.formatReport(snapshot, {authorization:'success',build:'success',upload:'success'}, [
    {module:'AI_Improve',status:'unknown'},
    {module:'Wireless_Utilities_And_Global_Coverage',status:'not-attempted'},
  ]);
  assert.match(report, /AI_Improve:.*unknown/i);
  assert.match(report, /Wireless_Utilities_And_Global_Coverage:.*not submitted/i);
  assert.throws(() => control.formatReport(snapshot, {authorization:'success',build:'success',upload:'success'}, [
    {module:'AI_Improve',status:'success'},
    {module:'Wireless_Utilities_And_Global_Coverage',status:'not-attempted'},
  ]));
});

// Additional validation: OpenAI-Codex.
