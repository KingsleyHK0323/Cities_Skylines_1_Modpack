// OpenAI-Codex_build_review
#if RED_STUB && FULLMAP_MATCHER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace FullMapMatcherResearch {
 public sealed class InstructionSnapshot {
  public readonly OpCode Code;
  public readonly object Operand;
  public readonly bool IsBranchDestination;
  public readonly bool HasExceptionMarker;
  public InstructionSnapshot(OpCode code, object operand, bool isBranchDestination, bool hasExceptionMarker) {
   Code = code;
   Operand = operand;
   IsBranchDestination = isBranchDestination;
   HasExceptionMarker = hasExceptionMarker;
  }
 }
 public static class MilestoneCallsiteMatcher {
  public static int Find(MethodInfo original, IList<InstructionSnapshot> instructions) { return -1; }
 }
}
#endif
