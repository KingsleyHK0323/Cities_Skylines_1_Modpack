// Pure synthetic arithmetic model; it has no game or Harmony integration.
// Signature: OpenAI-Codex
using System;

namespace RealisticCityTime.ClockSafetyFixture
{
    // Available describes a synthetic arithmetic proposal, never permission to write a clock.
    public enum ProposalStatus { Skipped, Unavailable, Available }

    public sealed class Snapshot
    {
        public readonly uint Frame;
        public readonly long PerFrameTicks;
        public readonly long OffsetTicks;
        public readonly long DateTicks;

        public Snapshot(uint frame, long perFrameTicks, long offsetTicks, long dateTicks)
        {
            Frame = frame;
            PerFrameTicks = perFrameTicks;
            OffsetTicks = offsetTicks;
            DateTicks = dateTicks;
        }
    }

    public static class SyntheticClock
    {
        public static bool IsCoherent(Snapshot value)
        {
            if (value == null || value.PerFrameTicks <= 0 || value.DateTicks < DateTime.MinValue.Ticks || value.DateTicks > DateTime.MaxValue.Ticks)
                return false;
            try { return checked((long)value.Frame * value.PerFrameTicks + value.OffsetTicks) == value.DateTicks; }
            catch (OverflowException) { return false; }
        }

        public static ProposalStatus TryPropose(Snapshot before, long deltaTicks, bool enabled, bool hasFrame,
            bool paused, bool continuityKnown, bool priorMutationUncertain, out Snapshot proposed)
        {
            proposed = before;
            if (before == null || priorMutationUncertain) return ProposalStatus.Unavailable;
            if (!enabled || !hasFrame || paused) return ProposalStatus.Skipped;
            if (!continuityKnown || deltaTicks < 0 || !IsCoherent(before) || before.Frame == UInt32.MaxValue)
                return ProposalStatus.Unavailable;
            try
            {
                uint nextFrame = checked(before.Frame + 1U);
                long nextDate = checked(before.DateTicks + deltaTicks);
                long nextOffset = checked(checked(before.OffsetTicks + deltaTicks) - before.PerFrameTicks);
                Snapshot candidate = new Snapshot(nextFrame, before.PerFrameTicks, nextOffset, nextDate);
                if (!IsCoherent(candidate)) return ProposalStatus.Unavailable;
                proposed = candidate;
                return ProposalStatus.Available;
            }
            catch (OverflowException) { return ProposalStatus.Unavailable; }
        }

        public static long FrameToTime(Snapshot value, uint frame)
        {
            return checked(checked((long)frame * value.PerFrameTicks) + value.OffsetTicks);
        }

        public static uint TimeToFrame(Snapshot value, long ticks)
        {
            if (value == null || value.PerFrameTicks <= 0) throw new ArgumentOutOfRangeException("value");
            long elapsed = checked(ticks - value.OffsetTicks);
            if (elapsed < 0) throw new ArgumentOutOfRangeException("ticks");
            return checked((uint)(elapsed / value.PerFrameTicks));
        }

        public static long RawDisplay(Snapshot value, uint referenceFrame, float timer)
        {
            long interpolated = (long)((float)value.PerFrameTicks * timer);
            return checked(checked(value.OffsetTicks + checked((long)referenceFrame * value.PerFrameTicks)) + interpolated);
        }

        public static Snapshot FakeNativeStep(Snapshot value)
        {
            return new Snapshot(checked(value.Frame + 1U), value.PerFrameTicks, value.OffsetTicks,
                checked(value.DateTicks + value.PerFrameTicks));
        }
    }
}
