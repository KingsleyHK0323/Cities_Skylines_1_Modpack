// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Publisher-controlled branch manifests, pinned to an immutable artifact commit.
import { createHash } from 'node:crypto';
import { safeRelative } from './core.mjs';
import { allowedInstallationAsset } from './asset-policy.mjs';
import { isForwardComparisonBody } from './update-order.mjs';

const sha=value=>typeof value==='string'&&/^[a-f0-9]{40}$/.test(value);
const positive=value=>Number.isSafeInteger(value)&&value>0;
const require=condition=>{if(!condition)throw new Error('ARTIFACT_SOURCE_UNVERIFIED');};
const encodePath=path=>path.split('/').map(encodeURIComponent).join('/');
export const artifactBranchFor=entry=>`Development_Build/${entry.branch}`;
export const artifactFileUrl=(repository,commit,path)=>`https://raw.githubusercontent.com/${repository}/${commit}/${encodePath(path)}`;
export function hasCompleteResponseHeaders(headers) {
  try {
    if (!headers || typeof headers !== 'object' || Array.isArray(headers)) return false;
    const prototype = Object.getPrototypeOf(headers);
    return (prototype === null || prototype === Object.prototype) &&
      Reflect.ownKeys(headers).every(key => typeof key === 'string' && key.toLowerCase() !== 'link');
  } catch { return false; }
}

export async function readArtifactRefs(repository,request,cachePhase='initial'){
  const url=`https://api.github.com/repos/${repository}/git/matching-refs/heads/`;
  const response=await request({url,method:'GET',redirect:'manual',credentials:'omit',cachePhase,signal:AbortSignal.timeout(30000)});
  require(response.status===200&&response.url===url&&response.redirected===false&&Array.isArray(response.body)&&response.body.length<=4096&&hasCompleteResponseHeaders(response.headers));
  const refs=new Map();
  for(const ref of response.body){require(typeof ref.ref==='string'&&ref.ref.startsWith('refs/heads/')&&ref.object?.type==='commit'&&sha(ref.object.sha)&&!refs.has(ref.ref));refs.set(ref.ref,ref.object.sha);}
  return refs;
}

