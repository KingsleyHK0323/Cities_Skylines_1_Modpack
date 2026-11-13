// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import https from 'node:https';

// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
const rejectedResponseObservations = new WeakMap();
const responseHeaderObservers = new WeakMap();

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
      let responseObservation;
      const rejectRequest = code => {
        const error = new Error(code);
        if (responseObservation) rejectedResponseObservations.set(error, responseObservation);
        reject(error);
      };
      const req = https.request(url, { method: 'GET', signal: options.signal, maxHeaderSize: 16384,
        headers: { 'User-Agent': 'Cities-Skylines-1-Modpack-Dev-Manager', Accept: 'application/vnd.github+json',
          'X-GitHub-Api-Version': '2022-11-28', 'Accept-Encoding': 'identity', 'Cache-Control': 'no-cache' } }, response => {
        // Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
        const receivedHeaders = response.headers ?? {};
        responseObservation = { status: response.statusCode, headers: {
          'x-ratelimit-remaining': receivedHeaders['x-ratelimit-remaining'],
          'x-ratelimit-reset': receivedHeaders['x-ratelimit-reset'],
          'retry-after': receivedHeaders['retry-after'], date: receivedHeaders.date } };
        responseHeaderObservers.get(options)?.(responseObservation);
        // Preserve error status and rate-limit headers without trusting or draining its body.
        if (Number.isInteger(response.statusCode) && response.statusCode >= 300) {
          response.on('error', () => {});
          resolve({ url: options.url, status: response.statusCode, redirected: false, headers: response.headers, body: null });
          response.destroy(); return;
        }
        const chunks = []; let size = 0;
        response.on('data', chunk => { size += chunk.length; if (size > 4 * 1024 * 1024) response.destroy(new Error('API_SIZE_LIMIT')); else chunks.push(chunk); });
        response.on('error', () => rejectRequest('API_RESPONSE_FAILED'));
        response.on('end', () => {
          try { resolve({ url: options.url, status: response.statusCode, redirected: false, headers: response.headers,
            body: JSON.parse(Buffer.concat(chunks).toString('utf8')) }); }
          catch { rejectRequest('API_JSON_INVALID'); }
        });
      });
      req.setTimeout(15000, () => req.destroy(new Error('API_TIMEOUT')));
      req.on('error', () => rejectRequest('API_REQUEST_FAILED')); req.end();
    });
  };
}

