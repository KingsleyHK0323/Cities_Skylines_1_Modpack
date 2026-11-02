# Full Map Unlock And Expansion

Planning-stage module for Cities: Skylines 1. The intended scope is full-map access and expansion. No game functionality, binaries, or game integration is implemented yet.

Project repository: https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack

## Validation and releases

The GitHub-hosted workflow checks documentation, compiles the pure preview model, and executes 14 assertions. It does not build or test a game mod. Game references and a game-mod build project are not configured. There is no automated release, DLL, ZIP, or Workshop upload in this branch.

Future releases are intended to provide DLLs and necessary files directly through this module's own GitHub Release. Gameplay acceptance requires the Steam Workshop subscribed build.

## License

Code: PolyForm Noncommercial License 1.0.0; see LICENSE. Original public documentation: CC BY-NC-SA 4.0 (https://creativecommons.org/licenses/by-nc-sa/4.0/). Copyright Kingsley.

Signature: OpenAI-Codex_Responsible: Full_Map_Unlock_And_Expansion


## Local preview prototype

Source/AreaPreview.cs classifies caller-supplied native eligibility, count and price. It is not connected to the game and never authorizes a purchase. Milestone mode remains undecided. Tests/PreviewChecks.cs contains pending cloud checks; neither compilation nor execution has been verified.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Observation contract and assembly evidence: [Area observation evidence](docs/AreaObservationEvidence.md). No lifecycle adapter is implemented; milestone policy remains undecided.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion


## Pending cloud validation

The branch workflow uses ubuntu-22.04 and its preinstalled .NET 8 SDK (latest available 8.x patch, printed and pinned per run). Missing SDK is an environment failure. Restore uses Tests/NuGet.Config with no package feeds or PackageReference. Restore, Release compilation, and executable checks are separate steps; this wiring has not been executed. Push to this branch, PR targeting it, or manual dispatch on this branch triggers checks with contents: read only. No upload, release, prerelease, Workshop action or game DLL is produced.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion


## Game observation DLL source (not compiled)

FullMapUnlockAndExpansion.csproj targets .NET Framework 3.5 with authorized ICities and UnityEngine references supplied through GameReferenceDir. It defines IUserMod, loading and area extension entry points. Eligibility and price callbacks return their input unchanged. Observation.Read returns an immutable latest callback record or null; eligibility and price are separate observations, and price has no tile identity. Values are not final eligibility after other extensions and never establish affordability. Unloading invalidates observations; replaced area providers cannot record or release the current provider. No UI or automatic polling is provided.

Main owns workflow integration. Candidate cloud command: MSBuild FullMapUnlockAndExpansion.csproj /t:Build /p:Configuration=Release /p:GameReferenceDir=<authorized-reference-directory>. Required environment: .NET 3.5 targeting tools and real authorized references. Expected artifact: bin/Release/FullMapUnlockAndExpansion.dll only; do not redistribute game DLLs. Compilation, discovery in Content Manager, callback ordering and lifecycle behavior remain unverified. No workflow or release changes are included.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Lifecycle boundary checks: Tests/LifecycleChecks.csproj compiles the production GameObservation.cs with minimal ICities boundary substitutes. It checks unload/reload, provider replacement, stale loading release, unchanged callback values and separated price observations. These substitutes are test-only and do not validate the real ICities ABI. Pending cloud commands: dotnet restore Tests/LifecycleChecks.csproj --configfile Tests/NuGet.Config; dotnet build Tests/LifecycleChecks.csproj -c Release --no-restore; dotnet Tests/bin/Release/net8.0/LifecycleChecks.dll. Run sequentially with preview checks (shared test output directory). No local execution was performed.
Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

The pending game-dll workflow runs preview and lifecycle contracts, then compiles the production observation sources against pinned cloud game references. Only the DLL and provenance are retained for 14 days; no Workshop upload or Release is performed. Signature: OpenAI-Codex.
