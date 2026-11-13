// Signature: OpenAI-Codex_native_validation_options
// One named case per process: the actual game-entry host is deliberately static and one-shot.
using System;
using System.Threading;
using ICities;
using RealisticCityTime.Diagnostics;

internal static class HostEntryChecks
{
    private sealed class Scene
    {
        internal readonly SimulationManager Simulation = new SimulationManager();
        internal readonly LoadingManager Loading = new LoadingManager();
        internal readonly ControlledReadOnlyDiagnosticMod Mod = new ControlledReadOnlyDiagnosticMod();
        internal readonly ControlledReadOnlyDiagnosticLoading Lifecycle = new ControlledReadOnlyDiagnosticLoading();
        internal readonly ControlledReadOnlyDiagnosticThreading Threading = new ControlledReadOnlyDiagnosticThreading();

        internal Scene()
        {
            Simulation.m_currentFrameIndex = 100;
            Simulation.m_dayTimeFrame = 200;
            Simulation.m_timePerFrame = TimeSpan.FromTicks(500);
            Simulation.m_referenceFrameIndex = 100;
            Simulation.m_referenceTimer = 0.25f;
            Simulation.m_currentDayTimeHour = 6f;
            Loading.m_currentlyLoading = false;
            Loading.m_loadingComplete = true;
            UnityEngine.Object.Simulations = new UnityEngine.Object[] { Simulation };
            UnityEngine.Object.Loadings = new UnityEngine.Object[] { Loading };
        }

        internal HostUiFake Show()
        {
            HostUiFake helper = new HostUiFake();
            Mod.OnSettingsUI(helper);
            Check(helper.Fields.Count == 2, "entry did not create status and result controls");
            return helper;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Controlled entry: " + message);
    }

