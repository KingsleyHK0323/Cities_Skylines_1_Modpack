// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// OpenAI-Codex_release_isolation
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
 public enum LoadingOwnershipIssue { None, OverlappingRegistrations, CreationFailure, CountExhausted }
#if FULLMAP_TEST_DIAGNOSTICS && !FULLMAP_RELEASE && !RELEASE
 public sealed class LoadingOwnershipDiagnostics {
  public readonly long RegisteredNotReleased;
  public readonly bool Ambiguous, CountExhausted;
  public readonly LoadingOwnershipIssue Issue;
  internal LoadingOwnershipDiagnostics(long count, bool ambiguous, bool countExhausted, LoadingOwnershipIssue issue) {
   RegisteredNotReleased = count; Ambiguous = ambiguous; CountExhausted = countExhausted; Issue = issue;
  }
 }
#endif
 public static class Observation {
  private static readonly object Gate = new object();
  private static long generation;
  private static bool active;
  private static object provider;
  private static object loadingOwner;
  private static Loading latestLoading;
  private static long liveLoadings;
  private static bool loadingAmbiguous, loadingCountExhausted;
  private static LoadingOwnershipIssue loadingIssue;
  private static AreaObservation latest;
#if FULLMAP_TEST_DIAGNOSTICS && !FULLMAP_RELEASE && !RELEASE
  // This records registration callbacks, not every loading instance that may exist.
  public static LoadingOwnershipDiagnostics ReadLoadingOwnership() {
   lock (Gate) { return new LoadingOwnershipDiagnostics(liveLoadings, loadingAmbiguous, loadingCountExhausted, loadingIssue); }
  }
#endif
  internal static object Bind(object owner, IAreas areas) {
   lock (Gate) { provider = owner; Advance(); return StartupSnapshots.BindAreas(owner, areas); }
  }
  private static void Advance() { generation = generation == long.MaxValue ? 1 : generation + 1; latest = null; }
  private static void QuarantineLoading(Loading owner, LoadingOwnershipIssue issue) {
   if (!loadingAmbiguous || issue == LoadingOwnershipIssue.CountExhausted) loadingIssue = issue;
   if (latestLoading != null) latestLoading.observationRetired = true;
   owner.observationRetired = true; latestLoading = null; loadingAmbiguous = true;
   active = false; loadingOwner = null; Advance(); StartupSnapshots.AmbiguousLoading();
  }
  internal static bool RegisterLoading(Loading owner, ILoading loading) {
   lock (Gate) {
    if (liveLoadings == long.MaxValue) { loadingCountExhausted = true; QuarantineLoading(owner, LoadingOwnershipIssue.CountExhausted); return false; }
    owner.observationCounted = true; liveLoadings++;
    if (loadingAmbiguous || latestLoading != null) { QuarantineLoading(owner, LoadingOwnershipIssue.OverlappingRegistrations); return false; }
    latestLoading = owner;
    StartupSnapshots.BindLoading(owner, loading);
    return true;
   }
  }
  internal static void RejectLoading(Loading owner) { lock (Gate) { QuarantineLoading(owner, LoadingOwnershipIssue.CreationFailure); } }
  internal static bool Begin(Loading owner, bool playable, LoadMode mode, out object snapshotEpoch) {
   lock (Gate) {
    snapshotEpoch = null;
    if (loadingAmbiguous || owner.observationRetired || !object.ReferenceEquals(latestLoading, owner)) return false;
    loadingOwner = owner; Advance(); active = playable;
    snapshotEpoch = StartupSnapshots.Loaded(owner, mode);
    return true;
   }
  }
  private static void EndObservation(object owner) {
   if (object.ReferenceEquals(loadingOwner, owner)) { active = false; loadingOwner = null; Advance(); }
  }
  internal static void End(object owner) { lock (Gate) { EndObservation(owner); StartupSnapshots.EndLoading(owner); } }
  internal static void StopLoading(object owner) { lock (Gate) { EndObservation(owner); StartupSnapshots.StopLoading(owner); } }
  internal static void ReleaseLoading(Loading owner) {
   lock (Gate) {
    EndObservation(owner);
    if (owner.observationCounted) { owner.observationCounted = false; liveLoadings--; }
    if (loadingAmbiguous) {
     if (liveLoadings == 0 && !loadingCountExhausted) {
      loadingAmbiguous = false; loadingIssue = LoadingOwnershipIssue.None; latestLoading = null; StartupSnapshots.ClearAmbiguousLoading();
     }
    } else {
     if (object.ReferenceEquals(latestLoading, owner)) latestLoading = null;
     StartupSnapshots.ReleaseLoading(owner);
    }
   }
  }
  // Provider release invalidates observations without ending the loaded city.
  internal static void Release(object owner) {
   lock (Gate) {
    if (object.ReferenceEquals(provider, owner)) { provider = null; Advance(); }
    StartupSnapshots.ReleaseAreas(owner);
   }
  }
  internal static void Record(object owner, int? x, int? z, bool? eligibility, int? price) {
   lock (Gate) {
    if (!active || !object.ReferenceEquals(provider, owner)) return;
    latest = new AreaObservation(generation, x, z, eligibility, price);
   }
  }
  public static AreaObservation Read() { lock (Gate) { return active ? latest : null; } }
  // True only at this instant; this cannot verify which city originated a callback.
  public static bool IsCurrent(AreaObservation observation) {
   lock (Gate) { return active && provider != null && observation != null && object.ReferenceEquals(latest, observation); }
  }
 }
 public enum StartupSnapshotStatus { Unavailable, UnsupportedMode, WaitingForDependencies, Pending, ObservedUnverified, Stopped, OwnerAmbiguous, WaitingForResume }
 public sealed class StartupSnapshot {
  public readonly long Generation, Revision;
  public readonly int UnlockedCount;
  public readonly LoadMode Mode;
  public readonly bool ProvenanceVerified = false;
  public readonly bool SimulationThreadVerified = false;
  public readonly bool SimulationRunningVerified = false;
  private readonly bool[] unlocked;
  internal StartupSnapshot(long generation, long revision, int count, LoadMode mode, bool[] values) {
   Generation = generation; Revision = revision; UnlockedCount = count; Mode = mode;
   unlocked = (bool[])values.Clone();
  }
  // A matching double read does not authenticate the city or prove atomicity.
  public bool[] GetUnlocked() { return (bool[])unlocked.Clone(); }
 }
 // Read-only observation only. A snapshot is never authority for a game write.
 public static class StartupSnapshots {
  private sealed class RequestContext {
   internal object epoch, ticket, loadingOwner, areaOwner, threadingOwner;
   internal ILoading loading;
   internal IAreas areas;
   internal IThreading threading;
   internal long generation;
   internal bool started, submissionComplete, completed, paused;
   internal bool[] values;
   internal int count;
  }
  private static readonly object Gate = new object();
  private static object loadingOwner, areaOwner, threadingOwner, epoch, ticket;
  private static ILoading loading;
  private static IAreas areas;
  private static IThreading threading;
  private static bool loaded;
  private static LoadMode? mode;
  private static long generation, revision;
  private static StartupSnapshot latest;
  private static StartupSnapshotStatus status = StartupSnapshotStatus.Unavailable;
  public static StartupSnapshotStatus Status { get { lock (Gate) { return status; } } }
  public static LoadMode? CurrentLoadMode { get { lock (Gate) { return loaded ? mode : null; } } }
  public static StartupSnapshot Read() { lock (Gate) { return status == StartupSnapshotStatus.ObservedUnverified ? latest : null; } }
  // Native submission is deliberately unavailable until provenance and call-site safety are proven.
  public static Initial25NoDebitDecision PrepareNativeSubmission(StartupSnapshot snapshot) {
   return new Initial25NoDebitDecision(Initial25DecisionKind.Unavailable, null);
  }
  // This checks only the adapter's received events and latest object at this instant.
  // It does not reread the game or authenticate the snapshot's city or contents.
  public static bool IsCurrent(StartupSnapshot value) {
   lock (Gate) { return value != null && status == StartupSnapshotStatus.ObservedUnverified && object.ReferenceEquals(value, latest); }
  }
  private static void ClearRequest() { ticket = null; latest = null; }
  internal static void AmbiguousLoading() {
   lock (Gate) {
    loadingOwner = null; loading = null; loaded = false; mode = null; epoch = null;
    ClearRequest(); status = StartupSnapshotStatus.OwnerAmbiguous;
   }
  }
  internal static void ClearAmbiguousLoading() {
   lock (Gate) { if (status == StartupSnapshotStatus.OwnerAmbiguous) status = StartupSnapshotStatus.Unavailable; }
  }
  internal static void BindLoading(object owner, ILoading value) {
   lock (Gate) {
    if (object.ReferenceEquals(loadingOwner, owner)) return;
    loadingOwner = owner; loading = value; loaded = false; mode = null; epoch = null;
    ClearRequest(); status = StartupSnapshotStatus.Unavailable;
   }
  }
  internal static object BindAreas(object owner, IAreas value) {
   lock (Gate) {
    if (object.ReferenceEquals(areaOwner, owner)) return epoch;
    bool replaced = areaOwner != null;
    areaOwner = owner; areas = value;
    if (loaded && replaced) { ClearRequest(); status = StartupSnapshotStatus.Stopped; }
    return epoch;
   }
  }
  internal static object BindThreading(object owner, IThreading value) {
   lock (Gate) {
    if (object.ReferenceEquals(threadingOwner, owner)) return epoch;
    bool replaced = threadingOwner != null;
    threadingOwner = owner; threading = value;
    if (loaded && replaced) { ClearRequest(); status = StartupSnapshotStatus.Stopped; }
    return epoch;
   }
  }
  internal static object Loaded(object owner, LoadMode value) {
   lock (Gate) {
    if (!object.ReferenceEquals(loadingOwner, owner) || loading == null) return null;
    ClearRequest(); mode = value; loaded = true; revision = 0; epoch = new object();
    if (generation == long.MaxValue) { status = StartupSnapshotStatus.Stopped; return epoch; }
    generation++;
    status = value == LoadMode.NewGame ? StartupSnapshotStatus.WaitingForDependencies : StartupSnapshotStatus.UnsupportedMode;
    return epoch;
   }
  }
  internal static void EndLoading(object owner) {
   lock (Gate) {
    if (!object.ReferenceEquals(loadingOwner, owner)) return;
    loaded = false; mode = null; epoch = null; ClearRequest(); status = StartupSnapshotStatus.Unavailable;
   }
  }
  internal static void StopLoading(object owner) {
   lock (Gate) {
    if (loaded && object.ReferenceEquals(loadingOwner, owner)) { ClearRequest(); status = StartupSnapshotStatus.Stopped; }
   }
  }
  internal static void ReleaseLoading(object owner) {
   lock (Gate) {
    if (!object.ReferenceEquals(loadingOwner, owner)) return;
    loadingOwner = null; loading = null; loaded = false; mode = null; epoch = null;
    ClearRequest(); status = StartupSnapshotStatus.Unavailable;
   }
  }
  internal static void ReleaseAreas(object owner) {
   lock (Gate) {
    if (!object.ReferenceEquals(areaOwner, owner)) return;
    areaOwner = null; areas = null;
    if (loaded) { ClearRequest(); status = StartupSnapshotStatus.Stopped; }
   }
  }
  internal static void ReleaseThreading(object owner) {
   lock (Gate) {
    if (!object.ReferenceEquals(threadingOwner, owner)) return;
    threadingOwner = null; threading = null;
    if (loaded) { ClearRequest(); status = StartupSnapshotStatus.Stopped; }
   }
  }
  private static bool SameRequest(RequestContext c) {
   return loaded && mode == LoadMode.NewGame && generation == c.generation &&
       object.ReferenceEquals(epoch, c.epoch) && object.ReferenceEquals(ticket, c.ticket) &&
       object.ReferenceEquals(loadingOwner, c.loadingOwner) && object.ReferenceEquals(loading, c.loading) &&
       object.ReferenceEquals(areaOwner, c.areaOwner) && object.ReferenceEquals(areas, c.areas) &&
       object.ReferenceEquals(threadingOwner, c.threadingOwner) && object.ReferenceEquals(threading, c.threading);
  }
  private static bool Pending(RequestContext c) { lock (Gate) { return status == StartupSnapshotStatus.Pending && SameRequest(c); } }
  private static void Stop(RequestContext c) {
   lock (Gate) { if (SameRequest(c)) { latest = null; status = StartupSnapshotStatus.Stopped; } }
  }
  private static void StopPending(RequestContext c) {
   lock (Gate) { if (status == StartupSnapshotStatus.Pending && SameRequest(c) && !c.paused) { latest = null; status = StartupSnapshotStatus.Stopped; } }
  }
  private static void WaitForResume(RequestContext c) {
   lock (Gate) {
    if (status == StartupSnapshotStatus.Pending && SameRequest(c)) {
     latest = null; c.paused = true;
     if (c.submissionComplete) status = StartupSnapshotStatus.WaitingForResume;
    }
   }
  }
  public static void CancelPending() {
   lock (Gate) {
    if (status == StartupSnapshotStatus.Pending || status == StartupSnapshotStatus.WaitingForResume) {
     ClearRequest(); status = StartupSnapshotStatus.Stopped;
    }
   }
  }
  public static StartupSnapshotStatus Request() { return RequestCore(null, null, 0); }
  internal static StartupSnapshotStatus RequestFromLoading(object owner, object eventEpoch) { return RequestCore(owner, eventEpoch, 1); }
  internal static StartupSnapshotStatus RequestFromAreas(object owner, object eventEpoch) { return RequestCore(owner, eventEpoch, 2); }
  internal static StartupSnapshotStatus RequestFromThreading(object owner, object eventEpoch) { return RequestCore(owner, eventEpoch, 3); }
  private static StartupSnapshotStatus RequestCore(object owner, object eventEpoch, int source) {
   RequestContext c;
   lock (Gate) {
    if (source != 0 && (eventEpoch == null || !object.ReferenceEquals(eventEpoch, epoch) ||
        (source == 1 && !object.ReferenceEquals(owner, loadingOwner)) ||
        (source == 2 && !object.ReferenceEquals(owner, areaOwner)) ||
        (source == 3 && !object.ReferenceEquals(owner, threadingOwner)))) return status;
    if (source != 0 && status != StartupSnapshotStatus.WaitingForDependencies) return status;
    if (!loaded || mode != LoadMode.NewGame || status == StartupSnapshotStatus.Pending || status == StartupSnapshotStatus.Stopped) return status;
    if (loading == null || areas == null || threading == null) { status = StartupSnapshotStatus.WaitingForDependencies; return status; }
    ticket = new object(); latest = null; status = StartupSnapshotStatus.Pending;
    c = new RequestContext { epoch = epoch, ticket = ticket, loadingOwner = loadingOwner, loading = loading,
        areaOwner = areaOwner, areas = areas, threadingOwner = threadingOwner, threading = threading, generation = generation };
   }
   try {
    c.threading.QueueSimulationThread(delegate { Capture(c); });
    lock (Gate) {
     if (status == StartupSnapshotStatus.Pending && SameRequest(c)) {
      c.submissionComplete = true;
      if (c.paused) status = StartupSnapshotStatus.WaitingForResume;
      else if (c.completed) Publish(c, c.values, c.count);
     }
    }
   }
   catch (System.Exception) { Stop(c); }
   return Status;
  }
  private static bool Ready(RequestContext c) {
   if (!Pending(c) || !c.loading.loadingComplete || !Pending(c) || c.loading.currentMode != AppMode.Game ||
       !Pending(c)) return false;
   if (c.threading.simulationPaused) { WaitForResume(c); return false; }
   return Pending(c);
  }
  // Called only under Gate, after the scheduler has returned successfully.
  private static void Publish(RequestContext c, bool[] values, int count) {
   if (status != StartupSnapshotStatus.Pending || !SameRequest(c)) return;
   if (revision == long.MaxValue) { status = StartupSnapshotStatus.Stopped; return; }
   revision++;
   latest = new StartupSnapshot(generation, revision, count, LoadMode.NewGame, values);
   status = StartupSnapshotStatus.ObservedUnverified;
  }
  private static void Capture(RequestContext c) {
   lock (Gate) {
    if (status != StartupSnapshotStatus.Pending || !SameRequest(c) || c.started) return;
    c.started = true;
   }
   try {
    if (!Ready(c)) { StopPending(c); return; }
    if (!Pending(c)) return;
    int before = c.areas.unlockedAreaCount, observed = 0;
    if (before < 0 || before > 25) { Stop(c); return; }
    bool[] values = new bool[25];
    for (int i = 0; i < 25; i++) {
     if (!Ready(c)) { StopPending(c); return; }
     values[i] = c.areas.IsAreaUnlocked(i % 5, i / 5);
     if (values[i]) observed++;
    }
    if (!Ready(c)) { StopPending(c); return; }
    int middle = c.areas.unlockedAreaCount;
    for (int i = 0; i < 25; i++) {
     if (!Ready(c)) { StopPending(c); return; }
     if (c.areas.IsAreaUnlocked(i % 5, i / 5) != values[i]) { Stop(c); return; }
    }
    if (!Ready(c)) { StopPending(c); return; }
    int after = c.areas.unlockedAreaCount;
    if (before != observed || middle != observed || after != observed) { Stop(c); return; }
    if (!Ready(c)) { StopPending(c); return; }
    lock (Gate) {
     if (status != StartupSnapshotStatus.Pending || !SameRequest(c)) return;
     if (c.submissionComplete) Publish(c, values, observed);
     else { c.values = values; c.count = observed; c.completed = true; }
    }
   } catch (System.Exception) { Stop(c); }
  }
 }
 public sealed class Loading : LoadingExtensionBase {
  // Only accessed while Observation holds its own gate; never lock another instance.
  internal bool observationRetired, observationCounted;
  private readonly object lifetime = new object();
  private bool created, registered, released, levelLoaded;
  public override void OnCreated(ILoading loading) {
   lock (lifetime) {
    if (created || released) return;
    created = true; registered = Observation.RegisterLoading(this, loading);
    try { base.OnCreated(loading); }
    catch (System.Exception) { registered = false; Observation.RejectLoading(this); throw; }
   }
  }
  public override void OnLevelLoaded(LoadMode mode) {
   object eventEpoch = null;
   lock (lifetime) {
    if (!released && registered) {
     if (levelLoaded) Observation.StopLoading(this);
     else if (Observation.Begin(this, mode == LoadMode.NewGame || mode == LoadMode.LoadGame, mode, out eventEpoch)) {
      levelLoaded = true;
     }
    }
   }
   if (eventEpoch != null) StartupSnapshots.RequestFromLoading(this, eventEpoch);
  }
  public override void OnLevelUnloading() { lock (lifetime) { if (!released) { levelLoaded = false; Observation.End(this); } } }
  public override void OnReleased() {
   lock (lifetime) {
    if (released) return;
    released = true;
    Observation.ReleaseLoading(this);
    base.OnReleased();
   }
  }
 }
 public sealed class Areas : AreasExtensionBase {
  private readonly object lifetime = new object();
  private bool created, released;
  public override void OnCreated(IAreas areas) {
   object eventEpoch = null;
   lock (lifetime) {
    if (created || released) return;
    created = true; base.OnCreated(areas); eventEpoch = Observation.Bind(this, areas);
   }
   if (eventEpoch != null) StartupSnapshots.RequestFromAreas(this, eventEpoch);
  }
  public override void OnReleased() {
   lock (lifetime) {
    if (released) return;
    released = true;
    Observation.Release(this);
    base.OnReleased();
   }
  }
  public override bool OnCanUnlockArea(int x, int z, bool originalResult) {
   lock (lifetime) { if (!released) Observation.Record(this, x, z, originalResult, null); }
   return originalResult;
  }
  public override int OnGetAreaPrice(uint ore, uint oil, uint forest, uint fertility, uint water,
      bool road, bool train, bool ship, bool plane, float landFlatness, int originalPrice) {
   // Price callbacks contain no tile identity; never associate with the prior eligibility callback.
   lock (lifetime) { if (!released) Observation.Record(this, null, null, null, originalPrice); }
   return originalPrice;
  }
 }
 public sealed class Threading : ThreadingExtensionBase {
  private readonly object lifetime = new object();
  private bool created, released;
  public override void OnCreated(IThreading threading) {
   object eventEpoch = null;
   lock (lifetime) {
    if (created || released) return;
    created = true; base.OnCreated(threading); eventEpoch = StartupSnapshots.BindThreading(this, threading);
   }
   if (eventEpoch != null) StartupSnapshots.RequestFromThreading(this, eventEpoch);
  }
  public override void OnReleased() {
   lock (lifetime) {
    if (released) return;
    released = true; StartupSnapshots.ReleaseThreading(this); base.OnReleased();
   }
  }
 }
}
