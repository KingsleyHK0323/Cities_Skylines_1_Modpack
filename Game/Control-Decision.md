# Dry-run control decisions and native integration gaps

This local batch contains no native time writer. `ClockControlPreview.CanWrite` always returns false, including for `Candidate` decisions. Neither the game entry classes nor the observation callback instantiate or execute the control boundary. No configuration switch enables native writes.

## Decision contract

`ClockControlBoundary` accepts an immutable current observation, a decimal multiplier, an explicit nullable precision policy, and explicit knowledge about execution-boundary verification and other clock writers. It reports inactive/unknown/stale/paused, missing precision, invalid multiplier, unverified boundary, conflicting-or-unknown writer, or a dry-run candidate. Missing knowledge never defaults to approval. Fractional input is not rounded to integer speed, and missing precision does not select a product default.

The boundary, writer-ownership, and observation-freshness flags are caller assertions for testing a policy, not evidence that the current implementation can verify a simulation thread, hook phase, current pause state, or installed conflicting mod. Freshness must be asserted separately at both evaluation and dry-run consumption; the old overloads supply unknown and cannot produce/consume a candidate. They are not connected to live controls. A timestamp inside the latest cached sample also does not by itself prove freshness in wall-clock time; no arbitrary age threshold is introduced here. Signature: OpenAI-Codex_Realistic_City_Time.

The independent `ClockInputSession` additionally rejects an interval unless its source explicitly asserts uninterrupted execution and a constant multiplier throughout the interval. A sequence gap also prevents later-interval evidence from covering missed samples. Contiguous sequence numbers and endpoint states alone cannot prove that no pause or temporary multiplier change occurred without a callback. The game callback does not currently furnish this evidence, so this is a fail-closed synthetic contract rather than a live source or native writer. Signature: OpenAI-Codex_Realistic_City_Time.

Dry-run consumption is bound to the boundary instance and exact cached snapshot. Consumption is one-shot per observation generation/sequence and rechecks currentness under the cache lock used for publication/unload, avoiding a stale validation-to-bookkeeping gap. It only updates local dry-run counters. A future native writer would still require its own approved simulation-thread serialization and all-field consistency strategy; these counters are not permission to add one.

The first successful consumer also owns dry-run consumption for the current cache generation. Another boundary sharing that cache is denied, even for a later snapshot; a new generation or unload clears the owner. Acquisition and one-shot consumption occur under the cache lock, so concurrent contenders cannot both win. Rejected and stale candidates do not acquire ownership. This local rule does not discover other cache instances, duplicated assemblies, or external mods, and must not be treated as native clock ownership. Signature: OpenAI-Codex_city_time_development.

The .NET 8 harness links the actual game-compatible cache and decision sources, with cases for missing precision, decimal bounds, pause/unknown, unverified boundary, unknown writer ownership, old samples/cities, foreign boundary instances, repeated consumption, competing owners, and concurrent acquisition. On 2026-11-04 the local harness passed 40 arithmetic checks and its observation and ownership contract checks. A separate local compilation of just the cache and decision sources against the approved `mscorlib` reference succeeded; the output references `mscorlib` version 2.0.0.0. It excludes the game entry class and other game dependencies, so neither check establishes full game-assembly compilation or gameplay. The cloud compiler entry script includes the decision source but is not executed locally; Main retains workflow ownership.

## Reference interface evidence

### Synthetic integration candidate — 2026-11-13

`SyntheticClockCoordinator` is linked only to the test project. It takes immutable cached snapshots, explicitly asserted continuity, boundary, freshness and ownership evidence, and combines the existing portable scaler with the dry-run ownership boundary. Its provisional input policy accepts 1–10 with at most two literal fractional digits and rejects extra digits, including extra trailing zeros; it is an adjustable implementation default selected by Main, not a finalized localized UI. No persistence format is selected.

An accepted virtual date accumulates verified intervals. Pauses, missing samples, rate changes and unknown intervals discard the ambiguous elapsed period without catch-up; known fractions and the virtual date remain within the same generation. A new generation or confirmed backward date/frame/monotonic timestamp clears the synthetic timeline. The candidate tracks rate changes even when the value changes back between observations. Its test-only lock ordering is coordinator, then preview boundary, then cache, so acceptance can recheck the current snapshot and local ownership under the cache lock. These locks and asserted evidence are local simulation mechanisms, not a game-wide writer lock or native atomic commit.

