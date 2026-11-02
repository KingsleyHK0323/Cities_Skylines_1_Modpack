# Realistic Border Control

Planned Cities: Skylines 1 module for border and entry-point controls. This branch includes a pure passenger quota preview model; no game functionality or playable release exists yet.

## Scope

Future investigation may cover passenger movement through city borders, airports, harbors, and railway stations. These are goals, not implemented features.

## Validation and delivery

The current GitHub-hosted workflow checks required documentation, then compiles and executes dependency-free .NET 8 model checks. This target is a cloud model harness, not the game mod target. A separate game-shell project is now defined below; its real game-reference compilation and loading are not yet verified.

## Inactive game shell source

<!-- Signature: OpenAI-Codex -->

`Source/RealisticBorderControl.Game.csproj` targets .NET Framework 3.5 and explicitly compiles only these files:

- `Source/Game/BorderControlMod.cs`: public ICities `IUserMod` identity and enable/disable callbacks.
- `Source/Game/BorderControlLifecycle.cs`: internal enabled/city-loaded flags, with idempotent cleanup.
- `Source/Game/BorderControlLoading.cs`: ICities loading extension; only NewGame and LoadGame enter the city-loaded state. Editors and unknown modes stay inactive.

This shell does not include the .NET 8 quota or supplied-observation model, synthetic test interfaces, Harmony patches, game-manager access, logging, callbacks, or background work. It does not read passengers or change traffic. Its description explicitly reports inactive functionality. Unloading/releasing clears city state; disabling also clears enabled state. Releasing the loading extension does not disable the mod for the next city.

Main owns workflow changes. A future authorized **cloud-only** game compilation must provision real `ICities.dll` through `GameManagedDir`, plus a .NET Framework 3.5 targeting/reference environment (for example a pre-provisioned `FrameworkPathOverride`). Automatic reference-assembly package acquisition is disabled. No paths are discovered from a local game installation, no packages are declared, and no reference DLLs are copied to output. Main must verify any transitive references required by the actual ICities version.

Cloud build input: `dotnet build Source/RealisticBorderControl.Game.csproj --configuration Release -p:GameManagedDir=<authorized-managed-reference-directory>`. The expected primary output is `Source/bin/Release/net35/RealisticBorderControl.dll`; this path is a build contract, not evidence that a DLL exists. Do not upload test/model DLLs or third-party reference assemblies as the mod.

`Tests/ShellChecks.csproj` explicitly compiles the same three game source files against **synthetic** ICities boundaries for callback sequencing, repeated cleanup, unsupported modes, and disabled loading. Future authorized cloud command: `dotnet run --project Tests/ShellChecks.csproj --configuration Release`. Passing this harness does not establish real ICities ABI compatibility or in-game acceptance. Real-reference compilation and Steam Workshop subscribed loading/unloading remain separate gates. No workflow was changed for this batch and no DLL was built locally.

## Passenger quota preview

<!-- Observation batch: OpenAI-Codex_Realistic_Border_Control -->

`PassengerQuotaPreview.Allocate` accepts an explicit nonnegative integer passenger budget for a caller-defined simulation window and nonnegative decimal weights keyed by stable connection identity. It distributes whole passenger quotas using largest remainders, with ordinal key ordering to break ties. Exact integer arithmetic avoids decimal overflow and rounding loss. Empty or all-zero weights return no allocation. Invalid budgets, keys, or weights are rejected. Inputs are never mutated; the returned dictionary is a detached preview.

There are no default policy limits, time-window duration, live traffic counts, or game writes. `ObservedPassengers` is null and `DataSource` explicitly reports unknown until a real game adapter is researched. Tests use synthetic inputs only. Preview-first development is confirmed; manually confirmed control belongs to a later phase. Roads, signals, paths, freight matching, gates, and docks retain their existing behavior.

Cloud command: `dotnet run --project Tests/BorderChecks.csproj --configuration Release`. Do not run this locally under the project's local build restriction.

## Supplied observation previews

`SuppliedObservationAdapter.CopyForPreview` copies caller-supplied connection rows into a read-only snapshot. Each row retains a connection key and generation plus nullable inbound/outbound passenger counts. Unknown counts remain null, not zero. Duplicate connection keys, negative values, and missing inputs are rejected. This is a pure-data input adapter, not a game observer: source is always explicitly unverified and `IsGameConnected` is always false. No source can claim trusted live status through this adapter. There are no game assemblies or verified observation API signatures in this module.

`TryGetPreviewRows` returns empty rows when city/load identity, latest snapshot sequence, or policy version differs, simulation time reverses, or the caller-specified maximum age is exceeded. Equal-age boundaries are accepted; paused simulation ticks do not consume age. `Current` only means consistent with the supplied context, never verified traffic. Callers must issue a new load identity after reload and a new sequence for every changed dataset, including connection generation changes. Reusing a sequence for different content or passing a stale current context cannot be detected without a verified provider; this batch does not implement that provider. Generation fields alone cannot prove the connection still exists.

Remaining game-adapter evidence: a permitted reference assembly/source copy, real external-connection enumeration and identity lifetime, direction and transport mode, passenger/freight classification, journey deduplication, simulation time units/window, capture completeness, and unload/thread rules. No counts are inferred from vehicle totals. No observation automatically feeds weights or controls traffic. The new snapshot checks are written but not executed in this local-only batch; the earlier cloud result covers the quota model only.

Already returned preview rows are immutable but cannot be revoked. Consumers must revalidate the snapshot before every use and must not cache a successful result as live traffic.

When implementation and dependencies are ready, this module is intended to build in GitHub cloud runners and publish its own DLL and required files directly as GitHub Release assets, without ZIP archives. Automatic release publication is not configured. Workshop stable uploads are a separate, explicitly authorized local operation.

## License

Code and original documentation in this branch use the PolyForm Noncommercial License 1.0.0; see [LICENSE](LICENSE). This is source-available software. Copyright Kingsley.

Official license source: https://polyformproject.org/licenses/noncommercial/1.0.0.txt

Maintainer signature: OpenAI-Codex_Responsible: Realistic_Border_Control

The pending border-game-dll workflow runs the pure model and synthetic shell contracts, then separately compiles only Source/Game production entry points against pinned real game references. The caller-supplied observation model is not connected to live gameplay. Only the inactive DLL and provenance are retained for 14 days; no Release or Workshop upload occurs. Signature: OpenAI-Codex.
