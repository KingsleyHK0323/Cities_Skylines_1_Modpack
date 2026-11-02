---
title: AI Improve compatibility
description: "How AI Improve behaves next to other popular Cities: Skylines 1 mods."
---

The checked [Development source at 75ef944](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/75ef94407962817051cef4aa74ec6fbc1831fd02) detects certain mods and contains conditional behavior for some combinations. Compatibility depends on the versions and conditions involved; in-game acceptance remains pending.

| Mod | What AI Improve does |
| --- | --- |
| **Harmony** | Required. AI Improve uses it to change game behaviour. |
| **UnifiedUI** | Optional. The checked Development source provides a panel for empty intercity train and bus scans and settings reset; individual feature switches are in Content Manager. A scan may offer to remove vehicles after confirmation. See the [in-game guide](/Cities_Skylines_1_Modpack/wiki/ai-improve/in-game/) for its conditions and limitations; installed versions may differ. |
| **Improved Public Transport**, **Express Bus Services** | The checked source conditionally disables *Shorten empty stop dwell* and *Unbunch vehicles on a line* when either mod is detected. |
| **Transfer Manager CE** | When its fire dispatch is active, the checked source leaves fire selection to it and retains the per-building responder cap. |
| **Traffic Manager: President Edition (TM:PE)** | The checked source implements a candidate adjustment for emergency-vehicle pathfinding costs. Applying it depends on the TM:PE version and matching expected instructions. If rejected, the original logic is preserved. Check the output log for warnings; in-game acceptance remains pending. |
| **Real Time** | Detected. Both mods touch the fire-dispatch step; in-game compatibility has not been established. |
| **SingleTrainTrackAI**, **Reversible Tram AI** | Detected. Detection alone does not establish compatibility. |

AI Improve does not change how many passengers any vehicle carries.

## Release and Development together

Do not enable both versions of AI Improve. They are the same mod and will conflict.

## Reporting a conflict

If you see a problem with another mod, open an [issue](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/new/choose) with your game version, full mod list, the AI Improve feature involved, steps to reproduce and the relevant part of the output log (see the [FAQ](/Cities_Skylines_1_Modpack/wiki/ai-improve/faq/)).

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->
<!-- Author: OpenAI-Codex_module_worker. -->
<!-- Updated by OpenAI-Codex on 2026-11-01. -->
