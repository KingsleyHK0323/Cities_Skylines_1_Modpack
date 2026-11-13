// Signature: OpenAI-Codex
// An isolated, one-shot event recorder. It does not register game callbacks.
using System;
using System.Threading;

namespace RealisticCityTime.Diagnostics.RelativeEventTrace
{
    public enum EventKind
    {
        BeforeSimulationFrame = 1,
        AfterSimulationFrame,
        BeforeSimulationTick,
        AfterSimulationTick,
        Update,
        SaveData
    }

    public enum EventPoint { Enter = 1, Exit }
    public enum RecorderState { Disabled, Recording, Stopping, Stopped }
    public enum RecordResult
    {
        Recorded,
        NotRecording,
        DroppedContention,
        DroppedInvalid,
        DroppedStopping,
        DroppedThreadConflict,
        DroppedFailure,
        DroppedReentrancy,
        DroppedAdmissionLimit
    }

    [Flags]
    public enum RecorderIssue
    {
        None = 0,
        CapacityReached = 1,
        ThreadLimitReached = 2,
        Contention = 4,
        InvalidEvent = 8,
        StopRace = 16,
        ThreadConflict = 32,
        Failure = 64,
        Pending = 128,
        Reentrancy = 256,
        NotStarted = 1024,
        AdmissionLimit = 512
    }

    public struct EventRecord
    {
        private readonly EventKind kind;
        private readonly EventPoint point;
        private readonly int order;
        private readonly int threadTag;

        internal EventRecord(EventKind kind, EventPoint point, int order, int threadTag)
        {
            this.kind = kind;
            this.point = point;
            this.order = order;
            this.threadTag = threadTag;
        }

        public EventKind Kind { get { return kind; } }
        public EventPoint Point { get { return point; } }
        public int Order { get { return order; } }
        public int ThreadTag { get { return threadTag; } }
    }

    public sealed class RecorderSnapshot
    {
        private readonly RecorderState state;
        private readonly RecorderIssue issues;
        private readonly EventRecord[] events;

        internal RecorderSnapshot(RecorderState state, RecorderIssue issues, EventRecord[] events)
        {
            this.state = state;
            this.issues = issues;
            this.events = events;
        }

        public RecorderState State { get { return state; } }
        public RecorderIssue Issues { get { return issues; } }
        public bool IsComplete { get { return state == RecorderState.Stopped && issues == RecorderIssue.None; } }
        public EventRecord[] Events { get { return (EventRecord[])events.Clone(); } }
    }

    public sealed class RelativeEventRecorder
    {
        private const int MaxEvents = 4096;
        private const int MaxThreads = 128;
        private const int MaxAdmissions = 1024;
        private const int IssueShift = 2;
        private const int AdmissionShift = 16;
        private const long StateMask = 3L;
        private const long AdmissionUnit = 1L << AdmissionShift;
        private const long IssueMask = (1L << (AdmissionShift - IssueShift)) - 1L;

        private sealed class ThreadBinding
        {
            internal readonly WeakReference Owner;
            internal readonly int Tag;

            internal ThreadBinding(RelativeEventRecorder owner, int tag)
            {
                Owner = new WeakReference(owner);
                Tag = tag;
            }
        }

        [ThreadStatic] private static ThreadBinding threadBinding;
        [ThreadStatic] private static bool _insideObserver;

        private readonly object _gate = new object();
        private readonly EventRecord[] events;
        private readonly int threadCapacity;
        private long control;
        private int eventCount;
        private int threadCount;

        public RelativeEventRecorder(int eventCapacity, int threadCapacity)
        {
            if (eventCapacity < 1 || eventCapacity > MaxEvents)
                throw new ArgumentOutOfRangeException("eventCapacity");
            if (threadCapacity < 1 || threadCapacity > MaxThreads)
                throw new ArgumentOutOfRangeException("threadCapacity");
            events = new EventRecord[eventCapacity];
            this.threadCapacity = threadCapacity;
        }

        private long ReadControl()
        {
            return Interlocked.CompareExchange(ref control, 0L, 0L);
        }

