# Full Map Unlock And Expansion

Planning-stage module for Cities: Skylines 1. The intended scope is full-map access and expansion. An observation-only prototype DLL was previously compiled; it contains no DevTool and does not implement area unlocking or purchasing.

Project repository: https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack

## Validation and releases

The baseline workflow checks documentation and compiles the pure preview checks. A separate game-observation project and cloud build are configured; neither check establishes in-game behavior or implements purchasing.

The planned development distribution uses the standalone Dev Manager installer; GitHub Release attachments are intended for that installer rather than separate module releases. This does not imply that distribution for this module is ready. Gameplay acceptance requires the Steam Workshop subscribed build.

## License

Code: PolyForm Noncommercial License 1.0.0; see LICENSE. Original public documentation: CC BY-NC-SA 4.0 (https://creativecommons.org/licenses/by-nc-sa/4.0/). Copyright Kingsley.

Signature: OpenAI-Codex_Responsible: Full_Map_Unlock_And_Expansion


## Local preview prototype

Source/AreaPreview.cs classifies caller-supplied native eligibility, count and price. It does not authorize a purchase. The first-stage product goal is all 25 areas unlocked at the start without debiting city funds; handling of existing saves remains undecided. An 81-area expansion is a separate future phase.

The pure `MissingForInitial25` function accepts 25 `Initial25Tile` records, each with a declared `(x, z)`, native index and nullable unlocked value. It checks bounds, uniqueness, the 5-by-5 mapping `nativeIndex = z * 5 + x`, the reported count, and caller-supplied generation and revision values. Input record order does not determine coordinates. It returns the row-major indices reported as locked, null for invalid or incomplete input, or an empty array if the supplied snapshot reports all 25 unlocked. The output is a detached candidate set, not an execution order or authorization.

Coordinate and index labels are also supplied by the caller. Even 25 unique, consistent labels and matching counts cannot prove that each value was actually read from its claimed native coordinate; swapped values can pass. Callers must independently establish that all 25 distinct, correct native coordinates were read into a stable snapshot and that its generation and revision are authentic and current. Without that evidence this preview must not be used as a basis for game writes. No game adapter currently invokes it, and it makes no cost decision.

`PreviewInitial25NoDebit` is a separate, stateless synthetic preview of one adjacent missing area at a time. A candidate carries `RequireMoney = false`; pending or failed reported attempts produce no next candidate, and a reported success requires a newer matching-generation snapshot showing that tile unlocked. The preview does not execute a game API or change funds. Its startup flag, previous-attempt report, coordinate labels and freshness tokens are supplied by the caller and are not authenticated; in particular, a later call can report `None` after failure. It cannot enforce persistence, prevent retries across calls, establish real success, or guarantee real game funds are unchanged. Any future game adapter must validate raw x and z are each in 0–4 at the actual call site as well as proving the snapshot and lifecycle boundaries.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Observation contract and assembly evidence: [Area observation evidence](docs/AreaObservationEvidence.md). The read-only lifecycle adapter is implemented; actual startup unlocking is not.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion


## Cloud validation

The baseline workflow uses ubuntu-22.04 and its preinstalled .NET 8 SDK (latest available 8.x patch, printed and pinned per run). Missing SDK is an environment failure. Restore uses Tests/NuGet.Config with no package feeds or PackageReference. Restore, Release compilation, and executable checks are separate steps. Push to this branch, PR targeting it, or manual dispatch on this branch triggers checks with contents: read only. The baseline workflow produces no game DLL, release, or artifact.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion


## Game observation DLL

FullMapUnlockAndExpansion.csproj targets .NET Framework 3.5 with authorized ICities and UnityEngine references supplied through GameReferenceDir. It defines IUserMod, loading and area extension entry points. Eligibility and price callbacks return their input unchanged. Observation.Read returns an immutable latest callback record or null; eligibility and price are separate observations, and price has no tile identity. Values are not final eligibility after other extensions and never establish affordability. Unloading clears the current observation, but previously returned immutable objects remain readable. Observation.IsCurrent checks object identity only at the instant of the call; it neither prevents a subsequent change nor proves which city originated a callback. A delayed callback from the same provider after a city transition cannot currently be distinguished from a new-city callback. No UI or automatic polling is provided.

Main owns workflow integration. The project targets .NET Framework 3.5 and requires real authorized references. A cloud build of commit d9daec9 compiled the observation DLL on 2026-11-02; subsequent changes require their own verification. Content Manager discovery, actual callback ordering, and lifecycle behavior in the game remain unverified. Do not redistribute game reference DLLs.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Lifecycle boundary checks: Tests/LifecycleChecks.csproj compiles the production GameObservation.cs with minimal ICities boundary substitutes. It checks unload/reload, provider replacement, stale loading release, unchanged callback values and separated price observations. These substitutes are test-only and do not validate the real ICities ABI. Cloud commands: dotnet restore Tests/LifecycleChecks.csproj --configfile Tests/NuGet.Config; dotnet build Tests/LifecycleChecks.csproj -c Release --no-restore; dotnet Tests/bin/Release/net8.0/LifecycleChecks.dll. Run sequentially with preview checks (shared test output directory).
Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

Releasing the current area provider clears its current observation, but the loaded city stays active until its loading owner unloads or releases it. A new provider can then resume observation in the same loaded city; binding a provider after city unload does not reactivate observation. These are synthetic lifecycle checks, not a guarantee about actual game callback order.

Signature: OpenAI-Codex_Full_Map_Unlock_And_Expansion

The game-dll workflow runs preview and lifecycle contracts, then compiles the production observation sources against pinned cloud game references. Only the DLL and provenance are retained for 14 days; no Workshop upload or Release is performed. Signature: OpenAI-Codex.
