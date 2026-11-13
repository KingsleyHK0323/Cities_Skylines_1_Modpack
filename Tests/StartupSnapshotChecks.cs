// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic boundary tests only; no game, scheduler, purchase or patch is executed.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ICities;
using FullMapUnlockAndExpansion;

public static class StartupSnapshotChecks {
 static int checks;
 static Type snapshots, threadingExtension;
 static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
 static string Status() { return snapshots.GetProperty("Status").GetValue(null, null).ToString(); }
 static object Read() { return snapshots.GetMethod("Read").Invoke(null, null); }
 static string Request() { return snapshots.GetMethod("Request").Invoke(null, null).ToString(); }
 static void Cancel() { snapshots.GetMethod("CancelPending").Invoke(null, null); }
 static object Epoch() { return snapshots.GetField("epoch", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null); }
 static string AreaTrigger(object owner, object eventEpoch) {
  return snapshots.GetMethod("RequestFromAreas", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { owner, eventEpoch }).ToString();
 }
 static bool IsCurrent(object value) { return (bool)snapshots.GetMethod("IsCurrent").Invoke(null, new object[] { value }); }
 static long ValueLong(object value, string field) { return (long)value.GetType().GetField(field).GetValue(value); }
 static int ValueInt(object value, string field) { return (int)value.GetType().GetField(field).GetValue(value); }
 static bool[] Grid(object value) { return (bool[])value.GetType().GetMethod("GetUnlocked").Invoke(value, null); }
 static string Mode() {
  object mode = snapshots.GetProperty("CurrentLoadMode").GetValue(null, null);
  return mode == null ? "Unknown" : mode.ToString();
 }
 static object LoadingDiagnostics() {
  MethodInfo method = typeof(Observation).GetMethod("ReadLoadingOwnership", BindingFlags.Public | BindingFlags.Static);
  if (method == null) throw new Exception("loading ownership diagnostics are missing");
  return method.Invoke(null, null);
 }
 static void CheckLoadingDiagnostics(long expected, bool ambiguous, string message) {
  object data = LoadingDiagnostics();
  FieldInfo issue = data.GetType().GetField("Issue");
  if (issue == null) throw new Exception("loading ownership issue diagnostics are missing");
  Check(ValueLong(data, "RegisteredNotReleased") == expected &&
      (bool)data.GetType().GetField("Ambiguous").GetValue(data) == ambiguous &&
      !(bool)data.GetType().GetField("CountExhausted").GetValue(data) &&
      issue.GetValue(data).ToString() == (ambiguous ? "OverlappingRegistrations" : "None"), message);
 }
 static void CheckLoadingCreationFailure() {
  var broken = new Loading(); bool threw = false;
  try { LoadingExtensionBase.FailOnCreated = true; broken.OnCreated(new FakeLoading()); }
  catch (InvalidOperationException) { threw = true; }
  finally { LoadingExtensionBase.FailOnCreated = false; }
  object data = LoadingDiagnostics();
  Check(threw && Status() == "OwnerAmbiguous" && ValueLong(data, "RegisteredNotReleased") == 1 &&
      data.GetType().GetField("Issue").GetValue(data).ToString() == "CreationFailure", "base creation failure is diagnosable and quarantined");
  broken.OnReleased(); CheckLoadingDiagnostics(0, false, "released failed creation clears its registration");
 }
 sealed class FakeLoading : ILoading {
  public bool Ready = true;
  public AppMode Mode = AppMode.Game;
  public bool loadingComplete { get { return Ready; } }
  public AppMode currentMode { get { return Mode; } }
 }
 sealed class FakeAreas : IAreas {
  public readonly bool[] Values = new bool[25];
  public int Reads, CountReads, UnexpectedWrites;
  public bool ThrowRead, WrongCount;
  public Action<int> OnRead;
  public FakeAreas() { Values[12] = true; }
  public int unlockedAreaCount {
   get { CountReads++; int count = 0; foreach (bool value in Values) if (value) count++; return WrongCount ? count + 1 : count; }
  }
  public bool IsAreaUnlocked(int x, int z) {
   if (x < 0 || x >= 5 || z < 0 || z >= 5) throw new Exception("outside fake area grid");
   Reads++;
   if (OnRead != null) OnRead(Reads);
   if (ThrowRead) throw new InvalidOperationException("synthetic read failure");
   return Values[z * 5 + x];
  }
  public void UnlockArea(int x, int z, bool requireMoney) { UnexpectedWrites++; throw new InvalidOperationException("unexpected fake native write"); }
 }
 sealed class FakeThreading : IThreading {
  public bool Paused, ForcedPaused, Inline, ThrowBefore, ThrowAfter;
  public Action BeforeReturn;
  public int Queues;
  public readonly List<Action> Actions = new List<Action>();
  public bool simulationPaused { get { return Paused; } }
  public void QueueSimulationThread(Action action) {
   bool throwAfterThisCall = ThrowAfter;
   Queues++;
   if (ThrowBefore) throw new InvalidOperationException("synthetic scheduler failure");
   Actions.Add(action);
   if (Inline) action();
   if (BeforeReturn != null) BeforeReturn();
   if (throwAfterThisCall) throw new InvalidOperationException("synthetic scheduler post-callback failure");
  }
  public void Deliver(int index) { Actions[index](); }
 }
 sealed class Fixture {
  public readonly Loading Loading = new Loading();
  public readonly Areas Areas = new Areas();
  public readonly FakeLoading LoadInfo = new FakeLoading();
  public readonly FakeAreas AreaInfo = new FakeAreas();
  public readonly FakeThreading ThreadInfo = new FakeThreading();
  public readonly object Threading = Activator.CreateInstance(threadingExtension);
  public Fixture() { Loading.OnCreated(LoadInfo); }
  public void BindArea() { Areas.OnCreated(AreaInfo); }
  public void BindThread() { threadingExtension.GetMethod("OnCreated").Invoke(Threading, new object[] { ThreadInfo }); }
  public void Bind() { BindArea(); BindThread(); }
  public void Release() {
   Loading.OnReleased(); Areas.OnReleased(); threadingExtension.GetMethod("OnReleased").Invoke(Threading, null);
  }
 }
 static void CheckModesAndOrder() {
  foreach (LoadMode mode in new LoadMode[] { LoadMode.LoadGame, LoadMode.NewGameFromScenario, LoadMode.NewMap }) {
   var f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(mode);
   Check(Status() == "UnsupportedMode" && Mode() == mode.ToString() && f.ThreadInfo.Queues == 0 && Read() == null, "non-new-game mode never schedules");
   f.Release();
  }
  int[][] orders = new int[][] { new int[]{0,1,2}, new int[]{0,2,1}, new int[]{1,0,2},
      new int[]{1,2,0}, new int[]{2,0,1}, new int[]{2,1,0} };
  foreach (int[] order in orders) {
   var f = new Fixture();
   foreach (int item in order) {
    if (item == 0) f.Loading.OnLevelLoaded(LoadMode.NewGame);
    if (item == 1) f.BindArea();
    if (item == 2) f.BindThread();
   }
   Check(Status() == "Pending" && f.ThreadInfo.Queues == 1, "each dependency order schedules once");
   f.ThreadInfo.Deliver(0); object snapshot = Read();
   Check(Status() == "ObservedUnverified" && snapshot != null && Mode() == "NewGame", "new game yields explicitly unverified observation");
   Check(!(bool)snapshot.GetType().GetField("ProvenanceVerified").GetValue(snapshot), "every captured snapshot declares provenance unverified");
   Check(!(bool)snapshot.GetType().GetField("SimulationThreadVerified").GetValue(snapshot) &&
       !(bool)snapshot.GetType().GetField("SimulationRunningVerified").GetValue(snapshot), "thread identity and full pause state are explicitly unverified");
   Check(ValueInt(snapshot, "UnlockedCount") == 1 && Grid(snapshot)[12] && f.AreaInfo.Reads == 50, "two complete coordinate reads match count");
   bool[] copy = Grid(snapshot); copy[12] = false;
   Check(Grid(snapshot)[12] && IsCurrent(snapshot), "returned array is detached and current at check time");
   f.Release();
  }
 }
 static void CheckAmbiguousLateCreation() {
  var older = new Loading();
  var f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.LoadGame);
  CheckLoadingDiagnostics(1, false, "one registered loading owner is reported");
  older.OnCreated(new FakeLoading()); older.OnLevelLoaded(LoadMode.NewGame);
  Check(Status() == "OwnerAmbiguous" && Mode() == "Unknown" && Read() == null && f.ThreadInfo.Queues == 0,
      "late older creation cannot classify another city's provider as a new game");
  CheckLoadingDiagnostics(2, true, "overlapping registered owners are reported");
  f.Loading.OnReleased(); f.Loading.OnReleased();
  Check(Status() == "OwnerAmbiguous" && Request() == "OwnerAmbiguous", "one remaining ambiguous owner cannot restart observation");
  CheckLoadingDiagnostics(1, true, "duplicate release does not change the remaining owner count");
  var third = new Loading(); third.OnCreated(new FakeLoading()); third.OnCreated(new FakeLoading());
  CheckLoadingDiagnostics(2, true, "duplicate creation does not increase the owner count");
  older.OnReleased(); older.OnReleased();
  Check(Status() == "OwnerAmbiguous", "a third registered owner keeps ambiguity after earlier owners release");
  CheckLoadingDiagnostics(1, true, "third registered owner remains counted");
  third.OnReleased(); third.OnReleased();
  Check(Status() == "Unavailable", "ambiguity clears only after all overlapping owners release");
  CheckLoadingDiagnostics(0, false, "all counted owners released and diagnostics reset");
  f.Areas.OnReleased(); threadingExtension.GetMethod("OnReleased").Invoke(f.Threading, null);
  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  Check(Read() != null && Status() == "ObservedUnverified", "fresh owner may observe after ambiguity clears");
  var uncreated = new Loading(); uncreated.OnReleased(); uncreated.OnCreated(new FakeLoading());
  Check(Status() == "ObservedUnverified", "release-before-create does not disturb current owner"); f.Release();

