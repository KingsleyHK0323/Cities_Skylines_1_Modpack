// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Experimental synchronous executor, exercised only in isolated temporary roots.
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import { createHash, randomUUID } from 'node:crypto';
import { isDeepStrictEqual } from 'node:util';
import { safeRelative, safeModuleId, verifyPackage, verifyPublication } from './core.mjs';

const digest = data => createHash('sha256').update(data).digest('hex');
const exists = p => { try { fs.lstatSync(p); return true; } catch (e) { if (e.code === 'ENOENT') return false; throw e; } };
const sameIdentity = (a, b) => a.dev === b.dev && a.ino === b.ino;
const marker = 'CS1_DEV_MANAGER_ISOLATED_V1\n';
const selectedModsAuthority = Symbol('explicit-user-selected-mods');
export function metadataForModsDirectory(modsRoot) {
  const root = path.resolve(modsRoot);
  const identity = process.platform === 'win32' ? root.toLowerCase() : root;
  return path.join(path.dirname(root), `CS1DevManagerData-${digest(Buffer.from(identity)).slice(0, 16)}`);
}

function inspect(p, directory) {
  const stat = fs.lstatSync(p, { bigint: true });
  if (stat.isSymbolicLink() || (directory ? !stat.isDirectory() : !stat.isFile() || stat.nlink !== 1n))
    throw new Error('Unsafe link or filesystem object');
  return stat;
}

function directoryChain(p) {
  const absolute = path.resolve(p);
  const parsed = path.parse(absolute);
  let current = parsed.root;
  inspect(current, true);
  for (const part of absolute.slice(parsed.root.length).split(path.sep).filter(Boolean)) {
    current = path.join(current, part); inspect(current, true);
  }
}

function readRegular(p) {
  const before = inspect(p, false);
  if (before.size > 32n * 1024n * 1024n) throw new Error('Oversized managed file');
  const fd = fs.openSync(p, 'r');
  try {
    const opened = fs.fstatSync(fd, { bigint: true });
    if (!sameIdentity(before, opened) || opened.nlink !== 1n || opened.size !== before.size) throw new Error('File changed while opening');
    // Read at most the original size plus one byte, even if another writer grows it.
    const buffer = Buffer.alloc(Number(before.size) + 1);
    let count = 0;
    while (count < buffer.length) {
      const read = fs.readSync(fd, buffer, count, buffer.length - count, count);
      if (!read) break;
      count += read;
    }
    if (BigInt(count) !== before.size) throw new Error('File length changed during read');
    const data = buffer.subarray(0, count);
    if (!sameIdentity(opened, inspect(p, false))) throw new Error('File path changed while reading');
    return data;
  } finally { fs.closeSync(fd); }
}

function writeNew(p, data, onCreated = () => {}) {
  const fd = fs.openSync(p, 'wx', 0o600);
  try { onCreated(); fs.writeFileSync(fd, data); fs.fsyncSync(fd); } finally { fs.closeSync(fd); }
}

function tree(directory) {
  inspect(directory, true);
  const files = new Map();
  function walk(current, relative) {
    const names = new Set();
    for (const name of fs.readdirSync(current)) {
      safeRelative(name);
      if (names.has(name.toLowerCase())) throw new Error('Case-insensitive filesystem collision');
      names.add(name.toLowerCase());
      const target = path.join(current, name);
      const stat = fs.lstatSync(target);
      if (stat.isSymbolicLink()) throw new Error('Link in module directory');
      const key = relative ? `${relative}/${name}` : name;
      if (stat.isDirectory()) { walk(target, key); }
      else { files.set(key, digest(readRegular(target))); }
    }
  }
  walk(directory, '');
  return files;
}

