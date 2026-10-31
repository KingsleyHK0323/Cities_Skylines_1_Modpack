# Realistic Pedestrian Traffic Control

Planned Cities: Skylines 1 module for pedestrian crossings, pedestrian signals, and interactions with vehicle traffic. No gameplay implementation or DLL is available yet.

## Development status

This branch now includes an internal pure C# crossing observation and preview model. It distinguishes crossing rules, geometry, signals (including no signal), marking visibility, source provenance, and endpoint/session revisions. No live game reader or game control is connected.

## Pure model checks

GitHub-hosted CI compiles `Source/Pedestrian/CrossingModel.cs` with the dependency-free `Tests/PedestrianChecks.csproj` console checks. The host target is .NET 8 with C# 3 syntax enforced; this does not establish compatibility with the game runtime or produce an installable game mod. No third-party packages or game assemblies are used.

The model rejects missing/unknown sources, invalid states, stale city/endpoint identities, unknown rules and unsupported geometry. Synthetic fixtures remain explicitly labeled synthetic. Unknown counts stay unknown; hidden markings do not prohibit crossing, and red lights do not represent a permanent ban. The optional visibility result is a hypothetical consequence of a confirmed rule, not a render command or proof of applied control.

`CrossingPreview.Evaluate(null, ...)` reports that no game observation source is connected. The model has no live-data source enum value, game hooks, network access, settings migration, persistence, or signal/path writes. It cannot discover whether an existing rule survived another mod's removal; absent data must remain unknown.

The cloud command is `dotnet run --project Tests/PedestrianChecks.csproj --configuration Release`. Local builds/runners/workflows are not authorized by these instructions. The existing baseline check remains a documentation check only.

A future game reader still requires verified game APIs and lawfully acquired reference assemblies. Any artificial-ID or topology revision fixtures are test inputs, not a verified game identity mechanism. Actual control remains a separately approved later phase. No TM:PE dependency, shared DLL, automatic mod disabling, public release, or Workshop upload is introduced.

## Delivery direction

Each module has its own branch and GitHub-hosted build. Future GitHub Releases should attach the DLL and necessary files directly, without ZIP archives. Automatic public releases remain disabled until dependencies and validation are ready. Workshop stable uploads are separate, explicitly authorized local operations.

## Licensing

Source code and original documentation in this module are source-available under PolyForm Noncommercial 1.0.0; see LICENSE. Copyright Kingsley. Third-party components retain their own terms. No Website content is included in this branch.

The license was obtained unchanged from https://github.com/polyformproject/polyform-licenses/blob/HEAD/PolyForm-Noncommercial-1.0.0.md.

Maintainer signature: OpenAI-Codex_Responsible: Realistic_Pedestrian_Traffic_Control
