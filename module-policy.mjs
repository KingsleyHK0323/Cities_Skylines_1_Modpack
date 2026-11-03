// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Same-repository, exact-branch GitHub policy adapter. No separate service/key.
import { createHash } from 'node:crypto';
import { safeRelative } from './core.mjs';
import { allowedInstallationAsset } from './asset-policy.mjs';
export async function resolveGitHubPolicy(entry, request, { commitSha } = {}) {
  try {
    const base = `https://api.github.com/repos/${entry.repository}`;
    const get = url => request({ url, method: 'GET', redirect: 'manual', credentials: 'omit', cachePhase: 'initial', signal: AbortSignal.timeout(15000) });
    let commit = commitSha;
    if (!commit) {
      const response = await get(`${base}/git/ref/heads/${entry.branch.split('/').map(encodeURIComponent).join('/')}`);
      if (response.status !== 200 || response.body.ref !== `refs/heads/${entry.branch}` || response.body.object?.type !== 'commit') throw new Error();
      commit = response.body.object.sha;
    }
    if (!/^[a-f0-9]{40}$/.test(commit)) throw new Error();
    const response = await get(`${base}/contents/dev-manager.manifest.json?ref=${commit}`);
    if (response.status === 404) return { ...entry, policyOrigin: 'bootstrap' };
    const file = response.body;
    if (response.status !== 200 || file.encoding !== 'base64' || !Number.isInteger(file.size) || file.size > 262144 || typeof file.content !== 'string') throw new Error();
    const bytes = Buffer.from(file.content, 'base64');
    if (bytes.length !== file.size || createHash('sha1').update(`blob ${bytes.length}\0`).update(bytes).digest('hex') !== file.sha) throw new Error();
    const manifest = JSON.parse(bytes.toString('utf8'));
    if (manifest.schema !== 1 || manifest.moduleId !== entry.moduleId || manifest.branch !== entry.branch || manifest.repository !== entry.repository ||
      manifest.sourceKind !== 'module-release' || manifest.development?.devTools !== true ||
      typeof manifest.assembly !== 'string' || entry.assembly && manifest.assembly !== entry.assembly) throw new Error();
    safeRelative(manifest.workflowPath); safeRelative(manifest.releaseTagPrefix);
    if (!manifest.workflowPath.startsWith('.github/workflows/') || !/\.ya?ml$/.test(manifest.workflowPath) ||
      manifest.releaseTagPrefix.includes('/') || !manifest.releaseTagPrefix.endsWith('-') ||
      entry.releaseTagPrefix && manifest.releaseTagPrefix !== entry.releaseTagPrefix ||
      !Array.isArray(manifest.runtimeAssets) || manifest.runtimeAssets.length === 0 || manifest.runtimeAssets.length > 256) throw new Error();
    const names = new Set(); const expectedAssemblies = {};
    for (const asset of manifest.runtimeAssets) {
      safeRelative(asset.name);
      if (!allowedInstallationAsset(asset.name,asset.assemblyName) || names.has(asset.name.toLowerCase())) throw new Error();
      names.add(asset.name.toLowerCase()); Object.defineProperty(expectedAssemblies, asset.name, { value: asset.assemblyName, enumerable: true });
    }
    if (expectedAssemblies[`${manifest.assembly}.dll`] !== manifest.assembly) throw new Error();
    const provenanceAssetName=manifest.development.provenanceAssetName ?? entry.provenanceAssetName;
    if(typeof provenanceAssetName!=='string' || !/^build-info\.(json|txt)$/.test(provenanceAssetName))throw new Error();
    return { ...entry, assembly: manifest.assembly, workflowPath: manifest.workflowPath,
      provenanceAssetName,
      releaseTagPrefix: manifest.releaseTagPrefix, runtimeAssetNames: manifest.runtimeAssets.map(asset => asset.name),
      expectedAssemblies, policyOrigin: 'branch-manifest', policyCommitSha: commit, unavailableReason: undefined };
  } catch { return { ...entry, runtimeAssetNames: undefined, unavailableReason: 'Module manifest is unavailable, invalid or mismatched; no source is assumed.' }; }
}
