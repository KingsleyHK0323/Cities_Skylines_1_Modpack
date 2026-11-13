# Relative event trace: isolated recorder candidate

**Validation update, 2026-11-14:** The isolated .NET 8 fake-callback executable passed 179 synthetic checks. Admission overlap, pending and the admission limit include white-box state simulations; reentrancy is also simulated and does not establish real nested callbacks or game scheduling. The recorder source separately compiled against the authorized project mscorlib reference; that output was not executed. This update supersedes the earlier preparation-stage statement that no test result is claimed here. When multiple failure conditions coincide, issue flags need not classify every cause. Signature: OpenAI-Codex.

This source-only candidate is independent of the normal game assembly and the existing controlled diagnostic host. Neither build entry lists `RelativeEventRecorder.cs`. The recorder does not register game callbacks, discover managers, or start automatically. A separately reviewed host would have to call it; this source alone does not authorize or establish a game integration.

## Recorded information

Each accepted record contains only an `EventKind`, an `Enter` or `Exit` point, a one-based recorder-local order, and a session-local thread tag. The supported kinds are `BeforeSimulationFrame`, `AfterSimulationFrame`, `BeforeSimulationTick`, `AfterSimulationTick`, `Update`, and `SaveData`. These are caller-supplied labels: `SaveData` does not itself open or write a save, and no label proves that a native callback or save occurred.

The recorder source does not read a raw managed or operating-system thread ID, a `Thread` object, a clock or duration, an absolute or metadata date, a frame value, a city identifier, or a game field. It does not perform file or network I/O. State and issue flags accompany snapshots; they are not additional game observations. A caller can retain or disclose copies, and event patterns may still be identifying in context. The local tags are pseudonyms, not a guarantee of anonymity or unlinkability.

The order is the serialization order of successful appends inside this recorder. It is not necessarily the order in which game callbacks actually began. The recorder does not pair or validate enter/exit records. The source does not establish native event occurrence, matching of real frame callbacks, CPU parallelism, a cross-thread happens-before relationship for game work, or save consistency. Calls rejected before admission are outside the recorder's observation window.

## Session and thread contract

Construct `RelativeEventRecorder(eventCapacity, threadCapacity)` with an event capacity from 1 through 4,096 and a thread capacity from 1 through 128. The recorder starts `Disabled`; `TryStart()` explicitly starts a single session. It cannot be restarted after stopping. `RequestStop()` before a start ends the one-shot session as `Stopped` with the `NotStarted` issue, so that an empty session is not reported complete.

An explicit `Observe(kind, point)` attempts one event. The private `ThreadStatic` binding contains a weak reference to the recorder and an integer tag; it does not contain a `Thread` object or raw ID. A live binding owned by another recorder that has not reached `Stopped` is refused and makes the new recorder incomplete. A stopped or collected owner may be replaced when the other admission and capacity checks succeed. The caller must keep an active recorder strongly reachable until stop is confirmed. Thread tags describe only this session's managed thread-local observations; they do not authenticate a Unity UI thread or establish an operating-system thread identity.

The event buffer, assigned thread tags, and simultaneous admissions each have a limit. Reaching the event or thread capacity, or the fixed 1,024-admission limit, closes admission and marks the session incomplete. Capacity reached by a successfully appended record does not mean the session is complete. Invalid input, a thread conflict, reentrancy, a failed operation, or other reported issues also prevent a complete result.

## Concurrency and stop

Admission uses compare-and-exchange. A retry caused by admission competition, or an admission while another observation is already admitted, can mark `Contention` while an append still succeeds; the flag alone does not prove an event was lost. Separately, failure to enter the private recording gate drops that observation, marks `Contention`, and closes admission. The recorder cannot prove that callers or native game code did not overlap outside its admitted observation window.

`RequestStop()` closes admission and attempts completion. Already admitted calls can still finish or report a stop race. If they have not drained, the state remains `Stopping`; call `TryCompleteStop()` again later instead of treating the request as confirmation. Before `Stopped`, `ReadSnapshot()` reports `Pending` and returns no events. After confirmed `Stopped`, it returns a snapshot copy; each `Events` access also returns a copy. `IsComplete` is true only for a stopped snapshot without issue flags, and applies only to this recorder's accepted observation scope.

Only the recorder's fixed event buffer and simultaneous admissions are bounded by this design. Thread-local bindings can outlive a session on participating threads, and repeated snapshots can be retained by callers. No process-wide `ThreadStatic` memory bound, worst-case execution-time bound, Unity thread affinity, or eventual stop completion is claimed.

## Evidence status

`RelativeEventRecorderChecks.cs` and `RelativeEventRecorderBehaviorChecks.cs` provide fake-check sources. No test result is claimed here. Compilation against the game references, Unity/Mono execution, actual callback order, and game or save behavior require separate evidence and authorization.

Signature: OpenAI-Codex_clock_surface
