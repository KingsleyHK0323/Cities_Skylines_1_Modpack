// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash, generateKeyPairSync, sign } from 'node:crypto';
import { createRequire } from 'node:module';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';
import { safeRelative, verifyPackage, installationPlan, uninstallPlan, updateStatus, coexistenceWarning, catalogStatus } from './core.mjs';
const bytes = Buffer.from('synthetic fixture, not a game DLL');
const sha256 = createHash('sha256').update(bytes).digest('hex');
const pkg = () => ({ moduleId: 'example', buildId: 'build-a', files: [{ path: 'mod.dll', bytes, sha256 }] });

test('future portable packages pin official Node 24.22.2 and preserve legal reference bytes',async()=>{
  const script=readFileSync('desktop/Build-Portable.ps1','utf8');assert.ok(script.includes("$version -ne 'v24.22.2'"));
  assert.ok(script.includes('1c331340fa6cac5e4294725959070a143fa0507195a78ad77eed7943f719aac9'));
  const license=readFileSync('desktop/Node-v24.22.2-LICENSE.txt');assert.equal(createHash('sha256').update(license).digest('hex'),'5888dbb9a1d2b18f2c3e6c5f6af1b39de658372b402a0577b002777f14c62ace');
  assert.match(readFileSync('desktop/Node-v24.22.2-SHASUMS256.txt','utf8'),/^1c331340fa6cac5e4294725959070a143fa0507195a78ad77eed7943f719aac9  win-x64\/node\.exe$/m);
  const {execFileSync}=await import('node:child_process');const attributes=execFileSync('git',['check-attr','text','--','desktop/Node-v24.22.2-LICENSE.txt','desktop/Node-v24.22.2-SHASUMS256.txt'],{encoding:'utf8',windowsHide:true});assert.ok(attributes.trim().split(/\r?\n/).every(line=>line.endsWith(': unset')));
  for(const file of ['desktop/Node-v24.22.2-LICENSE.txt','desktop/Node-v24.22.2-SHASUMS256.txt'])assert.equal(execFileSync('git',['hash-object',`--path=${file}`,file],{encoding:'utf8',windowsHide:true}).trim(),execFileSync('git',['hash-object','--no-filters',file],{encoding:'utf8',windowsHide:true}).trim());
  const notice=readFileSync('desktop/THIRD-PARTY.md','utf8').replace(/\r\n?/g,'\n');assert.equal(createHash('sha256').update(notice,'utf8').digest('hex'),'5767c6fbab782df8d7983972d71eae1ee12e04423098380565e246621a350c93');
});

test('portable recipe rejects untrusted or old runtime before execution or creating package output',{skip:process.platform!=='win32'},async()=>{
  const fs=await import('node:fs');const os=await import('node:os');const path=await import('node:path');const {execFileSync}=await import('node:child_process');
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'cs1-dev-manager-test-'));const fake=path.join(root,'fake-node.cmd');fs.writeFileSync(fake,'@echo off\r\necho executed> "%~dp0executed.txt"\r\necho v24.22.2\r\n');
  const candidates=[fake,...(process.version!=='v24.22.2'?[process.execPath]:[])];
  for(const [index,nodePath] of candidates.entries()){
    const output=path.join(root,`package-${index}`);let failure;
    try{execFileSync('pwsh',['-NoLogo','-NoProfile','-NonInteractive','-File',path.resolve('desktop/Build-Portable.ps1'),'-OutputDirectory',output,'-NodePath',nodePath],{windowsHide:true,encoding:'utf8',timeout:15000,stdio:['pipe','pipe','pipe']});}catch(error){failure=error;}
    assert.ok(failure);assert.match(String(failure.stderr),/Runtime does not match the official Node win-x64 checksum/);assert.equal(fs.existsSync(output),false);
  }
  assert.equal(fs.existsSync(path.join(root,'executed.txt')),false);
});

test('presentation preferences preserve legacy destinations and unknown fields across all language/theme switches',async()=>{
  const {presentationPreferences,mergePresentationPreferences}=await import('./preferences.mjs');
  const legacy={schema:1,mode:'game-mods',modsRoot:'Synthetic/Mods',sandbox:null,future:{keep:true},preferences:{futureOption:'retain'}};
  assert.deepEqual(presentationPreferences(legacy),{language:'en',theme:'system'});
  let state=legacy;
  for(const language of ['en','zh-Hant','zh-Hans','ja'])for(const theme of ['system','light','dark']){
    state=mergePresentationPreferences(state,{language,theme});const reopened=JSON.parse(JSON.stringify(state));assert.deepEqual(presentationPreferences(reopened),{language,theme});
    assert.equal(reopened.modsRoot,legacy.modsRoot);assert.deepEqual(reopened.future,legacy.future);assert.equal(reopened.preferences.futureOption,'retain');
  }
  assert.deepEqual(presentationPreferences(legacy),{language:'en',theme:'system'});
  assert.throws(()=>mergePresentationPreferences(state,{language:'bad'}));assert.throws(()=>mergePresentationPreferences(state,{theme:'bad'}));
});

test('desktop four-language catalog has complete translations and identical format parameters',async()=>{
  const catalog=JSON.parse(readFileSync('translations.json','utf8'));
  assert.deepEqual(Object.keys(catalog.languages),['en','zh-Hant','zh-Hans','ja']);
  const keys=Object.keys(catalog.languages.en).sort();assert.ok(keys.length>=70);
  for(const [language,values] of Object.entries(catalog.languages)){
    assert.deepEqual(Object.keys(values).sort(),keys,language);
    for(const key of keys){assert.ok(typeof values[key]==='string'&&values[key].trim(),`${language}:${key}`);assert.deepEqual(values[key].match(/\{[0-9]+\}/g)?.sort()??[],catalog.languages.en[key].match(/\{[0-9]+\}/g)?.sort()??[],`${language}:${key}`);}
  }
});

test('desktop translation keys and flat package imports are covered by offline resources',()=>{
  const catalog=JSON.parse(readFileSync('translations.json','utf8'));const source=readFileSync('desktop/DevManager.cs','utf8');
  for(const match of source.matchAll(/\bT\("([^"\n]+)"/g))assert.ok(Object.hasOwn(catalog.languages.en,match[1]),match[1]);
  const script=readFileSync('desktop/Build-Portable.ps1','utf8');const pending=['desktop-backend.mjs'];const visited=new Set();
  while(pending.length){const file=pending.pop();if(visited.has(file))continue;visited.add(file);assert.ok(script.includes(`'${file}'`),file);const content=readFileSync(file,'utf8');for(const match of content.matchAll(/from\s*['"]\.\/([^'"]+)['"]/g))if(match[1].endsWith('.mjs'))pending.push(match[1]);}
  assert.ok(script.includes("'translations.json'"));assert.match(script,/\/codepage:65001/);assert.match(readFileSync('desktop/Build-Native.ps1','utf8'),/\/codepage:65001/);
});

test('backend-facing messages localize all four languages with preserved dynamic names, paths and diagnostics',async()=>{
  const {translate,localizeReply,localizeProgress}=await import('./localization.mjs');
  for(const language of ['en','zh-Hant','zh-Hans','ja']){
    const state={preferences:{language,theme:'dark'}};const name='Module {1} / 模組';const file='builds/101-1/example.dll';
    assert.equal(localizeProgress(language,`Downloading ${name}: ${file}`),translate(language,'Downloading {0}: {1}',name,file));
    assert.equal(localizeProgress(language,`install: ${name}`),translate(language,'Installing {0}',name));
    const reply=localizeReply({ok:false,error:'CLOSE_GAME_BEFORE_CHANGING_FILES',result:{rows:[{configuration:'Awaiting publisher configuration',development:'Unconfirmed',reason:'A pinned artifact manifest exists. Successful source run and update order still require verification before installation.'}],target:{mode:'game-mods',path:'Synthetic/Mods',backupPath:'Synthetic/Backups'},results:[{moduleId:'example',status:'failed',reason:'unknown diagnostic'}]}},state);
    assert.equal(reply.errorText,translate(language,'Close the game before changing module files.'));assert.equal(reply.result.target.path,'Synthetic/Mods');assert.ok(reply.result.target.scopeText.includes('Synthetic/Backups'));
    assert.equal(reply.result.rows[0].configurationText,translate(language,'Awaiting publisher configuration'));
    assert.equal(reply.result.rows[0].reasonText,translate(language,'Candidate found. Full source and file verification is required before installation.'));
    assert.equal(reply.result.rows[0].reasonDiagnostic,'');
    assert.equal(reply.result.results[0].reasonDiagnostic,'unknown diagnostic');
    const unknown=localizeReply({ok:false,error:'FUTURE_ERROR_CODE'},state);assert.equal(unknown.error,'FUTURE_ERROR_CODE');assert.equal(unknown.errorText,translate(language,'Operation could not finish. Review details before retrying.'));
  }
});

async function backendSettingsFixture(){
  const fs=await import('node:fs');const os=await import('node:os');const path=await import('node:path');const {execFileSync}=await import('node:child_process');
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'cs1-dev-manager-test-'));
  for(const file of fs.readdirSync('.').filter(name=>name.endsWith('.mjs')&&!name.endsWith('.test.mjs')||name==='translations.json'))fs.copyFileSync(path.resolve(file),path.join(root,file));
  const statePath=path.join(root,'.desktop-state.json');const legacy={schema:1,mode:'unconfigured',sandbox:null,modsRoot:'preserved-synthetic-path',unknownTop:{retain:true},preferences:{futureOption:'keep'}};
  fs.writeFileSync(statePath,JSON.stringify(legacy));
  const run=(commands,entry='desktop-backend.mjs')=>execFileSync(process.execPath,[path.join(root,entry)],{input:commands.map(command=>JSON.stringify(command)).join('\n')+'\n',encoding:'utf8',windowsHide:true,timeout:15000}).trim().split(/\r?\n/).map(line=>JSON.parse(line));
  return {root,statePath,legacy,run,fs};
}

test('real backend serializes preferences with destination, preserves unknown data and restores settings after restart',async()=>{
  const f=await backendSettingsFixture();const replies=f.run([{id:1,command:'status'},{id:2,command:'preferences',preferences:{language:'ja',theme:'dark'}},{id:3,command:'destination',mode:'sandbox',confirmed:true},{id:4,command:'preferences',preferences:{language:'zh-Hant',theme:'light'}},{id:5,command:'preferences',preferences:{language:'invalid'}}]);
  assert.deepEqual(replies.map(reply=>reply.id),[1,2,3,4,5]);assert.deepEqual(replies[0].result.preferences,{language:'en',theme:'system'});
  assert.deepEqual(replies[1].result.preferences,{language:'ja',theme:'dark'});assert.match(replies[1].result.target.scopeText,/フォルダー/);
  assert.equal(replies[1].result.settingsRecoveryCleared,true);assert.notEqual(replies[4].result.settingsRecoveryCleared,true);
  assert.equal(replies[2].result.target.mode,'sandbox');assert.deepEqual(replies[3].result.preferences,{language:'zh-Hant',theme:'light'});assert.equal(replies[4].ok,false);assert.match(replies[4].errorText,/設定/);
  const saved=JSON.parse(f.fs.readFileSync(f.statePath,'utf8'));assert.equal(saved.mode,'sandbox');assert.ok(f.fs.existsSync(saved.sandbox));assert.equal(saved.modsRoot,f.legacy.modsRoot);assert.deepEqual(saved.unknownTop,f.legacy.unknownTop);assert.equal(saved.preferences.futureOption,'keep');
  const reopened=f.run([{id:6,command:'status'}])[0];assert.deepEqual(reopened.result.preferences,{language:'zh-Hant',theme:'light'});assert.equal(reopened.result.target.path,saved.sandbox);
});

test('backend reports preference save failure while preserving the original settings',{skip:process.platform!=='win32'},async()=>{
  const f=await backendSettingsFixture();f.fs.chmodSync(f.statePath,0o444);
  const reply=f.run([{id:1,command:'preferences',preferences:{language:'ja',theme:'dark'}}])[0];assert.equal(reply.ok,false);assert.equal(reply.error,'PREFERENCES_SAVE_FAILED');
  assert.deepEqual(JSON.parse(f.fs.readFileSync(f.statePath,'utf8')),f.legacy);assert.deepEqual(reply.result.preferences,{language:'en',theme:'system'});
});

test('atomic settings preserve old bytes and destination after partial staging or rename failures and reject oversized saves',async()=>{
  const {SettingsStore}=await import('./settings-store.mjs');const f=await backendSettingsFixture();const original=f.fs.readFileSync(f.statePath);
  for(const point of ['after-partial-stage','before-rename']){
    const store=new SettingsStore(f.statePath,{fault:name=>{if(name===point)throw new Error('injected failure');}});
    assert.throws(()=>store.update(current=>({...current,mode:'sandbox',sandbox:'new-uncommitted-destination'})));
    assert.deepEqual(f.fs.readFileSync(f.statePath),original);assert.equal(new SettingsStore(f.statePath).load().mode,'unconfigured');
  }
  assert.throws(()=>new SettingsStore(f.statePath).update(current=>({...current,huge:'x'.repeat(9000)})),/LOCAL_STATE_OVERSIZED/);
  assert.deepEqual(f.fs.readFileSync(f.statePath),original);
});

test('backend preserves its reported destination after an actual partial-stage failure',async()=>{
  const f=await backendSettingsFixture();const path=await import('node:path');const original=f.fs.readFileSync(f.statePath);
  f.fs.writeFileSync(path.join(f.root,'fault-backend.mjs'),`// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager\nimport {startBackend} from './desktop-backend.mjs';import {SettingsStore} from './settings-store.mjs';await startBackend({settings:new SettingsStore(${JSON.stringify(f.statePath)},{fault:point=>{if(point==='after-partial-stage')throw new Error('injected');}})});`);
  const replies=f.run([{id:1,command:'destination',mode:'sandbox',confirmed:true},{id:2,command:'status'}],'fault-backend.mjs');
  assert.equal(replies[0].ok,false);assert.equal(replies[0].result.target.mode,'unconfigured');assert.equal(replies[1].result.target.mode,'unconfigured');assert.deepEqual(f.fs.readFileSync(f.statePath),original);
  assert.ok(f.fs.readdirSync(f.root).some(file=>file.startsWith('local-state-stage-')));
});

test('8185-byte legacy settings reject oversized preferences and destinations without losing prior state',async()=>{
  const f=await backendSettingsFixture();const old={...f.legacy,padding:''};old.padding='x'.repeat(8185-Buffer.byteLength(JSON.stringify(old)));const original=Buffer.from(JSON.stringify(old));assert.equal(original.length,8185);f.fs.writeFileSync(f.statePath,original);
  const replies=f.run([{id:1,command:'preferences',preferences:{language:'ja',theme:'dark'}},{id:2,command:'destination',mode:'sandbox',confirmed:true},{id:3,command:'status'}]);
  assert.equal(replies[0].error,'LOCAL_STATE_OVERSIZED');assert.equal(replies[1].error,'LOCAL_STATE_OVERSIZED');assert.equal(replies[1].result.target.mode,'unconfigured');assert.equal(replies[2].result.target.mode,'unconfigured');assert.deepEqual(replies[2].result.preferences,{language:'en',theme:'system'});assert.deepEqual(f.fs.readFileSync(f.statePath),original);
});

test('settings atomic commit warnings preserve the committed outcome and block unsafe lock reuse',async()=>{
  const {SettingsStore}=await import('./settings-store.mjs');const f=await backendSettingsFixture();
  const committed=new SettingsStore(f.statePath,{fault:point=>{if(point==='after-rename')throw new Error('post-commit fault');}}).update(current=>({...current,committed:true}));
  assert.equal(committed.state.committed,true);assert.equal(committed.warning,'LOCAL_STATE_COMMITTED_REVIEW');assert.equal(new SettingsStore(f.statePath).load().committed,true);
  const warning=new SettingsStore(f.statePath,{fault:point=>{if(point==='before-lock-release')throw new Error('release failed');}}).update(current=>({...current,second:true}));
  assert.equal(warning.state.second,true);assert.equal(warning.warning,'LOCAL_STATE_COMMITTED_REVIEW');assert.throws(()=>new SettingsStore(f.statePath).update(current=>current),/LOCAL_STATE_BUSY/);
});

test('settings preserve both precommit and lock-release failures with explicit retained-lock recovery state',async()=>{
  const {SettingsStore}=await import('./settings-store.mjs');const f=await backendSettingsFixture();const original=f.fs.readFileSync(f.statePath);
  const operation=new Error('operation failed'),release=new Error('lock release failed');
  const store=new SettingsStore(f.statePath,{fault:point=>{if(point==='after-partial-stage')throw operation;if(point==='before-lock-release')throw release;}});
  let failure;try{store.update(current=>({...current,changed:true}));}catch(error){failure=error;}
  assert.equal(failure.message,'LOCAL_STATE_RECOVERY_REQUIRED');assert.equal(failure.lockMayRemain,true);assert.equal(failure.recoveryRequired,true);assert.equal(failure.errors[0],operation);assert.equal(failure.errors[1],release);
  assert.deepEqual(f.fs.readFileSync(f.statePath),original);assert.ok(f.fs.existsSync(`${f.statePath}.lock`));
});

