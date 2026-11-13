// OpenAI-Codex_release_isolation
#if FULLMAP_MATCHER_EXPERIMENT && FULLMAP_HARMONY_ADAPTER_EXPERIMENT && FULLMAP_NATIVE_TRANSPILER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;

namespace FullMapMatcherResearch {
 internal static class NativeMilestoneCandidateChecks {
  private const string ReferenceHash = "DF557994A03C93D8BB4A14F80B1FBCC0FB29D547658442445D078D069BCDD9FE";
  private const string HarmonyHash = "C228FF02A360A48C03A9154F49282DCCDB5809599D5E606CF3840CB4887A5503";
  private static readonly string[] AllowedReflectionOnly = {
   "Assembly-CSharp", "ColossalManaged", "ICities", "UnityEngine", "mscorlib", "System", "System.Core"
  };
  private static string referenceDirectory;

  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  private static int FakeTarget(int value) { return value + 13; }

  private static void Require(bool condition, string message) {
   if (!condition) throw new InvalidOperationException(message);
   Console.WriteLine("PASS " + message);
  }

  private static string HashFile(string path) {
   using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
   using (SHA256 hash = new SHA256Managed())
    return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", String.Empty);
  }

  private static int OwnerCount(MethodBase method) {
   Patches info = Harmony.GetPatchInfo(method);
   if (info == null) return 0;
   int count = 0;
   foreach (string ignored in info.Owners) count++;
   return count;
  }

  private static bool HasOwner(MethodBase method, string owner) {
   Patches info = Harmony.GetPatchInfo(method);
   if (info == null) return false;
   foreach (string value in info.Owners)
    if (String.Equals(value, owner, StringComparison.Ordinal)) return true;
   return false;
  }

  private static bool FakeResultsUnchanged() {
   return FakeTarget(-2) == 11 && FakeTarget(0) == 13 && FakeTarget(6) == 19;
  }

