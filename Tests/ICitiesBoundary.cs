// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Test boundary only. These types do not validate compatibility with real ICities.
namespace ICities {
 public enum LoadMode { NewGame, LoadGame, NewMap }
 public interface IUserMod { string Name { get; } string Description { get; } }
 public interface IAreas { }
 public class LoadingExtensionBase {
  public virtual void OnLevelLoaded(LoadMode mode) { }
  public virtual void OnLevelUnloading() { }
  public virtual void OnReleased() { }
 }
 public class AreasExtensionBase {
  public virtual void OnCreated(IAreas areas) { }
  public virtual void OnReleased() { }
  public virtual bool OnCanUnlockArea(int x, int z, bool result) { return result; }
  public virtual int OnGetAreaPrice(uint ore, uint oil, uint forest, uint fertility, uint water, bool road, bool train, bool ship, bool plane, float flatness, int price) { return price; }
 }
}
