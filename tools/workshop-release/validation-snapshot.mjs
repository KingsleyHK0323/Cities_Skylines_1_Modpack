// Signature: OpenAI-Codex.
import {appendFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {fileURLToPath} from 'node:url';
import {registry} from './request.mjs';
import {createBuildPlan} from './batch.mjs';

function createSnapshotFor(keys) {
  const modules = keys.map(key => {
    const item = registry.find(entry => entry.key === key);
    if (!item || item.paused || !item.reviewedSource) throw new Error('Validation module is unavailable.');
    return {key:item.key,branch:item.branch,assembly:item.assembly,
      workshopId:item.workshopId,sourceSha:item.reviewedSource};
  });
  const snapshot = {schema:1,repository:'KingsleyHK0323/Cities_Skylines_1_Modpack',
    issueNumber:1, validationOnly:true, modules};
  createBuildPlan(snapshot);
  return snapshot;
}

export function createValidationSnapshot() {
  return createSnapshotFor(['AI_Improve','Wireless_Utilities_And_Global_Coverage']);
}

export function createAptpValidationSnapshot() {
  return createSnapshotFor(['Automated_Public_Transport_Planning']);
}

function main() {
  const args = process.argv.slice(2);
  if (!(args.length === 0 || (args.length === 1 && args[0] === '--aptp')) || process.env.GITHUB_ACTIONS !== 'true' ||
      process.env.RUNNER_ENVIRONMENT !== 'github-hosted' ||
      process.env.GITHUB_REPOSITORY !== 'KingsleyHK0323/Cities_Skylines_1_Modpack' ||
      process.env.GITHUB_ACTOR !== 'KingsleyHK0323' || process.env.GITHUB_RUN_ATTEMPT !== '1' ||
      !process.env.GITHUB_ENV) throw new Error('Only the owner cloud validation can create this fixture.');
  const snapshot = args.length === 0 ? createValidationSnapshot() : createAptpValidationSnapshot();
  const encoded = Buffer.from(JSON.stringify(snapshot)).toString('base64');
  appendFileSync(process.env.GITHUB_ENV, `WORKSHOP_SNAPSHOT_BASE64=${encoded}\n`, 'utf8');
  process.stdout.write('Prepared a synthetic validation fixture; this is not an Issue approval.\n');
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { main(); }
  catch (error) { process.stderr.write(`Validation fixture rejected: ${error.message}\n`); process.exitCode = 1; }
}
