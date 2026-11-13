# Cities Skylines 1 Modpack Dev Manager

Windows x64 desktop application for independently checking, installing, updating
and removing Development modules. The selected GitHub Release format is Windows
`Setup.exe`, which installs the App into a user-selected empty folder. Each new
version uses a separate empty folder; users may need to select their existing
Mods destination again. The installed App includes its Node runtime and opens
through `DevManager.exe`, without a separate Node installation or development
toolchain. Windows .NET Framework is required.

Existing portable outputs are for local development/testing. The installer format
decision is not a claim that an installer has been published or that cloud
installation or in-game acceptance has completed. Main Session owns installer
packaging and Release workflows.
The current packaging recipe pins the official Node.js v24.22.2 Windows x64
runtime and its exact upstream license bytes. Old v24.19.0 references and local
packages are retained as historical outputs, not updated in place. Changing the
recipe is not evidence that a new-runtime local/cloud package was built or tested.
See `THIRD-PARTY.md` in the package root (`desktop/THIRD-PARTY.md` in the source
checkout) for official hashes and provenance. Local tests on an older runtime
must remain offline; runtime/package
acceptance on v24.22.2 is a separate validation step.
See [test readiness](TEST-READINESS.md) for implemented scope, evidence layers and
the current publication/module-source blockers before interpreting this as a
downloadable end-to-end test build.

The desktop supports English, Traditional Chinese, Simplified Chinese and Japanese,
plus Windows-following, light and dark themes. Top-level selectors save preferences
across restarts without replacing destination or unknown settings. High contrast
uses Windows system colors; system preference events update the palette and are
unsubscribed when the form is disposed. Flat offline `translations.json` resources
cover normal controls and backend-facing messages; unrecognized diagnostics retain
their original text beside a translated explanation.

The module version and missing-installation status describe Dev Manager records
at the selected destination. Details state that other installation sources,
including Steam Workshop, are not checked.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

Settings use an exclusive cross-process transaction: merge under lock, validate the
8192-byte serialized limit, back up, stage/fsync and atomically replace. In-memory
destinations change only after commit; interrupted staging preserves the old file.
Reads check file size before a bounded 8193-byte read and reject growth/races.
Lock identity failures close the descriptor and retain the unverified lock;
simultaneous operation/cleanup failures preserve both original errors and report
recovery required. First-start and post-commit warnings retain the committed
outcome and do not claim rollback or automatic recovery. Preference saves reject
another instance's changed destination instead of silently adopting it.
If a saved destination cannot be validated, retain its settings and allow a
validated replacement while refusing file operations. Missing settings files are
distinguished from access errors, and read-only probes cannot create metadata.
Synthetic fixtures cover these paths; real desktop and filesystem acceptance remain pending.
The first-initialization fixture confirms reaching an intercepted directory-creation
attempt; it does not write complete metadata or perform a real installation.
A newly requested test workspace is retained and identified if initialization or saving fails.
During one session, previously observed manager metadata must retain its verified
root and metadata identities; an unavailable or changed destination blocks
operations. A newly selected Mods folder without metadata may be initialized on
first use. The settings do not retain proof that metadata existed in a prior
session. Path-based checks cannot eliminate every external replacement race.
During the same session, reselecting a previously inspected Mods destination or
switching away and back preserves its observed root and metadata presence.
Verified test-workspace identities are also retained during the session.
If the first validation of a saved test workspace fails before its identity is
known, this session refuses that same workspace even if it is subsequently
repaired. A validated game Mods destination remains selectable. Restarting does
not prove the identity of the original workspace. Failed
Mods metadata validation is an unknown state, not proof of absence. Recovery
after repair requires matching directory identities observed around the failed
check and complete revalidation; unobservable original identities remain blocked
for that path. These checks do not authenticate earlier metadata contents.
An ENOENT probe is classified as absence; concurrent path changes can prevent it
from proving which component disappeared.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
Read the [user guide](USER-GUIDE.md) for save/inspection behavior. Headless probes
and isolated tests are not live GUI, installer or gameplay acceptance.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

Read the [user guide](USER-GUIDE.md) and [publisher contract](PUBLISHER-CONTRACT.md).
The next native and portable builds require the App icon at
`desktop/DevManager.ico`. Both build scripts embed those ICO bytes into the EXE;
the desktop form uses the icon extracted from its own executable for the window
and taskbar. Developer-shell cache identity includes the source, icon, compiler
and build script hashes, so changing the icon selects a different cached EXE.
Missing input stops the build; earlier executables and incomplete new build
outputs are preserved. Identity-only checks do not validate or create an image.
An existing isolated `OutputDirectory` can be supplied for developer builds.
The icon asset is included; visual acceptance remains pending.

