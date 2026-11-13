// Focused fake-host checks for an isolated transactional calendar candidate.
// These checks do not patch, load, or validate the game.
// Signature: OpenAI-Codex_clock_tests
using System;
using System.Threading;
using RealisticCityTime.TransactionalCalendar;

public static class TransactionalCandidateChecks
{
    private const long InitialDate = 621355968000000000L;
    private const long PerFrame = 1476562500L;
    private const long OneSecond = 10000000L;
    private const long HalfTimerTicks = 738281280L;
    private static int assertions;

    private sealed class SnapshotIdentity : System.Collections.Generic.IEqualityComparer<FrameSnapshot>
    {
        public bool Equals(FrameSnapshot left, FrameSnapshot right)
        {
            return Object.ReferenceEquals(left, right);
        }

        public int GetHashCode(FrameSnapshot value)
        {
            return Object.ReferenceEquals(value, null) ? 0
                : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }
    }

    private sealed class FakeStore : IAtomicFrameStore
    {
        private FrameSnapshot current;
        private readonly object publicationGate = new object();
        private long publishedTicks;
        private bool fenced;
        private object acceptedOwner;
        private readonly System.Collections.Generic.HashSet<FrameSnapshot> seen =
            new System.Collections.Generic.HashSet<FrameSnapshot>(new SnapshotIdentity());
        private readonly System.Collections.Generic.HashSet<uint> eventFrames =
            new System.Collections.Generic.HashSet<uint>();
        public int Reads;
        public int Attempts;
        public int Commits;
        public bool ThrowBefore;
        public bool ThrowAfter;
        public Action BeforeAttempt;
        public Action AfterReadCaptured;
        public Action BeforeFenceLock;

        public FakeStore(FrameSnapshot initial)
        {
            lock (publicationGate)
            {
            current = initial;
            publishedTicks = initial.LastPublishedTicks;
            seen.Add(initial);
            }
        }

        public bool TryAcquireOwner(object owner)
        {
            lock (publicationGate)
            {
                if (fenced || owner == null) return false;
                if (acceptedOwner == null) acceptedOwner = owner;
                return Object.ReferenceEquals(acceptedOwner, owner);
            }
        }

        public bool IsFenced
        {
            get
            {
                lock (publicationGate) { return fenced; }
            }
        }

        public void Fence()
        {
            Action beforeLock = BeforeFenceLock;
            if (beforeLock != null) beforeLock();
            lock (publicationGate) { fenced = true; }
        }

        public void RegisterEventFrame(uint frame)
        {
            lock (publicationGate)
            {
                if (fenced) throw new InvalidOperationException("Fake store session is fenced");
                eventFrames.Add(frame);
            }
        }

        public bool TrySaveWithoutWaiting(out FrameSnapshot snapshot)
        {
            snapshot = null;
            if (!Monitor.TryEnter(publicationGate)) return false;
            try
            {
                snapshot = Interlocked.CompareExchange(ref current, null, null);
                return true;
            }
            finally
            {
                Monitor.Exit(publicationGate);
            }
        }

        public FrameSnapshot Read()
        {
            Interlocked.Increment(ref Reads);
            Action afterRead = AfterReadCaptured;
            if (afterRead != null)
            {
                FrameSnapshot captured = Save();
                afterRead();
                return captured;
            }
            return Save();
        }

        public FrameSnapshot Save()
        {
            lock (publicationGate)
            return Interlocked.CompareExchange(ref current, null, null);
        }

        public void ReplaceExternally(FrameSnapshot replacement)
        {
            lock (publicationGate)
            {
                if (fenced || replacement == null || seen.Contains(replacement)
                    || replacement.LastPublishedTicks < publishedTicks) return;
                seen.Add(replacement);
            Interlocked.Exchange(ref current, replacement);
                publishedTicks = replacement.LastPublishedTicks;
            }
        }

        public long PublishedDisplay()
        {
            lock (publicationGate)
            {
                return publishedTicks;
            }
        }
        public bool PublishDisplayIfCurrent(FrameSnapshot observed)
        {
#if LEGACY_UNSYNCHRONIZED_READ
#error Legacy unsynchronized display publication is forbidden
            return Object.ReferenceEquals(Save(), observed);
#else
            lock (publicationGate)
            {
                if (fenced) return false;
                if (!Object.ReferenceEquals(Save(), observed)) return false;
                long value = RawDisplay(observed);
                if (value < publishedTicks) return false;
                publishedTicks = value;
                return true;
            }
#endif
        }

