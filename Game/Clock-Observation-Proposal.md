# Clock intervention evidence and bounded observation proposal

Status: design and static reference analysis only. No native clock writer or diagnostic probe is enabled by this document.

Signature: OpenAI-Codex

## Current, narrower diagnostic scope

**Source update, 2026-11-13:** The following description of a normal-symbol candidate without activation or visible output is a historical snapshot. The detached candidate now contains internal manual Start/Stop/Refresh and a fake-tested UI-helper view, with no automatic game registration. Its local revocation protocol and its limits are described in `Diagnostics/README.md`; no real game UI, unload, city identity or safe callback integration has been verified. Prior UI/host copies cannot be retracted. Signature: OpenAI-Codex.

### Path to actual clock control

A coordinated native calendar and day/night policy is the recommended direction for further investigation because it could meet the proposed 1x/10x behavior without replacing every consumer. It is not yet an approved write set. It requires a verified simulation-thread commit boundary, an exclusive or conflict-aware writer strategy, coherent frame/calendar/offset/metadata and display handling, phase and event behavior, and load/save recovery. Reference member accessibility alone proves none of these.

A separate shadow clock would avoid directly relying on a native date setter, but would require identifying and consistently replacing every relevant game and extension consumer, including daylight, events and save/load paths. A display-only clock has a smaller surface but cannot deliver actual calendar or day/night control. Neither alternative is established to meet the product goal.

Before connecting any writer, confirm the product relationship and interruption policy; establish a trustworthy source for pause/loading continuity, rate changes, native speed and session identity; and validate the chosen arithmetic and representation. In the inspected reference, `IThreading.simulationTime` is getter-only, the speed setter accepts integer levels 1–3, and a fixed whole-tick `m_timePerFrame` cannot exactly divide one day into 65,536 phase frames. A real implementation must resolve interpolation, wrap, competing writers and restoration rather than treating these observations as a ready-made transaction.

A separately authorized game experiment must identify the installed game and actual loaded modules, account for the existing date-reading observer, and specify the test city, game and Steam side effects, logging, autosave and recovery. Calendar observation beyond the current relative-only diagnostic also needs explicit authorization. Short-interval measurements can investigate rate but do not by themselves establish a full cycle, long-term continuity or save compatibility. The diagnostic UI is optional evidence tooling, not a prerequisite for designing the product control path. Signature: OpenAI-Codex.

The current instruction permits only a separately built, default-disabled candidate for relative simulation-frame and day/night display observations. It does not permit reading absolute calendar dates, metadata dates or save contents, even transiently. The candidate must not read player or account information, connect to a network, write game state, or initiate saving. It is excluded from the normal game assembly and remains disconnected until a separately reviewed integration exists. Relative timing and phase data are still game state and are not guaranteed to be anonymous.

The narrower source candidate is now present at `Diagnostics/ClockDiagnostics.cs`; its normal-symbol build has no automatic extension, activation, visible output or export. It retains at most eight records per stream and temporarily holds raw frame/phase baselines to calculate endpoint differences. Every record leaves lifecycle continuity, city identity and exclusion of overlapping callbacks unverified. `ElapsedCounterTicks` contains raw Stopwatch counter differences without a recorded frequency. The candidate cannot observe calendar date or save consistency. See `Diagnostics/README.md` for its exact scope and validation.

The broader observation sketch later in this document is retained as historical design context only. Its proposed reads of date, metadata and offsets are **not authorized for the current diagnostic**; statements there that a candidate is not implemented refer to that broader sketch. The existing `GameMod.cs` remains unchanged and already reads an absolute game date; this narrower diagnostic restriction must not be described as a property of the entire existing module.

## Reference scope

The inspected project copy of `Assembly-CSharp.dll` has SHA-256 `DF557994A03C93D8BB4A14F80B1FBCC0FB29D547658442445D078D069BCDD9FE`. It matches the project manifest, which does not verify the installed game version. The findings below concern that copy, not observed gameplay. The local inspection scripts and raw IL listings are retained under `Local_Validation_20261113_CityTime` and are not release artifacts.

The current `GameMod.cs` observes `m_currentGameTime`, pause state, frame index and a monotonic timestamp at `OnAfterSimulationTick`. Its loading generation and before/after readiness checks cannot authenticate a city or make the game's fields an atomic snapshot. No existing code establishes the additional relationships below.

