#if NONRELEASE_RELATIVE_CALLBACKS
// Signature: OpenAI-Codex_boundary_host
using System;
using System.Threading;
using RealisticCityTime.Diagnostics.RelativeEventTrace;

namespace RealisticCityTime.Diagnostics.NonReleaseCallbackBridge
{
    internal enum CallbackState
    {
        Disabled,
        Starting,
        Recording,
        Closing,
        Finalizing,
        Stopped
    }

    [Flags]
    internal enum CallbackIssue
    {
        None = 0,
        NotStarted = 1,
        StartCancelled = 2,
        ConcurrentCallbacks = 4,
        Reentrancy = 8,
        DroppedMarker = 16,
        WrongThread = 32,
        AdmissionLimit = 64,
        Failure = 128,
        Pending = 256,
        InvalidKind = 512
    }

    internal sealed class CallbackSnapshot
    {
        private readonly CallbackState state;
        private readonly CallbackIssue issues;
        private readonly bool isComplete;
        private readonly EventRecord[] events;

        internal CallbackSnapshot(CallbackState state, CallbackIssue issues,
            bool isComplete, EventRecord[] events)
        {
            this.state = state;
            this.issues = issues;
            this.isComplete = isComplete;
            this.events = events;
        }

        internal CallbackState State { get { return state; } }
        internal CallbackIssue Issues { get { return issues; } }
        internal bool IsComplete { get { return isComplete; } }
        internal EventRecord[] Events { get { return (EventRecord[])events.Clone(); } }
    }

    internal sealed class CallbackTicket
    {
        private readonly CallbackSessionGate owner;
        private readonly EventKind kind;
        private int endState;

        internal CallbackTicket(CallbackSessionGate owner, EventKind kind)
        {
            this.owner = owner;
            this.kind = kind;
        }

        internal EventKind Kind { get { return kind; } }
        internal bool IsActive { get { return Interlocked.CompareExchange(ref endState, 0, 0) != 2; } }
        internal bool IsOpen { get { return Interlocked.CompareExchange(ref endState, 0, 0) == 0; } }

        internal bool IsOwnedBy(CallbackSessionGate candidate)
        {
            return Object.ReferenceEquals(owner, candidate);
        }

        internal bool TryEnd()
        {
            return Interlocked.CompareExchange(ref endState, 1, 0) == 0;
        }

        internal void MarkEnded()
        {
            Interlocked.Exchange(ref endState, 2);
        }

        internal bool End()
        {
            return owner != null && owner.End(this);
        }
    }

    internal sealed class CallbackSessionGate
    {
        private const int MaxCallbacks = 1024;
        private const int MaxEndPins = 1048575;
        private const int IssueShift = 3;
        private const int CallbackShift = 17;
        private const int EndPinShift = 28;
        private const long StateMask = 7L;
        private const long IssueMask = 1023L;
        private const long CallbackMask = 2047L;
        private const long EndPinMask = 1048575L;
        private const long StartInFlight = 1L << 16;
        private const long CallbackUnit = 1L << CallbackShift;
        private const long EndPinUnit = 1L << EndPinShift;

        [ThreadStatic] private static WeakReference currentTicket;

        private readonly RelativeEventRecorder recorder;
        private long control;

        internal CallbackSessionGate(int eventCapacity, int threadCapacity)
        {
            recorder = new RelativeEventRecorder(eventCapacity, threadCapacity);
        }

        private long ReadControl()
        {
            return Interlocked.CompareExchange(ref control, 0L, 0L);
        }

        private static CallbackState StateOf(long value)
        {
            return (CallbackState)(value & StateMask);
        }

        private static CallbackIssue IssuesOf(long value)
        {
            return (CallbackIssue)((value >> IssueShift) & IssueMask);
        }

        private static long CallbackCountOf(long value)
        {
            return (value >> CallbackShift) & CallbackMask;
        }

        private static long EndPinCountOf(long value)
        {
            return (value >> EndPinShift) & EndPinMask;
        }

        private static long WithState(long value, CallbackState state)
        {
            return (value & ~StateMask) | (long)state;
        }

        private static long WithIssue(long value, CallbackIssue issue)
        {
            return value | ((long)issue << IssueShift);
        }

        private void MarkIssue(CallbackIssue issue, bool closeAdmission)
        {
            while (true)
            {
                long before = ReadControl();
                CallbackState state = StateOf(before);
                if (state == CallbackState.Stopped) return;
                long after = WithIssue(before, issue);
                if (closeAdmission && (state == CallbackState.Starting || state == CallbackState.Recording))
                    after = WithState(after, CallbackState.Closing);
                if (after == before || Interlocked.CompareExchange(ref control, after, before) == before)
                    return;
            }
        }

