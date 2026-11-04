// Signature: OpenAI-Codex_workshop_implementation
import test from 'node:test';
import assert from 'node:assert/strict';
import {registry, createPreview, renderPreview} from './request.mjs';
let orchestration;
try { orchestration = await import('./orchestration.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}
test('the issue orchestration adapter is implemented', () => {
  assert.ok(orchestration, 'the issue orchestration adapter has not been implemented');
});

const prefix = '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack';
function fixture() {
  const body = '### Modules\n\n' + registry.map(item =>
    `- [${item.key === 'AI_Improve' ? 'X' : ' '}] ${item.label}`).join('\n') + '\n\n### Proposed Workshop change note\n\nTest.\n';
  const issue = {number:17, state:'open', title:'[Workshop update]: test', body,
    updated_at:'2026-11-04T04:00:00Z', labels:[{name:'steam-workshop-release-approved'}]};
  const event = {action:'labeled', sender:{id:157894869,login:'KingsleyHK0323'},
    repository:{id:1425203074,full_name:'KingsleyHK0323/Cities_Skylines_1_Modpack',
      owner:{id:157894869,login:'KingsleyHK0323'}}, issue:structuredClone(issue),
    label:{name:'steam-workshop-release-approved'}};
  const approval = {id:12345,event:'labeled',actor:{id:157894869},
    label:{name:'steam-workshop-release-approved'},created_at:'2026-11-04T04:00:00Z'};
  const heads = {AI_Improve:'a'.repeat(40)};
  const preview = createPreview(issue, heads);
  const comment = {id:77,user:{id:41898282,login:'github-actions[bot]',type:'Bot'},
    created_at:'2026-11-04T03:59:00Z',updated_at:'2026-11-04T03:59:00Z',body:renderPreview(preview)};
  const state = {issue,event,approval,heads,comment,lastEditedAt:null};
  const api = {request:async (method,path,body) => {
    if (method === 'GET' && path === `${prefix}/issues/17`) return structuredClone(state.issue);
    if (method === 'GET' && path === `${prefix}/issues/17/events?per_page=100&page=1`) return [structuredClone(state.approval)];
    if (method === 'GET' && path === `${prefix}/issues/17/comments?per_page=100&page=1`) return [structuredClone(state.comment)];
    if (method === 'GET' && path === `${prefix}/git/ref/heads/AI_Improve`)
      return {ref:'refs/heads/AI_Improve',object:{type:'commit',sha:state.heads.AI_Improve}};
    if (method === 'POST' && path === '/graphql') return {data:{repository:{issue:{number:17,
      body:state.issue.body,title:state.issue.title,lastEditedAt:state.lastEditedAt}}}};
    throw new Error(`unexpected request ${method} ${path}`);
  }};
  return {state,api};
}

test('only a reviewed bot preview of the exact current source can form an approval snapshot', async () => {
  const f = fixture();
  const snapshot = await orchestration.reviewedSnapshot(f.api, f.state.event, 1);
  assert.equal(snapshot.issueNumber, 17);
  assert.equal(snapshot.labelEventId, 12345);
  assert.equal(snapshot.modules[0].sourceSha, 'a'.repeat(40));
});

test('body edit, source movement, edited preview and same-second edits invalidate approval', async () => {
  for (const mutate of [
    f => { f.state.issue.body += ' changed'; },
    f => { f.state.heads.AI_Improve = 'b'.repeat(40); },
    f => { f.state.comment.updated_at = '2026-11-04T03:59:01Z'; },
    f => { f.state.lastEditedAt = '2026-11-04T04:00:00Z'; },
    f => { f.state.approval.actor.id = 1; },
  ]) {
    const f = fixture(); mutate(f);
    await assert.rejects(orchestration.reviewedSnapshot(f.api, f.state.event, 1));
  }
});
