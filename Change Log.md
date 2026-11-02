# Change Log

## 2026-11-02

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
