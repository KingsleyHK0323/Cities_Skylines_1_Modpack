using System;
using AutomatedPublicTransportPlanning.Planning;
// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
internal static class RouteStreetStopChecks
{
    public static void Run()
    {
        // Signature: OpenAI-Codex_Automated_Public_Transport_Planning
        foreach (bool placementArgument in new[] { false, true })
        {
            if (!RouteStreetStopRules.Supports("RoadAI", true, true, placementArgument))
            { throw new Exception("Ordinary resolved RoadAI must accept either placement argument."); }
            foreach (string ai in new[] { "RoadBridgeAI", "RoadTunnelAI", "Other.RoadAI", "RoadAIChild", "", null })
            {
                if (RouteStreetStopRules.Supports(ai, true, true, placementArgument))
                { throw new Exception("Unsupported road AI admitted: " + ai); }
            }
            foreach (bool usable in new[] { false, true })
            {
                foreach (bool resolved in new[] { false, true })
                {
                    if (!usable || !resolved)
                    {
                        if (RouteStreetStopRules.Supports("RoadAI", usable, resolved, placementArgument))
                        { throw new Exception("Eligibility or resolution failure must refuse both placement arguments."); }
                    }
                }
            }
        }
    }
}