  private static int CheckOwnFakeHarmony() {
   MethodInfo target = typeof(NativeMilestoneCandidateChecks).GetMethod(
     "FakeTarget", BindingFlags.NonPublic | BindingFlags.Static);
   MethodInfo transpiler = typeof(NativeMilestoneTranspilerCandidate).GetMethod(
     "DisabledTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
   string owner = "OpenAI-Codex_release_isolation.NativeMilestoneFake." + Guid.NewGuid().ToString("N");
   Console.WriteLine("FAKE_OWNER=" + owner);
   Console.WriteLine("FAKE_TARGET=" + (target == null ? "<missing>" : target.DeclaringType.FullName + "." + target.Name));
   if (target == null || transpiler == null ||
       target.DeclaringType != typeof(NativeMilestoneCandidateChecks) ||
       target.Module.Assembly != typeof(NativeMilestoneCandidateChecks).Assembly || !target.IsStatic) {
    Console.Error.WriteLine("FAKE_SETUP_FAILURE target or disabled transpiler is not the own fake.");
    return 2;
   }
   Harmony harmony = new Harmony(owner);
   bool attempted = false, confirmed = false, failed = false, unknown = false;
   try {
    int before = OwnerCount(target);
    Console.WriteLine("FAKE_OWNER_COUNT_BEFORE=" + before);
    Console.WriteLine("FAKE_OWNER_PRESENT_BEFORE=" + HasOwner(target, owner));
    if (before != 0 || HasOwner(target, owner) || !FakeResultsUnchanged())
     throw new InvalidOperationException("Fake target has an unexpected prior owner or baseline.");
    attempted = true;
    MethodInfo replacement = harmony.Patch(target, null, null, new HarmonyMethod(transpiler), null);
    bool during = HasOwner(target, owner);
    Console.WriteLine("FAKE_OWNER_PRESENT_DURING=" + during);
    if (replacement == null || !during) {
     unknown = true;
     throw new InvalidOperationException("Fake patch result or owner is unconfirmed.");
    }
    confirmed = true;
    Require(FakeResultsUnchanged(), "disabled transpiler preserves own fake results while patched");
   } catch (Exception exception) {
    failed = true;
    if (attempted && !confirmed) unknown = true;
    Console.Error.WriteLine((unknown ? "FAKE_PATCH_UNKNOWN " : "FAKE_CHECK_FAILURE ") + exception);
   } finally {
    if (confirmed) {
     try {
      Console.WriteLine("FAKE_UNPATCH_ATTEMPT=1");
      harmony.Unpatch(target, HarmonyPatchType.Transpiler, owner);
      bool after = HasOwner(target, owner);
      int remaining = OwnerCount(target);
      Console.WriteLine("FAKE_OWNER_PRESENT_AFTER=" + after);
      Console.WriteLine("FAKE_OWNER_COUNT_AFTER=" + remaining);
      if (after || remaining != 0 || !FakeResultsUnchanged())
       throw new InvalidOperationException("Fake owner removal or baseline is unconfirmed.");
      Console.WriteLine("FAKE_UNPATCH_CONFIRMED owner-absent and baseline-restored");
     } catch (Exception exception) {
      unknown = true;
      Console.Error.WriteLine("FAKE_UNPATCH_UNKNOWN " + exception);
     }
    }
   }
   if (unknown) {
    Console.Error.WriteLine("RESULT_UNKNOWN no-retry no-reference-projection");
    return 2;
   }
   if (failed) return 1;
   Console.WriteLine("PASS own fake Harmony patch and targeted owner removal");
   return 0;
  }

  private sealed class NeverEnumerate : IEnumerable<CodeInstruction> {
   internal int EnumeratorRequests;
   public IEnumerator<CodeInstruction> GetEnumerator() {
    EnumeratorRequests++;
    throw new InvalidOperationException("DisabledTranspiler enumerated its input.");
   }
   IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
  }

  private sealed class PartialSequence : IEnumerable<CodeInstruction> {
   private readonly CodeInstruction first;
   internal int EnumeratorRequests;
   internal PartialSequence(CodeInstruction first) { this.first = first; }
   public IEnumerator<CodeInstruction> GetEnumerator() {
    EnumeratorRequests++;
    yield return first;
    throw new InvalidOperationException("Intentional partial enumeration failure.");
   }
   IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
  }

  private sealed class Row {
   internal CodeInstruction Instruction;
   internal OpCode Opcode;
   internal object Operand;
   internal object LabelList;
   internal object BlockList;
   internal Label[] Labels;
   internal ExceptionBlock[] Blocks;
  }

  private static Row[] Capture(IList<CodeInstruction> input) {
   Row[] rows = new Row[input.Count];
   for (int i = 0; i < input.Count; i++) {
    CodeInstruction item = input[i];
    if (item == null || item.labels == null || item.blocks == null)
     throw new InvalidOperationException("Fixture instruction or metadata list is missing.");
    Row row = new Row();
    row.Instruction = item;
    row.Opcode = item.opcode;
    row.Operand = item.operand;
    row.LabelList = item.labels;
    row.BlockList = item.blocks;
    row.Labels = item.labels.ToArray();
    row.Blocks = item.blocks.ToArray();
    rows[i] = row;
   }
   return rows;
  }

  private static bool SameValues(CodeInstruction item, Row row) {
   if (item == null || item.labels == null || item.blocks == null ||
       item.opcode != row.Opcode || !Object.Equals(item.operand, row.Operand) ||
       item.labels.Count != row.Labels.Length || item.blocks.Count != row.Blocks.Length) return false;
   for (int i = 0; i < row.Labels.Length; i++)
    if (!item.labels[i].Equals(row.Labels[i])) return false;
   for (int i = 0; i < row.Blocks.Length; i++)
    if (!Object.Equals(item.blocks[i], row.Blocks[i])) return false;
   return true;
  }

  private static bool InputUnchanged(IList<CodeInstruction> input, Row[] rows) {
   if (input == null || input.Count != rows.Length) return false;
   for (int i = 0; i < rows.Length; i++) {
    CodeInstruction item = input[i];
    if (!Object.ReferenceEquals(item, rows[i].Instruction) ||
        !Object.ReferenceEquals(item.operand, rows[i].Operand) ||
        !Object.ReferenceEquals(item.labels, rows[i].LabelList) ||
        !Object.ReferenceEquals(item.blocks, rows[i].BlockList) ||
        !SameValues(item, rows[i])) return false;
   }
   return true;
  }

  private static List<CodeInstruction> CopyFixture(IList<CodeInstruction> input) {
   List<CodeInstruction> result = new List<CodeInstruction>();
   foreach (CodeInstruction item in input) {
    CodeInstruction copy = new CodeInstruction(item.opcode, item.operand);
    copy.labels.AddRange(item.labels);
    copy.blocks.AddRange(item.blocks);
    result.Add(copy);
   }
   return result;
  }

  private static void CheckBuiltOutput(IList<CodeInstruction> input, Row[] rows,
      IList<CodeInstruction> output) {
   Require(InputUnchanged(input, rows), "valid input instructions and metadata were not modified");
   Require(output != null && output.Count == 81 && !Object.ReferenceEquals(output, input),
     "valid reference projection contains 81 independent output slots");
   for (int i = 0; i < output.Count; i++) {
    CodeInstruction item = output[i];
    if (item == null || item.labels == null || item.blocks == null)
     throw new InvalidOperationException("Output instruction or metadata list is missing at " + i);
    for (int j = 0; j < input.Count; j++) {
     if (Object.ReferenceEquals(item, input[j]) ||
         Object.ReferenceEquals(item.labels, input[j].labels) ||
         Object.ReferenceEquals(item.blocks, input[j].blocks))
      throw new InvalidOperationException("Output aliases input instruction or metadata at " + i);
    }
    for (int j = 0; j < i; j++) {
     if (Object.ReferenceEquals(item, output[j]) ||
         Object.ReferenceEquals(item.labels, output[j].labels) ||
         Object.ReferenceEquals(item.blocks, output[j].blocks))
      throw new InvalidOperationException("Output instructions or metadata alias each other at " + i);
    }
   }
   for (int i = 0; i < rows.Length; i++) {
    int outputIndex = i <= 24 ? i : i + 4;
    if (!SameValues(output[outputIndex], rows[i]))
     throw new InvalidOperationException("Original opcode, operand, labels, or blocks changed at " + i);
   }
   MethodInfo filter = typeof(NativeMilestoneTranspilerCandidate).GetMethod(
     "PreserveGateResult", BindingFlags.NonPublic | BindingFlags.Static);
   Require(filter != null && output[24].opcode == OpCodes.Callvirt &&
     Object.Equals(output[24].operand, rows[24].Operand) &&
     output[25].opcode == OpCodes.Ldarg_0 && output[25].operand == null &&
     output[26].opcode == OpCodes.Ldarg_1 && output[26].operand == null &&
     output[27].opcode == OpCodes.Ldarg_2 && output[27].operand == null &&
     output[28].opcode == OpCodes.Call && Object.Equals(output[28].operand, filter) &&
     output[29].opcode == rows[25].Opcode && Object.Equals(output[29].operand, rows[25].Operand),
     "original gate and branch surround the four identity-filter instructions");
   for (int i = 25; i <= 28; i++)
    if (output[i].labels.Count != 0 || output[i].blocks.Count != 0)
     throw new InvalidOperationException("Inserted instruction unexpectedly owns labels or exception blocks.");
   Console.WriteLine("PASS output owns independent instruction, label, and block lists");
  }

  private static void ExpectRejected(string name, MethodInfo original,
      IEnumerable<CodeInstruction> candidateInput, IList<CodeInstruction> trackedInput,
      LocalBuilder expectedLocal) {
   Row[] before = Capture(trackedInput);
   IList<CodeInstruction> output = new List<CodeInstruction>();
   bool built;
   try {
    built = NativeMilestoneTranspilerCandidate.TryBuildIdentityProjection(
      original, candidateInput, expectedLocal, out output);
   } catch (Exception exception) {
    throw new InvalidOperationException(name + " threw instead of rejecting.", exception);
   }
   Require(!built && output == null && InputUnchanged(trackedInput, before), name);
  }

  private static Assembly ResolveReflectionOnly(object sender, ResolveEventArgs args) {
   string simple = new AssemblyName(args.Name).Name;
   foreach (string allowed in AllowedReflectionOnly) {
    if (String.Equals(simple, allowed, StringComparison.OrdinalIgnoreCase))
     return Assembly.ReflectionOnlyLoadFrom(Path.Combine(referenceDirectory, allowed + ".dll"));
   }
   throw new InvalidOperationException("Reflection-only dependency outside allowlist: " + simple);
  }

  private static MethodInfo Helper(string name) {
   MethodInfo helper = typeof(HarmonySyntheticChecks).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
   if (helper == null) throw new InvalidOperationException("Named owned test helper missing: " + name);
   return helper;
  }

  private static int CheckReferenceProjection(string referencePath) {
   referenceDirectory = Path.GetDirectoryName(referencePath);
   if (!String.Equals(Path.GetFileName(referencePath), "Assembly-CSharp.dll", StringComparison.OrdinalIgnoreCase) ||
       !String.Equals(HashFile(referencePath), ReferenceHash, StringComparison.Ordinal))
    throw new InvalidOperationException("Authorized reference identity or hash mismatch.");
   AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveReflectionOnly;
   try {
    using (FileStream referenceLock = new FileStream(referencePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
     Assembly game = Assembly.ReflectionOnlyLoadFrom(referencePath);
     if (!game.ReflectionOnly ||
         !String.Equals(Path.GetFullPath(game.Location), referencePath, StringComparison.OrdinalIgnoreCase))
      throw new InvalidOperationException("Game reference was not opened reflection-only from the expected path.");
     Type manager = game.GetType("GameAreaManager", true);
     MethodInfo finder = Helper("FindGameMethod");
     MethodInfo original = finder.Invoke(null, new object[] { manager, "CanUnlock" }) as MethodInfo;
     MethodInfo other = finder.Invoke(null, new object[] { manager, "IsUnlocked" }) as MethodInfo;
     if (original == null || other == null || !original.Module.Assembly.ReflectionOnly ||
         !other.Module.Assembly.ReflectionOnly || Object.Equals(original, other))
      throw new InvalidOperationException("Reference-only method metadata is unavailable or ambiguous.");
     IList<InstructionSnapshot> neutral = Helper("DecodeReference").Invoke(null, new object[] { original })
       as IList<InstructionSnapshot>;
     if (neutral == null || neutral.Count != 77)
      throw new InvalidOperationException("The controlled reference decoder did not produce 77 instructions.");
     DynamicMethod labelFixture = new DynamicMethod("NativeMilestoneControlledLabels", typeof(void), Type.EmptyTypes);
     ILGenerator generator = labelFixture.GetILGenerator();
     LocalBuilder local = generator.DeclareLocal(typeof(bool));
     object[] makeArgs = new object[] { neutral, generator, local, -1 };
     IList<CodeInstruction> valid = Helper("MakeHarmonyStream").Invoke(null, makeArgs) as IList<CodeInstruction>;
     int localInstructionIndex = (int)makeArgs[3];
     if (valid == null || valid.Count != 77 || local.LocalIndex != 0 ||
         localInstructionIndex < 0 || localInstructionIndex >= valid.Count ||
         valid[24].opcode != OpCodes.Callvirt || valid[25].opcode != OpCodes.Brtrue)
      throw new InvalidOperationException("The owned synthetic Harmony fixture has an unexpected reference shape.");
     Console.WriteLine("REFERENCE_ONLY=True GAME_METHOD_INVOKED=False INPUT_COUNT=77 GATE_INDEX=24");
     Row[] baseline = Capture(valid);
     IList<CodeInstruction> output;
     bool built = NativeMilestoneTranspilerCandidate.TryBuildIdentityProjection(original, valid, local, out output);
     if (!built || output == null) {
      if (!InputUnchanged(valid, baseline))
       throw new InvalidOperationException("Failed positive build changed the input.");
      Console.Error.WriteLine("FAIL valid 77-instruction reference projection: TryBuild returned false or null output.");
      Console.WriteLine("NEGATIVE_CASES_NOT_RUN positive reference projection failed first");
      return 1;
     }
     CheckBuiltOutput(valid, baseline, output);

     ExpectRejected("wrong original reference method", other, valid, valid, local);
     List<CodeInstruction> missing = CopyFixture(valid);
     missing[24].opcode = OpCodes.Nop;
     missing[24].operand = null;
     ExpectRejected("missing milestone gate", original, missing, missing, local);
     List<CodeInstruction> duplicate = CopyFixture(valid);
     if (!(duplicate[21].operand is MethodBase) || Object.Equals(duplicate[21].operand, duplicate[24].operand))
      throw new InvalidOperationException("Duplicate-gate fixture precondition failed.");
     duplicate[21].operand = duplicate[24].operand;
     ExpectRejected("duplicate milestone gate reference", original, duplicate, duplicate, local);
     List<CodeInstruction> wrongLabel = CopyFixture(valid);
     if (!(wrongLabel[18].operand is Label))
      throw new InvalidOperationException("Wrong-label fixture precondition failed.");
     wrongLabel[18].operand = generator.DefineLabel();
     ExpectRejected("undefined branch label", original, wrongLabel, wrongLabel, local);
     List<CodeInstruction> duplicateLabel = CopyFixture(valid);
     Label branchLabel = (Label)duplicateLabel[18].operand;
     duplicateLabel[0].labels.Add(branchLabel);
     ExpectRejected("duplicate label definition", original, duplicateLabel, duplicateLabel, local);
     List<CodeInstruction> marked = CopyFixture(valid);
     marked[24].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
     ExpectRejected("exception block metadata", original, marked, marked, local);
     LocalBuilder wrongLocal = generator.DeclareLocal(typeof(bool));
     if (wrongLocal.LocalIndex == local.LocalIndex)
      throw new InvalidOperationException("Wrong-local fixture has the same index.");
     ExpectRejected("wrong expected local", original, valid, valid, wrongLocal);
     PartialSequence partial = new PartialSequence(valid[0]);
     ExpectRejected("partial enumerable throws without output", original, partial, valid, local);
     Require(partial.EnumeratorRequests == 1, "partial enumerable was actually enumerated");
     Console.WriteLine("PASS reference projection and conservative rejection cases");
     return 0;
    }
   } finally {
    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= ResolveReflectionOnly;
   }
  }

  private static int Main(string[] args) {
   try {
    if (args.Length != 2) throw new ArgumentException("Expected reference path and private Harmony path.");
    string referencePath = Path.GetFullPath(args[0]);
    string expectedHarmony = Path.GetFullPath(args[1]);
    Assembly loadedHarmony = typeof(Harmony).Assembly;
    string loadedPath = Path.GetFullPath(loadedHarmony.Location);
    Console.WriteLine("HARMONY_IDENTITY=" + loadedHarmony.FullName);
    Console.WriteLine("HARMONY_LOADED_PATH=" + loadedPath);
    Console.WriteLine("HARMONY_LOADED_SHA256=" + HashFile(loadedPath));
    Console.WriteLine("HOST_CLR=" + Environment.Version + " PROCESS_BITS=" + (IntPtr.Size * 8));
    if (!String.Equals(Path.GetFileName(expectedHarmony), "0Harmony.dll", StringComparison.OrdinalIgnoreCase) ||
        !String.Equals(loadedPath, expectedHarmony, StringComparison.OrdinalIgnoreCase) ||
        !String.Equals(HashFile(loadedPath), HarmonyHash, StringComparison.Ordinal) ||
        loadedHarmony.GetName().Name != "0Harmony" ||
        !loadedHarmony.GetName().Version.Equals(new Version(2, 4, 3, 0)) ||
        !Object.ReferenceEquals(typeof(CodeInstruction).Assembly, loadedHarmony))
     throw new InvalidOperationException("Private Harmony path, identity, or hash is unconfirmed.");
    Harmony.DEBUG = false;
    Console.WriteLine("NATIVE_PATCH_ATTEMPTS=0");
    int fakeResult = CheckOwnFakeHarmony();
    if (fakeResult != 0) return fakeResult;

    MethodInfo fakeTarget = typeof(NativeMilestoneCandidateChecks).GetMethod(
      "FakeTarget", BindingFlags.NonPublic | BindingFlags.Static);
    NeverEnumerate source = new NeverEnumerate();
    IEnumerable<CodeInstruction> disabled = NativeMilestoneTranspilerCandidate.DisabledTranspiler(source, fakeTarget);
    Require(Object.ReferenceEquals(source, disabled) && source.EnumeratorRequests == 0,
      "disabled transpiler returns the same input without enumeration");
    object marker = new object();
    Require(NativeMilestoneTranspilerCandidate.PreserveGateResult(false, marker, 1, 2) == false &&
      NativeMilestoneTranspilerCandidate.PreserveGateResult(true, marker, 1, 2) == true,
      "identity gate filter preserves false and true");
    Console.WriteLine("LIMIT=controlled fake and reflection-only projection; no generator provenance, capture atomicity, later patches, full game assembly, or game behavior established");
    return CheckReferenceProjection(referencePath);
   } catch (Exception exception) {
    Console.Error.WriteLine("SETUP_OR_CHECK_FAILURE " + exception);
    return 2;
   }
  }
 }
}
#endif
