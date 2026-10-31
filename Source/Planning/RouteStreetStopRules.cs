namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    internal static class RouteStreetStopRules
    {
        public static bool Supports(string netAiType, bool usable, bool resolved, bool fixedPlatform)
        {
            return netAiType == "RoadAI" && usable && resolved;
        }
    }
}
