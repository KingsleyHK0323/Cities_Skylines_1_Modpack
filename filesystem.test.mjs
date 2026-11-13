// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Fixture calls provide explicit expected installation snapshots.
import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { createHash } from 'node:crypto';

const sandbox = () => fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-'));
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const pkg = (buildId = 'a', content = 'fixture A') => ({ moduleId: 'example', buildId,
  files: ['mod.dll', 'dependency/api.dll'].map(p => {
    const bytes = Buffer.from(content + p);
    return { path: p, bytes, sha256: hash(bytes) };
  }) });
const read = (root, name = 'example/mod.dll') => fs.readFileSync(path.join(root, name), 'utf8');
const create = async (root, options) => {
  const { IsolatedInstaller } = await import('./filesystem.mjs');
  return new IsolatedInstaller(root, options);
};

for (const name of ['lock', 'journal.json']) {
  for (const method of ['writeFileSync', 'fsyncSync']) {
    test(`partial ${name} creation reports retained recovery state: ${method}`, async t => {
      const root = sandbox(); const manager = await create(root);
      const primary = Object.freeze(new Error('metadata creation injected failure'));
      const open = fs.openSync; const original = fs[method]; let targetFd;
      t.mock.method(fs, 'openSync', (target, ...args) => {
        const fd = open(target, ...args);
        if (target === path.join(root, '.dev-manager', name) && args[0] === 'wx') targetFd = fd;
        return fd;
      });
      t.mock.method(fs, method, (fd, ...args) => {
        if (fd === targetFd) throw primary;
        return original(fd, ...args);
      });
      assert.throws(() => manager.install(pkg(), null), error => {
        assert.equal(error.committed, false);
        assert.equal(error.recoveryRequired, true);
        assert.ok(error.errors.includes(primary));
        if (name === 'lock') assert.equal(error.lockMayRemain, true);
        return true;
      });
      assert.ok(fs.existsSync(path.join(root, '.dev-manager', name)));
      assert.equal(fs.existsSync(path.join(root, 'example')), false);
      assert.deepEqual(JSON.parse(fs.readFileSync(path.join(root, '.dev-manager', 'state.json'))).modules, {});
    });
  }
}

for (const phase of ['success', 'before-state', 'journal', 'lock-read', 'release-check']) {
  test(`lock release failure preserves transaction outcome: ${phase}`, async t => {
    const root = sandbox();
    const primary = Object.freeze(new Error('primary injected failure'));
    const cleanup = new Error('lock release injected failure');
    const manager = await create(root, { fault: point => {
      if (phase === 'before-state' && point === phase) throw primary;
    } });
    const unlink = fs.unlinkSync;
    if (phase === 'lock-read') {
      const lstat = fs.lstatSync;
      t.mock.method(fs, 'lstatSync', (target, ...args) => {
        if (target === path.join(root, '.dev-manager', 'lock')) throw cleanup;
        return lstat(target, ...args);
      });
    }
    if (phase === 'release-check') {
      const load = manager.load.bind(manager);
      t.mock.method(manager, 'load', () => {
        load();
        if (manager.state.modules.example) t.mock.method(manager, 'check', () => { throw cleanup; });
      });
    }
    t.mock.method(fs, 'unlinkSync', target => {
      if (target === path.join(root, '.dev-manager', 'lock')) throw cleanup;
      if (phase === 'journal' && target === path.join(root, '.dev-manager', 'journal.json')) throw primary;
      return unlink(target);
    });
    assert.throws(() => manager.install(pkg(), null), error => {
      assert.equal(error.committed, phase !== 'before-state');
      assert.equal(error.lockReleaseFailed, true);
      assert.ok(error.errors.includes(cleanup));
      if (['before-state', 'journal'].includes(phase)) assert.ok(error.errors.includes(primary));
      return true;
    });
    const state = JSON.parse(fs.readFileSync(path.join(root, '.dev-manager', 'state.json')));
    assert.equal(Object.hasOwn(state.modules, 'example'), phase !== 'before-state');
    assert.equal(fs.existsSync(path.join(root, 'example')), phase !== 'before-state');
    assert.ok(fs.existsSync(path.join(root, '.dev-manager', 'lock')));
    if (phase !== 'release-check') assert.throws(() => manager.install(pkg('retry'), null), /EEXIST/);
  });
}

