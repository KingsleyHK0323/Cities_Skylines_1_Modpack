// Isolated fake-host intervention candidate; not connected to the game or Harmony.
// Signature: OpenAI-Codex
using System;

namespace RealisticCityTime.TransactionalCalendar
{
    public enum AdvanceResult
    {
        Disabled, Advanced, NeedsReanchor, Rejected, Conflict, Uncertain, NoFrame
    }

    public sealed class FrameSnapshot
    {
        public readonly uint Frame;
        public readonly uint ReferenceFrame;
        public readonly long PerFrameTicks;
        public readonly long OffsetTicks;
        public readonly long DateTicks;
        public readonly float Timer;
        public readonly long LastPublishedTicks;

        public FrameSnapshot(uint frame, uint referenceFrame, long perFrameTicks, long offsetTicks,
            long dateTicks, float timer, long lastPublishedTicks)
        {
            Frame = frame;
            ReferenceFrame = referenceFrame;
            PerFrameTicks = perFrameTicks;
            OffsetTicks = offsetTicks;
            DateTicks = dateTicks;
            Timer = timer;
            LastPublishedTicks = lastPublishedTicks;
        }
    }

    public interface IAtomicFrameStore
    {
        FrameSnapshot Read();
        bool TryCommit(FrameSnapshot expected, FrameSnapshot next);
        bool IsFenced { get; }
        bool TryAcquireOwner(object owner);
        void Fence();
    }

    public sealed class TransactionalCalendarCandidate
    {
        private readonly IAtomicFrameStore store;
        private int entered;
        private int fenced;
        private int armed;
        private bool hasObserved;
        private long lastObserved;
        private long lastCommitted;
        private FrameSnapshot accepted;

        public TransactionalCalendarCandidate(IAtomicFrameStore store)
        {
            if (store == null) throw new ArgumentNullException("store");
            this.store = store;
        }

        private bool Enter()
        {
            if (System.Threading.Interlocked.CompareExchange(ref entered, 1, 0) == 0) return true;
            Fence();
            return false;
        }

        private void Exit()
        {
            System.Threading.Interlocked.Exchange(ref entered, 0);
        }

        private void Fence()
        {
            System.Threading.Interlocked.Exchange(ref fenced, 1);
            System.Threading.Interlocked.Exchange(ref armed, 0);
            try { store.Fence(); }
            catch (Exception) { /* The local instance remains fenced. */ }
        }

        private bool IsFenced()
        {
            return System.Threading.Interlocked.CompareExchange(ref fenced, 0, 0) != 0;
        }

        private bool IsAvailable()
        {
            if (!IsFenced() && System.Threading.Interlocked.CompareExchange(ref armed, 0, 0) != 0)
            {
                try { if (store.IsFenced) { Fence(); return false; } }
                catch (Exception) { Fence(); return false; }
            }
            return !IsFenced() && System.Threading.Interlocked.CompareExchange(ref armed, 0, 0) != 0;
        }

        private static bool TryDisplay(FrameSnapshot value, out long display)
        {
            display = 0;
            if (value == null || value.PerFrameTicks <= 0 || Single.IsNaN(value.Timer) ||
                Single.IsInfinity(value.Timer) || value.Timer < 0f || value.Timer >= 1f)
                return false;
            float fractional = (float)value.PerFrameTicks * value.Timer;
            if (Single.IsNaN(fractional) || Single.IsInfinity(fractional) || fractional < 0f ||
                (double)fractional >= 9223372036854775808.0d)
                return false;
            try
            {
                long fractionTicks = (long)fractional;
                display = checked(checked(value.OffsetTicks +
                    checked((long)value.ReferenceFrame * value.PerFrameTicks)) + fractionTicks);
                return display >= DateTime.MinValue.Ticks && display <= DateTime.MaxValue.Ticks;
            }
            catch (OverflowException) { return false; }
        }

        private static bool IsCoherent(FrameSnapshot value, out long display)
        {
            display = 0;
            if (!TryDisplay(value, out display) ||
                value.DateTicks < DateTime.MinValue.Ticks || value.DateTicks > DateTime.MaxValue.Ticks ||
                value.LastPublishedTicks < DateTime.MinValue.Ticks ||
                value.LastPublishedTicks > DateTime.MaxValue.Ticks)
                return false;
            try
            {
                return checked(checked((long)value.Frame * value.PerFrameTicks) +
                    value.OffsetTicks) == value.DateTicks;
            }
            catch (OverflowException) { return false; }
        }
        public void Stop()
        {
            // Explicit stop entry point.
            Fence();
        }

        public bool IsArmed
        {
            get { if (store != null) return IsAvailable(); return false; }
        }