export async function detectArtifactModule({entry,request,nowMs=Date.now(),timeoutMs=30000,snapshot,discoveryOnly=false}) {
  const controller=new AbortController();let timer;
  const unknown=()=>({status:'unknown',checkedAtMs:nowMs,developmentConfirmed:false,reason:'The module artifact branch, manifest or successful source build could not be verified.',latestPublished:{status:'unknown'}});
  try {
    const work=async()=>{
      require(/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(entry.repository)&&entry.repository.split('/').every(part=>!['.','..'].includes(part)));safeRelative(entry.branch);
      const base=`https://api.github.com/repos/${entry.repository}`;
      const get=async(url,cachePhase='initial')=>{
        const response=await request({url,method:'GET',redirect:'manual',credentials:'omit',cachePhase,signal:controller.signal});
        require(!controller.signal.aborted&&response.status===200&&response.url===url&&response.redirected===false);return response.body;
      };
      const artifactBranch=artifactBranchFor(entry);const refUrl=`${base}/git/ref/heads/${encodePath(artifactBranch)}`;
      const readRef=async(url,ref,phase)=>{if(snapshot){const commit=snapshot.get(ref);require(sha(commit));return commit;}const value=await get(url,phase);require(value.ref===ref&&value.object?.type==='commit'&&sha(value.object.sha));return value.object.sha;};
      const artifactCommit=await readRef(refUrl,`refs/heads/${artifactBranch}`);
      const file=await get(`${base}/contents/manifest.json?ref=${artifactCommit}`);
      require(file.type==='file'&&file.encoding==='base64'&&positive(file.size)&&file.size<=262144&&typeof file.content==='string');
      const bytes=Buffer.from(file.content,'base64');require(bytes.length===file.size&&createHash('sha1').update(`blob ${bytes.length}\0`).update(bytes).digest('hex')===file.sha);
      const manifest=JSON.parse(bytes.toString('utf8'));
      require(manifest.schema===1&&manifest.sourceKind==='artifact-branch'&&manifest.moduleId===entry.moduleId&&manifest.repository===entry.repository&&
        manifest.sourceBranch===entry.branch&&manifest.artifactBranch===artifactBranch&&sha(manifest.sourceSha)&&manifest.devTools===true&&
        positive(manifest.runId)&&positive(manifest.attempt)&&manifest.buildId===`${entry.branch}-dev-${manifest.runId}-${manifest.attempt}`);
      safeRelative(manifest.assembly);require(!manifest.assembly.includes('/')&&(!entry.assembly||entry.assembly===manifest.assembly));
      safeRelative(manifest.workflowPath);require(manifest.workflowPath.startsWith('.github/workflows/')&&/\.ya?ml$/.test(manifest.workflowPath)&&(!entry.workflowPath||entry.workflowPath===manifest.workflowPath));
      require(Array.isArray(manifest.files)&&manifest.files.length>0&&manifest.files.length<=256);
      const names=new Set();const paths=new Set();const expectedAssemblies={};let total=0;
      for(const asset of manifest.files){
        safeRelative(asset.path);safeRelative(asset.name);
        require(!paths.has(asset.path.toLowerCase())&&!names.has(asset.name.toLowerCase())&&allowedInstallationAsset(asset.name,asset.assemblyName)&&
          positive(asset.size)&&asset.size<=32*1024*1024&&/^[a-f0-9]{64}$/.test(asset.sha256));
        paths.add(asset.path.toLowerCase());names.add(asset.name.toLowerCase());total+=asset.size;require(total<=64*1024*1024);
        Object.defineProperty(expectedAssemblies,asset.name,{value:asset.assemblyName,enumerable:true});
      }
      for(const path of paths){const parts=path.split('/');for(let i=1;i<parts.length;i++)require(!paths.has(parts.slice(0,i).join('/')));}
      require(expectedAssemblies[`${manifest.assembly}.dll`]===manifest.assembly);
      if(entry.runtimeAssetNames)require(entry.runtimeAssetNames.every(name=>manifest.files.some(asset=>asset.name===name)));
      const policy={...entry,assembly:manifest.assembly,workflowPath:manifest.workflowPath,runtimeAssetNames:manifest.files.map(f=>f.name),expectedAssemblies,policyOrigin:'artifact-manifest'};
      const candidate={status:'candidate',moduleId:entry.moduleId,buildId:manifest.buildId,sourceSha:manifest.sourceSha,branch:entry.branch,
        artifactBranch,artifactCommit,manifestSha256:createHash('sha256').update(bytes).digest('hex'),runId:manifest.runId,attempt:manifest.attempt,
        assets:manifest.files.map(file=>({...file,url:artifactFileUrl(entry.repository,artifactCommit,file.path)}))};
      if(discoveryOnly)return {status:'candidate / verification required',checkedAtMs:nowMs,developmentConfirmed:false,policy,latestPublished:candidate,
        reason:'A pinned artifact manifest exists. Successful source run and update order still require verification before installation.'};
      const sourceRefUrl=`${base}/git/ref/heads/${encodePath(entry.branch)}`;
      const sourceHead=await readRef(sourceRefUrl,`refs/heads/${entry.branch}`);
      if(sourceHead!==manifest.sourceSha){const comparison=await get(`${base}/compare/${manifest.sourceSha}...${sourceHead}`);
        require(isForwardComparisonBody(comparison,manifest.sourceSha,sourceHead));}
      const run=await get(`${base}/actions/runs/${manifest.runId}/attempts/${manifest.attempt}`);
      require(run.id===manifest.runId&&run.run_attempt===manifest.attempt&&run.head_sha===manifest.sourceSha&&run.head_branch===entry.branch&&
        run.repository?.full_name===entry.repository&&run.head_repository?.full_name===entry.repository&&run.status==='completed'&&run.conclusion==='success'&&
        ['push','workflow_dispatch'].includes(run.event)&&[manifest.workflowPath,`${manifest.workflowPath}@${entry.branch}`,`${manifest.workflowPath}@refs/heads/${entry.branch}`].includes(run.path));
      const started=run.run_started_at??run.created_at;const completed=run.updated_at;
      require(Number.isFinite(Date.parse(started))&&Date.parse(started)<=nowMs&&Number.isFinite(Date.parse(completed))&&Date.parse(completed)>=Date.parse(started)&&Date.parse(completed)<=nowMs);
      if(!snapshot)require(await readRef(refUrl,`refs/heads/${artifactBranch}`,'confirm')===artifactCommit);
      return {status:'candidate',checkedAtMs:nowMs,developmentConfirmed:true,reason:'Pinned artifact manifest and successful source run match. File hashes and assembly identities are checked before installation.',policy,
        latestPublished:{...candidate,buildStartedAt:started,publishedAt:completed}};
    };
    return await Promise.race([work(),new Promise((_,reject)=>{timer=setTimeout(()=>{controller.abort();reject(new Error('ARTIFACT_SOURCE_TIMEOUT'));},timeoutMs);})]);
  }catch{return unknown();}finally{clearTimeout(timer);controller.abort();}
}

export async function downloadArtifactFile({repository,artifactCommit,asset,transport,timeoutMs=30000}) {
  require(sha(artifactCommit)&&asset.url===artifactFileUrl(repository,artifactCommit,safeRelative(asset.path))&&positive(asset.size)&&asset.size<=32*1024*1024&&/^[a-f0-9]{64}$/.test(asset.sha256));
  const controller=new AbortController();let timer;
  try{
    const work=async()=>{
      const response=await transport({url:asset.url,method:'GET',redirect:'manual',credentials:'omit',signal:controller.signal});
      require(response.url===asset.url&&response.status===200&&response.redirected===false&&response.headers&&(!response.headers['content-encoding']||response.headers['content-encoding']==='identity'));
      if(response.headers['content-length']!==undefined)require(String(response.headers['content-length'])===String(asset.size));
      const chunks=[];let length=0;let count=0;const hash=createHash('sha256');
      for await(const chunk of response.body){require(!controller.signal.aborted&&chunk instanceof Uint8Array&&++count<=65536&&length+chunk.byteLength<=asset.size);const copy=Buffer.from(chunk);chunks.push(copy);length+=copy.length;hash.update(copy);}
      require(!controller.signal.aborted&&length===asset.size&&hash.digest('hex')===asset.sha256);
      return {path:asset.name,bytes:Buffer.concat(chunks,length),sha256:asset.sha256};
    };
    return await Promise.race([work(),new Promise((_,reject)=>{timer=setTimeout(()=>{controller.abort();reject(new Error('ARTIFACT_DOWNLOAD_TIMEOUT'));},timeoutMs);})]);
  }catch{throw new Error('ARTIFACT_FILE_UNVERIFIED');}finally{clearTimeout(timer);controller.abort();}
}
