// Signature: OpenAI-Codex_workshop_implementation
// These mappings come from the owner's dated screenshots, not live Steam checks.
import { createHash } from 'node:crypto';
import { recipes as reviewedRecipes } from './recipes.mjs';

const LABEL = 'steam-workshop-release-approved';
const OWNER_ID = 157894869;
const REPOSITORY_ID = 1425203074;
const REPOSITORY = 'KingsleyHK0323/Cities_Skylines_1_Modpack';
const rows = [
  ['AI Improve', 'AI_Improve', 'AIImprove', '3782858610', '0fd60bcdd986e6e2ae40d162c326f1232c1bf639'],
  ['Automated Public Transport Planning', 'Automated_Public_Transport_Planning', 'AutomatedPublicTransportPlanning', '3823739383'],
  ['Automated Public Transport Vehicle Management', 'Automated_Public_Transport_Vehicle_Management', 'AutomatedPublicTransportVehicleManagement', '3832298237', 'afeeb21571a7ce5a496b892a4e55388d9463c968'],
  ['Full Map Unlock And Expansion', 'Full_Map_Unlock_And_Expansion', 'FullMapUnlockAndExpansion', '3832298906', 'd9daec9217b454f8dd8d751bcf480fb5d6df8dc9'],
  ['Intercity Resource Sharing', 'Intercity_Resource_Sharing', 'IntercityResourceSharing', '3832299398', '0ebc190ff9eb61789f968adba85b071c2cf0c89a'],
  ['Realistic Border Control', 'Realistic_Border_Control', 'RealisticBorderControl', '3832299650', '0df0f34b7e38ca679678251e748d99668d2c9915'],
  ['Realistic City First Response', 'Realistic_City_First_Response', 'RealisticCityFirstResponse', '3832299831', '8e70ce923705546c98e7da05b4f5c00dfe687797'],
  ['Realistic City Time', 'Realistic_City_Time', 'RealisticCityTime', '3832300004', '4c3b1e2949ddc75fc7c50902543a8f56c46f14b2'],
  ['Realistic Import Export Control', 'Realistic_Import_Export_Control', 'RealisticImportExportControl', '3832300234', '88bce99e0b348e8513c9a02e8cf68c1d2bd4e652'],
  ['Realistic Pedestrian Traffic Control', 'Realistic_Pedestrian_Traffic_Control', 'RealisticPedestrianTrafficControl', '3832300427', 'f4a55b98a8d8fb95640f3f3e45b8418b5d451f54'],
  ['Realistic Railway Traffic Control', 'Realistic_Railway_Traffic_Control', 'RealisticRailwayTrafficControl', '3832300561', 'af486927ee50fc9d099d6f260965fb8b90f311e7'],
  ['Realistic Urban Population', 'Realistic_Urban_Population', 'RealisticUrbanPopulation', '3832300706', '800da5e1a8c928530f0e289acb1561842a0e0b95'],
  ['Realistic Vehicle Traffic Control', 'Realistic_Vehicle_Traffic_Control', 'RealisticVehicleTrafficControl', '3832300888', 'df8821d7ea0012b7cec2c504bb26975d2d119891'],
  ['Wireless Utilities And Global Coverage', 'Wireless_Utilities_And_Global_Coverage', 'WirelessUtilitiesAndGlobalCoverage', '3832301115', 'fab5d2bfa27ce03981de3a845cf4ee40148a1201'],
  ['AI Gaming (paused)', 'AI_Gaming', 'AIGaming', '3823740106'],
];

export const registry = Object.freeze(rows.map(([label, key, assembly, workshopId, reviewedSource = null]) =>
  Object.freeze({ label, key, branch: key, assembly, workshopId, reviewedSource,
    paused: key === 'AI_Gaming', releaseReady: false })));

const fail = message => { throw new Error(message); };
const sha256 = text => createHash('sha256').update(text, 'utf8').digest('hex');
const isText = value => typeof value === 'string';
const recipes = new Set(['AI_Improve', ...reviewedRecipes.map(item => item.module)]);
for (const recipe of reviewedRecipes) {
  const item = registry.find(entry => entry.key === recipe.module);
  if (!item || item.assembly !== recipe.assembly || item.reviewedSource !== recipe.sourceSha || item.paused)
    fail('Compiler recipe and trusted module registry do not agree.');
}

