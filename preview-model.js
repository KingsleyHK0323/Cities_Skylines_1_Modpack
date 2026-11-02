// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
// Synthetic display data only. This model has no filesystem or network adapter.
'use strict';
(() => {
  const sampleModules = Object.freeze([
    { id: 'ai-improve', name: 'AI Improve', category: 'Simulation', status: 'different-build',
      installedBuild: 'fixture-a', catalogBuild: 'fixture-b',
      reason: 'The two sample build identities differ. Their order is unknown.' },
    { id: 'realistic-city-time', name: 'Realistic City Time', category: 'Simulation', status: 'unavailable',
      installedBuild: null, catalogBuild: null,
      reason: 'No verified per-module source or installation record is connected.' },
    { id: 'public-transport-planning', name: 'Automated Public Transport Planning', category: 'Public transport',
      status: 'current', installedBuild: 'fixture-c', catalogBuild: 'fixture-c',
      reason: 'The sample build identities match. No local game files were read.' },
    { id: 'public-transport-vehicles', name: 'Automated Public Transport Vehicle Management', category: 'Public transport',
      status: 'different-build', installedBuild: 'fixture-d', catalogBuild: 'fixture-e',
      reason: 'The two sample build identities differ. Their order is unknown.',
      coexistenceWarning: 'Sample scenario: stable and development copies coexist. Warning only; no subscriptions or loading settings change.' },
    { id: 'full-map-unlock', name: 'Full Map Unlock And Expansion', category: 'Map', status: 'not-installed',
      installedBuild: null, catalogBuild: 'fixture-f',
      reason: 'Sample scenario only: no owned installation is recorded. The real installation is unknown.' }
  ].map(item => Object.freeze({ ...item, synthetic: true })));

  function createPreviewModel(input = sampleModules) {
    const items = structuredClone(input); const selectedIds = new Set();
    if (!Array.isArray(items) || items.some(item => !item || typeof item.id !== 'string' || !item.id ||
      typeof item.name !== 'string' || !['unavailable', 'current', 'different-build', 'not-installed'].includes(item.status)) ||
      new Set(items.map(item => item.id)).size !== items.length) throw new Error('Invalid preview modules');
    const visible = (query = '', status = 'all') => structuredClone(items.filter(item =>
      item.name.toLowerCase().includes(String(query).trim().toLowerCase()) && (status === 'all' || item.status === status)));
    const select = (id, checked) => {
      if (!items.some(item => item.id === id) || typeof checked !== 'boolean') throw new Error('Invalid selection');
      if (checked) selectedIds.add(id); else selectedIds.delete(id);
    };
    return Object.freeze({ visible, select,
      selected: () => structuredClone(items.filter(item => selectedIds.has(item.id))),
      selectVisible: (query, status, checked) => { for (const item of visible(query, status)) select(item.id, checked); },
      clear: () => selectedIds.clear(),
      reviewSelection: () => ({ mode: 'preview-only', installAllowed: false,
        stages: ['Validate each module source', 'Collect every required runtime file', 'Verify complete packages',
          'Review ownership, conflicts and dependencies', 'Installation unavailable'],
        items: items.filter(item => selectedIds.has(item.id)).map(item => ({ id: item.id, name: item.name,
          status: item.status, blockers: [item.synthetic ? 'Synthetic example; no authenticated source is connected.' :
            'This preview is not connected to a real preparation or installation adapter.',
          ...(item.status === 'unavailable' ? [item.reason] : [])],
          coexistenceWarning: item.coexistenceWarning ?? null })) }),
      operationPlan: operation => {
        if (!['install', 'update', 'uninstall'].includes(operation)) throw new Error('Unknown preview operation');
        return { operation, allowed: false, reason: 'Preview only. Real installation and download adapters are not connected.',
          items: items.filter(item => selectedIds.has(item.id)).map(item => ({ id: item.id, allowed: false,
            reason: item.status === 'unavailable' ? item.reason : 'Preview only. No real operation is available.' })) };
      }
    });
  }
  const api = Object.freeze({ sampleModules, createPreviewModel });
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else globalThis.DevManagerPreview = api;
})();
