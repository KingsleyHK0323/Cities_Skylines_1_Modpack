// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import readline from 'node:readline';
import { createHash } from 'node:crypto';
import { PassThrough } from 'node:stream';
import { IsolatedInstaller, metadataForModsDirectory } from './filesystem.mjs';
import { startBackend } from './desktop-backend.mjs';
import { DesktopService } from './desktop-service.mjs';
import { SettingsStore } from './settings-store.mjs';

test('destination recovery: inaccessible saved location must not prevent the command reader starting', async t => {
  const original = { mode: 'game-mods', modsRoot: path.resolve('__synthetic_missing_mods__'), custom: { preserved: true } };
  const before = structuredClone(original); let updates = 0; let readerStarted = 0;
  const settings = { load: () => structuredClone(original), update: () => { updates++; throw new Error('UNEXPECTED_SETTINGS_WRITE'); } };
  t.mock.method(IsolatedInstaller, 'forModsDirectory', () => { throw Object.assign(new Error('SYNTHETIC_UNAVAILABLE'), { code: 'EACCES' }); });
  t.mock.method(readline, 'createInterface', () => { readerStarted++; return { on: () => {} }; });
  // Retained original regression invocation: await assert.doesNotReject(startBackend({ settings }));
  await assert.doesNotReject(startBackend({ settings, serviceFactory: () => ({ provider: null }) }));
  assert.equal(readerStarted, 1); assert.equal(updates, 0); assert.deepEqual(original, before);
});

function recoveryFixture({ failure = 'EACCES', initialMode = 'game-mods', invalidPathType = false,
  noExistingMetadata = false, saveFailure = null, externalChange = false, language = 'en', noSandbox = false,
  saveFailureAfterTransform = false, freshSandboxInitFailure = false } = {}) {
  const oldPath = path.resolve('__synthetic_recovery__', 'unavailable');
  const newPath = path.resolve('__synthetic_recovery__', 'replacement');
  const unsafePath = path.resolve('__synthetic_recovery__', 'unsafe');
  const sandboxPath = path.resolve('__synthetic_recovery__', 'old-sandbox');
  const freshSandboxPath = path.resolve('__synthetic_recovery__', 'new-sandbox');
  let saved = { mode: initialMode, modsRoot: invalidPathType ? { invalid: true } : oldPath,
    sandbox: noSandbox ? null : sandboxPath, preferences: { language, theme: 'system' }, custom: { preserved: true } };
  const original = structuredClone(saved); const calls = { loads: 0, updates: 0, probes: [], gameChecks: 0, network: 0, downloads: 0, sandboxCreates: 0, events: [] };
  let sandboxAvailable = false; let oldModsAvailable = noExistingMetadata; let freshSandboxReady = false;
  const input = new PassThrough(); const output = new PassThrough(); const pending = new Map(); let buffer = ''; let nextId = 1;
  output.on('data', bytes => {
    buffer += bytes.toString('utf8'); let split;
    while ((split = buffer.indexOf('\n')) >= 0) {
      const line = buffer.slice(0, split); buffer = buffer.slice(split + 1); if (!line) continue;
      const reply = JSON.parse(line); const waiter = pending.get(reply.id);
      if (waiter) { pending.delete(reply.id); clearTimeout(waiter.timer); waiter.resolve(reply); }
    }
  });
  const command = fields => new Promise((resolve, reject) => {
    const id = nextId++; const timer = setTimeout(() => { pending.delete(id); reject(new Error('SYNTHETIC_REPLY_TIMEOUT')); }, 3000);
    pending.set(id, { resolve, reject, timer }); input.write(JSON.stringify({ ...fields, id }) + '\n');
  });
  const unavailable = () => { const error = new Error('SYNTHETIC_DESTINATION_' + failure); error.code = failure; throw error; };
  class FixtureInstaller {
    constructor(location) {
      this.rootIdentity = { dev: 1n, ino: 4n }; this.metaIdentity = { dev: 1n, ino: 3n };
      calls.probes.push({ kind: 'sandbox', location }); calls.events.push('sandbox-initialize');
      if (location === freshSandboxPath) { if (freshSandboxInitFailure) unavailable(); freshSandboxReady = true; return; }
      if (location !== sandboxPath) throw new Error('UNEXPECTED_SANDBOX'); if (!sandboxAvailable) unavailable();
    }
    audit() { return { installed: null }; }
    static openExistingSandbox(location) { return new FixtureInstaller(location); }
    // Retained single-root fake: static inspectModsRoot() { return { dev: 1n, ino: 4n }; }
    static inspectModsRoot(location) { return { dev: 1n, ino: location === newPath ? 5n : 4n }; }
    static inspectModsMetadataIdentity(location) {
      if (location === newPath || oldModsAvailable && location === oldPath)
        throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' });
      unavailable();
    }
    static inspectSandbox(location) {
      // The real installer additionally compares these stable synthetic identities.
      calls.probes.push({ kind: 'sandbox-readonly', location });
      // Retained prior fake inspector: if (location === freshSandboxPath) { if (!freshSandboxReady) unavailable(); return { audit: () => ({ installed: null }) }; }
      if (location === freshSandboxPath) { if (!freshSandboxReady) unavailable(); return { audit: () => ({ installed: null }), rootIdentity: { dev: 1n, ino: 4n }, metaIdentity: { dev: 1n, ino: 3n } }; }
      if (location !== sandboxPath) throw new Error('UNEXPECTED_SANDBOX');
      // Retained prior fake inspector: if (!sandboxAvailable) unavailable(); return { audit: () => ({ installed: null }) };
      if (!sandboxAvailable) unavailable(); return { audit: () => ({ installed: null }), rootIdentity: { dev: 1n, ino: 4n }, metaIdentity: { dev: 1n, ino: 3n } };
    }
    static forModsDirectory(location, options) {
      if (options.expectedPresence === 'existing') unavailable();
      calls.probes.push({ kind: 'mods', location, create: options.create });
      if (location === newPath || oldModsAvailable && location === oldPath) return null;
      if (location === unsafePath || location === oldPath || typeof location !== 'string') unavailable();
      throw new Error('UNEXPECTED_DESTINATION');
    }
  }
  const settings = {
    load() { calls.loads++; return structuredClone(saved); },
    update(transform) {
      calls.updates++; if (saveFailure) throw new Error(saveFailure);
      const current = structuredClone(saved); if (externalChange) current.modsRoot = path.resolve('__synthetic_recovery__', 'changed-elsewhere');
      const next = transform(current); if (saveFailureAfterTransform) throw new Error('LOCAL_STATE_RECOVERY_REQUIRED');
      saved = structuredClone(next); calls.events.push('settings-committed'); return { state: structuredClone(saved) };
    },
    lastLoadResult: {}
  };
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
  const serviceFactory = options => new DesktopService({ ...options, registry,
    request: async () => { calls.network++; throw new Error('UNEXPECTED_NETWORK'); },
    resolvePolicy: async () => { calls.network++; throw new Error('UNEXPECTED_POLICY_REQUEST'); },
    assetTransport: async () => { calls.downloads++; throw new Error('UNEXPECTED_DOWNLOAD'); },
    assertGameClosed: async () => { calls.gameChecks++; return false; },
    inspectPackage: async () => { throw new Error('UNEXPECTED_INSPECTION'); },
    loadBundle: async () => { throw new Error('UNEXPECTED_BUNDLE'); },
    beginRefresh: () => {}, rateStatus: () => ({}) });
  const createSandbox = () => { calls.sandboxCreates++; if (noSandbox) return freshSandboxPath; throw new Error('UNEXPECTED_SANDBOX_CREATION'); };
  return { settings, input, output, Installer: FixtureInstaller, serviceFactory, createSandbox, command, calls, original, oldPath,
    // Retained earlier fake entries: newPath, unsafePath, sandboxPath, freshSandboxPath, restoreSandbox: () => { sandboxAvailable = true; }, invalidateMods: () => { oldModsAvailable = false; },
    newPath, unsafePath, sandboxPath, freshSandboxPath, restoreSandbox: () => { sandboxAvailable = true; }, invalidateSandbox: () => { sandboxAvailable = false; }, invalidateMods: () => { oldModsAvailable = false; },
    saved: () => structuredClone(saved), close: () => { input.end(); output.end(); } };
}

test('destination recovery: unavailable saved locations preserve state and block all file operations until a valid choice', async () => {
  for (const failure of ['ENOENT', 'EACCES', 'UNKNOWN_MARKER', 'RECOVERY_REQUIRED']) {
    const f = recoveryFixture({ failure });
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const initial = await f.command({ command: 'status' });
      assert.equal(initial.ok, true); assert.equal(initial.result.target.unavailable, true);
      assert.equal(initial.result.target.path, f.oldPath); assert.match(initial.result.target.scopeText, /destination/i);
      for (const key of ['canInstall', 'canUpdate', 'canUninstall', 'canRequestInstall', 'canRequestUpdate']) assert.equal(initial.result.rows[0][key], false, key);
      assert.deepEqual(f.saved(), f.original); assert.equal(f.calls.updates, 0);
      assert.equal(f.calls.probes.length, 1, 'status must not probe an unavailable destination again');
      for (const operation of ['install', 'update', 'uninstall']) {
        const reply = await f.command({ command: operation, modules: ['example'] });
        assert.equal(reply.ok, false); assert.equal(reply.error, 'SELECT_A_MODS_DESTINATION_FIRST');
      }
      assert.equal(f.calls.gameChecks, 0); assert.equal(f.calls.network, 0); assert.equal(f.calls.downloads, 0);
      const unconfirmed = await f.command({ command: 'destination', mode: 'game-mods', path: f.newPath, confirmed: false });
      assert.equal(unconfirmed.error, 'DESTINATION_CONFIRMATION_REQUIRED'); assert.equal(f.calls.updates, 0);
      const unsafe = await f.command({ command: 'destination', mode: 'game-mods', path: f.unsafePath, confirmed: true });
      assert.equal(unsafe.ok, false); assert.equal(f.calls.updates, 0); assert.deepEqual(f.saved(), f.original);
      const chosen = await f.command({ command: 'destination', mode: 'game-mods', path: f.newPath, confirmed: true });
      assert.equal(chosen.ok, true); assert.equal(chosen.result.target.unavailable, undefined);
      assert.equal(chosen.result.target.path, f.newPath); assert.equal(f.saved().modsRoot, f.newPath);
      assert.deepEqual(f.saved().custom, f.original.custom); assert.equal(f.calls.updates, 1);
      const after = await f.command({ command: 'install', modules: ['example'] });
      assert.equal(after.error, 'CLOSE_GAME_BEFORE_CHANGING_FILES'); assert.equal(f.calls.gameChecks, 1);
      assert.equal(f.calls.network, 0); assert.equal(f.calls.downloads, 0);
    } finally { f.close(); }
  }
});

test('destination recovery: absent installation metadata is a valid destination, not an error', async () => {
  const f = recoveryFixture({ noExistingMetadata: true });
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    const reply = await f.command({ command: 'status' });
    assert.equal(reply.ok, true); assert.equal(reply.result.target.unavailable, undefined);
    assert.equal(reply.result.target.path, f.oldPath); assert.equal(f.calls.updates, 0);
  } finally { f.close(); }
});

