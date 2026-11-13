// OpenAI-Codex_review_model
#if FULLMAP_SCOPED_FAKE_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using HarmonyLib;

namespace ScopedFakeResearch {
 internal sealed class FakeInvocation {
  internal readonly FakeScope Scope;
  internal bool Eligible = true, ReenterOnce, DirectReenterOnce, ForeignReenterOnce, NestedResult, ThrowFilter, BlockGate;
  internal FakeInvocation ForeignReentry;
  internal ManualResetEvent GateEntered, GateRelease;
  internal Exception GateException;
  internal int GateCalls;
  internal FakeInvocation(FakeScope scope) { Scope = scope; }
 }

 internal static class ScopedFakeTranspilerChecks {
  private static volatile bool workerUndrained;
  private static bool simulateUndrained;
  private static int nullGateCalls;

  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  internal static bool Gate(FakeInvocation invocation, bool nativeResult) {
   if (invocation == null) Interlocked.Increment(ref nullGateCalls);
   if (invocation != null) {
    invocation.GateCalls++;
    if (invocation.BlockGate) {
     invocation.GateEntered.Set();
     invocation.GateRelease.WaitOne();
    }
    if (invocation.GateException != null) throw invocation.GateException;
    if (invocation.ReenterOnce) {
     invocation.ReenterOnce = false;
     invocation.NestedResult = invocation.Scope.Execute(invocation, false);
    }
    if (invocation.DirectReenterOnce) {
     invocation.DirectReenterOnce = false;
     invocation.NestedResult = Target(invocation, false);
    }
    if (invocation.ForeignReenterOnce) {
     invocation.ForeignReenterOnce = false;
     invocation.NestedResult = Target(invocation.ForeignReentry, false);
    }
    if (!invocation.Eligible) return false;
   }
   return nativeResult;
  }

  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  internal static bool Target(FakeInvocation invocation, bool nativeResult) {
   return Gate(invocation, nativeResult);
  }

  private static MethodInfo Method(string name) {
   return typeof(ScopedFakeTranspilerChecks).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
  }

  private static List<CodeInstruction> ReferenceInput() {
   return new List<CodeInstruction> {
    new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1),
    new CodeInstruction(OpCodes.Call, Method("Gate")), new CodeInstruction(OpCodes.Ret)
   };
  }

  private static IEnumerable<CodeInstruction> PartialInput() {
   yield return new CodeInstruction(OpCodes.Ldarg_0);
   throw new InvalidOperationException("controlled partial producer failure");
  }

  private static FakeInvocation Attempt(bool permitted) {
   FakeScope scope = new FakeScope(permitted);
   FakeInvocation invocation = new FakeInvocation(scope);
   scope.Bind(invocation);
   return invocation;
  }

  private static void Require(bool condition, string name) {
   if (!condition) throw new InvalidOperationException("FAIL " + name);
   Console.WriteLine("PASS " + name);
  }

  private static bool HasOwner(MethodBase target, string owner) {
   Patches patches = Harmony.GetPatchInfo(target);
   if (patches == null) return false;
   foreach (string item in patches.Owners) if (String.Equals(item, owner, StringComparison.Ordinal)) return true;
   return false;
  }

  private static string FileHash(string path) {
   using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
   using (SHA256 hash = new SHA256Managed())
    return BitConverter.ToString(hash.ComputeHash(file)).Replace("-", String.Empty);
  }

