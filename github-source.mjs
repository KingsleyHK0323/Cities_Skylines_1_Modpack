// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Injected trusted-HTTPS GitHub API metadata resolver. No built-in network adapter.
import { safeModuleId, safeRelative } from './core.mjs';

const require = condition => { if (!condition) throw new Error('Incomplete or inconsistent GitHub snapshot'); };
const sha = value => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
const id = value => Number.isSafeInteger(value) && value > 0;

export async function detectGitHubModule({ policy: original, installed, request, nowMs = Date.now(), timeoutMs = 5000 } = {}) {
  let reason = 'INVALID_MODULE_POLICY'; let timer;
  let branchHead = { status: 'unknown' };
  const controller = new AbortController();
  const unknown = () => ({ status: 'unknown', installAllowed: false,
    assurance: 'trusted-https-github-api-metadata', reason, latestPublished: { status: 'unknown' }, branchHead });
  try {
    const policy = structuredClone(original);
    safeModuleId(policy.moduleId);
    require(typeof policy.repository === 'string' && /^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(policy.repository) &&
      policy.repository.split('/').every(part => !['.', '..'].includes(part)));
    safeRelative(policy.branch); safeRelative(policy.workflowPath); safeRelative(policy.releaseTagPrefix);
    require(!policy.releaseTagPrefix.includes('/') && policy.releaseTagPrefix.endsWith('-'));
    require(policy.workflowPath.startsWith('.github/workflows/') && /\.ya?ml$/.test(policy.workflowPath));
    require(Array.isArray(policy.runtimeAssetNames) && policy.runtimeAssetNames.length > 0 && policy.runtimeAssetNames.length <= 256);
    const names = new Set();
    for (const name of policy.runtimeAssetNames) {
      safeRelative(name); require(!name.includes('/') && !names.has(name.toLowerCase())); names.add(name.toLowerCase());
    }
    require(policy.runtimeAssetNames.includes(`${policy.assembly}.dll`));
    require(typeof request === 'function' && Number.isSafeInteger(nowMs) && nowMs >= 0 && Number.isInteger(timeoutMs) && timeoutMs >= 1 && timeoutMs <= 30000);
    const base = `https://api.github.com/repos/${policy.repository}`;
    const deadline = performance.now() + timeoutMs;
    const get = async (url, cachePhase = 'initial') => {
      require(!controller.signal.aborted && performance.now() < deadline);
      const parsed = new URL(url);
      require(parsed.origin === 'https://api.github.com' && !parsed.username && !parsed.password && !parsed.hash &&
        parsed.pathname.startsWith(`/repos/${policy.repository}/`) && parsed.href === url);
      const response = await request(Object.freeze({ url, method: 'GET', redirect: 'manual', credentials: 'omit', cache: 'no-store', cachePhase, signal: controller.signal }));
      if (response?.status === 429 || response?.status === 403 && response.headers?.['x-ratelimit-remaining'] === '0') reason = 'GITHUB_RATE_LIMIT';
      require(!controller.signal.aborted && performance.now() < deadline);
      require(response?.status === 200 && response.url === url && response.redirected === false && response.headers && typeof response.headers === 'object');
      require(Object.keys(response.headers).every(name => name === name.toLowerCase()));
      require(JSON.stringify(response.body).length <= 4 * 1024 * 1024);
      return { body: structuredClone(response.body), headers: structuredClone(response.headers) };
    };
    const collect = async endpoint => {
      const firstUrl = `${endpoint}?per_page=100&page=1`; let url = firstUrl;
      const all = []; let first;
      for (let page = 1; page <= 10; page++) {
        const response = await get(url); if (page === 1) first = response;
        require(Array.isArray(response.body) && response.body.length <= 100);
        all.push(...response.body);
        const link = response.headers.link;
        if (link === undefined || link === '') return { all, first, firstUrl };
        require(typeof link === 'string' && link.length <= 8192);
        const relations = new Map();
        for (const segment of link.split(',')) {
          const match = /^\s*<([^>]+)>;\s*rel="(next|prev|first|last)"\s*$/.exec(segment);
          require(match && !relations.has(match[2]));
          const target = new URL(match[1]); const number = Number(target.searchParams.get('page'));
          require(!target.username && !target.password && !target.hash && `${target.origin}${target.pathname}` === endpoint && target.searchParams.get('per_page') === '100' &&
            [...target.searchParams].length === 2 && id(number) && target.href === match[1]);
          relations.set(match[2], { url: match[1], page: number });
        }
        const next = relations.get('next');
        if (!next) { require(!relations.has('last') || relations.get('last').page <= page); return { all, first, firstUrl }; }
        require(next.page === page + 1); url = next.url;
      }
      throw new Error('Pagination limit');
    };
    const work = async () => {
      reason = 'BRANCH_UNAVAILABLE';
      const branchUrl = `${base}/git/ref/heads/${policy.branch.split('/').map(encodeURIComponent).join('/')}`;
      const readBranch = async (phase = 'initial') => {
        const ref = (await get(branchUrl, phase)).body;
        require(ref.ref === `refs/heads/${policy.branch}` && ref.object?.type === 'commit' && sha(ref.object.sha)); return ref.object.sha;
      };
      const head = await readBranch(); branchHead = { status: 'observed', sourceSha: head, checkedAtMs: nowMs };
      reason = 'RELEASE_LIST_INCOMPLETE';
      const listing = await collect(`${base}/releases`); const seen = new Set(); const candidates = [];
      for (const release of listing.all) {
        require(id(release?.id) && !seen.has(release.id) && typeof release.tag_name === 'string'); seen.add(release.id);
        if (!release.tag_name.startsWith(policy.releaseTagPrefix)) continue;
        require(typeof release.draft === 'boolean'); if (release.draft) continue;
        const suffix = release.tag_name.slice(policy.releaseTagPrefix.length);
        require(/^[1-9][0-9]{0,15}-[1-9][0-9]{0,5}$/.test(suffix));
        const [runId, attempt] = suffix.split('-').map(Number); require(id(runId) && id(attempt));
        const published = Date.parse(release.published_at);
        require(typeof release.published_at === 'string' && Number.isFinite(published) && published <= nowMs);
        candidates.push({ release, runId, attempt, published });
      }
      reason = 'NO_PUBLISHED_MODULE_RELEASE'; require(candidates.length > 0);
      candidates.sort((a, b) => b.published - a.published);
      reason = 'AMBIGUOUS_PUBLICATION_ORDER'; require(candidates.length === 1 || candidates[0].published !== candidates[1].published);
      const selected = candidates[0]; const tag = selected.release.tag_name;
      reason = 'RELEASE_BINDING_MISMATCH';
      const releaseUrl = `${base}/releases/${selected.release.id}`;
      const detail = (await get(releaseUrl)).body;
      require(detail.id === selected.release.id && detail.tag_name === tag && detail.draft === false && detail.published_at === selected.release.published_at &&
        detail.url === releaseUrl && detail.html_url === `https://github.com/${policy.repository}/releases/tag/${encodeURIComponent(tag)}`);
      const resolveTag = async (phase = 'initial') => {
        const ref = (await get(`${base}/git/ref/tags/${encodeURIComponent(tag)}`, phase)).body;
        require(ref.ref === `refs/tags/${tag}`); let object = ref.object; const visited = new Set();
        for (let depth = 0; depth <= 5; depth++) {
          require(object && sha(object.sha) && !visited.has(object.sha)); visited.add(object.sha);
          if (object.type === 'commit') return object.sha;
          require(object.type === 'tag'); const annotation = (await get(`${base}/git/tags/${object.sha}`, phase)).body;
          require(annotation.sha === object.sha); object = annotation.object;
        }
        throw new Error('Tag depth');
      };
      reason = 'TAG_OR_BUILD_UNVERIFIED'; const sourceSha = await resolveTag();
      const run = (await get(`${base}/actions/runs/${selected.runId}/attempts/${selected.attempt}`)).body;
      require(run.id === selected.runId && run.run_attempt === selected.attempt && run.head_branch === policy.branch && run.head_sha === sourceSha &&
        run.repository?.full_name === policy.repository && run.head_repository?.full_name === policy.repository &&
        run.status === 'completed' && run.conclusion === 'success' && ['push', 'workflow_dispatch'].includes(run.event));
      require([policy.workflowPath, `${policy.workflowPath}@${policy.branch}`, `${policy.workflowPath}@refs/heads/${policy.branch}`].includes(run.path));
      // target_commitish is not tag evidence: GitHub ignores it for an existing tag.
      reason = 'ASSETS_UNVERIFIED'; const assetListing = await collect(`${releaseUrl}/assets`);
      const assets = assetListing.all; const assetIds = new Set(); const assetNames = new Set(); let total = 0;
      for (const asset of assets) {
        require(id(asset?.id) && !assetIds.has(asset.id) && typeof asset.name === 'string' && !assetNames.has(asset.name.toLowerCase()));
        assetIds.add(asset.id); assetNames.add(asset.name.toLowerCase());
        if (/\.dll$/i.test(asset.name)) require(names.has(asset.name.toLowerCase()));
      }
      const requiredAssets=[...new Set([...policy.runtimeAssetNames,...(policy.provenanceAssetName?[policy.provenanceAssetName]:[])])];
      for (const name of requiredAssets) {
        const asset = assets.find(candidate => candidate.name === name);
        require(asset && asset.state === 'uploaded' && id(asset.size) && asset.size <= 32 * 1024 * 1024 &&
          /^sha256:[a-f0-9]{64}$/.test(asset.digest) && asset.url === `${base}/releases/assets/${asset.id}` &&
          asset.browser_download_url === `https://github.com/${policy.repository}/releases/download/${encodeURIComponent(tag)}/${encodeURIComponent(name)}`);
        total += asset.size;
      }
      require(total <= 64 * 1024 * 1024);
      reason = 'SNAPSHOT_CHANGED';
      const listingAgain = await get(listing.firstUrl, 'confirm');
      require(JSON.stringify(listingAgain.body) === JSON.stringify(listing.first.body) && listingAgain.headers.link === listing.first.headers.link);
      require(JSON.stringify((await get(releaseUrl, 'confirm')).body) === JSON.stringify(detail));
      require(await resolveTag('confirm') === sourceSha);
      const finalHead = await readBranch('confirm');
      if (finalHead !== head) { branchHead = { status: 'unknown' }; throw new Error('Branch changed'); }
      reason = 'INSTALLED_RECORD_UNKNOWN';
      const installedKnown = installed === null || installed && installed.moduleId === policy.moduleId &&
        typeof installed.buildId === 'string' && installed.buildId.length > 0 && installed.buildId.length <= 256;
      return { status: !installedKnown ? 'unknown' : installed === null ? 'not-installed' : installed.buildId === tag ? 'current' : 'different-build',
        ...(!installedKnown ? { reason: 'INSTALLED_RECORD_UNKNOWN' } : {}), installAllowed: false,
        assurance: 'trusted-https-github-api-metadata', checkedAtMs: nowMs, branchHead,
        latestPublished: { status: 'candidate', moduleId: policy.moduleId, releaseId: detail.id, buildId: tag, sourceSha,
          branch:policy.branch,buildStartedAt:run.run_started_at ?? run.created_at,
          runId: selected.runId, attempt: selected.attempt, publishedAt: detail.published_at,
          assets: requiredAssets.map(name => { const a = assets.find(item => item.name === name);
            return { name: a.name, id: a.id, size: a.size, sha256: a.digest.slice(7), url: a.browser_download_url }; }),
          relationToBranch: head === sourceSha ? 'same-commit' : 'different-commit' } };
    };
    return await Promise.race([work(), new Promise((_, reject) => {
      timer = setTimeout(() => { reason = 'API_SNAPSHOT_TIMEOUT'; controller.abort(); reject(new Error('Timeout')); }, timeoutMs);
    })]);
  } catch { return unknown(); }
  finally { clearTimeout(timer); controller.abort(); }
}