test('destination recovery: unreadable settings still fail closed without a settings update', async () => {
  const f = recoveryFixture(); const error = Object.assign(new Error('SYNTHETIC_SETTINGS_DENIED'), { code: 'EACCES' });
  f.settings.load = () => { f.calls.loads++; throw error; };
  try {
    await assert.rejects(startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox }), error);
    assert.equal(f.calls.updates, 0); assert.equal(f.calls.probes.length, 0); assert.deepEqual(f.saved(), f.original);
  } finally { f.close(); }
});

test('destination recovery: malformed saved path stays unavailable without breaking status', async () => {
  const f = recoveryFixture({ invalidPathType: true });
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    const reply = await f.command({ command: 'status' });
    assert.equal(reply.ok, true); assert.equal(reply.result.target.unavailable, true); assert.equal(reply.result.target.path, '');
    assert.equal(f.calls.updates, 0); assert.deepEqual(f.saved(), f.original);
  } finally { f.close(); }
});

test('destination recovery: an unavailable saved sandbox cannot be accepted without validation', async () => {
  const f = recoveryFixture({ initialMode: 'sandbox' });
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    const reply = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(reply.ok, false); assert.equal(f.calls.updates, 0); assert.deepEqual(f.saved(), f.original);
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
  } finally { f.close(); }
});

test('destination recovery: a restored sandbox can be explicitly reselected', async () => {
  const f = recoveryFixture({ initialMode: 'sandbox' });
  f.restoreSandbox();
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    f.invalidateSandbox();
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true); f.restoreSandbox();
    const chosen = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(chosen.ok, true); assert.equal(chosen.result.target.unavailable, undefined);
    assert.equal(chosen.result.target.path, f.sandboxPath); assert.equal(f.calls.updates, 1);
    assert.deepEqual(f.saved().custom, f.original.custom);
  } finally { f.close(); }
});

test('destination recovery: a valid saved sandbox can replace an unavailable Mods destination', async () => {
  const f = recoveryFixture();
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true); f.restoreSandbox();
    const chosen = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(chosen.ok, true); assert.equal(chosen.result.target.unavailable, undefined); assert.equal(chosen.result.target.path, f.sandboxPath);
    assert.equal(f.saved().mode, 'sandbox'); assert.equal(f.saved().modsRoot, f.original.modsRoot); assert.deepEqual(f.saved().custom, f.original.custom);
    assert.equal(f.calls.sandboxCreates, 0);
  } finally { f.close(); }
});

test('destination recovery: an unsafe saved sandbox cannot replace an available Mods destination', async () => {
  const f = recoveryFixture({ noExistingMetadata: true });
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    const chosen = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(chosen.ok, false); assert.equal(f.calls.updates, 0); assert.deepEqual(f.saved(), f.original);
    const current = await f.command({ command: 'status' }); assert.equal(current.result.target.unavailable, undefined); assert.equal(current.result.target.path, f.oldPath);
  } finally { f.close(); }
});

test('destination recovery: automatic source refresh cannot reactivate an unavailable destination', async () => {
  const f = recoveryFixture();
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
    const refreshed = await f.command({ command: 'refresh' });
    assert.equal(refreshed.ok, true); assert.equal(refreshed.result.target.unavailable, true);
    assert.equal(f.calls.probes.length, 1); assert.equal(f.calls.updates, 0); assert.equal(f.calls.downloads, 0);
    for (const operation of ['install', 'update', 'uninstall']) {
      const reply = await f.command({ command: operation, modules: ['example'] });
      assert.equal(reply.error, 'SELECT_A_MODS_DESTINATION_FIRST');
    }
    assert.equal(f.calls.gameChecks, 0);
  } finally { f.close(); }
});

test('destination recovery: disappearance after startup is reported and blocks subsequent operations', async () => {
  const f = recoveryFixture({ noExistingMetadata: true });
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, undefined);
    f.invalidateMods(); const unavailable = await f.command({ command: 'status' });
    assert.equal(unavailable.result.target.unavailable, true); assert.deepEqual(f.saved(), f.original);
    for (const operation of ['install', 'update', 'uninstall']) {
      assert.equal((await f.command({ command: operation, modules: ['example'] })).error, 'SELECT_A_MODS_DESTINATION_FIRST');
    }
    assert.equal(f.calls.gameChecks, 0); assert.equal(f.calls.downloads, 0);
  } finally { f.close(); }
});

test('destination recovery: sandbox validation does not construct an installer before a failed save', async () => {
  const f = recoveryFixture({ initialMode: 'sandbox', saveFailure: 'LOCAL_STATE_RECOVERY_REQUIRED' });
  f.restoreSandbox();
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    f.restoreSandbox(); const reply = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(reply.error, 'LOCAL_STATE_RECOVERY_REQUIRED'); assert.deepEqual(f.saved(), f.original);
    // Retained old unavailable-state count: assert.deepEqual(f.calls.probes.map(item => item.kind), ['sandbox-readonly', 'sandbox-readonly']);
    assert.deepEqual(f.calls.probes.map(item => item.kind), ['sandbox-readonly', 'sandbox-readonly', 'sandbox-readonly']);
    // Retained old unavailable-state expectation: assert.equal(f.calls.sandboxCreates, 0); assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
    assert.equal(f.calls.sandboxCreates, 0); assert.equal((await f.command({ command: 'status' })).result.target.unavailable, undefined);
  } finally { f.close(); }
});

test('destination recovery: failed persistence or concurrent destination changes preserve the previous state', async () => {
  for (const options of [{ saveFailure: 'LOCAL_STATE_RECOVERY_REQUIRED' }, { externalChange: true }]) {
    const f = recoveryFixture(options);
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const reply = await f.command({ command: 'destination', mode: 'game-mods', path: f.newPath, confirmed: true });
      assert.equal(reply.ok, false); assert.deepEqual(f.saved(), f.original);
      assert.equal(reply.error, options.externalChange ? 'DESTINATION_SELECTION_CHANGED_EXTERNALLY' : 'LOCAL_STATE_RECOVERY_REQUIRED');
      assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
    } finally { f.close(); }
  }
});

test('destination recovery: concurrent selection changes are refused even for an initially available destination', async () => {
  const { translate } = await import('./localization.mjs');
  const key = 'The destination changed in another App. Your selection was not saved. Reconfirm the destination or restart.';
  for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
    const f = recoveryFixture({ noExistingMetadata: true, externalChange: true, language });
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const reply = await f.command({ command: 'destination', mode: 'game-mods', path: f.newPath, confirmed: true });
      assert.equal(reply.error, 'DESTINATION_SELECTION_CHANGED_EXTERNALLY'); assert.equal(reply.errorText, translate(language, key));
      assert.deepEqual(f.saved(), f.original); assert.equal(f.calls.sandboxCreates, 0);
    } finally { f.close(); }
  }
});

test('destination recovery: a confirmed new sandbox is initialized before its settings are committed', async () => {
  const f = recoveryFixture({ noSandbox: true });
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    const chosen = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(chosen.ok, true); assert.equal(chosen.result.target.unavailable, undefined); assert.equal(chosen.result.target.path, f.freshSandboxPath);
    assert.deepEqual(f.calls.events.slice(0, 2), ['sandbox-initialize', 'settings-committed']); assert.equal(f.calls.sandboxCreates, 1);
    assert.equal(f.saved().modsRoot, f.original.modsRoot); assert.deepEqual(f.saved().custom, f.original.custom);
  } finally { f.close(); }
});

test('destination recovery: failed new sandbox initialization or settings save retains original state and exposes the new path', async () => {
  for (const options of [{ freshSandboxInitFailure: true }, { saveFailureAfterTransform: true }]) {
    const f = recoveryFixture({ ...options, noSandbox: true });
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const reply = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.equal(reply.ok, false); assert.deepEqual(f.saved(), f.original); assert.equal(reply.result.retainedSandboxPath, f.freshSandboxPath);
      assert.ok(reply.errorText.includes(f.freshSandboxPath)); assert.equal(f.calls.sandboxCreates, 1);
      assert.deepEqual(f.calls.events, ['sandbox-initialize']);
    } finally { f.close(); }
  }
});

test('destination recovery: retained workspace location is explained in all supported languages', async () => {
  const { translate } = await import('./localization.mjs');
  const key = 'A new test workspace was retained at {0}. Check saved settings before retrying.';
  for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
    const f = recoveryFixture({ noSandbox: true, freshSandboxInitFailure: true, language });
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const reply = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.ok(reply.errorText.includes(translate(language, key, f.freshSandboxPath)));
      assert.deepEqual(f.saved(), f.original);
    } finally { f.close(); }
  }
});

test('destination recovery: unconfirmed or concurrently changed sandbox choices never create a new workspace', async () => {
  for (const externalChange of [false, true]) {
    const f = recoveryFixture({ noSandbox: true, externalChange });
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const reply = await f.command({ command: 'destination', mode: 'sandbox', confirmed: externalChange });
      assert.equal(reply.error, externalChange ? 'DESTINATION_SELECTION_CHANGED_EXTERNALLY' : 'DESTINATION_CONFIRMATION_REQUIRED');
      assert.deepEqual(f.saved(), f.original); assert.equal(f.calls.sandboxCreates, 0); assert.deepEqual(f.calls.events, []);
    } finally { f.close(); }
  }
});

test('destination recovery: unavailable explanation is translated for each supported interface language', async () => {
  for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
    const f = recoveryFixture({ language });
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const reply = await f.command({ command: 'status' });
      assert.equal(reply.result.target.unavailable, true); assert.ok(reply.result.target.scopeText);
      if (language !== 'en') assert.notEqual(reply.result.target.scopeText, 'The saved destination is unavailable or unsafe. Choose another destination before file operations.');
    } finally { f.close(); }
  }
});

function virtualSettingsIo({ initial = true, observations = ['EACCES'] } = {}) {
  const statePath = 'synthetic-settings.json';
  const original = Buffer.from('{"schema":1,"mode":"game-mods","sandbox":null,"modsRoot":"preserve"}');
  const files = new Map(initial ? [[statePath, Buffer.from(original)]] : []); const descriptors = new Map(); let next = 1; let renames = 0; let reads = 0;
  const missing = () => Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' });
  const denied = () => Object.assign(new Error('SYNTHETIC_DENIED'), { code: 'EACCES' });
  const stat = location => ({ isFile: () => true, isSymbolicLink: () => false, nlink: 1, dev: 1, ino: location.endsWith('.lock') ? 11 : 12,
    size: files.get(location)?.length ?? 0 });
  const io = {
    existsSync(location) { return location === statePath ? false : files.has(location); },
    lstatSync(location) {
      if (location === statePath) {
        const observation = observations[Math.min(reads++, observations.length - 1)];
        if (observation === 'EACCES') throw denied(); if (observation === 'ENOENT') throw missing();
        if (observation !== 'present') throw Object.assign(new Error('SYNTHETIC_IO'), { code: observation });
      }
      if (!files.has(location)) throw missing(); return stat(location);
    },
    openSync(location, flags) {
      if (flags !== 'wx') throw new Error('UNEXPECTED_OPEN'); if (files.has(location)) throw Object.assign(new Error('EXISTS'), { code: 'EEXIST' });
      files.set(location, Buffer.alloc(0)); const id = next++; descriptors.set(id, location); return id;
    },
    fstatSync(id) { return stat(descriptors.get(id)); },
    writeFileSync(id, bytes) { const location = descriptors.get(id); files.set(location, Buffer.concat([files.get(location), Buffer.from(bytes)])); },
    fsyncSync() {}, closeSync() {},
    renameSync(from, to) { renames++; files.set(to, files.get(from)); files.delete(from); },
    unlinkSync(location) { files.delete(location); }
  };
  return { io, statePath, original, files, renames: () => renames };
}

