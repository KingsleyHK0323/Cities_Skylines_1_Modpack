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
  internal bool WorkerBeforeOnce, WorkerAfterOnce, WorkerCompleted, WorkerSawOwnerWaiting, WorkerNestedResult, WorkerPostDecision;
  internal int WorkerStage, WorkerObservedStage, WorkerOwnerId, WorkerId;
  internal Exception WorkerError;
  internal FakeInvocation WorkerArgument;
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
  private static Action fakeExitHook;

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
    if (invocation.WorkerBeforeOnce) {
     invocation.WorkerBeforeOnce = false;
     RunDirectWorker(invocation, 1);
    }
    if (!invocation.Eligible) return false;
   }
   return nativeResult;
  }

  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  internal static bool Target(FakeInvocation invocation, bool nativeResult) {
   return Gate(invocation, nativeResult);
  }

  private static void RunDirectWorker(FakeInvocation invocation, int stage) {
   Thread ownerThread = Thread.CurrentThread;
   invocation.WorkerOwnerId = ownerThread.ManagedThreadId;
   Interlocked.Exchange(ref invocation.WorkerStage, stage);
   Thread worker = new Thread(delegate() {
    try {
     invocation.WorkerId = Thread.CurrentThread.ManagedThreadId;
     DateTime deadline = DateTime.UtcNow.AddSeconds(3);
     while ((ownerThread.ThreadState & ThreadState.WaitSleepJoin) == 0) {
      if (DateTime.UtcNow >= deadline) throw new InvalidOperationException("Owner was not observed waiting.");
      Thread.Sleep(1);
     }
     invocation.WorkerSawOwnerWaiting = true;
     invocation.WorkerObservedStage = Interlocked.CompareExchange(ref invocation.WorkerStage, 0, 0);
     invocation.WorkerNestedResult = Target(invocation.WorkerArgument, false);
     invocation.WorkerCompleted = true;
    } catch (Exception exception) { invocation.WorkerError = exception; }
   });
   worker.IsBackground = true;
   workerUndrained = true;
   worker.Start();
   if (!worker.Join(5000)) throw new InvalidOperationException("Direct fake worker did not drain.");
   workerUndrained = false;
   Interlocked.Exchange(ref invocation.WorkerStage, 0);
   if (invocation.WorkerError != null) throw new InvalidOperationException("Direct fake worker failed.", invocation.WorkerError);
  }

  private static void AfterTargetPostfix(FakeInvocation invocation, bool __result) {
   if (invocation != null && invocation.WorkerAfterOnce) {
    invocation.WorkerAfterOnce = false;
    invocation.WorkerPostDecision = __result;
    RunDirectWorker(invocation, 2);
   }
  }

  private static Exception AfterTargetFinalizer(Exception __exception) {
   Action exit = fakeExitHook;
   if (exit != null) exit();
   return __exception;
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

  private static bool CrossThreadCase(string name, int kind, bool afterDecision, bool nativeValue) {
   FakeInvocation outer = Attempt(true);
   FakeInvocation other = kind == 1 ? Attempt(true) : null;
   outer.WorkerArgument = kind == 0 ? null : (kind == 1 ? other : outer);
   if (afterDecision) outer.WorkerAfterOnce = true; else outer.WorkerBeforeOnce = true;
   int nullBefore = Interlocked.CompareExchange(ref nullGateCalls, 0, 0);
   bool result = outer.Scope.Execute(outer, nativeValue);
   int state = outer.Scope.State, overrides = outer.Scope.Overrides, outerCalls = outer.GateCalls;
   int nullCalls = Interlocked.CompareExchange(ref nullGateCalls, 0, 0) - nullBefore;
   int otherCalls = other == null ? 0 : other.GateCalls;
   bool retry = outer.Scope.Execute(outer, false);
   Console.WriteLine("CROSS_THREAD_CASE " + name + " outer=" + result + " nested=" + outer.WorkerNestedResult +
     " state=" + state + " overrides=" + overrides + " retry=" + retry + " outerCalls=" + outerCalls +
     " otherCalls=" + otherCalls + " nullCalls=" + nullCalls + " stage=" + outer.WorkerObservedStage +
     " sawWait=" + outer.WorkerSawOwnerWaiting + " completed=" + outer.WorkerCompleted +
     " ownerId=" + outer.WorkerOwnerId + " workerId=" + outer.WorkerId + " undrained=" + workerUndrained +
     " postDecision=" + outer.WorkerPostDecision);
   bool counters = kind == 0 ? outerCalls == 1 && nullCalls == 1 :
     (kind == 1 ? outerCalls == 1 && otherCalls == 1 : outerCalls == 2);
   Require(outer.WorkerCompleted && outer.WorkerSawOwnerWaiting && !workerUndrained &&
     outer.WorkerOwnerId != outer.WorkerId && outer.WorkerObservedStage == (afterDecision ? 2 : 1) &&
     !outer.WorkerNestedResult && counters && (other == null || other.Scope.State == FakeScope.Armed),
     "cross-thread fixture entered and drained: " + name);
   if (afterDecision) Require(outer.WorkerPostDecision, "postfix saw original decision before worker: " + name);
   if (afterDecision)
    return result && state == FakeScope.Stopped && overrides == (nativeValue ? 0 : 1) && !retry;
   return result == nativeValue && state == FakeScope.Stopped && overrides == 0 && !retry;
  }

  private static bool ArmWhileOtherTargetActive() {
   FakeInvocation blocking = new FakeInvocation(null);
   blocking.BlockGate = true;
   blocking.GateEntered = new ManualResetEvent(false);
   blocking.GateRelease = new ManualResetEvent(false);
   bool workerResult = true;
   Exception workerError = null;
   Thread worker = new Thread(delegate() {
    try { workerResult = Target(blocking, false); }
    catch (Exception exception) { workerError = exception; }
   });
   worker.IsBackground = true;
   workerUndrained = true;
   worker.Start();
   FakeInvocation attempted = Attempt(true);
   bool result = true;
   int state = -1, overrides = -1;
   try {
    if (!blocking.GateEntered.WaitOne(3000)) throw new InvalidOperationException("Active fake worker did not enter gate.");
    result = attempted.Scope.Execute(attempted, false);
    state = attempted.Scope.State;
    overrides = attempted.Scope.Overrides;
   } finally {
    blocking.GateRelease.Set();
    if (!worker.Join(5000)) throw new InvalidOperationException("Active fake worker did not drain.");
    workerUndrained = false;
   }
   if (workerError != null) throw new InvalidOperationException("Active fake worker failed.", workerError);
   bool retry = attempted.Scope.Execute(attempted, false);
   Console.WriteLine("ARM_WHILE_ACTIVE outer=" + result + " state=" + state + " overrides=" + overrides +
     " retry=" + retry + " workerResult=" + workerResult + " workerGateCalls=" + blocking.GateCalls +
     " workerUndrained=" + workerUndrained);
   Require(!workerUndrained && !workerResult && blocking.GateCalls == 1, "unframed fake target was active and drained");
   return !result && state == FakeScope.Stopped && overrides == 0 && !retry;
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

   string[] caseNames = { "before-null", "before-other", "before-same", "after-null", "after-other", "after-same", "after-true-null" };
   bool[] caseResults = new bool[caseNames.Length];
   for (int i = 0; i < caseNames.Length; i++) caseResults[i] = CrossThreadCase(caseNames[i], i == 6 ? 0 : i % 3, i >= 3, i == 6);
   bool armWhileActive = ArmWhileOtherTargetActive();
   for (int i = 0; i < caseNames.Length; i++) Require(caseResults[i], "cross-thread decision boundary: " + caseNames[i]);
   Require(armWhileActive, "active unframed target prevents scope arming");

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
   MethodInfo target = Method("Target"), postfix = Method("AfterTargetPostfix"), finalizer = Method("AfterTargetFinalizer"), transpiler = typeof(ScopedFakeTranspiler).GetMethod(
    "Transpiler", BindingFlags.Static | BindingFlags.NonPublic);
   MethodInfo exit = typeof(ScopedFakeTranspiler).GetMethod("ExitTarget", BindingFlags.Static | BindingFlags.NonPublic);
   fakeExitHook = exit == null ? null : (Action)Delegate.CreateDelegate(typeof(Action), exit);
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
    MethodInfo replacement = harmony.Patch(target, null, new HarmonyMethod(postfix), new HarmonyMethod(transpiler), new HarmonyMethod(finalizer));
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
      Console.WriteLine("FAKE_POSTFIX_UNPATCH_ATTEMPT=1");
      harmony.Unpatch(target, postfix);
      Console.WriteLine("FAKE_FINALIZER_UNPATCH_ATTEMPT=1");
      harmony.Unpatch(target, finalizer);
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
