// Signature: OpenAI-Codex
using System;
using System.Globalization;

namespace RealisticCityTime.Game
{
    internal enum SyntheticClockStatus
    {
        Inactive, Stale, Unknown, Paused, Baseline, Discontinuity, SampleGap,
        UnverifiedInterval, MultiplierTransition, UnverifiedBoundary,
        ConflictingOrUnknownWriter, UnverifiedFreshness, InvalidInput, Overflow, Ready
    }

    // A diagnostic proposal, never permission to write any native clock or save.
    internal sealed class SyntheticClockPlan
    {
        internal readonly SyntheticClockCoordinator Owner;
        internal readonly long Revision;
        internal readonly GameClockSnapshot Snapshot;
        internal readonly SyntheticClockStatus Status;
        internal readonly long? DesiredTicks;
        internal readonly DateTime? ProposedGameTime;
        internal readonly ClockControlPreview Control;
        internal readonly bool CanAccept;
        internal readonly long NextGeneration;
        internal readonly long NextSequence;
        internal readonly bool NextHasAnchor;
        internal readonly long NextTimestamp;
        internal readonly DateTime NextGameTime;
        internal readonly uint NextFrame;
        internal readonly long NextRateRevision;
        private readonly PortableClockScaler nextScaler;
        internal readonly bool NextHasVirtualTime;
        internal readonly DateTime NextVirtualTime;
        internal readonly GameClockSnapshot NextObservedSnapshot;
        internal bool CanWrite { get { return false; } }

        internal SyntheticClockPlan(SyntheticClockCoordinator owner, long revision,
            GameClockSnapshot snapshot, SyntheticClockStatus status, long? desiredTicks,
            DateTime? proposedTime, ClockControlPreview control, bool canAccept,
            long nextGeneration, long nextSequence, bool nextHasAnchor, long nextTimestamp,
            DateTime nextGameTime, uint nextFrame, long nextRateRevision, PortableClockScaler nextScaler,
            bool nextHasVirtualTime, DateTime nextVirtualTime, GameClockSnapshot nextObservedSnapshot)
        {
            Owner = owner; Revision = revision; Snapshot = snapshot; Status = status;
            DesiredTicks = desiredTicks; ProposedGameTime = proposedTime; Control = control;
            CanAccept = canAccept; NextGeneration = nextGeneration; NextSequence = nextSequence;
            NextHasAnchor = nextHasAnchor; NextTimestamp = nextTimestamp; NextGameTime = nextGameTime;
            NextFrame = nextFrame; NextRateRevision = nextRateRevision; this.nextScaler = nextScaler;
            NextHasVirtualTime = nextHasVirtualTime; NextVirtualTime = nextVirtualTime;
            NextObservedSnapshot = nextObservedSnapshot;
        }

        // Never expose the mutable remainder held by a proposal or a committed coordinator.
        internal PortableClockScaler CopyScaler() { return nextScaler.Copy(); }
    }

    /// <summary>
    /// Synthetic-only coordinator for game-compatible sources. It is not linked from GameMod,
    /// supplies no native writer, and cannot prove callback continuity or external ownership.
    /// Evidence arguments are assertions made by a test fixture, not game observations.
    /// </summary>
    internal sealed class SyntheticClockCoordinator
    {
        private const int ProvisionalPrecision = 2;
        private readonly object gate = new object();
        private readonly GameClockState state;
        private readonly ClockControlBoundary boundary;
        private PortableClockScaler scaler = new PortableClockScaler(ProvisionalPrecision);
        private long revision;
        private long generation;
        private long sequence = -1;
        private bool hasAnchor;
        private long anchorTimestamp;
        private DateTime anchorGameTime;
        private uint anchorFrame;
        private long anchorRateRevision;
        private bool hasVirtualTime;
        private DateTime virtualTime;
        private GameClockSnapshot lastObservedSnapshot;
        private long rateRevision;
        private decimal multiplier = 1m;

        internal SyntheticClockCoordinator(GameClockState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            this.state = state;
            boundary = new ClockControlBoundary(state);
        }

        internal decimal Multiplier { get { lock (gate) { return multiplier; } } }

