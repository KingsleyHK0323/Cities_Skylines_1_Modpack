// Signature: OpenAI-Codex_Realistic_City_Time
using System;

namespace RealisticCityTime
{
    /// <summary>Data supplied by a future verified read-only source; null means unknown, never zero.</summary>
    public sealed class ClockObservation
    {
        public long Generation { get; private set; }
        public long Sequence { get; private set; }
        public long? MonotonicTimeSpanTicks { get; private set; }
        public DateTime? GameTime { get; private set; }
        public bool? Paused { get; private set; }
        public bool Discontinuity { get; private set; }

        public ClockObservation(long generation, long sequence, long? monotonicTimeSpanTicks,
            DateTime? gameTime, bool? paused, bool discontinuity = false)
        {
            Generation = generation;
            Sequence = sequence;
            MonotonicTimeSpanTicks = monotonicTimeSpanTicks;
            GameTime = gameTime;
            Paused = paused;
            Discontinuity = discontinuity;
        }
    }

    public enum ClockInputStatus { Inactive, Stale, Unknown, Paused, Baseline, Discontinuity, InvalidInput, Overflow, Ready }

    public sealed class ClockInputResult
    {
        public ClockInputStatus Status { get; private set; }
        public long? DesiredGameTicks { get; private set; }
        internal ClockInputResult(ClockInputStatus status, long? ticks = null)
        {
            Status = status;
            DesiredGameTicks = ticks;
        }
    }

    /// <summary>
    /// Serial-use input eligibility bridge, not a game reader or clock writer.
    /// All timestamps must already be normalized to TimeSpan ticks by the future source.
    /// </summary>
    public sealed class ClockInputSession
    {
        private readonly ClockModel model;
        private long generation;
        private long sequence = -1;
        private bool active;
        private long? anchor;
        private DateTime? lastGameTime;

        public ClockInputSession(int precision) { model = new ClockModel(precision); }

        public long BeginSession()
        {
            long next = checked(generation + 1);
            Clear(true);
            generation = next;
            sequence = -1;
            active = true;
            return generation;
        }

        public void EndSession() { active = false; Clear(true); }

        public ClockInputResult Observe(ClockObservation sample, decimal multiplier)
        {
            if (!active) return Result(ClockInputStatus.Inactive);
            if (sample == null) { Clear(false); return Result(ClockInputStatus.Unknown); }
            // A callback from an old generation must not invalidate the current anchor or remainder.
            if (sample.Generation != generation || sample.Sequence < 0 || sample.Sequence <= sequence)
                return Result(ClockInputStatus.Stale);
            sequence = sample.Sequence;
            if (!sample.MonotonicTimeSpanTicks.HasValue || !sample.GameTime.HasValue || !sample.Paused.HasValue)
            {
                Clear(false);
                return Result(ClockInputStatus.Unknown);
            }
            long now = sample.MonotonicTimeSpanTicks.Value;
            if (now < 0) { Clear(true); return Result(ClockInputStatus.InvalidInput); }
            if (sample.Discontinuity || (anchor.HasValue && now < anchor.Value)
                || (lastGameTime.HasValue && sample.GameTime.Value < lastGameTime.Value))
            {
                Clear(true);
                return Result(ClockInputStatus.Discontinuity);
            }
            if (sample.Paused.Value)
            {
                Clear(false);
                return Result(ClockInputStatus.Paused);
            }
            if (!anchor.HasValue)
            {
                anchor = now;
                lastGameTime = sample.GameTime;
                return Result(ClockInputStatus.Baseline);
            }
            long elapsed = now - anchor.Value; // Both timestamps are nonnegative and nondecreasing.
            try
            {
                long ticks = model.Advance(elapsed, multiplier);
                anchor = now;
                lastGameTime = sample.GameTime;
                return new ClockInputResult(ClockInputStatus.Ready, ticks);
            }
            catch (OverflowException) { Clear(true); return Result(ClockInputStatus.Overflow); }
            catch (ArgumentException) { Clear(true); return Result(ClockInputStatus.InvalidInput); }
        }

        private void Clear(bool resetFraction)
        {
            anchor = null;
            lastGameTime = null;
            if (resetFraction) model.Reset();
        }
        private static ClockInputResult Result(ClockInputStatus status) { return new ClockInputResult(status); }
    }
}
