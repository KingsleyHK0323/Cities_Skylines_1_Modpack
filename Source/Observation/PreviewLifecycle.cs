// Signature: OpenAI-Codex_Responsible: Realistic_Vehicle_Traffic_Control
namespace RealisticVehicleTrafficControl.Model {
    // In-memory callback identity only. No game, path or signal ownership API.
    public sealed class CaptureToken {
        internal readonly long Epoch;
        internal readonly ushort Node;
        internal readonly uint Revision;
        internal CaptureToken(long epoch, ushort node, uint revision) { Epoch = epoch; Node = node; Revision = revision; }
    }
    public sealed class PreviewLifecycle {
        private long epoch;
        private CaptureToken pending;
        private JunctionSnapshot accepted;
        public PreviewLifecycle(long cityEpoch) { epoch = cityEpoch; }
        public CaptureToken Begin(ushort nodeId, uint topologyRevision) {
            accepted = null;
            pending = new CaptureToken(epoch, nodeId, topologyRevision);
            return pending;
        }
        public bool Accept(CaptureToken token, JunctionSnapshot snapshot) {
            if (token == null || !object.ReferenceEquals(token, pending) || snapshot == null
                || snapshot.Origin == DataOrigin.Unknown || token.Epoch != epoch || snapshot.CityEpoch != epoch
                || snapshot.NodeId != token.Node || snapshot.TopologyRevision != token.Revision) return false;
            accepted = snapshot;
            pending = null;
            return true;
        }
        public void Invalidate(long newCityEpoch) { epoch = newCityEpoch; pending = null; accepted = null; }
        public bool IsCurrent(JunctionSnapshot freshObservation) {
            return accepted != null && freshObservation != null && freshObservation.CityEpoch == epoch
                && accepted.Fingerprint == freshObservation.Fingerprint;
        }
    }
}
