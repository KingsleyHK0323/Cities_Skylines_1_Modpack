// Signature: OpenAI-Codex_workshop_implementation
import {verifySteamItem, registry} from './request.mjs';
const endpoint = 'https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/';
const fail = message => { throw new Error(message); };

export async function queryRegisteredItem(moduleKey, transport = fetch, ownerId = process.env.STEAM_WORKSHOP_OWNER_ID) {
  const entry = registry.find(item => item.key === moduleKey);
  if (!entry || entry.paused || !entry.reviewedSource || typeof transport !== 'function' ||
      typeof ownerId !== 'string' || !/^[1-9][0-9]{0,19}$/.test(ownerId))
    fail('The selected module has no reviewed Steam preflight.');
  const expected = {id:entry.workshopId, creator:ownerId, appId:255710};
  const params = new URLSearchParams({'itemcount':'1', 'publishedfileids[0]':expected.id});
  const response = await transport(endpoint, {method:'POST', redirect:'error',
    signal:AbortSignal.timeout(15000), headers:{'Content-Type':'application/x-www-form-urlencoded',
      'Accept':'application/json','Cache-Control':'no-cache, no-store'}, body:params.toString()});
  if (!response.ok) fail(`Steam item preflight failed (HTTP ${response.status}).`);
  const age = response.headers.get('age');
  if (age !== null && (!/^\d+$/.test(age) || Number(age) > 0)) fail('Steam returned a cached item response.');
  const declared = response.headers.get('content-length');
  if (declared !== null && (!/^\d+$/.test(declared) || Number(declared) > 262144))
    fail('Steam item response is too large.');
  if (!response.body) fail('Steam item response is missing.');
  const chunks = []; let length = 0;
  for await (const chunk of response.body) {
    length += chunk.length;
    if (length > 262144) fail('Steam item response is too large.');
    chunks.push(Buffer.from(chunk));
  }
  let data;
  try { data = JSON.parse(Buffer.concat(chunks).toString('utf8')); }
  catch { fail('Steam item response is invalid.'); }
  if (data?.response?.result !== 1 || data.response.resultcount !== 1 ||
      !Array.isArray(data.response.publishedfiledetails) || data.response.publishedfiledetails.length !== 1)
    fail('Steam did not provide exactly one verifiable item.');
  return verifySteamItem(expected, data.response.publishedfiledetails[0]);
}

// Private owner configuration: OpenAI-Codex.
