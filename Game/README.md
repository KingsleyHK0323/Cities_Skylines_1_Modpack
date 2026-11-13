# Observation-only game assembly candidate

**Build channel selection:** The existing cloud-only compiler entry accepts the optional, case-sensitive Channel values Unspecified, Development, and Release. Omitting the parameter is equivalent to Unspecified. Unspecified adds no channel symbol; Development adds RCT_CHANNEL_DEVELOPMENT; Release adds RCT_CHANNEL_RELEASE. Unknown or incorrectly cased values are rejected before the output directory is created. Conflicting channel symbols are rejected by the game source at compile time.

The fake test executable requires exactly --expect-channel followed by Unspecified, Development, or Release. Supply these arguments after -- when using dotnet run. The hosted workflow definitions explicitly expect Unspecified for their current unmarked fake build; that check is separate from the subsequent game DLL compilation and does not verify that DLL. Signature: OpenAI-Codex.

The script is reserved for approved GitHub-hosted builds. Its existing GITHUB_ACTIONS environment-variable check does not authenticate the host. Signature: OpenAI-Codex.

The mod Name remains Realistic City Time. Unspecified and Development preserve the existing development description. Only Release selects "Observation-only foundation. Does not change game time or speed." All three choices retain the existing observation-only behavior and do not enable time writes. The internal GameBuildChannel value is a build label, not proof of a trusted installation, release provenance, or game compatibility.

In an approved GitHub-hosted build context, the existing command can explicitly select the Release channel as follows:

~~~powershell
./Game/Build-GameAssembly.ps1 -CompilerPath $compilerPath -ReferenceDirectory $referenceDirectory -OutputDirectory $freshOutputDirectory -Channel Release
~~~

Use -Channel Development for the explicit development channel, or omit -Channel for the existing default. Selecting a channel does not run a workflow, publish a release, or establish gameplay acceptance. Local fake tests and the isolated channel-selection fragment do not establish execution of the complete cloud-only script. Signature: OpenAI-Codex.

**Decision evidence correction, 2026-11-13:** Project notes record normal 1x as one real minute per game minute already confirmed, although the original user message was not re-read in this review. The following clarification was too broad if it is understood to erase that record. The full 24-hour/day-night design, phase anchor and interruption policy remain subject to Main's provisional status and further decisions. Signature: OpenAI-Codex.

A separate [controlled diagnostic host](../Diagnostics/ControlledReadOnlyHost/README.md) has its own source list and does not include this directory's `GameMod.cs`. Its fake checks and offline reference compilation do not establish real game behavior or authorize installation. Signature: OpenAI-Codex.

**Decision status clarification, 2026-11-13:** The historical phrase “approved 1:1 clock” below does not establish final approval of the proposed 24-real-hour 1x day or of calendar/day-night coupling. These figures remain Main's working assumptions pending explicit user confirmation. Signature: OpenAI-Codex.

The game's `IUserMod`, loading, and threading entry classes have an observation-build baseline at commit `4c3b1e2949ddc75fc7c50902543a8f56c46f14b2` ([cloud run](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41382255222)). Game loading and gameplay acceptance are still pending. Its description explicitly states that it does not change game time or speed. Enabling it does not implement the approved 1:1 clock or decimal acceleration.

The local [control decision batch](Control-Decision.md) adds a dry-run boundary with native writes hard-disabled. A later isolated local build compiled the current game candidate sources against the project's approved reference copies; the DLL was not executed and is not a release. The game's Mono runtime and gameplay remain unverified. Main alone owns workflow changes and subsequent publication approval.

The later `SyntheticClockCoordinator.cs` is included only in the synthetic test project, not in `Build-GameAssembly.ps1` or the game entry classes. Source copies and its integration cases passed an isolated .NET Framework harness; the same four-source subset also compiled against the project `mscorlib 2.0.0.0` reference with test-only symbols. This is not a complete game build, Mono execution or game test. Test-only lock access is guarded by `SYNTHETIC_CLOCK_TESTS` and is absent from the normal game-source build. No synthetic ownership assertion proves game-wide ownership or permits a clock write. Signature: OpenAI-Codex.

The reference copies were inspected as PE metadata and IL, without loading or executing game assemblies. They expose `IUserMod.Name/Description`, `LoadingExtensionBase.OnCreated/OnLevelLoaded/OnLevelUnloading/OnReleased`, `ThreadingExtensionBase.OnAfterSimulationTick`, `SimulationManager.m_currentGameTime`, `m_currentFrameIndex`, and the two pause getters. The inspected options panel finds `OnSettingsUI` by reflection. Reference identity is not a verified installed game version or proof of all callback ordering.

The game reference metadata uses CLR 2.0 and mscorlib 2.0.0.0. The pending game candidate includes the observer, dry-run boundary, portable scaler, and temporary input sources; it excludes the synthetic coordinator, game API fakes, and BigInteger-based pure model. The locally compiled candidate references, rather than embeds, the project copies. Compilation does not verify game Mono execution, game loading, or actual UI behavior. Signature: OpenAI-Codex.

## Cached observations

