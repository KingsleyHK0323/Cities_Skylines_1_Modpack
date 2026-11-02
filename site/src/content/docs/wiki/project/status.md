---
title: Module status and replacement targets
description: "A dated overview of 15 modules and 22 confirmed replacement directions, with implementation and acceptance kept separate."
---

Status snapshot: **2 November 2026**. The project contains **15 modules at different stages**, not 15 finished downloads. The 22 external targets below are confirmed replacement directions. They are not completed replacements, permission to unsubscribe, or proof of compatibility.

Design delivery, source implementation, cloud compilation, downloadable artifacts, public releases and game acceptance are separate milestones. The project has confirmed responsibility areas, but that does not mean the corresponding features have been migrated or accepted in game; implementation details and interfaces may remain proposals. No complete replacement has been established by the evidence used for this snapshot.

## Current delivery snapshot — 2 November 2026

Of the 15 gameplay modules, **10 non-paused modules have verified successful game-DLL cloud builds**. AI Gaming retains an older preview and remains paused. Railway and Vehicle have source batches awaiting cloud compilation; Intercity and Wireless still need their minimal game entry-point batches. A compiled foundation DLL is not a completed feature set or game-load acceptance.

| Recent game DLL | Verified cloud run | Current capability boundary |
| --- | --- | --- |
| Realistic City Time | [41382255222](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41382255222) | Observation only; 1:1 time control and acceleration are not implemented |
| Full Map Unlock And Expansion | [41383511005](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41383511005) | Observation only; no unlocking or purchasing |
| Realistic Urban Population | [41390004437](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41390004437) | Inactive foundation; no population or demand writes |
| Realistic City First Response | [41391816077](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41391816077) | Inactive foundation; no dispatch, fire suppression or traffic changes |
| Realistic Border Control | [41403613247](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41403613247) | Inactive foundation; no passenger quotas or live passenger reader |
| Realistic Import Export Control | [41404552970](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41404552970) | Inactive foundation; no cargo matching or economy control |
| Realistic Pedestrian Traffic Control | [41405888872](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41405888872) | Inactive foundation; no crossing-rule, signal, path or marking changes |

AI Improve, Public Transport Planning and Public Transport Vehicle Management account for the other three verified DLL builds. The recent foundation artifacts are retained for 14 days and may require GitHub sign-in; they are not individual public releases. Download availability must be checked separately from historical build success.

### Distribution and planned behavior

The deployed bundle collector gathers existing verified AI Improve, Planning and Vehicle Management outputs. It is scheduled for 08:00, 13:00 and 20:00 Hong Kong time, with manual dispatch; GitHub may delay runs. The new target pipeline will instead compile Development modules with real Dev Tools in the cloud before packaging and release. That replacement pipeline is **not yet implemented**. The planned stable pipeline uses an owner-approved issue, cloud builds without Dev Tools, and a local upload-only runner; it is not fully implemented.

The proposed Cities Skylines 1 Modpack Dev Manager has a local branch and requirements notes, but **no installable App**. Its intended features are per-module update detection and individual/batch installation, updates and removal.

The confirmed future duplicate-install behavior is a warning when stable and Development editions of the same module are installed. It will not automatically disable or uninstall either edition, cancel subscriptions or block loading. This warning has not yet been implemented across the modules and does not establish compatibility. The planned Development display-name suffix is:

```text
<Module Name> - Development Version. This version has DevTool. Be careful; the development version can let your save be broken.
```

This is a naming requirement, not proof that every foundation build already contains Dev Tools. Automatic traffic-light management belongs to Vehicle Traffic Control's planned scope and is not implemented or enabled in the current foundation.

**Intersection Marking Tool is not a replacement target.** Advanced Stop Selection Revisited, Advanced Building Level Control and Unlimited Outside Connections Revisited remain unconfirmed candidates; no new building-control module has been approved.

<!-- Updated by OpenAI-Codex on 2026-11-02. -->
## Earlier capability and scope context (1 November)

