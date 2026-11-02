# Read-only observation source audit

## Local evidence boundary

This batch inspected this module's authorized project copy. It contains the pure model, console checks, and no game reference DLLs or native reader implementation. No original game installation was read. The older external TM:PE research is design context, not evidence for the current game's ABI or the actual state of a loaded city.

| Field | Verified local source | Adapter behavior until verified |
| --- | --- | --- |
| Effective crossing restriction | None | Unknown; never infer Allow from absent settings or visible paint |
| Pedestrian traffic signal | None | Unknown; no assumed native method signature or conversion |
| Crossing geometry | None | Unknown; segment/node IDs alone do not establish a crossing |
| Rendered crosswalk marking | None | Unknown; neither restriction nor prefab naming proves rendered visibility |
| Waiting pedestrian count | None | Null; never substitute zero |
| City/session identity and topology revision | Synthetic fixture only | Integration must establish authoritative lifetime and revision tracking |

## Implemented local adapter seam

`CrossingReadAdapter` is an internal adapter around a supplied read callback, not an implemented game reader or cross-module API. A missing callback returns `Unavailable` with `Unknown` provenance. It does not add a `Live` source value; supplied observations still pass the model's source and state validation. Synthetic input remains synthetic.

Each `BeginSession` creates a new lifetime identity, even for the same city string. `Invalidate` closes the session. A result returned after a concurrent or reentrant session change is discarded. The adapter does not retain prior successful observations. Exceptions and null results remain unavailable and do not expose exception contents. Returned previews are point-in-time data, not enduring authorization for any action.

The adapter calls the supplied reader synchronously. A future native integration must establish the correct game-thread scheduling, atomicity/revision mechanism, lifetime notifications and API evidence before supplying a reader. The internal lock only protects this adapter's session identity; it does not make native game reads thread-safe. No automatic polling, background threads, game hooks, signal/marking/geometry writes, settings migration, or shared DLL is introduced.

## Pending cloud validation and publication checklist

1. Review the local diff and the new `Tests/AdapterChecks.cs` scenarios; preserve unrelated work.
2. Obtain explicit authorization for commit/push and cloud execution for this batch. No such operation is performed by this local-only delivery.
3. On the dedicated `Realistic_Pedestrian_Traffic_Control` branch, run the existing `model-checks.yml` workflow after approval. Its .NET 8 project includes `Source/Pedestrian/*.cs` and test files; compile and execute the existing model checks plus the adapter checks.
4. Record run ID, attempt, checked-out SHA, actual job steps and assertion log. The previous run's 31 passing assertions do not validate this batch.
5. Do not create a public release, Workshop item, or installable DLL from this host-only check project.
6. Before native integration, provide lawful project-copy reference assemblies/API evidence for each source above, actual simulation/render thread constraints, and invalidation events for unload, road replacement and ID reuse. Keep unresolved fields unknown.

This batch has not been compiled or executed locally or in the cloud. Written checks are pending; static whitespace/XML checks and code review do not establish behavior or game acceptance.

Signature: OpenAI-Codex_Realistic_Pedestrian_Traffic_Control
