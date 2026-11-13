// OpenAI-Codex_release_isolation
#if FULLMAP_MATCHER_EXPERIMENT && FULLMAP_HARMONY_ADAPTER_EXPERIMENT && FULLMAP_NATIVE_TRANSPILER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace FullMapMatcherResearch {
 // RED-only stub. It cannot build or register a native projection.
 internal static class NativeMilestoneTranspilerCandidate {
  internal static bool TryBuildIdentityProjection(MethodInfo original,
      IEnumerable<CodeInstruction> source, LocalBuilder expectedBooleanLocal,
      out IList<CodeInstruction> output) {
   output = null;
   return false;
  }

  internal static IEnumerable<CodeInstruction> DisabledTranspiler(
      IEnumerable<CodeInstruction> instructions, MethodBase original) {
   return instructions;
  }

  internal static bool PreserveGateResult(bool original, object manager, int x, int z) {
   return original;
  }
 }
}
#endif