| Module | Evidence stage | Availability and acceptance boundary |
| --- | --- | --- |
| [AI Gaming](/Cities_Skylines_1_Modpack/ai-gaming/) | Development paused by the project owner; an earlier preview exists | Existing documentation is historical context, not resumed work. Game runtime remains unverified. |
| [AI Improve](/Cities_Skylines_1_Modpack/ai-improve/) | Existing implementation; responsibility and migration inventory delivered | Confirmed responsibility areas include PathUnits, CitizenUnits and cross-transport route coordination. New responsibilities and migrations are not established as complete; combined-mod acceptance is pending. |
| [Automated Public Transport Planning](/Cities_Skylines_1_Modpack/automated-public-transport-planning/) | Existing Development prototype; shared-stop gap analysis delivered | Can attempt confirmed route creation or existing-line stop moves and change saves. Shared-stop replacement, in-game operation and save/reload acceptance are not established. |
| Realistic Urban Population | Adjustable residential/commercial/industrial or work demand direction confirmed | Each category can use native demand or a fixed target from 0–100; native is the default. Industrial/office coupling remains to be verified; game implementation is not accepted. |
| Realistic City First Response | First-version design and migration inventory delivered | Emergency response is a confirmed responsibility; fire-response details and migration remain unaccepted in game. |
| Realistic Vehicle Traffic Control | Road-feature inventory and single-writer design delivered | Initial release: observation/preview; human-confirmed control is planned for later stages. Lane and signal details remain design work. |
| Realistic Pedestrian Traffic Control | Pedestrian/crosswalk design and interface plan delivered | Initial release: observation/preview; human-confirmed control is planned for later stages. Restrictions, visual changes and integration remain design work. |
| Realistic Railway Traffic Control | Railway safety model and migration plan delivered | Initial release: observation/preview; human-confirmed control is planned for later stages. Rail safety responsibility does not establish migrated or accepted control. |
| Realistic Border Control | Passenger-data and quota-preview design delivered | Initial release: observation/preview; human-confirmed control is planned for later stages. Active arrival limiting is not established. |
| Realistic Import Export Control | Freight-matching and trade design delivered | Initial release: observation/preview; human-confirmed control is planned for later stages. Active freight matching is not established. |
| Intercity Resource Sharing | Water/electricity/heat exchange design delivered | Data sources, conservation and settlement are design work, not control of another save. |
| Automated Public Transport Vehicle Management | Read-only bus observation implemented; DLL compiled and delivered as an Actions artifact | No public Release or Workshop item. Fleet-control proposals and subscribed-game acceptance remain pending. |
| Full Map Unlock And Expansion | 25-area milestone/save design delivered | 25-area purchasing and 81-area expansion are separate scopes; no validated unlock implementation claimed. |
| Wireless Utilities And Global Coverage | Single-city electricity/water/sewage/heating design delivered | Facility behavior and pipe requirements remain design choices, separate from intercity exchange. |
| Realistic City Time | Foundation documents/branch and time-consistency design delivered | Confirmed direction: one real minute per game minute at normal speed, default 1×, maximum 10× and decimal input. Decimal precision and detailed behavior remain undecided; game acceptance is pending. |

### Vehicle Management artifact evidence

[Cloud run 41250632157](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41250632157) completed successfully for commit `3b14890f3ab37c4a66799230ba6ac496a1627871`. Artifact `vehicle-management-41250632157-1` (ID `13060977517`) was present and not expired when checked on 1 November 2026. The delivered DLL was reported and verified in the project handoff as **11,776 bytes**; the compressed artifact size is a different measurement.

Actions artifacts can expire and may require GitHub sign-in. This evidence does not establish a public Release, a Workshop ID, successful in-game observation, or implemented fleet control. Planned edition names do not establish public availability.

## Confirmed external replacement directions

These are unique external Workshop IDs. A target shared across internal modules is counted once. Confirmed responsibility areas do not settle every feature, migration or compatibility rule; this list does not advertise complete coverage or assign an owner to unresolved work.

Names and version labels are retained from the dated existing inventory; current versions and availability were not individually rechecked.

