# Realistic City Time

Source-available module in early development for Cities: Skylines 1. No game functionality or game mod binaries are implemented yet. A standalone pure clock conversion model exists locally; execution has not been verified.

Repository branch: [Realistic_City_Time](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_City_Time).

## Confirmed direction

- At 1x, one minute of real elapsed time corresponds to one minute of game time.
- The default speed is 1x and the maximum is 10x. Decimal multiplier entry is approved; its input precision remains undecided. Speeds below 1x are not included.
- Intended replacement targets: [Real Time 2.8 (3059406297)](https://steamcommunity.com/sharedfiles/filedetails/?id=3059406297) and [More Simulation Speed Options 2 (3343115359)](https://steamcommunity.com/sharedfiles/filedetails/?id=3343115359). These are external references, not this module's Workshop IDs or a claim of feature parity.

The scope of citizen schedules, aging, education, population, economy, and building operations remains undecided. Replacement intent does not approve every behavior of either external mod. Clock integration, pause behavior, loading, day transitions, and save compatibility require further design. The 1:1 baseline does not imply synchronization with the computer clock, date, or time zone, or offline catch-up.

## Validation and distribution

The foundation workflow checks documentation and licensing. A separate branch-scoped hosted workflow runs dependency-free pure-model checks. These do not establish game integration, performance, or gameplay acceptance. No local runner or workflow is required.

### Pending hosted validation wiring

`clock-model.yml` uses `ubuntu-latest` and the preinstalled stable .NET 8 SDK, selected by `global.json` (8.0.100 minimum feature baseline; latest installed 8.0 feature band, no major-version or preview fallback). It does not install an SDK or external packages. If that SDK or its framework packs are missing, the environment must be repaired under separate authorization; such a failure is not evidence of failing model behavior. The JSON file also carries attribution metadata for OpenAI-Codex_Realistic_City_Time.

The pending workflow runs these commands separately on GitHub-hosted infrastructure, never on the local PC:

```sh
dotnet --version
dotnet restore tests/ClockModel.Tests/ClockModel.Tests.csproj --configfile tests/ClockModel.Tests/NuGet.Config
dotnet build tests/ClockModel.Tests/ClockModel.Tests.csproj --configuration Release --no-restore
dotnet run --project tests/ClockModel.Tests/ClockModel.Tests.csproj --configuration Release --no-build --no-restore
```

The project explicitly compiles `Program.cs`, `ClockInputTests.cs`, `ClockModel.cs`, and `ClockInputSession.cs`. `Program.Main` executes arithmetic checks and then `ClockInputTests.Run`; failures exit nonzero. No game references, packages, game mod DLL, artifact upload, or release step is configured. The foundation documentation job is separate and cannot stand in for these checks. No commands above have been executed for the current local batch.

Triggers are push to `Realistic_City_Time`, pull requests targeting that branch, and manual dispatch constrained by the job condition to that branch. Permissions are only `contents: read`; checkout uses the previously verified fixed v4.2.2 commit with credential persistence disabled. Both known branch workflows are check-only: their definitions do not create a public prerelease on push. Repository-level external automation was not queried in this local-only review.

The initial tests were committed before implementation, but their public push was blocked pending publication authorization. No cloud red/green result exists. The subsequent implementation is a local, uncommitted batch; no build or tests have been run locally. The model checks target .NET 8 without external packages; this does not select the game's target framework.

## Pure model contract

`ClockModel` accepts an explicit decimal precision policy (0–28), with no default product precision. Decimal entry is approved; the final UI precision, step, localization, interruption behavior, and per-city activation remain undecided. Precision zero exists for policy validation, not as the selected product setting.

`ParseMultiplier` accepts unsigned invariant decimal text within 1–10 inclusive. It rejects signs, whitespace, comma separators, exponents, NaN, infinity, excess effective fractional digits, and values that cannot be represented exactly as a decimal rather than rounding them. The decimal representation ceiling can be stricter than the supplied precision policy. Trailing fractional zeros do not consume precision. This is an internal parser contract, not a finalized localized UI.

`Advance` consumes eligible elapsed `TimeSpan` ticks (10,000,000 per second), validates the multiplier, and returns whole desired game ticks. Integer numerator/remainder arithmetic via the framework's `BigInteger` preserves fractional ticks across calls and multiplier changes. Negative input, invalid multipliers, and output overflow leave the remainder unchanged. `Reset` clears only fractional ticks. Callers must serialize access; the arithmetic model has no clock, session, pause, save, UI, or game adapter and cannot establish real-world simulation speed.

## Read-only observation contract

`ClockObservation` is an immutable data contract for a future verified source. Nullable game time, pause state, and monotonic timestamp explicitly represent unknown data. The timestamp must already use TimeSpan tick units; it is not a raw Stopwatch counter. No implementation reads game fields yet: project reference copies exist, but current method signatures and lifecycle ordering have not been established.

`ClockInputSession` bridges supplied observations to the pure model on one serialized caller thread. `BeginSession` creates a new generation and resets fractional state; `EndSession` invalidates callbacks. Old-generation, duplicate, negative-sequence, and out-of-order samples cannot modify the active anchor. The first complete sample only establishes a baseline. Only a `Ready` result has `DesiredGameTicks`; all other results retain an unknown/null output, not a fabricated zero or measured game rate.

Unknown values and pause invalidate the elapsed-time anchor while retaining previously accepted fractional ticks; the next complete unpaused sample establishes a new baseline, so the missing interval is not counted. Explicit discontinuity, decreasing timestamps/game dates, invalid conversion, and overflow clear both anchor and fractional state. Invalid intervals are never retried as accumulated time. A generation change must be issued by the future lifecycle source before load/unload or city replacement; there is no actual game hook in this batch.

This is a conservative input eligibility contract, not a decision about game catch-up, automatic activation, UI defaults, or a maximum interruption threshold. A large forward gap is not detected automatically; a future verified source must flag discontinuities, and product interruption policy remains undecided. Forward day transitions are only supplied dates, never date writes or new-day event dispatches.

This foundation has no DLL release, Workshop upload, Workshop ID, icon, or game-reference dependency. Future implementation and distribution require separate approval. Gameplay acceptance requires a Steam Workshop subscribed build.

## License and attribution

Code and workflow: [PolyForm Noncommercial License 1.0.0](LICENSE). Original public documentation/content: [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/). Copyright Kingsley. This project is source-available.

The LICENSE file preserves the official text byte for byte. Official source: [PolyForm website source at caa273a0f67aba05e8cc06f7e869469918f53617](https://github.com/polyformproject/polyformproject.org/blob/caa273a0f67aba05e8cc06f7e869469918f53617/licenses/noncommercial/1.0.0.txt), also published on the [official license page](https://polyformproject.org/licenses/noncommercial/1.0.0). Attribution and this signature are kept outside the license terms.

Signature: OpenAI-Codex_Responsible: Realistic_City_Time

Local pure-model implementation signature: OpenAI-Codex_Realistic_City_Time

Observation contract batch signature: OpenAI-Codex_Realistic_City_Time
