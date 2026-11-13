# Transactional calendar intervention: isolated fake host

This disabled-by-default source candidate advances an immutable fake clock snapshot. It is not included in the normal game or controlled-host builds and has no Harmony or game-assembly reference. No live-game writer, patch, activation path or safe rollback is provided. Signature: OpenAI-Codex.

## What the candidate does

For one eligible fake frame, a trusted fake caller supplies a continuous monotonic timestamp in units of 100-nanosecond ticks. The candidate computes the elapsed ticks d since its last accepted frame and changes a complete snapshot once:

- Frame F becomes F + 1, metadata date M becomes M + d, and offset O becomes O + d - P, retaining M = F * P + O when P is fixed.
- Display reference frame R becomes R + 1 while timer T is unchanged. Under that condition, O + R * P + (long)((float)P * T) advances by d; the candidate also checks the last published display value.
- A verified continuous zero-frame observation keeps its elapsed interval pending. This is a fake scheduling assumption, not a confirmed policy for real stalls. Exactly one later frame can consume it once. An interval larger than one native P is refused; this bound does not prove that real game events are safe.
- A pause, unverified continuity or multiple-frame interval requires explicit reanchoring. Those intervals are not reported as achieving 1x. The product policy for pause and catch-up remains undecided.

The 1x examples verify the use of supplied, already-normalized elapsed ticks; they do not verify an operating-system monotonic source, frequency conversion, fractional remainders, session identity or real frame throughput. A caller-supplied continuity flag is not authenticated evidence.

Owner reservation and snapshot reading precede local arming. A successful return is linearized only with respect to the final shared-fence observation after local arming: a fence completed before that observation is detected. A fence can occur after the observation, including before the caller receives the result, so success is not a continuing guarantee. The captured snapshot can also become stale; a later compare-and-swap must reject it. Reservation, snapshot reading and the public armed state are not one globally atomic operation. Signature: OpenAI-Codex.

## Required fake-host contract

The fake host must give this candidate exclusive ownership of one session and publish each new immutable snapshot only with an atomic compare-and-swap against the exact previously read version. Versions must never be reused for an ABA transition. A false return must mean that this call did not publish; an exception can occur before or after publication and is therefore treated as uncertain. A fenced candidate never retries.

The fake store grants one owner for its lifetime; its time watermarks remain in the candidate instance and are not a persisted or authenticated game epoch. The commit interface does not carry the owner identity, so direct store calls can bypass the lease; the fake contract requires all commits to be routed through the trusted owner. Owner transfer, process restart and reusing an untrusted initial timestamp are not supported. If a host fence operation throws or does not enforce its contract, only the local candidate is known to be fenced; another instance or a native writer is not proven stopped. The fake test store retains prior snapshot identities to reject ABA and is not a bounded long-running production host.

Every fake reader, save and display publication must participate in the same serialization contract. An old reader must not publish a display after a newer version: version validation and the actual display update must share the same critical section. Fake saves can copy a complete snapshot; this does not establish that native game metadata and simulation data are saved together. No fake transaction can undo already executed external callbacks or other side effects.

A callback executed while the fake publication gate is held must not directly or indirectly wait for another thread that may enter this candidate or acquire the same gate, including Stop, Fence, reads, saves or display publication. ~~A bounded fake test demonstrates the waiting cycle and releases it after a timeout; without that bound such a contract violation can deadlock.~~ A bounded fake test observes a thread at the pre-lock hook and not completing for 300 ms while the gate is held; scheduler delay can also contribute. Waiting indefinitely under the gate for work that requires the same gate can deadlock. Signature: OpenAI-Codex. Stop synchronously requests the host fence, and an in-flight commit may already have happened. No general deadlock-freedom or rollback guarantee is claimed. Signature: OpenAI-Codex.

The input snapshot is the complete state before a fake frame. The existing reference method increments its frame before the intercepted date addition; using this candidate directly at that helper would risk incrementing the frame twice. The reference Update also changes R and T, which affect other timers and day/night rendering. This candidate has no verified strategy for ownership or serialization of those operations.

A reference load path resets the display reference to the current frame and its timer to zero. If the previously visible display was ahead, a backward display may already occur before this candidate can arm. Rejecting a snapshot with a lower raw display does not undo that earlier publication, and preserving the last published value across a real load has not been established.

Changing O changes the interpreted dates of other frame indices by d - P per committed fake frame. A synthetic event-frame mapping is checked. Event ordering, event range overflow, missing or duplicate events, day/night behavior, native save/load, other mods, Unity and Mono remain unverified.

## Focused in-memory checks

From the module root, the following PowerShell 7 commands compile only these two source files in memory and run the fake-host checks. They do not load Harmony or game assemblies.

    $base = Join-Path (Get-Location) 'Diagnostics/CalendarInterception/TransactionalCandidate'
    Add-Type -Path @((Join-Path $base 'TransactionalCalendarCandidate.cs'), (Join-Path $base 'TransactionalCandidateChecks.cs'))
    [TransactionalCandidateChecks]::Run()

The remaining integration prerequisite is a verified shared commit and publication boundary covering the actual simulation update, display writer and save readers, including the native frame increment and callback side effects. The existing reference observations do not establish such a boundary. Until it is established, this is a fake-host candidate rather than a game-ready 1x implementation.