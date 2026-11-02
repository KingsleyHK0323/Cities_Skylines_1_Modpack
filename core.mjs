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
