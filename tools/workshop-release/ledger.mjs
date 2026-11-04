// Signature: OpenAI-Codex_workshop_implementation
// A ledger must be initialized and protected separately; this module never creates or resets it.
const prefix = '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack';
const repository = 'KingsleyHK0323/Cities_Skylines_1_Modpack';
const branch = 'Workshop_Release_Ledger';
const isSha = value => typeof value === 'string' && /^[0-9a-f]{40}$/.test(value);
const fail = message => { throw new Error(message); };

function decodeJsonFile(file) {
  if (file?.type !== 'file' || file.encoding !== 'base64' || typeof file.content !== 'string' ||
      file.content.length > 65536 || !/^[a-zA-Z0-9+/=\r\n]+$/.test(file.content)) {
    fail('Ledger file cannot be verified.');
  }
  return JSON.parse(Buffer.from(file.content.replaceAll(/\s/g, ''), 'base64').toString('utf8'));
}

export async function claimOnce(api, snapshot, runId, controllerSha) {
  if (!snapshot || snapshot.schema !== 1 || snapshot.repository !== repository ||
      !Number.isSafeInteger(snapshot.issueNumber) || snapshot.issueNumber <= 0 ||
      !Number.isSafeInteger(snapshot.labelEventId) || snapshot.labelEventId <= 0 ||
      typeof snapshot.bodySha256 !== 'string' || !/^[0-9a-f]{64}$/.test(snapshot.bodySha256) ||
      !Array.isArray(snapshot.modules) || snapshot.modules.length < 1 || snapshot.modules.length > 15 ||
      !Number.isSafeInteger(runId) || runId <= 0 || !isSha(controllerSha)) {
    fail('Invalid immutable approval claim.');
  }
  for (const item of snapshot.modules) {
    if (typeof item?.key !== 'string' || !/^[A-Za-z0-9_]{1,100}$/.test(item.key) || !isSha(item.sourceSha))
      fail('Invalid source in approval claim.');
  }
  const refPath = `${prefix}/git/ref/heads/${branch}`;
  const ref = await api.request('GET', refPath);
  const head = ref?.object?.sha;
  if (ref?.object?.type !== 'commit' || !isSha(head)) fail('Ledger branch is unavailable.');
  const marker = decodeJsonFile(await api.request('GET', `${prefix}/contents/workshop-release-ledger.json?ref=${head}`));
  if (marker.schema !== 1 || marker.repository !== repository || marker.purpose !== 'workshop-release-claims')
    fail('Ledger is not initialized for this repository.');
  const claimPath = `claims/issue-${snapshot.issueNumber}.json`;
  try {
    await api.request('GET', `${prefix}/contents/${claimPath}?ref=${head}`);
    fail('This issue has already been claimed; open a new issue for a retry.');
  } catch (error) {
    if (error?.status !== 404) throw error;
  }
  const base = await api.request('GET', `${prefix}/git/commits/${head}`);
  if (base?.sha !== head || !isSha(base?.tree?.sha)) fail('Ledger head is invalid.');
  const claim = { schema: 1, repository, issueNumber: snapshot.issueNumber,
    labelEventId: snapshot.labelEventId, bodySha256: snapshot.bodySha256,
    controllerSha, runId, modules: snapshot.modules.map(item => ({key: item.key, sourceSha: item.sourceSha})) };
  const blob = await api.request('POST', `${prefix}/git/blobs`,
    {content: JSON.stringify(claim) + '\n', encoding: 'utf-8'});
  if (!isSha(blob?.sha)) fail('Ledger blob creation failed.');
  const tree = await api.request('POST', `${prefix}/git/trees`, {
    base_tree: base.tree.sha,
    tree: [{path: claimPath, mode: '100644', type: 'blob', sha: blob.sha}],
  });
  if (!isSha(tree?.sha)) fail('Ledger tree creation failed.');
  const commit = await api.request('POST', `${prefix}/git/commits`, {
    message: `Claim Workshop request #${snapshot.issueNumber}`,
    tree: tree.sha, parents: [head],
  });
  if (!isSha(commit?.sha)) fail('Ledger commit creation failed.');
  const updated = await api.request('PATCH', `${prefix}/git/refs/heads/${branch}`,
    {sha: commit.sha, force: false});
  if (updated?.object?.type !== 'commit' || updated.object.sha !== commit.sha)
    fail('Ledger claim could not be confirmed.');
  const observed = await api.request('GET', refPath);
  if (observed?.object?.sha !== commit.sha || observed.object.type !== 'commit')
    fail('Ledger head changed before claim confirmation.');
  const stored = decodeJsonFile(await api.request('GET', `${prefix}/contents/${claimPath}?ref=${commit.sha}`));
  if (JSON.stringify(stored) !== JSON.stringify(claim)) fail('Ledger claim differs from the request.');
  return commit.sha;
}

export async function verifyClaim(api, snapshot, runId, controllerSha) {
  if (!snapshot || snapshot.schema !== 1 || snapshot.repository !== repository ||
      !Number.isSafeInteger(snapshot.issueNumber) || snapshot.issueNumber <= 0 ||
      !Number.isSafeInteger(snapshot.labelEventId) || snapshot.labelEventId <= 0 ||
      typeof snapshot.bodySha256 !== 'string' || !/^[0-9a-f]{64}$/.test(snapshot.bodySha256) ||
      !Array.isArray(snapshot.modules) || snapshot.modules.length < 1 || snapshot.modules.length > 15 ||
      !Number.isSafeInteger(runId) || runId <= 0 || !isSha(controllerSha)) fail('Invalid claim verification context.');
  for (const item of snapshot.modules)
    if (typeof item?.key !== 'string' || !/^[A-Za-z0-9_]{1,100}$/.test(item.key) || !isSha(item.sourceSha))
      fail('Invalid source in approval claim.');
  const refPath = `${prefix}/git/ref/heads/${branch}`;
  const ref = await api.request('GET', refPath);
  const head = ref?.object?.sha;
  if (ref?.object?.type !== 'commit' || !isSha(head)) fail('Ledger branch is unavailable.');
  const marker = decodeJsonFile(await api.request('GET', `${prefix}/contents/workshop-release-ledger.json?ref=${head}`));
  if (marker.schema !== 1 || marker.repository !== repository || marker.purpose !== 'workshop-release-claims')
    fail('Ledger is not initialized for this repository.');
  const stored = decodeJsonFile(await api.request('GET', `${prefix}/contents/claims/issue-${snapshot.issueNumber}.json?ref=${head}`));
  const expected = {schema:1, repository, issueNumber:snapshot.issueNumber,
    labelEventId:snapshot.labelEventId, bodySha256:snapshot.bodySha256, controllerSha, runId,
    modules:snapshot.modules.map(item => ({key:item.key, sourceSha:item.sourceSha}))};
  if (JSON.stringify(stored) !== JSON.stringify(expected)) fail('The durable issue claim does not match this run.');
  return true;
}
