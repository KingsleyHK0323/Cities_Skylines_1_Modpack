// OpenAI-Codex_Full_Map_Unlock_And_Expansion
using System;
using System.Threading;
using ICities;
using FullMapUnlockAndExpansion;
class LifecycleChecks {
 static int checks;
 static void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
 static Loading NewLoading() { var value = new Loading(); value.OnCreated(null); return value; }
 static int Price(Areas a, int value) { return a.OnGetAreaPrice(0,0,0,0,0,false,false,false,false,0,value); }
 static bool IsCurrent(AreaObservation observation) {
  var method = typeof(Observation).GetMethod("IsCurrent");
  if (method == null) throw new Exception("Observation.IsCurrent implementation is missing");
  return (bool)method.Invoke(null, new object[] { observation });
 }
 public static void CheckReleasedLoading() {
  var areas = new Areas(); areas.OnCreated(null);
  var oldLoading = NewLoading(); oldLoading.OnLevelLoaded(LoadMode.NewGame);
  oldLoading.OnReleased();
  oldLoading.OnLevelLoaded(LoadMode.LoadGame);
  areas.OnCanUnlockArea(1,2,true);
  Check(Observation.Read() == null, "released loading must not reactivate observation");

  var newLoading = NewLoading(); newLoading.OnLevelLoaded(LoadMode.LoadGame);
  areas.OnCanUnlockArea(2,3,true); var current = Observation.Read();
  Check(current != null, "new loading observes normally");
  oldLoading.OnLevelLoaded(LoadMode.NewGame);
  oldLoading.OnLevelUnloading();
  Check(object.ReferenceEquals(current, Observation.Read()), "released loading cannot steal and unload new city");
  newLoading.OnReleased(); areas.OnReleased();
 }
 public static void CheckSupersededLoading() {
  var areas = new Areas(); areas.OnCreated(null);
  var oldLoading = NewLoading(); oldLoading.OnLevelLoaded(LoadMode.NewGame);
  var newLoading = NewLoading(); newLoading.OnLevelLoaded(LoadMode.LoadGame);
  areas.OnCanUnlockArea(1,2,true);
  oldLoading.OnLevelLoaded(LoadMode.NewGame);
  Check(Observation.Read() == null, "overlapping unreleased loading owners remain ambiguous");
  newLoading.OnLevelUnloading(); areas.OnCanUnlockArea(1,2,true);
  Check(Observation.Read() == null, "ambiguous owner unload does not reactivate observation");
  newLoading.OnLevelLoaded(LoadMode.NewGame); areas.OnCanUnlockArea(2,3,true);
  Check(Observation.Read() == null, "overlapping owner cannot resume merely by reloading");
  newLoading.OnReleased(); oldLoading.OnReleased();
  var fresh = NewLoading(); fresh.OnLevelLoaded(LoadMode.NewGame); fresh.OnLevelUnloading();
  fresh.OnLevelLoaded(LoadMode.LoadGame); areas.OnCanUnlockArea(2,3,true);
  Check(Observation.Read() != null, "fresh owner may reload after an explicit unload");
  fresh.OnReleased(); areas.OnReleased();
 }
 public static void CheckCreatedOrder() {
  var areas = new Areas(); areas.OnCreated(null);
  var oldLoading = new Loading(); oldLoading.OnCreated(null);
  var newLoading = new Loading(); newLoading.OnCreated(null); newLoading.OnLevelLoaded(LoadMode.LoadGame);
  areas.OnCanUnlockArea(2,3,true);
  oldLoading.OnLevelLoaded(LoadMode.NewGame);
  Check(Observation.Read() == null, "old created loading cannot claim its first late load");
  newLoading.OnLevelUnloading(); areas.OnCanUnlockArea(2,3,true);
  Check(Observation.Read() == null, "overlapping owner unload does not end ambiguity");
  newLoading.OnReleased(); oldLoading.OnReleased();
  var fresh = NewLoading(); fresh.OnLevelLoaded(LoadMode.NewGame);
  var newAreas = new Areas(); newAreas.OnCreated(null); newAreas.OnCanUnlockArea(3,4,true); var current = Observation.Read();
  areas.OnCreated(null); areas.OnCanUnlockArea(1,1,true);
  Check(object.ReferenceEquals(current, Observation.Read()), "old area instance cannot re-register after replacement");
  fresh.OnReleased(); areas.OnReleased(); newAreas.OnReleased();
 }
 public static void CheckUncreatedLoading() {
  var areas = new Areas(); areas.OnCreated(null);
  var currentLoading = NewLoading(); currentLoading.OnLevelLoaded(LoadMode.LoadGame);
  areas.OnCanUnlockArea(1,2,true); var current = Observation.Read();
  var uncreated = new Loading(); uncreated.OnLevelLoaded(LoadMode.NewGame);
  Check(object.ReferenceEquals(current, Observation.Read()), "uncreated loading cannot replace current owner");
  currentLoading.OnLevelUnloading(); areas.OnCanUnlockArea(1,2,true);
  Check(Observation.Read() == null, "current owner still unloads after uncreated callback");
  currentLoading.OnReleased(); uncreated.OnReleased(); areas.OnReleased();
 }
 public static void CheckReleasedAreas() {
  var loading = NewLoading(); loading.OnLevelLoaded(LoadMode.NewGame);
  var old = new Areas(); old.OnCreated(null); old.OnReleased();
  var currentProvider = new Areas(); currentProvider.OnCreated(null);
  currentProvider.OnCanUnlockArea(2,3,true); var current = Observation.Read();
  Check(current != null, "new area provider observes normally");
  old.OnCreated(null);
  Check(!old.OnCanUnlockArea(4,4,false), "released provider preserves false native result");
  Check(Price(old,-17) == -17, "released provider preserves native price");
  Check(object.ReferenceEquals(current, Observation.Read()), "released provider cannot bind or record again");
  loading.OnReleased(); currentProvider.OnReleased();
 }
 static void CheckConcurrentRelease() {
  for (int i = 0; i < 64; i++) {
   var area = new Areas(); area.OnCreated(null);
   var loading = NewLoading(); var start = new ManualResetEvent(false);
   Exception loadError = null, releaseError = null;
   var loadThread = new Thread(delegate() { try { start.WaitOne(); loading.OnLevelLoaded(LoadMode.NewGame); } catch (Exception ex) { loadError = ex; } });
   var releaseThread = new Thread(delegate() { try { start.WaitOne(); loading.OnReleased(); } catch (Exception ex) { releaseError = ex; } });
   loadThread.Start(); releaseThread.Start(); start.Set(); loadThread.Join(); releaseThread.Join(); start.Close();
   Check(loadError == null && releaseError == null, "concurrent loading callbacks do not throw");
   area.OnCanUnlockArea(1,1,true);
   Check(Observation.Read() == null, "released loading remains inactive after concurrent load");
   area.OnReleased();
  }
  var currentLoading = NewLoading(); currentLoading.OnLevelLoaded(LoadMode.LoadGame);
  for (int i = 0; i < 64; i++) {
   var area = new Areas(); var start = new ManualResetEvent(false);
   Exception createError = null, releaseError = null;
   var createThread = new Thread(delegate() { try { start.WaitOne(); area.OnCreated(null); } catch (Exception ex) { createError = ex; } });
   var releaseThread = new Thread(delegate() { try { start.WaitOne(); area.OnReleased(); } catch (Exception ex) { releaseError = ex; } });
   createThread.Start(); releaseThread.Start(); start.Set(); createThread.Join(); releaseThread.Join(); start.Close();
   Check(createError == null && releaseError == null, "concurrent provider callbacks do not throw");
   Check(area.OnCanUnlockArea(1,1,true), "released provider returns original true");
   Check(Observation.Read() == null, "released provider remains inactive after concurrent creation");
  }
  currentLoading.OnReleased();
 }
 static void Main() {
  var first = new Areas(); first.OnCreated(null);
  Check(!first.OnCanUnlockArea(1,2,false), "refusal preserved before load");
  Check(Observation.Read() == null, "inactive");
  var loading = NewLoading(); loading.OnLevelLoaded(LoadMode.NewGame);
  Check(first.OnCanUnlockArea(1,2,true), "true unchanged");
  var captured = Observation.Read();
  Check(captured != null && captured.X == 1 && captured.Z == 2 && captured.Eligibility == true && captured.Price == null, "eligibility only");
  Check(IsCurrent(captured), "latest observation is current");
  Check(!IsCurrent(null), "null is never current");
  Check(Price(first,0) == 0, "zero price unchanged");
  Check(Observation.Read().Price == 0 && Observation.Read().X == null && Observation.Read().Eligibility == null, "price not joined");
  Check(!IsCurrent(captured) && captured.Eligibility == true, "superseded reference remains readable but not current");
  var priceCapture = Observation.Read();
  Check(IsCurrent(priceCapture), "latest price observation is current");
  Check(Price(first,-1) == -1, "negative native price unchanged");
  loading.OnLevelUnloading(); Check(Observation.Read() == null, "unloaded");
  Check(!IsCurrent(priceCapture) && priceCapture.Price == 0, "unloaded reference remains readable but not current");
  first.OnCanUnlockArea(1,2,true); Check(Observation.Read() == null, "late area callback ignored");
  loading.OnLevelLoaded(LoadMode.LoadGame); Check(Observation.Read() == null, "new city empty");
  first.OnCanUnlockArea(1,2,false);
  Check(Observation.Read().Generation != captured.Generation && Observation.Read().Eligibility == false, "generation changed");
  var replacement = new Areas(); replacement.OnCreated(null);
  Check(Observation.Read() == null, "provider replacement clears");
  first.OnCanUnlockArea(1,2,true); Check(Observation.Read() == null, "old provider ignored");
  replacement.OnCanUnlockArea(2,3,true); var current = Observation.Read();
  first.OnReleased(); Check(object.ReferenceEquals(current, Observation.Read()), "old provider release isolated");
  loading.OnReleased();
  var nextLoading = NewLoading(); nextLoading.OnLevelLoaded(LoadMode.LoadGame);
  replacement.OnCanUnlockArea(2,3,true); current = Observation.Read();
  loading.OnReleased(); Check(object.ReferenceEquals(current, Observation.Read()), "old loading release isolated");
  nextLoading.OnLevelUnloading();
  nextLoading.OnLevelLoaded(LoadMode.NewMap);
  replacement.OnCanUnlockArea(1,2,true); Check(Observation.Read() == null, "editor excluded");
  nextLoading.OnLevelUnloading();
  nextLoading.OnLevelLoaded(LoadMode.LoadGame); replacement.OnCanUnlockArea(1,2,true);
  var beforeRelease = Observation.Read();
  replacement.OnReleased(); Check(Observation.Read() == null, "current provider released");
  Check(!IsCurrent(beforeRelease), "released provider reference not current");
  var rebound = new Areas(); rebound.OnCreated(null);
  Check(!rebound.OnCanUnlockArea(4,4,false), "rebound native refusal preserved");
  var reboundObservation = Observation.Read();
  Check(reboundObservation != null && reboundObservation.X == 4 && reboundObservation.Z == 4 && reboundObservation.Eligibility == false, "replacement after release resumes observation");
  Check(beforeRelease != null && reboundObservation.Generation != beforeRelease.Generation, "replacement invalidates previous generation");
  replacement.OnReleased();
  Check(object.ReferenceEquals(reboundObservation, Observation.Read()), "released predecessor cannot clear rebound");
  nextLoading.OnLevelUnloading();
  rebound.OnCanUnlockArea(1,1,true);
  Check(Observation.Read() == null, "rebound remains inactive after city unload");
  Check(!IsCurrent(reboundObservation), "unloaded rebound reference not current");
  var afterUnload = new Areas(); afterUnload.OnCreated(null);
  afterUnload.OnCanUnlockArea(1,1,true);
  Check(Observation.Read() == null, "new provider cannot reactivate unloaded city");
  afterUnload.OnReleased(); nextLoading.OnReleased(); rebound.OnReleased();
  CheckReleasedLoading();
  CheckSupersededLoading();
  CheckCreatedOrder();
  CheckUncreatedLoading();
  CheckReleasedAreas();
  CheckConcurrentRelease();
  Console.WriteLine("Passed " + checks + " lifecycle boundary checks; real game integration not tested.");
  StartupSnapshotChecks.Run();
  NativeSubmissionChecks.Run();
 }
}
