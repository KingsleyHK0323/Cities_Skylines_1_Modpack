// Signature: OpenAI-Codex_Wireless_Utilities_And_Global_Coverage
using System;
using ICities;

namespace WirelessUtilitiesAndGlobalCoverage
{
    public sealed class WirelessUtilitiesLoadingExtension : LoadingExtensionBase
    {
        private readonly ModuleLifecycle lifecycle;

        public WirelessUtilitiesLoadingExtension() : this(ReportStatus) { }

        // A caller may supply a non-engine reporting sink without changing lifecycle behavior.
        public WirelessUtilitiesLoadingExtension(Action<string> report)
        {
            lifecycle = new ModuleLifecycle(report);
        }

        public bool IsGameplayLoaded { get { return lifecycle.IsGameplayLoaded; } }
        public Guid SessionId { get { return lifecycle.SessionId; } }

        public override void OnCreated(ILoading loading)
        {
            lifecycle.LeaveGameplay();
            base.OnCreated(loading);
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame
                || mode == LoadMode.NewGameFromScenario)
            {
                lifecycle.EnterGameplay();
            }
            else
            {
                lifecycle.LeaveGameplay();
            }
        }

        public override void OnLevelUnloading()
        {
            lifecycle.LeaveGameplay();
        }

        public override void OnReleased()
        {
            lifecycle.LeaveGameplay();
            base.OnReleased();
        }

        private static void ReportStatus(string message)
        {
            UnityEngine.Debug.Log("[" + ModuleIdentity.DisplayName + "] " + message);
        }
    }
}
