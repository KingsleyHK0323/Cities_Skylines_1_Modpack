// OpenAI-Codex_Full_Map_Unlock_And_Expansion
#if FULLMAP_MATCHER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;

namespace FullMapMatcherResearch {
 // A caller maps a fully materialized instruction stream to this neutral form.
 // Branch operands are zero-based instruction indices, not byte offsets.
 public sealed class InstructionSnapshot {
  public readonly OpCode Code;
  public readonly object Operand;
  public readonly bool IsBranchDestination;
  public readonly bool HasExceptionMarker;

  public InstructionSnapshot(OpCode code, object operand, bool isBranchDestination, bool hasExceptionMarker) {
   Code = code;
   int[] targets = operand as int[];
   Operand = targets == null ? operand : (object)targets.Clone();
   IsBranchDestination = isBranchDestination;
   HasExceptionMarker = hasExceptionMarker;
  }
 }

 // Owns the checked neutral projection; it does not own a Harmony stream.
 public sealed class MatchedSnapshot {
  private readonly InstructionSnapshot[] instructions;
  public readonly int CallIndex;

  internal MatchedSnapshot(InstructionSnapshot[] source, int callIndex) {
   instructions = new InstructionSnapshot[source.Length];
   for (int i = 0; i < source.Length; i++) {
    InstructionSnapshot item = source[i];
    instructions[i] = new InstructionSnapshot(item.Code, item.Operand, item.IsBranchDestination, item.HasExceptionMarker);
   }
   CallIndex = callIndex;
  }

  public int Count { get { return instructions.Length; } }

  public InstructionSnapshot GetInstruction(int index) {
   InstructionSnapshot item = instructions[index];
   return new InstructionSnapshot(item.Code, item.Operand, item.IsBranchDestination, item.HasExceptionMarker);
  }
 }

 // This is a version-bound structural check, not authorization to rewrite IL.
 public static class MilestoneCallsiteMatcher {
  private static readonly Guid ExpectedMvid = new Guid("e8d46364-9995-4176-87e0-ae3192b9a20b");
  private const int OriginalToken = 0x0600324C;
  private const int GateToken = 0x060046FF;
  private const int MaxGetterToken = 0x0600321F;
  private const int SingletonReferenceToken = 0x0A000172;
  private const int CountFieldToken = 0x040029F0;
  private const int GridFieldToken = 0x040029F2;
  private const string ExpectedBodyHash = "728306E5DCCDE1012A529CB332094E8F87296389D8675F68B10EE01E27D82BF6";

  // A future adapter must still bind this projection to its unchanged source.
  public static MatchedSnapshot CaptureAndFind(MethodInfo original, IList<InstructionSnapshot> instructions) {
   try {
    if (instructions == null || instructions.Count != 77) return null;
    InstructionSnapshot[] captured = new InstructionSnapshot[77];
    for (int i = 0; i < captured.Length; i++) {
     InstructionSnapshot item = instructions[i];
     if (item == null) return null;
     captured[i] = new InstructionSnapshot(item.Code, item.Operand, item.IsBranchDestination, item.HasExceptionMarker);
    }
    if (instructions.Count != captured.Length) return null;
    int index = Find(original, captured);
    return index < 0 ? null : new MatchedSnapshot(captured, index);
   } catch (Exception) {
    return null;
   }
  }

