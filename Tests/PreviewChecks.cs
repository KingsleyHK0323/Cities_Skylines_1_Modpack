// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Pending cloud execution: no local build or test run is authorized.
using System;
class PreviewChecks {
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
 }
}