  private static void CheckUnpatchedProjection() {
   List<CodeInstruction> input = new List<CodeInstruction> {
    new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1),
    new CodeInstruction(OpCodes.Call, Method("Gate")), new CodeInstruction(OpCodes.Ret)
   };
   bool matched;
   IList<CodeInstruction> output = ScopedFakeTranspiler.Rewrite(input, out matched);
   bool legacyShape = matched && output != null && output.Count == 6 &&
     output[0].opcode == OpCodes.Ldarg_0 && output[1].opcode == OpCodes.Ldarg_1 &&
     output[2].opcode == OpCodes.Call && Object.Equals(output[2].operand, Method("Gate")) &&
     output[3].opcode == OpCodes.Ldarg_0 && output[4].opcode == OpCodes.Call &&
     Object.Equals(output[4].operand, ScopedFakeTranspiler.DecisionMethod) && output[5].opcode == OpCodes.Ret;
   MethodInfo entry = typeof(ScopedFakeTranspiler).GetMethod("EnterTarget", BindingFlags.Static | BindingFlags.NonPublic);
   bool guardedShape = matched && output != null && output.Count == 8 && entry != null &&
     output[0].opcode == OpCodes.Ldarg_0 && output[1].opcode == OpCodes.Call && Object.Equals(output[1].operand, entry) &&
     output[2].opcode == OpCodes.Ldarg_0 && output[3].opcode == OpCodes.Ldarg_1 &&
     output[4].opcode == OpCodes.Call && Object.Equals(output[4].operand, Method("Gate")) &&
     output[5].opcode == OpCodes.Ldarg_0 && output[6].opcode == OpCodes.Call &&
     Object.Equals(output[6].operand, ScopedFakeTranspiler.DecisionMethod) && output[7].opcode == OpCodes.Ret;
   Console.WriteLine("EXPECTED fixed fake gate call receives exactly one post-call decision");
   Require(legacyShape || guardedShape, "fixed fake gate rewritten");
   input[2].operand = null;
   Require(Object.Equals(output[guardedShape ? 4 : 2].operand, Method("Gate")), "owned projection survives source replacement");

   List<CodeInstruction> mismatch = ReferenceInput();
   mismatch[0].opcode = OpCodes.Ldc_I4_0;
   IList<CodeInstruction> declined = ScopedFakeTranspiler.Rewrite(mismatch, out matched);
   Require(!matched && declined.Count == 4 && declined[0].opcode == OpCodes.Ldc_I4_0 &&
     mismatch[0].opcode == OpCodes.Ldc_I4_0 && !Object.ReferenceEquals(declined[0], mismatch[0]),
     "mismatched fake stream is copied without rewrite");

   List<CodeInstruction> marked = ReferenceInput();
   marked[0].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
   bool rejected = false;
   try { ScopedFakeTranspiler.Rewrite(marked, out matched); }
   catch (InvalidOperationException) { rejected = true; }
   Require(rejected && marked[0].blocks.Count == 1, "exception-marked input is rejected unchanged");

   List<CodeInstruction> labeled = ReferenceInput();
   DynamicMethod fixture = new DynamicMethod("ScopedFakeLabels", typeof(void), Type.EmptyTypes);
   labeled[0].labels.Add(fixture.GetILGenerator().DefineLabel());
   rejected = false;
   try { ScopedFakeTranspiler.Rewrite(labeled, out matched); }
   catch (InvalidOperationException) { rejected = true; }
   Require(rejected && labeled[0].labels.Count == 1, "labeled input is rejected unchanged");

