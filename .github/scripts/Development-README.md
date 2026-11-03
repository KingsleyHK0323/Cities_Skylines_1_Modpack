# Automated Public Transport Planning — Development Build

## Module Overview

Automated Public Transport Planning provides city-data capture, demand analysis and ordinary in-city bus planning previews. Route creation is a Development prototype; connected or runnable routes and save/reload are not established by a successful build.

## Build Information and Changes

This build uses Configuration=Release with DevTools=true. The accompanying build-info.txt identifies the exact source SHA, fixed shared-reference SHA, workflow run and attempt. This development iteration adds move-only editing previews and confirmed application for existing ordinary city bus lines. The checks use a cloud CLR v4 host, not the game's Mono runtime. Both manual and automatic modes preserve the line ID and existing three-to-six-stop count. Fixed-platform stops and stop-count changes are not supported.

## Development Build Risk Warning

Back up saves and use a disposable copy. Development tools can damage saves. Never install alongside the stable Release/Workshop copy. Successful compilation and pure-data checks are not subscribed-game acceptance.

## Downloaded Files and Their Purpose

| File | Purpose |
| --- | --- |
| AutomatedPublicTransportPlanning.dll | Development mod |
| README.md | Installation, risks and usage |
| LICENSE | Original PolyForm Noncommercial License 1.0.0 |
| build-info.txt | Exact source and build provenance |
| Steam_Workshop_Link.txt | Stable item links recorded in source |
| manifest.json | Independent source and integrity metadata, stored at the Development build branch root |

## Installation Instructions

1. Close the game and disable/unsubscribe the stable Automated Public Transport Planning copy.
2. Create `%LOCALAPPDATA%/Colossal Order/Cities_Skylines/Addons/Mods/APTPDevelopment` on Windows.
3. Place only AutomatedPublicTransportPlanning.dll there. Do not add game assemblies, System.Core.dll or PDBs.
4. Start the game and enable the Development entry in Content Manager > Mods. Use a disposable save copy.

## Dev Tool Enablement and Usage

Open the module's Options page. Dev Tool controls are compiled in; no additional DLL is required. Under Diagnostics, select "Generate a read-only planning preview". Inspect and select a proposal, tick "Arm route creation; every click still asks for confirmation", then explicitly confirm each creation. Arming resets when the settings page opens. Do not assume proposals guarantee connected or runnable routes.

### Existing route: manual station placement and ordering

1. In "Existing bus route editing (Development)", enter the existing numeric line ID. Only complete Level1 city bus routes with three to six ordinary street stops are accepted.
2. Enter one comma-separated target per original stop. `1,3,2` reorders the original three station positions. A token such as `f123` or `b123` selects the captured forward/backward midpoint stop lane of road segment 123, respectively. These road tokens require a fresh read-only planning capture; they are not arbitrary coordinates. Road IDs can be taken from the planning proposal diagnostics.
3. Select "Prepare manual preview" and inspect every original node/position and target segment/lane/position. Unsupported lanes, repeated sites, stale city data, fixed-platform originals or changed stop counts are refused.

Manual input is limited to 128 characters with exactly one token per existing stop. Empty, signed, out-of-range and normalized duplicate tokens are refused before site resolution. Road IDs must be 1–65535 and the captured direction must have an anchorable lane.

### Existing route: automatic whole-route replanning

1. Generate a fresh planning preview, choose a one-based proposal number and enter it with the existing line ID in the editing controls.
2. Select "Prepare automatic preview". The selected proposal must have exactly the original stop count and the same bus prefab. A different-count proposal is refused rather than deleting/recreating the route.
3. Inspect the full before/after preview. Both modes then require arming editing and two separate confirmations for the exact preview. Input changes disarm the controls. The simulation thread checks the original route fingerprint and target roads again before any move.

Both modes use the game's MoveStop operation on the existing line; the mod does not release/recreate or rename it. Failure requests best-effort recovery of the original positions, which is not atomic and cannot restore all passenger, vehicle or path side effects. Recovery can fail: stop and restore your save backup when warned. Follow-up checks inspect stop lanes and adjacent path flags only. Verify the entire route, preserved line identity and save/reload yourself on a disposable save. Runtime UI and MoveStop behavior remain unverified in the subscribed game.

### Interpreting an edit failure

- `Refused`: the route did not satisfy the live preconditions; no edit was started by the executor.
- `PositionsRestored`: the edit failed, and original finite stop positions were observed within 0.5 metres after recovery. This is position-only verification, not an atomic rollback of passengers, vehicles or paths.
- `RecoveryIncomplete`: recovery could not verify positions, route completeness or identity. Stop editing, inspect the city and restore your save backup if necessary.
- `Invalidated`: the city or request changed. The old operation stops further apply and recovery writes; it does not report the new city as repaired.

Changing cities also invalidates old preview/status/completion callbacks. Fault-injection checks verify the executor's failure handling in a cloud test host; real in-game behavior still requires subscribed-build testing.

## Uninstallation and Return to Stable Release

Close the game and remove this Development copy from the Mods folder. Removing the DLL does not reverse saved city changes; restore your backup where necessary. Re-enable the stable Workshop copy only after this Development copy is removed.

## License, Dependencies, and Integrity Verification

Copyright Kingsley. LICENSE contains the unchanged PolyForm Noncommercial License 1.0.0. No game references, PDBs or public ZIP assets are distributed. The mod requires a compatible Cities: Skylines installation. Workshop links are source records, not a live Steam visibility check. The compile artifact's SHA256SUMS is verified before publication; the independent Development build branch records each distributed file's SHA-256 in its root manifest.json. For manual verification, compare `Get-FileHash AutomatedPublicTransportPlanning.dll -Algorithm SHA256` in PowerShell with that manifest.

Signature: OpenAI-Codex

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning

### Ordinary street stop eligibility correction

The placement API's fixedPlatform argument is not an asset classification: ordinary vehicle-stop resolution also returns true. Existing-route editing now classifies both originals and targets using the exact RoadAI type, existing segment eligibility and successful placement resolution, and preserves the original resolved argument for manual targets and recovery. Bridges, tunnels, station-owned roads and unknown/custom AI subclasses are conservatively refused. Earlier references to rejecting a true placement argument describe the previous bug, not the current policy. This does not enable elevated/shared stops or prove runtime/save compatibility.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
