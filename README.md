# Realistic City First Response

Planned Cities: Skylines 1 module for emergency-service dispatch and response. This branch now includes an immutable, caller-supplied observation snapshot and a pure rain/fire co-occurrence analysis model. A disabled foundation game entry point is now present; no compiled DLL or validated gameplay functionality is claimed.

## Build and publication status

Game compilation requires explicitly supplied authorized reference assemblies and .NET Framework 3.5 reference assemblies. Cloud compilation and direct DLL publication through module-specific GitHub Releases are future work. Automatic releases are disabled. No ZIP distribution is planned. Local workflows and Workshop uploads are outside this initialization.

## Licensing

Code and original documentation use [PolyForm Noncommercial 1.0.0](LICENSE.md). Copyright Kingsley. This is a source-available, noncommercial project. The official license text is from https://github.com/polyformproject/polyform-licenses/blob/1.0.0/PolyForm-Noncommercial-1.0.0.md .

Signature: OpenAI-Codex_Responsible: Realistic_City_First_Response

## Read-only analysis batch

`Source/FireWeatherSnapshot.cs` holds copied raw observations. `Source/FireWeatherAnalysis.cs` reports missing, stale, invalid, unknown, dry, non-burning, or positive rain/fire co-occurrence. These are analysis statuses, not fire-suppression eligibility. There is no live game adapter, Harmony hook, dispatch, route change, intensity write, rain normalization, or suppression formula.

Callers must supply a level generation, building identity and exact capture sequence; invalidate that sequence on ID reuse, reload or observation expiry. The model cannot independently detect recycled IDs. Rain and fire units and maximum values remain unverified. Unknown values remain unknown; values above 1 or 255 are not clamped based on assumptions.

`Tests/ObservationChecks.csproj` is a dependency-free .NET 8 console check project for this pure model only. It is not a game mod project. The 17 authored checks have not been compiled or executed. A future authorized cloud job can run `dotnet run --project Tests/ObservationChecks.csproj --configuration Release`; this command has not been run locally or remotely in this batch. The existing documentation workflow does not execute these checks.

Cloud review checklist: verify the intended commit and build attempt; run the observation checks on a GitHub-hosted runner; retain logs; obtain authorized reference assemblies before creating a real game adapter; validate copied field types and capture-thread consistency; define and approve product behavior before suppression work. No new commit, push, deployment or release is part of this local batch. A future DLL release needs explicit authorization, artifact ID/hash and release identity verification. Game acceptance requires the subscribed Workshop build.

Signature: OpenAI-Codex_Realistic_City_First_Response

## Game foundation project (uncompiled)

`RealisticCityFirstResponse.csproj` targets .NET Framework 3.5 and produces `bin/Release/net35/RealisticCityFirstResponse.dll`. `Source/Mod.cs` provides the ICities IUserMod and LoadingExtensionBase entry points. Only NewGame and LoadGame activate instance-local observation state; editor modes stay inactive. Unloading and release clear that state idempotently. No game capture, rain suppression, dispatch, priority request, traffic change or saved-game data is enabled. The private Workshop item ID is not yet assigned here.

For Main's authorized cloud compilation, use `dotnet build RealisticCityFirstResponse.csproj --configuration Release -p:GameReferencesDir=<authorized-reference-directory> -p:FrameworkPathOverride=<net35-reference-directory>`. Both directories must be supplied by the cloud job; no original game path is defaulted. ICities.dll and UnityEngine.dll are references with Private=false, not release files. Missing references fail explicitly. No dependency package, workflow, copy-to-game target or upload action is added by this batch.

The independent .NET 8 checks now include 17 observation and 7 lifecycle assertions. Run `dotnet run --project Tests/ObservationChecks.csproj --configuration Release` only in an authorized environment. These checks exercise the pure state model, not actual ICities callbacks. All compilation and execution remain pending; check the real game assembly's entry point compatibility and subscribed-build load/unload separately. A successful cloud compile is not Workshop gameplay acceptance.

Signature: OpenAI-Codex_Realistic_City_First_Response

The pending first-response-game-dll workflow runs observation and lifecycle checks and compiles the inactive production game entry point against pinned cloud references. Only the DLL and build provenance are retained for 14 days. No dispatch, fire suppression or traffic changes are enabled; no Release or Workshop upload is performed. Signature: OpenAI-Codex.

Cloud validation correction: observation checks now distinguish mismatched building identity from a matching zero-ID invalid sample (18 observation checks plus 7 lifecycle checks). The documentation baseline checks the present inactive game entry point instead of requiring the obsolete no-code statement. Signature: OpenAI-Codex.
