// Signature: OpenAI-Codex_workshop_implementation
import test from 'node:test';
import assert from 'node:assert/strict';
let lib;
try { lib = await import('./github.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}

test('the GitHub request client is implemented', () => {
  assert.ok(lib, 'the GitHub request client has not been implemented');
});

test('API requests target only the configured GitHub repository and refuse redirects', async () => {
  let observed;
  const api = new lib.GitHubClient('test-token', async (url, options) => {
    observed = {url, options}; return new Response('{"ok":true}', {status: 200});
  });
  assert.deepEqual(await api.request('GET', '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/17'), {ok:true});
  assert.equal(observed.url, 'https://api.github.com/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/17');
  assert.equal(observed.options.redirect, 'error');
  assert.equal(observed.options.headers.Authorization, 'Bearer test-token');
  for (const path of ['https://other.example/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/17',
    '//other.example/', '/repos/other/repo/issues/17', '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/../other',
    '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/17\nX: y'])
    await assert.rejects(api.request('GET', path));
});

test('remote error bodies and tokens are not included in thrown diagnostics', async () => {
  const api = new lib.GitHubClient('secret-value', async () => new Response('secret-value', {status: 403}));
  await assert.rejects(api.request('GET', '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/17'),
    error => error.status === 403 && !error.message.includes('secret-value'));
});

test('response size and malformed JSON are rejected', async () => {
  for (const response of [new Response('x'.repeat(4097), {status:200}),
    new Response('{bad', {status:200})]) {
    const api = new lib.GitHubClient('test-token', async () => response, 4096);
    await assert.rejects(api.request('GET', '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/17'));
  }
});

test('all issue event pages are read and a page limit fails closed', async () => {
  const calls = [];
  const api = {request: async (_method, path) => {
    calls.push(path); const page = Number(new URL(`https://api.github.com${path}`).searchParams.get('page'));
    return page === 1 ? Array.from({length:100}, (_, i) => ({id:i+1})) : [{id:101}];
  }};
  assert.equal((await lib.listIssuePages(api, 17, 'events')).length, 101);
  assert.equal(calls.length, 2);
  const alwaysFull = {request: async () => Array.from({length:100}, (_, i) => ({id:i+1}))};
  await assert.rejects(lib.listIssuePages(alwaysFull, 17, 'events', 2));
  await assert.rejects(lib.listIssuePages(api, 17, '../../bad'));
});

test('the latest relevant label event must be a current owner action', () => {
  const label = name => ({name});
  const events = [
    {id:10,event:'labeled',created_at:'2026-11-04T00:00:01Z',label:label('steam-workshop-release-approved'),actor:{id:157894869}},
    {id:11,event:'unlabeled',created_at:'2026-11-04T00:00:02Z',label:label('steam-workshop-release-approved'),actor:{id:157894869}},
    {id:12,event:'labeled',created_at:'2026-11-04T00:00:03Z',label:label('steam-workshop-release-approved'),actor:{id:157894869}},
    {id:13,event:'labeled',label:label('other'),actor:{id:1}},
  ];
  assert.equal(lib.latestOwnerApproval(events).id, 12);
  assert.throws(() => lib.latestOwnerApproval(events.slice(0, 2)));
  assert.throws(() => lib.latestOwnerApproval([...events,{id:14,event:'labeled',created_at:'2026-11-04T00:00:04Z',label:label('steam-workshop-release-approved'),actor:{id:1}}]));
  assert.throws(() => lib.latestOwnerApproval([...events,events[0]]));
});

test('approval uses event timestamps, rejects a newer non-owner, and fails closed on a tie', () => {
  const label = {name:'steam-workshop-release-approved'};
  const oldOwner = {id:21,event:'labeled',created_at:'2026-11-04T00:00:01Z',label,actor:{id:157894869}};
  const newerOther = {id:22,event:'labeled',created_at:'2026-11-04T00:00:02Z',label,actor:{id:1}};
  assert.throws(() => lib.latestOwnerApproval([newerOther,oldOwner]));
  assert.throws(() => lib.latestOwnerApproval([oldOwner,{...newerOther,created_at:oldOwner.created_at}]));
  const newestOwner = {...oldOwner,id:23,created_at:'2026-11-04T00:00:03Z'};
  assert.equal(lib.latestOwnerApproval([newestOwner,newerOther,oldOwner]).id, 23);
});

// Additional validation: OpenAI-Codex.
