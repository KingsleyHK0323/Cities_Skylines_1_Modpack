# Route creation, recovery and reload acceptance

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

Status: prepared checklist; no checks in this document have been executed by this local batch. Cloud tests, GitHub prereleases and game acceptance are separate evidence.

## Cloud verification to request

1. Review the scoped diff and preserve all unrelated changes. Record the candidate source SHA; this batch does not authorize a commit or push.
2. Once separately authorized, use the existing GitHub-hosted Windows workflow and pinned encrypted-reference restoration. Do not build locally, read the original game installation, install dependencies, or use a self-hosted runner.
3. Verify `RouteStreetStopChecks` executes both placement arguments across exact RoadAI, bridge/tunnel/unknown/custom subclass exclusions and failed segment eligibility/resolution. These are pure rule checks, not asset or native API tests.
4. Keep all existing manual-input, immutable-plan, request-ticket and twenty execution/recovery cases enabled. Verify test exit status and the complete run, rather than only the build step.
5. Inspect DLL/PDB nonempty assertions. Only the module DLL and the established five public documents may reach publication; never game references or PDBs.
6. Before any authorized publication, confirm the latest draft/public visibility instruction. Use a new full-name tag, eight-section English description, direct assets, exact source target and existing draft/asset verification. Do not recreate or delete old tags/releases. Record run URL, attempt, source SHA, asset digests and visibility.

## Subscribed-build game test prerequisites

Use the separately authorized Steam Workshop subscribed build, not a local DLL or GitHub download as gameplay acceptance. Record subscribed item, exact DLL SHA256/version, game version, enabled mod list and relevant road/vehicle assets. Use disposable copies and existing backups. This document does not authorize installing, unsubscribing, loading/modifying saves or uploading Workshop content.

Record each case as NOT RUN/PASS/FAIL/BLOCKED with evidence. A refused operation is not proof of a successful route edit.

| Case | Procedure after game-operation authorization | Required observations |
| --- | --- | --- |
| Ordinary original | Select a complete Level1 bus route with 3–6 ordinary RoadAI street stops and prepare an unchanged manual preview | Preview succeeds where eligible; no premature rejection merely because resolved placement argument is true; no city write during preview |
| Manual order/site | Prepare a reordered list and a separately captured directional road target; review and confirm twice | Exact original/target displayed; line ID retained, stop count unchanged; verify actual stop positions and whole-route operation; do not infer success from adjacent-path flags |
| Automatic edit | Choose a fresh same-count bus proposal; inspect before/after and confirm twice | Correct existing line modified, other routes unchanged; verify name/color/budget remain intact and vehicles can serve all stops |
| Refusal matrix | Try original and target bridge, tunnel, station-owned/Untouchable, unknown/custom AI, missing lane and stale road cases | Unsupported cases refuse before mutation; capture evidence for each actual asset instead of equating the placement bool to asset type |
| New candidate creation | Generate a fresh proposal and use the existing creation controls on a disposable city | Actual placement arguments retained through AddStop and closing stop; newly created route complete and runnable; editing-only ordinary-road policy must not be assumed to define the separate creation path |
| Cancel confirmation | Cancel each confirmation separately; then change input, planning preview or city before completing a dialog | No edit occurs from the obsolete dialog; controls disarm; no old preview is applied |
| Failed move/recovery | Observe a naturally refused operation or a separately approved controlled failure, never deliberately corrupt a production save | Actual positions/count/completeness inspected; distinguish PositionsRestored, RecoveryIncomplete and Invalidated; no claim passenger/vehicle/path effects were rolled back |
| Reload before apply | Prepare a preview, return to menu and load a disposable city copy, including a city reusing the same numeric line ID | Old preview, ticket, status and confirmation cannot authorize a write in the loaded city; a new preview is required |
| Reload during post-check | Leave the city while creation/edit checks are pending, then prepare a new operation after loading | Old completion cannot unlock or overwrite the new request; separately verify creation and editing paths, which have different current ownership guards |
| Save/load after edit | Save the disposable edited city, reload and inspect all affected and unrelated routes | Line identity, stop order/positions and real service remain valid; no claim based only on successful save/load completion |
| Return to stable | Only under a separately approved migration plan, remove the Development copy and use the subscribed stable build with a backed-up save | Record whether city data remains loadable and functional; do not promise rollback or automatic compatibility |