## Writer and callback evidence

A scan of the reference assembly's readable method bodies looked for direct field stores and field-address loads for 14 selected clock fields. It found the following direct stores; it does not establish exclusive runtime ownership:

| Field or state | Reference methods and relevant IL | Consequence |
| --- | --- | --- |
| `m_timePerFrame` | `SimulationManager.Awake` IL_005C | One direct store in this reference scan does not exclude another assembly, reflection, native code or a write through a field address. |
| `m_timeOffsetTicks` | `SimulationManager.Data.Deserialize` IL_01F9 | The load path reconstructs the offset. An interception during play must account for how it will be reconstructed on load. |
| `m_currentGameTime` | `Awake` IL_0040; `Update` IL_01E4 | The displayed time is separately initialized and refreshed with interpolation. |
| Frame and metadata date | `SimulationStep` IL_0132–015C | The before-frame extensions run before the frame increment and metadata date addition. This is a candidate observation boundary, not an exclusive commit point. |
| Day/night offset | `SimulationStep` IL_0199; `ThreadingWrapper.set_simulationDayTimeHour` IL_0037; `InfoPanel.Update` IL_008E; `DeveloperUI` paths | Multiple game methods can write the offset. The wrapper setter also changes `m_isNightTime`, so an offset update is not a complete day/night transaction. |

The scan covered 23,065 method definitions, 22,758 readable method bodies and 28 byte-filtered candidate bodies, with no body-read errors. Candidate bodies were instruction-decoded; other bodies were not fully instruction-decoded. Eight project reference DLLs were also checked for selected same-name member references. Neither scan includes third-party modules or proves what an address-taking call, runtime patch, reflection or native code does.

`SimulationStep` calls all before-frame extensions, increments frame and metadata date, updates day/night state, calls simulation managers, then calls all after-frame extensions. Another extension or manager can intervene. At reference speed zero it can still call the tick callbacks and managers with zero, but it does not call the frame callbacks; an exception can interrupt callbacks. `Update` writes display time and display hour before dispatching `OnUpdate` at IL_0365. The reference does not establish an atomic snapshot between these two callback streams.

## Relationships that a future writer must account for

Let `F` be the UInt32 simulation frame, `P` the Int64 `m_timePerFrame.Ticks`, `O` the Int64 `m_timeOffsetTicks`, and `M` the metadata date in ticks. `FrameToTime` and the `IThreading.simulationTime` getter compute `F * P + O` before constructing a `DateTime`. The reference uses ordinary IL multiplication and addition, not checked arithmetic. A valid observation must separately handle arithmetic overflow, `DateTime` range, zero divisors, UInt32 frame wrap, concurrent changes and uncertain load state.

In the inspected deserialize branch, `O` is reconstructed from `M - F * P`. On a subsequent ordinary frame, `F` increments and `M` advances by `P`. These are conditional relationships, not proof that independently read values are simultaneous. `LateUpdateData` also temporarily increments frame 16,384 times in three map-derived update modes and subtracts that count on normal completion; the method itself has no exception handler guaranteeing that subtraction after a failure.

As a mathematical comparison only, keeping `F * P + O` fixed at one unchanged frame while changing `P` would require `O_new = O_old + F * (P_old - P_new)`. This does not establish a safe write procedure. Display time uses the separate reference frame and a floating-point reference timer: under valid conversions its calculation is `O + referenceFrame * P + conv.i8(referenceTimer * float(P))`. The same offset adjustment can therefore leave the display interpolation discontinuous. `m_currentGameTime` must not be required to equal the metadata date in every sample.

The simulation day/night frame is `(F + dayTimeOffsetFrames) & (DAYTIME_FRAMES - 1)`, and its hour uses `DAYTIME_FRAME_TO_HOUR`. The visual interpolation uses a separate reference frame and timer. In one mode with day/night disabled, the reference recomputes the offset on every frame to hold the phase at noon. Metadata day/night hour is updated during an actual simulation frame; the deserialize path does not itself establish that it already equals the reconstructed phase.

## Calendar and day/night are separate product decisions

In this reference copy, `Awake` initializes `P` to 1,476,562,500 ticks, or 147.65625 simulated seconds per frame. The static initializer sets `DAYTIME_FRAMES` to 65,536. If those values and the day/night offset remain unchanged, one full day/night phase cycle spans 96,768,000,000,000 calendar ticks: **112 simulated calendar days**. This calculation is not a measurement of real elapsed time or current gameplay, and it does not imply that the two clocks initially have the same phase.

