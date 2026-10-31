# Realistic Railway Traffic Control

This source-available Cities: Skylines 1 module is in observation/preview development. No game implementation or playable mod DLL exists; the pure-data library is not a Cities: Skylines mod.

## Pure-data model development

The first approved stage is observation and preview, before any separately approved manual control. The branch implements immutable snapshot and movement data, a fail-closed validator, and a shadow conflict analyzer. Live observation is unavailable: no game data-source adapter is implemented. Behavior checks use explicitly synthetic model inputs, not live city observations.

- Invalid or incomplete evidence returns `Unknown` with a reason. Unsupported networks return `Unsupported` when the other evidence is complete.
- Conflicting neighboring resources are checked as well as directly requested resources. A known train may retain its own starting occupancy; a following train still sees that occupancy.
- Shadow reservations are all-or-nothing per request and exist only within one `Evaluate` call. Input ordering is deterministic; this is not an optimal or fair train scheduler.
- `WouldReserve` describes the model only. It is not a signal, permission to move, or proof of physical safety.
- Snapshot validation enforces city/capture identity, topology tokens, complete conflict relationships, and engineering caps: 128 segments, 64 consists, 256 resources, and 64 requests; at most 256 conflict keys and 64 occupant keys per resource, and 128 resource keys per request. Truncated or unknown evidence cannot become a clear track.
- A token is supplied model evidence, not a claimed native game generation/version field. Inputs must already identify all relevant trains, including tails and unsupported types occupying the area. The model cannot independently establish that a real-world capture is complete.

The separate `railway-model-checks.yml` workflow compiles a .NET Framework 4.8 pure-data library and console checks on GitHub-hosted Windows. Its source is restricted to C# 3 syntax. This does not establish .NET 3.5 binary compatibility or compile a Cities: Skylines mod. No game references, dependency installation, release publishing, or Workshop upload are involved.

## Project links

- [Canonical repository](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack)
- [Module branch](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_Railway_Traffic_Control)
- [Modpack website](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/)

## Intended scope

Railway signals, block occupancy, junction control, reversible trams, and station-platform behavior remain staged areas of work. This batch implements only the pure-data validator and shadow analyzer. It does not submit paths, change signals, reverse vehicles, allocate platforms, alter dwell time, or change vehicle counts. Existing AI Improve behavior and external mods are left untouched.

## Automation and release boundary

The `module-baseline` workflow runs documentation and repository baseline checks on GitHub-hosted Ubuntu for pushes and pull requests targeting `Realistic_Railway_Traffic_Control`, or by manual dispatch. It does not compile or test game code.

Future compilation needs an implemented build project and a legally permitted source for the required Cities: Skylines 1, ICities, Unity, and other declared reference assemblies. Their versions and acquisition method remain unresolved. Do not commit proprietary game assemblies.

Once those prerequisites are resolved, the intended distribution is an independent GitHub cloud build, Development files directly from a module-specific GitHub Release without ZIP packaging, and the stable version through Steam Workshop. No automatic release or Workshop upload is configured. Local execution of runners or workflows requires separate explicit authorization. Gameplay acceptance requires the Steam Workshop subscribed version.

## License

Copyright Kingsley. Code and original documentation in this branch use the PolyForm Noncommercial License 1.0.0; see [LICENSE](LICENSE). This is source-available software with noncommercial restrictions. The separate website's original content uses CC BY-NC-SA 4.0; website content is not included here. Third-party material retains its own terms.

Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