        public bool TryArm(long monotonicTicks)
        {
            if (store != null)
            {
                if (!Enter()) return false;
                try
                {
                    if (IsFenced() || IsAvailable()) return false;
                    try { if (store.IsFenced) { Fence(); return false; } }
                    catch (Exception) { Fence(); return false; }
                    if (monotonicTicks < 0 || (hasObserved && monotonicTicks < lastObserved))
                    {
                        Fence();
                        return false;
                    }
                    try { if (!store.TryAcquireOwner(this)) return false; }
                    catch (Exception) { Fence(); return false; }
                    FrameSnapshot snapshot;
                    try { snapshot = store.Read(); }
                    catch (Exception) { Fence(); return false; }
                    long display;
                    if (!IsCoherent(snapshot, out display) || display < snapshot.LastPublishedTicks || IsFenced())
                    {
                        Fence();
                        return false;
                    }
                    accepted = snapshot;
                    lastCommitted = monotonicTicks;
                    lastObserved = monotonicTicks;
                    hasObserved = true;
                    System.Threading.Interlocked.Exchange(ref armed, 1);
                    // Success observes the shared fence; it can change after that observation.
                    if (store != null) return IsAvailable();
                    return !IsFenced();
                }
                finally { Exit(); }
            }
            return false;
        }

        public AdvanceResult Advance(long monotonicTicks, int frameCount, bool continuityVerified, bool paused)
        {
            if (store != null)
            {
                if (!Enter()) return AdvanceResult.Uncertain;
                try
                {
                    if (IsFenced()) return AdvanceResult.Uncertain;
                    if (!IsAvailable()) return AdvanceResult.Disabled;
                    try { if (store.IsFenced) { Fence(); return AdvanceResult.Uncertain; } }
                    catch (Exception) { Fence(); return AdvanceResult.Uncertain; }
                    if (monotonicTicks < lastObserved || frameCount < 0)
                    {
                        Fence();
                        return AdvanceResult.Rejected;
                    }
                    lastObserved = monotonicTicks;
                    if (paused || !continuityVerified || frameCount > 1)
                    {
                        accepted = null;
                        System.Threading.Interlocked.Exchange(ref armed, 0);
                        return AdvanceResult.NeedsReanchor;
                    }
                    if (frameCount == 0) return AdvanceResult.NoFrame;
                    if (accepted == null)
                    {
                        Fence();
                        return AdvanceResult.Uncertain;
                    }

                    FrameSnapshot next;
                    long oldDisplay;
                    long newDisplay;
                    try
                    {
                        long elapsed = checked(monotonicTicks - lastCommitted);
                        if (elapsed < 0 || elapsed > accepted.PerFrameTicks ||
                            !IsCoherent(accepted, out oldDisplay))
                        {
                            Fence();
                            return AdvanceResult.Rejected;
                        }
                        uint nextFrame = checked(accepted.Frame + 1U);
                        uint nextReference = checked(accepted.ReferenceFrame + 1U);
                        long nextDate = checked(accepted.DateTicks + elapsed);
                        long nextOffset = checked(checked(accepted.OffsetTicks + elapsed) -
                            accepted.PerFrameTicks);
                        FrameSnapshot trial = new FrameSnapshot(nextFrame, nextReference,
                            accepted.PerFrameTicks, nextOffset, nextDate, accepted.Timer,
                            accepted.LastPublishedTicks);
                        if (!IsCoherent(trial, out newDisplay) ||
                            newDisplay < accepted.LastPublishedTicks ||
                            newDisplay != checked(oldDisplay + elapsed))
                        {
                            Fence();
                            return AdvanceResult.Rejected;
                        }
                        next = new FrameSnapshot(nextFrame, nextReference, accepted.PerFrameTicks,
                            nextOffset, nextDate, accepted.Timer, newDisplay);
                    }
                    catch (OverflowException)
                    {
                        Fence();
                        return AdvanceResult.Rejected;
                    }

                    if (IsFenced()) return AdvanceResult.Uncertain;
                    bool committed;
                    try { committed = store.TryCommit(accepted, next); }
                    catch (Exception) { Fence(); return AdvanceResult.Uncertain; }
                    if (!committed)
                    {
                        if (IsFenced()) return AdvanceResult.Uncertain;
                        Fence();
                        return AdvanceResult.Conflict;
                    }
                    if (IsFenced()) return AdvanceResult.Uncertain;
                    FrameSnapshot observed;
                    try { observed = store.Read(); }
                    catch (Exception) { Fence(); return AdvanceResult.Uncertain; }
                    if (!Object.ReferenceEquals(observed, next) || IsFenced())
                    {
                        Fence();
                        return AdvanceResult.Uncertain;
                    }
                    accepted = next;
                    lastCommitted = monotonicTicks;
                    return AdvanceResult.Advanced;
                }
                finally { Exit(); }
            }
            return AdvanceResult.Disabled;
        }
    }
}