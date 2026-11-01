---
title: Development downloads
description: "Download and install the experimental Development builds of the three Cities: Skylines 1 Modpack mods."
---

The three Development builds below are public GitHub prereleases. Download the individual release assets and follow the full instructions on the corresponding release page. Stable versions remain distributed through Steam Workshop. Development editions have been removed from Steam Workshop; GitHub prereleases remain a separate distribution. GitHub's automatically generated source archives are source snapshots, not installation packages.

> **Save warning:** Development builds contain experimental tools and can damage saves. Back up your saves and use a separate test city. Disable other local and Workshop copies of the same mod so only one edition is enabled. Cloud builds and file checks do not establish in-game acceptance.

Disable any active Workshop edition in Content Manager before closing the game. For AI Improve, subscribe to Harmony through Steam and enable Harmony in Content Manager after restarting the game.
This applies to editions of the same mod; unrelated mods do not need to be disabled.

## AI Gaming

[Download AI Gaming Development build](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/ai-gaming-dev-41185662728-1)

1. Exit the game and disable other AI Gaming editions. Verify the downloaded files against `SHA256SUMS.txt`.
2. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AI Gaming Development` and put `AIGaming.dll` inside.
3. Create a `Companion` subdirectory and put `AIGaming.Companion.exe` in it. The DLL and Windows x64 Companion must come from the **same release**: the DLL checks the exact Companion hash. Keep the included README, license, .NET notices and build information with the installation.
4. Enable the mod in Content Manager, configure local Ollama and test on a copy of your save. Review and confirm each request. Direct game control and cloud-provider integration are not implemented; real game and Ollama request behavior remain unverified.

## AI Improve

[Download AI Improve Development build](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/ai-improve-dev-41185802009-1)

1. Exit the game and disable other AI Improve editions. Subscribe to and enable the [Harmony Workshop dependency](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402).
2. Download `AIImprove.dll` and `CitiesHarmony.API.dll` from the **same release** and verify them against `SHA256SUMS`.
3. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AIImproveDevelopment` and place both DLLs together. Keep the included license and third-party notices with them.
4. Enable the Development build in Content Manager and use a separate test city. Follow the release page for Development tools and uninstallation.

## Automated Public Transport Planning

[Download Automated Public Transport Planning Development build](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/aptp-dev-41185686621-1)

1. Exit the game and disable other Automated Public Transport Planning editions. Verify `AutomatedPublicTransportPlanning.dll` against `SHA256SUMS`.
2. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\APTPDevelopment` and put the DLL there. Do not install game reference assemblies.
3. Enable the Development build in Content Manager and use a separate test city. The prototype can preview proposals and attempt to create a selected route only after arming and explicit confirmation. Proposals do not guarantee connected or runnable routes, and successful route creation and save/reload have not been established in subscribed-game acceptance.

To return to a stable version, exit the game and follow the individual release's uninstall instructions before enabling that mod's Steam Workshop edition. Removing a Development build does not undo changes already saved to a city.

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex. -->
