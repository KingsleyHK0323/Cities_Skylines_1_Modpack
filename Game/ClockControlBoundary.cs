// Signature: OpenAI-Codex_Realistic_City_Time
// Preview ownership boundary: OpenAI-Codex_city_time_development
// Explicit freshness evidence: OpenAI-Codex_Realistic_City_Time
// Update signature: OpenAI-Codex
using System;

namespace RealisticCityTime.Game
{
    internal enum ClockControlStatus
    {
        Inactive, Unknown, Stale, Paused, PrecisionRequired, InvalidMultiplier,
        UnverifiedBoundary, ConflictingOrUnknownWriter, Candidate, UnverifiedFreshness
    }

    internal sealed class ClockControlPreview
    {
        internal readonly ClockControlBoundary Owner;
        internal readonly GameClockSnapshot Snapshot;
        internal readonly decimal Multiplier;
        internal readonly int? Precision;
        internal readonly ClockControlStatus Status;
        // Deliberately hard-disabled: a valid preview is never permission to write native time.
        internal bool CanWrite { get { return false; } }

        internal ClockControlPreview(ClockControlBoundary owner, GameClockSnapshot snapshot,
            decimal multiplier, int? precision, ClockControlStatus status)
        {
            Owner = owner; Snapshot = snapshot; Multiplier = multiplier;
            Precision = precision; Status = status;
        }
    }

    /// <summary>Independent dry-run boundary. No game hook, date setter, or speed setter is attached.</summary>
    internal sealed class ClockControlBoundary
    {
        private readonly GameClockState state;
        private readonly object gate = new object();
#if SYNTHETIC_CLOCK_TESTS
        // OpenAI-Codex: test-only ordering matches the existing boundary-then-cache locks.
        internal object SyntheticGate { get { return gate; } }
#endif
        private long consumedGeneration;
        private long consumedSequence = -1;

        internal ClockControlBoundary(GameClockState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            this.state = state;
        }

        internal ClockControlPreview Evaluate(GameClockSnapshot sample, decimal multiplier,
            int? precision, bool? executionBoundaryVerified, bool? noOtherClockWriter)
        {
            return Evaluate(sample, multiplier, precision, executionBoundaryVerified, noOtherClockWriter, null);
        }

        internal ClockControlPreview Evaluate(GameClockSnapshot sample, decimal multiplier,
            int? precision, bool? executionBoundaryVerified, bool? noOtherClockWriter, bool? observationFreshnessVerified)
        {
            ClockControlStatus status;
            if (state.CurrentGeneration == 0) status = ClockControlStatus.Inactive;
            else if (sample == null) status = ClockControlStatus.Unknown;
            else if (!state.IsCurrent(sample))
                status = ClockControlStatus.Stale;
            else if (!sample.GameTime.HasValue || !sample.Frame.HasValue || !sample.Paused.HasValue
                || !sample.MonotonicTicks.HasValue || sample.MonotonicTicks.Value < 0)
                status = ClockControlStatus.Unknown;
            else if (sample.Paused.Value) status = ClockControlStatus.Paused;
            else if (!precision.HasValue) status = ClockControlStatus.PrecisionRequired;
            else if (precision.Value < 0 || precision.Value > 28 || multiplier < 1m || multiplier > 10m)
                status = ClockControlStatus.InvalidMultiplier;
            else if (decimal.Round(multiplier, precision.Value) != multiplier)
                status = ClockControlStatus.InvalidMultiplier;
            else if (executionBoundaryVerified != true) status = ClockControlStatus.UnverifiedBoundary;
            else if (noOtherClockWriter != true || !state.MayClaimPreview(this)) status = ClockControlStatus.ConflictingOrUnknownWriter;
            else if (observationFreshnessVerified != true) status = ClockControlStatus.UnverifiedFreshness;
            else status = ClockControlStatus.Candidate;
            return new ClockControlPreview(this, sample, multiplier, precision, status);
        }

        // This consumes only a preview. There is intentionally no native writer callback parameter.
        internal bool TryConsumeDryRun(ClockControlPreview preview, bool? executionBoundaryVerified,
            bool? noOtherClockWriter)
        {
            return TryConsumeDryRun(preview, executionBoundaryVerified, noOtherClockWriter, null);
        }

        internal bool TryConsumeDryRun(ClockControlPreview preview, bool? executionBoundaryVerified,
            bool? noOtherClockWriter, bool? observationFreshnessVerified)
        {
            lock (gate)
            {
                if (preview == null || !object.ReferenceEquals(preview.Owner, this)
                    || preview.Status != ClockControlStatus.Candidate) return false;
                ClockControlPreview current = Evaluate(preview.Snapshot, preview.Multiplier,
                    preview.Precision, executionBoundaryVerified, noOtherClockWriter, observationFreshnessVerified);
                if (current.Status != ClockControlStatus.Candidate) return false;
                return state.TryConsumePreview(preview.Snapshot, this, ref consumedGeneration, ref consumedSequence);
            }
        }
    }
}