  // The result is an index in this input snapshot, or -1 for any uncertainty.
  // A future caller must bind its own unchanged source stream to the snapshot.
  internal static int Find(MethodInfo original, IList<InstructionSnapshot> instructions) {
   try {
    if (!ExpectedOriginal(original) || instructions == null) return -1;
    int length = instructions.Count;
    if (length < 16) return -1;
    if (length != 77) return -1;
    InstructionSnapshot[] snapshot = new InstructionSnapshot[length];
    for (int i = 0; i < length; i++) {
     InstructionSnapshot item = instructions[i];
     if (item == null || item.HasExceptionMarker || item.Code.OpCodeType == OpCodeType.Prefix) return -1;
     if (item.Code.OperandType == OperandType.InlineMethod && !(item.Operand is MethodBase)) return -1;
     if (item.Code.OperandType == OperandType.InlineField && !(item.Operand is FieldInfo)) return -1;
     snapshot[i] = new InstructionSnapshot(item.Code, item.Operand, item.IsBranchDestination, item.HasExceptionMarker);
    }
    if (instructions.Count != length) return -1;
    if (!MatchesOriginalInstructions(original, snapshot)) return -1;

    Module module = original.Module;
    MethodInfo gate = module.ResolveMethod(GateToken) as MethodInfo;
    MethodInfo maximum = module.ResolveMethod(MaxGetterToken) as MethodInfo;
    MethodInfo singleton = module.ResolveMethod(SingletonReferenceToken) as MethodInfo;
    FieldInfo count = module.ResolveField(CountFieldToken);
    FieldInfo grid = module.ResolveField(GridFieldToken);
    if (!ExpectedMembers(module, gate, maximum, singleton, count, grid)) return -1;

    int callIndex = -1;
    int references = 0;
    for (int i = 0; i < length; i++) {
     MethodBase operand = snapshot[i].Operand as MethodBase;
     if (operand != null && SameMember(operand, gate)) {
      references++;
      callIndex = i;
     }
    }
    if (references != 1 || callIndex < 10 || callIndex + 5 >= length) return -1;
    int start = callIndex - 10;
    int singletonIndex = callIndex - 3;
    int ownershipIndex = callIndex + 4;

    if (snapshot[start].Code != OpCodes.Ldarg_0 ||
        snapshot[start + 1].Code != OpCodes.Ldfld || !SameMember(snapshot[start + 1].Operand as FieldInfo, count) ||
        snapshot[start + 2].Code != OpCodes.Ldarg_0 ||
        snapshot[start + 3].Code != OpCodes.Call || !SameMember(snapshot[start + 3].Operand as MethodInfo, maximum) ||
        snapshot[start + 4].Code != OpCodes.Blt || !IsTarget(snapshot[start + 4].Operand, singletonIndex) ||
        snapshot[start + 5].Code != OpCodes.Ldc_I4_0 || snapshot[start + 6].Code != OpCodes.Ret ||
        snapshot[singletonIndex].Code != OpCodes.Call || !SameMember(snapshot[singletonIndex].Operand as MethodInfo, singleton) ||
        snapshot[callIndex - 2].Code != OpCodes.Ldarg_0 ||
        snapshot[callIndex - 1].Code != OpCodes.Ldfld || !SameMember(snapshot[callIndex - 1].Operand as FieldInfo, count) ||
        snapshot[callIndex].Code != OpCodes.Callvirt || !SameMember(snapshot[callIndex].Operand as MethodInfo, gate) ||
        snapshot[callIndex + 1].Code != OpCodes.Brtrue || !IsTarget(snapshot[callIndex + 1].Operand, ownershipIndex) ||
        snapshot[callIndex + 2].Code != OpCodes.Ldc_I4_0 || snapshot[callIndex + 3].Code != OpCodes.Ret ||
        snapshot[ownershipIndex].Code != OpCodes.Ldarg_0 ||
        snapshot[ownershipIndex + 1].Code != OpCodes.Ldfld || !SameMember(snapshot[ownershipIndex + 1].Operand as FieldInfo, grid)) return -1;

    if (!snapshot[singletonIndex].IsBranchDestination || !snapshot[ownershipIndex].IsBranchDestination) return -1;
    for (int i = start + 1; i <= ownershipIndex + 1; i++) {
     if (i != singletonIndex && i != ownershipIndex && snapshot[i].IsBranchDestination) return -1;
    }
    for (int i = 0; i < length; i++) {
     OperandType kind = snapshot[i].Code.OperandType;
     if (kind == OperandType.InlineBrTarget || kind == OperandType.ShortInlineBrTarget) {
      if (!(snapshot[i].Operand is int) || !AllowedBranch(snapshot, i, (int)snapshot[i].Operand, start, callIndex)) return -1;
     } else if (kind == OperandType.InlineSwitch) {
      int[] targets = snapshot[i].Operand as int[];
      if (targets == null) return -1;
      foreach (int target in targets) {
       if (!AllowedBranch(snapshot, i, target, start, callIndex)) return -1;
      }
     }
    }
    return callIndex;
   } catch (Exception) {
    return -1;
   }
  }