test('settings recovery: inaccessible existing state must not be treated as missing or replaced', () => {
  for (const operation of ['load', 'update']) {
    const f = virtualSettingsIo(); const store = new SettingsStore(f.statePath, { io: f.io }); let failure;
    try { operation === 'load' ? store.load() : store.update(current => current); } catch (error) { failure = error; }
    assert.equal(f.renames(), 0, operation + ': unreadable settings must never be replaced');
    assert.deepEqual(f.files.get(f.statePath), f.original); assert.equal(failure?.code, 'EACCES');
  }
});

test('settings recovery: new state is created only after explicit missing-file results', () => {
  const f = virtualSettingsIo({ initial: false, observations: ['ENOENT'] }); const store = new SettingsStore(f.statePath, { io: f.io });
  const state = store.load(); assert.equal(state.mode, 'unconfigured'); assert.equal(f.renames(), 1);
  assert.equal(JSON.parse(f.files.get(f.statePath).toString('utf8')).mode, 'unconfigured');
});

test('settings recovery: a later access failure aborts before replacing existing state', () => {
  const f = virtualSettingsIo({ observations: ['ENOENT', 'EACCES'] }); const store = new SettingsStore(f.statePath, { io: f.io });
  assert.throws(() => store.update(current => current), error => error.code === 'EACCES');
  assert.equal(f.renames(), 0); assert.deepEqual(f.files.get(f.statePath), f.original);
});

function virtualDestinationFs(t, { root, metadata, missing = false, denied = false, disappearing = false,
  link = false, identityChange = false, markerChange = false, journal = false, unknownMarker = false } = {}) {
  const writes = []; let metadataChecks = 0; let rootChecks = 0; let deniedChecks = 0; let journalChecks = 0; let markerOpens = 0; let directoryReads = 0; let nextFd = 100; const descriptors = new Map();
  const marker = path.join(metadata, 'marker'); const state = path.join(metadata, 'state.json');
  const markerBytes = Buffer.from(unknownMarker ? 'UNKNOWN_MARKER\n' : 'CS1_DEV_MANAGER_ISOLATED_V1\n');
  // Retained prior synthetic state: const stateBytes = Buffer.from(JSON.stringify({ schema: 1, root: path.resolve(root), modules: {} }));
  const stateBytes = Buffer.from(JSON.stringify({ schema: 1, root: path.resolve(root), modules: arguments[1]?.modules ?? {} }));
  const files = new Map([[marker, markerBytes], [state, stateBytes]]);
  const missingError = () => Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' });
  const deniedError = () => Object.assign(new Error('SYNTHETIC_DENIED'), { code: 'EACCES' });
  function stat(location, opened = false) {
    const isFile = files.has(location); let ino = isFile ? (location === marker ? 11n : 12n) : 3n;
    if (location === root) { rootChecks++; ino = identityChange && rootChecks > 2 ? 5n : 4n; }
    if (location === root && raceControl.rootIno) ino = raceControl.rootIno;
    if (location === root && raceControl.rootIno) raceControl.rootIdentityHits = (raceControl.rootIdentityHits ?? 0) + 1;
    if (location === marker && opened && markerChange) ino = 19n;
    if (location === metadata && raceControl.metadataIno) ino = raceControl.metadataIno;
    if (location === metadata && raceControl.metadataIno) raceControl.metadataIdentityHits = (raceControl.metadataIdentityHits ?? 0) + 1;
    if (location === root && raceControl.zeroDevice) return { dev: 0n, ino, nlink: 1n, size: 0n,
      isDirectory: () => true, isFile: () => false, isSymbolicLink: () => false };
    return { dev: 1n, ino, nlink: 1n, size: BigInt(files.get(location)?.length ?? 0),
      isDirectory: () => !isFile, isFile: () => isFile, isSymbolicLink: () => link && location === root };
  }
  const raceControl = { fault: null };
  // Retained prior restoration: raceControl.restoreMarker = () => { raceControl.fault = null; raceControl.markerDenied = false; files.set(marker, Buffer.from('CS1_DEV_MANAGER_ISOLATED_V1\n')); };
  raceControl.restoreMarker = () => { raceControl.fault = null; raceControl.markerDenied = false; raceControl.stateDenied = false; files.set(marker, Buffer.from('CS1_DEV_MANAGER_ISOLATED_V1\n')); };
  t.mock.method(fs, 'lstatSync', (location, options) => {
    location = String(location);
    if (location === path.join(metadata, 'journal.json')) { journalChecks++; if (!journal) throw missingError(); return stat(location); }
    // Retained original virtual fault: if (location === metadata && raceControl.fault === 'ENOENT') throw missingError();
    if (location === metadata && raceControl.fault === 'ENOENT') { raceControl.faultHits = (raceControl.faultHits ?? 0) + 1; throw missingError(); }
    // Retained original virtual fault: if (location === metadata && raceControl.fault === 'EACCES') throw deniedError();
    if (location === metadata && raceControl.fault === 'EACCES') { raceControl.faultHits = (raceControl.faultHits ?? 0) + 1; throw deniedError(); }
    if (location === metadata && raceControl.identityDenied) { raceControl.identityDeniedHits = (raceControl.identityDeniedHits ?? 0) + 1; throw deniedError(); }
    if (location === metadata && !options?.bigint && raceControl.disappearOnCheck === metadataChecks + 1) { metadataChecks++; throw missingError(); }
    if (location === metadata && missing && options?.bigint) throw missingError();
    if (location === metadata) {
      if (denied) { deniedChecks++; throw deniedError(); }
      if (!options?.bigint) { metadataChecks++; if (missing || disappearing && metadataChecks > 1) throw missingError(); }
    }
    return stat(location);
  });
  t.mock.method(fs, 'openSync', (location, flags) => {
    if (flags !== 'r') { writes.push('open-for-write'); throw new Error('UNEXPECTED_WRITE'); }
    if (location === marker && raceControl.markerDenied) { raceControl.markerDeniedHits = (raceControl.markerDeniedHits ?? 0) + 1; throw deniedError(); }
    if (location === state && raceControl.stateDenied) { raceControl.stateDeniedHits = (raceControl.stateDeniedHits ?? 0) + 1; throw deniedError(); }
    if (!files.has(location)) throw missingError(); if (location === marker) markerOpens++; const fd = nextFd++; descriptors.set(fd, location); return fd;
  });
  t.mock.method(fs, 'fstatSync', fd => stat(descriptors.get(fd), true));
  t.mock.method(fs, 'readSync', (fd, buffer, offset, length, position) => {
    const data = files.get(descriptors.get(fd)); if (position >= data.length) return 0;
    return data.copy(buffer, offset, position, Math.min(data.length, position + length));
  });
  t.mock.method(fs, 'closeSync', () => {});
  t.mock.method(fs, 'readdirSync', () => { directoryReads++; return []; });
  for (const method of ['mkdirSync', 'mkdtempSync', 'writeFileSync', 'appendFileSync', 'fsyncSync', 'renameSync', 'unlinkSync', 'rmSync', 'rmdirSync', 'linkSync', 'copyFileSync'])
    t.mock.method(fs, method, () => { writes.push(method); throw new Error('UNEXPECTED_WRITE'); });
  // Retained pre-race fixture return: return { writes, observations: () => ({ metadataChecks, rootChecks, deniedChecks, journalChecks, markerOpens, directoryReads }) };
  return { writes, raceControl, observations: () => ({ metadataChecks, rootChecks, deniedChecks, journalChecks, markerOpens, directoryReads }) };
}

test('read-only destination probe: missing sandbox metadata must never be created', t => {
  assert.equal(typeof IsolatedInstaller.inspectSandbox, 'function');
  const root = path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE');
  const f = virtualDestinationFs(t, { root, metadata: path.join(root, '.dev-manager'), missing: true });
  try {
    assert.throws(() => IsolatedInstaller.inspectSandbox(root), /Read-only destination validation requires existing manager metadata/);
    assert.equal(f.observations().metadataChecks, 1); assert.deepEqual(f.writes, []);
  }
  finally { t.mock.restoreAll(); }
});

test('read-only destination probe: create false cannot recreate metadata that disappears', t => {
  const root = path.resolve('__synthetic_mods_probe__'); const metadata = metadataForModsDirectory(root);
  const f = virtualDestinationFs(t, { root, metadata, disappearing: true });
  try {
    assert.throws(() => IsolatedInstaller.forModsDirectory(root, { confirmed: true, create: false }), /Read-only destination validation requires existing manager metadata/);
    assert.equal(f.observations().metadataChecks, 2); assert.equal(f.observations().markerOpens, 1); assert.deepEqual(f.writes, []);
  }
  finally { t.mock.restoreAll(); }
});

test('read-only destination probe: an existing valid sandbox can be inspected without writes', t => {
  assert.equal(typeof IsolatedInstaller.inspectSandbox, 'function');
  const root = path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE');
  const f = virtualDestinationFs(t, { root, metadata: path.join(root, '.dev-manager') });
  try { assert.ok(IsolatedInstaller.inspectSandbox(root)); assert.deepEqual(f.writes, []); }
  finally { t.mock.restoreAll(); }
});