test('real install, reopen, update and uninstall preserve inactive backups', async () => {
  const root = sandbox();
  let manager = await create(root);
  manager.install(pkg(), null);
  assert.equal(read(root), 'fixture Amod.dll');
  manager = await create(root);
  assert.equal(manager.installed('example').buildId, 'a');
  manager.update(pkg('b', 'fixture B'), manager.installed('example'));
  assert.equal(read(root), 'fixture Bmod.dll');
  assert.equal(manager.installed('example').buildId, 'b');
  manager.uninstall('example', manager.installed('example'));
  assert.equal(fs.existsSync(path.join(root, 'example')), false);
  assert.equal(manager.installed('example'), null);
  assert.ok(fs.readdirSync(path.join(root, '.dev-manager', 'backups')).length >= 2);
});
test('unknown existing installations are never adopted or overwritten', async () => {
  const root = sandbox(); fs.mkdirSync(path.join(root, 'example'));
  fs.writeFileSync(path.join(root, 'example', 'personal.txt'), 'user file');
  const manager = await create(root);
  assert.throws(() => manager.install(pkg(), null));
  assert.equal(read(root, 'example/personal.txt'), 'user file');
  assert.throws(() => manager.uninstall('example', manager.installed('example')));
});
test('modified or extra files block update and uninstall without any removal', async () => {
  for (const changed of ['modified', 'extra']) {
    const root = sandbox(); const manager = await create(root); manager.install(pkg(), null);
    const file = changed === 'modified' ? 'mod.dll' : 'personal.txt';
    fs.writeFileSync(path.join(root, 'example', file), 'user change');
    assert.throws(() => manager.update(pkg('b'), manager.installed('example')));
    assert.throws(() => manager.uninstall('example', manager.installed('example')));
    assert.equal(read(root, `example/${file}`), 'user change');
    assert.equal(manager.installed('example').buildId, 'a');
  }
});
test('tampered input never becomes an active installation', async () => {
  const root = sandbox(); const manager = await create(root); const input = pkg();
  input.files[1].bytes = Buffer.from('changed');
  assert.throws(() => manager.install(input, null));
  assert.equal(fs.existsSync(path.join(root, 'example')), false);
});
test('update failures before commit restore the old installation and record', async () => {
  for (const point of ['staged', 'backed-up', 'activated', 'before-state']) {
    const root = sandbox(); (await create(root)).install(pkg(), null);
    const manager = await create(root, { fault: stage => { if (stage === point) throw new Error('injected failure'); } });
    assert.throws(() => manager.update(pkg('b', 'fixture B'), manager.installed('example')), /injected failure/);
    assert.equal(read(root), 'fixture Amod.dll', point);
    assert.equal((await create(root)).installed('example').buildId, 'a');
  }
});
test('failed uninstall restores the original installation', async () => {
  const root = sandbox(); (await create(root)).install(pkg(), null);
  const manager = await create(root, { fault: stage => { if (stage === 'before-state') throw new Error('injected'); } });
  assert.throws(() => manager.uninstall('example', manager.installed('example')));
  assert.equal(read(root), 'fixture Amod.dll');
});
test('directory links are rejected without touching outside content', async () => {
  const root = sandbox(); const outside = sandbox(); fs.writeFileSync(path.join(outside, 'sentinel'), 'outside');
  fs.symlinkSync(outside, path.join(root, 'example'), 'junction');
  const manager = await create(root);
  assert.throws(() => manager.install(pkg(), null));
  assert.throws(() => manager.uninstall('example', manager.installed('example')));
  assert.equal(read(outside, 'sentinel'), 'outside');
  assert.throws(() => new manager.constructor(path.join(root, 'example')));
});
test('link injected after staging is rejected before activation', async () => {
  const root = sandbox(); const outside = sandbox();
  let injected = false;
  fs.writeFileSync(path.join(outside, 'sentinel'), 'outside');
  const manager = await create(root, { fault: stage => {
    if (stage === 'staged') { fs.symlinkSync(outside, path.join(root, 'example'), 'junction'); injected = true; }
  } });
  assert.throws(() => manager.install(pkg(), null));
  assert.equal(injected, true, 'the adversarial filesystem change must actually have happened');
  assert.equal(read(outside, 'sentinel'), 'outside');
});
test('concurrent operation and incomplete journal fail closed', async () => {
  const root = sandbox(); const manager = await create(root);
  fs.writeFileSync(path.join(root, '.dev-manager', 'lock'), 'another operation');
  assert.throws(() => manager.install(pkg(), null));
  const otherRoot = sandbox(); await create(otherRoot);
  fs.writeFileSync(path.join(otherRoot, '.dev-manager', 'journal.json'), '{}');
  await assert.rejects(() => create(otherRoot));
});
test('hard-linked files cannot be treated as manager-owned normal files', async () => {
  const root = sandbox(); const manager = await create(root); manager.install(pkg(), null);
  const outside = sandbox();
  fs.linkSync(path.join(root, 'example', 'mod.dll'), path.join(outside, 'linked'));
  assert.throws(() => manager.uninstall('example', manager.installed('example')));
  assert.equal(read(outside, 'linked'), 'fixture Amod.dll');
});
test('executor refuses a non-isolated root before creating metadata', async () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'ordinary-user-directory-'));
  await assert.rejects(() => create(root), /isolated/);
  assert.deepEqual(fs.readdirSync(root), []);
});
test('unexpected empty directories also preserve user data', async () => {
  const root = sandbox(); const manager = await create(root); manager.install(pkg(), null);
  fs.mkdirSync(path.join(root, 'example', 'user-folder'));
  assert.throws(() => manager.update(pkg('b'), manager.installed('example')));
  assert.throws(() => manager.uninstall('example', manager.installed('example')));
  assert.equal(fs.statSync(path.join(root, 'example', 'user-folder')).isDirectory(), true);
});
test('rollback preserves edits introduced during attempted activation', async () => {
  const root = sandbox(); (await create(root)).install(pkg(), null);
  const manager = await create(root, { fault: point => {
    if (point === 'activated') fs.writeFileSync(path.join(root, 'example', 'personal.txt'), 'concurrent edit');
  } });
  assert.throws(() => manager.update(pkg('b'), manager.installed('example')));
  assert.equal(read(root), 'fixture Amod.dll');
  const backups = path.join(root, '.dev-manager', 'backups');
  const failed = fs.readdirSync(backups).find(name => name.endsWith('-failed'));
  assert.equal(fs.readFileSync(path.join(backups, failed, 'personal.txt'), 'utf8'), 'concurrent edit');
});
test('replacement link after backup preserves the old copy and recovery journal', async () => {
  const root = sandbox(); const outside = sandbox(); (await create(root)).install(pkg(), null);
  fs.writeFileSync(path.join(outside, 'sentinel'), 'outside');
  let injected = false;
  const manager = await create(root, { fault: point => {
    if (point === 'backed-up') { fs.symlinkSync(outside, path.join(root, 'example'), 'junction'); injected = true; }
  } });
  assert.throws(() => manager.update(pkg('b'), manager.installed('example')));
  assert.equal(injected, true);
  assert.equal(read(outside, 'sentinel'), 'outside');
  assert.equal(fs.existsSync(path.join(root, '.dev-manager', 'journal.json')), true);
  const backupRoot = path.join(root, '.dev-manager', 'backups');
  const saved = fs.readdirSync(backupRoot).find(name => !name.endsWith('-failed'));
  assert.equal(fs.readFileSync(path.join(backupRoot, saved, 'mod.dll'), 'utf8'), 'fixture Amod.dll');
  await assert.rejects(() => create(root), /recovery/);
});
test('mutating caller buffers after staging does not alter verified staged files', async () => {
  const input = pkg(); const root = sandbox();
  const manager = await create(root, { fault: point => { if (point === 'staged') input.files[0].bytes.fill(0); } });
  manager.install(input, null);
  assert.equal(read(root), 'fixture Amod.dll');
});
test('oversized owned-file replacement is rejected before reading it', async () => {
  const root = sandbox(); const manager = await create(root); manager.install(pkg(), null);
  const target = path.join(root, 'example', 'mod.dll');
  fs.truncateSync(target, 32 * 1024 * 1024 + 1);
  assert.throws(() => manager.uninstall('example', manager.installed('example')), /Oversized/);
  assert.equal(fs.statSync(target).size, 32 * 1024 * 1024 + 1);
});
test('a new destination during uninstall blocks completion and retains backups', async () => {
  const root = sandbox(); (await create(root)).install(pkg(), null);
  const manager = await create(root, { fault: point => {
    if (point === 'backed-up') {
      fs.mkdirSync(path.join(root, 'example'));
      fs.writeFileSync(path.join(root, 'example', 'user.txt'), 'user replacement');
    }
  } });
  assert.throws(() => manager.uninstall('example', manager.installed('example')));
  assert.equal(read(root, 'example/user.txt'), 'user replacement');
  assert.equal(fs.existsSync(path.join(root, '.dev-manager', 'journal.json')), true);
});
