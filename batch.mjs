// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Pure assessment and injected, memory-only package preparation. Never installs.
import { catalogStatus, safeModuleId, verifyPackage } from './core.mjs';
import { downloadCatalogAsset } from './download.mjs';

class BatchFailure extends Error {
  constructor(code, moduleId) {
    super(`Batch unavailable: ${code}`); this.code = code;
    if (moduleId) this.moduleId = moduleId;
  }
}
const blocked = reason => ({ canPrepare: false, installAllowed: false, reason, previewRecords: [], totalBytes: 0 });
function snapshot(entries) {
  if (!Array.isArray(entries) || entries.length === 0 || entries.length > 16)
    return { assessment: blocked('INVALID_BATCH') };
  const seen = new Set(); const copies = []; const previewRecords = []; let totalBytes = 0;
  try {
    for (const entry of entries) {
      safeModuleId(entry.moduleId);
      if (seen.has(entry.moduleId)) return { assessment: blocked('DUPLICATE_MODULE') };
      seen.add(entry.moduleId);
      const copy = { moduleId: entry.moduleId, policy: structuredClone(entry.policy),
        data: Buffer.isBuffer(entry.data) && entry.data.length <= 1024 * 1024 ? Buffer.from(entry.data) : null,
        installed: structuredClone(entry.installed) };
      const result = copy.policy?.moduleId !== copy.moduleId || !Object.hasOwn(entry, 'installed') ?
        { status: 'unavailable', reason: 'missing-or-mismatched-module-record' } : catalogStatus(copy.installed, copy.policy, copy.data);
      copy.catalog = result.catalog; copies.push(copy);
      if (result.catalog) totalBytes += result.catalog.files.reduce((sum, file) => sum + file.size, 0);
      previewRecords.push({ id: copy.moduleId, name: copy.moduleId, category: 'Pinned snapshot assessment',
        synthetic: entry.synthetic === true, status: result.status, installedBuild: copy.installed?.buildId ?? null,
        catalogBuild: result.selectedBuildId ?? null,
        reason: result.status === 'unavailable' ? `Source unavailable: ${result.reason}. No preparation is allowed.` :
          'Snapshot matches the supplied policy pin. Publisher authentication and freshness are external requirements.',
        coexistenceWarning: entry.stableInstalled === true && copy.installed != null ?
          'Stable and development copies may coexist. Warning only; no loading or subscription settings change.' : null });
    }
  } catch { return { assessment: blocked('INVALID_BATCH') }; }
  const canPrepare = previewRecords.every(item => item.status !== 'unavailable') && totalBytes <= 128 * 1024 * 1024;
  return { copies, assessment: { canPrepare, installAllowed: false, totalBytes, previewRecords,
    reason: canPrepare ? 'INJECTED_PREPARATION_ONLY' : totalBytes > 128 * 1024 * 1024 ? 'BATCH_SIZE_LIMIT' : 'SOURCE_UNAVAILABLE' } };
}

export function inspectModuleBatch(entries) { return snapshot(entries).assessment; }

export async function prepareModuleBatch(entries, { transport, totalTimeoutMs = 10000 } = {}) {
  const { copies, assessment } = snapshot(entries);
  if (!assessment.canPrepare) throw new BatchFailure('BATCH_UNAVAILABLE');
  if (typeof transport !== 'function' || !Number.isInteger(totalTimeoutMs) || totalTimeoutMs < 1 || totalTimeoutMs > 30000)
    throw new BatchFailure('INVALID_BATCH_POLICY');
  const deadline = performance.now() + totalTimeoutMs; const packages = [];
  for (const copy of copies) {
    const pkg = { moduleId: copy.moduleId, buildId: copy.catalog.buildId, files: [] };
    for (const file of copy.catalog.files) {
      const remaining = Math.floor(deadline - performance.now());
      if (remaining < 1) throw new BatchFailure('BATCH_TIMEOUT', copy.moduleId);
      try {
        const result = await downloadCatalogAsset({ policy: copy.policy, data: copy.data,
          assetPath: file.path, transport, timeoutMs: Math.min(5000, remaining) });
        pkg.files.push({ path: file.path, sha256: result.sha256, bytes: result.bytes });
      } catch (error) {
        throw new BatchFailure(error?.code === 'TIMEOUT' ? 'BATCH_TIMEOUT' : 'BATCH_ASSET_FAILED', copy.moduleId);
      }
    }
    try { verifyPackage(pkg); } catch { throw new BatchFailure('BATCH_PACKAGE_FAILED', copy.moduleId); }
    packages.push(pkg);
  }
  if (performance.now() >= deadline) throw new BatchFailure('BATCH_TIMEOUT');
  // A late failure exposes none of the earlier packages. No disk or game state changes.
  return { state: 'verified-in-memory', installAllowed: false, packages, assessment };
}
