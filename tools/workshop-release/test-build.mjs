// Signature: OpenAI-Codex_workshop_implementation
import test from 'node:test';
import assert from 'node:assert/strict';
import {createHash} from 'node:crypto';
let build;
try { build = await import('./build-policy.mjs'); } catch (error) {
  if (error.code !== 'ERR_MODULE_NOT_FOUND') throw error;
}

test('the cloud build policy is available', () => {
  assert.ok(build, 'the cloud build policy has not been implemented');
});

function packageFixture() {
  const files = {
    'AIImprove.dll': Buffer.from('MZ simulated managed AIImprove assembly'),
    'CitiesHarmony.API.dll': Buffer.from('MZ simulated CitiesHarmony API assembly'),
    'CitiesHarmony-LICENSE.txt': Buffer.from('MIT permission notice'),
    'THIRD-PARTY-NOTICES.md': Buffer.from('CitiesHarmony API notice'),
  };
  const manifest = {schema:1, repository:'KingsleyHK0323/Cities_Skylines_1_Modpack',
    issueNumber:17, runId:700, attempt:1, controllerSha:'a'.repeat(40),
    module:'AI_Improve', sourceSha:'0fd60bcdd986e6e2ae40d162c326f1232c1bf639', workshopId:'3782858610',
    files:Object.entries(files).map(([name,bytes]) => ({name, size:bytes.length,
      sha256:createHash('sha256').update(bytes).digest('hex')}))};
  const expected = {issueNumber:17, runId:700, attempt:1, controllerSha:'a'.repeat(40),
    module:'AI_Improve', sourceSha:'0fd60bcdd986e6e2ae40d162c326f1232c1bf639', workshopId:'3782858610'};
  const identities = {'AIImprove.dll':'AIImprove', 'CitiesHarmony.API.dll':'CitiesHarmony.API'};
  return {files, manifest, expected, identities};
}

test('a release package has the exact source, target, identities, dependencies and hashes', () => {
  const f = packageFixture();
  assert.equal(build.verifyReleasePackage(f.manifest, f.files, f.expected, f.identities), true);
});

test('manifest creation binds the exact approved request and bytes', () => {
  const f = packageFixture();
  const generated = build.createReleaseManifest(f.expected, f.files);
  assert.deepEqual(generated, f.manifest);
  assert.equal(build.verifyReleasePackage(generated, f.files, f.expected, f.identities), true);
  delete f.files['CitiesHarmony.API.dll'];
  assert.throws(() => build.createReleaseManifest(f.expected, f.files));
});

test('wrong issue, run, attempt, source, module, target or assembly is rejected', () => {
  for (const mutate of [
    f => { f.manifest.issueNumber = 18; }, f => { f.manifest.runId = 701; },
    f => { f.manifest.attempt = 2; }, f => { f.manifest.controllerSha = 'c'.repeat(40); },
    f => { f.manifest.sourceSha = 'd'.repeat(40); }, f => { f.manifest.module = 'AI_Gaming'; },
    f => { f.manifest.workshopId = '3823740106'; },
    f => { f.identities['AIImprove.dll'] = 'AIGaming'; },
    f => { f.identities['CitiesHarmony.API.dll'] = 'SomethingElse'; },
  ]) {
    const f = packageFixture(); mutate(f);
    assert.throws(() => build.verifyReleasePackage(f.manifest, f.files, f.expected, f.identities));
  }
});

test('missing, extra, empty or modified content and DevTool markers are rejected', () => {
  for (const mutate of [
    f => { delete f.files['CitiesHarmony.API.dll']; },
    f => { f.files['Assembly-CSharp.dll'] = Buffer.from('game reference'); },
    f => { f.files['CitiesHarmony-LICENSE.txt'] = Buffer.alloc(0); },
    f => { f.files['AIImprove.dll'] = Buffer.from('tampered'); },
    f => { f.files['AIImprove.dll'] = Buffer.from('DevTriggerExtension'); },
  ]) {
    const f = packageFixture(); mutate(f);
    assert.throws(() => build.verifyReleasePackage(f.manifest, f.files, f.expected, f.identities));
  }
  for (const marker of ['DevTriggerExtension','LateMergeProbe','SanitationLifecycleProbe',
    'TryGetAssignedBuilding','This version has DevTool']) {
    const f = packageFixture(); f.files['AIImprove.dll'] = Buffer.from(marker, 'utf16le');
    f.manifest.files.find(file => file.name === 'AIImprove.dll').size = f.files['AIImprove.dll'].length;
    f.manifest.files.find(file => file.name === 'AIImprove.dll').sha256 = createHash('sha256').update(f.files['AIImprove.dll']).digest('hex');
    assert.throws(() => build.verifyReleasePackage(f.manifest, f.files, f.expected, f.identities));
  }
});

test('a different module cannot reuse the AI Improve build recipe or target', () => {
  const f = packageFixture();
  f.expected.module = 'Automated_Public_Transport_Planning';
  f.manifest.module = f.expected.module;
  assert.throws(() => build.verifyReleasePackage(f.manifest, f.files, f.expected, f.identities));
});

test('Wireless uses its separately reviewed exact source and single-DLL recipe', () => {
  const expected = {issueNumber:17,runId:700,attempt:1,controllerSha:'a'.repeat(40),
    module:'Wireless_Utilities_And_Global_Coverage',sourceSha:'fab5d2bfa27ce03981de3a845cf4ee40148a1201',workshopId:'3832301115'};
  const files = {'WirelessUtilitiesAndGlobalCoverage.dll':Buffer.from('MZ simulated no DevTool foundation')};
  const identities = {'WirelessUtilitiesAndGlobalCoverage.dll':'WirelessUtilitiesAndGlobalCoverage'};
  const manifest = build.createReleaseManifest(expected, files);
  assert.equal(build.verifyReleasePackage(manifest, files, expected, identities), true);
  const changed = {...expected, sourceSha:'c'.repeat(40)};
  assert.throws(() => build.createReleaseManifest(changed, files));
  assert.throws(() => build.verifyReleasePackage(manifest, {...files,'Assembly-CSharp.dll':Buffer.from('game')}, expected, identities));
});

test('a known Development Preview label cannot be accepted as a release package', () => {
  const expected = {issueNumber:17,runId:700,attempt:1,controllerSha:'a'.repeat(40),
    module:'Full_Map_Unlock_And_Expansion',sourceSha:'d9daec9217b454f8dd8d751bcf480fb5d6df8dc9',workshopId:'3832298906'};
  const files = {'FullMapUnlockAndExpansion.dll':Buffer.from('Full Map Unlock And Expansion [Development Preview]', 'utf16le')};
  const identities = {'FullMapUnlockAndExpansion.dll':'FullMapUnlockAndExpansion'};
  const manifest = build.createReleaseManifest(expected, files);
  assert.throws(() => build.verifyReleasePackage(manifest, files, expected, identities), /Development/i);
});

// Additional validation: OpenAI-Codex.