   rejected = false;
   try { ScopedFakeTranspiler.Rewrite(PartialInput(), out matched); }
   catch (InvalidOperationException) { rejected = true; }
   Require(rejected, "partial producer failure produces no output");
  }

  private static void CheckScopedBehavior() {
   FakeInvocation denied = Attempt(false);
   Require(!denied.Scope.Execute(denied, false) && denied.Scope.Overrides == 0,
     "unpermitted fake invocation preserves false");

   FakeInvocation once = Attempt(true);
   Require(once.Scope.Execute(once, false) && once.Scope.State == FakeScope.Consumed &&
     once.Scope.Overrides == 1 && !once.Scope.Execute(once, false) && once.Scope.Overrides == 1,
     "one authorized false becomes true without retry");

   FakeInvocation alreadyTrue = Attempt(true);
   Require(alreadyTrue.Scope.Execute(alreadyTrue, true) && alreadyTrue.Scope.State == FakeScope.Consumed &&
     !alreadyTrue.Scope.Execute(alreadyTrue, false) && alreadyTrue.Scope.Overrides == 0,
     "original true consumes the ticket without override");

   FakeInvocation other = Attempt(true);
   FakeInvocation foreign = new FakeInvocation(other.Scope);
   Require(!other.Scope.Execute(foreign, false) && other.Scope.State == FakeScope.Armed &&
     !Target(other, false) && other.Scope.State == FakeScope.Armed && other.Scope.Execute(other, false),
     "other and unframed calls cannot consume the ticket");

   FakeInvocation condition = Attempt(true);
   condition.Eligible = false;
   bool conditionResult = condition.Scope.Execute(condition, true);
   condition.Eligible = true;
   Require(!conditionResult && condition.Scope.State == FakeScope.Stopped &&
     !condition.Scope.Execute(condition, false) && condition.Scope.Overrides == 0,
     "other fake condition stops without retry");

   FakeInvocation nested = Attempt(true);
   nested.ReenterOnce = true;
   Require(!nested.Scope.Execute(nested, false) && !nested.NestedResult &&
     nested.Scope.State == FakeScope.Stopped && nested.GateCalls == 2 && !nested.Scope.Execute(nested, false) &&
     nested.Scope.Overrides == 0, "synchronous reentry stops and cannot steal or retry");

   FakeInvocation direct = Attempt(true);
   direct.DirectReenterOnce = true;
   bool directOuter = direct.Scope.Execute(direct, false);
   Console.WriteLine("DIRECT_REENTRY outer=" + directOuter + " nested=" + direct.NestedResult +
     " state=" + direct.Scope.State + " overrides=" + direct.Scope.Overrides + " gateCalls=" + direct.GateCalls);
   Require(!directOuter && !direct.NestedResult && direct.Scope.State == FakeScope.Stopped &&
     direct.Scope.Overrides == 0 && direct.GateCalls == 2 && !direct.Scope.Execute(direct, false),
     "direct target reentry stops without stealing or retry");

   FakeInvocation cross = Attempt(true);
   FakeInvocation crossInner = Attempt(true);
   cross.ForeignReentry = crossInner;
   cross.ForeignReenterOnce = true;
   bool crossOuter = cross.Scope.Execute(cross, false);
   int crossState = cross.Scope.State, crossOverrides = cross.Scope.Overrides;
   int crossOuterCalls = cross.GateCalls, crossInnerCalls = crossInner.GateCalls;
   bool crossRetry = cross.Scope.Execute(cross, false);
   Console.WriteLine("CROSS_REENTRY outer=" + crossOuter + " nested=" + cross.NestedResult +
     " state=" + crossState + " overrides=" + crossOverrides + " retry=" + crossRetry +
     " innerState=" + crossInner.Scope.State + " outerCalls=" + crossOuterCalls + " innerCalls=" + crossInnerCalls);

   FakeInvocation nullScope = Attempt(true);
   nullScope.ForeignReentry = new FakeInvocation(null);
   nullScope.ForeignReenterOnce = true;
   bool nullOuter = nullScope.Scope.Execute(nullScope, false);
   int nullState = nullScope.Scope.State, nullOverrides = nullScope.Scope.Overrides;
   int nullOuterCalls = nullScope.GateCalls, nullInnerCalls = nullScope.ForeignReentry.GateCalls;
   bool nullRetry = nullScope.Scope.Execute(nullScope, false);
   Console.WriteLine("NULL_SCOPE_REENTRY outer=" + nullOuter + " nested=" + nullScope.NestedResult +
     " state=" + nullState + " overrides=" + nullOverrides + " retry=" + nullRetry +
     " outerCalls=" + nullOuterCalls + " innerCalls=" + nullInnerCalls);

   FakeInvocation nullArgument = Attempt(true);
   nullArgument.ForeignReentry = null;
   nullArgument.ForeignReenterOnce = true;
   int nullCallsBefore = Interlocked.CompareExchange(ref nullGateCalls, 0, 0);
   bool nullArgOuter = nullArgument.Scope.Execute(nullArgument, false);
   int nullArgState = nullArgument.Scope.State, nullArgOverrides = nullArgument.Scope.Overrides;
   int nullArgOuterCalls = nullArgument.GateCalls;
   int nullCallsDuring = Interlocked.CompareExchange(ref nullGateCalls, 0, 0) - nullCallsBefore;
   bool nullArgRetry = nullArgument.Scope.Execute(nullArgument, false);
   Console.WriteLine("NULL_ARGUMENT_REENTRY outer=" + nullArgOuter + " nested=" + nullArgument.NestedResult +
     " state=" + nullArgState + " overrides=" + nullArgOverrides + " retry=" + nullArgRetry +
     " outerCalls=" + nullArgOuterCalls + " nullGateCalls=" + nullCallsDuring);

   Require(!crossOuter && !cross.NestedResult && crossState == FakeScope.Stopped && crossOverrides == 0 &&
     !crossRetry && crossInner.Scope.State == FakeScope.Armed && crossOuterCalls == 1 && crossInnerCalls == 1,
     "cross-scope direct reentry stops outer without stealing");
   Require(!nullOuter && !nullScope.NestedResult && nullState == FakeScope.Stopped && nullOverrides == 0 &&
     !nullRetry && nullOuterCalls == 1 && nullInnerCalls == 1, "null-scope direct reentry stops outer without retry");
   Require(!nullArgOuter && !nullArgument.NestedResult && nullArgState == FakeScope.Stopped && nullArgOverrides == 0 &&
     !nullArgRetry && nullArgOuterCalls == 1 && nullCallsDuring == 1, "null-argument direct reentry stops outer without retry");

   bool guardMatched;
   IList<CodeInstruction> guarded = ScopedFakeTranspiler.Rewrite(ReferenceInput(), out guardMatched);
   MethodInfo entry = typeof(ScopedFakeTranspiler).GetMethod("EnterTarget", BindingFlags.Static | BindingFlags.NonPublic);
   Require(guardMatched && entry != null && guarded.Count == 8 && guarded[1].opcode == OpCodes.Call &&
     Object.Equals(guarded[1].operand, entry), "fixed fake entry guard is present");

   FakeInvocation throwing = Attempt(true);
   Exception expected = new InvalidOperationException("controlled fake gate exception");
   throwing.GateException = expected;
   Exception caught = null;
   try { throwing.Scope.Execute(throwing, false); }
   catch (Exception exception) { caught = exception; }
   throwing.GateException = null;
   Require(Object.ReferenceEquals(expected, caught) && throwing.Scope.State == FakeScope.Stopped &&
     !throwing.Scope.Execute(throwing, false) && throwing.Scope.Overrides == 0,
     "fake gate exception propagates and stops the scope");

   FakeInvocation filter = Attempt(true);
   filter.ThrowFilter = true;
   Require(!filter.Scope.Execute(filter, false) && filter.Scope.State == FakeScope.Stopped &&
     !filter.Scope.Execute(filter, false) && filter.Scope.Overrides == 0,
     "filter error preserves the obtained Boolean and stops");

   FakeInvocation wrongThread = Attempt(true);
   if (simulateUndrained) {
    wrongThread.BlockGate = true;
    wrongThread.GateEntered = new ManualResetEvent(false);
    wrongThread.GateRelease = new ManualResetEvent(false);
   }
   bool threadResult = true;
   Thread worker = new Thread(delegate() { threadResult = wrongThread.Scope.Execute(wrongThread, false); });
   worker.IsBackground = true;
   workerUndrained = true;
   worker.Start();
   if (simulateUndrained && !wrongThread.GateEntered.WaitOne(3000)) {
    throw new InvalidOperationException("Controlled worker did not signal; drain state is unknown.");
   }
   if (!worker.Join(simulateUndrained ? 100 : 5000)) {
    throw new InvalidOperationException("Fake worker did not drain before timeout.");
   }
   workerUndrained = false;
   Require(!threadResult && wrongThread.Scope.State == FakeScope.Stopped &&
     !wrongThread.Scope.Execute(wrongThread, false), "foreign thread stops the fake scope");
  }

  private static int RunHarmony() {
   MethodInfo target = Method("Target"), transpiler = typeof(ScopedFakeTranspiler).GetMethod(
    "Transpiler", BindingFlags.Static | BindingFlags.NonPublic);
   string owner = "OpenAI-Codex_review_model.ScopedFake." + Guid.NewGuid().ToString("N");
   Console.WriteLine("FAKE_OWNER=" + owner);
   Console.WriteLine("FAKE_TARGET=" + target.DeclaringType.FullName + "." + target.Name);
   Console.WriteLine("FAKE_TARGET_IL=" + BitConverter.ToString(target.GetMethodBody().GetILAsByteArray()));
   Require(target.DeclaringType == typeof(ScopedFakeTranspilerChecks) &&
     target.Module.Assembly == typeof(ScopedFakeTranspilerChecks).Assembly && target.IsStatic,
     "patch target is the fixed own fake method");
   Harmony harmony = new Harmony(owner);
   bool attempted = false, confirmed = false, unknown = false, failed = false;
   try {
    Patches prior = Harmony.GetPatchInfo(target);
    if (prior != null) foreach (string value in prior.Owners)
     throw new InvalidOperationException("Unexpected pre-existing fake patch owner: " + value);
    attempted = true;
    MethodInfo replacement = harmony.Patch(target, null, null, new HarmonyMethod(transpiler), null);
    if (replacement == null || !HasOwner(target, owner)) {
     unknown = true;
     throw new InvalidOperationException("Patch result or fake owner is unknown.");
    }
    confirmed = true;
    Require(ScopedFakeTranspiler.RewriteStatus == 1, "actual Harmony fake stream matches fixed profile");
    CheckScopedBehavior();
   } catch (Exception exception) {
    failed = true;
    if ((attempted && !confirmed) || workerUndrained) unknown = true;
    Console.Error.WriteLine((unknown ? "EXECUTION_UNKNOWN " : "FAKE_CHECK_FAILURE ") + exception);
   } finally {
    if (confirmed && workerUndrained) {
     unknown = true;
     Console.Error.WriteLine("UNPATCH_SKIPPED_UNKNOWN worker may still be in fake target");
    } else if (confirmed) {
     try {
      Console.WriteLine("FAKE_UNPATCH_ATTEMPT=1");
      harmony.Unpatch(target, transpiler);
      if (HasOwner(target, owner)) throw new InvalidOperationException("Fake owner remains after unpatch.");
      FakeInvocation post = Attempt(true);
      if (post.Scope.Execute(post, false) || post.Scope.Decisions != 0)
       throw new InvalidOperationException("Fake baseline or filter dispatch remains after unpatch.");
      Console.WriteLine("FAKE_UNPATCH_CONFIRMED owner-absent and fake-false-baseline");
     } catch (Exception exception) {
      unknown = true;
      Console.Error.WriteLine("UNPATCH_UNKNOWN " + exception);
     }
    }
   }
   if (unknown) { Console.WriteLine("RESULT_UNKNOWN process-stops-no-retry"); return 2; }
   if (failed) return 1;
   Console.WriteLine("PASS scoped fake Harmony experiment");
   return 0;
  }

  private static int Main(string[] args) {
   try {
    if (args.Length > 1 || (args.Length == 1 && args[0] != "--simulate-undrained"))
     throw new ArgumentException("Expected no argument or --simulate-undrained.");
    simulateUndrained = args.Length == 1;
    Assembly harmony = typeof(Harmony).Assembly;
    Console.WriteLine("HARMONY_IDENTITY=" + harmony.FullName);
    Console.WriteLine("HARMONY_LOCATION_FILE=" + harmony.Location);
    Console.WriteLine("HARMONY_LOCATION_FILE_SHA256=" + FileHash(harmony.Location));
    Console.WriteLine("HOST_CLR=" + Environment.Version + " BITS=" + (IntPtr.Size * 8));
    Require(harmony.GetName().Name == "0Harmony" && harmony.GetName().Version.Equals(new Version(2, 4, 3, 0)) &&
      FileHash(harmony.Location) == "C228FF02A360A48C03A9154F49282DCCDB5809599D5E606CF3840CB4887A5503",
      "expected Harmony file and declared identity");
    Console.WriteLine("LIMIT controlled fake only; source producer atomicity and game behavior not established");
    CheckUnpatchedProjection();
    return RunHarmony();
   } catch (Exception exception) {
    Console.Error.WriteLine("SETUP_OR_CHECK_ERROR=" + exception);
    return 1;
   }
  }
 }
}
#endif
