# Cities Skylines 1 Modpack Dev Manager

Dependency-free local prototype: package preflight and an isolated filesystem
executor. Run `node --test core.test.mjs filesystem.test.mjs` using an installed
Node.js runtime. The filesystem tests create synthetic temporary directories.

## Local interface preview

Open [preview.html](preview.html) directly in a browser from the local checkout.
Keep `preview.css`, `preview-model.js` and `preview.js` alongside it. No server,
desktop framework, package installation or account is required. The preview uses
only bundled static resources and blocks network connections through its content
security policy. No game directory, installed build or subscription is inspected.

Five synthetic module records demonstrate unknown sources, matching builds,
different builds and a not-installed scenario. Their display IDs and fixture
build identities are not an official publisher mapping or real installation
records. Search and state filters affect the visible list; individual and
visible bulk selection preserve hidden selections. Clear removes every selection.
All selections reset on reload. Light/dark themes and narrow-screen styles are
included, with native labeled controls, focus outlines and a live selection count.
Use **Review selection plan** to see the source/file/package checks and blocking
reasons for all selected items. This only explains a plan; no stage runs. Changing
the selection or filters hides the old review so it cannot appear current.

Install, update and uninstall controls are disabled with an explanation. The
preview model returns blocked operation plans for every selected item, including
unknown sources. A stable/development coexistence example displays a warning
only. It does not change subscriptions, enablement or loading settings.
Neither catalog validation nor the isolated executor is connected to this UI.

Validation: synthetic selection tests and a minimal DOM event harness exercise
search, state filtering, mixed/visible bulk selection, hidden-selection retention,
clear and theme changes. The harness does not prove browser rendering, actual
keyboard access, responsive layout or accessibility. The browser tool refused
the local `file:` preview under its URL policy; no alternate browser or local
server was used to bypass that restriction. Visual desktop/mobile acceptance
remains pending. Tests are included in the existing `core.test.mjs` command.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

## Verification and execution boundaries

`planning.mjs` adds a pure `planModuleOperations({requests, observation, rules})`
preflight for one or up to 16 selected install/uninstall requests. It never reads
a directory or executes a plan. `modelReady` only reports internal consistency of
supplied snapshots; **`executionAllowed` is always false**, even with no blockers.
Real source authentication, DLL identity checks and live filesystem/process
rechecks remain required. No actual compatibility rules or game observations
are configured by default.

Observation schema `1` requires a snapshot ID, explicit completeness/root safety,
`gameRunning:false` and up to 256 unique module records. Each module declares
directory presence, link safety, ownership (or explicit `null`), a complete file
and directory list, and optional stable-copy presence. Files declare path, SHA-256,
`kind:'file'`, `hardLink:false` and `reparsePoint:false`; directories declare their
path, `kind:'directory'` and `reparsePoint:false`. All ancestor directories must be
listed. These are caller declarations, not proof that a real filesystem is safe
or that the snapshot is still current.

Install requests provide `moduleId`, `operation:'install'`, a complete `package`
and `source:{policy,data}`. Unknown/existing destinations are refused. Every
verified package file, size, build and module must match its pinned catalog.
Uninstall requests provide `moduleId` and `operation:'uninstall'`; a matching
ownership record and exact unchanged observed tree are required, including
directory sets. Modified, missing, extra, ambiguous or linked content blocks the
whole plan. Uninstall steps only describe quarantining an owned directory, never
permanent deletion. Updates/replacement of an existing directory are not enabled
by this new planner.

Rules explicitly declare `complete:true`, `conflicts:[[id,id],...]` and
`requires:[{moduleId,dependencyId},...]` (up to 512 each). Missing rules or missing
referenced observations block planning. The planner checks the proposed final
presence set, protects dependencies on removal and orders requested dependencies
before installation/dependents before removal. Selected dependency cycles and
duplicate targets block the entire plan with no partial steps. Coexistence only
adds a warning; subscriptions and loading settings are not changed.
This also warns when a first development installation is selected alongside an
existing stable copy, even though the development directory does not yet exist.
Synthetic tests integrate prepared batch packages into the planner and confirm
that a subsequently changed source pin invalidates the plan.

