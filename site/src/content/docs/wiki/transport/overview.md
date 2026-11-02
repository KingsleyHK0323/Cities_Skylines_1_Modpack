---
title: Automated Public Transport Planning
description: "Survey diagnostics, Development route proposals and the limits of confirmed route-creation attempts."
---

> **Development build update, 31 October 2026:** The GitHub Development prototype can attempt to create a selected route after arming and confirmation; it can change a save and is not read-only. Successful route creation and save/reload have not been established in subscribed-game acceptance. See [Development downloads and installation](/Cities_Skylines_1_Modpack/wiki/development-downloads/).

Automated Public Transport Planning's Development prototype estimates city-bus routes and can attempt to create a selected proposal after arming and confirmation. The prototype may change saves and does not guarantee runnable routes. Other transport types remain a future direction.

## Survey diagnostics

- **Surveys your city when it loads.** It reads roads, stops and the transit network in the background. This survey is read-only; the separate Development creation prototype can change a save.
- **Diagnostics in the mod options.** Each button writes its result to the game log:

| Button | What it reports |
| --- | --- |
| Write a test line to the log | Confirms the mod is loaded and responding. |
| List bus prefabs in detail | The bus models available in your game. |
| Measure planning costs | How long planning work would take on your city. Causes a brief pause. |
| Capture a world snapshot | The roads and stops the planner would work with. |
| Build the route graph | The network of possible connections between stops. |

- **Language selector.** 11 languages, switchable in the mod options without restarting.

## What it does not do yet

The older 0.2.0 Release preview does not create, change or suggest bus lines. The Development prototype focuses on city buses; additional transport types remain future directions. See the [roadmap](/Cities_Skylines_1_Modpack/wiki/project/roadmap/).

## Development Version

The Development edition has been removed from Steam Workshop. Guidance for experimental builds is available in the [GitHub Development installation guide](/Cities_Skylines_1_Modpack/wiki/development-downloads/). Back up your save before using them.

- **Planning preview (being tested).** The *Generate a read-only planning preview* button estimates city-bus lines from your city's demand and lists them with their scores. It does not create or change any line. The estimates are not yet checked against the game's own pathfinding.
- **Route-creation prototype.** A selected proposal can be submitted for creation after the feature is armed and each attempt is confirmed. Proposals do not guarantee connected or runnable routes. Successful creation and save/reload remain unverified in subscribed-game acceptance; use a backup and a separate test city.
- **Existing-line stop-move prototype.** The [historical Development release](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases/tag/Automated_Public_Transport_Planning-dev-41252417067-1), built from [source `60976d7`](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/commit/60976d7c34f73aee698af192a660abe001ec347d), included manual and automatic move-only editing for existing regular city-bus lines with 3–6 stops. The prototype was intended to preserve the line ID and number of stops. Preview the changes, enable the feature and provide two confirmations before an attempt. Both the original and target stop locations must use exact `RoadAI` and pass segment-eligibility and placement checks. Stops on bridges, tunnels, station roads or unknown/custom-AI roads are conservatively rejected. An attempt may change the save; recovery is non-atomic and may fail, and cannot guarantee complete restoration of passengers, vehicles or paths. Back up first. In-game behavior and save/reload have not been accepted.

Availability check, 2 November 2026: current installable attachments could not be verified. Check [GitHub Releases](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases) and the actual release attachments before installing; automatically generated source archives are not installable mod files.

## Helping test

Run the diagnostics on cities of different sizes and include the log output when you [report results or problems](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/new/choose). This helps tune the planner before it arrives.

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex. -->

<!-- Author: OpenAI-Codex_status_worker. -->

<!-- Author: OpenAI-Codex_transport_update. -->
