// Signature: OpenAI-Codex
// Synthetic lifecycle boundary only; never include this file in the game DLL.
namespace ICities
{
    public interface IUserMod { string Name { get; } string Description { get; } }
    public enum LoadMode { NewGame, LoadGame, NewMap }
    public class LoadingExtensionBase
    {
        public virtual void OnLevelLoaded(LoadMode mode) { }
        public virtual void OnLevelUnloading() { }
        public virtual void OnReleased() { }
    }
}