test('settings lock identity failure closes its descriptor and retains the unverified lock',async()=>{
  const {SettingsStore}=await import('./settings-store.mjs');const f=await backendSettingsFixture();const original=f.fs.readFileSync(f.statePath);const identityError=new Error('fstat failed');let lockFd;let closed=false;
  const io={...f.fs,fstatSync:fd=>{lockFd=fd;throw identityError;},closeSync:fd=>{if(fd===lockFd)closed=true;return f.fs.closeSync(fd);}};
  let failure;try{new SettingsStore(f.statePath,{io}).update(current=>({...current,changed:true}));}catch(error){failure=error;}
  assert.equal(closed,true);assert.equal(failure.message,'LOCAL_STATE_RECOVERY_REQUIRED');assert.equal(failure.lockMayRemain,true);assert.equal(failure.errors[0],identityError);assert.ok(f.fs.existsSync(`${f.statePath}.lock`));assert.deepEqual(f.fs.readFileSync(f.statePath),original);
});

test('first settings creation exposes committed-lock warnings through backend status',async()=>{
  const f=await backendSettingsFixture();const path=await import('node:path');const file=path.join(f.root,'fresh-state.json');
  f.fs.writeFileSync(path.join(f.root,'fresh-backend.mjs'),`// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager\nimport {startBackend} from './desktop-backend.mjs';import {SettingsStore} from './settings-store.mjs';await startBackend({settings:new SettingsStore(${JSON.stringify(file)},{fault:point=>{if(point==='before-lock-release')throw new Error('release failed');}})});`);
  const replies=f.run([{id:1,command:'status'},{id:2,command:'preferences',preferences:{language:'ja'}}],'fresh-backend.mjs');
  assert.equal(replies[0].result.settingsWarning,'LOCAL_STATE_COMMITTED_REVIEW');assert.equal(replies[0].result.settingsLockMayRemain,true);assert.match(replies[0].result.settingsWarningText,/lock may remain/);assert.equal(replies[1].error,'LOCAL_STATE_BUSY');assert.match(replies[1].errorText,/retained recovery data/);assert.ok(f.fs.existsSync(`${file}.lock`));
});

test('startup recovery warning persists in ordinary replies and clears only after a verified clean save',async()=>{
  const f=await backendSettingsFixture();const path=await import('node:path');const fresh=path.join(f.root,'warning-state.json');
  f.fs.writeFileSync(path.join(f.root,'warning-backend.mjs'),`// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager\nimport {startBackend} from './desktop-backend.mjs';import {SettingsStore} from './settings-store.mjs';let first=true;await startBackend({settings:new SettingsStore(${JSON.stringify(fresh)},{fault:point=>{if(first&&point==='after-rename'){first=false;throw new Error('post-commit');}}})});`);
  const replies=f.run([{id:1,command:'status'},{id:2,command:'status'},{id:3,command:'preferences',preferences:{theme:'dark'}},{id:4,command:'status'}],'warning-backend.mjs');
  assert.equal(replies[0].result.settingsWarning,'LOCAL_STATE_COMMITTED_REVIEW');assert.equal(replies[1].result.settingsWarning,'LOCAL_STATE_COMMITTED_REVIEW');assert.equal(replies[2].result.settingsRecoveryCleared,true);assert.equal(replies[2].result.settingsWarning,undefined);assert.equal(replies[3].result.settingsWarning,undefined);
});

test('first-start process failures preserve safe recovery codes for identity and dual-write cleanup faults',async()=>{
  const path=await import('node:path');const {execFileSync}=await import('node:child_process');
  for(const kind of ['identity','dual']){
    const f=await backendSettingsFixture();const fresh=path.join(f.root,'new-state.json');const entry=path.join(f.root,'startup-fault.mjs');
    const options=kind==='identity'?`io:{...fs,fstatSync:()=>{throw new Error('identity failure');}}`:`fault:point=>{if(['after-partial-stage','before-lock-release'].includes(point))throw new Error(point);}`;
    f.fs.writeFileSync(entry,`// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager\nimport fs from 'node:fs';import {startBackend,startupFailureReply} from './desktop-backend.mjs';import {SettingsStore} from './settings-store.mjs';await startBackend({settings:new SettingsStore(${JSON.stringify(fresh)},{${options}})}).catch(error=>{process.stdout.write(JSON.stringify(startupFailureReply(error))+'\\n');process.exitCode=1;});`);
    let output;try{execFileSync(process.execPath,[entry],{encoding:'utf8',windowsHide:true,timeout:10000});assert.fail('startup must fail');}catch(error){assert.equal(error.status,1);output=error.stdout;}
    const reply=JSON.parse(output.trim());assert.equal(reply.error,'LOCAL_STATE_RECOVERY_REQUIRED');assert.equal(reply.lockMayRemain,true);assert.match(reply.errorText,/recovery lock may remain/);assert.ok(f.fs.existsSync(`${fresh}.lock`));assert.equal(f.fs.existsSync(fresh),false);
  }
});

test('startup recovery whitelist is localized while unknown failures reveal no private diagnostics',async()=>{
  const {startupFailureReply}=await import('./desktop-backend.mjs');const {translate}=await import('./localization.mjs');
  for(const language of ['en','zh-Hant','zh-Hans','ja']){
    const state={preferences:{language,theme:'system'}};
    assert.equal(startupFailureReply(new Error('LOCAL_STATE_RECOVERY_REQUIRED'),state).errorText,translate(language,'Settings were not saved. A recovery lock may remain. Inspect it before retrying; no lock was automatically removed.'));
    const unknown=startupFailureReply(new Error('private/path/token'),state);assert.equal(unknown.error,'LOCAL_STATE_UNAVAILABLE');assert.ok(!JSON.stringify(unknown).includes('private/path/token'));
  }
});

test('native recovery notice survives failures, module selection and normal updates until verified clearance',{skip:process.platform!=='win32'},async()=>{
  const fixture=await buildNativeFixtures();const {execFileSync}=await import('node:child_process');const path=await import('node:path');const {translate}=await import('./localization.mjs');
  const messages=[{ok:true,result:{settingsWarning:'LOCAL_STATE_COMMITTED_REVIEW',settingsLockMayRemain:true}},
    {ok:false,error:'SOURCE_UNAVAILABLE'},{event:'module-selection'},{ok:true,result:{rows:[]}},{ok:false,error:'LOCAL_STATE_BUSY'},{ok:true,result:{settingsRecoveryCleared:'true'}},{ok:true,result:{settingsRecoveryCleared:true}}];
  for(const language of ['en','zh-Hant','zh-Hans','ja']){
    const reply=JSON.parse(execFileSync(fixture.helper,['--recovery-probe',path.resolve('translations.json'),language,JSON.stringify(messages)],{windowsHide:true,encoding:'utf8'}));
    assert.equal(reply.ok,true);assert.deepEqual(reply.states.map(state=>state.active),[true,true,true,true,true,true,false]);
    assert.equal(reply.states[3].text,translate(language,'Recovery / lock warning: {0}',translate(language,'Recovery data or a settings lock may remain. Inspect before retrying; no automatic recovery is claimed.')));
  }
});

test('settings reads reject large files before reading and cap growth-race reads at 8193 bytes',async()=>{
  const {SettingsStore}=await import('./settings-store.mjs');const f=await backendSettingsFixture();let calls=0;
  f.fs.writeFileSync(f.statePath,Buffer.alloc(8193,32));const rejectIo={...f.fs,readSync:()=>{calls++;throw new Error('must not read');}};
  assert.throws(()=>new SettingsStore(f.statePath,{io:rejectIo}).load(),/LOCAL_STATE_OVERSIZED/);assert.equal(calls,0);
  f.fs.writeFileSync(f.statePath,JSON.stringify(f.legacy));let appended=false;let maximum=0;let closed=false;
  const growthIo={...f.fs,readSync:(fd,buffer,offset,length,position)=>{maximum=Math.max(maximum,length);const count=f.fs.readSync(fd,buffer,offset,length,position);if(!appended){appended=true;f.fs.appendFileSync(f.statePath,Buffer.alloc(9000,32));}return count;},closeSync:fd=>{closed=true;return f.fs.closeSync(fd);}};
  assert.throws(()=>new SettingsStore(f.statePath,{io:growthIo}).load(),/LOCAL_STATE_OVERSIZED/);assert.ok(maximum<=8193);assert.equal(closed,true);
});

test('an App changing only preferences refuses a destination changed by another live instance',async()=>{
  const f=await backendSettingsFixture();const path=await import('node:path');const {execFile}=await import('node:child_process');
  const a=path.join(f.root,'SyntheticModsA'),b=path.join(f.root,'SyntheticModsB');f.fs.mkdirSync(a);f.fs.mkdirSync(b);f.fs.writeFileSync(f.statePath,JSON.stringify({...f.legacy,mode:'game-mods',modsRoot:a}));
  const child=execFile(process.execPath,[path.join(f.root,'desktop-backend.mjs')],{windowsHide:true,timeout:15000});const waiters=new Map();let buffer='';
  child.stdout.on('data',chunk=>{buffer+=chunk.toString();let newline;while((newline=buffer.indexOf('\n'))>=0){const reply=JSON.parse(buffer.slice(0,newline));buffer=buffer.slice(newline+1);const waiter=waiters.get(reply.id);if(waiter){waiters.delete(reply.id);waiter(reply);}}});
  const request=command=>new Promise(resolve=>{waiters.set(command.id,resolve);child.stdin.write(JSON.stringify(command)+'\n');});
  const closed=new Promise((resolve,reject)=>{child.on('error',reject);child.on('close',code=>code===0?resolve():reject(new Error(`child exit ${code}`)));});
  try{
    const before=await request({id:1,command:'status'});assert.equal(before.result.target.path,a);
    const other=f.run([{id:2,command:'destination',mode:'game-mods',path:b,confirmed:true}])[0];assert.equal(other.ok,true);assert.equal(other.result.target.path,b);
    const rejected=await request({id:3,command:'preferences',preferences:{theme:'dark'}});assert.equal(rejected.error,'DESTINATION_CHANGED_EXTERNALLY');assert.equal(rejected.result.target.path,a);
    const saved=JSON.parse(f.fs.readFileSync(f.statePath,'utf8'));assert.equal(saved.modsRoot,b);assert.equal(saved.preferences.theme,undefined);assert.equal(saved.preferences.futureOption,'keep');
  }finally{child.stdin.end();}await closed;
});

test('recovery-lock and external-destination explanations are explicit in all four languages',async()=>{
  const {localizeReply,translate}=await import('./localization.mjs');
  for(const language of ['en','zh-Hant','zh-Hans','ja']){
    const state={preferences:{language,theme:'system'}};
    assert.equal(localizeReply({ok:false,error:'LOCAL_STATE_RECOVERY_REQUIRED'},state).errorText,translate(language,'Settings were not saved. A recovery lock may remain. Inspect it before retrying; no lock was automatically removed.'));
    assert.equal(localizeReply({ok:false,error:'DESTINATION_CHANGED_EXTERNALLY'},state).errorText,translate(language,'The destination changed in another App. Preferences were not saved. Reconfirm the destination or restart.'));
    assert.equal(localizeReply({ok:true,result:{settingsWarning:'LOCAL_STATE_COMMITTED_REVIEW',settingsLockMayRemain:true}},state).result.settingsWarningText,translate(language,'Settings were saved, but a recovery lock may remain. Inspect it before retrying; no automatic lock removal is performed.'));
  }
});

test('two real processes cannot overwrite each others destination and preference fields',async()=>{
  const f=await backendSettingsFixture();const path=await import('node:path');const {execFile,execFileSync}=await import('node:child_process');
  const release=path.join(f.root,'release-lock');
  const source=`import fs from 'node:fs';import {SettingsStore} from './settings-store.mjs';const start=Date.now();const store=new SettingsStore(${JSON.stringify(f.statePath)},{fault:point=>{if(point==='before-rename'){process.stdout.write('LOCKED\\n');while(!fs.existsSync(${JSON.stringify(release)})){if(Date.now()-start>8000)throw new Error('timeout');Atomics.wait(new Int32Array(new SharedArrayBuffer(4)),0,0,10);}}}});store.update(current=>({...current,mode:'sandbox',sandbox:'first-process-destination',firstProcess:true}));`;
  const child=execFile(process.execPath,['--input-type=module','-e',source],{cwd:f.root,windowsHide:true,timeout:10000});
  const closed=new Promise((resolve,reject)=>{child.on('error',reject);child.on('close',code=>code===0?resolve():reject(new Error(`child exit ${code}`)));});
  await new Promise((resolve,reject)=>{child.stdout.on('data',data=>{if(data.toString().includes('LOCKED'))resolve();});child.on('error',reject);});
  const second=`import {SettingsStore} from './settings-store.mjs';import {mergePresentationPreferences} from './preferences.mjs';try{const value=new SettingsStore(${JSON.stringify(f.statePath)}).update(current=>mergePresentationPreferences(current,{language:'ja',theme:'dark'}));process.stdout.write(JSON.stringify(value));}catch(error){process.stdout.write(JSON.stringify({error:error.message}));}`;
  const run=()=>JSON.parse(execFileSync(process.execPath,['--input-type=module','-e',second],{cwd:f.root,windowsHide:true,encoding:'utf8'}));
  assert.equal(run().error,'LOCAL_STATE_BUSY');assert.deepEqual(JSON.parse(f.fs.readFileSync(f.statePath,'utf8')),f.legacy);
  f.fs.writeFileSync(release,'release');await closed;const merged=run().state;assert.equal(merged.sandbox,'first-process-destination');assert.equal(merged.firstProcess,true);assert.equal(merged.modsRoot,f.legacy.modsRoot);assert.equal(merged.preferences.futureOption,'keep');assert.equal(merged.preferences.language,'ja');
});

test('same-source same-run accepted and rejected attempts never get a false positive translation',async()=>{
  const {assessUpdateOrder}=await import('./update-order.mjs');const {localizeReason,translate}=await import('./localization.mjs');
  const installed={buildId:'old',publication:{sourceSha:'a'.repeat(40),publishedAt:'2026-11-02T00:00:00Z',runId:100,attempt:2}};
  for(const attempt of [1,2,3]){
    const order=await assessUpdateOrder(installed,{buildId:`new-${attempt}`,sourceSha:'a'.repeat(40),publishedAt:'2026-11-03T00:00:00Z',runId:100,attempt},'Owner/Repo',()=>{throw new Error('No API needed');});assert.equal(order.allowed,attempt===3);
    for(const language of ['en','zh-Hant','zh-Hans','ja']){const result=localizeReason(language,order.reason);assert.equal(result.text,translate(language,'Only a later attempt of the same build can be an update.'));assert.notEqual(result.text,translate(language,'A forward update is verified. Installation will verify it again.'));}
  }
});

test('only exact known positive update reasons translate as a verified forward update',async()=>{
  const {localizeReason,translate}=await import('./localization.mjs');
  for(const language of ['en','zh-Hant','zh-Hans','ja']){
    for(const reason of ['A later publication with a later build of the same source is available.','A later publication with verified forward source ancestry is available.'])assert.equal(localizeReason(language,reason).text,translate(language,'A forward update is verified. Installation will verify it again.'));
    for(const reason of ['No later publication with a later build is available.','Unable to verify forward source ancestry.','Cannot verify the manifest before installation.']){const result=localizeReason(language,reason);assert.notEqual(result.text,translate(language,'A forward update is verified. Installation will verify it again.'));assert.notEqual(result.text,translate(language,'Candidate found. Full source and file verification is required before installation.'));assert.equal(result.diagnostic,reason);}
  }
});

test('native UTF-8 resource probe returns exact Chinese and Japanese strings and follows theme choices without a window',{skip:process.platform!=='win32'},async()=>{
  const fixture=await buildNativeFixtures();const {execFileSync}=await import('node:child_process');const path=await import('node:path');
  const fs=await import('node:fs');const resource=path.join(fixture.root,'translations.json');fs.copyFileSync('translations.json',resource);
  const probe=(language,theme,systemDark,highContrast=false,key='Find a module',args=[])=>JSON.parse(execFileSync(fixture.helper,['--presentation-probe',resource,language,theme,String(systemDark),String(highContrast),key,JSON.stringify(args)],{encoding:'utf8',windowsHide:true}).replace(/^\uFEFF/,''));
  for(const [language,expected] of [['en','Find a module'],['zh-Hant','搜尋模組'],['zh-Hans','搜索模组'],['ja','モジュールを検索']]){
    for(const systemDark of [false,true])for(const theme of ['system','light','dark']){const result=probe(language,theme,systemDark);assert.equal(result.ok,true);assert.equal(result.text,expected);assert.equal(result.dark,theme==='dark'||theme==='system'&&systemDark);}
  }
  const luminance=argb=>{const c=[argb>>>16&255,argb>>>8&255,argb&255].map(v=>{v/=255;return v<=0.04045?v/12.92:((v+0.055)/1.055)**2.4;});return .2126*c[0]+.7152*c[1]+.0722*c[2];};
  for(const theme of ['light','dark']){const result=probe('en',theme,false);for(const [background,foreground] of [[result.background,result.foreground],[result.buttonBackground,result.foreground],[result.selectionBackground,result.selectionForeground]]){const values=[luminance(background),luminance(foreground)].sort((a,b)=>a-b);assert.ok((values[1]+.05)/(values[0]+.05)>=4.5);}}
  assert.equal(probe('en','light',false,true).background,probe('en','dark',true,true).background);
  const {translate}=await import('./localization.mjs');
  for(const language of ['en','zh-Hant','zh-Hans','ja']){
    const args=['Module {0} / 模組','builds/101-1/example.dll'];assert.equal(probe(language,'light',false,false,'Downloading {0}: {1}',args).text,translate(language,'Downloading {0}: {1}',...args));
  }
});

