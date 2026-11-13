# Full Map Unlock And Expansion

Planning-stage module for Cities: Skylines 1. The intended scope is full-map access and expansion. An observation-only prototype DLL was previously compiled; it contains no DevTool and does not implement area unlocking or purchasing.

Project repository: https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack

## Validation and releases

The baseline workflow checks documentation and compiles the pure preview checks. A separate game-observation project and cloud build are configured; neither check establishes in-game behavior or implements purchasing.

The planned development distribution uses the standalone Dev Manager installer; GitHub Release attachments are intended for that installer rather than separate module releases. This does not imply that distribution for this module is ready. Gameplay acceptance requires the Steam Workshop subscribed build.

## License

Code: PolyForm Noncommercial License 1.0.0; see LICENSE. Original public documentation: CC BY-NC-SA 4.0 (https://creativecommons.org/licenses/by-nc-sa/4.0/). Copyright Kingsley.

Signature: OpenAI-Codex_Responsible: Full_Map_Unlock_And_Expansion


## Local preview prototype

Source/AreaPreview.cs classifies caller-supplied native eligibility, count and price. It does not authorize a purchase. The first-stage product goal is all 25 areas unlocked at the start without debiting city funds; handling of existing saves remains undecided. An 81-area expansion is a separate future phase.

The pure `MissingForInitial25` function accepts 25 `Initial25Tile` records, each with a declared `(x, z)`, native index and nullable unlocked value. It checks bounds, uniqueness, the 5-by-5 mapping `nativeIndex = z * 5 + x`, the reported count, and caller-supplied generation and revision values. Input record order does not determine coordinates. It returns the row-major indices reported as locked, null for invalid or incomplete input, or an empty array if the supplied snapshot reports all 25 unlocked. The output is a detached candidate set, not an execution order or authorization.

Coordinate and index labels are also supplied by the caller. Even 25 unique, consistent labels and matching counts cannot prove that each value was actually read from its claimed native coordinate; swapped values can pass. Callers must independently establish that all 25 distinct, correct native coordinates were read into a stable snapshot and that its generation and revision are authentic and current. Without that evidence this preview must not be used as a basis for game writes. No game adapter invokes this preview, and it makes no cost decision.

`PreviewInitial25NoDebit` is a separate, stateless synthetic preview of one adjacent missing area at a time. A candidate carries `RequireMoney = false`; pending or failed reported attempts produce no next candidate, and a reported success requires a newer matching-generation snapshot showing that tile unlocked. The preview does not execute a game API or change funds. Its startup flag, previous-attempt report, coordinate labels and freshness tokens are supplied by the caller and are not authenticated; in particular, a later call can report `None` after failure. It cannot enforce persistence, prevent retries across calls, establish real success, or guarantee real game funds are unchanged. Any future game adapter must validate raw x and z are each in 0–4 at the actual call site as well as proving the snapshot and lifecycle boundaries.

`Initial25NoDebitRun` adds an isolated, in-memory coordinator for one caller-supplied snapshot stream. It exposes at most one outstanding candidate per instance, tracks snapshot revisions while pending, and requires an explicit observation showing only that candidate changed. It halts on a conflicting snapshot, a changed city generation, or a failure identified by the current candidate's `AttemptId`; an old or unidentified failure cannot halt a later attempt. Attempt IDs are unique only within their run, so callers must route them to the originating instance. Separate instances and process restarts have independent state. Callers must provide stable snapshots. A confirmation describes only supplied values; it does not establish which action caused the change or prove the game's funds were unchanged. A previously returned candidate remains readable after the run halts and must not be treated as authorization. This coordinator has no game adapter and never performs an unlock or payment.

`StartupMilestoneScopePreview` is a separate synthetic experiment for a single scoped milestone exception. It compares caller-supplied owner, provider, generation, attempt, coordinates, startup state and thread, and rejects reuse after its one successful check or invalidation. The supplied context and claimed native-call position are not authenticated; another scope can be constructed independently. It is not a patch, an `IAreas` adapter, or authorization to perform a game write. The test-only dispatch fixture combines the pure models with fake scheduling and a fake grid under a single-controller assumption; it does not test a concurrent queue or execute the game's scheduler or unlock API.

The scope experiment cannot distinguish an unrelated nested call that supplies the same context before the intended call; a synthetic test reproduces this limitation. No native milestone intervention is enabled.

`INativeStartupSubmission` is a data-only interface candidate in the main project source list. Its sole in-module implementation, `DisabledNativeStartupSubmission`, returns `Unavailable` for every request and has no provider, scheduler, callback or writer parameter. The current `game-dll.yml` direct source list omits this interface and implementation; its included `GameObservation.cs` separately contains the always-unavailable `StartupSnapshots.PrepareNativeSubmission`. No hosted artifact has been verified for this change. The request envelope contains caller claims and does not retain the snapshot or establish eligibility; its supplied coordinates and tokens remain readable for as long as a caller retains the request. The separate snapshot argument still exposes its grid to an implementer and may be retained by a caller. No module code invokes the interface as a gameplay submission. Synthetic return and signature tests alone cannot detect a future implementation that uses a global game object before returning; the actual implementation and call graph must be reviewed after changes. Other assemblies could implement an interface independently, access other game objects or retain the snapshot; this is not a sandbox for third-party code.

`RedactedDiagnosticBuffer` is an in-memory, default-disabled test prototype with a per-instance limit of 64 closed-enum events. The main project's normal Release source list excludes it; an external compiler command that explicitly includes its source can still compile it even with Release symbols. Its public `Read` returns an in-process copy; it does not itself write files or transmit data. The prototype is not connected to game callbacks and has no public enable setter; its internal synthetic constructor can be invoked by same-assembly code or reflection. Closed enums and a size limit do not prevent deliberate data encoding through event order. The existing ordinary-new-game snapshot adapter still reads and retains area state in memory.

The separate `Diagnostics/LifecycleTraceCandidate.cs` data library is also default-disabled and disconnected from game callbacks. It can return fixed-field JSONL bytes to an in-process caller, but has no file or network output, user-visible activation, or installable game extension. The caller can retain or transfer returned bytes, including a new export after collection has stopped. Local labels and event ordering do not guarantee anonymity or authenticate game identity. See the [proposed read-only game validation plan](docs/Read_Only_Game_Validation_Plan.md) for its exact schema and limitations.

The distinct `Diagnostics/LifecycleCaptureCandidate.cs` is a guarded loading-callback candidate. Its reference-built diagnostic assembly contains public `IUserMod` and `ILoadingExtension` types, so a game might discover them if the assembly were installed. The buffer is default-disabled and that build contains no activation, display, export, file or network method; no game was run or installation attempted. Its local labels and event sequences can still be identifying. The main Release project does not include this source; a separate diagnostic flag is needed, and both `RELEASE` and `FULLMAP_RELEASE` suppress it. The older `LifecycleTraceCandidate.csproj` is a different standalone data library whose own default `Release` configuration still compiles diagnostic types; do not confuse it with the main module Release. The [lifecycle observation review plan](docs/Lifecycle_Observation_Review_Plan.md) describes the remaining UI and event-authority boundaries.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Observation contract and assembly evidence: [Area observation evidence](docs/AreaObservationEvidence.md). The read-only lifecycle and snapshot adapters are implemented; actual startup unlocking is not.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion


## Cloud validation

The baseline workflow uses ubuntu-22.04 and its preinstalled .NET 8 SDK (latest available 8.x patch, printed and pinned per run). Missing SDK is an environment failure. Restore uses Tests/NuGet.Config with no package feeds or PackageReference. Restore, Release compilation, and executable checks are separate steps. Push to this branch, PR targeting it, or manual dispatch on this branch triggers checks with contents: read only. The baseline workflow produces no game DLL, release, or artifact.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion


## Game observation DLL

FullMapUnlockAndExpansion.csproj targets .NET Framework 3.5 with authorized ICities and UnityEngine references supplied through GameReferenceDir. It defines IUserMod, loading and area extension entry points. Eligibility and price callbacks return their input unchanged. Observation.Read returns an immutable latest callback record or null; eligibility and price are separate observations, and price has no tile identity. Values are not final eligibility after other extensions and never establish affordability. Unloading clears the current observation, but previously returned immutable objects remain readable. Observation.IsCurrent checks object identity only at the instant of the call; it neither prevents a subsequent change nor proves which city originated a callback. A delayed callback from the same provider after a city transition cannot currently be distinguished from a new-city callback. No UI or automatic polling is provided.

Main owns workflow integration. The project targets .NET Framework 3.5 and requires real authorized references. A cloud build of commit d9daec9 compiled the observation DLL on 2026-11-02; subsequent changes require their own verification. Content Manager discovery, actual callback ordering, and lifecycle behavior in the game remain unverified. Do not redistribute game reference DLLs.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Lifecycle boundary checks: Tests/LifecycleChecks.csproj compiles the production sources with minimal ICities boundary substitutes. It checks lifecycle and startup-snapshot cases as well as unchanged callback values and separated price observations. These substitutes are test-only and do not validate the real ICities ABI. Cloud commands: dotnet restore Tests/LifecycleChecks.csproj --configfile Tests/NuGet.Config; dotnet build Tests/LifecycleChecks.csproj -c Release --no-restore; dotnet Tests/bin/Release/net8.0/LifecycleChecks.dll. Run sequentially with preview checks (shared test output directory).
Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Loading and area-extension instances synchronize their own callbacks and treat release as terminal: a delayed callback on the same released instance cannot rebind or record an observation. Tests exercise synthetic concurrent release and callbacks. The actual callback order, instance reuse in other game builds, and callbacks from a still-active provider across a city change remain unverified.

The `StartupSnapshots` adapter retains the received load mode and a per-load generation. For ordinary `NewGame` only, it schedules a read-only action with `IThreading.QueueSimulationThread`; inside that action it checks the current recorded owners, readiness, app mode and the exposed pause flag, and reads all 25 coordinates twice with count checks. A reported pause enters `WaitingForResume`; a caller can explicitly request a fresh observation after resuming, with lifecycle and readiness checked again. This never retries a game write. Other failures and cancellation remain stopped. The exposed pause flag does not cover every forced-pause condition in the inspected game assembly, so a later observation does not prove the simulation is running.

Overlapping registered loading instances enter `OwnerAmbiguous` and invalidate the observation until every overlapping instance is released; a later independent instance may then register. The underlying count, issue state and quarantine remain in the main Release; the public `Observation.ReadLoadingOwnership()` diagnostic getter and its result type are compiled only with the synthetic test symbol and without either Release symbol. Even in tests they do not enumerate all instances or authenticate their origin. The inspected game wrapper may replace loading extensions without releasing the previous instances, so the ambiguous state can remain until process restart. The adapter cannot detect an older instance that has not reached registration, and same-instance delayed unload callbacks cannot be assigned to a city from their arguments. No automatic reset guesses which owner is current.

The remaining requirements for a production-quality 25-area startup unlock are tracked in [25-area release evidence](docs/25_Area_Release_Evidence.md). The Release observation path and this diagnostic work do not perform an unlock.

Old tickets, duplicate load callbacks without an intervening unload, provider replacement, cancellation, inconsistent reads and exceptions do not publish a usable result. A result is explicitly `ObservedUnverified`, with provenance, actual simulation-thread identity and full running state all marked unverified. Matching reads can still form a mixed snapshot, and `IsCurrent` only checks the adapter's latest object and received events at that instant. `PrepareNativeSubmission` deliberately returns `Unavailable` for every input. No gameplay action consumes these snapshots.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Releasing the current area provider clears its current observation, but the loaded city stays active until its loading owner unloads or releases it. A new provider can then resume observation in the same loaded city; binding a provider after city unload does not reactivate observation. These are synthetic lifecycle checks, not a guarantee about actual game callback order.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

The game-dll workflow runs preview and lifecycle contracts, then directly compiles the two listed observation sources against pinned cloud game references. It does not invoke the main project or define a Release marker, and its source list omits the separate disabled submission-interface candidate. Its diagnostic exclusion therefore depends on the listed sources and absence of the test symbol. The System.Core reference has been added but the hosted workflow has not been run for this change. Only the DLL and provenance are configured for 14-day retention; no Workshop upload or Release is configured. Signature: OpenAI-Codex.

## Isolated milestone call-site research

`Diagnostics/MatcherResearch/MilestoneCallsiteMatcher.cs` is an opt-in, disconnected research source. Its experimental symbol is excluded by either Release symbol, and neither the main project nor the hosted direct source list includes it. It does not use Harmony, rewrite IL, register a patch or unlock a tile.

The matcher checks a supported original method's identity, IL body hash and specified local metadata, then compares all 77 instructions in a neutral projection, including resolved operands and branch destinations. It rejects unknown or different shapes. `CaptureAndFind` returns a separately owned neutral snapshot; the index-only helper is internal. Matching does not verify the complete game assembly, other methods, the active runtime wrapper or a city.

A future adapter must independently and faithfully map each branch label and exception marker, verify local-variable identity and type, and bind any change to the same unchanged source stream. The neutral snapshot's Boolean markers do not preserve label identities or complete exception blocks. No Harmony adapter, product writer, hosted artifact or game behavior has been verified by this research.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

### Later Harmony projection experiment

The later HarmonyMatcherAdapter is a separate, guarded projection experiment. It requires both FULLMAP_MATCHER_EXPERIMENT and FULLMAP_HARMONY_ADAPTER_EXPERIMENT and is excluded by either RELEASE or FULLMAP_RELEASE. It does not register a Harmony patch, rewrite a game method or unlock an area. This paragraph updates the earlier statement that no adapter had yet been examined.

In an isolated Windows .NET Framework process, Harmony 2.4.3.0 was used on an owned fake method, and a separate reflection-only fixture projected 77 instructions from the authorized reference method. Twenty targeted checks passed; the fake-method compiler emitted 13 CS1685 duplicate predefined-type warnings. Six direct compiler and PE metadata controls confirmed that the adapter and Harmony assembly reference appeared together only in the double-experiment case. These checks are not a main-project build, Unity or Mono compatibility check, or game validation.

The caller supplies the expected LocalBuilder and controls the source stream. A test demonstrates that supplying matching foreign locals and labels can still be accepted; their generator ownership is not authenticated. Capturing a mutable stream is not atomic, and the returned neutral snapshot does not preserve label identity or bind a later rewrite to that stream. These limits block using the matcher result alone as authority for a live patch.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion
### Scoped fake-method experiment

The separate, test-only ScopedFakeTranspiler and ScopedFakeTranspilerChecks sources are guarded by FULLMAP_SCOPED_FAKE_EXPERIMENT and excluded by either release symbol. Their fixed four-instruction profile belongs to an owned fake method, not the game-reference matcher. In an isolated Windows Framework process using Harmony 2.4.3.0, the test retained the original fake gate result except for one permitted false-to-true decision; an original true result consumed the ticket. Same-thread nested fake calls with the same invocation, a different scope, a null scope or a null argument were exercised, with the outer scope stopping instead of overriding the original false result.

An intentionally undrained fake worker produced UNKNOWN and skipped unpatch in a separate short-lived process; this does not prove that an unknown patch operation was rolled back. Patch or unpatch failure remains unknown, and removing a patch by method does not establish preservation of another owner using that same patch method. The test did not establish producer atomicity, live game behavior or general Harmony interoperability.

The fake guard uses thread-local state. A direct call on another thread with a null or different scope may not notify the outer scope, so a cross-thread reentry can remain undetected; this was reproduced in a controlled fake process, not in the game. Already returned decisions cannot be revoked. Neither the fake experiment nor the reference-only matcher authorizes a real game patch or a 25-area writer.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion
The reflection-only reference projection checks require game reference DLLs lawfully supplied by the user and their authorized dependencies. They are not included in this repository; do not commit, upload or redistribute game reference DLLs.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion