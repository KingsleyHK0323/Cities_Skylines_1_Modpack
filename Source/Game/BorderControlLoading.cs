// Signature: OpenAI-Codex
using ICities;

namespace RealisticBorderControl.Game
{
    public sealed class BorderControlLoading : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            BorderControlLifecycle.Load(mode == LoadMode.NewGame || mode == LoadMode.LoadGame);
        }
        public override void OnLevelUnloading() { BorderControlLifecycle.Unload(); }
        public override void OnReleased() { BorderControlLifecycle.Unload(); }
    }
}
