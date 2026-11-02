# Realistic Railway Traffic Control

This source-available Cities: Skylines 1 module is in observation/preview development. A minimal inactive game-entry project is present as uncompiled source. No game implementation of railway observation or control is enabled. No game DLL has been compiled or tested for this local batch; the .NET Framework 4.8 pure-data library is not the game mod.

## Pure-data model development

The first approved stage is observation and preview, before any separately approved manual control. The branch implements immutable snapshot and movement data, a fail-closed validator, and a shadow conflict analyzer. Live observation is unavailable: no game data-source adapter is implemented. Behavior checks use explicitly synthetic model inputs, not live city observations.

- Invalid or incomplete evidence returns `Unknown` with a reason. Unsupported networks return `Unsupported` when the other evidence is complete.
- Conflicting neighboring resources are checked as well as directly requested resources. A known train may retain its own starting occupancy; a following train still sees that occupancy.
- Shadow reservations are all-or-nothing per request and exist only within one `Evaluate` call. Input ordering is deterministic; this is not an optimal or fair train scheduler.
- `WouldReserve` describes the model only. It is not a signal, permission to move, or proof of physical safety.
- Snapshot validation enforces city/capture identity, topology tokens, complete conflict relationships, and engineering caps: 128 segments, 64 consists, 256 resources, and 64 requests; at most 256 conflict keys and 64 occupant keys per resource, and 128 resource keys per request. Truncated or unknown evidence cannot become a clear track.
- A token is supplied model evidence, not a claimed native game generation/version field. Inputs must already identify all relevant trains, including tails and unsupported types occupying the area. The model cannot independently establish that a real-world capture is complete.

The separate `railway-model-checks.yml` workflow compiles a .NET Framework 4.8 pure-data library and console checks on GitHub-hosted Windows. Its source is restricted to C# 3 syntax. This does not establish .NET 3.5 binary compatibility or compile a Cities: Skylines mod. No game references, dependency installation, release publishing, or Workshop upload are involved.

## Inactive game entry and local observation lifetime

`Source/RealisticRailwayTrafficControl.csproj` is a separate .NET Framework 3.5 game library project, containing the actual `ICities.IUserMod` entry and `LoadingExtensionBase` callbacks. Its explicit `Compile` items are the production source manifest: six model files, the observation session, the lifecycle guard, and two game-entry files. It compiles these sources directly and does not reference the .NET Framework 4.8 model DLL or any check program.

The entry reports **Realistic Railway Traffic Control** and describes itself as an inactive preview shell. `NewGame` and `LoadGame` callbacks start only local lifecycle bookkeeping; other modes remain inactive. Unload/release clears local observation state. The shell installs no AI patches, update subscriptions, capture requests, settings migration, or game-state writers. Its lifecycle guard contains normal callback errors, attempts local cleanup, and retains only the first failure's stage and type, plus whether cleanup also failed. It does not retain exception messages or claim an existing game UI for diagnostics.

The internal `RailwayObservationSession` supports one pending capture, immutable tickets, local city/capture counters, explicit invalidation, and rejection of stale, foreign, duplicate, or mismatched callbacks. Starting a new capture drops the previous preview. Receiving an incomplete snapshot preserves that incomplete evidence. Receipt is not validation, native object identity, or permission to move. No game data provider calls this session yet, and there is no public observation/control capability.

Inspection of an approved reference copy confirmed the loading-interface signatures and candidate rail fields, not runtime safety. `NetLane` reservation fields do not establish complete occupancy; no assumed `m_vehicles` lane census is used. Native vehicle identity across reuse, complete tail occupancy, and effective connection constraints remain unresolved. A local epoch is not a native vehicle generation, and a topology token is not automatically a verified game version.

## Commands for later authorized cloud validation

