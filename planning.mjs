// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Pure snapshot planning only. No filesystem, game process or execution adapter.
import { safeModuleId, safeRelative, verifyPackage, catalogStatus } from './core.mjs';

const digest = value => typeof value === 'string' && /^[a-f0-9]{64}$/.test(value);
const require = condition => { if (!condition) throw new Error('Invalid planning input'); };
function pathMap(files) {
  require(Array.isArray(files) && files.length <= 512);
  const result = new Map();
  for (const file of files) {
    const name = safeRelative(file.path).toLowerCase();
    require(!result.has(name) && file.reparsePoint === false);
    require(file.kind === 'directory' || file.kind === 'file' && file.hardLink === false && digest(file.sha256));
    result.set(name, file);
  }
  for (const name of result.keys()) {
    const parts = name.split('/');
    for (let i = 1; i < parts.length; i++) require(result.get(parts.slice(0, i).join('/'))?.kind === 'directory');
  }
  return result;
}
function ownedTreeMatches(module) {
  const record = module.ownership;
  if (!record || record.moduleId !== module.moduleId || typeof record.buildId !== 'string' || !record.buildId || record.buildId.length > 256 ||
    !Array.isArray(record.files) || record.files.length === 0 || record.files.length > 256) return false;
  const expected = new Map();
  for (const file of record.files) {
    const name = safeRelative(file.path).toLowerCase();
    if (expected.has(name) || !digest(file.sha256)) return false;
    expected.set(name, { kind: 'file', sha256: file.sha256 });
  }
  for (const name of [...expected.keys()]) {
    const parts = name.split('/');
    for (let i = 1; i < parts.length; i++) {
      const parent = parts.slice(0, i).join('/');
      if (expected.get(parent)?.kind === 'file') return false;
      expected.set(parent, { kind: 'directory' });
    }
  }
  const observed = pathMap(module.files);
  return expected.size === observed.size && [...expected].every(([name, file]) =>
    observed.get(name)?.kind === file.kind && (file.kind === 'directory' || observed.get(name).sha256 === file.sha256));
}

