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
function getInstaller(create, Installer = IsolatedInstaller) {
  if (state.mode === 'unconfigured') return null;
  if (state.mode === 'sandbox' && !create) return Installer.inspectSandbox(state.sandbox);
  const expected = arguments[2];
  if (state.mode === 'sandbox' && create) return Installer.openExistingSandbox(state.sandbox, expected ?? undefined);
  if (state.mode === 'game-mods' && expected === null && arguments[3])
    return Installer.forModsDirectory(state.modsRoot, { confirmed: true, create, expectedPresence: 'absent', expectedRoot: arguments[3] });
  if (state.mode === 'game-mods' && expected !== undefined)
    return Installer.forModsDirectory(state.modsRoot, { confirmed: true, create,
      expectedPresence: expected === null ? 'absent' : 'existing', expectedIdentity: expected ?? undefined });
  return state.mode === 'sandbox' ? new Installer(state.sandbox) :
    Installer.forModsDirectory(state.modsRoot, { confirmed: true, create });
}
// Keep a destination identity across asynchronous source checks; changed or unobservable identities fail closed.
function destinationSnapshot(installer) {
  if (installer === null) return null;
  const capture = identity => {
    if (identity && typeof identity.dev === 'bigint' && identity.dev <= 0n) throw new Error('DESTINATION_IDENTITY_UNVERIFIABLE');
    if (!identity || typeof identity.dev !== 'bigint' || typeof identity.ino !== 'bigint' || identity.ino <= 0n)
      throw new Error('DESTINATION_IDENTITY_UNVERIFIABLE');
    return { dev: identity.dev, ino: identity.ino };
  };
  return { root: capture(installer.rootIdentity), meta: capture(installer.metaIdentity) };
}
function missingMetadataRootSnapshot(Installer, root) {
  const identity = Installer.inspectModsRoot(root);
  if (!identity || typeof identity.dev !== 'bigint' || typeof identity.ino !== 'bigint' || identity.dev <= 0n || identity.ino <= 0n)
    throw new Error('DESTINATION_IDENTITY_UNVERIFIABLE');
  return { dev: identity.dev, ino: identity.ino };
}
function sameRoot(a, b) { return !!a && !!b && a.dev === b.dev && a.ino === b.ino; }
/* Retained earlier preflight observation; not executed.
function observeUnverifiedMods(Installer, root) {
  let rootIdentity; let identity;
  try { rootIdentity = missingMetadataRootSnapshot(Installer, root); } catch {}
  try { identity = destinationSnapshot(Installer.inspectModsMetadataIdentity(root)); } catch {}
  if (!rootIdentity || !identity || !sameRoot(rootIdentity, identity.root)) identity = null;
  return { root: rootIdentity ?? null, identity };
}
*/
function observeUnverifiedMods(Installer, root) {
  let rootIdentity; let identity; let presence = 'unknown';
  try { rootIdentity = missingMetadataRootSnapshot(Installer, root); } catch {}
  try {
    identity = destinationSnapshot(Installer.inspectModsMetadataIdentity(root));
    if (rootIdentity && identity && sameRoot(rootIdentity, identity.root)) presence = 'present';
    else identity = null;
  } catch (error) {
    if (rootIdentity && error?.code === 'ENOENT') presence = 'absent';
  }
  return { root: rootIdentity ?? null, identity: identity ?? null, presence };
}
function sameDestination(a, b) {
  return a.root.dev === b.root.dev && a.root.ino === b.root.ino &&
    a.meta.dev === b.meta.dev && a.meta.ino === b.meta.ino;
}
function assertMetadataPreflight(before, observation, rootIdentity) {
  if (before?.root && !sameRoot(before.root, rootIdentity)) throw new Error('DESTINATION_IDENTITY_CHANGED');
  if (before?.presence === 'unknown') throw new Error('DESTINATION_IDENTITY_UNVERIFIABLE');
  if (before?.presence === 'absent' && observation !== null) throw new Error('DESTINATION_IDENTITY_CHANGED');
  if (before?.identity && (!observation || !sameDestination(before.identity, observation)))
    throw new Error('DESTINATION_IDENTITY_CHANGED');
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
function targetInfo(unavailable = false) {
  if (state.mode === 'unconfigured') return { mode: state.mode, path: '', scope: 'Choose your existing game Mods folder in Destination to enable file operations. The test workspace is optional.' };
  if (unavailable) {
    const selected = state.mode === 'sandbox' ? state.sandbox : state.modsRoot;
    return { mode: state.mode, path: typeof selected === 'string' ? selected : '', unavailable: true };
  }
  return { mode: state.mode, path: state.mode === 'sandbox' ? state.sandbox : state.modsRoot,
    backupPath:state.mode==='game-mods'?metadataForModsDirectory(state.modsRoot):'',
    scope: state.mode === 'sandbox' ? 'Test workspace only. These files are not loaded by the game.' :
      `Only CS1DevManager-<module> folders directly under Mods are managed. Retained backups: ${metadataForModsDirectory(state.modsRoot)}` };
}

export async function startBackend({ settings = settingsStore, input = process.stdin, output = process.stdout,
  Installer = IsolatedInstaller, serviceFactory = options => new DesktopService(options),
  createSandbox = () => fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-')) } = {}) {
  state = settings.load();
  let destinationUnavailable = false;
  // Retained initial probe before destination identity tracking: try { getInstaller(false, Installer); } catch { destinationUnavailable = true; }
  let destinationObservation;
  let missingRootObservation;
  let startupMetadataBefore;
  if (state.mode === 'game-mods') {
    // Retained metadata-only probe: try { startupMetadataBefore = destinationSnapshot(Installer.inspectModsMetadataIdentity(state.modsRoot)); } catch {}
    startupMetadataBefore = observeUnverifiedMods(Installer, state.modsRoot);
  }
  // Retained original identity probe: try { destinationObservation = destinationSnapshot(getInstaller(false, Installer)); } catch { destinationUnavailable = true; }
  try {
    destinationObservation = destinationSnapshot(getInstaller(false, Installer));
    if (state.mode === 'game-mods' && destinationObservation === null)
      missingRootObservation = missingMetadataRootSnapshot(Installer, state.modsRoot);
    if (state.mode === 'game-mods')
      assertMetadataPreflight(startupMetadataBefore, destinationObservation, destinationObservation?.root ?? missingRootObservation);
  } catch { destinationUnavailable = true; }
  // Preserve observations for the whole session, including across destination switches.
  const observedModsDestinations = [];
  const modsKey = root => process.platform === 'win32' ? path.resolve(root).toLowerCase() : path.resolve(root);
  /* Retained initial present-only history candidate; not executed.
  const rememberModsDestination = (root, observation) => {
    if (!observation) return;
    const key = modsKey(root);
    const previous = observedModsDestinations.find(entry => entry.key === key || sameRoot(entry.observation.root, observation.root));
    if (previous && !sameDestination(previous.observation, observation)) throw new Error('DESTINATION_IDENTITY_CHANGED');
    if (!previous) observedModsDestinations.push({ key, observation });
  };
  const findObservedModsDestination = root => {
    const key = modsKey(root);
    const byPath = observedModsDestinations.find(entry => entry.key === key);
    if (byPath || observedModsDestinations.length === 0) return byPath;
    const identity = missingMetadataRootSnapshot(Installer, root);
    return observedModsDestinations.find(entry => sameRoot(entry.observation.root, identity));
  };
  if (state.mode === 'game-mods' && destinationObservation) rememberModsDestination(state.modsRoot, destinationObservation);
  */
  const rememberModsDestination = (root, observation, absentRoot, allowInitialization = false) => {
    const identity = observation ? observation.root : absentRoot;
    if (!identity) throw new Error('DESTINATION_IDENTITY_UNVERIFIABLE');
    const key = modsKey(root);
    const previous = observedModsDestinations.find(entry => entry.key === key) ??
      observedModsDestinations.find(entry => sameRoot(entry.root, identity));
    if (previous && !sameRoot(previous.root, identity)) throw new Error('DESTINATION_IDENTITY_CHANGED');
    if (previous && previous.observation && (!observation || !sameDestination(previous.observation, observation)))
      throw new Error('DESTINATION_IDENTITY_CHANGED');
    if (previous && !previous.observation && observation && !allowInitialization)
      throw new Error('DESTINATION_IDENTITY_CHANGED');
    if (previous && !previous.observation && observation && allowInitialization)
      for (const entry of observedModsDestinations) if (sameRoot(entry.root, identity)) entry.observation = observation;
    if (!observedModsDestinations.some(entry => entry.key === key))
      observedModsDestinations.push({ key, root: identity, observation });
  };
  const findObservedModsDestination = root => {
    const key = modsKey(root);
    const byPath = observedModsDestinations.find(entry => entry.key === key);
    if (byPath || observedModsDestinations.length === 0) return byPath;
    const identity = missingMetadataRootSnapshot(Installer, root);
    return observedModsDestinations.find(entry => sameRoot(entry.root, identity));
  };
  if (!destinationUnavailable)
  if (state.mode === 'game-mods' && destinationObservation !== undefined)
    if (destinationObservation !== null || missingRootObservation)
    rememberModsDestination(state.modsRoot, destinationObservation, missingRootObservation);
  // Preserve the last verified sandbox identity even when another destination is selected.
  const observedSandboxes = [];
  const sandboxKey = root => process.platform === 'win32' ? path.resolve(root).toLowerCase() : path.resolve(root);
  const unverifiedSandboxes = new Set();
  const markUnverifiedSandbox = root => {
    if (typeof root === 'string' && path.isAbsolute(root)) unverifiedSandboxes.add(sandboxKey(root));
  };
  const rememberSandbox = (root, observation) => {
    if (!observation) throw new Error('DESTINATION_IDENTITY_UNVERIFIABLE');
    const key = sandboxKey(root);
    const prior = observedSandboxes.find(entry => entry.key === key) ??
      observedSandboxes.find(entry => sameRoot(entry.observation.root, observation.root));
    if (prior && !sameDestination(prior.observation, observation)) throw new Error('DESTINATION_IDENTITY_CHANGED');
    if (!observedSandboxes.some(entry => entry.key === key)) observedSandboxes.push({ key, observation });
  };
  if (state.mode === 'sandbox' && destinationObservation) rememberSandbox(state.sandbox, destinationObservation);
  if (state.mode === 'sandbox' && destinationUnavailable) markUnverifiedSandbox(state.sandbox);
  // A failed complete validation is unknown, never proof of absent metadata.
  const unverifiedModsDestinations = [];
  const rememberUnverifiedMods = (root, before) => {
    if (typeof root !== 'string' || !path.isAbsolute(root) || root.startsWith('\\\\')) return;
    const key = modsKey(root);
    if (unverifiedModsDestinations.some(entry => entry.key === key && !entry.resolved)) return;
    /* Retained metadata-only unknown observation; not executed.
    let after;
    try { after = destinationSnapshot(Installer.inspectModsMetadataIdentity(root)); } catch {}
    const identity = before && after && sameDestination(before, after) ? before : null;
    unverifiedModsDestinations.push({ key, identity, resolved: false });
    */
    const after = observeUnverifiedMods(Installer, root);
    const rootIdentity = before?.root && after.root && sameRoot(before.root, after.root) ? before.root : null;
    const identity = rootIdentity && before.identity && after.identity && sameDestination(before.identity, after.identity) ? before.identity : null;
    unverifiedModsDestinations.push({ key, root: rootIdentity, identity, resolved: false });
  };
  const findUnverifiedMods = root => {
    const active = unverifiedModsDestinations.filter(entry => !entry.resolved);
    const byPath = active.find(entry => entry.key === modsKey(root));
    if (byPath) return byPath;
    /* Retained metadata-only alias lookup; not executed.
    const bound = active.filter(entry => entry.identity);
    if (!bound.length) return undefined;
    const identity = missingMetadataRootSnapshot(Installer, root);
    return bound.find(entry => sameRoot(entry.identity.root, identity));
    */
    const bound = active.filter(entry => entry.root);
    if (!bound.length) return undefined;
    const identity = missingMetadataRootSnapshot(Installer, root);
    return bound.find(entry => sameRoot(entry.root, identity));
  };
  if (state.mode === 'game-mods' && destinationUnavailable)
    rememberUnverifiedMods(state.modsRoot, startupMetadataBefore);
  let startupWarning=settings.lastLoadResult?.warning;let startupLock=settings.lastLoadResult?.lockMayRemain;
  const pool = new GitHubRequestPool(makeGitHubApiRequest('KingsleyHK0323/Cities_Skylines_1_Modpack'));
  const send = value => {if(value.result?.settingsRecoveryCleared===true){startupWarning=undefined;startupLock=undefined;}if(value.result&&startupWarning)value={...value,result:{...value.result,settingsWarning:value.result.settingsWarning??startupWarning,settingsLockMayRemain:value.result.settingsLockMayRemain??startupLock}};output.write(JSON.stringify(localizeReply(value,state)) + '\n');};
  const assetTransport=makeAssetTransport('KingsleyHK0323/Cities_Skylines_1_Modpack');
  const bundles=new BundleSource({registry:moduleRegistry,request:pool.request.bind(pool),transport:assetTransport,inspectArchive:inspectZipBytes});
  const guardedInstaller = create => {
    if (destinationUnavailable) throw new Error('DESTINATION_UNAVAILABLE');
    // Retained pre-identity guard: try { return getInstaller(create, Installer); }
    try {
      // Retained original call: const installer = getInstaller(create, Installer, destinationObservation);
      const installer = getInstaller(create, Installer, destinationObservation, missingRootObservation);
      const current = destinationSnapshot(installer);
      if (destinationObservation === null && state.mode === 'game-mods') {
        const currentRoot = current === null ? missingMetadataRootSnapshot(Installer, state.modsRoot) : current.root;
        if (!sameRoot(missingRootObservation, currentRoot)) throw new Error('DESTINATION_IDENTITY_CHANGED');
      }
      if (destinationObservation !== undefined &&
          (destinationObservation === null ? current !== null && !create : current === null || !sameDestination(destinationObservation, current)))
        throw new Error('DESTINATION_IDENTITY_CHANGED');
      // Retained present-only observation: if (state.mode === 'game-mods' && current !== null) rememberModsDestination(state.modsRoot, current);
      if (state.mode === 'game-mods')
        rememberModsDestination(state.modsRoot, current, missingRootObservation, create && destinationObservation === null);
      if (state.mode === 'sandbox') rememberSandbox(state.sandbox, current);
      destinationObservation = current;
      if (current !== null) missingRootObservation = undefined;
      return installer;
    }
    catch (error) { destinationUnavailable = true; throw error; }
  };
  const service = serviceFactory({ request: pool.request.bind(pool), getInstaller: guardedInstaller, inspectPackage, assertGameClosed: gameClosed,
    targetReady: () => state.mode !== 'unconfigured' && !destinationUnavailable,
    resolvePolicy: module => resolveGitHubPolicy(module, pool.request.bind(pool)),
    assetTransport,loadBundle:()=>bundles.load(), beginRefresh: () => pool.beginRefresh(),
    rateStatus: () => pool.status(), progress: text => send({ event: 'progress', text }) });
  service.provider=defaultSourceProvider;
  const reader = readline.createInterface({ input, crlfDelay: Infinity }); let queue = Promise.resolve();
  reader.on('line', line => {
    queue = queue.then(async () => {
      let command;
      let retainedSandboxPath;
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
          let confirmedInstaller; let confirmedObservation;
          let confirmedMissingRoot;
          let confirmedUnverifiedMods;
          if (command.mode === 'game-mods') {
            if (typeof command.path !== 'string' || !path.isAbsolute(command.path) || command.path.startsWith('\\\\') ||
              /[\x00-\x1f]/.test(command.path) || command.path === path.parse(command.path).root) throw new Error('INVALID_MODS_DIRECTORY');
            let selectionMetadataBefore; let priorModsDestination;
            // Retained metadata-only probe: try { selectionMetadataBefore = destinationSnapshot(Installer.inspectModsMetadataIdentity(command.path)); } catch {}
            selectionMetadataBefore = observeUnverifiedMods(Installer, command.path);
            try {
            // Retained prior declaration: const priorModsDestination = findObservedModsDestination(command.path);
            priorModsDestination = findObservedModsDestination(command.path);
            const unverified = findUnverifiedMods(command.path);
            if (unverified && !priorModsDestination) {
              if (!unverified.identity) throw new Error('DESTINATION_IDENTITY_UNVERIFIABLE');
              const currentRoot = missingMetadataRootSnapshot(Installer, command.path);
              if (!sameRoot(unverified.identity.root, currentRoot)) throw new Error('DESTINATION_IDENTITY_CHANGED');
              confirmedInstaller = Installer.forModsDirectory(command.path, { confirmed: true, create: false, expectedPresence: 'existing' });
              if (!confirmedInstaller || !sameDestination(unverified.identity, destinationSnapshot(confirmedInstaller)))
                throw new Error('DESTINATION_IDENTITY_CHANGED');
              confirmedUnverifiedMods = unverified;
            }
            // Read-only validation. No game-folder metadata is created until an explicit operation.
            // Retained prior validation call: Installer.forModsDirectory(command.path, { confirmed: true, create: false });
            /* Retained present-only re-selection candidate; not executed.
            if (priorModsDestination) {
              confirmedInstaller = Installer.forModsDirectory(command.path, { confirmed: true, create: false, expectedPresence: 'existing' });
              if (!confirmedInstaller || !sameDestination(priorModsDestination.observation, destinationSnapshot(confirmedInstaller)))
                throw new Error('DESTINATION_IDENTITY_CHANGED');
            }
            */
            if (priorModsDestination) {
              const currentRoot = missingMetadataRootSnapshot(Installer, command.path);
              if (!sameRoot(priorModsDestination.root, currentRoot)) throw new Error('DESTINATION_IDENTITY_CHANGED');
              confirmedInstaller = Installer.forModsDirectory(command.path, { confirmed: true, create: false,
                expectedPresence: priorModsDestination.observation ? 'existing' : 'absent' });
              if (priorModsDestination.observation ? !confirmedInstaller ||
                  !sameDestination(priorModsDestination.observation, destinationSnapshot(confirmedInstaller)) : confirmedInstaller !== null)
                throw new Error('DESTINATION_IDENTITY_CHANGED');
            }
            // Retained original choice guard: if (!priorModsDestination)
            if (!priorModsDestination && !confirmedUnverifiedMods)
            confirmedInstaller = Installer.forModsDirectory(command.path, { confirmed: true, create: false });
            const selectedObservation = destinationSnapshot(confirmedInstaller);
            const selectedRoot = selectedObservation?.root ?? missingMetadataRootSnapshot(Installer, command.path);
            assertMetadataPreflight(selectionMetadataBefore, selectedObservation, selectedRoot);
            if (confirmedInstaller !== null) rememberModsDestination(command.path, destinationSnapshot(confirmedInstaller));
            if (confirmedInstaller === null) {
              confirmedMissingRoot = missingMetadataRootSnapshot(Installer, command.path);
              if (!sameRoot(selectedRoot, confirmedMissingRoot)) throw new Error('DESTINATION_IDENTITY_CHANGED');
              rememberModsDestination(command.path, null, confirmedMissingRoot);
            }
            } catch (error) {
              if (!priorModsDestination) rememberUnverifiedMods(command.path, selectionMetadataBefore);
              throw error;
            }
          }
          // Retained review candidate; deliberately not executed:
          // if (command.mode === 'sandbox' && state.sandbox) new Installer(state.sandbox);
          // Retained conservative review guard; deliberately not executed:
          // if (command.mode === 'sandbox' && destinationUnavailable && state.sandbox) throw new Error('DESTINATION_UNAVAILABLE');
          // Retained prior validation call: if (command.mode === 'sandbox' && state.sandbox) Installer.inspectSandbox(state.sandbox);
          // Retained prior sandbox probe: if (command.mode === 'sandbox' && state.sandbox) confirmedInstaller = Installer.inspectSandbox(state.sandbox);
          if (command.mode === 'sandbox' && state.sandbox) {
            const key = sandboxKey(state.sandbox);
            const known = observedSandboxes.some(entry => entry.key === key);
            if (!known && unverifiedSandboxes.has(key)) throw new Error('DESTINATION_IDENTITY_UNVERIFIABLE');
            // Retained prior inspection-only guard: try { confirmedInstaller = Installer.inspectSandbox(state.sandbox); }
            try {
              confirmedInstaller = Installer.inspectSandbox(state.sandbox);
              rememberSandbox(state.sandbox, destinationSnapshot(confirmedInstaller));
            }
            catch (error) { if (!known) markUnverifiedSandbox(state.sandbox); throw error; }
          }
          // Retained prior unguarded identity check: if (command.mode === 'sandbox' && state.sandbox) rememberSandbox(state.sandbox, destinationSnapshot(confirmedInstaller));
          const saved=settings.update(current=>{
            const destination=value=>JSON.stringify([value.mode,value.sandbox??null,value.modsRoot??null]);
            if(destination(current)!==destination(state))throw new Error('DESTINATION_SELECTION_CHANGED_EXTERNALLY');
            let sandbox = current.sandbox;
            if (command.mode === 'sandbox' && !sandbox) {
              sandbox = createSandbox();
              retainedSandboxPath = sandbox;
              // Retained prior initialization call: new Installer(sandbox);
              confirmedInstaller = new Installer(sandbox);
              rememberSandbox(sandbox, destinationSnapshot(confirmedInstaller));
            }
            confirmedObservation = destinationSnapshot(confirmedInstaller);
            // Retained prior root probe: if (command.mode === 'game-mods' && confirmedObservation === null)
            //   confirmedMissingRoot = missingMetadataRootSnapshot(Installer, path.resolve(command.path));
            if (command.mode === 'game-mods' && confirmedObservation === null) {
              const currentRoot = missingMetadataRootSnapshot(Installer, path.resolve(command.path));
              if (confirmedMissingRoot && !sameRoot(confirmedMissingRoot, currentRoot)) throw new Error('DESTINATION_IDENTITY_CHANGED');
              confirmedMissingRoot = currentRoot;
            }
            return {...current,mode:command.mode,
              sandbox,
              modsRoot:command.mode==='game-mods'?path.resolve(command.path):current.modsRoot};
          });
          if (confirmedUnverifiedMods) confirmedUnverifiedMods.resolved = true;
          missingRootObservation = confirmedMissingRoot;
          destinationObservation = confirmedObservation;
          state=saved.state;retainedSandboxPath=undefined;destinationUnavailable=false;service.sources.clear();result={rows:service.rows(),providers:sourceProviders,settingsWarning:saved.warning,settingsLockMayRemain:saved.lockMayRemain,settingsRecoveryCleared:!saved.warning&&!saved.lockMayRemain};
        } else throw new Error('UNKNOWN_COMMAND');
        send({ id: command.id, ok: true, result: { ...result, target: targetInfo(destinationUnavailable) } });
      } catch (error) {
        const code = typeof error.message === 'string' && /^[A-Z0-9_]+$/.test(error.message) ? error.message : 'OPERATION_REFUSED_INSPECT_LOCAL_FILES';
        send({ id: command?.id ?? null, ok: false, error: code, result: { rows: service.rows(), target: targetInfo(destinationUnavailable), ...(typeof retainedSandboxPath === 'string' ? { retainedSandboxPath } : {}) } });
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

/*
Retained pre-recovery implementation lines for review; not executed.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
function getInstaller(create) {
  return state.mode === 'sandbox' ? new IsolatedInstaller(state.sandbox) :
    IsolatedInstaller.forModsDirectory(state.modsRoot, { confirmed: true, create });
function targetInfo() {
export async function startBackend({settings=settingsStore}={}) {
  state = settings.load(); getInstaller(false);
  const send = value => {if(value.result?.settingsRecoveryCleared===true){startupWarning=undefined;startupLock=undefined;}if(value.result&&startupWarning)value={...value,result:{...value.result,settingsWarning:value.result.settingsWarning??startupWarning,settingsLockMayRemain:value.result.settingsLockMayRemain??startupLock}};process.stdout.write(JSON.stringify(localizeReply(value,state)) + '\n');};
  const service = new DesktopService({ request: pool.request.bind(pool), getInstaller, inspectPackage, assertGameClosed: gameClosed,
    targetReady: () => state.mode !== 'unconfigured',
  const input = readline.createInterface({ input: process.stdin, crlfDelay: Infinity }); let queue = Promise.resolve();
  input.on('line', line => {
            IsolatedInstaller.forModsDirectory(command.path, { confirmed: true, create: false });
          const saved=settings.update(current=>({...current,mode:command.mode,
            sandbox: command.mode === 'sandbox' && !current.sandbox ? fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-')) : current.sandbox,
            modsRoot:command.mode==='game-mods'?path.resolve(command.path):current.modsRoot}));
          state=saved.state;service.sources.clear();result={rows:service.rows(),providers:sourceProviders,settingsWarning:saved.warning,settingsLockMayRemain:saved.lockMayRemain,settingsRecoveryCleared:!saved.warning&&!saved.lockMayRemain};
        send({ id: command.id, ok: true, result: { ...result, target: targetInfo() } });
        send({ id: command?.id ?? null, ok: false, error: code, result: { rows: service.rows(), target: targetInfo() } });
*/
