// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Synthetic tests; neither game integration nor runtime acceptance is covered.
using System;
using System.Reflection;
using System.Threading;
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
 static long? DecisionAttempt(object decision) {
  FieldInfo field = decision.GetType().GetField("AttemptId");
  if (field == null) throw new Exception("single-run attempt identity is missing");
  return (long?)field.GetValue(decision);
 }
 static object NewRun() {
  Type type = typeof(AreaPreview).Assembly.GetType("Initial25NoDebitRun");
  if (type == null) throw new Exception("single-run no-debit coordinator is missing");
  return Activator.CreateInstance(type);
 }
 static object RunCall(object run, string method, Array samples, int? count,
     long? capturedGeneration, long? currentGeneration, long? capturedRevision, long? currentRevision, bool? newGame) {
  MethodInfo action = run.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance);
  if (action == null) throw new Exception("single-run coordinator method is missing: " + method);
  return action.Invoke(run, new object[] { samples, 5, 5, count, capturedGeneration, currentGeneration,
      capturedRevision, currentRevision, newGame });
 }
 static object RunFailureFor(object run, long? attempt) {
  MethodInfo method = run.GetType().GetMethod("FailPending", new Type[] { typeof(long?) });
  if (method == null) throw new Exception("identified failure report is missing");
  return method.Invoke(run, new object[] { attempt });
 }
 static object RunStep(object run, string method, bool?[] grid, int count, long generation, long revision, bool? newGame) {
  return RunCall(run, method, grid == null ? null : CoordinateSamples(grid), count,
      generation, generation, revision, revision, newGame);
 }
 static void CheckSingleRun() {
  int priorChecks = candidateChecks;
  var run = NewRun(); var grid = Grid(false); grid[12] = true;
  Verify(DecisionKind(RunFailureFor(run, null)) == "Unavailable", "failure without proposal cannot start or stop a run");
  Verify(DecisionKind(RunStep(run, "Propose", grid, 1, 4, 1, false)) == "Unavailable", "existing or unknown game cannot start a run");
  object first = RunStep(run, "Propose", grid, 1, 4, 1, true);
  Verify(DecisionKind(first) == "Candidate" && DecisionIndex(first) == 7 && DecisionRequireMoney(first) == false, "one no-debit proposal");
  object duplicate = RunStep(run, "Propose", grid, 1, 4, 1, true);
  Verify(DecisionKind(duplicate) == "Waiting" && DecisionIndex(duplicate) == null && DecisionRequireMoney(duplicate) == null, "second proposal waits");
  Verify(DecisionKind(RunStep(run, "Observe", grid, 1, 4, 1, true)) == "Waiting", "same revision waits");
  Verify(DecisionKind(RunStep(run, "Observe", grid, 1, 4, 2, true)) == "Waiting", "new snapshot without target waits");
  Verify(DecisionKind(RunFailureFor(run, DecisionAttempt(first))) == "Halted", "reported failure halts the run");
  Verify(DecisionKind(RunStep(run, "Propose", grid, 1, 4, 3, true)) == "Halted", "failed run cannot retry");
  Verify(DecisionKind(RunFailureFor(run, DecisionAttempt(first))) == "Halted", "failure is terminal within the run");

  run = NewRun();
  Array firstSnapshot = CoordinateSamples(grid);
  first = RunCall(run, "Propose", firstSnapshot, 1, 9, 9, 1, 1, true);
  Verify(DecisionKind(first) == "Candidate", "fresh run may propose");
  firstSnapshot.SetValue(CoordinateSample(0,0,0,true), 0);
  grid[7] = true;
  object confirmed = RunStep(run, "Observe", grid, 2, 9, 2, true);
  Verify(DecisionKind(confirmed) == "Confirmed" && DecisionIndex(confirmed) == null, "new snapshot confirms only the proposed tile");
  Verify(DecisionKind(RunStep(run, "Observe", grid, 2, 9, 2, true)) == "Unavailable", "confirmation is not replayed");
  object second = RunStep(run, "Propose", grid, 2, 9, 2, true);
  Verify(DecisionKind(second) == "Candidate" && DecisionIndex(second) != 7 && DecisionRequireMoney(second) == false, "confirmed snapshot advances once");
  grid[0] = true;
  Verify(DecisionKind(RunStep(run, "Observe", grid, 3, 9, 3, true)) == "Halted", "unrelated tile change halts pending run");
  Verify(DecisionKind(RunStep(run, "Propose", grid, 3, 9, 3, true)) == "Halted", "conflicted run cannot retry");

  run = NewRun(); grid = Grid(false); grid[12] = true;
  RunStep(run, "Propose", grid, 1, 1, 1, true);
  Verify(DecisionKind(RunStep(run, "Observe", grid, 1, 2, 2, true)) == "Halted", "different city generation halts");
  Verify(DecisionKind(RunStep(run, "Propose", grid, 1, 1, 3, true)) == "Halted", "old city cannot revive halted run");

  run = NewRun(); Array bad = CoordinateSamples(grid); bad.SetValue(bad.GetValue(0), 1);
  Verify(DecisionKind(RunCall(run, "Propose", bad, 1, 1, 1, 1, 1, true)) == "Unavailable", "duplicate coordinates cannot start");
  Verify(DecisionKind(RunStep(run, "Propose", grid, 1, 1, 1, true)) == "Candidate", "rejected snapshot did not create an attempt");

  run = NewRun();
  Verify(DecisionKind(RunStep(run, "Propose", grid, 1, 1, 1, null)) == "Unavailable", "unknown startup cannot propose");
  Verify(DecisionKind(RunStep(run, "Propose", grid, 1, 1, 2, true)) == "Candidate", "known startup proposes");
  Verify(DecisionKind(RunCall(run, "Observe", CoordinateSamples(grid), 1, 1, 1, 3, 4, true)) == "Waiting", "inconsistent revision cannot confirm");
  Verify(DecisionKind(RunStep(run, "Observe", grid, 1, 1, 1, true)) == "Waiting", "older snapshot cannot confirm");
  grid[7] = true;
  Verify(DecisionKind(RunStep(run, "Observe", grid, 2, 1, 2, true)) == "Halted", "same revision with different content halts");
  grid[7] = false;

  run = NewRun();
  RunStep(run, "Propose", grid, 1, 5, 1, true);
  Verify(DecisionKind(RunStep(run, "Observe", grid, 1, 5, 3, true)) == "Waiting", "newer unchanged snapshot waits");
  grid[7] = true;
  Verify(DecisionKind(RunStep(run, "Observe", grid, 2, 5, 2, true)) == "Waiting", "older success cannot follow a newer unchanged snapshot");
  Verify(DecisionKind(RunStep(run, "Observe", grid, 2, 5, 3, true)) == "Halted", "same later revision with changed content halts");
  grid[7] = false;

  run = NewRun();
  RunStep(run, "Propose", grid, 1, 5, 1, true);
  grid[0] = true;
  Verify(DecisionKind(RunStep(run, "Propose", grid, 2, 5, 3, true)) == "Halted", "pending proposal detects unrelated snapshot change");
  grid[0] = false;

  run = NewRun();
  RunStep(run, "Propose", grid, 1, 5, 1, true);
  grid[7] = true;
  Verify(DecisionKind(RunStep(run, "Propose", grid, 2, 5, 3, true)) == "Waiting", "pending proposal does not confirm implicitly");
  Verify(DecisionKind(RunStep(run, "Observe", grid, 2, 5, 2, true)) == "Waiting", "pending proposal advances observed revision");
  Verify(DecisionKind(RunStep(run, "Observe", grid, 2, 5, 3, true)) == "Confirmed", "explicit observation confirms the latest supplied snapshot");
  grid[7] = false;

  run = NewRun();
  first = RunStep(run, "Propose", grid, 1, 5, 1, true);
  long? oldAttempt = DecisionAttempt(first);
  grid[7] = true;
  Verify(DecisionKind(RunStep(run, "Observe", grid, 2, 5, 2, true)) == "Confirmed", "first identified attempt confirmed");
  second = RunStep(run, "Propose", grid, 2, 5, 2, true);
  long? nextAttempt = DecisionAttempt(second);
  Verify(oldAttempt.HasValue && nextAttempt.HasValue && oldAttempt != nextAttempt, "successive attempts have distinct identities");
  Verify(DecisionKind(RunFailureFor(run, oldAttempt)) == "Unavailable", "late failure from an old attempt is ignored");
  Verify(DecisionKind(RunFailureFor(run, null)) == "Unavailable", "unidentified failure is ignored");
  Verify(DecisionKind(RunStep(run, "Propose", grid, 2, 5, 3, true)) == "Waiting", "old failure does not stop current attempt");
  Verify(DecisionKind(RunFailureFor(run, nextAttempt)) == "Halted", "identified current failure halts even after revision advances");
  grid[7] = false;

  run = NewRun(); var full = Grid(true);
  Verify(DecisionKind(RunStep(run, "Propose", full, 25, 3, 1, true)) == "Complete", "complete supplied snapshot");
  full[0] = false;
  Verify(DecisionKind(RunStep(run, "Propose", full, 24, 3, 2, true)) == "Halted", "completed run does not hide later changes");
  full[0] = true; run = NewRun();
  Verify(DecisionKind(RunStep(run, "Propose", full, 25, 3, 1, true)) == "Complete", "another run completes");
  full[0] = false;
  Verify(DecisionKind(RunStep(run, "Observe", full, 24, 3, 3, true)) == "Halted", "completed run detects conflicting observation");
  full[0] = true;
  Verify(DecisionKind(RunStep(run, "Propose", full, 25, 3, 2, true)) == "Halted", "older snapshot cannot hide completed-run conflict");

  run = NewRun(); grid = Grid(false); grid[12] = true; int count = 1, funds = 1000; long revision = 1;
  while (count < 25) {
   object proposal = RunStep(run, "Propose", grid, count, 20, revision, true);
   Verify(DecisionKind(proposal) == "Candidate" && DecisionRequireMoney(proposal) == false, "synthetic run proposes no debit");
   int index = DecisionIndex(proposal).Value;
   Verify(!grid[index].Value, "synthetic run does not repeat an unlocked tile");
   if (DecisionRequireMoney(proposal).Value) funds -= 10;
   grid[index] = true; count++; revision++;
   Verify(DecisionKind(RunStep(run, "Observe", grid, count, 20, revision, true)) == "Confirmed", "synthetic step observed");
  }
  Verify(DecisionKind(RunStep(run, "Propose", grid, count, 20, revision, true)) == "Complete" && funds == 1000, "synthetic 25-area run does not debit synthetic funds");

  run = NewRun(); grid = Grid(false); grid[12] = true;
  var contenders = new object[16]; var errors = new Exception[16]; var threads = new Thread[16];
  var start = new ManualResetEvent(false); Array concurrentSamples = CoordinateSamples(grid);
  for (int i = 0; i < threads.Length; i++) {
   int slot = i;
   threads[i] = new Thread(delegate() { try { start.WaitOne(); contenders[slot] = RunCall(run, "Propose", concurrentSamples, 1, 6, 6, 1, 1, true); } catch (Exception ex) { errors[slot] = ex; } });
   threads[i].Start();
  }
  start.Set(); foreach (Thread thread in threads) thread.Join(); start.Close();
  int candidates = 0, waiters = 0; long? concurrentAttempt = null;
  for (int i = 0; i < contenders.Length; i++) {
   Verify(errors[i] == null && contenders[i] != null, "concurrent proposal did not throw");
   if (DecisionKind(contenders[i]) == "Candidate") { candidates++; concurrentAttempt = DecisionAttempt(contenders[i]); }
   if (DecisionKind(contenders[i]) == "Waiting") waiters++;
  }
  Verify(candidates == 1 && waiters == 15, "one run exposes only one outstanding candidate");
  var separate = NewRun();
  object separateCandidate = RunStep(separate, "Propose", grid, 1, 6, 1, true);
  Verify(DecisionKind(separateCandidate) == "Candidate", "separate runs are not globally serialized");
  Verify(DecisionAttempt(separateCandidate) == concurrentAttempt, "attempt identities are scoped to their run, not globally unique");
  Console.WriteLine("Passed " + (candidateChecks - priorChecks) + " isolated single-run guard assertions; no game writes tested.");
 }
 static object NewScope(object owner, object provider, Thread simulationThread, long generation, long attempt, int x, int z) {
  Type type = typeof(AreaPreview).Assembly.GetType("StartupMilestoneScopePreview");
  if (type == null) throw new Exception("startup milestone scope preview is missing");
  return Activator.CreateInstance(type, new object[] { owner, provider, simulationThread, generation, attempt, x, z });
 }
 static bool ScopeTry(object scope, bool? nativeMilestone, object owner, object provider, long? generation,
     long? attempt, int? x, int? z, bool? newGame, bool? loadingComplete, bool? nativeUnlock) {
  return (bool)scope.GetType().GetMethod("TryUse").Invoke(scope, new object[] { nativeMilestone, owner,
      provider, generation, attempt, x, z, newGame, loadingComplete, nativeUnlock });
 }
 static bool FakeEligibility(object scope, object owner, object provider, int x, int z,
     int count, int maximum, bool owned, bool adjacent, bool extensionAllows) {
  if (x < 0 || x >= 5 || z < 0 || z >= 5 || count >= maximum) return false;
  bool milestone = ScopeTry(scope, false, owner, provider, 8, 3, x, z, true, true, true);
  if (!milestone || owned || !adjacent) return false;
  return extensionAllows;
 }
 static void CheckMilestoneScope() {
  int priorChecks = candidateChecks;
  object owner = new object(), provider = new object(), other = new object();
  object scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  Verify(!ScopeTry(scope, null, owner, provider, 8, 3, 2, 1, true, true, true), "unknown native milestone cannot be overridden");
  Verify(!ScopeTry(scope, true, owner, provider, 8, 3, 2, 1, true, true, true), "native allowance needs no override");
  Verify(!ScopeTry(scope, false, other, provider, 8, 3, 2, 1, true, true, true), "wrong loading owner rejected");
  Verify(!ScopeTry(scope, false, owner, other, 8, 3, 2, 1, true, true, true), "wrong area provider rejected");
  Verify(!ScopeTry(scope, false, owner, provider, 9, 3, 2, 1, true, true, true), "changed city rejected");
  Verify(!ScopeTry(scope, false, owner, provider, null, 3, 2, 1, true, true, true), "unknown generation rejected");
  Verify(!ScopeTry(scope, false, owner, provider, 8, 4, 2, 1, true, true, true), "other attempt rejected");
  Verify(!ScopeTry(scope, false, owner, provider, 8, 3, 3, 1, true, true, true), "other coordinate rejected");
  Verify(!ScopeTry(scope, false, owner, provider, 8, 3, 5, 0, true, true, true), "out-of-range coordinate rejected");
  Verify(!ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, false, true, true), "non-new game rejected");
  Verify(!ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, null, true, true), "unknown startup rejected");
  Verify(!ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, true, false, true), "loading-incomplete rejected");
  Verify(!ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, true, true, false), "outside native unlock rejected");
  bool wrongThreadAccepted = true; Exception wrongThreadError = null;
  Thread otherThread = new Thread(delegate() { try { wrongThreadAccepted = ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, true, true, true); } catch (Exception ex) { wrongThreadError = ex; } });
  otherThread.Start(); otherThread.Join();
  Verify(wrongThreadError == null && !wrongThreadAccepted, "other thread rejected without consuming scope");
  Verify(ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, true, true, true), "matching single synthetic call accepted");
  Verify(!ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, true, true, true), "nested or repeated call rejected");
  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  bool untrustedNestedFirst = ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, true, true, true);
  bool intendedAfterNested = ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, true, true, true);
  Verify(untrustedNestedFirst && !intendedAfterNested, "untrusted nested call can consume scope; it is not game authorization");
  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  Verify(ScopeTry(scope, false, owner, provider, 8, 3, 2, 1, true, true, true), "independent scopes are not globally serialized");

  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  object queuedScope = scope; bool queuedAllowed = true;
  Action queued = delegate { queuedAllowed = ScopeTry(queuedScope, false, owner, provider, 8, 3, 2, 1, true, true, true); };
  scope.GetType().GetMethod("Invalidate").Invoke(scope, null);
  queued();
  Verify(!queuedAllowed, "simulated queued callback after invalidation rejected");
  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 5, 0);
  Verify(!ScopeTry(scope, false, owner, provider, 8, 3, 5, 0, true, true, true), "invalid scope cannot permit coordinate alias");
  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  Verify(!FakeEligibility(scope, owner, provider, 2, 1, 25, 25, false, true, true), "synthetic maximum still applies");
  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  Verify(!FakeEligibility(scope, owner, provider, 2, 1, 1, 25, true, true, true), "synthetic owned tile remains rejected");
  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  Verify(!FakeEligibility(scope, owner, provider, 2, 1, 1, 25, false, false, true), "synthetic adjacency still applies");
  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  Verify(!FakeEligibility(scope, owner, provider, 2, 1, 1, 25, false, true, false), "synthetic other extension refusal remains effective");
  scope = NewScope(owner, provider, Thread.CurrentThread, 8, 3, 2, 1);
  Verify(FakeEligibility(scope, owner, provider, 2, 1, 1, 25, false, true, true), "only synthetic milestone gate is permitted");
  Console.WriteLine("Passed " + (candidateChecks - priorChecks) + " isolated milestone-scope assertions; no patch or game call tested.");
 }
 // In-memory, single-controller fixture only; the wrong-thread case is an explicit exception.
 // It is not an IAreas adapter, a real scheduler, or a concurrent-queue implementation.
 sealed class FakeStartupHarness {
  public readonly object Owner = new object();
  public object Provider = new object();
  public long Generation = 1, Revision = 1;
  public bool? NewGame = true, LoadingComplete = true, Paused = false;
  public bool Stopped, SimulateReentry;
  public int Requests;
  public bool? LastRequireMoney;
  public int? PendingIndex;
  public long? PendingAttempt;
  public Initial25DecisionKind? NestedStatus;
  public Initial25DecisionKind? LastProposalKind;
  public int FakeWriterCalls, FakeWriterX, FakeWriterZ, CallbackExecutions;
  public bool? FakeWriterRequireMoney;
  public bool ThrowFromFakeWriter, FakeWriterQueuedBeforeException;
  public int FakeWriterThrowCount;
  public readonly InvalidOperationException FakeWriterFailure = new InvalidOperationException("controlled fake writer failure");
  public Exception FakeHandledWriterFailure;
  public Initial25DecisionKind? FakeWriterFailureStatus;
  public Action LastDeliveredAction;
  public void UnlockArea(int x, int z, bool requireMoney) {
   FakeWriterCalls++;
   FakeWriterX = x; FakeWriterZ = z; FakeWriterRequireMoney = requireMoney;
   if (ThrowFromFakeWriter) {
    FakeWriterQueuedBeforeException = Queue();
    FakeWriterThrowCount++;
    throw FakeWriterFailure;
   }
  }
  public readonly bool?[] Grid = PreviewChecks.Grid(false);
  readonly Initial25NoDebitRun run = new Initial25NoDebitRun();
  readonly object boundProvider;
  readonly long boundGeneration;
  readonly Thread simulationThread;
  Action queued;
  public FakeStartupHarness() {
   Grid[12] = true; boundProvider = Provider; boundGeneration = Generation; simulationThread = Thread.CurrentThread;
  }
  bool CurrentContext() {
   return !Stopped && NewGame == true && LoadingComplete == true && Paused == false &&
       Generation == boundGeneration && object.ReferenceEquals(Provider, boundProvider) &&
       object.ReferenceEquals(Thread.CurrentThread, simulationThread);
  }
  Initial25Tile[] Snapshot() {
   Initial25Tile[] tiles = new Initial25Tile[25];
   for (int i = 0; i < 25; i++) tiles[i] = new Initial25Tile(i % 5, i / 5, i, Grid[i]);
   return tiles;
  }
  int Count() { int count = 0; foreach (bool? tile in Grid) if (tile == true) count++; return count; }
  public bool Queue() {
   if (Stopped || queued != null) return false;
   long queuedGeneration = Generation; object queuedProvider = Provider;
   int delivered = 0;
   queued = delegate {
    Interlocked.Increment(ref CallbackExecutions);
    if (Interlocked.Exchange(ref delivered, 1) != 0) return;
    if (!CurrentContext() ||
        Generation != queuedGeneration || !object.ReferenceEquals(Provider, queuedProvider)) { Stopped = true; return; }
    Initial25NoDebitDecision decision = run.Propose(Snapshot(), 5, 5, Count(), Generation, Generation, Revision, Revision, NewGame);
    LastProposalKind = decision.Kind;
    if (decision.Kind == Initial25DecisionKind.Halted) { Stopped = true; return; }
    if (decision.Kind != Initial25DecisionKind.Candidate) return;
    if (SimulateReentry) NestedStatus = run.Propose(Snapshot(), 5, 5, Count(), Generation, Generation, Revision, Revision, NewGame).Kind;
    int index = decision.NativeIndex.Value;
    var scope = new StartupMilestoneScopePreview(Owner, Provider, simulationThread, Generation, decision.AttemptId.Value, index % 5, index / 5);
    if (!scope.TryUse(false, Owner, Provider, Generation, decision.AttemptId, index % 5, index / 5, NewGame, LoadingComplete, true)) {
     run.FailPending(decision.AttemptId); Stopped = true; return;
    }
    Requests++; LastRequireMoney = decision.RequireMoney; PendingIndex = index; PendingAttempt = decision.AttemptId;
    try { UnlockArea(index % 5, index / 5, decision.RequireMoney.Value); } catch (Exception exception) {
     FakeHandledWriterFailure = exception;
     Stopped = true;
     FakeWriterFailureStatus = run.FailPending(decision.AttemptId).Kind;
    }
   };
   return true;
  }
  public void Deliver() { Action action = queued; if (action != null) LastDeliveredAction = action; queued = null; if (action != null) action(); }
  public Initial25DecisionKind Observe() {
   if (!CurrentContext()) { Stopped = true; return run.FailPending(PendingAttempt).Kind; }
   Initial25NoDebitDecision result = run.Observe(Snapshot(), 5, 5, Count(), Generation, Generation, Revision, Revision, NewGame);
   if (result.Kind == Initial25DecisionKind.Halted) Stopped = true;
   return result.Kind;
  }
  public Initial25DecisionKind Fail() {
   Initial25NoDebitDecision result = run.FailPending(PendingAttempt);
   if (result.Kind == Initial25DecisionKind.Halted) Stopped = true;
   return result.Kind;
  }
 }
 static void CheckSyntheticDispatch() {
  int priorChecks = candidateChecks;
  var fake = new FakeStartupHarness();
  Verify(fake.Queue() && !fake.Queue(), "one queued fake action only");
  Verify(fake.Requests == 0, "queueing alone does not confirm or execute");
  fake.Generation++;
  fake.Deliver();
  Verify(fake.Stopped && fake.Requests == 0 && !fake.Queue(), "unload before dispatch stops the fake run");

  fake = new FakeStartupHarness(); fake.Queue(); fake.Provider = new object(); fake.Deliver();
  Verify(fake.Stopped && fake.Requests == 0, "provider change before dispatch stops the fake run");
  fake = new FakeStartupHarness(); fake.Queue(); fake.Deliver();
  fake.Provider = new object(); fake.Grid[fake.PendingIndex.Value] = true; fake.Revision++;
  Verify(fake.Observe() == Initial25DecisionKind.Halted && !fake.Queue() && fake.Requests == 1, "provider change after dispatch cannot confirm");
  fake = new FakeStartupHarness(); fake.Queue();
  FakeStartupHarness wrongThreadFake = fake;
  Thread wrongThread = new Thread(delegate() { wrongThreadFake.Deliver(); });
  wrongThread.Start(); wrongThread.Join();
  Verify(fake.Stopped && fake.Requests == 0, "fake dispatch on another thread cannot request an unlock");
  fake = new FakeStartupHarness(); fake.Queue(); fake.Paused = true; fake.Deliver();
  Verify(fake.Stopped && fake.Requests == 0, "paused fake city stops the pending action");
  fake = new FakeStartupHarness(); fake.NewGame = false; fake.Queue(); fake.Deliver();
  Verify(fake.Stopped && fake.Requests == 0, "non-new fake city remains unchanged");
  fake = new FakeStartupHarness(); fake.LoadingComplete = null; fake.Queue(); fake.Deliver();
  Verify(fake.Stopped && fake.Requests == 0, "unknown readiness stops the fake action");
  fake = new FakeStartupHarness(); fake.Queue(); fake.Deliver(); fake.LoadingComplete = false;
  fake.Grid[fake.PendingIndex.Value] = true; fake.Revision++;
  Verify(fake.Observe() == Initial25DecisionKind.Halted && !fake.Queue(), "lost readiness after dispatch cannot confirm");

  fake = new FakeStartupHarness(); fake.SimulateReentry = true; fake.Queue(); fake.Deliver();
  Verify(fake.Requests == 1 && fake.LastRequireMoney == false && fake.PendingIndex.HasValue, "fake dispatch records one no-debit candidate");
  Verify(fake.NestedStatus == Initial25DecisionKind.Waiting, "nested callback cannot obtain another candidate");
  Verify(fake.Queue(), "fake caller can request a status check"); fake.Deliver();
  Verify(fake.Requests == 1, "later fake dispatch cannot duplicate a pending request");
  fake.Revision++;
  Verify(fake.Observe() == Initial25DecisionKind.Waiting && fake.Requests == 1, "missing fake completion waits without retry");
  fake.Grid[fake.PendingIndex.Value] = true; fake.Revision++;
  Verify(fake.Observe() == Initial25DecisionKind.Confirmed, "newer fake snapshot confirms target only");
  fake.Queue(); fake.Deliver();
  Verify(fake.Requests == 2, "next fake request follows explicit confirmation");

  fake = new FakeStartupHarness(); fake.Queue(); fake.Deliver();
  fake.Grid[0] = true; fake.Revision++;
  Verify(fake.Observe() == Initial25DecisionKind.Halted && !fake.Queue() && fake.Requests == 1, "external fake grid change stops retries");
  fake = new FakeStartupHarness(); fake.Queue(); fake.Deliver();
  Verify(fake.Fail() == Initial25DecisionKind.Halted && !fake.Queue() && fake.Requests == 1, "reported fake failure stops retries");
  Console.WriteLine("Passed " + (candidateChecks - priorChecks) + " synthetic dispatch assertions; no IAreas adapter or game writes tested.");
 }
  // OpenAI-Codex_build_review
  // Single-controller fake only; no game scheduler, provider, or funds are accessed.
  // OpenAI-Codex_build_review
  // Recorder and callback replay checks are confined to the single-controller fake.
  static FieldInfo FakePublicField(string name) {
   return typeof(FakeStartupHarness).GetField(name, BindingFlags.Instance | BindingFlags.Public);
  }
  static bool FakeRecorderAvailable() {
   Type type = typeof(FakeStartupHarness);
   return FakePublicField("FakeWriterCalls") != null && FakePublicField("FakeWriterX") != null &&
     FakePublicField("FakeWriterZ") != null && FakePublicField("FakeWriterRequireMoney") != null &&
     FakePublicField("CallbackExecutions") != null && FakePublicField("LastDeliveredAction") != null &&
     type.GetMethod("UnlockArea", BindingFlags.Instance | BindingFlags.Public, null,
       new Type[] { typeof(int), typeof(int), typeof(bool) }, null) != null;
  }
  static int FakeInt(FakeStartupHarness fake, string name) {
   FieldInfo field = FakePublicField(name);
   return field == null ? -1 : (int)field.GetValue(fake);
  }
  static Action FakeLastDelivered(FakeStartupHarness fake) {
   FieldInfo field = FakePublicField("LastDeliveredAction");
   return field == null ? null : (Action)field.GetValue(fake);
  }
  static Action FakeQueued(FakeStartupHarness fake) {
   FieldInfo field = typeof(FakeStartupHarness).GetField("queued", BindingFlags.Instance | BindingFlags.NonPublic);
   return field == null ? null : (Action)field.GetValue(fake);
  }
  static bool FakeWriteRecordMatches(FakeStartupHarness fake, int tile, int expectedCount) {
   object money = FakePublicField("FakeWriterRequireMoney").GetValue(fake);
   return FakeInt(fake, "FakeWriterCalls") == expectedCount &&
     FakeInt(fake, "FakeWriterX") == tile % 5 && FakeInt(fake, "FakeWriterZ") == tile / 5 &&
     money is bool && (bool)money == false;
  }
  static bool CheckFakeWriterAndReplay() {
   bool passed = true;
   bool recorder = FakeRecorderAvailable();
   if (!recorder) {
    Console.Error.WriteLine("FAIL fake writer and callback recorder unavailable");
    passed = false;
   }
   var fake = new FakeStartupHarness();
   Verify(fake.Queue(), "fake replay fixture queues the original action");
   Action captured = FakeQueued(fake);
   Verify(captured != null, "fake replay fixture captures the queued action");
   fake.Deliver();
   Verify(fake.Requests == 1 && fake.PendingIndex.HasValue, "fake replay fixture has one pending request");
   int tile = fake.PendingIndex.Value;
   if (recorder && (!Object.ReferenceEquals(FakeLastDelivered(fake), captured) ||
       !FakeWriteRecordMatches(fake, tile, 1))) {
    Console.Error.WriteLine("FAIL delivered fake action and writer arguments were not recorded");
    passed = false;
   }
   int priorCallbacks = FakeInt(fake, "CallbackExecutions");
   captured();
   if (priorCallbacks >= 0 && FakeInt(fake, "CallbackExecutions") != priorCallbacks + 1) {
    Console.Error.WriteLine("FAIL pending replay did not execute the saved callback");
    passed = false;
   }
   if (priorCallbacks < 0) Console.WriteLine("RED_PENDING_REPLAY_STATUS=" + fake.LastProposalKind);
   if (fake.Requests != 1 || (recorder && FakeInt(fake, "FakeWriterCalls") != 1)) {
    Console.Error.WriteLine("FAIL pending replay issued another fake request");
    passed = false;
   }
   fake.Grid[tile] = true;
   fake.Revision++;
   Verify(fake.Observe() == Initial25DecisionKind.Confirmed, "fake replay fixture confirms its first tile");
   priorCallbacks = FakeInt(fake, "CallbackExecutions");
   captured();
   if (priorCallbacks >= 0 && FakeInt(fake, "CallbackExecutions") != priorCallbacks + 1) {
    Console.Error.WriteLine("FAIL confirmed replay did not execute the saved callback");
    passed = false;
   }
   if (fake.Requests != 1 || (recorder && FakeInt(fake, "FakeWriterCalls") != 1)) {
    Console.Error.WriteLine("FAIL replay after confirmation issued another fake request: " + fake.Requests);
    passed = false;
   }
   if (passed) Console.WriteLine("PASS actual saved fake callback replay and writer record");
   return passed;
  }

  // OpenAI-Codex_build_review
  // A recorded fake call that throws leaves an unknown fake outcome; no rollback is inferred.
  static bool CheckFakeWriterExceptions() {
   bool passed = true;
   int priorChecks = candidateChecks;
   int[] failureSteps = new int[] { 1, 23, 24 };
   foreach (int failAt in failureSteps) {
    var fake = new FakeStartupHarness();
    for (int step = 1; step < failAt; step++) {
     Verify(fake.Queue(), "earlier fake step can be queued");
     fake.Deliver();
     Verify(fake.LastProposalKind == Initial25DecisionKind.Candidate && fake.PendingIndex.HasValue &&
       fake.PendingAttempt.HasValue && FakeWriteRecordMatches(fake, fake.PendingIndex.Value, step),
       "earlier fake step invokes its no-debit writer");
     fake.Grid[fake.PendingIndex.Value] = true;
     fake.Revision++;
     Verify(fake.Observe() == Initial25DecisionKind.Confirmed, "earlier fake step explicitly confirmed");
    }
    fake.ThrowFromFakeWriter = true;
    Verify(fake.Queue(), "throwing fake writer callback can be queued");
    Action failingAction = FakeQueued(fake);
    Verify(failingAction != null, "throwing callback is retained before delivery");
    int callbacksBefore = fake.CallbackExecutions;
    Exception escaped = null;
    try { fake.Deliver(); } catch (Exception exception) { escaped = exception; }
    bool expectedEscaped = Object.ReferenceEquals(escaped, fake.FakeWriterFailure);
    bool expectedHandled = Object.ReferenceEquals(fake.FakeHandledWriterFailure, fake.FakeWriterFailure);
    if ((escaped != null && !expectedEscaped) || expectedEscaped == expectedHandled ||
        fake.FakeWriterThrowCount != 1 || fake.FakeWriterCalls != failAt ||
        !fake.PendingIndex.HasValue || !fake.PendingAttempt.HasValue ||
        !FakeWriteRecordMatches(fake, fake.PendingIndex.Value, failAt) ||
        fake.CallbackExecutions != callbacksBefore + 1 || !fake.FakeWriterQueuedBeforeException) {
     Console.Error.WriteLine("UNEXPECTED fake writer exception fixture at step " + failAt);
     return false;
    }
    Console.WriteLine("CONTROLLED_FAKE_WRITER_THROW step=" + failAt + " calls=" + fake.FakeWriterCalls +
      " throws=" + fake.FakeWriterThrowCount + " exactException=True queuedBeforeThrow=True escaped=" +
      expectedEscaped + " handled=" + expectedHandled);
    if (expectedEscaped) {
     Console.Error.WriteLine("FAIL controlled fake writer exception escaped at step " + failAt);
     passed = false;
    }
    Verify(fake.Stopped && fake.FakeWriterFailureStatus == Initial25DecisionKind.Halted,
      "fake writer exception halts matching pending before queued delivery");
    Action queuedDuringFailure = FakeQueued(fake);
    Verify(queuedDuringFailure != null && !Object.ReferenceEquals(queuedDuringFailure, failingAction),
      "a distinct fake callback was queued before the exception");
    int callsBeforeLateCallbacks = fake.FakeWriterCalls;
    int callbacksBeforeLateCallbacks = fake.CallbackExecutions;
    fake.Deliver();
    Verify(Object.ReferenceEquals(FakeLastDelivered(fake), queuedDuringFailure) &&
      fake.CallbackExecutions == callbacksBeforeLateCallbacks + 1 &&
      fake.FakeWriterCalls == callsBeforeLateCallbacks, "already queued fake callback runs without another writer call");
    queuedDuringFailure();
    failingAction();
    Verify(fake.CallbackExecutions == callbacksBeforeLateCallbacks + 3 &&
      fake.FakeWriterCalls == callsBeforeLateCallbacks, "both saved fake callbacks really replay without another writer call");
    Initial25DecisionKind observed = fake.Observe();
    if (!fake.Stopped || fake.FakeWriterFailureStatus != Initial25DecisionKind.Halted ||
        observed != Initial25DecisionKind.Halted) {
     Console.Error.WriteLine("FAIL fake writer exception did not halt matching pending at step " + failAt);
     passed = false;
    }
    if (fake.Queue()) {
     Console.Error.WriteLine("FAIL fake retry allowed after writer exception at step " + failAt);
     passed = false;
    }
   }
   if (passed) Console.WriteLine("Passed " + (candidateChecks - priorChecks) +
     " controlled fake writer exception assertions; no real rollback tested.");
   return passed;
  }

  static bool CheckSequentialFakeDispatch() {
   FieldInfo statusField = typeof(FakeStartupHarness).GetField("LastProposalKind", BindingFlags.Instance | BindingFlags.Public);
   if (statusField == null) {
    Console.Error.WriteLine("FAIL fake harness proposal status is not observable");
    return false;
   }
   int priorChecks = candidateChecks;
   Func<FakeStartupHarness, Initial25DecisionKind> lastStatus = delegate(FakeStartupHarness value) {
    object boxed = statusField.GetValue(value);
    Verify(boxed != null, "fake proposal status was recorded");
    return (Initial25DecisionKind)boxed;
   };
   var fake = new FakeStartupHarness();
   long previousAttempt = 0;
   for (int step = 1; step <= 24; step++) {
    Verify(fake.Queue(), "one synthetic action can be queued");
    fake.Deliver();
    Verify(lastStatus(fake) == Initial25DecisionKind.Candidate && fake.Requests == step &&
      fake.LastRequireMoney == false && fake.PendingIndex.HasValue && fake.PendingAttempt.HasValue,
      "every fake request is one identified no-debit candidate");
    int tile = fake.PendingIndex.Value;
    if (FakeRecorderAvailable()) Verify(FakeWriteRecordMatches(fake, tile, step), "actual fake writer received expected coordinate and false");
    Verify(fake.PendingAttempt.Value > previousAttempt && fake.Grid[tile] == false,
      "fake candidates are distinct locked tiles with increasing attempt IDs");
    previousAttempt = fake.PendingAttempt.Value;
    Verify(fake.Queue(), "pending fake status can be queued");
    fake.Deliver();
    fake.Deliver();
    Verify(lastStatus(fake) == Initial25DecisionKind.Waiting && fake.Requests == step &&
      fake.Observe() == Initial25DecisionKind.Waiting,
      "duplicate and pending deliveries do not issue or confirm another request");
    fake.Grid[tile] = true;
    fake.Revision++;
    Verify(fake.Observe() == Initial25DecisionKind.Confirmed && fake.Requests == step,
      "only an explicit newer fake observation confirms the tile");
   }
   for (int i = 0; i < fake.Grid.Length; i++)
    Verify(fake.Grid[i] == true, "all 25 fake areas are observed unlocked");
   Verify(fake.Queue(), "completion can be queried");
   fake.Deliver();
   Verify(lastStatus(fake) == Initial25DecisionKind.Complete && fake.Requests == 24,
     "completion produces no twenty-fifth fake request");

   for (int failAt = 1; failAt <= 24; failAt++) {
    fake = new FakeStartupHarness();
    for (int step = 1; step <= failAt; step++) {
     Verify(fake.Queue(), "failure scenario can queue one fake request");
     fake.Deliver();
     Verify(lastStatus(fake) == Initial25DecisionKind.Candidate && fake.Requests == step &&
       fake.LastRequireMoney == false && fake.PendingIndex.HasValue && fake.PendingAttempt.HasValue,
       "failure scenario has an identified no-debit pending attempt");
     int tile = fake.PendingIndex.Value;
     if (FakeRecorderAvailable()) Verify(FakeWriteRecordMatches(fake, tile, step), "actual fake writer received expected coordinate and false");
     Verify(fake.Grid[tile] == false, "failure scenario targets a locked fake tile");
     if (step == failAt) {
      Action oldDeliveredCallback = FakeLastDelivered(fake);
      int callbacksBeforeFailure = FakeInt(fake, "CallbackExecutions");
      int writesBeforeFailure = FakeInt(fake, "FakeWriterCalls");
      Verify(fake.Queue(), "pending fake callback is queued before failure");
      Action pendingFailureCallback = FakeQueued(fake);
      Verify(pendingFailureCallback != null, "pending callback is retained before failure");
      if (FakeRecorderAvailable()) Verify(oldDeliveredCallback != null && writesBeforeFailure == step,
        "previous fake action and writer record are available");
      Verify(fake.Fail() == Initial25DecisionKind.Halted && fake.Stopped,
        "identified fake failure halts its run");
      Verify(!fake.Queue(), "halted fake run rejects retries");
      fake.Deliver();
      if (FakeRecorderAvailable()) Verify(Object.ReferenceEquals(FakeLastDelivered(fake), pendingFailureCallback) &&
        FakeInt(fake, "CallbackExecutions") == callbacksBeforeFailure + 1 &&
        FakeInt(fake, "FakeWriterCalls") == writesBeforeFailure,
        "post-failure delivery executed without a fake writer call");
      pendingFailureCallback();
      if (FakeRecorderAvailable()) Verify(FakeInt(fake, "CallbackExecutions") == callbacksBeforeFailure + 2 &&
        FakeInt(fake, "FakeWriterCalls") == writesBeforeFailure,
        "replayed post-failure callback executed without a fake writer call");
      if (oldDeliveredCallback != null) oldDeliveredCallback();
      if (FakeRecorderAvailable()) Verify(FakeInt(fake, "CallbackExecutions") == callbacksBeforeFailure + 3 &&
        FakeInt(fake, "FakeWriterCalls") == writesBeforeFailure,
        "replayed earlier callback executed without restoring fake dispatch");
      fake.Grid[tile] = true;
      fake.Revision++;
      Verify(fake.Observe() == Initial25DecisionKind.Halted && !fake.Queue() && fake.Requests == failAt,
        "late fake observation cannot restart or add a request");
     } else {
      fake.Grid[tile] = true;
      fake.Revision++;
      Verify(fake.Observe() == Initial25DecisionKind.Confirmed,
        "earlier fake steps require explicit confirmation");
     }
    }
   }
   Console.WriteLine("Passed " + (candidateChecks - priorChecks) +
     " sequential fake dispatch assertions; no game or real funds tested.");
   return true;
  }

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
  CheckSingleRun();
  CheckMilestoneScope();
  CheckSyntheticDispatch();
   if (!CheckFakeWriterAndReplay()) Environment.ExitCode = 1;
   if (!CheckFakeWriterExceptions()) Environment.ExitCode = 1;
   if (!CheckSequentialFakeDispatch()) { Environment.ExitCode = 1; return; }
 }
}
