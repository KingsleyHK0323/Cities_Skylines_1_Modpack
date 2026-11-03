# Automated Public Transport Planning

Visit the [Automated Public Transport Planning website page](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/automated-public-transport-planning/) for project information, documentation, and updates.

Author: Kingsley

## Licensing

- This app is source-available; its source code is licensed under the PolyForm Noncommercial License 1.0.0. See LICENSE.
- Website source code: PolyForm Noncommercial License 1.0.0.
- Website content, including text and images: Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0).
- Copyright Kingsley.

<!-- Prepared by OpenAI-Codex. -->

## Planning engine checks

The .NET Framework 3.5 check runner uses no third-party test framework. It compiles the production pure-data planning sources against the game assemblies configured by `Local.props` or `CITIES_SKYLINES_DIR`. If MSBuild is not on `PATH`, locate it with the Visual Studio Installer `vswhere.exe`. From this directory, run:

```text
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $msbuild Tests\PlanningEngineChecks.csproj /t:Rebuild /p:Configuration=Release /p:ModsDir=
.\bin\PlanningEngineChecks\Release\PlanningEngineChecks.exe
```

The checks cover citizen-unit-to-people conversion, planning snapshot copy isolation, the stale-snapshot warning, the 3–6 distinct-lane creation rule, concrete proposal stop data, people totals and route-segment coverage, normalized endpoint pairing, missing directed return paths, and the current endpoint/pair search caps. Build output stays under `bin\`; the test project has no game Mods deployment target.

The Development build also offers selected proposal creation. It records the actual bus lane and world direction selected for each stop, then revalidates the snapshot build index, bus prefab, segment build indices, lane, stop position, anchorability, and line limits on the simulation thread. Creation is hidden from Release builds, starts disarmed, requires confirmation on each click, and accepts only proposals with 3–6 distinct stop lanes. A later simulation-frame check inspects stop-lane connections, the `LineNotConnected` flag, and `PathFailed` flags on segments adjacent to stops; it does not verify each route's full path.

If creation is refused for stop eligibility or build-index changes, the Development log records the individual conditions and the capture and current indices. The global build index is diagnostic during capture but is a strict creation guard. <!-- OpenAI-Codex -->

The Development content-manager entry identifies DevTool and warns that the development version can damage saves. <!-- OpenAI-Codex -->

The route graph still treats junction-connected segments as connected and does not model lane-level turns or TM:PE restrictions. A generated proposal and its asynchronous checks are a Development prototype, not proof that every line can run in a live game.

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

## Cloud build and release readiness

The `Automated_Public_Transport_Planning` branch owns this module's CI. The existing source-integrity workflow checks project metadata only; it does not compile the DLL or validate gameplay. The manual `transport-cloud-readiness.yml` workflow intentionally fails while an approved, authenticated source of matching game reference assemblies is unavailable. It does not accept a variable as proof that dependencies exist.

Required references are `mscorlib.dll`, `System.dll`, `System.Core.dll`, `System.Xml.dll`, `ICities.dll`, `Assembly-CSharp.dll`, `ColossalManaged.dll`, and `UnityEngine.dll`. No reference DLLs are committed or redistributed by this change. Cloud compilation, planning checks, and direct per-module GitHub Release DLL attachments remain blocked pending a dependency provisioning decision. Releases must provide the mod DLL and necessary permitted files directly, without ZIP archives.

Automatic Development Workshop dispatch is disabled regardless of `WORKSHOP_DEV_AUTO_UPLOAD`. Workshop upload is a separately authorized local operation; this branch must not dispatch the Website upload workflow. No automatic public release is enabled.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Diagnostic cloud compilation

`cloud-compile-test.yml` uses GitHub-hosted Windows and Visual Studio MSBuild located through `vswhere`. It performs a real `Rebuild` with `Configuration=Release`, `DevTools=true`, and an empty `ModsDir`. Both game-path properties point to a newly created empty directory under `RUNNER_TEMP`; the project safeguards and `NoStdLib` remain intact. Missing game assemblies are expected to produce a genuine nonzero build result. This diagnostic publishes no artifacts or releases and performs no Workshop operation. It runs manually or when this workflow file changes on this module branch; ordinary source pushes do not trigger it. This legacy project has no package restore or companion stage.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Verified reference compilation update — 2026-10-31

The compile diagnostic now restores the approved encrypted reference bundle from fixed commit `7b8a5e7c10f2db84388e8dd8af2084f239957b13` in this repository using a sparse checkout. Only the restore step receives `CS1_GAME_REFERENCES_KEY`; the authenticated helper verifies the ciphertext and all eight file hashes before writing references under `RUNNER_TEMP/cs1-game-root`. MSBuild uses that isolated game root. The workflow asserts that the compiled module DLL and PDB both exist and are nonempty. The earlier empty-root diagnostic and unavailable-source descriptions above describe the previous setup; the manual readiness workflow is retained unchanged. No decrypted references or build outputs are uploaded, cached, or released. Ordinary source pushes still do not trigger this diagnostic.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Development prereleases

Every push to this module branch or manual run now builds and publishes a unique `aptp-dev-<run_id>-<attempt>` prerelease. Only the module DLL, Development README, unchanged LICENSE, build-info.txt, Steam_Workshop_Link.txt and SHA256SUMS are transferred to an isolated publisher. Uploaded draft assets are checked for exact names, count, nonzero size and SHA-256 before publication. No PDB, game assemblies or public ZIP are attached. Prereleases are not marked Latest and are not gameplay acceptance. The earlier diagnostic-only trigger statements above describe historical setup.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Planning checks gate and full-name releases

The cloud workflow now compiles and executes the eight existing planning-engine checks before asset preparation and publication. The .NET 3.5 test executable runs using an explicit cloud CLR v4 host; this does not change the mod target or establish game-Mono compatibility. Failures block publication. Future release titles use `Automated Public Transport Planning — Development Build`, with tags `Automated_Public_Transport_Planning-dev-<run_id>-<attempt>` and the approved eight-section installation/risk documentation. Historical diagnostic descriptions above do not describe the current all-push publication workflow. Editing existing bus routes remains unimplemented.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## Existing route editing — Development prototype

The new existing-route controls support both manual stop ordering/placement and automatic whole-route replacement from a selected planning proposal. Both modes preserve the existing line ID and stop count; the first implementation accepts complete ordinary Level1 city bus routes with three to six street stops only. It refuses fixed-platform originals and stop-count changes. This supersedes earlier statements that existing-route editing was unimplemented.

Enter the line ID and use original one-based indices (`1,3,2`) or captured segment-direction tokens (`f123,b456,3`) for a manual preview. Automatic mode takes a one-based planning proposal number with the same stop count. Road tokens and automatic mode require a fresh planning capture. Review the exact before/after preview, arm editing and confirm twice before the simulation-thread revalidation and MoveStop operations. A failed edit requests best-effort position recovery; it cannot guarantee atomic rollback or restore all simulation side effects. Use a save backup and inspect the full route afterwards. See the attached Development README for detailed usage.

Cloud model checks cover immutable input isolation, exact stale-route matching, line identity, unchanged counts and unsafe coordinate/site rejection. Successful cloud compilation and pure-data tests do not establish subscribed-game UI, MoveStop, connectivity or save/reload behavior. No stop insertion/deletion or line recreation is implemented.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Edit failure and city-change handling

Queued edit operations now carry unique ownership tickets. City unload invalidates the old ticket, so an old preview, delayed check or completion cannot overwrite a new city's edit state. Before each apply or recovery write the operation verifies its request, managers, line identity and unchanged stop count.

Recovery skips already unchanged stops and reads back finite stop positions, count and route completeness after the line refresh. `PositionsRestored` means only that original positions were observed within 0.5 metres; it does not restore all passenger, vehicle or path side effects. `RecoveryIncomplete` requires inspection and restoring a save backup. `Invalidated` stops further writes after a city/request change. False or throwing native calls may already have modified the route, so return values alone are never treated as proof of recovery.

Cloud fault-injection checks exercise these control-flow cases against a fake route surface. They do not establish the real game's MoveStop, UI, connectivity or save/reload behavior.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Manual input validation

Manual lists are limited to 128 characters and must contain exactly one comma-separated token per existing stop. Whitespace around tokens is allowed. Original indices must be positive and within the current stop count; `f`/`b` segment IDs must be 1–65535. Empty, signed, overflowing and normalized duplicate tokens (such as `f1,f001,3`) are refused before stop resolution. Opposite directions on one segment are separate tokens; the later physical-site validation may still reject overlapping stops. A road direction must have a captured anchorable lane. These syntax checks do not replace live-road, snapshot or confirmation safeguards.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Ordinary street stop eligibility correction

The placement API's fixedPlatform argument is not an asset classification: ordinary vehicle-stop resolution also returns true. Existing-route editing now classifies both originals and targets using the exact RoadAI type, existing segment eligibility and successful placement resolution, and preserves the original resolved argument for manual targets and recovery. Bridges, tunnels, station-owned roads and unknown/custom AI subclasses are conservatively refused. Earlier references to rejecting a true placement argument describe the previous bug, not the current policy. This does not enable elevated/shared stops or prove runtime/save compatibility.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Prepared acceptance coverage

See [route editing acceptance](docs/Route-Editing-Acceptance.md) for cloud-ready placement-rule regressions and separate subscribed-build creation, recovery and reload cases. The expanded placement-argument matrix is prepared but unexecuted in this local batch. It does not establish native or save compatibility. Publication requires the applicable authorization and visibility review.

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

### Prepared lifecycle ownership repair (2026-11-01)

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

Planning preview workers and queued route creation now use the existing request-ticket gate through `RunOwned`. Obsolete work cannot publish an exception or release a newer request from its finally callback. Creation's pending validation stores the owning ticket and manager instances; reset clears them together. No native game calls run under the ownership lock. This does not make native route mutation atomic with unloading.

Deferred-callback regressions are prepared in `RouteEditRequestGateChecks`, including reset during work followed by an exception, stale queued work, reused numeric line IDs, and current-owner error/cleanup. Tests and builds have NOT RUN in this local batch. See the acceptance checklist for the separate cloud and game approval gates.

### Compiled lifecycle verification wiring (prepared, 2026-11-01)

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

`CompiledLifecycleChecks` is included in the existing test executable and loads the Development DLL built earlier by the cloud workflow. It exercises that DLL's actual request gate (obsolete queued work, replacement during work followed by an exception, current error/finally), then inspects compiled controller call sites for `RunOwned`. The call-site inspection is structural evidence, not an execution of native game callbacks. No additional workflow or permissions are introduced. A successful branch push or manual dispatch still naturally publishes a public prerelease after all existing gates pass; publication requires separate approval. All new checks remain NOT RUN.

### Prepared immutable artifact handoff (2026-11-01)

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

The local cloud-workflow candidate now exports the upload artifact ID and producing build attempt, downloads that exact ID from the same run, and checks artifact API identity against build-info before creating any release. The artifact name includes run/attempt; release tags use the producing build attempt even when only publication is retried. Contract tests include the actual publisher with a fake API and assert mismatches stop before draft/upload/edit. Existing six-file/hash/draft verification remains. This supersedes the earlier name-only handoff limitation for the local candidate; no remote update or test execution is claimed.

### Captured lane preview and finite stop data (local candidate, 2026-11-02)

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

Read-only proposal descriptions now include each selected segment/direction, zero-based captured lane index and segment build index. These values identify a copied proposal; they are not live validity, selectable lane enumeration, occupancy information or permission to share a stop. Route creation's existing stop-site rules now reject NaN/infinite coordinates before distance checks; distinct-lane and 2.5 metre spacing rules remain unchanged. The existing manual confirmations and live revalidation still apply. Vehicle Management is optional and no fleet state or dependency is added.

`Tests/Run-StopPreviewSynthetic.ps1` executes the actual production stop-site rule source and preview formatter in memory, using only a small Vector3 arithmetic boundary substitute. Eighteen assertions passed locally after a meaningful NaN regression failed on the original source. This is not a game/module build, workflow runner or native Unity test. The full PlanningEngineChecks entry also includes the new checks and an actual Describe integration assertion, but the full game-reference build/test has not run for this candidate. No workflow or publication files were changed.

### Copied stop-list explanations (local candidate, 2026-11-02)

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

Each route preview now explains whether its copied stop list passes the existing data checks. Refusals identify missing segment/lane, non-finite coordinates, duplicate segment/lane pairs, unsupported stop counts or the existing 2.5 metre spacing rule; stop numbers are one-based positions in the displayed list. The eligibility boolean and preview explanation share one validator, preventing separate display-only rules. Passing means copied-data checks only: manual confirmation and live revalidation remain required, and connectivity/shared-stop permission is not established.

The in-memory synthetic suite now passes 33 assertions, including refusal explanations, after a missing-list explanation failed against the initial stub. Full preview rendering assertions are also prepared in PlanningEngineChecks but have not run with game references. Earlier local 18-assertion evidence remains historical. No workflow, Vehicle Management dependency or game mutation was added.

### Independent Development build publication (2026-11-03)

The cloud workflow retains the existing compile, planning checks and artifact provenance verification, and publishes verified Development files to the independent `Development_Build/Automated_Public_Transport_Planning` branch. The legacy per-module GitHub Release job is disabled. A failed publish must be retried by rerunning all jobs, so the build and publishing attempts match. The branch manifest records file hashes and source identity; actual cloud publication and in-game behavior require separate verification.

Signature: OpenAI-Codex
