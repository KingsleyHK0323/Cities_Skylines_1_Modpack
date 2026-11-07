// OpenAI-Codex_Full_Map_Unlock_And_Expansion
using System;
using ICities;
using FullMapUnlockAndExpansion;
class LifecycleChecks {
 static int checks;
 static void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
 static int Price(Areas a, int value) { return a.OnGetAreaPrice(0,0,0,0,0,false,false,false,false,0,value); }
 static bool IsCurrent(AreaObservation observation) {
  var method = typeof(Observation).GetMethod("IsCurrent");
  if (method == null) throw new Exception("Observation.IsCurrent implementation is missing");
  return (bool)method.Invoke(null, new object[] { observation });
 }
 static void Main() {
  var first = new Areas(); first.OnCreated(null);
  Check(!first.OnCanUnlockArea(1,2,false), "refusal preserved before load");
  Check(Observation.Read() == null, "inactive");
  var loading = new Loading(); loading.OnLevelLoaded(LoadMode.NewGame);
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
  var nextLoading = new Loading(); nextLoading.OnLevelLoaded(LoadMode.LoadGame);
  replacement.OnCanUnlockArea(2,3,true); current = Observation.Read();
  loading.OnReleased(); Check(object.ReferenceEquals(current, Observation.Read()), "old loading release isolated");
  nextLoading.OnLevelLoaded(LoadMode.NewMap);
  replacement.OnCanUnlockArea(1,2,true); Check(Observation.Read() == null, "editor excluded");
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
  Console.WriteLine("Passed " + checks + " lifecycle boundary checks; real game integration not tested.");
 }
}
