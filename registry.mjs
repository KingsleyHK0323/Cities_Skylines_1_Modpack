// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Explicit mappings reviewed from project branches on 2026-11-03; no inferred DLLs.
const repository = 'KingsleyHK0323/Cities_Skylines_1_Modpack';
export const defaultSourceProvider='artifact-branch';
const configured = [
  { moduleId: 'ai-improve', name: 'AI Improve', branch: 'AI_Improve', assembly: 'AIImprove',
    provenanceAssetName:'build-info.json',
    workflowPath: '.github/workflows/cloud-compile-test.yml', runtimeAssetNames: ['AIImprove.dll', 'CitiesHarmony.API.dll','LICENSE','CitiesHarmony-LICENSE.txt','THIRD-PARTY-NOTICES.md','README.md'],
    workflowBlobSha: 'b3ba1e090fee577ff96c4ca4d0e2249e558c6eb6',
    bundleRuntimePaths: { 'AIImprove.dll': 'AI_Improve/AIImprove.dll', 'CitiesHarmony.API.dll': 'AI_Improve/CitiesHarmony.API.dll',
      'LICENSE':'AI_Improve/LICENSE','CitiesHarmony-LICENSE.txt':'AI_Improve/CitiesHarmony-LICENSE.txt','THIRD-PARTY-NOTICES.md':'AI_Improve/THIRD-PARTY-NOTICES.md','README.md':'AI_Improve/README.md' },
    bundleProvenancePath: 'AI_Improve/build-info.json' },
  { moduleId: 'public-transport-planning', name: 'Automated Public Transport Planning', branch: 'Automated_Public_Transport_Planning',
    provenanceAssetName:'build-info.txt',
    assembly: 'AutomatedPublicTransportPlanning', workflowPath: '.github/workflows/cloud-compile-test.yml',
    runtimeAssetNames: ['AutomatedPublicTransportPlanning.dll','LICENSE','README.md'], workflowBlobSha: '13b4c213567771ad486e80f8d25578162189d75b',
    bundleRuntimePaths: { 'AutomatedPublicTransportPlanning.dll': 'AutomatedPublicTransportPlanning.dll',
      'LICENSE':'Documentation/Automated_Public_Transport_Planning/LICENSE','README.md':'Documentation/Automated_Public_Transport_Planning/README.md' },
    bundleProvenancePath: 'Documentation/Automated_Public_Transport_Planning/build-info.txt' },
  { moduleId: 'public-transport-vehicles', name: 'Automated Public Transport Vehicle Management', branch: 'Automated_Public_Transport_Vehicle_Management',
    assembly: 'AutomatedPublicTransportVehicleManagement', workflowPath: '.github/workflows/cloud-build.yml',
    runtimeAssetNames: ['AutomatedPublicTransportVehicleManagement.dll'], unavailableReason: 'Artifact-only source; DevTool development build evidence is unconfirmed.',
    bundleRuntimePaths:{'AutomatedPublicTransportVehicleManagement.dll':'AutomatedPublicTransportVehicleManagement.dll'},
    bundleProvenancePath:'Documentation/Automated_Public_Transport_Vehicle_Management/build-provenance.json' },
  { moduleId: 'realistic-city-time', name: 'Realistic City Time', branch: 'Realistic_City_Time', assembly: 'RealisticCityTime',
    workflowPath: '.github/workflows/game-dll.yml', runtimeAssetNames: ['RealisticCityTime.dll'], unavailableReason: 'Observation artifact only; no configured public Development release.' },
  { moduleId: 'full-map-unlock', name: 'Full Map Unlock And Expansion', branch: 'Full_Map_Unlock_And_Expansion', assembly: 'FullMapUnlockAndExpansion',
    workflowPath: '.github/workflows/game-dll.yml', runtimeAssetNames: ['FullMapUnlockAndExpansion.dll'], unavailableReason: 'Observation artifact only; no configured public Development release.' }
];
const pending = ['Intercity_Resource_Sharing','Realistic_Border_Control','Realistic_City_First_Response',
  'Realistic_Import_Export_Control','Realistic_Pedestrian_Traffic_Control','Realistic_Railway_Traffic_Control',
  'Realistic_Urban_Population','Realistic_Vehicle_Traffic_Control','Wireless_Utilities_And_Global_Coverage'].map(branch =>
  ({ moduleId: branch.toLowerCase().replaceAll('_', '-'), branch, name: branch.replaceAll('_', ' '),
    unavailableReason: 'Publisher, runtime dependencies and Development source mapping are not configured.' }));
export const moduleRegistry = Object.freeze([...configured, ...pending,
  { moduleId: 'ai-gaming', branch: 'AI_Gaming', name: 'AI Gaming', engineeringPaused: true,
    unavailableReason: 'No verified Development source mapping is configured.' }
].map(item => Object.freeze({ ...item, repository, publisherConfigured:['ai-improve','public-transport-planning'].includes(item.moduleId),releaseTagPrefix: `${item.branch}-dev-`,
  ...(item.runtimeAssetNames ? { runtimeAssetNames: Object.freeze(item.runtimeAssetNames) } : {}) })));
export const sourceProviders = Object.freeze([
  { id:'artifact-branch',name:'Independent Development build branches',enabled:true }
]);
