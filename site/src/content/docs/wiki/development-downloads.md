---
title: Development downloads
description: "Download and install the experimental Development builds of the three Cities: Skylines 1 Modpack mods."
---

The individual editions below are public GitHub prereleases; AI Gaming is historical and paused. Download the individual release assets and follow the full instructions on the corresponding release page. Check each mod's Steam Workshop page for stable-edition availability. Development editions have been removed from Steam Workshop; GitHub prereleases remain a separate distribution. GitHub's automatically generated source archives are source snapshots, not installation packages.

> **Save warning:** Development builds contain experimental tools and can damage saves. Back up your saves and use a separate test city. Disable other local and Workshop copies of the same mod so only one edition is enabled. Cloud builds and file checks do not establish in-game acceptance.

Disable any active Workshop edition in Content Manager before closing the game. For AI Improve, subscribe to Harmony through Steam and enable Harmony in Content Manager after restarting the game.
This applies to editions of the same mod; unrelated mods do not need to be disabled.

## Current bundle and foundation builds — 2 November 2026

[Download the verified three-module Development bundle](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/modpack-dev-199e88e5fc5794f8c076244c4c273a8fd8773636587ec6205850219d281b0637). Use the attached installation ZIP, not GitHub's automatically generated source archives. GitHub stores the attachment as `Cities.Skylines_1_Modpack.zip`; its display label retains `Cities Skylines_1_Modpack.zip`.

The bundle contains **AI Improve, Public Transport Planning and Public Transport Vehicle Management**. Keep `AI_Improve/` and its runtime dependency together. Put each standalone module DLL in its own mod directory; retain the included installation guides and license notices. Game reference assemblies are excluded. This is not an all-15-module bundle or proof that the modules work together in game.

The existing collector checks at 08:00, 13:00 and 20:00 Hong Kong time and supports manual dispatch. It skips duplicate publication when sources and packaging rules are unchanged. GitHub may delay scheduled runs. Its outputs are collected from existing successful builds; the newly specified cloud compile-with-Dev-Tools pipeline is not yet implemented.

Seven additional foundation game-DLL builds are listed with their exact cloud runs in the [dated project status](/Cities_Skylines_1_Modpack/wiki/project/status/). Their Actions artifacts have 14-day retention and may require GitHub sign-in; they are not complete gameplay releases or automatically included in this bundle. Some foundations do not yet contain Dev Tools. Railway and Vehicle still await first cloud game-DLL compilation.

**AI Gaming remains paused.** Its download below is an older historical preview, not a newly resumed or newly verified release.

### Installation manager and duplicate editions

Cities Skylines 1 Modpack Dev Manager is in requirements planning; no installable App is available. Per-module update detection and individual/batch installation, updates and removal are planned.

The confirmed future in-game response to a stable/Development duplicate installation is a warning only. The modules and proposed manager will not automatically disable or uninstall an edition, cancel subscriptions or block loading merely because both editions are installed. This behavior is not yet implemented across all modules and is not a compatibility guarantee. The manual installation guidance below leaves edition selection to you.

The planned Development display name is `<Module Name> - Development Version. This version has DevTool. Be careful; the development version can let your save be broken.` This does not certify that every foundation DLL already has Dev Tools. Stable publishing through an owner-approved issue, a cloud build without Dev Tools and a local upload-only runner is also a target workflow, not a completed delivery feature.

<!-- Updated by OpenAI-Codex on 2026-11-02. -->
## AI Gaming

[Download AI Gaming Development build](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/AI_Gaming-dev-41205697941-1)

1. Exit the game and disable other AI Gaming editions. Verify the downloaded files against `SHA256SUMS.txt`.
2. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AI Gaming Development` and put `AIGaming.dll` inside.
3. Create a `Companion` subdirectory and put `AIGaming.Companion.exe` in it. The DLL and Windows x64 Companion must come from the **same release**: the DLL checks the exact Companion hash. Keep the included README, license, .NET notices and build information with the installation.
4. Enable the mod in Content Manager, configure local Ollama and test on a copy of your save. Review and confirm each request. Direct game control and cloud-provider integration are not implemented; real game and Ollama request behavior remain unverified.

## AI Improve

[Download AI Improve Development build](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/AI_Improve-dev-41291471598-1)

1. Exit the game and disable other AI Improve editions. Subscribe to and enable the [Harmony Workshop dependency](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402).
2. Download `AIImprove.dll` and `CitiesHarmony.API.dll` from the **same release** and verify them against `SHA256SUMS`.
3. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AIImproveDevelopment` and place both DLLs together. Keep the included license and third-party notices with them.
4. Enable the Development build in Content Manager and use a separate test city. Follow the release page for Development tools and uninstallation.

## Automated Public Transport Planning

[Download Automated Public Transport Planning Development build](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/Automated_Public_Transport_Planning-dev-41291785222-1)

1. Exit the game and disable other Automated Public Transport Planning editions. Verify `AutomatedPublicTransportPlanning.dll` against `SHA256SUMS`.
2. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\APTPDevelopment` and put the DLL there. Do not install game reference assemblies.
3. Enable the Development build in Content Manager and use a separate test city. The prototype can preview proposals and attempt to create a selected route only after arming and explicit confirmation. Proposals do not guarantee connected or runnable routes, and successful route creation and save/reload have not been established in subscribed-game acceptance.

The [Development source at 60976d7](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/60976d7c34f73aee698af192a660abe001ec347d/README.md#existing-route-editing--development-prototype) also supports a move-only editing prototype for existing ordinary city bus lines with 3–6 street stops. It is designed to preserve the line ID and stop count without inserting or deleting stops. Review the before-and-after preview, arm editing and confirm twice before an edit is attempted. Original and target stop sites on bridges, tunnels or station-owned roads, or with unknown or custom road AI, are rejected; this does not check every road segment between stops. After a failure the code attempts best-effort recovery, which can fail or leave passenger, vehicle or path side effects. Back up saves: editing and save/reload remain unverified in game.

To return to an available Steam Workshop edition, check that mod's Workshop page, exit the game and follow the individual Development release's uninstall instructions before enabling it. Removing a Development build does not undo changes already saved to a city.

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex. -->

<!-- Author: OpenAI-Codex_module_worker. -->
