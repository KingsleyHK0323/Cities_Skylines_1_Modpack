// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// OpenAI-Codex_build_review
// Standalone, default-disabled data prototype. No game extensions, file I/O or network.
using System;
using System.Globalization;
using System.Text;
using ICities;

namespace FullMapUnlockAndExpansion.LifecycleDiagnostics {
 public enum TraceComponent { Loading = 1, Areas = 2, Threading = 3 }
 public enum TraceKind { Created = 1, Loaded = 2, ProviderBound = 3, QueueRequested = 4,
  CallbackEntered = 5, OverlapOrReentry = 6, CallbackExited = 7, Unloading = 8,
  CallbackAfterUnload = 9, Released = 10, LateCallback = 11, Truncated = 12
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
  , QueueReturned = 13, QueueThrew = 14
#endif
 }
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
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
  internal bool OutcomeObserved;
#endif
  internal int Depth;
  internal TraceQueueToken(object owner, int id, TraceInstanceToken thread, TraceGenerationToken generation) {
   Owner = owner; Id = id; Thread = thread; Generation = generation;
  }
 }
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
 // Recorder-local callback labels are observations, not capabilities or identity.
 public sealed class TraceInvocationToken {
  internal readonly object Owner;
  internal readonly int Id;
  internal readonly TraceQueueToken Queue;
  internal bool Ended;
  internal TraceInvocationToken(object owner, int id, TraceQueueToken queue) {
   Owner = owner; Id = id; Queue = queue;
  }
 }
 public sealed class TraceObservationDetail {
  public readonly int Sequence, ThreadTag, Invocation;
  internal TraceObservationDetail(int sequence, int threadTag, int invocation) {
   Sequence = sequence; ThreadTag = threadTag; Invocation = invocation;
  }
 }
#endif
 public sealed class LifecycleTraceCandidate {
  private const int RecordLimit = 64, InstanceLimit = 32, GenerationLimit = 32;
  private readonly object gate = new object(), owner = new object();
  private readonly TraceRecord[] records = new TraceRecord[RecordLimit];
  private bool enabled, overflowed;
  private int count, instances, generations, queues;
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
  private const int ThreadTagLimit = 32, InvocationLimit = 64;
  private readonly int[] threadTags = new int[RecordLimit], invocationTags = new int[RecordLimit];
  private readonly object[] threadMarkers = new object[ThreadTagLimit];
  private int threadTagCount, invocations;
  [ThreadStatic] private static WeakReference threadMarker;
#endif
  public LifecycleTraceCandidate() { enabled = false; }
  // Synthetic tests only; no production entry point enables or wires this candidate.
  internal LifecycleTraceCandidate(bool enabledForSyntheticTests) { enabled = enabledForSyntheticTests; }
  public bool Enabled { get { lock (gate) { return enabled; } } }
  public bool Overflowed { get { lock (gate) { return overflowed; } } }
  public void Stop() { lock (gate) { enabled = false;
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
    Array.Clear(threadMarkers, 0, threadMarkers.Length);
#endif
 } }
  private bool Add(TraceKind kind, int instance, int generation, int queue, TraceMode mode) {
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
   return AddDetailed(kind, instance, generation, queue, mode, 0);
#else

   if (!enabled) return false;
   if (count == RecordLimit) { overflowed = true; enabled = false; return false; }
   records[count] = new TraceRecord(count + 1, kind, instance, generation, queue, mode); count++;
   return true;
#endif
  }
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
  // The TLS reference does not retain a recorder or a Thread object.
  private int CurrentThreadTag() {
   object marker = threadMarker == null ? null : threadMarker.Target;
   if (marker == null) { marker = new object(); threadMarker = new WeakReference(marker); }
   for (int i = 0; i < threadTagCount; i++)
    if (object.ReferenceEquals(threadMarkers[i], marker)) return i + 1;
   if (threadTagCount == ThreadTagLimit) return 0;
   threadMarkers[threadTagCount] = marker; threadTagCount++; return threadTagCount;
  }
  private void StopDetailedOverflow() {
   overflowed = true; enabled = false;
   Array.Clear(threadMarkers, 0, threadMarkers.Length);
  }
  private bool AddDetailed(TraceKind kind, int instance, int generation, int queue, TraceMode mode, int invocation) {
   if (!enabled) return false;
   if (count == RecordLimit) { StopDetailedOverflow(); return false; }
   int tag = CurrentThreadTag();
   if (tag == 0) { StopDetailedOverflow(); return false; }
   records[count] = new TraceRecord(count + 1, kind, instance, generation, queue, mode);
   threadTags[count] = tag; invocationTags[count] = invocation; count++;
   return true;
  }
#endif
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
    if (instances == InstanceLimit) { overflowed = true; enabled = false;
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
    Array.Clear(threadMarkers, 0, threadMarkers.Length);
#endif
 return null; }
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
    if (generations == GenerationLimit) { overflowed = true; enabled = false;
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
    Array.Clear(threadMarkers, 0, threadMarkers.Length);
#endif
 return null; }
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
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
  // These calls record a caller's observation; they do not invoke a scheduler.
  public bool QueueReturned(TraceQueueToken queue) { return QueueResult(queue, TraceKind.QueueReturned); }
  public bool QueueThrew(TraceQueueToken queue) { return QueueResult(queue, TraceKind.QueueThrew); }
  private bool QueueResult(TraceQueueToken queue, TraceKind kind) {
   lock (gate) {
    if (!enabled || !IsQueue(queue) || queue.OutcomeObserved) return false;
    if (!Add(kind, queue.Thread.Id, queue.Generation.Id, queue.Id, TraceMode.None)) return false;
    queue.OutcomeObserved = true; return true;
   }
  }
  public TraceInvocationToken EnterInvocation(TraceQueueToken queue) {
   lock (gate) {
    if (!enabled || !IsQueue(queue)) return null;
    if (invocations == InvocationLimit) { StopDetailedOverflow(); return null; }
    TraceKind kind = queue.Thread.Released ? TraceKind.LateCallback : !queue.Generation.Active ? TraceKind.CallbackAfterUnload :
        queue.Thread.CallbackDepth > 0 ? TraceKind.OverlapOrReentry : TraceKind.CallbackEntered;
    int id = invocations + 1;
    if (!AddDetailed(kind, queue.Thread.Id, queue.Generation.Id, queue.Id, TraceMode.None, id)) return null;
    invocations = id; queue.Thread.CallbackDepth++;
    return new TraceInvocationToken(owner, id, queue);
   }
  }
  public bool ExitInvocation(TraceInvocationToken invocation) {
   lock (gate) {
    if (!enabled || invocation == null || !object.ReferenceEquals(invocation.Owner, owner) ||
        invocation.Ended || invocation.Queue.Thread.CallbackDepth == 0) return false;
    TraceQueueToken queue = invocation.Queue;
    if (!AddDetailed(TraceKind.CallbackExited, queue.Thread.Id, queue.Generation.Id, queue.Id, TraceMode.None, invocation.Id)) return false;
    invocation.Ended = true; queue.Thread.CallbackDepth--; return true;
   }
  }
#endif
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
#if FULLMAP_LIFECYCLE_SCHEDULER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
  // This and Read() are separate snapshots. A stopped trace cannot prove that callbacks did not occur.
  public TraceObservationDetail[] ReadObservationDetails() {
   lock (gate) {
    TraceObservationDetail[] copy = new TraceObservationDetail[count];
    for (int i = 0; i < count; i++)
     copy[i] = new TraceObservationDetail(records[i].Sequence, threadTags[i], invocationTags[i]);
    return copy;
   }
  }
#endif
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