test('preview selection supports individual and visible bulk selection without execution', () => {
  const { createPreviewModel, sampleModules } = createRequire(import.meta.url)('./preview-model.js');
  const model = createPreviewModel(sampleModules);
  assert.equal(model.selected().length, 0);
  model.select('ai-improve', true);
  assert.equal(model.selected().length, 1);
  model.selectVisible('', 'different-build', true);
  assert.equal(model.selected().length, 2);
  assert.equal(model.visible('CITY TIME', 'all').length, 1);
  assert.equal(model.visible('no results here', 'all').length, 0);
  assert.equal(model.visible('', 'unavailable').length, 1);
  assert.equal(model.selected().length, 2);
  for (const operation of ['install', 'update', 'uninstall']) {
    const plan = model.operationPlan(operation);
    assert.equal(plan.allowed, false); assert.equal(plan.items.length, 2);
    assert.ok(plan.items.every(item => item.allowed === false && item.reason));
  }
  model.clear(); assert.equal(model.selected().length, 0);
  assert.equal(model.operationPlan('install').allowed, false);
  assert.throws(() => model.select('unknown', true));
});
test('preview data is synthetic, detached and never upgrades different-build to newer', () => {
  const { createPreviewModel, sampleModules } = createRequire(import.meta.url)('./preview-model.js');
  const model = createPreviewModel(sampleModules);
  const unavailable = model.visible('', 'unavailable')[0];
  assert.ok(unavailable.reason);
  model.select(unavailable.id, true);
  assert.equal(model.operationPlan('install').items[0].allowed, false);
  const detached = model.visible('', 'all'); detached[0].name = 'tampered';
  assert.notEqual(model.visible('', 'all')[0].name, 'tampered');
  assert.ok(sampleModules.every(item => item.synthetic === true));
  assert.ok(!sampleModules.some(item => item.name === 'AI Gaming'));
  assert.ok(model.visible('', 'all').some(item => item.coexistenceWarning));
  assert.throws(() => model.operationPlan('execute'));
});
test('preview controller preserves selections across filters and keeps actions disabled', () => {
  // A minimal DOM harness exercises event wiring, not browser layout or accessibility.
  class Element {
    constructor(tag = 'div') { this.tag = tag; this.children = []; this.listeners = {}; this.attributes = {}; this.value = ''; }
    append(...nodes) { this.children.push(...nodes); }
    replaceChildren(...nodes) { this.children = nodes; }
    addEventListener(type, handler) { this.listeners[type] = handler; }
    setAttribute(key, value) { this.attributes[key] = value; }
    querySelectorAll() {
      return this.children.flatMap(child => [ ...(child.tag === 'input' && child.type === 'checkbox' ? [child] : []), ...child.querySelectorAll() ]);
    }
    dispatch(type) { this.listeners[type](); }
  }
  const html = readFileSync(new URL('./preview.html', import.meta.url), 'utf8');
  const elements = Object.fromEntries([...html.matchAll(/id="([a-z-]+)"/g)].map(match => [match[1], new Element()]));
  elements['status-filter'].value = 'all';
  const api = createRequire(import.meta.url)('./preview-model.js');
  const document = { getElementById: id => elements[id], createElement: tag => new Element(tag), documentElement: { dataset: {} } };
  vm.runInNewContext(readFileSync(new URL('./preview.js', import.meta.url), 'utf8'), { document, DevManagerPreview: api });
  assert.equal(elements.modules.children.length, 5);
  const first = elements.modules.querySelectorAll()[0]; first.checked = true; first.dispatch('change');
  assert.equal(elements['selection-count'].textContent, '1');
  assert.equal(elements['select-visible'].indeterminate, true);
  elements['status-filter'].value = 'different-build'; elements['status-filter'].dispatch('change');
  elements['select-visible'].checked = true; elements['select-visible'].dispatch('change');
  assert.equal(elements['selection-count'].textContent, '2');
  assert.equal(elements['select-visible'].checked, true);
  elements.review.dispatch('click');
  assert.equal(elements['review-panel'].hidden, false);
  assert.equal(elements['review-items'].children.length, 2);
  assert.equal(elements.review.attributes['aria-expanded'], 'true');
  elements.search.value = 'no match'; elements.search.dispatch('input');
  assert.equal(elements.empty.hidden, false); assert.equal(elements['select-visible'].disabled, true);
  assert.equal(elements['review-panel'].hidden, true);
  assert.equal(elements['selection-count'].textContent, '2');
  elements.clear.dispatch('click'); assert.equal(elements['selection-count'].textContent, '0');
  elements.theme.dispatch('click'); assert.equal(document.documentElement.dataset.theme, 'dark');
  assert.match(html, /role="status" aria-live="polite"/);
  for (const label of ['Install selected', 'Update selected', 'Uninstall selected'])
    assert.ok(html.includes(`<button type="button" disabled>${label}</button>`));
  assert.match(html, /connect-src 'none'/);
});

const catalogFixture = () => ({ schema: 1, moduleId: 'example',
  repository: 'Owner/Repo', branch: 'Example', assembly: 'Example', workflow: 'dev.yml',
  channel: 'development', buildId: 'opaque-b', sourceSha: 'a'.repeat(40), devTool: true,
  release: { id: 42, tag: 'example-dev-b', draft: false, publishedAt: '2026-11-02T00:00:00Z' },
  files: ['Example.dll', 'dependency/api.dll'].map((p, i) => ({ path: p,
    assetName: p.split('/').at(-1), assetId: 100 + i, size: 200, sha256: 'b'.repeat(64),
    url: `https://github.com/Owner/Repo/releases/download/example-dev-b/${p.split('/').at(-1)}` })) });
const catalogInput = (edit = () => {}) => {
  const catalog = catalogFixture(); edit(catalog);
  const data = Buffer.from(JSON.stringify(catalog));
  return { data, policy: { moduleId: 'example', repository: 'Owner/Repo', branch: 'Example',
    assembly: 'Example', workflow: 'dev.yml', requiredFiles: ['Example.dll', 'dependency/api.dll'],
    catalogSha256: createHash('sha256').update(data).digest('hex') } };
};

test('injected download verifies pinned catalog, streamed bytes and exact length', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const payload = Buffer.from('synthetic downloaded bytes');
  const input = catalogInput(c => { c.files[0].size = payload.length; c.files[0].sha256 = createHash('sha256').update(payload).digest('hex'); });
  const calls = [];
  const transport = async request => {
    calls.push(request);
    return { url: request.url, redirected: false, status: 200, headers: { 'content-length': String(payload.length) },
      body: (async function* () { yield payload.subarray(0, 4); yield payload.subarray(4); })() };
  };
  const result = await downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport });
  assert.deepEqual(result.bytes, payload); assert.equal(result.moduleId, 'example');
  assert.equal(calls.length, 1); assert.equal(calls[0].redirect, 'manual'); assert.equal(calls[0].credentials, 'omit');
  assert.equal(calls[0].signal.aborted, true);
  await assert.rejects(downloadCatalogAsset({ ...input, policy: { ...input.policy, catalogSha256: '0'.repeat(64) }, assetPath: 'Example.dll', transport }), { code: 'SOURCE_UNAVAILABLE' });
  assert.equal(calls.length, 1);
});

test('injected download rejects partial, oversized, corrupted and transformed streams', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const payload = Buffer.from('fixture');
  const input = catalogInput(c => { c.files[0].size = payload.length; c.files[0].sha256 = createHash('sha256').update(payload).digest('hex'); });
  const scenarios = [
    { bytes: payload.subarray(1), code: 'LENGTH_MISMATCH' },
    { bytes: Buffer.concat([payload, Buffer.from('x')]), code: 'SIZE_LIMIT' },
    { bytes: Buffer.from('xxxxxxx'), code: 'INTEGRITY_MISMATCH' },
    { bytes: payload, headers: { 'content-length': '999' }, code: 'LENGTH_MISMATCH' },
    { bytes: payload, headers: { 'content-encoding': 'gzip' }, code: 'ENCODING_REFUSED' },
    { bytes: 'fixture', code: 'INVALID_CHUNK' }
  ];
  for (const scenario of scenarios) {
    let signal;
    const transport = async request => {
      signal = request.signal;
      return { url: request.url, redirected: false, status: 200, headers: scenario.headers ?? {},
        body: (async function* () { yield scenario.bytes; })() };
    };
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport }), { code: scenario.code });
    assert.equal(signal.aborted, true);
  }
});

test('injected download rejects redirects by default and permits only exact HTTPS targets', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const payload = Buffer.from('fixture');
  const input = catalogInput(c => { c.files[0].size = payload.length; c.files[0].sha256 = createHash('sha256').update(payload).digest('hex'); });
  const target = 'https://cdn.example.test/exact/file.dll?signature=synthetic';
  let calls = 0;
  const transport = async request => {
    calls++;
    return request.url === target ? { url: request.url, redirected: false, status: 200, headers: {},
      body: (async function* () { yield payload; })() } :
      { url: request.url, redirected: false, status: 302, headers: { location: target } };
  };
  await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport }), { code: 'REDIRECT_REFUSED' });
  assert.equal(calls, 1);
  const result = await downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport, redirectUrls: [target] });
  assert.deepEqual(result.bytes, payload);
  await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport, redirectUrls: ['http://cdn.example.test/file'] }), { code: 'INVALID_DOWNLOAD_POLICY' });
});

test('injected download deadline aborts hanging headers and hanging body without a partial result', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const input = catalogInput();
  for (const phase of ['headers', 'body']) {
    let signal;
    const transport = request => {
      signal = request.signal;
      if (phase === 'headers') return new Promise(() => {});
      return { url: request.url, redirected: false, status: 200, headers: {},
        body: { [Symbol.asyncIterator]() { return { next: () => new Promise(() => {}) }; } } };
    };
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport, timeoutMs: 20 }), { code: 'TIMEOUT' });
    assert.equal(signal.aborted, true);
  }
});

test('injected download rejects automatic redirects, loops and excessive hops', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const input = catalogInput();
  for (const variant of ['automatic', 'changed-source', 'loop', 'limit', 'status']) {
    const transport = async request => ({ url: variant === 'changed-source' ? 'https://other.example.test/' : request.url,
      redirected: variant === 'automatic', status: variant === 'status' ? 403 : 302,
      headers: { location: request.url } });
    const code = { automatic: 'SOURCE_CHANGED', 'changed-source': 'SOURCE_CHANGED', loop: 'REDIRECT_LOOP', limit: 'REDIRECT_LIMIT', status: 'HTTP_STATUS' }[variant];
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport,
      redirectUrls: [JSON.parse(input.data).files[0].url], maxRedirects: variant === 'limit' ? 0 : 3 }), { code });
  }
  for (const target of ['https://user:secret@example.test/file', 'https://example.test/a/../file', 'https://example.test/file#fragment'])
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport: () => { throw new Error('must not request'); }, redirectUrls: [target] }), { code: 'INVALID_DOWNLOAD_POLICY' });
});

test('injected download hides transport diagnostics and never returns interrupted bytes', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const input = catalogInput();
  for (const phase of ['headers', 'stream']) {
    const transport = async request => {
      if (phase === 'headers') throw new Error('https://example.test/?secret=PRIVATE');
      return { url: request.url, redirected: false, status: 200, headers: {},
        body: (async function* () { yield Buffer.from('partial'); throw new Error('PRIVATE'); })() };
    };
    await assert.rejects(downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport }), error => {
      assert.equal(error.code, 'TRANSPORT_FAILED'); assert.ok(!error.message.includes('PRIVATE'));
      assert.equal(error.bytes, undefined); assert.equal(error.cause, undefined); return true;
    });
  }
});

test('injected download snapshots chunks before an adapter reuses its buffer', async () => {
  const { downloadCatalogAsset } = await import('./download.mjs');
  const payload = Buffer.from('fixture');
  const input = catalogInput(c => { c.files[0].size = payload.length; c.files[0].sha256 = createHash('sha256').update(payload).digest('hex'); });
  const transport = async request => ({ url: request.url, redirected: false, status: 200, headers: {},
    body: (async function* () { const chunk = Buffer.from(payload); yield chunk; chunk.fill(0); })() });
  const result = await downloadCatalogAsset({ ...input, assetPath: 'Example.dll', transport });
  assert.deepEqual(result.bytes, payload);
});

function batchFixture() {
  const payload = Buffer.from('batch fixture');
  const entries = ['first', 'second'].map(moduleId => {
    const input = catalogInput(c => {
      c.moduleId = moduleId;
      for (const file of c.files) { file.size = payload.length; file.sha256 = createHash('sha256').update(payload).digest('hex'); }
    });
    input.policy.moduleId = moduleId;
    return { moduleId, ...input, installed: null, synthetic: true };
  });
  return { payload, entries };
}
test('batch assessment feeds the selection model while retaining blocked installation', async () => {
  const { inspectModuleBatch } = await import('./batch.mjs');
  const { entries } = batchFixture();
  entries[1].policy.catalogSha256 = '0'.repeat(64);
  const assessment = inspectModuleBatch(entries);
  assert.equal(assessment.canPrepare, false); assert.equal(assessment.installAllowed, false);
  assert.equal(assessment.previewRecords[0].status, 'not-installed');
  assert.equal(assessment.previewRecords[1].status, 'unavailable');
  const { createPreviewModel } = createRequire(import.meta.url)('./preview-model.js');
  const model = createPreviewModel(assessment.previewRecords);
  model.selectVisible('', 'all', true);
  assert.equal(model.operationPlan('install').allowed, false);
});
test('batch preparation returns only complete verified in-memory module packages', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries, payload } = batchFixture(); let calls = 0;
  const result = await prepareModuleBatch(entries, { transport: async request => {
    calls++;
    return { url: request.url, redirected: false, status: 200, headers: {}, body: (async function* () { yield payload; })() };
  } });
  assert.equal(calls, 4); assert.equal(result.packages.length, 2);
  assert.equal(result.installAllowed, false);
  for (const pkg of result.packages) assert.equal(verifyPackage(pkg).length, 2);
});
test('batch preflight refuses unavailable or duplicate modules before any transfer', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries } = batchFixture(); let calls = 0;
  const transport = () => { calls++; throw new Error('must not request'); };
  await assert.rejects(prepareModuleBatch([entries[0], entries[0]], { transport }), { code: 'BATCH_UNAVAILABLE' });
  entries[1].policy.requiredFiles.push('missing.dll');
  await assert.rejects(prepareModuleBatch(entries, { transport }), { code: 'BATCH_UNAVAILABLE' });
  await assert.rejects(prepareModuleBatch([], { transport }), { code: 'BATCH_UNAVAILABLE' });
  assert.equal(calls, 0);
});
test('batch failure after an earlier module returns no partially prepared packages', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries, payload } = batchFixture(); let calls = 0;
  await assert.rejects(prepareModuleBatch(entries, { transport: async request => {
    calls++;
    return { url: request.url, redirected: false, status: 200, headers: {},
      body: (async function* () { yield calls === 3 ? Buffer.from('corrupt bytes') : payload; })() };
  } }), error => {
    assert.equal(error.code, 'BATCH_ASSET_FAILED'); assert.equal(error.moduleId, 'second');
    assert.equal(error.packages, undefined); assert.equal(error.bytes, undefined); return true;
  });
  assert.equal(calls, 3);
});
test('batch preparation snapshots all source contracts before awaiting transport', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries, payload } = batchFixture(); let calls = 0;
  const result = await prepareModuleBatch(entries, { transport: async request => {
    if (++calls === 1) { entries[1].data.fill(0); entries[1].policy.moduleId = 'tampered'; }
    return { url: request.url, redirected: false, status: 200, headers: {}, body: (async function* () { yield payload; })() };
  } });
  assert.equal(result.packages[1].moduleId, 'second'); assert.equal(calls, 4);
});
test('selection review is explanatory and cannot enable synthetic operations', () => {
  const { createPreviewModel } = createRequire(import.meta.url)('./preview-model.js');
  const model = createPreviewModel(); model.selectVisible('', 'all', true);
  const review = model.reviewSelection();
  assert.equal(review.installAllowed, false); assert.equal(review.items.length, 5);
  assert.ok(review.items.every(item => item.blockers.length > 0));
  assert.ok(review.stages.length >= 3);
});
test('batch bounds and explicit installation records fail closed before transfer', async () => {
  const { inspectModuleBatch, prepareModuleBatch } = await import('./batch.mjs');
  const { entries } = batchFixture(); let calls = 0;
  const transport = () => { calls++; throw new Error('must not request'); };
  delete entries[1].installed;
  assert.equal(inspectModuleBatch(entries).canPrepare, false);
  await assert.rejects(prepareModuleBatch(entries, { transport }), { code: 'BATCH_UNAVAILABLE' });
  const large = ['first', 'second', 'third'].map(moduleId => {
    const input = catalogInput(c => { c.moduleId = moduleId; for (const f of c.files) f.size = 32 * 1024 * 1024; });
    input.policy.moduleId = moduleId; return { moduleId, ...input, installed: null };
  });
  assert.equal(inspectModuleBatch(large).reason, 'BATCH_SIZE_LIMIT');
  await assert.rejects(prepareModuleBatch(large, { transport }), { code: 'BATCH_UNAVAILABLE' });
  await assert.rejects(prepareModuleBatch(batchFixture().entries, { transport, totalTimeoutMs: 30001 }), { code: 'INVALID_BATCH_POLICY' });
  assert.equal(calls, 0);
});
test('batch total deadline aborts a stalled asset and exposes no prepared package', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { entries } = batchFixture(); let signal;
  await assert.rejects(prepareModuleBatch(entries, { totalTimeoutMs: 25, transport: request => {
    signal = request.signal; return new Promise(() => {});
  } }), error => {
    assert.equal(error.code, 'BATCH_TIMEOUT'); assert.equal(error.packages, undefined); return true;
  });
  assert.equal(signal.aborted, true);
});

