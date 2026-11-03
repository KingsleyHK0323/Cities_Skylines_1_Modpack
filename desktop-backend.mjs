// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import readline from 'node:readline';
import { fileURLToPath } from 'node:url';
import { execFile } from 'node:child_process';
import { promisify } from 'node:util';
import { IsolatedInstaller, metadataForModsDirectory } from './filesystem.mjs';
import { DesktopService } from './desktop-service.mjs';
import { moduleRegistry, sourceProviders, defaultSourceProvider } from './registry.mjs';
import { makeGitHubApiRequest, GitHubRequestPool, makeAssetTransport } from './https-source.mjs';
import { resolveGitHubPolicy } from './module-policy.mjs';
import { BundleSource } from './bundle-source.mjs';
import { allowedInstallationAsset } from './asset-policy.mjs';
import {mergePresentationPreferences} from './preferences.mjs';
import {localizeReply} from './localization.mjs';
import {SettingsStore} from './settings-store.mjs';

const runFile = promisify(execFile);
const appRoot = path.dirname(fileURLToPath(import.meta.url));
const statePath = path.join(appRoot, '.desktop-state.json');
const settingsStore=new SettingsStore(statePath);
const helper = process.env.DEV_MANAGER_NATIVE_HELPER ?? path.join(appRoot, 'desktop', 'portable', 'DevManager.exe');
let state;
function getInstaller(create) {
  if (state.mode === 'unconfigured') return null;
  return state.mode === 'sandbox' ? new IsolatedInstaller(state.sandbox) :
    IsolatedInstaller.forModsDirectory(state.modsRoot, { confirmed: true, create });
}
export async function inspectAssembly(file, expected, helperPath = helper) {
  const { stdout } = await runFile(helperPath, ['--inspect', file], { windowsHide: true, timeout: 5000, maxBuffer: 65536 });
  const result = JSON.parse(stdout.trim());
  return result.ok === true && result.name === expected;
}
async function gameClosed() {
  if (process.platform !== 'win32') return false;
  try { const { stdout } = await runFile(helper, ['--game-running'], { windowsHide: true, timeout: 5000, maxBuffer: 4096 });
    return JSON.parse(stdout.trim()).running === false; } catch { return false; }
}
async function inspectPackage(pkg, module) {
  for (const file of pkg.files) {
    if (file.path.includes('/')) return false;
    if(!file.path.toLowerCase().endsWith('.dll')) {
      if(!module.runtimeAssetNames.includes(file.path) || !allowedInstallationAsset(file.path,null))return false;
      continue;
    }
    const expected = file.path === `${module.assembly}.dll` ? module.assembly :
      module.expectedAssemblies?.[file.path] ?? (module.runtimeAssetNames.includes(file.path) ? file.path.slice(0, -4) : null);
    if (!expected || !allowedInstallationAsset(file.path,expected) || !await inspectAssemblyBytes(file.bytes, expected)) return false;
  }
  return true;
}
export function inspectAssemblyBytes(bytes, expected, helperPath = helper) {
  return new Promise(resolve => {
    const child = execFile(helperPath, ['--inspect-stdin'], { windowsHide: true, timeout: 5000, maxBuffer: 65536 }, (error, stdout) => {
      if (error) return resolve(false);
      try { const result = JSON.parse(stdout.trim()); resolve(result.ok === true && result.name === expected); } catch { resolve(false); }
    });
    child.stdin.on('error', () => {}); child.stdin.end(bytes.toString('base64'));
  });
}
export function inspectZipBytes(bytes, helperPath = helper) {
  return new Promise((resolve,reject)=>{
    const child=execFile(helperPath,['--unzip-stdin'],{windowsHide:true,timeout:15000,maxBuffer:110*1024*1024},(error,stdout)=>{
      if(error)return reject(new Error('BUNDLE_ARCHIVE_UNAVAILABLE'));
      try{const value=JSON.parse(stdout.trim());if(!value.ok)throw new Error();resolve(value.entries);}catch{reject(new Error('BUNDLE_ARCHIVE_INVALID'));}
    });child.stdin.on('error',()=>{});child.stdin.end(bytes.toString('base64'));
  });
}
function targetInfo() {
  if (state.mode === 'unconfigured') return { mode: state.mode, path: '', scope: 'Choose your existing game Mods folder in Destination to enable file operations. The test workspace is optional.' };
  return { mode: state.mode, path: state.mode === 'sandbox' ? state.sandbox : state.modsRoot,
    backupPath:state.mode==='game-mods'?metadataForModsDirectory(state.modsRoot):'',
    scope: state.mode === 'sandbox' ? 'Test workspace only. These files are not loaded by the game.' :
      `Only CS1DevManager-<module> folders directly under Mods are managed. Retained backups: ${metadataForModsDirectory(state.modsRoot)}` };
}

