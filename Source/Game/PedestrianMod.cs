// Signature: OpenAI-Codex
using ICities;
namespace PedestrianTrafficControl {
    public sealed class PedestrianMod : IUserMod {
        internal static readonly PedestrianRuntime Runtime = new PedestrianRuntime();
        public string Name { get { return "Realistic Pedestrian Traffic Control"; } }
        public string Description { get { return "Inactive preview foundation. No pedestrian rules, signals, paths or markings are changed."; } }
        public void OnEnabled() { Dispatch(LifecycleEvent.Enable); }
        public void OnDisabled() { Dispatch(LifecycleEvent.Disable); }
        internal static void Dispatch(LifecycleEvent value) {
            LifecycleGuard.Run(delegate { Runtime.Apply(value); }, delegate { Runtime.Stop(); });
        }
    }
    public sealed class PedestrianLoading : ILoadingExtension {
        public void OnCreated(ILoading loading) { PedestrianMod.Dispatch(LifecycleEvent.Unload); }
        public void OnLevelLoaded(LoadMode mode) {
            bool city = mode == LoadMode.NewGame || mode == LoadMode.LoadGame;
            PedestrianMod.Dispatch(city ? LifecycleEvent.CityLoaded : LifecycleEvent.OtherLevelLoaded);
        }
        public void OnLevelUnloading() { PedestrianMod.Dispatch(LifecycleEvent.Unload); }
        public void OnReleased() { PedestrianMod.Dispatch(LifecycleEvent.Release); }
    }
}
