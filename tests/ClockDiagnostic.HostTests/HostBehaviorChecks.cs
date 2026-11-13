// Signature: OpenAI-Codex_native_validation_options
// Isolated fake host checks. No game, Steam, real UI, storage, or player data is accessed.
using System;
using System.Threading;
using RealisticCityTime.Diagnostics;

internal static class HostBehaviorChecks
{
    private sealed class ExistingSource : IExistingManagerSource
    {
        internal readonly SimulationManager Simulation = new SimulationManager();
        internal readonly LoadingManager Loading = new LoadingManager();
        internal int Calls;
        internal bool Available = true;
        internal bool BlockLookup;
        internal readonly ManualResetEvent LookupEntered = new ManualResetEvent(false);
        internal readonly ManualResetEvent ReleaseLookup = new ManualResetEvent(false);

        internal ExistingSource()
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
            Interlocked.Increment(ref Calls);
            if (BlockLookup)
            {
                LookupEntered.Set();
                if (!ReleaseLookup.WaitOne(10000)) throw new InvalidOperationException("Synthetic lookup timed out.");
            }
            simulation = Available ? Simulation : null;
            loading = Available ? Loading : null;
            return Available;
        }
    }

    private static void Check(bool value, string message) { HostBehaviorOracle.Check(value, message); }

    private static bool Contains(string text, string fragment)
    {
        return text != null && text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool HasUnverified(string text)
    {
        return Contains(text, "unverified") || Contains(text, "not verified") || Contains(text, "false");
    }

    private static ControlledReadOnlyHost Ready(ExistingSource source)
    {
        ControlledReadOnlyHost host = new ControlledReadOnlyHost(source);
        Check(host.State.ToString() == "Unavailable", "new host was not unavailable by default");
        host.OnLevelLoaded(true);
        Check(host.State.ToString() == "Ready", "gameplay notification did not enter ready state");
        return host;
    }

    private static HostUiFake Show(ControlledReadOnlyHost host)
    {
        HostUiFake helper = new HostUiFake();
        Check(new ControlledDiagnosticUi(host).Show(helper), "host UI did not initialize");
        Check(helper.Fields.Count == 2, "host UI did not expose status and result fields");
        return helper;
    }

    private static void RecordOneFrame(ControlledReadOnlyHost host, ExistingSource source)
    {
        Check(host.BeforeFrame(), "manual session did not accept fake before-frame");
        source.Simulation.m_currentFrameIndex = 101;
        source.Simulation.m_dayTimeFrame = 201;
        Check(host.AfterFrame(), "manual session did not accept fake after-frame");
    }

    private static void DefaultDisabledAndEagerUi()
    {
        ExistingSource source = new ExistingSource();
        source.Simulation.FailOnRead = true;
        source.Loading.FailOnRead = true;
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake helper = new HostUiFake();
        helper.EagerText = true;
        helper.EagerButtons = true;
        helper.AfterEagerButton = delegate(string ignored)
        {
            HostBehaviorOracle.NoPreArmReads(source.Simulation, source.Loading,
                host.BeforeFrame, host.AfterFrame, host.DisplayUpdate);
        };
        Check(new ControlledDiagnosticUi(host).Show(helper), "eager UI did not initialize");
        Check(helper.Fields.Count == 2, "eager UI field count was not two");
        HostBehaviorOracle.NoPreArmReads(source.Simulation, source.Loading,
            host.BeforeFrame, host.AfterFrame, host.DisplayUpdate);
        Check(source.Calls == 0 && host.State.ToString() == "Ready",
            "eager UI or disabled callback started or resolved a manager");
        Check(!host.RetryStop() && !host.RetryRevocation() && source.Calls == 0,
            "a no-token retry acted without a pending stop or revoke");
        string status = helper.Fields[0].text;
        Check(Contains(status, "Ready") && Contains(status, "snapshot"),
            "initial status did not disclose ready state and snapshot semantics");
        Check(Contains(status, "city") && HasUnverified(status),
            "initial status did not disclose unverified city identity");
        Check(Contains(status, "GameMod") && Contains(status, "date"),
            "initial status did not disclose the separate date-reading observer");

        bool nested = false;
        helper.Fields[0].OnNextTextWrite = delegate
        {
            nested = helper.Click("Start diagnostic");
        };
        Check(helper.Click("Check status") && nested, "reentrant status/start fake was not exercised");
        Check(source.Calls == 0 && host.State.ToString() == "Ready",
            "reentrant status callback started a diagnostic");
        HostBehaviorOracle.NoPreArmReads(source.Simulation, source.Loading,
            host.BeforeFrame, host.AfterFrame, host.DisplayUpdate);
    }

    private static void ManualObservationAndOneShot()
    {
        ExistingSource source = new ExistingSource();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake helper = Show(host);
        Check(helper.Click("Start diagnostic"), "start button missing");
        Check(host.State.ToString() == "Recording" && source.Calls == 1,
            "manual start did not begin exactly one host session");
        RecordOneFrame(host, source);
        Check(host.DisplayUpdate(), "first fake display update was not recorded");
        source.Simulation.m_referenceFrameIndex = 101;
        source.Simulation.m_referenceTimer = 0.5f;
        source.Simulation.m_currentDayTimeHour = 6.1f;
        Check(host.DisplayUpdate(), "second fake display update was not recorded");
        Check(helper.Click("Refresh diagnostic") && !Contains(helper.Fields[1].text, "Frame[0]"),
            "active host exposed a snapshot through the page");
        Check(helper.Click("Stop diagnostic") && host.State.ToString() == "Stopped",
            "explicit stop was not confirmed");
        Check(helper.Click("Refresh diagnostic") && Contains(helper.Fields[1].text, "Frame[0]"),
            "explicit refresh did not present the stopped fake result");
        string result = helper.Fields[1].text;
        Check(Contains(result, "FrameDelta=1") && Contains(result, "Display[0] HasDelta=False")
            && Contains(result, "Display[1] HasDelta=True"), "frame pair or display baseline semantics changed");
        Check(Contains(result, "CityIdentityVerified=False") && Contains(result, "LifecycleContinuityVerified=False")
            && Contains(result, "ConcurrentCallbacksExcluded=False") && Contains(result, "PhaseMayWrapOrJump=True"),
            "fake result incorrectly claimed verified game evidence");
        int calls = source.Calls;
        Check(helper.Click("Start diagnostic") && host.State.ToString() != "Recording" && source.Calls == calls,
            "a stopped one-shot host silently restarted");
    }

    private static void RebuiltPageKeepsControlsAndRetiresOldPage()
    {
        ExistingSource source = new ExistingSource();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake oldPage = Show(host);
        Check(oldPage.Click("Start diagnostic") && host.State.ToString() == "Recording", "old page start failed");
        RecordOneFrame(host, source);
        HostUiFake newPage = Show(host);
        Check(Contains(newPage.Fields[0].text, "Recording") && Contains(newPage.Fields[0].text, "snapshot"),
            "rebuilt page did not disclose its current status snapshot");
        int oldStatusCopies = oldPage.Fields[0].HostTextCopies.Count;
        int oldResultCopies = oldPage.Fields[1].HostTextCopies.Count;
        Check(oldPage.Click("Stop diagnostic") && host.State.ToString() == "Recording",
            "superseded page could stop the current session");
        Check(newPage.Click("Stop diagnostic") && host.State.ToString() == "Stopped",
            "rebuilt page could not stop the session");
        Check(newPage.Click("Refresh diagnostic") && Contains(newPage.Fields[1].text, "Frame[0]"),
            "rebuilt page could not show the stopped result");
        int calls = source.Calls;
        oldPage.Click("Start diagnostic");
        oldPage.Click("Check status");
        oldPage.Click("Refresh diagnostic");
        Check(host.State.ToString() == "Stopped" && source.Calls == calls,
            "superseded page restarted or changed the stopped session");
        Check(oldPage.Fields[0].HostTextCopies.Count == oldStatusCopies
            && oldPage.Fields[1].HostTextCopies.Count == oldResultCopies,
            "superseded page published a new stale status or result");
        host.OnLevelUnloading();
        Check(newPage.Click("Retry revocation"), "rebuilt page omitted revocation retry");
        Check(host.State.ToString() == "Revoked", "idle unload was not confirmed revoked");
    }

    private static void FailedRebuildDoesNotLeaveUncontrolledRecording()
    {
        ExistingSource source = new ExistingSource();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake oldPage = Show(host);
        Check(oldPage.Click("Start diagnostic") && host.State.ToString() == "Recording", "rebuild failure start failed");
        HostUiFake broken = new HostUiFake();
        broken.NullFieldAt = 2;
        Check(!new ControlledDiagnosticUi(host).Show(broken), "broken rebuilt page was reported usable");
        Check(host.State.ToString() == "Revoked" || host.State.ToString() == "RevokePending",
            "failed rebuilt page did not revoke or report pending");
        int calls = source.Calls;
        oldPage.Click("Start diagnostic");
        Check(source.Calls == calls && host.State.ToString() != "Recording",
            "old page restarted after failed rebuild");
    }

    private static void BusyUnloadRequiresExplicitRetry()
    {
        ExistingSource source = new ExistingSource();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake helper = Show(host);
        Check(helper.Click("Start diagnostic") && host.State.ToString() == "Recording", "busy unload start failed");
        source.Simulation.BlockOneRead();
        Exception captureError = null;
        Exception unloadError = null;
        ManualResetEvent unloadReturned = new ManualResetEvent(false);
        Thread capture = new Thread(delegate()
        {
            try { host.BeforeFrame(); }
            catch (Exception error) { captureError = error; }
        });
        Thread unload = new Thread(delegate()
        {
            try { host.OnLevelUnloading(); }
            catch (Exception error) { unloadError = error; }
            finally { unloadReturned.Set(); }
        });
        capture.IsBackground = true;
        unload.IsBackground = true;
        capture.Start();
        try
        {
            Check(source.Simulation.ReadEntered.WaitOne(5000), "busy unload fake getter did not begin");
            unload.Start();
            Check(unloadReturned.WaitOne(2000) && unloadError == null,
                "unload blocked on an in-flight fake callback");
            Check(host.State.ToString() == "RevokePending", "busy unload falsely confirmed revoke");
            Check(helper.Click("Retry revocation") && host.State.ToString() == "RevokePending",
                "busy retry falsely confirmed revoke");
        }
        finally
        {
            source.Simulation.ReleaseRead.Set();
            Check(capture.Join(5000), "busy capture thread did not finish");
            if (unload.IsAlive) Check(unload.Join(5000), "busy unload thread did not finish");
            unloadReturned.Close();
        }
        Check(captureError == null && unloadError == null, "busy unload helper raised an error");
        int simulationReads = source.Simulation.ReadCount;
        int loadingReads = source.Loading.ReadCount;
        Check(!host.BeforeFrame() && !host.AfterFrame() && !host.DisplayUpdate(),
            "old callback was accepted before revoke retry");
        Check(source.Simulation.ReadCount == simulationReads && source.Loading.ReadCount == loadingReads,
            "old callback read fake fields before revoke retry");
        host.OnLevelLoaded(true);
        Check(host.State.ToString() != "Recording" && host.State.ToString() != "Ready",
            "new notification activated an unresolved session");
        Check(helper.Click("Retry revocation") && host.State.ToString() == "Revoked",
            "explicit host UI retry did not confirm revoke");
        host.OnReleased();
        Check(host.State.ToString() == "Revoked", "release undid confirmed revoke");
    }

    private static void ReentrantResultSetterRequiresRetry()
    {
        ExistingSource source = new ExistingSource();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake helper = Show(host);
        Check(helper.Click("Start diagnostic") && host.State.ToString() == "Recording", "result revoke start failed");
        RecordOneFrame(host, source);
        Check(helper.Click("Stop diagnostic") && host.State.ToString() == "Stopped", "result revoke stop failed");
        bool invoked = false;
        string stateDuringSetter = null;
        helper.Fields[1].OnNextTextWrite = delegate
        {
            invoked = true;
            host.OnLevelUnloading();
            stateDuringSetter = host.State.ToString();
        };
        Check(helper.Click("Refresh diagnostic") && invoked, "reentrant result setter was not exercised");
        Check(stateDuringSetter == "RevokePending", "in-flight result setter falsely confirmed revoke");
        Check(helper.Click("Retry revocation") && host.State.ToString() == "Revoked",
            "result setter revoke did not confirm after the setter returned");
        int statusCopies = helper.Fields[0].HostTextCopies.Count;
        int resultCopies = helper.Fields[1].HostTextCopies.Count;
        helper.Click("Refresh diagnostic");
        Check(!helper.ContainsNewResult(0, statusCopies) && !helper.ContainsNewResult(1, resultCopies),
            "revoked page republished old results");
    }

    private static void UnloadWhileStartLookupIsInFlight()
    {
        ExistingSource source = new ExistingSource();
        source.BlockLookup = true;
        source.Simulation.FailOnRead = true;
        source.Loading.FailOnRead = true;
        ControlledReadOnlyHost host = Ready(source);
        bool started = true;
        Exception startError = null;
        Exception unloadError = null;
        ManualResetEvent unloaded = new ManualResetEvent(false);
        Thread start = new Thread(delegate()
        {
            try
            {
                HostUiFake page = Show(host);
                if (!page.Click("Start diagnostic")) throw new InvalidOperationException("start button missing");
                started = host.State.ToString() == "Recording";
            }
            catch (Exception error) { startError = error; }
        });
        Thread unload = new Thread(delegate()
        {
            try { host.OnLevelUnloading(); }
            catch (Exception error) { unloadError = error; }
            finally { unloaded.Set(); }
        });
        start.IsBackground = true;
        unload.IsBackground = true;
        start.Start();
        try
        {
            Check(source.LookupEntered.WaitOne(5000), "manual manager lookup did not start");
            unload.Start();
            Check(unloaded.WaitOne(2000) && unloadError == null, "unload blocked on an in-flight lookup");
            Check(host.State.ToString() == "RevokePending" && !host.RetryRevocation(),
                "unload falsely confirmed while start lookup was in flight");
        }
        finally
        {
            source.ReleaseLookup.Set();
            Check(start.Join(5000), "manual start lookup did not return");
            if (unload.IsAlive) Check(unload.Join(5000), "lookup-race unload did not return");
            unloaded.Close();
        }
        Check(startError == null && unloadError == null && !started,
            "in-flight start survived unload");
        Check(source.Simulation.ReadCount == 0 && source.Loading.ReadCount == 0,
            "cancelled start lookup read game fields after unload");
        HostBehaviorOracle.NoPreArmReads(source.Simulation, source.Loading,
            host.BeforeFrame, host.AfterFrame, host.DisplayUpdate);
        Check(host.RetryRevocation() && host.State.ToString() == "Revoked",
            "lookup-race revocation did not confirm after retry");
    }

    private static void ReleaseRequiresRetryWhenCaptureIsBusy()
    {
        ExistingSource source = new ExistingSource();
        ControlledReadOnlyHost host = Ready(source);
        HostUiFake helper = Show(host);
        Check(helper.Click("Start diagnostic") && host.State.ToString() == "Recording", "release-pending start failed");
        source.Simulation.BlockOneRead();
        Exception captureError = null;
        Exception releaseError = null;
        ManualResetEvent released = new ManualResetEvent(false);
        Thread capture = new Thread(delegate()
        {
            try { host.BeforeFrame(); }
            catch (Exception error) { captureError = error; }
        });
        Thread release = new Thread(delegate()
        {
            try { host.OnReleased(); }
            catch (Exception error) { releaseError = error; }
            finally { released.Set(); }
        });
        capture.IsBackground = true;
        release.IsBackground = true;
        capture.Start();
        try
        {
            Check(source.Simulation.ReadEntered.WaitOne(5000), "release-pending fake getter did not begin");
            release.Start();
            Check(released.WaitOne(2000) && releaseError == null, "release blocked on an in-flight callback");
            Check(host.State.ToString() == "RevokePending", "busy release falsely confirmed revoke");
        }
        finally
        {
            source.Simulation.ReleaseRead.Set();
            Check(capture.Join(5000), "release-pending capture did not finish");
            if (release.IsAlive) Check(release.Join(5000), "release-pending release did not finish");
            released.Close();
        }
        Check(captureError == null && releaseError == null, "release-pending helper raised an error");
        int simulationReads = source.Simulation.ReadCount;
        int loadingReads = source.Loading.ReadCount;
        Check(!host.BeforeFrame() && !host.AfterFrame() && !host.DisplayUpdate(),
            "released host accepted a late callback");
        Check(source.Simulation.ReadCount == simulationReads && source.Loading.ReadCount == loadingReads,
            "released host read fake fields before retry");
        Check(host.RetryRevocation() && host.State.ToString() == "Revoked",
            "release-pending retry did not confirm revoke");
    }

    private static int Main()
    {
        try
        {
            DefaultDisabledAndEagerUi();
            ManualObservationAndOneShot();
            RebuiltPageKeepsControlsAndRetiresOldPage();
            FailedRebuildDoesNotLeaveUncontrolledRecording();
            BusyUnloadRequiresExplicitRetry();
            ReentrantResultSetterRequiresRetry();
            UnloadWhileStartLookupIsInFlight();
            ReleaseRequiresRetryWhenCaptureIsBusy();
            Console.WriteLine("PASS isolated controlled-host fake checks; no game or player data accessed");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
