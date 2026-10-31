# Realistic Border Control

Planned Cities: Skylines 1 module for border and entry-point controls. This branch includes a pure passenger quota preview model; no game functionality or playable release exists yet.

## Scope

Future investigation may cover passenger movement through city borders, airports, harbors, and railway stations. These are goals, not implemented features.

## Validation and delivery

The GitHub-hosted workflow checks required documentation, then compiles and executes dependency-free .NET 8 model checks. This target is a cloud model harness, not the game mod target. No game assemblies are referenced. Game integration and the compatible game build target remain unresolved.

## Passenger quota preview

`PassengerQuotaPreview.Allocate` accepts an explicit nonnegative integer passenger budget for a caller-defined simulation window and nonnegative decimal weights keyed by stable connection identity. It distributes whole passenger quotas using largest remainders, with ordinal key ordering to break ties. Exact integer arithmetic avoids decimal overflow and rounding loss. Empty or all-zero weights return no allocation. Invalid budgets, keys, or weights are rejected. Inputs are never mutated; the returned dictionary is a detached preview.

There are no default policy limits, time-window duration, live traffic counts, or game writes. `ObservedPassengers` is null and `DataSource` explicitly reports unknown until a real game adapter is researched. Tests use synthetic inputs only. Preview-first development is confirmed; manually confirmed control belongs to a later phase. Roads, signals, paths, freight matching, gates, and docks retain their existing behavior.

Cloud command: `dotnet run --project Tests/BorderChecks.csproj --configuration Release`. Do not run this locally under the project's local build restriction.

When implementation and dependencies are ready, this module is intended to build in GitHub cloud runners and publish its own DLL and required files directly as GitHub Release assets, without ZIP archives. Automatic release publication is not configured. Workshop stable uploads are a separate, explicitly authorized local operation.

## License

Code and original documentation in this branch use the PolyForm Noncommercial License 1.0.0; see [LICENSE](LICENSE). This is source-available software. Copyright Kingsley.

Official license source: https://polyformproject.org/licenses/noncommercial/1.0.0.txt

Maintainer signature: OpenAI-Codex_Responsible: Realistic_Border_Control
