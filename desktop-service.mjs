// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import { createHash } from 'node:crypto';
import { detectGitHubModule } from './github-source.mjs';
import { downloadCatalogAsset } from './download.mjs';
import { verifyPackage } from './core.mjs';
import { moduleRegistry, sourceProviders, defaultSourceProvider } from './registry.mjs';
import { allowGitHubAssetRedirect } from './https-source.mjs';
import { resolveGitHubPolicy } from './module-policy.mjs';
import { discoverModuleReleases } from './release-index.mjs';
import { assessUpdateOrder } from './update-order.mjs';
import { verifyBundleRun } from './bundle-source.mjs';
import {detectArtifactModule,downloadArtifactFile,readArtifactRefs,artifactBranchFor} from './artifact-source.mjs';

export class DesktopService {
  constructor({ registry = moduleRegistry, request, assetTransport, installer, getInstaller,
    confirmDevelopment, inspectPackage, assertGameClosed, loadBundle, automaticRequestLimit=40, resolvePolicy = async module => module, targetReady = () => true, now = Date.now, progress = () => {}, beginRefresh = () => {}, rateStatus = () => ({}) }) {
    this.registry = registry; this.request = request; this.assetTransport = assetTransport;
    this.getInstaller = getInstaller ?? (() => installer); this.progress = progress;
    this.confirmDevelopment = confirmDevelopment ?? this.checkWorkflow.bind(this);
    this.inspectPackage = inspectPackage ?? (async () => false); this.assertGameClosed = assertGameClosed ?? (async () => false);
    this.beginRefresh = beginRefresh; this.rateStatus = rateStatus; this.sources = new Map(); this.busy = false;
    this.now = now;
    this.targetReady = targetReady;
    this.resolvePolicy = resolvePolicy; this.policies = new Map();
    this.loadBundle=loadBundle;this.provider=defaultSourceProvider;
    this.artifactManifestCache=new Map();this.artifactCursor=0;this.updateCursor=0;this.automaticRequestLimit=automaticRequestLimit;
  }
  async checkWorkflow(module, result) {
    try {
      if (module.policyOrigin === 'branch-manifest') {
        const atBuild = await resolveGitHubPolicy(module, this.request, { commitSha: result.latestPublished.sourceSha });
        if (atBuild.policyOrigin !== 'branch-manifest' || atBuild.workflowPath !== module.workflowPath || atBuild.assembly !== module.assembly || atBuild.provenanceAssetName!==module.provenanceAssetName ||
          JSON.stringify(atBuild.runtimeAssetNames) !== JSON.stringify(module.runtimeAssetNames)) return false;
      }
      // Bundle provenance bytes were already checked against the archive digest,
      // its complete checksum list and the exact successful source run.
      if(module.policyOrigin==='bundle-manifest')return true;
      if(!module.provenanceAssetName)return false;
      const asset=result.latestPublished.assets.find(a=>a.name===module.provenanceAssetName);
      if(!asset || asset.size>1048576)return false;
      const contract=this.catalogFor(module,result);
      const downloaded=await downloadCatalogAsset({...contract,assetPath:asset.name,transport:this.assetTransport,timeoutMs:15000,allowRedirect:allowGitHubAssetRedirect});
      const text=downloaded.bytes.toString('utf8').replace(/^\uFEFF/,'');let info;
      if(asset.name.endsWith('.json'))info=JSON.parse(text);
      else {
        info=Object.create(null);
        for(const line of text.split(/\r?\n/)) {const split=line.indexOf('=');if(split<0)continue;const key=line.slice(0,split);if(Object.hasOwn(info,key))return false;info[key]=line.slice(split+1);}
      }
      const latest=result.latestPublished;
      return info.sourceSHA===latest.sourceSha && String(info.runId ?? info.runid)===String(latest.runId) &&
        String(info.runAttempt ?? info.runattempt)===String(latest.attempt) && (info.DevTools===true || info.DevTools==='true') &&
        (info.repository===undefined || info.repository===module.repository) && (info.ref===undefined || info.ref===`refs/heads/${module.branch}`);
    } catch { return false; }
  }
  rows() {
    return this.registry.map(module => {
      let installed = null; let localError = false;
      try { installed = this.getInstaller(false)?.audit(module.moduleId).installed ?? null; } catch { localError = true; }
      const source = this.sources.get(module.moduleId);
      const engineeringPaused=module.engineeringPaused===true;
      const artifactPolicyKnown=source?.policy?.policyOrigin==='artifact-manifest'||this.policies.get(module.moduleId)?.policyOrigin==='artifact-manifest';
      const configurationPending=module.publisherConfigured===false&&!engineeringPaused&&!artifactPolicyKnown;
      const stale = source?.checkedAtMs != null && this.now() - source.checkedAtMs >= 3600000;
      const usable = source?.latestPublished?.status === 'candidate' && source.developmentConfirmed === true && !stale && !localError && this.targetReady();
      const candidate = source?.latestPublished?.buildId ?? source?.discoveryCandidate?.buildId;
      const canRequest = !!candidate && !stale && !localError && this.targetReady();
      const comparison = source?.developmentConfirmed && candidate && !stale ? installed ? installed.buildId===candidate?'current':'different-build':'not-installed' : source?.status ?? 'unknown';
      return { moduleId: module.moduleId, name: module.name, branch: module.branch,
        engineeringPaused,configurationPending,
        displayName:module.name+(engineeringPaused?' (development paused)':configurationPending?' (not configured)':''),
        configuration:engineeringPaused?'Development paused':configurationPending?'Awaiting publisher configuration':'Publisher mapping configured',
        // Signature: OpenAI-Codex. A matching discovery remains unverified.
        matchesCandidate: !!installed && !stale && candidate === installed.buildId,
        status: localError ? 'Local files require inspection' : stale ? 'stale / unknown' : comparison,
        installedBuild: installed?.buildId ?? '', latestBuild: candidate ?? '',
        sourceSha: source?.latestPublished?.sourceSha ?? source?.branchHead?.sourceSha ?? '',
        development: source?.developmentConfirmed ? 'DevTools provenance verified' : 'Unconfirmed',
        reason: localError ? 'Unknown, changed or unsafe local files are preserved.' : stale ? 'The source observation is stale. Check GitHub again before installing.' :
          source?.reason ?? source?.updateOrder?.reason ?? (source?.developmentConfirmed ? 'Published metadata checked; actual DLL bytes and assembly identity are checked before changes.' : module.unavailableReason ?? 'Not checked yet.'),
        checkedAtMs: source?.checkedAtMs ?? null, canInstall: usable && !installed,
        canUpdate: usable && !!installed && source.updateOrder?.allowed === true,
        canRequestInstall: canRequest && !installed, canRequestUpdate: canRequest && !!installed && source.updateOrder?.allowed === true,
        canUninstall: !localError && !!installed && this.targetReady() };
    });
  }
  async refresh(ids = this.registry.map(m => m.moduleId), { full = false } = {}) {
    this.beginRefresh();
    if(this.provider==='artifact-branch'){
      return this.refreshArtifactSources(ids,full);
    }
    if(this.provider==='modpack-bundle') {
      try {
        if(!this.loadBundle)throw new Error();this.progress('Checking the published modpack bundle');const bundle=await this.loadBundle();
        for(const item of bundle.modules) {
          const entry=this.registry.find(m=>m.moduleId===item.moduleId);
          if(!item.package){this.sources.set(item.moduleId,{status:'unknown',checkedAtMs:this.now(),reason:item.reason});continue;}
          const latest={status:'candidate',sourceSha:item.record.commit,runId:item.record.runId,attempt:item.record.attempt,releaseId:bundle.releaseId,publishedAt:bundle.publishedAt,buildId:item.package.buildId,branch:entry.branch};
          let installed;try{installed=this.getInstaller(false)?.audit(item.moduleId).installed;}catch{}
          const source={status:'candidate / verification required',checkedAtMs:this.now(),discoveryCandidate:latest,
            reason:'Bundle checksums and module provenance match. Source run and actual assembly bytes are verified before installation.',bundlePackage:item.package,bundleRecord:item,latestPublished:latest,developmentConfirmed:false};
          if(installed)source.updateOrder=await assessUpdateOrder(installed,latest,entry.repository,this.request);
          if(full && ids.includes(item.moduleId)) {
            const run=await verifyBundleRun(item,this.request);latest.buildStartedAt=run.run_started_at ?? run.created_at;
            source.developmentConfirmed=await this.confirmDevelopment(item.policy,{latestPublished:latest});
            if(!source.developmentConfirmed)throw new Error('BUNDLE_DEVTOOLS_UNCONFIRMED');
            if(installed)source.updateOrder=await assessUpdateOrder(installed,latest,entry.repository,this.request);
          }
          this.policies.set(item.moduleId,item.policy);this.sources.set(item.moduleId,source);
        }
      } catch {for(const id of ids)this.sources.set(id,{status:'unknown',checkedAtMs:this.now(),reason:'No published, complete and verifiable Development bundle is available. No fallback download is assumed.'});}
      return {rows:this.rows(),rate:this.rateStatus(),providers:sourceProviders};
    }
    if (!full) {
      this.progress('Checking published module candidates (shared release index)');
      const discoveries = await discoverModuleReleases(this.registry, this.request, this.now());
      for (const source of discoveries) if (ids.includes(source.moduleId)) {
        const module=this.registry.find(m=>m.moduleId===source.moduleId); let installed;
        try{installed=this.getInstaller(false)?.audit(source.moduleId).installed;}catch{}
        if(installed && source.discoveryCandidate && installed.buildId!==source.discoveryCandidate.buildId) {
          // A release target may be a mutable branch name. Resolve its tag and run
          // only for installed modules with a different published candidate.
          const policy=await this.resolvePolicy(module);this.policies.set(module.moduleId,policy);
          const verified=policy.runtimeAssetNames ? await detectGitHubModule({policy,installed,request:this.request,nowMs:this.now(),timeoutMs:30000}) : null;
          if(verified?.latestPublished?.status==='candidate' && verified.latestPublished.buildId===source.discoveryCandidate.buildId) {
            verified.updateOrder=await assessUpdateOrder(installed,verified.latestPublished,module.repository,this.request);
            if(!verified.updateOrder.allowed)verified.reason=verified.updateOrder.reason;
            this.sources.set(source.moduleId,verified);continue;
          }
          source.updateOrder={allowed:false,reason:'The new candidate tag and build could not be verified.'};source.reason=source.updateOrder.reason;
        }
        this.sources.set(source.moduleId, source);
      }
      return { rows: this.rows(), rate: this.rateStatus(), providers: sourceProviders };
    }
    await Promise.all(this.registry.filter(module => ids.includes(module.moduleId)).map(async entry => {
      const module = await this.resolvePolicy(entry); this.policies.set(entry.moduleId, module);
      if (!module.runtimeAssetNames) {
        this.sources.set(module.moduleId, { status: 'unknown', reason: module.unavailableReason }); return;
      }
      this.progress(`Checking ${module.name}`);
      let installed;
      try { installed = this.targetReady() ? this.getInstaller(false)?.audit(module.moduleId).installed ?? null : undefined; } catch { installed = undefined; }
      const result = await detectGitHubModule({ policy: module, installed, request: this.request, nowMs: this.now(), timeoutMs: 30000 });
      result.developmentConfirmed = result.latestPublished?.status === 'candidate' && await this.confirmDevelopment(module, result);
      if(installed && result.latestPublished?.status==='candidate') result.updateOrder=await assessUpdateOrder(installed,result.latestPublished,module.repository,this.request);
      if (result.latestPublished?.status === 'candidate' && !result.developmentConfirmed) {
        result.status = 'unknown'; result.reason = 'Development build evidence is unconfirmed; download/install is unavailable.';
      } else if (result.status === 'unknown') result.reason = module.unavailableReason ?? result.reason;
      this.sources.set(module.moduleId, result);
    }));
    return { rows: this.rows(), rate: this.rateStatus(), providers: sourceProviders };
  }
  async refreshArtifactSources(ids,full){
    const entries=this.registry.filter(entry=>ids.includes(entry.moduleId));const now=this.now();let count=0;let deferred=0;
    const limit=full?50:this.automaticRequestLimit;
    const request=async options=>{
      // Reserve one call for a shared final ref snapshot; no per-module fallback.
      const cap=options.cachePhase==='confirm'?limit:limit-1;
      if(count>=cap){deferred++;throw new Error('API_BUDGET_QUEUED');}count++;return this.request(options);
    };
    const queued=(reason,latest)=>({status:'queued / verification required',checkedAtMs:now,developmentConfirmed:false,reason,latestPublished:latest??{status:'unknown'}});
    const pending=new Map();const installedFor=new Map();
    try{
      const snapshot=await readArtifactRefs(entries[0].repository,request);
      const ordered=full?entries:[...entries.slice(this.artifactCursor%entries.length),...entries.slice(0,this.artifactCursor%entries.length)];
      let attempted=0;
      for(const entry of ordered){
        const commit=snapshot.get(`refs/heads/${artifactBranchFor(entry)}`);const previous=this.sources.get(entry.moduleId);
        if(!commit){pending.set(entry.moduleId,{status:'unknown',checkedAtMs:now,reason:'No published artifact branch exists.',latestPublished:{status:'unknown'}});continue;}
        if(count>=limit-1){deferred++;pending.set(entry.moduleId,queued('Queued for source verification: the shared request budget is exhausted.'));continue;}
        let source;
        const cached=this.artifactManifestCache.get(entry.moduleId);
        if(!full&&cached?.latestPublished?.artifactCommit===commit)source={...cached,checkedAtMs:now};
        else{
          attempted++;
          source=await detectArtifactModule({entry,request,nowMs:now,snapshot,discoveryOnly:!full});
          if(!full&&source.policy)this.artifactManifestCache.set(entry.moduleId,source);
        }
        if(source.policy)this.policies.set(entry.moduleId,source.policy);
        let installed;try{installed=this.getInstaller(false)?.audit(entry.moduleId).installed;}catch{}
        installedFor.set(entry.moduleId,installed);
        const head=snapshot.get(`refs/heads/${entry.branch}`);source.observedSourceHead=head;
        if(!full&&installed&&previous?.orderForInstalledBuild===installed.buildId&&previous.observedSourceHead===head&&previous.latestPublished?.artifactCommit===commit&&previous.updateOrder?.allowed===true){
          source.updateOrder=previous.updateOrder;source.orderForInstalledBuild=installed.buildId;
        }
        pending.set(entry.moduleId,source);
      }
      if(!full&&entries.length)this.artifactCursor=(this.artifactCursor+Math.max(1,attempted))%entries.length;
      const updates=entries.filter(entry=>{const installed=installedFor.get(entry.moduleId);const source=pending.get(entry.moduleId);return installed&&source?.latestPublished?.status==='candidate'&&installed.buildId!==source.latestPublished.buildId&&!source.updateOrder;});
      const updateOrder=full?entries:[...updates.slice(this.updateCursor%Math.max(1,updates.length)),...updates.slice(0,this.updateCursor%Math.max(1,updates.length))].slice(0,3);
      for(const entry of updateOrder){
        const installed=installedFor.get(entry.moduleId);let source=pending.get(entry.moduleId);if(!installed||source?.latestPublished?.status!=='candidate')continue;
        if(!full){if(count>=limit-1){deferred++;source.reason='Queued for full update verification: request budget exhausted.';continue;}
          source=await detectArtifactModule({entry,request,nowMs:now,snapshot});pending.set(entry.moduleId,source);}
        source.observedSourceHead=snapshot.get(`refs/heads/${entry.branch}`);
        if(source.latestPublished?.status==='candidate'){
          source.updateOrder=await assessUpdateOrder(installed,source.latestPublished,entry.repository,request);source.orderForInstalledBuild=installed.buildId;
          if(!source.updateOrder.allowed)source.reason=source.updateOrder.reason;
        }
      }
      if(!full&&updates.length)this.updateCursor=(this.updateCursor+updateOrder.length)%updates.length;
      for(const entry of updates){const source=pending.get(entry.moduleId);if(!source.updateOrder){source.reason='Queued for full update verification. Modules are checked in rotating order.';deferred++;}}
      const final=await readArtifactRefs(entries[0].repository,request,'confirm');
      for(const entry of entries){let source=pending.get(entry.moduleId);if(snapshot.get(`refs/heads/${artifactBranchFor(entry)}`)!==final.get(`refs/heads/${artifactBranchFor(entry)}`)||snapshot.get(`refs/heads/${entry.branch}`)!==final.get(`refs/heads/${entry.branch}`))source={status:'unknown',checkedAtMs:now,reason:'Source refs changed during the shared observation.',latestPublished:{status:'unknown'}};this.sources.set(entry.moduleId,source);}
    }catch{
      for(const entry of entries)this.sources.set(entry.moduleId,queued('Queued: GitHub request budget, rate limit or complete shared ref snapshot is unavailable. No fallback source is used.'));
      if(entries.length)this.artifactCursor=(this.artifactCursor+1)%entries.length;
    }
    return {rows:this.rows(),rate:this.rateStatus(),providers:sourceProviders,discovery:{requests:count,requestLimit:limit,deferred}};
  }
  catalogFor(module, source) {
    const candidate = source.latestPublished;
    const catalog = { schema: 1, moduleId: module.moduleId, repository: module.repository, branch: module.branch,
      assembly: module.assembly, workflow: module.workflowPath, channel: 'development', devTool: true,
      buildId: candidate.buildId, sourceSha: candidate.sourceSha,
      release: { id: candidate.releaseId, tag: candidate.buildId, draft: false, publishedAt: candidate.publishedAt },
      files: candidate.assets.map(asset => ({ path: asset.name, assetName: asset.name, assetId: asset.id,
        size: asset.size, sha256: asset.sha256, url: asset.url })) };
    const data = Buffer.from(JSON.stringify(catalog));
    return { data, policy: { ...module, workflow: module.workflowPath, requiredFiles: candidate.assets.map(asset=>asset.name),
      catalogSha256: createHash('sha256').update(data).digest('hex') } };
  }
  async execute(operation, ids) {
    if (this.busy) throw new Error('OPERATION_ALREADY_RUNNING');
    if (!['install', 'update', 'uninstall'].includes(operation) || !Array.isArray(ids) || ids.length === 0 || ids.length > 16 || new Set(ids).size !== ids.length)
      throw new Error('INVALID_SELECTION');
    const selected = ids.map(id => this.registry.find(module => module.moduleId === id));
    if (selected.some(module => !module)) throw new Error('UNAVAILABLE_SELECTION');
    if (!this.targetReady()) throw new Error('SELECT_A_MODS_DESTINATION_FIRST');
    this.busy = true;
    try {
      if (!await this.assertGameClosed()) throw new Error('CLOSE_GAME_BEFORE_CHANGING_FILES');
      for (const module of selected) {
        const record = this.getInstaller(false)?.audit(module.moduleId).installed ?? null;
        if (operation === 'install' ? !!record : !record) throw new Error('INSTALLATION_STATE_CHANGED');
      }
      const packages = new Map(); const unchanged=new Set(); let total = 0;
      if (operation !== 'uninstall') {
        await this.refresh(ids, { full: true });
        for (const entry of selected) {
          const module = this.policies.get(entry.moduleId) ?? entry;
          const source = this.sources.get(module.moduleId);
          if(source?.status?.startsWith('queued'))throw new Error('API_BUDGET_QUEUED_TRY_A_SMALLER_BATCH_AFTER_RETRY');
          if (!source?.developmentConfirmed || source.latestPublished?.status !== 'candidate') throw new Error('SOURCE_UNAVAILABLE');
          const installed=this.getInstaller(false)?.audit(module.moduleId).installed;
          if(operation==='update' && installed?.buildId===source.latestPublished.buildId) {unchanged.add(module.moduleId);continue;}
          if(operation==='update' && source.updateOrder?.allowed!==true) throw new Error('PUBLISHED_BUILD_IS_NOT_CONFIRMED_NEWER');
          const pkg = source.bundlePackage ? {moduleId:module.moduleId,buildId:source.bundlePackage.buildId,files:source.bundlePackage.files.map(f=>({...f,bytes:Buffer.from(f.bytes)}))} : { moduleId: module.moduleId, buildId: source.latestPublished.buildId, files: [] };
          pkg.publication={provider:this.provider,sourceSha:source.latestPublished.sourceSha,releaseId:source.latestPublished.releaseId,
            publishedAt:source.latestPublished.publishedAt,runId:source.latestPublished.runId,attempt:source.latestPublished.attempt,
            ...(source.latestPublished.buildStartedAt?{buildStartedAt:source.latestPublished.buildStartedAt}:{})};
          if(this.provider==='artifact-branch')Object.assign(pkg.publication,{artifactCommit:source.latestPublished.artifactCommit,artifactBranch:source.latestPublished.artifactBranch,manifestSha256:source.latestPublished.manifestSha256});
          if(source.bundlePackage){total+=pkg.files.reduce((sum,f)=>sum+f.bytes.length,0);if(total>128*1024*1024)throw new Error('BATCH_SIZE_LIMIT');}
          else {
          const contract = this.provider==='artifact-branch'?null:this.catalogFor(module, source);
          const files=this.provider==='artifact-branch'?source.latestPublished.assets:JSON.parse(contract.data).files.filter(file=>module.runtimeAssetNames.includes(file.path));
          for (const file of files) {
            total += file.size; if (total > 128 * 1024 * 1024) throw new Error('BATCH_SIZE_LIMIT');
            this.progress(`Downloading ${module.name}: ${file.path}`);
            const result = this.provider==='artifact-branch'?await downloadArtifactFile({repository:module.repository,artifactCommit:source.latestPublished.artifactCommit,asset:file,transport:this.assetTransport}):await downloadCatalogAsset({ ...contract, assetPath: file.path, transport: this.assetTransport,
              timeoutMs: 30000, allowRedirect: allowGitHubAssetRedirect });
            pkg.files.push({ path: this.provider==='artifact-branch'?file.name:file.path, sha256: result.sha256, bytes: result.bytes });
          }
          }
          verifyPackage(pkg);
          if (!await this.inspectPackage(pkg, module)) throw new Error('ASSEMBLY_IDENTITY_UNVERIFIED');
          packages.set(module.moduleId, pkg);
        }
      }
      if (!await this.assertGameClosed()) throw new Error('CLOSE_GAME_BEFORE_CHANGING_FILES');
      // Prepare every module before mutation. Per-module commits are not globally atomic.
      const installer = this.getInstaller(true); const results = [];
      for (const module of selected) {
        if(unchanged.has(module.moduleId)){results.push({moduleId:module.moduleId,operation,status:'current'});continue;}
        this.progress(`${operation}: ${module.name}`);
        try {
          installer.audit(module.moduleId);
          const result = operation === 'uninstall' ? installer.uninstall(module.moduleId) : installer[operation](packages.get(module.moduleId));
          results.push({ moduleId: module.moduleId, operation, status: 'committed', backup: result.backup });
        } catch (error) {
          results.push({ moduleId: module.moduleId, operation, status: error.committed ? 'committed-with-warning' : 'failed',
            reason: 'Files require inspection. Prior committed modules are retained; remaining modules were not attempted.' });
          return { results, halted: true, rows: this.rows() };
        }
      }
      return { results, halted: false, rows: this.rows() };
    } finally { this.busy = false; }
  }
}