        private static RecorderState StateOf(long value)
        {
            return (RecorderState)(value & StateMask);
        }

        private static RecorderIssue IssuesOf(long value)
        {
            return (RecorderIssue)((value >> IssueShift) & IssueMask);
        }

        public RecorderState State { get { return StateOf(ReadControl()); } }

        public bool TryStart()
        {
            return Interlocked.CompareExchange(ref control, (long)RecorderState.Recording, 0L) == 0L;
        }

        private static long WithIssue(long value, RecorderIssue issue)
        {
            return value | ((long)issue << IssueShift);
        }

        private static long WithState(long value, RecorderState state)
        {
            return (value & ~StateMask) | (long)state;
        }

        private void MarkIssue(RecorderIssue issue, bool closeAdmission)
        {
            while (true)
            {
                long before = ReadControl();
                if (StateOf(before) == RecorderState.Stopped) return;
                long after = WithIssue(before, issue);
                if (closeAdmission && StateOf(before) == RecorderState.Recording)
                    after = WithState(after, RecorderState.Stopping);
                if (after == before || Interlocked.CompareExchange(ref control, after, before) == before)
                    return;
            }
        }

        private bool TryAdmit(out RecordResult rejected)
        {
            bool sawCompetition = false;
            while (true)
            {
                long before = ReadControl();
                if (StateOf(before) != RecorderState.Recording)
                {
                    rejected = RecordResult.NotRecording;
                    return false;
                }

                long after;
                bool atLimit = (before >> AdmissionShift) >= MaxAdmissions;
                if (atLimit)
                {
                    after = WithState(WithIssue(before, RecorderIssue.AdmissionLimit), RecorderState.Stopping);
                    if (sawCompetition) after = WithIssue(after, RecorderIssue.Contention);
                }
                else
                {
                    after = before + AdmissionUnit;
                    if (sawCompetition) after = WithIssue(after, RecorderIssue.Contention);
                }

                if ((before >> AdmissionShift) != 0)
                    after = WithIssue(after, RecorderIssue.Contention);
                if (!atLimit && (before >> AdmissionShift) == MaxAdmissions - 1)
                    after = WithState(WithIssue(after, RecorderIssue.AdmissionLimit), RecorderState.Stopping);

                if (Interlocked.CompareExchange(ref control, after, before) == before)
                {
                    rejected = atLimit ? RecordResult.DroppedAdmissionLimit : RecordResult.Recorded;
                    if (!atLimit && (before >> AdmissionShift) == MaxAdmissions - 1)
                        rejected = RecordResult.DroppedAdmissionLimit;
                    return !atLimit;
                }
                sawCompetition = true;
            }
        }

        private void ReleaseAdmission()
        {
            while (true)
            {
                long before = ReadControl();
                if ((before >> AdmissionShift) == 0)
                    throw new InvalidOperationException("The observation admission count is invalid.");
                if (Interlocked.CompareExchange(ref control, before - AdmissionUnit, before) == before)
                    return;
            }
        }

        private static bool IsKnown(EventKind kind, EventPoint point)
        {
            return kind >= EventKind.BeforeSimulationFrame && kind <= EventKind.SaveData
                && (point == EventPoint.Enter || point == EventPoint.Exit);
        }

        private bool TryGetThreadTag(out int tag, out bool allocated)
        {
            tag = 0;
            allocated = false;
            if (threadCount >= threadCapacity)
            {
                MarkIssue(RecorderIssue.ThreadLimitReached, true);
                return false;
            }

            ThreadBinding binding = threadBinding;
            object owner = binding == null ? null : binding.Owner.Target;
            if (Object.ReferenceEquals(owner, this))
            {
                tag = binding.Tag;
                return true;
            }

            RelativeEventRecorder other = owner as RelativeEventRecorder;
            if (other != null && other.State != RecorderState.Stopped)
            {
                MarkIssue(RecorderIssue.ThreadConflict, true);
                return false;
            }

            int next = threadCount + 1;
            ThreadBinding replacement = new ThreadBinding(this, next);
            threadBinding = replacement;
            threadCount = next;
            tag = next;
            allocated = true;
            return true;
        }

