// Signature: OpenAI-Codex_Realistic_Vehicle_Traffic_Control
using System;
using System.Text;

namespace RealisticVehicleTrafficControl.Model {
    // A module-local read boundary; not a shared Modpack provider contract.
    public interface IReadOnlyRoadNetworkSource {
        DataOrigin Origin { get; }
        bool IsOnSimulationThread { get; }
        long CityEpoch { get; }
        bool TryReadNode(ushort nodeId, out RoadNodeRecord node);
        bool TryReadSegment(ushort segmentId, out RoadSegmentRecord segment);
        bool TryReadLane(uint laneId, out RoadLaneRecord lane);
    }

    public sealed class RoadNodeRecord {
        private readonly ushort[] segments;
        public ushort NodeId { get; private set; }
        public ushort[] SegmentIds { get { return (ushort[])segments.Clone(); } }
        public RoadNodeRecord(ushort nodeId, ushort[] segmentIds) {
            NodeId = nodeId;
            segments = segmentIds == null ? new ushort[0] : (ushort[])segmentIds.Clone();
        }
    }
    public sealed class RoadLaneDefinition {
        public string LaneType { get; private set; }
        public string VehicleType { get; private set; }
        public string FinalDirection { get; private set; }
        public RoadLaneDefinition(string laneType, string vehicleType, string finalDirection) {
            LaneType = laneType; VehicleType = vehicleType; FinalDirection = finalDirection;
        }
    }
    public sealed class RoadSegmentRecord {
        private readonly RoadLaneDefinition[] definitions;
        public ushort SegmentId { get; private set; }
        public uint BuildIndex { get; private set; }
        public ushort StartNode { get; private set; }
        public ushort EndNode { get; private set; }
        public uint FirstLaneId { get; private set; }
        public bool Inverted { get; private set; }
        public RoadLaneDefinition[] Definitions { get { return (RoadLaneDefinition[])definitions.Clone(); } }
        public RoadSegmentRecord(ushort segmentId, uint buildIndex, ushort startNode, ushort endNode,
            uint firstLaneId, bool inverted, RoadLaneDefinition[] laneDefinitions) {
            SegmentId = segmentId; BuildIndex = buildIndex; StartNode = startNode; EndNode = endNode;
            FirstLaneId = firstLaneId; Inverted = inverted;
            definitions = laneDefinitions == null ? new RoadLaneDefinition[0] : (RoadLaneDefinition[])laneDefinitions.Clone();
        }
    }
    public sealed class RoadLaneRecord {
        public uint LaneId { get; private set; }
        public ushort SegmentId { get; private set; }
        public uint NextLaneId { get; private set; }
        public RoadLaneRecord(uint laneId, ushort segmentId, uint nextLaneId) {
            LaneId = laneId; SegmentId = segmentId; NextLaneId = nextLaneId;
        }
    }
    public sealed class ObservedRoadSegment {
        private readonly RoadLaneRecord[] lanes;
        public RoadSegmentRecord Segment { get; private set; }
        public RoadLaneRecord[] Lanes { get { return (RoadLaneRecord[])lanes.Clone(); } }
        internal ObservedRoadSegment(RoadSegmentRecord segment, RoadLaneRecord[] laneRecords) {
            Segment = segment; lanes = (RoadLaneRecord[])laneRecords.Clone();
        }
        public bool HasCompleteLaneMetadata {
            get {
                RoadLaneDefinition[] defs = Segment.Definitions;
                if (defs.Length != lanes.Length) return false;
                foreach (RoadLaneDefinition d in defs)
                    if (d == null || String.IsNullOrEmpty(d.LaneType) || String.IsNullOrEmpty(d.VehicleType)
                        || String.IsNullOrEmpty(d.FinalDirection)) return false;
                return true;
            }
        }
    }
    // Raw current-buffer facts only. It has no conversion to legal turn targets,
    // lane rank, phase state or a Ready planner snapshot.
    public sealed class RoadJunctionObservation {
        private readonly ObservedRoadSegment[] segments;
        public long CityEpoch { get; private set; }
        public DataOrigin Origin { get; private set; }
        public RoadNodeRecord Node { get; private set; }
        public ObservedRoadSegment[] Segments { get { return (ObservedRoadSegment[])segments.Clone(); } }
        public string Fingerprint { get; private set; }
        internal RoadJunctionObservation(long epoch, DataOrigin origin, RoadNodeRecord node, ObservedRoadSegment[] values) {
            CityEpoch = epoch; Origin = origin; Node = node; segments = (ObservedRoadSegment[])values.Clone();
            StringBuilder text = new StringBuilder();
            text.Append(epoch).Append('/').Append((int)origin).Append('/').Append(node.NodeId);
            foreach (ushort id in node.SegmentIds) text.Append('/').Append(id);
            foreach (ObservedRoadSegment observed in segments) {
                RoadSegmentRecord s = observed.Segment;
                text.Append('|').Append(s.SegmentId).Append('/').Append(s.BuildIndex).Append('/').Append(s.StartNode)
                    .Append('/').Append(s.EndNode).Append('/').Append(s.FirstLaneId).Append('/').Append(s.Inverted ? '1' : '0');
                foreach (RoadLaneRecord lane in observed.Lanes)
                    text.Append(';').Append(lane.LaneId).Append('/').Append(lane.SegmentId).Append('/').Append(lane.NextLaneId);
                text.Append('#');
                foreach (RoadLaneDefinition d in s.Definitions) {
                    if (d == null) text.Append("null;");
                    else { AppendText(text, d.LaneType); AppendText(text, d.VehicleType); AppendText(text, d.FinalDirection); }
                }
            }
            Fingerprint = text.ToString();
        }
        private static void AppendText(StringBuilder text, string value) {
            text.Append(value == null ? -1 : value.Length).Append(':').Append(value).Append(';');
        }
        public bool HasCompleteLaneMetadata {
            get {
                foreach (ObservedRoadSegment s in segments) if (!s.HasCompleteLaneMetadata) return false;
                return true;
            }
        }
    }
    public enum RoadCaptureStatus { Unknown, Unsupported, Invalidated, CapturedRaw }
    public sealed class RoadCaptureResult {
        public RoadCaptureStatus Status { get; private set; }
        public DataOrigin Origin { get; private set; }
        public string Reason { get; private set; }
        public RoadJunctionObservation Observation { get; private set; }
        internal RoadCaptureResult(RoadCaptureStatus status, DataOrigin origin, string reason, RoadJunctionObservation observation) {
            Status = status; Origin = origin; Reason = reason; Observation = observation;
        }
    }
}
