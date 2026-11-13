// Signature: OpenAI-Codex_native_validation_options
// Deterministic page replacement checks; host UI barriers exist only in a reversible test copy.
using System;
using System.Threading;
using RealisticCityTime.Diagnostics;

internal static class HostPageRaceChecks
{
    private sealed class Source : IExistingManagerSource
    {
        internal readonly SimulationManager Simulation = new SimulationManager();
        internal readonly LoadingManager Loading = new LoadingManager();
        internal int Calls;
        internal Action DuringLookup;

        internal Source()
        {
            Loading.m_currentlyLoading = false;
            Loading.m_loadingComplete = true;
            Simulation.m_currentFrameIndex = 100;
            Simulation.m_dayTimeFrame = 200;
            Simulation.m_timePerFrame = TimeSpan.FromTicks(500);
            Simulation.m_referenceFrameIndex = 100;
            Simulation.m_referenceTimer = 0.25f;
            Simulation.m_currentDayTimeHour = 6f;
        }

        public bool TryGetExisting(out SimulationManager simulation, out LoadingManager loading)
        {
            Calls++;
            Action action = DuringLookup;
            DuringLookup = null;
            if (action != null) action();
            simulation = Simulation;
            loading = Loading;
            return true;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Controlled page race: " + message);
    }