function verifyTree(directory, record) {
  const actual = tree(directory);
  const expected = new Map(record.files.map(f => [f.path, f.sha256]));
  if (actual.size !== expected.size || [...expected].some(([name, hash]) => actual.get(name) !== hash))
    throw new Error('User changes or missing owned files; preserving installation');
  // Unexpected empty directories also count as user data.
  const expectedDirs = new Set();
  for (const name of expected.keys()) {
    const parts = name.split('/');
    for (let i = 1; i < parts.length; i++) expectedDirs.add(parts.slice(0, i).join('/'));
  }
  function checkDirs(current, relative = '') {
    for (const name of fs.readdirSync(current)) {
      const p = path.join(current, name); const key = relative ? `${relative}/${name}` : name;
      if (fs.lstatSync(p).isDirectory()) {
        if (!expectedDirs.has(key)) throw new Error('Unrecorded directory; preserving installation');
        checkDirs(p, key);
      }
    }
  }
  checkDirs(directory);
}

export class IsolatedInstaller {
  static forModsDirectory(modsRoot, { confirmed = false, create = false, fault = () => {} } = {}) {
    if (!confirmed || typeof modsRoot !== 'string' || !path.isAbsolute(modsRoot) || modsRoot.startsWith('\\\\'))
      throw new Error('Explicit local Mods-directory confirmation is required');
    const parent = path.resolve(modsRoot);
    if (parent === path.parse(parent).root) throw new Error('A volume root cannot be a Mods destination');
    directoryChain(parent);
    const metadata = metadataForModsDirectory(parent);
    if (exists(metadata)) {
      directoryChain(metadata);
      if (readRegular(path.join(metadata, 'marker')).toString() !== marker)
        throw new Error('Unknown existing manager directory; preserving files');
    } else {
      if (fs.readdirSync(parent).some(name => name.toLowerCase().startsWith('cs1devmanager-')))
        throw new Error('Unknown manager-prefixed directory; preserving files');
      if (!create) return null;
    }
    return new IsolatedInstaller(parent, { fault, authority: selectedModsAuthority });
  }

  constructor(root, { fault = () => {}, authority = null } = {}) {
    if (typeof root !== 'string' || !path.isAbsolute(root)) throw new Error('Explicit absolute root required');
    this.root = path.resolve(root);
    // Ordinary construction stays isolated; production requires the explicit factory.
    if (authority !== selectedModsAuthority && (path.dirname(this.root) !== path.resolve(os.tmpdir()) ||
        !/^cs1-dev-manager-test-[A-Za-z0-9]+$/.test(path.basename(this.root))))
      throw new Error('Only an isolated manager test directory under the current temporary root is supported');
    directoryChain(this.root);
    this.rootIdentity = inspect(this.root, true);
    this.directoryPrefix = authority === selectedModsAuthority ? 'CS1DevManager-' : '';
    this.meta = authority === selectedModsAuthority ? metadataForModsDirectory(this.root) : path.join(this.root, '.dev-manager');
    this.fault = fault;
    const fresh = !exists(this.meta);
    if (fresh) fs.mkdirSync(this.meta);
    inspect(this.meta, true);
    this.metaIdentity = inspect(this.meta, true);
    if (fresh) {
      writeNew(path.join(this.meta, 'marker'), marker);
      fs.mkdirSync(path.join(this.meta, 'staging'));
      fs.mkdirSync(path.join(this.meta, 'backups'));
      writeNew(path.join(this.meta, 'state.json'), JSON.stringify({ schema: 1, root: this.root, modules: {} }));
    }
    if (readRegular(path.join(this.meta, 'marker')).toString() !== marker) throw new Error('Unknown manager directory');
    this.check();
    if (exists(path.join(this.meta, 'journal.json'))) throw new Error('Incomplete transaction requires manual recovery');
    this.load();
  }

  check() {
    directoryChain(this.root);
    directoryChain(this.meta);
    if (!sameIdentity(inspect(this.root, true), this.rootIdentity) ||
        !sameIdentity(inspect(this.meta, true), this.metaIdentity)) throw new Error('Root identity changed');
    for (const sub of ['staging', 'backups']) inspect(path.join(this.meta, sub), true);
  }