function planningFixture() {
  const { entries, payload } = batchFixture();
  return { requests: entries.map(entry => ({ operation: 'install', moduleId: entry.moduleId, source: entry,
    package: { moduleId: entry.moduleId, buildId: JSON.parse(entry.data).buildId,
      files: JSON.parse(entry.data).files.map(f => ({ path: f.path, sha256: f.sha256, bytes: Buffer.from(payload) })) } })),
    observation: { schema: 1, snapshotId: 'synthetic-observation', complete: true, rootSafe: true, gameRunning: false,
      modules: entries.map(entry => ({ moduleId: entry.moduleId, directoryPresent: false,
        reparsePoint: false, ownership: null, files: [], stableInstalled: false })) },
    rules: { complete: true, conflicts: [], requires: [] } };
}

function sourceFixture(moduleId = 'example', branch = 'Example') {
  const input = catalogInput(c => { c.moduleId = moduleId; c.branch = branch; });
  input.policy.moduleId = moduleId; input.policy.branch = branch;
  const catalog = JSON.parse(input.data);
  const keys = generateKeyPairSync('ed25519');
  input.policy.freshnessPublicKey = keys.publicKey.export({ type: 'spki', format: 'pem' });
  const nowMs = Date.parse('2026-11-03T00:00:00Z'); const nonce = 'a'.repeat(32);
  const pointer = { schema: 1, selection: 'latest-published-development', moduleId: catalog.moduleId,
    repository: catalog.repository, branch: catalog.branch, assembly: catalog.assembly, workflow: catalog.workflow,
    channel: catalog.channel, catalogSha256: input.policy.catalogSha256, buildId: catalog.buildId,
    sourceSha: catalog.sourceSha, releaseId: catalog.release.id, releaseTag: catalog.release.tag,
    issuedAtMs: nowMs - 1000, expiresAtMs: nowMs + 60000, nonce, sequence: 3 };
  const proofFor = value => { const payload = Buffer.from(JSON.stringify(value));
    return { payload, signature: sign(null, payload, keys.privateKey) }; };
  return { ...input, nowMs, nonce, minimumSequence: 0, installed: null, proof: proofFor(pointer), pointer, proofFor,
    releases: [{ id: catalog.release.id, tag_name: catalog.release.tag, target_commitish: catalog.sourceSha,
      url: `https://api.github.com/repos/${catalog.repository}/releases/${catalog.release.id}`,
      html_url: `https://github.com/${catalog.repository}/releases/tag/${encodeURIComponent(catalog.release.tag)}`,
      draft: false, prerelease: false, published_at: catalog.release.publishedAt,
      assets: catalog.files.map(f => ({ id: f.assetId, name: f.assetName, size: f.size, state: 'uploaded',
        digest: `sha256:${f.sha256}`, browser_download_url: f.url })) }] };
}
test('per-module source requires a pinned signed fresh pointer and exact GitHub release binding', async () => {
  const { classifyModuleSource } = await import('./sources.mjs');
  const f = sourceFixture(); const result = classifyModuleSource(f);
  assert.equal(result.status, 'not-installed'); assert.equal(result.freshness, 'verified');
  assert.equal(result.verifiedSequence, 3); assert.equal(result.installAllowed, false);
  assert.equal(classifyModuleSource({ ...f, installed: { moduleId: 'example', buildId: 'opaque-b' } }).status, 'current');
  assert.equal(classifyModuleSource({ ...f, installed: { moduleId: 'example', buildId: 'opaque-z' } }).status, 'different-build');
  assert.equal(classifyModuleSource({ ...f, proof: undefined }).status, 'unknown');
  assert.equal(classifyModuleSource({ ...f, installed: undefined }).status, 'unknown');
});
test('freshness checks reject replay, expired/future pointers and untrusted signatures', async () => {
  const { classifyModuleSource } = await import('./sources.mjs');
  const f = sourceFixture();
  for (const override of [{ nonce: 'b'.repeat(32) }, { minimumSequence: 4 },
    { nowMs: f.pointer.expiresAtMs }, { nowMs: f.pointer.issuedAtMs - 1 },
    { policy: { ...f.policy, freshnessPublicKey: undefined } }, { policy: { ...f.policy, freshnessPublicKey: sourceFixture().policy.freshnessPublicKey } }])
    assert.equal(classifyModuleSource({ ...f, ...override }).status, 'unknown');
  const signature = Buffer.from(f.proof.signature); signature[0] ^= 1;
  assert.equal(classifyModuleSource({ ...f, proof: { ...f.proof, signature } }).status, 'unknown');
  const long = { ...f.pointer, expiresAtMs: f.pointer.issuedAtMs + 300001 };
  assert.equal(classifyModuleSource({ ...f, proof: f.proofFor(long) }).status, 'unknown');
});
test('signed declarations cannot cross module, branch, build, source or catalog boundaries', async () => {
  const { classifyModuleSource } = await import('./sources.mjs');
  const f = sourceFixture();
  for (const [key, value] of Object.entries({ moduleId: 'other', repository: 'Other/Repo', branch: 'Other',
    assembly: 'Other', workflow: 'other.yml', channel: 'stable', catalogSha256: '0'.repeat(64),
    buildId: 'different', sourceSha: 'b'.repeat(40), releaseId: 43, releaseTag: 'other' })) {
    const pointer = { ...f.pointer, [key]: value };
    assert.equal(classifyModuleSource({ ...f, proof: f.proofFor(pointer) }).status, 'unknown', key);
  }
});
test('release candidates stay unknown when draft, incomplete or inconsistent regardless of Latest labels', async () => {
  const { classifyModuleSource } = await import('./sources.mjs');
  for (const edit of [r => { r.draft = true; }, r => { r.published_at = null; },
    r => { r.target_commitish = 'Example'; }, r => { r.assets.pop(); },
    r => { r.assets[0].digest = null; }, r => { r.assets[0].browser_download_url = 'https://evil.example/file'; },
    r => { r.assets[0].size++; }, r => { r.assets[0].name = 'Wrong.dll'; }]) {
    const f = sourceFixture(); edit(f.releases[0]); f.releases[0].name = '[LATEST DEV]';
    assert.equal(classifyModuleSource(f).status, 'unknown', String(edit));
  }
  const f = sourceFixture();
  f.releases.unshift({ id: 999, name: 'Latest', prerelease: true });
  assert.equal(classifyModuleSource(f).status, 'not-installed');
  f.releases.push(f.releases[1]); assert.equal(classifyModuleSource(f).status, 'unknown');
});
test('independent modules cannot reuse another module freshness proof or signing pin', async () => {
  const { classifyModuleSource } = await import('./sources.mjs');
  const first = sourceFixture('first', 'First'); const second = sourceFixture('second', 'Second');
  assert.equal(classifyModuleSource(first).moduleId, 'first');
  assert.equal(classifyModuleSource(second).moduleId, 'second');
  assert.equal(classifyModuleSource({ ...second, proof: first.proof }).status, 'unknown');
  assert.equal(classifyModuleSource({ ...first, installed: { moduleId: 'second', buildId: 'opaque-b' } }).status, 'unknown');
  const malformed = { ...first, proof: { payload: Buffer.alloc(16385), signature: first.proof.signature } };
  assert.equal(classifyModuleSource(malformed).status, 'unknown');
});

function nativeSourceFixture() {
  const repository = 'Owner/Repo'; const base = `https://api.github.com/repos/${repository}`;
  const tag = 'Example-dev-100-1'; const sha = 'a'.repeat(40); const branchSha = 'b'.repeat(40);
  const release = { id: 42, tag_name: tag, target_commitish: 'Example', draft: false, prerelease: false,
    published_at: '2026-11-03T00:00:00Z', updated_at: '2026-11-03T00:00:00Z',
    url: `${base}/releases/42`, html_url: `https://github.com/${repository}/releases/tag/${tag}` };
  const assets = ['Example.dll', 'api.dll'].map((name, index) => ({ id: 200 + index, name, size: 50,
    digest: `sha256:${'c'.repeat(64)}`, state: 'uploaded', url: `${base}/releases/assets/${200 + index}`,
    browser_download_url: `https://github.com/${repository}/releases/download/${tag}/${name}` }));
  const run = { id: 100, run_attempt: 1, head_branch: 'Example', head_sha: sha, path: '.github/workflows/dev.yml',
    status: 'completed', conclusion: 'success', event: 'push',run_started_at:'2026-11-03T00:00:00Z', repository: { full_name: repository }, head_repository: { full_name: repository } };
  const routes = new Map([
    [`${base}/git/ref/heads/Example`, { ref: 'refs/heads/Example', object: { type: 'commit', sha: branchSha } }],
    [`${base}/releases?per_page=100&page=1`, [release]],
    [`${base}/releases/42`, release], [`${base}/releases/42/assets?per_page=100&page=1`, assets],
    [`${base}/git/ref/tags/${tag}`, { ref: `refs/tags/${tag}`, object: { type: 'commit', sha } }],
    [`${base}/actions/runs/100/attempts/1`, run]
  ]);
  const headers = new Map(); const calls = [];
  const request = async options => {
    calls.push(options.url); if (!routes.has(options.url)) throw new Error('Unexpected API route');
    return { status: 200, url: options.url, redirected: false, headers: headers.get(options.url) ?? {}, body: structuredClone(routes.get(options.url)) };
  };
  return { policy: { moduleId: 'example', repository, branch: 'Example', assembly: 'Example',
    workflowPath: '.github/workflows/dev.yml', releaseTagPrefix: 'Example-dev-', runtimeAssetNames: ['Example.dll', 'api.dll'] },
    installed: null, nowMs: Date.parse('2026-11-03T00:01:00Z'), request, routes, headers, calls, release, assets, run, base, tag, sha };
}
test('GitHub native module selection separates latest published from current branch head without signatures', async () => {
  const { detectGitHubModule } = await import('./github-source.mjs'); const f = nativeSourceFixture();
  const result = await detectGitHubModule(f);
  assert.equal(result.status, 'not-installed'); assert.equal(result.latestPublished.releaseId, 42);
  assert.equal(result.latestPublished.relationToBranch, 'different-commit');
  assert.equal(result.branchHead.sourceSha, 'b'.repeat(40)); assert.equal(result.installAllowed, false);
  assert.equal(result.assurance, 'trusted-https-github-api-metadata');
  assert.equal((await detectGitHubModule({ ...f, installed: { moduleId: 'example', buildId: f.tag } })).status, 'current');
});
test('GitHub native selection follows complete release and asset pages rather than repo Latest', async () => {
  const { detectGitHubModule } = await import('./github-source.mjs'); const f = nativeSourceFixture();
  const first = `${f.base}/releases?per_page=100&page=1`; const second = `${f.base}/releases?per_page=100&page=2`;
  f.routes.set(first, [{ ...f.release, id: 99, tag_name: 'Other-dev-999-1', name: 'Latest', published_at: '2026-11-03T00:00:30Z' }]);
  f.headers.set(first, { link: `<${second}>; rel="next", <${second}>; rel="last"` }); f.routes.set(second, [f.release]);
  const assetsFirst = `${f.base}/releases/42/assets?per_page=100&page=1`; const assetsSecond = `${f.base}/releases/42/assets?per_page=100&page=2`;
  f.routes.set(assetsFirst, [f.assets[0]]); f.routes.set(assetsSecond, [f.assets[1]]);
  f.headers.set(assetsFirst, { link: `<${assetsSecond}>; rel="next"` });
  assert.equal((await detectGitHubModule(f)).status, 'not-installed'); assert.ok(f.calls.includes(second)); assert.ok(f.calls.includes(assetsSecond));
});
test('GitHub native source keeps drafts, missing provenance, incomplete pages and wrong assets unknown', async () => {
  const { detectGitHubModule } = await import('./github-source.mjs');
  for (const edit of [f => { f.release.draft = true; }, f => { f.run.head_branch = 'Other'; },
    f => { f.run.head_sha = 'd'.repeat(40); }, f => { f.run.run_attempt = 2; },
    f => { f.run.path = '.github/workflows/other.yml'; }, f => { f.assets[0].digest = null; },
    f => { f.assets.pop(); }, f => { f.assets[0].browser_download_url = 'https://evil.example/file'; },
    f => { f.headers.set(`${f.base}/releases?per_page=100&page=1`, { link: '<https://evil.example/releases?page=2>; rel="next"' }); },
    f => { f.headers.set(`${f.base}/releases?per_page=100&page=1`, { link: `<${f.base}/releases?per_page=100&page=2>; rel="last"` }); }]) {
    const f = nativeSourceFixture(); edit(f); assert.equal((await detectGitHubModule(f)).status, 'unknown', String(edit));
  }
});
test('GitHub native tag peeling supports annotated tags and rejects cycles or changing snapshots', async () => {
  const { detectGitHubModule } = await import('./github-source.mjs'); const f = nativeSourceFixture();
  const tagSha = 'e'.repeat(40); const refUrl = `${f.base}/git/ref/tags/${f.tag}`;
  f.routes.set(refUrl, { ref: `refs/tags/${f.tag}`, object: { type: 'tag', sha: tagSha } });
  f.routes.set(`${f.base}/git/tags/${tagSha}`, { sha: tagSha, tag: f.tag, object: { type: 'commit', sha: f.sha } });
  assert.equal((await detectGitHubModule(f)).status, 'not-installed');
  f.routes.set(`${f.base}/git/tags/${tagSha}`, { sha: tagSha, tag: f.tag, object: { type: 'tag', sha: tagSha } });
  assert.equal((await detectGitHubModule(f)).status, 'unknown');
  const changed = nativeSourceFixture(); let listingReads = 0; const original = changed.request;
  changed.request = async options => {
    const response = await original(options);
    if (options.url === `${changed.base}/releases?per_page=100&page=1` && ++listingReads > 1) response.body = [];
    return response;
  };
  assert.equal((await detectGitHubModule(changed)).status, 'unknown');
});
test('GitHub native metadata stays independent from unknown installation records and volatile headers', async () => {
  const { detectGitHubModule } = await import('./github-source.mjs'); const f = nativeSourceFixture(); const original = f.request; let count = 0;
  f.request = async request => { const response = await original(request); response.headers['x-github-request-id'] = String(++count); return response; };
  const result = await detectGitHubModule({ ...f, installed: undefined });
  assert.equal(result.status, 'unknown'); assert.equal(result.latestPublished.status, 'candidate');
  assert.equal(result.branchHead.status, 'observed');
  f.routes.set(`${f.base}/git/ref/heads/Example`, { ref: 'refs/heads/Example', object: { type: 'commit', sha: f.sha } });
  assert.equal((await detectGitHubModule(f)).latestPublished.relationToBranch, 'same-commit');
});
test('GitHub native requests fail closed on API errors, redirects, hangs and unsafe pagination', async () => {
  const { detectGitHubModule } = await import('./github-source.mjs');
  for (const variant of ['status', 'redirect', 'credentials', 'fragment', 'missing-page']) {
    const f = nativeSourceFixture(); const original = f.request;
    if (['credentials', 'fragment', 'missing-page'].includes(variant)) {
      const next = `${f.base}/releases?per_page=100&page=2`;
      const target = variant === 'credentials' ? next.replace('https://', 'https://user@') : variant === 'fragment' ? `${next}#part` : next;
      f.headers.set(`${f.base}/releases?per_page=100&page=1`, { link: `<${target}>; rel="next"` });
    } else f.request = async options => ({ ...await original(options), ...(variant === 'status' ? { status: 429 } : { redirected: true }) });
    assert.equal((await detectGitHubModule(f)).status, 'unknown', variant);
  }
  const f = nativeSourceFixture(); let signal;
  const result = await detectGitHubModule({ ...f, timeoutMs: 20, request: options => { signal = options.signal; return new Promise(() => {}); } });
  assert.equal(result.status, 'unknown'); assert.equal(result.reason, 'API_SNAPSHOT_TIMEOUT'); assert.equal(signal.aborted, true);
});
test('GitHub native selection refuses ambiguous times and never falls back past an unverifiable latest release', async () => {
  const { detectGitHubModule } = await import('./github-source.mjs');
  const f = nativeSourceFixture(); const page = `${f.base}/releases?per_page=100&page=1`;
  const newer = { ...f.release, id: 43, tag_name: 'Example-dev-101-1', published_at: '2026-11-03T00:00:30Z' };
  f.routes.set(page, [f.release, newer]);
  assert.equal((await detectGitHubModule(f)).status, 'unknown');
  newer.published_at = f.release.published_at;
  assert.equal((await detectGitHubModule(f)).reason, 'AMBIGUOUS_PUBLICATION_ORDER');
  const capped = nativeSourceFixture();
  for (let n = 1; n <= 10; n++) {
    const url = `${capped.base}/releases?per_page=100&page=${n}`;
    capped.routes.set(url, []);
    capped.headers.set(url, { link: `<${capped.base}/releases?per_page=100&page=${n + 1}>; rel="next"` });
  }
  assert.equal((await detectGitHubModule(capped)).reason, 'RELEASE_LIST_INCOMPLETE');
});
test('desktop registry contains all fifteen mods without activating paused or unconfirmed sources', async () => {
  const { moduleRegistry } = await import('./registry.mjs');
  assert.equal(moduleRegistry.length, 15); assert.equal(new Set(moduleRegistry.map(m => m.moduleId)).size, 15);
  assert.equal(moduleRegistry.find(m => m.moduleId === 'ai-gaming').engineeringPaused, true);
  assert.ok(moduleRegistry.find(m => m.moduleId === 'public-transport-vehicles').unavailableReason);
});