    private static bool Contains(string text, string fragment)
    {
        return text != null && text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static ControlledReadOnlyHost Ready(Source source)
    {
        ControlledReadOnlyHost host = new ControlledReadOnlyHost(source);
        host.OnLevelLoaded(true);
        Check(host.State.ToString() == "Ready", "fake host did not enter ready state");
        return host;
    }

    private static HostUiFake Show(ControlledReadOnlyHost host)
    {
        HostUiFake helper = new HostUiFake();
        Check(new ControlledDiagnosticUi(host).Show(helper) && helper.Fields.Count == 2,
            "fake options page did not initialize");
        return helper;
    }

    private static void CaptureOne(ControlledReadOnlyHost host, Source source)
    {
        Check(host.BeforeFrame(), "fake before-frame was not accepted");
        source.Simulation.m_currentFrameIndex = 101;
        source.Simulation.m_dayTimeFrame = 201;
        Check(host.AfterFrame(), "fake after-frame was not accepted");
    }

    private static void PrepareStopped(out ControlledReadOnlyHost host, out Source source, out HostUiFake page)
    {
        source = new Source();
        host = Ready(source);
        page = Show(host);
        Check(page.Click("Start diagnostic") && host.State.ToString() == "Recording", "fake session did not start");
        CaptureOne(host, source);
        Check(page.Click("Stop diagnostic") && host.State.ToString() == "Stopped", "fake session did not stop");
    }

    private static void VerifyAfterStartBarrier()
    {
        Source source = new Source();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake page = Show(host);
        bool reached = false;
        HostPageRaceBarriers.AfterStartCall = delegate(bool started) { reached = started; };
        Check(page.Click("Start diagnostic") && reached, "after-start barrier was not instrumented");
        HostPageRaceBarriers.AfterStartCall = null;
        Check(page.Click("Stop diagnostic") && host.State.ToString() == "Stopped", "after-start preflight did not stop");
    }

    private static void VerifyAfterRefreshBarrier()
    {
        ControlledReadOnlyHost host;
        Source source;
        HostUiFake page;
        PrepareStopped(out host, out source, out page);
        bool reached = false;
        HostPageRaceBarriers.AfterRefreshSnapshot = delegate(string text) { reached = Contains(text, "Frame[0]"); };
        Check(page.Click("Refresh diagnostic") && reached, "after-refresh barrier was not instrumented");
        HostPageRaceBarriers.AfterRefreshSnapshot = null;
    }

    private static void ReplaceImmediatelyBeforeStart()
    {
        VerifyAfterStartBarrier();
        Source source = new Source();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake oldPage = Show(host);
        HostUiFake newPage = null;
        bool barrierReached = false;
        bool startReturned = false;
        bool accepted = false;
        bool sampled = false;
        HostPageRaceBarriers.BeforeStartCall = delegate
        {
            HostPageRaceBarriers.BeforeStartCall = null;
            barrierReached = true;
            newPage = Show(host);
        };
        HostPageRaceBarriers.AfterStartCall = delegate(bool started)
        {
            HostPageRaceBarriers.AfterStartCall = null;
            startReturned = true;
            accepted = started;
            if (started)
            {
                bool before = host.BeforeFrame();
                source.Simulation.m_currentFrameIndex = 101;
                source.Simulation.m_dayTimeFrame = 201;
                bool after = host.AfterFrame();
                sampled = before && after;
            }
        };
        Check(oldPage.Click("Start diagnostic"), "old page start button was missing");
        Check(barrierReached && newPage != null, "before-start barrier was not reached");
        if (sampled) Console.WriteLine("WITNESS retired page captured after replacement");
        Check((!startReturned || !accepted) && !sampled && source.Calls == 0
            && source.Simulation.ReadCount == 0 && source.Loading.ReadCount == 0,
            "retired page started or sampled after replacement");
        HostPageRaceBarriers.AfterStartCall = null;
        Check(newPage.Click("Start diagnostic") && host.State.ToString() == "Recording",
            "replacement page could not start after rejecting the old page");
        Check(newPage.Click("Stop diagnostic") && host.State.ToString() == "Stopped",
            "replacement page could not stop its own session");
    }

    private static void ReplaceDuringManagerLookup()
    {
        VerifyAfterStartBarrier();
        Source source = new Source();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake oldPage = Show(host);
        HostUiFake newPage = null;
        bool replacedDuringLookup = false;
        bool accepted = false;
        bool sampled = false;
        source.DuringLookup = delegate
        {
            newPage = Show(host);
            replacedDuringLookup = true;
        };
        HostPageRaceBarriers.AfterStartCall = delegate(bool started)
        {
            HostPageRaceBarriers.AfterStartCall = null;
            accepted = started;
            if (started)
            {
                bool before = host.BeforeFrame();
                source.Simulation.m_currentFrameIndex = 101;
                source.Simulation.m_dayTimeFrame = 201;
                bool after = host.AfterFrame();
                sampled = before && after;
            }
        };
        Check(oldPage.Click("Start diagnostic"), "lookup-race start button was missing");
        Check(replacedDuringLookup && newPage != null, "manager lookup did not replace the page");
        if (sampled) Console.WriteLine("WITNESS retired lookup captured after replacement");
        Check(!accepted && !sampled && source.Simulation.ReadCount == 0 && source.Loading.ReadCount == 0,
            "retired lookup committed or sampled after replacement");
    }

    private static void ReplaceImmediatelyBeforeStop()
    {
        Source source = new Source();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake oldPage = Show(host);
        Check(oldPage.Click("Start diagnostic") && host.State.ToString() == "Recording", "stop-race setup failed");
        HostUiFake newPage = null;
        bool barrierReached = false;
        HostPageRaceBarriers.BeforeStopCall = delegate
        {
            HostPageRaceBarriers.BeforeStopCall = null;
            barrierReached = true;
            newPage = Show(host);
        };
        Check(oldPage.Click("Stop diagnostic"), "old page stop button was missing");
        Check(barrierReached && newPage != null, "before-stop barrier was not reached");
        Check(host.State.ToString() == "Recording", "retired page stopped the replacement session");
        Check(newPage.Click("Stop diagnostic") && host.State.ToString() == "Stopped",
            "replacement page could not stop the session");
    }

    private static void ReplaceBeforeSnapshotRead()
    {
        VerifyAfterRefreshBarrier();
        ControlledReadOnlyHost host;
        Source source;
        HostUiFake oldPage;
        PrepareStopped(out host, out source, out oldPage);
        HostUiFake newPage = null;
        bool barrierReached = false;
        bool afterSnapshotReached = false;
        bool oldSnapshotObserved = false;
        HostPageRaceBarriers.BeforeRefreshSnapshot = delegate
        {
            HostPageRaceBarriers.BeforeRefreshSnapshot = null;
            barrierReached = true;
            newPage = Show(host);
        };
        HostPageRaceBarriers.AfterRefreshSnapshot = delegate(string text)
        {
            HostPageRaceBarriers.AfterRefreshSnapshot = null;
            afterSnapshotReached = true;
            oldSnapshotObserved = Contains(text, "Frame[0]");
        };
        Check(oldPage.Click("Refresh diagnostic"), "old page refresh button was missing");
        Check(barrierReached && newPage != null, "before-snapshot barrier was not reached");
        Check(!afterSnapshotReached && !oldSnapshotObserved,
            "retired page invoked the snapshot presenter after replacement");
        HostPageRaceBarriers.AfterRefreshSnapshot = null;
        Check(newPage.Click("Refresh diagnostic") && Contains(newPage.Fields[1].text, "Frame[0]"),
            "replacement page could not read the stopped fake snapshot");
    }

    private static void ReplaceImmediatelyBeforeResultSetter()
    {
        ControlledReadOnlyHost host;
        Source source;
        HostUiFake oldPage;
        PrepareStopped(out host, out source, out oldPage);
        HostUiFake newPage = null;
        bool barrierReached = false;
        int oldCopies = oldPage.Fields[1].HostTextCopies.Count;
        HostPageRaceBarriers.BeforeResultSetter = delegate
        {
            HostPageRaceBarriers.BeforeResultSetter = null;
            barrierReached = true;
            newPage = Show(host);
        };
        Check(oldPage.Click("Refresh diagnostic"), "result-race refresh button was missing");
        Check(barrierReached && newPage != null, "before-result-setter barrier was not reached");
        Check(!oldPage.ContainsNewResult(1, oldCopies), "retired page sent result after replacement");
        Check(newPage.Click("Refresh diagnostic") && Contains(newPage.Fields[1].text, "Frame[0]"),
            "replacement page could not display its fake snapshot");
    }

    private static void CrossThreadPageFailsClosed()
    {
        Source source = new Source();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake oldPage = Show(host);
        Check(oldPage.Click("Start diagnostic") && host.State.ToString() == "Recording", "cross-thread setup failed");
        HostUiFake foreign = new HostUiFake();
        bool shown = true;
        Exception error = null;
        Thread worker = new Thread(delegate()
        {
            try { shown = new ControlledDiagnosticUi(host).Show(foreign); }
            catch (Exception caught) { error = caught; }
        });
        worker.IsBackground = true;
        worker.Start();
        Check(worker.Join(5000) && error == null, "foreign fake UI thread did not finish cleanly");
        Check(!shown && foreign.Fields.Count == 0, "cross-thread replacement did not fail closed");
        if (host.State.ToString() == "Recording")
            Check(oldPage.Click("Stop diagnostic") && host.State.ToString() == "Stopped", "rejected replacement lost stop control");
        else if (host.State.ToString() == "RevokePending")
            Check(host.RetryRevocation() && host.State.ToString() == "Revoked", "cross-thread revoke remained unresolved");
        else
            Check(host.State.ToString() == "Revoked" || host.State.ToString() == "Stopped",
                "cross-thread replacement left an unexplained state");
    }

    private static void InFlightSetterCopyIsNotRetracted()
    {
        ControlledReadOnlyHost host;
        Source source;
        HostUiFake page;
        PrepareStopped(out host, out source, out page);
        int priorCopies = page.Fields[1].HostTextCopies.Count;
        bool invoked = false;
        string during = null;
        page.Fields[1].OnNextTextWrite = delegate
        {
            invoked = true;
            host.OnLevelUnloading();
            during = host.State.ToString();
        };
        Check(page.Click("Refresh diagnostic") && invoked, "in-flight fake setter was not exercised");
        Check(during == "RevokePending", "in-flight setter was falsely treated as completed");
        Check(page.ContainsNewResult(1, priorCopies), "fake host did not retain the already-entered setter copy");
        Check(page.Click("Retry revocation") && host.State.ToString() == "Revoked", "in-flight revoke did not finish");
        int copiesAfter = page.Fields[1].HostTextCopies.Count;
        page.Click("Refresh diagnostic");
        Check(!page.ContainsNewResult(1, copiesAfter), "revoked page delivered an additional old result");
        Check(page.ContainsNewResult(1, priorCopies), "previous fake host copy was incorrectly treated as retracted");
    }

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new ArgumentException("Specify exactly one page-race fake case.");
            switch (args[0])
            {
                case "start-after-replace": ReplaceImmediatelyBeforeStart(); break;
                case "lookup-after-replace": ReplaceDuringManagerLookup(); break;
                case "stop-after-replace": ReplaceImmediatelyBeforeStop(); break;
                case "refresh-after-replace": ReplaceBeforeSnapshotRead(); break;
                case "setter-after-replace": ReplaceImmediatelyBeforeResultSetter(); break;
                case "cross-thread-show": CrossThreadPageFailsClosed(); break;
                case "in-flight-copy": InFlightSetterCopyIsNotRetracted(); break;
                default: throw new ArgumentException("Unknown page-race fake case.");
            }
            Console.WriteLine("PASS controlled page-race fake: " + args[0]);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
