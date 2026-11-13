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

test('native and portable build contracts require and embed the shared app icon',()=>{
  const native=readFileSync('desktop/Build-Native.ps1','utf8');const portable=readFileSync('desktop/Build-Portable.ps1','utf8');const app=readFileSync('desktop/DevManager.cs','utf8');
  assert.match(native,/DevManager\.ico/);assert.match(native,/\/win32icon:/);assert.match(portable,/DevManager\.ico/);assert.match(portable,/\/win32icon:/);
  assert.match(app,/ExtractAssociatedIcon\(Application\.ExecutablePath\)/);
});

test('native build identity changes for different icon input without generating images or compiling',{skip:process.platform!=='win32'},async()=>{
  const fs=await import('node:fs');const os=await import('node:os');const path=await import('node:path');const {execFileSync}=await import('node:child_process');
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'cs1-dev-manager-test-'));const source=path.join(root,'source.txt');const source2=path.join(root,'source-other.txt');const a=path.join(root,'input-a.bin');const b=path.join(root,'input-b.bin');
  fs.writeFileSync(source,'synthetic source identity');fs.writeFileSync(source2,'changed synthetic source identity');fs.writeFileSync(a,'not an image: first identity');fs.writeFileSync(b,'not an image: second identity');
  const identity=(sourcePath,iconPath)=>execFileSync('pwsh',['-NoLogo','-NoProfile','-NonInteractive','-File',path.resolve('desktop/Build-Native.ps1'),'-IdentityOnly','-IdentitySourcePath',sourcePath,'-IdentityIconPath',iconPath,'-OutputDirectory',root],{encoding:'utf8',windowsHide:true,timeout:15000,stdio:['pipe','pipe','pipe']}).trim();
  const first=identity(source,a),again=identity(source,a),changedIcon=identity(source,b),changedSource=identity(source2,a);
  assert.equal(first,again);assert.notEqual(first,changedIcon);assert.notEqual(first,changedSource);assert.match(path.basename(first),/^DevManager-dev-[a-f0-9]{64}\.exe$/);
  const sameDirectory=(left,right)=>fs.realpathSync.native(left).toLowerCase()===fs.realpathSync.native(right).toLowerCase();
  assert.equal(sameDirectory(path.dirname(first),root),true);
  assert.equal(sameDirectory(root,`${root}${path.sep}..${path.sep}${path.basename(root)}`),true);
  assert.equal(sameDirectory(root,path.dirname(root)),false);
  assert.equal(fs.existsSync(first),false);assert.equal(fs.existsSync(changedIcon),false);
  let failure;try{identity(source,path.join(root,'missing-input.bin'));}catch(error){failure=error;}assert.ok(failure);assert.match(String(failure.stderr),/Required app icon is missing/);
  const attributes=execFileSync('git',['check-attr','text','--','desktop/DevManager.ico'],{encoding:'utf8',windowsHide:true});assert.match(attributes,/: unset\s*$/);
});

test('native identity refuses an input held for exclusive writing',{skip:process.platform!=='win32'},async()=>{
  const fs=await import('node:fs');const os=await import('node:os');const path=await import('node:path');const {spawn,execFileSync}=await import('node:child_process');
  const root=fs.mkdtempSync(path.join(os.tmpdir(),'cs1-dev-manager-test-'));const source=path.join(root,'source.txt');const sample=path.join(root,'not-an-image.bin');const helper=path.join(root,'hold-input.ps1');
  fs.writeFileSync(source,'synthetic source');fs.writeFileSync(sample,'not an image');fs.writeFileSync(helper,"# Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager\nparam([string]$FilePath)\n$stream=[IO.File]::Open($FilePath,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)\ntry { [Console]::WriteLine('LOCKED'); [Console]::Out.Flush(); $null=[Console]::In.ReadLine() } finally { $stream.Dispose() }\n");
  const child=spawn('pwsh',['-NoLogo','-NoProfile','-NonInteractive','-File',helper,'-FilePath',sample],{windowsHide:true,stdio:['pipe','pipe','pipe']});
  const completed=new Promise((resolve,reject)=>{child.once('error',reject);child.once('close',code=>code===0?resolve():reject(new Error(`Lock helper exited ${code}`)));});
  const ready=new Promise((resolve,reject)=>{const timer=setTimeout(()=>reject(new Error('Lock helper did not start')),8000);child.stdout.on('data',data=>{if(data.toString().includes('LOCKED')){clearTimeout(timer);resolve();}});child.once('error',error=>{clearTimeout(timer);reject(error);});});
  try{
    await ready;let failure;try{execFileSync('pwsh',['-NoLogo','-NoProfile','-NonInteractive','-File',path.resolve('desktop/Build-Native.ps1'),'-IdentityOnly','-IdentitySourcePath',source,'-IdentityIconPath',sample,'-OutputDirectory',root],{encoding:'utf8',windowsHide:true,timeout:15000,stdio:['pipe','pipe','pipe']});}catch(error){failure=error;}
    assert.ok(failure);assert.equal(String(failure.stdout).trim(),'');
  }finally{child.stdin.end('\n');await completed;}
});

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
  manager.install(pkg(), null);
  assert.ok(fs.existsSync(path.join(modsRoot, 'CS1DevManager-example', 'mod.dll')));
  assert.equal(path.relative(modsRoot, path.join(manager.root, 'CS1DevManager-example', 'mod.dll')).split(path.sep).length, 2);
  manager.update({ ...pkg(), buildId: 'updated' }, manager.installed('example')); manager.uninstall('example', manager.installed('example'));
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
  const installer = new IsolatedInstaller(fs.mkdtempSync(path.join(os.tmpdir(), 'cs1-dev-manager-test-'))); installer.install(pkg(), null);
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

test('an unmapped module uses an artifact manifest but still requires a successful run and inspected files', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const f = artifactFixture();
  const entry = { moduleId: f.entry.moduleId, name: f.entry.name, repository: f.entry.repository,
    branch: f.entry.branch, publisherConfigured: false };
  let installed = null; let downloads = 0; let inspected = 0; let identityAllowed = false;
  const installer = { audit: () => ({ installed }), install: (pkg, expected, checkFreshness) => {
    assert.equal(expected, null); checkFreshness(); installed = { moduleId: pkg.moduleId, buildId: pkg.buildId }; return {}; } };
  const service = new DesktopService({ registry: [entry], request: f.request, installer, now: () => f.nowMs,
    assertGameClosed: async () => true, assetTransport: async options => { downloads++; assert.ok(options.url.includes(`/${f.artifact}/`)); return f.transport(options); },
    inspectPackage: async (pkg, policy) => { inspected++; assert.equal(policy.policyOrigin, 'artifact-manifest');
      assert.equal(policy.assembly, 'Example'); assert.equal(policy.workflowPath, f.manifest.workflowPath);
      assert.deepEqual(pkg.files.map(file => file.path).sort(), ['Example.dll', 'LICENSE']); return identityAllowed; } });
  const discovered = (await service.refresh()).rows[0];
  assert.equal(discovered.configurationPending, false); assert.equal(discovered.canRequestInstall, true);
  assert.equal(discovered.development, 'Unconfirmed');
  const runUrl = `${f.base}/actions/runs/${f.manifest.runId}/attempts/${f.manifest.attempt}`;
  const run = structuredClone(f.routes.get(runUrl)); f.routes.set(runUrl, { ...run, conclusion: 'failure' });
  await assert.rejects(service.execute('install', [entry.moduleId]), /SOURCE_UNAVAILABLE/);
  assert.equal(downloads, 0); assert.equal(installed, null);
  f.routes.set(runUrl, run);
  await assert.rejects(service.execute('install', [entry.moduleId]), /ASSEMBLY_IDENTITY_UNVERIFIED/);
  assert.equal(downloads, 2); assert.equal(inspected, 1); assert.equal(installed, null);
  identityAllowed = true;
  const result = await service.execute('install', [entry.moduleId]);
  assert.equal(result.results[0].status, 'committed'); assert.equal(installed.buildId, f.manifest.buildId);
  assert.equal(downloads, 4); assert.equal(inspected, 2);
});

test('partially mapped artifact entries refuse contradictory identities or missing required files', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  for (const mismatch of ['assembly', 'workflow', 'required-file']) {
    const f = artifactFixture(); const entry = { ...f.entry, publisherConfigured: false };
    if (mismatch === 'assembly') f.manifest.assembly = 'Different';
    if (mismatch === 'workflow') f.manifest.workflowPath = '.github/workflows/different.yml';
    if (mismatch === 'required-file') f.manifest.files = f.manifest.files.filter(file => file.name !== 'LICENSE');
    f.configure();
    const service = new DesktopService({ registry: [entry], request: f.request, now: () => f.nowMs });
    const row = (await service.refresh()).rows[0];
    assert.equal(row.status, 'unknown', mismatch); assert.equal(row.configurationPending, true, mismatch);
    assert.equal(row.canRequestInstall, false, mismatch);
  }
});

test('artifact discovery distinguishes matching installed builds and keeps known publisher mapping during a queued check',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const {detectArtifactModule}=await import('./artifact-source.mjs');const f=artifactFixture();
  const entry={...f.entry,publisherConfigured:false};const candidate=(await detectArtifactModule({entry,request:f.request,nowMs:f.nowMs})).latestPublished;
  const installed={moduleId:entry.moduleId,buildId:candidate.buildId,publication:{provider:'artifact-branch',artifactBranch:candidate.artifactBranch,
    artifactCommit:candidate.artifactCommit,manifestSha256:candidate.manifestSha256,sourceSha:candidate.sourceSha,runId:candidate.runId,
    attempt:candidate.attempt,publishedAt:candidate.publishedAt,buildStartedAt:candidate.buildStartedAt}};
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

function fifteenDistinctAncestryFixture() {
  const f = fifteenArtifactFixture();
  for (const [index, entry] of f.registry.entries()) {
    const priorSource = (index + 200).toString(16).padStart(40, '0');
    f.installed.get(entry.moduleId).publication.sourceSha = priorSource;
    f.routes.set(`${f.base}/compare/${priorSource}...${f.sha}`,
      { status: 'ahead', base_commit: { sha: priorSource }, ahead_by: 1, behind_by: 0 });
  }
  return f;
}

// Signature: OpenAI-Codex_dev_manager_development
test('batch mutations recheck game status and report unattempted modules in four languages',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');
  const {localizeReply}=await import('./localization.mjs');
  const reason='The game is running or its status could not be verified. Close the game before changing module files.';
  const translations={
    en:['Not attempted',reason],
    'zh-Hant':['未執行','遊戲正在執行，或無法確認遊戲狀態。變更模組檔案前請關閉遊戲。'],
    'zh-Hans':['未执行','游戏正在运行，或无法确认游戏状态。更改模组文件前请关闭游戏。'],
    ja:['未実行','ゲームが起動中か、状態を確認できません。モジュールファイルを変更する前にゲームを終了してください。']
  };
  for(const operation of ['install','update','uninstall'])for(const probe of ['running','unknown']){
    const f=fifteenArtifactFixture();const registry=f.registry.slice(0,3);const ids=registry.map(entry=>entry.moduleId);
    const installed=new Map(operation==='install'?[]:ids.map(id=>[id,f.installed.get(id)]));const mutations=[];const progress=[];
    const installer={audit:id=>({installed:installed.get(id)??null})};
    installer.install=pkg=>{mutations.push(pkg.moduleId);installed.set(pkg.moduleId,{moduleId:pkg.moduleId,buildId:pkg.buildId});return {};};
    installer.update=installer.install;
    installer.uninstall=id=>{mutations.push(id);installed.delete(id);return {};};
    const service=new DesktopService({registry,request:f.request,assetTransport:f.transport,installer,now:()=>f.nowMs,
      inspectPackage:async()=>true,progress:text=>progress.push(text),assertGameClosed:async()=>{if(!mutations.length)return true;if(probe==='unknown')throw new Error('probe unavailable');return false;}});
    const result=await service.execute(operation,ids);
    assert.equal(result.halted,true,`${operation}/${probe}`);
    assert.deepEqual(mutations,[ids[0]],`${operation}/${probe}`);
    assert.deepEqual(progress.filter(text=>text.startsWith(`${operation}: `)),[`${operation}: ${registry[0].name}`]);
    assert.deepEqual(result.results.map(item=>[item.moduleId,item.status]),[[ids[0],'committed'],[ids[1],'not-attempted'],[ids[2],'not-attempted']]);
    assert.equal(result.results[1].reason,reason);
    assert.equal(result.results[2].reason,reason);
    for(const [language,[statusText,reasonText]] of Object.entries(translations)){
      const reply=localizeReply({ok:true,result},{preferences:{language,theme:'system'}});
      assert.equal(reply.result.results[1].statusText,statusText,language);
      assert.equal(reply.result.results[1].reasonText,reasonText,language);
      assert.equal(reply.result.results[1].reasonDiagnostic,'',language);
    }
  }
});

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
  const pool=new GitHubRequestPool(async options=>{calls++;return {url:options.url,status:200,redirected:false,headers:{},body:{}};},{hourlyLimit:3,now:()=>clock,monotonic:()=>clock});
  for(let i=0;i<5;i++){pool.beginRefresh();const result=await pool.request({url:`https://api.github.com/repos/Owner/Repo/test/${i}`});assert.equal(result.status,i<3?200:429);}
  assert.equal(calls,3);assert.equal(pool.status().localRemaining,0);assert.ok(pool.status().retryAtMs>clock);
  clock=3600002;pool.beginRefresh();assert.equal((await pool.request({url:'https://api.github.com/repos/Owner/Repo/test/resume'})).status,200);assert.equal(calls,4);
});

test('fifteen-module automatic queue remains fair across actual shared-pool hourly budget windows',async()=>{
  const {DesktopService}=await import('./desktop-service.mjs');const {GitHubRequestPool}=await import('./https-source.mjs');const f=fifteenArtifactFixture();let clock=f.nowMs;
  const pool=new GitHubRequestPool(f.request,{hourlyLimit:4,now:()=>clock,monotonic:()=>clock});
  const service=new DesktopService({registry:f.registry,request:pool.request.bind(pool),beginRefresh:()=>pool.beginRefresh(),rateStatus:()=>pool.status(),now:()=>clock,automaticRequestLimit:4});const seen=new Set();
  for(let round=0;round<9;round++){
    f.calls.length=0;await service.refresh();assert.ok(f.calls.length<=4);service.rows().filter(row=>row.canRequestInstall).forEach(row=>seen.add(row.moduleId));clock+=3600002;
  }
  assert.equal(seen.size,15);
});