  load() {
    this.check();
    this.stateBytes = readRegular(path.join(this.meta, 'state.json'));
    if (this.stateBytes.length > 4 * 1024 * 1024) throw new Error('Oversized ownership state');
    const state = JSON.parse(this.stateBytes.toString());
    if (state.schema !== 1 || state.root !== this.root || !state.modules || Array.isArray(state.modules))
      throw new Error('Invalid ownership state');
    for (const [id, record] of Object.entries(state.modules)) {
      safeModuleId(id);
      if (record.publication !== undefined) verifyPublication(record.publication);
      if (record.moduleId !== id || typeof record.buildId !== 'string' || !Array.isArray(record.files) ||
          !record.files.length || record.files.length > 256) throw new Error('Invalid module ownership state');
      const seen = new Set();
      for (const f of record.files) {
        safeRelative(f.path);
        if (!/^[a-f0-9]{64}$/.test(f.sha256) || seen.has(f.path.toLowerCase())) throw new Error('Invalid ownership file');
        seen.add(f.path.toLowerCase());
      }
    }
    this.state = state;
  }

  installed(id) {
    safeModuleId(id); this.load();
    return Object.hasOwn(this.state.modules, id) ? structuredClone(this.state.modules[id]) : null;
  }

  audit(id) {
    safeModuleId(id); this.load();
    const record = Object.hasOwn(this.state.modules, id) ? this.state.modules[id] : null;
    const folder = this.directoryPrefix + id;
    const occupied = fs.readdirSync(this.root).filter(name => name.toLowerCase() === folder.toLowerCase());
    if (record) {
      if (occupied.length !== 1 || occupied[0] !== folder) throw new Error('Owned module directory is missing or ambiguous');
      verifyTree(path.join(this.root, folder), record);
    } else if (occupied.length) throw new Error('Unknown existing module directory; preserving files');
    return { moduleId: id, installed: record ? structuredClone(record) : null };
  }

  install(pkg, expected, verifyFreshness) { return this.transact('install', pkg.moduleId, pkg, expected, verifyFreshness); }
  update(pkg, expected, verifyFreshness) { return this.transact('update', pkg.moduleId, pkg, expected, verifyFreshness); }
  uninstall(id, expected) { return this.transact('uninstall', id, undefined, expected); }

