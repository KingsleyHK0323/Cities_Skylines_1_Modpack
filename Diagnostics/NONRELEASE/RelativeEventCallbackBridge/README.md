# NONRELEASE relative callback bridge

Signature: OpenAI-Codex

This isolated source directly implements the public ICities.IThreadingExtension interface for offline fake-callback research. It is guarded by NONRELEASE_RELATIVE_CALLBACKS, starts disabled, and provides no loader, registration, user interface, automatic output, or game write. An internal caller must explicitly start a one-shot session. The inspected game assembly and controlled-host source lists do not include this directory; the directory name and compile symbol alone are not a universal build or loading guarantee.

OnCreated does not read or retain its IThreading argument, and OnUpdate does not read either time-delta argument. The implementation does not derive from ThreadingExtensionBase. Whether a real loader accepts the direct interface implementation, and how a real host expects IThreading to interact with extension creation and release, remain unverified.

The five accepted event kinds are before and after simulation tick, before and after simulation frame, and update. The bridge supplies only event kind and enter/exit markers to the existing bounded, in-memory recorder. Its records also contain the recorder's append order and session-local pseudonymous thread tags. No dates, time differences, frame or day/night values, city or save data, or raw thread identifiers are observed. The existing recorder defines a save-event kind, but this bridge rejects it and has no save callback.

While Recording or Closing, End first holds a local end pin and rejects a null or foreign ticket before reading its state, recording a marker, or changing a callback count. The receiving gate records Failure and closes admission; the ticket owner is unaffected. If the end-pin limit is already full, AdmissionLimit takes precedence and the result remains incomplete. End outside those states is refused without retroactively changing a snapshot. A ticket with no owner returns false from End. Signature: OpenAI-Codex.

Stopping closes new local admissions. A snapshot remains pending, contains no events, and is not complete while a locally admitted callback or start/end bookkeeping is still in flight. Completion requires an explicit retry after pending work ends; an abandoned ticket or stalled operation can leave the session pending indefinitely. Detected overlapping admitted intervals, reentrancy, marker loss, invalid use, and other recorded failures prevent a complete result. A complete empty snapshot can mean that no callback was admitted.

The boundary covers local admission through its release bookkeeping. It does not authenticate the origin of a queued callback or prove that every real callback returned, that the host queue drained, that frame or tick markers pair in the game, that execution was CPU-parallel, or that a save is consistent. Recorder order is only the order observed by this recorder.

Offline tests use a fake interface and synthetic callers. Their delayed-ticket checks exercise the gate directly; they do not reproduce a real game host. Compilation against project reference copies checks signatures only and does not validate Unity, Mono, loader discovery, lifecycle ordering, or gameplay.
