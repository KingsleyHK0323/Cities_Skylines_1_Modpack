using System.Globalization;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Automated_Public_Transport_Planning
    internal static class RouteStopPreview
    {
        // Describe the captured identity only; this is not shared-stop permission or live validity.
        public static string Describe(ushort segmentId, uint buildIndex, bool forward, int laneIndex)
        {
            return segmentId.ToString(CultureInfo.InvariantCulture) + (forward ? "+" : "-") +
                " [lane " + laneIndex.ToString(CultureInfo.InvariantCulture) +
                "; build " + buildIndex.ToString(CultureInfo.InvariantCulture) + "]";
        }
    }
}