  transact(operation, id, input, expected, verifyFreshness) {
    safeModuleId(id);
    // Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
    if (expected === undefined || (operation === 'install' ? expected !== null :
        !expected || typeof expected !== 'object' || Array.isArray(expected) || expected.moduleId !== id))
      throw new Error('INSTALLATION_SNAPSHOT_REQUIRED');
    if (verifyFreshness !== undefined && typeof verifyFreshness !== 'function') throw new Error('INVALID_SOURCE_FRESHNESS_GUARD');
    let approved;
    try { approved = structuredClone(expected); } catch { throw new Error('INSTALLATION_SNAPSHOT_REQUIRED'); }
    // Snapshot caller buffers; later callbacks cannot substitute bytes after verification.
    const pkg = input && { moduleId: id, buildId: input.buildId, ...(input.publication ? { publication: structuredClone(input.publication) } : {}),
      files: input.files?.map(f => ({ ...f, bytes: Buffer.isBuffer(f.bytes) ? Buffer.from(f.bytes) : f.bytes })) };
    const record = pkg && { moduleId: id, buildId: pkg.buildId, files: verifyPackage(pkg), ...(pkg.publication ? { publication: pkg.publication } : {}) };
    this.check();
    const lock = path.join(this.meta, 'lock'); const token = randomUUID();
    let lockMayRemain = false;
    try { writeNew(lock, token, () => { lockMayRemain = true; }); }
    catch (error) {
      // Never inspect or remove a partially written lock or an existing foreign lock.
      const failure = new AggregateError([error], String(error?.message ?? error));
      Object.assign(failure, { committed: false, lockMayRemain, lockAcquisitionFailed: true,
        lockReleaseFailed: false, recoveryRequired: lockMayRemain, operation, moduleId: id });
      throw failure;
    }
    const journal = path.join(this.meta, 'journal.json');
    let backedUp = false; let activated = false; let committed = false; let journalCreated = false;
    let journalMayRemain = false;
    const errors = []; let result; let lockReleaseFailed = false;
    const folder = this.directoryPrefix + id;
    const active = path.join(this.root, folder);
    const stage = path.join(this.meta, 'staging', token);
    const backup = path.join(this.meta, 'backups', token);
    try {
      if (exists(journal)) throw new Error('Incomplete transaction requires manual recovery');
      this.load();
      const old = Object.hasOwn(this.state.modules, id) ? this.state.modules[id] : null;
      if (!isDeepStrictEqual(old, approved)) throw new Error('INSTALLATION_STATE_CHANGED');
      if (verifyFreshness) verifyFreshness();
      const occupied = fs.readdirSync(this.root).filter(name => name.toLowerCase() === folder.toLowerCase());
      if (operation === 'install' ? old || occupied.length : !old || occupied.length !== 1 || occupied[0] !== folder)
        throw new Error('Existing or unowned module destination');
      if (old) verifyTree(active, old);
      if (pkg) {
        fs.mkdirSync(stage);
        for (const f of pkg.files) {
          const target = path.join(stage, ...f.path.split('/'));
          fs.mkdirSync(path.dirname(target), { recursive: true });
          directoryChain(path.dirname(target));
          writeNew(target, f.bytes);
        }
        verifyTree(stage, record);
      }
      this.fault('staged');
      this.check();
      if (old) verifyTree(active, old);
      else if (exists(active)) throw new Error('Destination appeared during staging');
      if (pkg) verifyTree(stage, record);
      if (verifyFreshness) verifyFreshness();
      writeNew(journal, JSON.stringify({ schema: 1, token, operation, id, stage, backup, active }),
        () => { journalMayRemain = true; });
      journalCreated = true;
      journalMayRemain = false;
      if (old) {
        if (exists(backup)) throw new Error('Backup destination already exists');
        fs.renameSync(active, backup); backedUp = true;
      }
      this.fault('backed-up');
      this.check();
      if (pkg) {
        if (exists(active)) throw new Error('Destination appeared before activation');
        verifyTree(stage, record);
        fs.renameSync(stage, active); activated = true;
      }
      this.fault('activated');
      this.fault('before-state');
      this.check();
      if (pkg) verifyTree(active, record);
      else if (exists(active)) throw new Error('Destination appeared during uninstall');
      if (backedUp) verifyTree(backup, old);
      if (!readRegular(path.join(this.meta, 'state.json')).equals(this.stateBytes)) throw new Error('Ownership state changed');
      const next = structuredClone(this.state);
      if (pkg) Object.defineProperty(next.modules, id, { value: record, enumerable: true, configurable: true, writable: true });
      else delete next.modules[id];
      const stateTemp = path.join(this.meta, `state-${token}.json`);
      writeNew(stateTemp, JSON.stringify(next));
      this.check();
      fs.renameSync(stateTemp, path.join(this.meta, 'state.json'));
      committed = true;
      fs.unlinkSync(journal); journalCreated = false;
      this.load();
      result = { operation, moduleId: id, buildId: record?.buildId ?? null,
        backup: backedUp ? backup : null };
    } catch (error) {
      errors.push(error);
      if (!committed && journalCreated) {
        try {
          this.check();
          if (activated) {
            inspect(active, true);
            fs.renameSync(active, path.join(this.meta, 'backups', `${token}-failed`));
          }
          if (backedUp) {
            if (exists(active)) throw new Error('Recovery destination occupied');
            inspect(backup, true); fs.renameSync(backup, active);
          }
          fs.unlinkSync(journal); journalCreated = false;
        } catch (recoveryError) {
          errors.push(recoveryError);
        }
      }
    } finally {
      try {
        this.check();
        if (readRegular(lock).toString() !== token) throw new Error('Lock changed; retained for inspection');
        fs.unlinkSync(lock);
      } catch (releaseError) {
        lockReleaseFailed = true;
        errors.push(releaseError);
      }
    }
    if (errors.length) {
      // Preserve original errors, including frozen objects, without unsafe recovery writes.
      const failure = new AggregateError(errors, errors.map(error => String(error?.message ?? error)).join('; '));
      Object.assign(failure, { committed, lockReleaseFailed, operation, moduleId: id,
        recoveryRequired: journalCreated || journalMayRemain });
      throw failure;
    }
    return result;
  }
}
