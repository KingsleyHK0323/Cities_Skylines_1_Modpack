// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { safeRelative, verifyPackage, installationPlan, uninstallPlan, updateStatus, coexistenceWarning } from './core.mjs';
const bytes = Buffer.from('synthetic fixture, not a game DLL');
const sha256 = createHash('sha256').update(bytes).digest('hex');
const pkg = () => ({ moduleId: 'example', buildId: 'build-a', files: [{ path: 'mod.dll', bytes, sha256 }] });
test('rejects traversal, Windows devices, alternate streams and ambiguous names', () => {
  for (const p of ['../x', '/x', 'C:/x', 'a\\b', 'x:stream', 'NUL.dll', 'a/../x', 'x.', 'x ', 'a//b'])
    assert.throws(() => safeRelative(p));
});
test('verifies all files and detects tampering', () => {
  assert.equal(verifyPackage(pkg()).length, 1);
  const p = pkg(); p.files[0].bytes = Buffer.from('changed');
  assert.throws(() => verifyPackage(p));
});
test('rejects case-insensitive duplicate destinations', () => {
  const p = pkg(); p.files.push({ ...p.files[0], path: 'MOD.dll' });
  assert.throws(() => verifyPackage(p));
});
test('does not overwrite a pre-existing user file', () => {
  assert.throws(() => installationPlan('D:\\Mods', pkg(), ['example/MOD.dll']));
  assert.equal(installationPlan('D:\\Mods', pkg())[0].destination, 'D:\\Mods\\example\\mod.dll');
});
test('uninstall retains modified files and rejects reparse points', () => {
  const record = { moduleId: 'example', files: [{ relative: 'example/mod.dll', sha256 }] };
  assert.deepEqual(uninstallPlan(record, { 'example/mod.dll': { sha256 } }), ['example/mod.dll']);
  assert.throws(() => uninstallPlan(record, { 'example/mod.dll': { sha256: 'changed' } }));
  assert.throws(() => uninstallPlan(record, { 'example/mod.dll': { sha256, reparsePoint: true } }));
  assert.throws(() => uninstallPlan({ ...record, moduleId: 'other' }, {}));
});
test('versions are compared per module without assuming numeric ordering', () => {
  assert.equal(updateStatus(pkg(), pkg()), 'current');
  assert.equal(updateStatus(pkg(), { moduleId: 'example', buildId: 'build-b' }), 'different-build');
  assert.throws(() => updateStatus(pkg(), { moduleId: 'other', buildId: 'a' }));
});
test('coexistence produces only a warning', () => {
  assert.equal(typeof coexistenceWarning(true, true), 'string');
  assert.equal(coexistenceWarning(true, false), null);
});
test('module identifiers cannot be Windows devices or ambiguous names', () => {
  for (const moduleId of ['CON', 'nul', 'com1', 'LPT9', 'a.', 'a ', 'a/b'])
    assert.throws(() => verifyPackage({ ...pkg(), moduleId }), moduleId);
});
test('rejects file versus directory overlap in either order', () => {
  for (const paths of [['a', 'a/b.dll'], ['A/b.dll', 'a']]) {
    const p = pkg(); p.files = paths.map(p => ({ path: p, bytes, sha256 }));
    assert.throws(() => verifyPackage(p));
  }
});
test('rejects invalid Windows characters and reserved device variants', () => {
  for (const name of ['a?.dll', 'a*.dll', 'a|b', 'a<b', 'COM¹.dll', 'CON .txt'])
    assert.throws(() => safeRelative(name), name);
});
test('ownership plans reject nested module identities and malformed hashes', () => {
  assert.throws(() => uninstallPlan({ moduleId: 'example/sub', files: [
    { relative: 'example/sub/mod.dll', sha256 }] }, { 'example/sub/mod.dll': { sha256 } }));
  assert.throws(() => uninstallPlan({ moduleId: 'example', files: [
    { relative: 'example/mod.dll', sha256: 'not-a-hash' }] },
  { 'example/mod.dll': { sha256: 'not-a-hash' } }));
});