test('an artifact without its source branch is unavailable until that branch returns', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { localizeReason } = await import('./localization.mjs'); const f = artifactFixture();
  const refUrl = `${f.base}/git/matching-refs/heads/`;
  const original = structuredClone(f.routes.get(refUrl));
  f.routes.set(refUrl, original.filter(ref => ref.ref !== `refs/heads/${f.entry.branch}`));
  let downloads = 0; let mutations = 0;
  const installer = { audit: () => ({ installed: null }), install: () => { mutations++; } };
  const service = new DesktopService({ registry: [{ ...f.entry, publisherConfigured: false }], request: f.request, installer, now: () => f.nowMs,
    assertGameClosed: async () => true, assetTransport: async () => { downloads++; throw new Error('unexpected download'); } });
  const row = (await service.refresh()).rows[0];
  assert.equal(row.status, 'unknown'); assert.equal(row.latestBuild, ''); assert.equal(row.canRequestInstall, false);
  assert.equal(row.reason, 'The artifact branch has no corresponding source branch. Check the repository before installing.');
  assert.equal(row.configurationPending, true);
  for (const language of ['zh-Hant', 'zh-Hans', 'ja']) assert.notEqual(localizeReason(language, row.reason).text, row.reason, language);
  await assert.rejects(service.execute('install', [f.entry.moduleId]), /SOURCE_UNAVAILABLE/);
  assert.equal(downloads, 0); assert.equal(mutations, 0);
  f.routes.set(refUrl, original);
  const recovered = (await service.refresh()).rows[0];
  assert.equal(recovered.canRequestInstall, true); assert.equal(recovered.development, 'Unconfirmed'); assert.equal(recovered.configurationPending, false);
  f.routes.set(refUrl, original.filter(ref => ref.ref !== `refs/heads/${f.entry.branch}`));
  const missingAgain = (await service.refresh()).rows[0];
  assert.equal(missingAgain.configurationPending, false); assert.equal(missingAgain.status, 'unknown');
  assert.equal(missingAgain.latestBuild, ''); assert.equal(missingAgain.canRequestInstall, false);
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
  installer.install(pkg(),null);assert.equal(service.rows()[0].canUninstall,true);assert.equal((await service.execute('uninstall',['example'])).results[0].status,'committed');
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

// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
test('game-reference identities stay blocked when installation filenames are renamed', async () => {
  const { allowedInstallationAsset } = await import('./asset-policy.mjs');
  for (const identity of ['mscorlib', 'System', 'System.Core', 'UnityEngine', 'ICities', 'ColossalManaged']) {
    assert.equal(allowedInstallationAsset(`${identity}.dll`, identity), false, identity);
    assert.equal(allowedInstallationAsset('renamed-runtime.dll', identity), false, identity);
    assert.equal(allowedInstallationAsset('renamed-runtime.dll', identity.toUpperCase()), false, identity);
  }
  assert.equal(allowedInstallationAsset('CitiesHarmony.API.dll', 'CitiesHarmony.API'), true);
  assert.equal(allowedInstallationAsset('renamed-dependency.dll', 'CitiesHarmony.API'), true);
  assert.equal(allowedInstallationAsset('LICENSE', null), true);
});

test('artifact source rejects renamed game-reference identities but accepts valid dependencies', async () => {
  const { detectArtifactModule } = await import('./artifact-source.mjs');
  for (const [identity, expected] of [['UnityEngine', 'unknown'], ['ICITIES', 'unknown'], ['CitiesHarmony.API', 'candidate / verification required']]) {
    const fixture = artifactFixture();
    fixture.manifest.files.push({ path: 'files/renamed.dll', name: 'renamed.dll', assemblyName: identity,
      size: fixture.payload.length, sha256: createHash('sha256').update(fixture.payload).digest('hex') });
    fixture.configure();
    const source = await detectArtifactModule({ entry: fixture.entry, request: fixture.request,
      nowMs: fixture.nowMs, discoveryOnly: true });
    assert.equal(source.status, expected, identity);
  }
});

test('source observations expire on clock rollback and invalid timestamps', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example' }];
  const observedAt = 1800000000000;
  let clock = observedAt; let installed = null;
  const service = new DesktopService({ registry, now: () => clock, installer: { audit: () => ({ installed }) } });
  const source = { status: 'candidate', checkedAtMs: observedAt, developmentConfirmed: true,
    latestPublished: { status: 'candidate', buildId: 'Example-dev-1-1' } };
  service.sources.set('example', source);
  assert.equal(service.rows()[0].canInstall, true);
  clock = observedAt + 3599999;
  assert.equal(service.rows()[0].canRequestInstall, true);
  clock = observedAt + 3600000;
  assert.equal(service.rows()[0].status, 'stale / unknown');
  assert.equal(service.rows()[0].canInstall, false);
  installed = { moduleId: 'example', buildId: source.latestPublished.buildId };
  clock = observedAt;
  assert.equal(service.rows()[0].matchesCandidate, true);
  clock = observedAt - 1;
  assert.equal(service.rows()[0].status, 'stale / unknown');
  assert.equal(service.rows()[0].matchesCandidate, false);
  installed = null; clock = observedAt;
  for (const invalid of [undefined, null, NaN, Infinity, '1800000000000']) {
    source.checkedAtMs = invalid;
    const row = service.rows()[0];
    assert.equal(row.status, 'stale / unknown', String(invalid));
    assert.equal(row.canInstall, false, String(invalid));
    assert.equal(row.canRequestInstall, false, String(invalid));
  }
  source.checkedAtMs = observedAt; clock = NaN;
  assert.equal(service.rows()[0].canInstall, false);
  source.latestPublished = { status: 'unknown' };
  source.discoveryCandidate = { buildId: 'Example-dev-2-1' };
  source.developmentConfirmed = false; clock = observedAt; source.checkedAtMs = undefined;
  assert.equal(service.rows()[0].canRequestInstall, false);
  source.checkedAtMs = observedAt;
  assert.equal(service.rows()[0].canRequestInstall, true);
  const unobserved = new DesktopService({ registry, now: () => observedAt });
  assert.equal(unobserved.rows()[0].status, 'unknown');
});

test('batch transaction failures report every unattempted module', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const { localizeReply, translate } = await import('./localization.mjs');
  for (const operation of ['install', 'update', 'uninstall']) for (const committed of [false, true, null]) {
    const fixture = fifteenArtifactFixture(); const registry = fixture.registry.slice(0, 3);
    const ids = registry.map(entry => entry.moduleId); const attempted = [];
    const installed = new Map(operation === 'install' ? [] : ids.map(id => [id, fixture.installed.get(id)]));
    const installer = { audit: id => ({ installed: installed.get(id) ?? null }) };
    const mutate = value => {
      const id = typeof value === 'string' ? value : value.moduleId; attempted.push(id);
      if (attempted.length === 2) {
        if (committed === null) throw null;
        const error = new Error('Synthetic transaction failure'); error.committed = committed; throw error;
      }
      return {};
    };
    installer.install = mutate; installer.update = mutate; installer.uninstall = mutate;
    const service = new DesktopService({ registry, request: fixture.request, assetTransport: fixture.transport,
      installer, now: () => fixture.nowMs, inspectPackage: async () => true, assertGameClosed: async () => true });
    const result = await service.execute(operation, ids);
    assert.equal(result.halted, true, operation);
    assert.deepEqual(attempted, ids.slice(0, 2), operation);
    assert.deepEqual(result.results.map(item => [item.moduleId, item.status]),
      [[ids[0], 'committed'], [ids[1], committed ? 'committed-with-warning' : 'failed'], [ids[2], 'not-attempted']], operation);
    for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
      const reply = localizeReply({ ok: true, result }, { preferences: { language, theme: 'system' } });
      assert.equal(reply.result.results[2].statusText, translate(language, 'Not attempted'), language);
      assert.ok(reply.result.results[2].reasonText, language);
    }
  }
});

test('batch handles a non-Error transaction failure without losing results', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const registry = ['one', 'two', 'three'].map(moduleId => ({ moduleId, name: moduleId }));
  const installer = { audit: moduleId => ({ installed: { moduleId, buildId: 'synthetic' } }),
    uninstall: moduleId => { if (moduleId === 'two') throw null; return {}; } };
  const service = new DesktopService({ registry, installer, assertGameClosed: async () => true });
  const result = await service.execute('uninstall', registry.map(entry => entry.moduleId));
  assert.deepEqual(result.results.map(item => item.status), ['committed', 'failed', 'not-attempted']);
});

// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
test('recorded game and framework references cannot be installed under original or renamed identities', async () => {
  const { allowedInstallationAsset } = await import('./asset-policy.mjs');
  const { detectArtifactModule } = await import('./artifact-source.mjs');
  for (const identity of ['Assembly-CSharp', 'System.Xml']) {
    assert.equal(allowedInstallationAsset(`${identity}.dll`, identity), false, identity);
    assert.equal(allowedInstallationAsset('renamed.dll', identity.toUpperCase()), false, identity);
    const fixture = artifactFixture();
    fixture.manifest.files.push({ path: 'files/renamed.dll', name: 'renamed.dll', assemblyName: identity,
      size: fixture.payload.length, sha256: createHash('sha256').update(fixture.payload).digest('hex') });
    fixture.configure();
    assert.equal((await detectArtifactModule({ entry: fixture.entry, request: fixture.request,
      nowMs: fixture.nowMs, discoveryOnly: true })).status, 'unknown', identity);
  }
  assert.equal(allowedInstallationAsset('renamed-dependency.dll', 'CitiesHarmony.API'), true);
});

test('installation refuses an expired or future observation before download and before mutation', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  for (const phase of ['after-refresh', 'after-download', 'clock-rollback', 'after-game-check']) {
    const fixture = artifactFixture(); let clock = fixture.nowMs; let downloads = 0; let writableInstaller = 0; let checks = 0;
    const mutations = []; const installer = { audit: () => ({ installed: null }), install: pkg => { mutations.push(pkg.moduleId); return {}; } };
    const service = new DesktopService({ registry: [fixture.entry], request: fixture.request,
      assetTransport: async options => { downloads++; return fixture.transport(options); },
      getInstaller: create => { if (create) writableInstaller++; return installer; }, now: () => clock,
      inspectPackage: async () => { if (phase === 'after-download') clock += 3600000; if (phase === 'clock-rollback') clock--; return true; },
      assertGameClosed: async () => { checks++; if (phase === 'after-game-check' && checks === 2) clock += 3600000; return true; } });
    const refresh = service.refresh.bind(service);
    service.refresh = async (...args) => { const result = await refresh(...args); if (phase === 'after-refresh') clock += 3600000; return result; };
    await assert.rejects(service.execute('install', ['example']), /SOURCE_UNAVAILABLE/, phase);
    assert.deepEqual(mutations, [], phase);
    assert.equal(writableInstaller, 0, phase);
    if (phase === 'after-refresh') assert.equal(downloads, 0);
  }
});

test('batch stops remaining installs and updates when the verified observation expires', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  for (const operation of ['install', 'update']) {
    const fixture = fifteenArtifactFixture(); const registry = fixture.registry.slice(0, 3);
    const ids = registry.map(entry => entry.moduleId); let clock = fixture.nowMs; const mutations = [];
    const installed = new Map(operation === 'install' ? [] : ids.map(id => [id, fixture.installed.get(id)]));
    const installer = { audit: id => ({ installed: installed.get(id) ?? null }) };
    installer.install = installer.update = pkg => { mutations.push(pkg.moduleId); clock += 3600000; return {}; };
    const service = new DesktopService({ registry, request: fixture.request, assetTransport: fixture.transport,
      installer, now: () => clock, inspectPackage: async () => true, assertGameClosed: async () => true });
    const result = await service.execute(operation, ids);
    assert.equal(result.halted, true, operation);
    assert.deepEqual(mutations, [ids[0]], operation);
    assert.deepEqual(result.results.map(item => item.status), ['committed', 'not-attempted', 'not-attempted'], operation);
    assert.match(result.results[1].reason, /stale/i);
  }
});

test('locked transactions require and compare complete installation snapshots before module access', async t => {
  const fs = (await import('node:fs')).default; const path = await import('node:path');
  const { IsolatedInstaller } = await import('./filesystem.mjs');
  const manager = Object.create(IsolatedInstaller.prototype);
  Object.assign(manager, { root: 'synthetic-root', meta: path.join('synthetic-root', '.manager'), directoryPrefix: '', fault: () => {} });
  const lock = path.join(manager.meta, 'lock'); const journal = path.join(manager.meta, 'journal.json');
  const events = []; let lockBytes = null; let actual = null; let onLoad = () => {};
  manager.check = () => {}; manager.load = () => { events.push('load'); manager.state = { modules: actual ? { example: structuredClone(actual) } : {} }; onLoad(); };
  const stats = () => ({ dev: 1n, ino: 2n, nlink: 1n, size: BigInt(lockBytes.length), isSymbolicLink: () => false, isFile: () => true, isDirectory: () => false });
  t.mock.method(fs, 'openSync', (name, flags) => { assert.equal(name, lock); if (flags === 'wx') { events.push('lock'); lockBytes = Buffer.alloc(0); return 10; } assert.equal(flags, 'r'); return 11; });
  t.mock.method(fs, 'writeFileSync', (fd, value) => { assert.equal(fd, 10); lockBytes = Buffer.from(value); });
  t.mock.method(fs, 'fsyncSync', () => {}); t.mock.method(fs, 'closeSync', () => {});
  t.mock.method(fs, 'lstatSync', name => { if (name === journal) throw Object.assign(new Error('missing'), { code: 'ENOENT' }); assert.equal(name, lock); assert.ok(lockBytes); return stats(); });
  t.mock.method(fs, 'fstatSync', fd => { assert.equal(fd, 11); return stats(); });
  t.mock.method(fs, 'readSync', (fd, buffer, offset, length, position) => { assert.equal(fd, 11); return lockBytes.copy(buffer, offset, position, Math.min(lockBytes.length, position + length)); });
  t.mock.method(fs, 'unlinkSync', name => { assert.equal(name, lock); events.push('unlock'); lockBytes = null; });
  t.mock.method(fs, 'readdirSync', () => { events.push('directory'); throw new Error('TEST_REACHED_DIRECTORY_CHECK'); });
  t.mock.method(fs, 'mkdirSync', () => { events.push('stage'); throw new Error('TEST_UNEXPECTED_STAGE'); });
  const prior = { moduleId: 'example', buildId: 'build-A', files: [{ path: 'mod.dll', sha256: 'a'.repeat(64) }],
    publication: { sourceSha: 'b'.repeat(40), runId: 1, attempt: 1 } };
  const newer = { ...structuredClone(prior), buildId: 'build-D' };
  const sameVersionChangedPublication = structuredClone(prior); sameVersionChangedPublication.publication.attempt = 2;
  const sameVersionChangedFiles = structuredClone(prior); sameVersionChangedFiles.files[0].sha256 = 'c'.repeat(64);
  for (const [operation, expected, current] of [['update', prior, newer], ['update', prior, null],
    ['update', prior, sameVersionChangedPublication], ['update', prior, sameVersionChangedFiles],
    ['uninstall', prior, newer], ['uninstall', prior, sameVersionChangedFiles], ['install', null, prior]]) {
    actual = current; events.length = 0;
    const input = operation === 'uninstall' ? 'example' : pkg();
    assert.throws(() => manager[operation](input, expected), /INSTALLATION_STATE_CHANGED/, operation);
    assert.ok(events.indexOf('lock') < events.indexOf('load'), operation);
    assert.equal(events.includes('directory'), false, operation);
    assert.equal(events.includes('stage'), false, operation);
    assert.equal(events.at(-1), 'unlock', operation);
  }
  for (const operation of ['install', 'update', 'uninstall']) {
    events.length = 0;
    assert.throws(() => manager[operation](operation === 'uninstall' ? 'example' : pkg()), /INSTALLATION_SNAPSHOT_REQUIRED/, operation);
    assert.deepEqual(events, [], operation);
  }
  for (const invalid of [null, {}, { moduleId: 'other' }, { moduleId: 'example', unsupported: () => {} }]) {
    events.length = 0;
    assert.throws(() => manager.update(pkg(), invalid), /INSTALLATION_SNAPSHOT_REQUIRED/);
    assert.deepEqual(events, []);
  }
  for (const [operation, current, expected] of [['install', null, null], ['update', prior, structuredClone(prior)], ['uninstall', prior, structuredClone(prior)]]) {
    actual = current; events.length = 0;
    assert.throws(() => manager[operation](operation === 'uninstall' ? 'example' : pkg(), expected), /TEST_REACHED_DIRECTORY_CHECK/, operation);
    assert.ok(events.includes('directory'), operation);
    assert.equal(events.includes('stage'), false, operation);
  }
  actual = prior; events.length = 0;
  assert.throws(() => manager.update(pkg(), prior, () => { events.push('freshness'); throw new Error('SOURCE_UNAVAILABLE'); }), /SOURCE_UNAVAILABLE/);
  assert.ok(events.indexOf('lock') < events.indexOf('load'));
  assert.ok(events.indexOf('load') < events.indexOf('freshness'));
  assert.equal(events.includes('directory'), false);
  assert.equal(events.includes('stage'), false);
  const mutable = structuredClone(prior); actual = prior; events.length = 0;
  onLoad = () => { mutable.buildId = 'changed-after-call'; };
  assert.throws(() => manager.update(pkg(), mutable), /TEST_REACHED_DIRECTORY_CHECK/);
  assert.ok(events.includes('directory'));
});