export function planModuleOperations({ requests, observation, rules } = {}) {
  const blockers = []; const warnings = ['REAL_EXECUTION_DISABLED', 'LIVE_RECHECK_REQUIRED', 'DLL_IDENTITY_UNVERIFIED'];
  const result = steps => ({ modelReady: blockers.length === 0, executionAllowed: false,
    snapshotId: typeof observation?.snapshotId === 'string' && observation.snapshotId.length <= 256 &&
      !/[\x00-\x1f\x7f]/.test(observation.snapshotId) ? observation.snapshotId : null,
    blockers, warnings, steps: blockers.length ? [] : steps });
  const block = (code, moduleId) => blockers.push(moduleId ? { code, moduleId } : { code });
  try {
    require(Array.isArray(requests) && requests.length > 0 && requests.length <= 16);
    require(observation?.schema === 1 && typeof observation.snapshotId === 'string' && observation.snapshotId.length > 0 && observation.snapshotId.length <= 256);
    if (observation.complete !== true || observation.rootSafe !== true || observation.gameRunning !== false) {
      block('OBSERVATION_UNAVAILABLE_OR_UNSAFE'); return result([]);
    }
    require(Array.isArray(observation.modules) && observation.modules.length <= 256);
    const modules = new Map();
    for (const module of observation.modules) {
      safeModuleId(module.moduleId); require(!modules.has(module.moduleId));
      require(typeof module.directoryPresent === 'boolean' && Object.hasOwn(module, 'ownership') && Array.isArray(module.files));
      if (module.reparsePoint !== false) block('UNSAFE_MODULE_OBJECT', module.moduleId);
      pathMap(module.files);
      if (!module.directoryPresent && (module.ownership !== null || module.files.length > 0)) block('INCONSISTENT_OBSERVATION', module.moduleId);
      modules.set(module.moduleId, module);
      if (module.stableInstalled === true && module.directoryPresent) warnings.push(`COEXISTENCE:${module.moduleId}`);
    }
    if (rules?.complete !== true || !Array.isArray(rules.conflicts) || !Array.isArray(rules.requires) ||
      rules.conflicts.length > 512 || rules.requires.length > 512) {
      block('COMPATIBILITY_RULES_UNAVAILABLE'); return result([]);
    }
    const selected = new Map(); const steps = new Map();
    for (const request of requests) {
      const id = safeModuleId(request.moduleId);
      if (selected.has(id)) { block('DUPLICATE_TARGET', id); continue; }
      selected.set(id, request.operation);
      const module = modules.get(id);
      if (!module) { block('MODULE_OBSERVATION_MISSING', id); continue; }
      if (request.operation === 'install') {
        if (module.stableInstalled === true && !module.directoryPresent) warnings.push(`COEXISTENCE:${id}`);
        if (module.directoryPresent || module.ownership !== null) block('EXISTING_DESTINATION', id);
        const source = request.source;
        const catalog = source?.policy?.moduleId === id ? catalogStatus(null, source.policy, source.data).catalog : null;
        if (!catalog) { block('SOURCE_UNAVAILABLE', id); continue; }
        try {
          const pkg = request.package; const files = verifyPackage(pkg);
          require(pkg.moduleId === id && pkg.buildId === catalog.buildId && files.length === catalog.files.length);
          for (const file of catalog.files) {
            const actual = pkg.files.find(candidate => candidate.path === file.path);
            require(actual && actual.sha256 === file.sha256 && actual.bytes.length === file.size);
          }
          steps.set(id, { moduleId: id, operation: 'install', mode: 'stage-verify-activate', files });
        } catch { block('PACKAGE_SOURCE_MISMATCH', id); }
      } else if (request.operation === 'uninstall') {
        if (!module.directoryPresent || !ownedTreeMatches(module)) { block('OWNERSHIP_OR_CONTENT_MISMATCH', id); continue; }
        steps.set(id, { moduleId: id, operation: 'uninstall', mode: 'quarantine-owned-directory',
          files: module.ownership.files.map(({ path, sha256 }) => ({ path, sha256 })) });
      } else block('UNSUPPORTED_OPERATION', id);
    }
    const present = new Set([...modules].filter(([, module]) => module.directoryPresent).map(([id]) => id));
    for (const [id, operation] of selected) {
      if (operation === 'install') present.add(id); else if (operation === 'uninstall') present.delete(id);
    }
    const checkRuleId = id => { safeModuleId(id); if (!modules.has(id)) block('RULE_OBSERVATION_MISSING', id); };
    for (const pair of rules.conflicts) {
      require(Array.isArray(pair) && pair.length === 2 && pair[0] !== pair[1]);
      pair.forEach(checkRuleId);
      if (present.has(pair[0]) && present.has(pair[1])) blockers.push({ code: 'CONFLICT_PAIR', moduleIds: [...pair] });
    }
    const edges = new Map([...selected.keys()].map(id => [id, new Set()]));
    for (const rule of rules.requires) {
      checkRuleId(rule.moduleId); checkRuleId(rule.dependencyId); require(rule.moduleId !== rule.dependencyId);
      if (present.has(rule.moduleId) && !present.has(rule.dependencyId)) block('DEPENDENCY_MISSING', rule.moduleId);
      if (selected.get(rule.moduleId) === 'install' && selected.get(rule.dependencyId) === 'install') edges.get(rule.dependencyId).add(rule.moduleId);
      if (selected.get(rule.moduleId) === 'uninstall' && selected.get(rule.dependencyId) === 'uninstall') edges.get(rule.moduleId).add(rule.dependencyId);
    }
    const ordered = []; const remaining = new Set(selected.keys());
    while (remaining.size) {
      const available = [...remaining].filter(id => ![...remaining].some(parent => edges.get(parent).has(id)))
        .sort((a, b) => Number(selected.get(b) === 'uninstall') - Number(selected.get(a) === 'uninstall'));
      if (!available.length) { block('DEPENDENCY_CYCLE'); break; }
      const id = available[0]; remaining.delete(id); if (steps.has(id)) ordered.push(steps.get(id));
    }
    return result(ordered);
  } catch {
    block('INVALID_PLANNING_INPUT'); return result([]);
  }
}
