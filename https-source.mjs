// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import https from 'node:https';

function permitted(url, repository, asset = false) {
  const target = new URL(url);
  if (target.protocol !== 'https:' || target.username || target.password || target.hash || target.href !== url) throw new Error('HTTPS_SOURCE_REFUSED');
  if (asset && target.origin === 'https://release-assets.githubusercontent.com') return target;
  if(asset && target.origin==='https://raw.githubusercontent.com'){
    if(!target.pathname.startsWith(`/${repository}/`)||!new RegExp(`^/[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+/[a-f0-9]{40}/.+$`).test(target.pathname))throw new Error('HTTPS_SOURCE_REFUSED');
    return target;
  }
  const prefix = asset ? `/${repository}/releases/download/` : `/repos/${repository}/`;
  if (target.origin !== (asset ? 'https://github.com' : 'https://api.github.com') || !target.pathname.startsWith(prefix)) throw new Error('HTTPS_SOURCE_REFUSED');
  return target;
}

export function makeGitHubApiRequest(repository) {
  return async options => {
    const url = permitted(options.url, repository);
    if (options.method !== 'GET') throw new Error('GET_ONLY');
    return new Promise((resolve, reject) => {
      const req = https.request(url, { method: 'GET', signal: options.signal, maxHeaderSize: 16384,
        headers: { 'User-Agent': 'Cities-Skylines-1-Modpack-Dev-Manager', Accept: 'application/vnd.github+json',
          'X-GitHub-Api-Version': '2022-11-28', 'Accept-Encoding': 'identity', 'Cache-Control': 'no-cache' } }, response => {
        const chunks = []; let size = 0;
        response.on('data', chunk => { size += chunk.length; if (size > 4 * 1024 * 1024) response.destroy(new Error('API_SIZE_LIMIT')); else chunks.push(chunk); });
        response.on('error', () => reject(new Error('API_RESPONSE_FAILED')));
        response.on('end', () => {
          try { resolve({ url: options.url, status: response.statusCode, redirected: false, headers: response.headers,
            body: JSON.parse(Buffer.concat(chunks).toString('utf8')) }); }
          catch { reject(new Error('API_JSON_INVALID')); }
        });
      });
      req.setTimeout(15000, () => req.destroy(new Error('API_TIMEOUT')));
      req.on('error', () => reject(new Error('API_REQUEST_FAILED'))); req.end();
    });
  };
}