test('batch preserves a newer concurrent installation and reports later modules unattempted', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { isDeepStrictEqual } = await import('node:util');
  for (const operation of ['install', 'update', 'uninstall']) {
    const fixture = fifteenArtifactFixture(); const registry = fixture.registry.slice(0, 3); const ids = registry.map(entry => entry.moduleId);
    const installed = new Map(operation === 'install' ? [] : ids.map(id => [id, { ...structuredClone(fixture.installed.get(id)), files: [{ path: 'Example.dll', sha256: 'a'.repeat(64) }] }]));
    const newer = { moduleId: ids[1], buildId: 'newer-build-D', files: [{ path: 'Example.dll', sha256: 'd'.repeat(64) }] };
    const mutations = []; const installer = { audit: id => ({ installed: structuredClone(installed.get(id) ?? null) }) };
    const mutate = (value, expected) => {
      const id = typeof value === 'string' ? value : value.moduleId; const current = installed.get(id) ?? null;
      if (expected !== undefined && !isDeepStrictEqual(current, expected)) throw new Error('INSTALLATION_STATE_CHANGED');
      mutations.push(id);
      if (operation === 'uninstall') installed.delete(id); else installed.set(id, { moduleId: id, buildId: value.buildId, files: [] });
      if (id === ids[0]) installed.set(ids[1], structuredClone(newer));
      return {};
    };
    installer.install = mutate; installer.update = mutate; installer.uninstall = mutate;
    const service = new DesktopService({ registry, request: fixture.request, assetTransport: fixture.transport,
      installer, now: () => fixture.nowMs, inspectPackage: async () => true, assertGameClosed: async () => true });
    const result = await service.execute(operation, ids);
    assert.equal(result.halted, true, operation);
    assert.deepEqual(mutations, [ids[0]], operation);
    assert.deepEqual(result.results.map(item => item.status), ['committed', 'failed', 'not-attempted'], operation);
    assert.deepEqual(installed.get(ids[1]), newer, operation);
  }
});

test('update ordering is bound to the original snapshot rather than an intermediate record', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const { assessUpdateOrder } = await import('./update-order.mjs');
  const { isDeepStrictEqual } = await import('node:util');
  const sourceSha = 'a'.repeat(40); const payload = Buffer.from('synthetic module');
  const file = { path: 'Example.dll', bytes: payload, sha256: createHash('sha256').update(payload).digest('hex') };
  const publication = (day, runId) => ({ provider: 'modpack-bundle', sourceSha, releaseId: 10 + runId, runId, attempt: 1,
    publishedAt: `2026-11-0${day}T00:00:00Z`, buildStartedAt: `2026-11-0${day}T00:00:00Z` });
  const candidate = { status: 'candidate', buildId: 'build-C', sourceSha, branch: 'Example', releaseId: 42,
    runId: 3, attempt: 1, publishedAt: '2026-11-02T00:00:00Z', buildStartedAt: '2026-11-02T00:00:00Z' };
  const registry = [{ moduleId: 'example', name: 'Example', repository: 'Owner/Repo', branch: 'Example' }];
  const request = async () => { throw new Error('Unexpected network request'); };
  for (const [initialDay, shouldUpdate] of [[3, false], [1, true]]) {
    const original = { moduleId: 'example', buildId: 'build-A', files: [{ path: file.path, sha256: file.sha256 }], publication: publication(initialDay, 4) };
    const intermediate = { ...structuredClone(original), buildId: 'build-B', publication: publication(1, 1) };
    let current = structuredClone(original); const mutations = []; const observations = [];
    const installer = { audit: () => ({ installed: structuredClone(current) }), update: (pkg, expected) => {
      if (expected !== undefined && !isDeepStrictEqual(current, expected)) throw new Error('INSTALLATION_STATE_CHANGED');
      mutations.push(pkg.buildId); return {};
    } };
    const service = new DesktopService({ registry, request, installer, now: () => 1800000000000,
      inspectPackage: async () => true, assertGameClosed: async () => true });
    service.provider = 'modpack-bundle';
    service.refresh = async () => {
      current = structuredClone(intermediate); observations.push(current.buildId);
      const order = await assessUpdateOrder(current, candidate, 'Owner/Repo', request);
      assert.equal(order.allowed, true);
      current = structuredClone(original); observations.push(current.buildId);
      service.sources.set('example', { status: 'candidate', checkedAtMs: 1800000000000, developmentConfirmed: true,
        latestPublished: candidate, updateOrder: order, bundlePackage: { moduleId: 'example', buildId: candidate.buildId, files: [file] } });
    };
    if (shouldUpdate) assert.equal((await service.execute('update', ['example'])).results[0].status, 'committed');
    else await assert.rejects(service.execute('update', ['example']), /PUBLISHED_BUILD_IS_NOT_CONFIRMED_NEWER/);
    assert.deepEqual(observations, ['build-B', 'build-A']);
    assert.deepEqual(mutations, shouldUpdate ? ['build-C'] : []);
  }
});

test('an unavailable audit snapshot is not treated as an explicitly absent installation', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const registry = [{ moduleId: 'example', name: 'Example', branch: 'Example' }];
  for (const inspector of [undefined, { audit: () => ({}) }, { audit: () => ({ installed: undefined }) }]) {
    let refreshes = 0;
    const service = new DesktopService({ registry, getInstaller: () => inspector, assertGameClosed: async () => true });
    service.refresh = async () => { refreshes++; throw new Error('SHOULD_NOT_REFRESH'); };
    await assert.rejects(service.execute('install', ['example']), /INSTALLATION_STATE_CHANGED/);
    assert.equal(refreshes, 0);
  }
});

test('an explicitly absent installation is passed as null to the locked installer', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const fixture = artifactFixture();
  let expectedArgument; let guarded = false;
  const installer = { audit: () => ({ installed: null }), install: (pkg, expected, verifyFreshness) => {
    expectedArgument = expected; assert.equal(typeof verifyFreshness, 'function'); verifyFreshness(); guarded = true; return {};
  } };
  const service = new DesktopService({ registry: [fixture.entry], request: fixture.request, assetTransport: fixture.transport,
    getInstaller: create => create ? installer : null, now: () => fixture.nowMs,
    inspectPackage: async () => true, assertGameClosed: async () => true });
  assert.equal((await service.execute('install', ['example'])).results[0].status, 'committed');
  assert.equal(expectedArgument, null); assert.equal(guarded, true);
});

test('source expiry inside the locked transaction guard prevents file mutation', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const fixture = artifactFixture();
  let clock = fixture.nowMs; const mutations = [];
  const installer = { audit: () => ({ installed: null }), install: (pkg, expected, verifyFreshness) => {
    assert.equal(expected, null); clock += 3600000; verifyFreshness(); mutations.push(pkg.moduleId); return {};
  } };
  const service = new DesktopService({ registry: [fixture.entry], request: fixture.request, assetTransport: fixture.transport,
    installer, now: () => clock, inspectPackage: async () => true, assertGameClosed: async () => true });
  const result = await service.execute('install', ['example']);
  assert.equal(result.halted, true); assert.equal(result.results[0].status, 'failed'); assert.deepEqual(mutations, []);
});

test('a concurrent replacement or missing installation stops before the first mutation', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  for (const [operation, replacement] of [['install', 'newer'], ['update', 'newer'], ['update', 'missing'], ['uninstall', 'newer'], ['uninstall', 'missing']]) {
    const fixture = fifteenArtifactFixture(); const entry = fixture.registry[0];
    let current = operation === 'install' ? null : { ...structuredClone(fixture.installed.get(entry.moduleId)), files: [{ path: 'Example.dll', sha256: 'a'.repeat(64) }] };
    const newer = { moduleId: entry.moduleId, buildId: 'newer-D', files: [{ path: 'Example.dll', sha256: 'd'.repeat(64) }] };
    const mutations = []; let checks = 0; let injected = false;
    const replace = () => { if (!injected) { current = replacement === 'missing' ? null : structuredClone(newer); injected = true; } };
    const installer = { audit: () => ({ installed: structuredClone(current) }) };
    installer.install = installer.update = value => { mutations.push(value.moduleId); return {}; };
    installer.uninstall = id => { mutations.push(id); return {}; };
    const service = new DesktopService({ registry: [entry], request: fixture.request, assetTransport: fixture.transport,
      installer, now: () => fixture.nowMs, inspectPackage: async () => { replace(); return true; },
      assertGameClosed: async () => { checks++; if (operation === 'uninstall' && checks === 2) replace(); return true; } });
    const result = await service.execute(operation, [entry.moduleId]);
    assert.equal(result.halted, true, `${operation}/${replacement}`);
    assert.equal(result.results[0].status, 'failed', `${operation}/${replacement}`);
    assert.deepEqual(mutations, [], `${operation}/${replacement}`);
    assert.deepEqual(current, replacement === 'missing' ? null : newer, `${operation}/${replacement}`);
  }
});

test('desktop expires cached observations and resumes automatic checks after clock rollback without a window', { skip: process.platform !== 'win32' }, async () => {
  const fs = await import('node:fs'); const os = await import('node:os'); const path = await import('node:path');
  const { execFileSync } = await import('node:child_process');
  const { DesktopService } = await import('./desktop-service.mjs');
  let destination = false; let installed = null;
  const service = new DesktopService({ registry: [{ moduleId: 'ui-fixture', name: 'Fixture', branch: 'Fixture', publisherConfigured: true }],
    getInstaller: () => destination ? { audit: () => ({ installed }) } : null, targetReady: () => destination, now: () => 5000 });
  service.sources.set('ui-fixture', { status: 'candidate / verification required', checkedAtMs: 5000, developmentConfirmed: false,
    latestPublished: { status: 'candidate', buildId: 'new' } });
  const noDestination = service.rows()[0];
  destination = true; installed = { moduleId: 'ui-fixture', buildId: 'old' };
  const updateUnconfirmed = service.rows()[0];
  assert.equal(noDestination.canRequestInstall, false); assert.equal(updateUnconfirmed.canRequestUpdate, false);
  const root = fs.mkdtempSync(path.join(path.resolve('desktop'), 'qa-product-state-'));
  const source = path.join(root, 'FreshnessProbe.cs'); const executable = path.join(root, 'FreshnessProbe.exe');
  fs.writeFileSync(source, `// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
using System; using System.Collections.Generic;
static class FreshnessProbe {
  static void Check(bool value, string message) { if (!value) throw new Exception(message); }
  static int Main() { try { Run(); Console.WriteLine("PASS"); return 0; } catch (Exception error) { Console.WriteLine("FAIL: " + error.Message); return 1; } }
  static void Run() {
    var row = new Dictionary<string,object> { {"checkedAtMs", 5000L}, {"status", "current"}, {"matchesCandidate", true},
      {"canInstall", true}, {"canUpdate", true}, {"canRequestInstall", true}, {"canRequestUpdate", true}, {"canUninstall", true} };
    Check(!SourceFreshness.Expire(row, 5000, "expired"), "fresh observation");
    Check(SourceFreshness.Expire(row, 4999, "expired"), "rollback not expired");
    Check((string)row["status"] == "stale / unknown" && !(bool)row["matchesCandidate"], "stale state not stored");
    Check(!(bool)row["canInstall"] && !(bool)row["canUpdate"] && !(bool)row["canRequestInstall"] && !(bool)row["canRequestUpdate"], "source action still enabled");
    Check((bool)row["canUninstall"] && (string)row["reasonText"] == "expired", "offline removal or reason changed");
    var detail = new DetailViewState();
    Check(detail.ShouldRefreshSelection(true), "selected stale details were not refreshed");
    detail.ShowResults(); Check(!detail.ShouldRefreshSelection(true), "operation results were overwritten");
    detail.ShowSelection(); Check(detail.ShouldRefreshSelection(true), "selected details did not resume");
    Check(!detail.ShouldRefreshSelection(false), "unrelated selection refreshed");
    var unknown = new Dictionary<string,object> { {"status", "unknown"} };
    Check(!SourceFreshness.Expire(unknown, 5000, "expired"), "unobserved source mislabelled");
    var paused = new Dictionary<string,object> { {"status", "Development paused"}, {"engineeringPaused", true}, {"installedBuild", "owned"}, {"canUninstall", true} };
    Check(!SourceFreshness.Expire(paused, 5000, "expired"), "paused state mislabelled as stale");
    var form = (ManagerForm)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ManagerForm));
    var statusMethod = typeof(ManagerForm).GetMethod("PlayerStatus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    Check((string)statusMethod.Invoke(form, new object[] { paused }) == "Development paused", "paused desktop status not displayed");
    var backendFixture = (IDictionary<string,object>)Program.Json.DeserializeObject(Environment.GetEnvironmentVariable("DEV_MANAGER_SYNTHETIC_ROWS"));
    var backendRows = (object[])backendFixture["rows"];
    Check((string)statusMethod.Invoke(form, new object[] { backendRows[0] }) == "Candidate found (select a destination to check installation)", "backend no-destination candidate was incorrectly reported unavailable");
    Check((string)statusMethod.Invoke(form, new object[] { backendRows[1] }) == "Installed (candidate found; update not confirmed)", "backend update candidate was incorrectly reported unavailable");
    var candidate = new Dictionary<string,object> { {"status", "candidate / verification required"}, {"latestBuild", "new"}, {"installedBuild", ""} };
    Check((string)statusMethod.Invoke(form, new object[] { candidate }) == "Candidate found (select a destination to check installation)", "candidate was incorrectly reported unavailable before choosing a destination");
    candidate["installedBuild"] = "old";
    Check((string)statusMethod.Invoke(form, new object[] { candidate }) == "Installed (candidate found; update not confirmed)", "candidate was incorrectly reported unavailable before update verification");
    candidate["status"] = "stale / unknown";
    Check((string)statusMethod.Invoke(form, new object[] { candidate }) == "stale / unknown", "stale candidate was reported as available");
    candidate["status"] = "Local files require inspection";
    Check((string)statusMethod.Invoke(form, new object[] { candidate }) == "Files need attention", "local inspection warning was hidden by candidate");
    candidate["status"] = "queued / verification required";
    Check((string)statusMethod.Invoke(form, new object[] { candidate }) == "Verification queued", "queued verification was hidden by candidate");
    var selectionSafety = typeof(ManagerForm).Assembly.GetType("SelectionSafety");
    Check(selectionSafety != null, "hidden selections are not accounted for before operations");
    var hiddenCount = selectionSafety.GetMethod("HiddenCount");
    var selectEligible = selectionSafety.GetMethod("EligibleSelection");
    var filteredSelection = new [] { new KeyValuePair<bool,bool>(true,false), new KeyValuePair<bool,bool>(true,true) };
    Check((int)hiddenCount.Invoke(null, new object[] { filteredSelection }) == 1, "hidden selected module was not counted");
    Check((bool)selectEligible.Invoke(null, new object[] { true, false, false }), "candidate selection cleared a hidden checked module");
    Check((bool)selectEligible.Invoke(null, new object[] { false, true, true }), "eligible visible module was not selected");
    Check(!(bool)selectEligible.Invoke(null, new object[] { true, true, false }), "ineligible visible module remained selected");
    var clearSearch = new [] { new KeyValuePair<bool,bool>(true,true), new KeyValuePair<bool,bool>(true,true) };
    Check((int)hiddenCount.Invoke(null, new object[] { clearSearch }) == 0, "fully visible selection remained blocked");
    var zeroResults = new [] { new KeyValuePair<bool,bool>(true,false), new KeyValuePair<bool,bool>(true,false), new KeyValuePair<bool,bool>(false,false) };
    Check((int)hiddenCount.Invoke(null, new object[] { zeroResults }) == 2, "zero-result filter lost selected modules");
    var clearedSelection = new [] { new KeyValuePair<bool,bool>(false,false), new KeyValuePair<bool,bool>(false,false) };
    Check((int)hiddenCount.Invoke(null, new object[] { clearedSelection }) == 0, "cleared selections remained blocked");
    Check(!(bool)selectEligible.Invoke(null, new object[] { false, false, true }), "candidate selection changed an unchecked hidden module");
    Presentation.Load(System.IO.Path.Combine(Environment.CurrentDirectory, "translations.json"));
    foreach (string language in new [] { "en", "zh-Hant", "zh-Hans", "ja" }) {
      Presentation.Language = language;
      string summary = Presentation.T("{0} selected, {1} hidden. Clear search to review or clear selection.", 2, 1);
      Check(summary.Contains("2") && summary.Contains("1") && !summary.Contains("{0}") && !summary.Contains("{1}"), "selection summary placeholders failed: " + language);
      string status = Presentation.T("Candidate found (select a destination to check installation)");
      Check(language == "en" || status != "Candidate found (select a destination to check installation)", "candidate status not translated: " + language);
    }
    var invalid = new Dictionary<string,object> { {"checkedAtMs", "invalid"}, {"canInstall", true} };
    Check(SourceFreshness.Expire(invalid, 5000, "expired"), "invalid timestamp accepted");
    var old = new Dictionary<string,object> { {"checkedAtMs", 5000L}, {"canInstall", true} };
    Check(SourceFreshness.Expire(old, 3605000, "expired"), "one-hour boundary accepted");
    var start = new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);
    var clock = new AutomaticRefreshClock(start);
    Check(!clock.TakeDue(start.AddMinutes(5), true, false), "early refresh");
    Check(clock.TakeDue(start.AddMinutes(-1), true, false), "rollback did not reschedule");
    Check(!clock.TakeDue(start.AddMinutes(-1), true, false), "duplicate refresh");
    var busy = new AutomaticRefreshClock(start);
    Check(!busy.TakeDue(start.AddHours(1), true, true), "refresh while busy");
    Check(busy.TakeDue(start.AddHours(1), true, false), "busy check was lost");
    var delayed = new AutomaticRefreshClock(start); delayed.DeferUntil(start.AddHours(3));
    Check(!delayed.TakeDue(start.AddHours(1), true, false), "rate delay ignored");
    Check(delayed.TakeDue(start.AddHours(3), true, false), "rate delay never resumed");
  }
}`);
  const compiler = path.join(process.env.WINDIR, 'Microsoft.NET', 'Framework64', 'v4.0.30319', 'csc.exe');
  execFileSync(compiler, ['/nologo', '/codepage:65001', '/target:exe', '/main:FreshnessProbe', '/reference:System.Windows.Forms.dll',
    '/reference:System.Drawing.dll', '/reference:System.Web.Extensions.dll', '/reference:System.IO.Compression.dll',
    `/out:${executable}`, path.resolve('desktop/DevManager.cs'), source], { windowsHide: true, timeout: 15000, encoding: 'utf8', stdio: 'pipe' });
  const command = "$ErrorActionPreference='Stop'; $a=[Reflection.Assembly]::LoadFile($env:DEV_MANAGER_FRESHNESS_PROBE); " +
    "if($a.EntryPoint.DeclaringType.FullName -ne 'FreshnessProbe'){throw 'Unexpected probe entry point'}; " +
    "$result=$a.EntryPoint.Invoke($null,@()); if($result -ne 0){throw 'Freshness probe failed'}";
  const output = execFileSync('powershell.exe', ['-NoLogo', '-NoProfile', '-NonInteractive', '-Command', command],
    { windowsHide: true, encoding: 'utf8', timeout: 10000, env: { ...process.env, DEV_MANAGER_FRESHNESS_PROBE: executable,
      DEV_MANAGER_SYNTHETIC_ROWS: JSON.stringify({ signature: 'OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager', rows: [noDestination, updateUnconfirmed] }) } });
  assert.equal(output.trim(), 'PASS');
});

