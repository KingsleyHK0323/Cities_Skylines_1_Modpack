// OpenAI-Codex_build_review
#if FULLMAP_MATCHER_EXPERIMENT && FULLMAP_HARMONY_ADAPTER_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
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
 internal static class HarmonySyntheticChecks {
  private const string GameHash = "DF557994A03C93D8BB4A14F80B1FBCC0FB29D547658442445D078D069BCDD9FE";
  private const string HarmonyHash = "C228FF02A360A48C03A9154F49282DCCDB5809599D5E606CF3840CB4887A5503";
  private static readonly string[] AllowedReflectionOnly = {
   "Assembly-CSharp", "ColossalManaged", "ICities", "UnityEngine", "mscorlib", "System", "System.Core"
  };
  private static string referenceDirectory;
  private static int passed;
  private static int failed;

  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  private static int FakeTarget() { return 5; }

  private static IEnumerable<CodeInstruction> FakeTranspiler(IEnumerable<CodeInstruction> input) {
   List<CodeInstruction> result = new List<CodeInstruction>();
   int count = 0;
   foreach (CodeInstruction item in input) {
    CodeInstruction copy = new CodeInstruction(item);
    if (copy.opcode == OpCodes.Ldc_I4_5 && copy.operand == null) {
     copy.opcode = OpCodes.Ldc_I4_7;
     count++;
    }
    result.Add(copy);
   }
   if (count != 1) throw new InvalidOperationException("Fake target does not have exactly one expected constant.");
   return result;
  }

  private static void FakePostfix(ref int __result) { __result += 100; }

  private static bool HasOwner(MethodBase method, string owner) {
   Patches patches = Harmony.GetPatchInfo(method);
   if (patches == null) return false;
   foreach (string value in patches.Owners) if (String.Equals(value, owner, StringComparison.Ordinal)) return true;
   return false;
  }

  private static void Require(bool condition, string message) {
   if (!condition) throw new InvalidOperationException(message);
  }

  private static void RunFakePatch(string firstId, string secondId) {
   Require(!String.IsNullOrEmpty(firstId) && !String.IsNullOrEmpty(secondId) && firstId != secondId,
     "Fake patch owners must be distinct nonempty IDs.");
   MethodInfo target = typeof(HarmonySyntheticChecks).GetMethod("FakeTarget", BindingFlags.NonPublic | BindingFlags.Static);
   MethodInfo transpiler = typeof(HarmonySyntheticChecks).GetMethod("FakeTranspiler", BindingFlags.NonPublic | BindingFlags.Static);
   MethodInfo postfix = typeof(HarmonySyntheticChecks).GetMethod("FakePostfix", BindingFlags.NonPublic | BindingFlags.Static);
   Harmony first = new Harmony(firstId);
   Harmony second = new Harmony(secondId);
   bool firstActive = false, secondActive = false;
   bool firstRemoveAttempted = false, secondRemoveAttempted = false;
   Console.WriteLine("FAKE_OWNER_FIRST=" + firstId);
   Console.WriteLine("FAKE_OWNER_SECOND=" + secondId);
   try {
    Require(target != null && transpiler != null && postfix != null, "Fake methods not found.");
    Require(!HasOwner(target, firstId) && !HasOwner(target, secondId), "Fake owner was already registered.");
    Require(FakeTarget() == 5, "Fake baseline mismatch.");
    Console.WriteLine("FAKE_BASELINE=5");
    Require(first.Patch(target, null, null, new HarmonyMethod(transpiler), null) != null,
      "First fake patch returned no replacement.");
    firstActive = true;
    Require(HasOwner(target, firstId) && FakeTarget() == 7, "First fake patch did not take effect.");
    Console.WriteLine("FAKE_AFTER_FIRST=7");
    Require(second.Patch(target, null, new HarmonyMethod(postfix), null, null) != null,
      "Second fake patch returned no replacement.");
    secondActive = true;
    Require(HasOwner(target, firstId) && HasOwner(target, secondId) && FakeTarget() == 107,
      "Second fake patch did not coexist.");
    Console.WriteLine("FAKE_AFTER_SECOND=107");
    firstRemoveAttempted = true;
    first.Unpatch(target, transpiler);
    firstActive = false;
    Require(!HasOwner(target, firstId) && HasOwner(target, secondId) && FakeTarget() == 105,
      "Removing the first fake patch changed the second owner or result.");
    Console.WriteLine("FAKE_AFTER_FIRST_REMOVED=105 SECOND_OWNER_PRESENT=True");
    secondRemoveAttempted = true;
    second.Unpatch(target, postfix);
    secondActive = false;
    Require(!HasOwner(target, firstId) && !HasOwner(target, secondId) && FakeTarget() == 5,
      "Fake patches were not fully removed or baseline was not restored.");
    Console.WriteLine("FAKE_AFTER_SECOND_REMOVED=5 BOTH_OWNERS_ABSENT=True");
    Console.WriteLine("PASS real Harmony fake patch, independent owner preservation, and targeted unpatch");
   } finally {
    if (firstActive && !firstRemoveAttempted) {
     firstRemoveAttempted = true;
     try {
      first.Unpatch(target, transpiler);
      Console.WriteLine("FAKE_FINALLY_FIRST_OWNER_PRESENT=" + HasOwner(target, firstId));
     } catch (Exception exception) {
      Console.Error.WriteLine("FAKE_FINALLY_FIRST_UNKNOWN=" + exception.GetType().FullName + ": " + exception.Message);
     }
    }
    if (secondActive && !secondRemoveAttempted) {
     secondRemoveAttempted = true;
     try {
      second.Unpatch(target, postfix);
      Console.WriteLine("FAKE_FINALLY_SECOND_OWNER_PRESENT=" + HasOwner(target, secondId));
     } catch (Exception exception) {
      Console.Error.WriteLine("FAKE_FINALLY_SECOND_UNKNOWN=" + exception.GetType().FullName + ": " + exception.Message);
     }
    }
   }
  }

  private static string HashFile(string path) {
   using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
   using (SHA256 hash = new SHA256Managed())
    return BitConverter.ToString(hash.ComputeHash(input)).Replace("-", String.Empty);
  }

  private static Assembly ResolveReflectionOnly(object sender, ResolveEventArgs args) {
   string simple = new AssemblyName(args.Name).Name;
   foreach (string allowed in AllowedReflectionOnly) {
    if (String.Equals(simple, allowed, StringComparison.OrdinalIgnoreCase))
     return Assembly.ReflectionOnlyLoadFrom(Path.Combine(referenceDirectory, allowed + ".dll"));
   }
   throw new InvalidOperationException("Reflection-only dependency outside allowlist: " + simple);
  }

  private static MethodInfo FindGameMethod(Type owner, string name) {
   MethodInfo match = null;
   foreach (MethodInfo method in owner.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)) {
    if (method.Name != name || method.IsStatic || method.ReturnType.FullName != "System.Boolean") continue;
    ParameterInfo[] args = method.GetParameters();
    if (args.Length != 2 || args[0].ParameterType.FullName != "System.Int32" ||
        args[1].ParameterType.FullName != "System.Int32") continue;
    if (match != null) throw new InvalidOperationException("Ambiguous reference method: " + name);
    match = method;
   }
   if (match == null) throw new InvalidOperationException("Reference method not found: " + name);
   return match;
  }

  private static List<InstructionSnapshot> DecodeReference(MethodInfo method) {
   MethodInfo decoder = typeof(global::MilestoneCallsiteMatcherChecks).GetMethod(
     "Decode", BindingFlags.NonPublic | BindingFlags.Static);
   if (decoder == null) throw new InvalidOperationException("Named reference decoder not found.");
   object result = decoder.Invoke(null, new object[] { method });
   List<InstructionSnapshot> snapshot = result as List<InstructionSnapshot>;
   if (snapshot == null || snapshot.Count != 77) throw new InvalidOperationException("Reference decoder failed.");
   return snapshot;
  }

  private static List<CodeInstruction> MakeHarmonyStream(
      IList<InstructionSnapshot> reference, ILGenerator generator, LocalBuilder local, out int localIndex) {
   Dictionary<int, Label> labels = new Dictionary<int, Label>();
   foreach (InstructionSnapshot instruction in reference) {
    OperandType kind = instruction.Code.OperandType;
    if (kind == OperandType.InlineBrTarget || kind == OperandType.ShortInlineBrTarget) {
     if (!(instruction.Operand is int)) throw new InvalidOperationException("Reference branch is not an index.");
     int destination = (int)instruction.Operand;
     if (destination < 0 || destination >= reference.Count) throw new InvalidOperationException("Reference branch is out of range.");
     if (!labels.ContainsKey(destination)) labels.Add(destination, generator.DefineLabel());
    }
   }
   List<CodeInstruction> result = new List<CodeInstruction>();
   localIndex = -1;
   for (int i = 0; i < reference.Count; i++) {
    InstructionSnapshot item = reference[i];
    object operand = item.Operand;
    OperandType kind = item.Code.OperandType;
    if (kind == OperandType.InlineBrTarget || kind == OperandType.ShortInlineBrTarget)
     operand = labels[(int)operand];
    if (item.Code == OpCodes.Ldloca_S) {
     if (!(operand is int) || (int)operand != 0 || localIndex >= 0)
      throw new InvalidOperationException("Unexpected reference local shape.");
     operand = local;
     localIndex = i;
    }
    CodeInstruction code = new CodeInstruction(item.Code, operand);
    Label label;
    if (labels.TryGetValue(i, out label)) code.labels.Add(label);
    if (item.IsBranchDestination != labels.ContainsKey(i) || item.HasExceptionMarker)
     throw new InvalidOperationException("Reference branch destination or exception marker mismatch.");
    result.Add(code);
   }
   if (localIndex < 0 || result.Count != 77) throw new InvalidOperationException("Reference projection shape not found.");
   return result;
  }

  private static List<CodeInstruction> Clone(IList<CodeInstruction> source) {
   List<CodeInstruction> result = new List<CodeInstruction>();
   foreach (CodeInstruction instruction in source) result.Add(new CodeInstruction(instruction));
   return result;
  }

  private static void Expect(string name, bool condition) {
   if (condition) { passed++; Console.WriteLine("PASS " + name); }
   else { failed++; Console.WriteLine("FAIL " + name); }
  }

  private static void ExpectRejected(string name, MethodInfo original,
      IList<CodeInstruction> source, LocalBuilder expectedLocal) {
   try {
    Expect(name, HarmonyMatcherAdapter.CaptureAndFind(original, source, expectedLocal) == null);
   } catch (Exception exception) {
    failed++;
    Console.WriteLine("FAIL " + name + ": unexpected " + exception.GetType().FullName + ": " + exception.Message);
   }
  }

  private sealed class ThrowingList : IList<CodeInstruction> {
   private readonly IList<CodeInstruction> source;
   internal ThrowingList(IList<CodeInstruction> source) { this.source = source; }
   public int Count { get { return source.Count; } }
   public bool IsReadOnly { get { return true; } }
   public CodeInstruction this[int index] {
    get {
     if (index == 7) throw new InvalidOperationException("intentional partial-read failure");
     return source[index];
    }
    set { throw new NotSupportedException(); }
   }
   public IEnumerator<CodeInstruction> GetEnumerator() {
    for (int i = 0; i < source.Count; i++) yield return this[i];
   }
   IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
   public void CopyTo(CodeInstruction[] array, int arrayIndex) {
    for (int i = 0; i < source.Count; i++) array[arrayIndex + i] = this[i];
   }
   public int IndexOf(CodeInstruction item) { return source.IndexOf(item); }
   public bool Contains(CodeInstruction item) { return source.Contains(item); }
   public void Add(CodeInstruction item) { throw new NotSupportedException(); }
   public void Clear() { throw new NotSupportedException(); }
   public void Insert(int index, CodeInstruction item) { throw new NotSupportedException(); }
   public bool Remove(CodeInstruction item) { throw new NotSupportedException(); }
   public void RemoveAt(int index) { throw new NotSupportedException(); }
  }

  private static int Main(string[] args) {
   try {
    if (args.Length != 4) throw new ArgumentException("Expected reference path, Harmony path, and two fake owner IDs.");
    string reference = Path.GetFullPath(args[0]);
    string expectedHarmony = Path.GetFullPath(args[1]);
    if (!String.Equals(Path.GetFileName(reference), "Assembly-CSharp.dll", StringComparison.OrdinalIgnoreCase))
     throw new ArgumentException("Unexpected reference file name.");
    if (!String.Equals(Path.GetFileName(expectedHarmony), "0Harmony.dll", StringComparison.OrdinalIgnoreCase))
     throw new ArgumentException("Unexpected Harmony file name.");
    Assembly loadedHarmony = typeof(Harmony).Assembly;
    string loadedPath = Path.GetFullPath(loadedHarmony.Location);
    Console.WriteLine("HARMONY_ASSEMBLY_IDENTITY=" + loadedHarmony.FullName);
    Console.WriteLine("HARMONY_ASSEMBLY_LOCATION=" + loadedPath);
    Console.WriteLine("HARMONY_LOADED_SHA256=" + HashFile(loadedPath));
    Console.WriteLine("CLR_VERSION=" + Environment.Version);
    Console.WriteLine("PROCESS_BITS=" + (IntPtr.Size * 8));
    Require(String.Equals(loadedPath, expectedHarmony, StringComparison.OrdinalIgnoreCase), "Unexpected loaded Harmony path.");
    Require(String.Equals(HashFile(loadedPath), HarmonyHash, StringComparison.Ordinal), "Loaded Harmony hash mismatch.");
    Require(String.Equals(HashFile(reference), GameHash, StringComparison.Ordinal), "Reference hash mismatch.");
    Harmony.DEBUG = false;

    RunFakePatch(args[2], args[3]);

    AssemblyName identity = loadedHarmony.GetName();
    Expect("actual Harmony 2.4.3.0 identity accepted", HarmonyMatcherAdapter.IsSupportedHarmony(identity));
    AssemblyName wrongVersion = new AssemblyName();
    wrongVersion.Name = "0Harmony";
    wrongVersion.Version = new Version(2, 4, 2, 0);
    Expect("other Harmony version rejected", !HarmonyMatcherAdapter.IsSupportedHarmony(wrongVersion));
    AssemblyName wrongName = new AssemblyName();
    wrongName.Name = "DifferentHarmony";
    wrongName.Version = new Version(2, 4, 3, 0);
    Expect("other Harmony name rejected", !HarmonyMatcherAdapter.IsSupportedHarmony(wrongName));

    referenceDirectory = Path.GetDirectoryName(reference);
    AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveReflectionOnly;
    using (FileStream gameLock = new FileStream(reference, FileMode.Open, FileAccess.Read, FileShare.Read)) {
     Assembly game = Assembly.ReflectionOnlyLoadFrom(reference);
     Require(game.ReflectionOnly, "Reference assembly was not loaded reflection-only.");
     Type manager = game.GetType("GameAreaManager", true);
     MethodInfo original = FindGameMethod(manager, "CanUnlock");
     MethodInfo other = FindGameMethod(manager, "IsUnlocked");
     List<InstructionSnapshot> neutral = DecodeReference(original);
     Require(original.Module.Assembly.ReflectionOnly && neutral.Count == 77, "Reference metadata is not reflection-only.");
     DynamicMethod fixture = new DynamicMethod("SyntheticLabels", typeof(void), Type.EmptyTypes);
     ILGenerator generator = fixture.GetILGenerator();
     LocalBuilder booleanLocal = generator.DeclareLocal(typeof(bool));
     Require(booleanLocal.LocalIndex == 0, "Unexpected controlled local index.");
     int localIndex;
     List<CodeInstruction> valid = MakeHarmonyStream(neutral, generator, booleanLocal, out localIndex);
     Require(valid[24].opcode == OpCodes.Callvirt && valid[18].opcode == OpCodes.Blt &&
       valid[25].opcode == OpCodes.Brtrue, "Unexpected synthetic reference shape.");
     Console.WriteLine("REFERENCE_ONLY=True INSTRUCTIONS=77 CALL_INDEX=24 LOCAL_INDEX=" + localIndex);

     List<CodeInstruction> cloneProbe = Clone(valid);
     bool independentLists = cloneProbe.Count == valid.Count;
     for (int i = 0; i < valid.Count; i++) {
      independentLists = independentLists &&
        !Object.ReferenceEquals(cloneProbe[i].labels, valid[i].labels) &&
        !Object.ReferenceEquals(cloneProbe[i].blocks, valid[i].blocks);
     }
     if (independentLists) {
      int sourceLabels = valid[0].labels.Count;
      int sourceBlocks = valid[0].blocks.Count;
      cloneProbe[0].labels.Add(generator.DefineLabel());
      cloneProbe[0].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
      independentLists = valid[0].labels.Count == sourceLabels && valid[0].blocks.Count == sourceBlocks;
     }
     Expect("clone label and block lists are independent", independentLists);
     Require(independentLists, "Unsafe shared test fixture lists would contaminate later checks.");
     try {
      List<CodeInstruction> mutable = Clone(valid);
      CodeInstruction oldCall = mutable[24];
      object oldTarget = oldCall.operand;
      MatchedSnapshot match = HarmonyMatcherAdapter.CaptureAndFind(original, mutable, booleanLocal);
      bool bound = match != null;
      if (bound) {
       mutable[24] = new CodeInstruction(OpCodes.Nop, null);
       oldCall.opcode = OpCodes.Nop;
       oldCall.operand = null;
       InstructionSnapshot saved = match.GetInstruction(24);
       bound = match.CallIndex == 24 && match.Count == 77 && saved.Code == OpCodes.Callvirt &&
         Object.Equals(saved.Operand, oldTarget);
      }
      Expect("synthetic Harmony projection and captured snapshot", bound);
     } catch (Exception exception) {
      failed++;
      Console.WriteLine("FAIL synthetic Harmony projection and captured snapshot: " +
        exception.GetType().FullName + ": " + exception.Message);
     }

     ExpectRejected("wrong reference method", other, Clone(valid), booleanLocal);
     List<CodeInstruction> missing = Clone(valid);
     missing[24] = new CodeInstruction(OpCodes.Nop, null);
     ExpectRejected("missing target call", original, missing, booleanLocal);
     List<CodeInstruction> duplicate = Clone(valid);
     duplicate[21].operand = duplicate[24].operand;
     ExpectRejected("duplicate target reference", original, duplicate, booleanLocal);

     List<CodeInstruction> missingLabel = Clone(valid);
     missingLabel[18].operand = generator.DefineLabel();
     ExpectRejected("branch label has no definition", original, missingLabel, booleanLocal);
     List<CodeInstruction> unusedLabel = Clone(valid);
     unusedLabel[21].labels.Add(generator.DefineLabel());
     ExpectRejected("unreferenced label at existing destination", original, unusedLabel, booleanLocal);

     Dictionary<int, int> firstBranchByDestination = new Dictionary<int, int>();
     int secondBranch = -1;
     int sharedDestination = -1;
     for (int i = 0; i < neutral.Count; i++) {
      OperandType kind = neutral[i].Code.OperandType;
      if (kind != OperandType.InlineBrTarget && kind != OperandType.ShortInlineBrTarget) continue;
      int destination = (int)neutral[i].Operand;
      if (firstBranchByDestination.ContainsKey(destination)) {
       secondBranch = i;
       sharedDestination = destination;
       break;
      }
      firstBranchByDestination.Add(destination, i);
     }
     Require(secondBranch >= 0 && sharedDestination >= 0, "No repeated branch destination for label fixture.");
     List<CodeInstruction> splitLabels = Clone(valid);
     Label additionalLabel = generator.DefineLabel();
     splitLabels[secondBranch].operand = additionalLabel;
     splitLabels[sharedDestination].labels.Add(additionalLabel);
     bool splitAccepted = false;
     try {
      MatchedSnapshot split = HarmonyMatcherAdapter.CaptureAndFind(original, splitLabels, booleanLocal);
      splitAccepted = split != null && split.CallIndex == 24 && split.Count == 77;
     } catch (Exception exception) {
      Console.WriteLine("TWO_LABELS_EXCEPTION=" + exception.GetType().FullName + ": " + exception.Message);
     }
     Expect("two referenced labels share one destination", splitAccepted);
     List<CodeInstruction> duplicateLabel = Clone(valid);
     Label branchLabel = (Label)duplicateLabel[18].operand;
     duplicateLabel[0].labels.Add(branchLabel);
     ExpectRejected("same label defined at two instructions", original, duplicateLabel, booleanLocal);
     List<CodeInstruction> wrongDestination = Clone(valid);
     Label movedLabel = (Label)wrongDestination[18].operand;
     Require(wrongDestination[21].labels.Remove(movedLabel), "Reference destination label missing.");
     wrongDestination[0].labels.Add(movedLabel);
     ExpectRejected("label maps to wrong destination", original, wrongDestination, booleanLocal);

     List<CodeInstruction> markedBlock = Clone(valid);
     markedBlock[24].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
     ExpectRejected("exception marker", original, markedBlock, booleanLocal);
     LocalBuilder wrongLocal = generator.DeclareLocal(typeof(bool));
     ExpectRejected("wrong expected local", original, Clone(valid), wrongLocal);
     DynamicMethod foreignFixture = new DynamicMethod("ForeignSyntheticLocal", typeof(void), Type.EmptyTypes);
     LocalBuilder foreignLocal = foreignFixture.GetILGenerator().DeclareLocal(typeof(bool));
     Require(foreignLocal.LocalIndex == booleanLocal.LocalIndex && !Object.ReferenceEquals(foreignLocal, booleanLocal),
       "Foreign local fixture does not have the intended identity.");
     List<CodeInstruction> foreignStream = Clone(valid);
     foreignStream[localIndex].operand = foreignLocal;
     ExpectRejected("foreign local with same index", original, foreignStream, booleanLocal);
     ILGenerator foreignGenerator = foreignFixture.GetILGenerator();
     for (int i = 0; i < 32; i++) foreignGenerator.DefineLabel();
     List<CodeInstruction> foreignWholeStream = Clone(valid);
     Dictionary<Label, Label> foreignLabels = new Dictionary<Label, Label>();
     foreach (CodeInstruction instruction in foreignWholeStream) {
      for (int i = 0; i < instruction.labels.Count; i++) {
       Label previous = instruction.labels[i];
       Label replacement;
       if (!foreignLabels.TryGetValue(previous, out replacement)) {
        replacement = foreignGenerator.DefineLabel();
        Require(!replacement.Equals(previous), "Foreign label fixture reused an equal label value.");
        foreignLabels.Add(previous, replacement);
       }
       instruction.labels[i] = replacement;
      }
     }
     foreach (CodeInstruction instruction in foreignWholeStream) {
      if (instruction.operand is Label) instruction.operand = foreignLabels[(Label)instruction.operand];
     }
     foreignWholeStream[localIndex].operand = foreignLocal;
     bool foreignPairAccepted = false;
     try {
      MatchedSnapshot foreignMatch = HarmonyMatcherAdapter.CaptureAndFind(original, foreignWholeStream, foreignLocal);
      foreignPairAccepted = foreignMatch != null && foreignMatch.CallIndex == 24 && foreignMatch.Count == 77;
     } catch (Exception exception) {
      Console.WriteLine("FOREIGN_GENERATOR_EXCEPTION=" + exception.GetType().FullName + ": " + exception.Message);
     }
     Expect("limitation: caller-supplied foreign local and labels accepted together", foreignPairAccepted);
     DynamicMethod integerFixture = new DynamicMethod("WrongSyntheticLocalType", typeof(void), Type.EmptyTypes);
     LocalBuilder integerLocal = integerFixture.GetILGenerator().DeclareLocal(typeof(int));
     Require(integerLocal.LocalIndex == 0 && integerLocal.LocalType == typeof(int),
       "Wrong-type local fixture does not have the intended index and type.");
     List<CodeInstruction> integerStream = Clone(valid);
     integerStream[localIndex].operand = integerLocal;
     ExpectRejected("index-zero Int32 local rejected", original, integerStream, integerLocal);
     List<CodeInstruction> changedOther = Clone(valid);
     changedOther[0] = new CodeInstruction(OpCodes.Ldc_I4_0, null);
     ExpectRejected("non-target instruction changed", original, changedOther, booleanLocal);

     bool safelyFailed = false;
     bool exceptionEscaped = false;
     try { safelyFailed = HarmonyMatcherAdapter.CaptureAndFind(original, new ThrowingList(valid), booleanLocal) == null; }
     catch (InvalidOperationException) { safelyFailed = true; exceptionEscaped = true; }
     Expect("partial input failure returns no match", safelyFailed && !exceptionEscaped);
    }
    Console.WriteLine("ADAPTER_RESULTS=" + passed + " passed; " + failed + " failed.");
    return failed == 0 ? 0 : 1;
   } catch (Exception exception) {
    Console.Error.WriteLine("SETUP_OR_FAKE_ERROR=" + exception.GetType().FullName + ": " + exception.Message);
    return 2;
   }
  }
 }
}
#endif
