// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic authority boundary only; no game, file, network or real unlock.
using System;
using System.Reflection;
using ICities;
using FullMapUnlockAndExpansion;
using FullMapLifecycleCapture;

// Supplement the existing fake ICities boundary for this isolated test only.
namespace ICities {
 public interface ILoadingExtension {
  void OnCreated(ILoading loading);
  void OnReleased();
  void OnLevelLoaded(LoadMode mode);
  void OnLevelUnloading();
 }
}

static class LifecycleAuthorityBoundaryChecks {
 static int checks;
 static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
 sealed class FakeLoading : ILoading {
  public int Reads;
  public bool loadingComplete { get { Reads++; return true; } }
  public AppMode currentMode { get { Reads++; return AppMode.Game; } }
 }
 sealed class FakeAreas : IAreas {
  public int Reads, Writes;
  public int unlockedAreaCount { get { Reads++; return 1; } }
  public bool IsAreaUnlocked(int x, int z) { Reads++; return x == 2 && z == 2; }
  public void UnlockArea(int x, int z, bool requireMoney) { Writes++; throw new Exception("unexpected synthetic native write"); }
 }
 sealed class FakeThreading : IThreading {
  public int Queued, PauseReads;
  readonly Action[] pending = new Action[4];
  public bool simulationPaused { get { PauseReads++; return false; } }
  public void QueueSimulationThread(Action action) {
   if (Queued == pending.Length) throw new Exception("fake queue capacity exceeded");
   pending[Queued++] = action;
  }
  public void Deliver(int index) { pending[index](); }
 }
 // Test-only page registration. This does not implement or authenticate a game UI.
 sealed class SyntheticPageGate {
  internal sealed class Page {
   internal Action Click;
   internal readonly CaptureBuffer Capture;
   internal Page(CaptureBuffer capture) { Capture = capture; }
  }
  readonly object gate = new object();
  Page current;
  bool ready;
  internal Page Register(CaptureBuffer capture, bool clickDuringRegistration) {
   lock (gate) {
    if (current != null) current.Capture.StopForSyntheticChecks();
    if (capture == null) { current = null; ready = false; return null; }
    Page page = new Page(capture); current = page; ready = false;
    page.Click = delegate {
     lock (gate) {
      if (object.ReferenceEquals(current, page) && ready) capture.BeginForSyntheticChecks();
     }
    };
    if (clickDuringRegistration) page.Click();
    return page;
   }
  }
  internal void MarkReady(Page page) {
   lock (gate) { if (page != null && object.ReferenceEquals(current, page)) ready = true; }
  }
  internal void Close(Page page) {
   lock (gate) {
    if (page != null && object.ReferenceEquals(current, page)) { current = null; ready = false; page.Capture.StopForSyntheticChecks(); }
   }
  }
 }
 static void AssertDenied(CaptureEntry entry, FakeLoading loading, FakeAreas areas, FakeThreading threading, int queuedBefore) {
  bool[] syntheticGrid = new bool[25]; syntheticGrid[12] = true;
  StartupSnapshot claimed = new StartupSnapshot(entry.Generation, 1, 1, LoadMode.NewGame, syntheticGrid);
  NativeSubmissionRequest request = new NativeSubmissionRequest(entry.Generation, 1, 1, 2, 1, true, true);
  Check(!claimed.ProvenanceVerified && !claimed.SimulationThreadVerified && !claimed.SimulationRunningVerified,
      "a claimed snapshot derived from local diagnostic labels remains unverified");
  Initial25NoDebitDecision decision = StartupSnapshots.PrepareNativeSubmission(claimed);
  Check(decision.Kind == Initial25DecisionKind.Unavailable && !decision.NativeIndex.HasValue &&
      !decision.AttemptId.HasValue && !decision.RequireMoney.HasValue,
      "the main preparation boundary returns no actionable fields for a diagnostic-derived snapshot");
  Check(new DisabledNativeStartupSubmission().Submit(claimed, request) == NativeSubmissionAvailability.Unavailable,
      "the built-in submission boundary rejects claimed diagnostic-derived authority");
  Check(areas.Writes == 0 && areas.Reads == 0 && loading.Reads == 0 &&
      threading.PauseReads == 0 && threading.Queued == queuedBefore,
      "forged diagnostic events and rejected requests cause no fake provider access or extra scheduling");
 }
 static void Main() {
  FakeLoading loading = new FakeLoading(); FakeAreas areas = new FakeAreas(); FakeThreading threading = new FakeThreading();
  Loading loadingExtension = new Loading(); Areas areasExtension = new Areas(); Threading threadingExtension = new Threading();
  loadingExtension.OnCreated(loading); areasExtension.OnCreated(areas); threadingExtension.OnCreated(threading);
  loadingExtension.OnLevelLoaded(LoadMode.NewGame);
  Check(threading.Queued == 1 && areas.Reads == 0 && areas.Writes == 0,
      "the main observation has one undelivered fake action and no fake area access");
  FakeAreas trapControl = new FakeAreas();
  try { ((IAreas)trapControl).UnlockArea(2, 1, false); } catch (Exception) { }
  Check(trapControl.Writes == 1, "the separate fake IAreas write trap detects interface dispatch");

  CaptureBuffer closeCapture = new CaptureBuffer(); LoadingCapture closeProbe = new LoadingCapture(closeCapture);
  SyntheticPageGate closeUi = new SyntheticPageGate();
  SyntheticPageGate.Page currentPage = closeUi.Register(closeCapture, false);
  closeUi.MarkReady(currentPage); currentPage.Click(); closeProbe.OnCreated(loading);
  Check(closeCapture.SnapshotForSyntheticChecks().Length == 1, "the current synthetic page has active capture before close");
  closeUi.Close(currentPage); closeProbe.OnLevelLoaded(LoadMode.NewGame);
  Check(closeCapture.SnapshotForSyntheticChecks().Length == 1, "closing the active synthetic page stops subsequent callbacks");
  closeUi.MarkReady(currentPage); currentPage.Click(); closeProbe.OnCreated(loading);
  Check(closeCapture.SnapshotForSyntheticChecks().Length == 1, "closing the active synthetic page invalidates its old click");

  CaptureBuffer capture = new CaptureBuffer(); LoadingCapture probe = new LoadingCapture(capture);
  SyntheticPageGate ui = new SyntheticPageGate();
  Check(ui.Register(null, true) == null, "a null synthetic capture cannot register a page");
  FieldInfo captureField = typeof(SyntheticPageGate.Page).GetField("Capture", BindingFlags.NonPublic | BindingFlags.Instance);
  Check(captureField != null && captureField.IsInitOnly, "a synthetic page cannot replace its capture buffer");
  ui.Close(null); ui.MarkReady(null);
  Check(capture.SnapshotForSyntheticChecks().Length == 0, "null synthetic page events leave capture disabled");
  SyntheticPageGate.Page firstPage = ui.Register(capture, true);
  probe.OnCreated(loading);
  Check(capture.SnapshotForSyntheticChecks().Length == 0, "a callback during synthetic page registration cannot enable capture");
  ui.MarkReady(firstPage);
  firstPage.Click();
  probe.OnCreated(loading); probe.OnLevelLoaded(LoadMode.NewGame);
  CaptureEntry[] spoofed = capture.SnapshotForSyntheticChecks();
  Check(spoofed.Length == 2 && spoofed[1].EventCode == 2,
      "a programmatic current-page click and fabricated callbacks can enter synthetic capture");
  int queuedBefore = threading.Queued;
  AssertDenied(spoofed[1], loading, areas, threading, queuedBefore);

  Check(ui.Register(null, false) == null, "a failed synthetic replacement returns no new page");
  probe.OnCreated(loading);
  Check(capture.SnapshotForSyntheticChecks().Length == 2, "a failed synthetic replacement stops active capture");
  ui.MarkReady(firstPage); firstPage.Click(); probe.OnCreated(loading);
  Check(capture.SnapshotForSyntheticChecks().Length == 2, "a failed synthetic replacement invalidates the old page callback");

  Action stale = delegate { probe.OnLevelLoaded(LoadMode.NewGame); };
  ui.Close(firstPage); firstPage.Click(); probe.OnCreated(loading);
  Check(capture.SnapshotForSyntheticChecks().Length == 2, "an already invalid synthetic page remains inactive");
  SyntheticPageGate.Page secondPage = ui.Register(capture, false);
  ui.MarkReady(firstPage); secondPage.Click(); probe.OnCreated(loading);
  Check(capture.SnapshotForSyntheticChecks().Length == 2, "an old page cannot mark its replacement ready");
  ui.MarkReady(secondPage);
  firstPage.Click(); probe.OnCreated(loading);
  Check(capture.SnapshotForSyntheticChecks().Length == 2, "a callback from the replaced synthetic page cannot restart capture");
  secondPage.Click();
  stale();
  CaptureEntry[] delayed = capture.SnapshotForSyntheticChecks();
  Check(delayed.Length == 3 && delayed[2].EventCode == 2,
      "an earlier synthetic work item can appear in a later diagnostic interval");
  AssertDenied(delayed[2], loading, areas, threading, queuedBefore);
  SyntheticPageGate.Page thirdPage = ui.Register(capture, false);
  probe.OnCreated(loading);
  Check(capture.SnapshotForSyntheticChecks().Length == 3,
      "replacing a synthetic page stops the previous capture before a new page is ready");
  ui.Close(thirdPage);
  loadingExtension.OnLevelUnloading();
  threading.Deliver(0);
  Check(areas.Reads == 0 && areas.Writes == 0 && loading.Reads == 0 && threading.PauseReads == 0 &&
      StartupSnapshots.Read() == null,
      "a fake action delivered after unload cannot read providers, publish a snapshot or write");
  Console.WriteLine("Passed " + checks + " forged and delayed diagnostic authority-boundary checks; fake interfaces only.");
 }
}
