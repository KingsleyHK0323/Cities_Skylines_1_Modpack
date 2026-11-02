# Change Log

## 2026-11-03

- Warn when planning a first development installation alongside an existing
  stable copy, even before the development directory exists. Keep this warning
  non-blocking and leave subscriptions, enablement and loading settings unchanged.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

## 2026-11-02

- Add pure single/batch install and quarantine-uninstall preflight planning with
  source/package binding, complete ownership-tree checks and structured blockers.
- Check explicit conflict/dependency rules, preserve required modules, order
  planned dependencies/removals and reject duplicate targets or cycles.
- Keep execution disabled even when synthetic model checks pass; add batch-to-plan
  integration and unsafe/changed/incomplete snapshot regression tests.

- Integrate per-module pinned assessment, injected transfer and complete package
  verification into a bounded, memory-only batch preparation API.
- Preflight all sources before transfer, snapshot contracts, stop on any missing
  asset/failure and expose no partial batch; keep installation explicitly blocked.
- Add preview selection-plan review with per-module blockers, plus synthetic
  assessment/selection integration, package, failure, snapshot and deadline tests.

- Add an injected, memory-only asset transfer core gated by pinned module catalog
  validation, exact HTTPS source/redirect rules and a total request deadline.
- Verify bounded streamed bytes, exact length and SHA-256 before exposing a result;
  abort failures and scrub transport diagnostics without creating partial files.
- Test synthetic success, stalls, truncation, corrupt/oversized data, redirects,
  interrupted streams and reused chunk buffers. Keep real transport/UI integration
  and installation disabled; no real download or dependency was introduced.

- Add a directly openable offline interface preview with five explicit synthetic
  module states, search/filter controls, individual and visible bulk selection,
  selection summary, light/dark themes and responsive styles.
- Keep install/update/uninstall disabled and expose only blocked operation plans;
  show stable/development coexistence as a warning without changing game state.
- Add preview selection and DOM-event harness coverage in the existing test entry
  point. Real browser, mobile and keyboard acceptance remains pending because the
  browser tool rejected local file URLs; no workaround was attempted.

- Reject repository/tag dot segments and verify parsed catalog asset URLs retain
  the expected origin and exact path; cover normal dotted names and encoded tags.

- Add offline per-module pinned catalog validation and conservative opaque build
  status comparison, with explicit unavailable reasons and synthetic fixtures.
- Reject drafts, unpublished or mismatched identities, incomplete runtime sets,
  unsafe paths, oversized assets and unexpected release download URLs.
- Keep live catalog authentication, freshness, binary validation and downloads
  unimplemented pending the publisher interface; retain independent module state.

- Report partial lock and journal creation after write/sync failure as requiring
  manual inspection, preserving original errors and potentially incomplete files.
- Add four metadata creation failure injection tests without unsafe cleanup.

- Preserve original transaction and recovery errors when lock cleanup fails.
- Report explicit commit, lock-release and journal recovery status without
  modifying original error objects or retrying unsafe filesystem writes.
- Add injected post-commit, pre-commit, journal, lock-read and root-check failures.

- Add pure package integrity, Windows path and ownership preflight planning.
- Add per-module opaque build comparison and warning-only coexistence handling.
- Add synthetic negative contract tests; no actual installation is implemented.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

## 2026-11-02 — Independent cloud checks

- Configure branch-specific Windows cloud checks for the isolated Node.js tests; no deployment or release is triggered.
- The workflow has not yet run in GitHub Actions. Signature: OpenAI-Codex.

## 2026-11-02 — Isolated filesystem execution

- Add actual install, update and quarantine-uninstall in restricted temporary
  directories, with staged hashes, ownership state, exclusive operation locks,
  pre-commit rollback and retained recovery journals/backups.
- Preserve modified, unknown, extra and hard-linked user files by refusing
  mutations. Recheck roots, links and directory/file identity at operation stages.
- Reject reserved module IDs, Windows-invalid names and parent/child file clashes.
- Expand regression tests for real filesystem round trips, injected failures,
  observed junction swaps, retained user edits and non-isolated-root rejection.
- Assess live GitHub metadata without asserting an unverified latest build.
- Add the unmodified official PolyForm Noncommercial 1.0.0 license.
- Keep production game paths, network downloads, graphical UI, bulk execution,
  assembly inspection and native race-resistant traversal unimplemented.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Validation: 28 Node.js tests passed on Windows with synthetic temporary data, including actual directory renames, junctions and hard links. The restricted execution environment initially denied those operations; the authorized isolated test rerun succeeded with the required filesystem permissions. Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager.
