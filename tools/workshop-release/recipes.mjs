// Signature: OpenAI-Codex_workshop_implementation
// Updated by: OpenAI-Codex.
import {readFileSync} from 'node:fs';
const allowedReferences = new Set(['mscorlib.dll','System.dll','System.Core.dll','System.Xml.dll',
  'UnityEngine.dll','ICities.dll','ColossalManaged.dll','Assembly-CSharp.dll']);
const allowedOptions = new Set(['/warnaserror+','/checked+']);
const fail = message => { throw new Error(message); };

export function validateRecipes(data) {
  if (!data || data.schema !== 1 || data.signature !== 'OpenAI-Codex_workshop_implementation' ||
      !Array.isArray(data.recipes) || data.recipes.length === 0 || data.recipes.length > 15)
    fail('Reviewed recipe registry is invalid.');
  const modules = new Set();
  for (const recipe of data.recipes) {
    if (!recipe || typeof recipe.module !== 'string' || !/^[A-Za-z0-9_]{1,100}$/.test(recipe.module) ||
        modules.has(recipe.module) || typeof recipe.sourceSha !== 'string' || !/^[0-9a-f]{40}$/.test(recipe.sourceSha) ||
        typeof recipe.assembly !== 'string' || !/^[A-Za-z][A-Za-z0-9]{1,100}$/.test(recipe.assembly) ||
        !['3','5','7.3'].includes(recipe.languageVersion) || !Array.isArray(recipe.sources) || recipe.sources.length < 1 ||
        recipe.sources.length > 100 || !Array.isArray(recipe.references) || recipe.references.length < 1 ||
        recipe.references.length > allowedReferences.size ||
        (recipe.compilerOptions !== undefined && (!Array.isArray(recipe.compilerOptions) || recipe.compilerOptions.some(option => !allowedOptions.has(option)))))
      fail('Reviewed compiler recipe is invalid.');
    modules.add(recipe.module);
    if ((recipe.module === 'Automated_Public_Transport_Planning' && recipe.builder !== 'msbuild') ||
        (recipe.module !== 'Automated_Public_Transport_Planning' && recipe.builder !== undefined))
      fail('A compiler recipe has an unexpected builder.');
    if (new Set(recipe.sources).size !== recipe.sources.length ||
        recipe.sources.some(path => typeof path !== 'string' || path.includes('..') ||
          !/^(?:Properties\/AssemblyInfo\.cs|(?:Source|Game)\/[A-Za-z0-9_./-]+\.cs)$/.test(path)) ||
        new Set(recipe.references).size !== recipe.references.length ||
        recipe.references.some(name => !allowedReferences.has(name)))
      fail('Reviewed compiler inputs contain an unsafe, duplicate or unknown entry.');
  }
  return true;
}
const data = JSON.parse(readFileSync(new URL('./reviewed-recipes.json', import.meta.url), 'utf8'));
validateRecipes(data);
export const recipes = Object.freeze(data.recipes.map(recipe => Object.freeze({...recipe,
  sources:Object.freeze([...recipe.sources]), references:Object.freeze([...recipe.references]),
  compilerOptions:Object.freeze([...(recipe.compilerOptions || [])])})));
