// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Review candidate only. It has no production activation or presentation path.
#if FULL_MAP_LIFECYCLE_DIAGNOSTIC && !RELEASE && !FULLMAP_RELEASE
using ICities;

namespace FullMapLifecycleCapture {
 internal struct CaptureEntry {
  internal readonly int Sequence, EventCode, Instance, Generation, Mode;
  internal CaptureEntry(int sequence, int eventCode, int instance, int generation, int mode) {
   Sequence = sequence; EventCode = eventCode; Instance = instance; Generation = generation; Mode = mode;
  }
 }

 // Local labels and callback order are observations, not authenticated game identities.
 internal sealed class CaptureBuffer {
  internal sealed class Handle {
   private readonly CaptureBuffer owner;
   private int epoch, instance, generation;
   internal Handle(CaptureBuffer owner) { this.owner = owner; }
   internal void Record(int eventCode, LoadMode mode) {
    owner.Record(ref epoch, ref instance, ref generation, eventCode, mode);
   }
  }
  private const int EntryLimit = 64, InstanceLimit = 32, GenerationLimit = 32;
  private readonly object gate = new object();
  private readonly CaptureEntry[] entries = new CaptureEntry[EntryLimit];
  private bool enabled = false, overflowed = false;
  private int epoch = 0, count = 0, nextInstance = 0, nextGeneration = 0;

  private static int ReduceMode(LoadMode mode) {
   if (mode == LoadMode.NewGame) return 1;
   if (mode == LoadMode.LoadGame) return 2;
   if (mode == LoadMode.NewGameFromScenario) return 3;
   return 4;
  }
  internal Handle NewHandle() { return new Handle(this); }
  private void Record(ref int seenEpoch, ref int instance, ref int generation, int eventCode, LoadMode mode) {
   lock (gate) {
    if (!enabled || overflowed || (eventCode != 1 && eventCode != 2 && eventCode != 3 && eventCode != 4)) return;
    if (count == EntryLimit) { enabled = false; overflowed = true; return; }
    if (seenEpoch != epoch) {
     if (nextInstance == InstanceLimit) { enabled = false; overflowed = true; return; }
     instance = ++nextInstance; seenEpoch = epoch; generation = 0;
    }
    if (eventCode == 2) {
     if (nextGeneration == GenerationLimit) { enabled = false; overflowed = true; return; }
     generation = ++nextGeneration;
    }
    int modeCode = eventCode == 2 ? ReduceMode(mode) : 0;
    entries[count] = new CaptureEntry(count + 1, eventCode, instance, generation, modeCode);
    count++;
   }
  }

#if FULL_MAP_LIFECYCLE_DIAGNOSTIC_TEST
  // These controls are absent from the reference-built candidate.
  internal bool BeginForSyntheticChecks() {
   lock (gate) {
    if (enabled || overflowed || epoch == int.MaxValue) return false;
    epoch++; enabled = true; return true;
   }
  }
  internal void StopForSyntheticChecks() { lock (gate) { enabled = false; } }
  internal bool OverflowedForSyntheticChecks { get { lock (gate) { return overflowed; } } }
  internal CaptureEntry[] SnapshotForSyntheticChecks() {
   lock (gate) {
    CaptureEntry[] copy = new CaptureEntry[count];
    System.Array.Copy(entries, copy, count); return copy;
   }
  }
#endif
 }

 internal static class CaptureRuntime {
  internal static readonly CaptureBuffer Buffer = new CaptureBuffer();
 }
 // This candidate is intentionally not an activation UI.
 public sealed class LifecycleCaptureMod : IUserMod {
  public string Name { get { return "Full Map Lifecycle Capture [Disabled Candidate]"; } }
  public string Description { get { return "Read-only lifecycle candidate without an activation or display path."; } }
 }
 // Implement the interface directly: no ILoading reference is retained or queried here.
 public sealed class LoadingCapture : ILoadingExtension {
  private readonly CaptureBuffer.Handle handle;
  public LoadingCapture() : this(CaptureRuntime.Buffer) { }
  internal LoadingCapture(CaptureBuffer buffer) { handle = buffer.NewHandle(); }
  public void OnCreated(ILoading loading) { handle.Record(1, default(LoadMode)); }
  public void OnLevelLoaded(LoadMode mode) { handle.Record(2, mode); }
  public void OnLevelUnloading() { handle.Record(3, default(LoadMode)); }
  public void OnReleased() { handle.Record(4, default(LoadMode)); }
 }
}
#endif
