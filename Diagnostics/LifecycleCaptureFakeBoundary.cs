// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic boundary only; it is not an ICities compatibility guarantee.
namespace ICities {
 public enum LoadMode { NewGame, LoadGame, NewMap, NewGameFromScenario }
 public enum AppMode { Game, MapEditor, ScenarioEditor }
 public interface ILoading { bool loadingComplete { get; } AppMode currentMode { get; } }
 public interface ILoadingExtension {
  void OnCreated(ILoading loading);
  void OnReleased();
  void OnLevelLoaded(LoadMode mode);
  void OnLevelUnloading();
 }
 public interface IUserMod { string Name { get; } string Description { get; } }
}
