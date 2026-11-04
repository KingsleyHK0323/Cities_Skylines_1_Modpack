// Signature: OpenAI-Codex_workshop_implementation
const repoRoot = '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack';
const fail = message => { throw new Error(message); };

export class GitHubClient {
  constructor(token, transport = fetch, maxResponseBytes = 2 * 1024 * 1024) {
    if (typeof token !== 'string' || token.length === 0 || /[\r\n]/.test(token) ||
        typeof transport !== 'function' || !Number.isSafeInteger(maxResponseBytes) || maxResponseBytes < 4096)
      fail('GitHub API client is not configured.');
    this.token = token;
    this.transport = transport;
    this.maxResponseBytes = maxResponseBytes;
  }
  async request(method, path, body) {
    if (!['GET','POST','PATCH'].includes(method) || typeof path !== 'string' ||
        !(path === '/graphql' || path.startsWith(repoRoot + '/')) ||
        path.includes('..') || /[%\\\r\n\s]/.test(path) ||
        !/^\/[A-Za-z0-9_./?=&-]+$/.test(path)) fail('GitHub API target is not allowed.');
    const options = {method, redirect:'error', signal: AbortSignal.timeout(15000), headers: {
      Accept:'application/vnd.github+json', Authorization:`Bearer ${this.token}`,
      'X-GitHub-Api-Version':'2022-11-28', 'User-Agent':'cs1-workshop-release',
    }};
    if (body !== undefined) {
      if (method === 'GET') fail('GET requests cannot have a body.');
      options.headers['Content-Type'] = 'application/json';
      options.body = JSON.stringify(body);
    }
    const response = await this.transport(`https://api.github.com${path}`, options);
    if (!response.ok) throw Object.assign(new Error(`GitHub API request failed (HTTP ${response.status}).`), {status:response.status});
    const declared = response.headers.get('content-length');
    if (declared !== null && (!/^\d+$/.test(declared) || Number(declared) > this.maxResponseBytes))
      fail('GitHub API response is too large.');
    let length = 0;
    const chunks = [];
    if (!response.body) fail('GitHub API response body is missing.');
    for await (const chunk of response.body) {
      length += chunk.length;
      if (length > this.maxResponseBytes) fail('GitHub API response is too large.');
      chunks.push(Buffer.from(chunk));
    }
    try { return JSON.parse(Buffer.concat(chunks).toString('utf8')); }
    catch { fail('GitHub API response is not valid JSON.'); }
  }
}

export async function listIssuePages(api, issueNumber, kind, limit = 100) {
  if (!Number.isSafeInteger(issueNumber) || issueNumber <= 0 || !['events','comments'].includes(kind) ||
      !Number.isSafeInteger(limit) || limit < 1 || limit > 100) fail('Invalid pagination request.');
  const all = [];
  for (let page = 1; page <= limit; page++) {
    const items = await api.request('GET', `${repoRoot}/issues/${issueNumber}/${kind}?per_page=100&page=${page}`);
    if (!Array.isArray(items) || items.length > 100) fail('Invalid GitHub issue page.');
    all.push(...items);
    if (items.length < 100) return all;
  }
  fail('GitHub issue pagination limit reached.');
}

export function latestOwnerApproval(events) {
  if (!Array.isArray(events)) fail('Issue events are unavailable.');
  const seen = new Set();
  let latest;
  for (const item of events) {
    if (!Number.isSafeInteger(item?.id) || item.id <= 0 || seen.has(item.id))
      fail('Issue events contain an invalid or duplicate identity.');
    seen.add(item.id);
    if (['labeled','unlabeled'].includes(item.event) && item.label?.name === 'steam-workshop-release-approved') {
      const timestamp = typeof item.created_at === 'string' ? Date.parse(item.created_at) : NaN;
      if (!Number.isFinite(timestamp)) fail('An approval event has no valid timestamp.');
      if (latest && timestamp === Date.parse(latest.created_at))
        fail('Approval event order is ambiguous.');
      if (!latest || timestamp > Date.parse(latest.created_at)) latest = item;
    }
  }
  if (!latest || latest.event !== 'labeled' || latest.actor?.id !== 157894869)
    fail('The latest approval label action is not an owner approval.');
  return latest;
}

// Additional validation: OpenAI-Codex.