## Known evidence gaps

Pure tests cannot prove exact native flag semantics, asset recognition, MoveStop side effects, route connectivity, unloading callback ordering or save compatibility. The original Source_Code lock and staged files remain outside this batch. No new transport types, shared-stop behavior, fleet/budget control or new compatibility policy is introduced.

## Lifecycle risks found by static review

The unload entry calls PlanningCapture cancellation and Mod.ReleaseTrackedUI, which resets route controls. Editing uses ticket ownership. This does not prove every asynchronous path has equivalent protection:

- PlanningCapture background exception/finally handling currently publishes an error and clears busy without the success path's generation check. A stale worker may affect a new capture's status/busy; no game race reproduction has been performed.
- RouteCreation's queued finally can clear request-pending after reset without request ownership. The old-preview reference guard prevents accepting an obsolete proposal, but is not equivalent to ticket-owned cleanup.

Before a later publication, review these risks and prepare bounded cloud fake-scheduling regressions: old worker exception after reset must not replace a new status; old finally must not clear a newer capture/creation request; stale delayed checks must not interpret a reused numeric line ID as the prior city. Production changes require their own reviewed batch. Do not mark these cases passed from this checklist or perform unsafe game fault injection.

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

## Prepared lifecycle repair follow-up (2026-11-01)

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

The preceding static-risk description records the pre-repair review. This local candidate now routes background preview and queued creation through `RouteEditRequestGate.RunOwned`, which checks ownership before work and gates error/finally state publications. Creation pending checks also retain a ticket and the original manager instances. The existing preview-generation comparison remains in place. Native work executes outside the gate lock; unloading during an already executing native operation remains a game acceptance boundary.

Prepared production-seam regressions cover old exception/finally after reset, a new request using the same numeric line ID, completed callbacks after another request starts, obsolete queued work, a session change during work followed by an exception, and current-owner failure/cleanup. These checks test ownership mechanics, not actual native route APIs or callback ordering in the game. Status: NOT RUN.

Before publication, separately approve the exact commit/push scope, GitHub-hosted build/test workflow, encrypted-reference restoration and prerelease visibility/assets. Require a successful compile plus the full check executable (including the new RunOwned cases and retained placement/manual-input/twenty recovery cases), then record source SHA/run/attempt and asset verification. This local batch authorizes none of those executions. The subscribed-build game matrix above remains NOT RUN.

## Cloud wiring and cumulative publication review (2026-11-01)

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

All entries below are static inspection of this local candidate. No new test/build/workflow result is claimed.

| Test source | Executed entry from PlanningEngineChecks.Main | Production coverage and limit |
| --- | --- | --- |
| Tests/RouteEditRequestGateChecks.cs | RouteEditRequestGateChecks.Run | The project links Source/Planning/RouteEditRequestGate.cs once. Delayed callbacks and replacement during work exercise the production source gate and RunOwned seam; no native controller execution |
| Tests/CompiledLifecycleChecks.cs | CompiledLifecycleChecks.Run | Loads bin/Release/AutomatedPublicTransportPlanning.dll, executes its actual internal gate via reflection, tests stale/current callbacks, then decodes controller/nested callback IL to verify RunOwned call sites in PlanningCapture and RouteCreationController. IL evidence is structural and separate from actual gate behavior; native controller/game callbacks are not executed |
| Tests/RouteStreetStopChecks.cs | RouteStreetStopChecks.Run | Links RouteStreetStopRules.cs; both placement arguments and eligibility/resolution/exact RoadAI exclusions |
| Tests/RouteEditExecutionChecks.cs | RouteEditExecutionChecks.Run | Existing twenty fake-surface execution/recovery cases retained; no native MoveStop execution |
| Tests/RouteEditPlanChecks.cs; Tests/RouteManualInputChecks.cs | Respective Run methods | Existing immutable-plan and bounded input checks retained |

