// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import { createHash } from 'node:crypto';
import path from 'node:path';

export function safeRelative(name) {
  if (typeof name !== 'string' || !name || name.length > 240 || /[\\:<>"|?*\x00-\x1f\x7f]/.test(name) || name.startsWith('/') ||
      name.split('/').some(p => !p || p === '.' || p === '..' || /[. ]$/.test(p) ||
        /^(con|prn|aux|nul|com[1-9¹²³]|lpt[1-9¹²³]) *(\.|$)/i.test(p))) throw new Error('Unsafe package path');
  return name;
}

export function safeModuleId(id) {
  if (typeof id !== 'string' || !/^[a-z0-9][a-z0-9_-]{0,63}$/.test(id)) throw new Error('Invalid canonical module ID');
  safeRelative(id);
  return id;
}

export function verifyPackage(pkg) {
  if (!pkg || typeof pkg.buildId !== 'string' || !pkg.buildId || pkg.buildId.length > 256 ||
      !Array.isArray(pkg.files) || !pkg.files.length || pkg.files.length > 256)
    throw new Error('Invalid package contract');
  safeModuleId(pkg.moduleId);
  if (pkg.publication !== undefined) verifyPublication(pkg.publication);
  const names = new Set();
  let total = 0;
  const result = pkg.files.map(file => {
    const name = safeRelative(file.path);
    if (names.has(name.toLowerCase())) throw new Error('Case-insensitive path collision');
    names.add(name.toLowerCase());
    if (!Buffer.isBuffer(file.bytes) || file.bytes.length === 0 || file.bytes.length > 32 * 1024 * 1024 ||
        file.sha256 !== createHash('sha256').update(file.bytes).digest('hex')) throw new Error('Integrity mismatch');
    return { path: name, sha256: file.sha256 };
  });
  for (const file of pkg.files) total += file.bytes.length;
  if (total > 64 * 1024 * 1024) throw new Error('Package size limit');
  for (const name of names) {
    const parts = name.split('/');
    for (let i = 1; i < parts.length; i++)
      if (names.has(parts.slice(0, i).join('/'))) throw new Error('File and directory overlap');
  }
  return result;
}

export function verifyPublication(value) {
  if (!value || !['module-release','modpack-bundle','artifact-branch'].includes(value.provider) || !/^[a-f0-9]{40}$/.test(value.sourceSha) ||
    (value.provider!=='artifact-branch' && (!Number.isSafeInteger(value.releaseId) || value.releaseId < 1)) || typeof value.publishedAt !== 'string' ||
    !Number.isFinite(Date.parse(value.publishedAt)) || !value.publishedAt.endsWith('Z') ||
    !Number.isSafeInteger(value.runId) || value.runId < 1 || !Number.isSafeInteger(value.attempt) || value.attempt < 1)
    throw new Error('Invalid publication provenance');
  if(value.provider==='artifact-branch'){
    if(!/^[a-f0-9]{40}$/.test(value.artifactCommit)||!/^[a-f0-9]{64}$/.test(value.manifestSha256))throw new Error('Invalid artifact provenance');
    safeRelative(value.artifactBranch);if(!value.artifactBranch.startsWith('Development_Build/'))throw new Error('Invalid artifact branch');
  }
  if(value.buildStartedAt!==undefined && (typeof value.buildStartedAt!=='string' || !Number.isFinite(Date.parse(value.buildStartedAt)) || !value.buildStartedAt.endsWith('Z')))
    throw new Error('Invalid build time provenance');
  return value;
}

// Build IDs are opaque identities, not sortable version numbers. The caller
// must supply an authenticated per-module latest catalog before using this result.
export function updateStatus(installed, latest) {
  if (!latest || typeof latest.buildId !== 'string' || !latest.buildId) return 'unavailable';
  if (!installed) return 'not-installed';
  if (installed.moduleId !== latest.moduleId) throw new Error('Module identity mismatch');
  return installed.buildId === latest.buildId ? 'current' : 'different-build';
}

// Pure preflight only: this module deliberately does not write game files.
// A future executor must recheck hashes, reparse points and root containment
// immediately before mutation; a plan alone is not installation authorization.
export function installationPlan(root, pkg, existingPaths = []) {
  if (typeof root !== 'string' || !path.win32.isAbsolute(root)) throw new Error('Explicit absolute Windows root required');
  const files = verifyPackage(pkg);
  const occupied = new Set(existingPaths.map(p => safeRelative(p).toLowerCase()));
  return files.map(file => {
    const relative = `${pkg.moduleId}/${file.path}`;
    if (occupied.has(relative.toLowerCase())) throw new Error('Existing file must not be overwritten');
    return { ...file, relative, destination: path.win32.join(root, ...relative.split('/')) };
  });
}

export function uninstallPlan(record, observed) {
  if (!record || !Array.isArray(record.files)) throw new Error('Missing ownership record');
  safeModuleId(record.moduleId);
  const seen = new Set();
  return record.files.map(file => {
    const relative = safeRelative(file.relative);
    if (!/^[a-f0-9]{64}$/.test(file.sha256)) throw new Error('Invalid ownership checksum');
    if (!relative.startsWith(`${record.moduleId}/`) || seen.has(relative.toLowerCase())) throw new Error('Invalid ownership record');
    seen.add(relative.toLowerCase());
    const actual = observed[relative];
    if (!actual || actual.reparsePoint || actual.sha256 !== file.sha256) throw new Error('Changed or unverifiable owned file');
    return relative;
  });
}

export function coexistenceWarning(stableInstalled, developmentInstalled) {
  return stableInstalled && developmentInstalled ? 'Stable and development copies are both installed.' : null;
}

// Experimental offline contract. Trust/freshness of the external policy is a
// caller responsibility; this does not discover or authenticate a live catalog.
export function catalogStatus(installed, policy, data) {
  let reason = 'invalid-policy';
  const require = condition => { if (!condition) throw new Error('Invalid catalog contract'); };
  const text = value => typeof value === 'string' && value.length > 0 && value.length <= 256 && !/[\x00-\x1f\x7f]/.test(value);
  const hash = value => typeof value === 'string' && /^[a-f0-9]{64}$/.test(value);
  const positiveId = value => Number.isSafeInteger(value) && value > 0;
  try {
    require(policy && hash(policy.catalogSha256));
    safeModuleId(policy.moduleId);
    require(typeof policy.repository === 'string' && /^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(policy.repository));
    require(policy.repository.split('/').every(part => part !== '.' && part !== '..'));
    for (const key of ['branch', 'assembly', 'workflow']) require(text(policy[key]));
    require(Array.isArray(policy.requiredFiles) && policy.requiredFiles.length > 0 && policy.requiredFiles.length <= 256);
    const required = new Set();
    for (const name of policy.requiredFiles) {
      safeRelative(name); require(!required.has(name.toLowerCase())); required.add(name.toLowerCase());
    }
    require(policy.requiredFiles.includes(`${policy.assembly}.dll`));
    reason = 'unverified-catalog';
    require(Buffer.isBuffer(data) && data.length > 0 && data.length <= 1024 * 1024);
    require(createHash('sha256').update(data).digest('hex') === policy.catalogSha256);
    reason = 'invalid-catalog';
    const catalog = JSON.parse(data.toString('utf8'));
    require(catalog && catalog.schema === 1);
    for (const key of ['moduleId', 'repository', 'branch', 'assembly', 'workflow']) require(catalog[key] === policy[key]);
    require(catalog.channel === 'development' && catalog.devTool === true && text(catalog.buildId));
    require(typeof catalog.sourceSha === 'string' && /^[a-f0-9]{40}$/.test(catalog.sourceSha));
    const release = catalog.release;
    require(release && positiveId(release.id) && release.draft === false && text(release.tag));
    require(release.tag !== '.' && release.tag !== '..');
    require(typeof release.publishedAt === 'string' && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/.test(release.publishedAt) &&
      Number.isFinite(Date.parse(release.publishedAt)) && new Date(release.publishedAt).toISOString() === release.publishedAt.replace('Z', '.000Z'));
    require(Array.isArray(catalog.files) && catalog.files.length === required.size);
    const names = new Set(); const ids = new Set(); const assets = new Set(); let total = 0;
    for (const file of catalog.files) {
      const name = safeRelative(file.path);
      require(required.has(name.toLowerCase()) && !names.has(name.toLowerCase())); names.add(name.toLowerCase());
      require(positiveId(file.assetId) && !ids.has(file.assetId)); ids.add(file.assetId);
      safeRelative(file.assetName);
      require(!file.assetName.includes('/') && !assets.has(file.assetName.toLowerCase())); assets.add(file.assetName.toLowerCase());
      require(positiveId(file.size) && file.size <= 32 * 1024 * 1024 && hash(file.sha256)); total += file.size;
      require(file.url === `https://github.com/${policy.repository}/releases/download/${encodeURIComponent(release.tag)}/${encodeURIComponent(file.assetName)}`);
      // Parsing must not normalize the accepted spelling into another location.
      const parsed = new URL(file.url);
      require(parsed.origin === 'https://github.com' && parsed.href === file.url &&
        parsed.pathname === `/${policy.repository}/releases/download/${encodeURIComponent(release.tag)}/${encodeURIComponent(file.assetName)}`);
    }
    require(total <= 64 * 1024 * 1024);
    for (const name of names) {
      const parts = name.split('/');
      for (let i = 1; i < parts.length; i++) require(!names.has(parts.slice(0, i).join('/')));
    }
    reason = 'invalid-installed-record';
    if (installed !== null && installed !== undefined)
      require(installed.moduleId === policy.moduleId && text(installed.buildId));
    return { status: updateStatus(installed, catalog), moduleId: policy.moduleId,
      selectedBuildId: catalog.buildId, catalog: structuredClone(catalog) };
  } catch {
    // Deliberately omit untrusted URLs, credentials and raw parser diagnostics.
    return { status: 'unavailable', reason };
  }
}
