# Intercity Resource Sharing

Planned Cities: Skylines 1 module for intercity water, electricity, heating, and resource-sharing agreements. No game functionality is implemented yet.

## Branch and automation

Canonical repository: [KingsleyHK0323/Cities_Skylines_1_Modpack](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack).

Project website: [Cities: Skylines 1 Modpack](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/).

This module is maintained on `Intercity_Resource_Sharing`. The bootstrap workflow checks documents; the separate `intercity-game-dll` workflow is configured to run synthetic checks and compile the inactive game foundation.

Game compilation requires authorized Cities: Skylines and Unity reference assemblies. The development distribution direction is to publish the Cities Skylines 1 Modpack Dev Manager Windows installer through GitHub Releases; the manager installs module development builds where available. This module's manager availability and installation have not been verified here. Stable builds must exclude DevTool and follow the owner-approved Workshop upload process. This foundation does not yet implement the complete development delivery pipeline.

## License

Code is source-available under PolyForm Noncommercial 1.0.0; see [LICENSE](LICENSE). Copyright Kingsley. Original public documentation and artwork are designated CC BY-NC-SA 4.0: https://creativecommons.org/licenses/by-nc-sa/4.0/. Third-party materials retain their own terms.

Signature: OpenAI-Codex_Responsible: Intercity_Resource_Sharing

## Synthetic preview

Source/ExchangeModel.csproj is an isolated .NET 8 pure-data model and explicitly excludes Source/Game/. The model is not integrated into the game foundation and performs no actual cross-city trades. Tests/ contains synthetic checks. Artificial water/electricity/heat units are integer quantities per manually advanced window, each bounded from 0 to 1,000,000,000. Source demand takes priority; transfers conserve the same quantity at both endpoints. An unavailable input returns zero transfer and unknown (`null`) surplus and shortfall; it is a pending preview, not a completed exchange.

Each scenario supports one fixed directional contract per resource. Limit changes require a new version. The latest identical request is replayable. An unavailable window can be corrected and retried at the same window while pending, subject to the same identity and contract checks. Once available inputs complete a preview, a conflicting request for that window is rejected; advancing to a newer window also rejects older windows, even if the newer window is unavailable. No historical ledger, cancellation, persistence, UI, game adapter or true cross-save exchange is implemented. Callers must serialize access. No game state is read or written. Consumers of `Output` must handle nullable `Surplus` and `Shortfall`.

The configured `intercity-game-dll` workflow includes model checks; bootstrap checks alone do not validate this model. The current local changes have not been verified in the cloud. Commit, push, cloud execution and publication require their applicable authorization.

Signature: OpenAI-Codex_Intercity_Resource_Sharing

## Inactive game foundation

`IntercityResourceSharing.csproj` targets .NET Framework 3.5 and explicitly compiles only `Source/Game/Entry.cs` and `Source/Game/Lifecycle.cs`. It is configured to produce `bin/Game/Release/IntercityResourceSharing.dll`, not the synthetic model DLL. The `IUserMod` entry exposes the module name and an inactive-foundation description. Loading a playable city establishes instance-local lifecycle state; editor/unknown modes, unloading, and release clear it. No city resource is read or written. Initialization failures are contained as failed state; there are no subscriptions, patches, timers, save access, or cross-city operations.

Authorized cloud build input: a Windows MSBuild environment with .NET Framework 3.5 reference assemblies, plus approved `ICities.dll` and `UnityEngine.dll` in an explicit `GameReferenceDir`. No dependency restore/install or local build is part of this handoff. The project fails when required references are missing and does not copy game references to output.

```powershell
MSBuild.exe IntercityResourceSharing.csproj /t:Build /p:Configuration=Release /p:GameReferenceDir="<authorized-reference-directory>"
dotnet run --project Tests/LifecycleChecks.csproj
dotnet run --project Tests/Checks.csproj
```

The game build is for a separately authorized cloud job. The synthetic checks may also run locally when authorized. Lifecycle checks link production sources against a clearly marked synthetic ICities boundary; they do not prove real game API compatibility. A historical real-reference cloud build does not establish Workshop-subscribed runtime load/unload acceptance.

This foundation uses the plain module name and does not claim DevTool support or a completed stable release. The required development-name template must be connected only when actual DevTool inclusion/exclusion is implemented. Installed stable/development coexistence detection and warning UI are pending; future behavior must warn only, never automatically disable, uninstall, block loading, unsubscribe, or delete files. Main owns workflow integration; no workflow file was changed in this batch.

Signature: OpenAI-Codex_Intercity_Resource_Sharing

The intercity-game-dll cloud workflow is configured to check the hypothetical model and synthetic lifecycle separately, then compile only Source/Game against pinned game references. Only IntercityResourceSharing.dll and provenance are retained for 14 days; no resource exchange or settlement is enabled. Cloud validation of the current local changes is pending. Signature: OpenAI-Codex.
