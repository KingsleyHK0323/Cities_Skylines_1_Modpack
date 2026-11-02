// OpenAI-Codex_Intercity_Resource_Sharing
using ICities;

namespace IntercityResourceSharing
{
    public sealed class Mod : IUserMod
    {
        public string Name { get { return "Intercity Resource Sharing"; } }
        public string Description
        {
            get { return "Inactive foundation only. No resource exchange, settlement, or DevTool is enabled."; }
        }
    }

    public sealed class Loading : LoadingExtensionBase
    {
        private readonly Lifecycle lifecycle = new Lifecycle();
        internal bool IsActive { get { return lifecycle.Active; } }

        public override void OnLevelLoaded(LoadMode mode)
        {
            // Active means a playable lifecycle only, not an enabled feature.
            lifecycle.Begin(mode == LoadMode.NewGame || mode == LoadMode.LoadGame, null);
        }

        public override void OnLevelUnloading() { lifecycle.End(); }
        public override void OnReleased()
        {
            lifecycle.End();
            base.OnReleased();
        }
    }
}