test('read-only destination probe: returned inspectors cannot perform filesystem transactions', async t => {
  const bytes = Buffer.from('synthetic package'); const digest = createHash('sha256').update(bytes).digest('hex');
  const pkg = { moduleId: 'example', buildId: 'fixture', files: [{ path: 'Example.dll', bytes, sha256: digest }] };
  const expected = { moduleId: 'example', buildId: 'older', files: [{ path: 'Example.dll', sha256: digest }] };
  for (const kind of ['sandbox', 'mods']) {
    await t.test(kind, child => {
      const root = kind === 'sandbox' ? path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE') : path.resolve('__synthetic_mods_probe__');
      const metadata = kind === 'sandbox' ? path.join(root, '.dev-manager') : metadataForModsDirectory(root);
      const f = virtualDestinationFs(child, { root, metadata });
      try {
        const inspector = kind === 'sandbox' ? IsolatedInstaller.inspectSandbox(root) : IsolatedInstaller.forModsDirectory(root, { confirmed: true, create: false });
        const errors = [];
        for (const run of [() => inspector.install(pkg, null), () => inspector.update(pkg, expected),
          () => inspector.uninstall('example', expected), () => inspector.transact('install', 'example', pkg, null)]) {
          try { run(); } catch (error) { errors.push(error.message); }
        }
        assert.deepEqual(f.writes, []); assert.deepEqual(errors, Array(4).fill('READ_ONLY_INSTALLER'));
        // Retained exploratory prototype-boundary assertions; not part of this recovery scope.
        if (false) {
          const derived = Object.create(inspector);
          assert.throws(() => derived.install(pkg, null), /READ_ONLY_INSTALLER/);
          assert.throws(() => IsolatedInstaller.prototype.transact.call(inspector, 'install', 'example', pkg, null), /READ_ONLY_INSTALLER/);
          assert.equal(Object.hasOwn(inspector, 'root'), false); assert.equal(Object.isFrozen(inspector), true); assert.deepEqual(f.writes, []);
        }
      } finally { child.mock.restoreAll(); }
    });
  }
});

test('read-only destination probe: denied metadata, links, changed identity, changed files and journals are refused without writes', async t => {
  const expected = { denied: /SYNTHETIC_DENIED/, link: /Unsafe link or filesystem object/,
    identityChange: /Root identity changed/, markerChange: /File changed while opening/,
    journal: /Incomplete transaction requires manual recovery/, unknownMarker: /Unknown manager directory/ };
  for (const scenario of Object.keys(expected)) {
    await t.test(scenario, child => {
      assert.equal(typeof IsolatedInstaller.inspectSandbox, 'function');
      const root = path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE');
      const f = virtualDestinationFs(child, { root, metadata: path.join(root, '.dev-manager'), [scenario]: true });
      try {
        assert.throws(() => IsolatedInstaller.inspectSandbox(root), expected[scenario]); const observed = f.observations();
        if (scenario === 'denied') assert.equal(observed.deniedChecks, 1);
        if (scenario === 'link') assert.ok(observed.rootChecks >= 1);
        if (scenario === 'identityChange') assert.ok(observed.rootChecks >= 4);
        if (scenario === 'markerChange' || scenario === 'unknownMarker') assert.equal(observed.markerOpens, 1);
        if (scenario === 'journal') assert.equal(observed.journalChecks, 1);
        assert.deepEqual(f.writes, []);
      }
      finally { child.mock.restoreAll(); }
    });
  }
});

test('read-only destination probe: a valid new Mods folder may have no manager metadata', t => {
  const root = path.resolve('__synthetic_mods_probe__'); const metadata = metadataForModsDirectory(root);
  const f = virtualDestinationFs(t, { root, metadata, missing: true });
  try {
    assert.equal(IsolatedInstaller.forModsDirectory(root, { confirmed: true, create: false }), null);
    assert.equal(f.observations().metadataChecks, 1); assert.equal(f.observations().directoryReads, 1); assert.deepEqual(f.writes, []);
  }
  finally { t.mock.restoreAll(); }
});


test('destination recovery: existing sandbox metadata disappearance after asynchronous preparation must not reinitialize', async t => {
  const root = path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE');
  const v = virtualDestinationFs(t, { root, metadata: path.join(root, '.dev-manager') });
  const f = recoveryFixture({ initialMode: 'sandbox' });
  f.settings.load = () => ({ mode: 'sandbox', sandbox: root, modsRoot: null, preferences: { language: 'en', theme: 'system' } });
  const events = []; const bytes = Buffer.from('synthetic package');
  const sha256 = createHash('sha256').update(bytes).digest('hex');
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
  const serviceFactory = options => {
    const service = new DesktopService({ ...options, registry,
      assetTransport: async () => { throw new Error('UNEXPECTED_ASSET_TRANSPORT'); },
      resolvePolicy: async () => { throw new Error('UNEXPECTED_POLICY_REQUEST'); },
      loadBundle: async () => { throw new Error('UNEXPECTED_BUNDLE_REQUEST'); },
      request: async () => { throw new Error('UNEXPECTED_NETWORK'); },
      assertGameClosed: async () => true,
      inspectPackage: async () => { events.push('inspected'); return true; } });
    service.refresh = async () => {
      await Promise.resolve(); events.push('metadata-disappeared'); v.raceControl.fault = 'ENOENT';
      service.provider = 'modpack-bundle';
      service.sources.set('example', { developmentConfirmed: true, checkedAtMs: Date.now(),
        latestPublished: { status: 'candidate', buildId: 'synthetic-build', sourceSha: 'a'.repeat(40), releaseId: 1,
          publishedAt: '2026-01-01T00:00:00Z', runId: 1, attempt: 1 },
        bundlePackage: { buildId: 'synthetic-build', files: [{ path: 'Example.dll', bytes, sha256 }] } });
    };
    return service;
  };
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, serviceFactory });
    const reply = await f.command({ command: 'install', modules: ['example'] });
    assert.deepEqual(events, ['metadata-disappeared', 'inspected']);
    assert.equal(reply.ok, false); assert.equal(reply.result.target.unavailable, true);
    assert.equal(reply.error, 'METADATA_PRESENCE_CHANGED'); assert.ok(v.raceControl.faultHits >= 1);
    assert.deepEqual(v.writes, [], 'existing sandbox metadata must not be recreated');
    assert.equal(f.calls.updates, 0);
  } finally { f.close(); t.mock.restoreAll(); }
});


test('destination recovery: Mods metadata disappearing inside the writable factory is not recreated', t => {
  const root = path.resolve('__synthetic_mods_race__'); const metadata = metadataForModsDirectory(root);
  const v = virtualDestinationFs(t, { root, metadata }); v.raceControl.disappearOnCheck = 2;
  try {
    assert.throws(() => IsolatedInstaller.forModsDirectory(root, { confirmed: true, create: true }), /METADATA_PRESENCE_CHANGED/);
    assert.equal(v.observations().markerOpens, 1); assert.equal(v.observations().metadataChecks, 2); assert.deepEqual(v.writes, []);
  } finally { t.mock.restoreAll(); }
});

test('destination recovery: absent and existing Mods metadata are distinct at initialization', async t => {
  await t.test('verified absent metadata can reach first initialization', child => {
    const root = path.resolve('__synthetic_mods_new__'); const metadata = metadataForModsDirectory(root);
    const v = virtualDestinationFs(child, { root, metadata, missing: true });
    try {
      assert.throws(() => IsolatedInstaller.forModsDirectory(root, { confirmed: true, create: true, expectedPresence: 'absent' }), /UNEXPECTED_WRITE/);
      assert.deepEqual(v.writes, ['mkdirSync']);
    } finally { child.mock.restoreAll(); }
  });
  await t.test('an unexpected existing metadata directory is preserved', child => {
    const root = path.resolve('__synthetic_mods_unexpected__'); const metadata = metadataForModsDirectory(root);
    const v = virtualDestinationFs(child, { root, metadata });
    try {
      assert.throws(() => IsolatedInstaller.forModsDirectory(root, { confirmed: true, create: true, expectedPresence: 'absent' }), /METADATA_PRESENCE_CHANGED/);
      assert.deepEqual(v.writes, []);
    } finally { child.mock.restoreAll(); }
  });
  await t.test('existing validated metadata opens without initialization writes', child => {
    const root = path.resolve('__synthetic_mods_existing__'); const metadata = metadataForModsDirectory(root);
    const v = virtualDestinationFs(child, { root, metadata });
    try {
      const expectedIdentity = { root: { dev: 1n, ino: 4n }, meta: { dev: 1n, ino: 3n } };
      assert.ok(IsolatedInstaller.forModsDirectory(root, { confirmed: true, create: true, expectedPresence: 'existing', expectedIdentity }));
      assert.deepEqual(v.writes, []);
    } finally { child.mock.restoreAll(); }
  });
});

test('destination recovery: existing metadata remains protected through asynchronous operations', async t => {
  for (const mode of ['sandbox', 'game-mods']) for (const operation of ['install', 'update', 'uninstall']) for (const fault of ['ENOENT', 'EACCES', 'replacement']) {
    await t.test(mode + '/' + operation + '/' + fault, async child => {
      const root = mode === 'sandbox' ? path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE') : path.resolve('__synthetic_mods_async__');
      const metadata = mode === 'sandbox' ? path.join(root, '.dev-manager') : metadataForModsDirectory(root);
      const unrelated = { moduleId: 'other', buildId: 'old', files: [{ path: 'Other.dll', sha256: 'a'.repeat(64) }] };
      const v = virtualDestinationFs(child, { root, metadata, modules: { other: unrelated } });
      const f = recoveryFixture({ initialMode: mode });
      f.settings.load = () => ({ mode, sandbox: mode === 'sandbox' ? root : null,
        modsRoot: mode === 'game-mods' ? root : null, preferences: { language: 'en', theme: 'system' } });
      const record = operation === 'install' ? null : { moduleId: 'example', buildId: 'same-build', files: [{ path: 'Example.dll', sha256: 'b'.repeat(64) }] };
      let audits = 0; let checks = 0; const events = [];
      child.mock.method(IsolatedInstaller.prototype, 'audit', () => { audits++; return { moduleId: 'example', installed: record }; });
      const applyFault = () => { events.push('fault'); if (fault === 'replacement') v.raceControl.metadataIno = 55n; else v.raceControl.fault = fault; };
      const bytes = Buffer.from('synthetic package'); const sha256 = createHash('sha256').update(bytes).digest('hex');
      const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
      const serviceFactory = options => {
        const service = new DesktopService({ ...options, registry,
      assetTransport: async () => { throw new Error('UNEXPECTED_ASSET_TRANSPORT'); },
      resolvePolicy: async () => { throw new Error('UNEXPECTED_POLICY_REQUEST'); },
      loadBundle: async () => { throw new Error('UNEXPECTED_BUNDLE_REQUEST'); },
          request: async () => { throw new Error('UNEXPECTED_NETWORK'); },
          assertGameClosed: async () => { checks++; if (operation === 'uninstall' && checks === 2) applyFault(); return true; },
          inspectPackage: async () => { events.push('inspected'); return true; } });
        service.refresh = async () => {
          await Promise.resolve(); applyFault(); service.provider = 'modpack-bundle';
          service.sources.set('example', { developmentConfirmed: true, checkedAtMs: Date.now(),
            latestPublished: { status: 'candidate', buildId: operation === 'update' ? 'same-build' : 'new-build', sourceSha: 'a'.repeat(40),
              releaseId: 1, publishedAt: '2026-01-01T00:00:00Z', runId: 1, attempt: 1 },
            bundlePackage: { buildId: 'new-build', files: [{ path: 'Example.dll', bytes, sha256 }] } });
        };
        return service;
      };
      try {
        await startBackend({ settings: f.settings, input: f.input, output: f.output, serviceFactory });
        const reply = await f.command({ command: operation, modules: ['example'] });
        assert.equal(reply.ok, false); assert.equal(reply.result.target.unavailable, true);
        assert.equal(audits, 1); assert.equal(checks, 2); assert.ok(events.includes('fault'));
        const expectedError = fault === 'ENOENT' ? 'METADATA_PRESENCE_CHANGED' : fault === 'EACCES' ? 'SYNTHETIC_DENIED' : 'DESTINATION_IDENTITY_CHANGED';
        assert.equal(reply.error, expectedError);
        assert.ok(fault === 'replacement' ? v.raceControl.metadataIdentityHits >= 1 : v.raceControl.faultHits >= 1);
        if (operation === 'install') assert.ok(events.includes('inspected'));
        assert.deepEqual(v.writes, []); assert.equal(f.calls.updates, 0);
      } finally { f.close(); child.mock.restoreAll(); }
    });
  }
});


