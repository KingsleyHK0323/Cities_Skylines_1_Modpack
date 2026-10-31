// Signature: OpenAI-Codex_Responsible: Realistic_Vehicle_Traffic_Control
using System;
using System.Collections.Generic;

namespace RealisticVehicleTrafficControl.Model {
    public sealed class LaneProposalPlanner {
        private static LaneProposal Refuse(JunctionSnapshot s, string reason) {
            return new LaneProposal(ProposalStatus.Unsupported, reason, s, new LaneConnection[0]);
        }
        public LaneProposal Build(JunctionSnapshot snapshot) {
            if (snapshot == null || snapshot.Origin == DataOrigin.Unknown)
                return new LaneProposal(ProposalStatus.Unknown, "Data source is unknown; no suggestion was computed.", snapshot, new LaneConnection[0]);
            if (!Enum.IsDefined(typeof(DataOrigin), snapshot.Origin)) return Refuse(snapshot, "Unrecognized source.");
            if (snapshot.CityEpoch <= 0 || snapshot.NodeId == 0) return Refuse(snapshot, "Missing capture identity.");
            if (!snapshot.SupportedGeometry || !snapshot.ExternalRulesKnown)
                return Refuse(snapshot, "Geometry or external rules are unresolved.");
            if (snapshot.SegmentCount != 3 && snapshot.SegmentCount != 4)
                return Refuse(snapshot, "Only three- or four-segment nodes are supported.");
            LaneObservation[] observedIn = snapshot.Incoming, observedOut = snapshot.Outgoing;
            if (observedIn.Length + observedOut.Length > 32) return Refuse(snapshot, "Capture exceeds the lane bound.");
            List<LaneObservation> incoming = new List<LaneObservation>(), outgoing = new List<LaneObservation>();
            Dictionary<uint, bool> identities = new Dictionary<uint, bool>();
            Dictionary<ushort, uint> segmentGenerations = new Dictionary<ushort, uint>();
            foreach (LaneObservation[] side in new LaneObservation[][] { observedIn, observedOut }) {
                foreach (LaneObservation lane in side) {
                    if (lane == null || lane.LaneId == 0 || lane.SegmentId == 0 || identities.ContainsKey(lane.LaneId))
                        return Refuse(snapshot, "Missing or duplicate lane identity.");
                    identities.Add(lane.LaneId, true);
                    uint generation;
                    if (segmentGenerations.TryGetValue(lane.SegmentId, out generation)) {
                        if (generation != lane.SegmentBuildIndex)
                            return Refuse(snapshot, "Capture mixes generations of the same segment.");
                    } else {
                        segmentGenerations.Add(lane.SegmentId, lane.SegmentBuildIndex);
                        if (segmentGenerations.Count > snapshot.SegmentCount)
                            return Refuse(snapshot, "Observed segments exceed the declared node topology.");
                    }
                    if (lane.Kind == LaneKind.Sidewalk) continue;
                    if (lane.Kind != LaneKind.GeneralCar) return Refuse(snapshot, "Mixed or unknown lane type.");
                    if (lane.Turn != TurnKind.Left && lane.Turn != TurnKind.Forward && lane.Turn != TurnKind.Right)
                        return Refuse(snapshot, "Unsupported movement direction.");
                    (Object.ReferenceEquals(side, observedIn) ? incoming : outgoing).Add(lane);
                }
            }
            if (incoming.Count == 0 || outgoing.Count == 0) return Refuse(snapshot, "No general-car approach or exit.");
            if (incoming.Count > 8) return Refuse(snapshot, "Approach exceeds eight car lanes.");
            ushort approach = incoming[0].SegmentId;
            foreach (LaneObservation lane in incoming)
                if (lane.SegmentId != approach) return Refuse(snapshot, "Select exactly one approach segment.");
            int[] turns = new int[3];
            foreach (LaneObservation lane in outgoing) {
                if (lane.SegmentId == approach) return Refuse(snapshot, "Return to the approach segment is unsupported.");
                if (++turns[(int)lane.Turn] > 8) return Refuse(snapshot, "Exit direction exceeds eight car lanes.");
            }
            incoming.Sort(delegate(LaneObservation a, LaneObservation b) { return Compare(a, b, snapshot.LeftHandTraffic, false); });
            outgoing.Sort(delegate(LaneObservation a, LaneObservation b) { return Compare(a, b, snapshot.LeftHandTraffic, true); });
            for (int i = 1; i < incoming.Count; i++)
                if (incoming[i].Order(snapshot.LeftHandTraffic) == incoming[i - 1].Order(snapshot.LeftHandTraffic))
                    return Refuse(snapshot, "Approach lane order is ambiguous.");
            for (int i = 1; i < outgoing.Count; i++)
                if (outgoing[i].Turn == outgoing[i - 1].Turn && outgoing[i].Order(snapshot.LeftHandTraffic) == outgoing[i - 1].Order(snapshot.LeftHandTraffic))
                    return Refuse(snapshot, "Exit lane order is ambiguous.");
            List<LaneConnection> result = new List<LaneConnection>();
            int offset = 0;
            for (int i = 0; i < incoming.Count; i++) {
                int start, count;
                if (outgoing.Count >= incoming.Count) {
                    count = outgoing.Count / incoming.Count + (i < outgoing.Count % incoming.Count ? 1 : 0);
                    start = offset; offset += count;
                } else {
                    start = i * outgoing.Count / incoming.Count; count = 1;
                }
                uint[] ids = new uint[count];
                uint[] allowed = incoming[i].AllowedTargets;
                for (int j = 0; j < count; j++) {
                    ids[j] = outgoing[start + j].LaneId;
                    if (Array.IndexOf(allowed, ids[j]) < 0) return Refuse(snapshot, "A proposed movement lacks explicit legality evidence.");
                }
                result.Add(new LaneConnection(incoming[i].LaneId, ids));
            }
            return new LaneProposal(ProposalStatus.Ready, "Preview only; no game state is written.", snapshot, result.ToArray());
        }
        private static int Compare(LaneObservation a, LaneObservation b, bool left, bool byTurn) {
            int turn = byTurn ? a.TurnOrder(left).CompareTo(b.TurnOrder(left)) : 0;
            if (turn != 0) return turn;
            int rank = a.Order(left).CompareTo(b.Order(left));
            return rank != 0 ? rank : a.LaneId.CompareTo(b.LaneId);
        }
    }
}