The test csproj includes each test source once and links the production gate once. CompiledLifecycleChecks requires the actual Development DLL, so invoking the checks alone without first building that DLL is not a valid verification run. A missing DLL/dependency is an infrastructure failure, never a behavioral RED result.

### Existing commands, toolchain and failure chain

The existing workflow is .github/workflows/cloud-compile-test.yml. It triggers on push only to Automated_Public_Transport_Planning and on workflow_dispatch; compile additionally requires that exact branch ref. There is no pull-request/cross-branch trigger or self-hosted runner. No workflow/scripts were changed in this batch.

GitHub-hosted windows-latest supplies Visual Studio MSBuild located through vswhere. Both projects target .NET Framework 3.5/C# 5 and reference the game-managed mscorlib/System/System.Core/System.Xml, ICities, Assembly-CSharp, ColossalManaged and UnityEngine as declared. Game references come only from encrypted bundles plus restoration helper at pinned repository SHA 7b8a5e7c10f2db84388e8dd8af2084f239957b13. The existing secret is scoped to the restore step; no secret value is read or output by this local review. CLR v4 is only the cloud test host, not the game's Mono runtime. No new SDK/package installation is required by the candidate.

Existing workflow commands (documented, NOT EXECUTED here):

```powershell
& $env:APTP_MSBUILD AutomatedPublicTransportPlanning.csproj /t:Rebuild /p:Configuration=Release /p:DevTools=true /p:ModsDir= "/p:CitiesSkylinesDir=$env:CS1_GAME_ROOT" "/p:CSInstallPath=$env:CS1_GAME_ROOT" /verbosity:normal /nologo
$checksOutput = Join-Path $env:RUNNER_TEMP 'aptp-planning-checks'
& $env:APTP_MSBUILD Tests/PlanningEngineChecks.csproj "/p:OutputPath=$checksOutput/" /t:Rebuild /p:Configuration=Release /p:ModsDir= "/p:CitiesSkylinesDir=$env:CS1_GAME_ROOT" /verbosity:normal /nologo
$exe = Join-Path $env:RUNNER_TEMP 'aptp-planning-checks/PlanningEngineChecks.exe'
# The existing workflow first writes the CLR v4 startup configuration beside this EXE.
& $exe
exit $LASTEXITCODE
```

Each build step propagates MSBuild's exit code. Main invokes each check through its existing Run wrapper; any thrown assertion increments failures and Main returns 1. The EXE step propagates that exit code. Default success gating then blocks Prepare-Release.ps1 and artifact transfer on failure; publish needs the compile job and has no always/continue-on-error bypass. DLL/PDB must be nonempty before test building. The existing compile permission is contents:read; publish uses contents:write/actions:read on ubuntu-latest. Existing checkout SHA, upload/download artifact behavior, attempt-specific tags and release-ID verification are preserved. No unrelated action version or artifact handoff migration is included.

### Exact cumulative candidate scope

The active Recovery checkout remains based on 60976d7c34f73aee698af192a660abe001ec347d. Local tracking-ref inspection found no commits in upstream..HEAD; no fetch or live remote verification was performed. Cumulative uncommitted source scope is eleven files, including all prior eight:

- Change Log.md
- README.md
- Source/Planning/PlanningCapture.cs
- Source/Planning/RouteCreation.cs
- Source/Planning/RouteEditRequestGate.cs
- Tests/RouteEditRequestGateChecks.cs
- Tests/RouteStreetStopChecks.cs
- docs/Route-Editing-Acceptance.md
- Tests/PlanningEngineChecks.cs
- Tests/PlanningEngineChecks.csproj
- Tests/CompiledLifecycleChecks.cs

A separately approved commit/push would expose this source diff and retain existing branch history/workflows at KingsleyHK0323/Cities_Skylines_1_Modpack, branch Automated_Public_Transport_Planning. It would also trigger the existing workflow and, if all gates pass, naturally create a draft and then publish a public prerelease (draft=false, prerelease=true, latest=false). Manual workflow_dispatch on the same branch has the same publication effect. There is no test-only dispatch option in the existing workflow.

