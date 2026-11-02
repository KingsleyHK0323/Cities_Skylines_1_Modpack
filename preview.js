// Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
'use strict';
(() => {
  const { createPreviewModel } = globalThis.DevManagerPreview;
  const model = createPreviewModel();
  const byId = id => document.getElementById(id);
  const labels = { unavailable: 'Unknown source', current: 'Same build', 'different-build': 'Different build', 'not-installed': 'Not installed' };
  const element = (tag, className, text) => {
    const node = document.createElement(tag); if (className) node.className = className;
    if (text !== undefined) node.textContent = text; return node;
  };
  const search = byId('search'); const filter = byId('status-filter'); const selectVisible = byId('select-visible');
  function renderSelection() {
    const selected = model.selected(); const ids = new Set(selected.map(item => item.id));
    byId('selection-count').textContent = String(selected.length);
    byId('selection-summary').textContent = selected.length ? `${selected.length} sample module${selected.length === 1 ? '' : 's'} selected. No action will run.` : 'No modules selected.';
    byId('selected-list').replaceChildren(...selected.map(item => element('li', '', item.name)));
    for (const checkbox of byId('modules').querySelectorAll('input[type="checkbox"]')) checkbox.checked = ids.has(checkbox.value);
    const visible = model.visible(search.value, filter.value); const count = visible.filter(item => ids.has(item.id)).length;
    selectVisible.checked = visible.length > 0 && count === visible.length;
    selectVisible.indeterminate = count > 0 && count < visible.length;
    selectVisible.disabled = visible.length === 0;
    byId('clear').disabled = selected.length === 0;
    byId('review').disabled = selected.length === 0;
    byId('review-panel').hidden = true;
    byId('review').setAttribute('aria-expanded', 'false');
  }
  function renderModules() {
    const visible = model.visible(search.value, filter.value);
    byId('visible-count').textContent = `${visible.length} of ${model.visible().length}`;
    byId('empty').hidden = visible.length > 0;
    byId('modules').replaceChildren(...visible.map(item => {
      const article = element('article', 'module');
      const top = element('div', 'module-top'); const label = element('label', 'module-label');
      const input = element('input'); input.type = 'checkbox'; input.value = item.id;
      input.setAttribute('aria-label', `Select ${item.name}`);
      input.addEventListener('change', () => { model.select(item.id, input.checked); renderSelection(); });
      const identity = element('span', 'module-identity'); identity.append(element('span', 'category', item.category), element('span', 'module-name', item.name));
      label.append(input, identity); top.append(label, element('span', `state ${item.status}`, labels[item.status])); article.append(top);
      const builds = element('dl', 'builds');
      for (const [labelText, value] of [['Sample local', item.installedBuild ?? 'Unknown / none'], ['Sample source', item.catalogBuild ?? 'Unavailable']]) {
        const group = element('div'); group.append(element('dt', '', labelText), element('dd', '', value)); builds.append(group);
      }
      article.append(builds, element('p', 'reason', item.reason));
      if (item.coexistenceWarning) article.append(element('p', 'coexistence', item.coexistenceWarning));
      return article;
    }));
    renderSelection();
  }
  search.addEventListener('input', renderModules); filter.addEventListener('change', renderModules);
  selectVisible.addEventListener('change', () => { model.selectVisible(search.value, filter.value, selectVisible.checked); renderSelection(); });
  byId('clear').addEventListener('click', () => { model.clear(); renderSelection(); });
  byId('review').addEventListener('click', () => {
    const review = model.reviewSelection();
    byId('review-stages').replaceChildren(...review.stages.map(stage => element('li', '', stage)));
    byId('review-items').replaceChildren(...review.items.map(item => {
      const li = element('li'); li.append(element('strong', '', item.name), element('p', '', item.blockers.join(' ')));
      if (item.coexistenceWarning) li.append(element('p', '', item.coexistenceWarning));
      return li;
    }));
    byId('review-panel').hidden = false;
    byId('review').setAttribute('aria-expanded', 'true');
  });
  let dark = globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  function applyTheme() {
    document.documentElement.dataset.theme = dark ? 'dark' : 'light';
    byId('theme').setAttribute('aria-pressed', String(dark));
  }
  byId('theme').addEventListener('click', () => { dark = !dark; applyTheme(); });
  applyTheme(); renderModules();
})();
