// OpenAI-Codex_channel_checks
using System;
using FullMapUnlockAndExpansion;
using ICities;

static class VersionChannelChecks {
 private const string StableName = "Full Map Unlock And Expansion";
 private const string PreviewName = "Full Map Unlock And Expansion [Development Preview]";
 private const string OriginalDescription =
  "Read-only area callback observation. Does not unlock or purchase land.";

 private static int checks;

 private static void Check(bool condition, string message) {
  checks++;
  if (!condition) throw new Exception(message);
 }

 private sealed class FakeAreaInfo : IAreas {
  public int UnlockCalls { get; private set; }
  public int unlockedAreaCount { get { return 0; } }
  public bool IsAreaUnlocked(int x, int z) { return false; }
  public void UnlockArea(int x, int z, bool requireMoney) {
   UnlockCalls++;
   throw new InvalidOperationException("A read-only callback must not unlock a fake area.");
  }
 }

 private static int Price(Areas areas, int value) {
  return areas.OnGetAreaPrice(1u, 2u, 3u, 4u, 5u, true, false, true, false, 0.5f, value);
 }

 private static void CheckCallbacks(Areas areas, string phase) {
  Check(areas.OnCanUnlockArea(1, 2, true), phase + ": true eligibility changed");
  Check(!areas.OnCanUnlockArea(3, 4, false), phase + ": false eligibility changed");
  Check(Price(areas, 42) == 42, phase + ": positive price changed");
  Check(Price(areas, -7) == -7, phase + ": negative price changed");
 }

 public static int Main(string[] args) {
  try {
   Run(args);
   return 0;
  } catch (Exception error) {
   Console.Error.WriteLine(error.Message);
   return 1;
  }
 }

 private static void Run(string[] args) {
  if (args == null || args.Length != 1 ||
      (args[0] != StableName && args[0] != PreviewName)) {
   throw new ArgumentException("Pass the full expected stable or preview name as the only argument.");
  }

  var mod = new Mod();
  Check(string.Equals(mod.Name, args[0], StringComparison.Ordinal),
   "Name mismatch: expected '" + args[0] + "' but got '" + mod.Name + "'.");
  Check(string.Equals(mod.Description, OriginalDescription, StringComparison.Ordinal),
   "The existing description changed.");

  var areas = new Areas();
  var fake = new FakeAreaInfo();
  CheckCallbacks(areas, "before creation");
  areas.OnCreated(fake);
  CheckCallbacks(areas, "after creation");
  areas.OnReleased();
  CheckCallbacks(areas, "after release");
  Check(fake.UnlockCalls == 0, "A callback attempted to unlock an area.");

  Console.WriteLine("Passed " + checks + " synthetic name and callback checks; no game was tested.");
 }
}