The prerelease has exactly AutomatedPublicTransportPlanning.dll, README.md (from .github/scripts/Development-README.md, not the root README), LICENSE, build-info.txt, Steam_Workshop_Link.txt and SHA256SUMS. The last four generated/copied metadata assets and the release body follow existing scripts; no public PDB, test EXE, game reference DLL, private note or ZIP is included. Source docs/tests are visible through the Git branch but are not extra release assets. Approval must cover this repository/branch, GitHub-hosted authenticated reference restore, workflow run/artifact transfer and automatic public tag/release/assets. No approval to execute these operations is implied by this checklist. The original Source_Code checkout's staged files and lock are excluded and preserved.

Artifact handoff clarification: this checkout currently transfers `aptp-public-assets` by artifact name through upload-artifact@v4/download-artifact@v4; it does not declare an upload artifact-id output or download artifact-ids binding. This batch preserves that existing behavior and the run-attempt-specific release tag/release-ID digest verification; it does not claim that an explicit artifact-ID handoff was verified here. Review this distinction before authorizing a rerun/publication. No sibling-branch workflow was consulted or copied.

## Artifact identity repair candidate (2026-11-01)

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

This section supersedes the earlier name-only transfer limitation and unchanged-workflow statement for this local candidate only. The remote has not been changed. The workflow now exposes compile.outputs.artifact-id from the actual upload action and compile.outputs.build-attempt from the compile job's GITHUB_RUN_ATTEMPT. Artifact names are aptp-public-assets-RUNID-BUILDATTEMPT. Publish validates both outputs before download (missing outputs cannot silently select every artifact), downloads artifact-ids with the current run-id, and passes the outputs to the publisher. Existing Action references remain unchanged; both jobs additionally require the exact repository and own branch.

Prepare-Release.ps1 retains the six-file schema, adds repository/ref to build-info and requires identity environment fields. Release_Provenance.py rejects missing/duplicate/mismatched sourceSHA/sharedrefsSHA/runid/runattempt/DevTools/repository/ref fields. The publisher first performs existing local asset/hash checks, then reads the selected artifact API under the current repository and verifies its ID, run/attempt-specific name, non-expired status, workflow_run.id, head_sha and head_branch. Only then may draft creation/upload/verification/publication proceed. No artifact ID is embedded into the artifact itself: it exists only after upload; instead the exact ID is carried by job outputs and verified through the repository-scoped API. No new public asset is added.

Tags now use RELEASE_BUILD_ATTEMPT, not the publication job's newer GITHUB_RUN_ATTEMPT. A publication-only retry reuses the producing build identity; an already existing tag/release is not overwritten or deleted by this change and may cause a safe failure requiring separate review. If the producing artifact expired or outputs are unavailable, publication fails rather than falling back by name/latest. This does not establish reproducible builds or runtime/game provenance.

The compile job now runs `python -B -m unittest discover -s Tests -p test_release_provenance.py -v` after checkout and before reference restoration. It uses Python supplied by the GitHub-hosted Windows image, with the standard library only. Tests import the actual pure validator and exercise the actual Publish-Release.py through runpy with synthetic six-file assets and a mocked subprocess/API; wrong artifact identity or stale build attempt must fail before any draft/upload/edit call. Additional cases cover wrong run/source/branch/repository, expired artifact, missing identity/fields, duplicate metadata and the original build attempt surviving publication retry. No real API, credential, DLL execution or release mutation occurs in those contract tests. This check is additive to the existing DLL/test EXE chain; no existing gate is bypassed.

Cumulative candidate scope is now sixteen files: the eleven previously listed plus .github/workflows/cloud-compile-test.yml, .github/scripts/Prepare-Release.ps1, .github/scripts/Publish-Release.py, .github/scripts/Release_Provenance.py and Tests/test_release_provenance.py. Only this APTP checkout is modified. AI Improve's workflow was read as an ID/attempt reference; its SDK, asset schema and files were not copied or changed. Existing compile/publish permissions and reference-secret scope are preserved. A separately approved push/dispatch still naturally creates a public prerelease if all gates pass; this local preparation is not approval to execute or publish.
