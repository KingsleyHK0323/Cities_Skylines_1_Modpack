// OpenAI-Codex_Full_Map_Unlock_And_Expansion
// Positive control: forcing legacy sources into an external Release compile includes them.
using System;
using System.Reflection;

static class LegacyReleaseBoundaryChecks {
 static void Main() {
  Assembly assembly = typeof(LegacyReleaseBoundaryChecks).Assembly;
  if (assembly.GetType("FullMapUnlockAndExpansion.RedactedDiagnosticBuffer") == null)
   throw new Exception("forced legacy buffer must be present in this positive control");
  if (assembly.GetType("FullMapUnlockAndExpansion.LifecycleDiagnostics.LifecycleTraceCandidate") == null)
   throw new Exception("forced legacy trace must be present in this positive control");
  Console.WriteLine("Passed 2 forced-source Release positive controls; project exclusion remains required.");
 }
}