        private void ClearStartInFlight()
        {
            while (true)
            {
                long before = ReadControl();
                long after = before & ~StartInFlight;
                if (after == before || Interlocked.CompareExchange(ref control, after, before) == before)
                    return;
            }
        }

        internal bool TryStart()
        {
            long starting = WithState(StartInFlight, CallbackState.Starting);
            if (Interlocked.CompareExchange(ref control, starting, 0L) != 0L)
                return false;

            try
            {
                if (!recorder.TryStart())
                {
                    MarkIssue(CallbackIssue.Failure, true);
                    return false;
                }

                while (true)
                {
                    long before = ReadControl();
                    if (StateOf(before) != CallbackState.Starting) return false;
                    long after = WithState(before, CallbackState.Recording);
                    if (Interlocked.CompareExchange(ref control, after, before) == before)
                        return true;
                }
            }
            catch (Exception)
            {
                MarkIssue(CallbackIssue.Failure, true);
                return false;
            }
            finally
            {
                ClearStartInFlight();
            }
        }

        private static bool IsAllowed(EventKind kind)
        {
            return kind == EventKind.BeforeSimulationFrame || kind == EventKind.AfterSimulationFrame ||
                kind == EventKind.BeforeSimulationTick || kind == EventKind.AfterSimulationTick ||
                kind == EventKind.Update;
        }

        private bool TryAdmit(EventKind kind)
        {
            while (true)
            {
                long before = ReadControl();
                if (StateOf(before) != CallbackState.Recording) return false;

                if (!IsAllowed(kind))
                {
                    long invalid = WithState(WithIssue(before, CallbackIssue.InvalidKind), CallbackState.Closing);
                    if (Interlocked.CompareExchange(ref control, invalid, before) == before) return false;
                    continue;
                }

                long count = CallbackCountOf(before);
                if (count >= MaxCallbacks)
                {
                    long full = WithState(WithIssue(before, CallbackIssue.AdmissionLimit), CallbackState.Closing);
                    if (Interlocked.CompareExchange(ref control, full, before) == before) return false;
                    continue;
                }

                long after = before + CallbackUnit;
                if (count != 0 || EndPinCountOf(before) != 0)
                    after = WithIssue(after, CallbackIssue.ConcurrentCallbacks);
                if (count == MaxCallbacks - 1)
                    after = WithState(WithIssue(after, CallbackIssue.AdmissionLimit), CallbackState.Closing);
                if (Interlocked.CompareExchange(ref control, after, before) == before) return true;
            }
        }

        private void ReleaseCallback()
        {
            while (true)
            {
                long before = ReadControl();
                if (CallbackCountOf(before) == 0)
                    throw new InvalidOperationException("The callback admission count is invalid.");
                if (Interlocked.CompareExchange(ref control, before - CallbackUnit, before) == before)
                    return;
            }
        }

        private bool TryPinEnd()
        {
            while (true)
            {
                long before = ReadControl();
                CallbackState state = StateOf(before);
                if (state != CallbackState.Recording && state != CallbackState.Closing) return false;

                if (EndPinCountOf(before) >= MaxEndPins)
                {
                    long full = WithIssue(before, CallbackIssue.AdmissionLimit);
                    if (state == CallbackState.Recording) full = WithState(full, CallbackState.Closing);
                    if (full == before || Interlocked.CompareExchange(ref control, full, before) == before)
                        return false;
                    continue;
                }

                long after = before + EndPinUnit;
                if (EndPinCountOf(before) == MaxEndPins - 1)
                {
                    after = WithIssue(after, CallbackIssue.AdmissionLimit);
                    if (state == CallbackState.Recording) after = WithState(after, CallbackState.Closing);
                }
                if (Interlocked.CompareExchange(ref control, after, before) == before)
                    return true;
            }
        }

        private void ReleaseEndPin()
        {
            while (true)
            {
                long before = ReadControl();
                if (EndPinCountOf(before) == 0)
                    throw new InvalidOperationException("The end-attempt count is invalid.");
                if (Interlocked.CompareExchange(ref control, before - EndPinUnit, before) == before)
                    return;
            }
        }

        private static CallbackTicket CurrentTicket()
        {
            WeakReference slot = currentTicket;
            return slot == null ? null : slot.Target as CallbackTicket;
        }

        private void RecordMarker(EventKind kind, EventPoint point)
        {
            try
            {
                if (recorder.Observe(kind, point) != RecordResult.Recorded)
                {
                    MarkIssue(CallbackIssue.DroppedMarker, true);
                    return;
                }

                if (recorder.State != RecorderState.Recording)
                    MarkIssue(CallbackIssue.Failure, true);
            }
            catch (Exception)
            {
                MarkIssue(CallbackIssue.DroppedMarker | CallbackIssue.Failure, true);
            }
        }

