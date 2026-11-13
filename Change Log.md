# Change Log

## 2026-11-13

- Apply trusted API rate-limit headers before response bodies finish, preventing
  later queued requests from starting during an already observed cooldown.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Apply received rate-limit headers even when an otherwise successful API response
  has invalid JSON or its body fails; keep rejecting it for source verification.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Preserve HTTP error status and rate-limit headers without consuming an error
  body; successful API responses still require valid JSON.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Report shared source failures as unavailable unless a request limit is known,
  refuse a candidate whose source branch is absent, and avoid reusing cached
  update-order decisions after installation or source-head changes.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Keep matching build IDs unknown when artifact manifest identities differ or
  comparable installation provenance is unavailable; block automatic updates.
  Preserve matching-manifest and historical provider behavior.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Preserve selections hidden by search, show their count and block install, update
  and removal until every selection is visible or cleared. Candidate-selection
  buttons now change visible rows only. Distinguish an observed candidate from an
  unavailable source when the destination or update order is still unconfirmed.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Reject existing blocked game and framework reference identities even if their
  DLL filenames are changed. This retains the existing, non-exhaustive denylist.
- Mark candidate observations stale when their timestamps are missing or invalid,
  or when the system clock moves behind the recorded observation time.
- Report every remaining module as not attempted after a transaction error or
  inspection warning, including a non-Error thrown value.
- Require explicit initial ownership snapshots for transactions and compare the
  complete record under the cooperative lock before staging; bind update-order
  verification to that same snapshot and stop on concurrent changes.
- Recheck source freshness before download and under the transaction lock, and
  expire desktop state and reschedule checks after a backward clock adjustment.
- Recheck freshness after staging before creating a journal; preserve staged data
  and the prior installation when that check fails. Use elapsed time for local
  request limits and server cooldowns, retain longer concurrent cooldowns, and
  interpret numeric retry-after values as seconds.
- Add the recorded `Assembly-CSharp` and `System.Xml` reference identities to
  the existing non-exhaustive blocked set.
- Preserve queued update verification across request windows using bounded,
  verified immutable comparison evidence; recheck full source and local state
  before downloads and distinguish an exhausted allowance from an older build.
- Reject contradictory returned comparison heads, isolate anonymous cache keys,
  rotate old cached evidence, and refuse mixed-repository batches so they must be
  checked separately.
- Reject install/update batches containing paused modules before source checks,
  retain user-initiated removal and localize the not-attempted results.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

## 2026-11-04

- Recheck the game process before each install, update or removal transaction;
  halt the remaining batch when the process is running or its state is unknown,
  preserve previously completed results and localize the unattempted status.
  An external launch can still race an individual transaction.
  Signature: OpenAI-Codex_dev_manager_development

- Update the development-test readiness note with the published beta.1 installer
  and two observed artifact branches, while retaining the historical assessment.
  Signature: OpenAI-Codex_dev_manager_icon

- Compare existing output directories by their Windows native real paths in the
  icon-cache identity test, allowing short and long aliases of the same directory
  while rejecting a different directory. The build implementation is unchanged.
  Signature: OpenAI-Codex_dev_manager_icon

- Distinguish an installed build matching a newly observed candidate from an
  unavailable source, while keeping the candidate explicitly unverified.
  Signature: OpenAI-Codex

## 2026-11-03

- Prepare Windows native and portable builds to embed the same required
  `desktop/DevManager.ico`; the desktop form selects its executable's associated
  icon for the window and taskbar. Preserve ICO bytes across Git checkout.
- Key the developer-shell cache by source, icon, build script and compiler hashes.
  Read-lock the source/icon during selection and compilation; build into a new
  staging EXE and promote only after verifying inputs remained unchanged.
  Missing inputs and failed builds do not replace existing executables. Local
  test executables contain the icon; visual acceptance remains pending.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Pin future Windows x64 packages to official Node.js v24.22.2 instead of v24.19.0;
  verify binary integrity before executing it and use the exact versioned license.
- Preserve new official license/checksum bytes with `-text` Git attributes, keep
  notices on LF checkout and document their normalized digest rule. Retain old
  reference files, archived notice and existing portable outputs without cleanup.
- Test official pins, legal bytes, Git attribute/filter identity, normalized notice
  hash and rejection of old/untrusted runtime before any package output is created.
  Offline older-runtime unit checks do not establish new-runtime package acceptance.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Correct matching-installed-candidate display for the artifact provider without
  treating discovery as full verification. Keep an established publisher mapping
  visible during queued checks while preserving unavailable installation actions.
- Add a test-readiness matrix and publication blockers, distinguishing local
  implementation, isolated/native probes, remote downloads and game acceptance.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Add actual desktop language/theme selectors: English, Traditional Chinese,
  Simplified Chinese and Japanese; follow Windows, light and dark. Persist choices
  through the existing backend while preserving destination and unknown fields.
- Localize controls, dialogs, statuses, progress, target scopes and friendly errors;
  retain unrecognized technical diagnostics. Keep update-order translations neutral
  when one reason is shared by allowed and rejected attempts.
- Add offline UTF-8 resources and explicit compiler codepage, headless native Unicode
  and palette probes, Windows preference event updates/unsubscription and system
  high-contrast colors. Do not claim visual/live-event GUI acceptance.