  // Compare the entire neutral projection against this reference method's IL.
  // Only operand forms present in the supported reference are accepted.
  private static bool MatchesOriginalInstructions(MethodInfo original, InstructionSnapshot[] snapshot) {
   if (!BitConverter.IsLittleEndian) return false;
   byte[] bytes = original.GetMethodBody().GetILAsByteArray();
   if (bytes == null || bytes.Length != 167 || snapshot.Length != 77) return false;
   int[] offsets = new int[snapshot.Length];
   long[] branchOffsets = new long[snapshot.Length];
   bool[] branches = new bool[snapshot.Length];
   int position = 0;
   for (int i = 0; i < snapshot.Length; i++) {
    InstructionSnapshot item = snapshot[i];
    offsets[i] = position;
    OpCode code = item.Code;
    ushort value = unchecked((ushort)code.Value);
    if (code.Size == 1) {
     if (!CanRead(bytes, position, 1) || value > 255 || bytes[position] != (byte)value) return false;
     position++;
    } else if (code.Size == 2) {
     if (!CanRead(bytes, position, 2) || (value >> 8) != 0xFE ||
         bytes[position] != 0xFE || bytes[position + 1] != (byte)value) return false;
     position += 2;
    } else {
     return false;
    }
    switch (code.OperandType) {
     case OperandType.InlineNone:
      if (item.Operand != null) return false;
      break;
     case OperandType.ShortInlineVar:
      if (!CanRead(bytes, position, 1) || !(item.Operand is int) || (int)item.Operand != bytes[position]) return false;
      position++;
      break;
     case OperandType.InlineField:
      if (!CanRead(bytes, position, 4) || !(item.Operand is FieldInfo)) return false;
      if (!SameMember((FieldInfo)item.Operand, original.Module.ResolveField(BitConverter.ToInt32(bytes, position)))) return false;
      position += 4;
      break;
     case OperandType.InlineMethod:
      if (!CanRead(bytes, position, 4) || !(item.Operand is MethodBase)) return false;
      if (!SameMember((MethodBase)item.Operand, original.Module.ResolveMethod(BitConverter.ToInt32(bytes, position)))) return false;
      position += 4;
      break;
     case OperandType.InlineBrTarget:
      if (!CanRead(bytes, position, 4) || !(item.Operand is int)) return false;
      branchOffsets[i] = (long)position + 4 + BitConverter.ToInt32(bytes, position);
      branches[i] = true;
      position += 4;
      break;
     case OperandType.ShortInlineBrTarget:
      if (!CanRead(bytes, position, 1) || !(item.Operand is int)) return false;
      branchOffsets[i] = (long)position + 1 + unchecked((sbyte)bytes[position]);
      branches[i] = true;
      position++;
      break;
     default:
      return false;
    }
   }
   if (position != bytes.Length) return false;
   bool[] destinations = new bool[snapshot.Length];
   for (int i = 0; i < snapshot.Length; i++) {
    if (!branches[i]) continue;
    int target = (int)snapshot[i].Operand;
    if (target < 0 || target >= snapshot.Length || branchOffsets[i] != offsets[target]) return false;
    destinations[target] = true;
   }
   for (int i = 0; i < snapshot.Length; i++) {
    if (snapshot[i].IsBranchDestination != destinations[i]) return false;
   }
   return true;
  }

  private static bool CanRead(byte[] bytes, int position, int count) {
   return position >= 0 && count >= 0 && position <= bytes.Length - count;
  }

