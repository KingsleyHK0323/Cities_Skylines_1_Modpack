// Signature: OpenAI-Codex_workshop_implementation
// Updated by: OpenAI-Codex.
import test from 'node:test';
import assert from 'node:assert/strict';

let controller;
try { controller = await import('./request.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}

test('the release request controller is available', () => {
  assert.ok(controller, 'the release request controller has not been implemented');
});

const items = [
  ['AI Improve', 'AI_Improve'],
  ['Automated Public Transport Planning', 'Automated_Public_Transport_Planning'],
  ['Automated Public Transport Vehicle Management', 'Automated_Public_Transport_Vehicle_Management'],
  ['Full Map Unlock And Expansion', 'Full_Map_Unlock_And_Expansion'],
  ['Intercity Resource Sharing', 'Intercity_Resource_Sharing'],
  ['Realistic Border Control', 'Realistic_Border_Control'],
  ['Realistic City First Response', 'Realistic_City_First_Response'],
  ['Realistic City Time', 'Realistic_City_Time'],
  ['Realistic Import Export Control', 'Realistic_Import_Export_Control'],
  ['Realistic Pedestrian Traffic Control', 'Realistic_Pedestrian_Traffic_Control'],
  ['Realistic Railway Traffic Control', 'Realistic_Railway_Traffic_Control'],
  ['Realistic Urban Population', 'Realistic_Urban_Population'],
  ['Realistic Vehicle Traffic Control', 'Realistic_Vehicle_Traffic_Control'],
  ['Wireless Utilities And Global Coverage', 'Wireless_Utilities_And_Global_Coverage'],
  ['AI Gaming (paused)', 'AI_Gaming'],
];
const body = selected => '### Modules\n\n' + items.map(([name]) =>
  `- [${selected.includes(name) ? 'X' : ' '}] ${name}`).join('\n') +
  '\n\n### Proposed Workshop change note\n\nExample notes.\n';

const fixture = () => {
  const issue = { number: 17, state: 'open', title: '[Workshop update]: test',
    body: body(['AI Improve', 'Automated Public Transport Planning']),
    updated_at: '2026-11-04T04:00:00Z', labels: [{ name: 'steam-workshop-release-approved' }] };
  const event = { action: 'labeled', sender: { id: 157894869, login: 'KingsleyHK0323' },
    repository: { id: 1425203074, full_name: 'KingsleyHK0323/Cities_Skylines_1_Modpack',
      owner: { id: 157894869, login: 'KingsleyHK0323' } },
    issue: structuredClone(issue), label: { name: 'steam-workshop-release-approved' } };
  const label = { id: 12345, event: 'labeled', actor: { id: 157894869 },
    label: { name: 'steam-workshop-release-approved' }, created_at: '2026-11-04T04:00:00Z' };
  const heads = { AI_Improve: 'a'.repeat(40), Automated_Public_Transport_Planning: 'b'.repeat(40) };
  return { event, issue, label, heads, attempt: 1 };
};

test('selection resolves one or several registered modules only', () => {
  assert.deepEqual(controller.parseSelection(body(['AI Improve'])), ['AI_Improve']);
  assert.deepEqual(controller.parseSelection(body(['AI Improve', 'Realistic City Time'])),
    ['AI_Improve', 'Realistic_City_Time']);
});

test('missing, unknown, duplicated, malformed, empty or paused selections fail closed', () => {
  const cases = [
    body([]), body(['AI Gaming (paused)']),
    body(['AI Improve']).replace('- [ ] Realistic City Time', '- [X] ../../unexpected'),
    body(['AI Improve']).replace('- [ ] Realistic City Time', '- [ ] AI Improve'),
    body(['AI Improve']).replace('- [ ] Realistic City Time\n', ''),
    body(['AI Improve']).replace('- [X] AI Improve', '- [yes] AI Improve'),
    body(['AI Improve']) + '\n### Modules\n\n- [X] AI Improve\n',
    'x'.repeat(66000),
  ];
  for (const text of cases) assert.throws(() => controller.parseSelection(text));
});

test('the published change note is a single bounded VDF-safe English line', () => {
  assert.equal(controller.parseChangeNote(body(['AI Improve'])), 'Example notes.');
  for (const text of ['bad\nnext line', 'bad"quote', 'bad\\path', '', 'x'.repeat(241), 'bad\u0001control']) {
    const candidate = body(['AI Improve']).replace('Example notes.', text);
    assert.throws(() => controller.parseChangeNote(candidate));
  }
});

test('only the repository owner label event authorizes an immutable source snapshot', () => {
  const f = fixture();
  const snapshot = controller.createSnapshot(f);
  assert.equal(snapshot.issueNumber, 17);
  assert.equal(snapshot.labelEventId, 12345);
  assert.deepEqual(snapshot.modules.map(x => [x.key, x.sourceSha]), [
    ['AI_Improve', 'a'.repeat(40)],
    ['Automated_Public_Transport_Planning', 'b'.repeat(40)],
  ]);
  assert.equal(snapshot.modules[0].workshopId, '3782858610');
  assert.equal(snapshot.modules[1].workshopId, '3823739383');
  assert.match(snapshot.bodySha256, /^[0-9a-f]{64}$/);
});

test('rejects spoofed owner, wrong repository, stale label, reruns and malformed heads', () => {
  const cases = [
    f => { f.event.sender.id = 1; },
    f => { f.event.repository.id = 1; },
    f => { f.event.repository.owner.id = 1; },
    f => { f.event.action = 'opened'; },
    f => { f.event.label.name = 'another-label'; },
    f => { f.label.actor.id = 1; },
    f => { f.label.event = 'unlabeled'; },
    f => { f.issue.labels = []; },
    f => { f.issue.state = 'closed'; },
    f => { f.issue.updated_at = '2026-11-04T04:01:00Z'; },
    f => { f.issue.body += 'changed'; },
    f => { f.issue.title += 'changed'; },
    f => { f.issue.number = 18; },
    f => { f.attempt = 2; },
    f => { f.heads.AI_Improve = 'not-a-sha'; },
    f => { delete f.heads.AI_Improve; },
  ];
  for (const mutate of cases) {
    const f = fixture(); mutate(f); assert.throws(() => controller.createSnapshot(f));
  }
});

test('approval becomes invalid after any branch, body, title, label or issue timestamp changes', () => {
  const f = fixture(); const snapshot = controller.createSnapshot(f);
  assert.equal(controller.verifySnapshot(snapshot, f), true);
  for (const mutate of [
    x => { x.heads.AI_Improve = 'c'.repeat(40); },
    x => { x.issue.body += 'changed'; },
    x => { x.issue.title += 'changed'; },
    x => { x.issue.labels = []; },
    x => { x.issue.updated_at = '2026-11-04T04:01:00Z'; },
    x => { x.label.id = 12346; },
  ]) {
    const changed = fixture(); mutate(changed);
    assert.throws(() => controller.verifySnapshot(snapshot, changed));
  }
});

test('screenshot-derived mappings do not make an unreviewed module release-ready', () => {
  const snapshot = controller.createSnapshot(fixture());
  assert.throws(() => controller.requireReady(snapshot), /not ready/i);
});

test('reviewed AI Improve and Wireless can be selected together without crossing their source identities', () => {
  const f = fixture();
  f.issue.body = body(['AI Improve']); f.event.issue.body = f.issue.body;
  f.heads.AI_Improve = '0fd60bcdd986e6e2ae40d162c326f1232c1bf639';
  const single = controller.createSnapshot(f);
  assert.equal(controller.requireBuildable(single), true);
  const unsupported = fixture();
  unsupported.heads.AI_Improve = '0fd60bcdd986e6e2ae40d162c326f1232c1bf639';
  const multi = controller.createSnapshot(unsupported);
  assert.throws(() => controller.requireBuildable(multi), /not implemented/i);
  f.issue.body = body(['AI Improve','Wireless Utilities And Global Coverage']); f.event.issue.body = f.issue.body;
  f.heads.Wireless_Utilities_And_Global_Coverage = 'fab5d2bfa27ce03981de3a845cf4ee40148a1201';
  const selected = controller.createSnapshot(f);
  assert.equal(controller.requireBuildable(selected), true);
  f.heads.Wireless_Utilities_And_Global_Coverage = 'c'.repeat(40);
  assert.throws(() => controller.requireBuildable(controller.createSnapshot(f)), /review/i);
});

test('Steam metadata must match the exact registered item, creator and game', () => {
  const expected = { id: '3782858610', creator: '76561198000000000', appId: 255710 };
  const actual = { publishedfileid: '3782858610', creator: '76561198000000000',
    consumer_app_id: 255710, result: 1 };
  assert.equal(controller.verifySteamItem(expected, actual), true);
  for (const update of [
    {publishedfileid: '3823739383'}, {creator: '1'}, {consumer_app_id: 42},
    {result: 9}, {publishedfileid: 3782858610},
  ]) assert.throws(() => controller.verifySteamItem(expected, {...actual, ...update}));
});

test('the known Development Preview source is shown as blocked before an approval can start a build', () => {
  const f = fixture();
  f.issue.body = body(['Full Map Unlock And Expansion']); f.event.issue.body = f.issue.body;
  f.heads.Full_Map_Unlock_And_Expansion = 'd9daec9217b454f8dd8d751bcf480fb5d6df8dc9';
  const preview = controller.createPreview(f.issue, f.heads);
  assert.match(controller.renderPreview(preview), /blocked.*Development Preview/i);
  assert.throws(() => controller.requireBuildable(controller.createSnapshot(f)), /Development Preview/i);
});

test('preview presents exact module choices and full source SHAs before approval', () => {
  const f = fixture();
  const preview = controller.createPreview(f.issue, f.heads);
  const rendered = controller.renderPreview(preview);
  assert.match(rendered, /AI_Improve/);
  assert.match(rendered, new RegExp('a'.repeat(40)));
  assert.match(rendered, /Automated_Public_Transport_Planning/);
  assert.match(rendered, new RegExp('b'.repeat(40)));
  assert.match(rendered, /not yet authorized/i);
  const comment = {id: 4, user: {id: 41898282, login: 'github-actions[bot]', type: 'Bot'},
    created_at: '2026-11-04T03:59:00Z', updated_at: '2026-11-04T03:59:00Z', body: rendered};
  assert.equal(controller.verifyPreview(preview, comment, f), true);
  for (const mutate of [
    c => { c.user.id = 1; }, c => { c.updated_at = '2026-11-04T03:59:01Z'; },
    c => { c.body += 'tampered'; },
    c => { c.created_at = '2026-11-04T04:00:00Z'; c.updated_at = c.created_at; },
  ]) {
    const changed = structuredClone(comment); mutate(changed);
    assert.throws(() => controller.verifyPreview(preview, changed, f));
  }
  const changed = fixture(); changed.heads.AI_Improve = 'c'.repeat(40);
  assert.throws(() => controller.verifyPreview(preview, comment, changed));
});

test('the APTP preview reports its missing reviewed batch recipe accurately', () => {
  const f = fixture();
  const preview = controller.createPreview(f.issue, f.heads);
  const rendered = controller.renderPreview(preview);
  assert.match(rendered, /blocked: no reviewed batch recipe for the updated release preview/i);
  assert.doesNotMatch(rendered, /DevTools=false also excludes core planning/i);
});
