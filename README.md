# Realistic City Time

A separate [relative event trace study](Diagnostics/RelativeEventTrace/README.md) records bounded synthetic callback markers and session-local pseudonymous thread tags. It is disabled by default, excluded from the normal game build, and not connected to the game. Its observation order does not establish native write order, actual frame identity, CPU parallelism, or save consistency. Signature: OpenAI-Codex.

Source-available module in early development for Cities: Skylines 1. No game functionality has been accepted in gameplay. The local candidate observes time and offers a clearly temporary multiplier input, but does not save that value or change the game clock. Its source has been compiled against project reference copies and exercised with in-memory game fixtures; the resulting DLL has not been run in the game.

Latest verified baseline: commit `645a3e93cf983c48d7898915a561c5a7e4f3710c` passed [cloud build and model/observation checks](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41291599199), including 40 arithmetic checks, with .NET SDK 8.0.427 and zero build warnings/errors. Statements below about the original uncommitted batch describe its earlier preparation stage. They do not supersede that baseline evidence.

The original local [observation-only game assembly candidate](Game/README.md) added game entry classes and a cloud compiler entry script, with no workflow change. Its CLR-compatible observation code is deliberately separate from the BigInteger pure model. Later local reference compilation and temporary input are described below; no gameplay acceptance or clock control is claimed.

Update: the observation build baseline is now `4c3b1e2949ddc75fc7c50902543a8f56c46f14b2` ([run 41382255222](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41382255222)); the preceding candidate description is historical. The latest pending batch adds [control decisions and one-shot dry-run boundaries](Game/Control-Decision.md), not a clock writer or a new release. Missing precision or unverified native ownership/boundary facts prevent a candidate; even valid candidates always report `CanWrite=false`.

The local test harness now verifies that competing dry-run boundaries cannot consume observations for the same city session. This safeguards the preview state only; it neither detects other mods nor controls the game clock. Signature: OpenAI-Codex_city_time_development.

A separate <!-- [calendar IL matcher](Diagnostics/CalendarInterception/README.md) -->calendar IL matcher ~~(source retained locally and excluded from this backup)~~ ([source](Diagnostics/CalendarInterception/README.md)) checks a single reference-derived instruction shape using actual reflection-member identities supplied by its caller. It is not wired into the game build, Harmony or a clock writer. Its 22, later extended to 23 and then 24, in-memory checks use synthetic members and do not authenticate instruction provenance or validate game behavior. Signature: OpenAI-Codex.

A separate <!-- [Harmony fake-target study](Diagnostics/CalendarInterception/HarmonyFixture/README.md) -->Harmony fake-target study ~~(source retained locally and excluded from this backup)~~ ([source](Diagnostics/CalendarInterception/HarmonyFixture/README.md)) exercised a passive interception in an isolated x86 .NET Framework process. It does not implement 1x time or validate game/Mono behavior; the local compiler reported 13 warnings. Signature: OpenAI-Codex.

A separate [transactional fake-host calendar candidate](Diagnostics/CalendarInterception/TransactionalCandidate/README.md) can update complete fake clock snapshots from supplied monotonic ticks. It is disabled by default, excluded from normal game builds, and does not patch or write the game clock. Its atomic publication, owner and save conditions are fake-host requirements, not verified game guarantees. Signature: OpenAI-Codex.

Repository branch: [Realistic_City_Time](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_City_Time).

## Confirmed direction

**Decision evidence correction, 2026-11-13:** Existing project notes record normal 1x as one real minute per game minute already confirmed; this review did not inspect the original user message. The next historical clarification was too broad if read as denying that record. Under uninterrupted eligible 1:1 time, 24 real hours per 24 game hours follows arithmetically; Main still treats the complete 24-hour and day/night design as provisional. The phase relationship, anchor and interruption policy are not independently confirmed. Signature: OpenAI-Codex.

A separate [controlled read-only host](Diagnostics/ControlledReadOnlyHost/README.md) now connects the existing bounded recorder to isolated game entry sources. It starts without sampling and is not included in the normal game assembly. Offline fakes and project-reference compilation do not establish in-game UI access, actual loading order, city identity or native lookup behavior; no game execution is authorized by this update. Signature: OpenAI-Codex.

~~**Decision status clarification, 2026-11-13:** Do not treat the historical elapsed-time wording below as final approval of the proposed 1x = 24 eligible real hours per simulated day or its day/night coupling. Main's 24-hour 1x and 2.4-hour 10x figures remain working assumptions pending explicit user confirmation. No clock rate is applied by the current game entry. Signature: OpenAI-Codex.~~

