// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Pure observation: never authorizes or executes a purchase.
public sealed class Initial25Tile {
 public readonly int X, Z, NativeIndex;
 public readonly bool? IsUnlocked;
 public Initial25Tile(int x, int z, int nativeIndex, bool? isUnlocked) {
  X = x; Z = z; NativeIndex = nativeIndex; IsUnlocked = isUnlocked;
 }
}
public enum Initial25PreviousAttempt { None, Pending, Failed, ObservedUnlocked }
public enum Initial25DecisionKind { Unavailable, Waiting, Halted, Complete, Candidate, Confirmed }
public sealed class Initial25NoDebitDecision {
 public readonly Initial25DecisionKind Kind;
 public readonly int? NativeIndex;
 public readonly long? AttemptId;
 // This is a proposed argument, never proof that a game transaction did not debit funds.
 public readonly bool? RequireMoney;
 internal Initial25NoDebitDecision(Initial25DecisionKind kind, int? nativeIndex) : this(kind, nativeIndex, null) { }
 internal Initial25NoDebitDecision(Initial25DecisionKind kind, int? nativeIndex, long? attemptId) {
  Kind = kind; NativeIndex = nativeIndex; AttemptId = attemptId;
  RequireMoney = kind == Initial25DecisionKind.Candidate ? (bool?)false : null;
 }
}
// Isolated policy experiment, not a game hook or a write authorization. All context
// values are supplied by the caller and are not authenticated against the game.
public sealed class StartupMilestoneScopePreview {
 private readonly object gate = new object();
 private readonly object owner, provider;
 private readonly System.Threading.Thread simulationThread;
 private readonly long generation, attemptId;
 private readonly int x, z;
 private readonly bool valid;
 private bool closed;
 public StartupMilestoneScopePreview(object owner, object provider, System.Threading.Thread simulationThread,
     long generation, long attemptId, int x, int z) {
  this.owner = owner; this.provider = provider; this.simulationThread = simulationThread;
  this.generation = generation; this.attemptId = attemptId; this.x = x; this.z = z;
  valid = owner != null && provider != null && simulationThread != null && generation > 0 && attemptId > 0 &&
      x >= 0 && x < 5 && z >= 0 && z < 5;
 }
 // True means only that this synthetic milestone exception may be considered once.
 // It says nothing about the remaining native eligibility checks or game write safety.
 public bool TryUse(bool? nativeMilestone, object currentOwner, object currentProvider, long? currentGeneration,
     long? currentAttemptId, int? currentX, int? currentZ, bool? ordinaryNewGame, bool? loadingComplete, bool? insideNativeUnlock) {
  lock (gate) {
   if (closed || !valid || nativeMilestone != false || ordinaryNewGame != true || loadingComplete != true ||
       insideNativeUnlock != true || !object.ReferenceEquals(owner, currentOwner) ||
       !object.ReferenceEquals(provider, currentProvider) ||
       !object.ReferenceEquals(simulationThread, System.Threading.Thread.CurrentThread) ||
       currentGeneration != generation || currentAttemptId != attemptId || currentX != x || currentZ != z) return false;
   closed = true;
   return true;
  }
 }
 public void Invalidate() { lock (gate) { closed = true; } }
}
// In-memory coordination for one supplied snapshot stream only. This neither performs a
// game write nor authenticates the supplied tiles, startup flag, revisions, or funds.
// A new instance or process has independent state; callers must supply stable snapshots.
public sealed class Initial25NoDebitRun {
 private enum Stage { Unstarted, Ready, Pending, Halted, Complete }
 private readonly object gate = new object();
 private Stage stage;
 private long generation, revision, nextAttempt, pendingAttempt;
 private int pendingIndex;
 private bool pendingUnlocked;
 private bool[] baseline;