test('virtual locked update preserves concurrent state and commits an unchanged snapshot', async t => {
  const fs = (await import('node:fs')).default; const path = await import('node:path');
  const { IsolatedInstaller } = await import('./filesystem.mjs');
  const root = path.resolve('C:\\synthetic-dev-manager-root'); const meta = path.join(root, '.dev-manager');
  const nodes = new Map(); const handles = new Map(); const events = []; let inode = 1n; let nextFd = 10;
  const key = value => path.resolve(value).toLowerCase();
  const missing = () => Object.assign(new Error('Synthetic missing path'), { code: 'ENOENT' });
  const add = (name, directory, contents = Buffer.alloc(0)) => {
    const absolute = path.resolve(name); const node = { path: absolute, directory, contents: Buffer.from(contents), ino: inode++ };
    nodes.set(key(absolute), node); return node;
  };
  const directory = name => {
    const absolute = path.resolve(name); if (nodes.has(key(absolute))) return;
    const parent = path.dirname(absolute); if (parent !== absolute) directory(parent); add(absolute, true);
  };
  const get = name => { const node = nodes.get(key(name)); if (!node) throw missing(); return node; };
  const stat = node => ({ dev: 1n, ino: node.ino, nlink: 1n, size: BigInt(node.contents.length),
    isSymbolicLink: () => false, isFile: () => !node.directory, isDirectory: () => node.directory });
  const snapshot = () => [...nodes.values()].map(node => [node.path, node.directory, node.contents.toString('base64')]).sort((a, b) => a[0].localeCompare(b[0]));
  directory(root); directory(meta); directory(path.join(meta, 'staging')); directory(path.join(meta, 'backups')); directory(path.join(root, 'example'));
  const oldBytes = Buffer.from('synthetic old module'); const newBytes = Buffer.from('synthetic new module');
  const hash = value => createHash('sha256').update(value).digest('hex');
  const old = { moduleId: 'example', buildId: 'build-A', files: [{ path: 'mod.dll', sha256: hash(oldBytes) }],
    publication: { provider: 'modpack-bundle', sourceSha: 'a'.repeat(40), releaseId: 1, runId: 1, attempt: 1, publishedAt: '2026-11-01T00:00:00Z' } };
  const newer = { ...structuredClone(old), buildId: 'build-D', publication: { ...old.publication, runId: 4 } };
  const statePath = path.join(meta, 'state.json');
  const setState = record => { get(statePath).contents = Buffer.from(JSON.stringify({ schema: 1, root, modules: record ? { example: record } : {} })); };
  add(path.join(root, 'example', 'mod.dll'), false, oldBytes); add(statePath, false, '{}');
  const manager = Object.create(IsolatedInstaller.prototype);
  Object.assign(manager, { root, meta, directoryPrefix: '', fault: () => {}, check: () => {},
    load() { events.push('load'); this.stateBytes = Buffer.from(get(statePath).contents); this.state = JSON.parse(this.stateBytes.toString()); } });
  t.mock.method(fs, 'lstatSync', name => stat(get(name)));
  t.mock.method(fs, 'openSync', (name, flags) => {
    const absolute = path.resolve(name); let node = nodes.get(key(absolute));
    if (flags === 'wx') { if (node) throw Object.assign(new Error('Synthetic exists'), { code: 'EEXIST' }); get(path.dirname(absolute)); node = add(absolute, false); events.push('create:' + path.basename(absolute)); }
    else { assert.equal(flags, 'r'); node = get(absolute); }
    const fd = nextFd++; handles.set(fd, node); return fd;
  });
  t.mock.method(fs, 'writeFileSync', (fd, value) => { const node = handles.get(fd); assert.ok(node); node.contents = Buffer.from(value); });
  t.mock.method(fs, 'fstatSync', fd => stat(handles.get(fd)));
  t.mock.method(fs, 'readSync', (fd, buffer, offset, length, position) => { const data = handles.get(fd).contents; return data.copy(buffer, offset, position, Math.min(data.length, position + length)); });
  t.mock.method(fs, 'fsyncSync', () => {}); t.mock.method(fs, 'closeSync', fd => { handles.delete(fd); });
  t.mock.method(fs, 'readdirSync', name => { const parent = key(name); get(name); return [...nodes.values()].filter(node => key(path.dirname(node.path)) === parent && key(node.path) !== parent).map(node => path.basename(node.path)); });
  t.mock.method(fs, 'mkdirSync', (name, options) => { if (nodes.has(key(name))) { if (!options?.recursive) throw new Error('Synthetic directory exists'); return; } if (options?.recursive) directory(name); else { get(path.dirname(name)); add(name, true); } events.push('mkdir:' + path.basename(name)); });
  t.mock.method(fs, 'renameSync', (from, to) => {
    const source = path.resolve(from); const target = path.resolve(to); get(source); get(path.dirname(target));
    const prefix = key(source) + path.sep; const moving = [...nodes.values()].filter(node => key(node.path) === key(source) || key(node.path).startsWith(prefix));
    for (const node of moving) nodes.delete(key(node.path));
    for (const node of moving) { node.path = target + node.path.slice(source.length); nodes.set(key(node.path), node); }
    events.push('rename:' + path.basename(source));
  });
  t.mock.method(fs, 'unlinkSync', name => { get(name); nodes.delete(key(name)); events.push('unlink:' + path.basename(name)); });
  const next = { moduleId: 'example', buildId: 'build-C', files: [{ path: 'mod.dll', bytes: newBytes, sha256: hash(newBytes) }] };
  const changedFiles = structuredClone(old); changedFiles.files[0].sha256 = 'c'.repeat(64);
  for (const actual of [newer, null, { ...structuredClone(old), publication: { ...old.publication, attempt: 2 } }, changedFiles]) {
    setState(actual); const before = snapshot(); events.length = 0;
    assert.throws(() => manager.update(next, structuredClone(old)), /INSTALLATION_STATE_CHANGED/);
    assert.deepEqual(snapshot(), before);
    assert.equal(events.some(event => event.startsWith('mkdir:') || event.startsWith('rename:') || event === 'create:journal.json'), false);
  }
  setState(old); const beforeMissing = snapshot(); events.length = 0;
  assert.throws(() => manager.update(next), /INSTALLATION_SNAPSHOT_REQUIRED/);
  assert.deepEqual(snapshot(), beforeMissing); assert.deepEqual(events, []);
  get(path.join(root, 'example', 'mod.dll')).contents = Buffer.from('synthetic changed content');
  const beforeChangedContent = snapshot();
  assert.throws(() => manager.update(next, structuredClone(old)), /User changes or missing owned files/);
  assert.deepEqual(snapshot(), beforeChangedContent);
  assert.equal(nodes.has(key(path.join(meta, 'journal.json'))), false);
  get(path.join(root, 'example', 'mod.dll')).contents = Buffer.from(oldBytes);
  let expired = false;
  manager.fault = point => { if (point === 'staged') expired = true; };
  assert.throws(() => manager.update(next, structuredClone(old), () => { if (expired) throw new Error('SOURCE_UNAVAILABLE'); }), /SOURCE_UNAVAILABLE/);
  assert.deepEqual(get(path.join(root, 'example', 'mod.dll')).contents, oldBytes);
  assert.equal(JSON.parse(get(statePath).contents.toString()).modules.example.buildId, 'build-A');
  assert.equal(nodes.has(key(path.join(meta, 'journal.json'))), false);
  assert.ok([...nodes.values()].some(node => path.dirname(node.path).toLowerCase() === path.join(meta, 'staging').toLowerCase()));
  manager.fault = () => {};
  let guarded = false;
  const result = manager.update(next, structuredClone(old), () => { guarded = true; });
  assert.equal(guarded, true); assert.equal(result.operation, 'update');
  assert.deepEqual(get(path.join(root, 'example', 'mod.dll')).contents, newBytes);
  assert.deepEqual(get(path.join(result.backup, 'mod.dll')).contents, oldBytes);
  assert.equal(JSON.parse(get(statePath).contents.toString()).modules.example.buildId, 'build-C');
  const fresh = { moduleId: 'fresh', buildId: 'build-X', files: [{ path: 'fresh.dll', bytes: newBytes, sha256: hash(newBytes) }] };
  assert.equal(manager.install(fresh, null, () => {}).operation, 'install');
  assert.deepEqual(get(path.join(root, 'fresh', 'fresh.dll')).contents, newBytes);
  const installed = JSON.parse(get(statePath).contents.toString()).modules.example;
  const removed = manager.uninstall('example', installed);
  assert.equal(removed.operation, 'uninstall');
  assert.equal(nodes.has(key(path.join(root, 'example'))), false);
  assert.deepEqual(get(path.join(removed.backup, 'mod.dll')).contents, newBytes);
});

test('local request allowance uses elapsed time when the system clock moves backward', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  let wall = 1800000000000; let elapsed = 1000; let calls = 0;
  const pool = new GitHubRequestPool(async options => { calls++; return { url: options.url, status: 200, redirected: false, headers: {}, body: {} }; },
    { hourlyLimit: 1, now: () => wall, monotonic: () => elapsed });
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/first' })).status, 200);
  wall -= 7200000; elapsed += 1000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/second' })).status, 429);
  assert.equal(calls, 1);
  assert.ok(pool.status().retryAtMs - wall <= 3600001);
  elapsed = 3601001; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/third' })).status, 200);
  assert.equal(calls, 2);
});

test('clock rollback does not bypass a server retry-after cooldown', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  const initial = Date.now(); let wall = initial; let elapsed = 1000; let calls = 0;
  const pool = new GitHubRequestPool(async options => { calls++; return { url: options.url, status: calls === 1 ? 429 : 200,
    redirected: false, headers: calls === 1 ? { 'retry-after': '60' } : {}, body: {} }; },
    { hourlyLimit: 10, now: () => wall, monotonic: () => elapsed });
  await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/first' });
  wall -= 7200000; elapsed += 59000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/second' })).status, 429);
  assert.equal(calls, 1);
  elapsed += 2000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/third' })).status, 200);
  assert.equal(calls, 2);
});

test('relative server retry-after cannot be bypassed by advancing the system clock', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  let wall = 1800000000000; let elapsed = 1000; let calls = 0;
  const pool = new GitHubRequestPool(async options => { calls++; return { url: options.url, status: calls === 1 ? 429 : 200,
    redirected: false, headers: calls === 1 ? { 'retry-after': '60' } : {}, body: {} }; },
    { hourlyLimit: 10, now: () => wall, monotonic: () => elapsed });
  await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/first' });
  wall += 3600000; elapsed++; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/second' })).status, 429);
  assert.equal(calls, 1);
  elapsed += 60000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/third' })).status, 200);
  assert.equal(calls, 2);
});

