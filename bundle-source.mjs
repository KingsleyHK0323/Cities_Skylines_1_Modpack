// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import { createHash } from 'node:crypto';
import { safeRelative, verifyPackage } from './core.mjs';
import { discoverModuleReleases } from './release-index.mjs';
import { allowGitHubAssetRedirect } from './https-source.mjs';
import { allowedInstallationAsset } from './asset-policy.mjs';

const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const require = condition => { if (!condition) throw new Error('BUNDLE_UNAVAILABLE_OR_INVALID'); };
const positive = value => Number.isSafeInteger(value) && value > 0;
export function verifyBundleEntries(entries, registry, identity) {
  require(Array.isArray(entries) && entries.length > 0 && entries.length <= 512);
  const files = new Map(); const names = new Set(); let total = 0;
  for (const entry of entries) {
    safeRelative(entry.path); require(!names.has(entry.path.toLowerCase())); names.add(entry.path.toLowerCase());
    const bytes = Buffer.isBuffer(entry.bytes) ? Buffer.from(entry.bytes) : Buffer.from(entry.bytes, 'base64');
    require(bytes.length > 0 && bytes.length <= 32 * 1024 * 1024); total += bytes.length; require(total <= 64 * 1024 * 1024);
    files.set(entry.path, bytes);
  }
  for (const name of names) { const parts=name.split('/'); for(let i=1;i<parts.length;i++) require(!names.has(parts.slice(0,i).join('/'))); }
  require(files.has('manifest.json') && files.has('SHA256SUMS') && files.get('manifest.json').length <= 1048576 && files.get('SHA256SUMS').length <= 262144);
  const checks = new Map();
  for (const line of files.get('SHA256SUMS').toString('utf8').replace(/^\uFEFF/,'').trimEnd().split(/\r?\n/)) {
    const match=/^([a-fA-F0-9]{64})  (.+)$/.exec(line); require(match && !checks.has(match[2])); safeRelative(match[2]); checks.set(match[2],match[1].toLowerCase());
  }
  require(checks.size === files.size - 1);
  for(const [name,bytes] of files) if(name!=='SHA256SUMS') require(checks.get(name)===hash(bytes));
  const manifest=JSON.parse(files.get('manifest.json').toString('utf8').replace(/^\uFEFF/,''));
  require(/^[a-f0-9]{64}$/.test(manifest.sourceIdentity) && manifest.sourceIdentity===identity && Array.isArray(manifest.modules) && manifest.modules.length<=64);
  const branches=new Set();
  for(const record of manifest.modules) { require(typeof record.branch==='string' && !branches.has(record.branch) && /^[a-f0-9]{40}$/.test(record.commit) && positive(record.runId) && positive(record.attempt)); branches.add(record.branch); }
  return registry.map(entry=>{
    try {
      const record=manifest.modules.find(module=>module.branch===entry.branch); require(record && (record.moduleId===undefined || record.moduleId===entry.moduleId));
      const mappings=record.runtimeFiles ?? (entry.bundleRuntimePaths ? Object.entries(entry.bundleRuntimePaths).map(([name,path])=>({name,path,assemblyName:name.endsWith('.dll')?name.slice(0,-4):null})) : null);
      require(Array.isArray(mappings) && mappings.length>0 && mappings.length<=256);
      if(entry.bundleRuntimePaths)require(Object.keys(entry.bundleRuntimePaths).every(name=>mappings.some(mapping=>mapping.name===name)));
      const assembly=record.assembly ?? entry.assembly; require(typeof assembly==='string' && (!entry.assembly || entry.assembly===assembly));
      let devTools=record.devTools===true;
      const provenancePath=record.provenancePath ?? entry.bundleProvenancePath;
      if(provenancePath) {
        const raw=files.get(safeRelative(provenancePath)); require(raw && raw.length<=1048576);
        const text=raw.toString('utf8').replace(/^\uFEFF/,'');
        const info=provenancePath.endsWith('.json') ? JSON.parse(text) : Object.fromEntries(text.split(/\r?\n/).filter(line=>line.includes('=')).map(line=>{const i=line.indexOf('=');return [line.slice(0,i),line.slice(i+1)];}));
        require((info.sourceSHA ?? info.commit)===record.commit && String(info.runId ?? info.runid)===String(record.runId) && String(info.runAttempt ?? info.runattempt ?? info.attempt)===String(record.attempt));
        devTools=info.DevTools===true || info.DevTools==='true';
        if(info.repository!==undefined) require(info.repository===entry.repository);
        if(info.ref!==undefined) require(info.ref===`refs/heads/${entry.branch}`);
      }
      require(devTools);
      const pkg={moduleId:entry.moduleId,buildId:`${entry.branch}-dev-${record.runId}-${record.attempt}`,files:[]}; const expectedAssemblies={};
      for(const mapping of mappings) {
        safeRelative(mapping.name); safeRelative(mapping.path);
        require(allowedInstallationAsset(mapping.name,mapping.assemblyName) && files.has(mapping.path) && !Object.hasOwn(expectedAssemblies,mapping.name));
        Object.defineProperty(expectedAssemblies,mapping.name,{value:mapping.assemblyName,enumerable:true});
        const bytes=files.get(mapping.path); pkg.files.push({path:mapping.name,bytes:Buffer.from(bytes),sha256:hash(bytes)});
      }
      require(expectedAssemblies[`${assembly}.dll`]===assembly); verifyPackage(pkg);
      const workflowPath=record.workflowPath ?? entry.workflowPath; safeRelative(workflowPath); require(workflowPath.startsWith('.github/workflows/') && /\.ya?ml$/.test(workflowPath));
      return { moduleId:entry.moduleId,policy:{...entry,assembly,workflowPath,runtimeAssetNames:mappings.map(m=>m.name),expectedAssemblies,policyOrigin:'bundle-manifest'},
        package:pkg,record,developmentEvidence:true };
    } catch { return {moduleId:entry.moduleId,reason:'This module has no verified runtime mapping and DevTools provenance in the bundle.'}; }
  });
}