export class GitHubRequestPool {
  constructor(request, { concurrency = 2, hourlyLimit=50, now=Date.now } = {}) {
    this.transport = request; this.concurrency = concurrency; this.active = 0; this.waiting = [];
    this.cache = new Map(); this.retryAtMs = 0; this.remaining = null;
    this.hourlyLimit=hourlyLimit;this.now=now;this.sent=[];this.serverResetAtMs=0;
  }
  beginRefresh() { this.cache.clear(); }
  budget(){const now=this.now();this.sent=this.sent.filter(time=>time>now-3600000);return {left:Math.max(0,this.hourlyLimit-this.sent.length),retryAtMs:this.sent.length?this.sent[0]+3600001:now};}
  status() {const budget=this.budget();return { retryAtMs: Math.max(this.retryAtMs,budget.left===0?budget.retryAtMs:0), remaining: this.remaining,localRemaining:budget.left }; }
  async waitFor(value, signal) {
    if (!signal) return structuredClone(await value);
    if (signal.aborted) throw new Error('API_WAITER_CANCELLED');
    return new Promise((resolve, reject) => {
      const cancel = () => { signal.removeEventListener('abort', cancel); reject(new Error('API_WAITER_CANCELLED')); };
      signal.addEventListener('abort', cancel, { once: true });
      value.then(result => { signal.removeEventListener('abort', cancel); resolve(structuredClone(result)); },
        () => { signal.removeEventListener('abort', cancel); reject(new Error('API_REQUEST_FAILED')); });
    });
  }
  async request(options) {
    if (this.now() < this.retryAtMs) return { url: options.url, status: 429, redirected: false,
      headers: { 'retry-after': String(Math.ceil((this.retryAtMs - this.now()) / 1000)) }, body: {} };
    const key = `${options.cachePhase === 'confirm' ? 'confirm' : 'initial'}:${options.url}`; const cached = this.cache.get(key);
    if (cached && Date.now() - cached.createdAt < 30000) return this.waitFor(cached.value, options.signal);
    const value = (async () => {
      if (this.active >= this.concurrency) await new Promise(resolve => this.waiting.push(resolve));
      else this.active++;
      try {
        const budget=this.budget();
        if(this.now()<this.retryAtMs||budget.left===0||this.remaining===0&&this.now()<this.serverResetAtMs){
          const retry=Math.max(this.retryAtMs,budget.left===0?budget.retryAtMs:0,this.remaining===0?this.serverResetAtMs:0);
          this.retryAtMs=retry;return {url:options.url,status:429,redirected:false,headers:{'retry-after':String(Math.max(1,Math.ceil((retry-this.now())/1000)))},body:{code:'API_BUDGET_QUEUED'}};
        }
        this.sent.push(this.now());
        // The shared request has an independent bounded lifetime. Each caller can
        // cancel its own wait without aborting another module's shared response.
        const response = await this.transport({ ...options, signal: AbortSignal.timeout(30000) }); const headers = response.headers ?? {};
        if (/^[0-9]+$/.test(headers['x-ratelimit-remaining'] ?? '')) this.remaining = Number(headers['x-ratelimit-remaining']);
        if(/^[0-9]+$/.test(headers['x-ratelimit-reset']??''))this.serverResetAtMs=Number(headers['x-ratelimit-reset'])*1000;
        if (response.status === 429 || response.status === 403) {
          const retry = Number(headers['retry-after']); const reset = Number(headers['x-ratelimit-reset']) * 1000;
          const retryDate = Date.parse(headers['retry-after']);
          this.retryAtMs = Math.max(Date.now() + (Number.isFinite(retry) && retry > 0 ? Math.min(retry, 86400) * 1000 : 60000),
            Number.isFinite(retryDate) && retryDate > Date.now() ? Math.min(retryDate, Date.now() + 86400000) : 0,
            Number.isFinite(reset) && reset > Date.now() ? Math.min(reset, Date.now() + 86400000) : 0);
        }
        return response;
      } finally { const next = this.waiting.shift(); if (next) next(); else this.active--; }
    })();
    const entry = { createdAt: Date.now(), value }; this.cache.set(key, entry);
    value.catch(() => { if (this.cache.get(key) === entry) this.cache.delete(key); });
    return this.waitFor(value, options.signal);
  }
}

export function makeAssetTransport(repository) {
  return async options => {
    const target = permitted(options.url, repository, true);
    return new Promise((resolve, reject) => {
      const req = https.request(target, { method: 'GET', signal: options.signal, maxHeaderSize: 16384,
        headers: { 'User-Agent': 'Cities-Skylines-1-Modpack-Dev-Manager', 'Accept-Encoding': 'identity' } }, response => {
        resolve({ url: options.url, status: response.statusCode, redirected: false, headers: response.headers, body: response });
        if ([301, 302, 303, 307, 308].includes(response.statusCode)) response.destroy();
      });
      req.setTimeout(15000, () => req.destroy(new Error('ASSET_TIMEOUT')));
      req.on('error', () => reject(new Error('ASSET_REQUEST_FAILED'))); req.end();
    });
  };
}

export function allowGitHubAssetRedirect(from, to) {
  try {
    const source = new URL(from); const target = new URL(to);
    return ['https://github.com', 'https://release-assets.githubusercontent.com'].includes(source.origin) &&
      target.origin === 'https://release-assets.githubusercontent.com' && !target.username && !target.password && !target.hash && target.href === to;
  } catch { return false; }
}