test('parallel server cooldowns retain the longest observed delay', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  let wall = 1800000000000; let elapsed = 1000; let calls = 0; const replies = [];
  const pool = new GitHubRequestPool(options => { calls++; if (calls > 2) return Promise.resolve({ url: options.url, status: 200, redirected: false, headers: {}, body: {} }); return new Promise(resolve => replies.push((seconds) => resolve({
    url: options.url, status: 429, redirected: false, headers: { 'retry-after': String(seconds) }, body: {} }))); },
    { hourlyLimit: 10, now: () => wall, monotonic: () => elapsed });
  const first = pool.request({ url: 'https://api.github.com/repos/Owner/Repo/long' });
  const second = pool.request({ url: 'https://api.github.com/repos/Owner/Repo/short' });
  assert.equal(replies.length, 2); replies[0](180); await first; replies[1](60); await second;
  wall += 61000; elapsed += 61000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/next' })).status, 429);
  assert.equal(calls, 2);
});

test('parallel rate headers cannot replace zero remaining with an older larger value', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  let wall = 1800000000000; let elapsed = 1000; let calls = 0; const replies = [];
  const reset = String((wall + 120000) / 1000);
  const pool = new GitHubRequestPool(options => { calls++; if (calls > 2) return Promise.resolve({ url: options.url, status: 200, redirected: false, headers: {}, body: {} }); return new Promise(resolve => replies.push((remaining) => resolve({
    url: options.url, status: 200, redirected: false, headers: { 'x-ratelimit-remaining': String(remaining), 'x-ratelimit-reset': reset }, body: {} }))); },
    { hourlyLimit: 10, now: () => wall, monotonic: () => elapsed });
  const first = pool.request({ url: 'https://api.github.com/repos/Owner/Repo/zero' });
  const second = pool.request({ url: 'https://api.github.com/repos/Owner/Repo/older' });
  assert.equal(replies.length, 2); replies[0](0); await first; replies[1](1); await second;
  wall += 1000; elapsed += 1000; pool.beginRefresh();
  assert.equal(pool.status().remaining, 0);
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/next' })).status, 429);
  assert.equal(calls, 2);
});

test('numeric retry-after is seconds rather than an HTTP date', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  const wall = 1800000000000; const elapsed = 1000;
  const pool = new GitHubRequestPool(async options => ({ url: options.url, status: 429, redirected: false,
    headers: { 'retry-after': '3600' }, body: {} }), { now: () => wall, monotonic: () => elapsed });
  await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/first' });
  assert.equal(pool.status().retryAtMs - wall, 3600000);
});

test('zero server allowance without a reset uses a conservative retry delay', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  let wall = 1800000000000; let elapsed = 1000; let calls = 0;
  const pool = new GitHubRequestPool(async options => { calls++; return { url: options.url, status: 200, redirected: false,
    headers: calls === 1 ? { 'x-ratelimit-remaining': '0' } : {}, body: {} }; },
    { now: () => wall, monotonic: () => elapsed });
  await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/first' }); pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/second' })).status, 429);
  assert.equal(calls, 1);
  elapsed += 60001; wall += 60001; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/third' })).status, 200);
});

test('parallel requests cannot exceed the local hourly allowance', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  let calls = 0; const replies = [];
  const pool = new GitHubRequestPool(options => { calls++; return new Promise(resolve => replies.push(() => resolve({
    url: options.url, status: 200, redirected: false, headers: {}, body: {} }))); },
    { concurrency: 2, hourlyLimit: 2, now: () => 1800000000000, monotonic: () => 1000 });
  const requests = ['one', 'two', 'three', 'four'].map(name =>
    pool.request({ url: `https://api.github.com/repos/Owner/Repo/${name}` }));
  assert.equal(replies.length, 2); replies.forEach(reply => reply());
  assert.deepEqual((await Promise.all(requests)).map(response => response.status), [200, 200, 429, 429]);
  assert.equal(calls, 2);
});

test('zero remaining honors the longer of reset and retry-after', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  let wall = 1800000000000; let elapsed = 1000; let calls = 0;
  const pool = new GitHubRequestPool(async options => { calls++; return { url: options.url, status: 200,
    redirected: false, headers: calls === 1 ? { 'x-ratelimit-remaining': '0',
      'x-ratelimit-reset': String((wall + 60000) / 1000), 'retry-after': '180' } : {}, body: {} }; },
    { now: () => wall, monotonic: () => elapsed });
  await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/first' });
  wall += 61000; elapsed += 61000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/second' })).status, 429);
  assert.equal(calls, 1);
  wall += 120000; elapsed += 120000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/third' })).status, 200);
  assert.equal(calls, 2);
});

test('HTTP-date retry-after uses the server date and elapsed time', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  const server = 1800000000000; let wall = server + 3600000; let elapsed = 1000; let calls = 0;
  const pool = new GitHubRequestPool(async options => { calls++; return { url: options.url, status: calls === 1 ? 429 : 200,
    redirected: false, headers: calls === 1 ? { date: new Date(server).toUTCString(),
      'retry-after': new Date(server + 120000).toUTCString() } : {}, body: {} }; },
    { now: () => wall, monotonic: () => elapsed });
  await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/first' });
  elapsed += 119000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/second' })).status, 429);
  elapsed += 2000; pool.beginRefresh();
  assert.equal((await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/third' })).status, 200);
  assert.equal(calls, 2);
});

test('extreme server cooldown remains representable by the desktop clock', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  const pool = new GitHubRequestPool(async options => ({ url: options.url, status: 429, redirected: false,
    headers: { 'retry-after': '99999999999999999' }, body: {} }),
    { now: () => 1800000000000, monotonic: () => 1000 });
  await pool.request({ url: 'https://api.github.com/repos/Owner/Repo/first' });
  assert.equal(pool.status().retryAtMs, Date.UTC(9999, 11, 30));
});

test('fifteen distinct update comparisons require 62 uncached fake API calls', async () => {
  const { readArtifactRefs, detectArtifactModule } = await import('./artifact-source.mjs');
  const { assessUpdateOrder } = await import('./update-order.mjs');
  const f = fifteenDistinctAncestryFixture(); const snapshot = await readArtifactRefs(f.registry[0].repository, f.request);
  for (const entry of f.registry) {
    const source = await detectArtifactModule({ entry, request: f.request, nowMs: f.nowMs, snapshot });
    assert.equal(source.developmentConfirmed, true);
    assert.equal((await assessUpdateOrder(f.installed.get(entry.moduleId), source.latestPublished, entry.repository, f.request)).allowed, true);
  }
  assert.deepEqual(await readArtifactRefs(f.registry[0].repository, f.request, 'confirm'), snapshot);
  assert.equal(f.calls.length, 62);
  assert.equal(f.calls.filter(url => url.includes('/git/matching-refs/')).length, 2);
  assert.equal(f.calls.filter(url => url.includes('/contents/manifest.json')).length, 15);
  assert.equal(f.calls.filter(url => url.includes('/actions/runs/')).length, 15);
  assert.equal(f.calls.filter(url => url.includes('/compare/')).length, 30);
});

test('a budget-limited fifteen-module update resumes verified comparisons in the next window', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { GitHubRequestPool } = await import('./https-source.mjs');
  const f = fifteenDistinctAncestryFixture(); let wall = f.nowMs; let elapsed = 1000; let downloads = 0; let gameChecks = 0;
  const records = new Map([...f.installed].map(([id, record]) => [id, structuredClone(record)])); const commits = [];
  const installer = { audit: id => ({ installed: structuredClone(records.get(id) ?? null) }),
    update: (pkg, expected, guard) => { assert.deepEqual(expected, records.get(pkg.moduleId)); guard();
      records.set(pkg.moduleId, { moduleId: pkg.moduleId, buildId: pkg.buildId, publication: pkg.publication }); commits.push(pkg.moduleId); return {}; } };
  const pool = new GitHubRequestPool(f.request, { hourlyLimit: 50, now: () => wall, monotonic: () => elapsed });
  const service = new DesktopService({ registry: f.registry, request: pool.request.bind(pool), beginRefresh: () => pool.beginRefresh(),
    rateStatus: () => pool.status(), now: () => wall, installer, inspectPackage: async () => true,
    assetTransport: async options => { downloads++; return f.transport(options); }, assertGameClosed: async () => { gameChecks++; return true; } });
  const ids = f.registry.map(entry => entry.moduleId);
  await assert.rejects(service.execute('update', ids), /API_BUDGET_QUEUED_TRY_A_SMALLER_BATCH_AFTER_RETRY/);
  assert.equal(f.calls.length, 50); assert.equal(downloads, 0); assert.deepEqual(commits, []);
  assert.equal(service.rows().filter(row => row.status === 'queued / verification required').length, 6);
  wall += 3600001; elapsed += 3600001; f.calls.length = 0; gameChecks = 0;
  const result = await service.execute('update', ids);
  assert.equal(f.calls.length, 44); assert.equal(result.halted, false); assert.equal(downloads, 30);
  assert.equal(result.results.filter(row => row.status === 'committed').length, 15);
  assert.deepEqual(commits, ids); assert.equal(gameChecks, 17);
});

test('unverified comparison responses are requested again instead of reused', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0];
  const old = f.installed.get(entry.moduleId).publication.sourceSha; const url = `${f.base}/compare/${old}...${f.sha}`;
  f.routes.set(url, { status: 'ahead', base_commit: { sha: 'f'.repeat(40) }, ahead_by: 1, behind_by: 0 });
  const service = new DesktopService({ registry: [entry], request: f.request, now: () => f.nowMs,
    installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
  await service.refresh([entry.moduleId], { full: true }); assert.equal(service.rows()[0].canUpdate, false);
  f.routes.set(url, { status: 'ahead', base_commit: { sha: old }, ahead_by: 1, behind_by: 0 });
  await service.refresh([entry.moduleId], { full: true }); assert.equal(service.rows()[0].canUpdate, true);
  assert.equal(f.calls.filter(call => call === url).length, 2);
});

test('verified immutable comparisons are reused while mutable sources are checked again', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0];
  const service = new DesktopService({ registry: [entry], request: f.request, now: () => f.nowMs,
    installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
  await service.refresh([entry.moduleId], { full: true }); await service.refresh([entry.moduleId], { full: true });
  assert.equal(service.rows()[0].canUpdate, true);
  assert.equal(f.calls.filter(url => url.includes('/git/matching-refs/')).length, 4);
  assert.equal(f.calls.filter(url => url.includes('/contents/manifest.json')).length, 2);
  assert.equal(f.calls.filter(url => url.includes('/actions/runs/')).length, 2);
  assert.equal(f.calls.filter(url => url.includes('/compare/')).length, 2);
});

test('a changed installed source SHA is compared rather than using old evidence', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0];
  const service = new DesktopService({ registry: [entry], request: f.request, now: () => f.nowMs,
    installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
  await service.refresh([entry.moduleId], { full: true }); assert.equal(service.rows()[0].canUpdate, true);
  const changed = 'f'.repeat(40); f.installed.get(entry.moduleId).publication.sourceSha = changed;
  const url = `${f.base}/compare/${changed}...${f.sha}`;
  f.routes.set(url, { status: 'diverged', base_commit: { sha: changed }, ahead_by: 1, behind_by: 1 });
  await service.refresh([entry.moduleId], { full: true });
  assert.equal(service.rows()[0].canUpdate, false); assert.ok(f.calls.includes(url));
});

test('automatic discovery does not reuse update order after the installed record changes', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0];
  const service = new DesktopService({ registry: [entry], request: f.request, now: () => f.nowMs,
    installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
  await service.refresh(); assert.equal(service.rows()[0].canRequestUpdate, true);
  const changed = 'f'.repeat(40); f.installed.get(entry.moduleId).publication.sourceSha = changed;
  const url = `${f.base}/compare/${changed}...${f.sha}`;
  f.routes.set(url, { status: 'diverged', base_commit: { sha: changed }, ahead_by: 1, behind_by: 1 });
  await service.refresh();
  assert.equal(service.rows()[0].canRequestUpdate, false); assert.ok(f.calls.includes(url));
});

test('a full check followed by cached discovery never reuses an obsolete update order', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  for (const change of ['installed-source', 'branch-head']) {
    const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0];
    const service = new DesktopService({ registry: [entry], request: f.request, now: () => f.nowMs,
      installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
    await service.refresh([entry.moduleId], { full: true });
    assert.equal(service.rows()[0].canUpdate, true, `${change}: full check`);
    await service.refresh();
    assert.equal(service.rows()[0].canRequestUpdate, true, `${change}: unchanged discovery`);
    let comparison;
    if (change === 'installed-source') {
      const source = 'f'.repeat(40); f.installed.get(entry.moduleId).publication.sourceSha = source;
      comparison = `${f.base}/compare/${source}...${f.sha}`;
      f.routes.set(comparison, { status: 'diverged', base_commit: { sha: source }, ahead_by: 1, behind_by: 1 });
    } else {
      const head = 'd'.repeat(40);
      f.routes.get(`${f.base}/git/matching-refs/heads/`).find(ref => ref.ref === `refs/heads/${entry.branch}`).object.sha = head;
      comparison = `${f.base}/compare/${f.sha}...${head}`;
      f.routes.set(comparison, { status: 'diverged', base_commit: { sha: f.sha }, ahead_by: 1, behind_by: 1 });
    }
    f.calls.length = 0; await service.refresh();
    assert.equal(service.rows()[0].canRequestUpdate, false, `${change}: obsolete order remained actionable`);
    assert.ok(f.calls.includes(comparison), `${change}: changed ancestry was not checked`);
    if (change === 'branch-head') { assert.equal(service.rows()[0].status, 'unknown'); assert.doesNotMatch(service.rows()[0].reason, /queued/i); }
  }
});

test('comparison evidence is specific to repository, SHA order and valid responses', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const left = 'a'.repeat(40); const right = 'b'.repeat(40);
  const calls = []; const service = new DesktopService({ now: () => 1800000000000, request: async options => {
    calls.push(options.url); const forward = options.url === `https://api.github.com/repos/Owner/Repo/compare/${left}...${right}`;
    return { url: options.url, status: 200, redirected: false, headers: { 'synthetic-header': 'private' },
      body: { status: forward ? 'ahead' : 'diverged', base_commit: { sha: forward ? left : right }, ahead_by: 1, behind_by: forward ? 0 : 1 } };
  } });
  const options = url => ({ url, method: 'GET', redirect: 'manual', credentials: 'omit' });
  const forward = `https://api.github.com/repos/Owner/Repo/compare/${left}...${right}`;
  const reverse = `https://api.github.com/repos/Owner/Repo/compare/${right}...${left}`;
  const otherRepo = `https://api.github.com/repos/Other/Repo/compare/${left}...${right}`;
  const initial = await service.requestWithComparisonEvidence(options(forward)); initial.body.status = 'modified';
  const reused = await service.requestWithComparisonEvidence(options(forward)); assert.equal(reused.body.status, 'ahead');
  assert.deepEqual(reused.headers, {}); reused.body.base_commit.sha = 'changed';
  assert.equal((await service.requestWithComparisonEvidence(options(forward))).body.base_commit.sha, left);
  assert.equal((await service.requestWithComparisonEvidence(options(reverse))).body.status, 'diverged');
  assert.equal((await service.requestWithComparisonEvidence(options(reverse))).body.status, 'diverged');
  assert.equal((await service.requestWithComparisonEvidence(options(otherRepo))).body.status, 'diverged');
  assert.deepEqual(calls, [forward, reverse, reverse, otherRepo]);
});

