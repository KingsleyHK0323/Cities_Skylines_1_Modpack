// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Injected transport only: no built-in fetch, network connection or filesystem.
import { createHash } from 'node:crypto';
import { catalogStatus } from './core.mjs';

class DownloadFailure extends Error {
  constructor(code) { super(`Download unavailable: ${code}`); this.code = code; }
}
const fail = code => { throw new DownloadFailure(code); };
function canonicalHttps(value) {
  if (typeof value !== 'string' || value.length > 4096 || /[\s\x00-\x1f\x7f]/.test(value)) return false;
  try {
    const url = new URL(value);
    return url.protocol === 'https:' && !url.username && !url.password && !url.hash && url.href === value;
  } catch { return false; }
}

export async function downloadCatalogAsset({ policy, data, assetPath, transport,
  redirectUrls = [], timeoutMs = 5000, maxRedirects = 3 } = {}) {
  const verified = catalogStatus(null, policy, data);
  if (verified.status === 'unavailable') fail('SOURCE_UNAVAILABLE');
  const asset = verified.catalog.files.find(file => file.path === assetPath);
  if (!asset) fail('ASSET_UNAVAILABLE');
  if (typeof transport !== 'function' || !Number.isInteger(timeoutMs) || timeoutMs < 1 || timeoutMs > 30000 ||
    !Number.isInteger(maxRedirects) || maxRedirects < 0 || maxRedirects > 5 ||
    !Array.isArray(redirectUrls) || redirectUrls.length > 16 || !redirectUrls.every(canonicalHttps)) fail('INVALID_DOWNLOAD_POLICY');
  const allowed = new Set(redirectUrls);
  const controller = new AbortController(); const deadline = performance.now() + timeoutMs;
  let timer;
  const checkDeadline = () => { if (controller.signal.aborted || performance.now() >= deadline) fail('TIMEOUT'); };
  const transfer = async () => {
    let url = asset.url; const visited = new Set(); let redirects = 0;
    while (true) {
      checkDeadline();
      if (!canonicalHttps(url)) fail('SOURCE_REFUSED');
      if (visited.has(url)) fail('REDIRECT_LOOP'); visited.add(url);
      const response = await transport(Object.freeze({ url, method: 'GET', redirect: 'manual', credentials: 'omit', signal: controller.signal }));
      checkDeadline();
      if (!response || response.url !== url || response.redirected !== false) fail('SOURCE_CHANGED');
      const headers = response.headers;
      if (!headers || typeof headers !== 'object' || Array.isArray(headers) ||
        Object.keys(headers).some(key => key !== key.toLowerCase())) fail('INVALID_HEADERS');
      const header = name => Object.hasOwn(headers, name) ? headers[name] : undefined;
      if ([301, 302, 303, 307, 308].includes(response.status)) {
        const next = header('location');
        if (!canonicalHttps(next) || !allowed.has(next)) fail('REDIRECT_REFUSED');
        if (++redirects > maxRedirects) fail('REDIRECT_LIMIT');
        url = next; continue;
      }
      if (response.status !== 200) fail('HTTP_STATUS');
      const encoding = header('content-encoding');
      if (encoding !== undefined && encoding !== 'identity') fail('ENCODING_REFUSED');
      const length = header('content-length');
      if (length !== undefined && (typeof length !== 'string' || !/^[1-9][0-9]{0,9}$/.test(length) || Number(length) !== asset.size)) fail('LENGTH_MISMATCH');
      if (!response.body || typeof response.body[Symbol.asyncIterator] !== 'function') fail('INVALID_BODY');
      const chunks = []; const hash = createHash('sha256'); let received = 0; let chunkCount = 0;
      for await (const chunk of response.body) {
        checkDeadline();
        if (!(chunk instanceof Uint8Array)) fail('INVALID_CHUNK');
        if (++chunkCount > 65536 || chunk.byteLength > asset.size - received) fail('SIZE_LIMIT');
        // Copy before hashing; an adapter cannot mutate already verified chunks.
        const copy = Buffer.from(chunk); received += copy.length;
        hash.update(copy); chunks.push(copy);
      }
      checkDeadline();
      if (received !== asset.size) fail('LENGTH_MISMATCH');
      if (hash.digest('hex') !== asset.sha256) fail('INTEGRITY_MISMATCH');
      return { moduleId: verified.moduleId, buildId: verified.selectedBuildId, path: asset.path,
        sha256: asset.sha256, bytes: Buffer.concat(chunks, received) };
    }
  };
  try {
    return await Promise.race([transfer(), new Promise((_, reject) => {
      timer = setTimeout(() => { controller.abort(); reject(new DownloadFailure('TIMEOUT')); }, timeoutMs);
    })]);
  } catch (error) {
    // Do not expose transport errors or signed redirect URLs in diagnostics.
    throw error instanceof DownloadFailure ? error : new DownloadFailure('TRANSPORT_FAILED');
  } finally {
    clearTimeout(timer); controller.abort();
  }
}
