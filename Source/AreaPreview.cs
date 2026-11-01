// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Pure observation: never authorizes or executes a purchase.
public static class AreaPreview {
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
