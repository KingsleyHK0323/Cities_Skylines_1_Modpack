// OpenAI-Codex_build_review
#if RED_ADAPTER_STUB && FULLMAP_MATCHER_EXPERIMENT && FULLMAP_HARMONY_ADAPTER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace FullMapMatcherResearch {
 public static class HarmonyMatcherAdapter {
  internal static bool IsSupportedHarmony(AssemblyName identity) { return false; }
  public static MatchedSnapshot CaptureAndFind(MethodInfo original, IList<CodeInstruction> source, LocalBuilder expectedBooleanLocal) {
   return null;
  }
 }
}
#endif
