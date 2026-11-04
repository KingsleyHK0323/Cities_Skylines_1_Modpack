// Signature: OpenAI-Codex.
import {resolve} from 'node:path';
import {fileURLToPath} from 'node:url';
import {requireBuildable} from './request.mjs';

export function createBuildPlan(snapshot) {
  if (!snapshot || snapshot.schema !== 1 ||
      snapshot.repository !== 'KingsleyHK0323/Cities_Skylines_1_Modpack' ||
      !Number.isSafeInteger(snapshot.issueNumber) || snapshot.issueNumber < 1)
    throw new Error('Invalid approved build snapshot.');
  requireBuildable(snapshot);
  return snapshot.modules.map(item => ({module:item.key, sourceSha:item.sourceSha}));
}

function main() {
  if (process.argv.length !== 2) throw new Error('No command arguments are accepted.');
  const encoded = process.env.WORKSHOP_SNAPSHOT_BASE64;
  if (typeof encoded !== 'string' || encoded.length < 1 || encoded.length > 65536 ||
      !/^[A-Za-z0-9+/=]+$/.test(encoded)) throw new Error('Approved snapshot is missing.');
  const snapshot = JSON.parse(Buffer.from(encoded, 'base64').toString('utf8'));
  process.stdout.write(JSON.stringify(createBuildPlan(snapshot)));
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { main(); }
  catch (error) { process.stderr.write(`Build plan rejected: ${error.message}\n`); process.exitCode = 1; }
}
