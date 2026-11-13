// Signature: OpenAI-Codex_native_validation_options
// Intentionally defective fake host; this is not a claim about the game or product host.
using System;

internal static class HostNegativeControlChecks
{
    private sealed class ReadsBeforeStart
    {
        private readonly SimulationManager simulation;
        internal ReadsBeforeStart(SimulationManager simulation) { this.simulation = simulation; }
        internal bool BeforeFrame()
        {
            uint ignored = simulation.m_currentFrameIndex;
            return ignored == UInt32.MaxValue;
        }
        internal bool AfterFrame() { return false; }
        internal bool DisplayUpdate() { return false; }
    }

    private static int Main()
    {
        try
        {
            SimulationManager simulation = new SimulationManager();
            LoadingManager loading = new LoadingManager();
            ReadsBeforeStart broken = new ReadsBeforeStart(simulation);
            HostBehaviorOracle.NoPreArmReads(simulation, loading,
                broken.BeforeFrame, broken.AfterFrame, broken.DisplayUpdate);
            Console.WriteLine("ERROR negative control unexpectedly passed");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }
}
