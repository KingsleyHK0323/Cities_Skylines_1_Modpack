// Signature: OpenAI-Codex_Realistic_Vehicle_Traffic_Control
using ICities;
using RealisticVehicleTrafficControl.Model;

namespace RealisticVehicleTrafficControl {
    public sealed class RealisticVehicleTrafficControlMod : IUserMod {
        public string Name { get { return "Realistic Vehicle Traffic Control"; } }
        public string Description {
            get { return "Development foundation. Traffic observation and control are disabled. No gameplay changes. By Kingsley."; }
        }
        public void OnDisabled() { GameSession.Lifecycle.Unload(null); }
    }

    internal static class GameSession {
        internal static readonly ModLifecycle Lifecycle = new ModLifecycle();
    }

    public sealed class VehicleTrafficLoadingExtension : LoadingExtensionBase {
        public override void OnLevelLoaded(LoadMode mode) {
            GameSession.Lifecycle.Load(delegate { NotifyBaseLoaded(mode); });
        }
        public override void OnLevelUnloading() {
            GameSession.Lifecycle.Unload(NotifyBaseUnloading);
        }
        private void NotifyBaseLoaded(LoadMode mode) { base.OnLevelLoaded(mode); }
        private void NotifyBaseUnloading() { base.OnLevelUnloading(); }
    }
}