test('destination recovery: an unverifiable device identity blocks operations without writes', async t => {
  const root = path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE');
  const v = virtualDestinationFs(t, { root, metadata: path.join(root, '.dev-manager') }); v.raceControl.zeroDevice = true;
  const f = recoveryFixture({ initialMode: 'sandbox' });
  f.settings.load = () => ({ mode: 'sandbox', sandbox: root, modsRoot: null, preferences: { language: 'en', theme: 'system' } });
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, serviceFactory: f.serviceFactory });
    const status = await f.command({ command: 'status' });
    assert.equal(status.result.target.unavailable, true);
    const install = await f.command({ command: 'install', modules: ['example'] });
    assert.equal(install.error, 'SELECT_A_MODS_DESTINATION_FIRST'); assert.deepEqual(v.writes, []); assert.equal(f.calls.updates, 0);
  } finally { f.close(); t.mock.restoreAll(); }
});


test('destination recovery: a metadata-free Mods root cannot be replaced during asynchronous preparation', async t => {
  const root = path.resolve('__synthetic_mods_empty_root_race__'); const metadata = metadataForModsDirectory(root);
  const v = virtualDestinationFs(t, { root, metadata, missing: true });
  const f = recoveryFixture({ initialMode: 'game-mods' });
  f.settings.load = () => ({ mode: 'game-mods', sandbox: null, modsRoot: root, preferences: { language: 'en', theme: 'system' } });
  const events = []; const bytes = Buffer.from('synthetic package'); const sha256 = createHash('sha256').update(bytes).digest('hex');
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
  const serviceFactory = options => {
    const service = new DesktopService({ ...options, registry,
      assetTransport: async () => { throw new Error('UNEXPECTED_ASSET_TRANSPORT'); },
      resolvePolicy: async () => { throw new Error('UNEXPECTED_POLICY_REQUEST'); },
      loadBundle: async () => { throw new Error('UNEXPECTED_BUNDLE_REQUEST'); }, request: async () => { throw new Error('UNEXPECTED_NETWORK'); },
      assertGameClosed: async () => true, inspectPackage: async () => { events.push('inspected'); return true; } });
    service.refresh = async () => {
      await Promise.resolve(); events.push('root-replaced'); v.raceControl.rootIno = 77n; service.provider = 'modpack-bundle';
      service.sources.set('example', { developmentConfirmed: true, checkedAtMs: Date.now(),
        latestPublished: { status: 'candidate', buildId: 'synthetic-build', sourceSha: 'a'.repeat(40), releaseId: 1,
          publishedAt: '2026-01-01T00:00:00Z', runId: 1, attempt: 1 },
        bundlePackage: { buildId: 'synthetic-build', files: [{ path: 'Example.dll', bytes, sha256 }] } });
    };
    return service;
  };
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, serviceFactory });
    const reply = await f.command({ command: 'install', modules: ['example'] });
    assert.deepEqual(events, ['root-replaced', 'inspected']); assert.equal(reply.ok, false);
    assert.equal(reply.result.target.unavailable, true); assert.deepEqual(v.writes, []); assert.equal(f.calls.updates, 0);
    assert.equal(reply.error, 'DESTINATION_IDENTITY_CHANGED'); assert.ok(v.raceControl.rootIdentityHits >= 1);
  } finally { f.close(); t.mock.restoreAll(); }
});


test('destination recovery: reselecting a previously observed Mods root cannot forget missing metadata', async t => {
  const root = path.resolve('__synthetic_reselect_mods__'); const metadata = metadataForModsDirectory(root);
  const v = virtualDestinationFs(t, { root, metadata }); const f = recoveryFixture({ initialMode: 'game-mods' });
  let saved = { mode: 'game-mods', sandbox: null, modsRoot: root, preferences: { language: 'en', theme: 'system' } };
  const before = structuredClone(saved); let updates = 0;
  const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
  const forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden,
    resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, serviceFactory });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, undefined);
    assert.ok(v.observations().markerOpens >= 1, 'existing metadata was inspected');
    v.raceControl.fault = 'ENOENT';
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
    const hitsBeforeReselect = v.raceControl.faultHits;
    const reply = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
    assert.equal(reply.ok, false); assert.equal(reply.error, 'METADATA_PRESENCE_CHANGED');
    assert.ok(v.raceControl.faultHits > hitsBeforeReselect); assert.equal(reply.result.target.unavailable, true);
    assert.equal(updates, 0); assert.deepEqual(saved, before); assert.deepEqual(v.writes, []);
  } finally { f.close(); t.mock.restoreAll(); }
});


test('destination recovery: reselecting an observed empty Mods root cannot accept a replaced root', async t => {
  const root = path.resolve('__synthetic_reselect_empty_mods__'); const metadata = metadataForModsDirectory(root);
  const v = virtualDestinationFs(t, { root, metadata, missing: true }); const f = recoveryFixture({ initialMode: 'game-mods' });
  let saved = { mode: 'game-mods', sandbox: null, modsRoot: root, preferences: { language: 'en', theme: 'system' } };
  const before = structuredClone(saved); let updates = 0;
  const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
  const forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden,
    resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, serviceFactory });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, undefined);
    v.raceControl.rootIno = 77n;
    const reply = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
    assert.equal(reply.ok, false); assert.equal(reply.error, 'DESTINATION_IDENTITY_CHANGED');
    assert.ok(v.raceControl.rootIdentityHits >= 1); assert.equal(updates, 0); assert.deepEqual(saved, before); assert.deepEqual(v.writes, []);
  } finally { f.close(); t.mock.restoreAll(); }
});


test('destination recovery: session history survives destination switches and aliases', async t => {
  const cases = ['sandbox-then-missing', 'other-mods-then-missing', 'alias-then-missing', 'denied',
    'restored', 'metadata-replaced', 'new-root', 'empty-root-replaced', 'synthetic-initialization'];
  for (const scenario of cases) await t.test(scenario, async child => {
    const rootA = path.resolve('__synthetic_history_A__'); const aliasA = path.resolve('__synthetic_history_alias_A__');
    const rootB = path.resolve('__synthetic_history_B__'); const sandbox = path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE');
    const f = recoveryFixture({ initialMode: 'game-mods' }); const calls = []; let aStatus = 'present'; let aMeta = 3n;
    const identityReads = [];
    let bPresent = false; let bRoot = 40n; let syntheticInitializations = 0;
    let saved = { mode: 'game-mods', sandbox, modsRoot: rootA, preferences: { language: 'en', theme: 'system' } }; let updates = 0;
    const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
    const snapshot = (rootIno, metaIno) => ({ rootIdentity: { dev: 1n, ino: rootIno }, metaIdentity: { dev: 1n, ino: metaIno },
      audit: () => ({ installed: null }) });
    class FixtureInstaller {
      static inspectSandbox(location) { assert.equal(location, sandbox); return snapshot(6n, 7n); }
      static inspectModsMetadataIdentity(location) {
        const isA = location === rootA || location === aliasA;
        if (isA && aStatus === 'denied') throw Object.assign(new Error('SYNTHETIC_DENIED'), { code: 'EACCES' });
        if (isA ? aStatus !== 'present' : !bPresent) throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' });
        return { rootIdentity: FixtureInstaller.inspectModsRoot(location), metaIdentity: { dev: 1n, ino: isA ? aMeta : 41n } };
      }
      static inspectModsRoot(location) {
        if (location === rootA || location === aliasA) identityReads.push({ kind: 'root', location, ino: 4n });
        if (location === rootB) identityReads.push({ kind: 'root', location, ino: bRoot });
        if (location === rootA || location === aliasA) return { dev: 1n, ino: 4n };
        if (location === rootB) return { dev: 1n, ino: bRoot };
        throw new Error('UNEXPECTED_ROOT');
      }
      static forModsDirectory(location, options) {
        calls.push({ location, ...options }); const isA = location === rootA || location === aliasA;
        if (!isA && location !== rootB) throw new Error('UNEXPECTED_DESTINATION');
        if (isA && aStatus === 'denied') throw new Error('SYNTHETIC_DENIED');
        let present = isA ? aStatus === 'present' : bPresent;
        const rootIdentity = FixtureInstaller.inspectModsRoot(location);
        if (options.expectedRoot && (rootIdentity.dev !== options.expectedRoot.dev || rootIdentity.ino !== options.expectedRoot.ino))
          throw new Error('DESTINATION_IDENTITY_CHANGED');
        if (options.expectedPresence === 'existing' && !present || options.expectedPresence === 'absent' && present)
          throw new Error('METADATA_PRESENCE_CHANGED');
        if (!present && options.create) { if (isA) aStatus = 'present'; else bPresent = true; present = true; syntheticInitializations++; }
        if (!present) return null;
        const item = snapshot(rootIdentity.ino, isA ? aMeta : 41n);
        identityReads.push({ kind: 'metadata', location, ino: item.metaIdentity.ino });
        if (options.expectedIdentity && (item.rootIdentity.ino !== options.expectedIdentity.root.ino || item.metaIdentity.ino !== options.expectedIdentity.meta.ino))
          throw new Error('DESTINATION_IDENTITY_CHANGED');
        return item;
      }
    }
    const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
    const forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
    const serviceFactory = options => {
      const service = new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden,
        resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
      if (scenario === 'synthetic-initialization') service.execute = async () => {
        assert.equal(options.getInstaller(false), null); assert.ok(options.getInstaller(true)); return { rows: [] };
      };
      return service;
    };
    try {
      await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
      assert.equal((await f.command({ command: 'status' })).result.target.unavailable, undefined);
      if (scenario === 'sandbox-then-missing') {
        assert.equal((await f.command({ command: 'destination', mode: 'sandbox', confirmed: true })).ok, true);
        aStatus = 'missing';
      } else if (['other-mods-then-missing', 'new-root', 'empty-root-replaced', 'synthetic-initialization'].includes(scenario)) {
        assert.equal((await f.command({ command: 'destination', mode: 'game-mods', path: rootB, confirmed: true })).ok, true);
        if (scenario === 'other-mods-then-missing') aStatus = 'missing';
        if (scenario === 'empty-root-replaced') bRoot = 88n;
      } else if (scenario === 'denied') aStatus = 'denied';
      else if (scenario === 'restored') { aStatus = 'missing'; assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true); aStatus = 'present'; }
      else if (scenario === 'metadata-replaced') aMeta = 33n;
      else aStatus = 'missing';
      if (scenario === 'new-root') { assert.equal(saved.modsRoot, rootB); assert.equal(updates, 1); return; }
      if (scenario === 'synthetic-initialization') {
        assert.equal((await f.command({ command: 'install', modules: ['example'] })).ok, true);
        assert.equal(syntheticInitializations, 1);
      }
      const before = structuredClone(saved); const beforeUpdates = updates;
      const selected = scenario === 'alias-then-missing' ? aliasA : ['empty-root-replaced', 'synthetic-initialization'].includes(scenario) ? rootB : rootA;
      const callsBeforeReselect = calls.length; const identitiesBeforeReselect = identityReads.length;
      const reply = await f.command({ command: 'destination', mode: 'game-mods', path: selected, confirmed: true });
      if (scenario === 'restored' || scenario === 'synthetic-initialization') {
        assert.equal(reply.ok, true); assert.equal(updates, beforeUpdates + 1);
        // Retained broad earlier assertion: assert.ok(calls.some(call => call.location === selected && call.expectedPresence === 'existing'));
        assert.ok(calls.slice(callsBeforeReselect).some(call => call.location === selected && call.expectedPresence === 'existing'));
      } else {
        const expected = scenario === 'denied' ? 'SYNTHETIC_DENIED' : ['metadata-replaced', 'empty-root-replaced'].includes(scenario) ? 'DESTINATION_IDENTITY_CHANGED' : 'METADATA_PRESENCE_CHANGED';
        assert.equal(reply.ok, false); assert.equal(reply.error, expected); assert.equal(updates, beforeUpdates); assert.deepEqual(saved, before);
        // Retained broad earlier assertion: if (scenario !== 'empty-root-replaced') assert.ok(calls.some(call => call.location === selected && call.expectedPresence === 'existing'));
        if (scenario !== 'empty-root-replaced') assert.ok(calls.slice(callsBeforeReselect).some(call => call.location === selected && call.expectedPresence === 'existing'));
        if (scenario === 'empty-root-replaced') assert.ok(identityReads.slice(identitiesBeforeReselect).some(read => read.kind === 'root' && read.location === rootB && read.ino === 88n));
        if (scenario === 'metadata-replaced') assert.ok(identityReads.slice(identitiesBeforeReselect).some(read => read.kind === 'metadata' && read.location === rootA && read.ino === 33n));
      }
    } finally { f.close(); child.mock.restoreAll(); }
  });
});


