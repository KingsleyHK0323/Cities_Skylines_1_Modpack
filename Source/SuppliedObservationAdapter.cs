// Signature: OpenAI-Codex_Realistic_Border_Control
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RealisticBorderControl
{
    // Caller-defined identities and ticks, not verified game identifiers or time units.
    public sealed class ObservationStamp
    {
        public string CitySession { get; private set; }
        public long Sequence { get; private set; }
        public long PolicyVersion { get; private set; }
        public long SimulationTick { get; private set; }

        public ObservationStamp(string citySession, long sequence, long policyVersion, long simulationTick)
        {
            if (string.IsNullOrWhiteSpace(citySession)) throw new ArgumentException("A load-specific city session is required.");
            if (sequence < 0 || policyVersion < 0 || simulationTick < 0) throw new ArgumentOutOfRangeException("stamp");
            CitySession = citySession;
            Sequence = sequence;
            PolicyVersion = policyVersion;
            SimulationTick = simulationTick;
        }
    }

    // Null counts mean unknown, including classification or observation-window gaps.
    public sealed class SuppliedConnectionRow
    {
        public string ConnectionKey { get; private set; }
        public long ConnectionGeneration { get; private set; }
        public long? InboundPassengers { get; private set; }
        public long? OutboundPassengers { get; private set; }

        public SuppliedConnectionRow(string connectionKey, long connectionGeneration, long? inboundPassengers, long? outboundPassengers)
        {
            if (string.IsNullOrWhiteSpace(connectionKey)) throw new ArgumentException("A connection key is required.");
            if (connectionGeneration < 0 || inboundPassengers < 0 || outboundPassengers < 0)
                throw new ArgumentOutOfRangeException("row");
            ConnectionKey = connectionKey;
            ConnectionGeneration = connectionGeneration;
            InboundPassengers = inboundPassengers;
            OutboundPassengers = outboundPassengers;
        }
    }

    public enum SnapshotFreshness { Current, CityChanged, Superseded, PolicyChanged, TimeReversed, Expired }

    public sealed class SuppliedObservationSnapshot
    {
        private readonly ObservationStamp captured;
        private readonly ReadOnlyCollection<SuppliedConnectionRow> rows;
        private static readonly ReadOnlyCollection<SuppliedConnectionRow> Empty = new List<SuppliedConnectionRow>().AsReadOnly();
        public bool IsGameConnected { get { return false; } }
        public string DataSource { get { return "Unverified supplied preview data; no game API connected."; } }

        internal SuppliedObservationSnapshot(ObservationStamp captured, List<SuppliedConnectionRow> rows)
        {
            this.captured = captured;
            this.rows = rows.AsReadOnly();
        }

        // Current means fresh for the supplied context, not verified or complete game data.
        public SnapshotFreshness TryGetPreviewRows(ObservationStamp current, long maximumAgeTicks, out IReadOnlyList<SuppliedConnectionRow> preview)
        {
            preview = Empty;
            if (current == null) throw new ArgumentNullException("current");
            if (maximumAgeTicks < 0) throw new ArgumentOutOfRangeException("maximumAgeTicks");
            if (!StringComparer.Ordinal.Equals(captured.CitySession, current.CitySession)) return SnapshotFreshness.CityChanged;
            if (captured.Sequence != current.Sequence) return SnapshotFreshness.Superseded;
            if (captured.PolicyVersion != current.PolicyVersion) return SnapshotFreshness.PolicyChanged;
            if (current.SimulationTick < captured.SimulationTick) return SnapshotFreshness.TimeReversed;
            if (current.SimulationTick - captured.SimulationTick > maximumAgeTicks) return SnapshotFreshness.Expired;
            preview = rows;
            return SnapshotFreshness.Current;
        }
    }

    public static class SuppliedObservationAdapter
    {
        // No IO, game access, aggregation, classification inference, or quota application.
        public static SuppliedObservationSnapshot CopyForPreview(ObservationStamp captured, IEnumerable<SuppliedConnectionRow> suppliedRows)
        {
            if (captured == null) throw new ArgumentNullException("captured");
            if (suppliedRows == null) throw new ArgumentNullException("suppliedRows");
            var rows = new List<SuppliedConnectionRow>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in suppliedRows)
            {
                if (row == null || !keys.Add(row.ConnectionKey)) throw new ArgumentException("Rows must be nonnull with unique connection keys.");
                rows.Add(row);
            }
            return new SuppliedObservationSnapshot(captured, rows);
        }
    }
}