`batch.mjs` integrates pinned module assessment, injected asset transfer and
complete package verification without enabling installation. `inspectModuleBatch`
returns safe preview records and a preparation assessment. Every entry explicitly
supplies `moduleId`, `policy`, exact catalog `data` Buffer and `installed` (an owned
record, or explicit `null`); an omitted installation record is unknown and blocks
preparation. Matching source pins remain an external trust/freshness dependency.
An optional `stableInstalled:true` produces a coexistence warning only.

`prepareModuleBatch(entries, {transport, totalTimeoutMs})` snapshots all source
contracts before awaiting the injected adapter. It preflights every selected
module, collects every allowlisted runtime asset sequentially and verifies each
complete package using the existing package contract. Any unavailable module,
duplicate ID or missing dependency blocks the entire batch before transmission.
A later failure returns no partial packages and does not change disk/game state.
Success returns `state:'verified-in-memory'`, complete `packages` and
`installAllowed:false`; it is not an installation or DLL identity acceptance.

Bounds are 1–16 modules and 128 MiB of declared asset bytes for the whole batch.
The total transfer deadline defaults to ten seconds (1 ms–30 seconds configurable),
with each asset using at most five seconds of the remaining budget. Batch redirects
are currently refused; a trusted per-asset redirect policy has not been wired in.
There is no default real transport, authenticated source delivery or UI invocation
of this preparation API. Synthetic integration tests connect assessment records
to the preview selection model; the browser page still uses explicit fixtures.

`downloadCatalogAsset` in `download.mjs` is a transport-injected integrity core,
not a configured network downloader. It first validates exact catalog bytes
against the external policy pin, then selects one exact allowlisted asset path.
It never discovers releases, calls `fetch` or writes files. No UI control calls it.

The injected adapter receives `GET`, `redirect:'manual'`, `credentials:'omit'`
and an `AbortSignal`. Responses must declare their exact requested URL,
`redirected:false`, numeric status, lowercase-header record and async byte stream.
An actual adapter must honor manual redirects and abort/close all outstanding
response bodies; such an adapter has not been implemented or tested here.
Source URLs come only from the validated module catalog. Redirects are denied
unless each complete canonical HTTPS target is explicitly allowed by a separate
trusted caller policy; no CDN or origin is implicitly trusted. At most five
redirects are configurable (default three), and loops are rejected.

Only status 200 and identity encoding are accepted. A supplied Content-Length
must match the pinned size. The stream must finish at exactly that size and match
the pinned SHA-256. Asset limits remain 32 MiB, with at most 65,536 chunks; chunks
are copied before hashing. One total deadline covers headers, redirects and body
(default five seconds, configurable from 1 ms to 30 seconds). Failure aborts the
adapter and returns a bounded error code, never partial bytes or signed URLs.
Verified output is an in-memory buffer only, not installation authorization.
No temporary download file or installable partial artifact is created.

All download tests use injected synthetic responses, including stalled headers
and body, truncation, oversized/corrupt data, redirect rules, adapter buffer reuse
and interrupted streams. A pinned hash alone is not publisher authentication;
live trusted pin delivery, freshness and actual transport behavior remain
unimplemented. Cooperative abort cannot preempt malicious synchronous adapter
code; production resource isolation remains a separate requirement.

This is not yet a desktop application. There is no network downloader, archive
extractor, authenticated latest catalog, assembly inspector or game-process detector.
Tests use synthetic bytes, not real module DLLs. A matching SHA-256 proves that
bytes match the supplied contract; it does not authenticate a publisher or module.

