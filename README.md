# Wireless Utilities And Global Coverage

Cities: Skylines 1 module in the Cities Skylines 1 Modpack.

## Status

The current local source batch contains a real `ICities.IUserMod`, a `LoadingExtensionBase` entry point, and an isolated, inactive lifecycle. It is not a pure-model DLL substituted for a game entry point.

There is no game implementation of wireless utility control, no verified compiled DLL for this batch, and no in-game acceptance evidence. The entry point does not change electricity, water, sewage, heating, production, capacity, cost, pollution, shortages, or connection requirements. No resource-manager hooks, city scanning, cross-city effects, save writes, or asset changes are installed.

The plain module name and description identify an inactive foundation. There is no DevTool; this batch must not be labeled as a complete Development Version. Development naming and actual DevTool inclusion, official builds excluding tools, installed-version detection, and warning UI remain separate work. The agreed policy for simultaneous official/development installations is warning only, with no automatic disabling, uninstallation, subscription changes, or load blocking. That warning is not implemented here.

The intended scope is city-wide electricity, water, sewage, and heating coverage without manually placed utility connections. Actual behavior, compatibility, and save handling require a separate approved design and implementation.

## Game project and pending cloud verification

`Source/WirelessUtilitiesAndGlobalCoverage.csproj` explicitly lists the production source files for `Source/bin/Release/WirelessUtilitiesAndGlobalCoverage.dll`, targeting .NET Framework 3.5 / C# 3. `Checks/EntryPointChecks.csproj` references that game project and the real game interfaces; it does not define substitute ICities or Unity types.

### Synthetic transfer assessment

`Source/UtilityTransferAssessment.cs` is a standalone, immutable-data consistency check for one caller-supplied observation of electricity, water, sewage, or heating. It distinguishes unknown or unsupported values from a known zero; requires one resource, city session, observation epoch, and exact unit; and rejects negative amounts or an accepted amount above the supplied offer or request. Successful arithmetic reports the unused offer and unmet request without increasing either quantity.

This model does not read game managers, start facilities, establish pipe-free operation, route resources, verify pollution, infer native units, or prevent repeated use of the same offer across transactions. It is not a ledger or game adapter and is not wired into the game lifecycle. Synthetic inputs and a consistent result do not prove actual native supply, sewage treatment, heating, conservation, compatibility, or gameplay behavior.

The following optional local synthetic check compiles that single pure source file in memory and runs no game DLL, Steam tool, runner, or workflow:

```powershell
pwsh -NoProfile -File ./Checks/UtilityTransferAssessmentChecks.ps1
```

Its results cover the pure model only; the actual .NET Framework 3.5 project and Workshop-subscribed game remain separate verification boundaries. The existing cloud workflow does not run this new check; a separately reviewed workflow update is required before it becomes a cloud gate.

Provide `GameReferenceRoot` (or `CS1_GAME_REFERENCE_ROOT`) pointing to an authorized reference copy. The production project requires `mscorlib.dll`, `System.dll`, `ICities.dll`, `ColossalManaged.dll`, `UnityEngine.dll`, and `Assembly-CSharp.dll`. It never searches an installed game directory. Reference copying is disabled in the production project; the check executable's private dependency directory is not a distributable mod folder.

The following commands are for a separately authorized Windows cloud build, from this module's root. They were not executed locally:

```powershell
msbuild Source/WirelessUtilitiesAndGlobalCoverage.csproj /t:Rebuild /p:Configuration=Release /p:DevTools=false "/p:GameReferenceRoot=$referenceRoot" "/p:FrameworkPathOverride=$referenceRoot"
if ($LASTEXITCODE -ne 0) { throw 'Game DLL build failed.' }
msbuild Checks/EntryPointChecks.csproj /t:Rebuild /p:Configuration=Release /p:DevTools=false "/p:GameReferenceRoot=$referenceRoot" "/p:FrameworkPathOverride=$referenceRoot"
if ($LASTEXITCODE -ne 0) { throw 'Entry-point check build failed.' }
& ./Checks/bin/Release/WirelessUtilitiesAndGlobalCoverage.EntryPointChecks.exe
if ($LASTEXITCODE -ne 0) { throw 'Entry-point checks failed.' }
```

The checks use an injected reporting sink, not Unity logging, and cover 12 named synthetic lifecycle/identity cases. Their .NET Framework 4.8 host configuration is for cloud checks only; it does not establish game Mono compatibility. Compilation, execution, default Unity logging, actual game discovery, and subscribed-game loading/unloading remain unverified. `DevTools=true` is rejected rather than silently pretending to provide a tool build.

## Workflow and delivery status

The `Wireless Utilities scaffold checks` workflow runs on GitHub-hosted Ubuntu for pushes and pull requests targeting `Wireless_Utilities_And_Global_Coverage`. It can also be requested manually on that branch. It verifies required documentation, whitespace, and the official license file's SHA-256 digest. It does not compile or test a game mod.

Main Session owns workflow changes; this source batch does not modify workflow files. Pushes to this branch trigger the existing cloud workflows. The current development distribution direction uses a separate Dev Manager and its Windows installer as the GitHub Releases entry point. The official direction remains owner-approved issue-driven cloud builds without tools followed by an upload-only local runner for Workshop releases. These target pipelines, complete DevTool builds, and warning UI are not delivered by this foundation. The module's Workshop item is [3832301115](https://steamcommunity.com/sharedfiles/filedetails/?id=3832301115); an owner-provided screenshot recorded it as Hidden on 2026-11-02, and its current visibility has not been verified. No automatic release or Workshop upload is configured on this branch. Gameplay acceptance must use the Steam Workshop subscribed build.

## Licensing

Copyright Kingsley. This is a source-available, noncommercial project.
Code and original documentation in this branch are provided under the [PolyForm Noncommercial License 1.0.0](LICENSE.md). No separately licensed website content is included.

`LICENSE.md` is the unmodified official text from [PolyForm Project](https://github.com/polyformproject/polyform-licenses/blob/1.0.0/PolyForm-Noncommercial-1.0.0.md), retrieved on 2026-10-31. See [NOTICE.md](NOTICE.md) for the project notice.

Signature: OpenAI-Codex_Responsible: Wireless_Utilities_And_Global_Coverage

Current source-batch signature: OpenAI-Codex_Wireless_Utilities_And_Global_Coverage

Synthetic transfer assessment signature: OpenAI-Codex_Wireless_Utilities_And_Global_Coverage

The wireless-game-dll cloud workflow compiles the explicit inactive production sources against pinned game references, then compiles and runs the entry-point checks under cloud CLR v4 in a separate temporary directory. Game references remain in RUNNER_TEMP; only the module DLL and provenance are uploaded for 14 days. This checks API/lifecycle wiring, not gameplay or utility control. Signature: OpenAI-Codex.