test('partial or malformed comparison responses are never reused', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const base = 'a'.repeat(40); const head = 'b'.repeat(40);
  const url = `https://api.github.com/repos/Owner/Repo/compare/${base}...${head}`; const options = { url, method: 'GET', redirect: 'manual', credentials: 'omit' };
  const valid = () => ({ url, status: 200, redirected: false, headers: {},
    body: { status: 'ahead', base_commit: { sha: base }, head_commit: { sha: head }, ahead_by: 1, behind_by: 0 } });
  const alter = [
    response => { response.headers.link = '<https://example.invalid/page/2>; rel="next"'; },
    response => { delete response.body.ahead_by; },
    response => { response.body.head_commit.sha = 'c'.repeat(40); },
    response => { response.body.behind_by = 1; },
    response => { response.url += '?mismatch'; },
    response => { response.redirected = true; },
    response => { response.status = 503; },
    response => { delete response.headers; },
    response => { response.headers = new Map([['link', '<https://example.invalid/page/2>; rel="next"']]); }
  ];
  for (const edit of alter) {
    let calls = 0; const service = new DesktopService({ now: () => 1800000000000, request: async () => { calls++; const response = valid(); edit(response); return response; } });
    for (let attempt = 0; attempt < 2; attempt++) {
      try { await service.requestWithComparisonEvidence(options); }
      catch (error) { assert.match(error.message, /COMPARISON_HEAD_UNVERIFIED/); }
    }
    assert.equal(calls, 2, String(edit));
  }
});

test('comparison cache does not retain authenticated or ambiguous requests', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const base = 'a'.repeat(40); const head = 'b'.repeat(40);
  const url = `https://api.github.com/repos/Owner/Repo/compare/${base}...${head}`;
  for (const extra of [{ credentials: 'include', redirect: 'manual' }, { redirect: 'manual' },
    { credentials: 'omit', redirect: 'follow' }, { credentials: 'omit', redirect: 'manual', headers: { Authorization: 'synthetic-value' } }]) {
    let calls = 0; const service = new DesktopService({ now: () => 1800000000000, request: async options => { calls++; return {
      url: options.url, status: 200, redirected: false, headers: {}, body: { status: 'ahead', base_commit: { sha: base }, ahead_by: 1, behind_by: 0 } }; } });
    const options = { url, method: 'GET', ...extra };
    await service.requestWithComparisonEvidence(options); await service.requestWithComparisonEvidence(options);
    assert.equal(calls, 2);
  }
});

test('bounded comparison cache admits new proofs after reaching capacity', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const base = 'a'.repeat(40); let calls = 0;
  const service = new DesktopService({ now: () => 1800000000000, request: async options => { calls++; return {
    url: options.url, status: 200, redirected: false, headers: {}, body: { status: 'ahead', base_commit: { sha: base }, ahead_by: 1, behind_by: 0 } }; } });
  const options = index => ({ url: `https://api.github.com/repos/Owner/Repo/compare/${base}...${(index + 1).toString(16).padStart(40, '0')}`,
    method: 'GET', redirect: 'manual', credentials: 'omit' });
  for (let index = 0; index < 512; index++) await service.requestWithComparisonEvidence(options(index));
  await service.requestWithComparisonEvidence(options(0)); assert.equal(calls, 512);
  await service.requestWithComparisonEvidence(options(512)); await service.requestWithComparisonEvidence(options(512)); assert.equal(calls, 513);
  await service.requestWithComparisonEvidence(options(0)); assert.equal(calls, 513);
  await service.requestWithComparisonEvidence(options(1)); assert.equal(calls, 514);
});

test('a saturated comparison cache can finish new verification over two allowance windows', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { GitHubRequestPool } = await import('./https-source.mjs');
  const f = fifteenDistinctAncestryFixture(); const base = 'd'.repeat(40); let wall = f.nowMs; let elapsed = 1000;
  const prefix = `${f.base}/compare/${base}...`;
  const transport = async options => options.url.startsWith(prefix) ? { url: options.url, status: 200, redirected: false, headers: {},
    body: { status: 'ahead', base_commit: { sha: base }, ahead_by: 1, behind_by: 0 } } : f.request(options);
  const pool = new GitHubRequestPool(transport, { hourlyLimit: 50, now: () => wall, monotonic: () => elapsed });
  const service = new DesktopService({ registry: f.registry, request: pool.request.bind(pool), beginRefresh: () => pool.beginRefresh(),
    rateStatus: () => pool.status(), now: () => wall, installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
  for (let index = 0; index < 512; index++) {
    if (index && index % 50 === 0) { wall += 3600001; elapsed += 3600001; }
    await service.requestWithComparisonEvidence({ url: `${prefix}${(index + 1).toString(16).padStart(40, '0')}`,
      method: 'GET', redirect: 'manual', credentials: 'omit' });
  }
  wall += 3600001; elapsed += 3600001; const ids = f.registry.map(entry => entry.moduleId);
  await service.refresh(ids, { full: true }); assert.equal(f.calls.length, 50);
  assert.equal(service.rows().filter(row => row.status === 'queued / verification required').length, 6);
  wall += 3600001; elapsed += 3600001; f.calls.length = 0;
  await service.refresh(ids, { full: true }); assert.equal(f.calls.length, 44);
  assert.equal(service.rows().filter(row => row.canUpdate).length, 15);
});

test('full verification rejects an explicitly contradictory comparison head', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  for (const headValue of [{ sha: 'f'.repeat(40) }, { sha: 'invalid' }, null]) {
    const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0]; const installed = f.installed.get(entry.moduleId);
    const refs = f.routes.get(`${f.base}/git/matching-refs/heads/`);
    const artifact = refs.find(ref => ref.ref === `refs/heads/Development_Build/${entry.branch}`).object.sha;
    const url = `${f.base}/compare/${installed.publication.artifactCommit}...${artifact}`;
    f.routes.get(url).head_commit = headValue;
    const service = new DesktopService({ registry: [entry], request: f.request, now: () => f.nowMs,
      installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
    await service.refresh([entry.moduleId], { full: true }); assert.equal(service.rows()[0].canUpdate, false);
  }
  const valid = fifteenDistinctAncestryFixture(); const entry = valid.registry[0];
  const service = new DesktopService({ registry: [entry], request: valid.request, now: () => valid.nowMs,
    installer: { audit: id => ({ installed: structuredClone(valid.installed.get(id)) }) } });
  await service.refresh([entry.moduleId], { full: true }); assert.equal(service.rows()[0].canUpdate, true);
});

test('source-branch ancestry rejects a comparison that declares the wrong head', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0];
  const head = 'e'.repeat(40); const refs = f.routes.get(`${f.base}/git/matching-refs/heads/`);
  refs.find(ref => ref.ref === `refs/heads/${entry.branch}`).object.sha = head;
  f.routes.set(`${f.base}/compare/${f.sha}...${head}`, { status: 'ahead', base_commit: { sha: f.sha },
    head_commit: { sha: 'f'.repeat(40) }, ahead_by: 1, behind_by: 0 });
  const service = new DesktopService({ registry: [entry], request: f.request, now: () => f.nowMs,
    installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
  await service.refresh([entry.moduleId], { full: true }); assert.equal(service.rows()[0].canUpdate, false);
  assert.equal(service.sources.get(entry.moduleId).developmentConfirmed, false);
  const { detectArtifactModule, readArtifactRefs } = await import('./artifact-source.mjs');
  const snapshot = await readArtifactRefs(entry.repository, f.request);
  const direct = await detectArtifactModule({ entry, request: f.request, nowMs: f.nowMs, snapshot });
  assert.equal(direct.developmentConfirmed, false);
});

test('comparison consumers reject contradictory heads and non-forward outcomes without the desktop cache', async () => {
  const { assessUpdateOrder } = await import('./update-order.mjs');
  for (const variant of ['wrong-head', 'invalid-head', 'wrong-direction', 'unknown', 'correct-head', 'missing-head']) {
    const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0]; const installed = f.installed.get(entry.moduleId);
    const refs = f.routes.get(`${f.base}/git/matching-refs/heads/`); const artifact = refs.find(ref => ref.ref === `refs/heads/Development_Build/${entry.branch}`).object.sha;
    const response = f.routes.get(`${f.base}/compare/${installed.publication.artifactCommit}...${artifact}`);
    if (variant === 'wrong-head') response.head_commit = { sha: 'f'.repeat(40) };
    if (variant === 'invalid-head') response.head_commit = null;
    if (variant === 'wrong-direction') { response.status = 'behind'; response.behind_by = 1; }
    if (variant === 'unknown') response.status = 'unknown';
    if (variant === 'correct-head') response.head_commit = { sha: artifact };
    const { detectArtifactModule, readArtifactRefs } = await import('./artifact-source.mjs');
    const snapshot = await readArtifactRefs(entry.repository, f.request);
    const source = await detectArtifactModule({ entry, request: f.request, nowMs: f.nowMs, snapshot });
    const result = await assessUpdateOrder(installed, source.latestPublished, entry.repository, f.request);
    assert.equal(result.allowed, variant === 'correct-head' || variant === 'missing-head', variant);
  }
});

test('a mixed-repository artifact batch is refused before reading shared refs', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { localizeReply } = await import('./localization.mjs'); const f = fifteenArtifactFixture();
  const entries = [f.registry[0], { ...f.registry[1], repository: 'Other/Repo' }];
  const service = new DesktopService({ registry: entries, request: f.request, now: () => f.nowMs,
    installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
  const result = await service.refresh(entries.map(entry => entry.moduleId), { full: true });
  assert.equal(f.calls.length, 0); assert.equal(result.rows.filter(row => row.canUpdate).length, 0);
  assert.ok(result.rows.every(row => row.reason === 'Mixed-repository batches are refused. Check each repository separately.'));
  for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
    const localized = localizeReply({ ok: true, result }, { preferences: { language, theme: 'system' } });
    assert.ok(localized.result.rows.every(row => row.reasonText && !row.reasonDiagnostic));
  }
});

test('non-JSON HTTP failures preserve rate limits through the real API transport', async () => {
  const https = (await import('node:https')).default; const { EventEmitter } = await import('node:events');
  const { makeGitHubApiRequest, GitHubRequestPool } = await import('./https-source.mjs');
  const { DesktopService } = await import('./desktop-service.mjs');
  const originalRequest = https.request;
  try {
    for (const phase of ['initial', 'confirm']) {
      for (const fault of ['empty-429', 'text-429', 'empty-403', 'text-403', 'plain-403', 'text-302', 'empty-404', 'text-500', 'text-200', 'limited-empty-200', 'limited-text-200', 'limited-stream-200', 'limited-oversize-200', 'limited-request-200']) {
        const f = artifactFixture(); let calls = 0; let refs = 0; let destroyed = 0;
        https.request = (url, options, receive) => {
          const req = new EventEmitter(); req.setTimeout = () => req;
          req.destroy = error => { if (error) req.emit('error', error); return req; };
          req.end = () => queueMicrotask(() => {
            try {
              calls++; const address = url.toString(); const isRefs = address.endsWith('/git/matching-refs/heads/');
              if (isRefs) refs++;
              const fail = isRefs && refs === (phase === 'initial' ? 1 : 2);
              const status = fail ? Number(fault.slice(-3)) : 200;
              const text = fail ? (fault.includes('empty') ? '' : 'non-JSON server response') : JSON.stringify(f.routes.get(address));
              if (text === undefined) throw new Error('Unexpected synthetic route');
              const res = new EventEmitter(); let closed = false;
              res.statusCode = status; res.headers = fail && fault.startsWith('limited-') ?
                { 'x-ratelimit-remaining': '0', 'x-ratelimit-reset': String(f.nowMs / 1000 + 120) } :
                status === 429 || status === 403 && fault !== 'plain-403' ? { 'retry-after': '120' } : {};
              res.destroy = error => { closed = true; destroyed++; if (error) queueMicrotask(() => res.emit('error', error)); return res; };
              receive(res);
              if (!closed && fail && fault === 'limited-stream-200') res.emit('error', new Error('Synthetic stream failure'));
              else if (!closed && fail && fault === 'limited-request-200') req.emit('error', new Error('Synthetic request failure'));
              else if (!closed) {
                if (fail && fault === 'limited-oversize-200') res.emit('data', Buffer.alloc(4 * 1024 * 1024 + 1));
                else if (text) res.emit('data', Buffer.from(text));
                if (!closed) res.emit('end');
              }
            } catch (error) { req.emit('error', error); }
          });
          return req;
        };
        const now = f.nowMs; const pool = new GitHubRequestPool(makeGitHubApiRequest(f.entry.repository), { now: () => now, monotonic: () => 1000 });
        const service = new DesktopService({ registry: [f.entry], request: pool.request.bind(pool), now: () => now,
          beginRefresh: () => pool.beginRefresh(), rateStatus: () => pool.status() });
        const result = await service.refresh(); const limited = fault.endsWith('429') || fault.endsWith('403') && fault !== 'plain-403';
        assert.equal(result.rows[0].status, limited ? 'queued / verification required' : 'unknown', `${phase}/${fault}`);
        assert.equal(result.rows[0].canRequestInstall, false, `${phase}/${fault}`);
        const cooldown = limited || fault.startsWith('limited-') ? 120000 : fault === 'plain-403' ? 60000 : 0;
        assert.equal(pool.status().retryAtMs, cooldown ? now + cooldown : 0, `${phase}/${fault}`);
        if (cooldown) { const before = calls; const retry = await service.refresh(); assert.equal(calls, before, `${phase}/${fault}: cooldown did not prevent retry`);
          assert.equal(retry.rows[0].status, 'queued / verification required', `${phase}/${fault}: retry was not queued`); }
        if (!fault.endsWith('200')) assert.equal(destroyed, 1, `${phase}/${fault}: non-success response body was not closed`);
        if (fault.endsWith('200')) {
          refs = phase === 'initial' ? 0 : 1;
          const expected = fault === 'limited-request-200' ? /API_REQUEST_FAILED/ :
            fault === 'limited-stream-200' || fault === 'limited-oversize-200' ? /API_RESPONSE_FAILED/ : /API_JSON_INVALID/;
          await assert.rejects(makeGitHubApiRequest(f.entry.repository)({ url: `${f.base}/git/matching-refs/heads/`, method: 'GET' }), error => {
            assert.match(error.message, expected); assert.equal(error.headers, undefined); assert.equal(error.status, undefined);
            assert.deepEqual(Object.getOwnPropertySymbols(error), []); return true;
          });
        }
      }
    }
    const forged = new Error('API_JSON_INVALID'); forged.headers = { 'x-ratelimit-remaining': '0', 'x-ratelimit-reset': '9999999999' };
    const untrusted = new GitHubRequestPool(async () => { throw forged; }, { now: () => 1800000000000, monotonic: () => 1000 });
    await assert.rejects(untrusted.request({ url: 'https://api.github.com/repos/Owner/Repo/test' }), /API_JSON_INVALID/);
    assert.equal(untrusted.status().retryAtMs, 0, 'unmarked transport error must not forge a response observation');
    https.request = () => {
      const req = new EventEmitter(); req.setTimeout = () => req; req.destroy = () => req;
      req.end = () => queueMicrotask(() => req.emit('error', new Error('Synthetic failure before headers'))); return req;
    };
    let downloads = 0; let mutations = 0; const f = artifactFixture();
    const beforeHeaders = new GitHubRequestPool(makeGitHubApiRequest(f.entry.repository), { now: () => f.nowMs, monotonic: () => 1000 });
    const service = new DesktopService({ registry: [f.entry], request: beforeHeaders.request.bind(beforeHeaders), now: () => f.nowMs,
      beginRefresh: () => beforeHeaders.beginRefresh(), assertGameClosed: async () => true,
      installer: { audit: () => ({ installed: null }), install: () => { mutations++; } },
      assetTransport: async () => { downloads++; throw new Error('unexpected download'); } });
    assert.equal((await service.refresh()).rows[0].status, 'unknown');
    assert.equal(beforeHeaders.status().retryAtMs, 0, 'no headers must not invent a cooldown');
    await assert.rejects(service.execute('install', [f.entry.moduleId]), /SOURCE_UNAVAILABLE/);
    assert.equal(downloads, 0); assert.equal(mutations, 0);
  } finally { https.request = originalRequest; }
});

