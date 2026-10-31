// Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
namespace RealisticRailwayTrafficControl.Model
{
    public static class RailwayShadowAnalyzer
    {
        public static ShadowDecision[] Evaluate(RailwaySnapshot snapshot, MovementRequest[] requests)
        {
            if (requests == null) return new ShadowDecision[0];
            ShadowDecision[] result = new ShadowDecision[requests.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = new ShadowDecision(requests[i] == null ? null : requests[i].RequestId,
                    ShadowOutcome.Unknown, ShadowReasonCode.InvalidRequest, new string[0]);
            return result;
        }
    }
}
