// Signature: OpenAI-Codex_workshop_implementation
import {readFileSync, appendFileSync} from 'node:fs';
import {resolve} from 'node:path';
import {fileURLToPath} from 'node:url';
import {GitHubClient} from './github.mjs';
import {registry, requireBuildable} from './request.mjs';
import {previewRequest, reviewedSnapshot} from './orchestration.mjs';
import {claimOnce, verifyClaim} from './ledger.mjs';

const root = '/repos/KingsleyHK0323/Cities_Skylines_1_Modpack';
const validJobStatus = new Set(['success','failure','cancelled','skipped']);
const fail = message => { throw new Error(message); };

export function formatReport(snapshot, statuses, results) {
  if (!snapshot || !Array.isArray(snapshot.modules) || snapshot.modules.length === 0 ||
      snapshot.modules.some(item => !registry.some(entry => entry.key === item?.key)) ||
      !statuses || Object.values(statuses).some(status => !validJobStatus.has(status))) fail('Invalid report data.');
  if (results !== undefined && (!Array.isArray(results) || results.length !== snapshot.modules.length ||
      statuses.build !== 'success' || statuses.upload !== 'success' ||
      results.some((item, index) => item?.module !== snapshot.modules[index].key ||
        item.status !== (index === 0 ? 'unknown' : 'not-attempted'))))
    fail('The batch result cannot be verified.');
  let outcome;
  if (statuses.authorization !== 'success' || statuses.build !== 'success' || statuses.upload === 'skipped') {
    outcome = 'Not submitted to Steam by this workflow.';
  } else {
    outcome = 'Steam submission outcome is unknown until the item and Steam build log are independently checked. Do not retry this issue.';
  }
  return ['## Workshop release request result', '',
    ...snapshot.modules.map((item, index) => {
      const reason = statuses.build === 'skipped'
        ? ' The batch build did not run; check approval and recipe status.' : '';
      const itemOutcome = results?.[index]?.status === 'not-attempted'
        ? 'Not submitted to Steam by this workflow.' : outcome;
      return `- ${item.key}: ${itemOutcome}${reason}`;
    }), '',
    'A Steam update does not establish game compatibility or stability.', ''].join('\n');
}

// Batch result clarification: OpenAI-Codex.

function getSnapshot() {
  const encoded = process.env.WORKSHOP_SNAPSHOT_BASE64;
  if (typeof encoded !== 'string' || encoded.length < 1 || encoded.length > 65536 ||
      !/^[A-Za-z0-9+/=]+$/.test(encoded)) fail('Approved snapshot is missing.');
  return JSON.parse(Buffer.from(encoded, 'base64').toString('utf8'));
}

export async function recheck(api, event, snapshot, runId, controllerSha, attempt) {
  const current = await reviewedSnapshot(api, event, attempt);
  if (JSON.stringify(current) !== JSON.stringify(snapshot)) fail('Approval snapshot is no longer current.');
  await verifyClaim(api, snapshot, runId, controllerSha);
  return true;
}

async function main() {
  const mode = process.argv[2];
  if (!['preview','authorize','recheck','report'].includes(mode) || process.argv.length !== 3)
    fail('Expected a fixed workflow control mode.');
  const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
  const api = new GitHubClient(process.env.GH_TOKEN || '');
  const runId = Number(process.env.GITHUB_RUN_ID);
  const attempt = Number(process.env.GITHUB_RUN_ATTEMPT);
  const controllerSha = process.env.GITHUB_SHA;
  if (mode === 'preview') {
    await previewRequest(api, event);
    process.stdout.write('A source review preview was recorded. No upload was authorized.\n');
  } else if (mode === 'authorize') {
    const snapshot = await reviewedSnapshot(api, event, attempt);
    const encoded = Buffer.from(JSON.stringify(snapshot), 'utf8').toString('base64');
    appendFileSync(process.env.GITHUB_OUTPUT, `snapshot=${encoded}\n`, 'utf8');
    try { requireBuildable(snapshot); }
    catch (error) {
      appendFileSync(process.env.GITHUB_OUTPUT, 'buildable=false\n', 'utf8');
      process.stdout.write(`No release build will run: ${error.message}\n`);
      return;
    }
    await claimOnce(api, snapshot, runId, controllerSha);
    appendFileSync(process.env.GITHUB_OUTPUT, 'buildable=true\n', 'utf8');
    process.stdout.write('The reviewed issue was durably claimed for this run.\n');
  } else if (mode === 'recheck') {
    await recheck(api, event, getSnapshot(), runId, controllerSha, attempt);
    process.stdout.write('The issue, source and durable claim still match.\n');
  } else {
    const snapshot = getSnapshot();
    if (event?.issue?.number !== snapshot.issueNumber) fail('Report issue differs from the approved snapshot.');
    const statuses = {authorization:process.env.AUTH_STATUS, build:process.env.BUILD_STATUS, upload:process.env.UPLOAD_STATUS};
    let results;
    if (process.env.UPLOAD_RESULTS) {
      if (process.env.UPLOAD_RESULTS.length > 65536 || !/^[A-Za-z0-9+/=]+$/.test(process.env.UPLOAD_RESULTS))
        fail('The batch result is invalid.');
      results = JSON.parse(Buffer.from(process.env.UPLOAD_RESULTS, 'base64').toString('utf8'));
    }
    await api.request('POST', `${root}/issues/${snapshot.issueNumber}/comments`, {body:formatReport(snapshot, statuses, results)});
    process.stdout.write('A conservative result report was recorded.\n');
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  main().catch(error => { process.stderr.write(`Workshop release stopped: ${error.message}\n`); process.exitCode = 1; });
}