test('shared ref failures distinguish unavailable sources from an actual request limit', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  const { localizeReply, translate } = await import('./localization.mjs');
  const unavailable = 'The shared artifact source snapshot could not be verified. Check the connection and try again.';
  for (const phase of ['initial', 'confirm']) {
    for (const fault of ['http-500', 'network-error', 'invalid-headers', 'forbidden', 'rate-429', 'rate-403']) {
      const f = artifactFixture(); let downloads = 0; let mutations = 0;
      const request = async options => {
        if (options.url.endsWith('/git/matching-refs/heads/') && options.cachePhase === phase) {
          if (fault === 'network-error') throw new Error('Synthetic connection failure');
          if (fault === 'invalid-headers') return { ...(await f.request(options)), headers: { link: 'partial' } };
          const status = fault === 'http-500' ? 500 : fault === 'rate-429' ? 429 : 403;
          const headers = fault === 'rate-403' ? { 'x-ratelimit-remaining': '0' } : {};
          return { url: options.url, status, redirected: false, headers, body: {} };
        }
        return f.request(options);
      };
      const installer = { audit: () => ({ installed: null }), install: () => { mutations++; } };
      const service = new DesktopService({ registry: [f.entry], request, installer, now: () => f.nowMs,
        assertGameClosed: async () => true, assetTransport: async () => { downloads++; throw new Error('unexpected download'); } });
      const result = await service.refresh(); const row = result.rows[0];
      const limited = fault === 'rate-429' || fault === 'rate-403';
      assert.equal(row.status, limited ? 'queued / verification required' : 'unknown', `${phase}/${fault}`);
      assert.equal(row.latestBuild, '', `${phase}/${fault}`);
      assert.equal(row.canRequestInstall, false, `${phase}/${fault}`);
      if (!limited) {
        assert.equal(row.reason, unavailable, `${phase}/${fault}`);
        for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
          const message = localizeReply({ result: { rows: [row] } }, { preferences: { language, theme: 'system' } });
          assert.equal(message.result.rows[0].reasonText, translate(language, unavailable), `${phase}/${fault}/${language}`);
        }
      }
      await assert.rejects(service.execute('install', [f.entry.moduleId]),
        limited ? /API_BUDGET_QUEUED_TRY_A_SMALLER_BATCH_AFTER_RETRY/ : /SOURCE_UNAVAILABLE/, `${phase}/${fault}`);
      assert.equal(downloads, 0, `${phase}/${fault}`); assert.equal(mutations, 0, `${phase}/${fault}`);
    }
  }
  const f = artifactFixture();
  const limited = new DesktopService({ registry: [f.entry], request: f.request, now: () => f.nowMs, automaticRequestLimit: 1 });
  assert.equal((await limited.refresh()).rows[0].status, 'queued / verification required');
  assert.equal(f.calls.length, 0);
  const later = fifteenArtifactFixture();
  const request = async options => options.cachePhase === 'confirm' && options.url.endsWith('/git/matching-refs/heads/') ?
    { url: options.url, status: 500, redirected: false, headers: {}, body: {} } : later.request(options);
  const finalFailure = new DesktopService({ registry: later.registry.slice(0, 4), request, now: () => later.nowMs, automaticRequestLimit: 3 });
  const result = await finalFailure.refresh();
  assert.ok(result.discovery.deferred > 0, 'fixture must defer earlier module checks');
  assert.ok(result.rows.every(row => row.status === 'unknown' && row.reason === unavailable));
});

test('shared ref snapshots reject absent, malformed or paginated response headers', async () => {
  const { DesktopService } = await import('./desktop-service.mjs');
  for (const variant of ['missing', 'null', 'array', 'map', 'upper-link', 'null-prototype', 'valid']) {
    const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0];
    const request = async options => { const response = await f.request(options);
      if (options.url.includes('/git/matching-refs/')) {
        if (variant === 'missing') delete response.headers;
        if (variant === 'null') response.headers = null;
        if (variant === 'array') response.headers = [];
        if (variant === 'map') response.headers = new Map([['link', 'partial']]);
        if (variant === 'upper-link') response.headers.Link = '<https://example.invalid/page/2>; rel="next"';
        if (variant === 'null-prototype') response.headers = Object.assign(Object.create(null), { 'x-ratelimit-remaining': '9' });
      }
      return response;
    };
    const service = new DesktopService({ registry: [entry], request, now: () => f.nowMs,
      installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
    await service.refresh([entry.moduleId], { full: true });
    assert.equal(service.rows()[0].canUpdate, variant === 'valid' || variant === 'null-prototype', variant);
  }
});

test('cached comparisons cannot preserve an update after source refs change during verification', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0]; let changed = false;
  const request = async options => { const response = await f.request(options);
    if (changed && options.cachePhase === 'confirm') response.body.find(ref => ref.ref === `refs/heads/${entry.branch}`).object.sha = 'f'.repeat(40);
    return response; };
  const service = new DesktopService({ registry: [entry], request, now: () => f.nowMs,
    installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }) } });
  await service.refresh([entry.moduleId], { full: true }); assert.equal(service.rows()[0].canUpdate, true);
  changed = true; await service.refresh([entry.moduleId], { full: true });
  assert.equal(service.rows()[0].canUpdate, false); assert.match(service.rows()[0].reason, /changed/);
});

test('cached comparison evidence cannot bypass server cooldown or stale observations', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { GitHubRequestPool } = await import('./https-source.mjs');
  const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0]; let wall = f.nowMs; let elapsed = 1000; let transactions = 0;
  const pool = new GitHubRequestPool(f.request, { hourlyLimit: 50, now: () => wall, monotonic: () => elapsed });
  const service = new DesktopService({ registry: [entry], request: pool.request.bind(pool), beginRefresh: () => pool.beginRefresh(),
    rateStatus: () => pool.status(), now: () => wall, installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }),
      update: () => { transactions++; } }, assertGameClosed: async () => true });
  await service.refresh([entry.moduleId], { full: true }); assert.equal(service.rows()[0].canUpdate, true);
  wall += 3600001; elapsed += 3600001; assert.equal(service.rows()[0].canUpdate, false);
  pool.observeServer({ 'retry-after': '60' }, 429); const before = f.calls.length;
  await assert.rejects(service.execute('update', [entry.moduleId]), /API_BUDGET_QUEUED_TRY_A_SMALLER_BATCH_AFTER_RETRY/);
  assert.equal(f.calls.length, before); assert.equal(transactions, 0);
});

test('a cooldown starting on final refs remains queued before any update download', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { GitHubRequestPool } = await import('./https-source.mjs');
  const f = fifteenDistinctAncestryFixture(); const entry = f.registry[0]; let downloads = 0; let transactions = 0;
  const pool = new GitHubRequestPool(async options => { const response = await f.request(options);
    if (options.cachePhase === 'confirm') response.headers = { 'x-ratelimit-remaining': '0',
      'x-ratelimit-reset': String((f.nowMs + 60000) / 1000) }; return response;
  }, { now: () => f.nowMs, monotonic: () => 1000 });
  const service = new DesktopService({ registry: [entry], request: pool.request.bind(pool), beginRefresh: () => pool.beginRefresh(),
    rateStatus: () => pool.status(), now: () => f.nowMs, installer: { audit: id => ({ installed: structuredClone(f.installed.get(id)) }),
      update: () => { transactions++; } }, assertGameClosed: async () => true, inspectPackage: async () => true,
    assetTransport: async options => { downloads++; return f.transport(options); } });
  await assert.rejects(service.execute('update', [entry.moduleId]), /API_BUDGET_QUEUED_TRY_A_SMALLER_BATCH_AFTER_RETRY/);
  assert.equal(downloads, 0); assert.equal(transactions, 0);
});

test('paused modules reject install and update before source or file transactions', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { localizeReply } = await import('./localization.mjs');
  for (const operation of ['install', 'update']) {
    const f = artifactFixture(); const entry = { ...f.entry, engineeringPaused: true }; let sourceCalls = 0; let transactions = 0;
    const installed = operation === 'install' ? null : { moduleId: entry.moduleId, buildId: 'old' };
    const service = new DesktopService({ registry: [entry], request: async () => { sourceCalls++; throw new Error('unexpected source call'); },
      installer: { audit: () => ({ installed }), install: () => { transactions++; }, update: () => { transactions++; } },
      assertGameClosed: async () => true, now: () => f.nowMs });
    service.sources.set(entry.moduleId, { checkedAtMs: f.nowMs, developmentConfirmed: true,
      latestPublished: { status: 'candidate', buildId: 'candidate' }, updateOrder: { allowed: true } });
    const row = service.rows()[0];
    assert.equal(row.canInstall, false); assert.equal(row.canUpdate, false);
    assert.equal(row.canRequestInstall, false); assert.equal(row.canRequestUpdate, false);
    assert.equal(row.canUninstall, operation === 'update');
    assert.equal(row.status, 'Development paused'); assert.equal(row.latestBuild, '');
    assert.equal(row.matchesCandidate, false); assert.equal(row.checkedAtMs, null);
    const result = await service.execute(operation, [entry.moduleId]);
    assert.equal(result.halted, true); assert.equal(result.results[0].status, 'not-attempted');
    assert.equal(sourceCalls, 0); assert.equal(transactions, 0);
    for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
      const localized = localizeReply({ ok: true, result }, { preferences: { language, theme: 'system' } });
      assert.notEqual(localized.result.results[0].reasonText, ''); assert.equal(localized.result.results[0].reasonDiagnostic, '');
      if (language !== 'en') assert.notEqual(localized.result.results[0].reasonText, result.results[0].reason);
    }
  }
});

test('a mixed batch containing a paused module stops every selected installation and update', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const { localizeReply } = await import('./localization.mjs');
  for (const operation of ['install', 'update']) {
    const f = fifteenArtifactFixture(); const entries = [{ ...f.registry[0], engineeringPaused: true }, f.registry[1]];
    let sourceCalls = 0; let transactions = 0;
    const service = new DesktopService({ registry: entries, request: async () => { sourceCalls++; throw new Error('unexpected source call'); },
      installer: { audit: id => ({ installed: operation === 'install' ? null : f.installed.get(id) }),
        install: () => { transactions++; }, update: () => { transactions++; } }, assertGameClosed: async () => true, now: () => f.nowMs });
    const result = await service.execute(operation, entries.map(entry => entry.moduleId));
    assert.equal(result.halted, true); assert.deepEqual(result.results.map(row => row.status), ['not-attempted', 'not-attempted']);
    assert.equal(sourceCalls, 0); assert.equal(transactions, 0);
    for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
      const localized = localizeReply({ ok: true, result }, { preferences: { language, theme: 'system' } });
      for (const row of localized.result.results) { assert.notEqual(row.reasonText, ''); assert.equal(row.reasonDiagnostic, ''); }
    }
  }
});

test('a paused module can still remove its owned installation without source access', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const f = artifactFixture();
  const entry = { ...f.entry, engineeringPaused: true }; let record = { moduleId: entry.moduleId, buildId: 'owned' }; let sources = 0;
  const installer = { audit: () => ({ installed: structuredClone(record) }), uninstall: (id, expected) => {
    assert.equal(id, entry.moduleId); assert.deepEqual(expected, record); record = null; return {}; } };
  const service = new DesktopService({ registry: [entry], installer, request: async () => { sources++; throw new Error('unexpected source call'); },
    assertGameClosed: async () => true, now: () => f.nowMs });
  const result = await service.execute('uninstall', [entry.moduleId]);
  assert.equal(result.results[0].status, 'committed'); assert.equal(record, null); assert.equal(sources, 0);
});

test('refresh does not request individual sources of paused modules', async () => {
  const { DesktopService } = await import('./desktop-service.mjs'); const f = fifteenArtifactFixture();
  const entries = [{ ...f.registry[0], engineeringPaused: true }, f.registry[1]];
  const service = new DesktopService({ registry: entries, request: f.request, now: () => f.nowMs,
    installer: { audit: () => ({ installed: null }) } });
  await service.refresh();
  const pausedArtifact = f.routes.get(`${f.base}/git/matching-refs/heads/`)[1].object.sha;
  assert.equal(f.calls.includes(`${f.base}/contents/manifest.json?ref=${pausedArtifact}`), false);
  assert.equal(service.rows()[0].canRequestInstall, false); assert.equal(service.rows()[1].canRequestInstall, true);
});

test('install observes exhausted quota before a stalled response body finishes', async () => {
  const https = (await import('node:https')).default;
  const { EventEmitter } = await import('node:events');
  const { makeGitHubApiRequest, GitHubRequestPool } = await import('./https-source.mjs');
  const { DesktopService } = await import('./desktop-service.mjs');
  const f = artifactFixture(); const calls = []; const runUrl = `${f.base}/actions/runs/100/attempts/1`;
  const originalRequest = https.request; const originalSetTimeout = globalThis.setTimeout;
  let expireModule; let heldResponse; let downloads = 0; let installs = 0;
  try {
    globalThis.setTimeout = (callback, delay, ...args) => {
      if (delay === 30000 && !expireModule) { expireModule = () => callback(...args); return undefined; }
      return originalSetTimeout(callback, delay, ...args);
    };
    https.request = (url, options, receive) => {
      const request = new EventEmitter(); request.setTimeout = () => request;
      request.destroy = error => { if (error) request.emit('error', error); return request; };
      request.end = () => queueMicrotask(() => {
        const address = url.toString(); calls.push(address);
        const response = new EventEmitter(); response.statusCode = 200; response.headers = {};
        response.destroy = error => { if (error) response.emit('error', error); return response; };
        if (address === runUrl) {
          response.headers = { 'x-ratelimit-remaining': '0', 'x-ratelimit-reset': String(f.nowMs / 1000 + 120) };
          heldResponse = response; receive(response);
          if (!expireModule) { request.emit('error', new Error('Missing synthetic module timeout')); return; }
          expireModule(); return;
        }
        if (!f.routes.has(address)) { request.emit('error', new Error('Unexpected synthetic route')); return; }
        receive(response); response.emit('data', Buffer.from(JSON.stringify(f.routes.get(address)))); response.emit('end');
      });
      return request;
    };
    const pool = new GitHubRequestPool(makeGitHubApiRequest(f.entry.repository), { now: () => f.nowMs, monotonic: () => 1000 });
    const service = new DesktopService({ registry: [f.entry], request: pool.request.bind(pool), now: () => f.nowMs,
      beginRefresh: () => pool.beginRefresh(), rateStatus: () => pool.status(), assertGameClosed: async () => true,
      installer: { audit: () => ({ installed: null }), install: () => { installs++; } },
      assetTransport: async () => { downloads++; throw new Error('Unexpected synthetic download'); } });
    const outcome = await service.execute('install', [f.entry.moduleId]).then(() => null, error => error);
    assert.deepEqual(calls, [`${f.base}/git/matching-refs/heads/`, `${f.base}/contents/manifest.json?ref=${f.artifact}`, runUrl]);
    assert.match(outcome?.message ?? '', /API_BUDGET_QUEUED_TRY_A_SMALLER_BATCH_AFTER_RETRY/);
    assert.equal(pool.status().retryAtMs, f.nowMs + 120000);
    assert.equal(service.rows()[0].canRequestInstall, false);
    assert.equal(downloads, 0); assert.equal(installs, 0);
  } finally {
    if (heldResponse) heldResponse.emit('error', new Error('Synthetic held response closed'));
    https.request = originalRequest; globalThis.setTimeout = originalSetTimeout;
    await Promise.resolve(); await Promise.resolve();
  }
});

