// Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
namespace RealisticRailwayTrafficControl.Model
{
    public sealed class ValidationReport
    {
        public bool IsValid { get; private set; }
        public ShadowOutcome FailureOutcome { get; private set; }
        public ShadowReasonCode ReasonCode { get; private set; }
        internal ValidationReport(bool valid, ShadowOutcome outcome, ShadowReasonCode reason)
        { IsValid = valid; FailureOutcome = outcome; ReasonCode = reason; }
    }
}
