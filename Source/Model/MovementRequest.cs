// Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
namespace RealisticRailwayTrafficControl.Model
{
    public sealed class MovementRequest
    {
        private readonly string[] resources;
        public string RequestId { get; private set; }
        public string VehicleKey { get; private set; }
        public long CityEpoch { get; private set; }
        public long CaptureSerial { get; private set; }
        public string TopologyToken { get; private set; }
        public string[] RequiredResourceKeys { get { return ResourceEvidence.Copy(resources); } }
        public MovementRequest(string id, string vehicle, long epoch, long serial, string topology, string[] resources)
        {
            RequestId = id; VehicleKey = vehicle; CityEpoch = epoch; CaptureSerial = serial;
            TopologyToken = topology; this.resources = ResourceEvidence.Copy(resources);
        }
    }
}
