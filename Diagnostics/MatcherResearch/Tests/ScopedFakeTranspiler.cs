// OpenAI-Codex_review_model
#if FULLMAP_SCOPED_FAKE_EXPERIMENT && !RELEASE && !FULLMAP_RELEASE
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;

namespace ScopedFakeResearch {
 // Test-only scope: callers and the fake gate are controlled by this process.
 internal sealed class FakeScope {
  internal const int Armed = 1, Consumed = 2, Stopped = 3, Denied = 4;
  private static readonly object EntryGate = new object();
  private static FakeScope activeScope;
  private static int activeTargets;
  private static long nextEntryId;
  private static bool trackingUnknown;
  [ThreadStatic]
  private static Stack<long> entries;
  private readonly Thread owner;
  private FakeInvocation expected;
  private long targetEntryId;
  private int state, depth, targetEntries, overrides, decisions;

  internal FakeScope(bool allowed) {
   owner = Thread.CurrentThread;
   state = allowed ? Armed : Denied;
  }

  internal int State { get { return Interlocked.CompareExchange(ref state, 0, 0); } }
  internal int Overrides { get { return Interlocked.CompareExchange(ref overrides, 0, 0); } }
  internal int Decisions { get { return Interlocked.CompareExchange(ref decisions, 0, 0); } }

  internal void Bind(FakeInvocation invocation) {
   if (invocation == null || !Object.ReferenceEquals(invocation.Scope, this) || expected != null)
    throw new InvalidOperationException("Invalid fake scope binding.");
   expected = invocation;
  }

  internal void Stop() { Interlocked.Exchange(ref state, Stopped); }

  internal static void ObserveActiveTargetEntry() {
   lock (EntryGate) {
    if (entries == null) entries = new Stack<long>();
    if (activeTargets == Int32.MaxValue || nextEntryId == Int64.MaxValue) {
     trackingUnknown = true;
     if (activeScope != null) activeScope.Stop();
     entries.Push(0);
     return;
    }
    long entry = ++nextEntryId;
    entries.Push(entry);
    activeTargets++;
    FakeScope active = activeScope;
    if (active != null) active.ObserveTargetEntry(entry);
   }
  }

  internal static void ObserveTargetExit() {
   lock (EntryGate) {
    if (entries == null || entries.Count == 0) {
     trackingUnknown = true;
     if (activeScope != null) activeScope.Stop();
     return;
    }
    long entry = entries.Pop();
    if (entry == 0 || activeTargets <= 0) {
     trackingUnknown = true;
     if (activeScope != null) activeScope.Stop();
     return;
    }
    activeTargets--;
   }
  }

  // This entry witness applies only to the fixed fake target in this process.
  private void ObserveTargetEntry(long entry) {
   targetEntries++;
   if (depth != 1 || activeTargets != 1 || targetEntries != 1 || targetEntryId != 0 ||
       !Object.ReferenceEquals(Thread.CurrentThread, owner)) { Stop(); return; }
   targetEntryId = entry;
  }

  internal bool Execute(FakeInvocation invocation, bool nativeResult) {
   if (invocation == null || !Object.ReferenceEquals(invocation.Scope, this))
    return ScopedFakeTranspilerChecks.Target(invocation, nativeResult);
   if (!Object.ReferenceEquals(Thread.CurrentThread, owner)) {
    Stop();
    return ScopedFakeTranspilerChecks.Target(invocation, nativeResult);
   }
   if (!Object.ReferenceEquals(invocation, expected))
    return ScopedFakeTranspilerChecks.Target(invocation, nativeResult);
   bool activated = false;
   lock (EntryGate) {
    if (trackingUnknown || activeTargets != 0 || activeScope != null || depth != 0) {
     if (activeScope != null) activeScope.Stop();
     Stop();
    } else if (state == Armed) {
     depth = 1;
     activeScope = this;
     activated = true;
    }
   }
   if (!activated) return ScopedFakeTranspilerChecks.Target(invocation, nativeResult);
   try {
    return ScopedFakeTranspilerChecks.Target(invocation, nativeResult);
   } catch {
    Stop();
    throw;
   } finally {
    lock (EntryGate) {
     if (!Object.ReferenceEquals(activeScope, this)) { trackingUnknown = true; Stop(); }
     if (activeTargets != 0) Stop();
     if (Object.ReferenceEquals(activeScope, this)) activeScope = null;
     depth = 0;
     Interlocked.CompareExchange(ref state, Stopped, Armed);
    }
   }
  }