export function parseSelection(body) {
  if (!isText(body) || Buffer.byteLength(body, 'utf8') > 65535) fail('Invalid issue body.');
  const lines = body.replaceAll('\r\n', '\n').split('\n');
  const headings = lines.flatMap((line, index) => line === '### Modules' ? [index] : []);
  if (headings.length !== 1) fail('Exactly one Modules section is required.');
  const start = headings[0] + 1;
  let end = start;
  while (end < lines.length && !/^### /.test(lines[end])) end++;
  const choices = lines.slice(start, end).filter(line => line.trim() !== '');
  if (choices.length !== registry.length) fail('The module list does not match the registered form.');
  const seen = new Set();
  const selected = new Set();
  for (const line of choices) {
    const match = /^- \[([ xX])\] (.+)$/.exec(line);
    if (!match) fail('Malformed module checkbox.');
    const item = registry.find(entry => entry.label === match[2]);
    if (!item || seen.has(item.key)) fail('Unknown or duplicate module.');
    seen.add(item.key);
    if (match[1].toLowerCase() === 'x') {
      if (item.paused) fail('Paused module cannot be selected.');
      selected.add(item.key);
    }
  }
  if (selected.size === 0) fail('Select at least one module.');
  return registry.filter(item => selected.has(item.key)).map(item => item.key);
}

export function parseChangeNote(body) {
  if (!isText(body) || Buffer.byteLength(body, 'utf8') > 65535) fail('Invalid issue body.');
  const lines = body.replaceAll('\r\n', '\n').split('\n');
  const headings = lines.flatMap((line, index) => line === '### Proposed Workshop change note' ? [index] : []);
  if (headings.length !== 1) fail('Exactly one Workshop change note section is required.');
  let end = headings[0] + 1;
  while (end < lines.length && !/^### /.test(lines[end])) end++;
  const values = lines.slice(headings[0] + 1, end).filter(line => line.trim() !== '');
  if (values.length !== 1) fail('The Workshop change note must be a single line.');
  const note = values[0].trim();
  if (note.length < 1 || note.length > 240 || !/^[\x20-\x21\x23-\x5b\x5d-\x7e]+$/.test(note))
    fail('The Workshop change note has unsupported characters or length.');
  return note;
}

export function createPreview(issue, heads) {
  if (!issue || !Number.isSafeInteger(issue.number) || issue.number <= 0 ||
      issue.state !== 'open' || issue.pull_request || !isText(issue.body) || !isText(issue.title) ||
      !issue.title.startsWith('[Workshop update]: '))
    fail('A valid open issue is required.');
  parseChangeNote(issue.body);
  const modules = parseSelection(issue.body).map(key => {
    const item = registry.find(entry => entry.key === key);
    const sourceSha = heads?.[item.branch];
    if (!isText(sourceSha) || !/^[0-9a-f]{40}$/.test(sourceSha)) fail('A branch head is invalid.');
    return { key: item.key, branch: item.branch, sourceSha, workshopId: item.workshopId };
  });
  return { schema: 1, repository: REPOSITORY, issueNumber: issue.number,
    titleSha256: sha256(issue.title), bodySha256: sha256(issue.body), modules };
}

export function renderPreview(preview) {
  if (!preview || preview.schema !== 1 || preview.repository !== REPOSITORY ||
      !Number.isSafeInteger(preview.issueNumber) || !Array.isArray(preview.modules) ||
      preview.modules.length < 1 || preview.modules.length > 15 ||
      !/^[0-9a-f]{64}$/.test(preview.bodySha256) || !/^[0-9a-f]{64}$/.test(preview.titleSha256))
    fail('Preview is invalid.');
  const entries = preview.modules.map(item => {
    const registered = registry.find(entry => entry.key === item.key);
    if (!registered || item.branch !== registered.branch || item.workshopId !== registered.workshopId ||
        !/^[0-9a-f]{40}$/.test(item.sourceSha) || registered.paused) fail('Preview module is invalid.');
    const status = item.key === 'Automated_Public_Transport_Planning' ?
      'blocked: no reviewed batch recipe for the updated release preview' :
      !recipes.has(item.key) ? 'blocked: no reviewed release recipe' :
      item.sourceSha !== registered.reviewedSource ? 'blocked: source changed since review' :
      item.key === 'Full_Map_Unlock_And_Expansion' ? 'blocked: source still identifies as Development Preview' :
      item.key === 'Automated_Public_Transport_Vehicle_Management' ? 'candidate is observation-only and does not dispatch vehicles' :
      item.key === 'Realistic_City_Time' ? 'candidate is observation-only and does not change time or speed' :
      item.key === 'Intercity_Resource_Sharing' ? 'candidate is an inactive foundation without resource exchange' :
      item.key === 'Wireless_Utilities_And_Global_Coverage' ? 'candidate is an inactive foundation without utility control' :
      'candidate recipe; cloud and Steam checks required';
    return `- ${item.key}: commit \`${item.sourceSha}\`; Workshop ID \`${item.workshopId}\`; ${status}`;
  });
  return [
    '## Workshop release request preview',
    '',
    'This request is not yet authorized. The owner must review the exact modules and commits below.',
    'This preview does not establish release readiness or Steam ownership.',
    'Automatic Steam upload remains disabled pending independent verification.',
    '', ...entries, '',
    'Changing the issue or a selected branch invalidates this preview. A consumed issue cannot be reused.',
    '', `<!-- cs1-workshop-preview-v1:${Buffer.from(JSON.stringify(preview), 'utf8').toString('base64url')} -->`, '',
  ].join('\n');
}

export function verifyPreview(preview, comment, evidence) {
  const current = createPreview(evidence.issue, evidence.heads);
  createSnapshot(evidence);
  if (JSON.stringify(current) !== JSON.stringify(preview) ||
      !comment || !Number.isSafeInteger(comment.id) || comment.id <= 0 ||
      comment.user?.id !== 41898282 || comment.user?.login !== 'github-actions[bot]' ||
      comment.user?.type !== 'Bot' || !isText(comment.created_at) ||
      comment.created_at !== comment.updated_at || comment.body !== renderPreview(preview) ||
      !Number.isFinite(Date.parse(comment.created_at)) ||
      Date.parse(comment.created_at) >= Date.parse(evidence.label.created_at))
    fail('The reviewed preview is missing, edited, stale or not from GitHub Actions.');
  return true;
}

export function createSnapshot({ event, issue, label, heads, attempt }) {
  if (attempt !== 1) fail('Workflow reruns are not authorized.');
  if (event?.action !== 'labeled' || event.label?.name !== LABEL ||
      event.sender?.id !== OWNER_ID || event.sender?.login !== 'KingsleyHK0323' ||
      event.repository?.id !== REPOSITORY_ID || event.repository?.full_name !== REPOSITORY ||
      event.repository.owner?.id !== OWNER_ID || event.repository.owner?.login !== 'KingsleyHK0323') {
    fail('Owner label event is not valid.');
  }
  const source = event.issue;
  if (!source || !issue || !Number.isSafeInteger(source.number) || source.number <= 0 ||
      source.number !== issue.number || source.pull_request || issue.pull_request ||
      source.state !== 'open' || issue.state !== 'open' ||
      !isText(source.body) || source.body !== issue.body ||
      !isText(source.title) || !source.title.startsWith('[Workshop update]: ') || source.title !== issue.title ||
      !isText(source.updated_at) || source.updated_at !== issue.updated_at ||
      !Array.isArray(issue.labels) || !issue.labels.some(entry => entry?.name === LABEL)) {
    fail('Issue was changed, closed or does not match the approved snapshot.');
  }
  if (!label || !Number.isSafeInteger(label.id) || label.id <= 0 || label.event !== 'labeled' ||
      label.label?.name !== LABEL || label.actor?.id !== OWNER_ID ||
      !isText(label.created_at) || !Number.isFinite(Date.parse(label.created_at))) {
    fail('The latest label event is not an owner approval.');
  }
  parseChangeNote(source.body);
  const selected = parseSelection(source.body);
  const modules = selected.map(key => {
    const item = registry.find(entry => entry.key === key);
    const sourceSha = heads?.[item.branch];
    if (!isText(sourceSha) || !/^[0-9a-f]{40}$/.test(sourceSha)) fail('A branch head is invalid.');
    return { key: item.key, branch: item.branch, assembly: item.assembly,
      workshopId: item.workshopId, sourceSha };
  });
  return { schema: 1, repository: REPOSITORY, issueNumber: source.number,
    issueUpdatedAt: source.updated_at, titleSha256: sha256(source.title),
    bodySha256: sha256(source.body), labelEventId: label.id,
    labelCreatedAt: label.created_at, modules };
}

export function verifySnapshot(snapshot, evidence) {
  const current = createSnapshot(evidence);
  if (JSON.stringify(current) !== JSON.stringify(snapshot)) fail('Approval snapshot changed.');
  return true;
}

export function requireReady(snapshot) {
  if (!snapshot || !Array.isArray(snapshot.modules) || snapshot.modules.length === 0) fail('Invalid snapshot.');
  for (const selection of snapshot.modules) {
    const item = registry.find(entry => entry.key === selection.key);
    if (!item || !item.releaseReady) fail(`Module is not ready for automatic Workshop upload: ${selection.key}`);
  }
  return true;
}

export function requireBuildable(snapshot) {
  if (!snapshot || !Array.isArray(snapshot.modules) || snapshot.modules.length === 0) fail('Invalid snapshot.');
  const seen = new Set();
  for (const item of snapshot.modules) {
    const registered = registry.find(entry => entry.key === item?.key);
    if (!registered || !recipes.has(item.key)) fail(`Cloud release recipe is not implemented for ${item?.key || 'the selected module'}.`);
    if (seen.has(item.key) || item.branch !== registered.branch || item.workshopId !== registered.workshopId ||
        item.assembly !== registered.assembly) fail('Invalid or duplicate release selection.');
    if (item.sourceSha !== registered.reviewedSource) fail(`Source changed since release review: ${item.key}`);
    if (item.key === 'Full_Map_Unlock_And_Expansion')
      fail('Source still identifies as Development Preview and cannot be released.');
    seen.add(item.key);
  }
  return true;
}

// Review status clarification: OpenAI-Codex.

export function verifySteamItem(expected, actual) {
  if (!expected || !actual || !/^[1-9][0-9]{0,19}$/.test(expected.id) ||
      !/^[1-9][0-9]{0,19}$/.test(expected.creator) || expected.appId !== 255710 ||
      typeof actual.publishedfileid !== 'string' || actual.publishedfileid !== expected.id ||
      typeof actual.creator !== 'string' || actual.creator !== expected.creator ||
      actual.consumer_app_id !== expected.appId || actual.result !== 1) {
    fail('Steam item, creator or game did not match the registered target.');
  }
  return true;
}
