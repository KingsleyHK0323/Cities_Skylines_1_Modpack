// Signature: OpenAI-Codex_workshop_implementation
// Assembly identities must be independently extracted from the actual managed PE bytes.
import {createHash} from 'node:crypto';
import {registry} from './request.mjs';
import {recipes} from './recipes.mjs';

const repository = 'KingsleyHK0323/Cities_Skylines_1_Modpack';
const profiles = Object.freeze({
  AI_Improve: Object.freeze({workshopId:'3782858610', reviewedSource:'0fd60bcdd986e6e2ae40d162c326f1232c1bf639', files:Object.freeze([
    'AIImprove.dll','CitiesHarmony.API.dll','CitiesHarmony-LICENSE.txt','THIRD-PARTY-NOTICES.md',
  ]), identities:Object.freeze({'AIImprove.dll':'AIImprove','CitiesHarmony.API.dll':'CitiesHarmony.API'})}),
  ...Object.fromEntries(recipes.map(recipe => {
    const item = registry.find(entry => entry.key === recipe.module);
    const filename = `${recipe.assembly}.dll`;
    return [recipe.module, Object.freeze({workshopId:item.workshopId, reviewedSource:recipe.sourceSha,
      files:Object.freeze([filename]), identities:Object.freeze({[filename]:recipe.assembly})})];
  })),
});
const forbidden = Object.freeze([
  'DevTriggerExtension', 'LateMergeProbe', 'SanitationLifecycleProbe',
  'TryGetAssignedBuilding', 'This version has DevTool',
]);
const fail = message => { throw new Error(message); };

export function createReleaseManifest(expected, files) {
  const profile = profiles[expected?.module];
  if (!expected || !profile || expected.workshopId !== profile.workshopId ||
      (profile.reviewedSource && expected.sourceSha !== profile.reviewedSource) ||
      !Number.isSafeInteger(expected.issueNumber) || expected.issueNumber <= 0 ||
      !Number.isSafeInteger(expected.runId) || expected.runId <= 0 || expected.attempt !== 1 ||
      !/^[0-9a-f]{40}$/.test(expected.controllerSha) || !/^[0-9a-f]{40}$/.test(expected.sourceSha) ||
      !files || Object.keys(files).length !== profile.files.length) fail('Cannot create an unbound release manifest.');
  const entries = profile.files.map(name => {
    const bytes = files[name];
    if (!Buffer.isBuffer(bytes) || bytes.length <= 0 || bytes.length > 32 * 1024 * 1024)
      fail('A required release file is missing or invalid.');
    return {name, size:bytes.length, sha256:createHash('sha256').update(bytes).digest('hex')};
  });
  return {schema:1, repository, issueNumber:expected.issueNumber, runId:expected.runId,
    attempt:expected.attempt, controllerSha:expected.controllerSha, module:expected.module,
    sourceSha:expected.sourceSha, workshopId:expected.workshopId, files:entries};
}

export function verifyReleasePackage(manifest, files, expected, identities) {
  const profile = profiles[expected?.module];
  if (!manifest || !expected || manifest.schema !== 1 || manifest.repository !== repository ||
      !profile || expected.workshopId !== profile.workshopId ||
      (profile.reviewedSource && expected.sourceSha !== profile.reviewedSource) ||
      !Number.isSafeInteger(expected.issueNumber) || expected.issueNumber <= 0 ||
      !Number.isSafeInteger(expected.runId) || expected.runId <= 0 || expected.attempt !== 1 ||
      !/^[0-9a-f]{40}$/.test(expected.controllerSha) || !/^[0-9a-f]{40}$/.test(expected.sourceSha))
    fail('Release package expectations are invalid or the module is not implemented.');
  for (const key of ['issueNumber','runId','attempt','controllerSha','module','sourceSha','workshopId'])
    if (manifest[key] !== expected[key]) fail('Release package provenance or target does not match.');
  if (!files || !Array.isArray(manifest.files) || manifest.files.length !== profile.files.length ||
      Object.keys(files).length !== profile.files.length || !identities ||
      Object.entries(profile.identities).some(([file, name]) => identities[file] !== name))
    fail('Release package contents or managed assembly identities do not match.');
  const seen = new Set();
  let total = 0;
  for (const entry of manifest.files) {
    if (!entry || !profile.files.includes(entry.name) || seen.has(entry.name) ||
        !Number.isSafeInteger(entry.size) || entry.size <= 0 || entry.size > 32 * 1024 * 1024 ||
        typeof entry.sha256 !== 'string' || !/^[0-9a-f]{64}$/.test(entry.sha256))
      fail('Release manifest contains an unexpected or duplicate entry.');
    seen.add(entry.name);
    const bytes = files[entry.name];
    if (!Buffer.isBuffer(bytes) || bytes.length !== entry.size ||
        createHash('sha256').update(bytes).digest('hex') !== entry.sha256)
      fail('Release file bytes do not match their manifest.');
    total += bytes.length;
    if (total > 64 * 1024 * 1024) fail('Release package is too large.');
  }
  if (profile.files.some(name => !seen.has(name))) fail('Release package is incomplete.');
  const mainDllName = Object.keys(profile.identities).find(name => name !== 'CitiesHarmony.API.dll');
  const mainDll = files[mainDllName];
  for (const marker of ['Development Preview', 'Development Version. This version has DevTool'])
    if (mainDll.includes(Buffer.from(marker, 'utf8')) || mainDll.includes(Buffer.from(marker, 'utf16le')))
      fail('A known Development label is present in the release DLL.');
  if (expected.module === 'AI_Improve') {
    const dll = files['AIImprove.dll'];
    for (const marker of forbidden)
      if (dll.includes(Buffer.from(marker, 'utf8')) || dll.includes(Buffer.from(marker, 'utf16le')))
        fail('A reviewed Development-only marker is present in the release DLL.');
  }
  return true;
}

// Known development-label guard: OpenAI-Codex.
