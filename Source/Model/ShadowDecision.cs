// Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
namespace RealisticRailwayTrafficControl.Model
{
    public enum ShadowOutcome { Unknown, WouldReserve, Conflict, Unsupported }
    public enum ShadowReasonCode
    {
        None, IncompleteCapture, InvalidIdentity, StaleCapture, InvalidTopology,
        InvalidRequest, UnsupportedNetwork, OccupiedResource, ReservedConflict
    }
    public sealed class ShadowDecision
    {
        private readonly string[] resources;
        public string RequestId { get; private set; }
        public ShadowOutcome Outcome { get; private set; }
        public ShadowReasonCode ReasonCode { get; private set; }
        public string[] ResourceKeys { get { return ResourceEvidence.Copy(resources); } }
        internal ShadowDecision(string id, ShadowOutcome outcome, ShadowReasonCode reason, string[] resources)
        { RequestId = id; Outcome = outcome; ReasonCode = reason; this.resources = ResourceEvidence.Copy(resources); }
    }
}
