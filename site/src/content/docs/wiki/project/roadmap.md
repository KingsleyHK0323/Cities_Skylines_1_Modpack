---
title: Roadmap
description: "Where each Modpack mod is today and what it is working towards."
---

> <span lang="en">Distribution guidance: If Dev Manager Windows Setup.exe is published, check GitHub Releases. Module availability varies; use verified builds shown in the app when available. Historical mod-release and bundle-download instructions below are not current download methods. See the <a href="/Cities_Skylines_1_Modpack/wiki/development-downloads/">Development guide</a>.</span>

<!--
> <span lang="en">Distribution update (3 November 2026): GitHub Releases is planned for the Dev Manager Windows installer. Individual mod artifacts are planned on separate branches; the AI Improve and Automated Public Transport Planning candidates and the installer are not yet published. Earlier mod-release and bundle-download instructions below are historical. See the <a href="/Cities_Skylines_1_Modpack/wiki/development-downloads/">Development guide</a>.</span>
-->

<!-- Distribution update: OpenAI-Codex. -->

<p lang="en">Status update, 2 November 2026: all 15 modules have Workshop items, but availability varies and an item is not proof of gameplay readiness. The project has 22 replacement directions; see the <a href="/Cities_Skylines_1_Modpack/wiki/project/status/">complete status index</a>. AI Gaming development is paused. Earlier roadmap entries below are historical context; they do not establish current availability or combined-game acceptance.</p>

<details>
<summary lang="en">Historical mod-release guidance — not a current download</summary>

> **Development build update, 2 November 2026:** Records from 31 October describe separate experimental builds for AI Gaming, AI Improve, and Automated Public Transport Planning. Current public availability of installable binaries is unverified and may change; check the actual assets on [GitHub Releases](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases), as automatically generated source archives are not installable binaries. AI Gaming development is paused; its earlier build required a matching Windows x64 Companion and its game runtime remains unverified. The Automated Public Transport Planning prototype can attempt selected route creation after confirmation. See [Development downloads and installation](/Cities_Skylines_1_Modpack/wiki/development-downloads/).

</details>

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
| Player-confirmed Ollama Local advice | Earlier Development packaging paired a DLL and Companion; current availability and game runtime unverified |
| Watch AI play and limited game actions | Planned; not implemented |

## All mods

The Modpack mods are designed to work together, but combined operation has not been accepted in game. The public AI Gaming Development source `e7de649f` detects providers and reads interoperability manifests; calls that execute another module's capabilities have not been established. AI Gaming development remains paused.

Follow progress in the [change log](/Cities_Skylines_1_Modpack/change-log/).

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex_status_worker. -->

<!-- Updated by OpenAI-Codex on 2026-11-02. -->

<!-- Updated by OpenAI-Codex_wiki_update on 2026-11-02. -->

<!-- Historical disclosure: OpenAI-Codex. -->