| Direction | Observable effect under the stated reference assumptions | Principal risk |
| --- | --- | --- |
| Change calendar rate only | Changing `P` changes calendar ticks per simulation frame. At unchanged frame cadence and day/night settings, the day/night phase still advances according to frame count. | The calendar-to-phase ratio changes; frame-to-date, display interpolation, other date consumers and save reconstruction may diverge or jump. |
| Change day/night phase only | Altering the day/night offset changes the light phase and the reference's night-state calculation without directly advancing metadata date. A one-time phase change is not by itself an ongoing speed policy. | The setter and other game paths can compete; abrupt changes can skip or alter transitions and their side effects. It does not solve calendar speed. |
| Coordinate both | A future policy could define how calendar rate and phase rate relate and preserve chosen anchors across transitions and loads. | It needs a specified target relationship, safe multi-field ordering, other-writer coordination, event behavior and save recovery. None is established here. |

The user has not yet confirmed whether the final product should retain 112:1, adopt the 1:1 candidate below, or use another relationship. The behavior of other time-dependent systems also remains open.

### Adjustable working candidate

While the user's answer is pending, Main's adjustable candidate gives the calendar and day/night phase a shared baseline: at 1x, 24 eligible real hours correspond to one simulated calendar day and one complete day/night cycle; at 10x, each ideally takes 2.4 real hours. This is a 1:1 calendar-day-to-phase-cycle relationship, not the reference initialization's calculated 112:1 relationship. It is a working product assumption, not the user's final decision. The three alternatives above remain open, and the temporary multiplier field does not currently control either rate.

For an uninterrupted eligible interval of `E` real seconds and multiplier `r`, the ideal candidate advances the calendar by `E * r * 10,000,000` ticks and the day/night phase by `E * r / 86,400` cycles. These are target relationships, not a guarantee of simulation throughput or a decision to catch up time lost during pauses, loading or stalls.

For example, over 60 eligible real seconds, a future implementation would target the following. These are proposed acceptance values, not measurements from the current candidate.

| Multiplier | Calendar advancement | Day/night phase advancement |
| --- | --- | --- |
| 1x | 60 simulated seconds | 1/1,440 of a cycle, equivalent to one minute on a 24-hour phase |
| 2x | 120 simulated seconds | 1/720 of a cycle, equivalent to two minutes on a 24-hour phase |
| 10x | 600 simulated seconds | 1/144 of a cycle, equivalent to ten minutes on a 24-hour phase |

There is a concrete representation constraint if the reference's 65,536-frame phase cycle is retained unchanged and advances by exactly one phase frame per simulation frame. One calendar day divided by 65,536 is 13,183,593.75 ticks per frame, but `m_timePerFrame` is a `TimeSpan` with whole Int64 ticks. A single fixed whole-tick value cannot represent that ratio exactly: rounding down is short by 49,152 ticks (4.9152 ms) per phase cycle, while rounding up exceeds it by 16,384 ticks (1.6384 ms). This does not rule out another policy, variable phase advancement or a future method for retaining fractional remainders; each would need its own continuity, concurrency, event and save evidence. No such method is connected here.

Under the same one-phase-step-per-frame assumption, a complete cycle in 24 real hours or 2.4 real hours would require averages of about 0.7585 or 7.5852 simulation frames per real second respectively. Actual cadence is unknown, and those figures are not a recommendation to slow or accelerate the simulation frame loop. Changing `m_timeOffsetTicks` alone affects frame-to-time and display calculations; it does not itself correct the separately advanced metadata date. Varying per-frame duration would also require revalidating consumers such as `TimeToFrame`, which uses the current duration in its conversion.

Before an actual intervention, observe the unmodified calendar rate and phase rate separately, together with elapsed monotonic time, native speed and pause indications. An endpoint-only interval cannot exclude a hidden pause, offset adjustment, phase wrap or change-and-revert; report such cases as indeterminate rather than as a measured multiplier. After a separately approved, safe implementation exists, the intended comparison would repeat the same controlled observation at 1x, 2x and 10x and compare both rates with the shared 1x target. No such multiplier test is performed or enabled here.

