// Signature: OpenAI-Codex_Realistic_City_Time
// Update signature: OpenAI-Codex_lifecycle_impl
// Update signature: OpenAI-Codex
using System;
using System.Diagnostics;
using ColossalFramework;
using ICities;

namespace RealisticCityTime.Game
{
    public sealed class RealisticCityTimeMod : IUserMod
    {
        public string Name { get { return "Realistic City Time"; } }
        public string Description
        {
            get { return "Development observation-only foundation. Does not change game time or speed."; }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            new ClockRateSettingsUi(TemporaryClockRateHost.Settings).Show(helper);
        }
    }

    internal static class GameClockHost
    {
        internal static readonly GameClockState State = new GameClockState();

        // This is a conservative readiness check, not a city identity or atomic lifecycle proof.
        internal static bool LoadingIsStable()
        {
            try
            {
                if (!Singleton<LoadingManager>.exists) return false;
                LoadingManager loading = Singleton<LoadingManager>.instance;
                return loading != null && !loading.m_currentlyLoading && loading.m_loadingComplete;
            }
            catch (Exception) { return false; }
        }

        internal static void Capture()
        {
            long token = State.CurrentGeneration;
            if (token == 0) return;
            try
            {
                if (!LoadingIsStable() || !Singleton<SimulationManager>.exists)
                {
                    State.Publish(token, null, null, null, null);
                    return;
                }
                SimulationManager simulation = Singleton<SimulationManager>.instance;
                DateTime time = simulation.m_currentGameTime;
                bool paused = simulation.SimulationPaused || simulation.ForcedSimulationPaused;
                uint frame = simulation.m_currentFrameIndex;
                long counter = Stopwatch.GetTimestamp();
                long frequency = Stopwatch.Frequency;
                long? ticks = frequency > 0 && counter >= 0
                    ? (long?)checked((long)decimal.Truncate(counter * 10000000m / frequency)) : null;
                if (!LoadingIsStable())
                {
                    State.Publish(token, null, null, null, null);
                    return;
                }
                // Generation validation discards a callback whose city unloaded during these reads.
                State.Publish(token, time, paused, frame, ticks);
            }
            catch (Exception)
            {
                // A failed read must not retain a previous sample as if it were a fresh observation.
                State.Publish(token, null, null, null, null);
            }
        }
    }

    public sealed class RealisticCityTimeLoading : LoadingExtensionBase
    {
        private readonly object lifecycleGate = new object();
        private long lifecycleRevision;
        private bool released;
        private bool created;
        private bool awaitingCompletion;

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            lock (lifecycleGate)
            {
                if (!released)
                {
                    if (!created) { created = true; awaitingCompletion = true; }
                    GameClockHost.State.RegisterLoadingOwner(this);
                }
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            long revision;
            lock (lifecycleGate)
            {
                if (released || !GameClockHost.State.EndLoadingOwner(this)) return;
                if (lifecycleRevision == long.MaxValue)
                {
                    released = true;
                    GameClockHost.State.ReleaseLoadingOwner(this);
                    return;
                }
                revision = ++lifecycleRevision;
                // A second completion without a preceding unload has no trustworthy city identity.
                if (!awaitingCompletion) return;
            }
            if ((mode == LoadMode.NewGame || mode == LoadMode.LoadGame
                || mode == LoadMode.NewGameFromScenario || mode == LoadMode.LoadScenario)
                && GameClockHost.LoadingIsStable())
            {
                long token;
                lock (lifecycleGate)
                {
                    if (released || lifecycleRevision != revision) return;
                    token = GameClockHost.State.BeginLoadingOwner(this);
                    if (token != 0) awaitingCompletion = false;
                }
                if (token != 0 && !GameClockHost.LoadingIsStable())
                    GameClockHost.State.TryEndLoadingOwner(this, token);
                else if (token != 0)
                {
                    lock (lifecycleGate)
                    {
                        if (released || lifecycleRevision != revision) return;
                        GameClockHost.State.ConfirmLoadingOwner(this, token);
                    }
                }
            }
        }

        public override void OnLevelUnloading()
        {
            lock (lifecycleGate)
            {
                GameClockHost.State.EndLoadingOwner(this);
                if (!released) awaitingCompletion = true;
                if (lifecycleRevision == long.MaxValue)
                {
                    released = true;
                    GameClockHost.State.ReleaseLoadingOwner(this);
                }
                else lifecycleRevision++;
            }
        }

        public override void OnReleased()
        {
            lock (lifecycleGate)
            {
                released = true;
                awaitingCompletion = false;
                GameClockHost.State.ReleaseLoadingOwner(this);
                if (lifecycleRevision != long.MaxValue) lifecycleRevision++;
            }
        }
    }

    public sealed class RealisticCityTimeThreading : ThreadingExtensionBase
    {
        public override void OnAfterSimulationTick() { GameClockHost.Capture(); }
    }
}
