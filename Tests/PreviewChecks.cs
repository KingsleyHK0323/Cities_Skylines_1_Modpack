// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic tests; neither game integration nor runtime acceptance is covered.
using System;
using System.Reflection;
class PreviewChecks {
 static int candidateChecks;
 static void Verify(bool ok, string name) { candidateChecks++; if (!ok) throw new Exception(name); }
 static bool?[] Grid(bool owned) {
  var result = new bool?[25];
  for (int i = 0; i < result.Length; i++) result[i] = owned;
  return result;
 }
 static int[] Initial25(bool?[] tiles, int? width, int? height, int? count,
     long? capturedGeneration, long? currentGeneration, long? capturedRevision, long? currentRevision) {
  MethodInfo method = typeof(AreaPreview).GetMethod("MissingForInitial25", BindingFlags.Public | BindingFlags.Static);
  if (method == null) throw new Exception("MissingForInitial25 implementation is missing");
  Array samples = tiles == null ? null : CoordinateSamples(tiles);
  return (int[])method.Invoke(null, new object[] { samples, width, height, count,
      capturedGeneration, currentGeneration, capturedRevision, currentRevision });
 }
 static Array CoordinateSamples(bool?[] tiles) {
  Type sampleType = typeof(AreaPreview).Assembly.GetType("Initial25Tile");
  if (sampleType == null) throw new Exception("coordinate identity contract is missing");
  Array samples = Array.CreateInstance(sampleType, tiles.Length);
  for (int i = 0; i < tiles.Length; i++)
   samples.SetValue(Activator.CreateInstance(sampleType, new object[] { i % 5, i / 5, i, tiles[i] }), i);
  return samples;
 }
 static object CoordinateSample(int x, int z, int nativeIndex, bool? unlocked) {
  Type sampleType = typeof(AreaPreview).Assembly.GetType("Initial25Tile");
  if (sampleType == null) throw new Exception("coordinate identity contract is missing");
  return Activator.CreateInstance(sampleType, new object[] { x, z, nativeIndex, unlocked });
 }
 static int[] Initial25Coordinates(Array samples, int count) {
  MethodInfo method = typeof(AreaPreview).GetMethod("MissingForInitial25", BindingFlags.Public | BindingFlags.Static);
  if (method == null) throw new Exception("MissingForInitial25 implementation is missing");
  return (int[])method.Invoke(null, new object[] { samples, 5, 5, count, 1L, 1L, 1L, 1L });
 }
 static object NoDebitCoordinates(Array samples, int count) {
  Type previousType = typeof(AreaPreview).Assembly.GetType("Initial25PreviousAttempt");
  MethodInfo method = typeof(AreaPreview).GetMethod("PreviewInitial25NoDebit", BindingFlags.Public | BindingFlags.Static);
  if (previousType == null || method == null) throw new Exception("no-debit startup preview is missing");
  return method.Invoke(null, new object[] { samples, 5, 5, count, 1L, 1L, 1L, 1L,
      true, Enum.Parse(previousType, "None"), null, null, null });
 }
 static void CheckCoordinateIdentity() {
  int previousChecks = candidateChecks;
  var grid = Grid(false); grid[12] = true;
  Array original = CoordinateSamples(grid);
  int[] expected = Initial25Coordinates(original, 1);
  Verify(expected != null && expected.Length == 24 && Array.IndexOf(expected, 12) < 0, "coordinate baseline");

  Array shuffled = (Array)original.Clone();
  object first = shuffled.GetValue(0), last = shuffled.GetValue(24);
  shuffled.SetValue(last, 0); shuffled.SetValue(first, 24);
  int[] reordered = Initial25Coordinates(shuffled, 1);
  Verify(reordered != null && reordered.Length == expected.Length, "shuffled complete records accepted");
  for (int i = 0; i < expected.Length; i++) Verify(reordered[i] == expected[i], "shuffled records retain coordinates");
  Array reversed = (Array)original.Clone();
  for (int i = 0; i < 12; i++) {
   object left = reversed.GetValue(i), right = reversed.GetValue(24 - i);
   reversed.SetValue(right, i); reversed.SetValue(left, 24 - i);
  }
  int[] reversedResult = Initial25Coordinates(reversed, 1);
  Verify(reversedResult != null && reversedResult.Length == expected.Length, "reversed records accepted");
  for (int i = 0; i < expected.Length; i++) Verify(reversedResult[i] == expected[i], "reverse preserves tile identity");

  Array duplicated = (Array)original.Clone();
  duplicated.SetValue(duplicated.GetValue(0), 1);
  Verify(Initial25Coordinates(duplicated, 1) == null, "duplicate coordinate with same owned count rejected");
  var partialGrid = new bool?[24];
  for (int i = 0; i < partialGrid.Length; i++) partialGrid[i] = grid[i];
  Verify(Initial25Coordinates(CoordinateSamples(partialGrid), 1) == null, "missing coordinate rejected");
  Array nullEntry = (Array)original.Clone(); nullEntry.SetValue(null, 1);
  Verify(Initial25Coordinates(nullEntry, 1) == null, "missing coordinate observation rejected");
  Array wrongIndex = (Array)original.Clone();
  wrongIndex.SetValue(CoordinateSample(0, 0, 1, false), 0);
  wrongIndex.SetValue(CoordinateSample(1, 0, 0, false), 1);
  Verify(Initial25Coordinates(wrongIndex, 1) == null, "coordinate and native-index swap rejected");
  Array outside = (Array)original.Clone();
  outside.SetValue(CoordinateSample(5, 0, 5, false), 5);
  Verify(Initial25Coordinates(outside, 1) == null, "out-of-range coordinate rejected");
  Array negative = (Array)original.Clone();
  negative.SetValue(CoordinateSample(-1, 0, -1, false), 0);
  Verify(Initial25Coordinates(negative, 1) == null, "negative coordinate rejected");
  Array badIndex = (Array)original.Clone();
  badIndex.SetValue(CoordinateSample(0, 0, -1, false), 0);
  Verify(Initial25Coordinates(badIndex, 1) == null, "invalid native index rejected");
  // Raw (5,0) and valid (0,1) both flatten to index 5 in the observed game assembly.
  Array positiveAlias = (Array)original.Clone();
  positiveAlias.SetValue(CoordinateSample(5, 0, 5, false), 5);
  Verify(Initial25Coordinates(positiveAlias, 1) == null, "positive coordinate alias rejected");
  object aliasDecision = NoDebitCoordinates(positiveAlias, 1);
  Verify(DecisionKind(aliasDecision) == "Unavailable" && DecisionIndex(aliasDecision) == null &&
      DecisionRequireMoney(aliasDecision) == null, "positive alias never produces a no-debit proposal");
  Verify(Initial25Coordinates(original, 1) != null, "valid (0,1) native index 5 remains accepted");
  // Raw (-1,1) and valid (4,0) both flatten to index 4.
  Array negativeAlias = (Array)original.Clone();
  negativeAlias.SetValue(CoordinateSample(-1, 1, 4, false), 4);
  Verify(Initial25Coordinates(negativeAlias, 1) == null, "negative coordinate alias rejected");
  object negativeDecision = NoDebitCoordinates(negativeAlias, 1);
  Verify(DecisionKind(negativeDecision) == "Unavailable" && DecisionIndex(negativeDecision) == null,
      "negative alias never produces a no-debit proposal");

  // Valid-looking caller labels cannot prove that the value came from that native tile.
  Array mislabelled = (Array)original.Clone();
  mislabelled.SetValue(CoordinateSample(0, 0, 0, true), 0);
  mislabelled.SetValue(CoordinateSample(2, 2, 12, false), 12);
  int[] unverifiable = Initial25Coordinates(mislabelled, 1);
  Verify(unverifiable != null && Array.IndexOf(unverifiable, 0) < 0 && Array.IndexOf(unverifiable, 12) >= 0,
      "mislabelled values remain indistinguishable without a trusted source");
  Console.WriteLine("Passed " + (candidateChecks - previousChecks) + " coordinate-identity assertions; source authenticity is not established.");
 }
 static void CheckInitial25() {
  bool?[] empty = Grid(false);
  int[] all = Initial25(empty, 5, 5, 0, 1, 1, 1, 1);
  Verify(all != null && all.Length == 25 && all[0] == 0 && all[24] == 24, "all 25 candidates");
  for (int i = 0; i < all.Length; i++) Verify(all[i] == i, "row-major unique candidate");
  empty[12] = true;
  int[] missing = Initial25(empty, 5, 5, 1, 1, 1, 1, 1);
  Verify(missing != null && missing.Length == 24, "preserve existing area");
  for (int i = 0; i < missing.Length; i++) Verify(missing[i] != 12, "exclude existing area");
  missing[0] = -1;
  Verify(Initial25(empty, 5, 5, 1, 1, 1, 1, 1)[0] == 0 && empty[0] == false, "output does not alias input or another result");
  var nearlyFull = Grid(true); nearlyFull[24] = false;
  var last = Initial25(nearlyFull, 5, 5, 24, 8, 8, 9, 9);
  Verify(last != null && last.Length == 1 && last[0] == 24, "last missing area");
  Verify(Initial25(Grid(true), 5, 5, 25, 1, 1, 1, 1).Length == 0, "already complete");
  Verify(Initial25(null, 5, 5, 0, 1, 1, 1, 1) == null, "missing grid");
  Verify(Initial25(new bool?[24], 5, 5, 0, 1, 1, 1, 1) == null, "partial grid");
  var unknown = Grid(false); unknown[7] = null;
  Verify(Initial25(unknown, 5, 5, 0, 1, 1, 1, 1) == null, "unknown area");
  Verify(Initial25(empty, 5, 5, 0, 1, 1, 1, 1) == null, "count mismatch");
  Verify(Initial25(empty, 5, 5, -1, 1, 1, 1, 1) == null, "negative count");
  Verify(Initial25(empty, 5, 5, 26, 1, 1, 1, 1) == null, "over 25 count");
  Verify(Initial25(empty, 9, 9, 1, 1, 1, 1, 1) == null, "81 grid out of scope");
  Verify(Initial25(empty, null, 5, 1, 1, 1, 1, 1) == null, "unknown dimensions");
  Verify(Initial25(empty, 5, 5, null, 1, 1, 1, 1) == null, "unknown count");
  Verify(Initial25(empty, 5, 5, 1, 1, 2, 1, 1) == null, "stale city generation");
  Verify(Initial25(empty, 5, 5, 1, 1, 1, 1, 2) == null, "stale same-city revision");
  Verify(Initial25(empty, 5, 5, 1, 0, 0, 1, 1) == null, "invalid generation");
  Verify(Initial25(empty, 5, 5, 1, 1, 1, 0, 0) == null, "invalid revision");
  Verify(Initial25(empty, 5, 5, 1, 1, 1, null, 1) == null, "unknown revision");
  Verify(Initial25(empty, 5, 9, 1, 1, 1, 1, 1) == null, "non-25 rectangular grid");
  Verify(Initial25(new bool?[26], 5, 5, 1, 1, 1, 1, 1) == null, "extra cell");
  Verify(Initial25(empty, 5, 5, 1, 1, null, 1, 1) == null, "unknown current generation");
  Verify(Initial25(empty, 5, 5, 1, 1, 1, 1, null) == null, "unknown current revision");
  var random = new Random(20261107);
  for (int sample = 0; sample < 64; sample++) {
   var input = Grid(false); int owned = 0;
   for (int i = 0; i < input.Length; i++) { input[i] = random.Next(2) == 1; if (input[i].Value) owned++; }
   int[] candidates = Initial25(input, 5, 5, owned, 3, 3, 7, 7);
   Verify(candidates != null && candidates.Length == 25 - owned, "candidate count property");
   var seen = new bool[25];
   foreach (int index in candidates) {
    Verify(index >= 0 && index < 25 && !seen[index] && input[index] == false, "candidate membership property");
    seen[index] = true;
   }
  }
  Console.WriteLine("Passed " + candidateChecks + " initial-25 candidate assertions; no game writes or payment actions.");
 }
 static object NoDebitStep(bool?[] tiles, int? count, bool? startup, string previousState,
     int? previousIndex, long? previousGeneration, long? previousRevision,
     long? capturedGeneration, long? currentGeneration, long? capturedRevision, long? currentRevision) {
  Type previousType = typeof(AreaPreview).Assembly.GetType("Initial25PreviousAttempt");
  MethodInfo method = typeof(AreaPreview).GetMethod("PreviewInitial25NoDebit", BindingFlags.Public | BindingFlags.Static);
  if (previousType == null || method == null) throw new Exception("no-debit startup preview is missing");
  object previous = Enum.Parse(previousType, previousState);
  Array samples = tiles == null ? null : CoordinateSamples(tiles);
  return method.Invoke(null, new object[] { samples, 5, 5, count, capturedGeneration,
      currentGeneration, capturedRevision, currentRevision, startup, previous,
      previousIndex, previousGeneration, previousRevision });
 }
 static string DecisionKind(object decision) { return decision.GetType().GetField("Kind").GetValue(decision).ToString(); }
 static int? DecisionIndex(object decision) { return (int?)decision.GetType().GetField("NativeIndex").GetValue(decision); }
 static bool? DecisionRequireMoney(object decision) { return (bool?)decision.GetType().GetField("RequireMoney").GetValue(decision); }
 static void CheckNoDebitStartup() {
  int priorChecks = candidateChecks;
  var city = Grid(false); city[12] = true;
  object step = NoDebitStep(city, 1, true, "None", null, null, null, 10, 10, 1, 1);
  Verify(DecisionKind(step) == "Candidate" && DecisionIndex(step) == 7 && DecisionRequireMoney(step) == false, "adjacent candidate requests no city funds");
  Verify(city[7] == false && city[12] == true, "preview never changes input");
  object waiting = NoDebitStep(city, 1, true, "Pending", 7, 10, 1, 10, 10, 1, 1);
  Verify(DecisionKind(waiting) == "Waiting" && DecisionIndex(waiting) == null && DecisionRequireMoney(waiting) == null, "pending attempt cannot issue a second request");
  object failed = NoDebitStep(city, 1, true, "Failed", 7, 10, 1, 10, 10, 2, 2);
  Verify(DecisionKind(failed) == "Halted" && DecisionIndex(failed) == null && DecisionRequireMoney(failed) == null, "failed attempt cannot automatically retry");
  int failedSyntheticFunds = 1000;
  if (DecisionRequireMoney(step).Value) failedSyntheticFunds -= 10;
  Verify(failedSyntheticFunds == 1000 && city[7] == false && DecisionKind(failed) == "Halted", "synthetic failed step neither debits funds nor unlocks");
  object callerReset = NoDebitStep(city, 1, true, "None", null, null, null, 10, 10, 2, 2);
  Verify(DecisionKind(callerReset) == "Candidate", "stateless preview cannot enforce caller attempt history");
  object notConfirmed = NoDebitStep(city, 1, true, "ObservedUnlocked", 7, 10, 1, 10, 10, 2, 2);
  Verify(DecisionKind(notConfirmed) == "Halted" && DecisionIndex(notConfirmed) == null, "reported success without unlocked tile halts");
  city[7] = true;
  object sameRevision = NoDebitStep(city, 2, true, "ObservedUnlocked", 7, 10, 1, 10, 10, 1, 1);
  Verify(DecisionKind(sameRevision) == "Waiting" && DecisionIndex(sameRevision) == null, "same revision does not confirm completion");
  object next = NoDebitStep(city, 2, true, "ObservedUnlocked", 7, 10, 1, 10, 10, 2, 2);
  Verify(DecisionKind(next) == "Candidate" && DecisionIndex(next) != 7 && DecisionRequireMoney(next) == false, "confirmed unlocked tile advances without debit");
  object wrongCity = NoDebitStep(city, 2, true, "ObservedUnlocked", 7, 9, 1, 10, 10, 2, 2);
  Verify(DecisionKind(wrongCity) == "Unavailable" && DecisionIndex(wrongCity) == null, "previous city cannot authorize next step");
  object stale = NoDebitStep(city, 2, true, "None", null, null, null, 10, 10, 1, 2);
  Verify(DecisionKind(stale) == "Unavailable" && DecisionRequireMoney(stale) == null, "stale snapshot gives no request");
  object noStart = NoDebitStep(city, 2, false, "None", null, null, null, 10, 10, 2, 2);
  Verify(DecisionKind(noStart) == "Unavailable" && DecisionIndex(noStart) == null, "non-start context gives no request");
  object unknownStart = NoDebitStep(city, 2, null, "None", null, null, null, 10, 10, 2, 2);
  Verify(DecisionKind(unknownStart) == "Unavailable", "unknown startup context");
  object empty = NoDebitStep(Grid(false), 0, true, "None", null, null, null, 10, 10, 1, 1);
  Verify(DecisionKind(empty) == "Unavailable" && DecisionIndex(empty) == null, "no known starting area");
  object complete = NoDebitStep(Grid(true), 25, true, "None", null, null, null, 10, 10, 1, 1);
  Verify(DecisionKind(complete) == "Complete" && DecisionRequireMoney(complete) == null, "already complete requires no funds or request");
  object invalidMetadata = NoDebitStep(city, 2, true, "None", 7, 10, 1, 10, 10, 2, 2);
  Verify(DecisionKind(invalidMetadata) == "Unavailable", "unexpected previous metadata is rejected");
  object invalidState = NoDebitStep(city, 2, true, "999", null, null, null, 10, 10, 2, 2);
  Verify(DecisionKind(invalidState) == "Unavailable" && DecisionIndex(invalidState) == null, "unknown attempt state gives no request");
  object pendingComplete = NoDebitStep(Grid(true), 25, true, "Pending", 7, 10, 1, 10, 10, 2, 2);
  Verify(DecisionKind(pendingComplete) == "Waiting", "pending attempt is not silently treated as complete");
  object missingPrevious = NoDebitStep(city, 2, true, "ObservedUnlocked", null, 10, 1, 10, 10, 2, 2);
  Verify(DecisionKind(missingPrevious) == "Unavailable", "missing previous tile cannot advance");
  object outsidePrevious = NoDebitStep(city, 2, true, "ObservedUnlocked", 25, 10, 1, 10, 10, 2, 2);
  Verify(DecisionKind(outsidePrevious) == "Unavailable", "out-of-range previous tile cannot advance");
  object missingPriorRevision = NoDebitStep(city, 2, true, "ObservedUnlocked", 7, 10, null, 10, 10, 2, 2);
  Verify(DecisionKind(missingPriorRevision) == "Unavailable", "missing previous revision cannot advance");
  object inconsistentCount = NoDebitStep(city, 1, true, "None", null, null, null, 10, 10, 2, 2);
  Verify(DecisionKind(inconsistentCount) == "Unavailable", "inconsistent snapshot cannot propose");

  var random = new Random(20261108);
  for (int sample = 0; sample < 64; sample++) {
   var grid = Grid(false); int owned = 0;
   for (int i = 0; i < grid.Length; i++) { grid[i] = random.Next(2) == 1; if (grid[i].Value) owned++; }
   object proposal = NoDebitStep(grid, owned, true, "None", null, null, null, 5, 5, 1, 1);
   if (owned == 0) {
    Verify(DecisionKind(proposal) == "Unavailable" && DecisionIndex(proposal) == null, "empty grid has no guessed starting tile");
   } else if (owned == 25) {
    Verify(DecisionKind(proposal) == "Complete" && DecisionIndex(proposal) == null, "full grid needs no step");
   } else {
    int index = DecisionIndex(proposal).Value, x = index % 5, z = index / 5;
    bool adjacent = (x > 0 && grid[index - 1].Value) || (x < 4 && grid[index + 1].Value) ||
        (z > 0 && grid[index - 5].Value) || (z < 4 && grid[index + 5].Value);
    Verify(DecisionKind(proposal) == "Candidate" && !grid[index].Value && adjacent &&
        DecisionRequireMoney(proposal) == false, "synthetic candidate is missing, adjacent and no-debit");
   }
  }

  var synthetic = Grid(false); synthetic[12] = true;
  int syntheticFunds = 1000, count = 1, attempts = 0; long revision = 1;
  string prior = "None"; int? priorIndex = null; long? priorGeneration = null, priorRevision = null;
  while (count < 25) {
   object proposal = NoDebitStep(synthetic, count, true, prior, priorIndex, priorGeneration, priorRevision, 22, 22, revision, revision);
   Verify(DecisionKind(proposal) == "Candidate" && DecisionRequireMoney(proposal) == false, "synthetic proposal never requests city funds");
   int tile = DecisionIndex(proposal).Value;
   Verify(!synthetic[tile].Value, "synthetic proposal excludes already-unlocked tile");
   // Synthetic adapter only: a true requireMoney would debit this in-memory balance.
   if (DecisionRequireMoney(proposal).Value) syntheticFunds -= 10;
   synthetic[tile] = true; count++; attempts++;
   prior = "ObservedUnlocked"; priorIndex = tile; priorGeneration = 22; priorRevision = revision; revision++;
  }
  object done = NoDebitStep(synthetic, count, true, prior, priorIndex, priorGeneration, priorRevision, 22, 22, revision, revision);
  Verify(DecisionKind(done) == "Complete" && attempts == 24 && syntheticFunds == 1000, "synthetic 25-area sequence preserves funds");
  Console.WriteLine("Passed " + (candidateChecks - priorChecks) + " no-debit startup preview assertions; no game writes tested.");
 }
 static void Main() {
  if (AreaPreview.DescribeObservation(true, 24, 0, 1, 1, 4, 4) != "Observed native eligibility only") throw new Exception("24 edge, zero is not purchase approval");
  if (AreaPreview.DescribeObservation(true, 25, 1, 1, 1, 0, 0) != "Outside preview scope") throw new Exception("25");
  if (AreaPreview.DescribeObservation(true, 26, 1, 1, 1, 0, 0) != "Outside preview scope") throw new Exception("26");
  if (AreaPreview.DescribeObservation(true, -1, 1, 1, 1, 0, 0) != "Unknown") throw new Exception("negative count");
  if (AreaPreview.DescribeObservation(true, 9, 1, 1, 2, 0, 0) != "Unknown") throw new Exception("stale generation");
  if (AreaPreview.DescribeObservation(true, 9, 1, null, 2, 0, 0) != "Unknown") throw new Exception("missing generation");
  if (AreaPreview.DescribeObservation(true, 9, 1, 0, 0, 0, 0) != "Unknown") throw new Exception("inactive generation");
  if (AreaPreview.DescribeObservation(true, 9, 1, 1, 1, 5, 0) != "Outside preview scope") throw new Exception("outside grid");
  if (AreaPreview.DescribeObservation(null, 9, 1, 1, 1, 0, 0) != "Unknown") throw new Exception("missing eligibility");
  if (AreaPreview.Describe(null, null, null) != "Unknown") throw new Exception("unknown");
  if (AreaPreview.Describe(false, 9, 100) != "Native refusal") throw new Exception("refusal");
  if (AreaPreview.Describe(true, 9, 100) != "Observed native eligibility only") throw new Exception("observation");
  if (AreaPreview.Describe(true, 25, 100) != "Outside preview scope") throw new Exception("cap");
  if (AreaPreview.Describe(true, 9, -1) != "Unknown") throw new Exception("price");
  Console.WriteLine("Passed 14 pure preview assertions; no game integration tested.");
  CheckInitial25();
  CheckCoordinateIdentity();
  CheckNoDebitStartup();
 }
}
