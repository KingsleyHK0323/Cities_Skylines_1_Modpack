# Realistic Vehicle Traffic Control

Early pure-model component of Cities Skylines 1 Modpack. The intended scope is vehicle traffic rules, signals, turning restrictions and lane allocation. No game behavior is implemented yet.

## Project links

- [Canonical repository](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack)
- [Module branch](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_Vehicle_Traffic_Control)
- [Project website](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/)

## Development status

This branch now contains a pure C# model library and executable behavior checks. It does not contain a game adapter, in-game UI, game-ready mod DLL, or validated gameplay feature. The .NET 8 model assembly is not a Cities: Skylines mod and must not be installed in the game.

The first implemented batch previews **one selected road approach**, not all movements at a junction. It accepts immutable caller-supplied lane observations, checks explicit movement legality, and proposes bounded, ordered exit groups. It does not measure traffic or read live game data. Unknown data returns `Unknown`; unresolved external rules, mixed rail/bus lanes, unsupported geometry or missing legal exits return `Unsupported`, with no partial proposal. Ordinary sidewalk lanes are excluded from car allocation without rejecting the entire road.

`DataOrigin.SyntheticFixture` labels all current check fixtures. `GameCapture` is a reserved caller-supplied label, not evidence that game capture exists. Any future reader must independently establish provenance, segment/build identity, orientation and legal targets; the model cannot verify those facts. `Ready` means only that the supplied model supports a preview, never permission to apply it.

The in-memory preview lifecycle rejects superseded, duplicate, cross-city and mismatched-topology callbacks. Currentness requires a fresh observation with the same full fingerprint. Without a game reader, it cannot automatically detect changes in a running city.

### Current model limits

- Three- or four-segment node metadata, exactly one general-car approach segment.
- At most eight approach car lanes, eight exits per turn direction and 32 observed lanes including sidewalks.
- Explicit left/forward/right exits only; complex geometry, U-turns, rail and bus mixtures are unsupported.
- All observations of the same segment must have a consistent build index; observed distinct segments cannot exceed the declared node segment count.
- Direction normalization uses reader-supplied lateral ranks and orientation flags. It does not infer game handedness or `NetSegment.Invert` from an ID.
- A stable bounded allocation is validated against every incoming lane's allowed target IDs. No alternative search is attempted if that allocation is illegal. A refusal does not prove there is no possible route.
- No signals, lane buffers, paths, vehicle counts, transfer offers or PathUnits capacity are written. No mod is disabled or unsubscribed.

A future game adapter still needs verified game API signatures and reviewed, licensed game reference assemblies (including ICities and Unity dependencies as required). The current pure model has no game assembly or third-party package references. Its C# 3 language restriction is checked separately from game framework compatibility: compiling against .NET 8 does not establish .NET Framework 3.5 binary compatibility.

## Automation and distribution

The branch workflow runs on pushes and pull requests targeting this branch, and manually. GitHub-hosted Ubuntu checks repository text/licensing and compiles and runs the complete pure-model behavior suite using the .NET 8 SDK. No local runner, workflow or build is required. It does not publish artifacts or a release.

Cloud verification command: `dotnet run --project Tests/VehicleTrafficChecks.csproj --configuration Release`. The console reports named failures and exits nonzero if any check fails. Checks use synthetic fixtures only; their success is not gameplay acceptance. Test-first history retains an intentional failing cloud run before the model implementation.

Future cloud builds will attach this module's DLL and necessary files directly to its own GitHub Release, without ZIP archives. Release publication remains disabled until dependencies and validation are ready. Steam Workshop stable uploads remain a separate, explicitly authorized local operation.

## Licensing

Code and original repository documentation: PolyForm Noncommercial License 1.0.0; see LICENSE. Copyright Kingsley. This project is source-available and restricts commercial use. Website content, if separately created, follows the website's CC BY-NC-SA policy; this branch contains no website.

Official license source: https://github.com/polyformproject/polyform-licenses/blob/HEAD/PolyForm-Noncommercial-1.0.0.md

Signature: OpenAI-Codex_Responsible: Realistic_Vehicle_Traffic_Control