**Lifecycle scope clarification, 2026-11-13:** `OnLevelUnloading` and the initial cleanup in `OnLevelLoaded` call `EndLoadingOwner(this)`, which checks sole loader identity and has no originating-generation parameter. A late unload for G1 after G2 starts on the same loader can close G2, reducing availability; a late completion without a preceding unload can also close G2. If completion is awaited and flags appear stable, a stale completion can instead activate a new local generation without proving which city emitted it. `TryEndLoadingOwner` compares an exact token only for delayed failure cleanup; `OnReleased` uses registered loader identity, not a generation token. These are source-level scenarios, not verified game ordering. Signature: OpenAI-Codex.

**Direct Begin update:** The subsequent historical paragraph describes the earlier implementation. The current internal `Begin()` cancels an existing pending reservation, and refuses a new generation while any loading owner is registered or an owner conflict remains. A refused call leaves an already active owner unchanged; a caller must handle its zero result. With no registered owner or conflict, the prior direct `Begin()` behavior remains. This local guard neither authenticates a city nor protects against arbitrary same-process calls. Signature: OpenAI-Codex.

The legacy internal `Begin()` is not called by the current game entry. While a loading generation is pending, `Begin()` cancels that pending reservation and returns zero; outside that case it does not independently authenticate registered loading owners or resolve conflicts. A second direct call can therefore start a new generation after the pending reservation is canceled. It is not a game-wide ownership boundary. Signature: OpenAI-Codex.

**Lifecycle update, 2026-11-13:** A reserved local loading generation is not active or publishable before the second loading-stability check returns successfully and matching ownership, lifecycle revision and token are confirmed. Fake barriers exercise successful activation, changed flags, unload, release, competing and replacement owners, duplicate completion and a failed check. This does not prove game callback ordering, city identity, absence of loading-flag ABA or a native write boundary. Signature: OpenAI-Codex.

Only a registered, unambiguous loading instance in an active gameplay mode can enable capture. Matching unload/release invalidates its state; conflicting registered instances fail closed. Capture reads on `OnAfterSimulationTick` only while reference loading flags appear stable; failed or unstable reads replace values with unknowns. A generation token prevents a sample that began before replacement from publishing into the new generation. The public callbacks do not carry a city identity, so same-instance late callbacks and flag races remain unverified. Returned snapshots are immutable and internal to this assembly.

A cached sample is not necessarily fresh: paused games may not receive simulation ticks. The temporary UI does not persist a value or connect it to elapsed-time inference or model advancement. Stopwatch counter units are explicitly normalized into TimeSpan ticks before caching. These values are observations, not applied game time. Runtime callback ordering, threading, pause coverage, and save/load behavior remain unverified.

Within one cache instance, the first valid dry-run consumer owns preview consumption until unloading or a new city generation; competing boundaries are refused. This is not a game-wide ownership mechanism and does not detect other clock writers. Signature: OpenAI-Codex_city_time_development.

**Diagnostic update, 2026-11-13:** The next paragraph describes the prior candidate. The detached source now includes internal manual controls and a fake-tested UI-helper view, without game callback registration or inclusion in this assembly. Its local revocation does not establish actual unload, city identity or Unity UI-thread behavior; previously delivered UI/host copies cannot be recalled. Signature: OpenAI-Codex.

The independent [diagnostic candidate](../Diagnostics/README.md) is excluded from this game assembly. It is disabled, has no automatic callback registration or visible activation/export, and is limited to relative frame and day/night display observations in the fake harness. It does not read an absolute date or save content; this restriction does not apply to the existing game observer described above. Signature: OpenAI-Codex.

## Main-owned cloud integration checklist

No workflow is changed by this batch. Main must separately approve and wire:

1. Obtain the already approved reference copies through the existing protected mechanism; do not upload or log their contents.
2. Select the approved hosted compiler's `csc.dll`, provide a fresh output directory, and invoke the following cloud-only script:

   ```powershell
   ./Game/Build-GameAssembly.ps1 -CompilerPath $compilerPath -ReferenceDirectory $referenceDirectory -OutputDirectory $freshOutputDirectory
   ```

3. The script passes `/noconfig /nostdlib+ /target:library /langversion:7.3` and explicit reference paths to Roslyn. It uses no game project restore, external package, SDK installation, reference copying, release upload, or Workshop operation. It refuses an existing DLL output. Script syntax validation alone does not establish compilation.
4. Run the existing .NET 8 harness, including the actual `GameClockState.cs` and `GameMod.cs` against test-only API fakes. Those cases do not prove native callback integration. Separately compile and inspect `RealisticCityTime.dll` for the game entry classes, no forbidden clock writes, and intended assembly references.
5. Only after approved cloud build and distribution can a Steam Workshop subscribed build establish game loading/observation acceptance. No Workshop identity or distribution is configured here.

Remaining product gates include decimal input precision, interruption/catch-up policy, per-city activation, and integration of the portable scaler with a verified game-time source. The observer never sets date offsets, simulation speed, population schedules, or aging.

Signature: OpenAI-Codex_Realistic_City_Time

2026-11-13 update signature: OpenAI-Codex
