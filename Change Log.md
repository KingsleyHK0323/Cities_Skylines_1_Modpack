# Change Log

Copyright © Kingsley.

## 2026-10-28 — Development route refusal diagnostics

- Added eligibility and build-index details to route creation refusal logs while retaining the existing safety checks.
- Author: OpenAI-Codex.

## 2026-10-28 — Development description

- Updated the Development content-manager entry to identify DevTool and warn that it can damage saves.
- Author: OpenAI-Codex.

## 2026-10-27 — Development route proposal creation

- Added a Development-only flow to select and create one eligible planning proposal. It remains disarmed by default and requires a fresh confirmation for each creation.
- Stop proposals now preserve the captured segment build index, world direction, selected bus lane, position, and platform setting. Creation revalidates these values, the bus prefab, city build index, stop anchorability, and line limits on the simulation thread.
- Added a 3–6 distinct-stop-lane eligibility rule and asynchronous checks for stop connections and `PathFailed` flags on segments adjacent to stops.
- The planner still does not model lane-level turns or TM:PE restrictions; this remains a Development prototype.
- Author: OpenAI-Codex.

## 2026-10-31 — Independent cloud workflow readiness

- Disabled automatic Development Workshop dispatch.
- Added a manual, intentionally failing dependency-readiness gate and documented the unresolved game-reference source, independent branch ownership, and direct DLL release direction.
- No compilation, public release, or Workshop upload is enabled by this change.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-10-31 — Real cloud compilation diagnostic

- Added a narrowly triggered GitHub-hosted Windows MSBuild Rebuild attempt using an isolated empty game root, retaining original missing-reference errors and nonzero exit status.
- No game references, artifacts, releases, or Workshop uploads are provisioned by this diagnostic.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-10-31 — Authenticated references for cloud compilation

- Connected the compile diagnostic to the fixed encrypted reference bundle and step-scoped Actions Secret, restoring only under RUNNER_TEMP.
- Added assertions for nonempty compiled DLL and PDB outputs; retained read-only permissions and no artifact, release, or Workshop publishing.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-10-31 — Independent Development prereleases

- Publish unique Development prereleases after cloud compilation with a strict six-file asset whitelist, unchanged license, install instructions and verified SHA-256 digests.
- Separate read-only build and write-permitted publisher; no decrypted references, PDBs, public ZIP, or Workshop upload.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-10-31 — Cloud planning checks and unified release format

- Build and execute the eight existing pure-data planning checks before allowing publication; the cloud executable uses an explicit CLR v4 host, not the game's Mono runtime.
- Use the complete module name in future Development release titles and tags, with the eight approved release-description sections. Existing releases are preserved.
- Editing existing bus routes remains unimplemented; no game behavior is changed by this iteration.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-10-31 — Existing-route editing safety model

- Added immutable move-only editing plans for manual and automatic modes, with exact original-route fingerprints, finite coordinates, preserved line identity and unchanged three-to-six-stop counts.
- Added pure-data validation checks and cloud inspection of the actual public MoveStop signature. Live editing UI and application are not enabled by this model-only step.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-10-31 — Existing-route manual and automatic editing prototype

- Added Development-only existing city bus route editing: manual original-stop ordering or captured directional road midpoints, and automatic replacement using a selected planning proposal while retaining the line ID and stop count.
- Require a read-only before/after preview, arming and two independent confirmations; revalidate city epoch, original line fingerprint and target lanes on the simulation thread. Serialize edits with new-route creation.
- Support complete ordinary Level1 bus routes with three to six street stops. Reject fixed-platform originals, stop-count changes, invalid coordinates, repeated sites and stale captures. Failure requests best-effort position recovery and reports remaining risk; no atomic rollback or in-game acceptance is claimed.
- Updated active repository/Pages links and both module checkout remotes to KingsleyHK0323; historical release/tag records remain unchanged.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-10-31 — Observed route recovery and request ownership

