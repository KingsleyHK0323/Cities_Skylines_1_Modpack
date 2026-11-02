// Signature: OpenAI-Codex_Realistic_Urban_Population
using System;

namespace RealisticUrbanPopulation.Demand
{
    // A future game source must read on the verified simulation context. No game source is supplied here.
    public interface IDemandSnapshotSource
    {
        // The source must validate this epoch and its instance lifetime in its safe read context.
        bool TryRead(long expectedEpoch, out DemandReadValues values);
    }

    public sealed class DemandReadValues
    {
        private readonly int residential;
        private readonly int commercial;
        private readonly int workplace;
        public DemandReadValues(int residential, int commercial, int workplace)
        { this.residential = residential; this.commercial = commercial; this.workplace = workplace; }
        public int Residential { get { return residential; } }
        public int Commercial { get { return commercial; } }
        public int Workplace { get { return workplace; } }
    }

    public enum DemandReadStatus { Unavailable, Captured, SourceFailed, Invalidated }

    public sealed class DemandReadSnapshot
    {
        private readonly DemandReadStatus status;
        private readonly long epoch;
        private readonly long sessionRevision;
        private readonly long sequence;
        private readonly string sourceId;
        private readonly DemandReadValues values;
        internal DemandReadSnapshot(DemandReadStatus status, long epoch, long revision, long sequence,
            string sourceId, DemandReadValues values)
        {
            this.status = status; this.epoch = epoch; sessionRevision = revision;
            this.sequence = sequence; this.sourceId = sourceId; this.values = values;
        }
        public DemandReadStatus Status { get { return status; } }
        public long Epoch { get { return epoch; } }
        public long SessionRevision { get { return sessionRevision; } }
        public long Sequence { get { return sequence; } }
        // A caller-provided label is provenance, not proof of a verified game version or source.
        public string SourceId { get { return sourceId; } }
        public DemandReadValues Values { get { return values; } }
        public bool GameSourceVerified { get { return false; } }
        public bool OfficeCouplingKnown { get { return false; } }
    }

    // Caller-owned reader: it neither hooks the game nor writes to DemandSession or game fields.
    public sealed class DemandSnapshotReader
    {
        private readonly object sync = new object();
        private readonly DemandSession session;
        private IDemandSnapshotSource source;
        private string sourceId;
        private long epoch;
        private long binding;
        private long request;
        private long sequence;
        private DemandReadSnapshot latest;

        public DemandSnapshotReader(DemandSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            this.session = session;
        }

        private DemandReadSnapshot Empty(DemandReadStatus status)
        { return new DemandReadSnapshot(status, epoch, 0, sequence, sourceId, null); }

        public bool Bind(long cityEpoch, IDemandSnapshotSource candidate, string candidateId)
        {
            lock (sync)
            {
                DemandSessionState state = session.ReadState();
                // A stale request must not invalidate the binding for a newer city.
                if (!state.Loaded || state.Epoch != cityEpoch) return false;
                binding = checked(binding + 1);
                epoch = cityEpoch;
                source = candidate;
                sourceId = candidateId;
                latest = null;
                if (candidate == null || candidateId == null || candidateId.Trim().Length == 0)
                { source = null; sourceId = null; return false; }
                return true;
            }
        }

        public bool Invalidate(long cityEpoch)
        {
            lock (sync)
            {
                DemandSessionState state = session.ReadState();
                if (epoch != cityEpoch || state.Epoch != cityEpoch) return false;
                binding = checked(binding + 1);
                source = null;
                latest = Empty(DemandReadStatus.Invalidated);
                return true;
            }
        }

        public DemandReadSnapshot Capture()
        {
            IDemandSnapshotSource capturedSource;
            string capturedSourceId;
            long capturedBinding;
            long capturedRequest;
            DemandSessionState before;
            lock (sync)
            {
                before = session.ReadState();
                if (source == null || !before.Loaded || before.Epoch != epoch)
                { latest = null; return Empty(DemandReadStatus.Unavailable); }
                capturedSource = source;
                capturedSourceId = sourceId;
                capturedBinding = binding;
                capturedRequest = request = checked(request + 1);
            }

            DemandReadValues values = null;
            bool read;
            try { read = capturedSource.TryRead(before.Epoch, out values); }
            catch (Exception) { read = false; }

            lock (sync)
            {
                // Old completions cannot clear or overwrite a newer binding or its observation.
                if (capturedBinding != binding || capturedRequest != request)
                    return new DemandReadSnapshot(DemandReadStatus.Invalidated, before.Epoch,
                        before.Revision, 0, capturedSourceId, null);
                DemandSessionState after = session.ReadState();
                if (!after.Loaded || after.Epoch != before.Epoch || after.Revision != before.Revision)
                { latest = null; return Empty(DemandReadStatus.Invalidated); }
                if (!read || values == null)
                { latest = Empty(DemandReadStatus.SourceFailed); return latest; }
                sequence = checked(sequence + 1);
                latest = new DemandReadSnapshot(DemandReadStatus.Captured, epoch, after.Revision,
                    sequence, sourceId, values);
                return latest;
            }
        }

        // Revalidates the cached observation; callers retaining an old object must not call it current.
        public DemandReadSnapshot ReadLatest()
        {
            lock (sync)
            {
                DemandSessionState state = session.ReadState();
                if (latest == null) return Empty(DemandReadStatus.Unavailable);
                if (!state.Loaded || state.Epoch != epoch ||
                    (latest.Status == DemandReadStatus.Captured && state.Revision != latest.SessionRevision))
                { latest = null; return Empty(DemandReadStatus.Invalidated); }
                return latest;
            }
        }
    }
}