        public bool TryCommit(FrameSnapshot expected, FrameSnapshot next)
        {
            lock (publicationGate)
            {
            Interlocked.Increment(ref Attempts);
            if (fenced || expected == null || next == null || seen.Contains(next)
                || next.LastPublishedTicks < publishedTicks) Fence();
            if (fenced || expected == null || next == null || seen.Contains(next)
                || next.LastPublishedTicks < publishedTicks) return false;
            Action before = BeforeAttempt;
            if (before != null) before();
            if (fenced || seen.Contains(next) || next.LastPublishedTicks < publishedTicks) Fence();
            if (fenced || seen.Contains(next) || next.LastPublishedTicks < publishedTicks) return false;
            foreach (uint eventFrame in eventFrames)
            {
                try
                {
                    long eventDate = checked(checked((long)eventFrame * next.PerFrameTicks) + next.OffsetTicks);
                    if (eventDate < DateTime.MinValue.Ticks || eventDate > DateTime.MaxValue.Ticks) Fence();
                    if (eventDate < DateTime.MinValue.Ticks || eventDate > DateTime.MaxValue.Ticks) return false;
                }
                catch (OverflowException)
                {
                    Fence();
                    return false;
                }
            }
            if (ThrowBefore) Fence();
            if (ThrowBefore) throw new InvalidOperationException("Injected before commit");
            FrameSnapshot prior = Interlocked.CompareExchange(ref current, next, expected);
            if (!Object.ReferenceEquals(prior, expected)) Fence();
            if (!Object.ReferenceEquals(prior, expected)) return false;
            seen.Add(next);
            Interlocked.Increment(ref Commits);
            publishedTicks = next.LastPublishedTicks;
            if (ThrowAfter) Fence();
            if (ThrowAfter) throw new InvalidOperationException("Injected after commit");
            return true;
            }
        }
    }

