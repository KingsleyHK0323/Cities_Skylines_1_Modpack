// Signature: OpenAI-Codex_Realistic_City_Time
// Update signature: OpenAI-Codex_lifecycle_impl
// Preview ownership boundary: OpenAI-Codex_city_time_development
// Update signature: OpenAI-Codex
using System;
using System.Collections.Generic;

namespace RealisticCityTime.Game
{
    // This is a cached observation, not a guarantee that the game is still advancing.
    internal sealed class GameClockSnapshot
    {
        internal readonly long Generation;
        internal readonly long Sequence;
        internal readonly DateTime? GameTime;
        internal readonly bool? Paused;
        internal readonly uint? Frame;
        internal readonly long? MonotonicTicks;

        internal GameClockSnapshot(long generation, long sequence, DateTime? gameTime,
            bool? paused, uint? frame, long? monotonicTicks)
        {
            Generation = generation; Sequence = sequence; GameTime = gameTime;
            Paused = paused; Frame = frame; MonotonicTicks = monotonicTicks;
        }
    }

    internal sealed class GameClockState
    {
        private readonly object gate = new object();
#if SYNTHETIC_CLOCK_TESTS
        // OpenAI-Codex: test-only shared gate; no native writer or game hook uses this member.
        internal object SyntheticGate { get { return gate; } }
        internal bool FailSnapshotForTest;
        internal void SetSequenceForTest(long value) { lock (gate) { sequence = value; } }
#endif
        private long generation;
        private long sequence;
        private bool active;
        // A reserved loading generation is invisible until its second readiness check completes.
        private bool pendingLoading;
        private GameClockSnapshot latest;
        private ClockControlBoundary previewOwner;
        private readonly List<object> loadingOwners = new List<object>();
        private bool loadingOwnerConflict;

        private int LoadingOwnerIndex(object owner)
        {
            for (int i = 0; i < loadingOwners.Count; i++)
                if (object.ReferenceEquals(loadingOwners[i], owner)) return i;
            return -1;
        }

        private bool IsSoleLoadingOwner(object owner)
        {
            return owner != null && !loadingOwnerConflict && loadingOwners.Count == 1
                && object.ReferenceEquals(loadingOwners[0], owner);
        }

        internal long Begin()
        {
            lock (gate)
            {
                if (pendingLoading) { pendingLoading = false; active = false; latest = null; previewOwner = null; return 0; }
                // A direct begin cannot supersede registered or conflicting loading owners.
                if (loadingOwners.Count != 0 || loadingOwnerConflict) return 0;
                long next = checked(generation + 1);
                generation = next; sequence = 0; latest = null; previewOwner = null; active = true;
                return next;
            }
        }

        internal void End()
        {
            lock (gate) { pendingLoading = false; active = false; latest = null; previewOwner = null; }
        }

        // Competing registered instances make ownership unknown; duplicate registration is inert.
        internal void RegisterLoadingOwner(object owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            lock (gate)
            {
                if (LoadingOwnerIndex(owner) >= 0) return;
                pendingLoading = false;
                active = false; latest = null; previewOwner = null;
                if (loadingOwners.Count != 0) loadingOwnerConflict = true;
                loadingOwners.Add(owner);
            }
        }

        internal bool EndLoadingOwner(object owner)
        {
            lock (gate)
            {
                if (!IsSoleLoadingOwner(owner)) return false;
                pendingLoading = false;
                active = false; latest = null; previewOwner = null;
                return true;
            }
        }

        internal long BeginLoadingOwner(object owner)
        {
            lock (gate)
            {
                if (!IsSoleLoadingOwner(owner)) return 0;
                active = false; latest = null; previewOwner = null; pendingLoading = false;
                long next = checked(generation + 1);
                generation = next; pendingLoading = true;
                generation = next; sequence = 0; latest = null; previewOwner = null; active = true && !pendingLoading;
                return next;
            }
        }

        // Only the same pending owner and generation may become observable.
        internal bool ConfirmLoadingOwner(object owner, long token)
        {
            lock (gate)
            {
                if (!IsSoleLoadingOwner(owner) || !pendingLoading || active || token != generation) return false;
                pendingLoading = false;
                active = true;
                return true;
            }
        }

        internal bool TryEndLoadingOwner(object owner, long token)
        {
            lock (gate)
            {
                if (IsSoleLoadingOwner(owner) && pendingLoading && !active && token == generation)
                {
                    pendingLoading = false; latest = null; previewOwner = null;
                    return true;
                }
                if (!IsSoleLoadingOwner(owner) || !active || token != generation) return false;
                active = false; latest = null; previewOwner = null;
                return true;
            }
        }

        internal bool ReleaseLoadingOwner(object owner)
        {
            lock (gate)
            {
                int index = LoadingOwnerIndex(owner);
                if (index < 0) return false;
                pendingLoading = false;
                loadingOwners.RemoveAt(index);
                if (loadingOwners.Count == 0) loadingOwnerConflict = false;
                active = false; latest = null; previewOwner = null;
                return true;
            }
        }

        internal long CurrentGeneration
        {
            get { lock (gate) { return active ? generation : 0; } }
        }

        internal bool Publish(long token, DateTime? gameTime, bool? paused, uint? frame, long? monotonicTicks)
        {
            lock (gate)
            {
                if (!active || token != generation) return false;
                if (sequence == long.MaxValue) { latest = null; return false; }
                // Invalidate before allocating, so a failed replacement cannot retain stale data.
                latest = null;
#if SYNTHETIC_CLOCK_TESTS
                if (FailSnapshotForTest) throw new InvalidOperationException("Synthetic snapshot allocation failure.");
#endif
                latest = new GameClockSnapshot(generation, sequence++, gameTime, paused, frame, monotonicTicks);
                return true;
            }
        }

        internal GameClockSnapshot Latest
        {
            get { lock (gate) { if (!active) return null; return latest; } }
        }

        internal bool IsCurrent(GameClockSnapshot snapshot)
        {
            lock (gate) { return active && snapshot != null && object.ReferenceEquals(snapshot, latest); }
        }

        internal bool MayClaimPreview(ClockControlBoundary claimant)
        {
            lock (gate)
            {
                return active && claimant != null && (previewOwner == null || object.ReferenceEquals(previewOwner, claimant));
            }
        }

        // Atomic dry-run bookkeeping with the same lock used by unload and sample publication.
        // No native writer is invoked inside this method.
        internal bool TryConsumePreview(GameClockSnapshot snapshot, ClockControlBoundary claimant,
            ref long consumedGeneration, ref long consumedSequence)
        {
            lock (gate)
            {
                if (!active || snapshot == null || claimant == null || !object.ReferenceEquals(snapshot, latest)) return false;
                if (previewOwner != null && !object.ReferenceEquals(previewOwner, claimant)) return false;
                if (snapshot.Generation == consumedGeneration && snapshot.Sequence <= consumedSequence) return false;
                previewOwner = claimant;
                consumedGeneration = snapshot.Generation;
                consumedSequence = snapshot.Sequence;
                return true;
            }
        }
    }
}
