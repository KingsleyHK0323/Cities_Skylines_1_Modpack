---
title: Roadmap
description: "Where each Modpack mod is today and what it is working towards."
---

<p lang="en">Status update, 2 November 2026: the project includes 15 modules at different stages and 22 replacement directions. See the <a href="/Cities_Skylines_1_Modpack/wiki/project/status/">complete status index</a>. AI Gaming development is paused. Earlier roadmap entries below are historical context; they do not establish current availability or combined-game acceptance.</p>

> **Development build update, 31 October 2026:** Separate GitHub prereleases are available for AI Gaming, AI Improve, and Automated Public Transport Planning. AI Gaming has a matching Windows x64 Companion asset, while its game runtime remains unverified. Automated Public Transport Planning can attempt selected route creation after confirmation. See [Development downloads and installation](/Cities_Skylines_1_Modpack/wiki/development-downloads/).

These are directions, not release dates. Experimental Development builds can damage saves; back up before testing.

## AI Improve

**Now:** A Workshop release is recorded, but its current availability has not been rechecked. The public [Development source at `75ef944`](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/75ef94407962817051cef4aa74ec6fbc1831fd02/SettingsPageUI.cs) statically contains 25 switches; the stable Workshop count was not independently verified.

**Next:** in-game screenshots for this guide, and continued tuning based on player reports.

## Automated Public Transport Planning

| Stage | Status |
| --- | --- |
| Network survey and diagnostics | Read-only survey; separate creation prototype may change saves |
| Demand analysis and candidate city-bus lines | Being tested in the Development Version |
| Confirmed route-creation attempts | Development prototype; game acceptance pending |
| Existing-line stop moves | Public Development prototype `60976d7`; preview, enable and two confirmations precede an attempt; game and save/reload acceptance pending |
| Other public transport types (trams, metro, trains, ferries and more), one at a time | Later |

It currently covers regular city buses only.

## AI Gaming

| Stage | Status |
| --- | --- |
| Player-confirmed Ollama Local advice | Development assets include matching DLL and Companion; game runtime unverified |
| Watch AI play and limited game actions | Planned; not implemented |

## All mods

The Modpack mods are designed to work together, but combined operation has not been accepted in game. The public AI Gaming Development source `e7de649f` detects providers and reads interoperability manifests; calls that execute another module's capabilities have not been established. AI Gaming development remains paused.

Follow progress in the [change log](/Cities_Skylines_1_Modpack/change-log/).

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex_status_worker. -->

<!-- Updated by OpenAI-Codex on 2026-11-02. -->