    private static void Check(bool condition, string name)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
    }

    private static FrameSnapshot Initial()
    {
        return new FrameSnapshot(100U, 100U, PerFrame, InitialDate - 100L * PerFrame,
            InitialDate, 0.5f, InitialDate + HalfTimerTicks);
    }

    private static long RawDisplay(FrameSnapshot value)
    {
        return checked(checked(value.OffsetTicks + checked((long)value.ReferenceFrame * value.PerFrameTicks))
            + (long)((float)value.PerFrameTicks * value.Timer));
    }

    private static bool Coherent(FrameSnapshot value)
    {
        return checked((long)value.Frame * value.PerFrameTicks + value.OffsetTicks) == value.DateTicks;
    }

    private static void DisabledDoesNotTouchStore()
    {
        FakeStore store = new FakeStore(Initial());
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(!candidate.IsArmed, "candidate begins disabled");
        Check(candidate.Advance(OneSecond, 1, true, false) == AdvanceResult.Disabled,
            "disabled candidate declines advancement");
        Check(store.Reads == 0 && store.Attempts == 0, "disabled candidate does not read or write");
    }

    private static void OneMinuteAndEventMapping()
    {
        FrameSnapshot initial = Initial();
        FakeStore store = new FakeStore(initial);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "eligible fake session can be armed");
        Check(RawDisplay(initial) == InitialDate + HalfTimerTicks,
            "half-timer display uses the independently calculated float value");

        const uint eventFrame = 150U;
        store.RegisterEventFrame(eventFrame);
        long eventBefore = checked((long)eventFrame * PerFrame + initial.OffsetTicks);
        Check(candidate.Advance(OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "first eligible fake frame is committed");
        FrameSnapshot first = store.Save();
        long eventAfter = checked((long)eventFrame * PerFrame + first.OffsetTicks);
        Check(eventAfter - eventBefore == OneSecond - PerFrame,
            "a stored event frame changes interpreted date by delta minus per-frame duration");
        Check(first.LastPublishedTicks == InitialDate + HalfTimerTicks + OneSecond,
            "a half-timer display advances by one second instead of moving backward");

        for (int frame = 2; frame <= 60; frame++)
            Check(candidate.Advance(frame * OneSecond, 1, true, false) == AdvanceResult.Advanced,
                "each eligible fake frame commits once: " + frame);

        FrameSnapshot after = store.Save();
        Check(after.Frame == 160U && after.ReferenceFrame == 160U,
            "one minute advances the fake frame and display reference 60 times");
        Check(after.DateTicks == InitialDate + 600000000L,
            "60 real seconds advance the fake calendar by exactly 60 seconds at 1x");
        Check(after.PerFrameTicks == PerFrame && after.Timer == 0.5f,
            "per-frame duration and timer remain fixed in this fake contract");
        Check(after.OffsetTicks == initial.OffsetTicks + 60L * (OneSecond - PerFrame),
            "offset reflects each compensated frame");
        Check(Coherent(after), "frame to date remains coherent after one minute");
        Check((after.DateTicks - after.OffsetTicks) / after.PerFrameTicks == after.Frame,
            "date to frame returns the current fake frame");
        Check(after.LastPublishedTicks == InitialDate + HalfTimerTicks + 600000000L,
            "display advances by exactly one minute from its published starting value");
        Check(RawDisplay(after) == after.LastPublishedTicks, "raw display equals the published fake display");
        Check(store.Commits == 60, "each fake frame is committed exactly once");
    }

    private static void ZeroElapsedFrame()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(10L), "zero elapsed case arms");
        Check(candidate.Advance(10L, 1, true, false) == AdvanceResult.Advanced,
            "one observed frame with zero elapsed ticks can be represented");
        FrameSnapshot after = store.Save();
        Check(after.Frame == before.Frame + 1U && after.DateTicks == before.DateTicks,
            "zero elapsed ticks preserve date while advancing one fake frame");
        Check(after.LastPublishedTicks == before.LastPublishedTicks && Coherent(after),
            "zero elapsed ticks preserve display and frame/date consistency");
    }

    private static void ZeroFramesAccumulate()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "zero-frame accumulation example arms");
        for (int i = 1; i <= 60; i++)
            Check(candidate.Advance(i * OneSecond, 0, true, false) == AdvanceResult.NoFrame,
                "a continuous zero-frame interval remains pending: " + i);
        Check(candidate.IsArmed && store.Attempts == 0 && Object.ReferenceEquals(store.Save(), before),
            "continuous zero-frame observations preserve the anchor and do not write");
        Check(candidate.Advance(60L * OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "one frame consumes the pending 60-second interval");
        FrameSnapshot first = store.Save();
        Check(first.DateTicks == InitialDate + 600000000L && first.Frame == 101U && Coherent(first),
            "the pending 60 seconds advance the fake date exactly once");
        Check(first.LastPublishedTicks == before.LastPublishedTicks + 600000000L,
            "the fake display advances by the same accumulated interval");
        Check(candidate.Advance(60L * OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "the next frame may occur at the same timestamp");
        FrameSnapshot second = store.Save();
        Check(second.DateTicks == first.DateTicks && second.Frame == 102U && store.Commits == 2,
            "the following zero-elapsed frame does not consume the interval again");
    }
    private static void PauseAndUncertainIntervals()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "pause example arms");
        Check(candidate.Advance(5L * OneSecond, 1, true, true) == AdvanceResult.NeedsReanchor,
            "pause requires an explicit new anchor");
        Check(!candidate.IsArmed && Object.ReferenceEquals(store.Save(), before) && store.Attempts == 0,
            "pause makes no fake clock write");
        Check(candidate.Advance(6L * OneSecond, 1, true, false) != AdvanceResult.Advanced,
            "pause cannot silently reuse the previous anchor");
        Check(candidate.TryArm(6L * OneSecond), "a fake caller can explicitly reanchor");
        Check(candidate.Advance(7L * OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "one newly eligible second can advance after explicit reanchor");
        Check(store.Save().DateTicks == InitialDate + OneSecond,
            "the fake example does not silently include the paused interval");

#if LEGACY_ZERO_FRAME_POLICY
#error Legacy zero-frame elapsed policy is forbidden
        int[] frameCounts = new int[] { 0, 2 };
        for (int i = 0; i < frameCounts.Length; i++)
        {
            before = Initial();
            store = new FakeStore(before);
            candidate = new TransactionalCalendarCandidate(store);
            Check(candidate.TryArm(0L), "frame-count case arms: " + frameCounts[i]);
            Check(candidate.Advance(OneSecond, frameCounts[i], true, false) == AdvanceResult.NeedsReanchor,
                "zero or multiple fake frames require a new anchor: " + frameCounts[i]);
            Check(!candidate.IsArmed && store.Attempts == 0 && Object.ReferenceEquals(store.Save(), before),
                "zero or multiple fake frames do not write: " + frameCounts[i]);
        }

#endif
        before = Initial();
        store = new FakeStore(before);
        candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "multiple-frame case arms");
        Check(candidate.Advance(OneSecond, 2, true, false) == AdvanceResult.NeedsReanchor,
            "multiple fake frames require a new anchor");
        Check(!candidate.IsArmed && store.Attempts == 0 && Object.ReferenceEquals(store.Save(), before),
            "multiple fake frames do not write");
        before = Initial();
        store = new FakeStore(before);
        candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "unknown continuity example arms");
        Check(candidate.Advance(OneSecond, 1, false, false) == AdvanceResult.NeedsReanchor,
            "unknown interval continuity requires a new anchor");
        Check(!candidate.IsArmed && store.Attempts == 0 && Object.ReferenceEquals(store.Save(), before),
            "unknown interval continuity does not write");
    }

    private static void StaleVersionAndDisplayFence()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "stale snapshot example arms");
        FrameSnapshot external = new FrameSnapshot(before.Frame, before.ReferenceFrame, before.PerFrameTicks,
            before.OffsetTicks, before.DateTicks, 0.25f, before.LastPublishedTicks);
        store.ReplaceExternally(external);
        Check(candidate.Advance(OneSecond, 1, true, false) == AdvanceResult.Conflict,
            "an externally changed timer invalidates the expected version");
        Check(!candidate.IsArmed && Object.ReferenceEquals(store.Save(), external) && store.Commits == 0,
            "a stale proposal never overwrites the external snapshot");
        TransactionalCalendarCandidate staleSuccessor = new TransactionalCalendarCandidate(store);
        Check(store.IsFenced && !staleSuccessor.TryArm(2L * OneSecond),
            "a CAS conflict fences the shared store before returning");

        FrameSnapshot highDisplay = new FrameSnapshot(before.Frame, before.ReferenceFrame, before.PerFrameTicks,
            before.OffsetTicks, before.DateTicks, before.Timer, before.LastPublishedTicks + 2L * OneSecond);
        store = new FakeStore(highDisplay);
        candidate = new TransactionalCalendarCandidate(store);
        bool armed = candidate.TryArm(0L);
        if (armed)
            Check(candidate.Advance(OneSecond, 1, true, false) == AdvanceResult.Rejected,
                "a proposal below the last published display is rejected");
        else
            Check(candidate.Advance(OneSecond, 1, true, false) != AdvanceResult.Advanced,
                "an unsafe initial display cannot be advanced");
        Check(store.Attempts == 0 && Object.ReferenceEquals(store.Save(), highDisplay),
            "the display fence causes no fake clock write");
    }

    private static void RejectImpossibleFrame(FrameSnapshot initial, long start, long now, string name)
    {
        FakeStore store = new FakeStore(initial);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        bool armed = candidate.TryArm(start);
        if (armed)
            Check(candidate.Advance(now, 1, true, false) == AdvanceResult.Rejected, name + " rejected");
        else
            Check(candidate.Advance(now, 1, true, false) != AdvanceResult.Advanced, name + " cannot arm");
        Check(store.Attempts == 0 && Object.ReferenceEquals(store.Save(), initial),
            name + " never writes");
    }

    private static void ReloadedDisplayAlreadyReversed()
    {
        FrameSnapshot previouslyPublished = Initial();
        FrameSnapshot reloaded = new FrameSnapshot(previouslyPublished.Frame,
            previouslyPublished.ReferenceFrame, previouslyPublished.PerFrameTicks,
            previouslyPublished.OffsetTicks, previouslyPublished.DateTicks, 0f,
            previouslyPublished.LastPublishedTicks);
        FakeStore store = new FakeStore(reloaded);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        bool armed = candidate.TryArm(0L);
        if (armed)
            Check(candidate.Advance(OneSecond, 1, true, false) == AdvanceResult.Rejected,
                "a reloaded timer cannot republish below the saved last display");
        else
            Check(candidate.Advance(OneSecond, 1, true, false) != AdvanceResult.Advanced,
                "an already reversed reload display cannot arm an advancing candidate");
        Check(store.Attempts == 0 && Object.ReferenceEquals(store.Save(), reloaded),
            "the candidate does not write after an already reversed fake reload");
        Check(RawDisplay(reloaded) < reloaded.LastPublishedTicks,
            "the modeled reload reversal happened before candidate intervention");
    }
    private static void InvalidTimerValuesAreRejected()
    {
        float[] invalid = new float[]
        {
            Single.NaN, Single.PositiveInfinity, Single.NegativeInfinity, -0.01f, 1f, 1.5f
        };
        FrameSnapshot ordinary = Initial();
        for (int i = 0; i < invalid.Length; i++)
        {
            FrameSnapshot bad = new FrameSnapshot(ordinary.Frame, ordinary.ReferenceFrame,
                ordinary.PerFrameTicks, ordinary.OffsetTicks, ordinary.DateTicks, invalid[i],
                InitialDate - 100L * OneSecond);
            RejectImpossibleFrame(bad, 0L, OneSecond, "invalid reference timer case " + i);
        }
    }

    private static void NoFrameTimestampRegressionIsFenced()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "NoFrame timestamp regression example arms");
        Check(candidate.Advance(OneSecond, 0, true, false) == AdvanceResult.NoFrame,
            "first continuous NoFrame interval remains pending");
        Check(candidate.Advance(2L * OneSecond, 0, true, false) == AdvanceResult.NoFrame,
            "second continuous NoFrame interval remains pending");
        AdvanceResult result = candidate.Advance(OneSecond + OneSecond / 2L, 1, true, false);
        Check(result != AdvanceResult.Advanced && result != AdvanceResult.NoFrame,
            "a timestamp older than the latest NoFrame observation is rejected");
        Check(!candidate.IsArmed && store.Attempts == 0 && Object.ReferenceEquals(store.Save(), before),
            "a timestamp regression fences the candidate without consuming or writing");
        Check(candidate.Advance(3L * OneSecond, 1, true, false) != AdvanceResult.Advanced
            && store.Attempts == 0, "a later timestamp cannot silently consume the invalid interval");
    }

    private static void ElapsedPerFrameBound()
    {
        RejectImpossibleFrame(Initial(), 0L, PerFrame + 1L,
            "elapsed exceeding native per-frame duration");

        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "exact per-frame elapsed bound example arms");
        Check(candidate.Advance(PerFrame, 1, true, false) == AdvanceResult.Advanced,
            "elapsed exactly equal to the native per-frame duration is accepted");
        FrameSnapshot after = store.Save();
        Check(after.DateTicks == before.DateTicks + PerFrame && after.OffsetTicks == before.OffsetTicks
            && after.LastPublishedTicks == before.LastPublishedTicks + PerFrame && Coherent(after),
            "the inclusive bound keeps offset and advances date and display by exactly P");
    }

    private static void InvalidAndOverflowInputs()
    {
        FrameSnapshot ordinary = Initial();
        RejectImpossibleFrame(ordinary, 100L, 99L, "backward monotonic timestamp");

        long frameOffset = InitialDate - (long)UInt32.MaxValue;
        FrameSnapshot frameWrap = new FrameSnapshot(UInt32.MaxValue, 100U, 1L, frameOffset,
            InitialDate, 0f, frameOffset + 100L);
        RejectImpossibleFrame(frameWrap, 0L, 1L, "frame counter wrap");

        long referenceOffset = InitialDate - 100L;
        FrameSnapshot referenceWrap = new FrameSnapshot(100U, UInt32.MaxValue, 1L, referenceOffset,
            InitialDate, 0f, referenceOffset + (long)UInt32.MaxValue);
        RejectImpossibleFrame(referenceWrap, 0L, 1L, "display reference counter wrap");

        long nearMax = DateTime.MaxValue.Ticks - 1L;
        FrameSnapshot dateOverflow = new FrameSnapshot(0U, 0U, 1L, nearMax, nearMax, 0f, nearMax);
        RejectImpossibleFrame(dateOverflow, 0L, 5L, "calendar range overflow");
    }

    private static void CommitFailureFences(bool after)
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        store.ThrowBefore = !after;
        store.ThrowAfter = after;
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "failure example arms: " + after);
        Check(candidate.Advance(OneSecond, 1, true, false) == AdvanceResult.Uncertain,
            "commit exception is reported as uncertain: " + after);
        Check(!candidate.IsArmed, "commit exception disables candidate: " + after);

        FrameSnapshot observed = store.Save();
        if (after)
            Check(!Object.ReferenceEquals(observed, before) && observed.DateTicks == InitialDate + OneSecond
                && store.Commits == 1, "an exception after commit does not claim rollback");
        else
            Check(Object.ReferenceEquals(observed, before) && store.Commits == 0,
                "an exception before commit leaves the fake snapshot unchanged");
        store.ThrowBefore = false;
        store.ThrowAfter = false;
        int previousAttempts = store.Attempts;
        Check(!candidate.TryArm(2L * OneSecond), "an uncertain session cannot rearm: " + after);
        Check(candidate.Advance(3L * OneSecond, 1, true, false) != AdvanceResult.Advanced
            && store.Attempts == previousAttempts, "an uncertain commit is never retried: " + after);
    }

    private static void SaveAndStalePublication()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "save example arms");
        FrameSnapshot during = null;
        store.BeforeAttempt = delegate { during = store.Save(); };
        Check(candidate.Advance(OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "fake transaction with concurrent save completes");
        FrameSnapshot after = store.Save();
        Check(Object.ReferenceEquals(during, before) && !Object.ReferenceEquals(after, before),
            "fake save sees the complete old or new immutable snapshot");
        Check(Coherent(during) && Coherent(after), "both saved fake snapshots are coherent");
        long publishedBeforeStale = store.PublishedDisplay();
        Check(!store.PublishDisplayIfCurrent(before),
            "a delayed old-version fake display is rejected");
        Check(store.PublishedDisplay() == publishedBeforeStale,
            "a rejected old-version fake publication changes no displayed value");
        Check(store.PublishDisplayIfCurrent(after),
            "the current-version fake display can be published");
    }

    private static void ReentrantCommitIsUncertain()
    {
        FakeStore store = new FakeStore(Initial());
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "reentry example arms");
        AdvanceResult nested = AdvanceResult.Advanced;
        store.BeforeAttempt = delegate
        {
            store.BeforeAttempt = null;
            nested = candidate.Advance(OneSecond, 1, true, false);
        };
        AdvanceResult outer = candidate.Advance(OneSecond, 1, true, false);
        Check(nested != AdvanceResult.Advanced && outer != AdvanceResult.Advanced,
            "reentry cannot be reported as a successful outer commit");
        Check(!candidate.IsArmed, "reentry fences the fake session");
        Check(store.IsFenced && store.Commits == 0,
            "a callback-fenced fake store does not publish the outer proposal");
        int attempts = store.Attempts;
        Check(!candidate.TryArm(2L * OneSecond)
            && candidate.Advance(3L * OneSecond, 1, true, false) != AdvanceResult.Advanced
            && store.Attempts == attempts, "reentry cannot trigger a retry");
    }

    private static void RejectSnapshotAba()
    {
        FrameSnapshot first = Initial();
        FakeStore store = new FakeStore(first);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "ABA example arms");
        Check(candidate.Advance(0L, 1, true, false) == AdvanceResult.Advanced,
            "zero-elapsed fake commit produces a distinct snapshot");
        FrameSnapshot second = store.Save();
        long watermark = store.PublishedDisplay();
        Check(first.LastPublishedTicks == watermark && second.LastPublishedTicks == watermark,
            "ABA example has equal published watermarks");
        store.ReplaceExternally(first);
        Check(Object.ReferenceEquals(store.Save(), second) && store.PublishedDisplay() == watermark,
            "an already published snapshot cannot be reintroduced as an external version");
        Check(!store.TryCommit(second, first) && Object.ReferenceEquals(store.Save(), second),
            "an already published snapshot cannot be used as a commit version");
        TransactionalCalendarCandidate abaSuccessor = new TransactionalCalendarCandidate(store);
        Check(store.IsFenced && !abaSuccessor.TryArm(OneSecond),
            "an ABA rejection fences the shared store before returning");
    }

    private static void RejectForgedDisplayWatermark()
    {
        FakeStore store = new FakeStore(Initial());
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "watermark example arms");
        Check(candidate.Advance(OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "watermark example first publishes a newer display");
        FrameSnapshot current = store.Save();
        long watermark = store.PublishedDisplay();
        FrameSnapshot forged = new FrameSnapshot(current.Frame, current.ReferenceFrame,
            current.PerFrameTicks, current.OffsetTicks, current.DateTicks, current.Timer, watermark - 1L);
        store.ReplaceExternally(forged);
        Check(Object.ReferenceEquals(store.Save(), current) && store.PublishedDisplay() == watermark,
            "external snapshot cannot lower the actual published display watermark");
        Check(!store.TryCommit(current, forged) && Object.ReferenceEquals(store.Save(), current)
            && store.PublishedDisplay() == watermark,
            "commit cannot lower the actual published display watermark");
        TransactionalCalendarCandidate watermarkSuccessor = new TransactionalCalendarCandidate(store);
        Check(store.IsFenced && !watermarkSuccessor.TryArm(2L * OneSecond),
            "a watermark rejection fences the shared store before returning");
    }

    private static void FencedStoreRejectsNewCandidate()
    {
        FakeStore store = new FakeStore(Initial());
        store.ThrowAfter = true;
        TransactionalCalendarCandidate first = new TransactionalCalendarCandidate(store);
        Check(first.TryArm(0L), "after-commit fence example arms");
        Check(first.Advance(OneSecond, 1, true, false) == AdvanceResult.Uncertain,
            "an exception after fake commit reports uncertainty");
        Check(store.IsFenced && store.Commits == 1,
            "after-commit exception fences the shared store without claiming rollback");
        int attempts = store.Attempts;
        store.ThrowAfter = false;
        TransactionalCalendarCandidate later = new TransactionalCalendarCandidate(store);
        Check(!later.TryArm(2L * OneSecond), "a different candidate cannot arm the fenced store");
        Check(later.Advance(3L * OneSecond, 1, true, false) != AdvanceResult.Advanced
            && store.Attempts == attempts, "a different candidate cannot write after uncertain commit");
    }

    private static void StopFencesSharedStore()
    {
        FakeStore store = new FakeStore(Initial());
        TransactionalCalendarCandidate first = new TransactionalCalendarCandidate(store);
        Check(first.TryArm(0L), "stop fence example arms");
        first.Stop();
        Check(store.IsFenced, "stopping the first candidate fences its fake store session");
        TransactionalCalendarCandidate later = new TransactionalCalendarCandidate(store);
        Check(!later.TryArm(OneSecond), "a second candidate cannot arm after stop");
        Check(later.Advance(2L * OneSecond, 1, true, false) != AdvanceResult.Advanced
            && store.Attempts == 0, "a second candidate cannot write after stop");
    }

    private static void ConcurrentSaveCannotSeeHalfCommit()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "concurrent save example arms");
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent release = new ManualResetEvent(false);
        ManualResetEvent done = new ManualResetEvent(false);
        Exception failure = null;
        AdvanceResult result = AdvanceResult.Disabled;
        store.BeforeAttempt = delegate
        {
            entered.Set();
            if (!release.WaitOne(2000)) throw new TimeoutException("Fake commit was not released");
        };
        Thread worker = new Thread(delegate()
        {
            try { result = candidate.Advance(OneSecond, 1, true, false); }
            catch (Exception error) { failure = error; }
            finally { done.Set(); }
        });
        worker.IsBackground = true;
        bool enteredCommit = false;
        bool completed = false;
        bool readDuringCommit = true;
        FrameSnapshot partial = null;
        try
        {
            worker.Start();
            enteredCommit = entered.WaitOne(2000);
            if (enteredCommit) readDuringCommit = store.TrySaveWithoutWaiting(out partial);
        }
        finally
        {
            release.Set();
            completed = done.WaitOne(2000);
        }
        Check(enteredCommit, "worker reached the fake commit critical section");
        Check(completed && failure == null, "fake commit worker completed without an exception");
        Check(!readDuringCommit && partial == null,
            "another thread cannot read a partial fake snapshot while the commit lock is held");
        Check(result == AdvanceResult.Advanced, "the released fake commit succeeds");
        FrameSnapshot after = store.Save();
        Check(!Object.ReferenceEquals(after, before) && Coherent(before) && Coherent(after)
            && after.DateTicks == before.DateTicks + OneSecond,
            "save after release reads a complete new fake snapshot");
        Check(store.PublishedDisplay() == after.LastPublishedTicks,
            "the fake published display and saved snapshot share the committed watermark");
        if (completed)
        {
            entered.Close();
            release.Close();
            done.Close();
        }
    }

    private static void RejectOutOfRangeEventFrame()
    {
        FrameSnapshot before = new FrameSnapshot(1U, 1U, 100L, 0L, 100L, 0f, 100L);
        FakeStore store = new FakeStore(before);
        store.RegisterEventFrame(0U);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "event-range example arms");
        AdvanceResult result = candidate.Advance(1L, 1, true, false);
        Check(result != AdvanceResult.Advanced, "an event frame mapped before DateTime.MinValue is rejected");
        Check(Object.ReferenceEquals(store.Save(), before) && store.Commits == 0
            && store.PublishedDisplay() == before.LastPublishedTicks,
            "an out-of-range known event cannot publish the fake clock");
        TransactionalCalendarCandidate eventSuccessor = new TransactionalCalendarCandidate(store);
        Check(store.IsFenced && !eventSuccessor.TryArm(2L),
            "an event-range rejection fences the shared store before returning");
    }

    private static void RealCalendarOverflowAfterValidElapsed()
    {
        long nearMax = DateTime.MaxValue.Ticks - 1L;
        FrameSnapshot before = new FrameSnapshot(0U, 0U, 10L, nearMax, nearMax, 0f, nearMax);
        RejectImpossibleFrame(before, 0L, 5L, "calendar overflow with elapsed below native P");
    }

    private static void EventArithmeticOverflowFencesBeforeReturn()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        store.RegisterEventFrame(UInt32.MaxValue);
        FrameSnapshot invalid = new FrameSnapshot(before.Frame, before.ReferenceFrame,
            Int64.MaxValue, before.OffsetTicks, before.DateTicks, before.Timer, before.LastPublishedTicks);
        Check(!store.TryCommit(before, invalid) && Object.ReferenceEquals(store.Save(), before),
            "overflow while mapping a known event rejects the proposal");
        TransactionalCalendarCandidate successor = new TransactionalCalendarCandidate(store);
        Check(store.IsFenced && !successor.TryArm(OneSecond),
            "event mapping overflow fences the shared store before returning");
    }

    private static void DirectStaleCasFencesBeforeReturn()
    {
        FrameSnapshot old = Initial();
        FakeStore store = new FakeStore(old);
        FrameSnapshot current = new FrameSnapshot(old.Frame, old.ReferenceFrame, old.PerFrameTicks,
            old.OffsetTicks, old.DateTicks, old.Timer, old.LastPublishedTicks);
        store.ReplaceExternally(current);
        FrameSnapshot proposed = new FrameSnapshot(old.Frame, old.ReferenceFrame, old.PerFrameTicks,
            old.OffsetTicks, old.DateTicks, old.Timer, old.LastPublishedTicks);
        Check(!store.TryCommit(old, proposed) && Object.ReferenceEquals(store.Save(), current),
            "a stale direct compare-and-swap cannot replace the current version");
        TransactionalCalendarCandidate successor = new TransactionalCalendarCandidate(store);
        Check(store.IsFenced && !successor.TryArm(OneSecond),
            "a direct compare-and-swap mismatch fences the store before returning");
    }

    private static void FakeOwnerAcquisitionContract()
    {
        FakeStore store = new FakeStore(Initial());
        object first = new object();
        object second = new object();
        Check(store.TryAcquireOwner(first), "the first fake owner is accepted");
        Check(store.TryAcquireOwner(first), "the same fake owner can reacquire");
        Check(!store.TryAcquireOwner(second), "a different fake owner is rejected");
        store.Fence();
        Check(!store.TryAcquireOwner(first), "a fenced fake store refuses even its existing owner");
    }

    private static void MultipleCandidatesCannotDoubleCountElapsed()
    {
        FakeStore store = new FakeStore(Initial());
        TransactionalCalendarCandidate first = new TransactionalCalendarCandidate(store);
        TransactionalCalendarCandidate second = new TransactionalCalendarCandidate(store);
        Check(first.TryArm(0L), "the first candidate owns the fake session");
        Check(!second.TryArm(0L), "the second candidate cannot arm before the first commit");
        Check(first.Advance(OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "the owning candidate commits the first elapsed second");
        Check(!second.TryArm(0L), "the second candidate cannot arm with the stale initial timestamp");
        Check(first.Advance(2L * OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "the owning candidate commits the next elapsed second");
        Check(store.Save().DateTicks == InitialDate + 2L * OneSecond && store.Commits == 2,
            "two elapsed seconds advance the fake calendar by exactly two seconds");

        Check(first.Advance(3L * OneSecond, 1, true, true) == AdvanceResult.NeedsReanchor,
            "pause requires the owner to reanchor");
        Check(first.TryArm(4L * OneSecond), "the existing owner can reanchor after pause");
        Check(!second.TryArm(4L * OneSecond), "a different candidate remains excluded after reanchor");
        Check(first.Advance(5L * OneSecond, 1, true, false) == AdvanceResult.Advanced,
            "the same owner can advance a newly eligible second");
        Check(store.Save().DateTicks == InitialDate + 3L * OneSecond && store.Commits == 3,
            "the fake pause example neither duplicates earlier elapsed time nor silently catches up");
    }

    private static void ExternalStoreFenceImmediatelyDisarms()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "external fence example arms");
        store.Fence();
        Check(!candidate.IsArmed, "shared store fence is immediately visible through IsArmed");
        Check(candidate.Advance(OneSecond, 1, true, false) != AdvanceResult.Advanced
            && store.Attempts == 0 && Object.ReferenceEquals(store.Save(), before),
            "an externally fenced store cannot receive an advancement");
        Check(!candidate.TryArm(2L * OneSecond), "an externally fenced store cannot rearm");
    }

    private static void FenceBetweenReadCaptureAndArmReturn()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        ManualResetEvent captured = new ManualResetEvent(false);
        ManualResetEvent fenced = new ManualResetEvent(false);
        bool hookReached = false;
        Exception fenceError = null;
        Exception armError = null;
        bool armed = false;
        store.AfterReadCaptured = delegate
        {
            hookReached = true;
            captured.Set();
            if (!fenced.WaitOne(1500)) throw new TimeoutException("Read hook fence did not finish");
        };
        Thread fencer = new Thread(delegate()
        {
            try
            {
                if (!captured.WaitOne(1500)) throw new TimeoutException("Read was not captured");
                store.Fence();
            }
            catch (Exception error) { fenceError = error; }
            finally { fenced.Set(); }
        });
        fencer.IsBackground = true;
        fencer.Start();
        try { armed = candidate.TryArm(0L); }
        catch (Exception error) { armError = error; }
        bool joined = fencer.Join(1500);
        Check(joined && hookReached && fenceError == null && armError == null,
            "the read capture and concurrent fake fence completed within their bounds");
        Check(!armed, "TryArm refuses a fence completed after snapshot capture and before return");
        Check(!candidate.IsArmed && store.IsFenced && store.Attempts == 0
            && Object.ReferenceEquals(store.Save(), before),
            "the concurrent fence leaves the candidate disarmed and fake clock unchanged");
        if (joined)
        {
            captured.Close();
            fenced.Close();
        }
    }

    private static void CallbackWaitForFenceIsBounded()
    {
        FrameSnapshot before = Initial();
        FakeStore store = new FakeStore(before);
        TransactionalCalendarCandidate candidate = new TransactionalCalendarCandidate(store);
        Check(candidate.TryArm(0L), "bounded callback wait example arms");
        ManualResetEvent workerStarted = new ManualResetEvent(false);
        ManualResetEvent workerDone = new ManualResetEvent(false);
        ManualResetEvent fenceAtGate = new ManualResetEvent(false);
        Thread worker = null;
        Exception workerError = null;
        bool started = false;
        bool timedOut = false;
        bool reachedFenceLock = false;
        store.BeforeFenceLock = delegate { fenceAtGate.Set(); };
        store.BeforeAttempt = delegate
        {
            worker = new Thread(delegate()
            {
                workerStarted.Set();
#if LEGACY_DIRECT_STORE_FENCE
#error Legacy direct store fence does not exercise the candidate path
                try { store.Fence(); }
#else
                try { candidate.Stop(); }
#endif
                catch (Exception error) { workerError = error; }
                finally { workerDone.Set(); }
            });
            worker.IsBackground = true;
            worker.Start();
            started = workerStarted.WaitOne(1000);
            if (started) reachedFenceLock = fenceAtGate.WaitOne(1000);
#if LEGACY_UNCONFIRMED_FENCE_WAIT
#error Legacy wait lacks proof that the contender reached Fence
            if (started) timedOut = !workerDone.WaitOne(300);
#else
            if (started && reachedFenceLock) timedOut = !workerDone.WaitOne(300);
#endif
        };
        AdvanceResult outer = candidate.Advance(OneSecond, 1, true, false);
        bool completed = workerDone.WaitOne(1500);
        bool joined = worker != null && worker.Join(1500);
        Check(started && timedOut, "waiting for another thread to fence under the fake commit lock times out");
        Check(reachedFenceLock, "the second thread reached the fake Fence lock before the bounded wait");
        Check(completed && joined && workerError == null,
            "the blocked fake fence completes after the callback releases the lock");
        Check(store.IsFenced && !candidate.IsArmed, "the completed fake fence prevents later advancement");
        FrameSnapshot observed = store.Save();
        if (store.Commits == 1)
            Check(!Object.ReferenceEquals(observed, before) && Coherent(observed)
                && observed.DateTicks == before.DateTicks + OneSecond,
                "an outer commit that completed before the fence remains an actual commit");
        else
            Check(store.Commits == 0 && Object.ReferenceEquals(observed, before),
                "a rejected outer proposal leaves the fake snapshot unchanged");
        Check(outer != AdvanceResult.Advanced || store.Commits == 1,
            "the outer result cannot claim a commit that did not happen");
        int attempts = store.Attempts;
        Check(candidate.Advance(2L * OneSecond, 1, true, false) != AdvanceResult.Advanced
            && store.Attempts == attempts, "no later fake write follows the completed fence");
        if (joined)
        {
            workerStarted.Close();
            workerDone.Close();
            fenceAtGate.Close();
        }
    }

    public static int Run()
    {
        assertions = 0;
        DisabledDoesNotTouchStore();
        OneMinuteAndEventMapping();
        ZeroElapsedFrame();
        ZeroFramesAccumulate();
        PauseAndUncertainIntervals();
        StaleVersionAndDisplayFence();
        InvalidTimerValuesAreRejected();
        NoFrameTimestampRegressionIsFenced();
        ElapsedPerFrameBound();
        InvalidAndOverflowInputs();
        ReloadedDisplayAlreadyReversed();
        CommitFailureFences(false);
        CommitFailureFences(true);
        SaveAndStalePublication();
        ReentrantCommitIsUncertain();
        RejectSnapshotAba();
        RejectForgedDisplayWatermark();
        FencedStoreRejectsNewCandidate();
        StopFencesSharedStore();
        ConcurrentSaveCannotSeeHalfCommit();
        RejectOutOfRangeEventFrame();
        RealCalendarOverflowAfterValidElapsed();
        EventArithmeticOverflowFencesBeforeReturn();
        DirectStaleCasFencesBeforeReturn();
        FakeOwnerAcquisitionContract();
        MultipleCandidatesCannotDoubleCountElapsed();
        ExternalStoreFenceImmediatelyDisarms();
        FenceBetweenReadCaptureAndArmReturn();
        CallbackWaitForFenceIsBounded();
        return assertions;
    }
}