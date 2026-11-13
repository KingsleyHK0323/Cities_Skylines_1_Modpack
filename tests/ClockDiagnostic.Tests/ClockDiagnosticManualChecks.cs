// Signature: OpenAI-Codex_diagnostic_tests
// Standalone fake checks for the normal-symbol managed controller and presenter.
using System;
using System.Reflection;
using System.Threading;

internal static class ClockDiagnosticManualChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type recorderType;
    private static Type adapterType;
    private static Type controllerType;
    private static Type presenterType;

    private sealed class ProbeContext : SynchronizationContext
    {
        internal int Posts;
        internal int Sends;
        public override void Post(SendOrPostCallback callback, object state) { Interlocked.Increment(ref Posts); }
        public override void Send(SendOrPostCallback callback, object state) { Interlocked.Increment(ref Sends); }
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Manual diagnostic: " + message);
    }

    private static object NewObject(Type type, params object[] arguments)
    {
        return Activator.CreateInstance(type, Members, null, arguments, null);
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(name, Members);
        Check(method != null, "missing method " + name);
        try { return method.Invoke(target, arguments); }
        catch (TargetInvocationException error) { throw new InvalidOperationException("method " + name + " threw", error.InnerException); }
    }

    private static bool Call(object target, string name, params object[] arguments)
    {
        return (bool)Invoke(target, name, arguments);
    }

    private static object Member(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, Members);
        if (property != null) return property.GetValue(target, null);
        FieldInfo field = target.GetType().GetField(name, Members);
        Check(field != null, "missing result member " + name);
        return field.GetValue(target);
    }

    private static int Count(object snapshot, string name) { return Convert.ToInt32(Member(snapshot, name)); }
    private static object Recorder() { return NewObject(recorderType); }
    private static object Adapter(object controller)
    {
        Check(controller.GetType().GetMethod("CreateAdapter", Members) != null, "normal-symbol controller adapter is missing");
        return Invoke(controller, "CreateAdapter");
    }
    private static object Controller(object recorder) { return NewObject(controllerType, recorder); }
    private static object Presenter(object controller) { return NewObject(presenterType, controller); }

    private static SimulationManager Simulation()
    {
        return new SimulationManager {
            m_currentFrameIndex = 100, m_dayTimeFrame = 200, m_timePerFrame = TimeSpan.FromTicks(50),
            m_referenceFrameIndex = 300, m_referenceTimer = 0.25f, m_currentDayTimeHour = 5.5f
        };
    }

    private static LoadingManager StableLoading() { return new LoadingManager { m_currentlyLoading = false, m_loadingComplete = true }; }

    private static bool Before(object adapter, long generation, SimulationManager simulation, LoadingManager loading)
    {
        return Call(adapter, "BeforeFrame", generation, simulation, loading);
    }

    private static bool After(object adapter, long generation, SimulationManager simulation, LoadingManager loading)
    {
        return Call(adapter, "AfterFrame", generation, simulation, loading);
    }

    private static bool Display(object adapter, long generation, SimulationManager simulation, LoadingManager loading)
    {
        return Call(adapter, "DisplayUpdate", generation, simulation, loading);
    }

    private static void Unverified(object record, string stream)
    {
        Check(!(bool)Member(record, "LifecycleContinuityVerified"), stream + " certifies lifecycle continuity");
        Check(!(bool)Member(record, "CityIdentityVerified"), stream + " certifies city identity");
        Check(!(bool)Member(record, "ConcurrentCallbacksExcluded"), stream + " excludes concurrent callbacks");
        Check((bool)Member(record, "PhaseMayWrapOrJump"), stream + " certifies a phase path from endpoints");
    }

    private static void DefaultDisabledUntilManualAction()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object adapter = Adapter(controller);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.FailOnRead = true;
        loading.FailOnRead = true;
        Check(!(bool)Member(recorder, "Enabled"), "construction automatically enabled capture");
        Check(!Call(adapter, "BeforeFrame", 7L, simulation, loading), "disabled adapter accepted a frame");
        Check(!Call(adapter, "DisplayUpdate", 7L, simulation, loading), "disabled adapter accepted an update");
        Check(simulation.ReadCount == 0 && loading.ReadCount == 0, "disabled adapter read game data");
        Check(Invoke(controller, "TryReadSnapshot") == null, "an unstarted controller exposed a completed snapshot");
        Check(!Call(controller, "StartFromManualAction", 0L), "invalid manual generation was accepted");
        Check(!(bool)Member(recorder, "Enabled"), "invalid manual action enabled capture");
        Check(Call(controller, "StartFromManualAction", 7L), "explicit manual start was rejected");
        Check((bool)Member(recorder, "Enabled"), "explicit manual start did not enable capture");
        Check(!Call(controller, "StartFromManualAction", 7L), "a second start unexpectedly reset an active capture");
        Check(Invoke(controller, "TryReadSnapshot") == null, "active capture exposed a completed snapshot");
        Check(Call(controller, "StopFromManualAction"), "idle manual stop was not confirmed");
        Check(!Call(controller, "StartFromManualAction", 8L), "stopped recorder was silently rearmed");
    }

    private static void FillBothStreams(object controller, long generation)
    {
        object adapter = Adapter(controller);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        for (int i = 0; i < 8; i++)
        {
            simulation.m_currentFrameIndex = (uint)(1234567000 + i);
            simulation.m_dayTimeFrame = (uint)(60000 + i);
            simulation.m_timePerFrame = TimeSpan.FromTicks(1234567890L);
            Check(Before(adapter, generation, simulation, loading), "frame setup failed before capacity");
            simulation.m_currentFrameIndex = (uint)(1234567001 + i);
            simulation.m_dayTimeFrame = (uint)(60001 + i);
            Check(After(adapter, generation, simulation, loading), "frame completion failed before capacity");
            simulation.m_referenceFrameIndex = (uint)(1634567000 + i);
            simulation.m_referenceTimer = 0.25f + i * 0.03125f;
            simulation.m_currentDayTimeHour = i == 0 ? 1.0f : 20.0f + (i - 1) * 0.125f;
            Check(Display(adapter, generation, simulation, loading), "display setup failed before capacity");
        }
        int reads = simulation.ReadCount;
        int loadingReads = loading.ReadCount;
        Check(!Before(adapter, generation, simulation, loading), "frame stream exceeded eight");
        Check(!Display(adapter, generation, simulation, loading), "display stream exceeded eight");
        Check(simulation.ReadCount == reads && loading.ReadCount == loadingReads, "full streams continued reading fake fields");
    }

    private static void SnapshotIsFiniteRelativeAndReadOnly()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        Check(Call(controller, "StartFromManualAction", 8L), "manual capture start failed");
        FillBothStreams(controller, 8);
        Check(Invoke(controller, "TryReadSnapshot") == null, "active capture exposed a completed snapshot");
        Check(Call(controller, "StopFromManualAction"), "stop after completed capture was not confirmed");
        object first = Invoke(controller, "TryReadSnapshot");
        Check(first != null && Count(first, "FrameCount") == 8 && Count(first, "DisplayCount") == 8,
            "stopped snapshot did not contain the two bounded streams");
        object frame = Invoke(first, "FrameAt", 0);
        object display = Invoke(first, "DisplayAt", 1);
        Check(Convert.ToInt64(Member(frame, "FrameDelta")) == 1 && Convert.ToInt64(Member(frame, "PhaseDelta")) == 1,
            "frame snapshot did not expose endpoint differences");
        Check(Convert.ToDouble(Member(display, "PhaseHourDelta")) == 19, "display snapshot lost its signed endpoint difference");
        Unverified(frame, "frame snapshot");
        Unverified(display, "display snapshot");
        foreach (FieldInfo field in frame.GetType().GetFields(Members))
            Check(field.FieldType != typeof(string) && field.FieldType != typeof(DateTime), "frame record exposed text or an absolute date");
        foreach (FieldInfo field in display.GetType().GetFields(Members))
            Check(field.FieldType != typeof(string) && field.FieldType != typeof(DateTime), "display record exposed text or an absolute date");

        FieldInfo deltaField = frame.GetType().GetField("FrameDelta", Members);
        Check(deltaField != null, "frame delta field is missing");
        try { deltaField.SetValue(frame, Convert.ChangeType(999, deltaField.FieldType)); }
        catch (FieldAccessException) { }
        object second = Invoke(controller, "TryReadSnapshot");
        Check(second != null && Convert.ToInt64(Member(Invoke(first, "FrameAt", 0), "FrameDelta")) == 1
            && Convert.ToInt64(Member(Invoke(second, "FrameAt", 0), "FrameDelta")) == 1,
            "modifying a returned record changed a stored snapshot");
    }

    private static void BusyStopAndReadRemainUnconfirmed()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object adapter = Adapter(controller);
        Check(Call(controller, "StartFromManualAction", 9L), "busy-stop setup did not start");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.BlockOneRead();
        Exception callbackError = null;
        Thread callback = new Thread(delegate()
        {
            try { Call(adapter, "BeforeFrame", 9L, simulation, loading); }
            catch (Exception error) { callbackError = error; }
        });
        callback.IsBackground = true;
        callback.Start();
        Check(simulation.ReadEntered.WaitOne(5000), "fake callback never reached the controlled read");
        bool stopResult = true;
        object busySnapshot = new object();
        Exception controlError = null;
        ManualResetEvent finished = new ManualResetEvent(false);
        Thread control = new Thread(delegate()
        {
            try { stopResult = Call(controller, "StopFromManualAction"); busySnapshot = Invoke(controller, "TryReadSnapshot"); }
            catch (Exception error) { controlError = error; }
            finally { finished.Set(); }
        });
        control.IsBackground = true;
        control.Start();
        bool returnedWhileReadWasBlocked = finished.WaitOne(2000);
        simulation.ReleaseRead.Set();
        Check(callback.Join(5000) && control.Join(5000), "fake busy-stop threads did not finish");
        Check(returnedWhileReadWasBlocked && callbackError == null && controlError == null, "busy control blocked or threw");
        Check(!stopResult && busySnapshot == null, "busy operation claimed stop or returned a partial snapshot");
        Check(Call(controller, "StopFromManualAction"), "manual stop was not confirmed after callback completion");
        Check(Invoke(controller, "TryReadSnapshot") != null, "confirmed stop did not permit snapshot reading");
        int reads = simulation.ReadCount;
        int loadingReads = loading.ReadCount;
        Check(!Call(adapter, "BeforeFrame", 9L, simulation, loading), "stopped adapter accepted another frame");
        Check(simulation.ReadCount == reads && loading.ReadCount == loadingReads, "confirmed stop allowed more game reads");
    }

    private static void PanelShowsOnlyExplicitBoundedResults()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object presenter = Presenter(controller);
        Check(!(bool)Member(recorder, "Enabled"), "creating presenter automatically enabled capture");
        string initial = (string)Invoke(presenter, "RefreshClicked");
        Check(initial != null, "owner-thread refresh did not return status text");
        Check(Call(presenter, "StartClicked", 10L), "explicit start action failed");
        FillBothStreams(controller, 10);
        string active = (string)Invoke(presenter, "RefreshClicked");
        Check(active != null && CountLines(active, "Frame[") == 0 && CountLines(active, "Display[") == 0,
            "active refresh exposed unconfirmed samples");
        Check(Call(presenter, "StopClicked"), "explicit idle stop action failed");
        string text = (string)Invoke(presenter, "RefreshClicked");
        Check(text != null && CountLines(text, "Frame[") == 8 && CountLines(text, "Display[") == 8,
            "refresh did not show exactly eight records from each stream");
        Check(text.Contains("LifecycleContinuityVerified=False") && text.Contains("CityIdentityVerified=False")
            && text.Contains("ConcurrentCallbacksExcluded=False") && text.Contains("PhaseMayWrapOrJump=True"),
            "refresh omitted uncertainty flags");
        Check(text.Contains("ElapsedCounterTicks") && text.ToLowerInvariant().Contains("not seconds"),
            "refresh did not identify counter units");
        Check(text.Contains("19"), "refresh did not retain the 01:00 to 20:00 endpoint difference");
        Check(!text.Contains("1234567000") && !text.Contains("1634567000")
            && !text.Contains("1234567890") && !text.Contains("9000001000"),
            "refresh exposed an absolute fake input");
    }

    private static void PanelDoesNotClaimStopDuringBusyCapture()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object presenter = Presenter(controller);
        object adapter = Adapter(controller);
        Check(Call(presenter, "StartClicked", 14L), "presenter busy-stop setup failed");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.BlockOneRead();
        Exception callbackError = null;
        Thread callback = new Thread(delegate()
        {
            try { Call(adapter, "BeforeFrame", 14L, simulation, loading); }
            catch (Exception error) { callbackError = error; }
        });
        callback.IsBackground = true;
        callback.Start();
        Check(simulation.ReadEntered.WaitOne(5000), "presenter busy-stop callback did not enter fake read");
        ManualResetEvent actionsReturned = new ManualResetEvent(false);
        Thread watchdog = new Thread(delegate()
        {
            if (!actionsReturned.WaitOne(4000)) simulation.ReleaseRead.Set();
        });
        watchdog.IsBackground = true;
        watchdog.Start();
        bool stopped = Call(presenter, "StopClicked");
        string busyText = (string)Invoke(presenter, "RefreshClicked");
        bool returnedBeforeRelease = !simulation.ReleaseRead.WaitOne(0);
        actionsReturned.Set();
        simulation.ReleaseRead.Set();
        Check(callback.Join(5000) && watchdog.Join(5000), "presenter busy-stop helpers did not finish");
        Check(callbackError == null && returnedBeforeRelease && !stopped, "busy presenter blocked or claimed stop");
        Check(busyText != null && CountLines(busyText, "Frame[") == 0 && CountLines(busyText, "Display[") == 0,
            "busy refresh exposed an unconfirmed result");
        Check(Call(presenter, "StopClicked"), "presenter did not confirm stop after callback completion");
    }

    private static int CountLines(string text, string prefix)
    {
        int count = 0;
        foreach (string line in text.Replace("\r", "").Split('\n'))
            if (line.StartsWith(prefix, StringComparison.Ordinal)) count++;
        return count;
    }

    private static void PresenterDoesNotActOnAnotherThread()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object presenter = Presenter(controller);
        bool started = true;
        bool stopped = true;
        object text = new object();
        Exception error = null;
        ProbeContext context = new ProbeContext();
        Thread worker = new Thread(delegate()
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(context);
                started = Call(presenter, "StartClicked", 11L);
                stopped = Call(presenter, "StopClicked");
                text = Invoke(presenter, "RefreshClicked");
            }
            catch (Exception exception) { error = exception; }
        });
        worker.IsBackground = true;
        worker.Start();
        Check(worker.Join(5000), "wrong-thread presenter probe did not finish");
        Check(error == null && !started && !stopped && text == null, "presenter accepted an action from another thread");
        Check(!(bool)Member(recorder, "Enabled") && context.Posts == 0 && context.Sends == 0,
            "wrong-thread presenter action enabled capture or posted to a context");
    }

    private static void CapturesDoNotPushToSynchronizationContext()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object adapter = Adapter(controller);
        Check(Call(controller, "StartFromManualAction", 12L), "context probe start failed");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        ProbeContext context = new ProbeContext();
        Exception error = null;
        bool captured = false;
        Thread worker = new Thread(delegate()
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(context);
                bool before = Call(adapter, "BeforeFrame", 12L, simulation, loading);
                simulation.m_currentFrameIndex = 101;
                simulation.m_dayTimeFrame = 201;
                bool after = Call(adapter, "AfterFrame", 12L, simulation, loading);
                bool update = Call(adapter, "DisplayUpdate", 12L, simulation, loading);
                captured = before && after && update;
            }
            catch (Exception exception) { error = exception; }
        });
        worker.IsBackground = true;
        worker.Start();
        Check(worker.Join(5000), "context callback probe did not finish");
        Check(error == null && captured && context.Posts == 0 && context.Sends == 0,
            "fake capture pushed to a synchronization context or failed");
        Check(Call(controller, "StopFromManualAction"), "context probe stop was not confirmed");
    }

    private static void AbaRecordsRemainUnverified()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object adapter = Adapter(controller);
        Check(Call(controller, "StartFromManualAction", 13L), "ABA capture start failed");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        Check(Call(adapter, "BeforeFrame", 13L, simulation, loading), "ABA frame setup failed");
        simulation.m_currentFrameIndex = 101;
        simulation.m_dayTimeFrame = 201;
        bool transitioned = false;
        simulation.OnNextRead = delegate {
            loading.m_currentlyLoading = true; loading.m_loadingComplete = false;
            loading.m_currentlyLoading = false; loading.m_loadingComplete = true; transitioned = true;
        };
        Check(Call(adapter, "AfterFrame", 13L, simulation, loading) && transitioned, "ABA frame endpoints were not exercised");
        transitioned = false;
        simulation.OnNextRead = delegate {
            loading.m_currentlyLoading = true; loading.m_loadingComplete = false;
            loading.m_currentlyLoading = false; loading.m_loadingComplete = true; transitioned = true;
        };
        Check(Call(adapter, "DisplayUpdate", 13L, simulation, loading) && transitioned, "ABA display endpoints were not exercised");
        Check(Call(controller, "StopFromManualAction"), "ABA capture stop failed");
        object snapshot = Invoke(controller, "TryReadSnapshot");
        Check(snapshot != null && Count(snapshot, "FrameCount") == 1 && Count(snapshot, "DisplayCount") == 1,
            "ABA observation snapshot is incomplete");
        Unverified(Invoke(snapshot, "FrameAt", 0), "ABA frame");
        Unverified(Invoke(snapshot, "DisplayAt", 0), "ABA display");
    }

    private static int Main(string[] args)
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            recorderType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticRecorder", false);
            adapterType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticGameAdapter", false);
            controllerType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticManualController", false);
            presenterType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticPanelPresenter", false);
            if (args.Length != 0 && args[0] == "presenter-red")
            {
                Check(presenterType != null, "presenter is missing; no managed refresh action exists");
                return 0;
            }
            Check(recorderType != null && adapterType != null, "prior diagnostic candidate is missing");
            Check(controllerType != null, "controller is missing; no normal-symbol manual control exists");
            if (args.Length != 0 && args[0] == "controller-red") return 0;
            Check(presenterType != null, "presenter is missing; no managed refresh action exists");
            DefaultDisabledUntilManualAction();
            SnapshotIsFiniteRelativeAndReadOnly();
            BusyStopAndReadRemainUnconfirmed();
            PanelShowsOnlyExplicitBoundedResults();
            PanelDoesNotClaimStopDuringBusyCapture();
            PresenterDoesNotActOnAnotherThread();
            CapturesDoNotPushToSynchronizationContext();
            AbaRecordsRemainUnverified();
            Console.WriteLine("PASS managed manual-control and presenter fake checks; no game UI or player data accessed");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
