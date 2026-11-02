// OpenAI-Codex_Intercity_Resource_Sharing
// Synthetic boundary only; never compile this file into the game DLL.
namespace ICities
{
    public enum LoadMode { NewGame, LoadGame, NewMap }
    public interface IUserMod { string Name { get; } string Description { get; } }
    public class LoadingExtensionBase
    {
        public virtual void OnLevelLoaded(LoadMode mode) { }
        public virtual void OnLevelUnloading() { }
        public virtual void OnReleased() { }
    }
}