        // This is a provisional fake-input policy, not a finalized localized game UI.
        internal bool TrySetMultiplier(string text)
        {
            decimal value;
            if (string.IsNullOrEmpty(text)) return false;
            int point = -1;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '.' && point < 0) { point = i; continue; }
                if (c < '0' || c > '9') return false;
            }
            if (point == 0 || point == text.Length - 1 || (point >= 0 && text.Length - point - 1 > ProvisionalPrecision))
                return false;
            if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)
                || value < 1m || value > 10m) return false;
            lock (gate)
            {
                if (value != multiplier)
                {
                    long nextRateRevision = checked(rateRevision + 1);
                    long nextRevision = checked(revision + 1);
                    multiplier = value; rateRevision = nextRateRevision; revision = nextRevision;
                }
                return true;
            }
        }

        internal SyntheticClockPlan Prepare(GameClockSnapshot sample, bool? intervalUninterrupted,
            bool? executionBoundaryVerified, bool? noOtherClockWriter, bool? freshnessVerified)
        {
            lock (gate)
            {
                if (state.CurrentGeneration == 0) return Reject(sample, SyntheticClockStatus.Inactive);
                if (sample == null) return Reject(null, SyntheticClockStatus.Unknown);
                if (!state.IsCurrent(sample)) return Reject(sample, SyntheticClockStatus.Stale);
                bool sameGeneration = generation == sample.Generation;
                if (sameGeneration && sample.Sequence <= sequence) return Reject(sample, SyntheticClockStatus.Stale);

                PortableClockScaler nextScaler = sameGeneration ? scaler.Copy() : new PortableClockScaler(ProvisionalPrecision);
                bool priorAnchor = sameGeneration && hasAnchor;
                long priorSequence = sameGeneration ? sequence : -1;
                bool complete = sample.MonotonicTicks.HasValue && sample.MonotonicTicks.Value >= 0
                    && sample.GameTime.HasValue && sample.Frame.HasValue && sample.Paused.HasValue;
                if (complete && ((priorAnchor && (sample.MonotonicTicks.Value < anchorTimestamp
                    || sample.GameTime.Value < anchorGameTime || sample.Frame.Value < anchorFrame))
                    || (sameGeneration && lastObservedSnapshot != null
                        && (sample.MonotonicTicks.Value < lastObservedSnapshot.MonotonicTicks.Value
                            || sample.GameTime.Value < lastObservedSnapshot.GameTime.Value
                            || sample.Frame.Value < lastObservedSnapshot.Frame.Value))))
                {
                    nextScaler.Reset();
                    return Make(sample, SyntheticClockStatus.Discontinuity, null, null, null,
                        false, 0, DateTime.MinValue, 0, rateRevision, nextScaler);
                }
                ClockControlPreview control = boundary.Evaluate(sample, multiplier, ProvisionalPrecision,
                    executionBoundaryVerified, noOtherClockWriter, freshnessVerified);
                SyntheticClockStatus blocking = MapBlocking(control.Status);
                if (blocking != SyntheticClockStatus.Ready)
                {
                    if (blocking == SyntheticClockStatus.Stale || blocking == SyntheticClockStatus.Inactive)
                        return Reject(sample, blocking);
                    return Make(sample, blocking, null, null, null, false, 0, DateTime.MinValue, 0, rateRevision, nextScaler);
                }

                long now = sample.MonotonicTicks.Value;
                DateTime date = sample.GameTime.Value;
                uint frame = sample.Frame.Value;
                if (!priorAnchor)
                    return Make(sample, SyntheticClockStatus.Baseline, null, null, null,
                        true, now, date, frame, rateRevision, nextScaler);
                if (priorSequence >= 0 && sample.Sequence - priorSequence > 1)
                    return Make(sample, SyntheticClockStatus.SampleGap, null, null, null,
                        true, now, date, frame, rateRevision, nextScaler);
                if (intervalUninterrupted != true)
                    return Make(sample, SyntheticClockStatus.UnverifiedInterval, null, null, null,
                        true, now, date, frame, rateRevision, nextScaler);
                if (anchorRateRevision != rateRevision)
                    return Make(sample, SyntheticClockStatus.MultiplierTransition, null, null, null,
                        true, now, date, frame, rateRevision, nextScaler);

                try
                {
                    long desired = nextScaler.Advance(now - anchorTimestamp, multiplier);
                    DateTime proposed = virtualTime.AddTicks(desired);
                    return Make(sample, SyntheticClockStatus.Ready, desired, proposed, control,
                        true, now, date, frame, rateRevision, nextScaler);
                }
                catch (OverflowException)
                {
                    nextScaler.Reset();
                    return Make(sample, SyntheticClockStatus.Overflow, null, null, null,
                        false, 0, DateTime.MinValue, 0, rateRevision, nextScaler);
                }
                catch (ArgumentOutOfRangeException)
                {
                    nextScaler.Reset();
                    return Make(sample, SyntheticClockStatus.Overflow, null, null, null,
                        false, 0, DateTime.MinValue, 0, rateRevision, nextScaler);
                }
            }
        }

        // Only local bookkeeping is committed. A stale proposal never commits its copied remainder.
        internal bool TryAccept(SyntheticClockPlan plan, bool? executionBoundaryVerified,
            bool? noOtherClockWriter, bool? freshnessVerified)
        {
            lock (gate)
            {
                // The test-only cache gate covers currentness, ownership, and local bookkeeping.
                // No caller-supplied callback or native action runs inside this critical section.
                lock (boundary.SyntheticGate)
                {
                    lock (state.SyntheticGate)
                    {
                        if (plan == null || !object.ReferenceEquals(plan.Owner, this) || !plan.CanAccept
                            || plan.Revision != revision || !state.IsCurrent(plan.Snapshot)) return false;
                        long nextRevision = checked(revision + 1);
                        PortableClockScaler acceptedScaler = plan.CopyScaler();
                        // Baselines and reanchors need the same fresh evidence as an advancing proposal.
                        if (plan.NextHasAnchor && boundary.Evaluate(plan.Snapshot, multiplier, ProvisionalPrecision,
                            executionBoundaryVerified, noOtherClockWriter, freshnessVerified).Status != ClockControlStatus.Candidate)
                            return false;
                        if (plan.Status == SyntheticClockStatus.Ready
                            && !boundary.TryConsumeDryRun(plan.Control, executionBoundaryVerified, noOtherClockWriter, freshnessVerified))
                            return false;
                        if (!state.IsCurrent(plan.Snapshot)) return false;
                        generation = plan.NextGeneration; sequence = plan.NextSequence;
                        hasAnchor = plan.NextHasAnchor; anchorTimestamp = plan.NextTimestamp;
                        anchorGameTime = plan.NextGameTime; anchorFrame = plan.NextFrame;
                        anchorRateRevision = plan.NextRateRevision; scaler = acceptedScaler; revision = nextRevision;
                        hasVirtualTime = plan.NextHasVirtualTime; virtualTime = plan.NextVirtualTime;
                        lastObservedSnapshot = plan.NextObservedSnapshot;
                        return true;
                    }
                }
            }
        }

        private SyntheticClockPlan Reject(GameClockSnapshot sample, SyntheticClockStatus status)
        {
            return new SyntheticClockPlan(this, revision, sample, status, null, null, null, false,
                0, 0, false, 0, DateTime.MinValue, 0, 0, null, false, DateTime.MinValue, null);
        }

        private SyntheticClockPlan Make(GameClockSnapshot sample, SyntheticClockStatus status,
            long? ticks, DateTime? proposed, ClockControlPreview control, bool nextHasAnchor,
            long timestamp, DateTime time, uint frame, long nextRateRevision, PortableClockScaler nextScaler)
        {
            bool nextHasVirtualTime = generation == sample.Generation && hasVirtualTime;
            DateTime nextVirtualTime = nextHasVirtualTime ? virtualTime : DateTime.MinValue;
            GameClockSnapshot nextObservedSnapshot = generation == sample.Generation ? lastObservedSnapshot : null;
            if (status == SyntheticClockStatus.Discontinuity || status == SyntheticClockStatus.Overflow
                || status == SyntheticClockStatus.InvalidInput)
            {
                nextHasVirtualTime = false;
                nextVirtualTime = DateTime.MinValue;
            }
            if (nextHasAnchor && !nextHasVirtualTime)
            {
                nextHasVirtualTime = true;
                nextVirtualTime = time;
            }
            if (status == SyntheticClockStatus.Ready)
            {
                nextHasVirtualTime = true;
                nextVirtualTime = proposed.Value;
            }
            if (sample.MonotonicTicks.HasValue && sample.MonotonicTicks.Value >= 0
                && sample.GameTime.HasValue && sample.Frame.HasValue && sample.Paused.HasValue)
                nextObservedSnapshot = sample;
            return new SyntheticClockPlan(this, revision, sample, status, ticks, proposed, control, true,
                sample.Generation, sample.Sequence, nextHasAnchor, timestamp, time, frame, nextRateRevision, nextScaler,
                nextHasVirtualTime, nextVirtualTime, nextObservedSnapshot);
        }

        private static SyntheticClockStatus MapBlocking(ClockControlStatus status)
        {
            switch (status)
            {
                case ClockControlStatus.Candidate: return SyntheticClockStatus.Ready;
                case ClockControlStatus.Inactive: return SyntheticClockStatus.Inactive;
                case ClockControlStatus.Stale: return SyntheticClockStatus.Stale;
                case ClockControlStatus.Paused: return SyntheticClockStatus.Paused;
                case ClockControlStatus.UnverifiedBoundary: return SyntheticClockStatus.UnverifiedBoundary;
                case ClockControlStatus.ConflictingOrUnknownWriter: return SyntheticClockStatus.ConflictingOrUnknownWriter;
                case ClockControlStatus.UnverifiedFreshness: return SyntheticClockStatus.UnverifiedFreshness;
                case ClockControlStatus.InvalidMultiplier:
                case ClockControlStatus.PrecisionRequired: return SyntheticClockStatus.InvalidInput;
                default: return SyntheticClockStatus.Unknown;
            }
        }
    }
}
