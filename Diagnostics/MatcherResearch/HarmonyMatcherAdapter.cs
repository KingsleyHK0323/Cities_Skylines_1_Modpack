// OpenAI-Codex_Full_Map_Unlock_And_Expansion
#if FULLMAP_MATCHER_EXPERIMENT && FULLMAP_HARMONY_ADAPTER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace FullMapMatcherResearch {
 // Experimental projection only: this does not register or rewrite a patch.
 public static class HarmonyMatcherAdapter {
  private sealed class CapturedInstruction {
   internal OpCode Code;
   internal object Operand;
   internal Label[] Labels;
  }

  internal static bool IsSupportedHarmony(AssemblyName identity) {
   return identity != null && String.Equals(identity.Name, "0Harmony", StringComparison.Ordinal) &&
       identity.Version != null && identity.Version.Equals(new Version(2, 4, 3, 0));
  }

  // expectedBooleanLocal is supplied by a controlled producer; its origin is
  // not authenticated by comparing a LocalBuilder reference or Label values.
  // The returned neutral snapshot is not bound to a mutable Harmony stream.
  public static MatchedSnapshot CaptureAndFind(MethodInfo original, IList<CodeInstruction> source,
      LocalBuilder expectedBooleanLocal) {
   try {
    if (!IsSupportedHarmony(typeof(CodeInstruction).Assembly.GetName()) || original == null || source == null ||
        expectedBooleanLocal == null || expectedBooleanLocal.LocalIndex != 0 ||
        expectedBooleanLocal.LocalType != typeof(bool) || expectedBooleanLocal.IsPinned || source.Count != 77) return null;
    CapturedInstruction[] captured = new CapturedInstruction[77];
    int totalLabels = 0;
    for (int i = 0; i < captured.Length; i++) {
     CodeInstruction instruction = source[i];
     if (instruction == null || instruction.labels == null || instruction.blocks == null ||
         instruction.blocks.Count != 0 || instruction.labels.Count > 11) return null;
     Label[] labels = instruction.labels.ToArray();
     totalLabels += labels.Length;
     if (totalLabels > 11) return null;
     CapturedInstruction item = new CapturedInstruction();
     item.Code = instruction.opcode;
     item.Operand = instruction.operand;
     item.Labels = labels;
     captured[i] = item;
    }
    if (source.Count != captured.Length) return null;

    Dictionary<Label, int> definitions = new Dictionary<Label, int>();
    for (int i = 0; i < captured.Length; i++) {
     foreach (Label label in captured[i].Labels) {
      if (definitions.ContainsKey(label)) return null;
      definitions.Add(label, i);
     }
    }
    HashSet<Label> used = new HashSet<Label>();
    List<InstructionSnapshot> projected = new List<InstructionSnapshot>(captured.Length);
    for (int i = 0; i < captured.Length; i++) {
     CapturedInstruction item = captured[i];
     object operand = item.Operand;
     switch (item.Code.OperandType) {
      case OperandType.InlineNone:
       if (operand != null) return null;
       break;
      case OperandType.InlineField:
       if (!(operand is FieldInfo)) return null;
       break;
      case OperandType.InlineMethod:
       if (!(operand is MethodBase)) return null;
       break;
      case OperandType.InlineBrTarget:
      case OperandType.ShortInlineBrTarget:
       if (!(operand is Label)) return null;
       Label label = (Label)operand;
       int target;
       if (!definitions.TryGetValue(label, out target)) return null;
       used.Add(label);
       operand = target;
       break;
      case OperandType.ShortInlineVar:
       if (item.Code != OpCodes.Ldloca_S || !Object.ReferenceEquals(operand, expectedBooleanLocal)) return null;
       operand = expectedBooleanLocal.LocalIndex;
       break;
      default:
       return null;
     }
     projected.Add(new InstructionSnapshot(item.Code, operand, item.Labels.Length != 0, false));
    }
    if (used.Count != definitions.Count) return null;
    return MilestoneCallsiteMatcher.CaptureAndFind(original, projected);
   } catch (Exception) {
    return null;
   }
  }
 }
}
#endif
