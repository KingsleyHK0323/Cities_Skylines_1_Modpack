// Signature: OpenAI-Codex_workshop_implementation
import test from 'node:test';
import assert from 'node:assert/strict';
let ledger;
try { ledger = await import('./ledger.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}

test('the durable approval ledger is implemented', () => {
  assert.ok(ledger, 'the durable approval ledger has not been implemented');
});

const oldHead = 'a'.repeat(40);
const newHead = 'b'.repeat(40);
const tree = 'c'.repeat(40);
const blob = 'd'.repeat(40);
const newTree = 'e'.repeat(40);
const prefix = '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack';
const sample = { schema: 1, repository: 'KingsleyHK0323/Cities_Skylines_1_Modpack',
  issueNumber: 17, labelEventId: 12345, bodySha256: 'a'.repeat(64),
  modules: [{key: 'AI_Improve', sourceSha: 'b'.repeat(40)}] };

function fakeApi(options = {}) {
  let head = oldHead;
  let claim = null;
  let blobText;
  return {
    async request(method, path, body) {
      if (method === 'GET' && path === `${prefix}/git/ref/heads/Workshop_Release_Ledger`) {
        if (options.missingBranch) throw Object.assign(new Error('missing'), {status: 404});
        return {object: {sha: head, type: 'commit'}};
      }
      if (method === 'GET' && path === `${prefix}/git/commits/${oldHead}`)
        return {sha: oldHead, tree: {sha: tree}};
      if (method === 'GET' && path === `${prefix}/contents/workshop-release-ledger.json?ref=${head}`) {
        const marker = options.badMarker ? {} : {schema: 1, repository: sample.repository, purpose: 'workshop-release-claims'};
        return {type: 'file', encoding: 'base64', content: Buffer.from(JSON.stringify(marker)).toString('base64')};
      }
      if (method === 'GET' && path.startsWith(`${prefix}/contents/claims/issue-17.json?ref=`)) {
        if (!claim) throw Object.assign(new Error('not found'), {status: 404});
        return {type: 'file', encoding: 'base64', content: Buffer.from(claim).toString('base64')};
      }
      if (method === 'POST' && path === `${prefix}/git/blobs`) {
        assert.equal(body.encoding, 'utf-8'); blobText = body.content; return {sha: blob};
      }
      if (method === 'POST' && path === `${prefix}/git/trees`) {
        assert.equal(body.base_tree, tree);
        assert.deepEqual(body.tree, [{path: 'claims/issue-17.json', mode: '100644', type: 'blob', sha: blob}]);
        return {sha: newTree};
      }
      if (method === 'POST' && path === `${prefix}/git/commits`) {
        assert.deepEqual(body.parents, [oldHead]); assert.equal(body.tree, newTree);
        return {sha: newHead};
      }
      if (method === 'PATCH' && path === `${prefix}/git/refs/heads/Workshop_Release_Ledger`) {
        assert.equal(body.force, false); assert.equal(body.sha, newHead);
        if (options.race) throw Object.assign(new Error('not fast forward'), {status: 422});
        head = newHead; claim = blobText; return {object: {sha: newHead, type: 'commit'}};
      }
      throw new Error(`unexpected API request ${method} ${path}`);
    },
    get claim() { return claim; },
    get head() { return head; },
  };
}

test('claims an issue once using a non-force commit parented to the observed ledger head', async () => {
  const api = fakeApi();
  assert.equal(await ledger.claimOnce(api, sample, 700, 'f'.repeat(40)), newHead);
  const stored = JSON.parse(api.claim);
  assert.equal(stored.issueNumber, 17);
  assert.equal(stored.runId, 700);
  assert.equal(stored.labelEventId, 12345);
  assert.equal(stored.bodySha256, sample.bodySha256);
  assert.equal(stored.controllerSha, 'f'.repeat(40));
});

test('a missing or uninitialized ledger is never recreated automatically', async () => {
  for (const options of [{missingBranch: true}, {badMarker: true}])
    await assert.rejects(ledger.claimOnce(fakeApi(options), sample, 700, 'f'.repeat(40)));
});

test('a non-fast-forward claim conflict stops rather than overwriting or retrying', async () => {
  const api = fakeApi({race: true});
  await assert.rejects(ledger.claimOnce(api, sample, 700, 'f'.repeat(40)));
  assert.equal(api.claim, null);
  assert.equal(api.head, oldHead);
});

test('an existing issue claim is never reused, even with a fresh label event', async () => {
  const api = fakeApi();
  await ledger.claimOnce(api, sample, 700, 'f'.repeat(40));
  await assert.rejects(ledger.claimOnce(api, {...sample, labelEventId: 45678}, 701, 'f'.repeat(40)));
});

test('the uploader can only proceed with the same issue, run, controller and source claim', async () => {
  const api = fakeApi();
  await ledger.claimOnce(api, sample, 700, 'f'.repeat(40));
  assert.equal(await ledger.verifyClaim(api, sample, 700, 'f'.repeat(40)), true);
  await assert.rejects(ledger.verifyClaim(api, sample, 701, 'f'.repeat(40)));
  await assert.rejects(ledger.verifyClaim(api, {...sample, modules:[{key:'AI_Improve',sourceSha:'c'.repeat(40)}]}, 700, 'f'.repeat(40)));
});
