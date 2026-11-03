// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Low-cost discovery only. Candidates cannot authorize downloads or installation.
export async function discoverModuleReleases(registry, request, nowMs = Date.now()) {
  const repository = registry[0]?.repository; const endpoint = `https://api.github.com/repos/${repository}/releases`;
  const controller = new AbortController(); const timer = setTimeout(() => controller.abort(), 30000);
  const unknown = reason => registry.map(module => ({ moduleId: module.moduleId, status: 'unknown', checkedAtMs: nowMs, reason }));
  try {
    let url = `${endpoint}?per_page=100&page=1`; const records = []; const ids = new Set(); let complete = false;
    for (let page = 1; page <= 10; page++) {
      const response = await request({ url, method: 'GET', redirect: 'manual', credentials: 'omit', cachePhase: 'discovery', signal: controller.signal });
      if (response.status !== 200 || response.url !== url || response.redirected !== false || !Array.isArray(response.body) || response.body.length > 100 || !response.headers) throw new Error('API_UNAVAILABLE_OR_RATE_LIMITED');
      for (const release of response.body) {
        if (!Number.isSafeInteger(release.id) || ids.has(release.id) || typeof release.tag_name !== 'string') throw new Error('INCOMPLETE_RELEASE_INDEX');
        ids.add(release.id); records.push(release);
      }
      const link = response.headers.link; if (link === undefined || link === '') { complete = true; break; }
      if (typeof link !== 'string' || link.length > 8192) throw new Error('INCOMPLETE_PAGINATION');
      let next; let last;
      for (const part of link.split(',')) {
        const match = /^\s*<([^>]+)>;\s*rel="(next|prev|first|last)"\s*$/.exec(part); if (!match) throw new Error('INCOMPLETE_PAGINATION');
        const target = new URL(match[1]); const number = Number(target.searchParams.get('page'));
        if (`${target.origin}${target.pathname}` !== endpoint || target.username || target.password || target.hash || target.href !== match[1] ||
          target.searchParams.get('per_page') !== '100' || [...target.searchParams].length !== 2 || !Number.isSafeInteger(number) || number < 1) throw new Error('UNSAFE_PAGINATION');
        if (match[2] === 'next') { if (next || number !== page + 1) throw new Error('INCOMPLETE_PAGINATION'); next = match[1]; }
        if (match[2] === 'last') last = number;
      }
      if (!next) { if (last > page) throw new Error('INCOMPLETE_PAGINATION'); complete = true; break; } url = next;
    }
    if (!complete) throw new Error('PAGINATION_LIMIT');
    return registry.map(module => {
      const matching = records.filter(release => release.tag_name.startsWith(module.releaseTagPrefix));
      if (matching.some(release => release.draft !== true && (release.draft !== false || typeof release.published_at !== 'string' ||
        !Number.isFinite(Date.parse(release.published_at)) || Date.parse(release.published_at) > nowMs)))
        return { moduleId: module.moduleId, status: 'unknown', checkedAtMs: nowMs, reason: 'Same-module publication metadata is incomplete, invalid or in the future; no older fallback is selected.' };
      const candidates = records.filter(release => release.tag_name.startsWith(module.releaseTagPrefix) && release.draft === false && typeof release.published_at === 'string')
        .map(release => ({ release, published: Date.parse(release.published_at) }))
        .filter(item => Number.isFinite(item.published) && item.published <= nowMs)
        .sort((a, b) => b.published - a.published);
      if (!candidates.length) return { moduleId: module.moduleId, status: 'unknown', checkedAtMs: nowMs, reason: 'No usable published module release was found.' };
      if (candidates.length > 1 && candidates[0].published === candidates[1].published) return { moduleId: module.moduleId, status: 'unknown', checkedAtMs: nowMs, reason: 'Published candidates have ambiguous dates.' };
      const release = candidates[0].release;
      return { moduleId: module.moduleId, status: 'candidate / verification required', checkedAtMs: nowMs,
        discoveryCandidate: { releaseId: release.id, buildId: release.tag_name, sourceSha: release.target_commitish, publishedAt: release.published_at,branch:module.branch,
          ...(/^[1-9][0-9]*-[1-9][0-9]*$/.test(release.tag_name.slice(module.releaseTagPrefix.length)) ?
            {runId:Number(release.tag_name.slice(module.releaseTagPrefix.length).split('-')[0]),attempt:Number(release.tag_name.slice(module.releaseTagPrefix.length).split('-')[1])}: {}) },
        reason: 'A published candidate exists. Branch, build, assets and actual assembly bytes must be verified before installation.' };
    });
  } catch (error) { return unknown(/^[A-Z_]+$/.test(error.message) ? error.message : 'RELEASE_INDEX_UNAVAILABLE'); }
  finally { clearTimeout(timer); }
}