export async function startBackend({settings=settingsStore}={}) {
  state = settings.load(); getInstaller(false);
  let startupWarning=settings.lastLoadResult?.warning;let startupLock=settings.lastLoadResult?.lockMayRemain;
  const pool = new GitHubRequestPool(makeGitHubApiRequest('KingsleyHK0323/Cities_Skylines_1_Modpack'));
  const send = value => {if(value.result?.settingsRecoveryCleared===true){startupWarning=undefined;startupLock=undefined;}if(value.result&&startupWarning)value={...value,result:{...value.result,settingsWarning:value.result.settingsWarning??startupWarning,settingsLockMayRemain:value.result.settingsLockMayRemain??startupLock}};process.stdout.write(JSON.stringify(localizeReply(value,state)) + '\n');};
  const assetTransport=makeAssetTransport('KingsleyHK0323/Cities_Skylines_1_Modpack');
  const bundles=new BundleSource({registry:moduleRegistry,request:pool.request.bind(pool),transport:assetTransport,inspectArchive:inspectZipBytes});
  const service = new DesktopService({ request: pool.request.bind(pool), getInstaller, inspectPackage, assertGameClosed: gameClosed,
    targetReady: () => state.mode !== 'unconfigured',
    resolvePolicy: module => resolveGitHubPolicy(module, pool.request.bind(pool)),
    assetTransport,loadBundle:()=>bundles.load(), beginRefresh: () => pool.beginRefresh(),
    rateStatus: () => pool.status(), progress: text => send({ event: 'progress', text }) });
  service.provider=defaultSourceProvider;
  const input = readline.createInterface({ input: process.stdin, crlfDelay: Infinity }); let queue = Promise.resolve();
  input.on('line', line => {
    queue = queue.then(async () => {
      let command;
      try {
        if (line.length > 16384) throw new Error('COMMAND_TOO_LARGE'); command = JSON.parse(line);
        let result;
        if (command.command === 'status') result = { rows: service.rows(), providers: sourceProviders };
        else if (command.command === 'refresh') result = await service.refresh();
        else if(command.command==='source') {
          if(!sourceProviders.some(source=>source.id===command.provider&&source.enabled))throw new Error('INVALID_SOURCE_PROVIDER');
          service.provider=command.provider;service.sources.clear();result=await service.refresh();
        }
        else if (['install', 'update', 'uninstall'].includes(command.command)) result = await service.execute(command.command, command.modules);
        else if(command.command==='preferences'){
          try{
            const saved=settings.update(current=>{
              const destination=value=>JSON.stringify([value.mode,value.sandbox??null,value.modsRoot??null]);
              if(destination(current)!==destination(state))throw new Error('DESTINATION_CHANGED_EXTERNALLY');
              return mergePresentationPreferences(current,command.preferences);
            });
            state=saved.state;result={rows:service.rows(),providers:sourceProviders,settingsWarning:saved.warning,settingsLockMayRemain:saved.lockMayRemain,settingsRecoveryCleared:!saved.warning&&!saved.lockMayRemain};
          }catch(error){if(!['INVALID_PRESENTATION_PREFERENCES','EXISTING_PREFERENCES_REQUIRE_INSPECTION','LOCAL_STATE_BUSY','LOCAL_STATE_OVERSIZED','LOCAL_STATE_RECOVERY_REQUIRED','DESTINATION_CHANGED_EXTERNALLY'].includes(error.message))throw new Error('PREFERENCES_SAVE_FAILED');throw error;}
        }
        else if (command.command === 'destination') {
          if (!command.confirmed || !['sandbox', 'game-mods'].includes(command.mode)) throw new Error('DESTINATION_CONFIRMATION_REQUIRED');
          if (command.mode === 'game-mods') {
            if (typeof command.path !== 'string' || !path.isAbsolute(command.path) || command.path.startsWith('\\\\') ||
              /[\x00-\x1f]/.test(command.path) || command.path === path.parse(command.path).root) throw new Error('INVALID_MODS_DIRECTORY');
            // Read-only validation. No game-folder metadata is created until an explicit operation.
            IsolatedInstaller.forModsDirectory(command.path, { confirmed: true, create: false });
          }
          const saved=settings.update(current=>({...current,mode:command.mode,
            sandbox: command.mode === 'sandbox' && !current.sandbox ? fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-')) : current.sandbox,
            modsRoot:command.mode==='game-mods'?path.resolve(command.path):current.modsRoot}));
          state=saved.state;service.sources.clear();result={rows:service.rows(),providers:sourceProviders,settingsWarning:saved.warning,settingsLockMayRemain:saved.lockMayRemain,settingsRecoveryCleared:!saved.warning&&!saved.lockMayRemain};
        } else throw new Error('UNKNOWN_COMMAND');
        send({ id: command.id, ok: true, result: { ...result, target: targetInfo() } });
      } catch (error) {
        const code = typeof error.message === 'string' && /^[A-Z0-9_]+$/.test(error.message) ? error.message : 'OPERATION_REFUSED_INSPECT_LOCAL_FILES';
        send({ id: command?.id ?? null, ok: false, error: code, result: { rows: service.rows(), target: targetInfo() } });
      }
    }).catch(() => send({ ok: false, error: 'BACKEND_UNAVAILABLE' }));
  });
}
export function startupFailureReply(error,current=state){
  const code=['LOCAL_STATE_RECOVERY_REQUIRED','LOCAL_STATE_BUSY'].includes(error?.message)?error.message:'LOCAL_STATE_UNAVAILABLE';
  return localizeReply({ok:false,error:code,...(code==='LOCAL_STATE_RECOVERY_REQUIRED'?{lockMayRemain:true,recoveryRequired:true}:{})},current);
}
if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  startBackend().catch(error => { process.stdout.write(JSON.stringify(startupFailureReply(error)) + '\n'); process.exitCode = 1; });
}