- Replace truncating settings writes with cross-process locked atomic transactions,
  pre-write size validation and backups. Update in-memory destination only after
  commit; distinguish retained old state from post-commit recovery warnings.
- Test actual partial-stage and pre-rename failures, the 8185-byte legacy boundary,
  competing processes, restart persistence, translation keys/placeholders and flat
  package imports. Keep all new test output in isolated ignored project directories.
- Preserve both errors in a precommit/lock-release failure, protect lock-identity
  cleanup and surface first-start committed warnings. Bound settings reads and
  reject growth. Refuse preference writes after another instance changes the
  destination; send only the selected preference field. Do not claim automatic
  lock recovery or interpret a busy lock as proof of an active App instance.
- Preserve recovery-specific startup errors through a safe whitelist. Show a
  persistent localized recovery/lock notice until a verified clean save, independent
  of transient details or module selection. Keep Windows-controlled chrome outside
  the custom palette acceptance claim.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Expose only independent artifact branches as the player-facing module source;
  reject legacy source selection at the backend boundary. Keep internal historical
  providers/tests and owned-installation management without automatic fallback.
- Show 12 awaiting-publisher mappings and AI Gaming's development pause separately
  from verified source availability. Clarify that Releases are for App Setup.exe
  only; module Releases and bundle ZIPs are not player-facing download options.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Document the selected Windows `Setup.exe` Release format: install into a
  user-selected empty folder, use a separate folder for each new version, and
  reselect the Mods destination when needed. Classify existing portable outputs
  as local development/testing artifacts. No publication, cloud installation or
  in-game acceptance is claimed; Main Session owns installer/Release workflows.
  Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

- Replace per-module automatic artifact verification with shared matching-ref
  snapshots and immutable manifest discovery, preserving full pre-install checks.
- Bound automatic/selected checks and shared hourly API traffic; rotate deferred
  manifest and update work fairly. Show queued verification and retain GitHub retry
  state rather than silently accepting unverified candidates or falling back.
- Cover 15 fully available modules, unchanged checks, all installed updates,
  low-budget rotation, real pooled hourly windows and changing shared snapshots.

- Default desktop selection, backend and service startup to independent Development
  build branches. Keep legacy providers as manual migration choices, with no
  automatic fallback when the default source is missing or invalid. Add a startup
  behavior regression and explicitly select legacy providers in their own tests.

- Add an independent `Development_Build/<source branch>` provider with a pinned
  artifact commit, hash-checked manifest, source ancestry/run/DevTools declaration,
  bounded per-file raw downloads and actual downstream assembly verification.
- Persist artifact provenance without requiring a release ID; keep prior records
  removable and retain existing providers during migration. Require artifact and
  source/build advancement instead of treating different build IDs as newer.
- Document the exact manifest/publisher contract and cover isolated installation,
  forward update, older-build refusal, legacy removal and synthetic managed DLLs.
  No desktop operation, live artifact-branch acceptance or publication is claimed.

- Add a Windows Forms desktop application and versioned portable build recipe
  using the existing compiler and checksum-pinned Node runtime with official notices.
- Register all 15 modules; add anonymous HTTPS GitHub transport, shared discovery,
  hourly idle checks, manual checks, rate-limit cooldown and stale-state handling.
- Connect single/batch install, forward update and quarantine removal to checked
  user-selected Mods destinations. Keep active DLLs in direct module children and
  staging/metadata/backups outside Mods. Preserve unknown or modified files.
- Check managed assembly names without executing module code; validate bounded
  archives in memory, including paths, collisions, links, sizes and all checksums.
- Add the scheduled bundle provider with per-module provenance/run validation and
  independent availability. Require hash-verified build-info for individual releases;
  workflow text alone is not DevTools evidence.
- Resolve changed installed candidates on demand before enabling updates; persist
  source/build/publication records and refuse older, divergent or unknown ordering.
- Cover actual HTTP-to-assembly-to-filesystem install/update/rollback/removal and
  two-module bundle installation with synthetic compiled assemblies and destinations.
  No actual game loading or real published module installation is claimed.
- Add player instructions and the publisher integration contract. This local
  desktop milestone is uncommitted and is not the earlier successful cloud run.

- Add a GitHub-native per-module candidate resolver using explicit module registry
  mappings, complete release/asset pagination, tag peeling and exact workflow-run
  attempt/source/asset checks under ordinary trusted HTTPS API assumptions.
- Separate latest published module build from branch head; keep incomplete,
  ambiguous, failed or changing observations unknown without guessing Repo Latest.
- Retain signed-pointer work as optional, unadopted research rather than a required
  new service. Add synthetic pagination, provenance, tag, race and timeout coverage.

- Add independent per-module source classification with externally pinned Ed25519
  signatures, catalog/source/build binding and bounded nonce/time/sequence checks.
- Bind selected published GitHub releases and runtime assets exactly; keep status
  unknown without trusted freshness or complete evidence. Do not infer latest
  from repository ordering, titles or prerelease flags.
- Correct the README's obsolete first-push/cloud statements using the exact
  committed SHA and successful cloud run; keep new local changes distinguished.

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