  internal bool Decide(bool original, FakeInvocation invocation) {
   Interlocked.Increment(ref decisions);
   if (!Object.ReferenceEquals(invocation, expected) || depth != 1 ||
       !Object.ReferenceEquals(Thread.CurrentThread, owner)) return original;
   lock (EntryGate) {
    try {
     if (trackingUnknown || !Object.ReferenceEquals(activeScope, this) || activeTargets != 1 ||
         entries == null || entries.Count == 0 || entries.Peek() != targetEntryId || targetEntryId == 0) {
      Stop();
      return original;
     }
     if (!invocation.Eligible) { Stop(); return original; }
     if (invocation.ThrowFilter) throw new InvalidOperationException("Controlled fake filter error.");
     if (Interlocked.CompareExchange(ref state, Consumed, Armed) != Armed) return original;
     if (!original) Interlocked.Increment(ref overrides);
     return true;
    } catch (Exception) {
     Stop();
     return original;
    }
   }
  }
 }

 internal static class ScopedFakeTranspiler {
  private static readonly MethodInfo TargetMethod = typeof(ScopedFakeTranspilerChecks).GetMethod(
   "Target", BindingFlags.Static | BindingFlags.NonPublic);
  private static readonly MethodInfo GateMethod = typeof(ScopedFakeTranspilerChecks).GetMethod(
   "Gate", BindingFlags.Static | BindingFlags.NonPublic);
  internal static readonly MethodInfo DecisionMethod = typeof(ScopedFakeTranspiler).GetMethod(
   "Decide", BindingFlags.Static | BindingFlags.NonPublic);
  private static readonly MethodInfo EntryMethod = typeof(ScopedFakeTranspiler).GetMethod(
   "EnterTarget", BindingFlags.Static | BindingFlags.NonPublic);
  internal static int RewriteStatus;

  internal static bool Decide(bool original, FakeInvocation invocation) {
   if (invocation != null && invocation.Scope != null)
    return invocation.Scope.Decide(original, invocation);
   return original;
  }

  internal static void EnterTarget(FakeInvocation invocation) {
   FakeScope.ObserveActiveTargetEntry();
  }

  internal static void ExitTarget() { FakeScope.ObserveTargetExit(); }

  internal static IList<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> source, out bool matched) {
   matched = false;
   if (source == null || TargetMethod == null || GateMethod == null || DecisionMethod == null || EntryMethod == null)
    throw new InvalidOperationException("Fake profile or instruction input unavailable.");
   List<CodeInstruction> captured = new List<CodeInstruction>();
   foreach (CodeInstruction instruction in source) {
    if (captured.Count == 16 || instruction == null || instruction.labels == null ||
        instruction.blocks == null || instruction.labels.Count != 0 || instruction.blocks.Count != 0 ||
        (instruction.operand != null && !(instruction.operand is MethodBase)))
     throw new InvalidOperationException("Unsupported fake instruction metadata.");
    captured.Add(new CodeInstruction(instruction.opcode, instruction.operand));
   }
   if (captured.Count == 4 &&
       captured[0].opcode == OpCodes.Ldarg_0 && captured[0].operand == null &&
       captured[1].opcode == OpCodes.Ldarg_1 && captured[1].operand == null &&
       captured[2].opcode == OpCodes.Call && Object.Equals(captured[2].operand, GateMethod) &&
       captured[3].opcode == OpCodes.Ret && captured[3].operand == null) {
    captured.Insert(0, new CodeInstruction(OpCodes.Ldarg_0));
    captured.Insert(1, new CodeInstruction(OpCodes.Call, EntryMethod));
    captured.Insert(5, new CodeInstruction(OpCodes.Ldarg_0));
    captured.Insert(6, new CodeInstruction(OpCodes.Call, DecisionMethod));
    matched = true;
   }
   source = captured;
   return new List<CodeInstruction>(source);
  }

  // The only installed target is the fixed fake MethodInfo in this assembly.
  internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
   try {
    if (!Object.Equals(original, TargetMethod)) {
     Interlocked.Exchange(ref RewriteStatus, 2);
     throw new InvalidOperationException("Unexpected fake target.");
    }
    bool matched;
    IList<CodeInstruction> result = Rewrite(instructions, out matched);
    Interlocked.Exchange(ref RewriteStatus, matched ? 1 : 2);
    return result;
   } catch {
    Interlocked.Exchange(ref RewriteStatus, 3);
    throw;
   }
  }
 }
}
#endif