test('destination recovery: an unreadable empty Mods root keeps the command reader available', async t => {
  for (const fault of ['EACCES', 'ENOENT', 'zero-identity']) await t.test(fault, async child => {
    const f = recoveryFixture({ noExistingMetadata: true }); const original = f.Installer.inspectModsRoot; let hits = 0;
    f.Installer.inspectModsRoot = location => {
      if (location !== f.oldPath) return original(location);
      hits++;
      if (fault === 'zero-identity') return { dev: 0n, ino: 4n };
      throw Object.assign(new Error('SYNTHETIC_ROOT_' + fault), { code: fault });
    };
    try {
      await assert.doesNotReject(startBackend({ settings: f.settings, input: f.input, output: f.output,
        Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox }));
      // Retained earlier single-probe assertion: assert.equal(hits, 1);
      assert.ok(hits >= 2); const status = await f.command({ command: 'status' });
      assert.equal(status.result.target.unavailable, true); assert.deepEqual(f.saved(), f.original);
      for (const operation of ['install', 'update', 'uninstall'])
        assert.equal((await f.command({ command: operation, modules: ['example'] })).error, 'SELECT_A_MODS_DESTINATION_FIRST');
      const changed = await f.command({ command: 'destination', mode: 'game-mods', path: f.newPath, confirmed: true });
      assert.equal(changed.ok, true); assert.equal(changed.result.target.path, f.newPath);
      assert.equal(f.calls.updates, 1); assert.equal(f.calls.downloads, 0); assert.equal(f.calls.gameChecks, 0);
    } finally { f.close(); child.mock.restoreAll(); }
  });
});


test('destination recovery: a valid replacement sandbox cannot be accepted by reselecting', async t => {
  for (const flow of ['direct', 'through-mods']) for (const replacement of ['root', 'metadata']) await t.test(flow + '/' + replacement, async child => {
    const sandbox = path.join(os.tmpdir(), 'cs1-dev-manager-test-FAKE'); const mods = path.resolve('__synthetic_sandbox_switch_mods__');
    const v = virtualDestinationFs(child, { root: sandbox, metadata: path.join(sandbox, '.dev-manager') });
    const f = recoveryFixture({ initialMode: 'sandbox' }); let saved = { mode: 'sandbox', sandbox, modsRoot: mods, preferences: { language: 'en', theme: 'system' } }; let updates = 0;
    const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
    class FixtureInstaller extends IsolatedInstaller {
      static forModsDirectory(location, options) { assert.equal(location, mods); assert.equal(options.create, false); return null; }
      static inspectModsRoot(location) { assert.equal(location, mods); return { dev: 2n, ino: 60n }; }
      static inspectModsMetadataIdentity(location) { assert.equal(location, mods); throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' }); }
    }
    const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
    const forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
    const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden,
      resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
    try {
      await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
      assert.equal((await f.command({ command: 'status' })).result.target.unavailable, undefined);
      if (flow === 'through-mods') assert.equal((await f.command({ command: 'destination', mode: 'game-mods', path: mods, confirmed: true })).ok, true);
      if (replacement === 'root') v.raceControl.rootIno = 77n; else v.raceControl.metadataIno = 55n;
      if (flow === 'direct') assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
      const before = structuredClone(saved), beforeUpdates = updates, beforeMarkers = v.observations().markerOpens;
      const beforeIdentityHits = replacement === 'root' ? v.raceControl.rootIdentityHits ?? 0 : v.raceControl.metadataIdentityHits ?? 0;
      const reply = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.equal(reply.ok, false); assert.equal(reply.error, 'DESTINATION_IDENTITY_CHANGED');
      assert.ok(v.observations().markerOpens > beforeMarkers, 'valid replacement marker was inspected');
      assert.ok(replacement === 'root' ? v.raceControl.rootIdentityHits >= 1 : v.raceControl.metadataIdentityHits >= 1);
      assert.ok((replacement === 'root' ? v.raceControl.rootIdentityHits ?? 0 : v.raceControl.metadataIdentityHits ?? 0) > beforeIdentityHits);
      assert.equal(updates, beforeUpdates); assert.deepEqual(saved, before); assert.deepEqual(v.writes, []);
      if (flow === 'direct') assert.equal(reply.result.target.unavailable, true);
      v.raceControl.rootIno = null; v.raceControl.metadataIno = null;
      const restored = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.equal(restored.ok, true); assert.equal(restored.result.target.unavailable, undefined); assert.equal(updates, beforeUpdates + 1);
      assert.deepEqual(v.writes, []);
    } finally { f.close(); child.mock.restoreAll(); }
  });
});


test('destination recovery: unverifiable existing Mods metadata cannot be reselected as absent', async t => {
  // Retained initial variants: for (const fault of ['invalid-marker', 'unreadable-marker']) await t.test(fault, async child => {
  for (const fault of ['invalid-marker', 'unreadable-marker', 'unreadable-state']) await t.test(fault, async child => {
    const root = path.resolve('__synthetic_unverified_mods__'); const metadata = metadataForModsDirectory(root);
    const v = virtualDestinationFs(child, { root, metadata, unknownMarker: fault === 'invalid-marker' });
    if (fault === 'unreadable-marker') v.raceControl.markerDenied = true;
    if (fault === 'unreadable-state') v.raceControl.stateDenied = true;
    const f = recoveryFixture({ initialMode: 'game-mods' }); let saved = { mode: 'game-mods', sandbox: null, modsRoot: root, preferences: { language: 'en', theme: 'system' } }; let updates = 0;
    const before = structuredClone(saved); const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
    const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }];
    const forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
    const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden,
      resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
    try {
      await startBackend({ settings, input: f.input, output: f.output, serviceFactory });
      assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
      // Retained earlier marker-only assertion: assert.ok(fault === 'invalid-marker' ? v.observations().markerOpens >= 1 : v.raceControl.markerDeniedHits >= 1);
      assert.ok(fault === 'invalid-marker' ? v.observations().markerOpens >= 1 : fault === 'unreadable-marker' ? v.raceControl.markerDeniedHits >= 1 : v.raceControl.stateDeniedHits >= 1);
      v.raceControl.fault = 'ENOENT'; const hits = v.raceControl.faultHits ?? 0;
      const refused = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
      assert.equal(refused.ok, false); assert.equal(refused.error, 'METADATA_PRESENCE_CHANGED');
      assert.ok(v.raceControl.faultHits > hits); assert.equal(refused.result.target.unavailable, true);
      assert.equal(updates, 0); assert.deepEqual(saved, before); assert.deepEqual(v.writes, []);
      v.raceControl.restoreMarker();
      const restored = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
      assert.equal(restored.ok, true); assert.equal(restored.result.target.unavailable, undefined); assert.equal(updates, 1);
      assert.deepEqual(v.writes, []);
    } finally { f.close(); child.mock.restoreAll(); }
  });
});


test('destination recovery: unknown metadata with an unobservable identity cannot be silently accepted', async t => {
  const root = path.resolve('__synthetic_unknown_identity__'), other = path.resolve('__synthetic_unknown_other__'), metadata = metadataForModsDirectory(root);
  const v = virtualDestinationFs(t, { root, metadata }); v.raceControl.identityDenied = true;
  const f = recoveryFixture({ initialMode: 'game-mods' }); let saved = { mode: 'game-mods', sandbox: null, modsRoot: root, preferences: { language: 'en', theme: 'system' } }; let updates = 0;
  const before = structuredClone(saved); const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  class FixtureInstaller extends IsolatedInstaller {
    static forModsDirectory(location, options) { if (location === other) return null; return IsolatedInstaller.forModsDirectory(location, options); }
    static inspectModsRoot(location) { if (location === other) return { dev: 2n, ino: 60n }; return IsolatedInstaller.inspectModsRoot(location); }
    static inspectModsMetadataIdentity(location) { if (location === other) throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' }); return IsolatedInstaller.inspectModsMetadataIdentity(location); }
  }
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }]; const forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true); assert.ok(v.raceControl.identityDeniedHits >= 2);
    v.raceControl.identityDenied = false;
    const refused = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
    assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_UNVERIFIABLE'); assert.equal(updates, 0); assert.deepEqual(saved, before);
    const changed = await f.command({ command: 'destination', mode: 'game-mods', path: other, confirmed: true });
    assert.equal(changed.ok, true); assert.equal(changed.result.target.path, other); assert.equal(updates, 1); assert.deepEqual(v.writes, []);
  } finally { f.close(); t.mock.restoreAll(); }
});