The new [artifact branch contract](ARTIFACT-CONTRACT.md) implements independent
`Development_Build/<source branch>` downloads without module Releases or ZIPs.
**Independent Development build branches** is the startup default. Unavailable
artifact sources never trigger automatic fallback. It is the only player-facing
module source. GitHub Releases are reserved for Dev Manager `Setup.exe`, not module
Releases or modpack ZIPs; real published branch acceptance is pending.
Automatic artifact checks use shared ref snapshots and cached immutable manifests,
with bounded rotating verification and a shared rolling-hour request budget.
Discovery remains separate from full pre-install source/run/file validation.
An artifact without its matching source branch remains unavailable. A failed or
incomplete shared source snapshot is reported as unknown; an observed request
limit or locally enforced cooldown is reported as queued. Cached manifests never grant update-order evidence
for a changed installation snapshot or source branch head.
For the same build ID, compare the installed artifact branch and exact manifest
SHA-256 with the observed candidate. A different manifest or missing comparable
provenance stays unknown and blocks automatic updates; an artifact commit change
alone does not mean that the manifest changed.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
HTTP error status and rate-limit headers are preserved even when an error body is
empty or not JSON. A successful response with invalid JSON or a failed body is
still rejected, but its received rate-limit headers are applied to the local cooldown.
The API transport applies rate-limit headers as they arrive, before the response
body finishes. Requests already sent can still finish during that cooldown.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager
The desktop registers all 15 project modules. Missing published sources stay
unavailable; registry membership is not a promise that a module can be downloaded.
The current registry shows 12 modules awaiting publisher configuration and AI Gaming
with development paused. Valid artifact manifests can establish pending mappings.
For a paused module, individual source checks, installation and updates are
disabled. A batch containing one stops before source checks or downloads. An
intact owned installation can still be removed by the user.
The existing static browser preview below remains a separate synthetic demo.

The desktop uses anonymous HTTPS GitHub requests, shared artifact-ref discovery,
verified source runs, hashed runtime assets and DevTools provenance declarations.
Legacy Release/bundle code and tests remain internal for historical compatibility.
They are not exposed by the desktop source control or backend source selection.
Normal button clicks
start single/batch operations; no additional confirmation dialog is required.
Select an existing Mods destination first. No game folder is accessed by default.
Filtered-out selections remain checked; install, update and removal are disabled
until the search is cleared to review every selected module or the selection is
cleared. Candidate-selection buttons affect visible rows only.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

An installed build matching the observed candidate is shown separately from an
unavailable source; the candidate still requires verification. Signature: OpenAI-Codex

Source observations expire after one hour. A missing or invalid observation time,
or a system clock moved behind that observation, makes its candidate unavailable
until a fresh check. Existing blocked reference names are checked against both
the DLL filename and the declared assembly identity, including renamed files.
This denylist is not a complete inventory of game or framework assemblies.
The service checks source freshness after refresh, after package preparation and
again under the transaction lock before and after staging. The desktop expires
cached rows and schedules a new check after a backward system-clock adjustment.
The local request allowance and observed server cooldowns use elapsed time, and
concurrent responses cannot shorten an active cooldown. Date-based server limits
use the response date when available; otherwise they depend on the local clock.
Each full pre-install check revalidates refs, manifest, run and local ownership.
Verified forward comparisons may be reused in memory for the same repository and
ordered immutable commit pair; incomplete, failed or paginated responses are not
cached. Only explicitly anonymous requests qualify. At most 512 such proofs are
retained for the App process, without response headers; least recently used proofs
are displaced as new ones arrive. A compare response that explicitly reports a
different or invalid head is rejected. Mixed-repository batches are refused and
must be checked separately. If the allowance is insufficient, selected updates
remain queued and no package is downloaded; a later attempt can reuse valid
proofs. Completion depends on the sources, available allowance and fresh checks,
not the number of modules.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

Active modules are direct children `Mods/CS1DevManager-<module>/`. Staging,
ownership records and retained backups are outside Mods in a sibling
`CS1DevManagerData-<identifier>` directory. Unknown or changed files are preserved.
The initial complete ownership record is the update-order baseline and is checked
again under the cooperative transaction lock before staging. Concurrent changes
stop the affected operation; this is not a lock against unrelated external tools.
An intervening change that restores an identical record and contents is not
detectable from value comparison; owned primary file contents are separately
checked under the lock. NTFS alternate data streams are not enumerated, and their
backup and removal behavior has not been verified.
Direct internal installer callers must supply an explicit expected snapshot:
`install(package, null)`, `update(package, installedRecord)` or
`uninstall(moduleId, installedRecord)`. Omitting it is rejected. This changes
the earlier internal method signatures.
Prepared batches are verified before mutation, but commits are per module: a later
failure keeps earlier successful modules and stops the remainder. Removal retains
the owned directory as a backup rather than permanently deleting it.

