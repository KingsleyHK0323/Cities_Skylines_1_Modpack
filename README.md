# Wireless Utilities And Global Coverage

Cities: Skylines 1 module in the Cities Skylines 1 Modpack.

## Status

The current local source batch contains a real `ICities.IUserMod`, a `LoadingExtensionBase` entry point, and an isolated, inactive lifecycle. It is not a pure-model DLL substituted for a game entry point.

There is no game implementation of wireless utility control, no verified compiled DLL for this batch, and no in-game acceptance evidence. The entry point does not change electricity, water, sewage, heating, production, capacity, cost, pollution, shortages, or connection requirements. No resource-manager hooks, city scanning, cross-city effects, save writes, or asset changes are installed.

The plain module name and description identify an inactive foundation. There is no DevTool; this batch must not be labeled as a complete Development Version. Development naming and actual DevTool inclusion, official builds excluding tools, installed-version detection, and warning UI remain separate work. The agreed policy for simultaneous official/development installations is warning only, with no automatic disabling, uninstallation, subscription changes, or load blocking. That warning is not implemented here.

The intended scope is city-wide electricity, water, sewage, and heating coverage without manually placed utility connections. Actual behavior, compatibility, and save handling require a separate approved design and implementation.

## Game project and pending cloud verification

`Source/WirelessUtilitiesAndGlobalCoverage.csproj` explicitly compiles the five production source files into `Source/bin/Release/WirelessUtilitiesAndGlobalCoverage.dll`, targeting .NET Framework 3.5 / C# 3. `Checks/EntryPointChecks.csproj` references that game project and the real game interfaces; it does not define substitute ICities or Unity types.

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

Main Session owns workflow changes; this source batch does not modify or trigger workflows. The current shared direction is cloud DevTool builds followed by a cloud ZIP/Release for approved development modules, and owner-approved issue-driven cloud builds without tools followed by an upload-only local runner for official Workshop releases. These target pipelines, complete DevTool builds, warning UI, and the first private Workshop ID are not delivered by this foundation. No automatic release or Workshop upload is configured on this branch. Gameplay acceptance must use the Steam Workshop subscribed build.

## Licensing

Copyright Kingsley. This is a source-available, noncommercial project.
Code and original documentation in this branch are provided under the [PolyForm Noncommercial License 1.0.0](LICENSE.md). No separately licensed website content is included.

`LICENSE.md` is the unmodified official text from [PolyForm Project](https://github.com/polyformproject/polyform-licenses/blob/1.0.0/PolyForm-Noncommercial-1.0.0.md), retrieved on 2026-10-31. See [NOTICE.md](NOTICE.md) for the project notice.

Signature: OpenAI-Codex_Responsible: Wireless_Utilities_And_Global_Coverage

Current source-batch signature: OpenAI-Codex_Wireless_Utilities_And_Global_Coverage

The wireless-game-dll cloud workflow compiles the explicit inactive production sources against pinned game references, then compiles and runs the entry-point checks under cloud CLR v4 in a separate temporary directory. Game references remain in RUNNER_TEMP; only the module DLL and provenance are uploaded for 14 days. This checks API/lifecycle wiring, not gameplay or utility control. Signature: OpenAI-Codex.