test('destination recovery: a failed selection remembers unverifiable metadata without blocking the current target', async t => {
  const root = path.resolve('__synthetic_failed_selection__'), other = path.resolve('__synthetic_current_target__'), metadata = metadataForModsDirectory(root);
  const v = virtualDestinationFs(t, { root, metadata, unknownMarker: true });
  const f = recoveryFixture({ initialMode: 'game-mods' }); let saved = { mode: 'game-mods', sandbox: null, modsRoot: other, preferences: { language: 'en', theme: 'system' } }; let updates = 0;
  const before = structuredClone(saved); const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  class FixtureInstaller extends IsolatedInstaller {
    static forModsDirectory(location, options) { if (location === other) return null; return IsolatedInstaller.forModsDirectory(location, options); }
    static inspectModsRoot(location) { if (location === other) return { dev: 2n, ino: 60n }; return IsolatedInstaller.inspectModsRoot(location); }
    static inspectModsMetadataIdentity(location) { if (location === other) throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' }); return IsolatedInstaller.inspectModsMetadataIdentity(location); }
  }
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }]; const forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
    const first = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
    assert.equal(first.ok, false); assert.equal(first.error, 'OPERATION_REFUSED_INSPECT_LOCAL_FILES'); assert.ok(v.observations().markerOpens >= 1);
    assert.equal(first.result.target.path, other); assert.equal(first.result.target.unavailable, undefined); assert.equal(updates, 0);
    v.raceControl.fault = 'ENOENT'; const hits = v.raceControl.faultHits ?? 0;
    const second = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
    assert.equal(second.ok, false); assert.equal(second.error, 'METADATA_PRESENCE_CHANGED'); assert.ok(v.raceControl.faultHits > hits);
    assert.equal(second.result.target.path, other); assert.equal(second.result.target.unavailable, undefined); assert.deepEqual(saved, before); assert.equal(updates, 0);
    v.raceControl.restoreMarker(); const restored = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
    assert.equal(restored.ok, true); assert.equal(restored.result.target.path, root); assert.equal(updates, 1); assert.deepEqual(v.writes, []);
  } finally { f.close(); t.mock.restoreAll(); }
});

test('destination recovery: a synthetically created sandbox keeps its observed identity', async () => {
  const f = recoveryFixture({ noSandbox: true }); const sandbox = f.freshSandboxPath; let rootIno = 4n, metaIno = 3n, creates = 0;
  let saved = { mode: 'unconfigured', sandbox: null, modsRoot: null, preferences: { language: 'en', theme: 'system' } }; let updates = 0;
  const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  const item = () => ({ rootIdentity: { dev: 1n, ino: rootIno }, metaIdentity: { dev: 1n, ino: metaIno }, audit: () => ({ installed: null }) });
  class FixtureInstaller {
    constructor(location) { assert.equal(location, sandbox); creates++; Object.assign(this, item()); }
    static inspectSandbox(location) { assert.equal(location, sandbox); return item(); }
  }
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }]; const forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory, createSandbox: () => sandbox });
    const created = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true }); assert.equal(created.ok, true); assert.equal(creates, 1);
    const before = structuredClone(saved); rootIno = 77n; metaIno = 55n;
    const refused = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_CHANGED'); assert.deepEqual(saved, before); assert.equal(updates, 1);
    rootIno = 4n; metaIno = 3n; assert.equal((await f.command({ command: 'destination', mode: 'sandbox', confirmed: true })).ok, true); assert.equal(updates, 2);
  } finally { f.close(); }
});


test('destination recovery: a different valid metadata directory cannot resolve an unknown prior identity', async t => {
  const root = path.resolve('__synthetic_unknown_replaced__'), metadata = metadataForModsDirectory(root);
  const v = virtualDestinationFs(t, { root, metadata, unknownMarker: true }); const f = recoveryFixture({ initialMode: 'game-mods' });
  let saved = { mode: 'game-mods', sandbox: null, modsRoot: root, preferences: { language: 'en', theme: 'system' } }; let updates = 0;
  const before = structuredClone(saved); const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }], forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, serviceFactory });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true); assert.ok(v.observations().markerOpens >= 1);
    v.raceControl.restoreMarker(); v.raceControl.metadataIno = 55n; const markers = v.observations().markerOpens;
    const refused = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
    assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_CHANGED');
    assert.ok(v.observations().markerOpens > markers); assert.ok(v.raceControl.metadataIdentityHits >= 1); assert.equal(updates, 0);
    assert.deepEqual(saved, before); assert.deepEqual(v.writes, []);
  } finally { f.close(); t.mock.restoreAll(); }
});

test('destination recovery: unknown metadata stays protected through destination switches and a known alias', async t => {
  const root = path.resolve('__synthetic_unknown_alias_root__'), alias = path.resolve('__synthetic_unknown_alias__'), other = path.resolve('__synthetic_unknown_alias_other__'), metadata = metadataForModsDirectory(root);
  const v = virtualDestinationFs(t, { root, metadata, unknownMarker: true }); const f = recoveryFixture({ initialMode: 'game-mods' });
  let saved = { mode: 'game-mods', sandbox: null, modsRoot: root, preferences: { language: 'en', theme: 'system' } }; let updates = 0, aliasExpected = 0;
  const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  class FixtureInstaller extends IsolatedInstaller {
    static inspectModsRoot(location) { if (location === alias) return { dev: 1n, ino: 4n }; if (location === other) return { dev: 2n, ino: 60n }; return IsolatedInstaller.inspectModsRoot(location); }
    static inspectModsMetadataIdentity(location) { if (location === alias || location === other) throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' }); return IsolatedInstaller.inspectModsMetadataIdentity(location); }
    static forModsDirectory(location, options) {
      if (location === other) return null;
      if (location === alias) { if (options.expectedPresence === 'existing') { aliasExpected++; throw new Error('METADATA_PRESENCE_CHANGED'); } return null; }
      return IsolatedInstaller.forModsDirectory(location, options);
    }
  }
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }], forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true); assert.ok(v.observations().markerOpens >= 1);
    const changed = await f.command({ command: 'destination', mode: 'game-mods', path: other, confirmed: true }); assert.equal(changed.ok, true); assert.equal(updates, 1);
    const before = structuredClone(saved); v.raceControl.fault = 'ENOENT';
    const refused = await f.command({ command: 'destination', mode: 'game-mods', path: alias, confirmed: true });
    assert.equal(refused.ok, false); assert.equal(refused.error, 'METADATA_PRESENCE_CHANGED'); assert.equal(aliasExpected, 1);
    assert.deepEqual(saved, before); assert.equal(updates, 1); assert.equal(refused.result.target.path, other); assert.equal(refused.result.target.unavailable, undefined); assert.deepEqual(v.writes, []);
  } finally { f.close(); t.mock.restoreAll(); }
});


test('destination recovery: a readable unknown Mods root cannot be bypassed via alias when metadata identity is denied', async t => {
  const root = path.resolve('__synthetic_unknown_root_only__'), alias = path.resolve('__synthetic_unknown_root_alias__'), other = path.resolve('__synthetic_unknown_root_other__');
  const metadata = metadataForModsDirectory(root), v = virtualDestinationFs(t, { root, metadata }); v.raceControl.identityDenied = true;
  const f = recoveryFixture({ initialMode: 'game-mods' }); let saved = { mode: 'game-mods', sandbox: null, modsRoot: root, preferences: { language: 'en', theme: 'system' } }; let updates = 0, aliasFactoryCalls = 0, aliasRootReads = 0;
  const before = structuredClone(saved), settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  class FixtureInstaller extends IsolatedInstaller {
    static inspectModsRoot(location) { if (location === alias) { aliasRootReads++; return { dev: 1n, ino: 4n }; } if (location === other) return { dev: 2n, ino: 60n }; return IsolatedInstaller.inspectModsRoot(location); }
    static inspectModsMetadataIdentity(location) { if (location === alias || location === other) throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' }); return IsolatedInstaller.inspectModsMetadataIdentity(location); }
    static forModsDirectory(location, options) { if (location === alias) { aliasFactoryCalls++; return null; } if (location === other) return null; return IsolatedInstaller.forModsDirectory(location, options); }
  }
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }], forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
    assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true); assert.ok(v.raceControl.identityDeniedHits >= 2);
    v.raceControl.identityDenied = false;
    const refused = await f.command({ command: 'destination', mode: 'game-mods', path: alias, confirmed: true });
    assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_UNVERIFIABLE'); assert.ok(aliasRootReads >= 1); assert.equal(aliasFactoryCalls, 0);
    assert.equal(updates, 0); assert.deepEqual(saved, before); assert.deepEqual(v.writes, []);
    const changed = await f.command({ command: 'destination', mode: 'game-mods', path: other, confirmed: true });
    assert.equal(changed.ok, true); assert.equal(changed.result.target.path, other); assert.equal(updates, 1); assert.deepEqual(v.writes, []);
  } finally { f.close(); t.mock.restoreAll(); }
});


test('destination recovery: metadata observed before validation cannot become absent unnoticed', async t => {
  for (const phase of ['startup', 'selection']) await t.test(phase, async child => {
    const root = path.resolve('__synthetic_preflight_present__'), other = path.resolve('__synthetic_preflight_other__');
    const f = recoveryFixture({ initialMode: 'game-mods' }); let present = true, presentProbes = 0, removalEvents = 0, updates = 0;
    let saved = { mode: 'game-mods', sandbox: null, modsRoot: phase === 'startup' ? root : other, preferences: { language: 'en', theme: 'system' } }; const before = structuredClone(saved);
    const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
    class FixtureInstaller {
      static inspectModsRoot(location) { return location === root ? { dev: 1n, ino: 4n } : { dev: 2n, ino: 60n }; }
      static inspectModsMetadataIdentity(location) {
        if (location === root && present) { presentProbes++; return { rootIdentity: { dev: 1n, ino: 4n }, metaIdentity: { dev: 1n, ino: 3n } }; }
        throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' });
      }
      static forModsDirectory(location) { if (location === root && present) { present = false; removalEvents++; } return null; }
    }
    const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }], forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
    const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
    try {
      await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
      if (phase === 'startup') {
        const status = await f.command({ command: 'status' }); assert.equal(status.result.target.unavailable, true);
        assert.equal((await f.command({ command: 'install', modules: ['example'] })).error, 'SELECT_A_MODS_DESTINATION_FIRST');
      } else {
        const refused = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
        assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_CHANGED');
        assert.equal(refused.result.target.path, other); assert.equal(refused.result.target.unavailable, undefined);
      }
      assert.ok(presentProbes >= 1); assert.equal(removalEvents, 1); assert.equal(updates, 0); assert.deepEqual(saved, before);
    } finally { f.close(); child.mock.restoreAll(); }
  });
});