If a transaction fails or finishes with an inspection warning, the result lists
each remaining selected module as not attempted; earlier results are retained.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

The game-process check also runs before each module transaction. If the game is
running or its status cannot be verified, earlier completed modules remain and
the current and remaining modules are reported as not attempted. This reduces,
but does not eliminate, the race between checking and an external game launch.
Signature: OpenAI-Codex_dev_manager_development

Run `node --test core.test.mjs filesystem.test.mjs` for development checks.
Filesystem tests use synthetic temporary destinations and compiled synthetic DLLs.
These checks do not prove actual subscribed-game loading or gameplay stability.
The current desktop work is local and is not covered by the earlier pushed CI run.

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

### Historical internal GitHub Release candidates

This retained implementation is not a player-facing source. Module distribution
uses independent artifact branches; the section documents internal legacy tests.

`github-source.mjs` provides `detectGitHubModule({policy, installed, request,
nowMs, timeoutMs})`. The normal source assumption is a trusted HTTPS GitHub API
adapter, not an additional signing service. The adapter is injectable for tests;
`https-source.mjs` configures the real desktop transport. Its GET response contract is explicit
status/URL/redirect state, a complete lowercase-header record (including Link)
and decoded JSON `body`. Requests omit credentials, reject redirects and request
`cache:'no-store'`. This is API metadata trust, not independent signatures or
cryptographic freshness. Desktop install controls invoke the resolver before downloads.

The trusted module registry supplies `moduleId`, `repository`, `branch`,
`assembly`, `workflowPath`, `releaseTagPrefix` and a complete `runtimeAssetNames`
allowlist. The supported explicit convention is `<releaseTagPrefix><runId>-<attempt>`;
unconfigured/legacy conventions stay unknown rather than being guessed.

The resolver reads the exact branch ref, follows every release Link page, filters
only the module prefix and selects the unambiguous most recently **published**
non-draft module release by `published_at`. It resolves lightweight or annotated
tag refs to commits, checks the exact successful Actions run attempt, repository,
head repository, branch, SHA and workflow path, and follows all release-asset pages.
Required runtime assets must have unique IDs/names, expected names, uploaded state,
bounded sizes, SHA-256 metadata and exact repository asset/download URLs. Missing
dependencies, unexpected DLLs or an unverifiable newer candidate yield unknown;
the resolver does not silently fall back to an older build.

`latestPublished` and `branchHead` are separate. A different SHA means
`different-commit`, not behind/ahead without ancestry evidence. `status` compares
the installed record's opaque `buildId` with the selected full tag; an unknown
installation record keeps comparison unknown even when a published candidate is
identified. Repository-wide Latest, release titles and prerelease flags do not
select builds. A draft-only module has no usable published candidate.

Bounds are ten pages of up to 100 records per release/asset collection, five tag
objects, 4 MiB per decoded response, 32 MiB per declared runtime asset and 64 MiB
per runtime set. One 1 ms–30 second deadline covers all requests (default five
seconds). Incomplete/unsafe pagination, API errors, stalls, ambiguous publication
times or inconsistent repeat reads yield unknown. The first release page, chosen
release, tag and branch are rechecked; volatile request headers are not compared.
GitHub pagination is not an atomic snapshot, and repeat checks cannot prove the
absence of every concurrent mutation. Results are observations at fetch time;
they do not prove actual DLL identity, source-to-binary provenance or future
freshness. Publication is not compilation or subscribed-game acceptance.

