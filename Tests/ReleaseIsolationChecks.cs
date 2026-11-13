// OpenAI-Codex_release_isolation
// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic release-shape and ownership-quarantine checks; no game is executed.
using System;
using System.Reflection;
using FullMapUnlockAndExpansion;
using ICities;

static class ReleaseIsolationChecks {
 static int checks;
 static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
 sealed class FakeLoading : ILoading {
  public bool loadingComplete { get { return true; } }
  public AppMode currentMode { get { return AppMode.Game; } }
 }
 static void Main() {
  Assembly assembly = typeof(Observation).Assembly;
  string[] absent = new string[] {
   "FullMapUnlockAndExpansion.LoadingOwnershipDiagnostics",
   "FullMapUnlockAndExpansion.DiagnosticSignal",
   "FullMapUnlockAndExpansion.DiagnosticOutcome",
   "FullMapUnlockAndExpansion.DiagnosticRecord",
   "FullMapUnlockAndExpansion.RedactedDiagnosticBuffer",
   "FullMapUnlockAndExpansion.LifecycleDiagnostics.LifecycleTraceCandidate",
   "FullMapLifecycleCapture.LifecycleCaptureMod",
   "FullMapLifecycleCapture.LoadingCapture"
  };
  foreach (string name in absent) Check(assembly.GetType(name) == null, "release must exclude " + name);
  Check(typeof(Observation).GetMethod("ReadLoadingOwnership", BindingFlags.Public | BindingFlags.Static) == null,
      "release must not expose the test-only ownership diagnostic getter");

  var one = new Loading(); var two = new Loading(); var three = new Loading();
  one.OnCreated(new FakeLoading()); one.OnLevelLoaded(LoadMode.NewGame);
  Check(StartupSnapshots.Status == StartupSnapshotStatus.WaitingForDependencies, "one owner can wait for dependencies");
  two.OnCreated(new FakeLoading());
  Check(StartupSnapshots.Status == StartupSnapshotStatus.OwnerAmbiguous && StartupSnapshots.Read() == null,
      "overlap quarantines observation without a diagnostic getter");
  one.OnReleased();
  Check(StartupSnapshots.Status == StartupSnapshotStatus.OwnerAmbiguous, "one remaining owner does not clear quarantine");
  three.OnCreated(new FakeLoading()); two.OnReleased();
  Check(StartupSnapshots.Status == StartupSnapshotStatus.OwnerAmbiguous, "a later overlapping owner remains quarantined");
  three.OnReleased();
  Check(StartupSnapshots.Status == StartupSnapshotStatus.Unavailable, "all known owners must release before quarantine clears");
  var fresh = new Loading(); fresh.OnCreated(new FakeLoading()); fresh.OnLevelLoaded(LoadMode.NewGame);
  Check(StartupSnapshots.Status == StartupSnapshotStatus.WaitingForDependencies, "fresh owner may begin after release");
  fresh.OnReleased();
  Console.WriteLine("Passed " + checks + " synthetic release isolation and quarantine checks; no game tested.");
 }
}
