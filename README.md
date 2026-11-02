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

## Local read-adapter batch (pending cloud validation)

`Source/Pedestrian/CrossingReadAdapter.cs` adds an internal synchronous read-source seam with explicit session opening and invalidation. It rejects results produced across session changes, preserves unknown/synthetic provenance, and never reuses cached successes. No native game reader is connected; actual rules, signals, geometry and markings remain unverified. See [source audit and pending cloud checklist](docs/Observation-Sources.md).

`Tests/AdapterChecks.cs` adds lifecycle and unavailable-source scenarios to the existing console check entry point. These new checks have been written but not executed or compiled. The earlier 31 passing cloud assertions apply to commit `77a4488`, not to this local batch. No commit, push, cloud run or release is part of this batch.

Signature: OpenAI-Codex_Realistic_Pedestrian_Traffic_Control

## Game DLL foundation (local, uncompiled)

`Source/Game/PedestrianMod.cs` supplies `IUserMod` metadata and `ILoadingExtension` callbacks. The runtime remains an inactive observation foundation: it opens only an internal lifetime for enabled NewGame/LoadGame callbacks and invalidates it on disable, unloading, release, or non-city loads. Enabling while a level is already open does not infer an active session. There is no polling, settings UI, signal/path/rule/marking write, or native observation reader.

The production project is `Source/RealisticPedestrianTrafficControl.csproj`, targeting .NET Framework 3.5 with C# 3 syntax. It explicitly compiles only:

- `Source/Pedestrian/CrossingModel.cs`
- `Source/Pedestrian/CrossingReadAdapter.cs`
- `Source/Runtime/PedestrianRuntime.cs`
- `Source/Game/PedestrianMod.cs`

After separate cloud authorization, a Windows MSBuild invocation is:

```powershell
MSBuild.exe Source/RealisticPedestrianTrafficControl.csproj /t:Build /p:Configuration=Release /p:GameReferencePath="<authorized-cloud-reference-directory>"
```

Expected output: `Source/bin/Release/RealisticPedestrianTrafficControl.dll`. The cloud environment must provide the .NET Framework 3.5 reference assemblies and a lawfully obtained, matching `ICities.dll`. The project fails with an explicit error when `ICities.dll` is missing and never searches an installed game directory. Additional transitive reference requirements must be determined by an actual game-target compile, not by fake API stubs. Tests are not included in the game DLL.

Pending pure host checks remain `dotnet run --project Tests/PedestrianChecks.csproj --configuration Release`; these include adapter and lifecycle/error-isolation cases, but do not compile the ICities-facing entry points. No checks or builds have been executed for this local batch, and no workflow files were changed. No DLL exists from this batch yet. Successful cloud compilation must precede any separate private Workshop upload/ID assignment approval; compilation alone is not subscribed-game acceptance.

The callback signatures are informed by public author source examples, not a verified current local API assembly: [boformer's loading extension example](https://github.com/boformer/PrefabHook) and [CSM's IUserMod entry](https://github.com/CitiesSkylinesMultiplayer/CSM/blob/master/src/csm/CSM.cs). Their licenses/code are not copied. The actual API and callback lifecycle still require compile/runtime validation.

Signature: OpenAI-Codex

The pending pedestrian-game-dll workflow runs crossing, adapter and lifecycle checks, then compiles the production Pedestrian, Runtime and Game sources against pinned real game references. Only the inactive DLL and provenance are retained for 14 days; no Release or Workshop upload occurs. The documentation baseline checks source entry points and still rejects committed DLL/ZIP binaries. Signature: OpenAI-Codex.
