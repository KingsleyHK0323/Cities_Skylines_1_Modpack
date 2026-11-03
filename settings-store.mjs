// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// One atomic settings transaction shared by preferences and destinations.
import fs from 'node:fs';
import path from 'node:path';
import {randomUUID} from 'node:crypto';
const defaults=()=>({schema:1,mode:'unconfigured',sandbox:null,modsRoot:null});
const validate=value=>{
  if(!value||value.schema!==1||!['sandbox','game-mods','unconfigured'].includes(value.mode)||value.sandbox!==null&&typeof value.sandbox!=='string')throw new Error('LOCAL_STATE_INVALID');return value;
};
const regular=(file,io)=>{const stat=io.lstatSync(file);if(!stat.isFile()||stat.isSymbolicLink()||stat.nlink!==1)throw new Error('LOCAL_STATE_UNSAFE');return stat;};
export class SettingsStore {
  constructor(file,{fault=()=>{},io=fs}={}){this.file=file;this.fault=fault;this.io=io;this.lastLoadResult=null;}
  read(){
    const io=this.io;const initial=regular(this.file,io);if(initial.size>8192)throw new Error('LOCAL_STATE_OVERSIZED');
    const fd=io.openSync(this.file,'r');let bytes;
    try{
      const opened=io.fstatSync(fd);if(!opened.isFile()||opened.nlink!==1||opened.dev!==initial.dev||opened.ino!==initial.ino)throw new Error('LOCAL_STATE_CHANGED');
      if(opened.size>8192)throw new Error('LOCAL_STATE_OVERSIZED');
      const buffer=Buffer.alloc(8193);let count=0;let received;
      while(count<buffer.length&&(received=io.readSync(fd,buffer,count,buffer.length-count,count))>0)count+=received;
      const final=io.fstatSync(fd);if(count>8192||final.size>8192)throw new Error('LOCAL_STATE_OVERSIZED');
      if(final.size!==opened.size||count!==opened.size)throw new Error('LOCAL_STATE_CHANGED');bytes=buffer.subarray(0,count);
    }finally{io.closeSync(fd);}
    return {state:validate(JSON.parse(new TextDecoder('utf-8',{fatal:true}).decode(bytes))),bytes};
  }
  load(){this.lastLoadResult=this.io.existsSync(this.file)?{state:this.read().state}:this.update(current=>current);return this.lastLoadResult.state;}
  update(transform){
    const io=this.io;
    const lock=`${this.file}.lock`;let lockFd;
    try{lockFd=io.openSync(lock,'wx',0o600);}catch(error){if(error.code==='EEXIST')throw new Error('LOCAL_STATE_BUSY');throw error;}
    let identity;let committed=false;let updated;let failure;let warning;let cleanupFailure;
    try{
      identity=io.fstatSync(lockFd);
      const old=io.existsSync(this.file)?this.read():null;
      updated=validate(transform(old?old.state:defaults()));
      const bytes=Buffer.from(JSON.stringify(updated),'utf8');if(bytes.length>8192)throw new Error('LOCAL_STATE_OVERSIZED');
      if(old){
        const backup=path.join(path.dirname(this.file),`local-state-backup-${randomUUID()}.json`);
        const backupFd=io.openSync(backup,'wx',0o600);try{io.writeFileSync(backupFd,old.bytes);io.fsyncSync(backupFd);}finally{io.closeSync(backupFd);}
      }
      const stage=path.join(path.dirname(this.file),`local-state-stage-${randomUUID()}.json`);
      const stageFd=io.openSync(stage,'wx',0o600);
      try{
        const split=Math.max(1,Math.floor(bytes.length/2));io.writeFileSync(stageFd,bytes.subarray(0,split));this.fault('after-partial-stage');io.writeFileSync(stageFd,bytes.subarray(split));io.fsyncSync(stageFd);
      }finally{io.closeSync(stageFd);}
      this.fault('before-rename');
      if(old){if(!this.read().bytes.equals(old.bytes))throw new Error('LOCAL_STATE_CHANGED');}
      else if(io.existsSync(this.file))throw new Error('LOCAL_STATE_CHANGED');
      io.renameSync(stage,this.file);committed=true;this.fault('after-rename');
    }catch(error){failure=error;}
    finally{
      try{
        io.closeSync(lockFd);if(!identity)throw new Error('LOCAL_STATE_LOCK_IDENTITY_UNKNOWN');
        const current=regular(lock,io);if(current.dev!==identity.dev||current.ino!==identity.ino)throw new Error('LOCAL_STATE_LOCK_CHANGED');
        this.fault('before-lock-release');io.unlinkSync(lock);
      }catch(error){cleanupFailure=error;if(committed)warning='LOCAL_STATE_COMMITTED_REVIEW';}
    }
    if(committed)return {state:updated,warning:warning??(failure?'LOCAL_STATE_COMMITTED_REVIEW':undefined),lockMayRemain:!!cleanupFailure};
    if(cleanupFailure){const error=new AggregateError([failure,cleanupFailure].filter(Boolean),'LOCAL_STATE_RECOVERY_REQUIRED',{cause:failure});error.lockMayRemain=true;error.recoveryRequired=true;throw error;}
    throw failure??new Error('LOCAL_STATE_SAVE_FAILED');
  }
}
