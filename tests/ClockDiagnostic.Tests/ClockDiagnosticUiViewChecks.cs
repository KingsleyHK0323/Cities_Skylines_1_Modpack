// Signature: OpenAI-Codex_diagnostic_tests
// Fake UI-helper checks only; this does not establish a Unity UI thread or game behavior.
using System;
using System.Reflection;
using System.Threading;

internal static class ClockDiagnosticUiViewChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type recorderType;
    private static Type adapterType;
    private static Type controllerType;
    private static Type presenterType;
    private static Type viewType;
    private static bool allowLegacyAdapterForRed;

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Diagnostic UI view: " + message);
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

    private static bool Call(object target, string name, params object[] arguments) { return (bool)Invoke(target, name, arguments); }
    private static object Member(object target, string name)
    {
        PropertyInfo property = target.GetType().GetProperty(name, Members);
        if (property != null) return property.GetValue(target, null);
        FieldInfo field = target.GetType().GetField(name, Members);
        Check(field != null, "missing member " + name);
        return field.GetValue(target);
    }

    private static object Recorder() { return NewObject(recorderType); }
    private static object Adapter(object controller, object recorder)
    {
        MethodInfo factory = controller.GetType().GetMethod("CreateAdapter", Members);
        if (factory != null) return Invoke(controller, "CreateAdapter");
        Check(allowLegacyAdapterForRed, "normal-symbol controller adapter is missing");
        return NewObject(adapterType, recorder);
    }
    private static object Controller(object recorder) { return NewObject(controllerType, recorder); }
    private static object Presenter(object controller) { return NewObject(presenterType, controller); }
    private static object View(object presenter) { return NewObject(viewType, presenter); }
    private static bool Enabled(object recorder) { return (bool)Member(recorder, "Enabled"); }

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

    private static void RequireRevocation(object controller)
    {
        Check(controller.GetType().GetMethod("RevokeFromManualAction", Members) != null,
            "revocation capability is missing");
    }

    private static bool Revoke(object controller) { return Call(controller, "RevokeFromManualAction"); }

    private static void RequireHostRevocation(object view)
    {
        Check(view.GetType().GetMethod("RevokeFromHost", Members) != null,
            "host revocation capability is missing");
    }

    private static bool HostRevoke(object view) { return Call(view, "RevokeFromHost"); }

    private static void RecordOneFrame(object controller, object recorder, long generation)
    {
        object adapter = Adapter(controller, recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        Check(Before(adapter, generation, simulation, loading), "synthetic frame before failed");
        simulation.m_currentFrameIndex = 101;
        simulation.m_dayTimeFrame = 201;
        Check(After(adapter, generation, simulation, loading), "synthetic frame after failed");
    }

    private static bool HasNewResultCopy(FakeDiagnosticUiHelper helper, int priorCount)
    {
        for (int i = priorCount; i < helper.Field.HostTextCopies.Count; i++)
        {
            string text = helper.Field.HostTextCopies[i];
            if (text.Contains("Frame[") || text.Contains("Display[")) return true;
        }
        return false;
    }

    private static void InitializationAndEagerCallbacksStayDisabled()
    {
        object recorder = Recorder();
        object view = View(Presenter(Controller(recorder)));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        bool eagerStarted = false;
        helper.EagerText = true;
        helper.EagerButtons = true;
        helper.AfterEagerButton = delegate(string label) { if (Enabled(recorder)) eagerStarted = true; };
        Check(Call(view, "Show", helper, 71L), "valid fake view setup failed");
        Check(!eagerStarted && !Enabled(recorder), "registration-time helper callback started capture");
        Check(helper.Field.readOnly && !helper.Field.numericalOnly, "diagnostic field is not a read-only text display");
        Check(helper.ButtonRegistrations == 3, "expected three explicit actions");
    }

    private static void ExplicitActionsAndRelativeView()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object adapter = Adapter(controller, recorder);
        object view = View(Presenter(controller));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 72L), "view setup failed");
        Check(!Enabled(recorder), "view setup started capture");
        Check(helper.Click("Start diagnostic") && Enabled(recorder), "explicit start action did not start capture");
        int uiTouches = helper.Field.AccessCount;
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        Exception callbackError = null;
        bool sampled = false;
        Thread callback = new Thread(delegate()
        {
            try
            {
                bool before = Call(adapter, "BeforeFrame", 72L, simulation, loading);
                simulation.m_currentFrameIndex = 101;
                simulation.m_dayTimeFrame = 201;
                bool after = Call(adapter, "AfterFrame", 72L, simulation, loading);
                bool display = Call(adapter, "DisplayUpdate", 72L, simulation, loading);
                sampled = before && after && display;
            }
            catch (Exception error) { callbackError = error; }
        });
        callback.IsBackground = true;
        callback.Start();
        Check(callback.Join(5000) && callbackError == null && sampled, "synthetic callback failed");
        Check(helper.Field.AccessCount == uiTouches && helper.Field.WrongThreadAccessCount == 0,
            "simulation callback accessed a UI field");
        Check(helper.Click("Stop diagnostic") && !Enabled(recorder), "explicit stop action did not stop capture");
        Check(helper.Click("Refresh diagnostic"), "refresh button was unavailable");
        string text = helper.Field.text;
        Check(text.Contains("Frame[") && text.Contains("Display["), "refresh omitted the two observation streams");
        Check(text.Contains("LifecycleContinuityVerified=False") && text.Contains("CityIdentityVerified=False")
            && text.Contains("ConcurrentCallbacksExcluded=False") && text.Contains("PhaseMayWrapOrJump=True"),
            "view omitted unverified conditions");
        string lower = text.ToLowerInvariant();
        Check(lower.Contains("host") && lower.Contains("retain") && lower.Contains("event") && lower.Contains("log"),
            "view omitted host copy, event or logging disclosure");
        Check(!text.Contains("m_currentGameTime") && !text.Contains("m_metaData") && !text.Contains("SaveGame"),
            "view displayed prohibited sources");
    }

    private static void ReentrantOrWrongThreadActionsDoNotStart()
    {
        object recorder = Recorder();
        object view = View(Presenter(Controller(recorder)));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 73L), "re-entry view setup failed");
        bool reentered = false;
        helper.Field.OnNextTextWrite = delegate { reentered = helper.Click("Start diagnostic"); };
        Check(helper.Click("Refresh diagnostic"), "refresh action missing");
        Check(reentered && !Enabled(recorder), "reentrant UI write started capture");
        int priorAccesses = helper.Field.AccessCount;
        Exception error = null;
        Thread other = new Thread(delegate()
        {
            try
            {
                helper.Click("Start diagnostic");
                helper.Click("Stop diagnostic");
                helper.Click("Refresh diagnostic");
            }
            catch (Exception exception) { error = exception; }
        });
        other.IsBackground = true;
        other.Start();
        Check(other.Join(5000) && error == null, "wrong-thread fake action failed unexpectedly");
        Check(!Enabled(recorder) && helper.Field.AccessCount == priorAccesses && helper.Field.WrongThreadAccessCount == 0,
            "wrong-thread action started capture or touched the UI field");

        recorder = Recorder();
        view = View(Presenter(Controller(recorder)));
        helper = new FakeDiagnosticUiHelper();
        object wrongThreadView = view;
        FakeDiagnosticUiHelper wrongThreadHelper = helper;
        bool shown = true;
        error = null;
        other = new Thread(delegate()
        {
            try { shown = Call(wrongThreadView, "Show", wrongThreadHelper, 73L); }
            catch (Exception exception) { error = exception; }
        });
        other.IsBackground = true;
        other.Start();
        Check(other.Join(5000) && error == null && !shown, "wrong-thread Show was accepted");
        Check(helper.Field.AccessCount == 0 && helper.ButtonRegistrations == 0,
            "wrong-thread Show touched the fake UI helper");
    }

    private static void UnusableUiDoesNotStart()
    {
        object recorder = Recorder();
        object view = View(Presenter(Controller(recorder)));
        Check(!Call(view, "Show", null, 74L), "null helper was accepted");
        FakeDiagnosticUiHelper missing = new FakeDiagnosticUiHelper();
        missing.NullField = true;
        Check(!Call(view, "Show", missing, 74L), "missing text field was accepted");
        Check(!Enabled(recorder), "missing UI started capture");

        recorder = Recorder();
        view = View(Presenter(Controller(recorder)));
        FakeDiagnosticUiHelper missingButton = new FakeDiagnosticUiHelper();
        missingButton.NullButtonAt = 2;
        Check(!Call(view, "Show", missingButton, 75L), "missing stop button was accepted");
        missingButton.Click("Start diagnostic");
        Check(!Enabled(recorder), "partial registration allowed start");

        recorder = Recorder();
        view = View(Presenter(Controller(recorder)));
        FakeDiagnosticUiHelper filtered = new FakeDiagnosticUiHelper();
        filtered.Field.Filter = delegate(string input) { return input.Length > 5 ? input.Substring(0, 5) : input; };
        Check(!Call(view, "Show", filtered, 76L), "filtered initial status was accepted");
        filtered.Click("Start diagnostic");
        Check(!Enabled(recorder), "filtered status allowed start");

        recorder = Recorder();
        view = View(Presenter(Controller(recorder)));
        FakeDiagnosticUiHelper writable = new FakeDiagnosticUiHelper();
        writable.Field.IgnoreReadOnly = true;
        Check(!Call(view, "Show", writable, 80L), "field that remains editable was accepted");
        writable.Click("Start diagnostic");
        Check(!Enabled(recorder), "editable display allowed start");
    }

    private static void TruncatedRefreshDoesNotPretendToDisplayResults()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object presenter = Presenter(controller);
        object view = View(presenter);
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 81L), "truncated refresh setup failed");
        Check(helper.Click("Start diagnostic") && Enabled(recorder), "truncated refresh start failed");
        object adapter = Adapter(controller, recorder);
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        Check(Before(adapter, 81, simulation, loading), "truncated refresh frame setup failed");
        simulation.m_currentFrameIndex = 101;
        simulation.m_dayTimeFrame = 201;
        Check(After(adapter, 81, simulation, loading), "truncated refresh frame completion failed");
        Check(Display(adapter, 81, simulation, loading), "truncated refresh display setup failed");
        Check(helper.Click("Stop diagnostic") && !Enabled(recorder), "truncated refresh stop failed");
        string full = (string)Invoke(presenter, "RefreshClicked");
        Check(full != null && full.Length > 80, "truncated refresh fixture is not long enough");
        helper.Field.Filter = delegate(string input) { return input.Length > 80 ? input.Substring(0, 80) : input; };
        Check(helper.Click("Refresh diagnostic"), "truncated refresh action missing");
        string shown = helper.Field.text;
        Check(!String.Equals(shown, full, StringComparison.Ordinal), "truncation fixture did not filter the result");
        string lower = shown.ToLowerInvariant();
        Check(lower.Contains("unavailable") || lower.Contains("not confirmed") || lower.Contains("incomplete"),
            "truncated result was not reported unavailable or unconfirmed");
    }

    private static void DisplayFailureDoesNotBlockStopOrClaimSuccess()
    {
        object recorder = Recorder();
        object view = View(Presenter(Controller(recorder)));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 77L), "display-failure setup failed");
        Check(helper.Click("Start diagnostic") && Enabled(recorder), "display-failure start failed");
        helper.Field.FailOnWrite = true;
        Check(helper.Click("Stop diagnostic"), "stop callback missing after display failure");
        Check(!Enabled(recorder), "failed display prevented manual stop");

        recorder = Recorder();
        view = View(Presenter(Controller(recorder)));
        helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 78L), "post-start filtering setup failed");
        object capturedRecorder = recorder;
        helper.Field.Filter = delegate(string input) { return Enabled(capturedRecorder) ? "filtered" : input; };
        Check(helper.Click("Start diagnostic"), "post-start filtering action missing");
        Check(!Enabled(recorder), "unreadable start status failed to stop the newly started capture");
    }

    private static void UnrecoverableDisplayBeforeStartNeverArms()
    {
        object recorder = Recorder();
        object view = View(Presenter(Controller(recorder)));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 82L), "pre-start filtering setup failed");
        object filteredRecorder = recorder;
        bool startedDuringFilter = false;
        helper.Field.Filter = delegate(string input)
        {
            if (Enabled(filteredRecorder)) startedDuringFilter = true;
            return input.Length > 5 ? input.Substring(0, 5) : input;
        };
        Check(helper.Click("Start diagnostic"), "pre-start filtered action missing");
        Check(!startedDuringFilter && !Enabled(recorder), "persistently filtered status allowed capture to start");

        recorder = Recorder();
        view = View(Presenter(Controller(recorder)));
        helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 83L), "pre-start unreadable setup failed");
        object unreadableRecorder = recorder;
        bool startedDuringWrite = false;
        helper.Field.OnNextTextWrite = delegate { if (Enabled(unreadableRecorder)) startedDuringWrite = true; };
        helper.Field.FailOnRead = true;
        Check(helper.Click("Start diagnostic"), "pre-start unreadable action missing");
        Check(!startedDuringWrite && !Enabled(recorder), "unreadable status allowed capture to start");

        recorder = Recorder();
        view = View(Presenter(Controller(recorder)));
        helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 84L), "pre-start unwritable setup failed");
        helper.Field.FailOnWrite = true;
        Check(helper.Click("Start diagnostic"), "pre-start unwritable action missing");
        Check(!Enabled(recorder), "unwritable status allowed capture to start");
    }

    private static void OversizedTextMarksDisplayUnavailable()
    {
        object recorder = Recorder();
        object presenter = Presenter(Controller(recorder));
        object view = View(presenter);
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 85L), "oversized-text setup failed");
        string before = helper.Field.text;
        Check(Call(presenter, "TryBeginViewPresentation"), "oversized-text presentation gate was not acquired");
        bool accepted;
        try { accepted = Call(view, "TrySetText", new string('x', 8192)); }
        finally { Invoke(presenter, "EndViewPresentation"); }
        Check(!accepted, "oversized text was accepted");
        string after = helper.Field.text.ToLowerInvariant();
        Check(!String.Equals(before, helper.Field.text, StringComparison.Ordinal), "oversized text left the old status visible");
        Check(after.Contains("unavailable") || after.Contains("incomplete"), "oversized text did not mark results unavailable");
    }

    private static void OversizedTextWithUnwritableFallbackCannotArm()
    {
        object recorder = Recorder();
        object presenter = Presenter(Controller(recorder));
        object view = View(presenter);
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 86L), "unwritable oversized-text setup failed");
        string before = helper.Field.text;
        helper.Field.FailOnWrite = true;
        Check(Call(presenter, "TryBeginViewPresentation"), "unwritable oversized-text presentation gate was not acquired");
        bool accepted;
        try { accepted = Call(view, "TrySetText", new string('x', 8192)); }
        finally { Invoke(presenter, "EndViewPresentation"); }
        Check(!accepted, "oversized text with unavailable field was accepted");
        Check(String.Equals(before, helper.Field.text, StringComparison.Ordinal), "unwritable fake field unexpectedly displayed a fallback");
        helper.Field.FailOnWrite = false;
        Check(helper.Click("Start diagnostic"), "start callback missing after unavailable fallback");
        Check(!Enabled(recorder), "unconfirmed fallback allowed capture to start");
    }

    private static void SetterPropertyMutationCannotConsumeManualStart(string property)
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object view = View(Presenter(controller));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 88L), "setter-property setup failed");
        bool changed = false;
        helper.Field.OnNextTextWrite = delegate
        {
            if (property == "readOnly") helper.Field.readOnly = false;
            else if (property == "numericalOnly") helper.Field.numericalOnly = true;
            else if (property == "maxLength") helper.Field.maxLength = 8191;
            else throw new InvalidOperationException("Unknown synthetic property.");
            changed = true;
        };
        Check(helper.Click("Start diagnostic") && changed, "setter-property mutation was not exercised");
        Check(!Enabled(recorder), "display-property mutation left capture running");
        Check(Call(controller, "StartFromManualAction", 89L), "display-property mutation consumed the one-shot start");
        Check(Call(controller, "StopFromManualAction"), "synthetic one-shot probe did not stop");
    }

    private static void NewViewDoesNotClaimSharedRecorderIsInactive()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object first = View(Presenter(controller));
        FakeDiagnosticUiHelper firstHelper = new FakeDiagnosticUiHelper();
        Check(Call(first, "Show", firstHelper, 90L), "first shared view setup failed");
        Check(firstHelper.Click("Start diagnostic") && Enabled(recorder), "first shared view did not start");
        object second = View(Presenter(controller));
        FakeDiagnosticUiHelper secondHelper = new FakeDiagnosticUiHelper();
        Check(Call(second, "Show", secondHelper, 91L), "second shared view setup failed");
        Check(Enabled(recorder), "showing a second view changed the active first session");
        string status = secondHelper.Field.text.ToLowerInvariant();
        Check(!status.Contains("inactive"), "second view falsely claims an active shared recorder is inactive");
        Check(firstHelper.Click("Stop diagnostic") && !Enabled(recorder), "first shared session did not stop");
    }

    private static void NewSharedViewHasNoConfirmedStop()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object first = View(Presenter(controller));
        FakeDiagnosticUiHelper firstHelper = new FakeDiagnosticUiHelper();
        Check(Call(first, "Show", firstHelper, 92L), "first shared-stop view setup failed");
        Check(firstHelper.Click("Start diagnostic") && Enabled(recorder), "first shared-stop view did not start");
        object second = View(Presenter(controller));
        FakeDiagnosticUiHelper secondHelper = new FakeDiagnosticUiHelper();
        Check(Call(second, "Show", secondHelper, 92L), "second shared-stop view setup failed");
        Check(Enabled(recorder), "second shared-stop view stopped the active session");
        Check((bool)Member(second, "StopNotConfirmed"), "new shared view falsely implies stop was confirmed");
        Check(firstHelper.Click("Stop diagnostic") && !Enabled(recorder), "shared-stop fixture did not stop");
    }

    private static void ForeignControllerCannotStopOwner()
    {
        object recorder = Recorder();
        object owner = Controller(recorder);
        object foreign = Controller(recorder);
        Check(Call(owner, "StartFromManualAction", 93L), "owner start failed");
        Check(!Call(foreign, "StopFromManualAction"), "foreign controller stopped the owner's session");
        Check(Enabled(recorder), "foreign controller changed owner state");
        Check(Call(owner, "StopFromManualAction"), "owner stop failed");
    }

    private static void FirstConstructedControllerOwnsBeforeStart()
    {
        object recorder = Recorder();
        object first = Controller(recorder);
        object later = Controller(recorder);
        Check(!Call(later, "StartFromManualAction", 111L), "later controller started before first owner");
        Check(!Enabled(recorder), "later controller changed the unstarted recorder");
        Check(Call(first, "StartFromManualAction", 111L), "first constructed controller could not start");
        Check(Call(first, "StopFromManualAction"), "first constructed controller could not stop");
    }

    private static void IdleStopDoesNotReleaseConstructionOwner()
    {
        object recorder = Recorder();
        object first = Controller(recorder);
        object later = Controller(recorder);
        Check(Call(first, "StopFromManualAction"), "first controller could not confirm idle stop");
        Check(!Call(later, "StartFromManualAction", 112L), "later controller started after first owner's idle stop");
        Check(!Enabled(recorder), "idle stop allowed a later controller to enable capture");
        Check(Call(first, "StartFromManualAction", 112L), "idle stop consumed the first owner's start");
        Check(Call(first, "StopFromManualAction"), "first owner could not stop after idle stop");
    }

    private static void PreStartRevokeDoesNotReleaseOldRecorder()
    {
        object recorder = Recorder();
        object first = Controller(recorder);
        object later = Controller(recorder);
        RequireRevocation(first);
        Check(Revoke(first), "unstarted first owner revoke was not confirmed");
        Check(!Call(first, "StartFromManualAction", 113L), "revoked first controller restarted old recorder");
        Check(!Call(later, "StartFromManualAction", 113L), "later controller started after first owner revoked");
        Check(!Enabled(recorder), "revoked old recorder became enabled");
        object freshRecorder = Recorder();
        object freshController = Controller(freshRecorder);
        Check(Call(freshController, "StartFromManualAction", 113L), "new recorder could not start after old revoke");
        Check(Call(freshController, "StopFromManualAction"), "new recorder could not stop after old revoke");
    }

    private static void ForeignStopDoesNotClaimBusy()
    {
        object recorder = Recorder();
        object ownerController = Controller(recorder);
        object foreignController = Controller(recorder);
        object ownerView = View(Presenter(ownerController));
        object foreignView = View(Presenter(foreignController));
        FakeDiagnosticUiHelper ownerHelper = new FakeDiagnosticUiHelper();
        FakeDiagnosticUiHelper foreignHelper = new FakeDiagnosticUiHelper();
        Check(Call(ownerView, "Show", ownerHelper, 114L), "owner view setup failed");
        Check(ownerHelper.Click("Start diagnostic") && Enabled(recorder), "owner view did not start");
        bool foreignShown = Call(foreignView, "Show", foreignHelper, 114L);
        if (foreignShown)
        {
            Check(foreignHelper.Click("Stop diagnostic"), "foreign stop callback was not registered");
            string status = foreignHelper.Field.text.ToLowerInvariant();
            Check(status.Contains("not confirmed"), "foreign stop did not report an unconfirmed result");
            Check(!status.Contains("busy"), "foreign stop incorrectly claims busy");
        }
        else foreignHelper.Click("Stop diagnostic");
        Check(Enabled(recorder), "foreign view stopped the active owner");
        Check(ownerHelper.Click("Stop diagnostic") && !Enabled(recorder), "owner view could not stop");
    }

    private static void ForeignViewCannotShowOrTouchUi()
    {
        object recorder = Recorder();
        object owner = Controller(recorder);
        object foreign = Controller(recorder);
        object foreignView = View(Presenter(foreign));
        FakeDiagnosticUiHelper foreignHelper = new FakeDiagnosticUiHelper();
        Check(!Call(foreignView, "Show", foreignHelper, 115L), "foreign view Show was accepted");
        Check(foreignHelper.Field.AccessCount == 0 && foreignHelper.ButtonRegistrations == 0,
            "foreign view Show touched the fake UI");
        Check(Call(owner, "StartFromManualAction", 115L), "owner could not start after foreign Show");
        Check(Call(owner, "StopFromManualAction"), "owner could not stop after foreign Show");
    }

    private static void ForeignControllerCannotReadOwner()
    {
        object recorder = Recorder();
        object owner = Controller(recorder);
        object foreign = Controller(recorder);
        Check(Call(owner, "StartFromManualAction", 94L), "owner-read start failed");
        RecordOneFrame(owner, recorder, 94);
        Check(Call(owner, "StopFromManualAction"), "owner-read stop failed");
        object ownerResult = Invoke(owner, "TryReadSnapshot");
        Check(ownerResult != null && Convert.ToInt32(Member(ownerResult, "FrameCount")) == 1,
            "owner did not retain its result");
        Check(Invoke(foreign, "TryReadSnapshot") == null, "foreign controller read the owner's result");
    }

    private static void ForeignAdapterCannotSampleOwner()
    {
        object recorder = Recorder();
        object owner = Controller(recorder);
        object foreign = Controller(recorder);
        object foreignAdapter = Adapter(foreign, recorder);
        Check(Call(owner, "StartFromManualAction", 103L), "foreign-adapter owner start failed");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        Check(!Before(foreignAdapter, 103L, simulation, loading), "foreign adapter sampled owner session");
        Check(simulation.ReadCount == 0 && loading.ReadCount == 0, "foreign adapter read owner session fields");
        Check(Call(owner, "StopFromManualAction"), "foreign-adapter owner stop failed");
    }

    private static void ForeignAdapterCannotInvalidateOwnerPending()
    {
        object recorder = Recorder();
        object owner = Controller(recorder);
        object foreign = Controller(recorder);
        object ownerAdapter = Adapter(owner, recorder);
        object foreignAdapter = Adapter(foreign, recorder);
        Check(Call(owner, "StartFromManualAction", 104L), "owner-pending start failed");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        Check(Before(ownerAdapter, 104L, simulation, loading), "owner pending frame did not begin");
        SimulationManager foreignSimulation = Simulation();
        LoadingManager foreignLoading = StableLoading();
        Check(!Before(foreignAdapter, 104L, foreignSimulation, foreignLoading), "foreign pending callback was accepted");
        Check(foreignSimulation.ReadCount == 0 && foreignLoading.ReadCount == 0, "foreign pending callback read fields");
        simulation.m_currentFrameIndex = 101;
        simulation.m_dayTimeFrame = 201;
        Check(After(ownerAdapter, 104L, simulation, loading), "foreign adapter invalidated owner's pending frame");
        Check(Call(owner, "StopFromManualAction"), "owner-pending stop failed");
        object snapshot = Invoke(owner, "TryReadSnapshot");
        Check(snapshot != null && Convert.ToInt32(Member(snapshot, "FrameCount")) == 1,
            "owner pending frame was not retained");
    }

    private static void ForeignAdapterCannotInvalidateHeldOwnerGate()
    {
        object recorder = Recorder();
        object owner = Controller(recorder);
        object foreign = Controller(recorder);
        object ownerAdapter = Adapter(owner, recorder);
        object foreignAdapter = Adapter(foreign, recorder);
        Check(Call(owner, "StartFromManualAction", 105L), "held-gate owner start failed");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.BlockOneRead();
        ManualResetEvent beforeReturned = new ManualResetEvent(false);
        ManualResetEvent finishPair = new ManualResetEvent(false);
        bool beforeAccepted = false;
        bool afterAccepted = false;
        Exception ownerError = null;
        Thread ownerThread = new Thread(delegate()
        {
            try
            {
                beforeAccepted = Before(ownerAdapter, 105L, simulation, loading);
                beforeReturned.Set();
                if (!finishPair.WaitOne(5000)) throw new InvalidOperationException("Synthetic owner-pair wait timed out.");
                simulation.m_currentFrameIndex = 101;
                simulation.m_dayTimeFrame = 201;
                afterAccepted = After(ownerAdapter, 105L, simulation, loading);
            }
            catch (Exception error) { ownerError = error; beforeReturned.Set(); }
        });
        ownerThread.IsBackground = true;
        ownerThread.Start();
        Check(simulation.ReadEntered.WaitOne(5000), "owner did not hold the gate in the fake read");
        SimulationManager foreignSimulation = Simulation();
        LoadingManager foreignLoading = StableLoading();
        bool foreignAccepted = Before(foreignAdapter, 105L, foreignSimulation, foreignLoading);
        simulation.ReleaseRead.Set();
        Check(beforeReturned.WaitOne(5000), "owner before callback did not return");
        finishPair.Set();
        Check(ownerThread.Join(5000) && ownerError == null, "owner pair thread did not finish");
        Check(!foreignAccepted && foreignSimulation.ReadCount == 0 && foreignLoading.ReadCount == 0,
            "foreign held-gate callback read or was accepted");
        Check(beforeAccepted && afterAccepted, "foreign concurrent callback invalidated owner capture");
        Check(Call(owner, "StopFromManualAction"), "held-gate owner stop failed");
        object snapshot = Invoke(owner, "TryReadSnapshot");
        Check(snapshot != null && Convert.ToInt32(Member(snapshot, "FrameCount")) == 1,
            "owner held-gate pair was not retained");
    }

    private static void ForeignRevokeCannotStopOwnerOrClaimCompletion()
    {
        object recorder = Recorder();
        object owner = Controller(recorder);
        object foreign = Controller(recorder);
        RequireRevocation(foreign);
        Check(Call(owner, "StartFromManualAction", 102L), "foreign-revoke owner start failed");
        RecordOneFrame(owner, recorder, 102);
        Check(!Revoke(foreign), "foreign revoke falsely claimed the active owner had stopped");
        Check(Enabled(recorder), "foreign revoke stopped the owner's session");
        Check(Invoke(foreign, "TryReadSnapshot") == null, "revoked foreign controller read the active owner");
        Check(Call(owner, "StopFromManualAction"), "foreign-revoke owner stop failed");
        Check(Revoke(foreign), "foreign revoke did not confirm after the owner stopped");
        Check(Invoke(foreign, "TryReadSnapshot") == null, "revoked foreign controller read the stopped owner");
        object ownerResult = Invoke(owner, "TryReadSnapshot");
        Check(ownerResult != null && Convert.ToInt32(Member(ownerResult, "FrameCount")) == 1,
            "foreign revoke changed the owner's retained result");
    }

    private static void RevokedOldViewCannotStart()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        RequireRevocation(controller);
        object view = View(Presenter(controller));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 95L), "pre-start revoke setup failed");
        Check(Revoke(controller), "idle pre-start revoke was not confirmed");
        helper.Click("Start diagnostic");
        Check(!Enabled(recorder), "revoked old view restarted capture");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.FailOnRead = true;
        loading.FailOnRead = true;
        Check(!Call(Adapter(controller, recorder), "BeforeFrame", 95L, simulation, loading), "revoked old view allowed sampling");
        Check(simulation.ReadCount == 0 && loading.ReadCount == 0, "revoked old view caused game reads");
    }

    private static void RevokedViewDoesNotSendPriorResults()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        RequireRevocation(controller);
        object view = View(Presenter(controller));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 96L), "stale-refresh setup failed");
        Check(helper.Click("Start diagnostic") && Enabled(recorder), "stale-refresh start failed");
        RecordOneFrame(controller, recorder, 96);
        Check(helper.Click("Stop diagnostic") && !Enabled(recorder), "stale-refresh stop failed");
        Check(helper.Click("Refresh diagnostic") && helper.Field.text.Contains("Frame["), "fixture did not display a prior result");
        int retained = helper.Field.HostTextCopies.Count;
        Check(Revoke(controller), "idle result revoke was not confirmed");
        helper.Click("Refresh diagnostic");
        Check(!HasNewResultCopy(helper, retained), "revoked refresh sent a previous result again");
        bool oldCopyExists = false;
        for (int i = 0; i < retained; i++) if (helper.Field.HostTextCopies[i].Contains("Frame[")) oldCopyExists = true;
        Check(oldCopyExists, "synthetic host did not retain the earlier displayed copy");
    }

    private static void RevokeDuringCaptureIsPendingAndSticky()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        RequireRevocation(controller);
        object adapter = Adapter(controller, recorder);
        Check(Call(controller, "StartFromManualAction", 97L), "capture-revoke start failed");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.BlockOneRead();
        Exception callbackError = null;
        Thread callback = new Thread(delegate()
        {
            try { Call(adapter, "BeforeFrame", 97L, simulation, loading); }
            catch (Exception error) { callbackError = error; }
        });
        callback.IsBackground = true;
        callback.Start();
        Check(simulation.ReadEntered.WaitOne(5000), "capture-revoke fake read did not begin");
        bool revokeResult = true;
        Exception revokeError = null;
        ManualResetEvent revokeReturned = new ManualResetEvent(false);
        Thread revoker = new Thread(delegate()
        {
            try { revokeResult = Revoke(controller); }
            catch (Exception error) { revokeError = error; }
            finally { revokeReturned.Set(); }
        });
        revoker.IsBackground = true;
        revoker.Start();
        bool returnedWhileBlocked = revokeReturned.WaitOne(2000);
        simulation.ReleaseRead.Set();
        Check(callback.Join(5000) && revoker.Join(5000), "capture-revoke fake threads did not finish");
        Check(returnedWhileBlocked && revokeError == null && callbackError == null && !revokeResult,
            "in-flight capture revoke blocked or falsely confirmed completion");
        simulation.FailOnRead = true;
        loading.FailOnRead = true;
        int reads = simulation.ReadCount;
        int loadingReads = loading.ReadCount;
        Check(!Call(adapter, "BeforeFrame", 97L, simulation, loading), "revoked callback sampled before revoke retry");
        Check(simulation.ReadCount == reads && loading.ReadCount == loadingReads,
            "revoked callback read game fields before revoke retry");
        Check(Invoke(controller, "TryReadSnapshot") == null, "pending revoked controller exposed a snapshot");
        Check(Revoke(controller), "capture revoke did not confirm after the in-flight callback");
        Check(!Enabled(recorder), "completed revoke did not stop the recorder");
    }

    private static void ReentrantSetterRevokeIsPendingAcrossViews()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        RequireRevocation(controller);
        object first = View(Presenter(controller));
        object second = View(Presenter(controller));
        FakeDiagnosticUiHelper firstHelper = new FakeDiagnosticUiHelper();
        FakeDiagnosticUiHelper secondHelper = new FakeDiagnosticUiHelper();
        Check(Call(first, "Show", firstHelper, 98L) && Call(second, "Show", secondHelper, 98L),
            "shared setter-revoke views did not initialize");
        Check(firstHelper.Click("Start diagnostic") && Enabled(recorder), "setter-revoke start failed");
        RecordOneFrame(controller, recorder, 98);
        Check(firstHelper.Click("Stop diagnostic") && !Enabled(recorder), "setter-revoke stop failed");
        bool invoked = false;
        bool firstResult = true;
        firstHelper.Field.OnNextTextWrite = delegate { invoked = true; firstResult = Revoke(controller); };
        Check(firstHelper.Click("Refresh diagnostic"), "setter-revoke refresh callback missing");
        Check(invoked && !firstResult, "reentrant setter revoke falsely confirmed completion");
        Check(Revoke(controller), "setter revoke did not confirm after the in-flight setter");
        int priorCopies = secondHelper.Field.HostTextCopies.Count;
        secondHelper.Click("Refresh diagnostic");
        Check(!HasNewResultCopy(secondHelper, priorCopies), "another view sent results after shared revoke");
    }

    private static void OtherThreadSetterRevokeIsPending(bool viaHost)
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object hostView = View(Presenter(controller));
        if (viaHost) RequireHostRevocation(hostView);
        else RequireRevocation(controller);
        ManualResetEvent ready = new ManualResetEvent(false);
        ManualResetEvent runRefresh = new ManualResetEvent(false);
        ManualResetEvent setterEntered = new ManualResetEvent(false);
        ManualResetEvent releaseSetter = new ManualResetEvent(false);
        Exception uiError = null;
        Thread ui = new Thread(delegate()
        {
            try
            {
                object view = View(Presenter(controller));
                FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
                Check(Call(view, "Show", helper, 101L), "other-thread setter view setup failed");
                Check(helper.Click("Start diagnostic") && Enabled(recorder), "other-thread setter start failed");
                RecordOneFrame(controller, recorder, 101);
                Check(helper.Click("Stop diagnostic") && !Enabled(recorder), "other-thread setter stop failed");
                helper.Field.OnNextTextWrite = delegate
                {
                    setterEntered.Set();
                    if (!releaseSetter.WaitOne(5000)) throw new InvalidOperationException("Synthetic setter release timed out.");
                };
                ready.Set();
                if (!runRefresh.WaitOne(5000)) throw new InvalidOperationException("Synthetic refresh signal timed out.");
                Check(helper.Click("Refresh diagnostic"), "other-thread setter refresh missing");
            }
            catch (Exception error) { uiError = error; ready.Set(); }
        });
        ui.IsBackground = true;
        ui.Start();
        Check(ready.WaitOne(5000) && uiError == null, "other-thread setter setup did not finish");
        runRefresh.Set();
        Check(setterEntered.WaitOne(5000), "other-thread setter did not become in-flight");
        bool revokeResult = true;
        Exception revokeError = null;
        ManualResetEvent revokeReturned = new ManualResetEvent(false);
        Thread revoker = new Thread(delegate()
        {
            try { revokeResult = viaHost ? HostRevoke(hostView) : Revoke(controller); }
            catch (Exception error) { revokeError = error; }
            finally { revokeReturned.Set(); }
        });
        revoker.IsBackground = true;
        revoker.Start();
        bool returnedWhileSetterBlocked = revokeReturned.WaitOne(2000);
        releaseSetter.Set();
        Check(ui.Join(5000) && revoker.Join(5000), "other-thread setter helpers did not finish");
        Check(returnedWhileSetterBlocked && revokeError == null && uiError == null && !revokeResult,
            "in-flight setter revoke blocked or falsely confirmed completion");
        Check(viaHost ? HostRevoke(hostView) : Revoke(controller), "other-thread setter revoke did not confirm after return");
        Check(Invoke(controller, "TryReadSnapshot") == null, "revoked controller exposed a snapshot");
    }

    private static void RevokeDuringStartSetterCannotArm()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        RequireRevocation(controller);
        object view = View(Presenter(controller));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 99L), "start-setter revoke setup failed");
        bool invoked = false;
        bool firstResult = true;
        helper.Field.OnNextTextWrite = delegate { invoked = true; firstResult = Revoke(controller); };
        Check(helper.Click("Start diagnostic"), "start-setter callback missing");
        Check(invoked && !firstResult, "start-setter revoke falsely confirmed completion");
        Check(!Enabled(recorder), "start continued after revoke during preflight setter");
        Check(Revoke(controller), "start-setter revoke did not confirm after the callback");
        helper.Click("Start diagnostic");
        Check(!Enabled(recorder), "revoked start callback recovered its capability");
    }

    private static void RevokeDuringShowCannotReviveOldView()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        RequireRevocation(controller);
        object view = View(Presenter(controller));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        helper.EagerText = true;
        bool invoked = false;
        bool firstResult = true;
        helper.AfterEagerText = delegate { invoked = true; firstResult = Revoke(controller); };
        Call(view, "Show", helper, 100L);
        Check(invoked && !firstResult, "in-flight Show revoke falsely confirmed completion");
        Check(Revoke(controller), "Show revoke did not confirm after helper returned");
        helper.Click("Start diagnostic");
        Check(!Enabled(recorder), "view initialized during revoke later recovered start capability");
    }

    private static void HostRevokeBeforeShowDoesNotTouchUi()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object view = View(Presenter(controller));
        RequireHostRevocation(view);
        Check(HostRevoke(view), "idle host revoke was not confirmed");
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(!Call(view, "Show", helper, 106L), "revoked view was shown");
        Check(helper.Field.AccessCount == 0 && helper.ButtonRegistrations == 0,
            "revoked Show touched the fake UI");
        helper.Click("Start diagnostic");
        Check(!Enabled(recorder), "revoked unshown view started capture");
    }

    private static void HostRevokeFromOtherThreadDoesNotTouchUi()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object first = View(Presenter(controller));
        object second = View(Presenter(controller));
        RequireHostRevocation(second);
        FakeDiagnosticUiHelper firstHelper = new FakeDiagnosticUiHelper();
        FakeDiagnosticUiHelper secondHelper = new FakeDiagnosticUiHelper();
        Check(Call(first, "Show", firstHelper, 107L) && Call(second, "Show", secondHelper, 107L),
            "host revoke views did not initialize");
        Check(firstHelper.Click("Start diagnostic") && Enabled(recorder), "host revoke start failed");
        RecordOneFrame(controller, recorder, 107);
        Check(firstHelper.Click("Stop diagnostic") && !Enabled(recorder), "host revoke stop failed");
        Check(firstHelper.Click("Refresh diagnostic") && firstHelper.Field.text.Contains("Frame["),
            "host revoke fixture produced no prior result");
        int firstAccess = firstHelper.Field.AccessCount;
        int secondAccess = secondHelper.Field.AccessCount;
        bool revoked = false;
        Exception error = null;
        Thread host = new Thread(delegate()
        {
            try { revoked = HostRevoke(second); }
            catch (Exception exception) { error = exception; }
        });
        host.IsBackground = true;
        host.Start();
        Check(host.Join(5000) && error == null && revoked, "other-thread host revoke was not confirmed");
        Check(firstHelper.Field.AccessCount == firstAccess && secondHelper.Field.AccessCount == secondAccess,
            "host revoke touched a fake UI field from another thread");
        Check(Invoke(controller, "TryReadSnapshot") == null, "host-revoked controller exposed a snapshot");
        int firstCopies = firstHelper.Field.HostTextCopies.Count;
        int secondCopies = secondHelper.Field.HostTextCopies.Count;
        firstHelper.Click("Refresh diagnostic");
        secondHelper.Click("Refresh diagnostic");
        Check(!HasNewResultCopy(firstHelper, firstCopies) && !HasNewResultCopy(secondHelper, secondCopies),
            "a shared view sent prior results after host revoke");
    }

    private static void HostRevokeDuringShowCannotReviveView()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object view = View(Presenter(controller));
        RequireHostRevocation(view);
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        helper.EagerText = true;
        bool invoked = false;
        bool firstResult = true;
        helper.AfterEagerText = delegate { invoked = true; firstResult = HostRevoke(view); };
        Call(view, "Show", helper, 108L);
        Check(invoked && !firstResult, "host revoke during Show falsely confirmed completion");
        Check(HostRevoke(view), "host revoke did not confirm after Show returned");
        helper.Click("Start diagnostic");
        Check(!Enabled(recorder), "host-revoked view later started capture");
    }

    private static void HostRevokeDuringStartSetterCannotArm()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object view = View(Presenter(controller));
        RequireHostRevocation(view);
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 110L), "host start-setter setup failed");
        bool invoked = false;
        bool firstResult = true;
        helper.Field.OnNextTextWrite = delegate { invoked = true; firstResult = HostRevoke(view); };
        Check(helper.Click("Start diagnostic"), "host start-setter callback missing");
        Check(invoked && !firstResult, "host revoke during start setter falsely confirmed completion");
        Check(!Enabled(recorder), "start continued after host revoke during preflight setter");
        Check(HostRevoke(view), "host start-setter revoke did not confirm after return");
        helper.Click("Start diagnostic");
        Check(!Enabled(recorder), "host-revoked start callback recovered its capability");
    }

    private static void HostRevokeDuringSharedSetterIsPending()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object first = View(Presenter(controller));
        object second = View(Presenter(controller));
        RequireHostRevocation(second);
        FakeDiagnosticUiHelper firstHelper = new FakeDiagnosticUiHelper();
        FakeDiagnosticUiHelper secondHelper = new FakeDiagnosticUiHelper();
        Check(Call(first, "Show", firstHelper, 109L) && Call(second, "Show", secondHelper, 109L),
            "host shared-setter views did not initialize");
        Check(firstHelper.Click("Start diagnostic") && Enabled(recorder), "host shared-setter start failed");
        RecordOneFrame(controller, recorder, 109);
        Check(firstHelper.Click("Stop diagnostic") && !Enabled(recorder), "host shared-setter stop failed");
        bool invoked = false;
        bool firstResult = true;
        firstHelper.Field.OnNextTextWrite = delegate { invoked = true; firstResult = HostRevoke(second); };
        Check(firstHelper.Click("Refresh diagnostic"), "host shared-setter refresh callback missing");
        Check(invoked && !firstResult, "host revoke during shared setter falsely confirmed completion");
        Check(HostRevoke(second), "host revoke did not confirm after shared setter returned");
        int firstCopies = firstHelper.Field.HostTextCopies.Count;
        int secondCopies = secondHelper.Field.HostTextCopies.Count;
        firstHelper.Click("Refresh diagnostic");
        secondHelper.Click("Refresh diagnostic");
        Check(!HasNewResultCopy(firstHelper, firstCopies) && !HasNewResultCopy(secondHelper, secondCopies),
            "host-revoked shared view sent prior results");
    }

    private static void BusyStopRemainsUnconfirmed()
    {
        object recorder = Recorder();
        object controller = Controller(recorder);
        object adapter = Adapter(controller, recorder);
        object view = View(Presenter(controller));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 79L), "busy-stop view setup failed");
        Check(helper.Click("Start diagnostic") && Enabled(recorder), "busy-stop start failed");
        SimulationManager simulation = Simulation();
        LoadingManager loading = StableLoading();
        simulation.BlockOneRead();
        Exception error = null;
        Thread callback = new Thread(delegate()
        {
            try { Call(adapter, "BeforeFrame", 79L, simulation, loading); }
            catch (Exception exception) { error = exception; }
        });
        callback.IsBackground = true;
        callback.Start();
        Check(simulation.ReadEntered.WaitOne(5000), "busy-stop callback did not enter fake read");
        ManualResetEvent actionReturned = new ManualResetEvent(false);
        Thread watchdog = new Thread(delegate() { if (!actionReturned.WaitOne(4000)) simulation.ReleaseRead.Set(); });
        watchdog.IsBackground = true;
        watchdog.Start();
        bool clicked = helper.Click("Stop diagnostic");
        string status = helper.Field.text.ToLowerInvariant();
        bool returnedBeforeRelease = !simulation.ReleaseRead.WaitOne(0);
        actionReturned.Set();
        simulation.ReleaseRead.Set();
        Check(callback.Join(5000) && watchdog.Join(5000) && error == null, "busy-stop helpers failed");
        Check(clicked && returnedBeforeRelease && Enabled(recorder), "busy stop blocked or falsely confirmed stop");
        Check(status.Contains("busy") || status.Contains("not confirmed"), "busy stop did not report uncertainty");
        Check(!status.Contains("inactive"), "busy stop status falsely also says inactive");
        Check(helper.Click("Stop diagnostic") && !Enabled(recorder), "manual retry did not confirm stop");
    }

    private static void ArmedStatusDoesNotSayInactive()
    {
        object recorder = Recorder();
        object view = View(Presenter(Controller(recorder)));
        FakeDiagnosticUiHelper helper = new FakeDiagnosticUiHelper();
        Check(Call(view, "Show", helper, 87L), "armed-status setup failed");
        Check(helper.Click("Start diagnostic") && Enabled(recorder), "armed-status start failed");
        string status = helper.Field.text.ToLowerInvariant();
        Check(!status.Contains("inactive"), "armed status falsely also says inactive");
    }

    private static int Main(string[] args)
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            allowLegacyAdapterForRed = args.Length != 0 && args[0].EndsWith("-red", StringComparison.Ordinal);
            recorderType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticRecorder", false);
            adapterType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticGameAdapter", false);
            controllerType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticManualController", false);
            presenterType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticPanelPresenter", false);
            viewType = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticUiView", false);
            Check(viewType != null, "UI view is missing; no standalone UI-helper view exists");
            if (args.Length != 0 && args[0] == "ui-view-red") return 0;
            Check(recorderType != null && adapterType != null && controllerType != null && presenterType != null,
                "prior diagnostic candidate is missing");
            if (args.Length != 0 && args[0] == "ui-prestart-check") { UnrecoverableDisplayBeforeStartNeverArms(); return 0; }
            if (args.Length != 0 && args[0] == "ui-oversize-red") { OversizedTextMarksDisplayUnavailable(); return 0; }
            if (args.Length != 0 && args[0] == "ui-oversize-unwritable-red") { OversizedTextWithUnwritableFallbackCannotArm(); return 0; }
            if (args.Length != 0 && args[0] == "ui-armed-status-red") { ArmedStatusDoesNotSayInactive(); return 0; }
            if (args.Length != 0 && args[0] == "ui-busy-status-red") { BusyStopRemainsUnconfirmed(); return 0; }
            if (args.Length != 0 && args[0] == "ui-setter-readonly-red") { SetterPropertyMutationCannotConsumeManualStart("readOnly"); return 0; }
            if (args.Length != 0 && args[0] == "ui-setter-numeric-red") { SetterPropertyMutationCannotConsumeManualStart("numericalOnly"); return 0; }
            if (args.Length != 0 && args[0] == "ui-setter-maxlength-red") { SetterPropertyMutationCannotConsumeManualStart("maxLength"); return 0; }
            if (args.Length != 0 && args[0] == "ui-shared-status-red") { NewViewDoesNotClaimSharedRecorderIsInactive(); return 0; }
            if (args.Length != 0 && args[0] == "ui-shared-stopflag-red") { NewSharedViewHasNoConfirmedStop(); return 0; }
            if (args.Length != 0 && args[0] == "ui-foreign-stop-red") { ForeignControllerCannotStopOwner(); return 0; }
            if (args.Length != 0 && args[0] == "ui-construction-owner-red") { FirstConstructedControllerOwnsBeforeStart(); return 0; }
            if (args.Length != 0 && args[0] == "ui-idle-stop-owner-red") { IdleStopDoesNotReleaseConstructionOwner(); return 0; }
            if (args.Length != 0 && args[0] == "ui-prestart-revoke-owner-red") { PreStartRevokeDoesNotReleaseOldRecorder(); return 0; }
            if (args.Length != 0 && args[0] == "ui-foreign-stop-status-red") { ForeignStopDoesNotClaimBusy(); return 0; }
            if (args.Length != 0 && args[0] == "ui-foreign-show-red") { ForeignViewCannotShowOrTouchUi(); return 0; }
            if (args.Length != 0 && args[0] == "ui-foreign-read-red") { ForeignControllerCannotReadOwner(); return 0; }
            if (args.Length != 0 && args[0] == "ui-foreign-adapter-red") { ForeignAdapterCannotSampleOwner(); return 0; }
            if (args.Length != 0 && args[0] == "ui-foreign-pending-red") { ForeignAdapterCannotInvalidateOwnerPending(); return 0; }
            if (args.Length != 0 && args[0] == "ui-foreign-held-gate-red") { ForeignAdapterCannotInvalidateHeldOwnerGate(); return 0; }
            if (args.Length != 0 && args[0] == "ui-foreign-revoke-red") { ForeignRevokeCannotStopOwnerOrClaimCompletion(); return 0; }
            if (args.Length != 0 && args[0] == "ui-revoke-before-red") { RevokedOldViewCannotStart(); return 0; }
            if (args.Length != 0 && args[0] == "ui-revoke-stale-red") { RevokedViewDoesNotSendPriorResults(); return 0; }
            if (args.Length != 0 && args[0] == "ui-revoke-capture-red") { RevokeDuringCaptureIsPendingAndSticky(); return 0; }
            if (args.Length != 0 && args[0] == "ui-revoke-same-setter-red") { ReentrantSetterRevokeIsPendingAcrossViews(); return 0; }
            if (args.Length != 0 && args[0] == "ui-revoke-other-setter-red") { OtherThreadSetterRevokeIsPending(false); return 0; }
            if (args.Length != 0 && args[0] == "ui-host-other-setter-red") { OtherThreadSetterRevokeIsPending(true); return 0; }
            if (args.Length != 0 && args[0] == "ui-revoke-preflight-red") { RevokeDuringStartSetterCannotArm(); return 0; }
            if (args.Length != 0 && args[0] == "ui-revoke-show-red") { RevokeDuringShowCannotReviveOldView(); return 0; }
            if (args.Length != 0 && args[0] == "ui-host-before-show-red") { HostRevokeBeforeShowDoesNotTouchUi(); return 0; }
            if (args.Length != 0 && args[0] == "ui-host-other-thread-red") { HostRevokeFromOtherThreadDoesNotTouchUi(); return 0; }
            if (args.Length != 0 && args[0] == "ui-host-show-red") { HostRevokeDuringShowCannotReviveView(); return 0; }
            if (args.Length != 0 && args[0] == "ui-host-preflight-red") { HostRevokeDuringStartSetterCannotArm(); return 0; }
            if (args.Length != 0 && args[0] == "ui-host-shared-setter-red") { HostRevokeDuringSharedSetterIsPending(); return 0; }
            InitializationAndEagerCallbacksStayDisabled();
            ExplicitActionsAndRelativeView();
            ReentrantOrWrongThreadActionsDoNotStart();
            UnusableUiDoesNotStart();
            TruncatedRefreshDoesNotPretendToDisplayResults();
            DisplayFailureDoesNotBlockStopOrClaimSuccess();
            UnrecoverableDisplayBeforeStartNeverArms();
            OversizedTextMarksDisplayUnavailable();
            OversizedTextWithUnwritableFallbackCannotArm();
            SetterPropertyMutationCannotConsumeManualStart("readOnly");
            SetterPropertyMutationCannotConsumeManualStart("numericalOnly");
            SetterPropertyMutationCannotConsumeManualStart("maxLength");
            NewViewDoesNotClaimSharedRecorderIsInactive();
            NewSharedViewHasNoConfirmedStop();
            ForeignControllerCannotStopOwner();
            FirstConstructedControllerOwnsBeforeStart();
            IdleStopDoesNotReleaseConstructionOwner();
            PreStartRevokeDoesNotReleaseOldRecorder();
            ForeignStopDoesNotClaimBusy();
            ForeignViewCannotShowOrTouchUi();
            ForeignControllerCannotReadOwner();
            ForeignAdapterCannotSampleOwner();
            ForeignAdapterCannotInvalidateOwnerPending();
            ForeignAdapterCannotInvalidateHeldOwnerGate();
            ForeignRevokeCannotStopOwnerOrClaimCompletion();
            RevokedOldViewCannotStart();
            RevokedViewDoesNotSendPriorResults();
            RevokeDuringCaptureIsPendingAndSticky();
            ReentrantSetterRevokeIsPendingAcrossViews();
            OtherThreadSetterRevokeIsPending(false);
            OtherThreadSetterRevokeIsPending(true);
            RevokeDuringStartSetterCannotArm();
            RevokeDuringShowCannotReviveOldView();
            HostRevokeBeforeShowDoesNotTouchUi();
            HostRevokeFromOtherThreadDoesNotTouchUi();
            HostRevokeDuringShowCannotReviveView();
            HostRevokeDuringStartSetterCannotArm();
            HostRevokeDuringSharedSetterIsPending();
            BusyStopRemainsUnconfirmed();
            ArmedStatusDoesNotSayInactive();
            Console.WriteLine("PASS standalone fake UI-helper view checks; no Unity UI or game was accessed");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
