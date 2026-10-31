# Realistic Import Export Control

Planned Cities: Skylines 1 module for import/export controls and goods-flow management.

## Status

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

No game API mapping or capture adapter exists. `Observation.Disconnected()` reports
`Game data source unavailable`; synthetic examples are not city observations.
Callers must provide amounts already excluding known in-transit commitments,
validated commodity labels, snapshot-scoped endpoint identities, and consistent
quantity units. There is no unit conversion or assumption that priority is quantity.
Priority ordering is a model convention, not a verified mapping of native priorities.
The model does not write matching orders, vehicles, RCI, routes, signals or shared
gate/dock slots. Rescue, garbage and deathcare are outside its cargo scope.

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

Signature: OpenAI-Codex_Responsible: Realistic_Import_Export_Control
