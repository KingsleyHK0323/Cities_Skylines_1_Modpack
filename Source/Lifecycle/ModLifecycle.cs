// Signature: OpenAI-Codex_Realistic_Vehicle_Traffic_Control
using System;

namespace RealisticVehicleTrafficControl.Model {
    // Failure isolation for this disabled foundation only. No background work,
    // Harmony patches, game buffer readers, UI subscriptions or traffic writers.
    public sealed class ModLifecycle {
        private readonly PreviewLifecycle previews = new PreviewLifecycle(0);
        private bool hasSession;
        public bool IsLoaded { get; private set; }
        public long SessionEpoch { get; private set; }
        public string LastFailure { get; private set; }

        public bool Load(Action initialize) {
            if (hasSession) return IsLoaded;
            if (SessionEpoch == Int64.MaxValue) { LastFailure = "Load:EpochExhausted"; return false; }
            hasSession = true; IsLoaded = false;
            SessionEpoch++; previews.Invalidate(SessionEpoch); LastFailure = null;
            try {
                if (initialize != null) initialize();
                IsLoaded = true; return true;
            } catch (Exception error) {
                LastFailure = "Load:" + error.GetType().Name;
                return false;
            }
        }
        public bool Unload(Action cleanup) {
            if (!hasSession) return true;
            hasSession = false; IsLoaded = false;
            if (SessionEpoch < Int64.MaxValue) SessionEpoch++;
            previews.Invalidate(SessionEpoch); LastFailure = null;
            try {
                if (cleanup != null) cleanup();
                return true;
            } catch (Exception error) {
                LastFailure = "Unload:" + error.GetType().Name;
                return false;
            }
        }
        public CaptureToken BeginPreview(ushort nodeId, uint topologyRevision) {
            return IsLoaded ? previews.Begin(nodeId, topologyRevision) : null;
        }
        public bool AcceptPreview(CaptureToken token, JunctionSnapshot snapshot) {
            return IsLoaded && previews.Accept(token, snapshot);
        }
    }
}
