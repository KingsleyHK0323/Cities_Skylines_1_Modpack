# Realistic City Time

Planning-stage, source-available module for Cities: Skylines 1. No game functionality, build project, or binaries are implemented yet.

Repository branch: [Realistic_City_Time](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_City_Time).

## Confirmed direction

- At 1x, one minute of real elapsed time corresponds to one minute of game time.
- The default speed is 1x and the maximum is 10x. Decimal multiplier entry is approved; its input precision remains undecided. Speeds below 1x are not included.
- Intended replacement targets: [Real Time 2.8 (3059406297)](https://steamcommunity.com/sharedfiles/filedetails/?id=3059406297) and [More Simulation Speed Options 2 (3343115359)](https://steamcommunity.com/sharedfiles/filedetails/?id=3343115359). These are external references, not this module's Workshop IDs or a claim of feature parity.

The scope of citizen schedules, aging, education, population, economy, and building operations remains undecided. Replacement intent does not approve every behavior of either external mod. Clock integration, pause behavior, loading, day transitions, and save compatibility require further design. The 1:1 baseline does not imply synchronization with the computer clock, date, or time zone, or offline catch-up.

## Validation and distribution

The foundation workflow checks documentation and licensing. A separate branch-scoped hosted workflow runs dependency-free pure-model checks. These do not establish game integration, performance, or gameplay acceptance. No local runner or workflow is required.

The initial model tests are committed before implementation to observe a failing cloud run. The model project uses the hosted .NET 8 SDK framework without external packages; it is not a game mod project. Decimal precision will be an explicit caller-supplied policy, not a product default. No rounding to integer speeds is permitted.

This foundation has no DLL release, Workshop upload, Workshop ID, icon, or game-reference dependency. Future implementation and distribution require separate approval. Gameplay acceptance requires a Steam Workshop subscribed build.

## License and attribution

Code and workflow: [PolyForm Noncommercial License 1.0.0](LICENSE). Original public documentation/content: [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/). Copyright Kingsley. This project is source-available.

The LICENSE file preserves the official text byte for byte. Official source: [PolyForm website source at caa273a0f67aba05e8cc06f7e869469918f53617](https://github.com/polyformproject/polyformproject.org/blob/caa273a0f67aba05e8cc06f7e869469918f53617/licenses/noncommercial/1.0.0.txt), also published on the [official license page](https://polyformproject.org/licenses/noncommercial/1.0.0). Attribution and this signature are kept outside the license terms.

Signature: OpenAI-Codex_Responsible: Realistic_City_Time
