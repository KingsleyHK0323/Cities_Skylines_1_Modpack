// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
export const languages=Object.freeze(['en','zh-Hant','zh-Hans','ja']);
export const themes=Object.freeze(['system','light','dark']);
const object=value=>value&&typeof value==='object'&&!Array.isArray(value);
export function presentationPreferences(state){
  const prefs=object(state?.preferences)?state.preferences:{};
  return {language:languages.includes(prefs.language)?prefs.language:'en',theme:themes.includes(prefs.theme)?prefs.theme:'system'};
}
export function mergePresentationPreferences(state,patch){
  if(!object(patch)||Object.keys(patch).some(key=>!['language','theme'].includes(key))||
    Object.hasOwn(patch,'language')&&!languages.includes(patch.language)||Object.hasOwn(patch,'theme')&&!themes.includes(patch.theme))throw new Error('INVALID_PRESENTATION_PREFERENCES');
  if(state.preferences!==undefined&&!object(state.preferences))throw new Error('EXISTING_PREFERENCES_REQUIRE_INSPECTION');
  return {...state,preferences:{...state.preferences,...patch}};
}
