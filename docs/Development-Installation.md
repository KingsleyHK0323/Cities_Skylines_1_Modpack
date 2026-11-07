# AI Gaming — Development Build

> **Current distribution:** The manual Release-asset instructions below are historical. Dev Manager is the intended route for available development builds, but AI Gaming availability has not yet been verified.
<!-- Signature: OpenAI-Codex -->

## Module Overview

AI Gaming previews local Ollama advice using player-selected, reviewed city data. It does not implement cloud models or game control.

## Build Information and Changes

This build integrates shared settings/in-game request state, native navigation, bounded full-text display windows and confirmation lifecycle guards. Exact source commit, reference commit, run, version and Companion hash are in build-info.json. Checks do not establish gameplay acceptance.

## Development Build Risk Warning

This Windows x64 preview can damage saves. Back up saves and test with a separate city. Do not enable or install it alongside the stable Release or another AI Gaming copy. Cloud compilation is not gameplay acceptance.

## Downloaded Files and Their Purpose

| File | Purpose |
| --- | --- |
| AIGaming.dll | Development game mod |
| AIGaming.Companion.exe | Matching self-contained Windows x64 local AI companion |
| README.md | Installation, removal and risk information |
| LICENSE | Unchanged module license |
| DOTNET-LICENSE.txt | Bundled .NET runtime and host license |
| DOTNET-THIRD-PARTY-NOTICES.txt | Bundled runtime third-party notices |
| Steam_Workshop_Link.txt | Source-recorded stable item links |
| build-info.json | Build and pairing provenance |
| SHA256SUMS.txt | Asset integrity checksums |

## Installation Instructions

1. Exit the game. Disable the stable AI Gaming subscription in Content Manager before using this preview. Ensure its DLL is not loaded alongside this copy.
2. Download the individual release assets. Verify SHA256SUMS.txt. Never mix DLL and Companion files from different releases: the DLL requires the exact Companion hash.
3. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AI Gaming Development` and put `AIGaming.dll` in it.
4. Create a `Companion` subdirectory inside that mod directory and put `AIGaming.Companion.exe` there. Keep README.md, LICENSE, both DOTNET notices, and build-info.json with the installation. No separate .NET runtime installation is required for the self-contained Companion; native runtime components are bundled and may extract at launch.
## Dev Tool Enablement and Usage

1. Start Cities: Skylines 1, open Content Manager > Mods, and enable the AI Gaming Development entry. DevTool is compiled in (`DevTools=true`); there is no separate build switch to enable. Check its Development warning before loading a test save.
2. Configure local Ollama and select the local AI request options in the mod settings. Review the city-data preview and separately confirm each local request. This is a local Ollama preview; cloud models and direct game control are not implemented.

## Uninstallation and Return to Stable Release

Exit the game, disable this preview in Content Manager, and remove the dedicated `AI Gaming Development` directory you created. Re-enable the stable version only after the preview is removed. Restore a backup if a test save becomes unusable.

## License, Dependencies, and Integrity Verification

AI Gaming is source-available under the included unchanged PolyForm Noncommercial License 1.0.0, copyright Kingsley. The bundled .NET runtime and host use the official license and third-party notices supplied as DOTNET-LICENSE.txt and DOTNET-THIRD-PARTY-NOTICES.txt. build-info.json records their exact official source URLs and runtime version, source commit, shared-reference commit and cloud run. Game reference assemblies are build inputs only and are not distributed here.

Steam_Workshop_Link.txt lists stable item links recorded in project source; these are not live Steam status claims. This prerelease is not marked Latest. No ZIP package is supplied; GitHub may display its own automatic source-archive links.

Signature: OpenAI-Codex_Responsible: AI_Gaming

## Current distribution status

The manual Release-asset instructions above describe a historical route. New
module GitHub Release publishing is disabled; the intended route for available development builds is the
Dev Manager Windows installer. AI Gaming availability in
Dev Manager has not yet been verified. The paired DLL and Companion must still
match before use.

<!-- Signature: OpenAI-Codex -->
