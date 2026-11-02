// Signature: OpenAI-Codex_Realistic_Railway_Traffic_Control
using System;
using ICities;
using RealisticRailwayTrafficControl.Observation;
using RealisticRailwayTrafficControl.Runtime;

namespace RealisticRailwayTrafficControl.Game
{
    internal static class RailwayRuntime
    {
        private static readonly RailwayObservationSession session = new RailwayObservationSession();
        internal static readonly RailwayModLifecycle Lifecycle = new RailwayModLifecycle(BeginCity, session.Disable);

        private static void BeginCity()
        {
            if (!session.ActivateCity()) throw new InvalidOperationException("Local city epoch exhausted.");
            // No data source, update subscription, capture request, or AI patch is installed.
        }
    }

    public sealed class RailwayLoadingExtension : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            try { base.OnCreated(loading); }
            catch (Exception error) { RailwayRuntime.Lifecycle.RecordFailure("OnCreated", error); }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            // Other modes remain inactive. This shell does not claim editor or scenario support.
            RailwayRuntime.Lifecycle.TryLoad(mode == LoadMode.NewGame || mode == LoadMode.LoadGame);
        }

        public override void OnLevelUnloading()
        {
            RailwayRuntime.Lifecycle.TryUnload();
        }

        public override void OnReleased()
        {
            RailwayRuntime.Lifecycle.TryUnload();
            try { base.OnReleased(); }
            catch (Exception error) { RailwayRuntime.Lifecycle.RecordFailure("OnReleased", error); }
        }
    }
}
