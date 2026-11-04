// Signature: OpenAI-Codex_workshop_implementation
import {lstatSync, readFileSync, writeFileSync} from 'node:fs';
import {resolve, relative, isAbsolute, join, sep, dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {GitHubClient} from './github.mjs';
import {recheck} from './control.mjs';
import {expectedFromEnvironment, verifyPackageFromDisk} from './package.mjs';
import {queryRegisteredItem} from './steam.mjs';
import {parseChangeNote, registry, requireReady} from './request.mjs';
const fail = message => { throw new Error(message); };

export async function submitAfterChecks({requireReady: checkReady, recheck, verifyPackage, steamPreflight, prepare, submit}) {
  if ([checkReady, recheck, verifyPackage, steamPreflight, prepare, submit].some(fn => typeof fn !== 'function'))
    fail('Upload protections are incomplete.');
  if (await checkReady() !== true) fail('The selected modules are not ready for upload.');
  if (await recheck() !== true) fail('Approval is no longer valid.');
  const before = await verifyPackage();
  if (typeof before !== 'string' || !/^[0-9a-f]{64}$/.test(before)) fail('Package identity is invalid.');
  if (await steamPreflight() !== true) fail('Steam item preflight failed.');
  if (await recheck() !== true) fail('Approval changed during Steam preflight.');
  const after = await verifyPackage();
  if (after !== before) fail('Package changed during Steam preflight.');
  let prepared;
  try { prepared = await prepare(); }
  catch { fail('Upload preparation failed before SteamCMD was started.'); }
  try { await submit(prepared); } catch { /* A started Steam submission may still complete. */ }
  return {status:'unknown', attempted:true};
}

export function requireTempPath(target) {
  const root = resolve(process.env.RUNNER_TEMP || '');
  const absolute = resolve(target);
  const suffix = relative(root, absolute);
  if (!process.env.RUNNER_TEMP || !suffix || suffix === '..' || suffix.startsWith('..' + sep) || isAbsolute(suffix))
    fail('Artifact must remain below RUNNER_TEMP.');
  let current = root;
  if (lstatSync(current).isSymbolicLink()) fail('RUNNER_TEMP must not be a link.');
  for (const part of suffix.split(sep)) {
    current = join(current, part);
    if (lstatSync(current).isSymbolicLink()) fail('Artifact paths must not contain links.');
  }
  return absolute;
}

export function renderSteamVdf(moduleKey, workshopId, content, changeNote) {
  const entry = registry.find(item => item.key === moduleKey);
  if (!entry || entry.paused || !entry.reviewedSource || entry.workshopId !== workshopId ||
      typeof content !== 'string' || /["\x00-\x1f\x7f]/.test(content) ||
      typeof changeNote !== 'string' || changeNote.length < 1 || changeNote.length > 240 ||
      !/^[\x20-\x21\x23-\x5b\x5d-\x7e]+$/.test(changeNote))
    fail('Steam target or change note does not match a reviewed module.');
  const escaped = content.replaceAll('\\', '\\\\');
  return `"workshopitem"\n{\n  "appid" "255710"\n  "publishedfileid" "${workshopId}"\n  "contentfolder" "${escaped}"\n  "changenote" "${changeNote}"\n}\n`;
}

export function prepareSteamCommand(executable, username, content, runId, changeNote, expected) {
  if (!isAbsolute(executable) || !/^[A-Za-z0-9_.@-]{1,128}$/.test(username) ||
      /["\r\n]/.test(content) || !Number.isSafeInteger(runId) || runId <= 0 ||
      typeof changeNote !== 'string' || changeNote.length < 1 || changeNote.length > 240 ||
      !/^[\x20-\x21\x23-\x5b\x5d-\x7e]+$/.test(changeNote))
    fail('SteamCMD configuration is invalid.');
  const exe = lstatSync(executable);
  if (!exe.isFile() || exe.isSymbolicLink()) fail('SteamCMD executable is not a regular file.');
  const text = renderSteamVdf(expected?.module, expected?.workshopId, content, changeNote);
  const vdf = join(resolve(process.env.RUNNER_TEMP), `cs1-workshop-request-${runId}-${expected.module}.vdf`);
  writeFileSync(vdf, text, {encoding:'utf8', flag:'wx'});
  return {executable, username, vdf};
}

export function steamProcessEnvironment(source) {
  const childEnv = {};
  for (const key of ['SystemRoot','WINDIR','TEMP','TMP','PATH','USERPROFILE','LOCALAPPDATA','APPDATA','ProgramFiles','ProgramFiles(x86)','ProgramData']) {
    if (typeof source[key] === 'string') childEnv[key] = source[key];
  }
  return childEnv;
}

export function steamCmdSubmission({executable, username, vdf}) {
  const childEnv = steamProcessEnvironment(process.env);
  return new Promise((resolvePromise, rejectPromise) => {
    let child;
    try {
      child = spawn(executable, ['+login',username,'+workshop_build_item',vdf,'+quit'],
        {cwd:dirname(executable), stdio:'ignore', windowsHide:true, shell:false, env:childEnv});
    } catch { rejectPromise(new Error('SteamCMD could not be started.')); return; }
    child.once('error', () => rejectPromise(new Error('SteamCMD could not be started.')));
    child.once('exit', code => code === 0 ? resolvePromise() : rejectPromise(new Error('SteamCMD did not exit cleanly.')));
  });
}

async function main() {
  const [contentArg, manifestArg] = process.argv.slice(2);
  if (!contentArg || !manifestArg || process.argv.length !== 4 ||
      process.env.GITHUB_ACTIONS !== 'true' || process.env.RUNNER_ENVIRONMENT !== 'self-hosted' ||
      process.env.WORKSHOP_RELEASE_UPLOAD_ENABLED !== 'true')
    fail('Steam upload is disabled or is not running on the approved self-hosted runner.');
  const content = requireTempPath(contentArg);
  const manifest = requireTempPath(manifestArg);
  const snapshot = JSON.parse(Buffer.from(process.env.WORKSHOP_SNAPSHOT_BASE64 || '', 'base64').toString('utf8'));
  const expected = expectedFromEnvironment();
  const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
  const api = new GitHubClient(process.env.GH_TOKEN || '');
  const revalidate = () => recheck(api, event, snapshot, Number(process.env.GITHUB_RUN_ID),
    process.env.GITHUB_SHA, Number(process.env.GITHUB_RUN_ATTEMPT));
  const result = await submitAfterChecks({
    requireReady:() => requireReady(snapshot),
    recheck:revalidate,
    verifyPackage:async () => verifyPackageFromDisk(content, manifest, expected),
    steamPreflight:() => queryRegisteredItem(expected.module),
    prepare:() => prepareSteamCommand(process.env.STEAMCMD_EXE || '', process.env.STEAMCMD_USERNAME || '',
      content, Number(process.env.GITHUB_RUN_ID), parseChangeNote(event.issue.body), expected),
    submit:steamCmdSubmission,
  });
  if (result.status !== 'unknown') fail('Unexpected Steam submission status.');
  process.stdout.write('Steam submission outcome is unknown. Check the item and Steam build log; do not retry this issue.\n');
}

// Additional validation: OpenAI-Codex.

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => { process.stderr.write(`Workshop upload stopped: ${error.message}\n`); process.exitCode = 1; });
}