  older = new Loading(); f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  object prior = Read(); older.OnCreated(new FakeLoading());
  Check(Status() == "OwnerAmbiguous" && Read() == null && !IsCurrent(prior), "overlap invalidates an already observed snapshot");
  f.Release(); older.OnReleased();
  Check(Status() == "Unavailable", "all released owners clear ambiguity after an observed snapshot");

  older = new Loading(); f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  older.OnCreated(new FakeLoading()); f.ThreadInfo.Deliver(0);
  Check(Status() == "OwnerAmbiguous" && Read() == null && f.AreaInfo.Reads == 0, "ambiguous owner invalidates an existing queued callback");
  f.Release(); older.OnReleased();

  var a = new Loading(); var b = new Loading(); var start = new ManualResetEvent(false);
  Exception errorA = null, errorB = null;
  var threadA = new Thread(delegate() { try { start.WaitOne(); a.OnCreated(new FakeLoading()); } catch (Exception ex) { errorA = ex; } });
  var threadB = new Thread(delegate() { try { start.WaitOne(); b.OnCreated(new FakeLoading()); } catch (Exception ex) { errorB = ex; } });
  threadA.Start(); threadB.Start(); start.Set(); threadA.Join(); threadB.Join(); start.Close();
  Check(errorA == null && errorB == null && Status() == "OwnerAmbiguous", "concurrently registered owners are quarantined");
  a.OnReleased(); b.OnReleased();
  Check(Status() == "Unavailable", "concurrent owners release without leaking the quarantine count");