`catalogStatus(installed, policy, data)` now validates a proposed offline,
per-module catalog contract before comparing opaque build IDs. `data` is the
exact JSON Buffer; `policy` supplies an independently trusted digest, repository,
module/branch/assembly/workflow mapping and complete runtime file allowlist.
Unavailable or invalid sources return `unavailable` with a bounded reason and
no package. Valid results are `not-installed`, `current` or `different-build`;
the last result does not mean newer. Draft/unpublished entries, mismatched
identities, missing/extra dependencies, unsafe paths and noncanonical download
URLs are rejected. Neither repository-wide Latest nor `prerelease` selects a build.
Repository components and release tags cannot be `.` or `..`; URL parsing must
preserve the exact expected origin and path without normalization.
This validates declarations and an externally pinned snapshot, not DLL identity,
actual file bytes, workflow provenance or source freshness. Live trust-anchor
delivery, replay protection and publisher integration remain unimplemented.

`IsolatedInstaller` supports actual install/update/uninstall operations only in
directories immediately under the current OS temporary directory named
`cs1-dev-manager-test-<random suffix>`. Other roots are rejected before metadata
creation. It cannot currently be pointed at a real game or Steam installation.

Each module has a dedicated directory. Canonical module IDs are lowercase ASCII
letters, digits, underscores and hyphens; Windows device names are rejected.
Packages contain nonempty buffers with expected SHA-256 values. Limits are 256
files, 32 MiB per file and 64 MiB per package. Parent/child file collisions,
case-insensitive duplicate paths, reserved names and alternate streams are refused.

The executor stages and verifies all files, rechecks the old installation, then
renames directories and atomically replaces its ownership record. An advisory
exclusive lock prevents cooperating operations from running concurrently. Failed
pre-commit operations restore the old active directory where possible. Modified,
extra, missing, hard-linked or linked files block update/uninstall. Uninstall moves
the verified module out of its active location into retained backups; it does not
permanently erase those bytes. Failed activation files are retained as well.

If rollback cannot safely restore a directory, a journal and backup remain and
future mutations refuse to proceed. There is no automatic crash recovery or backup
purge yet. A commit that succeeded but failed final housekeeping reports an error
with `committed=true`; callers must inspect state instead of retrying blindly.
Transaction failures use `AggregateError.errors` to retain original operation,
recovery and lock-release errors without modifying them. `lockReleaseFailed`
identifies failed lock validation or removal; `recoveryRequired` identifies a
retained or potentially partial journal. A release failure does not change the recorded commit outcome.
Failed lock acquisition reports `lockAcquisitionFailed` and `lockMayRemain`;
successful exclusive creation followed by write/sync failure requires manual
inspection (`recoveryRequired=true`). Partial metadata is retained without
additional reads or attempted removal. Existing foreign locks are never removed.
Diagnostic reporting makes no additional writes to a potentially unsafe root.

Path-component, directory identity and opened-file checks reject observed symlink,
junction and hard-link substitution. Tests include injected junction changes before
activation and after backup. These JavaScript path checks do **not** establish an
atomic Windows handle-relative security boundary against a hostile concurrent
process. Other Windows reparse-point types, power-loss durability and production
permissions need native validation before real game installation is enabled.

Stable/development coexistence produces only a warning; no subscription,
enablement or loading change is authorized by detection. Bulk execution, real
operation progress, actual game-root integration and real download/installation acceptance
remain future work. See [source contract assessment](SOURCE-CONTRACT.md).

Code and original documentation use [PolyForm Noncommercial 1.0.0](LICENSE).
Copyright Kingsley. This is source-available software with noncommercial
restrictions. The unmodified official text was obtained from the PolyForm Project
[versioned repository](https://github.com/polyformproject/polyform-licenses/blob/1.0.0/PolyForm-Noncommercial-1.0.0.md).
The upstream LICENSE is kept unmodified; authorship of this implementation is
recorded here and in source files. No dependency was installed, and no commit,
push, public release or Workshop operation has been performed by this session.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

The `Dev Manager checks` workflow is configured to run the isolated Node.js tests
on GitHub's Windows runner for this project's own branch. It does not install
dependencies, publish a release or access game files. Its cloud result has not
been verified before the first branch push. Signature: OpenAI-Codex.
