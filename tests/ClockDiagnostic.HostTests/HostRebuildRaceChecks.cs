// Signature: OpenAI-Codex_native_validation_options
// Isolated checks for status delivery and options-page replacement during start.
using System;
using System.Threading;
using RealisticCityTime.Diagnostics;

internal static class HostRebuildRaceChecks
{
    private sealed class Source : IExistingManagerSource
    {
        internal readonly SimulationManager Simulation = new SimulationManager();
        internal readonly LoadingManager Loading = new LoadingManager();
        internal readonly ManualResetEvent Entered = new ManualResetEvent(false);
        internal readonly ManualResetEvent Continue = new ManualResetEvent(false);
        internal bool Block;
        internal Action DuringLookup;

        internal Source()
        {
            Loading.m_currentlyLoading = false;
            Loading.m_loadingComplete = true;
            Simulation.m_currentFrameIndex = 20;
            Simulation.m_dayTimeFrame = 30;
            Simulation.m_timePerFrame = TimeSpan.FromTicks(500);
            Simulation.m_referenceFrameIndex = 20;
            Simulation.m_referenceTimer = 0.25f;
            Simulation.m_currentDayTimeHour = 6f;
        }

        public bool TryGetExisting(out SimulationManager simulation, out LoadingManager loading)
        {
            Action action = DuringLookup;
            DuringLookup = null;
            if (action != null) action();
            if (Block)
            {
                Entered.Set();
                if (!Continue.WaitOne(10000)) throw new InvalidOperationException("Synthetic manager lookup timed out.");
            }
            simulation = Simulation;
            loading = Loading;
            return true;
        }
    }

    private static void Check(bool value, string message) { HostBehaviorOracle.Check(value, message); }

    private static void RebuildMustNotPublishNotStartedWhileRecording()
    {
        Source source = new Source();
        ControlledReadOnlyHost host = new ControlledReadOnlyHost(source);
        host.OnLevelLoaded(true);
        HostUiFake first = new HostUiFake();
        Check(new ControlledDiagnosticUi(host).Show(first), "old page did not initialize");
        Check(first.Click("Start diagnostic") && host.State.ToString() == "Recording", "old page did not start");
        HostUiFake next = new HostUiFake();
        Check(new ControlledDiagnosticUi(host).Show(next), "replacement page did not initialize");
        Check(next.Fields.Count == 2, "replacement page did not produce two fake fields");
        foreach (string text in next.Fields[0].HostTextCopies)
            Check(text == null || text.IndexOf("not started", StringComparison.OrdinalIgnoreCase) < 0,
                "rebuilt page published not-started status during recording");
        Check(next.Click("Stop diagnostic") && host.State.ToString() == "Stopped", "replacement page could not stop");
    }

    private static void FailedRebuildCannotLeaveInFlightStartRecording()
    {
        Source source = new Source();
        ControlledReadOnlyHost host = new ControlledReadOnlyHost(source);
        host.OnLevelLoaded(true);
        HostUiFake oldPage = new HostUiFake();
        Check(new ControlledDiagnosticUi(host).Show(oldPage), "old page setup failed");
        bool replacementAttempted = false;
        bool replacementShown = true;
        source.DuringLookup = delegate
        {
            replacementAttempted = true;
            HostUiFake broken = new HostUiFake();
            broken.NullFieldAt = 2;
            replacementShown = new ControlledDiagnosticUi(host).Show(broken);
        };
        Check(oldPage.Click("Start diagnostic") && replacementAttempted && !replacementShown,
            "reentrant failed replacement was not exercised");
        Check(host.State.ToString() != "Recording",
            "in-flight start became recording after replacement failed");
        int reads = source.Simulation.ReadCount;
        int loadingReads = source.Loading.ReadCount;
        Check(!host.BeforeFrame() && !host.AfterFrame() && !host.DisplayUpdate(),
            "failed replacement accepted later callbacks");
        Check(source.Simulation.ReadCount == reads && source.Loading.ReadCount == loadingReads,
            "failed replacement allowed later game field reads");
    }

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] == "recording-status") RebuildMustNotPublishNotStartedWhileRecording();
            if (args.Length == 0 || args[0] == "starting-failure") FailedRebuildCannotLeaveInFlightStartRecording();
            Console.WriteLine("PASS controlled host page-rebuild fake checks");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
