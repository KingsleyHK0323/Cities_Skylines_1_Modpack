// OpenAI-Codex_build_review
#if FULLMAP_MATCHER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using FullMapMatcherResearch;

internal static class MilestoneCallsiteMatcherChecks {
 private const string ReferenceHash = "DF557994A03C93D8BB4A14F80B1FBCC0FB29D547658442445D078D069BCDD9FE";
 private const string BodyHash = "728306E5DCCDE1012A529CB332094E8F87296389D8675F68B10EE01E27D82BF6";
 private static readonly string[] AllowedAssemblies = {
  "Assembly-CSharp", "ColossalManaged", "ICities", "UnityEngine", "mscorlib", "System", "System.Core"
 };
 private static readonly Dictionary<ushort, OpCode> Opcodes = CreateOpcodes();
 private static string referenceDirectory;
 private static int passed;
 private static int failed;

 private sealed class RawInstruction {
  internal int Offset;
  internal OpCode Code;
  internal object Operand;
 }

 private static Dictionary<ushort, OpCode> CreateOpcodes() {
  Dictionary<ushort, OpCode> result = new Dictionary<ushort, OpCode>();
  foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)) {
   OpCode op = (OpCode)field.GetValue(null);
   result.Add(unchecked((ushort)op.Value), op);
  }
  return result;
 }

 private static Assembly ResolveReflectionOnly(object sender, ResolveEventArgs args) {
  string simpleName = new AssemblyName(args.Name).Name;
  foreach (string allowed in AllowedAssemblies) {
   if (String.Equals(simpleName, allowed, StringComparison.OrdinalIgnoreCase))
    return Assembly.ReflectionOnlyLoadFrom(Path.Combine(referenceDirectory, allowed + ".dll"));
  }
  throw new InvalidOperationException("Reflection-only dependency is outside the seven-name allowlist: " + simpleName);
 }

 private static string Hex(byte[] bytes) {
  return BitConverter.ToString(bytes).Replace("-", String.Empty);
 }

 private static MethodInfo FindGameMethod(Type type, string name) {
  MethodInfo match = null;
  foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
   if (method.Name != name || method.IsStatic || method.ReturnType.FullName != "System.Boolean")
    continue;
   ParameterInfo[] parameters = method.GetParameters();
   if (parameters.Length != 2 || parameters[0].ParameterType.FullName != "System.Int32" ||
       parameters[1].ParameterType.FullName != "System.Int32")
    continue;
   if (match != null) throw new InvalidOperationException("Method lookup is ambiguous: " + name);
   match = method;
  }
  if (match == null) throw new InvalidOperationException("Method not found: " + name);
  return match;
 }

 private static int ReadInt32(byte[] bytes, ref int position) {
  int value = BitConverter.ToInt32(bytes, position);
  position += 4;
  return value;
 }

 private static List<InstructionSnapshot> Decode(MethodInfo method) {
  MethodBody body = method.GetMethodBody();
  if (body == null || body.ExceptionHandlingClauses.Count != 0)
   throw new InvalidOperationException("Reference method body is unavailable or has exception regions.");
  byte[] bytes = body.GetILAsByteArray();
  if (bytes == null || bytes.Length != 167)
   throw new InvalidOperationException("Reference method body length differs from the reviewed copy.");
  using (SHA256 hash = new SHA256Managed()) {
   if (!String.Equals(Hex(hash.ComputeHash(bytes)), BodyHash, StringComparison.Ordinal))
    throw new InvalidOperationException("Reference method body hash differs from the reviewed copy.");
  }

  List<RawInstruction> raw = new List<RawInstruction>();
  Dictionary<int, int> offsetToIndex = new Dictionary<int, int>();
  int p = 0;
  while (p < bytes.Length) {
   int offset = p;
   ushort value = bytes[p++];
   if (value == 0xFE) {
    if (p >= bytes.Length) throw new InvalidOperationException("Truncated two-byte opcode.");
    value = (ushort)(0xFE00 | bytes[p++]);
   }
   OpCode op;
   if (!Opcodes.TryGetValue(value, out op)) throw new InvalidOperationException("Unknown opcode.");
   object operand = null;
   switch (op.OperandType) {
    case OperandType.InlineNone: break;
    case OperandType.ShortInlineI: operand = unchecked((sbyte)bytes[p++]); break;
    case OperandType.ShortInlineVar: operand = (int)bytes[p++]; break;
    case OperandType.InlineVar: operand = (int)BitConverter.ToUInt16(bytes, p); p += 2; break;
    case OperandType.InlineI: operand = ReadInt32(bytes, ref p); break;
    case OperandType.InlineI8: operand = BitConverter.ToInt64(bytes, p); p += 8; break;
    case OperandType.ShortInlineR: operand = BitConverter.ToSingle(bytes, p); p += 4; break;
    case OperandType.InlineR: operand = BitConverter.ToDouble(bytes, p); p += 8; break;
    case OperandType.ShortInlineBrTarget: {
     int delta = unchecked((sbyte)bytes[p++]);
     operand = p + delta;
     break;
    }
    case OperandType.InlineBrTarget: {
     int delta = ReadInt32(bytes, ref p);
     operand = p + delta;
     break;
    }
    case OperandType.InlineField:
     operand = method.Module.ResolveField(ReadInt32(bytes, ref p)); break;
    case OperandType.InlineMethod:
     operand = method.Module.ResolveMethod(ReadInt32(bytes, ref p)); break;
    case OperandType.InlineType:
     operand = method.Module.ResolveType(ReadInt32(bytes, ref p)); break;
    case OperandType.InlineTok:
     operand = method.Module.ResolveMember(ReadInt32(bytes, ref p)); break;
    case OperandType.InlineString:
     operand = method.Module.ResolveString(ReadInt32(bytes, ref p)); break;
    default:
     throw new InvalidOperationException("Unsupported operand type in the reference method: " + op.OperandType);
   }
   offsetToIndex.Add(offset, raw.Count);
   RawInstruction instruction = new RawInstruction();
   instruction.Offset = offset;
   instruction.Code = op;
   instruction.Operand = operand;
   raw.Add(instruction);
  }
  if (raw.Count != 77 || raw[24].Offset != 0x3C || raw[24].Code != OpCodes.Callvirt)
   throw new InvalidOperationException("Reference instruction shape differs from the reviewed copy.");
  MethodInfo target = raw[24].Operand as MethodInfo;
  if (target == null || target.Name != "Unlocked" || target.DeclaringType.FullName != "UnlockManager" ||
      target.IsStatic || target.ReturnType.FullName != "System.Boolean" || target.GetParameters().Length != 1 ||
      target.GetParameters()[0].ParameterType.FullName != "System.Int32")
   throw new InvalidOperationException("Reference milestone call does not have the expected signature.");

  HashSet<int> destinations = new HashSet<int>();
  foreach (RawInstruction item in raw) {
   if (item.Code.OperandType == OperandType.InlineBrTarget ||
       item.Code.OperandType == OperandType.ShortInlineBrTarget) {
    int targetIndex;
    if (!offsetToIndex.TryGetValue((int)item.Operand, out targetIndex))
     throw new InvalidOperationException("Branch target is not an instruction boundary.");
    item.Operand = targetIndex;
    destinations.Add(targetIndex);
   }
  }
  List<InstructionSnapshot> snapshots = new List<InstructionSnapshot>();
  for (int i = 0; i < raw.Count; i++)
   snapshots.Add(new InstructionSnapshot(raw[i].Code, raw[i].Operand, destinations.Contains(i), false));
  return snapshots;
 }

 private static List<InstructionSnapshot> Replace(
     IList<InstructionSnapshot> source, int index, OpCode code, object operand, bool exceptionMarker) {
  List<InstructionSnapshot> result = new List<InstructionSnapshot>(source);
  InstructionSnapshot old = source[index];
  result[index] = new InstructionSnapshot(code, operand, old.IsBranchDestination, exceptionMarker);
  return result;
 }

 private static List<InstructionSnapshot> WithBranchDestinations(IList<InstructionSnapshot> source) {
  HashSet<int> targets = new HashSet<int>();
  foreach (InstructionSnapshot instruction in source) {
   if (instruction.Code.OperandType == OperandType.InlineBrTarget ||
       instruction.Code.OperandType == OperandType.ShortInlineBrTarget) {
    if (!(instruction.Operand is int)) throw new InvalidOperationException("Invalid fixture branch operand.");
    targets.Add((int)instruction.Operand);
   }
  }
  List<InstructionSnapshot> result = new List<InstructionSnapshot>();
  for (int i = 0; i < source.Count; i++) {
   InstructionSnapshot item = source[i];
   result.Add(new InstructionSnapshot(item.Code, item.Operand, targets.Contains(i), item.HasExceptionMarker));
  }
  return result;
 }

 private static List<InstructionSnapshot> InsertPrefix(IList<InstructionSnapshot> source, int index) {
  List<InstructionSnapshot> result = new List<InstructionSnapshot>();
  for (int i = 0; i < source.Count; i++) {
   if (i == index) result.Add(new InstructionSnapshot(OpCodes.Tailcall, null, false, false));
   InstructionSnapshot item = source[i];
   object operand = item.Operand;
   if (item.Code.OperandType == OperandType.InlineBrTarget ||
       item.Code.OperandType == OperandType.ShortInlineBrTarget) {
    int target = (int)operand;
    operand = target >= index ? target + 1 : target;
   }
   result.Add(new InstructionSnapshot(item.Code, operand, false, item.HasExceptionMarker));
  }
  return WithBranchDestinations(result);
 }

 private static void Expect(string name, MethodInfo original, IList<InstructionSnapshot> instructions, int expected) {
  try {
   int actual = MilestoneCallsiteMatcher.Find(original, instructions);
   if (actual == expected) {
    passed++;
    Console.WriteLine("PASS " + name);
   } else {
    failed++;
    Console.WriteLine("FAIL " + name + ": expected " + expected + ", actual " + actual);
   }
  } catch (Exception exception) {
   failed++;
   Console.WriteLine("FAIL " + name + ": unexpected " + exception.GetType().FullName + ": " + exception.Message);
  }
 }

 private static int Main(string[] args) {
  try {
   if (args.Length != 1) throw new ArgumentException("Supply the authorized Assembly-CSharp.dll path.");
   string referencePath = Path.GetFullPath(args[0]);
   if (!String.Equals(Path.GetFileName(referencePath), "Assembly-CSharp.dll", StringComparison.OrdinalIgnoreCase))
    throw new ArgumentException("The reference file name must be Assembly-CSharp.dll.");
   referenceDirectory = Path.GetDirectoryName(referencePath);
   using (FileStream reference = new FileStream(referencePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
    using (SHA256 hash = new SHA256Managed()) {
     if (!String.Equals(Hex(hash.ComputeHash(reference)), ReferenceHash, StringComparison.Ordinal))
      throw new InvalidOperationException("Assembly-CSharp.dll hash differs from the authorized reference.");
    }
    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveReflectionOnly;
    Assembly game = Assembly.ReflectionOnlyLoadFrom(referencePath);
    if (!game.ReflectionOnly) throw new InvalidOperationException("Game assembly was not loaded reflection-only.");
    Type manager = game.GetType("GameAreaManager", true);
    MethodInfo original = FindGameMethod(manager, "CanUnlock");
    MethodInfo another = FindGameMethod(manager, "IsUnlocked");
    List<InstructionSnapshot> source = Decode(original);
    const int callIndex = 24;
    const int branchIndex = 25;
    if (source[branchIndex].Code != OpCodes.Brtrue || !(source[branchIndex].Operand is int) ||
        (int)source[branchIndex].Operand != 28)
     throw new InvalidOperationException("Reference branch shape differs from the reviewed copy.");
    Console.WriteLine("Reference: SHA-256 and 167-byte method body verified; 77 instructions; expected call index 24.");

    Expect("reference call site", original, source, callIndex);
    string snapshotFailure = null;
    try {
     MethodInfo capture = typeof(MilestoneCallsiteMatcher).GetMethod(
       "CaptureAndFind", BindingFlags.Public | BindingFlags.Static, null,
       new Type[] { typeof(MethodInfo), typeof(IList<InstructionSnapshot>) }, null);
     if (capture == null) {
      snapshotFailure = "CaptureAndFind is unavailable";
     } else if (capture.ReturnType.Name != "MatchedSnapshot") {
      snapshotFailure = "CaptureAndFind does not return MatchedSnapshot";
     } else {
      List<InstructionSnapshot> mutable = new List<InstructionSnapshot>(source);
      InstructionSnapshot originalCall = mutable[callIndex];
      object bound = capture.Invoke(null, new object[] { original, mutable });
      if (bound == null) {
       snapshotFailure = "CaptureAndFind returned null";
      } else {
       mutable[callIndex] = new InstructionSnapshot(OpCodes.Nop, null, false, false);
       Type snapshotType = bound.GetType();
       PropertyInfo indexProperty = snapshotType.GetProperty("CallIndex", BindingFlags.Public | BindingFlags.Instance);
       PropertyInfo countProperty = snapshotType.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
       FieldInfo indexField = snapshotType.GetField("CallIndex", BindingFlags.Public | BindingFlags.Instance);
       FieldInfo countField = snapshotType.GetField("Count", BindingFlags.Public | BindingFlags.Instance);
       object boundIndex = indexProperty != null ? indexProperty.GetValue(bound, null) :
         (indexField != null ? indexField.GetValue(bound) : null);
       object boundCount = countProperty != null ? countProperty.GetValue(bound, null) :
         (countField != null ? countField.GetValue(bound) : null);
       MethodInfo getInstruction = snapshotType.GetMethod(
         "GetInstruction", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(int) }, null);
       InstructionSnapshot capturedCall = getInstruction == null ? null :
         getInstruction.Invoke(bound, new object[] { callIndex }) as InstructionSnapshot;
       if (!(boundIndex is int) || (int)boundIndex != 24 || !(boundCount is int) || (int)boundCount != 77 ||
           capturedCall == null || capturedCall.Code != OpCodes.Callvirt ||
           !Object.Equals(capturedCall.Operand, originalCall.Operand))
        snapshotFailure = "snapshot did not retain index 24, count 77, and the original callvirt";
      }
     }
    } catch (Exception exception) {
     snapshotFailure = exception.GetType().FullName + ": " + exception.Message;
    }
    if (snapshotFailure == null) {
     passed++;
     Console.WriteLine("PASS matched snapshot survives source-list replacement");
    } else {
     failed++;
     Console.WriteLine("FAIL matched snapshot survives source-list replacement: " + snapshotFailure);
    }
    int[] coordinateLoads = { 0, 3, 6, 9 };
    foreach (int position in coordinateLoads)
     Expect("coordinate load replaced with zero at " + position, original,
       Replace(source, position, OpCodes.Ldc_I4_0, null, false), -1);
    Expect("integer operand is a string", original,
      Replace(source, 7, OpCodes.Ldc_I4, "not-an-int", false), -1);
    List<InstructionSnapshot> bypass = Replace(source, 0, OpCodes.Br, 14, false);
    Expect("branch bypasses coordinate checks", original, WithBranchDestinations(bypass), -1);
    Expect("missing target call", original, Replace(source, callIndex, OpCodes.Nop, null, false), -1);

    List<InstructionSnapshot> duplicate = new List<InstructionSnapshot>(source);
    duplicate.Add(new InstructionSnapshot(source[callIndex].Code, source[callIndex].Operand, false, false));
    Expect("duplicate target call", original, duplicate, -1);

    Expect("wrong call opcode", original,
      Replace(source, callIndex, OpCodes.Call, source[callIndex].Operand, false), -1);
    List<InstructionSnapshot> wrongBranch = Replace(
      source, branchIndex, source[branchIndex].Code, branchIndex + 1, false);
    Expect("wrong but valid branch destination", original, WithBranchDestinations(wrongBranch), -1);
    Expect("wrong original method with matching signature", another, source, -1);
    Expect("exception marker on call", original,
      Replace(source, callIndex, source[callIndex].Code, source[callIndex].Operand, true), -1);
    Expect("unexpected instruction prefix", original, InsertPrefix(source, callIndex), -1);
    Console.WriteLine("Results: " + passed + " passed; " + failed + " failed.");
    return failed == 0 ? 0 : 1;
   }
  } catch (Exception exception) {
   Console.Error.WriteLine("SETUP ERROR: " + exception.GetType().FullName + ": " + exception.Message);
   return 2;
  }
 }
}
#endif