test('rejected injected transport cannot forge early quota headers through options or error', async () => {
  const { GitHubRequestPool } = await import('./https-source.mjs');
  const headers = { 'x-ratelimit-remaining': '0', 'x-ratelimit-reset': '1800000120' };
  const forged = Object.assign(new Error('API_JSON_INVALID'), { headers, status: 429 });
  const pool = new GitHubRequestPool(async options => {
    for (const key of Reflect.ownKeys(options)) {
      if (typeof options[key] === 'function') {
        try { options[key]({ headers, status: 429 }); } catch {}
      }
    }
    throw forged;
  }, { now: () => 1800000000000, monotonic: () => 1000 });
  await assert.rejects(pool.request({ url: 'https://api.github.com/repos/Owner/Repo/test', method: 'GET' }), /API_JSON_INVALID/);
  assert.equal(pool.status().retryAtMs, 0);
});

test('queued requests stop after quota headers while already running requests finish', async () => {
  const https = (await import('node:https')).default;
  const { EventEmitter } = await import('node:events');
  const { makeGitHubApiRequest, GitHubRequestPool } = await import('./https-source.mjs');
  const originalRequest = https.request; const calls = []; let heldResponse; let settled = false; let first;
  try {
    https.request = (url, options, receive) => {
      const request = new EventEmitter(); request.setTimeout = () => request;
      request.destroy = error => { if (error) request.emit('error', error); return request; };
      request.end = () => queueMicrotask(() => {
        const address = url.toString(); calls.push(address);
        const response = new EventEmitter(); response.statusCode = 200;
        response.headers = address.endsWith('/one') ?
          { 'x-ratelimit-remaining': '0', 'x-ratelimit-reset': '1800000120' } : {};
        response.destroy = error => { if (error) response.emit('error', error); return response; };
        receive(response);
        if (address.endsWith('/one')) { heldResponse = response; return; }
        response.emit('data', Buffer.from('{}')); response.emit('end');
      });
      return request;
    };
    const pool = new GitHubRequestPool(makeGitHubApiRequest('Owner/Repo'),
      { now: () => 1800000000000, monotonic: () => 1000 });
    const options = name => ({ url: `https://api.github.com/repos/Owner/Repo/${name}`, method: 'GET' });
    first = pool.request(options('one'));
    const second = pool.request(options('two')); const third = pool.request(options('three'));
    assert.equal((await second).status, 200);
    assert.equal((await third).status, 429);
    assert.deepEqual(calls, [options('one').url, options('two').url]);
    heldResponse.emit('data', Buffer.from('{}')); heldResponse.emit('end'); settled = true;
    assert.equal((await first).status, 200);
  } finally {
    if (heldResponse && !settled) heldResponse.emit('error', new Error('Synthetic held response closed'));
    if (first) await first.catch(() => {});
    https.request = originalRequest;
  }
});

test('slow successful or failed bodies do not restart the quota cooldown', async () => {
  const https = (await import('node:https')).default;
  const { EventEmitter } = await import('node:events');
  const { makeGitHubApiRequest, GitHubRequestPool } = await import('./https-source.mjs');
  const originalRequest = https.request; const start = 1800000000000;
  for (const outcome of ['success', 'failure']) {
    let wall = start; let elapsed = 1000; let heldResponse; let finished = false; let pending; let markHeaders;
    const headersReceived = new Promise(resolve => { markHeaders = resolve; });
    try {
      https.request = (url, options, receive) => {
        const request = new EventEmitter(); request.setTimeout = () => request;
        request.destroy = error => { if (error) request.emit('error', error); return request; };
        request.end = () => queueMicrotask(() => {
          const response = new EventEmitter(); response.statusCode = 200;
          response.headers = { date: new Date(start).toUTCString(), 'x-ratelimit-remaining': '0', 'x-ratelimit-reset': '1800000120' };
          response.destroy = error => { if (error) response.emit('error', error); return response; };
          heldResponse = response; receive(response); markHeaders();
        });
        return request;
      };
      const pool = new GitHubRequestPool(makeGitHubApiRequest('Owner/Repo'), { now: () => wall, monotonic: () => elapsed });
      pending = pool.request({ url: 'https://api.github.com/repos/Owner/Repo/slow', method: 'GET' });
      await headersReceived; assert.equal(pool.status().retryAtMs, start + 120000, outcome);
      wall += 30000; elapsed += 30000;
      if (outcome === 'success') { heldResponse.emit('data', Buffer.from('{}')); heldResponse.emit('end'); }
      else heldResponse.emit('error', new Error('Synthetic stream failure'));
      finished = true;
      if (outcome === 'success') assert.equal((await pending).status, 200);
      else await assert.rejects(pending, /API_RESPONSE_FAILED/);
      assert.equal(pool.status().retryAtMs, start + 120000, outcome);
    } finally {
      if (heldResponse && !finished) heldResponse.emit('error', new Error('Synthetic held response closed'));
      if (pending) await pending.catch(() => {});
      https.request = originalRequest;
    }
  }
});

// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
async function sameBuildManifestFixture() {
  const { detectArtifactModule } = await import('./artifact-source.mjs');
  const { verifyPublication } = await import('./core.mjs');
  const { DesktopService } = await import('./desktop-service.mjs');
  const f = artifactFixture();
  const original = await detectArtifactModule({ entry: f.entry, request: f.request, nowMs: f.nowMs });
  assert.equal(original.developmentConfirmed, true);
  const c = original.latestPublished;
  const publication = { provider: 'artifact-branch', sourceSha: c.sourceSha, artifactBranch: c.artifactBranch,
    artifactCommit: c.artifactCommit, manifestSha256: c.manifestSha256, runId: c.runId, attempt: c.attempt,
    buildStartedAt: c.buildStartedAt, publishedAt: c.publishedAt };
  verifyPublication(publication);
  let installed = { moduleId: f.entry.moduleId, buildId: c.buildId, publication };
  const calls = { downloads: 0, writes: 0 };
  const installer = { audit: () => ({ installed: structuredClone(installed) }),
    update: () => { calls.writes++; throw new Error('UNEXPECTED_WRITE'); },
    uninstall: () => { calls.writes++; throw new Error('UNEXPECTED_WRITE'); } };
  const service = new DesktopService({ registry: [f.entry], request: f.request, installer, now: () => f.nowMs,
    assertGameClosed: async () => true, assetTransport: async () => { calls.downloads++; throw new Error('UNEXPECTED_DOWNLOAD'); } });
  return { f, service, installer, calls, original, setInstalled: value => { installed = value; }, getInstalled: () => structuredClone(installed) };
}
function changeSameBuildManifest(f) {
  f.manifest.files.push({ path: 'files/Extra.dll', name: 'Extra.dll', assemblyName: 'Extra', size: 1, sha256: 'e'.repeat(64) });
  f.advanceArtifact('d'.repeat(40));
}
test('artifact identity conflict: fully verified same build with changed manifest is not reported current', async () => {
  const { detectArtifactModule } = await import('./artifact-source.mjs');
  const q = await sameBuildManifestFixture(); changeSameBuildManifest(q.f);
  const candidate = await detectArtifactModule({ entry: q.f.entry, request: q.f.request, nowMs: q.f.nowMs });
  assert.equal(candidate.developmentConfirmed, true);
  assert.equal(candidate.latestPublished.buildId, q.original.latestPublished.buildId);
  assert.notEqual(candidate.latestPublished.manifestSha256, q.original.latestPublished.manifestSha256);
  const row = (await q.service.refresh()).rows[0];
  assert.equal(row.matchesCandidate, false);
  assert.equal(row.status, 'unknown');
  assert.equal(row.canRequestUpdate, false);
  assert.match(row.reason, /manifest differs/i);
  assert.deepEqual(q.calls, { downloads: 0, writes: 0 });
});
test('artifact identity conflict: manual update refuses instead of returning current', async () => {
  const q = await sameBuildManifestFixture(); changeSameBuildManifest(q.f);
  await assert.rejects(q.service.execute('update', [q.f.entry.moduleId]), /ARTIFACT_IDENTITY_UNVERIFIED/);
  assert.deepEqual(q.calls, { downloads: 0, writes: 0 });
});
test('artifact identity conflict: identical manifest remains current after artifact commit advances', async () => {
  const q = await sameBuildManifestFixture(); q.f.advanceArtifact('d'.repeat(40));
  const result = await q.service.execute('update', [q.f.entry.moduleId]);
  assert.equal(result.results[0].status, 'current');
  assert.equal(result.rows[0].status, 'current');
  assert.equal(result.rows[0].matchesCandidate, true);
  assert.deepEqual(q.calls, { downloads: 0, writes: 0 });
});
test('artifact identity conflict: historical provider has no manifest identity but remains removable', async () => {
  const { verifyPublication } = await import('./core.mjs');
  const q = await sameBuildManifestFixture(); const old = q.getInstalled(); const p = old.publication;
  old.publication = { provider: 'module-release', sourceSha: p.sourceSha, releaseId: 42,
    publishedAt: p.publishedAt, runId: p.runId, attempt: p.attempt, buildStartedAt: p.buildStartedAt };
  verifyPublication(old.publication); q.setInstalled(old);
  const row = (await q.service.refresh()).rows[0];
  assert.equal(row.status, 'unknown'); assert.equal(row.matchesCandidate, false);
  assert.equal(row.canRequestUpdate, false); assert.equal(row.canUninstall, true);
  assert.match(row.reason, /cannot be verified/i);
  await assert.rejects(q.service.execute('update', [q.f.entry.moduleId]), /ARTIFACT_IDENTITY_UNVERIFIED/);
  assert.deepEqual(q.calls, { downloads: 0, writes: 0 });
});
test('artifact identity conflict: non-string installed fingerprint cannot establish equality', async () => {
  const q = await sameBuildManifestFixture(); const old = q.getInstalled();
  old.publication.manifestSha256 = [old.publication.manifestSha256]; q.setInstalled(old);
  const row = (await q.service.refresh()).rows[0];
  assert.equal(row.status, 'unknown'); assert.equal(row.matchesCandidate, false);
  await assert.rejects(q.service.execute('update', [q.f.entry.moduleId]), /ARTIFACT_IDENTITY_UNVERIFIED/);
});
test('artifact identity conflict: row uses the current audited record after discovery', async () => {
  const q = await sameBuildManifestFixture(); await q.service.refresh([], { full: true });
  await q.service.refresh([q.f.entry.moduleId], { full: true });
  const old = q.getInstalled(); old.publication.manifestSha256 = 'f'.repeat(64); q.setInstalled(old);
  const row = q.service.rows()[0];
  assert.equal(row.status, 'unknown'); assert.equal(row.matchesCandidate, false);
});
test('artifact identity conflict: update checks its approved snapshot after a conflicting refresh audit', async () => {
  const { detectArtifactModule } = await import('./artifact-source.mjs');
  const q = await sameBuildManifestFixture(); changeSameBuildManifest(q.f);
  const candidate = (await detectArtifactModule({ entry: q.f.entry, request: q.f.request, nowMs: q.f.nowMs })).latestPublished;
  const different = q.getInstalled(); const matching = structuredClone(different);
  matching.publication.artifactCommit = candidate.artifactCommit; matching.publication.manifestSha256 = candidate.manifestSha256;
  let audits = 0; q.installer.audit = () => ({ installed: structuredClone(++audits === 2 ? matching : different) });
  await assert.rejects(q.service.execute('update', [q.f.entry.moduleId]), /ARTIFACT_IDENTITY_UNVERIFIED/);
  assert.deepEqual(q.calls, { downloads: 0, writes: 0 });
});

test('artifact identity conflict: existing non-artifact source can still show the same build as current', async () => {
  const q = await sameBuildManifestFixture(); const old = q.getInstalled(); const p = old.publication;
  old.publication = { provider: 'module-release', sourceSha: p.sourceSha, releaseId: 42,
    publishedAt: p.publishedAt, runId: p.runId, attempt: p.attempt, buildStartedAt: p.buildStartedAt };
  q.setInstalled(old); q.service.provider = 'module-release';
  q.service.sources.set(q.f.entry.moduleId, { status: 'candidate', developmentConfirmed: true, checkedAtMs: q.f.nowMs,
    latestPublished: { status: 'candidate', buildId: old.buildId }, reason: 'Published metadata checked.' });
  const row = q.service.rows()[0]; assert.equal(row.status, 'current'); assert.equal(row.matchesCandidate, true);
});
test('artifact identity conflict: legacy providers retain verified forward updates for a different build', async () => {
  const { assessUpdateOrder } = await import('./update-order.mjs');
  const { verifyPublication } = await import('./core.mjs');
  for (const provider of ['module-release', 'modpack-bundle']) {
    const publication = { provider, sourceSha: 'a'.repeat(40), releaseId: 42, runId: 100, attempt: 1,
      publishedAt: '2026-11-03T00:00:10Z', buildStartedAt: '2026-11-03T00:00:00Z' };
    verifyPublication(publication);
    const installed = { moduleId: 'example', buildId: 'Example-dev-100-1', publication };
    const candidate = { buildId: 'Example-dev-101-1', sourceSha: publication.sourceSha, branch: 'Example',
      runId: 101, attempt: 1, buildStartedAt: '2026-11-03T01:00:00Z', publishedAt: '2026-11-03T01:00:10Z' };
    const result = await assessUpdateOrder(installed, candidate, 'Owner/Repo', async () => { throw new Error('UNEXPECTED_NETWORK'); });
    assert.equal(result.allowed, true);
  }
});

test('artifact identity conflict: distinct reasons and operation errors have translations in all supported languages', async () => {
  const { localizeReason, localizeReply, translationCatalog } = await import('./localization.mjs');
  const changed = await sameBuildManifestFixture(); changeSameBuildManifest(changed.f);
  const changedRow = (await changed.service.refresh()).rows[0];
  const historical = await sameBuildManifestFixture(); const record = historical.getInstalled();
  historical.setInstalled({ moduleId: record.moduleId, buildId: record.buildId });
  const unknownRow = (await historical.service.refresh()).rows[0];
  assert.match(changedRow.reason, /manifest differs/i); assert.match(unknownRow.reason, /cannot be verified/i);
  for (const language of ['en', 'zh-Hant', 'zh-Hans', 'ja']) {
    for (const row of [changedRow, unknownRow]) {
      const localized = localizeReason(language, row.reason);
      assert.equal(localized.diagnostic, ''); assert.equal(localized.text, translationCatalog.languages[language][row.reason]);
      assert.ok(localized.text);
    }
    const reply = localizeReply({ error: 'ARTIFACT_IDENTITY_UNVERIFIED' }, { preferences: { language } });
    assert.equal(reply.errorText, translationCatalog.languages[language][unknownRow.reason]);
  }
});
