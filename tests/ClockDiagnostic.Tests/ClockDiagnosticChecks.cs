// Signature: OpenAI-Codex_diagnostic_tests
// Reflection keeps the red harness compilable before the candidate exists.
using System;
using System.Reflection;
using System.Threading;

internal static class ClockDiagnosticChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type recorderType;
    private static Type adapterType;

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("Clock diagnostic: " + description);
    }

    private static object NewObject(Type type, params object[] arguments)
    {
        return Activator.CreateInstance(type, Members, null, arguments, null);
    }

    private static object Invoke(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(name, Members);
        Check(method != null, "missing candidate method " + name);
        try { return method.Invoke(target, arguments); }
        catch (TargetInvocationException error)
        {
            throw new InvalidOperationException("candidate method " + name + " threw", error.InnerException);
        }
    }

    private static object Member(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, Members);
        if (property != null) return property.GetValue(target, null);
        FieldInfo field = target.GetType().GetField(name, Members);
        Check(field != null, "missing candidate result member " + name);
        return field.GetValue(target);
    }

    private static bool Call(object target, string name, params object[] arguments)
    {
        return (bool)Invoke(target, name, arguments);
    }

    private static object NewRecorder()
    {
        return NewObject(recorderType);
    }

    private static object Armed(long generation)
    {
        object recorder = NewRecorder();
        Check(Call(recorder, "ArmForTest", generation), "test fixture could not arm");
        return recorder;
    }

    private static object Snapshot(object recorder) { return Invoke(recorder, "SnapshotForTest"); }
    private static int Count(object snapshot, string name) { return Convert.ToInt32(Member(snapshot, name)); }

    private static void LifecycleAndCityAreUnverified(object record, string description)
    {
        Check(!(bool)Member(record, "LifecycleContinuityVerified"), description + " falsely certifies continuous lifecycle");
        Check(!(bool)Member(record, "CityIdentityVerified"), description + " falsely certifies city identity");
        Check(!(bool)Member(record, "ConcurrentCallbacksExcluded"), description + " falsely excludes concurrent callbacks");
    }

    private static bool Before(object recorder, long generation, uint frame, long duration, uint phase, long timestamp, int thread)
    {
        return Call(recorder, "BeforeFrame", generation, frame, duration, phase, timestamp, thread);
    }

    private static bool After(object recorder, long generation, uint frame, long duration, uint phase, long timestamp, int thread)
    {
        return Call(recorder, "AfterFrame", generation, frame, duration, phase, timestamp, thread);
    }

    private static bool Display(object recorder, long generation, uint frame, float timer, float phaseHour, long duration, long timestamp, int thread)
    {
        return Call(recorder, "DisplayUpdate", generation, frame, timer, phaseHour, duration, timestamp, thread);
    }

    private static object Adapter(object recorder) { return NewObject(adapterType, recorder); }

    private static SimulationManager Simulation()
    {
        return new SimulationManager {
            m_currentFrameIndex = 100, m_dayTimeFrame = 200, m_timePerFrame = TimeSpan.FromTicks(50),
            m_referenceFrameIndex = 300, m_referenceTimer = 0.25f, m_currentDayTimeHour = 5.5f
        };
    }

    private static LoadingManager StableLoading()
    {
        return new LoadingManager { m_currentlyLoading = false, m_loadingComplete = true };
    }

    private static void DisabledAndInvalidSourcesAreNotRead()
    {
        object recorder = NewRecorder();
        object adapter = Adapter(recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.FailOnRead = true;
        loading.FailOnRead = true;
        Check(!Call(adapter, "BeforeFrame", 1L, simulation, loading), "disabled before-frame must decline");
        Check(!Call(adapter, "AfterFrame", 1L, simulation, loading), "disabled after-frame must decline");
        Check(!Call(adapter, "DisplayUpdate", 1L, simulation, loading), "disabled update must decline");
        Check(simulation.ReadCount == 0 && loading.ReadCount == 0, "disabled adapter read game fields");

        recorder = Armed(2);
        adapter = Adapter(recorder);
        loading = StableLoading();
        Check(!Call(adapter, "BeforeFrame", 2L, null, loading), "null simulation must be declined");
        simulation = Simulation();
        Check(!Call(adapter, "BeforeFrame", 2L, simulation, null), "null loading must be declined");
        Check(simulation.ReadCount == 0, "null loading must not read simulation fields");
        loading.m_currentlyLoading = true;
        Check(!Call(adapter, "BeforeFrame", 2L, simulation, loading), "unstable loading must be declined");
        Check(simulation.ReadCount == 0, "unstable loading must not read simulation fields");
    }

    private static void SeparateStreamsAndLiteralDeltas()
    {
        object recorder = Armed(7);
        Check(Before(recorder, 7, 1000, 50, 2000, 10000, 11), "before-frame should be accepted");
        Check(After(recorder, 7, 1001, 50, 2001, 10012, 11), "after-frame should complete the pair");
        object first = Snapshot(recorder);
        Check(Count(first, "FrameCount") == 1 && Count(first, "DisplayCount") == 0, "frame must not create display samples");
        object frame = Invoke(first, "FrameAt", 0);
        Check(Convert.ToInt64(Member(frame, "FrameDelta")) == 1, "frame delta must be derived from the pair");
        Check(Convert.ToInt64(Member(frame, "PhaseDelta")) == 1, "phase delta must be derived from the pair");
        Check(Convert.ToInt64(Member(frame, "PerFrameDeltaTicks")) == 0, "unchanged duration must have zero delta");
        Check(Convert.ToInt64(Member(frame, "ElapsedCounterTicks")) == 12, "frame elapsed counter must be relative");
        Check((bool)Member(frame, "PhaseMayWrapOrJump"), "ordinary frame phase must remain ambiguous");
        LifecycleAndCityAreUnverified(frame, "ordinary frame");

        Check(Display(recorder, 7, 5000, 0.25f, 5.0f, 50, 10100, 12), "first display baseline should be accepted");
        Check(Display(recorder, 7, 5001, 0.50f, 5.5f, 50, 10115, 12), "second display sample should be accepted");
        object both = Snapshot(recorder);
        Check(Count(both, "FrameCount") == 1 && Count(both, "DisplayCount") == 2, "display samples must stay in their own stream");
        object baseline = Invoke(both, "DisplayAt", 0);
        object display = Invoke(both, "DisplayAt", 1);
        Check(!(bool)Member(baseline, "HasDelta") && (bool)Member(display, "HasDelta"), "first display sample must be an unpaired baseline");
        Check(Convert.ToInt64(Member(display, "ReferenceFrameDelta")) == 1, "display reference-frame delta is wrong");
        Check(Math.Abs(Convert.ToDouble(Member(display, "ReferenceTimerDelta")) - 0.25) < 0.000001, "display timer delta is wrong");
        Check(Math.Abs(Convert.ToDouble(Member(display, "PhaseHourDelta")) - 0.5) < 0.000001, "display phase delta is wrong");
        Check((bool)Member(display, "PhaseMayWrapOrJump"), "ordinary display phase must remain ambiguous");
        Check(Convert.ToInt64(Member(display, "PerFrameDeltaTicks")) == 0, "display duration delta is wrong");
        Check(Convert.ToInt64(Member(display, "ElapsedCounterTicks")) == 15, "display elapsed counter must be relative");
        LifecycleAndCityAreUnverified(baseline, "ordinary display baseline");
        LifecycleAndCityAreUnverified(display, "ordinary display delta");
    }

    private static void EachStreamStopsAtEight()
    {
        object recorder = Armed(9);
        for (int i = 0; i < 8; i++)
        {
            uint frame = (uint)(100 + i);
            long time = 1000 + i * 2;
            Check(Before(recorder, 9, frame, 50, frame, time, 3), "bounded frame before declined early");
            Check(After(recorder, 9, frame + 1, 50, frame + 1, time + 1, 3), "bounded frame after declined early");
            Check(Display(recorder, 9, frame, 0.25f, 5.0f, 50, 2000 + i, 4), "bounded display declined early");
        }
        Check(!Before(recorder, 9, 500, 50, 500, 3000, 3), "ninth frame should not start");
        Check(!After(recorder, 9, 501, 50, 501, 3001, 3), "ninth frame should not be recorded");
        Check(!Display(recorder, 9, 500, 0.25f, 5.0f, 50, 3002, 4), "ninth display should be declined");
        object snapshot = Snapshot(recorder);
        Check(Count(snapshot, "FrameCount") == 8 && Count(snapshot, "DisplayCount") == 8, "bounded streams exceeded or missed their limits");
        object adapter = Adapter(recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.FailOnRead = true;
        loading.FailOnRead = true;
        Check(!Call(adapter, "BeforeFrame", 9L, simulation, loading), "full frame stream must decline before-frame");
        Check(!Call(adapter, "AfterFrame", 9L, simulation, loading), "full frame stream must decline after-frame");
        Check(!Call(adapter, "DisplayUpdate", 9L, simulation, loading), "full display stream must decline an update");
        Check(simulation.ReadCount == 0 && loading.ReadCount == 0, "full streams continued reading game fields");
    }

    private static void AmbiguousPairsAndInputsAreRejected()
    {
        object recorder = Armed(4);
        Check(Before(recorder, 4, 10, 50, 20, 100, 1), "re-entry setup failed");
        Check(!Before(recorder, 4, 10, 50, 20, 101, 1), "re-entry should not be accepted");
        Check(!After(recorder, 4, 11, 50, 21, 102, 1), "re-entry must invalidate the old pair");
        Check(Before(recorder, 4, 10, 50, 20, 110, 1), "thread mismatch setup failed");
        Check(!After(recorder, 4, 11, 50, 21, 111, 2), "cross-thread pair should not be accepted");
        Check(!After(recorder, 4, 11, 50, 21, 112, 1), "mismatched thread must invalidate the old pair");
        Check(Before(recorder, 4, 10, 50, 20, 120, 1), "generation mismatch setup failed");
        Check(!After(recorder, 5, 11, 50, 21, 121, 1), "wrong generation should not complete a pair");
        Check(!After(recorder, 4, 11, 50, 21, 122, 1), "wrong generation must invalidate the old pair");
        Check(Before(recorder, 4, 10, 50, 20, 130, 1), "counter regression setup failed");
        Check(!After(recorder, 4, 11, 50, 21, 129, 1), "decreasing counter should not complete a pair");
        Check(!Display(recorder, 4, 1, Single.NaN, 1.0f, 50, 140, 1), "NaN timer must be rejected");
        Check(!Display(recorder, 4, 1, 0.5f, Single.PositiveInfinity, 50, 141, 1), "infinite display hour must be rejected");
        object snapshot = Snapshot(recorder);
        Check(Count(snapshot, "FrameCount") == 0 && Count(snapshot, "DisplayCount") == 0, "ambiguous inputs produced records");
    }

    private static void WrapAndJumpAreNotReportedAsNormalProgress()
    {
        object recorder = Armed(12);
        Check(Before(recorder, 12, UInt32.MaxValue, 50, 10, 100, 1), "frame wrap setup failed");
        Check(!After(recorder, 12, 0, 50, 11, 101, 1), "frame wrap was accepted as an ordinary increment");
        Check(Count(Snapshot(recorder), "FrameCount") == 0, "frame wrap produced an ordinary frame record");

        Check(Before(recorder, 12, 20, 50, 65535, 110, 1), "phase wrap setup failed");
        Check(After(recorder, 12, 21, 50, 0, 111, 1), "phase wrap should retain an explicitly unknown observation");
        object wrapped = Invoke(Snapshot(recorder), "FrameAt", 0);
        Check((bool)Member(wrapped, "PhaseMayWrapOrJump"), "phase wrap was not marked unknown");
        Check(Convert.ToInt64(Member(wrapped, "PhaseDelta")) == -65535, "phase wrap lost its signed endpoint difference");
        Check(Before(recorder, 12, 21, 50, 100, 120, 1), "phase jump setup failed");
        Check(After(recorder, 12, 22, 50, 200, 121, 1), "phase jump should retain an explicitly unknown observation");
        object jumped = Invoke(Snapshot(recorder), "FrameAt", 1);
        Check((bool)Member(jumped, "PhaseMayWrapOrJump") && Convert.ToInt64(Member(jumped, "PhaseDelta")) == 100,
            "phase jump lost its endpoint difference or was reported as normal progress");

        Check(Display(recorder, 12, 30, 0.1f, 23.5f, 50, 130, 1), "display wrap baseline failed");
        Check(Display(recorder, 12, 31, 0.2f, 0.5f, 50, 131, 1), "display wrap observation failed");
        object display = Invoke(Snapshot(recorder), "DisplayAt", 1);
        Check((bool)Member(display, "PhaseMayWrapOrJump") && Convert.ToDouble(Member(display, "PhaseHourDelta")) == -23,
            "display phase wrap lost its signed endpoint difference or was reported as normal progress");
    }

    private static object OneFrameRecord(uint beforePhase, uint afterPhase)
    {
        object recorder = Armed(51);
        Check(Before(recorder, 51, 100, 50, beforePhase, 1000, 3), "phase frame setup failed");
        Check(After(recorder, 51, 101, 50, afterPhase, 1010, 3), "phase frame completion failed");
        object snapshot = Snapshot(recorder);
        Check(Count(snapshot, "FrameCount") == 1, "phase frame setup produced no record");
        return Invoke(snapshot, "FrameAt", 0);
    }

    private static object OneDisplayDelta(float beforeHour, float afterHour)
    {
        object recorder = Armed(52);
        Check(Display(recorder, 52, 200, 0.25f, beforeHour, 50, 2000, 4), "phase display baseline failed");
        Check(Display(recorder, 52, 201, 0.50f, afterHour, 50, 2010, 4), "phase display completion failed");
        object snapshot = Snapshot(recorder);
        Check(Count(snapshot, "DisplayCount") == 2, "phase display setup produced no delta record");
        return Invoke(snapshot, "DisplayAt", 1);
    }

    private static void OrdinaryFramePhaseIsNotCertified()
    {
        object frame = OneFrameRecord(100, 101);
        Check(Convert.ToInt64(Member(frame, "PhaseDelta")) == 1, "ordinary frame lost its endpoint difference");
        Check((bool)Member(frame, "PhaseMayWrapOrJump"), "ordinary frame phase must remain ambiguous");
    }

    private static void PositiveDisplayPhaseIsNotCertified()
    {
        object display = OneDisplayDelta(1.0f, 20.0f);
        Check(Convert.ToDouble(Member(display, "PhaseHourDelta")) == 19, "positive display lost its endpoint difference");
        Check((bool)Member(display, "PhaseMayWrapOrJump"), "positive display phase must remain ambiguous");
    }

    private static void NegativeFramePhaseRetainsEndpointDifference()
    {
        object frame = OneFrameRecord(65535, 0);
        Check(Convert.ToInt64(Member(frame, "PhaseDelta")) == -65535, "negative frame lost its signed endpoint difference");
        Check((bool)Member(frame, "PhaseMayWrapOrJump"), "negative frame phase must remain ambiguous");
    }

    private static void NegativeDisplayPhaseRetainsEndpointDifference()
    {
        object display = OneDisplayDelta(23.5f, 0.5f);
        Check(Convert.ToDouble(Member(display, "PhaseHourDelta")) == -23, "negative display lost its signed endpoint difference");
        Check((bool)Member(display, "PhaseMayWrapOrJump"), "negative display phase must remain ambiguous");
    }

    private static void FrameConcurrencyIsNotCertified()
    {
        object frame = OneFrameRecord(100, 101);
        Check(!(bool)Member(frame, "ConcurrentCallbacksExcluded"), "frame falsely excludes concurrent callbacks");
    }

    private static void DisplayConcurrencyIsNotCertified()
    {
        object display = OneDisplayDelta(1.0f, 2.0f);
        Check(!(bool)Member(display, "ConcurrentCallbacksExcluded"), "display falsely excludes concurrent callbacks");
    }

    private static void OutOfRangeDisplayRequiresFreshBaseline(float invalidHour)
    {
        object recorder = Armed(53);
        Check(Display(recorder, 53, 200, 0.25f, 1.0f, 50, 2000, 4), "range baseline setup failed");
        Check(!Display(recorder, 53, 201, 0.50f, invalidHour, 50, 2010, 4), "out-of-range display hour must be rejected");
        Check(Count(Snapshot(recorder), "DisplayCount") == 1, "out-of-range display produced a record");
        Check(Display(recorder, 53, 202, 0.75f, 2.0f, 50, 2020, 4), "valid display after range failure should be accepted");
        object snapshot = Snapshot(recorder);
        Check(Count(snapshot, "DisplayCount") == 2, "valid display after range failure was not recorded");
        object newBaseline = Invoke(snapshot, "DisplayAt", 1);
        Check(!(bool)Member(newBaseline, "HasDelta"), "out-of-range display must invalidate the old baseline");
    }

    private static void AdapterStopsReadingAfterCapacity()
    {
        object recorder = Armed(10);
        object adapter = Adapter(recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        for (int i = 0; i < 8; i++)
        {
            simulation.m_currentFrameIndex = (uint)(100 + i);
            simulation.m_dayTimeFrame = (uint)(200 + i);
            Check(Call(adapter, "BeforeFrame", 10L, simulation, loading), "adapter before-frame declined before capacity");
            simulation.m_currentFrameIndex = (uint)(101 + i);
            simulation.m_dayTimeFrame = (uint)(201 + i);
            Check(Call(adapter, "AfterFrame", 10L, simulation, loading), "adapter after-frame declined before capacity");
            simulation.m_referenceFrameIndex = (uint)(300 + i);
            simulation.m_currentDayTimeHour = 4.0f + i * 0.01f;
            Check(Call(adapter, "DisplayUpdate", 10L, simulation, loading), "adapter update declined before capacity");
        }
        object snapshot = Snapshot(recorder);
        Check(Count(snapshot, "FrameCount") == 8 && Count(snapshot, "DisplayCount") == 8, "adapter did not fill both streams");
        int reads = simulation.ReadCount;
        int loadingReads = loading.ReadCount;
        simulation.FailOnRead = true;
        loading.FailOnRead = true;
        Check(!Call(adapter, "BeforeFrame", 10L, simulation, loading), "full adapter accepted a ninth frame");
        Check(!Call(adapter, "AfterFrame", 10L, simulation, loading), "full adapter accepted an extra after-frame");
        Check(!Call(adapter, "DisplayUpdate", 10L, simulation, loading), "full adapter accepted a ninth update");
        Check(simulation.ReadCount == reads && loading.ReadCount == loadingReads, "full adapter read fields after eight observations");
    }

    private static void LoadingChangeDuringReadInvalidatesPair()
    {
        object recorder = Armed(3);
        object adapter = Adapter(recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        Check(Call(adapter, "BeforeFrame", 3L, simulation, loading), "stable before-frame failed");
        simulation.m_currentFrameIndex = 101;
        simulation.m_dayTimeFrame = 201;
        simulation.OnNextRead = delegate { loading.m_currentlyLoading = true; };
        Check(!Call(adapter, "AfterFrame", 3L, simulation, loading), "loading transition must reject the in-flight sample");
        loading.m_currentlyLoading = false;
        Check(!Call(adapter, "AfterFrame", 3L, simulation, loading), "an invalidated before-frame must not be reused");
        Check(Count(Snapshot(recorder), "FrameCount") == 0, "loading transition produced a frame record");
        simulation.OnNextRead = delegate { loading.m_loadingComplete = false; };
        Check(!Call(adapter, "DisplayUpdate", 3L, simulation, loading), "loading transition must reject the in-flight display sample");
        Check(Count(Snapshot(recorder), "DisplayCount") == 0, "loading transition produced a display record");
        loading.m_loadingComplete = true;
        simulation.FailOnRead = true;
        Check(!Call(adapter, "BeforeFrame", 3L, simulation, loading), "field read failure must be contained");
        simulation.FailOnRead = false;
        Check(!Call(adapter, "AfterFrame", 3L, simulation, loading), "failed before-frame must not be reused");
    }

    private static void LoadingAbaFrameIsNotCertified()
    {
        object recorder = Armed(31);
        object adapter = Adapter(recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        Check(Call(adapter, "BeforeFrame", 31L, simulation, loading), "ABA frame setup failed");
        simulation.m_currentFrameIndex = 101;
        simulation.m_dayTimeFrame = 201;
        bool transitioned = false;
        simulation.OnNextRead = delegate
        {
            loading.m_currentlyLoading = true;
            loading.m_loadingComplete = false;
            loading.m_currentlyLoading = false;
            loading.m_loadingComplete = true;
            transitioned = true;
        };
        Check(Call(adapter, "AfterFrame", 31L, simulation, loading), "ABA endpoint fixture should be accepted by endpoint checks");
        Check(transitioned, "ABA frame fixture did not change loading state");
        object snapshot = Snapshot(recorder);
        Check(Count(snapshot, "FrameCount") == 1, "ABA frame fixture produced no observable record");
        LifecycleAndCityAreUnverified(Invoke(snapshot, "FrameAt", 0), "ABA frame");
    }

    private static void LoadingAbaDisplayIsNotCertified()
    {
        object recorder = Armed(32);
        object adapter = Adapter(recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        bool transitioned = false;
        simulation.OnNextRead = delegate
        {
            loading.m_currentlyLoading = true;
            loading.m_loadingComplete = false;
            loading.m_currentlyLoading = false;
            loading.m_loadingComplete = true;
            transitioned = true;
        };
        Check(Call(adapter, "DisplayUpdate", 32L, simulation, loading), "ABA display fixture should be accepted by endpoint checks");
        Check(transitioned, "ABA display fixture did not change loading state");
        object snapshot = Snapshot(recorder);
        Check(Count(snapshot, "DisplayCount") == 1, "ABA display fixture produced no observable record");
        LifecycleAndCityAreUnverified(Invoke(snapshot, "DisplayAt", 0), "ABA display");
    }

    private static void StopDoesNotClaimCompletionWhileReading()
    {
        object recorder = Armed(6);
        object adapter = Adapter(recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.BlockOneRead();
        Exception callbackError = null;
        Thread callback = new Thread(delegate()
        {
            try { Call(adapter, "BeforeFrame", 6L, simulation, loading); }
            catch (Exception error) { callbackError = error; }
        });
        callback.IsBackground = true;
        callback.Start();
        Check(simulation.ReadEntered.WaitOne(5000), "callback never reached the controlled fake read");
        bool stopped = false;
        Exception stopError = null;
        ManualResetEvent returned = new ManualResetEvent(false);
        Thread stopper = new Thread(delegate()
        {
            try { stopped = Call(recorder, "StopForTest"); }
            catch (Exception error) { stopError = error; }
            finally { returned.Set(); }
        });
        stopper.IsBackground = true;
        stopper.Start();
        bool returnedWithoutWaitingForRead = returned.WaitOne(2000);
        int readsAtStop = simulation.ReadCount;
        int loadingReadsAtStop = loading.ReadCount;
        simulation.ReleaseRead.Set();
        Check(callback.Join(5000) && stopper.Join(5000), "controlled stop threads did not finish");
        Check(returnedWithoutWaitingForRead && stopError == null && callbackError == null, "stop blocked or threw during a callback");
        if (stopped)
        {
            Check(simulation.ReadCount == readsAtStop && loading.ReadCount == loadingReadsAtStop,
                "a confirmed stop allowed additional game reads");
            Check(Count(Snapshot(recorder), "FrameCount") == 0, "a confirmed stop allowed an in-flight record");
        }
        else Check(Call(recorder, "StopForTest"), "busy stop did not become confirmable after callback finished");
        int reads = simulation.ReadCount;
        int loadingReads = loading.ReadCount;
        Check(!Call(adapter, "BeforeFrame", 6L, simulation, loading), "stopped adapter accepted a callback");
        Check(simulation.ReadCount == reads && loading.ReadCount == loadingReads, "stopped adapter read game fields");
    }

    private static void SameRecordValues(object first, object second, string stream)
    {
        foreach (FieldInfo field in first.GetType().GetFields(Members))
        {
            Check(field.FieldType != typeof(DateTime) && field.FieldType != typeof(string), stream + " record contains date or text");
            Check(Object.Equals(field.GetValue(first), field.GetValue(second)), stream + " record exposes an absolute input: " + field.Name);
        }
    }

    private static void SameDeltasDoNotExposeAbsoluteInputs()
    {
        object first = Armed(8);
        object second = Armed(8);
        Check(Before(first, 8, 1000, 50, 2000, 10000, 1) && After(first, 8, 1001, 50, 2001, 10012, 1), "first privacy fixture failed");
        Check(Before(second, 8, 8000, 80, 9000, 50000, 1) && After(second, 8, 8001, 80, 9001, 50012, 1), "second privacy fixture failed");
        object a = Invoke(Snapshot(first), "FrameAt", 0);
        object b = Invoke(Snapshot(second), "FrameAt", 0);
        SameRecordValues(a, b, "frame");
        Check(Display(first, 8, 1000, 0.25f, 5.0f, 50, 10000, 1), "first display privacy baseline failed");
        Check(Display(first, 8, 1001, 0.50f, 5.5f, 50, 10012, 1), "first display privacy sample failed");
        Check(Display(second, 8, 8000, 0.50f, 17.0f, 80, 50000, 1), "second display privacy baseline failed");
        Check(Display(second, 8, 8001, 0.75f, 17.5f, 80, 50012, 1), "second display privacy sample failed");
        SameRecordValues(Invoke(Snapshot(first), "DisplayAt", 0), Invoke(Snapshot(second), "DisplayAt", 0), "display baseline");
        SameRecordValues(Invoke(Snapshot(first), "DisplayAt", 1), Invoke(Snapshot(second), "DisplayAt", 1), "display");
    }

    private static int Main(string[] args)
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            recorderType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticRecorder", false);
            Check(recorderType != null, "recorder is missing; no bounded observation candidate exists");
            if (args.Length != 0 && args[0] == "red") return 0;
            adapterType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticGameAdapter", false);
            Check(adapterType != null, "game adapter candidate is missing");
            if (args.Length != 0 && args[0] == "aba-frame-red") { LoadingAbaFrameIsNotCertified(); return 0; }
            if (args.Length != 0 && args[0] == "aba-display-red") { LoadingAbaDisplayIsNotCertified(); return 0; }
            if (args.Length != 0 && args[0] == "phase-frame-red") { OrdinaryFramePhaseIsNotCertified(); return 0; }
            if (args.Length != 0 && args[0] == "phase-display-red") { PositiveDisplayPhaseIsNotCertified(); return 0; }
            if (args.Length != 0 && args[0] == "phase-negative-frame-red") { NegativeFramePhaseRetainsEndpointDifference(); return 0; }
            if (args.Length != 0 && args[0] == "phase-negative-display-red") { NegativeDisplayPhaseRetainsEndpointDifference(); return 0; }
            if (args.Length != 0 && args[0] == "concurrency-frame-red") { FrameConcurrencyIsNotCertified(); return 0; }
            if (args.Length != 0 && args[0] == "concurrency-display-red") { DisplayConcurrencyIsNotCertified(); return 0; }
            if (args.Length != 0 && args[0] == "phase-range-negative-red") { OutOfRangeDisplayRequiresFreshBaseline(-0.25f); return 0; }
            if (args.Length != 0 && args[0] == "phase-range-24-red") { OutOfRangeDisplayRequiresFreshBaseline(24.0f); return 0; }
            if (args.Length != 0 && args[0] == "phase-range-above-red") { OutOfRangeDisplayRequiresFreshBaseline(25.0f); return 0; }
            DisabledAndInvalidSourcesAreNotRead();
            SeparateStreamsAndLiteralDeltas();
            EachStreamStopsAtEight();
            AmbiguousPairsAndInputsAreRejected();
            WrapAndJumpAreNotReportedAsNormalProgress();
            OrdinaryFramePhaseIsNotCertified();
            PositiveDisplayPhaseIsNotCertified();
            NegativeFramePhaseRetainsEndpointDifference();
            NegativeDisplayPhaseRetainsEndpointDifference();
            FrameConcurrencyIsNotCertified();
            DisplayConcurrencyIsNotCertified();
            OutOfRangeDisplayRequiresFreshBaseline(-0.25f);
            OutOfRangeDisplayRequiresFreshBaseline(24.0f);
            OutOfRangeDisplayRequiresFreshBaseline(25.0f);
            AdapterStopsReadingAfterCapacity();
            LoadingChangeDuringReadInvalidatesPair();
            LoadingAbaFrameIsNotCertified();
            LoadingAbaDisplayIsNotCertified();
            StopDoesNotClaimCompletionWhileReading();
            SameDeltasDoNotExposeAbsoluteInputs();
            Console.WriteLine("PASS bounded diagnostic fake checks; no actual game or player data accessed");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