- Added an ownership ticket gate for queued previews, status publication, delayed checks and completion. City reset invalidates old tickets; an old callback cannot publish a preview or unlock a newer request.
- Recheck the current city, line identity and unchanged stop count before apply/recovery writes; track stop-node changes caused by this operation while rejecting intervening replacements.
- Classify recovery by observed finite positions (within 0.5 metres), stop count and line completeness after refresh, rather than MoveStop return values. Skip already unchanged positions and stop writing after invalidation.
- Added fault-injection checks for false/throwing moves with side effects, misleading recovery results, lost identity/count, NaN and request invalidation. The initial test-first cloud run failed as intended and skipped publication.
- Recovery remains position-only and non-atomic; passenger, vehicle and path effects and subscribed-game behavior remain unverified.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-10-31 — Bounded manual edit input

- Validate the complete manual stop list before resolving stop sites or requiring a road planning capture: at most 128 characters, exactly the existing stop count, positive bounded indices and directional segment IDs, and no normalized duplicate tokens.
- Reject captured road directions that cannot anchor a stop, rather than accepting an open direction with no usable stop lane.
- Added cloud checks for valid mixed tokens, supported counts, malformed/oversized input, bounds and normalized duplicates. Live road and stale-route checks remain mandatory.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-11-01 — Ordinary bus stop placement flag correction

- Fixed a reachable rejection: successful vehicle-stop resolution sets fixedPlatform=true, which was incorrectly treated as proof of an unsupported station asset.
- Classify original and target stops by exact ordinary RoadAI type plus existing segment eligibility and successful placement resolution. Bridges, tunnels, station-owned/unusable roads and unknown/custom AI types remain refused.
- Preserve the resolved placement argument in original stop snapshots, stale matching, manual original-site targets and position recovery instead of forcing false. This argument is not an asset category or a persisted-node flag.
- Added cloud red/green classification and recovery-argument regression checks. No shared-stop or cross-mode capability is introduced; in-game behavior remains unverified.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

## 2026-11-01 — Prepared placement and lifecycle acceptance coverage

- Expanded cloud-ready street-stop regression coverage to both placement arguments and all failed eligibility/resolution combinations; bridges, tunnels and unknown/custom AI types remain refused.
- Added docs/Route-Editing-Acceptance.md with separate candidate creation, edit recovery and subscribed-build reload checks, plus a concrete cloud/publication review list.
- This local batch performed static review only. The new checks were not executed and no commit, push, build, game operation or release was performed.
- Signature: OpenAI-Codex_Automated_Public_Transport_Planning

## 2026-11-01 - Prepared lifecycle ownership repair

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

- Reuse request-ticket ownership for PlanningCapture worker success/error/finally and RouteCreation queued work/error/finally. Reset invalidates obsolete callbacks before they can publish or clear newer state.
- Bind creation post-check state to its ticket and manager instances, and publish pending-state cleanup only for the current owner. Preserve existing placement rules and route behavior.
- Prepare deferred callback checks against the production RunOwned seam, including session replacement during work followed by failure. This is a static control-flow repair; no in-game race reproduction, test execution, build, commit or publication is claimed.

## 2026-11-01 - Prepared compiled lifecycle verification

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

- Include CompiledLifecycleChecks in PlanningEngineChecks and invoke it from the existing failure-counting entry point. Exercise the freshly built Development DLL's actual gate through reflection; separately verify compiled preview/creation call sites target RunOwned.
- Keep the existing branch-scoped DLL build, test build/execution and six-asset publication chain unchanged. Document cumulative source publication scope and automatic public prerelease effects. No tests, workflow, commit or push were executed.

## 2026-11-01 - Prepared immutable artifact provenance handoff

Signature: OpenAI-Codex_Automated_Public_Transport_Planning

- Export upload artifact ID and producing attempt from compile; use a run/attempt-specific artifact name and exact-ID download within the same run. Validate required outputs before download and constrain both jobs to the APTP repository/branch.
- Add repository/ref to build-info. Verify artifact API ID/name/expiry/workflow run/source against the compile output and strict build-info fields before any release mutation. Release tags use the original producing attempt on publication-only retry.
- Add pure provenance and actual-publisher fake-API negative contracts to the cloud compile gate. Preserve the existing whitelist, hashes, draft/release-ID digest checks and English release format. All new checks remain unexecuted; no local or remote workflow was run.
