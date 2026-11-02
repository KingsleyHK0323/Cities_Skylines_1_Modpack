// Signature: OpenAI-Codex_Realistic_Import_Export_Control
// Test-only API shapes; these do not prove compatibility with the real ICities assembly.
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
