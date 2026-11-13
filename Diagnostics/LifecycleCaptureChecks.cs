// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic callbacks only; no game, file, clipboard or network access.
using System;
using System.Reflection;
using System.Threading;
using ICities;

class LifecycleCaptureChecks {
 static int checks;
 static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
#if FULL_MAP_LIFECYCLE_DIAGNOSTIC && FULL_MAP_LIFECYCLE_DIAGNOSTIC_TEST && !RELEASE && !FULLMAP_RELEASE
 sealed class TrapLoading : ILoading {
  public int GetterCalls;
  public bool loadingComplete { get { GetterCalls++; throw new Exception("unexpected loading read"); } }
  public AppMode currentMode { get { GetterCalls++; throw new Exception("unexpected loading read"); } }
 }
 static void Run() {
  FullMapLifecycleCapture.CaptureBuffer capture = new FullMapLifecycleCapture.CaptureBuffer();
  FullMapLifecycleCapture.LoadingCapture probe = new FullMapLifecycleCapture.LoadingCapture(capture);
  TrapLoading loading = new TrapLoading();
  Check(probe is ILoadingExtension, "candidate implements only the loading callback contract");
  Check(typeof(FullMapLifecycleCapture.LoadingCapture).BaseType == typeof(object),
      "candidate does not inherit a base class that retains ILoading");
  foreach (FieldInfo field in typeof(FullMapLifecycleCapture.LoadingCapture).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
   Check(field.FieldType != typeof(ILoading), "candidate does not retain an ILoading provider field");
  probe.OnCreated(loading); probe.OnLevelLoaded(LoadMode.NewGame); probe.OnLevelUnloading(); probe.OnReleased();
  Check(capture.SnapshotForSyntheticChecks().Length == 0, "callbacks before explicit synthetic activation are ignored");
  Check(loading.GetterCalls == 0, "loading provider data is not read");
  Check(capture.BeginForSyntheticChecks(), "explicit synthetic activation starts collection");
  probe.OnLevelLoaded(LoadMode.NewGame);
  FullMapLifecycleCapture.CaptureEntry[] first = capture.SnapshotForSyntheticChecks();
  Check(first.Length == 1 && first[0].EventCode == 2 && first[0].Instance == 1 && first[0].Generation == 1 && first[0].Mode == 1,
      "first observed load is not fabricated as a prior creation event");
  FullMapLifecycleCapture.LoadingCapture other = new FullMapLifecycleCapture.LoadingCapture(capture);
  other.OnCreated(loading); probe.OnLevelUnloading(); other.OnReleased();
  FullMapLifecycleCapture.CaptureEntry[] next = capture.SnapshotForSyntheticChecks();
  Check(next.Length == 4 && next[1].EventCode == 1 && next[1].Instance == 2 && next[1].Generation == 0,
      "actual created callback receives a distinct local instance label");
  Check(next[2].EventCode == 3 && next[2].Instance == 1 && next[2].Generation == 1 &&
      next[3].EventCode == 4 && next[3].Instance == 2, "unload and release retain local labels");
  for (int i = 0; i < next.Length; i++) Check(next[i].Sequence == i + 1, "relative event sequence is monotonic");
  next[0] = default(FullMapLifecycleCapture.CaptureEntry);
  Check(capture.SnapshotForSyntheticChecks()[0].EventCode == 2, "snapshot array is detached");
  capture.StopForSyntheticChecks();
  probe.OnLevelLoaded(LoadMode.LoadGame); other.OnCreated(loading);
  Check(capture.SnapshotForSyntheticChecks().Length == 4, "stop prevents additional capture");
  Check(capture.BeginForSyntheticChecks(), "synthetic reactivation requires another explicit action");
  probe.OnLevelLoaded(LoadMode.LoadGame);
  FullMapLifecycleCapture.CaptureEntry[] resumed = capture.SnapshotForSyntheticChecks();
  Check(resumed.Length == 5 && resumed[4].Instance == 3 && resumed[4].Generation == 2 && resumed[4].Mode == 2,
      "a later collection interval assigns new local labels");
  probe.OnLevelLoaded(LoadMode.NewGameFromScenario); probe.OnLevelLoaded((LoadMode)999);
  resumed = capture.SnapshotForSyntheticChecks();
  Check(resumed[5].Mode == 3 && resumed[6].Mode == 4, "unknown modes are reduced to fixed codes");
  Check(loading.GetterCalls == 0, "provider getters remain untouched after all callbacks");

  FullMapLifecycleCapture.CaptureBuffer stale = new FullMapLifecycleCapture.CaptureBuffer();
  FullMapLifecycleCapture.LoadingCapture staleProbe = new FullMapLifecycleCapture.LoadingCapture(stale);
  Check(stale.BeginForSyntheticChecks(), "stale-callback fixture starts explicitly");
  Action delayedCallback = delegate { staleProbe.OnLevelLoaded(LoadMode.NewGame); };
  stale.StopForSyntheticChecks();
  Check(stale.BeginForSyntheticChecks(), "stale-callback fixture starts a later interval");
  delayedCallback();
  FullMapLifecycleCapture.CaptureEntry[] delayed = stale.SnapshotForSyntheticChecks();
  Check(delayed.Length == 1 && delayed[0].EventCode == 2 && delayed[0].Instance == 1 && delayed[0].Generation == 1,
      "a synthetic callback prepared before stop is indistinguishable after restart");

  FieldInfo[] fields = typeof(FullMapLifecycleCapture.CaptureEntry).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
  Check(fields.Length == 5, "records contain exactly five fields");
  foreach (FieldInfo field in fields) Check(field.IsInitOnly && field.FieldType == typeof(int),
      "each event field is a read-only integer without a dedicated text or object field");
  Check(typeof(FullMapLifecycleCapture.LifecycleCaptureMod).GetMethod("OnSettingsUI") == null,
      "unverified game UI is not connected");
  Check(typeof(FullMapLifecycleCapture.CaptureBuffer).GetMethod("BeginForSyntheticChecks", BindingFlags.Instance | BindingFlags.Public) == null,
      "synthetic activation is not public");

  FullMapLifecycleCapture.CaptureBuffer bounded = new FullMapLifecycleCapture.CaptureBuffer();
  FullMapLifecycleCapture.LoadingCapture boundedProbe = new FullMapLifecycleCapture.LoadingCapture(bounded);
  Check(bounded.BeginForSyntheticChecks(), "bounded fixture starts explicitly");
  for (int i = 0; i < 65; i++) boundedProbe.OnCreated(loading);
  Check(bounded.SnapshotForSyntheticChecks().Length == 64 && bounded.OverflowedForSyntheticChecks,
      "collection is capped at 64 events and reports truncation");
  Check(!bounded.BeginForSyntheticChecks(), "overflow cannot silently resume collection");

  FullMapLifecycleCapture.CaptureBuffer invalid = new FullMapLifecycleCapture.CaptureBuffer();
  Check(invalid.BeginForSyntheticChecks(), "invalid-event fixture starts explicitly");
  FullMapLifecycleCapture.CaptureBuffer.Handle invalidHandle = invalid.NewHandle();
  invalidHandle.Record(999, LoadMode.NewGame);
  Check(invalid.SnapshotForSyntheticChecks().Length == 0, "unknown event codes cannot create a record");
  invalidHandle.Record(1, LoadMode.NewGame);
  Check(invalid.SnapshotForSyntheticChecks()[0].Instance == 1, "unknown events do not reserve instance labels");
  Check(typeof(FullMapLifecycleCapture.CaptureBuffer).GetMethod("Record", BindingFlags.NonPublic | BindingFlags.Instance).IsPrivate,
      "callers cannot supply numeric instance or generation fields through the buffer API");
  foreach (FieldInfo field in typeof(FullMapLifecycleCapture.CaptureBuffer.Handle).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
   Check(field.IsPrivate, "handle state is private to the candidate");

  FullMapLifecycleCapture.CaptureBuffer instanceBound = new FullMapLifecycleCapture.CaptureBuffer();
  Check(instanceBound.BeginForSyntheticChecks(), "instance fixture starts explicitly");
  for (int i = 0; i < 33; i++) new FullMapLifecycleCapture.LoadingCapture(instanceBound).OnCreated(loading);
  Check(instanceBound.SnapshotForSyntheticChecks().Length == 32 && instanceBound.OverflowedForSyntheticChecks,
      "instance labels are bounded and exhaustion stops collection");

  FullMapLifecycleCapture.CaptureBuffer generationBound = new FullMapLifecycleCapture.CaptureBuffer();
  FullMapLifecycleCapture.LoadingCapture generationProbe = new FullMapLifecycleCapture.LoadingCapture(generationBound);
  Check(generationBound.BeginForSyntheticChecks(), "generation fixture starts explicitly");
  for (int i = 0; i < 33; i++) generationProbe.OnLevelLoaded(LoadMode.NewGame);
  Check(generationBound.SnapshotForSyntheticChecks().Length == 32 && generationBound.OverflowedForSyntheticChecks,
      "generation labels are bounded and exhaustion stops collection");

  FullMapLifecycleCapture.CaptureBuffer concurrent = new FullMapLifecycleCapture.CaptureBuffer();
  FullMapLifecycleCapture.LoadingCapture sharedProbe = new FullMapLifecycleCapture.LoadingCapture(concurrent);
  Check(concurrent.BeginForSyntheticChecks(), "concurrent fixture starts explicitly");
  ManualResetEvent start = new ManualResetEvent(false);
  Thread[] threads = new Thread[4]; Exception[] errors = new Exception[4];
  for (int i = 0; i < threads.Length; i++) {
   int slot = i;
   threads[i] = new Thread(delegate() {
    try { start.WaitOne(); for (int j = 0; j < 10; j++) sharedProbe.OnCreated(loading); }
    catch (Exception error) { errors[slot] = error; }
   });
   threads[i].Start();
  }
  start.Set(); foreach (Thread thread in threads) thread.Join(); start.Close();
  foreach (Exception error in errors) Check(error == null, "concurrent synthetic callback does not throw");
  FullMapLifecycleCapture.CaptureEntry[] ordered = concurrent.SnapshotForSyntheticChecks();
  Check(ordered.Length == 40, "all bounded concurrent events are captured");
  for (int i = 0; i < ordered.Length; i++)
   Check(ordered[i].Sequence == i + 1 && ordered[i].Instance == 1, "concurrent recording order and instance labels are consistent");
  Console.WriteLine("Passed " + checks + " lifecycle capture checks using a fake ICities boundary.");
 }
#elif FULL_MAP_LIFECYCLE_DIAGNOSTIC && !RELEASE && !FULLMAP_RELEASE
 sealed class TrapLoading : ILoading {
  public int GetterCalls;
  public bool loadingComplete { get { GetterCalls++; throw new Exception("unexpected loading read"); } }
  public AppMode currentMode { get { GetterCalls++; throw new Exception("unexpected loading read"); } }
 }
 static void Run() {
  TrapLoading loading = new TrapLoading();
  FullMapLifecycleCapture.LoadingCapture probe = new FullMapLifecycleCapture.LoadingCapture();
  probe.OnCreated(loading); probe.OnLevelLoaded(LoadMode.NewGame); probe.OnLevelUnloading(); probe.OnReleased();
  object buffer = FullMapLifecycleCapture.CaptureRuntime.Buffer;
  Type type = buffer.GetType();
  FieldInfo enabled = type.GetField("enabled", BindingFlags.NonPublic | BindingFlags.Instance);
  FieldInfo count = type.GetField("count", BindingFlags.NonPublic | BindingFlags.Instance);
  Check(!(bool)enabled.GetValue(buffer) && (int)count.GetValue(buffer) == 0,
      "production-shaped candidate stays disabled through lifecycle callbacks");
  Check(loading.GetterCalls == 0, "production-shaped callback does not read provider data");
  Check(type.GetMethod("BeginForSyntheticChecks", BindingFlags.NonPublic | BindingFlags.Instance) == null &&
      type.GetMethod("SnapshotForSyntheticChecks", BindingFlags.NonPublic | BindingFlags.Instance) == null,
      "production-shaped candidate omits synthetic activation and read controls");
  Check(typeof(FullMapLifecycleCapture.LifecycleCaptureMod).GetMethod("OnSettingsUI") == null,
      "production-shaped candidate has no unverified game UI");
  Console.WriteLine("Passed " + checks + " default-disabled production-shape checks using a fake ICities boundary.");
 }
#else
 static void Run() {
  Assembly assembly = typeof(LifecycleCaptureChecks).Assembly;
  Check(assembly.GetType("FullMapLifecycleCapture.LoadingCapture") == null, "loading capture type is absent without the diagnostic flag or in release");
  Check(assembly.GetType("FullMapLifecycleCapture.LifecycleCaptureMod") == null, "diagnostic mod type is absent without the diagnostic flag or in release");
  Console.WriteLine("Passed " + checks + " compile-flag exclusion checks.");
 }
#endif
 static void Main() { Run(); }
}
