// Signature: OpenAI-Codex_Realistic_City_First_Response
using ICities;

namespace RealisticCityFirstResponse
{
    public sealed class Mod : IUserMod
    {
        public string Name { get { return "Realistic City First Response"; } }
        public string Description
        {
            get { return "Foundation build: no dispatch, rain suppression or traffic changes are enabled."; }
        }
    }

    public sealed class LoadingExtension : LoadingExtensionBase
    {
        private readonly ObservationLifecycle lifecycle = new ObservationLifecycle();

        public override void OnLevelLoaded(LoadMode mode)
        {
            // No game state is captured or modified by this foundation entry point.
            lifecycle.Load(mode == LoadMode.NewGame || mode == LoadMode.LoadGame);
        }

        public override void OnLevelUnloading()
        {
            lifecycle.Unload();
        }

        public override void OnReleased()
        {
            lifecycle.Unload();
            base.OnReleased();
        }
    }
}