test('player-facing sources expose only artifact branches and show twelve pending mappings plus paused AI Gaming',async()=>{
  const {sourceProviders,moduleRegistry}=await import('./registry.mjs');const {DesktopService}=await import('./desktop-service.mjs');
  assert.deepEqual(sourceProviders.map(source=>source.id),['artifact-branch']);
  const rows=new DesktopService({registry:moduleRegistry}).rows();
  assert.equal(rows.filter(row=>row.configurationPending).length,12);
  const gaming=rows.find(row=>row.moduleId==='ai-gaming');assert.equal(gaming.engineeringPaused,true);assert.match(gaming.displayName,/development paused/);
  assert.ok(rows.filter(row=>row.configurationPending).every(row=>row.displayName.includes('not configured')));
  assert.ok(rows.every(row=>!row.canRequestInstall&&!row.canRequestUpdate));
});
test('shared GitHub request pool deduplicates module lists and stops requests during rate-limit cooldown', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs'); let calls = 0;
  const pool = new GitHubRequestPool(async options => { calls++; return { status: 200, url: options.url, redirected: false, headers: {}, body: [] }; });
  const options = { url: 'https://api.github.com/repos/Owner/Repo/releases?per_page=100&page=1', cachePhase: 'initial' };
  await Promise.all([pool.request(options), pool.request(options), pool.request(options)]); assert.equal(calls, 1);
  await pool.request({ ...options, cachePhase: 'confirm' }); assert.equal(calls, 2);
  const limited = new GitHubRequestPool(async options => { calls++; return { status: 429, url: options.url, redirected: false,
    headers: { 'retry-after': '60' }, body: {} }; });
  await limited.request(options); const count = calls;
  assert.equal((await limited.request({ ...options, url: options.url + '&page=2' })).status, 429);
  assert.equal(calls, count); assert.ok(limited.status().retryAtMs > Date.now());
});
test('explicit Mods destination factory only touches its own child and retains unknown directories', async () => {
  const fs = await import('node:fs'); const os = await import('node:os'); const path = await import('node:path');
  const { IsolatedInstaller } = await import('./filesystem.mjs');
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-')); const modsRoot = path.join(root, 'SyntheticMods'); fs.mkdirSync(modsRoot);
  assert.throws(() => IsolatedInstaller.forModsDirectory(modsRoot, { confirmed: false }));
  assert.equal(IsolatedInstaller.forModsDirectory(modsRoot, { confirmed: true, create: false }), null);
  const manager = IsolatedInstaller.forModsDirectory(modsRoot, { confirmed: true, create: true });
  assert.equal(manager.root, modsRoot);
  assert.equal(path.dirname(manager.meta), path.dirname(modsRoot));
  assert.ok(!path.relative(modsRoot, manager.meta).split(path.sep).every(p => p !== '..'));
  manager.install(pkg());
  assert.ok(fs.existsSync(path.join(modsRoot, 'CS1DevManager-example', 'mod.dll')));
  assert.equal(path.relative(modsRoot, path.join(manager.root, 'CS1DevManager-example', 'mod.dll')).split(path.sep).length, 2);
  manager.update({ ...pkg(), buildId: 'updated' }); manager.uninstall('example');
  assert.equal(fs.readdirSync(modsRoot).length, 0);
  assert.ok(fs.readdirSync(path.join(manager.meta, 'backups')).length >= 2);
  assert.equal(manager.installed('example'), null);
  const otherMods = path.join(root, 'OtherMods'); fs.mkdirSync(otherMods); fs.mkdirSync(path.join(otherMods, 'CS1DevManager-example'));
  fs.writeFileSync(path.join(otherMods, 'CS1DevManager-example', 'personal.txt'), 'keep');
  assert.throws(() => IsolatedInstaller.forModsDirectory(otherMods, { confirmed: true, create: true }));
  assert.equal(fs.readFileSync(path.join(otherMods, 'CS1DevManager-example', 'personal.txt'), 'utf8'), 'keep');
});
test('desktop service connects real file transactions to synthetic source and reports per-module outcomes', async () => {
  const fs = await import('node:fs'); const os = await import('node:os'); const path = await import('node:path');
  const { DesktopService } = await import('./desktop-service.mjs'); const { IsolatedInstaller } = await import('./filesystem.mjs');
  const f = nativeSourceFixture(); const payload = Buffer.from('synthetic DLL bytes');
  for (const asset of f.assets) { asset.size = payload.length; asset.digest = `sha256:${createHash('sha256').update(payload).digest('hex')}`; }
  const installer = new IsolatedInstaller(fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-')));
  const service = new DesktopService({ registry: [{ ...f.policy, name: 'Synthetic module', workflowBlobSha: 'a'.repeat(40) }],
    request: f.request, installer, now: () => f.nowMs, confirmDevelopment: async () => true, inspectPackage: async () => true, assertGameClosed: async () => true,
    assetTransport: async request => ({ url: request.url, redirected: false, status: 200, headers: {}, body: (async function* () { yield payload; })() }) });
  service.provider='module-release';
  const installed = await service.execute('install', ['example']); assert.equal(installed.results[0].status, 'committed');
  assert.equal(fs.readFileSync(path.join(installer.root, 'example', 'Example.dll'), 'utf8'), payload.toString());
  assert.equal((await service.execute('update', ['example'])).results[0].status, 'current');
  assert.equal((await service.execute('uninstall', ['example'])).results[0].status, 'committed');
  assert.equal(fs.existsSync(path.join(installer.root, 'example')), false);
});
test('one pooled API waiter cancellation does not abort another module sharing that request', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  let finish; let transportSignal; let count = 0;
  const pool = new GitHubRequestPool(options => { count++; transportSignal = options.signal; return new Promise(resolve => { finish = resolve; }); });
  const first = new AbortController(); const second = new AbortController();
  const options = { url: 'https://api.github.com/repos/Owner/Repo/releases?per_page=100&page=1' };
  const waitingOne = pool.request({ ...options, signal: first.signal });
  const waitingTwo = pool.request({ ...options, signal: second.signal });
  first.abort();
  assert.equal(transportSignal.aborted, false);
  finish({ status: 200, url: options.url, redirected: false, headers: {}, body: [] });
  await assert.rejects(waitingOne); assert.equal((await waitingTwo).status, 200); assert.equal(count, 1);
});
test('same-branch manifest enables a previously unmapped module without App source edits', async () => {
  const { resolveGitHubPolicy } = await import('./module-policy.mjs'); const f = nativeSourceFixture();
  const entry = { moduleId: 'example', name: 'Synthetic', repository: f.policy.repository, branch: f.policy.branch };
  const manifest = { schema: 1, ...entry, sourceKind: 'module-release', assembly: 'Example', workflowPath: '.github/workflows/dev.yml',
    releaseTagPrefix: 'Example-dev-', development: { devTools: true,provenanceAssetName:'build-info.json' }, runtimeAssets: [{ name: 'Example.dll', assemblyName: 'Example' }] };
  const bytes = Buffer.from(JSON.stringify(manifest)); const commit = 'b'.repeat(40);
  const key = `${f.base}/contents/dev-manager.manifest.json?ref=${commit}`;
  const blob = value => { const bytes = Buffer.from(JSON.stringify(value)); return { encoding: 'base64', content: bytes.toString('base64'), size: bytes.length,
    sha: createHash('sha1').update(`blob ${bytes.length}\0`).update(bytes).digest('hex') }; };
  f.routes.set(key, blob(manifest));
  const policy = await resolveGitHubPolicy(entry, f.request); assert.equal(policy.policyOrigin, 'branch-manifest');
  assert.deepEqual(policy.runtimeAssetNames, ['Example.dll']); assert.equal(policy.expectedAssemblies['Example.dll'], 'Example');
  f.routes.set(key, blob({ ...manifest, branch: 'Other' }));
  assert.equal((await resolveGitHubPolicy(entry, f.request)).runtimeAssetNames, undefined);
});
test('engineering scheduling never prevents removing an existing owned module', async () => {
  const fs = await import('node:fs'); const os = await import('node:os'); const path = await import('node:path');
  const { DesktopService } = await import('./desktop-service.mjs'); const { IsolatedInstaller } = await import('./filesystem.mjs');
  const installer = new IsolatedInstaller(fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-'))); installer.install(pkg());
  const service = new DesktopService({ installer, registry: [{ moduleId: 'example', name: 'Synthetic', engineeringPaused: true }], assertGameClosed: async () => true });
  assert.equal(service.rows()[0].canUninstall, true);
  assert.equal((await service.execute('uninstall', ['example'])).results[0].status, 'committed');
});

let nativeFixtureBuild;
async function buildNativeFixtures() {
  if (!nativeFixtureBuild) nativeFixtureBuild = (async () => {
    const fs = await import('node:fs'); const os = await import('node:os'); const path = await import('node:path');
    const { execFileSync } = await import('node:child_process');
    const root = fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-'));
    const compiler = path.join(process.env.WINDIR, 'Microsoft.NET', 'Framework64', 'v4.0.30319', 'csc.exe');
    const helper = path.join(root, 'MetadataHelper.exe'); const dll = path.join(root, 'IdentityFixture.dll');
    execFileSync(compiler, ['/nologo','/codepage:65001','/target:winexe','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll','/reference:System.IO.Compression.dll',
      `/out:${helper}`, path.resolve('desktop/DevManager.cs')], { windowsHide: true });
    execFileSync(compiler, ['/nologo','/target:library',`/out:${dll}`,path.resolve('desktop/IdentityFixture.cs')], { windowsHide: true });
    return { helper, dll, root };
  })();
  return nativeFixtureBuild;
}
test('native metadata helper verifies actual managed bytes and rejects wrong identities or corrupt input', { skip: process.platform !== 'win32' }, async () => {
  const { inspectAssemblyBytes } = await import('./desktop-backend.mjs');
  const fixture = await buildNativeFixtures(); const bytes = readFileSync(fixture.dll);
  assert.equal(await inspectAssemblyBytes(bytes, 'IdentityFixture', fixture.helper), true);
  assert.equal(await inspectAssemblyBytes(bytes, 'WrongModule', fixture.helper), false);
  assert.equal(await inspectAssemblyBytes(Buffer.from('not a PE file'), 'IdentityFixture', fixture.helper), false);
});
test('HTTP socket to assembly inspection to direct Mods folders supports install, update, rollback and quarantine removal', { skip: process.platform !== 'win32' }, async t => {
  const http = await import('node:http'); const fs = await import('node:fs'); const path = await import('node:path');
  const { DesktopService } = await import('./desktop-service.mjs'); const { IsolatedInstaller } = await import('./filesystem.mjs');
  const { inspectAssemblyBytes } = await import('./desktop-backend.mjs'); const fixture = await buildNativeFixtures();
  const f = nativeSourceFixture(); const payload = fs.readFileSync(fixture.dll);
  f.policy.assembly = 'IdentityFixture'; f.policy.runtimeAssetNames = ['IdentityFixture.dll']; f.assets.splice(1);
  Object.assign(f.assets[0], { name: 'IdentityFixture.dll', size: payload.length, digest: `sha256:${createHash('sha256').update(payload).digest('hex')}`,
    browser_download_url: `https://github.com/${f.policy.repository}/releases/download/${f.tag}/IdentityFixture.dll` });
  const server = http.createServer((req, res) => {
    const url = new URL(req.url, 'http://127.0.0.1');
    if (url.pathname === '/asset') { res.write(payload.subarray(0,100)); res.end(payload.subarray(100)); }
    else { const body = f.routes.get(url.searchParams.get('url')); if (!body) { res.statusCode=404; res.end('{}'); } else res.end(JSON.stringify(body)); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve)); t.after(() => server.close());
  const origin = `http://127.0.0.1:${server.address().port}`;
  const request = options => new Promise((resolve, reject) => {
    const req = http.get(`${origin}/json?url=${encodeURIComponent(options.url)}`, { signal: options.signal }, response => {
      const chunks=[]; response.on('data', chunk=>chunks.push(chunk)); response.on('end',()=>resolve({url:options.url,status:response.statusCode,redirected:false,headers:response.headers,body:JSON.parse(Buffer.concat(chunks))})); response.on('error',reject);
    }); req.on('error',reject);
  });
  const transport = options => new Promise((resolve,reject) => {
    const req = http.get(`${origin}/asset`, {signal:options.signal}, response=>resolve({url:options.url,status:200,redirected:false,headers:response.headers,body:response})); req.on('error',reject);
  });
  const mods = path.join(fixture.root, 'SyntheticGameMods'); fs.mkdirSync(mods); fs.mkdirSync(path.join(mods,'UserMod')); fs.writeFileSync(path.join(mods,'UserMod','keep.txt'),'personal');
  let fail = false;
  const installer = IsolatedInstaller.forModsDirectory(mods,{confirmed:true,create:true,fault:point=>{if(fail && point==='before-state') throw new Error('injected transaction failure');}});
  const service = new DesktopService({ registry:[{...f.policy,name:'Synthetic identity fixture',workflowBlobSha:'a'.repeat(40)}], request, assetTransport:transport, installer,
    now:()=>f.nowMs, confirmDevelopment:async()=>true, assertGameClosed:async()=>true,
    inspectPackage:async pkg=>inspectAssemblyBytes(pkg.files[0].bytes,'IdentityFixture',fixture.helper) });
  service.provider='module-release';
  assert.equal((await service.execute('install',['example'])).results[0].status,'committed');
  const active = path.join(mods,'CS1DevManager-example','IdentityFixture.dll'); assert.deepEqual(fs.readFileSync(active),payload);
  const advance = runId => {
    const tag=`Example-dev-${runId}-1`; Object.assign(f.release,{id:runId+1000,tag_name:tag,published_at:`2026-11-03T00:00:${runId-90}Z`,url:`${f.base}/releases/${runId+1000}`,html_url:`https://github.com/${f.policy.repository}/releases/tag/${tag}`});
    f.assets[0].browser_download_url=`https://github.com/${f.policy.repository}/releases/download/${tag}/IdentityFixture.dll`; f.run.id=runId; f.run.run_started_at=f.release.published_at;
    f.routes.set(`${f.base}/releases/${runId+1000}`,f.release); f.routes.set(`${f.base}/releases/${runId+1000}/assets?per_page=100&page=1`,f.assets);
    f.routes.set(`${f.base}/actions/runs/${runId}/attempts/1`,f.run); f.routes.set(`${f.base}/git/ref/tags/${tag}`,{ref:`refs/tags/${tag}`,object:{type:'commit',sha:f.sha}});
  };
  advance(101); await service.refresh();assert.equal(service.rows()[0].canRequestUpdate,true);
  assert.equal((await service.execute('update',['example'])).results[0].status,'committed');
  advance(102); fail=true; const failed=await service.execute('update',['example']); assert.equal(failed.results[0].status,'failed');
  assert.equal(installer.installed('example').buildId,'Example-dev-101-1'); assert.deepEqual(fs.readFileSync(active),payload);
  fail=false; assert.equal((await service.execute('uninstall',['example'])).results[0].status,'committed');
  assert.equal(fs.existsSync(active),false); assert.equal(fs.readFileSync(path.join(mods,'UserMod','keep.txt'),'utf8'),'personal');
  assert.deepEqual(fs.readdirSync(mods),['UserMod']); assert.equal(path.dirname(installer.meta),path.dirname(mods));
});
test('automatic fifteen-module discovery uses one shared request with no release and never labels candidates verified', async () => {
  const { discoverModuleReleases } = await import('./release-index.mjs'); const { moduleRegistry } = await import('./registry.mjs'); let calls=0;
  const empty = await discoverModuleReleases(moduleRegistry, async options => {calls++;return {url:options.url,status:200,redirected:false,headers:{},body:[]};});
  assert.equal(calls,1); assert.equal(empty.length,15); assert.ok(empty.every(r=>r.status==='unknown'));
  const f=nativeSourceFixture(); const rows=await discoverModuleReleases([f.policy], f.request, f.nowMs);
  assert.equal(rows[0].status,'candidate / verification required'); assert.equal(rows[0].developmentConfirmed,undefined);
});
test('discovery never falls back behind invalid or future same-module publication metadata', async () => {
  const { discoverModuleReleases } = await import('./release-index.mjs');
  for (const timestamp of ['not a date','2099-01-01T00:00:00Z']) {
    const f=nativeSourceFixture(); f.routes.set(`${f.base}/releases?per_page=100&page=1`,[f.release,{...f.release,id:43,tag_name:'Example-dev-101-1',published_at:timestamp}]);
    const rows=await discoverModuleReleases([f.policy],f.request,f.nowMs); assert.equal(rows[0].status,'unknown');
    assert.equal(rows[0].discoveryCandidate,undefined);
  }
});

test('automatic installed-module refresh resolves tag and run before enabling a forward update', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const f=nativeSourceFixture();
  const installed={moduleId:'example',buildId:'Example-dev-99-1',publication:{sourceSha:f.sha,publishedAt:'2026-11-02T00:00:00Z',runId:99,attempt:1,buildStartedAt:'2026-11-02T00:00:00Z'}};
  const service=new DesktopService({registry:[{...f.policy,name:'Synthetic'}],request:f.request,now:()=>f.nowMs,installer:{audit:()=>({installed})},confirmDevelopment:async()=>true});
  service.provider='module-release';
  await service.refresh();
  assert.equal(service.rows()[0].canRequestUpdate,true);
  assert.equal(service.sources.get('example').latestPublished.branch,'Example');
  assert.ok(f.calls.includes(`${f.base}/git/ref/tags/${f.tag}`));
  f.run.run_started_at='2026-11-01T00:00:00Z';
  await service.refresh(); assert.equal(service.rows()[0].canRequestUpdate,false);
});

test('individual release requires hash-verified DevTools provenance bound to the selected successful run',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');
  const f=nativeSourceFixture();const policy={...f.policy,provenanceAssetName:'build-info.json'};
  let info={sourceSHA:f.sha,runId:100,runAttempt:1,DevTools:true};let corrupt=false;
  const service=new DesktopService({registry:[policy],request:f.request,now:()=>f.nowMs,
    assetTransport:async options=>({url:options.url,status:200,redirected:false,headers:{},body:(async function*(){yield corrupt?Buffer.from('corrupt'):Buffer.from(JSON.stringify(info));})()})});
  const configure=()=>{const bytes=Buffer.from(JSON.stringify(info));const asset={id:202,name:'build-info.json',size:bytes.length,state:'uploaded',digest:`sha256:${createHash('sha256').update(bytes).digest('hex')}`,url:`${f.base}/releases/assets/202`,browser_download_url:`https://github.com/${f.policy.repository}/releases/download/${f.tag}/build-info.json`}; f.assets.splice(2,1,asset);};
  service.provider='module-release';
  configure(); await service.refresh(['example'],{full:true});assert.equal(service.sources.get('example').developmentConfirmed,true);
  info={...info,runId:99};configure();await service.refresh(['example'],{full:true});assert.equal(service.sources.get('example').developmentConfirmed,false);
  info={...info,runId:100,DevTools:false};configure();await service.refresh(['example'],{full:true});assert.equal(service.sources.get('example').developmentConfirmed,false);
  info={...info,DevTools:true};configure();corrupt=true;await service.refresh(['example'],{full:true});assert.equal(service.sources.get('example').developmentConfirmed,false);
  corrupt=false;f.assets.splice(2);await service.refresh(['example'],{full:true});assert.equal(service.sources.get('example').developmentConfirmed,false);
});

