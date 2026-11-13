// Signature: OpenAI-Codex_native_validation_options
// Shared assertions for a detached host and its deliberately broken negative control.
using System;

internal static class HostBehaviorOracle
{
    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Controlled host: " + message);
    }

    internal static void NoPreArmReads(SimulationManager simulation, LoadingManager loading,
        Func<bool> before, Func<bool> after, Func<bool> display)
    {
        int simulationReads = simulation.ReadCount;
        int loadingReads = loading.ReadCount;
        bool acceptedBefore = before();
        bool acceptedAfter = after();
        bool acceptedDisplay = display();
        Check(simulation.ReadCount == simulationReads && loading.ReadCount == loadingReads,
            "host read a game field before manual start");
        Check(!acceptedBefore && !acceptedAfter && !acceptedDisplay,
            "host accepted a callback before manual start");
    }
}