These are reviewable commands, not results from this local batch. Main Session has separately prepared `railway-game-dll.yml` and refreshed the baseline checks; this Fork did not edit either workflow or run them. The new workflow is intended to run the .NET Framework 4.8 checks first, then select an explicit .NET 8 SDK compiler with C# 3 and approved game references. The project-based command below is an alternative using the same explicit production manifest and requires installed .NET Framework 3.5 targeting files. Neither route has compiled this draft yet.

1. Build and run the pure-data/lifecycle checks on the approved Windows host:

   ```powershell
   & $msbuild Checks/RailwayModelChecks.csproj /t:Rebuild /p:Configuration=Release
   if ($LASTEXITCODE -ne 0) { throw 'Model/check compilation failed.' }
   & ./bin/ModelChecks/Release/RailwayModelChecks.exe
   if ($LASTEXITCODE -ne 0) { throw 'Behavior checks failed.' }
   ```

2. Build the real game library with installed .NET Framework 3.5 reference assemblies and an explicitly approved reference directory:

   ```powershell
   & $msbuild Source/RealisticRailwayTrafficControl.csproj /t:Rebuild /p:Configuration=Release "/p:GameReferenceRoot=$referenceRoot"
   if ($LASTEXITCODE -ne 0) { throw 'Game compilation failed.' }
   $dll = Get-Item -LiteralPath './bin/Game/Release/RealisticRailwayTrafficControl.dll'
   if ($dll.Length -le 0) { throw 'Game DLL is empty.' }
   Get-FileHash -LiteralPath $dll.FullName -Algorithm SHA256
   ```

The reference directory must contain the approved `ICities.dll`, `ColossalManaged.dll`, and `UnityEngine.dll`; the project does not discover a game installation or install dependencies. References have `Private=False` and must not be distributed with the mod. Build tools, framework targeting files, reference provenance, source SHA, and the exact resulting game DLL must be verified before any upload is considered. Do not use the .NET Framework 4.8 `RailwayShadowModel.dll` as the Workshop payload.

The local draft now contains 49 check groups (the original 29, 12 observation-session groups, and 8 lifecycle/error-isolation groups). The added groups and game entry have **not** been compiled or executed in this batch. Earlier results at commit `0ab9b1f` cover only the earlier 29 groups. After compilation, game loading and unload behavior still require separate Steam Workshop subscribed-build acceptance. No Workshop ID, upload, or publication is created here.

## Project links

- [Canonical repository](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack)
- [Module branch](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_Railway_Traffic_Control)
- [Modpack website](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/)

## Intended scope

Railway signals, block occupancy, junction control, reversible trams, and station-platform behavior remain staged areas of work. This batch implements only the pure-data validator and shadow analyzer. It does not submit paths, change signals, reverse vehicles, allocate platforms, alter dwell time, or change vehicle counts. Existing AI Improve behavior and external mods are left untouched.

## Automation and release boundary

The `module-baseline` workflow runs documentation and repository baseline checks on GitHub-hosted Ubuntu for pushes and pull requests targeting `Realistic_Railway_Traffic_Control`, or by manual dispatch. It does not compile or test game code.

Game compilation is now explicitly defined by `Source/RealisticRailwayTrafficControl.csproj` but has not been run for this draft. It requires the approved reference copies and installed .NET Framework 3.5 targeting files; successful static metadata inspection does not prove compilation or runtime compatibility. Do not commit proprietary game assemblies.

Once those prerequisites are resolved, the intended distribution is an independent GitHub cloud build, Development files directly from a module-specific GitHub Release without ZIP packaging, and the stable version through Steam Workshop. No automatic release or Workshop upload is configured. Local execution of runners or workflows requires separate explicit authorization. Gameplay acceptance requires the Steam Workshop subscribed version.

## License

Copyright Kingsley. Code and original documentation in this branch use the PolyForm Noncommercial License 1.0.0; see [LICENSE](LICENSE). This is source-available software with noncommercial restrictions. The separate website's original content uses CC BY-NC-SA 4.0; website content is not included here. Third-party material retains its own terms.

Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control

Current source batch signature: OpenAI-Codex_Realistic_Railway_Traffic_Control
