// Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
using System;

namespace RealisticRailwayTrafficControl.Model
{
    public enum EvidenceState { Unknown, Clear, Occupied }
    public enum NetworkSupport { Unknown, SupportedDedicatedRail, Unsupported }

    public sealed class VehicleEvidence
    {
        public string VehicleKey { get; private set; }
        public bool IdentityVerified { get; private set; }
        public bool TailVerified { get; private set; }
        public VehicleEvidence(string key, bool identityVerified, bool tailVerified)
        { VehicleKey = key; IdentityVerified = identityVerified; TailVerified = tailVerified; }
    }

    public sealed class ResourceEvidence
    {
        private readonly string[] conflicts;
        private readonly string[] occupants;
        public string ResourceKey { get; private set; }
        public EvidenceState State { get; private set; }
        public bool ConflictsComplete { get; private set; }
        public string[] ConflictKeys { get { return Copy(conflicts); } }
        public string[] OccupantKeys { get { return Copy(occupants); } }
        public ResourceEvidence(string key, EvidenceState state, string[] conflicts,
            bool conflictsComplete, string[] occupants)
        {
            ResourceKey = key; State = state; ConflictsComplete = conflictsComplete;
            this.conflicts = Copy(conflicts); this.occupants = Copy(occupants);
        }
        internal static string[] Copy(string[] values)
        { return values == null ? null : (string[])values.Clone(); }
    }

    public sealed class RailwaySnapshot
    {
        private readonly ResourceEvidence[] resources;
        private readonly VehicleEvidence[] occupants;
        public long CityEpoch { get; private set; }
        public long CaptureSerial { get; private set; }
        public string TopologyToken { get; private set; }
        public long CapturedFrame { get; private set; }
        public int SegmentCount { get; private set; }
        public bool Complete { get; private set; }
        public NetworkSupport Network { get; private set; }
        public ResourceEvidence[] Resources
        { get { return resources == null ? null : (ResourceEvidence[])resources.Clone(); } }
        public VehicleEvidence[] Occupants
        { get { return occupants == null ? null : (VehicleEvidence[])occupants.Clone(); } }
        public RailwaySnapshot(long epoch, long serial, string topology, long frame,
            int segmentCount, bool complete, NetworkSupport network,
            ResourceEvidence[] resources, VehicleEvidence[] occupants)
        {
            CityEpoch = epoch; CaptureSerial = serial; TopologyToken = topology;
            CapturedFrame = frame; SegmentCount = segmentCount; Complete = complete; Network = network;
            this.resources = resources == null ? null : (ResourceEvidence[])resources.Clone();
            this.occupants = occupants == null ? null : (VehicleEvidence[])occupants.Clone();
        }
    }
}
