// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import fs from 'node:fs';
import {presentationPreferences} from './preferences.mjs';
export const translationCatalog=JSON.parse(fs.readFileSync(new URL('./translations.json',import.meta.url),'utf8'));
export function translate(language,key,...args){
  key=key??'';
  const template=translationCatalog.languages[language]?.[key]??translationCatalog.languages.en[key]??key;
  return template.replace(/\{([0-9]+)\}/g,(_,index)=>String(args[Number(index)]??`{${index}}`));
}
export function localizeReason(language,reason=''){
  if(!reason)return {text:'',diagnostic:''};
  if(Object.hasOwn(translationCatalog.languages.en,reason))return {text:translate(language,reason),diagnostic:''};
  let key='Source verification is unavailable. See technical details.';
  if(reason.startsWith('Same source/run:'))key='Only a later attempt of the same build can be an update.';
  else if(/queued|request budget|rate limit/i.test(reason))key='Verification is queued fairly. Wait for request allowance or the next check.';
  else if(/stale|expired/i.test(reason))key='Observation expired. Check GitHub again before installing.';
  else if(/unknown.*local files|changed.*local files|files require inspection|unsafe local files/i.test(reason))key='Unknown or changed local files are preserved. Inspect them before retrying.';
  else if(/not confirmed newer|unknown, older|older.*build|different.*not.*new|has not advanced|not auto-selected|not established/i.test(reason))key='A different build is not confirmed newer. No automatic update is offered.';
  else if(['A later publication with a later build of the same source is available.','A later publication with verified forward source ancestry is available.'].includes(reason))key='A forward update is verified. Installation will verify it again.';
  else if(['A pinned artifact manifest exists. Successful source run and update order still require verification before installation.','Pinned artifact manifest and successful source run match. File hashes and assembly identities are checked before installation.','Bundle checksums and module provenance match. Source run and actual assembly bytes are verified before installation.','Published metadata checked; actual DLL bytes and assembly identity are checked before changes.','A published candidate exists. Branch, build, assets and actual assembly bytes must be verified before installation.'].includes(reason))key='Candidate found. Full source and file verification is required before installation.';
  return {text:translate(language,key),diagnostic:key==='Source verification is unavailable. See technical details.'?reason:''};
}
const errors={
  CLOSE_GAME_BEFORE_CHANGING_FILES:'Close the game before changing module files.',SELECT_A_MODS_DESTINATION_FIRST:'Choose a Mods destination first.',
  SOURCE_UNAVAILABLE:'The selected source cannot be verified.',INSTALLATION_STATE_CHANGED:'The installation state changed. Check again.',
  ASSEMBLY_IDENTITY_UNVERIFIED:'The package or assembly identity could not be verified.',ARTIFACT_FILE_UNVERIFIED:'The package or assembly identity could not be verified.',
  API_BUDGET_QUEUED_TRY_A_SMALLER_BATCH_AFTER_RETRY:'Request budget exhausted. Wait for retry or select fewer modules.',
  INVALID_PRESENTATION_PREFERENCES:'Settings could not be saved. Existing data is preserved for inspection.',EXISTING_PREFERENCES_REQUIRE_INSPECTION:'Settings could not be saved. Existing data is preserved for inspection.',
  LOCAL_STATE_UNAVAILABLE:'Settings could not be saved. Existing data is preserved for inspection.',LOCAL_STATE_UNSAFE:'Settings could not be saved. Existing data is preserved for inspection.',
  LOCAL_STATE_INVALID:'Settings could not be saved. Existing data is preserved for inspection.',LOCAL_STATE_OVERSIZED:'Settings exceed the safe size limit. Existing settings were not replaced.',LOCAL_STATE_BUSY:'Another App instance is saving settings. Retry after it finishes.',
  LOCAL_STATE_RECOVERY_REQUIRED:'Settings were not saved. A recovery lock may remain. Inspect it before retrying; no lock was automatically removed.',DESTINATION_CHANGED_EXTERNALLY:'The destination changed in another App. Preferences were not saved. Reconfirm the destination or restart.',
  PREFERENCES_SAVE_FAILED:'Settings could not be saved. Existing data is preserved for inspection.',INVALID_MODS_DIRECTORY:'The selected Mods folder is invalid or unsafe.',
  OPERATION_ALREADY_RUNNING:'Another operation is already running.',INVALID_SELECTION:'The selected operation or modules are unavailable.',UNAVAILABLE_SELECTION:'The selected operation or modules are unavailable.',
  PUBLISHED_BUILD_IS_NOT_CONFIRMED_NEWER:'A different build is not confirmed newer. No automatic update is offered.'
};
export function localizeProgress(language,text){
  const downloading=/^Downloading (.+): (.+)$/.exec(text);if(downloading)return translate(language,'Downloading {0}: {1}',downloading[1],downloading[2]);
  for(const [prefix,key] of [['install: ','Installing {0}'],['update: ','Updating {0}'],['uninstall: ','Removing {0}'],['Checking ','Checking {0}']])
    if(text.startsWith(prefix)&&!text.includes('published'))return translate(language,key,text.slice(prefix.length));
  return translate(language,'Source check in progress');
}
export function localizeReply(message,state){
  const {language}=presentationPreferences(state);const copy={...message};
  if(copy.event==='progress')copy.textLocalized=localizeProgress(language,copy.text);
  if(copy.error)copy.errorText=translate(language,errors[copy.error]??'Operation could not finish. Review details before retrying.');
  if(copy.result){
    copy.result={...copy.result,preferences:presentationPreferences(state)};
    if(copy.result.settingsWarning)copy.result.settingsWarningText=translate(language,copy.result.settingsLockMayRemain?'Settings were saved, but a recovery lock may remain. Inspect it before retrying; no automatic lock removal is performed.':'Settings were saved, but recovery data needs inspection before the next save.');
    if(copy.result.rows)copy.result.rows=copy.result.rows.map(row=>{const reason=localizeReason(language,row.reason);return {...row,reasonText:reason.text,reasonDiagnostic:reason.diagnostic,configurationText:translate(language,row.configuration),developmentText:translate(language,row.development)};});
    if(copy.result.target){const target=copy.result.target;copy.result.target={...target,scopeText:target.mode==='unconfigured'?translate(language,'Choose your existing game Mods folder in Destination to enable file operations. The test workspace is optional.'):target.mode==='sandbox'?translate(language,'Test workspace only. These files are not loaded by the game.'):translate(language,'Only owned module folders are managed. Retained backups: {0}',target.backupPath??'')};}
    // Signature: OpenAI-Codex_dev_manager_development
    if(copy.result.results)copy.result.results=copy.result.results.map(result=>{const reason=localizeReason(language,result.reason);return {...result,statusText:translate(language,({committed:'Completed',current:'Already current','committed-with-warning':'Completed; inspection required',failed:'Failed; files preserved where possible','not-attempted':'Not attempted'})[result.status]??'Operation could not finish. Review details before retrying.'),reasonText:reason.text,reasonDiagnostic:reason.diagnostic};});
  }
  return copy;
}
