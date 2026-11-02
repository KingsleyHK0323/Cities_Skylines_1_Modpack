// OpenAI-Codex_Full_Map_Unlock_And_Expansion
using ICities;

namespace FullMapUnlockAndExpansion {
 public sealed class Mod : IUserMod {
  public string Name { get { return "Full Map Unlock And Expansion [Development Preview]"; } }
  public string Description { get { return "Read-only area callback observation. Does not unlock or purchase land."; } }
 }
 // Values describe this callback position, not the final result after other extensions.
 public sealed class AreaObservation {
  public readonly long Generation;
  public readonly int? X, Z;
  public readonly bool? Eligibility;
  public readonly int? Price;
  internal AreaObservation(long generation, int? x, int? z, bool? eligibility, int? price) {
   Generation = generation; X = x; Z = z; Eligibility = eligibility; Price = price;
  }
 }
 public static class Observation {
  private static readonly object Gate = new object();
  private static long generation;
  private static bool active;
  private static object provider;
  private static object loadingOwner;
  private static AreaObservation latest;
  internal static void Bind(object owner) { lock (Gate) { provider = owner; Advance(); } }
  private static void Advance() { generation = generation == long.MaxValue ? 1 : generation + 1; latest = null; }
  internal static void Begin(object owner, bool playable) { lock (Gate) { loadingOwner = owner; Advance(); active = playable; } }
  internal static void End(object owner) { lock (Gate) { if (!object.ReferenceEquals(loadingOwner, owner)) return; active = false; loadingOwner = null; Advance(); } }
  internal static void Release(object owner) { lock (Gate) { if (object.ReferenceEquals(provider, owner)) { provider = null; active = false; Advance(); } } }
  internal static void Record(object owner, int? x, int? z, bool? eligibility, int? price) {
   lock (Gate) {
    if (!active || !object.ReferenceEquals(provider, owner)) return;
    latest = new AreaObservation(generation, x, z, eligibility, price);
   }
  }
  public static AreaObservation Read() { lock (Gate) { return active ? latest : null; } }
 }
 public sealed class Loading : LoadingExtensionBase {
  public override void OnLevelLoaded(LoadMode mode) { Observation.Begin(this, mode == LoadMode.NewGame || mode == LoadMode.LoadGame); }
  public override void OnLevelUnloading() { Observation.End(this); }
  public override void OnReleased() { Observation.End(this); base.OnReleased(); }
 }
 public sealed class Areas : AreasExtensionBase {
  public override void OnCreated(IAreas areas) { base.OnCreated(areas); Observation.Bind(this); }
  public override void OnReleased() { Observation.Release(this); base.OnReleased(); }
  public override bool OnCanUnlockArea(int x, int z, bool originalResult) {
   Observation.Record(this, x, z, originalResult, null);
   return originalResult;
  }
  public override int OnGetAreaPrice(uint ore, uint oil, uint forest, uint fertility, uint water,
      bool road, bool train, bool ship, bool plane, float landFlatness, int originalPrice) {
   // Price callbacks contain no tile identity; never associate with the prior eligibility callback.
   Observation.Record(this, null, null, null, originalPrice);
   return originalPrice;
  }
 }
}
