# Realistic Urban Population

Planning-stage Cities: Skylines 1 module for more realistic urban population simulation. No gameplay implementation or DLL is available.

## Project links

- Repository: https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_Urban_Population
- Modpack website: https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/

## Automation and delivery

The branch foundation workflow runs on pushes and pull requests targeting `Realistic_Urban_Population`, or by manual dispatch. It validates required non-empty project documents and licensing on GitHub-hosted Ubuntu. It does not compile or publish a mod.

Game delivery requires compilation against authorized ICities references and the .NET Framework 3.5 reference framework, followed by subscribed-game acceptance. The inactive entry/loading source and a separate game project are now prepared locally; game-data features may require further game assemblies. Reference assembly distribution rights and acquisition are unresolved. Once ready, this module will publish its DLL and required individual files directly through its own GitHub Releases, without ZIP packaging. Automatic release publication remains disabled.

## License

Code and original project documentation/content are source-available under PolyForm Noncommercial License 1.0.0; see LICENSE. Copyright Kingsley. Official text: https://polyformproject.org/licenses/noncommercial/1.0.0

Maintainer signature: OpenAI-Codex_Responsible: Realistic_Urban_Population

## Pure demand-model development

The standalone policy and city-session model has no game references, game field writes, UI or save adapter. It is not a playable mod or a downloadable game DLL.

The confirmed first-version policy is per-category Native or Fixed Target (0–100), with all categories Native by default. Industrial / Workplace is the temporary third-category name; office coupling is unverified. Slider step, initial edit target, save behavior and automatic activation after load are not selected by this model. Native policies have no numeric target.

Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population

### Model boundaries and cloud checks

- `Source/Demand/DemandPolicy.cs`: immutable category policies; Native passes the current input unchanged; Fixed Target accepts only an explicitly provided integer from 0 through 100. Missing or invalid policies cannot silently become a fixed target.
- `Source/Demand/DemandSession.cs`: one lock covers city-epoch validation, load readiness, permission changes, atomic settings publication, restoration and shutdown. Stale city events cannot affect a new city. Unknown/conflicting permission revokes activation; returning to Allowed does not automatically reactivate it.
- `Checks/`: ten check groups compile the same source with C# 3 syntax on .NET 8, including controlled transition orders and concurrent snapshot reads. This does not prove .NET Framework 3.5 runtime compatibility or real game-thread behavior.
- The `Urban Population Demand Model` workflow runs these checks on GitHub-hosted Ubuntu for this branch, pull requests targeting it, or manual dispatch. The cloud-only command is `dotnet run --project Checks/DemandChecks.csproj --configuration Release`. No test or build is run on the local PC.

`TryApply` is an explicit model command, not a decision about whether a future save adapter will automatically apply settings after loading. The model does not invent a slider step or initial numeric edit value. Restore stops this model's override and passes subsequent inputs; it does not restore old city demand, undo growth or guarantee an immediate display update. Industrial / Workplace and office coupling still require current game-pipeline evidence before any adapter is implemented.

The test-first commit deliberately fails with unimplemented contracts; subsequent checks must be assessed by their exact commit SHA. Passing pure checks must not be described as a successful game build or gameplay acceptance. No automatic release publication or Workshop operation is configured.

Signature: OpenAI-Codex_Responsible: Realistic_Urban_Population

## Local read-only snapshot preparation (not cloud-validated)

`Source/Demand/DemandSnapshotReader.cs` adds an injectable observation boundary. It does not contain a game source, game hook, reflection access or a `ZoneManager` field reader. The module checkout has no game reference assembly copy to verify such a reader. `GameSourceVerified` and `OfficeCouplingKnown` remain false even when an injected fixture supplies values.

The boundary carries the caller's source label, city epoch, session revision and observation sequence. It preserves raw residential/commercial/workplace integers without applying fixed-target clamping. A source failure, city/revision change or invalidation removes usable cached values; late completions cannot replace a newer source binding or capture request. Reading never calls `TryApply`, changes the session, or writes demand.

Callers must use `ReadLatest` to revalidate a retained observation. A previously returned immutable snapshot is a historical capture, not a live freshness or simulation-frame guarantee. No automatic time-to-live, sampling interval, game-thread identity or industrial/office separation is invented. A future game source must establish its actual assembly/version, instance lifecycle and safe simulation-thread read context before binding.

`Checks/SnapshotChecks.cs` contains synthetic source cases for this boundary. They are registered in the check executable but have not been compiled or executed in this local-only batch. The previous ten-group cloud result applies only to commit `d61181022cfd3b703aa5df6ece04d97dc6112517`, not to these new changes. Commit, push, cloud execution and publication require a subsequent authorized step; no Release or Workshop operation is part of this batch.

Signature: OpenAI-Codex_Realistic_Urban_Population

## Inactive game DLL source batch (2026-11-02; not built)

`Source/RealisticUrbanPopulation.csproj` is a separate classic MSBuild library targeting .NET Framework 3.5, C# 3 and AnyCPU. Its output is `bin/Game/Development/RealisticUrbanPopulation.dll` (or the Release configuration). It requires an explicitly supplied, authorized reference directory containing `ICities.dll`; reference copying is disabled. It performs no original-game discovery, game-directory deployment, packaging, dependency installation or upload.

`Source/Game/RealisticUrbanPopulationMod.cs` provides the `IUserMod` name/description and a `LoadingExtensionBase` entry. Only NewGame/LoadGame start an inactive in-memory lifecycle; unloading/release close it. It installs no demand extension or patch, does not read/write game demand fields and grants no model control permission. Metadata explicitly describes the inactive foundation. The ICities signatures and true game loading remain pending authorized cloud compilation and game acceptance.

`Source/Runtime/PopulationLifecycle.cs` and the synthetic lifecycle/error-boundary checks are independent of ICities. The pure check project includes Runtime sources, but excludes Game entry sources; its success alone cannot validate the game DLL. Snapshot sources receive the expected city epoch and must validate their instance lifetime inside a verified simulation context. Pre/post checks only reject stale results; a city can close between the pre-check and an already-started source read. No zero-read-after-concurrent-unload guarantee is made.

For Main's future authorized Windows cloud build, supply .NET Framework 3.5 reference assemblies and an authorized ICities reference copy, then invoke:

```text
MSBuild.exe Source/RealisticUrbanPopulation.csproj /t:Build /p:Configuration=Development /p:GameManagedDir=<authorized-reference-copy>
```

Do not execute this command locally. Missing `ICities.dll` is an explicit error; the project has no substitute interface or modern-framework fallback. The workflow is owned by Main and was not edited in this batch. Cloud build, publication and a first private Workshop item require subsequent authorization and a real validated DLL. No new Workshop ID is assigned here.

Signature: OpenAI-Codex_Realistic_Urban_Population

The pending population-game-dll workflow runs demand, snapshot and lifecycle checks and compiles the inactive game foundation against pinned cloud references. Only the DLL and build provenance are retained for 14 days; population and RCI writes remain disabled. No Release or Workshop upload is performed. Signature: OpenAI-Codex.