test('destination recovery: a preflight identity cannot silently change during validation', async t => {
  for (const phase of ['startup', 'selection']) for (const changed of ['metadata', 'root']) await t.test(phase + '/' + changed, async child => {
    const root = path.resolve('__synthetic_preflight_change__'), other = path.resolve('__synthetic_preflight_stable__');
    const f = recoveryFixture({ initialMode: 'game-mods' }); let currentRoot = 4n, validationEvents = 0, rootProbes = 0, metadataProbes = 0, updates = 0;
    let saved = { mode: 'game-mods', sandbox: null, modsRoot: phase === 'startup' ? root : other, preferences: { language: 'en', theme: 'system' } }; const before = structuredClone(saved);
    const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
    class FixtureInstaller {
      static inspectModsRoot(location) { if (location === root) { rootProbes++; return { dev: 1n, ino: currentRoot }; } return { dev: 2n, ino: 60n }; }
      static inspectModsMetadataIdentity(location) {
        if (location === root) { metadataProbes++; if (changed === 'metadata') return { rootIdentity: { dev: 1n, ino: 4n }, metaIdentity: { dev: 1n, ino: 3n } }; throw Object.assign(new Error('SYNTHETIC_DENIED'), { code: 'EACCES' }); }
        throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' });
      }
      static forModsDirectory(location) {
        if (location !== root) return null;
        validationEvents++;
        if (changed === 'root') { currentRoot = 77n; return null; }
        return { rootIdentity: { dev: 1n, ino: 4n }, metaIdentity: { dev: 1n, ino: 55n }, audit: () => ({ installed: null }) };
      }
    }
    const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }], forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
    const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
    try {
      await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
      if (phase === 'startup') assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
      else {
        const refused = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
        assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_CHANGED');
        assert.equal(refused.result.target.path, other); assert.equal(refused.result.target.unavailable, undefined);
      }
      assert.ok(rootProbes >= 1); assert.ok(metadataProbes >= 1); assert.equal(validationEvents, 1); assert.equal(updates, 0); assert.deepEqual(saved, before);
    } finally { f.close(); child.mock.restoreAll(); }
  });
});


test('destination recovery: a first empty Mods selection cannot change roots between validation probes', async () => {
  const root = path.resolve('__synthetic_first_empty_probe_change__'), f = recoveryFixture({ initialMode: 'game-mods' });
  let saved = { mode: 'unconfigured', sandbox: null, modsRoot: null, preferences: { language: 'en', theme: 'system' } }; let updates = 0, probes = 0;
  const before = structuredClone(saved), settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
  class FixtureInstaller {
    static inspectModsRoot(location) { assert.equal(location, root); probes++; return { dev: 1n, ino: probes < 3 ? 4n : 77n }; }
    static inspectModsMetadataIdentity(location) { assert.equal(location, root); throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' }); }
    static forModsDirectory(location, options) { assert.equal(location, root); assert.equal(options.create, false); return null; }
  }
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }], forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
  const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
  try {
    await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
    const refused = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
    assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_CHANGED');
    assert.ok(probes >= 3); assert.equal(updates, 0); assert.deepEqual(saved, before);
  } finally { f.close(); }
});


test('destination recovery: an unknown metadata probe cannot silently become absent or present', async t => {
  for (const phase of ['startup', 'selection']) for (const outcome of ['absent', 'present']) await t.test(phase + '/' + outcome, async child => {
    const root = path.resolve('__synthetic_unknown_transition__'), other = path.resolve('__synthetic_unknown_transition_other__');
    const f = recoveryFixture({ initialMode: 'game-mods' }); let deniedProbes = 0, validations = 0, updates = 0;
    let saved = { mode: 'game-mods', sandbox: null, modsRoot: phase === 'startup' ? root : other, preferences: { language: 'en', theme: 'system' } }; const before = structuredClone(saved);
    const settings = { load: () => structuredClone(saved), update: transform => { updates++; saved = transform(structuredClone(saved)); return { state: structuredClone(saved) }; }, lastLoadResult: {} };
    class FixtureInstaller {
      static inspectModsRoot(location) { return location === root ? { dev: 1n, ino: 4n } : { dev: 2n, ino: 60n }; }
      static inspectModsMetadataIdentity(location) {
        if (location === root) { deniedProbes++; throw Object.assign(new Error('SYNTHETIC_DENIED'), { code: 'EACCES' }); }
        throw Object.assign(new Error('SYNTHETIC_MISSING'), { code: 'ENOENT' });
      }
      static forModsDirectory(location) {
        if (location !== root) return null;
        validations++; if (outcome === 'absent') return null;
        return { rootIdentity: { dev: 1n, ino: 4n }, metaIdentity: { dev: 1n, ino: 55n }, audit: () => ({ installed: null }) };
      }
    }
    const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example', repository: 'Owner/Repo' }], forbidden = async () => { throw new Error('UNEXPECTED_EXTERNAL_REQUEST'); };
    const serviceFactory = options => new DesktopService({ ...options, registry, request: forbidden, assetTransport: forbidden, resolvePolicy: forbidden, loadBundle: forbidden, assertGameClosed: async () => false });
    try {
      await startBackend({ settings, input: f.input, output: f.output, Installer: FixtureInstaller, serviceFactory });
      if (phase === 'startup') assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
      else {
        const refused = await f.command({ command: 'destination', mode: 'game-mods', path: root, confirmed: true });
        assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_UNVERIFIABLE');
        assert.equal(refused.result.target.path, other); assert.equal(refused.result.target.unavailable, undefined);
      }
      assert.ok(deniedProbes >= 1); assert.equal(validations, 1); assert.equal(updates, 0); assert.deepEqual(saved, before);
    } finally { f.close(); child.mock.restoreAll(); }
  });
});


test('destination recovery: an initially unidentifiable sandbox cannot be silently accepted later', async t => {
  for (const flow of ['direct', 'through-mods']) await t.test(flow, async child => {
    const f = recoveryFixture({ initialMode: 'sandbox' }); let identityProbeAttempts = 0;
    f.Installer.inspectSandboxIdentity = () => { identityProbeAttempts++; throw Object.assign(new Error('SYNTHETIC_IDENTITY_DENIED'), { code: 'EACCES' }); };
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      assert.equal((await f.command({ command: 'status' })).result.target.unavailable, true);
      if (flow === 'through-mods') assert.equal((await f.command({ command: 'destination', mode: 'game-mods', path: f.newPath, confirmed: true })).ok, true);
      f.restoreSandbox(); const before = f.saved(), updates = f.calls.updates;
      const sandboxProbesBefore = f.calls.probes.filter(item => item.kind === 'sandbox-readonly').length; assert.equal(sandboxProbesBefore, 1);
      const refused = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_UNVERIFIABLE');
      // Retained unused identity-probe expectation: assert.ok(identityProbeAttempts >= 2);
      assert.equal(identityProbeAttempts, 0); assert.equal(f.calls.probes.filter(item => item.kind === 'sandbox-readonly').length, sandboxProbesBefore);
      assert.equal(f.calls.updates, updates); assert.deepEqual(f.saved(), before); assert.equal(f.calls.sandboxCreates, 0);
      if (flow === 'direct') {
        assert.equal(refused.result.target.unavailable, true);
        const alternative = await f.command({ command: 'destination', mode: 'game-mods', path: f.newPath, confirmed: true });
        assert.equal(alternative.ok, true); assert.equal(alternative.result.target.path, f.newPath);
      } else { assert.equal(refused.result.target.path, f.newPath); assert.equal(refused.result.target.unavailable, undefined); }
    } finally { f.close(); child.mock.restoreAll(); }
  });
});


test('destination recovery: a failed first sandbox selection remains unknown while another target stays usable', async () => {
  const f = recoveryFixture({ noExistingMetadata: true });
  try {
    await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
    const before = f.saved(); assert.equal((await f.command({ command: 'status' })).result.target.unavailable, undefined);
    const first = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(first.ok, false); assert.equal(first.error, 'SYNTHETIC_DESTINATION_EACCES'); assert.equal(first.result.target.path, f.oldPath);
    assert.equal(first.result.target.unavailable, undefined); assert.equal(f.calls.updates, 0); assert.deepEqual(f.saved(), before);
    const probesBefore = f.calls.probes.filter(item => item.kind === 'sandbox-readonly').length; assert.equal(probesBefore, 1);
    f.restoreSandbox(); const refused = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
    assert.equal(refused.ok, false); assert.equal(refused.error, 'DESTINATION_IDENTITY_UNVERIFIABLE');
    assert.equal(f.calls.probes.filter(item => item.kind === 'sandbox-readonly').length, probesBefore);
    assert.equal(refused.result.target.path, f.oldPath); assert.equal(refused.result.target.unavailable, undefined);
    assert.equal(f.calls.updates, 0); assert.deepEqual(f.saved(), before); assert.equal(f.calls.sandboxCreates, 0);
  } finally { f.close(); }
});


test('destination recovery: zero sandbox identities remain unknown unless a prior identity was verified', async t => {
  await t.test('first selection stays unknown', async child => {
    const f = recoveryFixture({ noExistingMetadata: true }); let invalid = true, inspections = 0;
    f.Installer.inspectSandbox = location => {
      assert.equal(location, f.sandboxPath); inspections++;
      return { rootIdentity: { dev: invalid ? 0n : 1n, ino: invalid ? 4n : 77n }, metaIdentity: { dev: 1n, ino: invalid ? 3n : 55n }, audit: () => ({ installed: null }) };
    };
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const before = f.saved(); const first = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.equal(first.ok, false); assert.equal(first.error, 'DESTINATION_IDENTITY_UNVERIFIABLE'); assert.equal(inspections, 1);
      invalid = false; const second = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.equal(second.ok, false); assert.equal(second.error, 'DESTINATION_IDENTITY_UNVERIFIABLE'); assert.equal(inspections, 1);
      assert.equal(second.result.target.path, f.oldPath); assert.equal(second.result.target.unavailable, undefined);
      assert.equal(f.calls.updates, 0); assert.deepEqual(f.saved(), before); assert.equal(f.calls.sandboxCreates, 0);
    } finally { f.close(); child.mock.restoreAll(); }
  });
  await t.test('verified identity can be restored', async child => {
    const f = recoveryFixture({ initialMode: 'sandbox' }); f.restoreSandbox(); const original = f.Installer.inspectSandbox; let invalid = false, invalidInspections = 0;
    f.Installer.inspectSandbox = location => {
      if (invalid) { invalidInspections++; return { rootIdentity: { dev: 0n, ino: 4n }, metaIdentity: { dev: 1n, ino: 3n }, audit: () => ({ installed: null }) }; }
      return original(location);
    };
    try {
      await startBackend({ settings: f.settings, input: f.input, output: f.output, Installer: f.Installer, serviceFactory: f.serviceFactory, createSandbox: f.createSandbox });
      const before = f.saved(); invalid = true; const first = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.equal(first.ok, false); assert.equal(first.error, 'DESTINATION_IDENTITY_UNVERIFIABLE'); assert.ok(invalidInspections >= 1);
      assert.equal(f.calls.updates, 0); assert.deepEqual(f.saved(), before);
      invalid = false; const restored = await f.command({ command: 'destination', mode: 'sandbox', confirmed: true });
      assert.equal(restored.ok, true); assert.equal(restored.result.target.unavailable, undefined); assert.equal(f.calls.updates, 1);
    } finally { f.close(); child.mock.restoreAll(); }
  });
});
