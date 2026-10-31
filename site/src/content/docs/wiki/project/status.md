---
title: Module status and replacement targets
description: "A dated overview of 15 modules and 20 confirmed replacement directions, with implementation and acceptance kept separate."
---

Status snapshot: **1 November 2026**. The project contains **15 modules at different stages**, not 15 finished downloads. The 20 external targets below are confirmed replacement directions. They are not completed replacements, permission to unsubscribe, or proof of compatibility.

Design delivery, source implementation, cloud compilation, downloadable artifacts, public releases and subscribed-game acceptance are separate milestones. Detailed responsibility assignments and interfaces remain **proposals** unless explicitly confirmed. No complete replacement has been established by the evidence used for this snapshot.

## Module status

| Module | Evidence stage | Availability and acceptance boundary |
| --- | --- | --- |
| AI Gaming | Development paused by the project owner; an earlier preview exists | Existing documentation is historical context, not resumed work. Game runtime remains unverified. |
| AI Improve | Existing implementation; responsibility and migration inventory delivered | Existing project/download pages remain separate from proposed new responsibilities. Combined-mod acceptance is pending. |
| Automated Public Transport Planning | Existing Development prototype; shared-stop gap analysis delivered | Can attempt confirmed route creation and change saves. Shared-stop replacement and subscribed-game acceptance are not established. |
| Realistic Urban Population | Adjustable residential/commercial/industrial demand design delivered | Modes, numeric ranges and defaults remain proposals; no validated game implementation is claimed. |
| Realistic City First Response | First-version design and migration inventory delivered | Fire-response behavior and implementation choices require decisions; no replacement acceptance claimed. |
| Realistic Vehicle Traffic Control | Road-feature inventory and single-writer design delivered | Lane preview/application and signal ownership details remain proposals. |
| Realistic Pedestrian Traffic Control | Pedestrian/crosswalk design and interface plan delivered | Restrictions, visual changes and integration remain design proposals. |
| Realistic Railway Traffic Control | Railway safety model and migration plan delivered | Observation versus reversible-tram priorities remain undecided; no operational control claimed. |
| Realistic Border Control | Passenger-data and quota-preview design delivered | Preview versus active arrival limiting remains undecided. |
| Realistic Import Export Control | Freight-matching and trade design delivered | Recommendation versus active matching remains undecided. |
| Intercity Resource Sharing | Water/electricity/heat exchange design delivered | Data sources, conservation and settlement are design work, not control of another save. |
| Automated Public Transport Vehicle Management | Read-only bus observation implemented; DLL compiled and delivered as an Actions artifact | No public Release or Workshop item. Fleet-control proposals and subscribed-game acceptance remain pending. |
| Full Map Unlock And Expansion | 25-area milestone/save design delivered | 25-area purchasing and 81-area expansion are separate scopes; no validated unlock implementation claimed. |
| Wireless Utilities And Global Coverage | Single-city electricity/water/sewage/heating design delivered | Facility behavior and pipe requirements remain design choices, separate from intercity exchange. |
| Realistic City Time | Foundation documents/branch and time-consistency design delivered | Confirmed direction: 1:1 at normal speed, default 1×, maximum 10×. Intermediate controls and detailed behavior remain proposals. |

### Vehicle Management artifact evidence

[Cloud run 41250632157](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/actions/runs/41250632157) completed successfully for commit `3b14890f3ab37c4a66799230ba6ac496a1627871`. Artifact `vehicle-management-41250632157-1` (ID `13060977517`) was present and not expired when checked on 1 November 2026. The delivered DLL was reported and verified in the project handoff as **11,776 bytes**; the compressed artifact size is a different measurement.

Actions artifacts can expire and may require GitHub sign-in. This evidence does not establish a public Release, a Workshop ID, successful in-game observation, or implemented fleet control. The Release/Development icon labels identify intended editions, not a publication status.

## Confirmed external replacement directions

These are unique external Workshop IDs. A target shared across internal modules is counted once. The detailed owner, feature coverage, migration and compatibility rules remain proposals; this list deliberately does not advertise complete coverage or assign a final owner to unresolved work.

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
| Hide TM:PE Crosswalks: Renewed | [3682758360](https://steamcommunity.com/sharedfiles/filedetails/?id=3682758360) |

The dated collection inventory has 210 unique entries: 20 confirmed targets, 2 identified dependencies and 188 unclassified entries. Harmony and Patch Loader are dependencies, not replacement targets. This is not a coverage percentage: the collection also contains assets and tools that this project does not recreate.

## Compatibility and optional cooperation

Independent modules and optional public-transport cooperation are design goals, not proof of safe combined operation. No mandatory shared DLL is introduced by this index. Detailed cooperation protocols, single-writer assignments and feature migrations remain proposals. No building or other 3D asset production is included.

Use the [Development installation guide](/Cities_Skylines_1_Modpack/wiki/development-downloads/) for the existing previews. Back up saves, use a test city and enable only one edition of each mod. Do not disable external mods merely because they appear in the target list. Keep cloud/file verification separate from Steam Workshop subscribed-game acceptance.

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->
