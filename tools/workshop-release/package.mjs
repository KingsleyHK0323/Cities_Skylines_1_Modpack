// Signature: OpenAI-Codex_workshop_implementation
import {lstatSync, readdirSync, readFileSync, writeFileSync} from 'node:fs';
import {resolve, join, dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import {execFileSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import {createReleaseManifest, verifyReleasePackage} from './build-policy.mjs';
import {recipes} from './recipes.mjs';

const allowed = Object.freeze({
  AI_Improve:['AIImprove.dll','CitiesHarmony.API.dll','CitiesHarmony-LICENSE.txt','THIRD-PARTY-NOTICES.md'],
  ...Object.fromEntries(recipes.map(recipe => [recipe.module,[`${recipe.assembly}.dll`]])),
});
const fail = message => { throw new Error(message); };

export function readContent(path, moduleKey) {
  const moduleFiles = allowed[moduleKey];
  if (!moduleFiles) fail('The module has no reviewed package recipe.');
  const root = resolve(path);
  const rootStat = lstatSync(root);
  if (!rootStat.isDirectory() || rootStat.isSymbolicLink()) fail('Content root is not a regular directory.');
  const names = readdirSync(root).sort();
  if (JSON.stringify(names) !== JSON.stringify([...moduleFiles].sort())) fail('Content has missing or unexpected files.');
  const files = {};
  for (const name of moduleFiles) {
    const file = join(root, name);
    const stat = lstatSync(file);
    if (!stat.isFile() || stat.isSymbolicLink() || stat.nlink !== 1 || stat.size < 1 || stat.size > 32 * 1024 * 1024)
      fail('A content file is not a single regular file of an allowed size.');
    files[name] = readFileSync(file);
    if (files[name].length !== stat.size) fail('A content file changed during reading.');
  }
  return files;
}

function inspect(root, moduleKey) {
  const inspector = join(dirname(fileURLToPath(import.meta.url)), 'Inspect-Assemblies.ps1');
  const dll = moduleKey === 'AI_Improve' ? 'AIImprove.dll' :
    recipes.find(recipe => recipe.module === moduleKey)?.assembly + '.dll';
  if (!allowed[moduleKey] || !dll) fail('The module has no reviewed assembly inspector.');
  const args = ['-NoLogo','-NoProfile','-NonInteractive','-File',inspector, '-Module',moduleKey, '-ModuleDll',join(root,dll)];
  if (moduleKey === 'AI_Improve') args.push('-DependencyDll',join(root,'CitiesHarmony.API.dll'));
  const result = execFileSync('pwsh', args,
  {encoding:'utf8', timeout:15000, maxBuffer:16384, windowsHide:true});
  return JSON.parse(result.trim());
}

export function expectedFromEnvironment() {
  const snapshot = JSON.parse(Buffer.from(process.env.WORKSHOP_SNAPSHOT_BASE64 || '', 'base64').toString('utf8'));
  const moduleKey = process.env.WORKSHOP_MODULE_KEY;
  const matches = Array.isArray(snapshot?.modules) ? snapshot.modules.filter(item => item?.key === moduleKey) : [];
  if (snapshot?.schema !== 1 || snapshot.repository !== 'KingsleyHK0323/Cities_Skylines_1_Modpack' ||
      !allowed[moduleKey] || matches.length !== 1) fail('The module is not part of the reviewed snapshot.');
  return { issueNumber:snapshot.issueNumber, runId:Number(process.env.GITHUB_RUN_ID),
    attempt:Number(process.env.GITHUB_RUN_ATTEMPT), controllerSha:process.env.GITHUB_SHA,
    module:matches[0].key, sourceSha:matches[0].sourceSha,
    workshopId:matches[0].workshopId };
}

export function verifyPackageFromDisk(contentArg, manifestArg, expected) {
  const content = resolve(contentArg);
  const manifestPath = resolve(manifestArg);
  const stat = lstatSync(manifestPath);
  if (!stat.isFile() || stat.isSymbolicLink() || stat.nlink !== 1 || stat.size < 1 || stat.size > 65536)
    fail('Manifest is not a safe regular file.');
  const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
  verifyReleasePackage(manifest, readContent(content, expected.module), expected, inspect(content, expected.module));
  return createHash('sha256').update(JSON.stringify(manifest)).digest('hex');
}

function main() {
  const [mode, contentArg, manifestArg] = process.argv.slice(2);
  if (!['create','verify'].includes(mode) || !contentArg || !manifestArg || process.argv.length !== 5)
    fail('Usage: node package.mjs create|verify <content-directory> <manifest-file>');
  const content = resolve(contentArg);
  const manifestPath = resolve(manifestArg);
  const expected = expectedFromEnvironment();
  if (mode === 'create') {
    const files = readContent(content, expected.module);
    const identities = inspect(content, expected.module);
    const manifest = createReleaseManifest(expected, files);
    verifyReleasePackage(manifest, files, expected, identities);
    writeFileSync(manifestPath, JSON.stringify(manifest) + '\n', {encoding:'utf8', flag:'wx'});
  } else {
    verifyPackageFromDisk(content, manifestPath, expected);
  }
  process.stdout.write('Release package identity, dependency set and hashes verified.\n');
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { main(); }
  catch (error) { process.stderr.write(`Release package validation failed: ${error.message}\n`); process.exitCode = 1; }
}
