// Signature: OpenAI-Codex_workshop_implementation
import {registry, parseSelection, createPreview, renderPreview, createSnapshot, verifyPreview} from './request.mjs';
import {listIssuePages, latestOwnerApproval} from './github.mjs';

const root = '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack';
const fail = message => { throw new Error(message); };

async function readHeads(api, body) {
  const keys = parseSelection(body);
  const heads = {};
  for (const key of keys) {
    const item = registry.find(entry => entry.key === key);
    const data = await api.request('GET', `${root}/git/ref/heads/${item.branch}`);
    if (data?.ref !== `refs/heads/${item.branch}` || data.object?.type !== 'commit' ||
        typeof data.object.sha !== 'string' || !/^[0-9a-f]{40}$/.test(data.object.sha))
      fail('Registered branch head is unavailable.');
    heads[item.branch] = data.object.sha;
  }
  return heads;
}

export async function previewRequest(api, event) {
  if (!['opened','edited'].includes(event?.action) ||
      event.repository?.id !== 1425203074 || event.repository?.full_name !== 'KingsleyHK0323/Cities_Skylines_1_Modpack' ||
      !Number.isSafeInteger(event.issue?.number) || event.issue.number <= 0 || event.issue.pull_request)
    fail('The preview event is not a valid issue request.');
  const issue = await api.request('GET', `${root}/issues/${event.issue.number}`);
  if (issue.number !== event.issue.number || issue.state !== 'open' || issue.body !== event.issue.body ||
      issue.title !== event.issue.title || issue.updated_at !== event.issue.updated_at)
    fail('Issue changed before the preview was generated.');
  const preview = createPreview(issue, await readHeads(api, issue.body));
  await api.request('POST', `${root}/issues/${issue.number}/comments`, {body:renderPreview(preview)});
  return preview;
}

export async function reviewedSnapshot(api, event, attempt) {
  if (!Number.isSafeInteger(event?.issue?.number) || event.issue.number <= 0) fail('Issue number is invalid.');
  const number = event.issue.number;
  const issue = await api.request('GET', `${root}/issues/${number}`);
  const label = latestOwnerApproval(await listIssuePages(api, number, 'events'));
  const heads = await readHeads(api, issue.body);
  const evidence = {event, issue, label, heads, attempt};
  const snapshot = createSnapshot(evidence);
  const graph = await api.request('POST', '/graphql', {
    query:'query($number:Int!){repository(owner:"KingsleyHK0323",name:"Cities_Skylines_1_Modpack"){issue(number:$number){number title body lastEditedAt}}}',
    variables:{number},
  });
  const graphIssue = graph?.data?.repository?.issue;
  if (graph?.errors || graphIssue?.number !== number || graphIssue.title !== issue.title || graphIssue.body !== issue.body ||
      (graphIssue.lastEditedAt !== null && (typeof graphIssue.lastEditedAt !== 'string' ||
        !Number.isFinite(Date.parse(graphIssue.lastEditedAt)) || Date.parse(graphIssue.lastEditedAt) >= Date.parse(label.created_at))))
    fail('Issue edit history is missing or changed at or after approval.');
  const comments = await listIssuePages(api, number, 'comments');
  const previews = comments.filter(comment => comment?.user?.id === 41898282 &&
    typeof comment.body === 'string' && comment.body.startsWith('## Workshop release request preview\n'));
  if (previews.length === 0) fail('No reviewed Actions preview was found.');
  const comment = previews[previews.length - 1];
  const match = /<!-- cs1-workshop-preview-v1:([A-Za-z0-9_-]+) -->\n$/.exec(comment.body);
  if (!match || match[1].length > 65536) fail('The latest preview payload is invalid.');
  let preview;
  try { preview = JSON.parse(Buffer.from(match[1], 'base64url').toString('utf8')); }
  catch { fail('The latest preview payload is invalid.'); }
  verifyPreview(preview, comment, evidence);
  return snapshot;
}
