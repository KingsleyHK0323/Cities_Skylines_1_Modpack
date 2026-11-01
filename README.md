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

