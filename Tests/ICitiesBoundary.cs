// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Test boundary only. These types do not validate compatibility with real ICities.
namespace ICities {
 public enum LoadMode { NewGame, LoadGame, NewMap, NewGameFromScenario }
 public enum AppMode { Game, MapEditor, ScenarioEditor }
 public interface IUserMod { string Name { get; } string Description { get; } }
 public interface ILoading { bool loadingComplete { get; } AppMode currentMode { get; } }
 public interface IAreas { int unlockedAreaCount { get; } bool IsAreaUnlocked(int x, int z); void UnlockArea(int x, int z, bool requireMoney); }
 public interface IThreading { bool simulationPaused { get; } void QueueSimulationThread(System.Action action); }
 public class LoadingExtensionBase {
  public static bool FailOnCreated;
  public virtual void OnCreated(ILoading loading) { if (FailOnCreated) throw new System.InvalidOperationException("synthetic base creation failure"); }
  public virtual void OnLevelLoaded(LoadMode mode) { }
  public virtual void OnLevelUnloading() { }
  public virtual void OnReleased() { }
 }
 public class ThreadingExtensionBase {
  public virtual void OnCreated(IThreading threading) { }
  public virtual void OnReleased() { }
 }
 public class AreasExtensionBase {
  public virtual void OnCreated(IAreas areas) { }
  public virtual void OnReleased() { }
  public virtual bool OnCanUnlockArea(int x, int z, bool result) { return result; }
  public virtual int OnGetAreaPrice(uint ore, uint oil, uint forest, uint fertility, uint water, bool road, bool train, bool ship, bool plane, float flatness, int price) { return price; }
 }
}
