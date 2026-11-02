// Signature: OpenAI-Codex_Realistic_Urban_Population
using System;
using RealisticUrbanPopulation.Demand;
using RealisticUrbanPopulation.Runtime;

internal static class LifecycleChecks
{
    private static int assertions;
    public static int AssertionCount { get { return assertions; } }
    private static void Assert(bool condition, string message)
    { assertions++; if (!condition) throw new Exception(message); }

    public static void InactiveLifecycle()
    {
        PopulationLifecycle lifecycle = new PopulationLifecycle();
        Assert(lifecycle.BeginLevel(false) == 0, "Non-gameplay does not start");
        long a = lifecycle.BeginLevel(true);
        Assert(a > 0 && !lifecycle.ReadState().Loaded && !lifecycle.ReadState().Armed,
            "Gameplay creates only an unready inactive model");
        Assert(lifecycle.ReadState().Permission == ControlPermission.Unknown, "No control permission granted");
        Assert(lifecycle.ReadState().Settings.Residential.Mode == DemandMode.Native &&
            lifecycle.ReadState().Settings.Commercial.Mode == DemandMode.Native &&
            lifecycle.ReadState().Settings.Workplace.Mode == DemandMode.Native, "All categories Native");
        long b = lifecycle.BeginLevel(true);
        Assert(b > a, "Repeated load replaces own epoch");
        Assert(!lifecycle.EndLevel(a), "Stale closure cannot close new model");
        Assert(lifecycle.EndLevel(b) && !lifecycle.EndLevel(b), "Unload is idempotent");
        Assert(!lifecycle.ReadState().Loaded && !lifecycle.ReadState().Armed, "Unloaded inactive");
        lifecycle.BeginLevel(true);
        Assert(lifecycle.BeginLevel(false) == 0 && !lifecycle.ReadState().Armed, "Non-gameplay clears previous model");
    }

    public static void CallbackErrorBoundary()
    {
        LifecycleBoundary boundary = new LifecycleBoundary();
        Assert(!boundary.TryRun(null) && boundary.LastError is ArgumentNullException, "Null callback rejected");
        InvalidOperationException expected = new InvalidOperationException("Synthetic lifecycle failure");
        Assert(!boundary.TryRun(delegate { throw expected; }) && object.ReferenceEquals(expected, boundary.LastError),
            "Callback exception retained instead of escaping");
        bool called = false;
        Assert(boundary.TryRun(delegate { called = true; }) && called && boundary.LastError == null,
            "Next successful callback clears prior error");
    }
}
