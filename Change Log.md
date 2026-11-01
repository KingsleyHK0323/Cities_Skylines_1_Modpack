# Change Log

## Unreleased — 2026-11-01

- Wire separate hosted SDK validation, framework-only restore, explicit compilation of both models and both check files, and execution without implicit build/restore.
- Select the preinstalled stable .NET 8 SDK through global.json without installation or preview/major fallback; SDK failures remain infrastructure failures.
- Keep all accumulated changes local and unpublished; no current test result or game DLL is claimed.

- Add immutable, explicitly unknown observation data and a generation/sequence-guarded model input session without live game reads or clock writes.
- Prepare contract cases for stale callbacks, pause/resume, unknown samples, time reversal, explicit discontinuity, overflow recovery, and fractional-state isolation.
- Keep the observation batch local and uncommitted; no compilation or tests have been executed.

- Implement the local pure decimal multiplier parser and exact tick conversion model with explicit precision policy and fractional remainder preservation.
- Add precision-limit and large-input regression cases; correct decimal constant reflection in the test harness.
- Keep this batch uncommitted and unpublished. No compilation or test execution has been performed; cloud red/green evidence remains pending publication approval.

- Approve decimal multiplier entry within 1x to 10x; precision remains a separate decision.
- Add dependency-free cloud model checks before implementing the pure conversion model.
- Keep all game-clock adapters and save-date writes out of this change.

- Establish the independent Realistic_City_Time branch foundation.
- Record the approved 1:1 baseline, default 1x, maximum 10x, and unresolved behavior scope.
- Add official PolyForm Noncommercial License 1.0.0 and document content licensing separately.
- Add branch-scoped GitHub-hosted documentation checks without game builds or publication.
- No game functionality, binaries, Workshop identity, or gameplay acceptance is provided.

Signature: OpenAI-Codex_Responsible: Realistic_City_Time

Local pure-model implementation signature: OpenAI-Codex_Realistic_City_Time

Observation contract batch signature: OpenAI-Codex_Realistic_City_Time
