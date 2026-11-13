// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
import { safeRelative } from './core.mjs';
const gameReferences=new Set(['mscorlib.dll','system.dll','system.core.dll','system.xml.dll','unityengine.dll','icities.dll','colossalmanaged.dll','assembly-csharp.dll']);
const gameReferenceAssemblies=new Set([...gameReferences].map(name=>name.slice(0,-4)));
export function allowedInstallationAsset(name, assemblyName) {
  try {
    safeRelative(name);if(name.includes('/'))return false;
    if(/\.dll$/i.test(name)) {safeRelative(assemblyName);return !gameReferences.has(name.toLowerCase()) && !gameReferenceAssemblies.has(assemblyName.toLowerCase()) && !assemblyName.includes('/');}
    return assemblyName===null && (/^LICENSE(?:[.-].+)?$/i.test(name) || /\.(?:json|xml|txt|md|config|notice|license)$/i.test(name));
  }catch{return false;}
}