    private static bool Contains(string value, string fragment)
    {
        return value != null && value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string State { get { return ControlledDiagnosticGameHost.Instance.State.ToString(); } }

    private static void CallFrames(Scene scene)
    {
        scene.Threading.OnBeforeSimulationFrame();
        scene.Threading.OnAfterSimulationFrame();
        scene.Threading.OnUpdate(0.1f, 0.1f);
    }

    private static void DefaultIsDisabled()
    {
        Scene scene = new Scene();
        scene.Simulation.FailOnRead = true;
        scene.Loading.FailOnRead = true;
        scene.Lifecycle.OnLevelLoaded(LoadMode.NewGame);
        HostUiFake helper = new HostUiFake();
        helper.EagerText = true;
        helper.EagerButtons = true;
        helper.AfterEagerButton = delegate(string ignored) { CallFrames(scene); };
        scene.Mod.OnSettingsUI(helper);
        Check(helper.Fields.Count == 2 && State == "Ready", "entry UI did not remain ready");
        CallFrames(scene);
        Check(UnityEngine.Object.SimulationLookups == 0 && UnityEngine.Object.LoadingLookups == 0,
            "entry looked up managers before manual start");
        Check(scene.Simulation.ReadCount == 0 && scene.Loading.ReadCount == 0,
            "entry read game fields before manual start");
        Check(Contains(helper.Fields[0].text, "snapshot"), "status did not disclose snapshot semantics");
    }

    private static void UniqueManagersAndPageRebuild()
    {
        Scene scene = new Scene();
        scene.Lifecycle.OnLevelLoaded(LoadMode.NewGame);
        HostUiFake oldPage = scene.Show();
        Check(oldPage.Click("Start diagnostic") && State == "Recording", "entry start failed");
        Check(UnityEngine.Object.SimulationLookups == 1 && UnityEngine.Object.LoadingLookups == 1,
            "manual start did not make one lookup per manager type");
        scene.Threading.OnBeforeSimulationFrame();
        scene.Simulation.m_currentFrameIndex = 101;
        scene.Simulation.m_dayTimeFrame = 201;
        scene.Threading.OnAfterSimulationFrame();
        scene.Threading.OnUpdate(0.1f, 0.1f);
        scene.Simulation.m_referenceFrameIndex = 101;
        scene.Simulation.m_referenceTimer = 0.5f;
        scene.Simulation.m_currentDayTimeHour = 6.1f;
        scene.Threading.OnUpdate(0.1f, 0.1f);

        HostUiFake newPage = scene.Show();
        Check(Contains(newPage.Fields[0].text, "Recording") && Contains(newPage.Fields[0].text, "snapshot"),
            "rebuilt entry page did not show its status snapshot");
        int oldStatusCopies = oldPage.Fields[0].HostTextCopies.Count;
        int oldResultCopies = oldPage.Fields[1].HostTextCopies.Count;
        oldPage.Click("Stop diagnostic");
        Check(State == "Recording", "old entry page stopped the current session");
        Check(newPage.Click("Stop diagnostic") && State == "Stopped", "new entry page could not stop");
        Check(newPage.Click("Refresh diagnostic") && Contains(newPage.Fields[1].text, "Frame[0]"),
            "entry refresh did not display a relative fake record");
        oldPage.Click("Start diagnostic");
        oldPage.Click("Check status");
        oldPage.Click("Refresh diagnostic");
        Check(State == "Stopped" && oldPage.Fields[0].HostTextCopies.Count == oldStatusCopies
            && oldPage.Fields[1].HostTextCopies.Count == oldResultCopies,
            "retired entry page changed state or published stale data");
        Check(UnityEngine.Object.SimulationLookups == 1 && UnityEngine.Object.LoadingLookups == 1,
            "entry re-resolved managers after the one-shot start");
        scene.Lifecycle.OnLevelUnloading();
        Check(newPage.Click("Retry revocation") && State == "Revoked", "entry revoke was not confirmed");
    }

    private static void RefusedManagerLookup(string kind)
    {
        Scene scene = new Scene();
        SimulationManager extra = new SimulationManager();
        scene.Simulation.FailOnRead = true;
        scene.Loading.FailOnRead = true;
        extra.FailOnRead = true;
        if (kind == "missing") UnityEngine.Object.Simulations = new UnityEngine.Object[0];
        else if (kind == "duplicate") UnityEngine.Object.Simulations = new UnityEngine.Object[] { scene.Simulation, extra };
        else if (kind == "lookup-error") UnityEngine.Object.ThrowOnLookup = true;
        else throw new InvalidOperationException("Unknown manager rejection case.");
        scene.Lifecycle.OnLevelLoaded(LoadMode.NewGame);
        HostUiFake helper = scene.Show();
        Check(helper.Click("Start diagnostic"), "rejected lookup start button missing");
        Check(State != "Recording", "missing, duplicate, or failed manager lookup started capture");
        CallFrames(scene);
        Check(scene.Simulation.ReadCount == 0 && scene.Loading.ReadCount == 0 && extra.ReadCount == 0,
            "rejected manager lookup led to game field reads");
        Check(UnityEngine.Object.SimulationLookups == 1, "manual start did not attempt one simulation lookup");
        if (kind != "lookup-error") Check(UnityEngine.Object.LoadingLookups == 1,
            "manual start did not attempt one loading lookup");
    }

    private static void NonGameplayNeverLooksUp()
    {
        Scene scene = new Scene();
        scene.Simulation.FailOnRead = true;
        scene.Loading.FailOnRead = true;
        scene.Lifecycle.OnLevelLoaded(LoadMode.Other);
        HostUiFake helper = scene.Show();
        helper.Click("Start diagnostic");
        CallFrames(scene);
        Check(State != "Recording" && UnityEngine.Object.SimulationLookups == 0
            && UnityEngine.Object.LoadingLookups == 0, "non-gameplay entry looked up or started managers");
        Check(scene.Simulation.ReadCount == 0 && scene.Loading.ReadCount == 0,
            "non-gameplay entry read game fields");
    }

    private static void BusyUnloadOrRelease(string kind)
    {
        Scene scene = new Scene();
        scene.Lifecycle.OnLevelLoaded(LoadMode.NewGame);
        HostUiFake helper = scene.Show();
        Check(helper.Click("Start diagnostic") && State == "Recording", "busy entry start failed");
        scene.Simulation.BlockOneRead();
        Exception callbackError = null;
        Exception endingError = null;
        ManualResetEvent endingReturned = new ManualResetEvent(false);
        Thread callback = new Thread(delegate()
        {
            try { scene.Threading.OnBeforeSimulationFrame(); }
            catch (Exception error) { callbackError = error; }
        });
        Thread ending = new Thread(delegate()
        {
            try
            {
                if (kind == "unload") scene.Lifecycle.OnLevelUnloading();
                else scene.Threading.OnReleased();
            }
            catch (Exception error) { endingError = error; }
            finally { endingReturned.Set(); }
        });
        callback.IsBackground = true;
        ending.IsBackground = true;
        callback.Start();
        try
        {
            Check(scene.Simulation.ReadEntered.WaitOne(5000), "entry callback did not enter synthetic read");
            ending.Start();
            Check(endingReturned.WaitOne(2000) && endingError == null, "entry unload or release blocked");
            Check(State == "RevokePending", "entry unload or release falsely confirmed revoke");
            Check(helper.Click("Retry revocation") && State == "RevokePending",
                "entry retry confirmed while a callback was in flight");
        }
        finally
        {
            scene.Simulation.ReleaseRead.Set();
            Check(callback.Join(5000), "entry callback did not finish");
            if (ending.IsAlive) Check(ending.Join(5000), "entry unload or release did not finish");
            endingReturned.Close();
        }
        Check(callbackError == null && endingError == null, "entry fake thread raised an error");
        int simulationReads = scene.Simulation.ReadCount;
        int loadingReads = scene.Loading.ReadCount;
        scene.Threading.OnBeforeSimulationFrame();
        scene.Threading.OnAfterSimulationFrame();
        Check(scene.Simulation.ReadCount == simulationReads && scene.Loading.ReadCount == loadingReads,
            "late entry frame callback read after revoke request");
        Check(helper.Click("Retry revocation") && State == "Revoked", "entry retry did not confirm revoke");
        scene.Threading.OnUpdate(0.1f, 0.1f);
        Check(scene.Simulation.ReadCount == simulationReads && scene.Loading.ReadCount == loadingReads,
            "entry update read fake fields after revoke confirmation");
    }

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 1) throw new ArgumentException("Specify exactly one entry fake case.");
            switch (args[0])
            {
                case "default": DefaultIsDisabled(); break;
                case "unique-rebuild": UniqueManagersAndPageRebuild(); break;
                case "missing": RefusedManagerLookup("missing"); break;
                case "duplicate": RefusedManagerLookup("duplicate"); break;
                case "lookup-error": RefusedManagerLookup("lookup-error"); break;
                case "non-gameplay": NonGameplayNeverLooksUp(); break;
                case "busy-unload": BusyUnloadOrRelease("unload"); break;
                case "busy-release": BusyUnloadOrRelease("release"); break;
                default: throw new ArgumentException("Unknown entry fake case.");
            }
            Console.WriteLine("PASS controlled entry fake: " + args[0]);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