The associated tests cover one elapsed minute at 1x and 10x, provisional input, pause, unverified intervals, rate transitions, sequence gaps, midnight, in-memory save exchange, stale plans, publication races and competing preview owners. Source copies passed an isolated .NET Framework synthetic harness, and the four-source subset compiled against the project CLR 2.0 reference with test-only symbols; the concurrency cases do not guarantee every interleaving. The game entry classes do not construct this coordinator, the cloud game-source script does not include it, and `CanWrite` remains false.

### Reference timing and save dependencies

Static IL in the single approved reference copy shows that `SimulationManager.SimulationStep` obtains final speed before its tick callback, and, when frame processing runs, updates frame, metadata date, and day/night state around frame callbacks. `SimulationManager.Update` separately computes displayed time from reference frame/timer, per-frame duration and offset. The existing tick observer's cached game date and frame have not been proven to be a mutually current snapshot.

`SimulationManager.Data` and `SimulationMetaData` serialize different related state: the latter also stores date and daytime hour. Deserialization contains version-specific branches, while cross-container ordering, external writers, failure recovery and atomicity remain unverified. A future coherent commit would have to preserve the relationship among frame, per-frame ticks, offset and date, as well as display interpolation, day/night, events and save restoration; this is a design requirement inferred from one reference copy, not an approved write set. No system-clock synchronization is selected.

Signature: OpenAI-Codex.

PE metadata inspection of the approved reference copies shows that `ICities.IThreading.simulationTime` is getter-only. The wrapper derives its returned date from the simulation frame index, per-frame time, and time offset. It is not a supported transactional date setter.

`IThreading.simulationSpeed` and `SimulationManager.SelectedSimulationSpeed` use integer values. In this reference wrapper, the speed setter clamps to 1–3 before forwarding to SelectedSimulationSpeed. It cannot represent the approved 1–10 decimal policy directly; adding a second multiplier without defining the native speed relationship would risk double acceleration.

The reference exposes mutable `m_timePerFrame` (TimeSpan), `m_timeOffsetTicks` (Int64), `m_currentGameTime` (DateTime), and static/daytime frame-to-hour conversion fields. Their accessibility is not proof that changing one field or independently updating all of them is safe. Native event delivery, frame wrap, date continuity, save/load restoration, and rollback remain unverified. No setter or patch for these fields is implemented.

An additional static inspection of the approved reference copy found custom `SimulationManager.Data` serialization methods. Its `Serialize` references the current tick/frame and day-time offset; `Deserialize` restores or calculates frame/day-night fields and derives the time offset using metadata date and per-frame time. `AfterDeserialize` handles later frame/timer initialization. These are observations about one reference copy, not proof of how every data container or installed game version saves time. In particular, they do not establish a safe single-field edit, a transactional multi-field update, or a reversible save migration. Signature: OpenAI-Codex_Realistic_City_Time.

## Smallest path toward actual clock control

1. Keep the current observation DLL read-only. Validate game loading and callback observations through an approved subscribed build, independently of model tests.
2. Confirm the final game interruption/catch-up policy and decimal input precision. The general dry-run boundary still takes precision from its caller; the later synthetic coordinator uses Main's adjustable two-decimal and no-catch-up defaults. An integer-only slider is not the selected product design.
3. Verify the native per-frame time update and daytime/event consumers using approved reference evidence. Choose one supported simulation-thread commit point and a coherent date/daylight/save strategy. The alternatives are a unified native time policy (preferred for further study), or a separate shadow clock that requires replacing all consumers (substantially larger risk); display-only time does not meet the requested behavior.
4. Validate and integrate the local exact-arithmetic bridge. `PortableClockScaler` needs no BigInteger and takes an explicit precision policy; it has passed a .NET 8 differential harness and a partial compile against the CLR 2.0 reference copy. It is not connected to observation or native writes, and actual Mono runtime compatibility and the complete game build remain unverified. Do not silently reduce precision. Signature: OpenAI-Codex_Realistic_City_Time.
5. Request approval for the specific native write set and failure recovery before implementing/enabling a writer. No phase, global patch, pause override, or load-time date reset is authorized by this dry-run candidate.

The key remaining game behavior choice is interruption handling: favor safety and omit unavailable elapsed intervals, displaying achieved speed, or retain a bounded debt for later simulated catch-up. The latter changes subsequent speed and needs explicit limits. Neither option can guarantee 1:1 under insufficient simulation throughput without additional game behavior decisions. The synthetic fixture uses no catch-up as an adjustable working default; the final native policy remains unselected.

Signature: OpenAI-Codex_Realistic_City_Time

2026-11-13 update signature: OpenAI-Codex
