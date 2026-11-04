// Signature: OpenAI-Codex.
import {readFileSync, appendFileSync} from 'node:fs';
import {resolve, join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {registry, requireReady, parseChangeNote} from './request.mjs';
import {createBuildPlan} from './batch.mjs';
import {GitHubClient} from './github.mjs';
import {recheck} from './control.mjs';
import {expectedFromEnvironment, verifyPackageFromDisk} from './package.mjs';
import {queryRegisteredItem} from './steam.mjs';
import {requireTempPath, prepareSteamCommand, steamCmdSubmission, submitAfterChecks} from './upload.mjs';

const fail = message => { throw new Error(message); };

// Unknown is the only currently supported submission outcome. It stops the
// batch because SteamCMD exit status alone does not prove the item was updated.
export async function uploadSelected({modules, requireReady:checkReady, recheck:checkApproval, verify, preflight, submit}) {
  if (!Array.isArray(modules) || modules.length < 1 || modules.length > 15 ||
      new Set(modules).size !== modules.length ||
      modules.some(key => !registry.some(item => item.key === key && !item.paused)) ||
      [checkReady,checkApproval,verify,preflight,submit].some(fn => typeof fn !== 'function'))
    fail('Invalid batch or missing safety checks.');
  if (await checkReady() !== true || await checkApproval() !== true)
    fail('The batch is not ready or approval is stale.');
  const identities = new Map();
  for (const module of modules) {
    const identity = await verify(module);
    if (typeof identity !== 'string' || !/^[0-9a-f]{64}$/.test(identity)) fail('A package identity is invalid.');
    if (await preflight(module) !== true) fail('A Steam target could not be verified.');
    identities.set(module, identity);
  }
  const results = [];
  for (let i = 0; i < modules.length; i++) {
    const module = modules[i];
    if (await checkApproval() !== true || await verify(module) !== identities.get(module))
      fail('Approval or package changed after batch preflight.');
    let outcome;
    try { outcome = await submit(module); } catch { outcome = {status:'unknown'}; }
    if (outcome?.status !== 'unknown') fail('A Steam submission result has not been independently verified.');
    results.push({module,status:'unknown'});
    for (const remaining of modules.slice(i + 1)) results.push({module:remaining,status:'not-attempted'});
    break;
  }
  return results;
}

async function main() {
  const [packageArg] = process.argv.slice(2);
  if (!packageArg || process.argv.length !== 3 || process.env.GITHUB_ACTIONS !== 'true' ||
      process.env.RUNNER_ENVIRONMENT !== 'self-hosted' || process.env.WORKSHOP_RELEASE_UPLOAD_ENABLED !== 'true')
    fail('Batch upload is disabled or the runner is not approved.');
  const root = requireTempPath(packageArg);
  const snapshot = JSON.parse(Buffer.from(process.env.WORKSHOP_SNAPSHOT_BASE64 || '', 'base64').toString('utf8'));
  const modules = createBuildPlan(snapshot).map(item => item.module);
  const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
  const api = new GitHubClient(process.env.GH_TOKEN || '');
  const runId = Number(process.env.GITHUB_RUN_ID);
  const checkApproval = () => recheck(api, event, snapshot, runId,
    process.env.GITHUB_SHA, Number(process.env.GITHUB_RUN_ATTEMPT));
  const select = key => {
    process.env.WORKSHOP_MODULE_KEY = key;
    return {expected:expectedFromEnvironment(),
      content:requireTempPath(join(root, key, 'content')),
      manifest:requireTempPath(join(root, key, 'manifest.json'))};
  };
  const verify = key => {
    const item = select(key);
    return verifyPackageFromDisk(item.content, item.manifest, item.expected);
  };
  const preflight = key => queryRegisteredItem(key);
  const results = await uploadSelected({modules, requireReady:() => requireReady(snapshot),
    recheck:checkApproval, verify, preflight,
    submit:async key => {
      const item = select(key);
      return submitAfterChecks({requireReady:() => requireReady(snapshot), recheck:checkApproval,
        verifyPackage:() => verify(key), steamPreflight:() => preflight(key),
        prepare:() => prepareSteamCommand(process.env.STEAMCMD_EXE || '', process.env.STEAMCMD_USERNAME || '',
          item.content, runId, parseChangeNote(event.issue.body), item.expected), submit:steamCmdSubmission});
    }});
  if (typeof process.env.GITHUB_OUTPUT !== 'string' || !process.env.GITHUB_OUTPUT) fail('Result output is unavailable.');
  appendFileSync(process.env.GITHUB_OUTPUT, `results=${Buffer.from(JSON.stringify(results)).toString('base64')}\n`, 'utf8');
  process.stdout.write('Steam outcome is unverified; remaining modules were not attempted. Do not retry this issue.\n');
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => {process.stderr.write(`Workshop batch stopped: ${error.message}\n`); process.exitCode = 1;});
}