        public RecordResult Observe(EventKind kind, EventPoint point)
        {
            RecordResult rejected;
            if (!TryAdmit(out rejected)) return rejected;

            bool ownsGuard = false;
            bool ownsGate = false;
            try
            {
                if (rejected == RecordResult.DroppedAdmissionLimit) return rejected;
                if (_insideObserver)
                {
                    MarkIssue(RecorderIssue.Reentrancy, true);
                    return RecordResult.DroppedReentrancy;
                }
                _insideObserver = true;
                ownsGuard = true;
                if (!Monitor.TryEnter(_gate))
                {
                    MarkIssue(RecorderIssue.Contention, true);
                    return RecordResult.DroppedContention;
                }
                ownsGate = true;

                if (State != RecorderState.Recording)
                {
                    MarkIssue(RecorderIssue.StopRace, false);
                    return RecordResult.DroppedStopping;
                }
                if (!IsKnown(kind, point))
                {
                    MarkIssue(RecorderIssue.InvalidEvent, true);
                    return RecordResult.DroppedInvalid;
                }

                int tag;
                bool allocated;
                if (!TryGetThreadTag(out tag, out allocated))
                    return RecordResult.DroppedThreadConflict;

                events[eventCount] = new EventRecord(kind, point, eventCount + 1, tag);
                eventCount++;
                RecorderIssue reached = RecorderIssue.None;
                if (eventCount == events.Length) reached |= RecorderIssue.CapacityReached;
                if (allocated && threadCount == threadCapacity) reached |= RecorderIssue.ThreadLimitReached;
                if (reached != RecorderIssue.None) MarkIssue(reached, true);
                return RecordResult.Recorded;
            }
            catch (Exception)
            {
                MarkIssue(RecorderIssue.Failure, true);
                return RecordResult.DroppedFailure;
            }
            finally
            {
                try
                {
                    if (ownsGate) Monitor.Exit(_gate);
                }
                catch (Exception)
                {
                    MarkIssue(RecorderIssue.Failure, true);
                }
                finally
                {
                    try { ReleaseAdmission(); }
                    finally { if (ownsGuard) _insideObserver = false; }
                }
            }
        }

        public RecorderState RequestStop()
        {
            while (true)
            {
                long before = ReadControl();
                RecorderState state = StateOf(before);
                if (state == RecorderState.Disabled)
                {
                    long stopped = WithState(WithIssue(before, RecorderIssue.NotStarted), RecorderState.Stopped);
                    if (Interlocked.CompareExchange(ref control, stopped, before) == before)
                        return RecorderState.Stopped;
                    continue;
                }
                if (state == RecorderState.Disabled || state == RecorderState.Stopped) return state;
                if (state == RecorderState.Stopping) return TryCompleteStop();
                long after = WithState(before, RecorderState.Stopping);
                if (Interlocked.CompareExchange(ref control, after, before) == before)
                    return TryCompleteStop();
            }
        }

        public RecorderState TryCompleteStop()
        {
            while (true)
            {
                long before = ReadControl();
                RecorderState state = StateOf(before);
                if (state != RecorderState.Stopping || (before >> AdmissionShift) != 0) return state;
                long after = WithState(before, RecorderState.Stopped);
                if (Interlocked.CompareExchange(ref control, after, before) == before)
                    return RecorderState.Stopped;
            }
        }

        public RecorderSnapshot ReadSnapshot()
        {
            long value = ReadControl();
            RecorderState state = StateOf(value);
            RecorderIssue issues = IssuesOf(value);
            if (state != RecorderState.Stopped)
                return new RecorderSnapshot(state, issues | RecorderIssue.Pending, new EventRecord[0]);
            EventRecord[] copy = new EventRecord[eventCount];
            Array.Copy(events, copy, eventCount);
            return new RecorderSnapshot(state, issues, copy);
        }
    }
}