function storedZip(entries) {
  const locals=[];const centrals=[];let offset=0;
  for(const entry of entries) {
    const name=Buffer.from(entry.path);const data=Buffer.from(entry.bytes);let crc=0xffffffff;
    for(const byte of data){crc^=byte;for(let i=0;i<8;i++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}crc=(crc^0xffffffff)>>>0;
    const local=Buffer.alloc(30);local.writeUInt32LE(0x04034b50);local.writeUInt16LE(20,4);local.writeUInt32LE(crc,14);local.writeUInt32LE(data.length,18);local.writeUInt32LE(data.length,22);local.writeUInt16LE(name.length,26);
    locals.push(local,name,data);const central=Buffer.alloc(46);central.writeUInt32LE(0x02014b50);central.writeUInt16LE(20,4);central.writeUInt16LE(20,6);central.writeUInt32LE(crc,16);central.writeUInt32LE(data.length,20);central.writeUInt32LE(entry.declaredSize??data.length,24);central.writeUInt16LE(name.length,28);central.writeUInt32LE(entry.attributes??0,38);central.writeUInt32LE(offset,42);centrals.push(central,name);offset+=local.length+name.length+data.length;
  }
  const directory=Buffer.concat(centrals);const end=Buffer.alloc(22);end.writeUInt32LE(0x06054b50);end.writeUInt16LE(entries.length,8);end.writeUInt16LE(entries.length,10);end.writeUInt32LE(directory.length,12);end.writeUInt32LE(offset,16);return Buffer.concat([...locals,directory,end]);
}

function artifactFixture() {
  const f=nativeSourceFixture();const artifact='c'.repeat(40);const payload=Buffer.from('synthetic artifact bytes');
  const entry={...f.policy,name:'Synthetic artifact',runtimeAssetNames:['Example.dll','LICENSE']};
  const digest=createHash('sha256').update(payload).digest('hex');
  const manifest={schema:1,sourceKind:'artifact-branch',moduleId:'example',repository:entry.repository,sourceBranch:'Example',artifactBranch:'Development_Build/Example',
    sourceSha:f.sha,assembly:'Example',workflowPath:entry.workflowPath,runId:100,attempt:1,buildId:'Example-dev-100-1',devTools:true,
    files:[{path:'files/Example.dll',name:'Example.dll',assemblyName:'Example',size:payload.length,sha256:digest},{path:'files/LICENSE',name:'LICENSE',assemblyName:null,size:payload.length,sha256:digest}]};
  const refUrl=`${f.base}/git/ref/heads/Development_Build/Example`;let currentArtifact=artifact;
  const configure=()=>{
    const bytes=Buffer.from(JSON.stringify(manifest));f.routes.set(refUrl,{ref:'refs/heads/Development_Build/Example',object:{type:'commit',sha:currentArtifact}});
    f.routes.set(`${f.base}/contents/manifest.json?ref=${currentArtifact}`,{type:'file',encoding:'base64',content:bytes.toString('base64'),size:bytes.length,sha:createHash('sha1').update(`blob ${bytes.length}\0`).update(bytes).digest('hex')});
    f.routes.set(`${f.base}/git/ref/heads/Example`,{ref:'refs/heads/Example',object:{type:'commit',sha:f.sha}});
    f.routes.set(`${f.base}/git/matching-refs/heads/`,[f.routes.get(refUrl),f.routes.get(`${f.base}/git/ref/heads/Example`)]);
    f.run.id=manifest.runId;f.run.run_attempt=manifest.attempt;f.run.updated_at=new Date(Date.parse(f.run.run_started_at)+10000).toISOString();f.routes.set(`${f.base}/actions/runs/${manifest.runId}/attempts/${manifest.attempt}`,{...f.run});
  };configure();
  const transport=async options=>({url:options.url,status:200,redirected:false,headers:{},body:(async function*(){yield payload;})()});
  return {...f,entry,manifest,artifact,payload,configure,transport,advanceArtifact:sha=>{currentArtifact=sha;configure();}};
}

test('artifact branch pins manifest and downloads each required file without Releases',async()=>{
  const {detectArtifactModule,downloadArtifactFile}=await import('./artifact-source.mjs');const f=artifactFixture();
  const source=await detectArtifactModule({entry:f.entry,request:f.request,nowMs:f.nowMs});
  assert.equal(source.developmentConfirmed,true);assert.equal(source.latestPublished.artifactCommit,f.artifact);
  assert.equal(source.latestPublished.releaseId,undefined);assert.ok(f.calls.every(url=>!url.includes('/releases')));
  for(const asset of source.latestPublished.assets){assert.ok(asset.url.includes(`/${f.artifact}/`));const result=await downloadArtifactFile({repository:f.entry.repository,artifactCommit:f.artifact,asset,transport:f.transport});assert.deepEqual(result.bytes,f.payload);}
});

test('startup defaults to independent artifact branches and never falls back to available legacy Releases',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const f=artifactFixture();
  const service=new DesktopService({registry:[f.entry],request:f.request,now:()=>f.nowMs});
  assert.equal(service.provider,'artifact-branch');await service.refresh();assert.equal(service.rows()[0].canRequestInstall,true);
  assert.ok(f.calls.every(url=>!url.includes('/releases')));
  const old=nativeSourceFixture();const missing=new DesktopService({registry:[old.policy],request:old.request,now:()=>old.nowMs,confirmDevelopment:async()=>true});
  await missing.refresh();assert.equal(missing.provider,'artifact-branch');assert.equal(missing.rows()[0].canRequestInstall,false);
  assert.ok(old.calls.every(url=>!url.includes('/releases')));
  missing.provider='module-release';await missing.refresh(['example'],{full:true});assert.equal(missing.rows()[0].canRequestInstall,true);
});

test('artifact discovery distinguishes matching installed builds and keeps known publisher mapping during a queued check',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const f=artifactFixture();
  const entry={...f.entry,publisherConfigured:false};const installed={moduleId:entry.moduleId,buildId:f.manifest.buildId};
  const service=new DesktopService({registry:[entry],request:f.request,now:()=>f.nowMs,installer:{audit:()=>({installed})}});
  await service.refresh();const row=service.rows()[0];assert.equal(row.matchesCandidate,true);assert.equal(row.canRequestUpdate,false);assert.equal(row.configurationPending,false);
  service.sources.set(entry.moduleId,{status:'queued / verification required',checkedAtMs:f.nowMs,latestPublished:{status:'unknown'},reason:'Queued for source verification: the shared request budget is exhausted.'});
  assert.equal(service.rows()[0].configurationPending,false);assert.equal(service.rows()[0].canRequestInstall,false);assert.equal(service.rows()[0].canRequestUpdate,false);
  service.now=()=>f.nowMs+3600001;assert.equal(service.rows()[0].matchesCandidate,false);
});

function fifteenArtifactFixture(){
  const f=artifactFixture();const registry=[];const refs=[];const installed=new Map();
  for(let i=0;i<15;i++){
    const branch=`Example${i}`;const artifact=(i+10).toString(16).padStart(40,'0');const prior=(i+100).toString(16).padStart(40,'0');
    const entry={...f.entry,moduleId:`example-${i}`,name:`Example ${i}`,branch};registry.push(entry);
    const manifest={...f.manifest,moduleId:entry.moduleId,sourceBranch:branch,artifactBranch:`Development_Build/${branch}`,runId:100+i,buildId:`${branch}-dev-${100+i}-1`};
    const bytes=Buffer.from(JSON.stringify(manifest));f.routes.set(`${f.base}/contents/manifest.json?ref=${artifact}`,{type:'file',encoding:'base64',content:bytes.toString('base64'),size:bytes.length,sha:createHash('sha1').update(`blob ${bytes.length}\0`).update(bytes).digest('hex')});
    refs.push({ref:`refs/heads/${branch}`,object:{type:'commit',sha:f.sha}},{ref:`refs/heads/Development_Build/${branch}`,object:{type:'commit',sha:artifact}});
    f.routes.set(`${f.base}/actions/runs/${100+i}/attempts/1`,{...f.run,id:100+i,head_branch:branch,run_started_at:'2026-11-03T00:00:10Z',updated_at:'2026-11-03T00:00:20Z'});
    f.routes.set(`${f.base}/compare/${prior}...${artifact}`,{status:'ahead',base_commit:{sha:prior},ahead_by:1,behind_by:0});
    installed.set(entry.moduleId,{moduleId:entry.moduleId,buildId:`${branch}-dev-${50+i}-1`,publication:{provider:'artifact-branch',artifactBranch:`Development_Build/${branch}`,artifactCommit:prior,manifestSha256:'a'.repeat(64),sourceSha:f.sha,runId:50+i,attempt:1,publishedAt:'2026-11-02T00:00:20Z',buildStartedAt:'2026-11-02T00:00:10Z'}});
  }
  f.routes.set(`${f.base}/git/matching-refs/heads/`,refs);return {...f,registry,installed};
}

test('fifteen available artifacts need 17 initial API calls and two unchanged automatic checks, with full verification before install',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const f=fifteenArtifactFixture();const service=new DesktopService({registry:f.registry,request:f.request,now:()=>f.nowMs});
  const initial=await service.refresh();assert.equal(initial.discovery.requests,17);assert.equal(f.calls.length,17);assert.equal(service.rows().filter(row=>row.canRequestInstall).length,15);
  assert.ok([...service.sources.values()].every(source=>source.developmentConfirmed===false));assert.ok(f.calls.every(url=>!url.includes('/actions/runs/')));
  f.calls.length=0;const unchanged=await service.refresh();assert.equal(unchanged.discovery.requests,2);assert.equal(f.calls.length,2);
  f.calls.length=0;await service.refresh(f.registry.map(entry=>entry.moduleId),{full:true});assert.equal(f.calls.length,32);
  assert.ok([...service.sources.values()].every(source=>source.developmentConfirmed===true));assert.equal(f.calls.filter(url=>url.includes('/actions/runs/')).length,15);
});

test('low-budget automatic artifact discovery rotates all fifteen modules without fixed-order starvation',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const f=fifteenArtifactFixture();const service=new DesktopService({registry:f.registry,request:f.request,now:()=>f.nowMs,automaticRequestLimit:4});const seen=new Set();
  for(let round=0;round<9;round++){
    f.calls.length=0;const result=await service.refresh();assert.ok(f.calls.length<=4);assert.ok(result.discovery.requests<=4);
    service.rows().filter(row=>row.canRequestInstall).forEach(row=>seen.add(row.moduleId));
    if(round===0)assert.ok(service.rows().some(row=>row.status.includes('queued')));
  }
  assert.equal(seen.size,15);assert.equal(service.rows().filter(row=>row.canRequestInstall).length,15);
});

test('fifteen installed changed modules receive fair bounded update verification and stable cached ordering',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const f=fifteenArtifactFixture();const service=new DesktopService({registry:f.registry,request:f.request,now:()=>f.nowMs,installer:{audit:id=>({installed:f.installed.get(id)})}});
  for(let round=0;round<5;round++){f.calls.length=0;const result=await service.refresh();assert.ok(f.calls.length<=40);assert.ok(result.discovery.requests<=40);}
  assert.equal(service.rows().filter(row=>row.canRequestUpdate).length,15);
  f.calls.length=0;await service.refresh();assert.equal(f.calls.length,2);
});

test('shared GitHub pool caps actual hourly calls across refreshes and resumes after reset',async()=>{
  const {GitHubRequestPool}=await import('./https-source.mjs');let clock=0;let calls=0;
  const pool=new GitHubRequestPool(async options=>{calls++;return {url:options.url,status:200,redirected:false,headers:{},body:{}};},{hourlyLimit:3,now:()=>clock});
  for(let i=0;i<5;i++){pool.beginRefresh();const result=await pool.request({url:`https://api.github.com/repos/Owner/Repo/test/${i}`});assert.equal(result.status,i<3?200:429);}
  assert.equal(calls,3);assert.equal(pool.status().localRemaining,0);assert.ok(pool.status().retryAtMs>clock);
  clock=3600002;pool.beginRefresh();assert.equal((await pool.request({url:'https://api.github.com/repos/Owner/Repo/test/resume'})).status,200);assert.equal(calls,4);
});

test('fifteen-module automatic queue remains fair across actual shared-pool hourly budget windows',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const {GitHubRequestPool}=await import('./https-source.mjs');const f=fifteenArtifactFixture();let clock=f.nowMs;
  const pool=new GitHubRequestPool(f.request,{hourlyLimit:4,now:()=>clock});
  const service=new DesktopService({registry:f.registry,request:pool.request.bind(pool),beginRefresh:()=>pool.beginRefresh(),rateStatus:()=>pool.status(),now:()=>clock,automaticRequestLimit:4});const seen=new Set();
  for(let round=0;round<9;round++){
    f.calls.length=0;await service.refresh();assert.ok(f.calls.length<=4);service.rows().filter(row=>row.canRequestInstall).forEach(row=>seen.add(row.moduleId));clock+=3600002;
  }
  assert.equal(seen.size,15);
});