  var same = new Loading(); start = new ManualResetEvent(false); ManualResetEvent sameStart = start;
  threadA = new Thread(delegate() { sameStart.WaitOne(); same.OnCreated(new FakeLoading()); });
  threadB = new Thread(delegate() { sameStart.WaitOne(); same.OnCreated(new FakeLoading()); });
  threadA.Start(); threadB.Start(); start.Set(); threadA.Join(); threadB.Join(); start.Close();
  same.OnLevelLoaded(LoadMode.NewGame);
  Check(Status() == "WaitingForDependencies", "duplicate concurrent creation of one owner is counted once");
  same.OnReleased(); Check(Status() == "Unavailable", "one release clears the single owner");
 }
 static void CheckPauseRetries() {
  var parallel = new Fixture(); parallel.Bind(); parallel.ThreadInfo.Paused = true;
  parallel.Loading.OnLevelLoaded(LoadMode.NewGame); parallel.ThreadInfo.Deliver(0); parallel.ThreadInfo.Paused = false;
  string resultA = null, resultB = null; var start = new ManualResetEvent(false);
  var threadA = new Thread(delegate() { start.WaitOne(); resultA = Request(); });
  var threadB = new Thread(delegate() { start.WaitOne(); resultB = Request(); });
  threadA.Start(); threadB.Start(); start.Set(); threadA.Join(); threadB.Join(); start.Close();
  Check(resultA == "Pending" && resultB == "Pending" && parallel.ThreadInfo.Queues == 2, "concurrent explicit retries create only one new ticket");
  parallel.ThreadInfo.Deliver(0);
  Check(Status() == "Pending" && Read() == null, "old paused callback cannot complete the new ticket");
  parallel.ThreadInfo.Deliver(1); Check(Read() != null, "the single resumed ticket can complete"); parallel.Release();

  var f = new Fixture(); f.Bind(); f.ThreadInfo.Paused = true; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  f.ThreadInfo.Paused = false;
  Check(AreaTrigger(f.Areas, Epoch()) == "WaitingForResume" && f.ThreadInfo.Queues == 1, "resume alone never schedules an automatic retry");
  f.LoadInfo.Ready = false; Check(Request() == "Pending", "explicit retry can be queued for revalidation"); f.ThreadInfo.Deliver(1);
  Check(Status() == "Stopped" && f.AreaInfo.Reads == 0, "retry rechecks loading readiness before reading"); f.Release();

  f = new Fixture(); f.Bind(); f.ThreadInfo.Paused = true; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  f.ThreadInfo.Paused = false; Check(Request() == "Pending", "provider race retry queued");
  var replacement = new Areas(); replacement.OnCreated(new FakeAreas()); f.ThreadInfo.Deliver(1);
  Check(Status() == "Stopped" && Read() == null && f.AreaInfo.Reads == 0, "provider replacement invalidates resumed observation");
  replacement.OnReleased(); f.Release();

  f = new Fixture(); f.Bind(); f.ThreadInfo.Paused = true; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  f.Loading.OnLevelUnloading(); f.ThreadInfo.Paused = false;
  Check(Request() == "Unavailable" && f.ThreadInfo.Queues == 1, "unload invalidates the paused observation before retry"); f.Release();

  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Fixture pausesDuringRead = f;
  f.AreaInfo.OnRead = delegate(int n) { if (n == 3) pausesDuringRead.ThreadInfo.Paused = true; };
  f.ThreadInfo.Deliver(0);
  Check(Status() == "WaitingForResume" && Read() == null && f.AreaInfo.Reads == 3, "observed pause stops subsequent grid reads");
  f.AreaInfo.OnRead = null; f.ThreadInfo.Paused = false; Request(); f.ThreadInfo.Deliver(1);
  Check(Read() != null && Status() == "ObservedUnverified", "explicit retry starts a fresh snapshot after mid-read pause"); f.Release();

  f = new Fixture(); f.Bind(); f.ThreadInfo.Inline = true; f.ThreadInfo.ThrowAfter = true; f.ThreadInfo.Paused = true;
  f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Check(Status() == "Stopped" && Request() == "Stopped", "scheduler failure dominates an inline paused callback"); f.Release();

  f = new Fixture(); f.Bind(); f.ThreadInfo.Inline = true; f.ThreadInfo.ThrowAfter = true; f.ThreadInfo.Paused = true;
  string reentrantRetry = null; Fixture reentrantFixture = f;
  f.ThreadInfo.BeforeReturn = delegate {
   reentrantFixture.ThreadInfo.BeforeReturn = null; reentrantFixture.ThreadInfo.ThrowAfter = false; reentrantFixture.ThreadInfo.Paused = false;
   reentrantRetry = Request();
  };
  f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Check(reentrantRetry == "Pending" && f.ThreadInfo.Queues == 1 && Status() == "Stopped" && Read() == null,
      "paused inline callback cannot retry before the failing scheduler returns"); f.Release();

  f = new Fixture(); f.Bind(); f.ThreadInfo.Paused = true; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0); Cancel();
  f.ThreadInfo.Paused = false;
  Check(Status() == "Stopped" && Request() == "Stopped", "cancelled pause cannot be retried"); f.Release();

  f = new Fixture(); f.Bind(); f.ThreadInfo.ForcedPaused = true; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  object uncertain = Read();
  Check(f.ThreadInfo.ForcedPaused && uncertain != null && !(bool)uncertain.GetType().GetField("SimulationRunningVerified").GetValue(uncertain),
      "normal pause flag cannot establish that forced pause is absent");
  var noNative = (Initial25NoDebitDecision)snapshots.GetMethod("PrepareNativeSubmission").Invoke(null, new object[] { uncertain });
  Check(noNative.Kind == Initial25DecisionKind.Unavailable, "unknown full pause state cannot authorize native submission"); f.Release();

  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Fixture wrongThreadFixture = f;
  var wrongThread = new Thread(delegate() { wrongThreadFixture.ThreadInfo.Deliver(0); }); wrongThread.Start(); wrongThread.Join();
  uncertain = Read();
  Check(uncertain != null && !(bool)uncertain.GetType().GetField("SimulationThreadVerified").GetValue(uncertain),
      "fake wrong-thread callback remains explicitly unverified");
  noNative = (Initial25NoDebitDecision)snapshots.GetMethod("PrepareNativeSubmission").Invoke(null, new object[] { uncertain });
  Check(noNative.Kind == Initial25DecisionKind.Unavailable, "unverified thread cannot authorize native submission"); f.Release();
 }
 static void CheckSchedulingAndLifecycle() {
  var f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Check(Status() == "Pending" && Request() == "Pending" && f.ThreadInfo.Queues == 1 && Read() == null, "pending queue neither duplicates nor publishes");
  f.Loading.OnLevelUnloading(); f.ThreadInfo.Deliver(0);
  Check(Read() == null && f.AreaInfo.Reads == 0 && Status() == "Unavailable", "delayed callback after unload reads nothing");
  f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(1);
  object first = Read(); Check(first != null, "same loading instance can serve a later new city");
  Check(Request() == "Pending" && f.ThreadInfo.Queues == 3, "explicit new snapshot creates one fresh request");
  f.ThreadInfo.Deliver(1); Check(Status() == "Pending" && Read() == null, "old callback cannot clear or publish over new ticket");
  f.ThreadInfo.Deliver(2); object second = Read();
  Check(second != null && ValueLong(second, "Revision") > ValueLong(first, "Revision") && !IsCurrent(first), "newer snapshot has a higher revision");
  object oldEpoch = Epoch();
  Check(AreaTrigger(f.Areas, oldEpoch) == "ObservedUnverified" && f.ThreadInfo.Queues == 3 && object.ReferenceEquals(second, Read()),
      "late same-epoch automatic trigger cannot request another snapshot");
  f.Loading.OnLevelUnloading(); f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(3);
  object current = Read();
  Check(AreaTrigger(f.Areas, oldEpoch) == "ObservedUnverified" && f.ThreadInfo.Queues == 4 && object.ReferenceEquals(current, Read()),
      "old epoch automatic trigger cannot affect a new city");
  f.Release();

  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Check(Status() == "Stopped" && f.ThreadInfo.Queues == 1, "duplicate load without unload cannot start a new city");
  f.ThreadInfo.Deliver(0);
  Check(Read() == null && f.AreaInfo.Reads == 0, "duplicate load invalidates prior queued snapshot");
  f.Loading.OnLevelUnloading(); f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(1);
  Check(Read() != null, "same loading owner can start again after explicit unload"); f.Release();

  f = new Fixture(); f.Bind(); f.ThreadInfo.Inline = true; string nested = null;
  f.AreaInfo.OnRead = delegate(int n) { if (n == 3) nested = Request(); };
  f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Check(nested == "Pending" && f.ThreadInfo.Queues == 1 && Status() == "ObservedUnverified", "inline reentry cannot schedule a nested snapshot");
  f.Release();

  f = new Fixture(); f.Bind(); f.ThreadInfo.ThrowBefore = true; f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Check(Status() == "Stopped" && Read() == null && Request() == "Stopped", "scheduler failure stops without retry");
  f.ThreadInfo.ThrowBefore = false; f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Check(Status() == "Stopped" && f.ThreadInfo.Queues == 1, "duplicate load cannot restart after failure"); f.Release();
  f = new Fixture(); f.Bind(); f.ThreadInfo.Inline = true; f.ThreadInfo.ThrowAfter = true;
  string duringSubmission = null; object premature = null;
  f.ThreadInfo.BeforeReturn = delegate { duringSubmission = Status(); premature = Read(); };
  f.Loading.OnLevelLoaded(LoadMode.NewGame);
  Check(duringSubmission == "Pending" && premature == null, "inline result stays unpublished until scheduler returns");
  Check(Status() == "Stopped" && Read() == null, "scheduler exception after inline callback invalidates result"); f.Release();
  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame); Cancel(); f.ThreadInfo.Deliver(0);
  Check(Status() == "Stopped" && Read() == null && f.AreaInfo.Reads == 0 && Request() == "Stopped", "cancelled or missing callback cannot restart"); f.Release();
 }
 static void CheckInvalidationAndSnapshot() {
  var f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  f.AreaInfo.OnRead = delegate(int n) { if (n == 3) f.Loading.OnLevelUnloading(); };
  f.ThreadInfo.Deliver(0);
  Check(Read() == null && Status() == "Unavailable" && f.AreaInfo.Reads == 3, "unload during read aborts without publishing"); f.Release();

  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  var entered = new ManualResetEvent(false); var unblock = new ManualResetEvent(false); var unloaded = new ManualResetEvent(false);
  f.AreaInfo.OnRead = delegate(int n) { if (n == 1) { entered.Set(); unblock.WaitOne(3000); } };
  Fixture blockedFixture = f;
  var captureThread = new Thread(delegate() { blockedFixture.ThreadInfo.Deliver(0); }); captureThread.Start();
  bool enteredRead = entered.WaitOne(3000);
  var unloadThread = new Thread(delegate() { blockedFixture.Loading.OnLevelUnloading(); unloaded.Set(); }); unloadThread.Start();
  bool unloadedWhileBlocked = unloaded.WaitOne(2000);
  unblock.Set(); captureThread.Join(); unloadThread.Join(); entered.Close(); unblock.Close(); unloaded.Close();
  Check(enteredRead && unloadedWhileBlocked && Read() == null && Status() == "Unavailable", "unload does not wait on an external area getter"); f.Release();

  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  f.AreaInfo.OnRead = delegate(int n) { if (n == 26) { f.AreaInfo.Values[12] = false; f.AreaInfo.Values[11] = true; } };
  f.ThreadInfo.Deliver(0);
  Check(Status() == "Stopped" && Read() == null, "same-count grid change between passes stops"); f.Release();

  f = new Fixture(); f.Bind(); f.AreaInfo.Values[12] = false; f.AreaInfo.Values[0] = true; f.AreaInfo.Values[10] = true;
  Fixture changing = f;
  f.AreaInfo.OnRead = delegate(int n) {
   if (n == 3 || n == 28) { changing.AreaInfo.Values[0] = false; changing.AreaInfo.Values[10] = false; changing.AreaInfo.Values[1] = true; changing.AreaInfo.Values[20] = true; }
   if (n == 26) { changing.AreaInfo.Values[0] = true; changing.AreaInfo.Values[10] = true; changing.AreaInfo.Values[1] = false; changing.AreaInfo.Values[20] = false; }
  };
  f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0); object mixed = Read(); bool[] mixedValues = Grid(mixed);
  Check(Status() == "ObservedUnverified" && mixedValues[0] && mixedValues[20] && !mixedValues[1] &&
      !f.AreaInfo.Values[0] && f.AreaInfo.Values[1], "matching double reads can still produce an unauthenticated mixed grid");
  f.LoadInfo.Ready = false;
  Check(IsCurrent(mixed), "IsCurrent only checks adapter state and does not reread loading status"); f.Release();
  f = new Fixture(); f.Bind(); f.AreaInfo.WrongCount = true; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  Check(Status() == "Stopped" && Read() == null, "inconsistent count stops"); f.Release();
  f = new Fixture(); f.Bind(); f.AreaInfo.ThrowRead = true; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  Check(Status() == "Stopped" && Read() == null && Request() == "Stopped", "native read exception stops"); f.Release();
  f = new Fixture(); f.Bind(); f.LoadInfo.Ready = false; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  Check(Status() == "Stopped" && f.AreaInfo.Reads == 0, "incomplete loading stops before reading"); f.Release();
  f = new Fixture(); f.Bind(); f.ThreadInfo.Paused = true; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  Check(Status() == "WaitingForResume" && f.AreaInfo.Reads == 0 && Read() == null, "paused simulation waits before reading");
  f.ThreadInfo.Paused = false;
  Check(Request() == "Pending" && f.ThreadInfo.Queues == 2, "resumed observation requires an explicit request");
  f.ThreadInfo.Deliver(1); Check(Read() != null && Status() == "ObservedUnverified", "explicit retry observes after resume"); f.Release();
  f = new Fixture(); f.Bind(); f.LoadInfo.Mode = AppMode.ScenarioEditor; f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  Check(Status() == "Stopped" && f.AreaInfo.Reads == 0, "unexpected app mode stops before reading"); f.Release();

  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  var replacement = new Areas(); var newAreas = new FakeAreas(); replacement.OnCreated(newAreas);
  Check(Status() == "Stopped", "provider replacement stops current city");
  f.Loading.OnLevelUnloading();
  f.Loading.OnLevelLoaded(LoadMode.NewGame); Check(Status() == "Pending" && f.ThreadInfo.Queues == 2, "new city can use replacement provider");
  f.ThreadInfo.Deliver(0); Check(Status() == "Pending" && Read() == null, "old provider ticket cannot disturb newer request");
  f.Areas.OnReleased(); f.ThreadInfo.Deliver(1);
  Check(Read() != null && newAreas.Reads == 50, "old provider release cannot invalidate current provider");
  replacement.OnReleased(); f.Release();

  f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
  object newThread = Activator.CreateInstance(threadingExtension); var newThreadInfo = new FakeThreading();
  threadingExtension.GetMethod("OnCreated").Invoke(newThread, new object[] { newThreadInfo });
  Check(Status() == "Stopped", "scheduler replacement stops current city");
  f.Loading.OnLevelUnloading();
  f.Loading.OnLevelLoaded(LoadMode.NewGame); Check(Status() == "Pending", "new city can use replacement scheduler");
  f.ThreadInfo.Deliver(0); Check(Status() == "Pending" && Read() == null, "old scheduler callback cannot affect current ticket");
  threadingExtension.GetMethod("OnReleased").Invoke(f.Threading, null); newThreadInfo.Deliver(0);
  Check(Read() != null, "old scheduler release cannot affect replacement");
  threadingExtension.GetMethod("OnReleased").Invoke(newThread, null); f.Release();
 }
 static void CheckFakeModelBridge() {
  var f = new Fixture(); f.Bind(); f.Loading.OnLevelLoaded(LoadMode.NewGame); f.ThreadInfo.Deliver(0);
  object snapshot = Read(); long generation = ValueLong(snapshot, "Generation"), revision = ValueLong(snapshot, "Revision");
  MethodInfo gate = snapshots.GetMethod("PrepareNativeSubmission", BindingFlags.Public | BindingFlags.Static);
  if (gate == null) throw new Exception("native submission denial entry is missing");
  var denied = (Initial25NoDebitDecision)gate.Invoke(null, new object[] { snapshot });
  Check(denied.Kind == Initial25DecisionKind.Unavailable && denied.NativeIndex == null && denied.RequireMoney == null && denied.AttemptId == null,
      "unverified snapshot is denied by the native submission entry");
  denied = (Initial25NoDebitDecision)gate.Invoke(null, new object[] { null });
  Check(denied.Kind == Initial25DecisionKind.Unavailable && denied.NativeIndex == null, "missing snapshot is denied by native submission entry");
  // Intentionally bypass the disabled native entry solely to exercise fake models.
  bool[] values = Grid(snapshot); Initial25Tile[] tiles = new Initial25Tile[25];
  for (int i = 0; i < 25; i++) tiles[i] = new Initial25Tile(i % 5, i / 5, i, values[i]);
  var run = new Initial25NoDebitRun();
  Initial25NoDebitDecision decision = run.Propose(tiles, 5, 5, 1, generation, generation, revision, revision, true);
  Check(decision.Kind == Initial25DecisionKind.Candidate && decision.RequireMoney == false, "fake snapshot connects to one no-debit candidate");
  int index = decision.NativeIndex.Value;
  var scope = new StartupMilestoneScopePreview(f.Loading, f.Areas, Thread.CurrentThread, generation, decision.AttemptId.Value, index % 5, index / 5);
  Check(scope.TryUse(false, f.Loading, f.Areas, generation, decision.AttemptId, index % 5, index / 5, true, true, true), "fake candidate connects to synthetic scope");
  f.ThreadInfo.Paused = true; Request(); f.ThreadInfo.Deliver(1);
  Check(Status() == "WaitingForResume" && Read() == null, "paused observation while a fake attempt is pending remains unconfirmed");
  f.ThreadInfo.Paused = false; Request(); f.ThreadInfo.Deliver(2); object resumed = Read(); values = Grid(resumed);
  for (int i = 0; i < 25; i++) tiles[i] = new Initial25Tile(i % 5, i / 5, i, values[i]);
  long resumedRevision = ValueLong(resumed, "Revision");
  Check(run.Propose(tiles, 5, 5, 1, generation, generation, resumedRevision, resumedRevision, true).Kind == Initial25DecisionKind.Waiting,
      "resuming observation does not retry a pending fake write");
  f.AreaInfo.Values[index] = true; Request(); f.ThreadInfo.Deliver(3); object newer = Read(); values = Grid(newer);
  for (int i = 0; i < 25; i++) tiles[i] = new Initial25Tile(i % 5, i / 5, i, values[i]);
  long newRevision = ValueLong(newer, "Revision");
  Check(run.Observe(tiles, 5, 5, 2, generation, generation, newRevision, newRevision, true).Kind == Initial25DecisionKind.Confirmed,
      "only fake grid change is observed as confirmed");
  Check(f.AreaInfo.UnexpectedWrites == 0, "fake model bridge never invokes the IAreas write interface");
  f.Release();
 }
 // OpenAI-Codex_build_review
 // Test-only bridge: all providers are local fakes and production submission remains disabled.
 sealed class RecordingAreas : IAreas {
  internal readonly FakeAreas Inner = new FakeAreas();
  internal bool WriterEnabled;
  internal int WriterCalls, BlockedCalls, LastX, LastZ;
  internal bool? LastRequireMoney;
  internal Func<bool> EntryInvalidObserver; internal bool? InvalidAtWriterEntry;
  public int unlockedAreaCount { get { return Inner.unlockedAreaCount; } }
  public bool IsAreaUnlocked(int x, int z) { return Inner.IsAreaUnlocked(x, z); }
  public void UnlockArea(int x, int z, bool requireMoney) {
   if (!WriterEnabled) { BlockedCalls++; throw new InvalidOperationException("unarmed fake writer"); }
   if (x < 0 || x >= 5 || z < 0 || z >= 5 || requireMoney)
    throw new InvalidOperationException("unexpected fake writer arguments");
   if (EntryInvalidObserver != null) InvalidAtWriterEntry = EntryInvalidObserver();
   WriterCalls++; LastX = x; LastZ = z; LastRequireMoney = requireMoney;
#if !FULLMAP_FAKE_BRIDGE_RED
   Inner.Values[z * 5 + x] = true;
#endif
  }
 }
 sealed class RecordingFixture {
  internal readonly Loading Loading = new Loading();
  internal readonly Areas Areas = new Areas();
  internal readonly FakeLoading LoadInfo = new FakeLoading();
  internal readonly RecordingAreas AreaInfo = new RecordingAreas();
  internal readonly FakeThreading ThreadInfo = new FakeThreading();
  internal readonly object Threading = Activator.CreateInstance(threadingExtension);
  internal readonly StartupSnapshot Snapshot;
  internal RecordingFixture() {
   Loading.OnCreated(LoadInfo);
   Areas.OnCreated(AreaInfo);
   threadingExtension.GetMethod("OnCreated").Invoke(Threading, new object[] { ThreadInfo });
   Loading.OnLevelLoaded(LoadMode.NewGame);
   Check(Status() == "Pending" && ThreadInfo.Actions.Count == 1, "fake bridge starts with a real queued snapshot");
   ThreadInfo.Deliver(0);
   Snapshot = (StartupSnapshot)Read();
   Check(Snapshot != null && Snapshot.Mode == LoadMode.NewGame && !Snapshot.ProvenanceVerified &&
     !Snapshot.SimulationThreadVerified && !Snapshot.SimulationRunningVerified,
     "fake bridge snapshot remains explicitly unverified");
  }
  internal void Release() {
   Loading.OnReleased(); Areas.OnReleased(); threadingExtension.GetMethod("OnReleased").Invoke(Threading, null);
  }
 }
 enum FakeBridgeState { Idle, Pending, Executed, Confirmed, Stopped, Unknown }
 sealed class ScheduledFakeBridge {
  readonly StartupSnapshot original;
  readonly IAreas writer;
  readonly IThreading scheduler;
  readonly Initial25NoDebitRun run = new Initial25NoDebitRun();
  readonly object stateGate = new object();
  FakeBridgeState state;
  bool queueSucceeded, callbackSucceeded;
  void RecordFakeCallbackSuccess() {
   lock (stateGate) {
    if (state == FakeBridgeState.Unknown || state == FakeBridgeState.Stopped) return;
    callbackSucceeded = true;
    if (queueSucceeded) state = FakeBridgeState.Executed;
   }
  }
  void RecordFakeQueueSuccess() {
   lock (stateGate) {
    if (state == FakeBridgeState.Unknown || state == FakeBridgeState.Stopped) return;
    queueSucceeded = true;
    if (callbackSucceeded) state = FakeBridgeState.Executed;
   }
  }
  internal FakeBridgeState State {
   get { lock (stateGate) { return state; } }
   set { lock (stateGate) { if (state != FakeBridgeState.Unknown) state = value; } }
  }
  internal Initial25NoDebitDecision Candidate;
  internal int CallbackCount;
  internal bool LastCurrentBeforeWrite, QueueException;
  internal Action BeforeWrite;
  internal ScheduledFakeBridge(StartupSnapshot snapshot, IAreas areas, IThreading threading) {
   original = snapshot; writer = areas; scheduler = threading;
  }
  static Initial25Tile[] Tiles(StartupSnapshot snapshot) {
   bool[] values = snapshot.GetUnlocked();
   Initial25Tile[] result = new Initial25Tile[25];
   for (int i = 0; i < result.Length; i++) result[i] = new Initial25Tile(i % 5, i / 5, i, values[i]);
   return result;
  }
  internal bool Start() {
   if (State != FakeBridgeState.Idle) return false;
   Candidate = run.Propose(Tiles(original), 5, 5, original.UnlockedCount,
     original.Generation, original.Generation, original.Revision, original.Revision, original.Mode == LoadMode.NewGame);
   if (Candidate.Kind != Initial25DecisionKind.Candidate || Candidate.RequireMoney != false ||
       !Candidate.NativeIndex.HasValue || !Candidate.AttemptId.HasValue) { State = FakeBridgeState.Stopped; return false; }
   State = FakeBridgeState.Pending;
   int invoked = 0;
   try {
    scheduler.QueueSimulationThread(delegate {
     Interlocked.Increment(ref CallbackCount);
     if (Interlocked.Exchange(ref invoked, 1) != 0 || State != FakeBridgeState.Pending) return;
     LastCurrentBeforeWrite = StartupSnapshots.IsCurrent(original);
     if (!LastCurrentBeforeWrite) { run.FailPending(Candidate.AttemptId); State = FakeBridgeState.Stopped; return; }
     try {
      if (BeforeWrite != null) BeforeWrite();
      int index = Candidate.NativeIndex.Value;
      writer.UnlockArea(index % 5, index / 5, Candidate.RequireMoney.Value);
     } catch (Exception) { run.FailPending(Candidate.AttemptId); State = FakeBridgeState.Unknown; return; }
     if (!StartupSnapshots.IsCurrent(original)) { run.FailPending(Candidate.AttemptId); State = FakeBridgeState.Unknown; return; }
     RecordFakeCallbackSuccess();
    });
    RecordFakeQueueSuccess();
   } catch (Exception) { QueueException = true; run.FailPending(Candidate.AttemptId); State = FakeBridgeState.Unknown; }
   return true;
  }
  internal Initial25DecisionKind Confirm(StartupSnapshot snapshot) {
   if (State != FakeBridgeState.Executed || snapshot == null || !StartupSnapshots.IsCurrent(snapshot))
    return Initial25DecisionKind.Unavailable;
   Initial25NoDebitDecision decision = run.Observe(Tiles(snapshot), 5, 5, snapshot.UnlockedCount,
     snapshot.Generation, snapshot.Generation, snapshot.Revision, snapshot.Revision, snapshot.Mode == LoadMode.NewGame);
   if (decision.Kind == Initial25DecisionKind.Confirmed) State = FakeBridgeState.Confirmed;
   return decision.Kind;
  }
 }
 static void CheckIntegratedFakeBridge() {
  int before = checks;
  var originalTrap = new FakeAreas(); bool rejected = false;
  bool[] originalTrapBaseline = (bool[])originalTrap.Values.Clone();
  try { originalTrap.UnlockArea(0, 0, false); } catch (InvalidOperationException) { rejected = true; }
  Check(rejected && originalTrap.UnexpectedWrites == 1 && !originalTrap.Values[0],
    "original fake areas write trap is unchanged");
  for (int i = 0; i < originalTrapBaseline.Length; i++)
   Check(originalTrap.Values[i] == originalTrapBaseline[i], "original fake write trap preserves every fake tile");
  var unarmed = new RecordingAreas(); rejected = false;
  try { unarmed.UnlockArea(0, 0, false); } catch (InvalidOperationException) { rejected = true; }
  Check(rejected && unarmed.BlockedCalls == 1 && unarmed.WriterCalls == 0 && !unarmed.Inner.Values[0],
    "recording fake writer is disabled until explicitly enabled");

  var f = new RecordingFixture();
  try {
   f.AreaInfo.WriterEnabled = true;
   StartupSnapshot first = f.Snapshot;
   var request = new NativeSubmissionRequest(first.Generation, first.Revision, 1, 2, 1, true, true);
   Check(StartupSnapshots.PrepareNativeSubmission(first).Kind == Initial25DecisionKind.Unavailable &&
     new DisabledNativeStartupSubmission().Submit(first, request) == NativeSubmissionAvailability.Unavailable,
     "both real submission gates remain unavailable");
   bool[] old = first.GetUnlocked();
   var bridge = new ScheduledFakeBridge(first, f.AreaInfo, f.ThreadInfo);
   Check(bridge.Start() && bridge.State == FakeBridgeState.Pending && bridge.Candidate.RequireMoney == false &&
     f.ThreadInfo.Actions.Count == 2 && f.AreaInfo.WriterCalls == 0, "one fake writer action is queued");
   int tile = bridge.Candidate.NativeIndex.Value;
   Action saved = f.ThreadInfo.Actions[1];
   saved();
   Check(bridge.State == FakeBridgeState.Executed && bridge.CallbackCount == 1 &&
     f.AreaInfo.WriterCalls == 1 && f.AreaInfo.LastX == tile % 5 && f.AreaInfo.LastZ == tile / 5 &&
     f.AreaInfo.LastRequireMoney == false, "fake IAreas receives one exact no-debit call");
   saved();
   Check(Object.ReferenceEquals(saved, f.ThreadInfo.Actions[1]) && bridge.CallbackCount == 2 &&
     f.AreaInfo.WriterCalls == 1, "same fake action really replays without another writer call");
   Check(Request() == "Pending" && f.ThreadInfo.Actions.Count == 3, "a newer real snapshot is requested");
   f.ThreadInfo.Deliver(2);
   StartupSnapshot newer = (StartupSnapshot)Read();
   Check(newer != null && newer.Generation == first.Generation && newer.Revision > first.Revision &&
     newer.UnlockedCount == first.UnlockedCount + 1, "fresh fake snapshot reflects exactly one recorded writer call");
   bool[] changed = newer.GetUnlocked();
   for (int i = 0; i < changed.Length; i++)
    Check(changed[i] == (i == tile || old[i]), "only the fake candidate tile changes");
   Check(bridge.Confirm(newer) == Initial25DecisionKind.Confirmed && bridge.State == FakeBridgeState.Confirmed,
     "real run confirms only the newer fake snapshot");
   saved();
   Check(bridge.CallbackCount == 3 && f.AreaInfo.WriterCalls == 1 && !bridge.Start() &&
     f.AreaInfo.Inner.UnexpectedWrites == 0, "confirmed fake action replay cannot repeat the write");
   Console.WriteLine("FAKE_BRIDGE_CONFIRMED writes=1 requireMoney=False snapshot=Confirmed");
  } finally { f.Release(); }

  f = new RecordingFixture();
  try {
   f.AreaInfo.WriterEnabled = true;
   var bridge = new ScheduledFakeBridge(f.Snapshot, f.AreaInfo, f.ThreadInfo);
   Check(bridge.Start(), "old fake action queued");
   Action oldAction = f.ThreadInfo.Actions[1];
   f.Loading.OnLevelUnloading(); f.Loading.OnLevelLoaded(LoadMode.NewGame);
   Check(f.ThreadInfo.Actions.Count == 3, "new load queues its own fake snapshot");
   oldAction(); oldAction();
   Check(bridge.State == FakeBridgeState.Stopped && bridge.CallbackCount == 2 &&
     f.AreaInfo.WriterCalls == 0 && !bridge.Start(), "old fake action after unload cannot write or retry");
   f.ThreadInfo.Deliver(2);
   Check(Read() != null && !IsCurrent(f.Snapshot), "new fake snapshot is independent of old action");
   Console.WriteLine("FAKE_BRIDGE_OLD_ACTION callbacks=2 writes=0 stopped=True");
  } finally { f.Release(); }

  f = new RecordingFixture();
  try {
   f.AreaInfo.WriterEnabled = true; f.ThreadInfo.ThrowBefore = true;
   var bridge = new ScheduledFakeBridge(f.Snapshot, f.AreaInfo, f.ThreadInfo);
   Check(bridge.Start() && bridge.QueueException && bridge.State == FakeBridgeState.Unknown &&
     f.ThreadInfo.Actions.Count == 1 && f.AreaInfo.WriterCalls == 0 && !bridge.Start(),
     "queue exception before storage is unknown and cannot retry");
   Console.WriteLine("FAKE_BRIDGE_THROW_BEFORE saved=0 writes=0 state=Unknown");
  } finally { f.Release(); }

  f = new RecordingFixture();
  try {
   f.AreaInfo.WriterEnabled = true; f.ThreadInfo.ThrowAfter = true;
   var bridge = new ScheduledFakeBridge(f.Snapshot, f.AreaInfo, f.ThreadInfo);
   Check(bridge.Start() && bridge.QueueException && bridge.State == FakeBridgeState.Unknown &&
     f.ThreadInfo.Actions.Count == 2, "queue exception after storage is unknown");
   Action saved = f.ThreadInfo.Actions[1]; saved(); saved();
   Check(bridge.CallbackCount == 2 && f.AreaInfo.WriterCalls == 0 && bridge.State == FakeBridgeState.Unknown &&
     !bridge.Start(), "saved fake action after queue exception is disabled even when replayed");
   Console.WriteLine("FAKE_BRIDGE_THROW_SAVED callbacks=2 writes=0 state=Unknown");
  } finally { f.Release(); }

  f = new RecordingFixture();
  try {
   f.AreaInfo.WriterEnabled = true; f.ThreadInfo.Inline = true; f.ThreadInfo.ThrowAfter = true;
   var bridge = new ScheduledFakeBridge(f.Snapshot, f.AreaInfo, f.ThreadInfo);
   Check(bridge.Start() && bridge.QueueException && bridge.State == FakeBridgeState.Unknown &&
     f.AreaInfo.WriterCalls == 1 && bridge.CallbackCount == 1 && f.ThreadInfo.Actions.Count == 2,
     "inline fake writer ran before queue threw; outcome remains unknown");
   Action saved = f.ThreadInfo.Actions[1]; saved(); saved();
   Check(bridge.CallbackCount == 3 && f.AreaInfo.WriterCalls == 1 && !bridge.Start() &&
     bridge.State == FakeBridgeState.Unknown, "inline failure is not retried or rolled back");
   Console.WriteLine("FAKE_BRIDGE_THROW_INLINE callbacks=3 writes=1 state=Unknown");
  } finally { f.Release(); }

  f = new RecordingFixture();
  try {
   f.AreaInfo.WriterEnabled = true;
   var bridge = new ScheduledFakeBridge(f.Snapshot, f.AreaInfo, f.ThreadInfo);
   RecordingFixture unloading = f;
   int eventOrder = 0, unloadOrder = 0, writerEntryOrder = 0;
   unloading.AreaInfo.EntryInvalidObserver = delegate {
    writerEntryOrder = ++eventOrder;
    return !StartupSnapshots.IsCurrent(unloading.Snapshot);
   };
   bridge.BeforeWrite = delegate { unloading.Loading.OnLevelUnloading(); unloadOrder = ++eventOrder; };
   Check(bridge.Start(), "controlled unload-window fake action queued");
   f.ThreadInfo.Deliver(1);
   Check(bridge.LastCurrentBeforeWrite && !IsCurrent(f.Snapshot) && f.AreaInfo.WriterCalls == 1 &&
     bridge.State == FakeBridgeState.Unknown && !bridge.Start(),
     "unload after last current check still permits the controlled fake call and yields unknown");
   Check(unloadOrder == 1 && writerEntryOrder == 2 && unloading.AreaInfo.InvalidAtWriterEntry == true,
     "fake writer entry occurs after unload and sees the snapshot invalid");
   Console.WriteLine("FAKE_BRIDGE_WRITER_ENTRY unloadOrder=1 writerOrder=2 invalidAtEntry=True");
   Console.WriteLine("FAKE_BRIDGE_UNLOAD_WINDOW lastCurrent=True fakeWrites=1 state=Unknown");
  } finally { f.Release(); }
  f = new RecordingFixture();
  try {
   f.AreaInfo.WriterEnabled = true; f.ThreadInfo.ThrowAfter = true;
   var bridge = new ScheduledFakeBridge(f.Snapshot, f.AreaInfo, f.ThreadInfo);
   RecordingFixture concurrent = f;
   var entered = new ManualResetEvent(false); var resume = new ManualResetEvent(false);
   Thread worker = null; Exception workerError = null; bool drained = false;
   bridge.BeforeWrite = delegate {
    entered.Set();
    if (!resume.WaitOne(5000)) throw new InvalidOperationException("controlled fake callback release timed out");
   };
   f.ThreadInfo.BeforeReturn = delegate {
    worker = new Thread(delegate() {
     try { concurrent.ThreadInfo.Deliver(1); } catch (Exception exception) { workerError = exception; }
    });
    worker.IsBackground = true;
    worker.Start();
    if (!entered.WaitOne(3000)) throw new InvalidOperationException("controlled fake callback did not reach its barrier");
   };
   try {
    Check(bridge.Start() && bridge.QueueException && bridge.State == FakeBridgeState.Unknown &&
      f.AreaInfo.WriterCalls == 0, "queue exception becomes unknown while fake callback is paused");
   } finally {
    resume.Set();
    if (worker != null) drained = worker.Join(5000);
   }
   Console.WriteLine("FAKE_BRIDGE_QUEUE_RACE state=" + bridge.State + " fakeWrites=" + f.AreaInfo.WriterCalls +
     " drained=" + drained + " workerError=" + (workerError != null));
   Check(drained && workerError == null && bridge.State == FakeBridgeState.Unknown &&
     f.AreaInfo.WriterCalls == 1 && bridge.CallbackCount == 1 && !bridge.Start(),
     "unknown fake state must remain terminal after callback resumes");
   entered.Close(); resume.Close();
  } finally { f.Release(); }

  f = new RecordingFixture();
  try {
   f.AreaInfo.WriterEnabled = true; f.ThreadInfo.Inline = true; f.ThreadInfo.ThrowAfter = true;
   var bridge = new ScheduledFakeBridge(f.Snapshot, f.AreaInfo, f.ThreadInfo);
   RecordingFixture inlineFixture = f;
   Initial25DecisionKind? earlyConfirmation = null;
   bool beforeReturnEntered = false;
   f.ThreadInfo.BeforeReturn = delegate {
    beforeReturnEntered = true;
    inlineFixture.ThreadInfo.BeforeReturn = null;
    inlineFixture.ThreadInfo.ThrowAfter = false;
    Check(Request() == "ObservedUnverified", "inline fake snapshot publishes during outer queue");
    StartupSnapshot newer = (StartupSnapshot)Read();
    Check(newer != null && newer.Generation == inlineFixture.Snapshot.Generation &&
      newer.Revision > inlineFixture.Snapshot.Revision &&
      newer.UnlockedCount == inlineFixture.Snapshot.UnlockedCount + 1,
      "controlled early-confirmation snapshot is newer and includes the fake write");
    earlyConfirmation = bridge.Confirm(newer);
   };
   Check(bridge.Start(), "inline throwing fake queue was attempted");
   Console.WriteLine("FAKE_BRIDGE_EARLY_CONFIRM result=" + earlyConfirmation + " final=" + bridge.State +
     " writes=" + f.AreaInfo.WriterCalls + " queueException=" + bridge.QueueException);
   Check(beforeReturnEntered && earlyConfirmation == Initial25DecisionKind.Unavailable &&
     bridge.QueueException && bridge.State == FakeBridgeState.Unknown && f.AreaInfo.WriterCalls == 1,
     "fake confirmation cannot occur before queue returned successfully");
   Action saved = f.ThreadInfo.Actions[1]; saved();
   Check(bridge.CallbackCount == 2 && f.AreaInfo.WriterCalls == 1 && !bridge.Start() &&
     bridge.State == FakeBridgeState.Unknown, "early-confirmation fake outcome remains unknown without retry");
  } finally { f.Release(); }

  Console.WriteLine("Passed " + (checks - before) +
    " integrated fake bridge checks; no production submission or game validation.");
 }

 public static void Run() {
  snapshots = typeof(Observation).Assembly.GetType("FullMapUnlockAndExpansion.StartupSnapshots");
  threadingExtension = typeof(Observation).Assembly.GetType("FullMapUnlockAndExpansion.Threading");
  if (snapshots == null || threadingExtension == null) throw new Exception("read-only startup snapshot adapter is missing");
  CheckModesAndOrder(); CheckAmbiguousLateCreation(); CheckLoadingCreationFailure(); CheckSchedulingAndLifecycle(); CheckInvalidationAndSnapshot(); CheckPauseRetries(); CheckFakeModelBridge();
  CheckIntegratedFakeBridge();
  Console.WriteLine("Passed " + checks + " synthetic startup snapshot checks; provenance and real game behavior remain unverified.");
 }
}
