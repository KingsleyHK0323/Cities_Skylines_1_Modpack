// Signature: OpenAI-Codex.
import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {registry} from './request.mjs';

test('the unvalidated candidate cannot schedule a self-hosted upload', () => {
  const workflow = readFileSync(new URL('../../.github/workflows/workshop-release-request.yml', import.meta.url), 'utf8');
  const section = workflow.split(/\r?\n  upload:\r?\n/)[1]?.split(/\r?\n  report:\r?\n/)[0];
  assert.ok(section, 'upload job is missing');
  assert.match(section, /^    if: \$\{\{ false \}\}\s*$/m);
  assert.ok(registry.every(item => item.releaseReady === false));
});
