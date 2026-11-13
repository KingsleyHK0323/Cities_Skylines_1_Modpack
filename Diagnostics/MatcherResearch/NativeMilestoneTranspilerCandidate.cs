// OpenAI-Codex_Full_Map_Unlock_And_Expansion
#if FULLMAP_MATCHER_EXPERIMENT && FULLMAP_HARMONY_ADAPTER_EXPERIMENT && FULLMAP_NATIVE_TRANSPILER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace FullMapMatcherResearch {
 // Isolated research only: no registration, authorization, activation, or game write.
 internal static class NativeMilestoneTranspilerCandidate {
  // The disabled Harmony entry point preserves its input and never enumerates it.
  internal static IEnumerable<CodeInstruction> DisabledTranspiler(
      IEnumerable<CodeInstruction> instructions, MethodBase original) {
   return instructions;
  }

  // A successful result is an owned identity-filter projection, not permission
  // to patch a game method or a guarantee about the producer or ILGenerator.
  internal static bool TryBuildIdentityProjection(MethodInfo original,
      IEnumerable<CodeInstruction> source, LocalBuilder expectedBooleanLocal,
      out IList<CodeInstruction> output) {
   output = null;
   try {
    if (original == null || source == null || expectedBooleanLocal == null) return false;
    List<CodeInstruction> owned = new List<CodeInstruction>(77);
    foreach (CodeInstruction instruction in source) {
     if (owned.Count >= 77 || instruction == null || instruction.labels == null ||
         instruction.blocks == null || instruction.blocks.Count != 0 ||
         instruction.labels.Count > 11) return false;
     Label[] labels = instruction.labels.ToArray();
     if (labels.Length > 11) return false;
     CodeInstruction copy = new CodeInstruction(instruction.opcode, instruction.operand);
     copy.labels.AddRange(labels);
     owned.Add(copy);
    }
    if (owned.Count != 77) return false;

    MatchedSnapshot matched = HarmonyMatcherAdapter.CaptureAndFind(
      original, owned, expectedBooleanLocal);
    if (matched == null || matched.Count != owned.Count || matched.CallIndex != 24) return false;
    int gate = matched.CallIndex;
    if (owned[gate].opcode != OpCodes.Callvirt || owned[gate].labels.Count != 0 ||
        owned[gate].blocks.Count != 0 || owned[gate + 1].opcode != OpCodes.Brtrue ||
        owned[gate + 1].labels.Count != 0 || owned[gate + 1].blocks.Count != 0) return false;

    MethodInfo preserve = typeof(NativeMilestoneTranspilerCandidate).GetMethod(
      "PreserveGateResult", BindingFlags.NonPublic | BindingFlags.Static);
    if (preserve == null || !preserve.IsStatic || preserve.ReturnType != typeof(bool)) return false;
    ParameterInfo[] parameters = preserve.GetParameters();
    if (parameters.Length != 4 || parameters[0].ParameterType != typeof(bool) ||
        parameters[1].ParameterType != typeof(object) ||
        parameters[2].ParameterType != typeof(int) || parameters[3].ParameterType != typeof(int)) return false;

    owned.Insert(gate + 1, new CodeInstruction(OpCodes.Ldarg_0));
    owned.Insert(gate + 2, new CodeInstruction(OpCodes.Ldarg_1));
    owned.Insert(gate + 3, new CodeInstruction(OpCodes.Ldarg_2));
    owned.Insert(gate + 4, new CodeInstruction(OpCodes.Call, preserve));
    output = owned;
    return true;
   } catch (Exception) {
    return false;
   }
  }

  // The inspected gate still runs once; this deliberately preserves its result.
  internal static bool PreserveGateResult(bool original, object manager, int x, int z) {
   return original;
  }
 }
}
#endif