test('shared artifact snapshot changes invalidate discovered candidates and full verification together',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const f=fifteenArtifactFixture();
  const request=async options=>{const response=await f.request(options);if(options.cachePhase==='confirm')response.body[1].object.sha='f'.repeat(40);return response;};
  const service=new DesktopService({registry:f.registry,request,now:()=>f.nowMs});await service.refresh();assert.equal(service.rows()[0].canRequestInstall,false);assert.match(service.rows()[0].reason,/changed/);
  await service.refresh(['example-0'],{full:true});assert.equal(service.rows()[0].canInstall,false);
});

test('artifact manifest accepts versioned build subpaths, accompanying notices and an optional author signature',async()=>{
  const {detectArtifactModule}=await import('./artifact-source.mjs');const f=artifactFixture();
  f.manifest.signature='OpenAI-Codex';
  f.manifest.files.forEach(file=>file.path=`builds/100-1/${file.name}`);
  for(const name of ['README.md','Steam_Workshop_Link.txt','build-info.json','build-info.txt','THIRD-PARTY-NOTICES.md'])
    f.manifest.files.push({path:`builds/100-1/${name}`,name,assemblyName:null,size:f.payload.length,sha256:createHash('sha256').update(f.payload).digest('hex')});
  f.configure();const source=await detectArtifactModule({entry:f.entry,request:f.request,nowMs:f.nowMs});
  assert.equal(source.developmentConfirmed,true);assert.equal(source.latestPublished.assets.length,7);
  assert.ok(source.latestPublished.assets.every(file=>file.url.includes(`/${f.artifact}/builds/100-1/`)));
});

test('artifact source rejects wrong identity, source, run, DevTools, required dependency and mutable snapshots',async()=>{
  const {detectArtifactModule}=await import('./artifact-source.mjs');
  for(const edit of [f=>f.manifest.moduleId='another',f=>f.manifest.sourceBranch='Other',f=>f.manifest.artifactBranch='Development_Build/Other',f=>f.manifest.sourceSha='b'.repeat(40),
    f=>f.manifest.devTools=false,f=>f.manifest.assembly='Wrong',f=>f.manifest.workflowPath='.github/workflows/wrong.yml',f=>f.manifest.files.pop(),
    f=>f.manifest.files[0].path='../bad',f=>f.manifest.files.push({...f.manifest.files[0],name:'EXAMPLE.dll'}),f=>f.run.conclusion='failure',f=>f.run.head_repository.full_name='Other/Repo']){
    const f=artifactFixture();edit(f);f.configure();const result=await detectArtifactModule({entry:f.entry,request:f.request,nowMs:f.nowMs});assert.equal(result.developmentConfirmed,false,String(edit));
  }
  const f=artifactFixture();const original=f.request;const request=async options=>{const result=await original(options);if(options.cachePhase==='confirm')result.body.object.sha='e'.repeat(40);return result;};
  assert.equal((await detectArtifactModule({entry:f.entry,request,nowMs:f.nowMs})).status,'unknown');
});

test('artifact transfer rejects redirects, tampering, truncation, oversize and aborts stalled downloads',async()=>{
  const {detectArtifactModule,downloadArtifactFile}=await import('./artifact-source.mjs');const f=artifactFixture();const source=await detectArtifactModule({entry:f.entry,request:f.request,nowMs:f.nowMs});const asset=source.latestPublished.assets[0];
  for(const edit of [r=>r.status=302,r=>r.redirected=true,r=>r.body=(async function*(){yield Buffer.from('wrong');})(),r=>r.body=(async function*(){yield Buffer.concat([f.payload,Buffer.from('x')]);})()]){
    const transport=async options=>{const response=await f.transport(options);edit(response);return response;};await assert.rejects(downloadArtifactFile({repository:f.entry.repository,artifactCommit:f.artifact,asset,transport}));
  }
  let signal;await assert.rejects(downloadArtifactFile({repository:f.entry.repository,artifactCommit:f.artifact,asset,timeoutMs:20,transport:options=>{signal=options.signal;return new Promise(()=>{});}}));assert.equal(signal.aborted,true);
});

test('artifact provider supports isolated install, automatic forward update and legacy removal',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const {IsolatedInstaller}=await import('./filesystem.mjs');const fs=await import('node:fs');const os=await import('node:os');const path=await import('node:path');const f=artifactFixture();
  const installer=new IsolatedInstaller(fs.mkdtempSync(path.join(os.tmpdir(),'cs1-dev-manager-test-')));
  const service=new DesktopService({registry:[f.entry],request:f.request,assetTransport:f.transport,installer,now:()=>f.nowMs,inspectPackage:async()=>true,assertGameClosed:async()=>true});service.provider='artifact-branch';
  await service.refresh();assert.equal(service.rows()[0].canRequestInstall,true);
  assert.equal((await service.execute('install',['example'])).results[0].status,'committed');const prior=installer.installed('example');assert.equal(prior.publication.artifactCommit,f.artifact);assert.equal(prior.publication.releaseId,undefined);
  const next='d'.repeat(40);f.manifest.runId=101;f.manifest.buildId='Example-dev-101-1';f.run.run_started_at='2026-11-03T00:00:05Z';f.advanceArtifact(next);
  f.routes.set(`${f.base}/compare/${f.artifact}...${next}`,{status:'ahead',base_commit:{sha:f.artifact},ahead_by:1,behind_by:0});
  await service.refresh();assert.equal(service.rows()[0].canRequestUpdate,true);assert.equal((await service.execute('update',['example'])).results[0].status,'committed');assert.equal(installer.installed('example').publication.artifactCommit,next);
  // Re-publishing an older build cannot become an update even on a newer artifact commit.
  const older='e'.repeat(40);f.manifest.runId=100;f.manifest.buildId='Example-dev-100-1';f.run.run_started_at='2026-11-02T00:00:00Z';f.advanceArtifact(older);
  f.routes.set(`${f.base}/compare/${next}...${older}`,{status:'ahead',base_commit:{sha:next},ahead_by:1,behind_by:0});await service.refresh();assert.equal(service.rows()[0].canRequestUpdate,false);
  f.routes.clear();assert.equal((await service.execute('uninstall',['example'])).results[0].status,'committed');
  installer.install(pkg());assert.equal(service.rows()[0].canUninstall,true);assert.equal((await service.execute('uninstall',['example'])).results[0].status,'committed');
});

test('artifact source accepts an older source still on the declared branch and refuses divergent ancestry',async()=>{
  const {detectArtifactModule}=await import('./artifact-source.mjs');const f=artifactFixture();
  f.routes.set(`${f.base}/git/ref/heads/Example`,{ref:'refs/heads/Example',object:{type:'commit',sha:'b'.repeat(40)}});
  const key=`${f.base}/compare/${f.sha}...${'b'.repeat(40)}`;f.routes.set(key,{status:'ahead',base_commit:{sha:f.sha},ahead_by:1,behind_by:0});
  assert.equal((await detectArtifactModule({entry:f.entry,request:f.request,nowMs:f.nowMs})).developmentConfirmed,true);
  f.routes.set(key,{status:'diverged',base_commit:{sha:f.sha},ahead_by:1,behind_by:1});assert.equal((await detectArtifactModule({entry:f.entry,request:f.request,nowMs:f.nowMs})).status,'unknown');
});

test('artifact downloads pass actual managed identity inspection before direct Mods installation',{skip:process.platform!=='win32'},async()=>{
  const fixture=await buildNativeFixtures();const f=artifactFixture();const fs=await import('node:fs');const path=await import('node:path');
  const {DesktopService}=await import('./desktop-service.mjs');const {IsolatedInstaller}=await import('./filesystem.mjs');const {inspectAssemblyBytes}=await import('./desktop-backend.mjs');
  const dll=fs.readFileSync(fixture.dll);f.entry.assembly='IdentityFixture';f.entry.runtimeAssetNames=['IdentityFixture.dll','LICENSE'];f.manifest.assembly='IdentityFixture';
  Object.assign(f.manifest.files[0],{path:'files/IdentityFixture.dll',name:'IdentityFixture.dll',assemblyName:'IdentityFixture',size:dll.length,sha256:createHash('sha256').update(dll).digest('hex')});f.configure();
  const mods=path.join(fixture.root,'ArtifactSyntheticMods');fs.mkdirSync(mods);const installer=IsolatedInstaller.forModsDirectory(mods,{confirmed:true,create:true});
  const service=new DesktopService({registry:[f.entry],request:f.request,installer,now:()=>f.nowMs,assertGameClosed:async()=>true,
    inspectPackage:async pkg=>inspectAssemblyBytes(pkg.files.find(file=>file.path.endsWith('.dll')).bytes,'IdentityFixture',fixture.helper),
    assetTransport:async options=>({url:options.url,status:200,redirected:false,headers:{},body:(async function*(){yield options.url.endsWith('.dll')?dll:f.payload;})()})});service.provider='artifact-branch';
  assert.equal((await service.execute('install',['example'])).results[0].status,'committed');assert.deepEqual(fs.readFileSync(path.join(mods,'CS1DevManager-example','IdentityFixture.dll')),dll);
  assert.equal((await service.execute('uninstall',['example'])).results[0].status,'committed');assert.deepEqual(fs.readdirSync(mods),[]);
});

test('native archive reader accepts bounded bytes and rejects unsafe paths, duplicates, links and expansion sizes',{skip:process.platform!=='win32'},async()=>{
  const {inspectZipBytes}=await import('./desktop-backend.mjs');const fixture=await buildNativeFixtures();
  const entries=await inspectZipBytes(storedZip([{path:'Module/mod.dll',bytes:'bytes'}]),fixture.helper);
  assert.equal(entries[0].path,'Module/mod.dll');assert.equal(Buffer.from(entries[0].bytes,'base64').toString(),'bytes');
  for(const path of ['../outside','Module//','NUL/','Module/<bad>/','/root','Module\\bad','Module/trailing.'])
    await assert.rejects(inspectZipBytes(storedZip([{path,bytes:'x'}]),fixture.helper),path);
  await assert.rejects(inspectZipBytes(storedZip([{path:'a',bytes:'x'},{path:'A',bytes:'x'}]),fixture.helper));
  await assert.rejects(inspectZipBytes(storedZip([{path:'a',bytes:'x'},{path:'a/b',bytes:'x'}]),fixture.helper));
  await assert.rejects(inspectZipBytes(storedZip([{path:'a',bytes:'x',attributes:0xa0000000}]),fixture.helper));
  await assert.rejects(inspectZipBytes(storedZip([{path:'a',bytes:'x',declaredSize:33554433}]),fixture.helper));
});

test('update ordering rejects legacy records, withdrawn older releases, divergent sources and older same-source builds',async()=>{
  const {assessUpdateOrder}=await import('./update-order.mjs');const f=nativeSourceFixture();
  const installed={buildId:'old',publication:{sourceSha:f.sha,runId:100,attempt:1,publishedAt:'2026-11-02T01:00:00Z',buildStartedAt:'2026-11-02T00:00:00Z'}};
  const candidate={buildId:'new',sourceSha:'b'.repeat(40),branch:'Example',runId:101,attempt:1,publishedAt:'2026-11-03T01:00:00Z'};
  const url=`${f.base}/compare/${f.sha}...${candidate.sourceSha}`;
  f.routes.set(url,{status:'ahead',base_commit:{sha:f.sha},behind_by:0,ahead_by:1});
  assert.equal((await assessUpdateOrder(installed,candidate,f.policy.repository,f.request)).allowed,true);
  for(const value of [{...candidate,publishedAt:'2026-11-01T00:00:00Z'},{...candidate,sourceSha:f.sha,runId:100,attempt:1},{...candidate,sourceSha:f.sha,buildStartedAt:'2026-11-01T00:00:00Z'}])
    assert.equal((await assessUpdateOrder(installed,value,f.policy.repository,f.request)).allowed,false);
  assert.equal((await assessUpdateOrder({buildId:'legacy'},candidate,f.policy.repository,f.request)).allowed,false);
  f.routes.set(url,{status:'diverged',base_commit:{sha:f.sha},behind_by:1,ahead_by:1});assert.equal((await assessUpdateOrder(installed,candidate,f.policy.repository,f.request)).allowed,false);
  assert.equal((await assessUpdateOrder(installed,{...candidate,sourceSha:f.sha,runId:100,attempt:2},f.policy.repository,f.request)).allowed,true);
});

test('bundle checksums bind every payload and missing DevTools proof disables only its module',async()=>{
  const {verifyBundleEntries}=await import('./bundle-source.mjs');const identity='d'.repeat(64);const source='a'.repeat(40);
  const registry=['one','two'].map(id=>({moduleId:id,repository:'Owner/Repo',branch:id,assembly:'Example',workflowPath:'.github/workflows/dev.yml',bundleRuntimePaths:{'Example.dll':`${id}/Example.dll`},bundleProvenancePath:`${id}/build-info.json`}));
  const entries=[{path:'manifest.json',bytes:Buffer.from(JSON.stringify({sourceIdentity:identity,modules:registry.map(m=>({branch:m.branch,commit:source,runId:100,attempt:1}))}))}];
  registry.forEach((m,i)=>entries.push({path:`${m.moduleId}/Example.dll`,bytes:Buffer.from('synthetic')},{path:m.bundleProvenancePath,bytes:Buffer.from(JSON.stringify({sourceSHA:source,runId:100,runAttempt:1,DevTools:i===0}))}));
  entries.push({path:'SHA256SUMS',bytes:Buffer.from(entries.map(e=>`${createHash('sha256').update(e.bytes).digest('hex')}  ${e.path}`).join('\n'))});
  const modules=verifyBundleEntries(entries,registry,identity);assert.ok(modules[0].package);assert.equal(modules[1].package,undefined);
  entries[1].bytes=Buffer.from('tampered');assert.throws(()=>verifyBundleEntries(entries,registry,identity));
});

