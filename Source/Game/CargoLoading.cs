// Signature: OpenAI-Codex_Realistic_Import_Export_Control
using ICities;

namespace RealisticImportExportControl
{
    public sealed class CargoLoading : LoadingExtensionBase
    {
        private CargoSession owned;
        private bool released;
        public override void OnLevelLoaded(LoadMode mode)
        {
            StopOwned();
            if (released) return;
            if (mode != LoadMode.NewGame && mode != LoadMode.LoadGame) return;
            owned = new CargoSession();
            if (owned.Start(null))
            {
                if (CargoRuntime.Current != null) CargoRuntime.Current.Stop();
                CargoRuntime.Current = owned;
            }
        }
        public override void OnLevelUnloading() { StopOwned(); }
        public override void OnReleased() { released = true; StopOwned(); }
        private void StopOwned()
        {
            if (owned == null) return;
            owned.Stop();
            if (object.ReferenceEquals(CargoRuntime.Current, owned)) CargoRuntime.Current = null;
            owned = null;
        }
    }
}
