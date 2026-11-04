---
title: Getting started
description: "Subscribe, pick the right version and enable the Modpack mods in Cities: Skylines 1."
---

> <span lang="en"><strong>Development downloads:</strong> If a Dev Manager Windows Setup.exe is published, look for it on <a href="https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases">GitHub Releases</a>. Module availability can vary; use the builds the app identifies as verified when available. This does not establish game acceptance. AI Gaming development is paused. Historical module-release and bundle information below is not a current download entry.</span>

<!-- Earlier notice retained for traceability. OpenAI-Codex_distribution_docs.
> <span lang="en"><strong>Distribution plan — 3 November 2026:</strong> GitHub Releases is intended only for the Dev Manager Windows Setup.exe installer. Separate artifact branches are planned as the manager’s mod sources. Only the new artifact-branch changes for AI Improve and Automated Public Transport Planning are current candidates; these candidates have not been committed, pushed or published. The Windows installer has not yet been built or published. A local portable manager candidate and synthetic tests do not establish real downloads, installation or in-game acceptance. AI Gaming remains paused. Earlier GitHub mod-release and bundle instructions below describe the previous distribution approach, not availability under this new plan.</span>
-->

<!-- Updated by OpenAI-Codex_distribution_docs on 2026-11-03. -->

<details>
<summary lang="en">Historical download only — <!-- not a current download; new installer not released -->not a current mod-download method</summary>

> **Development builds on GitHub:** Current public availability of installable binaries is unverified and may change. Check the actual assets on [GitHub Releases](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/releases); automatically generated source archives are not installable binaries. AI Gaming development is paused. See [Development downloads and installation](/Cities_Skylines_1_Modpack/wiki/development-downloads/) for dependencies and save risks. Do not enable multiple copies of the same mod.

</details>

## 1. Subscribe to the dependencies

- **[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402)** — required by AI Improve. It is a shared library that many Cities: Skylines mods use to change game behaviour safely.
- **UnifiedUI** — optional for AI Improve. In the public [Development source at `75ef944`](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/75ef94407962817051cef4aa74ec6fbc1831fd02/IngameUI.cs), its panel provides scans for empty intercity trains and buses, plus a settings reset. Individual feature settings are on the mod's Options page. This description is based on static source review; the stable Workshop interface has not been independently verified in game.

Automated Public Transport Planning and AI Gaming need no other mods. An AI Gaming Development build, if an official installable build is available, also requires its matching Windows x64 Companion executable.

## 2. Choose Release or Development

Workshop release records and current availability differ by mod and have not been rechecked here. Check each mod's page for its links.

<details>
<summary lang="en">Historical download only — <!-- not a current download; new installer not released -->not a current mod-download method</summary>

 An experimental GitHub Development build, if an official installable build is available, must be downloaded and installed separately.

</details>


<details>
<summary lang="en">Historical edition comparison — not a current mod-download method</summary>

| | Stable Release | GitHub Development build |
| --- | --- | --- |
| For | Everyday play | Trying changes early and helping test |
| Updates | Through Steam Workshop when published | <details><summary lang="en">Historical download only — <!-- not a current download; new installer not released -->not a current mod-download method</summary>Check GitHub Releases and the build's installable assets</details> |
| Testing tools | Not included | Depends on the build; check its documentation |

</details>

<p lang="en">For an available stable edition, check the mod’s Steam Workshop page. Back up saves before testing.</p>
<!-- Historical edition comparison: OpenAI-Codex. -->

**Enable only one copy of each mod.** Disable other local or Workshop editions before testing a GitHub Development build. Links are on each mod's page: [AI Improve](/Cities_Skylines_1_Modpack/ai-improve/), [Automated Public Transport Planning](/Cities_Skylines_1_Modpack/automated-public-transport-planning/), [AI Gaming](/Cities_Skylines_1_Modpack/ai-gaming/).

## 3. Enable and configure

1. Start the game and open **Content Manager → Mods**.
2. Enable the mod (and Harmony, for AI Improve).
3. In Content Manager, open the mod's **Options** page to adjust it. The AI Improve [settings guide](/Cities_Skylines_1_Modpack/wiki/ai-improve/settings/) uses [public Development source `75ef944`](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/blob/75ef94407962817051cef4aa74ec6fbc1831fd02/SettingsPageUI.cs); installed versions may differ and in-game behavior has not been verified.

## 4. If something goes wrong

Open an issue with the [issue forms](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/new/choose). Include the mod and version, your other mods, the steps that lead to the problem and the relevant part of the game's output log. The [FAQ](/Cities_Skylines_1_Modpack/wiki/ai-improve/faq/) explains where to find the log. Remove your Windows username or other private details before posting.

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex. -->

<!-- Author: OpenAI-Codex_status_worker. -->
<!-- Updated by OpenAI-Codex on 2026-11-01. -->

<!-- Updated by OpenAI-Codex_wiki_update on 2026-11-02. -->

<!-- Updated by OpenAI-Codex_distribution_docs on 2026-11-03. -->

<!-- Updated by OpenAI-Codex_distribution_docs on 2026-11-03. -->
