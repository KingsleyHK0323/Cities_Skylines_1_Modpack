// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Standalone, default-disabled data prototype. No game extensions, file I/O or network.
using System;
using System.Globalization;
using System.Text;
using ICities;

namespace FullMapUnlockAndExpansion.LifecycleDiagnostics {
 public enum TraceComponent { Loading = 1, Areas = 2, Threading = 3 }
 public enum TraceKind { Created = 1, Loaded = 2, ProviderBound = 3, QueueRequested = 4,
  CallbackEntered = 5, OverlapOrReentry = 6, CallbackExited = 7, Unloading = 8,
  CallbackAfterUnload = 9, Released = 10, LateCallback = 11, Truncated = 12 }
 public enum TraceMode { None = 0, NewGame = 1, LoadGame = 2, Scenario = 3, Other = 4 }
 public sealed class TraceRecord {
  public readonly int Sequence, Instance, Generation, Queue;
  public readonly TraceKind Kind;
  public readonly TraceMode Mode;
  internal TraceRecord(int sequence, TraceKind kind, int instance, int generation, int queue, TraceMode mode) {
   Sequence = sequence; Kind = kind; Instance = instance; Generation = generation; Queue = queue; Mode = mode;
  }
 }
 // Tokens are local diagnostic labels, not game identities, capabilities or write authority.
 public sealed class TraceInstanceToken {
  internal readonly object Owner;
  internal readonly int Id;
  internal readonly TraceComponent Component;
  internal TraceGenerationToken Current;
  internal bool Released;
  internal int CallbackDepth;
  internal TraceInstanceToken(object owner, int id, TraceComponent component) { Owner = owner; Id = id; Component = component; }
 }
 public sealed class TraceGenerationToken {
  internal readonly object Owner;
  internal readonly int Id;
  internal bool Active;
  internal TraceGenerationToken(object owner, int id) { Owner = owner; Id = id; Active = true; }
 }
 public sealed class TraceQueueToken {
  internal readonly object Owner;
  internal readonly int Id;
  internal readonly TraceInstanceToken Thread;
  internal readonly TraceGenerationToken Generation;
  internal int Depth;
  internal TraceQueueToken(object owner, int id, TraceInstanceToken thread, TraceGenerationToken generation) {
   Owner = owner; Id = id; Thread = thread; Generation = generation;
  }
 }
 public sealed class LifecycleTraceCandidate {
  private const int RecordLimit = 64, InstanceLimit = 32, GenerationLimit = 32;
  private readonly object gate = new object(), owner = new object();
  private readonly TraceRecord[] records = new TraceRecord[RecordLimit];
  private bool enabled, overflowed;
  private int count, instances, generations, queues;
  public LifecycleTraceCandidate() { enabled = false; }
  // Synthetic tests only; no production entry point enables or wires this candidate.
  internal LifecycleTraceCandidate(bool enabledForSyntheticTests) { enabled = enabledForSyntheticTests; }
  public bool Enabled { get { lock (gate) { return enabled; } } }
  public bool Overflowed { get { lock (gate) { return overflowed; } } }
  public void Stop() { lock (gate) { enabled = false; } }
  private bool Add(TraceKind kind, int instance, int generation, int queue, TraceMode mode) {
   if (!enabled) return false;
   if (count == RecordLimit) { overflowed = true; enabled = false; return false; }
   records[count] = new TraceRecord(count + 1, kind, instance, generation, queue, mode); count++;
   return true;
  }
  private bool IsInstance(TraceInstanceToken value) { return value != null && object.ReferenceEquals(value.Owner, owner); }
  private bool IsGeneration(TraceGenerationToken value) { return value != null && object.ReferenceEquals(value.Owner, owner); }
  private bool IsQueue(TraceQueueToken value) { return value != null && object.ReferenceEquals(value.Owner, owner); }
  private static TraceMode ReduceMode(LoadMode mode) {
   if (mode == LoadMode.NewGame) return TraceMode.NewGame;
   if (mode == LoadMode.LoadGame) return TraceMode.LoadGame;
   if (mode == LoadMode.NewGameFromScenario) return TraceMode.Scenario;
   return TraceMode.Other;
  }
  public TraceInstanceToken Register(TraceComponent component) {
   lock (gate) {
    if (!enabled || !Enum.IsDefined(typeof(TraceComponent), component)) return null;
    if (instances == InstanceLimit) { overflowed = true; enabled = false; return null; }
    int id = instances + 1;
    if (!Add(TraceKind.Created, id, 0, 0, TraceMode.None)) return null;
    instances = id; return new TraceInstanceToken(owner, id, component);
   }
  }
  public TraceGenerationToken Loaded(TraceInstanceToken instance, LoadMode mode) {
   lock (gate) {
    if (!enabled || !IsInstance(instance) || instance.Component != TraceComponent.Loading) return null;
    if (instance.Released) { Add(TraceKind.LateCallback, instance.Id, 0, 0, TraceMode.None); return null; }
    if (instance.Current != null && instance.Current.Active) {
     Add(TraceKind.OverlapOrReentry, instance.Id, instance.Current.Id, 0, ReduceMode(mode)); return null;
    }
    if (generations == GenerationLimit) { overflowed = true; enabled = false; return null; }
    int id = generations + 1;
    if (!Add(TraceKind.Loaded, instance.Id, id, 0, ReduceMode(mode))) return null;
    generations = id; instance.Current = new TraceGenerationToken(owner, id); return instance.Current;
   }
  }
  public bool BindProvider(TraceInstanceToken instance, TraceGenerationToken generation) {
   lock (gate) {
    if (!enabled || !IsInstance(instance) || instance.Released || instance.Component != TraceComponent.Areas ||
        !IsGeneration(generation) || !generation.Active) return false;
    return Add(TraceKind.ProviderBound, instance.Id, generation.Id, 0, TraceMode.None);
   }
  }
  public TraceQueueToken Queue(TraceInstanceToken instance, TraceGenerationToken generation) {
   lock (gate) {
    if (!enabled || !IsInstance(instance) || instance.Released || instance.Component != TraceComponent.Threading ||
        !IsGeneration(generation) || !generation.Active) return null;
    int id = queues + 1;
    if (!Add(TraceKind.QueueRequested, instance.Id, generation.Id, id, TraceMode.None)) return null;
    queues = id; return new TraceQueueToken(owner, id, instance, generation);
   }
  }
  public bool Enter(TraceQueueToken queue) {
   lock (gate) {
    if (!enabled || !IsQueue(queue)) return false;
    TraceKind kind = queue.Thread.Released ? TraceKind.LateCallback : !queue.Generation.Active ? TraceKind.CallbackAfterUnload :
        queue.Thread.CallbackDepth > 0 ? TraceKind.OverlapOrReentry : TraceKind.CallbackEntered;
    if (!Add(kind, queue.Thread.Id, queue.Generation.Id, queue.Id, TraceMode.None)) return false;
    queue.Thread.CallbackDepth++; queue.Depth++; return true;
   }
  }
  public bool Exit(TraceQueueToken queue) {
   lock (gate) {
    if (!enabled || !IsQueue(queue) || queue.Depth == 0 || queue.Thread.CallbackDepth == 0) return false;
    if (!Add(TraceKind.CallbackExited, queue.Thread.Id, queue.Generation.Id, queue.Id, TraceMode.None)) return false;
    queue.Thread.CallbackDepth--; queue.Depth--; return true;
   }
  }
  public bool Unload(TraceInstanceToken instance) {
   lock (gate) {
    if (!enabled || !IsInstance(instance) || instance.Released || instance.Component != TraceComponent.Loading ||
        instance.Current == null || !instance.Current.Active) return false;
    if (!Add(TraceKind.Unloading, instance.Id, instance.Current.Id, 0, TraceMode.None)) return false;
    instance.Current.Active = false; return true;
   }
  }
  public bool Release(TraceInstanceToken instance) {
   lock (gate) {
    if (!enabled || !IsInstance(instance) || instance.Released) return false;
    int generation = instance.Current == null ? 0 : instance.Current.Id;
    if (!Add(TraceKind.Released, instance.Id, generation, 0, TraceMode.None)) return false;
    instance.Released = true; if (instance.Current != null) instance.Current.Active = false; return true;
   }
  }
  public TraceRecord[] Read() {
   lock (gate) { TraceRecord[] copy = new TraceRecord[count]; Array.Copy(records, copy, count); return copy; }
  }
  private static void Append(StringBuilder text, int sequence, TraceKind kind, int instance, int generation, int queue, TraceMode mode) {
   text.Append("{\"seq\":").Append(sequence.ToString(CultureInfo.InvariantCulture));
   text.Append(",\"event\":").Append(((int)kind).ToString(CultureInfo.InvariantCulture));
   text.Append(",\"instance\":").Append(instance.ToString(CultureInfo.InvariantCulture));
   text.Append(",\"generation\":").Append(generation.ToString(CultureInfo.InvariantCulture));
   text.Append(",\"queue\":").Append(queue.ToString(CultureInfo.InvariantCulture));
   text.Append(",\"mode\":").Append(((int)mode).ToString(CultureInfo.InvariantCulture)).Append("}\n");
  }
  // Produces an in-process copy only. Callers can retain or transfer it; there is no file or network API here.
  public byte[] ExportUtf8() {
   TraceRecord[] copy; bool truncated;
   lock (gate) { copy = new TraceRecord[count]; Array.Copy(records, copy, count); truncated = overflowed; }
   if (copy.Length == 0 && !truncated) return new byte[0];
   StringBuilder result = new StringBuilder();
   foreach (TraceRecord item in copy) Append(result, item.Sequence, item.Kind, item.Instance, item.Generation, item.Queue, item.Mode);
   if (truncated) Append(result, copy.Length + 1, TraceKind.Truncated, 0, 0, 0, TraceMode.None);
   return new UTF8Encoding(false).GetBytes(result.ToString());
  }
 }
}