test('verified bundle downloads once and installs two actual managed modules into isolated Mods',{skip:process.platform!=='win32'},async()=>{
  const {BundleSource}=await import('./bundle-source.mjs');const {DesktopService}=await import('./desktop-service.mjs');
  const {inspectZipBytes,inspectAssemblyBytes}=await import('./desktop-backend.mjs');const {IsolatedInstaller}=await import('./filesystem.mjs');
  const fs=await import('node:fs');const path=await import('node:path');const fixture=await buildNativeFixtures();const f=nativeSourceFixture();
  const identity='d'.repeat(64);const tag=`modpack-dev-${identity}`;const payload=fs.readFileSync(fixture.dll);
  const registry=['one','two'].map((id,index)=>({moduleId:id,name:id,repository:f.policy.repository,branch:`Branch${index}`,assembly:'IdentityFixture',workflowPath:f.policy.workflowPath,
    bundleRuntimePaths:{'IdentityFixture.dll':`${id}/IdentityFixture.dll`,'LICENSE':`${id}/LICENSE`},bundleProvenancePath:`${id}/build-info.json`}));
  const records=registry.map((m,i)=>({branch:m.branch,commit:f.sha,runId:100+i,attempt:1}));
  const entries=[{path:'manifest.json',bytes:Buffer.from(JSON.stringify({sourceIdentity:identity,modules:records}))}];
  registry.forEach((m,i)=>{entries.push({path:`${m.moduleId}/IdentityFixture.dll`,bytes:payload},{path:`${m.moduleId}/LICENSE`,bytes:Buffer.from('synthetic legal notice')},
    {path:m.bundleProvenancePath,bytes:Buffer.from(JSON.stringify({sourceSHA:f.sha,runId:100+i,runAttempt:1,DevTools:true}))});f.routes.set(`${f.base}/actions/runs/${100+i}/attempts/1`,{...f.run,id:100+i,head_branch:m.branch});});
  entries.push({path:'SHA256SUMS',bytes:Buffer.from(entries.map(e=>`${createHash('sha256').update(e.bytes).digest('hex')}  ${e.path}`).join('\n')+'\n')});
  const zip=storedZip(entries);const release={...f.release,tag_name:tag};const asset={id:303,name:'Cities.Skylines_1_Modpack.zip',size:zip.length,state:'uploaded',digest:`sha256:${createHash('sha256').update(zip).digest('hex')}`,url:`${f.base}/releases/assets/303`,browser_download_url:`https://github.com/${f.policy.repository}/releases/download/${tag}/Cities.Skylines_1_Modpack.zip`};
  f.routes.set(`${f.base}/releases?per_page=100&page=1`,[release]);f.routes.set(`${f.base}/releases/42`,release);f.routes.set(`${f.base}/releases/42/assets?per_page=100&page=1`,[asset]);let downloads=0;
  const bundle=new BundleSource({registry,request:f.request,now:()=>f.nowMs,inspectArchive:b=>inspectZipBytes(b,fixture.helper),transport:async options=>{downloads++;return {url:options.url,status:200,redirected:false,headers:{},body:(async function*(){yield zip;})()};}});
  const mods=path.join(fixture.root,'SyntheticBundleMods');fs.mkdirSync(mods);const installer=IsolatedInstaller.forModsDirectory(mods,{confirmed:true,create:true});
  const service=new DesktopService({registry,request:f.request,installer,loadBundle:()=>bundle.load(),now:()=>f.nowMs,assertGameClosed:async()=>true,
    inspectPackage:async pkg=>inspectAssemblyBytes(pkg.files.find(file=>file.path.endsWith('.dll')).bytes,'IdentityFixture',fixture.helper)});service.provider='modpack-bundle';
  await service.refresh();assert.equal(service.rows().filter(r=>r.canRequestInstall).length,2);
  const result=await service.execute('install',['one','two']);assert.deepEqual(result.results.map(r=>r.status),['committed','committed']);assert.equal(downloads,1);
  for(const id of ['one','two']){assert.deepEqual(fs.readFileSync(path.join(mods,`CS1DevManager-${id}`,'IdentityFixture.dll')),payload);assert.equal(fs.readFileSync(path.join(mods,`CS1DevManager-${id}`,'LICENSE'),'utf8'),'synthetic legal notice');}
  assert.deepEqual((await service.execute('uninstall',['one','two'])).results.map(r=>r.status),['committed','committed']);assert.deepEqual(fs.readdirSync(mods),[]);
});
function ownPlanningModule(fixture, index) {
  const pkg = fixture.requests[index].package; const observed = fixture.observation.modules[index];
  observed.directoryPresent = true;
  observed.ownership = { moduleId: pkg.moduleId, buildId: pkg.buildId, files: pkg.files.map(({ path, sha256 }) => ({ path, sha256 })) };
  observed.files = [{ path: 'dependency', kind: 'directory', reparsePoint: false },
    ...pkg.files.map(({ path, sha256 }) => ({ path, sha256, kind: 'file', hardLink: false, reparsePoint: false }))];
}
test('operation planner checks complete synthetic packages but never authorizes execution', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); const plan = planModuleOperations(f);
  assert.equal(plan.modelReady, true); assert.equal(plan.executionAllowed, false);
  assert.equal(plan.steps.length, 2); assert.ok(plan.warnings.includes('DLL_IDENTITY_UNVERIFIED'));
  assert.equal(f.observation.modules[0].directoryPresent, false);
});
test('operation planner refuses unavailable sources, mismatched packages and unknown targets', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  for (const edit of [f => { f.requests[0].source.policy.catalogSha256 = '0'.repeat(64); },
    f => { f.requests[0].package.buildId = 'wrong'; }, f => { f.requests[0].package.files.pop(); },
    f => { f.observation.modules[0].directoryPresent = true; },
    f => { f.observation.complete = false; }, f => { f.observation.gameRunning = true; },
    f => { f.rules.complete = false; }, f => { f.requests.push(f.requests[0]); }]) {
    const f = planningFixture(); edit(f); const plan = planModuleOperations(f);
    assert.equal(plan.modelReady, false, String(edit)); assert.equal(plan.steps.length, 0);
  }
});
test('uninstall planner preserves modified, unknown, missing and linked content', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  for (const edit of [f => { f.observation.modules[0].files[1].sha256 = '0'.repeat(64); },
    f => { f.observation.modules[0].files[1].hardLink = true; },
    f => { f.observation.modules[0].reparsePoint = true; },
    f => { f.observation.modules[0].files.push({ path: 'personal', kind: 'directory', reparsePoint: false }); },
    f => { f.observation.modules[0].files.pop(); }, f => { f.observation.modules[0].ownership = null; }]) {
    const f = planningFixture(); ownPlanningModule(f, 0);
    f.requests = [{ operation: 'uninstall', moduleId: 'first' }]; edit(f);
    const plan = planModuleOperations(f); assert.equal(plan.modelReady, false, String(edit));
    assert.equal(plan.executionAllowed, false); assert.deepEqual(plan.steps, []);
  }
});
test('uninstall planning is quarantine-only and coexistence never changes subscriptions', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); ownPlanningModule(f, 0); f.observation.modules[0].stableInstalled = true;
  f.requests = [{ operation: 'uninstall', moduleId: 'first' }];
  const plan = planModuleOperations(f);
  assert.equal(plan.modelReady, true); assert.equal(plan.steps[0].mode, 'quarantine-owned-directory');
  assert.ok(plan.warnings.includes('COEXISTENCE:first')); assert.equal(plan.executionAllowed, false);
});
test('first development install warns about an existing stable copy without blocking the model', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); f.observation.modules[0].stableInstalled = true;
  const plan = planModuleOperations(f);
  assert.equal(plan.modelReady, true);
  assert.ok(plan.warnings.includes('COEXISTENCE:first'));
  assert.equal(plan.warnings.filter(w => w === 'COEXISTENCE:first').length, 1);
  assert.equal(plan.executionAllowed, false);
  assert.equal(f.observation.modules[0].directoryPresent, false);
});
test('operation planner detects explicit conflicts and protects required modules', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); f.rules.conflicts.push(['first', 'second']);
  assert.equal(planModuleOperations(f).modelReady, false);
  const removal = planningFixture(); ownPlanningModule(removal, 0); ownPlanningModule(removal, 1);
  removal.rules.requires.push({ moduleId: 'second', dependencyId: 'first' });
  removal.requests = [{ operation: 'uninstall', moduleId: 'first' }];
  assert.ok(planModuleOperations(removal).blockers.some(b => b.code === 'DEPENDENCY_MISSING'));
});
test('operation planner orders dependencies and rejects cycles without partial steps', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); f.rules.requires.push({ moduleId: 'second', dependencyId: 'first' });
  f.requests.reverse();
  assert.deepEqual(planModuleOperations(f).steps.map(s => s.moduleId), ['first', 'second']);
  f.rules.requires.push({ moduleId: 'first', dependencyId: 'second' });
  assert.equal(planModuleOperations(f).steps.length, 0);
  const removal = planningFixture(); ownPlanningModule(removal, 0); ownPlanningModule(removal, 1);
  removal.rules.requires.push({ moduleId: 'second', dependencyId: 'first' });
  removal.requests = [{ operation: 'uninstall', moduleId: 'first' }, { operation: 'uninstall', moduleId: 'second' }];
  assert.deepEqual(planModuleOperations(removal).steps.map(s => s.moduleId), ['second', 'first']);
});
test('prepared batch feeds planning but source changes still invalidate installation plans', async () => {
  const { prepareModuleBatch } = await import('./batch.mjs');
  const { planModuleOperations } = await import('./planning.mjs');
  const f = planningFixture(); const { payload } = batchFixture();
  const prepared = await prepareModuleBatch(f.requests.map(r => r.source), { transport: async request =>
    ({ url: request.url, redirected: false, status: 200, headers: {}, body: (async function* () { yield payload; })() }) });
  for (const request of f.requests) request.package = prepared.packages.find(pkg => pkg.moduleId === request.moduleId);
  assert.equal(planModuleOperations(f).modelReady, true);
  f.requests[1].source.policy.catalogSha256 = '0'.repeat(64);
  assert.equal(planModuleOperations(f).modelReady, false);
  assert.equal(planModuleOperations(f).executionAllowed, false);
});
test('planner rejects unsafe paths, missing rule observations and inconsistent snapshots', async () => {
  const { planModuleOperations } = await import('./planning.mjs');
  for (const edit of [f => { f.observation.modules[0].files.push({ path: '../outside', kind: 'directory', reparsePoint: false }); },
    f => { f.rules.conflicts.push(['first', 'unknown']); },
    f => { f.observation.modules.push(f.observation.modules[0]); },
    f => { f.observation.rootSafe = false; },
    f => { f.requests[0].operation = 'delete'; },
    f => { f.observation.modules[0].files.push({ path: 'extra', kind: 'directory', reparsePoint: false }); }]) {
    const f = planningFixture(); edit(f); const plan = planModuleOperations(f);
    assert.equal(plan.modelReady, false, String(edit)); assert.equal(plan.steps.length, 0);
  }
});
test('pinned per-module catalog compares opaque builds without inferring ordering', () => {
  const { data, policy } = catalogInput();
  assert.equal(catalogStatus(null, policy, data).status, 'not-installed');
  assert.equal(catalogStatus({ moduleId: 'example', buildId: 'opaque-b' }, policy, data).status, 'current');
  assert.equal(catalogStatus({ moduleId: 'example', buildId: 'opaque-z' }, policy, data).status, 'different-build');
  assert.equal(catalogStatus({ moduleId: 'other', buildId: 'opaque-b' }, policy, data).status, 'unavailable');
  const withFlag = catalogInput(c => { c.release.prerelease = false; });
  assert.equal(catalogStatus(null, withFlag.policy, withFlag.data).status, 'not-installed');
});
test('catalog trust anchor is external and exact bytes must match', () => {
  const { data, policy } = catalogInput();
  assert.equal(catalogStatus(null, { ...policy, catalogSha256: null }, data).status, 'unavailable');
  assert.equal(catalogStatus(null, policy, Buffer.concat([data, Buffer.from(' ')])).status, 'unavailable');
  assert.equal(catalogStatus(null, policy, Buffer.alloc(1024 * 1024 + 1)).status, 'unavailable');
});
test('catalog refuses repository and release dot segments before URL normalization', () => {
  for (const repository of ['./Repo', '../Repo', 'Owner/.', 'Owner/..']) {
    const input = catalogInput(c => {
      c.repository = repository;
      for (const f of c.files) f.url = `https://github.com/${repository}/releases/download/${encodeURIComponent(c.release.tag)}/${encodeURIComponent(f.assetName)}`;
    });
    input.policy.repository = repository;
    assert.equal(catalogStatus(null, input.policy, input.data).status, 'unavailable', repository);
  }
  for (const tag of ['.', '..']) {
    const input = catalogInput(c => {
      c.release.tag = tag;
      for (const f of c.files) f.url = `https://github.com/${c.repository}/releases/download/${encodeURIComponent(tag)}/${encodeURIComponent(f.assetName)}`;
    });
    assert.equal(catalogStatus(null, input.policy, input.data).status, 'unavailable', tag);
  }
});
test('catalog retains canonical dotted repository names and encoded tag segments', () => {
  for (const tag of ['v1.2.3', 'feature/v1.2', '%2e%2e']) {
    const input = catalogInput(c => {
      c.repository = 'Owner.Name/Repo.Name'; c.release.tag = tag;
      for (const f of c.files) f.url = `https://github.com/${c.repository}/releases/download/${encodeURIComponent(tag)}/${encodeURIComponent(f.assetName)}`;
    });
    input.policy.repository = 'Owner.Name/Repo.Name';
    assert.equal(catalogStatus(null, input.policy, input.data).status, 'not-installed', tag);
    const parsed = new URL(JSON.parse(input.data).files[0].url);
    assert.equal(parsed.pathname.split('/').length, 7);
  }
});
test('module selection remains independent and invalid sources expose no package', () => {
  const first = catalogInput();
  const second = catalogInput(c => { c.moduleId = 'second'; c.branch = 'Second'; c.buildId = 'second-only'; });
  second.policy.moduleId = 'second'; second.policy.branch = 'Second';
  assert.equal(catalogStatus(null, second.policy, second.data).selectedBuildId, 'second-only');
  const wrong = catalogStatus(null, first.policy, second.data);
  assert.equal(wrong.status, 'unavailable'); assert.equal(wrong.catalog, undefined);
  assert.equal(catalogStatus(null, first.policy, first.data).selectedBuildId, 'opaque-b');
  const bad = Buffer.from('{');
  assert.equal(catalogStatus(null, { ...first.policy,
    catalogSha256: createHash('sha256').update(bad).digest('hex') }, bad).reason, 'invalid-catalog');
  assert.equal(catalogStatus(null, first.policy, first.data).catalog.files.length, 2);
});
test('catalog rejects incomplete, unpublished, mismatched and unsafe sources', () => {
  const edits = [c => { c.schema = 2; }, c => { c.moduleId = 'other'; },
    c => { c.repository = 'Other/Repo'; }, c => { c.branch = 'Other'; },
    c => { c.assembly = 'Other'; }, c => { c.workflow = 'other.yml'; },
    c => { c.channel = 'stable'; }, c => { c.devTool = false; },
    c => { c.release.draft = true; }, c => { c.release.publishedAt = null; },
    c => { c.sourceSha = 'latest'; }, c => { c.buildId = ''; },
    c => { c.files.pop(); }, c => { c.files.push({ ...c.files[0], path: 'extra.dll' }); },
    c => { c.files[1].path = 'EXAMPLE.dll'; }, c => { c.files[0].sha256 = ''; },
    c => { c.files[0].size = 0; }, c => { c.files[0].url += '?token=secret'; },
    c => { c.files[0].url = 'https://evil.example/Example.dll'; },
    c => { c.files[0].url = c.files[0].url.replace('example-dev-b', 'other'); },
    c => { c.files[0].assetId = -1; }, c => { c.release.id = 0; },
    c => { c.release.draft = undefined; }, c => { c.release.publishedAt = '2026-02-30T00:00:00Z'; },
    c => { c.files[0].size = 32 * 1024 * 1024 + 1; },
    c => { c.files[1].assetId = c.files[0].assetId; },
    c => { c.files[0].path = '../Example.dll'; }];
  for (const edit of edits) {
    const { data, policy } = catalogInput(edit);
    assert.equal(catalogStatus(null, policy, data).status, 'unavailable', String(edit));
  }
});
test('rejects traversal, Windows devices, alternate streams and ambiguous names', () => {
  for (const p of ['../x', '/x', 'C:/x', 'a\\b', 'x:stream', 'NUL.dll', 'a/../x', 'x.', 'x ', 'a//b'])
    assert.throws(() => safeRelative(p));
});
test('verifies all files and detects tampering', () => {
  assert.equal(verifyPackage(pkg()).length, 1);
  const p = pkg(); p.files[0].bytes = Buffer.from('changed');
  assert.throws(() => verifyPackage(p));
});
test('rejects case-insensitive duplicate destinations', () => {
  const p = pkg(); p.files.push({ ...p.files[0], path: 'MOD.dll' });
  assert.throws(() => verifyPackage(p));
});
test('does not overwrite a pre-existing user file', () => {
  assert.throws(() => installationPlan('D:\\Mods', pkg(), ['example/MOD.dll']));
  assert.equal(installationPlan('D:\\Mods', pkg())[0].destination, 'D:\\Mods\\example\\mod.dll');
});
test('uninstall retains modified files and rejects reparse points', () => {
  const record = { moduleId: 'example', files: [{ relative: 'example/mod.dll', sha256 }] };
  assert.deepEqual(uninstallPlan(record, { 'example/mod.dll': { sha256 } }), ['example/mod.dll']);
  assert.throws(() => uninstallPlan(record, { 'example/mod.dll': { sha256: 'changed' } }));
  assert.throws(() => uninstallPlan(record, { 'example/mod.dll': { sha256, reparsePoint: true } }));
  assert.throws(() => uninstallPlan({ ...record, moduleId: 'other' }, {}));
});
test('versions are compared per module without assuming numeric ordering', () => {
  assert.equal(updateStatus(pkg(), pkg()), 'current');
  assert.equal(updateStatus(pkg(), { moduleId: 'example', buildId: 'build-b' }), 'different-build');
  assert.throws(() => updateStatus(pkg(), { moduleId: 'other', buildId: 'a' }));
});
test('coexistence produces only a warning', () => {
  assert.equal(typeof coexistenceWarning(true, true), 'string');
  assert.equal(coexistenceWarning(true, false), null);
});
test('module identifiers cannot be Windows devices or ambiguous names', () => {
  for (const moduleId of ['CON', 'nul', 'com1', 'LPT9', 'a.', 'a ', 'a/b'])
    assert.throws(() => verifyPackage({ ...pkg(), moduleId }), moduleId);
});
test('rejects file versus directory overlap in either order', () => {
  for (const paths of [['a', 'a/b.dll'], ['A/b.dll', 'a']]) {
    const p = pkg(); p.files = paths.map(p => ({ path: p, bytes, sha256 }));
    assert.throws(() => verifyPackage(p));
  }
});
test('rejects invalid Windows characters and reserved device variants', () => {
  for (const name of ['a?.dll', 'a*.dll', 'a|b', 'a<b', 'COM¹.dll', 'CON .txt'])
    assert.throws(() => safeRelative(name), name);
});
test('ownership plans reject nested module identities and malformed hashes', () => {
  assert.throws(() => uninstallPlan({ moduleId: 'example/sub', files: [
    { relative: 'example/sub/mod.dll', sha256 }] }, { 'example/sub/mod.dll': { sha256 } }));
  assert.throws(() => uninstallPlan({ moduleId: 'example', files: [
    { relative: 'example/mod.dll', sha256: 'not-a-hash' }] },
  { 'example/mod.dll': { sha256: 'not-a-hash' } }));
});

// Signature: OpenAI-Codex
test('an installed build matching an observed candidate is distinguished from an unavailable source', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const fixture = nativeSourceFixture();
  const installed = { moduleId: 'example', buildId: fixture.tag };
  const service = new DesktopService({
    registry: [{ ...fixture.policy, name: 'Synthetic module' }],
    request: fixture.request,
    installer: { audit: () => ({ installed }) },
    now: () => fixture.nowMs
  });
  service.provider='module-release';
  await service.refresh();
  assert.equal(service.rows()[0].matchesCandidate, true);
  assert.equal(service.rows()[0].canRequestUpdate, false);
  service.now = () => fixture.nowMs + 3600001;
  assert.equal(service.rows()[0].matchesCandidate, false);
});
