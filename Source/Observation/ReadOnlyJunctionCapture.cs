// Signature: OpenAI-Codex_Realistic_Vehicle_Traffic_Control
using System;
using System.Collections.Generic;

namespace RealisticVehicleTrafficControl.Model {
    public sealed class ReadOnlyJunctionCapture {
        private static RoadCaptureResult Failure(RoadCaptureStatus status, DataOrigin origin, string reason) {
            return new RoadCaptureResult(status, origin, reason, null);
        }
        public RoadCaptureResult Capture(IReadOnlyRoadNetworkSource source, long expectedEpoch, ushort nodeId) {
            DataOrigin origin = DataOrigin.Unknown;
            try {
                if (source == null) return Failure(RoadCaptureStatus.Unknown, origin, "No source.");
                origin = source.Origin;
                if (origin != DataOrigin.SyntheticFixture && origin != DataOrigin.GameCapture)
                    return Failure(RoadCaptureStatus.Unknown, DataOrigin.Unknown, "Unknown source provenance.");
                if (!source.IsOnSimulationThread) return Failure(RoadCaptureStatus.Unsupported, origin, "Simulation-thread capture required.");
                if (expectedEpoch <= 0 || source.CityEpoch != expectedEpoch)
                    return Failure(RoadCaptureStatus.Invalidated, origin, "City session changed or is unknown.");
                if (nodeId == 0) return Failure(RoadCaptureStatus.Unsupported, origin, "No selected node.");
                RoadCaptureResult first = ReadPass(source, expectedEpoch, nodeId, origin);
                if (first.Status != RoadCaptureStatus.CapturedRaw) return first;
                RoadCaptureResult second = ReadPass(source, expectedEpoch, nodeId, origin);
                if (!StableContext(source, expectedEpoch, origin))
                    return Failure(RoadCaptureStatus.Invalidated, origin, "Capture context changed.");
                if (second.Status != RoadCaptureStatus.CapturedRaw
                    || first.Observation.Fingerprint != second.Observation.Fingerprint)
                    return Failure(RoadCaptureStatus.Invalidated, origin, "Node, segment or lane data changed during capture.");
                return second;
            } catch (Exception) {
                // The reader is a failure boundary. Do not leak exception text,
                // retain a partial capture or treat a read failure as empty data.
                return Failure(RoadCaptureStatus.Unknown, origin, "Source read failed; no observation retained.");
            }
        }
        private static bool StableContext(IReadOnlyRoadNetworkSource source, long epoch, DataOrigin origin) {
            return source.IsOnSimulationThread && source.CityEpoch == epoch && source.Origin == origin;
        }
        private static RoadCaptureResult ReadPass(IReadOnlyRoadNetworkSource source, long epoch, ushort nodeId, DataOrigin origin) {
            RoadNodeRecord node;
            if (!StableContext(source, epoch, origin)) return Failure(RoadCaptureStatus.Invalidated, origin, "Capture context changed.");
            if (!source.TryReadNode(nodeId, out node) || node == null)
                return Failure(RoadCaptureStatus.Unknown, origin, "Node unavailable.");
            if (node.NodeId != nodeId) return Failure(RoadCaptureStatus.Unsupported, origin, "Node identity mismatch.");
            ushort[] ids = node.SegmentIds;
            if (ids.Length > 8) return Failure(RoadCaptureStatus.Unsupported, origin, "Node exceeds eight segment slots.");
            Dictionary<ushort, bool> seenSegments = new Dictionary<ushort, bool>();
            Dictionary<uint, bool> seenLanes = new Dictionary<uint, bool>();
            List<ObservedRoadSegment> rows = new List<ObservedRoadSegment>();
            foreach (ushort id in ids) {
                if (id == 0) continue;
                if (!StableContext(source, epoch, origin)) return Failure(RoadCaptureStatus.Invalidated, origin, "Capture context changed.");
                if (seenSegments.ContainsKey(id)) return Failure(RoadCaptureStatus.Unsupported, origin, "Duplicate segment slot.");
                seenSegments.Add(id, true);
                RoadSegmentRecord segment;
                if (!source.TryReadSegment(id, out segment) || segment == null)
                    return Failure(RoadCaptureStatus.Unknown, origin, "Segment unavailable.");
                if (segment.SegmentId != id || (segment.StartNode != nodeId && segment.EndNode != nodeId))
                    return Failure(RoadCaptureStatus.Unsupported, origin, "Segment is not attached to selected node.");
                if (segment.Definitions.Length > 64)
                    return Failure(RoadCaptureStatus.Unsupported, origin, "Lane definition bound exceeded.");
                List<RoadLaneRecord> lanes = new List<RoadLaneRecord>();
                uint next = segment.FirstLaneId;
                while (next != 0) {
                    if (!StableContext(source, epoch, origin)) return Failure(RoadCaptureStatus.Invalidated, origin, "Capture context changed.");
                    if (lanes.Count >= 64 || seenLanes.Count >= 256 || seenLanes.ContainsKey(next))
                        return Failure(RoadCaptureStatus.Unsupported, origin, "Lane chain is cyclic, shared or exceeds capture bounds.");
                    seenLanes.Add(next, true);
                    RoadLaneRecord lane;
                    if (!source.TryReadLane(next, out lane) || lane == null)
                        return Failure(RoadCaptureStatus.Unknown, origin, "Lane unavailable.");
                    if (lane.LaneId != next || lane.SegmentId != id)
                        return Failure(RoadCaptureStatus.Unsupported, origin, "Lane identity or segment membership mismatch.");
                    lanes.Add(lane); next = lane.NextLaneId;
                }
                rows.Add(new ObservedRoadSegment(segment, lanes.ToArray()));
            }
            if (!StableContext(source, epoch, origin)) return Failure(RoadCaptureStatus.Invalidated, origin, "Capture context changed.");
            if (rows.Count == 0) return Failure(RoadCaptureStatus.Unsupported, origin, "No attached segments observed.");
            return new RoadCaptureResult(RoadCaptureStatus.CapturedRaw, origin,
                "Current raw buffer observation only; movement legality and external rules remain unresolved.",
                new RoadJunctionObservation(epoch, origin, node, rows.ToArray()));
        }
    }
}