        internal bool TryBegin(EventKind kind, out CallbackTicket ticket)
        {
            ticket = null;
            if (!TryAdmit(kind)) return false;

            CallbackTicket candidate = null;
            bool handedOut = false;
            try
            {
                CallbackTicket existing = CurrentTicket();
                if (existing != null && existing.IsActive)
                {
                    MarkIssue(CallbackIssue.Reentrancy, true);
                    return false;
                }

                candidate = new CallbackTicket(this, kind);
                currentTicket = new WeakReference(candidate);
                RecordMarker(kind, EventPoint.Enter);
                ticket = candidate;
                handedOut = true;
                return true;
            }
            catch (Exception)
            {
                MarkIssue(CallbackIssue.DroppedMarker | CallbackIssue.Failure, true);
                return false;
            }
            finally
            {
                if (!handedOut)
                {
                    try
                    {
                        if (candidate != null)
                        {
                            candidate.MarkEnded();
                            if (Object.ReferenceEquals(CurrentTicket(), candidate)) currentTicket = null;
                        }
                    }
                    finally
                    {
                        ReleaseCallback();
                    }
                }
            }
        }

        internal bool End(CallbackTicket ticket)
        {
            if (!TryPinEnd()) return false;
            try
            {
                if (ticket == null || !ticket.IsOwnedBy(this))
                {
                    MarkIssue(CallbackIssue.Failure, true);
                    return false;
                }
                if (!ticket.IsOpen) return false;
                if (!Object.ReferenceEquals(CurrentTicket(), ticket))
                {
                    MarkIssue(CallbackIssue.WrongThread, true);
                    return false;
                }
                if (!ticket.TryEnd()) return false;

                try
                {
                    RecordMarker(ticket.Kind, EventPoint.Exit);
                }
                finally
                {
                    try
                    {
                        ReleaseCallback();
                    }
                    finally
                    {
                        ticket.MarkEnded();
                        if (Object.ReferenceEquals(CurrentTicket(), ticket)) currentTicket = null;
                    }
                }
                return true;
            }
            finally
            {
                ReleaseEndPin();
            }
        }

        internal CallbackState RequestStop()
        {
            while (true)
            {
                long before = ReadControl();
                CallbackState state = StateOf(before);
                if (state == CallbackState.Stopped || state == CallbackState.Finalizing) return state;
                if (state == CallbackState.Closing) return TryCompleteStop();

                long after = before;
                if (state == CallbackState.Disabled) after = WithIssue(after, CallbackIssue.NotStarted);
                if (state == CallbackState.Starting) after = WithIssue(after, CallbackIssue.StartCancelled);
                after = WithState(after, CallbackState.Closing);
                if (Interlocked.CompareExchange(ref control, after, before) == before)
                    return TryCompleteStop();
            }
        }

        private CallbackState LeaveFinalizing(CallbackState next)
        {
            while (true)
            {
                long before = ReadControl();
                if (StateOf(before) != CallbackState.Finalizing) return StateOf(before);
                long after = WithState(before, next);
                if (Interlocked.CompareExchange(ref control, after, before) == before) return next;
            }
        }

        internal CallbackState TryCompleteStop()
        {
            while (true)
            {
                long before = ReadControl();
                CallbackState state = StateOf(before);
                if (state != CallbackState.Closing) return state;
                if ((before & StartInFlight) != 0 || CallbackCountOf(before) != 0 || EndPinCountOf(before) != 0)
                    return state;
                long after = WithState(before, CallbackState.Finalizing);
                if (Interlocked.CompareExchange(ref control, after, before) != before) continue;
                break;
            }

            try
            {
                RecorderState state = recorder.RequestStop();
                if (state != RecorderState.Stopped) state = recorder.TryCompleteStop();
                if (state == RecorderState.Stopped) return LeaveFinalizing(CallbackState.Stopped);
            }
            catch (Exception)
            {
                MarkIssue(CallbackIssue.Failure, false);
            }
            return LeaveFinalizing(CallbackState.Closing);
        }

        internal CallbackSnapshot ReadSnapshot()
        {
            long value = ReadControl();
            CallbackState state = StateOf(value);
            CallbackIssue issues = IssuesOf(value);
            if (state != CallbackState.Stopped)
                return new CallbackSnapshot(state, issues | CallbackIssue.Pending, false, new EventRecord[0]);

            RecorderSnapshot snapshot = recorder.ReadSnapshot();
            if (!snapshot.IsComplete && issues == CallbackIssue.None) issues |= CallbackIssue.Failure;
            return new CallbackSnapshot(state, issues,
                issues == CallbackIssue.None && snapshot.IsComplete, snapshot.Events);
        }
    }
}
#endif