async function receiveZip(asset, transport) {
  const controller=new AbortController(); let timer;
  try {
    const work=async()=>{
      let url=asset.browser_download_url;
      for(let hops=0;hops<=3;hops++) {
        const response=await transport({url,method:'GET',redirect:'manual',credentials:'omit',signal:controller.signal});
        require(response.url===url && response.redirected===false);
        if([301,302,303,307,308].includes(response.status)) {const next=response.headers.location;require(allowGitHubAssetRedirect(url,next));url=next;continue;}
        require(response.status===200 && (!response.headers['content-encoding'] || response.headers['content-encoding']==='identity'));
        if(response.headers['content-length']!==undefined) require(Number(response.headers['content-length'])===asset.size);
        const chunks=[];let size=0;let count=0;const digest=createHash('sha256');
        for await(const chunk of response.body) { require(chunk instanceof Uint8Array && ++count<=65536 && size+chunk.byteLength<=asset.size && !controller.signal.aborted);const copy=Buffer.from(chunk);size+=copy.length;digest.update(copy);chunks.push(copy); }
        require(size===asset.size && `sha256:${digest.digest('hex')}`===asset.digest);return Buffer.concat(chunks,size);
      }
      throw new Error('BUNDLE_REDIRECT_LIMIT');
    };
    return await Promise.race([work(),new Promise((_,reject)=>{timer=setTimeout(()=>{controller.abort();reject(new Error('BUNDLE_TIMEOUT'));},30000);})]);
  } finally {clearTimeout(timer);controller.abort();}
}

export class BundleSource {
  constructor({registry,request,transport,inspectArchive,now=Date.now}) {Object.assign(this,{registry,request,transport,inspectArchive,now});this.cached=null;}
  async load() {
    const repository=this.registry[0].repository;
    const [discovery]=await discoverModuleReleases([{moduleId:'bundle',repository,releaseTagPrefix:'modpack-dev-'}],this.request,this.now());
    require(discovery.discoveryCandidate);
    const base=`https://api.github.com/repos/${repository}`;
    const get=async url=>{const response=await this.request({url,method:'GET',redirect:'manual',credentials:'omit',cachePhase:'bundle',signal:AbortSignal.timeout(15000)});require(response.status===200 && response.url===url && !response.redirected);return response;};
    const release=(await get(`${base}/releases/${discovery.discoveryCandidate.releaseId}`)).body;
    require(release.id===discovery.discoveryCandidate.releaseId && release.draft===false && release.published_at===discovery.discoveryCandidate.publishedAt && release.tag_name===discovery.discoveryCandidate.buildId && /^modpack-dev-[a-f0-9]{64}$/.test(release.tag_name));
    const assets=await get(`${base}/releases/${release.id}/assets?per_page=100&page=1`);
    require(Array.isArray(assets.body) && assets.body.length===1 && !assets.headers.link?.includes('rel="next"'));
    const asset=assets.body[0]; require(positive(asset.id) && positive(asset.size) && asset.size<=64*1024*1024 && /^sha256:[a-f0-9]{64}$/.test(asset.digest) && asset.state==='uploaded');
    require(['Cities Skylines_1_Modpack.zip','Cities.Skylines_1_Modpack.zip'].includes(asset.name) &&
      asset.url===`${base}/releases/assets/${asset.id}` && asset.browser_download_url===`https://github.com/${repository}/releases/download/${encodeURIComponent(release.tag_name)}/${encodeURIComponent(asset.name)}`);
    const key=`${release.id}:${asset.id}:${asset.digest}`;
    if(this.cached?.key===key) return this.cached;
    const bytes=await receiveZip(asset,this.transport); const entries=await this.inspectArchive(bytes);
    const modules=verifyBundleEntries(entries,this.registry,release.tag_name.slice('modpack-dev-'.length));
    this.cached={key,releaseId:release.id,modules,publishedAt:release.published_at};return this.cached;
  }
}

export async function verifyBundleRun(module, request) {
  const {policy,record}=module;
  const url=`https://api.github.com/repos/${policy.repository}/actions/runs/${record.runId}/attempts/${record.attempt}`;
  const response=await request({url,method:'GET',redirect:'manual',credentials:'omit',cachePhase:'bundle-run',signal:AbortSignal.timeout(15000)});const run=response.body;
  require(response.status===200 && response.url===url && response.redirected===false && run.id===record.runId && run.run_attempt===record.attempt && run.head_branch===policy.branch && run.head_sha===record.commit &&
    run.repository?.full_name===policy.repository && run.head_repository?.full_name===policy.repository && run.status==='completed' && run.conclusion==='success' &&
    ['push','workflow_dispatch'].includes(run.event) && [policy.workflowPath,`${policy.workflowPath}@${policy.branch}`].includes(run.path));
  return run;
}