**Current calendar clarification:** The 1x calendar requirement is one simulated minute per real minute. During an uninterrupted eligible 1:1 interval, 24 simulated hours therefore correspond to 24 real hours. Day/night coupling, phase anchoring, pause eligibility and catch-up remain separate decisions. The current game entry does not apply a clock rate. Signature: OpenAI-Codex.

- At 1x, one minute of real elapsed time corresponds to one minute of game time.
- The default speed is 1x and the maximum is 10x. Decimal multiplier entry is approved; its input precision remains undecided. Speeds below 1x are not included.
- Intended replacement targets: [Real Time 2.8 (3059406297)](https://steamcommunity.com/sharedfiles/filedetails/?id=3059406297) and [More Simulation Speed Options 2 (3343115359)](https://steamcommunity.com/sharedfiles/filedetails/?id=3343115359). These are external references, not this module's Workshop IDs or a claim of feature parity.

The scope of citizen schedules, aging, education, population, economy, and building operations remains undecided. Replacement intent does not approve every behavior of either external mod. Clock integration, pause behavior, loading, day transitions, and save compatibility require further design. The 1:1 baseline does not imply synchronization with the computer clock, date, or time zone, or offline catch-up.

## Validation and distribution

The foundation workflow checks documentation and licensing. A separate branch-scoped hosted workflow runs dependency-free pure-model checks. These do not establish game integration, performance, or gameplay acceptance. No local runner or workflow is required.

### Pending hosted validation wiring

`clock-model.yml` uses `ubuntu-latest` and the preinstalled stable .NET 8 SDK, selected by `global.json` (8.0.100 minimum feature baseline; latest installed 8.0 feature band, no major-version or preview fallback). It does not install an SDK or external packages. If that SDK or its framework packs are missing, the environment must be repaired under separate authorization; such a failure is not evidence of failing model behavior. The JSON file also carries attribution metadata for OpenAI-Codex_Realistic_City_Time.

The pending workflow runs these commands separately on GitHub-hosted infrastructure, never on the local PC:

```sh
dotnet --version
dotnet restore tests/ClockModel.Tests/ClockModel.Tests.csproj --configfile tests/ClockModel.Tests/NuGet.Config
dotnet build tests/ClockModel.Tests/ClockModel.Tests.csproj --configuration Release --no-restore
dotnet run --project tests/ClockModel.Tests/ClockModel.Tests.csproj --configuration Release --no-build --no-restore
```

The current test project explicitly includes arithmetic, observation, lifecycle, ownership, continuity, portable-scaler, and temporary-input sources and checks. It compiles the game entry against test-only API fakes, not game DLLs; `Program.Main` runs the suites sequentially and failures exit nonzero. This job does not publish an artifact. The foundation documentation job is separate and cannot stand in for these checks. The 2026-11-13 local batches were checked with isolated output paths; their hosted workflow has not run.

Triggers are push to `Realistic_City_Time`, pull requests targeting that branch, and manual dispatch constrained by the job condition to that branch. Permissions are only `contents: read`; checkout uses the previously verified fixed v4.2.2 commit with credential persistence disabled. ~~Both known branch workflows are check-only: their definitions do not create a public prerelease on push.~~ The current branch has three workflows; the observation-DLL workflow can upload an Actions artifact but does not create a Release. The three inspected definitions contain no deployment step. External automation and branch protection remain unverified. Signature: OpenAI-Codex. Repository-level external automation was not queried in this local-only review.

Historically, the initial tests were committed before implementation and their first public push was blocked. The later `645a3e9` cloud run succeeded, but did not provide a historical red/green run for that first commit. The 2026-11-13 changes remain uncommitted and have isolated local synthetic red/green evidence only. The model checks target .NET 8 without external packages; this does not select the game's target framework.

## Pure model contract

`ClockModel` accepts an explicit decimal precision policy (0–28), with no default product precision. Decimal entry is approved; the final UI precision, step, localization, interruption behavior, and per-city activation remain undecided. Precision zero exists for policy validation, not as the selected product setting.

`ParseMultiplier` accepts unsigned invariant decimal text within 1–10 inclusive. It rejects signs, whitespace, comma separators, exponents, NaN, infinity, excess effective fractional digits, and values that cannot be represented exactly as a decimal rather than rounding them. The decimal representation ceiling can be stricter than the supplied precision policy. Trailing fractional zeros do not consume precision. This is an internal parser contract, not a finalized localized UI.

`Advance` consumes eligible elapsed `TimeSpan` ticks (10,000,000 per second), validates the multiplier, and returns whole desired game ticks. Integer numerator/remainder arithmetic via the framework's `BigInteger` preserves fractional ticks across calls and multiplier changes. Negative input, invalid multipliers, and output overflow leave the remainder unchanged. `Reset` clears only fractional ticks. Callers must serialize access; the arithmetic model has no clock, session, pause, save, UI, or game adapter and cannot establish real-world simulation speed.

## Read-only observation contract

**Lifecycle scope clarification, 2026-11-13:** `OnLevelUnloading` and the start of `OnLevelLoaded` check the loader instance, not the originating city generation. A delayed unload from an earlier city on the same loader can close a newer observation; a stale completion received while completion is awaited can pass stable flags without authenticating its city. Only delayed failure cleanup through `TryEndLoadingOwner` compares the exact generation token. Actual game ordering is unverified. Direct internal `Begin()` is refused while a loading owner is registered; this does not prove city identity or game-wide ownership. Native writes remain disabled. Signature: OpenAI-Codex.

**Lifecycle update, 2026-11-13:** A newly reserved observation generation remains inactive until the existing second loading-stability check succeeds and the same local owner, lifecycle revision and token are confirmed. Isolated fake barriers reproduced and covered the prior early-publication window. This only narrows local observation exposure; actual callback ordering, city identity, loading-flag ABA, other writers and gameplay remain unverified. No native time writer is enabled. Signature: OpenAI-Codex.

The local loading candidate registers each loading-extension instance, rejects overlapping registered owners, and checks the reference loading flags before starting and around capture. A released instance cannot re-register, and an unexpected duplicate completion without an unload fails closed. These checks reduce identified stale-instance cases but do not authenticate a game callback's city: the reference interface has no city epoch, the same extension may serve multiple cities, and the flags are not atomic with this module's state. Plugin re-enumeration may replace an old extension without releasing it; the conservative conflict latch can therefore leave observation disabled until the relevant instances are released and a fresh instance is registered. This availability tradeoff requires review. Signature: OpenAI-Codex.

The candidate also exposes a temporary multiplier panel through the reference `OnSettingsUI` convention. It accepts the provisional two-decimal policy on an explicit Apply action, clearly says the value is not saved and does not change game time, and uses only a process-memory store. UI event order, rendering, accessibility, and actual game loading remain unverified. The separate [settings decision](Game/Settings-Decision.md) explains why real persistence is not connected. Signature: OpenAI-Codex.

**Diagnostic update, 2026-11-13:** The next paragraph is a historical snapshot of the earlier source. The detached candidate now includes an internal manual controller and fake-tested UI-helper view, still default-disabled and excluded from the inspected game build. Its local revocation is not a verified game unload, city identity, UI-thread or release integration; previously delivered UI/host copies cannot be recalled. The existing game observer still reads an absolute game date. Signature: OpenAI-Codex.

A separate [diagnostic candidate](Diagnostics/README.md) is disabled and disconnected from game callbacks. Fake checks exercise bounded relative frame and day/night display observations; the normal game assembly excludes the candidate. It has no player-visible control or export and does not read absolute dates or save contents. The existing observer still reads a game date. Signature: OpenAI-Codex.

On 2026-11-13, isolated synthetic checks exposed and covered four input gaps: a pause with no tick can hide between two unpaused snapshots, a changed multiplier cannot be applied retrospectively to the entire interval, missed samples cannot be covered by evidence for only a later interval, and an invalid multiplier must not establish a baseline. `ClockInputSession` now requires an explicit `IntervalUninterrupted=true` assertion for the whole interval, including an unchanged multiplier throughout; equal endpoint states or equal endpoint multipliers are not proof. Legacy observations without that assertion fail closed with `UnverifiedInterval`. An observed multiplier transition returns `MultiplierTransition`, and a sequence gap returns `SampleGap`; each reanchors the preview without producing ticks for the ambiguous interval.

Contiguous sequence numbers only rule out detected missing observations; they do not prove that a game pause, resume, or temporary multiplier change could not occur without a callback. A future source must independently establish continuity. Synthetic checks do not supply that game evidence. The local .NET 8 test harness and a separate CLR 2.0 reference compilation of the cache/control-boundary subset passed; the latter is not a complete game assembly or gameplay test. Signature: OpenAI-Codex_Realistic_City_Time.

The assertion is a contract supplied by a future verified source, not evidence generated by the current game observer. No existing callback establishes interval continuity, so the current game assembly cannot safely enable time advancement through this mechanism. The preview's reanchoring does not decide the product's eventual interruption or catch-up policy. The same local batch makes dry-run candidates require freshness evidence at both evaluation and consumption; the legacy overloads provide no such assertion and fail closed. The synthetic tests use explicit fake assertions and do not validate native thread or pause behavior. Signature: OpenAI-Codex_Realistic_City_Time.

`PortableClockScaler` is a local, game-compatible-source candidate for exact tick conversion without `BigInteger`. Its precision is mandatory input, with no selected product default. It is not connected to the observer, pause handling, a native clock writer, or save data. An isolated .NET 8 harness compared it with the existing `BigInteger` model, including high-precision and overflow cases; a separate subset compilation referenced the project's CLR 2.0 `mscorlib` copy only. These checks do not verify the complete game assembly, the game's Mono runtime, or gameplay. Signature: OpenAI-Codex_Realistic_City_Time.

The later `SyntheticClockCoordinator` is linked only into the synthetic test project, not the game assembly or observer. It combines the portable scaler, the existing cache and preview ownership with a provisional 1–10 input policy of at most two literal fractional digits. The provisional policy rejects excess digits rather than rounding; unlike the general pure-model parser, it also rejects extra trailing zero digits. The coordinator accumulates a virtual date, discards unverified elapsed intervals, and requires fresh caller assertions before accepting a local plan. Those assertions are fake inputs, and every plan keeps `CanWrite=false`. An isolated .NET Framework synthetic harness compiled source copies and passed its cache, ownership and integration checks. A separate partial compile against the project `mscorlib 2.0.0.0` reference also succeeded with test-only symbols. Neither check includes `GameMod.cs` or the complete game assembly. The two-decimal setting is Main's adjustable working default, not a confirmed final UI or permanent user decision. Signature: OpenAI-Codex.

`ClockObservation` is an immutable data contract for a future verified source. Nullable game time, pause state, and monotonic timestamp explicitly represent unknown data. The timestamp must already use TimeSpan tick units; it is not a raw Stopwatch counter. The observation-only `GameMod.cs` already reads game time, pause state, and frame fields into a separate cache, but it does not feed `ClockObservation` or prove callback ordering, continuity, or actual gameplay behavior.

`ClockInputSession` bridges supplied observations to the pure model on one serialized caller thread. `BeginSession` creates a new generation and resets fractional state; `EndSession` invalidates callbacks. Old-generation, duplicate, negative-sequence, and out-of-order samples cannot modify the active anchor. The first complete sample only establishes a baseline. Only a `Ready` result has `DesiredGameTicks`; all other results retain an unknown/null output, not a fabricated zero or measured game rate.

Unknown values and pause invalidate the elapsed-time anchor while retaining previously accepted fractional ticks; the next complete unpaused sample establishes a new baseline, so the missing interval is not counted. Explicit discontinuity, decreasing timestamps/game dates, invalid conversion, and overflow clear both anchor and fractional state. Invalid intervals are never retried as accumulated time. A generation change must reach this session before load/unload or city replacement; the existing game observation lifecycle callbacks are not connected to this independent session.

This is a conservative input eligibility contract, not a decision about game catch-up, automatic activation, UI defaults, or a maximum interruption threshold. A large forward gap is not detected automatically; a future verified source must flag discontinuities, and product interruption policy remains undecided. Forward day transitions are only supplied dates, never date writes or new-day event dispatches.

The pure model has no game-reference dependency; the separate observation-only game source uses project reference copies. This local batch is not a DLL release or Workshop upload. Future implementation and distribution require separate approval. Gameplay acceptance requires a Steam Workshop subscribed build.

## License and attribution

Code and workflow: [PolyForm Noncommercial License 1.0.0](LICENSE). Original public documentation/content: [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/). Copyright Kingsley. This project is source-available.

The LICENSE file preserves the official text byte for byte. Official source: [PolyForm website source at caa273a0f67aba05e8cc06f7e869469918f53617](https://github.com/polyformproject/polyformproject.org/blob/caa273a0f67aba05e8cc06f7e869469918f53617/licenses/noncommercial/1.0.0.txt), also published on the [official license page](https://polyformproject.org/licenses/noncommercial/1.0.0). Attribution and this signature are kept outside the license terms.

Signature: OpenAI-Codex_Responsible: Realistic_City_Time

2026-11-13 update signature: OpenAI-Codex

Local pure-model implementation signature: OpenAI-Codex_Realistic_City_Time

Observation contract batch signature: OpenAI-Codex_Realistic_City_Time

The game-dll workflow produced an observation-only DLL and provenance artifact at the historical `4c3b1e2` baseline; it does not enable time control. The 2026-11-13 local changes have not been included in a cloud game assembly, Release, or Workshop submission. The artifact's current availability was not checked in this local batch. Signatures: OpenAI-Codex; update OpenAI-Codex_Realistic_City_Time.
