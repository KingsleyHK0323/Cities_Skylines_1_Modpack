// Signature: OpenAI-Codex_Responsible: Realistic_Vehicle_Traffic_Control
using System;
using System.Text;
using System.Globalization;

namespace RealisticVehicleTrafficControl.Model {
    public enum DataOrigin { Unknown, SyntheticFixture, GameCapture }
    public enum LaneKind { Unknown, GeneralCar, Sidewalk, Bus, Rail }
    public enum TurnKind { Left, Forward, Right, UTurn, Unknown }
    public enum ProposalStatus { Unknown, Unsupported, Ready }

    // All orientation and legality evidence must be supplied by the reader.
    // This model never infers game geometry from an ID or lane array index.
    public sealed class LaneObservation {
        private readonly uint[] allowed;
        public uint LaneId { get; private set; }
        public ushort SegmentId { get; private set; }
        public uint SegmentBuildIndex { get; private set; }
        public int LateralRank { get; private set; }
        public bool OrientationReversed { get; private set; }
        public LaneKind Kind { get; private set; }
        public TurnKind Turn { get; private set; }
        public uint[] AllowedTargets { get { return (uint[])allowed.Clone(); } }

        public LaneObservation(uint laneId, ushort segmentId, uint segmentBuildIndex,
            int lateralRank, bool orientationReversed, LaneKind kind, TurnKind turn, uint[] allowedTargets) {
            LaneId = laneId; SegmentId = segmentId; SegmentBuildIndex = segmentBuildIndex;
            LateralRank = lateralRank; OrientationReversed = orientationReversed;
            Kind = kind; Turn = turn;
            allowed = allowedTargets == null ? new uint[0] : (uint[])allowedTargets.Clone();
        }
        internal long Order(bool leftHandTraffic) {
            long rank = LateralRank;
            return (OrientationReversed ^ leftHandTraffic) ? -rank : rank;
        }
        internal int TurnOrder(bool leftHandTraffic) {
            return leftHandTraffic && (Turn == TurnKind.Left || Turn == TurnKind.Right)
                ? 2 - (int)Turn : (int)Turn;
        }
        internal void AppendIdentity(StringBuilder text) {
            text.Append(LaneId).Append(':').Append(SegmentId).Append(':').Append(SegmentBuildIndex)
                .Append(':').Append(LateralRank.ToString(CultureInfo.InvariantCulture)).Append(':')
                .Append(OrientationReversed ? '1' : '0').Append(':').Append((int)Kind).Append(':').Append((int)Turn);
            uint[] ids = AllowedTargets;
            Array.Sort(ids);
            foreach (uint id in ids) text.Append(',').Append(id);
            text.Append(';');
        }
    }

    // One selected road approach, not a complete node conflict graph.
    public sealed class JunctionSnapshot {
        private readonly LaneObservation[] incoming;
        private readonly LaneObservation[] outgoing;
        public long CityEpoch { get; private set; }
        public ushort NodeId { get; private set; }
        public uint TopologyRevision { get; private set; }
        public bool LeftHandTraffic { get; private set; }
        public int SegmentCount { get; private set; }
        public bool SupportedGeometry { get; private set; }
        public bool ExternalRulesKnown { get; private set; }
        public DataOrigin Origin { get; private set; }
        public LaneObservation[] Incoming { get { return (LaneObservation[])incoming.Clone(); } }
        public LaneObservation[] Outgoing { get { return (LaneObservation[])outgoing.Clone(); } }
        public string Fingerprint { get; private set; }

        public JunctionSnapshot(long cityEpoch, ushort nodeId, uint topologyRevision, bool leftHandTraffic,
            int segmentCount, bool supportedGeometry, bool externalRulesKnown, DataOrigin origin,
            LaneObservation[] incomingLanes, LaneObservation[] outgoingLanes) {
            CityEpoch = cityEpoch; NodeId = nodeId; TopologyRevision = topologyRevision;
            LeftHandTraffic = leftHandTraffic; SegmentCount = segmentCount;
            SupportedGeometry = supportedGeometry; ExternalRulesKnown = externalRulesKnown; Origin = origin;
            incoming = incomingLanes == null ? new LaneObservation[0] : (LaneObservation[])incomingLanes.Clone();
            outgoing = outgoingLanes == null ? new LaneObservation[0] : (LaneObservation[])outgoingLanes.Clone();
            StringBuilder text = new StringBuilder();
            text.Append(cityEpoch.ToString(CultureInfo.InvariantCulture)).Append('/').Append(nodeId)
                .Append('/').Append(topologyRevision).Append('/').Append(leftHandTraffic ? '1' : '0')
                .Append('/').Append(segmentCount).Append('/').Append(supportedGeometry ? '1' : '0')
                .Append('/').Append(externalRulesKnown ? '1' : '0').Append('/').Append((int)origin);
            AppendLanes(text, incoming); AppendLanes(text, outgoing);
            Fingerprint = text.ToString();
        }
        private static void AppendLanes(StringBuilder text, LaneObservation[] values) {
            text.Append('|');
            foreach (LaneObservation lane in values) {
                if (lane == null) text.Append("null;"); else lane.AppendIdentity(text);
            }
        }
    }

    public sealed class LaneConnection {
        private readonly uint[] outgoing;
        public uint IncomingLaneId { get; private set; }
        public uint[] OutgoingLaneIds { get { return (uint[])outgoing.Clone(); } }
        internal LaneConnection(uint incomingLaneId, uint[] outgoingLaneIds) {
            IncomingLaneId = incomingLaneId; outgoing = (uint[])outgoingLaneIds.Clone();
        }
    }
    public sealed class LaneProposal {
        private readonly LaneConnection[] connections;
        public ProposalStatus Status { get; private set; }
        public string Reason { get; private set; }
        public DataOrigin Origin { get; private set; }
        public string Fingerprint { get; private set; }
        public LaneConnection[] Connections { get { return (LaneConnection[])connections.Clone(); } }
        internal LaneProposal(ProposalStatus status, string reason, JunctionSnapshot snapshot, LaneConnection[] values) {
            Status = status; Reason = reason;
            Origin = snapshot == null ? DataOrigin.Unknown : snapshot.Origin;
            Fingerprint = snapshot == null ? "" : snapshot.Fingerprint;
            connections = (LaneConnection[])values.Clone();
        }
    }
}
