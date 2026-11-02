# Cargo observation source boundary

Signature: OpenAI-Codex_Realistic_Import_Export_Control

## Current evidence

The authorized project contains `Resource/Game_Data/Assembly-CSharp.dll`. Its
existence alone proves neither quantity units nor the meaning of transfer offers.
No game DLL was loaded, executed or decompiled in this batch. No native reason
mapping, quantity conversion, in-transit subtraction or capture API is established.

Read-only inspection of authorized project source copies found these limited clues
(paths relative to the workspace root; no file below was modified):

- `AI_Improve/Source_Code/ShipDockAssignmentPatch.cs:56` checks `OutsideConnectionAI`.
  Lines 63–64 read source/target building fields, but lines 86–89 describe a target
  that may instead identify a node on a transport line. These are not universal
  cargo endpoint or import/export mappings.
- `AI_Improve/Source_Code/EmptyVehicleAuditor.cs:73–74,89–90` uses transfer size in
  passenger-specific conditions, not evidence of a universal freight quantity unit.
- `AI_Improve/Source_Code/SanitationIdleSeekTracker.cs:190–198,254` distinguishes
  material semantics for service facilities. Lines 53–57 warn about dispatched
  vehicles and repeat dispatch. This supports preserving unknown commitments but
  supplies no valid freight subtraction formula.

The limited source scan found no freight `TransferOffer.Amount` unit mapping in
the inspected existing modules. It is not an exhaustive audit of game behavior.

`CargoObservationAdapter` reads immutable caller-provided record values. It is not
a game adapter and does not discover buildings, vehicles, offers or shipments.
`Unavailable()` is the state for a missing native capture provider; it returns no
example rows. Fixtures live in the test project only.

## Counts and duplication

- A raw counter remains opaque, nullable and unconverted. Negative counters are
  preserved as raw source evidence, not interpreted as negative cargo quantities.
- No counter total is available. Cargo quantity units and in-transit semantics are
  unknown, even when a caller supplies local/external endpoint classifications.
- Record IDs identify a single source record within a capture. Repeated identical
  IDs collapse; conflicting records with the same ID invalidate the observation.
- Different IDs are never assumed to represent different physical cargo. An offer,
  inventory row and in-transit vehicle can describe the same goods; the reader does
  not add them together. Identity reconciliation requires native evidence.
- Local-to-local, external-to-local and local-to-external labels describe endpoints
  only. Unknown endpoints remain unknown; external transit is separate. An outside
  endpoint is not inferred from vehicle type, building index or a guessed ID range.
- Raw reasons are retained without asserting they belong to a cargo whitelist.
  No rescue, garbage or deathcare task is submitted, and no planner snapshot can
  be created from these records.
- Capture completeness and consistency are caller claims checked as prerequisites,
  not independently verified by this adapter. Cross-batch version proof still
  belongs to a future native capture provider.

## Required evidence before native integration

1. A permitted source/version/hash for each capture field and its lifecycle.
2. Exact reason values, cargo whitelist and unique task owner mapping.
3. Meaning and units of counters, stock and commitments; whether offers already
   exclude in-transit amounts, including cross-mode transfers and outside transit.
4. Record identity, version reuse, capture consistency, thread affinity, bounded
   batching and cancellation. A building ID alone is not an offer identity.
5. A read-only way to establish endpoint locality without editing shared routing,
   gate/dock allocation, transfer orders, RCI or economic state.

Until these are evidenced, native capture stays unavailable. This library is .NET 8
and is not game-loadable. It does not replace TMCE or Optimised Outside Connections.

## Proposed cloud verification batch (not executed or authorized by this file)

- Review only the model, adapter, tests and necessary README/Change Log changes on
  `Realistic_Import_Export_Control`; preserve unrelated changes.
- With fresh commit/push authorization, publish those changes to that branch only.
- Let the existing GitHub-hosted workflow compile and run
  `dotnet run --project Tests/CargoModelChecks.csproj --configuration Release`.
- Expect the existing 28 checks and 14 observation checks, 42 total; this count is
  a planned expectation, not an execution result for the current working tree.
- Require the documentation workflow and pure-model workflow to pass on the same
  full commit SHA; inspect actual logs before claiming compilation or test success.
- Do not create a Release, upload a game DLL or run a self-hosted job. No game
  integration or gameplay acceptance follows from these synthetic checks.
