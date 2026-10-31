using System;
using AutomatedPublicTransportPlanning.Planning;
// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
internal static class RouteStreetStopChecks
{
    public static void Run()
    {
        if (!RouteStreetStopRules.Supports("RoadAI", true, true, true)) { throw new Exception("Ordinary resolved RoadAI stop must not be rejected by its placement API flag."); }
        foreach (string ai in new[] { "RoadBridgeAI", "RoadTunnelAI", "Other.RoadAI", "", null })
        { if (RouteStreetStopRules.Supports(ai, true, true, true)) { throw new Exception("Unsupported road AI admitted: " + ai); } }
        if (RouteStreetStopRules.Supports("RoadAI", false, true, true) || RouteStreetStopRules.Supports("RoadAI", true, false, true)) { throw new Exception("Unusable or unresolved stop admitted."); }
    }
}
