// OpenAI-Codex_Full_Map_Unlock_And_Expansion
using System;
using ICities;
using FullMapUnlockAndExpansion;
class LifecycleChecks {
 static int checks;
 static void Check(bool condition, string name) { checks++; if (!condition) throw new Exception(name); }
 static int Price(Areas a, int value) { return a.OnGetAreaPrice(0,0,0,0,0,false,false,false,false,0,value); }
 static void Main() {
  var first = new Areas(); first.OnCreated(null);
  Check(!first.OnCanUnlockArea(1,2,false), "refusal preserved before load");
  Check(Observation.Read() == null, "inactive");
  var loading = new Loading(); loading.OnLevelLoaded(LoadMode.NewGame);
  Check(first.OnCanUnlockArea(1,2,true), "true unchanged");
  var captured = Observation.Read();
  Check(captured != null && captured.X == 1 && captured.Z == 2 && captured.Eligibility == true && captured.Price == null, "eligibility only");
  Check(Price(first,0) == 0, "zero price unchanged");
  Check(Observation.Read().Price == 0 && Observation.Read().X == null && Observation.Read().Eligibility == null, "price not joined");
  Check(Price(first,-1) == -1, "negative native price unchanged");
  loading.OnLevelUnloading(); Check(Observation.Read() == null, "unloaded");
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
  replacement.OnReleased(); Check(Observation.Read() == null, "current provider released");
  Console.WriteLine("Passed " + checks + " lifecycle boundary checks; real game integration not tested.");
 }
}