| External target | Workshop reference |
| --- | --- |
| TM:PE 11.9.4.1 STABLE (Traffic Manager: President Edition) | [1637663252](https://steamcommunity.com/sharedfiles/filedetails/?id=1637663252) |
| Remove Need For Power Lines | [572888650](https://steamcommunity.com/sharedfiles/filedetails/?id=572888650) |
| Remove Need For Pipes | [576997275](https://steamcommunity.com/sharedfiles/filedetails/?id=576997275) |
| All 25 Areas purchasable | [405810376](https://steamcommunity.com/sharedfiles/filedetails/?id=405810376) |
| Real Time 2.8 | [3059406297](https://steamcommunity.com/sharedfiles/filedetails/?id=3059406297) |
| More Simulation Speed Options 2 | [3343115359](https://steamcommunity.com/sharedfiles/filedetails/?id=3343115359) |
| Realistic Population 2 2.2.4 | [2025147082](https://steamcommunity.com/sharedfiles/filedetails/?id=2025147082) |
| Lifecycle Rebalance Revisited 1.6.9 | [2027161563](https://steamcommunity.com/sharedfiles/filedetails/?id=2027161563) |
| Outside Traffic Balancer | [2893645082](https://steamcommunity.com/sharedfiles/filedetails/?id=2893645082) |
| Improved Public Transport 3 | [3690061052](https://steamcommunity.com/sharedfiles/filedetails/?id=3690061052) |
| Transfer Manager CE v3.1.39 | [2804719780](https://steamcommunity.com/sharedfiles/filedetails/?id=2804719780) |
| Improved Lane Connections v2 | [2119310640](https://steamcommunity.com/sharedfiles/filedetails/?id=2119310640) |
| SharedStopEnabler | [2096382380](https://steamcommunity.com/sharedfiles/filedetails/?id=2096382380) |
| Rain Firefighting | [629370088](https://steamcommunity.com/sharedfiles/filedetails/?id=629370088) |
| Reversible Tram AI | [2740907672](https://steamcommunity.com/sharedfiles/filedetails/?id=2740907672) |
| Optimised Outside Connections | [1721492498](https://steamcommunity.com/sharedfiles/filedetails/?id=1721492498) |
| More Outside Interaction | [1706703944](https://steamcommunity.com/sharedfiles/filedetails/?id=1706703944) |
| Infinite Demand | [1522785644](https://steamcommunity.com/sharedfiles/filedetails/?id=1522785644) |
| More PathUnits 1.1.3 | [2710657019](https://steamcommunity.com/sharedfiles/filedetails/?id=2710657019) |
| More CitizenUnits | [2654364627](https://steamcommunity.com/sharedfiles/filedetails/?id=2654364627) |
| Hide TM:PE Crosswalks: Renewed | [3682758360](https://steamcommunity.com/sharedfiles/filedetails/?id=3682758360) |
| Right of Way - Priority for Emergency Vehicles | [3808510434](https://steamcommunity.com/sharedfiles/filedetails/?id=3808510434) |

The project has assigned the More CitizenUnits direction, including global CitizenUnits capacity and data integrity, to AI Improve. Capacity expansion and safe removal have not been accepted in game; saves using expanded capacity may depend on its provider.

The dated collection inventory has 210 unique entries: 21 confirmed targets, 2 identified dependencies and 187 unclassified entries. Harmony and Patch Loader are dependencies, not replacement targets. This is not a feature-coverage percentage or evidence that any item can be unsubscribed: the collection also contains assets and tools that this project does not recreate.

## Compatibility and optional cooperation

Independent modules and optional public-transport cooperation are design goals, not proof of safe combined operation. No mandatory shared DLL is introduced by this index. Confirmed responsibility areas do not establish completed cooperation protocols or feature migrations; detailed interfaces remain design work. No building or other 3D asset production is included.

Use the [Development installation guide](/Cities_Skylines_1_Modpack/wiki/development-downloads/) for the existing previews. Back up saves, use a test city and enable only one edition of each mod. Do not disable external mods merely because they appear in the target list. Keep cloud/file verification separate from Steam Workshop subscribed-game acceptance.

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->

<!-- Author: OpenAI-Codex_status_worker. -->

There are now 22 confirmed directions. Right of Way was added outside that historical 210-item collection snapshot; the 187 unclassified entries are not reduced to 186, and the current online collection size is not asserted.
<!-- Updated by OpenAI-Codex. -->