export class GitHubRequestPool {
  constructor(request, { concurrency = 2, hourlyLimit=50, now=Date.now, monotonic=() => Number(process.hrtime.bigint()/1000000n) } = {}) {
    this.transport = request; this.concurrency = concurrency; this.active = 0; this.waiting = [];
    this.cache = new Map(); this.remaining = null;
    this.hourlyLimit=hourlyLimit;this.now=now;this.monotonic=monotonic;this.sent=[];this.serverUntil=0;this.serverWindow=null;
  }
  beginRefresh() { this.cache.clear(); }
  budget(){
    const now=this.now(),elapsed=this.monotonic();
    if(!Number.isFinite(now)||!Number.isFinite(elapsed))return {left:0,retryAtMs:0,wait:Infinity};
    this.sent=this.sent.filter(time=>time>elapsed-3600000);
    const wait=this.sent.length?Math.max(0,this.sent[0]+3600001-elapsed):0;
    return {left:Math.max(0,this.hourlyLimit-this.sent.length),retryAtMs:now+wait,wait};
  }
  serverWait(elapsed=this.monotonic()) {
    if(!Number.isFinite(elapsed))return Infinity;
    if(this.serverUntil>0&&elapsed>=this.serverUntil){this.serverUntil=0;if(this.remaining===0){this.remaining=null;this.serverWindow=null;}}
    return this.serverUntil>0?Math.max(0,this.serverUntil-elapsed):0;
  }
  status() {
    const budget=this.budget(),wait=Math.max(this.serverWait(),budget.left===0?budget.wait:0),now=this.now();
    return {retryAtMs:wait>0&&Number.isFinite(now)?Math.min(Date.UTC(9999,11,30),now+wait):0,remaining:this.remaining,localRemaining:budget.left};
  }
  observeServer(headers,status) {
    const elapsed=this.monotonic(),now=this.now();
    if(!Number.isFinite(elapsed)||!Number.isFinite(now)){this.serverUntil=Number.MAX_SAFE_INTEGER;return;}
    this.serverWait(elapsed);
    const integer=value=>typeof value==='string'&&/^[0-9]+$/.test(value)?Number(value):null;
    const remaining=integer(headers['x-ratelimit-remaining']);
    const resetSeconds=integer(headers['x-ratelimit-reset']);
    const reset=resetSeconds===null?null:Math.min(Number.MAX_SAFE_INTEGER,resetSeconds*1000);
    const serverDate=typeof headers.date==='string'?Date.parse(headers.date):NaN;
    const reference=Number.isFinite(serverDate)?serverDate:now;
    const resetDelay=reset!==null&&reset>reference?reset-reference:0;
    const retryText=typeof headers['retry-after']==='string'?headers['retry-after'].trim():'';
    let retryDelay=0;
    if(/^[0-9]+$/.test(retryText))retryDelay=Math.min(Number.MAX_SAFE_INTEGER,Number(retryText)*1000);
    else {const date=Date.parse(retryText);if(Number.isFinite(date)&&date>reference)retryDelay=date-reference;}
    const hold=delay=>{this.serverUntil=Math.max(this.serverUntil,Math.min(Number.MAX_SAFE_INTEGER,elapsed+Math.max(1,delay)));};
    const older=reset!==null&&this.serverWindow!==null&&reset<this.serverWindow;
    if(remaining!==null&&!older){
      const later=reset!==null&&this.serverWindow!==null&&reset>this.serverWindow;
      if(later&&this.serverWait(elapsed)===0)this.remaining=remaining;
      else this.remaining=this.remaining===null?remaining:Math.min(this.remaining,remaining);
      if(reset!==null&&(this.serverWindow===null||reset>=this.serverWindow))this.serverWindow=reset;
      if(remaining===0)hold(Math.max(resetDelay,retryDelay)||60000);
    }
    if(status===429||status===403)hold(Math.max(retryDelay||60000,resetDelay));
  }
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
    const initialServerWait=this.serverWait();
    if (initialServerWait>0) return { url: options.url, status: 429, redirected: false,
      headers: { 'retry-after': String(Number.isFinite(initialServerWait)?Math.max(1,Math.ceil(initialServerWait/1000)):60) }, body: {} };
    const key = `${options.cachePhase === 'confirm' ? 'confirm' : 'initial'}:${options.url}`; const cached = this.cache.get(key);
    const elapsed=this.monotonic();
    if (cached && Number.isFinite(elapsed) && elapsed>=cached.createdAt && elapsed-cached.createdAt < 30000) return this.waitFor(cached.value, options.signal);
    const value = (async () => {
      if (this.active >= this.concurrency) await new Promise(resolve => this.waiting.push(resolve));
      else this.active++;
      try {
        const budget=this.budget();
        const wait=Math.max(this.serverWait(),budget.left===0?budget.wait:0);
        if(wait>0||budget.left===0){
          return {url:options.url,status:429,redirected:false,headers:{'retry-after':String(Number.isFinite(wait)?Math.max(1,Math.ceil(wait/1000)):60)},body:{code:'API_BUDGET_QUEUED'}};
        }
        this.sent.push(this.monotonic());
        // The shared request has an independent bounded lifetime. Each caller can
        // cancel its own wait without aborting another module's shared response.
        let response; let headersObserved = false;
        const transportOptions = { ...options, signal: AbortSignal.timeout(30000) };
        // Only this module's API transport can access the observer; injected transports
        // cannot create an early rate-limit observation by forging an options callback.
        responseHeaderObservers.set(transportOptions, observation => {
          headersObserved = true;
          try { this.observeServer(observation.headers, observation.status); }
          catch { this.serverUntil = Number.MAX_SAFE_INTEGER; }
        });
        try { response = await this.transport(transportOptions); }
        catch(error) {
          const observation = error instanceof Error ? rejectedResponseObservations.get(error) : undefined;
          if(observation && !headersObserved) this.observeServer(observation.headers ?? {}, observation.status);
          throw error;
        }
        finally { responseHeaderObservers.delete(transportOptions); }
        const headers = response.headers ?? {};
        if (!headersObserved) this.observeServer(headers,response.status);
        return response;
      } finally { const next = this.waiting.shift(); if (next) next(); else this.active--; }
    })();
    const entry = { createdAt: this.monotonic(), value }; this.cache.set(key, entry);
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
