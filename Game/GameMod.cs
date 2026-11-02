// Signature: OpenAI-Codex_Realistic_City_Time
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
    }

    internal static class GameClockHost
    {
        internal static readonly GameClockState State = new GameClockState();

        internal static void Capture()
        {
            long token = State.CurrentGeneration;
            if (token == 0) return;
            try
            {
                if (!Singleton<SimulationManager>.exists)
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
        public override void OnLevelLoaded(LoadMode mode)
        {
            GameClockHost.State.End();
            if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame
                || mode == LoadMode.NewGameFromScenario || mode == LoadMode.LoadScenario)
                GameClockHost.State.Begin();
        }

        public override void OnLevelUnloading() { GameClockHost.State.End(); }
        public override void OnReleased() { GameClockHost.State.End(); }
    }

    public sealed class RealisticCityTimeThreading : ThreadingExtensionBase
    {
        public override void OnAfterSimulationTick() { GameClockHost.Capture(); }
    }
}