See the [official releases API](https://docs.github.com/en/rest/releases/releases),
[pagination](https://docs.github.com/en/rest/using-the-rest-api/using-pagination-in-the-rest-api),
[workflow runs](https://docs.github.com/en/rest/actions/workflow-runs) and
[Git references](https://docs.github.com/en/rest/git/refs). `target_commitish` can
be a branch and is ignored when its tag already exists, so this resolver uses the
resolved tag commit rather than treating that field as commit proof.

### Optional signed-pointer research

`sources.mjs` adds pure `classifyModuleSource(input)` for a proposed signed,
per-module latest-published-development pointer. This is optional, unadopted
research; it is not a prerequisite for GitHub-native update detection. It does not poll GitHub or infer
latest from repository ordering, titles, timestamps or `prerelease`. Missing
authentication, freshness, installation knowledge or release binding returns
`status:'unknown'`, with no install permission or usable package.

The caller independently pins an Ed25519 public key in `policy.freshnessPublicKey`,
supplies exact catalog bytes, a signed pointer, a fresh unpredictable request
`nonce`, trusted `nowMs`, per-module persistent `minimumSequence`, and GitHub-shaped
`releases`. The verifier binds module/branch/assembly/workflow/build/source SHA,
catalog hash, release ID/tag and development channel. A pointer must match the
nonce, not fall below the sequence floor and be within its signed validity window
(at most five minutes). The selected non-draft published release must match exact
repository URLs, target SHA and runtime asset IDs, names, sizes, digests and URLs.
Unrelated candidates and their Latest/prerelease flags cannot replace it.

Valid results compare opaque build identities (`current`, `different-build` or
`not-installed`) under the authenticated pointer's stated scope. `different-build`
still does not establish version ordering. Signing proves the pinned key's
statement, not live branch ancestry, actual DLL identity or workflow execution.
There is no production signer, key distribution/rotation/revocation, fresh nonce
exchange, trusted clock or persisted anti-rollback state configured. Reusing a
nonce or resetting the sequence floor can weaken replay protection; the caller
must manage both. API data alone cannot satisfy this optional signature contract. The UI,
batch preparation and actual installation are not wired to this verifier yet.

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
used by the configured desktop network downloader. It first validates exact catalog bytes
against the external policy pin, then selects one exact allowlisted asset path.
It never discovers releases, calls `fetch` or writes files itself. Desktop controls call it.

The injected adapter receives `GET`, `redirect:'manual'`, `credentials:'omit'`
and an `AbortSignal`. Responses must declare their exact requested URL,
`redirected:false`, numeric status, lowercase-header record and async byte stream.
An actual adapter must honor manual redirects and abort/close all outstanding
response bodies; the desktop HTTPS adapter supplies this behavior.
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

The desktop adds a network downloader, bounded in-memory archive reader,
reflection-only assembly-name inspector and game-process check. Tests use synthetic
compiled assemblies, not real module DLLs. A matching SHA-256 proves that bytes
match the supplied GitHub contract; it is not independent publisher authentication.

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

**Scope of the next paragraph: direct construction for the isolated test workspace only.**

`IsolatedInstaller` supports actual install/update/uninstall operations only in
directories immediately under the current OS temporary directory named
`cs1-dev-manager-test-<random suffix>`. Other roots are rejected before metadata
creation. It cannot currently be pointed at a real game or Steam installation.
The temporary-root restriction in the preceding paragraph applies to direct
test-workspace construction. The separate `forModsDirectory` factory can
accept an explicitly confirmed local game Mods directory, subject to its ownership
and safety checks. This describes a code path; real GUI and game acceptance have
not been established.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

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
permissions remain residual risks. The practical checked-path destination factory
is enabled without claiming a hostile-process-proof native security boundary.

Stable/development coexistence produces only a warning; no subscription,
enablement or loading change is authorized by detection. The desktop supports
bulk execution and progress with an explicit Mods destination. Real module
download/game-loading acceptance remains separate. See [publisher contract](PUBLISHER-CONTRACT.md).

Code and original documentation use [PolyForm Noncommercial 1.0.0](LICENSE).
Copyright Kingsley. This is source-available software with noncommercial
restrictions. The unmodified official text was obtained from the PolyForm Project
[versioned repository](https://github.com/polyformproject/polyform-licenses/blob/1.0.0/PolyForm-Noncommercial-1.0.0.md).
The upstream LICENSE is kept unmodified; authorship of this implementation is
recorded here and in source files. No dependency was installed and no public
release or Workshop operation was performed for this application.

Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager

The `Dev Manager checks` workflow is configured to run the isolated Node.js tests
on GitHub's Windows runner for this project's own branch. It does not install
dependencies, publish a release or access game files. Commit
[`6ce59fdbecdbc1789732ef58c673e573e30fc813`](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/commit/6ce59fdbecdbc1789732ef58c673e573e30fc813)
was pushed to the independent application branch. On 2026-11-03, read-only API
verification confirmed [run 41535446517](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41535446517)
completed successfully for that exact SHA. That committed milestone had 70 local
synthetic tests; subsequent local source-classification changes are not included
in that run. This is cloud test evidence, not browser or game acceptance.
Signature: OpenAI-Codex_Cities_Skylines_1_Modpack_Dev_Manager.