 private static Initial25NoDebitDecision Decision(Initial25DecisionKind kind) {
  return new Initial25NoDebitDecision(kind, null);
 }
 private Initial25NoDebitDecision Halt() {
  stage = Stage.Halted;
  return Decision(Initial25DecisionKind.Halted);
 }
 private bool ChangedGeneration(long? capturedGeneration, long? currentGeneration) {
  return stage != Stage.Unstarted &&
      ((capturedGeneration.HasValue && capturedGeneration.Value != generation) ||
       (currentGeneration.HasValue && currentGeneration.Value != generation));
 }
 private static bool[] ReadGrid(Initial25Tile[] snapshot, int? width, int? height, int? count,
     long? capturedGeneration, long? currentGeneration, long? capturedRevision, long? currentRevision) {
  if (AreaPreview.MissingForInitial25(snapshot, width, height, count, capturedGeneration,
      currentGeneration, capturedRevision, currentRevision) == null) return null;
  bool[] grid = new bool[25];
  foreach (Initial25Tile tile in snapshot) grid[tile.NativeIndex] = tile.IsUnlocked.Value;
  return grid;
 }
 private Initial25NoDebitDecision CheckPending(Initial25Tile[] tiles, int? width, int? height, int? count,
     long? capturedGeneration, long? currentGeneration, long? capturedRevision, long? currentRevision, bool confirm) {
  Initial25Tile[] snapshot = tiles == null ? null : (Initial25Tile[])tiles.Clone();
  bool[] grid = ReadGrid(snapshot, width, height, count, capturedGeneration, currentGeneration, capturedRevision, currentRevision);
  if (grid == null || capturedRevision.Value < revision) return Decision(Initial25DecisionKind.Waiting);
  for (int i = 0; i < grid.Length; i++) {
   if (i != pendingIndex && grid[i] != baseline[i]) return Halt();
  }
  bool unlocked = grid[pendingIndex];
  if ((capturedRevision.Value == revision && unlocked != pendingUnlocked) || (pendingUnlocked && !unlocked)) return Halt();
  revision = capturedRevision.Value;
  pendingUnlocked = unlocked;
  if (confirm && pendingUnlocked) {
   baseline = grid;
   stage = Stage.Ready;
   return Decision(Initial25DecisionKind.Confirmed);
  }
  return Decision(Initial25DecisionKind.Waiting);
 }
 public Initial25NoDebitDecision Propose(Initial25Tile[] tiles, int? width, int? height, int? count,
     long? capturedGeneration, long? currentGeneration, long? capturedRevision, long? currentRevision, bool? isNewGame) {
  lock (gate) {
   if (stage == Stage.Halted) return Decision(Initial25DecisionKind.Halted);
   if (ChangedGeneration(capturedGeneration, currentGeneration) || (stage != Stage.Unstarted && isNewGame == false)) return Halt();
   if (isNewGame != true || !capturedGeneration.HasValue || !currentGeneration.HasValue)
    return Decision(stage == Stage.Pending ? Initial25DecisionKind.Waiting : Initial25DecisionKind.Unavailable);
   if (stage == Stage.Pending) return CheckPending(tiles, width, height, count, capturedGeneration,
       currentGeneration, capturedRevision, currentRevision, false);

   Initial25Tile[] snapshot = tiles == null ? null : (Initial25Tile[])tiles.Clone();
   bool[] grid = ReadGrid(snapshot, width, height, count, capturedGeneration, currentGeneration, capturedRevision, currentRevision);
   if (grid == null) return Decision(Initial25DecisionKind.Unavailable);
   if (stage != Stage.Unstarted) {
    if (capturedRevision.Value < revision) return Decision(Initial25DecisionKind.Unavailable);
    for (int i = 0; i < grid.Length; i++) if (grid[i] != baseline[i]) return Halt();
   }
   Initial25NoDebitDecision decision = AreaPreview.PreviewInitial25NoDebit(snapshot, width, height, count,
       capturedGeneration, currentGeneration, capturedRevision, currentRevision, isNewGame,
       Initial25PreviousAttempt.None, null, null, null);
   if (decision.Kind == Initial25DecisionKind.Candidate || decision.Kind == Initial25DecisionKind.Complete) {
    generation = capturedGeneration.Value;
    revision = capturedRevision.Value;
    baseline = grid;
    if (decision.Kind == Initial25DecisionKind.Candidate) {
     if (nextAttempt == long.MaxValue) return Halt();
     pendingIndex = decision.NativeIndex.Value;
     pendingAttempt = ++nextAttempt;
     pendingUnlocked = false;
     stage = Stage.Pending;
     return new Initial25NoDebitDecision(Initial25DecisionKind.Candidate, pendingIndex, pendingAttempt);
    } else stage = Stage.Complete;
   }
   return decision;
  }
 }
 public Initial25NoDebitDecision Observe(Initial25Tile[] tiles, int? width, int? height, int? count,
     long? capturedGeneration, long? currentGeneration, long? capturedRevision, long? currentRevision, bool? isNewGame) {
  lock (gate) {
   if (stage == Stage.Halted) return Decision(Initial25DecisionKind.Halted);
   if (ChangedGeneration(capturedGeneration, currentGeneration) || (stage != Stage.Unstarted && isNewGame == false)) return Halt();
   if (stage == Stage.Unstarted) return Decision(Initial25DecisionKind.Unavailable);
   if (isNewGame != true || !capturedGeneration.HasValue || !currentGeneration.HasValue)
    return Decision(stage == Stage.Pending ? Initial25DecisionKind.Waiting : Initial25DecisionKind.Unavailable);
   if (stage != Stage.Pending) {
    Initial25Tile[] snapshot = tiles == null ? null : (Initial25Tile[])tiles.Clone();
    bool[] grid = ReadGrid(snapshot, width, height, count, capturedGeneration, currentGeneration, capturedRevision, currentRevision);
    if (grid == null || capturedRevision.Value < revision) return Decision(Initial25DecisionKind.Unavailable);
    for (int i = 0; i < grid.Length; i++) if (grid[i] != baseline[i]) return Halt();
    revision = capturedRevision.Value;
    return Decision(Initial25DecisionKind.Unavailable);
   }
   return CheckPending(tiles, width, height, count, capturedGeneration,
       currentGeneration, capturedRevision, currentRevision, true);
  }
 }
 public Initial25NoDebitDecision FailPending(long? attemptId) {
  lock (gate) {
   if (stage == Stage.Halted) return Decision(Initial25DecisionKind.Halted);
   return stage == Stage.Pending && attemptId.HasValue && attemptId.Value == pendingAttempt
       ? Halt() : Decision(Initial25DecisionKind.Unavailable);
  }
 }
}
public static class AreaPreview {
 // Returns a detached candidate set, not an execution order or permission to unlock.
 // Coordinate/index labels are caller-supplied and cannot authenticate the observed value.
 // Callers must prove that each value was obtained from its declared native coordinate;
 // without that proof, do not use this preview as a basis for game writes.
 // Callers must provide a stable snapshot and trustworthy generation/revision values.
 public static int[] MissingForInitial25(Initial25Tile[] tiles, int? width, int? height, int? reportedUnlockedCount,
     long? capturedGeneration, long? currentGeneration, long? capturedRevision, long? currentRevision) {
  if (tiles == null || tiles.Length != 25 || width != 5 || height != 5 ||
      !reportedUnlockedCount.HasValue || reportedUnlockedCount.Value < 0 || reportedUnlockedCount.Value > 25 ||
      !capturedGeneration.HasValue || !currentGeneration.HasValue ||
      capturedGeneration.Value <= 0 || capturedGeneration.Value != currentGeneration.Value ||
      !capturedRevision.HasValue || !currentRevision.HasValue ||
      capturedRevision.Value <= 0 || capturedRevision.Value != currentRevision.Value) return null;

  bool[] seen = new bool[25], unlocked = new bool[25];
  int owned = 0;
  for (int i = 0; i < tiles.Length; i++) {
   Initial25Tile tile = tiles[i];
   if (tile == null || !tile.IsUnlocked.HasValue || tile.X < 0 || tile.X >= 5 || tile.Z < 0 || tile.Z >= 5) return null;
   int index = tile.Z * 5 + tile.X;
   if (tile.NativeIndex != index || seen[index]) return null;
   seen[index] = true;
   unlocked[index] = tile.IsUnlocked.Value;
   if (tile.IsUnlocked.Value) owned++;
  }
  if (owned != reportedUnlockedCount.Value) return null;
  int[] result = new int[25 - owned];
  int count = 0;
  for (int index = 0; index < seen.Length; index++) {
   if (!seen[index]) return null;
   if (!unlocked[index]) result[count++] = index;
  }
  return result;
 }
 // Stateless synthetic preview only. Caller-supplied startup/attempt states are not verified;
 // a new call with None can propose again after a failure. The caller must retain attempt history,
 // prove snapshot provenance, and never treat this result as authority to call the game's write API.
 // Any future game adapter must also revalidate the actual call's x and z are each in 0..4.
 public static Initial25NoDebitDecision PreviewInitial25NoDebit(Initial25Tile[] tiles,
     int? width, int? height, int? reportedUnlockedCount, long? capturedGeneration,
     long? currentGeneration, long? capturedRevision, long? currentRevision, bool? atStartup,
     Initial25PreviousAttempt previous, int? previousIndex, long? previousGeneration, long? previousRevision) {
  if (atStartup != true) return new Initial25NoDebitDecision(Initial25DecisionKind.Unavailable, null);
  if (previous == Initial25PreviousAttempt.Pending) return new Initial25NoDebitDecision(Initial25DecisionKind.Waiting, null);
  if (previous == Initial25PreviousAttempt.Failed) return new Initial25NoDebitDecision(Initial25DecisionKind.Halted, null);
  if (previous != Initial25PreviousAttempt.None && previous != Initial25PreviousAttempt.ObservedUnlocked)
   return new Initial25NoDebitDecision(Initial25DecisionKind.Unavailable, null);
  if (previous == Initial25PreviousAttempt.None && (previousIndex.HasValue || previousGeneration.HasValue || previousRevision.HasValue))
   return new Initial25NoDebitDecision(Initial25DecisionKind.Unavailable, null);

  Initial25Tile[] snapshot = tiles == null ? null : (Initial25Tile[])tiles.Clone();
  int[] missing = MissingForInitial25(snapshot, width, height, reportedUnlockedCount,
      capturedGeneration, currentGeneration, capturedRevision, currentRevision);
  if (missing == null) return new Initial25NoDebitDecision(Initial25DecisionKind.Unavailable, null);
  bool[] unlocked = new bool[25];
  foreach (Initial25Tile tile in snapshot) unlocked[tile.NativeIndex] = tile.IsUnlocked.Value;

  if (previous == Initial25PreviousAttempt.ObservedUnlocked) {
   if (!previousIndex.HasValue || previousIndex.Value < 0 || previousIndex.Value >= 25 ||
       !previousGeneration.HasValue || previousGeneration.Value != capturedGeneration.Value ||
       !previousRevision.HasValue || previousRevision.Value <= 0)
    return new Initial25NoDebitDecision(Initial25DecisionKind.Unavailable, null);
   if (capturedRevision.Value <= previousRevision.Value)
    return new Initial25NoDebitDecision(Initial25DecisionKind.Waiting, null);
   if (!unlocked[previousIndex.Value]) return new Initial25NoDebitDecision(Initial25DecisionKind.Halted, null);
  }
  if (missing.Length == 0) return new Initial25NoDebitDecision(Initial25DecisionKind.Complete, null);
  foreach (int index in missing) {
   int x = index % 5, z = index / 5;
   if ((x > 0 && unlocked[index - 1]) || (x < 4 && unlocked[index + 1]) ||
       (z > 0 && unlocked[index - 5]) || (z < 4 && unlocked[index + 5]))
    return new Initial25NoDebitDecision(Initial25DecisionKind.Candidate, index);
  }
  return new Initial25NoDebitDecision(Initial25DecisionKind.Unavailable, null);
 }
 // Generation tokens are assigned by the future observation owner, not game IDs.
 public static string DescribeObservation(bool? eligibility, int? count, int? price,
     long? capturedGeneration, long? currentGeneration, int? x, int? z) {
  if (!capturedGeneration.HasValue || !currentGeneration.HasValue ||
      capturedGeneration.Value <= 0 || currentGeneration.Value <= 0 ||
      capturedGeneration.Value != currentGeneration.Value) return "Unknown";
  if (!x.HasValue || !z.HasValue) return "Unknown";
  if (x.Value < 0 || x.Value >= 5 || z.Value < 0 || z.Value >= 5)
   return "Outside preview scope";
  return Describe(eligibility, count, price);
 }
 // Legacy input-only classification has no freshness guarantee.
 public static string Describe(bool? nativeEligibility, int? purchasedCount, int? nativePrice) {
  if (!nativeEligibility.HasValue || !purchasedCount.HasValue || !nativePrice.HasValue || purchasedCount.Value < 0 || nativePrice.Value < 0) return "Unknown";
  if (purchasedCount.Value >= 25) return "Outside preview scope";
  return nativeEligibility.Value ? "Observed native eligibility only" : "Native refusal";
 }
}
