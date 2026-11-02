// Signature: OpenAI-Codex_Realistic_Railway_Traffic_Control
using System;
using RealisticRailwayTrafficControl.Observation;
using RealisticRailwayTrafficControl.Runtime;

internal static partial class Program
{
    private static void LifecycleStartsUnloaded()
    {
        RailwayModLifecycle lifecycle = new RailwayModLifecycle(delegate { }, delegate { });
        Assert(!lifecycle.IsLevelActive && !lifecycle.Faulted, "Constructing a shell cannot start city activity.");
        Assert(lifecycle.LastFailureStage == null && lifecycle.LastFailureType == null, "No inferred error before a callback.");
    }

    private static void LifecycleGameplayOnly()
    {
        int starts = 0;
        int stops = 0;
        RailwayModLifecycle lifecycle = new RailwayModLifecycle(delegate { starts++; }, delegate { stops++; });
        Assert(lifecycle.TryLoad(false), "Non-game modes should leave the shell inactive without failing.");
        Assert(starts == 0 && stops == 1 && !lifecycle.IsLevelActive, "Editors must not start a city observation lifecycle.");
        Assert(lifecycle.TryLoad(true) && lifecycle.IsLevelActive && starts == 1, "Gameplay may start only local bookkeeping.");
        Assert(lifecycle.TryUnload() && !lifecycle.IsLevelActive, "Unload must stop local bookkeeping.");
    }

    private static void LifecycleUnloadInvalidatesCapture()
    {
        RailwayObservationSession session = new RailwayObservationSession();
        RailwayModLifecycle lifecycle = new RailwayModLifecycle(
            delegate { if (!session.ActivateCity()) throw new InvalidOperationException(); }, session.Disable);
        lifecycle.TryLoad(true);
        CaptureTicket old = Begin(session);
        Assert(lifecycle.TryUnload(), "Unload should complete.");
        Assert(session.State == ObservationState.Disabled && session.CurrentSnapshot == null, "Unload clears only local capture state.");
        Assert(session.TryComplete(old, ObservationSnapshot(old)) == CaptureCompletion.Stale, "Unloaded callbacks cannot resurrect a city.");
    }

    private static void LifecycleLoadFailure()
    {
        int stops = 0;
        RailwayModLifecycle lifecycle = new RailwayModLifecycle(
            delegate { throw new InvalidOperationException("private diagnostic must not be retained"); }, delegate { stops++; });
        Assert(!lifecycle.TryLoad(true) && !lifecycle.IsLevelActive && lifecycle.Faulted, "Load exceptions must not escape or activate the shell.");
        Assert(stops == 2, "Failed start requires another cleanup after pre-load cleanup.");
        Assert(lifecycle.LastFailureStage == "Load" && lifecycle.LastFailureType == typeof(InvalidOperationException).FullName,
            "Retain stage and type only, without exception messages.");
        Assert(!lifecycle.CleanupFailed, "Successful cleanup must not be reported as failed.");
    }

    private static void LifecycleCleanupFailure()
    {
        int starts = 0;
        RailwayModLifecycle lifecycle = new RailwayModLifecycle(delegate { starts++; }, delegate { throw new ArgumentException(); });
        Assert(!lifecycle.TryLoad(true) && starts == 0, "A failed cleanup must prevent a new load.");
        Assert(lifecycle.Faulted && lifecycle.CleanupFailed && !lifecycle.IsLevelActive, "Cleanup failure leaves the shell inactive.");
        Assert(lifecycle.LastFailureStage == "BeforeLoad", "Identify the failing pre-load boundary.");
        Assert(!lifecycle.TryUnload(), "Unload exceptions must be contained as well.");
    }

    private static void LifecycleSecondaryFailure()
    {
        int stops = 0;
        RailwayModLifecycle lifecycle = new RailwayModLifecycle(delegate { throw new InvalidOperationException(); },
            delegate { stops++; if (stops > 1) throw new ArgumentException(); });
        Assert(!lifecycle.TryLoad(true), "A failing start and cleanup must return failure.");
        Assert(lifecycle.LastFailureStage == "Load" && lifecycle.LastFailureType == typeof(InvalidOperationException).FullName,
            "Cleanup cannot overwrite the first failure in the load cycle.");
        Assert(lifecycle.CleanupFailed && !lifecycle.IsLevelActive, "A secondary cleanup failure must remain visible.");
    }

    private static void LifecycleExternalFailure()
    {
        int stops = 0;
        RailwayModLifecycle lifecycle = new RailwayModLifecycle(delegate { }, delegate { stops++; });
        lifecycle.TryLoad(true);
        lifecycle.RecordFailure("OnCreated", new InvalidOperationException());
        Assert(lifecycle.Faulted && !lifecycle.IsLevelActive && stops == 2, "Extension callback failures must stop local activity.");
        Assert(lifecycle.LastFailureStage == "OnCreated", "Preserve the callback boundary.");
    }

    private static void LifecycleRepeatedCycles()
    {
        RailwayObservationSession session = new RailwayObservationSession();
        RailwayModLifecycle lifecycle = new RailwayModLifecycle(
            delegate { if (!session.ActivateCity()) throw new InvalidOperationException(); }, session.Disable);
        lifecycle.TryLoad(true);
        CaptureTicket old = Begin(session);
        Assert(lifecycle.TryLoad(true), "A new explicit load must clear the old local lifecycle first.");
        CaptureTicket current = Begin(session);
        Assert(current.CityEpoch > old.CityEpoch && session.TryComplete(old, ObservationSnapshot(old)) == CaptureCompletion.Stale,
            "Repeated loads must not accept an earlier city's callback.");
        Assert(lifecycle.TryUnload() && lifecycle.TryUnload(), "Repeated unload is safe for local state.");
        Assert(!lifecycle.IsLevelActive && session.State == ObservationState.Disabled, "No pending capture survives unload.");
    }
}