  private static bool ExpectedOriginal(MethodInfo method) {
   if (!ExpectedMethod(method, "GameAreaManager", "CanUnlock", "System.Boolean", false, new string[] { "System.Int32", "System.Int32" })) return false;
   Module module = method.Module;
   AssemblyName name = module.Assembly.GetName();
   if (name.Name != "Assembly-CSharp" || name.Version == null || name.Version.ToString() != "0.0.0.0" ||
       module.ModuleVersionId != ExpectedMvid || method.MetadataToken != OriginalToken) return false;
   MethodBody body = method.GetMethodBody();
   if (body == null || body.ExceptionHandlingClauses.Count != 0) return false;
   if (!body.InitLocals || body.MaxStackSize != 4 || body.LocalVariables.Count != 1 ||
       body.LocalVariables[0].LocalIndex != 0 || body.LocalVariables[0].IsPinned ||
       body.LocalVariables[0].LocalType.FullName != "System.Boolean") return false;
   byte[] bytes = body.GetILAsByteArray();
   if (bytes == null || bytes.Length != 167) return false;
   using (SHA256 hash = SHA256.Create()) {
    return String.Equals(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", String.Empty), ExpectedBodyHash, StringComparison.Ordinal);
   }
  }

  private static bool ExpectedMembers(Module module, MethodInfo gate, MethodInfo maximum, MethodInfo singleton, FieldInfo count, FieldInfo grid) {
   if (!ExpectedMethod(gate, "UnlockManager", "Unlocked", "System.Boolean", false, new string[] { "System.Int32" }) ||
       gate.Module != module || gate.MetadataToken != GateToken ||
       !ExpectedMethod(maximum, "GameAreaManager", "get_MaxAreaCount", "System.Int32", false, new string[0]) ||
       maximum.Module != module || maximum.MetadataToken != MaxGetterToken ||
       !ExpectedField(count, module, CountFieldToken, "m_areaCount", "System.Int32") ||
       !ExpectedField(grid, module, GridFieldToken, "m_areaGrid", "System.Int32[]")) return false;
   if (singleton == null || !singleton.IsPublic || !singleton.IsStatic || singleton.IsVirtual ||
       singleton.IsGenericMethod || singleton.ContainsGenericParameters || singleton.Name != "get_instance" ||
       singleton.ReturnType.FullName != "UnlockManager" || singleton.GetParameters().Length != 0) return false;
   Type owner = singleton.DeclaringType;
   if (owner == null || !owner.IsGenericType || owner.ContainsGenericParameters ||
       owner.Assembly.GetName().Name != "ColossalManaged" ||
       owner.GetGenericTypeDefinition().FullName != "ColossalFramework.Singleton" + (char)96 + "1") return false;
   Type[] arguments = owner.GetGenericArguments();
   return arguments.Length == 1 && arguments[0] == gate.DeclaringType && singleton.ReturnType == gate.DeclaringType;
  }

  private static bool ExpectedMethod(MethodInfo method, string owner, string name, string returns, bool isStatic, string[] parameters) {
   if (method == null || method.DeclaringType == null || method.DeclaringType.FullName != owner ||
       method.Name != name || method.ReturnType.FullName != returns || !method.IsPublic ||
       method.IsStatic != isStatic || method.IsVirtual || method.IsGenericMethod || method.ContainsGenericParameters ||
       (method.CallingConvention & CallingConventions.VarArgs) != 0) return false;
   ParameterInfo[] actual = method.GetParameters();
   if (actual.Length != parameters.Length) return false;
   for (int i = 0; i < actual.Length; i++) {
    if (actual[i].ParameterType.FullName != parameters[i]) return false;
   }
   return true;
  }

  private static bool ExpectedField(FieldInfo field, Module module, int token, string name, string type) {
   return field != null && field.Module == module && field.MetadataToken == token &&
       field.DeclaringType != null && field.DeclaringType.FullName == "GameAreaManager" &&
       field.Name == name && field.FieldType.FullName == type && field.IsPublic && !field.IsStatic;
  }

  private static bool SameMember(MemberInfo left, MemberInfo right) {
   return left != null && right != null && left.MemberType == right.MemberType && left.Module == right.Module &&
       left.MetadataToken == right.MetadataToken && left.DeclaringType == right.DeclaringType;
  }

  private static bool IsTarget(object operand, int expected) {
   return operand is int && (int)operand == expected;
  }

  private static bool AllowedBranch(InstructionSnapshot[] snapshot, int source, int target, int start, int callIndex) {
   if (target < 0 || target >= snapshot.Length || !snapshot[target].IsBranchDestination) return false;
   int end = callIndex + 5;
   if (target <= start || target > end) return true;
   return (source == start + 4 && target == callIndex - 3) ||
       (source == callIndex + 1 && target == callIndex + 4);
  }
 }
}
#endif
