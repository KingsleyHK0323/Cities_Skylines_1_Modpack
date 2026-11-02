# Realistic Import Export Control

Planned Cities: Skylines 1 module for import/export controls and goods-flow management.

## Status

An inactive game-entry shell is now staged in `Source/Game`, separately from the
.NET 8 preview model. It exposes the full English name through `IUserMod` and starts
only a no-data lifecycle session for NewGame/LoadGame. Unload/release are idempotent;
retired loaders cannot clear a newer session or restart themselves. It performs no
cargo reads, matching, economy changes or other game-state control. No game DLL has
been built or validated for this local batch.

### Game shell production build (cloud only; Main owns workflow changes)

With real, explicitly restored .NET 3.5 game references, the proposed command is:

```powershell
msbuild Source/Game/RealisticImportExportControl.csproj /t:Rebuild /p:Configuration=Release /p:GameManagedDir="<restored-managed-reference-directory>"
```

Expected output: `Source/Game/bin/Release/RealisticImportExportControl.dll`.
The production compile list is exactly `CargoMod.cs`, `CargoSession.cs` and
`CargoLoading.cs`; it excludes the .NET 8 preview code and all test stubs. References
are compile-only and are not copied into the output. No game installation is searched.
The ICities boundary tests use test-only API shapes and do not prove real-game API
compatibility. Nine lifecycle/entry assertions are added to the existing console
checks (51 total expected including pending observation checks); none were run in
this batch. Real-reference cloud compilation and subscribed-game loading remain
required before claiming an uploadable game DLL or runtime acceptance.

Pure cargo preview model with synthetic fixture checks. No game integration or
validated gameplay exists. The .NET 8 model library is not a Cities: Skylines mod
DLL and must not be installed in the game's Mods folder.

## Read-only model

The model classifies caller-labelled cargo offers, separates domestic/import/export
flows, and proposes same-commodity, same-unit allocations within available amounts.
Unknown tasks, services, unknown in-transit quantities, conflicting duplicate IDs,
stale, partial, or non-simultaneous snapshots do not produce reliable allocations.
Unranked alternatives are mutually exclusive and have no additive total.
Reachability is always unknown and every result is non-dispatchable.

No native game API mapping or capture provider exists. `Observation.Disconnected()` reports
`Game data source unavailable`; synthetic examples are not city observations.
Callers must provide amounts already excluding known in-transit commitments,
validated commodity labels, snapshot-scoped endpoint identities, and consistent
quantity units. There is no unit conversion or assumption that priority is quantity.
Priority ordering is a model convention, not a verified mapping of native priorities.
The model does not write matching orders, vehicles, RCI, routes, signals or shared
gate/dock slots. Rescue, garbage and deathcare are outside its cargo scope.

## Raw observation adapter (local, awaiting cloud verification)

`CargoObservationAdapter` reads caller-supplied records with explicit provenance,
capture generation and sequence. It preserves raw counters without interpreting
units, removes identical repeated source records and rejects conflicting duplicates
or incomplete/non-simultaneous captures. Caller-evidenced endpoint pairs distinguish
domestic, import, export and outside transit; unknown boundaries remain unknown.
No native game data is read. Totals, quantity units and in-transit semantics stay
unknown, and these records cannot create planner snapshots or dispatch orders.
See [source evidence and cloud verification checklist](docs/CargoSourceEvidence.md).
The added observation fixtures have not been compiled or executed in this local batch.

## Cloud checks

GitHub-hosted Ubuntu compiles `Source/CargoModel.csproj` and runs the dependency-free
console assertions in `Tests/CargoModelChecks.csproj` using .NET 8. No third-party
test packages or game references are required. The checks cover conservation,
classification, invalid/stale inputs and explicit disconnected state. Compilation
and fixture checks are not gameplay acceptance or complete TMCE/OOC replacement.

## Validation and delivery

GitHub-hosted checks validate documentation and the pure model on this branch.
They do not build a game mod.
Game reference DLL sourcing and redistribution rights remain unresolved.
Future cloud builds should publish this module's DLL and necessary files directly
to its own GitHub Release, without ZIP archives. Automatic releases are not enabled.
Local workflows and runners must not be started without separate authorization.
Steam Workshop uploads are outside this scaffold.

## Licensing

Source code and original documentation: PolyForm Noncommercial License 1.0.0;
see LICENSE. Copyright Kingsley. This project is source-available.
Third-party material retains its own terms.

Signature: OpenAI-Codex_Realistic_Import_Export_Control

The pending import-export-game-dll workflow runs cargo model, observation and synthetic lifecycle checks, then compiles only Source/Game production entry points against pinned real game references. Test API stubs and the modern model are excluded from the game DLL. Only the inactive DLL and provenance are retained for 14 days; no Release or Workshop upload occurs. Signature: OpenAI-Codex.
