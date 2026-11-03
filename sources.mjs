// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Proposed signed latest-pointer contract only; no live signer or polling adapter.
// Optional research only, not an adopted prerequisite for GitHub-native detection.
import { createPublicKey, verify } from 'node:crypto';
import { catalogStatus } from './core.mjs';

export function classifyModuleSource(input = {}) {
  let reason = 'SOURCE_UNAVAILABLE';
  const unknown = () => ({ status: 'unknown', freshness: 'unknown', installAllowed: false, reason });
  const require = condition => { if (!condition) throw new Error('Unverified source'); };
  const integer = value => Number.isSafeInteger(value) && value >= 0;
  try {
    const { installed, policy, data, proof, nonce, nowMs, minimumSequence, releases } = input;
    require(installed !== undefined);
    const source = catalogStatus(installed, policy, data);
    require(source.status !== 'unavailable');
    const catalog = source.catalog;
    reason = 'FRESHNESS_UNVERIFIED';
    require(integer(nowMs) && integer(minimumSequence) && typeof nonce === 'string' && /^[a-f0-9]{32,128}$/.test(nonce));
    require(typeof policy.freshnessPublicKey === 'string' && policy.freshnessPublicKey.length <= 4096 &&
      policy.freshnessPublicKey.startsWith('-----BEGIN PUBLIC KEY-----'));
    const key = createPublicKey(policy.freshnessPublicKey);
    require(key.asymmetricKeyType === 'ed25519');
    require(Buffer.isBuffer(proof?.payload) && proof.payload.length > 0 && proof.payload.length <= 16384 &&
      Buffer.isBuffer(proof.signature) && proof.signature.length === 64);
    require(verify(null, proof.payload, key, proof.signature));
    const pointer = JSON.parse(proof.payload.toString('utf8'));
    require(pointer.schema === 1 && pointer.selection === 'latest-published-development');
    for (const field of ['moduleId', 'repository', 'branch', 'assembly', 'workflow', 'channel', 'buildId', 'sourceSha'])
      require(pointer[field] === catalog[field]);
    require(pointer.catalogSha256 === policy.catalogSha256 && pointer.releaseId === catalog.release.id && pointer.releaseTag === catalog.release.tag);
    require(integer(pointer.issuedAtMs) && integer(pointer.expiresAtMs) && pointer.expiresAtMs > pointer.issuedAtMs &&
      pointer.expiresAtMs - pointer.issuedAtMs <= 300000 && pointer.issuedAtMs <= nowMs && nowMs < pointer.expiresAtMs);
    require(pointer.nonce === nonce && integer(pointer.sequence) && pointer.sequence > 0 && pointer.sequence >= minimumSequence);
    require(Date.parse(catalog.release.publishedAt) <= pointer.issuedAtMs);
    reason = 'RELEASE_BINDING_UNVERIFIED';
    require(Array.isArray(releases) && releases.length <= 100);
    const candidates = releases.filter(release => release?.id === catalog.release.id);
    require(candidates.length === 1);
    const release = candidates[0];
    require(release.url === `https://api.github.com/repos/${catalog.repository}/releases/${catalog.release.id}` &&
      release.html_url === `https://github.com/${catalog.repository}/releases/tag/${encodeURIComponent(catalog.release.tag)}`);
    require(release.tag_name === catalog.release.tag && release.target_commitish === catalog.sourceSha &&
      release.draft === false && release.published_at === catalog.release.publishedAt);
    require(Array.isArray(release.assets) && release.assets.length <= 512);
    const ids = new Set(); const names = new Set();
    for (const asset of release.assets) {
      require(integer(asset.id) && asset.id > 0 && !ids.has(asset.id) && typeof asset.name === 'string' && !names.has(asset.name.toLowerCase()));
      ids.add(asset.id); names.add(asset.name.toLowerCase());
    }
    for (const file of catalog.files) {
      const asset = release.assets.find(candidate => candidate.id === file.assetId);
      require(asset && asset.name === file.assetName && asset.state === 'uploaded' && asset.size === file.size &&
        asset.digest === `sha256:${file.sha256}` && asset.browser_download_url === file.url);
    }
    return { status: source.status, freshness: 'verified', installAllowed: false,
      moduleId: catalog.moduleId, selectedBuildId: catalog.buildId, sourceSha: catalog.sourceSha,
      releaseId: catalog.release.id, selection: pointer.selection, verifiedSequence: pointer.sequence,
      checkedAtMs: nowMs, validUntilMs: pointer.expiresAtMs };
  } catch { return unknown(); }
}
