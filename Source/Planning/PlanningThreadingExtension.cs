using ICities;
using AutomatedPublicTransportPlanning;

namespace AutomatedPublicTransportPlanning.Planning
{
    // OpenAI-Codex: drive route post-checks from simulation frames and publish localized status.
    /// <summary>
    /// Bridges the sliced capture to actual simulation frames. AddAction drains its
    /// queue in one step, so it cannot be used to spread a capture over frames.
    /// </summary>
    public sealed class PlanningThreadingExtension : ThreadingExtensionBase
    {
        public override void OnAfterSimulationFrame()
        {
            PlanningCapture.OnAfterSimulationFrame();
#if DEVTOOLS
            RouteCreationController.OnAfterSimulationFrame();
            RouteEditingController.OnAfterSimulationFrame(); // OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
#endif
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            Mod.UpdatePlanningStatus(PlanningCapture.DisplayText);
        }
    }
}

// Release preview bridge: OpenAI-Codex_aptp_release_boundary.
