// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Different opaque IDs do not establish a safe forward update.
export function isForwardComparisonBody(body, base, head) {
  const sha = value => typeof value === 'string' && /^[a-f0-9]{40}$/.test(value);
  return sha(base) && sha(head) && !!body && typeof body === 'object' && !Array.isArray(body) &&
    body.status === 'ahead' && body.base_commit?.sha === base &&
    (!Object.hasOwn(body, 'head_commit') || body.head_commit?.sha === head) && body.behind_by === 0 &&
    Number.isSafeInteger(body.ahead_by) && body.ahead_by > 0;
}
export async function assessUpdateOrder(installed, candidate, repository, request) {
  if (!installed || installed.buildId === candidate.buildId) return {allowed:false,reason:'No different installed build requires updating.'};
  const prior=installed.publication;
  if(prior?.provider==='artifact-branch'){
    if(candidate.artifactBranch!==prior.artifactBranch || !/^[a-f0-9]{40}$/.test(candidate.artifactCommit) || candidate.artifactCommit===prior.artifactCommit)
      return {allowed:false,reason:'Artifact publication has not advanced on the same configured branch.'};
    try{
      const url=`https://api.github.com/repos/${repository}/compare/${prior.artifactCommit}...${candidate.artifactCommit}`;
      const response=await request({url,method:'GET',redirect:'manual',credentials:'omit',cachePhase:'update-order',signal:AbortSignal.timeout(15000)});const comparison=response.body;
      if(response.status!==200||response.url!==url||response.redirected!==false||!isForwardComparisonBody(comparison,prior.artifactCommit,candidate.artifactCommit))throw new Error();
    }catch{return {allowed:false,reason:'Artifact branch ancestry is unknown, older or divergent.'};}
  }
  if (!prior || !/^[a-f0-9]{40}$/.test(prior.sourceSha) || !/^[a-f0-9]{40}$/.test(candidate.sourceSha) ||
    !Number.isFinite(Date.parse(candidate.publishedAt)) || Date.parse(candidate.publishedAt)<=Date.parse(prior.publishedAt))
    return {allowed:false,reason:'Published build differs, but a newer forward update is not established. Older and legacy records are not auto-selected.'};
  if(prior.sourceSha===candidate.sourceSha) {
    if(candidate.runId===prior.runId) return {allowed:Number.isSafeInteger(candidate.attempt)&&candidate.attempt>prior.attempt,
      reason:'Same source/run: only a later verified run attempt is a forward update.'};
    try {
      let started=candidate.buildStartedAt;
      if(!started && Number.isSafeInteger(candidate.runId)&&Number.isSafeInteger(candidate.attempt)) {
        const url=`https://api.github.com/repos/${repository}/actions/runs/${candidate.runId}/attempts/${candidate.attempt}`;
        const response=await request({url,method:'GET',redirect:'manual',credentials:'omit',cachePhase:'update-order',signal:AbortSignal.timeout(15000)});const run=response.body;
        if(response.status===200 && response.url===url && !response.redirected && run.id===candidate.runId && run.run_attempt===candidate.attempt &&
          run.head_sha===candidate.sourceSha && run.head_branch===candidate.branch && run.repository?.full_name===repository && run.status==='completed' && run.conclusion==='success') started=run.run_started_at;
      }
      if(Number.isFinite(Date.parse(prior.buildStartedAt)) && Number.isFinite(Date.parse(started)) && Date.parse(started)>Date.parse(prior.buildStartedAt))
        return {allowed:true,reason:'A later publication with a later build of the same source is available.'};
    }catch{}
    return {allowed:false,reason:'Same source but build order is not confirmed newer; an older republished build is not auto-selected.'};
  }
  try {
    const url=`https://api.github.com/repos/${repository}/compare/${prior.sourceSha}...${candidate.sourceSha}`;
    const response=await request({url,method:'GET',redirect:'manual',credentials:'omit',cachePhase:'update-order',signal:AbortSignal.timeout(15000)});
    const comparison=response.body;
    if(response.status===200 && response.url===url && response.redirected===false &&
      isForwardComparisonBody(comparison,prior.sourceSha,candidate.sourceSha))
      return {allowed:true,reason:'A later publication with verified forward source ancestry is available.'};
  } catch {}
  return {allowed:false,reason:'Published build differs; source ancestry is unknown, older or divergent. It is not auto-selected for update.'};
}