The evidence needed before a coordinated intervention includes a trustworthy eligible-time source; an identified simulation-thread transition with a verified owner; a way to represent the chosen relationship without unaccounted drift; continuity of both the calendar and interpolated display; handling of phase offset changes, wrap, sunrise/sunset side effects and competing setters; and compatible save/load and failure recovery. Consistent short observations alone do not establish any of those guarantees.

## Load and save implications

For the inspected successful version-at-least-139 load branch, `LoadingManager` deserializes `SimulationMetaData`, calls `Merge`, then deserializes `SimulationManager.Data`. Each `DataSerializer.Deserialize<T>` call runs its own `Deserialize` and then `AfterDeserialize` pass. Merge can replace the incoming metadata date or day/night hour before the offset is reconstructed. Older-version, new-city, missing-data and error branches need separate analysis.

The loading coroutine waits for `completedOrFailed`, not a success-only result, before setting its simulation-data flag and scheduling the loading-complete callback. A tick can become eligible before `OnLevelLoaded`; the flag alone is not proof that all data restoration succeeded. No first-tick ordering across those operations is assumed by the proposal.

The reference save path opens a nonempty `filename` using `FileMode.Create`, then serializes metadata and simulation data separately. Its native exception handler can close and delete that same `filename`; the evidence does not establish whether the name is final or temporary. No save or deletion is proposed in the first observation below.

## Historical broader observation sketch — not authorized for this diagnostic

Recommend a separate, default-disabled diagnostic candidate, reviewed before any game execution. It would use existing extension callbacks only, without patching methods, changing a native field, writing a file or networking. The candidate is not implemented in this batch.

1. After an explicit local arm action, record at most eight complete before/after simulation-frame pairs and eight independent `OnUpdate` samples. Stop adding samples at each cap. An incomplete pair, re-entry, loading instability, overflow, read failure or exhausted buffer is reported as unknown or dropped, never as a successful sample. A paused game may produce no frame pair.
2. In the frame stream, capture the frame, `P`, `O` and metadata date at both ends; after the frame, also observe day/night offset, phase, conversion constants, metadata hour and night flag. Check the observed frame increment, calendar-tick delta and phase formula only when the sampled inputs permit checked calculations. Matching endpoints cannot rule out intervening changes or an ABA transition.
3. In the independent update stream, capture reference frame, reference timer, `P`, `O`, displayed date and displayed day/night inputs. Compare the sampled values with the reference calculation while preserving its float conversion; classify a mismatch as an observation to investigate, not an automatically proven defect. Do not require either display value to equal the simulation-frame value.
4. Attach the existing local generation, a per-stream sequence, managed-thread identity and monotonic start/end times. Associate the two streams only by generation and observation time; the generation is not proof of city identity and nearby timestamps do not make the samples atomic. Present counts, deltas and residuals in memory, without city names, save paths or an automatic export. Absolute dates may be held transiently while calculating a pair and are not part of the proposed display.

Use only bounded diagnostic state and a non-blocking acquisition strategy; mark contention as a dropped sample. Do not acquire game locks or perform I/O in a callback. The small sample can falsify a proposed invariant or identify missing data; even eight matching pairs cannot prove continuous ownership, all event ordering or save compatibility. The existing candidate's real UI and loading behavior remain unverified.

For a preliminary unmodified-rate observation, an operator could arm the bounded capture twice, about one minute apart, in the same locally observed generation while ordinary simulation is running. Record the actual monotonic elapsed time, calendar delta and unwrapped day/night phase delta, with the observed pause, native speed and phase-offset values. If phase wrapping cannot be uniquely resolved, report that delta as unknown. If interval continuity is not independently established, report available endpoint deltas as observations with continuity unverified, not as an established achieved rate or pass. This baseline does not test the working multiplier candidate: the current temporary field has no clock connection.

The next execution would require separate approval to implement and compile the diagnostic, enable the candidate, and run a specified game build in a user-approved controlled environment with a disposable city. The first observation should make no manual save. Starting the game can still change its own logs, settings, autosaves or achievements, and Steam or cloud synchronization may have effects outside the diagnostic; the environment and those effects need explicit authorization. If no suitably controlled environment is available, retain this as a design and continue static analysis only. Any later save/load experiment needs a separately reviewed disposable target and the required authorization for the game's possible overwrite or deletion behavior.
