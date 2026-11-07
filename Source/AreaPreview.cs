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
public enum Initial25DecisionKind { Unavailable, Waiting, Halted, Complete, Candidate }
public sealed class Initial25NoDebitDecision {
 public readonly Initial25DecisionKind Kind;
 public readonly int? NativeIndex;
 // This is a proposed argument, never proof that a game transaction did not debit funds.
 public readonly bool? RequireMoney;
 internal Initial25NoDebitDecision(Initial25DecisionKind kind, int? nativeIndex) {
  Kind = kind; NativeIndex = nativeIndex;
  RequireMoney = kind == Initial25DecisionKind.Candidate ? (bool?)false : null;
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
