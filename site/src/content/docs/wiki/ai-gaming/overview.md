---
title: AI Gaming
description: "What the 0.4.0 Development source does, its confirmation flow, and its packaging and runtime limits."
---

> <span lang="en">Distribution guidance: If Dev Manager Windows Setup.exe is published, check GitHub Releases. Module availability varies; use verified builds shown in the app when available. Historical mod-release and bundle-download instructions below are not current download methods. AI Gaming development remains paused. See the <a href="/Cities_Skylines_1_Modpack/wiki/development-downloads/">Development guide</a>.</span>

<!--
> <span lang="en">Distribution update (3 November 2026): GitHub Releases is planned for the Dev Manager Windows installer. Individual mod artifacts are planned on separate branches; the AI Improve and Automated Public Transport Planning candidates and the installer are not yet published. Earlier mod-release and bundle-download instructions below are historical. AI Gaming remains paused and is not one of the current candidates. See the <a href="/Cities_Skylines_1_Modpack/wiki/development-downloads/">Development guide</a>.</span>
-->

<!-- Distribution update: OpenAI-Codex. -->

<details>
<summary lang="en">Historical mod-release guidance — not a current download</summary>

> **Historical Development update, 31 October 2026:** An earlier release included a Windows x64 Companion and matching DLL as individual assets. Development is paused and current public availability is unverified. In-game behavior remains unverified. See [Development downloads and installation](/Cities_Skylines_1_Modpack/wiki/development-downloads/).

</details>

The **[0.4.0 Development source at e7de649](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/e7de649f58c775f0692ef74d62eed351cf2d6607/Source/LocalAiRequestController.cs)** offers player-confirmed advice from Ollama Local through a Windows x64 Companion. It does not control the simulation. Other play modes remain planned.

## What the 0.4.0 source implements

- Choose six basic fields: game date, population, funds, residential demand, commercial demand and workplace demand. Four further groups cover utilities, built zoned buildings, traffic and public transport, and city service status.
- All six fields and four groups start selected and can be deselected. On request, the separate city-summary command can still write the game date, population, funds, residential demand, commercial demand and workplace demand to the local game log.
- Identifiers for DLC, creator packs and radio stations reported by the game as installed and available are included automatically, with no separate selection control.
- Enter an Ollama model name and a question, then review the complete data, model and question in the preview before sending.
- Confirm each request separately. The model response is shown as advice only and is never converted into a game action.

## Package and runtime status

<details>
<summary lang="en">Historical mod-release guidance — not a current download</summary>

An earlier GitHub Development release packaged the DLL and self-contained Windows x64 Companion as separate assets. If matching official assets are available, use files from the same release: the DLL checks the exact Companion hash. Published assets and cloud builds do not establish successful in-game use or Ollama requests; both remain unverified. Back up saves and use a separate test city.

</details>

## Local data and Ollama

After each confirmation, AI Gaming sends the previewed data, model name and question through the local Companion to Ollama on this PC. Ollama may route inference to a cloud service depending on its own configuration; AI Gaming cannot inspect or guarantee that configuration. The 0.4.0 source has no cloud-provider adapter or API-key entry/storage. Do not include city names, account details, secrets or personal information in the free-text question.

## Planned modes

Watching an AI build a city and letting it perform limited game actions are planned ideas, not implemented features. The Development source detects providers and reads interop manifests; calls to other mods' capabilities and interoperability in game have not been established.

See the [roadmap](/Cities_Skylines_1_Modpack/wiki/project/roadmap/) for progress.

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->
<!-- Author: OpenAI-Codex_module_worker. -->
<!-- Author: OpenAI-Codex_gaming_update. -->

<!-- Historical disclosure: OpenAI-Codex. -->
