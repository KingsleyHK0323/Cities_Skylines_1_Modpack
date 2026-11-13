// Signature: OpenAI-Codex
// Signature: OpenAI-Codex_lifecycle_tests
using System;
using System.Threading;
using ColossalFramework;
using ICities;
using RealisticCityTime.Game;

internal static class GameLoadingLifecycleTests
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception("Game loading fixture: " + message);
    }

    private static void OldInstanceCannotUnloadNewCity()
    {
        GameClockHost.State.End();
        RealisticCityTimeLoading oldLoader = new RealisticCityTimeLoading();
        RealisticCityTimeLoading newLoader = new RealisticCityTimeLoading();
        oldLoader.OnCreated(null);
        oldLoader.OnLevelLoaded(LoadMode.NewGame);
        long oldGeneration = GameClockHost.State.CurrentGeneration;
        Check(oldGeneration > 0, "old city becomes active");
        newLoader.OnCreated(null);
        Check(GameClockHost.State.CurrentGeneration == 0, "competing registered instances make ownership unknown");
        newLoader.OnLevelLoaded(LoadMode.LoadGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "competing instance cannot start observation");
        oldLoader.OnReleased();
        newLoader.OnLevelLoaded(LoadMode.LoadGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "one remaining conflicted instance is not automatically trusted");
        newLoader.OnReleased();
        RealisticCityTimeLoading replacement = new RealisticCityTimeLoading();
        replacement.OnCreated(null);
        replacement.OnLevelLoaded(LoadMode.LoadGame);
        long newGeneration = GameClockHost.State.CurrentGeneration;
        Check(newGeneration > oldGeneration, "new city receives a different generation");
        oldLoader.OnLevelUnloading();
        Check(GameClockHost.State.CurrentGeneration == newGeneration,
            "old instance unload invalidated new city generation");
        oldLoader.OnReleased();
        Check(GameClockHost.State.CurrentGeneration == newGeneration,
            "old instance release invalidated new city generation");
        oldLoader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == newGeneration,
            "old instance completion invalidated new city generation");
        oldLoader.OnLevelLoaded(LoadMode.Other);
        Check(GameClockHost.State.CurrentGeneration == newGeneration,
            "old instance unsupported mode invalidated new city generation");
        replacement.OnLevelUnloading();
        Check(GameClockHost.State.CurrentGeneration == 0, "owning instance can unload its city");
        replacement.OnReleased();
    }

    private static void LateRegistrationFailsClosed()
    {
        RealisticCityTimeLoading oldLoader = new RealisticCityTimeLoading();
        RealisticCityTimeLoading newLoader = new RealisticCityTimeLoading();
        newLoader.OnCreated(null);
        newLoader.OnLevelLoaded(LoadMode.LoadGame);
        long generation = GameClockHost.State.CurrentGeneration;
        Check(generation > 0, "new instance begins alone");
        oldLoader.OnCreated(null);
        Check(GameClockHost.State.CurrentGeneration == 0, "late competing registration invalidates observation");
        oldLoader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "ambiguous old instance cannot restart observation");
        oldLoader.OnReleased();
        newLoader.OnLevelLoaded(LoadMode.LoadGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "conflict remains closed after the old release");
        newLoader.OnReleased();
        RealisticCityTimeLoading replacement = new RealisticCityTimeLoading();
        replacement.OnCreated(null);
        replacement.OnLevelLoaded(LoadMode.LoadGame);
        Check(GameClockHost.State.CurrentGeneration > generation, "fresh registration after all releases can start");
        replacement.OnReleased();
    }

    private static void ReverseReleaseOrderDoesNotTrustOldInstance()
    {
        RealisticCityTimeLoading oldLoader = new RealisticCityTimeLoading();
        RealisticCityTimeLoading newLoader = new RealisticCityTimeLoading();
        oldLoader.OnCreated(null);
        oldLoader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration > 0, "old instance starts before competing registration");
        newLoader.OnCreated(null);
        newLoader.OnReleased();
        oldLoader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "old remaining instance is not trusted after conflict");
        oldLoader.OnReleased();
    }

    private static void SameInstanceNormalLifecycle()
    {
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        loader.OnCreated(null);
        loader.OnLevelLoaded(LoadMode.NewGame);
        long first = GameClockHost.State.CurrentGeneration;
        loader.OnCreated(null);
        Check(GameClockHost.State.CurrentGeneration == first, "duplicate registration is inert");
        loader.OnLevelUnloading();
        Check(GameClockHost.State.CurrentGeneration == 0, "normal unload clears its generation");
        loader.OnLevelLoaded(LoadMode.LoadGame);
        Check(GameClockHost.State.CurrentGeneration > first, "same loader starts a new generation");
        loader.OnReleased();
        Check(GameClockHost.State.CurrentGeneration == 0, "normal release clears its generation");
    }

    private static void DuplicateCompletionFailsClosed()
    {
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        loader.OnCreated(null);
        loader.OnLevelLoaded(LoadMode.NewGame);
        long original = GameClockHost.State.CurrentGeneration;
        Check(original > 0, "duplicate completion precondition");
        loader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "unexpected duplicate completion must fail closed");
        loader.OnLevelUnloading();
        loader.OnLevelLoaded(LoadMode.LoadGame);
        Check(GameClockHost.State.CurrentGeneration > original, "an unload followed by completion starts a new generation");
        loader.OnReleased();
    }

    private static void InFlightObservationCannotPublishIntoReplacement()
    {
        GameClockHost.State.End();
        long oldGeneration = GameClockHost.State.Begin();
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent release = new ManualResetEvent(false);
        SimulationManager simulation = new SimulationManager();
        simulation.m_currentGameTime = new DateTime(2026, 1, 1);
        simulation.m_currentFrameIndex = 1;
        simulation.EnteredPauseRead = entered;
        simulation.ContinuePauseRead = release;
        Singleton<SimulationManager>.exists = true;
        Singleton<SimulationManager>.instance = simulation;
        Thread worker = new Thread(delegate() { GameClockHost.Capture(); });
        worker.IsBackground = true;
        worker.Start();
        Check(entered.WaitOne(5000), "capture reached fake read barrier");
        long newGeneration = GameClockHost.State.Begin();
        Check(newGeneration > oldGeneration, "replacement generation started");
        release.Set();
        Check(worker.Join(5000), "in-flight capture returned");
        Check(!simulation.PauseWaitTimedOut, "fake capture barrier was released rather than timed out");
        Check(GameClockHost.State.CurrentGeneration == newGeneration && GameClockHost.State.Latest == null,
            "old in-flight capture cannot publish to new generation");
        simulation.EnteredPauseRead = null;
        GameClockHost.Capture();
        Check(GameClockHost.State.Latest != null && GameClockHost.State.Latest.Generation == newGeneration,
            "current fake observation can publish to the new generation");
        Singleton<SimulationManager>.exists = false;
        Singleton<SimulationManager>.instance = null;
        GameClockHost.State.End();
        entered.Close(); release.Close();
    }

    private static void FailedReplacementDoesNotRetainOldObservation()
    {
        GameClockState state = new GameClockState();
        long oldGeneration = state.Begin();
        Check(state.Publish(oldGeneration, new DateTime(2026, 1, 1), false, 1, 1), "old snapshot published");
        state.FailSnapshotForTest = true;
        bool failed = false;
        try { state.Publish(oldGeneration, new DateTime(2026, 1, 2), false, 2, 2); }
        catch (InvalidOperationException) { failed = true; }
        Check(failed && state.Latest == null, "failed replacement invalidates prior snapshot");
        state.FailSnapshotForTest = false;
        long replacement = state.Begin();
        Check(state.Publish(replacement, new DateTime(2026, 2, 1), false, 1, 3), "replacement snapshot published");
        GameClockSnapshot current = state.Latest;
        state.FailSnapshotForTest = true;
        Check(!state.Publish(oldGeneration, null, null, null, null), "old token is rejected before allocation");
        Check(object.ReferenceEquals(state.Latest, current), "old token cannot clear the replacement snapshot");
        state.FailSnapshotForTest = false;
        state.SetSequenceForTest(long.MaxValue);
        Check(!state.Publish(oldGeneration, null, null, null, null), "old token is rejected even after sequence exhaustion");
        Check(object.ReferenceEquals(state.Latest, current), "old token cannot clear the exhausted current generation");
        Check(!state.Publish(replacement, null, null, null, null), "exhausted sequence rejects publication");
        Check(state.Latest == null, "exhausted sequence does not retain a stale snapshot");
    }

    private static void LoadingFlagsRejectStaleCompletion()
    {
        GameClockHost.State.End();
        LoadingManager manager = Singleton<LoadingManager>.instance;
        manager.m_loadingComplete = false;
        manager.m_currentlyLoading = true;
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        loader.OnCreated(null);
        loader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "old completion during a new load is ignored");
        manager.m_loadingComplete = true;
        manager.m_currentlyLoading = false;
        loader.OnLevelLoaded(LoadMode.NewGame);
        long active = GameClockHost.State.CurrentGeneration;
        Check(active > 0, "stable completion can start observation");
        manager.m_loadingComplete = false;
        manager.m_currentlyLoading = true;
        GameClockHost.Capture();
        Check(GameClockHost.State.Latest != null && !GameClockHost.State.Latest.GameTime.HasValue,
            "capture during a load publishes unknown rather than a stale value");
        manager.m_loadingComplete = true;
        manager.m_currentlyLoading = false;
        loader.OnReleased();
        Check(GameClockHost.State.CurrentGeneration == 0, "release ends the owned generation");
        loader.OnCreated(null);
        loader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "late created callback cannot revive a released instance");
        Singleton<LoadingManager>.exists = false;
        RealisticCityTimeLoading noManager = new RealisticCityTimeLoading();
        noManager.OnCreated(null);
        noManager.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "missing loading manager fails closed");
        noManager.OnReleased();
        Singleton<LoadingManager>.exists = true;
        RealisticCityTimeLoading preReleased = new RealisticCityTimeLoading();
        preReleased.OnReleased();
        preReleased.OnCreated(null);
        preReleased.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "release before registration permanently retires the instance");
    }

    private static void ReleaseDuringCreatedDoesNotRegister()
    {
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent release = new ManualResetEvent(false);
        LoadingExtensionBase.EnteredCreated = entered;
        LoadingExtensionBase.ContinueCreated = release;
        Thread worker = new Thread(delegate() { loader.OnCreated(null); });
        worker.IsBackground = true;
        worker.Start();
        try
        {
            Check(entered.WaitOne(5000), "creation reached fake barrier");
            loader.OnReleased();
        }
        finally
        {
            release.Set();
            Check(worker.Join(5000), "delayed creation completed");
            LoadingExtensionBase.EnteredCreated = null;
            LoadingExtensionBase.ContinueCreated = null;
            entered.Close(); release.Close();
        }
        loader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "release during creation prevents registration");
        RealisticCityTimeLoading fresh = new RealisticCityTimeLoading();
        fresh.OnCreated(null);
        fresh.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration > 0, "released delayed creation left no ghost owner");
        fresh.OnReleased();
    }

    private static void UnloadDuringReadinessCheckDoesNotBegin()
    {
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        loader.OnCreated(null);
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent release = new ManualResetEvent(false);
        Singleton<LoadingManager>.EnteredExists = entered;
        Singleton<LoadingManager>.ContinueExists = release;
        Singleton<LoadingManager>.ExistsWaitTimedOut = false;
        Singleton<LoadingManager>.ExistsReadCount = 0;
        Singleton<LoadingManager>.ExistsBarrierAtRead = 1;
        Thread worker = new Thread(delegate() { loader.OnLevelLoaded(LoadMode.NewGame); });
        worker.IsBackground = true;
        worker.Start();
        try
        {
            Check(entered.WaitOne(5000), "readiness check reached fake barrier");
            loader.OnLevelUnloading();
        }
        finally
        {
            release.Set();
            Check(worker.Join(5000), "delayed completion returned");
            Singleton<LoadingManager>.EnteredExists = null;
            Singleton<LoadingManager>.ContinueExists = null;
            entered.Close(); release.Close();
        }
        Check(GameClockHost.State.CurrentGeneration == 0, "unload during readiness check prevents begin");
        Check(!Singleton<LoadingManager>.ExistsWaitTimedOut, "readiness barrier was released rather than timed out");
        loader.OnReleased();
    }

    private static void UnstablePostBeginRemainsClosedUntilUnload()
    {
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        loader.OnCreated(null);
        LoadingManager manager = Singleton<LoadingManager>.instance;
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent release = new ManualResetEvent(false);
        Singleton<LoadingManager>.EnteredExists = entered;
        Singleton<LoadingManager>.ContinueExists = release;
        Singleton<LoadingManager>.ExistsWaitTimedOut = false;
        Singleton<LoadingManager>.ExistsReadCount = 0;
        Singleton<LoadingManager>.ExistsBarrierAtRead = 2;
        Thread worker = new Thread(delegate() { loader.OnLevelLoaded(LoadMode.NewGame); });
        worker.IsBackground = true;
        worker.Start();
        try
        {
            Check(entered.WaitOne(5000), "post-begin readiness check reached fake barrier");
            Check(!(GameClockHost.State.CurrentGeneration > 0), "synthetic generation began before recheck");
            manager.m_currentlyLoading = true;
            manager.m_loadingComplete = false;
        }
        finally
        {
            release.Set();
            Check(worker.Join(5000), "post-begin recheck returned");
            Singleton<LoadingManager>.EnteredExists = null;
            Singleton<LoadingManager>.ContinueExists = null;
            entered.Close(); release.Close();
        }
        Check(!Singleton<LoadingManager>.ExistsWaitTimedOut, "post-begin barrier was released rather than timed out");
        Check(GameClockHost.State.CurrentGeneration == 0, "unstable recheck invalidates the new generation");
        manager.m_currentlyLoading = false;
        manager.m_loadingComplete = true;
        loader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration == 0, "without unload a second completion remains closed");
        loader.OnLevelUnloading();
        loader.OnLevelLoaded(LoadMode.NewGame);
        Check(GameClockHost.State.CurrentGeneration > 0, "a later unload permits a fresh completion");
        loader.OnReleased();
    }

    // These fixtures model only the local readiness gate, not real game lifecycle or city identity.
    private static void DuringBlockedSecondReadinessCheck(
        Action<RealisticCityTimeLoading> during, Action<RealisticCityTimeLoading> after)
    {
        GameClockHost.State.End();
        Singleton<LoadingManager>.exists = true;
        LoadingManager manager = new LoadingManager();
        manager.m_loadingComplete = true;
        manager.m_currentlyLoading = false;
        Singleton<LoadingManager>.instance = manager;
        Singleton<SimulationManager>.exists = true;
        Singleton<SimulationManager>.instance = new SimulationManager();
        Singleton<SimulationManager>.ExistsReadCount = 0;
        ManualResetEvent entered = new ManualResetEvent(false);
        ManualResetEvent release = new ManualResetEvent(false);
        Singleton<LoadingManager>.EnteredExists = entered;
        Singleton<LoadingManager>.ContinueExists = release;
        Singleton<LoadingManager>.ExistsWaitTimedOut = false;
        Singleton<LoadingManager>.ExistsReadCount = 0;
        Singleton<LoadingManager>.ExistsBarrierAtRead = 2;
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        loader.OnCreated(null);
        Exception workerError = null;
        Thread worker = new Thread(delegate()
        {
            try { loader.OnLevelLoaded(LoadMode.NewGame); }
            catch (Exception error) { workerError = error; }
        });
        worker.IsBackground = true;
        try
        {
            worker.Start();
            try
            {
                Check(entered.WaitOne(5000), "second readiness check reached fake barrier");
                during(loader);
            }
            finally
            {
                release.Set();
                Check(worker.Join(5000), "blocked readiness check returned");
                Singleton<LoadingManager>.EnteredExists = null;
                Singleton<LoadingManager>.ContinueExists = null;
                Singleton<LoadingManager>.ExistsBarrierAtRead = 0;
            }
            Check(workerError == null, "readiness worker threw");
            Check(!Singleton<LoadingManager>.ExistsWaitTimedOut, "second readiness barrier did not time out");
            if (after != null) after(loader);
        }
        finally
        {
            loader.OnReleased();
            Singleton<LoadingManager>.EnteredExists = null;
            Singleton<LoadingManager>.ContinueExists = null;
            Singleton<LoadingManager>.ExistsBarrierAtRead = 0;
            Singleton<SimulationManager>.exists = false;
            Singleton<SimulationManager>.instance = null;
            entered.Close();
            release.Close();
        }
    }

    internal static void RunPendingGenerationVisibility()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration == 0, "pending generation was exposed before recheck");
                Check(GameClockHost.State.Latest == null, "pending generation exposed a cached observation");
            },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration > 0, "stable recheck did not activate the generation");
            });
    }

    internal static void RunPendingCaptureReadIsolation()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader)
            {
                int loadingReads = Singleton<LoadingManager>.ExistsReadCount;
                int simulationReads = Singleton<SimulationManager>.ExistsReadCount;
                GameClockHost.Capture();
                Check(Singleton<LoadingManager>.ExistsReadCount == loadingReads,
                    "pending capture queried fake loading manager");
                Check(Singleton<SimulationManager>.ExistsReadCount == simulationReads,
                    "pending capture queried fake simulation manager");
                Check(GameClockHost.State.Latest == null, "pending capture published a cached observation");
            },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration > 0, "stable capture fixture did not activate");
                GameClockHost.Capture();
                GameClockSnapshot result = GameClockHost.State.Latest;
                Check(result != null && result.Generation == GameClockHost.State.CurrentGeneration,
                    "stable activation did not permit a current fake observation");
            });
    }

    internal static void RunPendingUnload()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader) { loader.OnLevelUnloading(); },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration == 0, "unload allowed an old pending activation");
                loader.OnLevelLoaded(LoadMode.NewGame);
                Check(GameClockHost.State.CurrentGeneration > 0, "a fresh completion after unload did not activate");
            });
    }

    internal static void RunPendingRelease()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader) { loader.OnReleased(); },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration == 0, "release allowed an old pending activation");
                loader.OnCreated(null);
                loader.OnLevelLoaded(LoadMode.NewGame);
                Check(GameClockHost.State.CurrentGeneration == 0, "released loader was revived");
            });
    }

    internal static void RunPendingCompetingOwner()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader)
            {
                RealisticCityTimeLoading competing = new RealisticCityTimeLoading();
                competing.OnCreated(null);
                competing.OnReleased();
            },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration == 0, "competing owner allowed an old pending activation");
                loader.OnLevelLoaded(LoadMode.NewGame);
                Check(GameClockHost.State.CurrentGeneration == 0, "owner conflict was silently cleared");
            });
    }

    internal static void RunPendingDuplicateCompletion()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader) { loader.OnLevelLoaded(LoadMode.NewGame); },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration == 0, "duplicate completion revived old pending activation");
            });
    }

    // This witness is deliberately not part of Run: the old implementation returns true.
    internal static bool PendingCaptureProducesFakeSnapshot()
    {
        bool exposed = false;
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader)
            {
                GameClockHost.Capture();
                GameClockSnapshot snapshot = GameClockHost.State.Latest;
                exposed = snapshot != null && snapshot.GameTime.HasValue
                    && snapshot.Paused.HasValue && snapshot.Frame.HasValue;
            },
            null);
        return exposed;
    }

    internal static void RunPendingDirectPublicationRejection()
    {
        GameClockState state = new GameClockState();
        object owner = new object();
        state.RegisterLoadingOwner(owner);
        try
        {
            long token = state.BeginLoadingOwner(owner);
            Check(token > 0, "pending publication fixture did not reserve a token");
            Check(!state.Publish(token, new DateTime(2026, 1, 1), false, 1, 1),
                "matching token published before readiness recheck");
            Check(state.Latest == null, "pending token left a published snapshot");
        }
        finally { state.ReleaseLoadingOwner(owner); }
    }

    internal static void RunPendingDirectEnd()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader) { GameClockHost.State.End(); },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration == 0, "direct End revived old pending activation");
            });
    }

    internal static void RunPendingOwnerReplacement()
    {
        RealisticCityTimeLoading replacement = null;
        long replacementGeneration = 0;
        try
        {
            DuringBlockedSecondReadinessCheck(
                delegate(RealisticCityTimeLoading loader)
                {
                    loader.OnReleased();
                    replacement = new RealisticCityTimeLoading();
                    replacement.OnCreated(null);
                    replacement.OnLevelLoaded(LoadMode.LoadGame);
                    replacementGeneration = GameClockHost.State.CurrentGeneration;
                    Check(replacementGeneration > 0, "replacement did not activate");
                },
                delegate(RealisticCityTimeLoading loader)
                {
                    Check(GameClockHost.State.CurrentGeneration == replacementGeneration,
                        "old pending completion invalidated or replaced the new owner");
                });
        }
        finally { if (replacement != null) replacement.OnReleased(); }
    }

    internal static void RunPendingRecheckException()
    {
        GameClockHost.State.End();
        Singleton<LoadingManager>.exists = true;
        LoadingManager manager = new LoadingManager();
        manager.m_loadingComplete = true;
        manager.m_currentlyLoading = false;
        Singleton<LoadingManager>.instance = manager;
        ManualResetEvent entered = new ManualResetEvent(false);
        Singleton<LoadingManager>.EnteredExists = entered;
        Singleton<LoadingManager>.ContinueExists = null;
        Singleton<LoadingManager>.ExistsWaitTimedOut = false;
        Singleton<LoadingManager>.ExistsReadCount = 0;
        Singleton<LoadingManager>.ExistsBarrierAtRead = 2;
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        loader.OnCreated(null);
        try
        {
            loader.OnLevelLoaded(LoadMode.NewGame);
            Check(entered.WaitOne(0), "throwing second readiness lookup was not reached");
            Check(GameClockHost.State.CurrentGeneration == 0, "readiness exception activated the generation");
            Singleton<LoadingManager>.EnteredExists = null;
            Singleton<LoadingManager>.ExistsBarrierAtRead = 0;
            loader.OnLevelLoaded(LoadMode.NewGame);
            Check(GameClockHost.State.CurrentGeneration == 0, "readiness exception allowed a duplicate completion");
            loader.OnLevelUnloading();
            loader.OnLevelLoaded(LoadMode.NewGame);
            Check(GameClockHost.State.CurrentGeneration > 0, "fresh completion after exception did not activate");
        }
        finally
        {
            loader.OnReleased();
            Singleton<LoadingManager>.EnteredExists = null;
            Singleton<LoadingManager>.ContinueExists = null;
            Singleton<LoadingManager>.ExistsBarrierAtRead = 0;
            entered.Close();
        }
    }

    internal static void RunPendingDirectBeginInvalidation()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.Begin() == 0, "direct Begin bypassed pending reservation");
                Check(GameClockHost.State.CurrentGeneration == 0 && GameClockHost.State.Latest == null,
                    "direct Begin exposed an observation while readiness was pending");
            },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration == 0,
                    "direct Begin allowed old pending activation");
                Check(GameClockHost.State.Latest == null, "direct Begin left a stale observation");
                loader.OnLevelUnloading();
                loader.OnLevelLoaded(LoadMode.NewGame);
                Check(GameClockHost.State.CurrentGeneration > 0,
                    "fresh completion after direct Begin did not activate");
            });
    }

    internal static void RunPendingDoubleDirectBeginIsolation()
    {
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader)
            {
                long first = GameClockHost.State.Begin();
                long second = GameClockHost.State.Begin();
                int loadingReads = Singleton<LoadingManager>.ExistsReadCount;
                int simulationReads = Singleton<SimulationManager>.ExistsReadCount;
                GameClockHost.Capture();
                GameClockSnapshot result = GameClockHost.State.Latest;
                LoadingManager manager = Singleton<LoadingManager>.instance;
                manager.m_currentlyLoading = true;
                manager.m_loadingComplete = false;
                Check(first == 0, "first direct Begin bypassed pending reservation");
                Check(second == 0, "second direct Begin activated a registered loading owner");
                Check(Singleton<LoadingManager>.ExistsReadCount == loadingReads,
                    "double direct Begin caused a fake loading lookup");
                Check(Singleton<SimulationManager>.ExistsReadCount == simulationReads,
                    "double direct Begin caused a fake simulation lookup");
                Check(result == null, "double direct Begin published a fake observation");
            },
            delegate(RealisticCityTimeLoading loader)
            {
                Check(GameClockHost.State.CurrentGeneration == 0,
                    "failed original recheck left a double-Begin generation active");
                Check(GameClockHost.State.Latest == null,
                    "failed original recheck retained a double-Begin observation");
            });
    }

    // This old-behavior witness is intentionally excluded from Run; fixed code returns false.
    internal static bool DoubleBeginExposesFakeSnapshotAfterFailedRecheck()
    {
        bool exposed = false;
        bool retainedAfterFailedRecheck = false;
        DuringBlockedSecondReadinessCheck(
            delegate(RealisticCityTimeLoading loader)
            {
                long first = GameClockHost.State.Begin();
                long second = GameClockHost.State.Begin();
                GameClockHost.Capture();
                GameClockSnapshot result = GameClockHost.State.Latest;
                exposed = first == 0 && second > 0 && result != null
                    && result.GameTime.HasValue && result.Paused.HasValue && result.Frame.HasValue;
                LoadingManager manager = Singleton<LoadingManager>.instance;
                manager.m_currentlyLoading = true;
                manager.m_loadingComplete = false;
            },
            delegate(RealisticCityTimeLoading loader)
            {
                retainedAfterFailedRecheck = GameClockHost.State.CurrentGeneration > 0
                    && GameClockHost.State.Latest != null;
            });
        return exposed && retainedAfterFailedRecheck;
    }

    internal static void RunDirectBeginWithoutLoadingOwner()
    {
        GameClockState state = new GameClockState();
        long first = state.Begin();
        Check(first > 0 && state.CurrentGeneration == first,
            "direct Begin without a loading owner did not activate");
        Check(state.Publish(first, new DateTime(2026, 1, 1), false, 1, 1),
            "direct Begin without a loading owner could not publish a fake observation");
        Check(state.Latest != null && state.Latest.Generation == first,
            "direct Begin without a loading owner did not retain its observation");
        state.End();
        Check(state.CurrentGeneration == 0 && state.Latest == null,
            "direct End without a loading owner did not deactivate");
        long second = state.Begin();
        Check(second > first && state.CurrentGeneration == second,
            "subsequent direct Begin without a loading owner did not activate");
        state.End();
    }

    internal static void RunDirectBeginCannotReplaceActiveOwner()
    {
        GameClockState state = new GameClockState();
        object owner = new object();
        state.RegisterLoadingOwner(owner);
        try
        {
            long token = state.BeginLoadingOwner(owner);
            Check(token > 0 && state.ConfirmLoadingOwner(owner, token),
                "active owner fixture did not activate");
            Check(state.Publish(token, new DateTime(2026, 1, 1), false, 1, 1),
                "active owner fixture could not publish");
            GameClockSnapshot before = state.Latest;
            Check(before != null, "active owner fixture did not retain its observation");
            long attempted = state.Begin();
            Check(attempted == 0, "direct Begin replaced an active registered owner");
            Check(state.CurrentGeneration == token && object.ReferenceEquals(state.Latest, before),
                "direct Begin changed the active owner's generation or observation");
        }
        finally { state.ReleaseLoadingOwner(owner); }
    }

    internal static void RunDirectBeginRejectedForCompetingOwners()
    {
        GameClockState state = new GameClockState();
        object first = new object();
        object second = new object();
        state.RegisterLoadingOwner(first);
        state.RegisterLoadingOwner(second);
        try
        {
            Check(state.Begin() == 0, "direct Begin bypassed competing registered owners");
            Check(state.CurrentGeneration == 0 && state.Latest == null,
                "competing owners exposed an observation");
            Check(state.ReleaseLoadingOwner(second), "second competing owner was not released");
            Check(state.ReleaseLoadingOwner(first), "first competing owner was not released");
            long token = state.Begin();
            Check(token > 0 && state.CurrentGeneration == token,
                "direct Begin after all owners released did not activate");
            state.End();
        }
        finally
        {
            state.ReleaseLoadingOwner(second);
            state.ReleaseLoadingOwner(first);
            state.End();
        }
    }

    internal static void RunDirectBeginRejectedForUnresolvedConflict()
    {
        GameClockState state = new GameClockState();
        object first = new object();
        object second = new object();
        state.RegisterLoadingOwner(first);
        state.RegisterLoadingOwner(second);
        try
        {
            Check(state.ReleaseLoadingOwner(second), "second conflicting owner was not released");
            Check(state.Begin() == 0, "direct Begin bypassed an unresolved owner conflict");
            Check(state.CurrentGeneration == 0 && state.Latest == null,
                "unresolved owner conflict exposed an observation");
            Check(state.ReleaseLoadingOwner(first), "first conflicting owner was not released");
            long token = state.Begin();
            Check(token > 0 && state.CurrentGeneration == token,
                "direct Begin after conflict owners released did not activate");
            state.End();
        }
        finally
        {
            state.ReleaseLoadingOwner(second);
            state.ReleaseLoadingOwner(first);
            state.End();
        }
    }

    // The callback interface supplies no city identity; these calls are an artificial ordering.
    internal static void ObserveSameLoaderLateCallbacks()
    {
        GameClockHost.State.End();
        Singleton<LoadingManager>.exists = true;
        LoadingManager manager = new LoadingManager();
        manager.m_loadingComplete = true;
        manager.m_currentlyLoading = false;
        Singleton<LoadingManager>.instance = manager;
        RealisticCityTimeLoading loader = new RealisticCityTimeLoading();
        loader.OnCreated(null);
        try
        {
            loader.OnLevelLoaded(LoadMode.NewGame);
            long first = GameClockHost.State.CurrentGeneration;
            Check(first > 0, "late-callback fixture did not activate its first generation");
            loader.OnLevelUnloading();
            loader.OnLevelLoaded(LoadMode.LoadGame);
            long second = GameClockHost.State.CurrentGeneration;
            Check(second > first, "late-callback fixture did not activate its second generation");

            // Simulate a delayed unload from the first generation; it carries no such token.
            loader.OnLevelUnloading();
            bool artificialUnloadClosedSecond = GameClockHost.State.CurrentGeneration == 0;
            // Simulate a later completion whose source city cannot be authenticated.
            loader.OnLevelLoaded(LoadMode.NewGame);
            bool uncorrelatedCompletionStartedNewGeneration =
                GameClockHost.State.CurrentGeneration > second;
            Console.WriteLine("INFO artificial same-loader callbacks: unloadClosedSecond="
                + artificialUnloadClosedSecond + " uncorrelatedCompletionStartedNewGeneration="
                + uncorrelatedCompletionStartedNewGeneration
                + "; no city identity or real callback ordering is established.");
        }
        finally { loader.OnReleased(); }
    }

    internal static void Run()
    {
        Singleton<LoadingManager>.exists = true;
        Singleton<LoadingManager>.instance = new LoadingManager();
        Singleton<LoadingManager>.instance.m_loadingComplete = true;
        OldInstanceCannotUnloadNewCity();
        LateRegistrationFailsClosed();
        ReverseReleaseOrderDoesNotTrustOldInstance();
        SameInstanceNormalLifecycle();
        DuplicateCompletionFailsClosed();
        InFlightObservationCannotPublishIntoReplacement();
        FailedReplacementDoesNotRetainOldObservation();
        LoadingFlagsRejectStaleCompletion();
        ReleaseDuringCreatedDoesNotRegister();
        UnloadDuringReadinessCheckDoesNotBegin();
        UnstablePostBeginRemainsClosedUntilUnload();
        RunPendingGenerationVisibility();
        RunPendingCaptureReadIsolation();
        RunPendingUnload();
        RunPendingRelease();
        RunPendingCompetingOwner();
        RunPendingDuplicateCompletion();
        RunPendingDirectPublicationRejection();
        RunPendingDirectEnd();
        RunPendingDirectBeginInvalidation();
        RunPendingDoubleDirectBeginIsolation();
        RunDirectBeginWithoutLoadingOwner();
        RunDirectBeginCannotReplaceActiveOwner();
        RunDirectBeginRejectedForCompetingOwners();
        RunDirectBeginRejectedForUnresolvedConflict();
        ObserveSameLoaderLateCallbacks();
        RunPendingOwnerReplacement();
        RunPendingRecheckException();
        Singleton<LoadingManager>.exists = false;
        Singleton<LoadingManager>.instance = null;
        Console.WriteLine("PASS game loading callback fixtures (no actual game runtime)");
    }
}
