// Isolated Harmony 2 fixture experiment. Compile and run only in an authorized fresh process.
// The host must verify the exact Harmony bytes before loading and pass that Assembly to Run.
// This source does not authenticate the host, prove a final patch chain, or control game time.
// Execution markers: OpenAI-Codex
// Signature: OpenAI-Codex_harmony_fixture_impl
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using RealisticCityTime.CalendarInterception;

public static class HarmonyFixtureChecks
{
    private static int runStarted;
    private static int helperCalls;
    private static int candidateBuilt;
    private static int checks;

    private sealed class CapturedInstruction
    {
        public OpCode OpCode;
        public object Operand;
        public Label[] Labels;
        public ExceptionBlock[] Blocks;
    }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    private static bool IsForbiddenAssembly(string name)
    {
        return name.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(name, "ColossalManaged", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(name, "ICities", StringComparison.OrdinalIgnoreCase);
    }

    private static MethodInfo TrustedTarget()
    {
        Type fixture = typeof(SimulationManager);
        if (fixture.Assembly != typeof(HarmonyFixtureChecks).Assembly || fixture.Module != typeof(HarmonyFixtureChecks).Module ||
            fixture.Assembly == typeof(Harmony).Assembly || IsForbiddenAssembly(fixture.Assembly.GetName().Name))
            throw new InvalidOperationException("The target is not the directly bound fixture type.");
        MethodInfo method = fixture.GetMethod("SimulationStep", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            null, Type.EmptyTypes, null);
        if (method == null || method.DeclaringType != fixture || method.Module != fixture.Module ||
            !method.IsPrivate || method.IsStatic || method.IsGenericMethod || method.ReturnType != typeof(void))
            throw new InvalidOperationException("The fixture target signature is not supported.");
        return method;
    }

    private static void VerifyEnvironment(Assembly verifiedHarmonyAssembly)
    {
        if (verifiedHarmonyAssembly == null || verifiedHarmonyAssembly != typeof(Harmony).Assembly)
            throw new InvalidOperationException("The host did not bind the verified Harmony assembly.");
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            if (IsForbiddenAssembly(assembly.GetName().Name))
                throw new InvalidOperationException("A game or Unity assembly is present in the fixture process.");
        TrustedTarget();
    }

    private static object CopyOperand(object operand)
    {
        Label[] labels = operand as Label[];
        return labels == null ? operand : labels.Clone();
    }

    private static ExceptionBlock CopyBlock(ExceptionBlock block)
    {
        if (block == null) throw new InvalidOperationException("Missing exception block.");
        return new ExceptionBlock(block.blockType, block.catchType);
    }

    // A new list is emitted only after the supplied fixture method and complete snapshot pass.
    // The caller must prevent concurrent mutation while Harmony supplies these instructions.
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        Interlocked.Exchange(ref candidateBuilt, 0);
        MethodInfo target = TrustedTarget();
        MethodInfo actual = original as MethodInfo;
        if (instructions == null || actual == null || !target.Equals(actual))
            throw new InvalidOperationException("Untrusted fixture method or missing instructions.");

        List<CapturedInstruction> captured = new List<CapturedInstruction>();
        List<CalendarInstruction> shape = new List<CalendarInstruction>();
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction == null || instruction.labels == null || instruction.blocks == null)
                throw new InvalidOperationException("Incomplete instruction metadata.");
            CapturedInstruction item = new CapturedInstruction();
            item.OpCode = instruction.opcode;
            item.Operand = CopyOperand(instruction.operand);
            item.Labels = instruction.labels.ToArray();
            item.Blocks = new ExceptionBlock[instruction.blocks.Count];
            for (int i = 0; i < item.Blocks.Length; i++) item.Blocks[i] = CopyBlock(instruction.blocks[i]);
            captured.Add(item);
            shape.Add(new CalendarInstruction(item.OpCode, item.Operand));
        }
        CalendarMatch match = CalendarIlMatcher.Find(typeof(SimulationManager), actual, shape.ToArray());
        if (match.Status != CalendarMatchStatus.Unique)
            throw new InvalidOperationException("Fixture match rejected: " + match.Status);

        MethodInfo helper = typeof(HarmonyFixtureChecks).GetMethod("PassiveAdd", BindingFlags.Public | BindingFlags.Static);
        if (helper == null || helper.ReturnType != typeof(DateTime))
            throw new InvalidOperationException("Passive helper is unavailable.");
        List<CodeInstruction> result = new List<CodeInstruction>(captured.Count);
        for (int i = 0; i < captured.Count; i++)
        {
            CapturedInstruction item = captured[i];
            object operand = i == match.AdditionInstructionIndex ? (object)helper : CopyOperand(item.Operand);
            CodeInstruction clone = new CodeInstruction(item.OpCode, operand);
            clone.labels.AddRange(item.Labels);
            foreach (ExceptionBlock block in item.Blocks) clone.blocks.Add(CopyBlock(block));
            result.Add(clone);
        }
        Interlocked.Exchange(ref candidateBuilt, 1);
        return result;
    }

    // The counter is fixture-only instrumentation. The returned date preserves native addition semantics.
    public static DateTime PassiveAdd(DateTime date, TimeSpan span)
    {
        Interlocked.Increment(ref helperCalls);
        return date + span;
    }

    private static List<CodeInstruction> Pattern()
    {
        FieldInfo metadata = typeof(SimulationManager).GetField("m_metaData");
        FieldInfo date = typeof(SimulationMetaData).GetField("m_currentDateTime");
        FieldInfo perFrame = typeof(SimulationManager).GetField("m_timePerFrame");
        MethodInfo addition = typeof(DateTime).GetMethod("op_Addition", new Type[] { typeof(DateTime), typeof(TimeSpan) });
        return new List<CodeInstruction> {
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, metadata),
            new CodeInstruction(OpCodes.Dup), new CodeInstruction(OpCodes.Ldfld, date),
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, perFrame),
            new CodeInstruction(OpCodes.Call, addition), new CodeInstruction(OpCodes.Stfld, date)
        };
    }

    private static bool Rejected(List<CodeInstruction> input, MethodInfo target)
    {
        try { Transpiler(input, target); }
        catch (InvalidOperationException) { return true; }
        return false;
    }

    private static bool ContainsOnlyOwner(Patches info, string owner, out bool ownsPatch)
    {
        ownsPatch = false;
        if (info == null) return true;
        foreach (string seen in info.Owners)
        {
            if (!String.Equals(seen, owner, StringComparison.Ordinal)) return false;
            ownsPatch = true;
        }
        return true;
    }

    private static bool GuardedTick(SimulationManager fixture, ref bool uncertain)
    {
        if (uncertain) return false;
        try { return fixture.Tick(); }
        catch (ArgumentOutOfRangeException) { uncertain = true; return false; }
        catch (OverflowException) { uncertain = true; return false; }
    }

    private static void CheckCandidateConstruction(MethodInfo target)
    {
        List<CodeInstruction> input = Pattern();
        DynamicMethod scratch = new DynamicMethod("FixtureLabel", typeof(void), Type.EmptyTypes);
        Label label = scratch.GetILGenerator().DefineLabel();
        input[6].labels.Add(label);
        input[6].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        input.Insert(0, new CodeInstruction(OpCodes.Switch, new Label[] { label }));
        object originalOperand = input[7].operand;
        List<CodeInstruction> candidate = new List<CodeInstruction>(Transpiler(input, target));
        MethodInfo helper = typeof(HarmonyFixtureChecks).GetMethod("PassiveAdd", BindingFlags.Public | BindingFlags.Static);
        Check(input[7].operand.Equals(originalOperand) && candidate[7].operand.Equals(helper), "only candidate call operand is replaced");
        Check(!Object.ReferenceEquals(input[7], candidate[7]) && !Object.ReferenceEquals(input[7].labels, candidate[7].labels) &&
            !Object.ReferenceEquals(input[7].blocks, candidate[7].blocks) && candidate[7].labels.Count == 1 &&
            candidate[7].labels[0].Equals(label) && candidate[7].blocks.Count == 1 &&
            !Object.ReferenceEquals(input[7].blocks[0], candidate[7].blocks[0]) &&
            candidate[7].blocks[0].blockType == input[7].blocks[0].blockType &&
            candidate[7].blocks[0].catchType == input[7].blocks[0].catchType, "labels and exception metadata are copied");
        Check(!Object.ReferenceEquals(input[0].operand, candidate[0].operand) &&
            ((Label[])candidate[0].operand)[0].Equals(label), "switch-label operand array is copied");

        List<CodeInstruction> missing = Pattern();
        missing[6] = new CodeInstruction(OpCodes.Nop);
        Check(Rejected(missing, target) && missing[6].opcode.Equals(OpCodes.Nop), "zero matches rejected without mutation");
        List<CodeInstruction> duplicate = Pattern();
        duplicate.AddRange(Pattern());
        Check(Rejected(duplicate, target) && duplicate.Count == 16, "multiple matches rejected without mutation");
        List<CodeInstruction> address = Pattern();
        address.Add(new CodeInstruction(OpCodes.Ldflda, typeof(SimulationMetaData).GetField("m_currentDateTime")));
        Check(Rejected(address, target), "date-field address load rejected");
        List<CodeInstruction> invalidBlock = Pattern();
        invalidBlock[6].blocks.Add(null);
        Check(Rejected(invalidBlock, target), "null exception block rejected before output");
        Check(Rejected(candidate, target), "previously transformed candidate is not treated as a fresh match");
        candidate[7].operand = originalOperand;
        Check(!candidate[7].operand.Equals(helper) && input[7].operand.Equals(originalOperand), "later rewrite invalidates only the candidate, not input");
    }

    // The host must terminate this isolated process on any exception or uncertain cleanup.
    public static void Run(Assembly verifiedHarmonyAssembly)
    {
        if (Interlocked.Exchange(ref runStarted, 1) != 0) throw new InvalidOperationException("Use a fresh fixture process.");
        VerifyEnvironment(verifiedHarmonyAssembly);
        MethodInfo target = TrustedTarget();
        if (Harmony.GetPatchInfo(target) != null) throw new InvalidOperationException("Fixture target already has patch state.");
        checks = 0;
        CheckCandidateConstruction(target);

        DateTime initial = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan delta = TimeSpan.FromSeconds(1);
        Check(PassiveAdd(initial, delta) == initial + delta && PassiveAdd(initial, delta).Kind == DateTimeKind.Utc,
            "passive helper preserves addition and date kind");
        bool directOverflow = false;
        try { PassiveAdd(DateTime.MaxValue, TimeSpan.FromTicks(1)); }
        catch (ArgumentOutOfRangeException) { directOverflow = true; }
        Check(directOverflow, "passive helper preserves date overflow");

        Interlocked.Exchange(ref helperCalls, 0);
        SimulationManager baseline = new SimulationManager(initial, delta);
        baseline.Tick();
        Check(helperCalls == 0 && baseline.m_metaData.m_currentDateTime == initial + delta, "unpatched fixture uses native addition");

        string owner = "org.openai.codex.fixture." + Guid.NewGuid().ToString("N");
        Harmony harmony = new Harmony(owner);
        bool attempted = false;
        bool installed = false;
        SimulationManager fake = new SimulationManager(initial, delta);
        try
        {
            Interlocked.Exchange(ref candidateBuilt, 0);
            MethodInfo transpiler = typeof(HarmonyFixtureChecks).GetMethod("Transpiler", BindingFlags.Public | BindingFlags.Static);
            attempted = true;
            Console.WriteLine("FIXTURE_PATCH_BEGIN");
            harmony.Patch(target, null, null, new HarmonyMethod(transpiler), null);
            Console.WriteLine("FIXTURE_PATCH_RETURNED");
            installed = true;
            bool owns;
            if (candidateBuilt != 1 || !ContainsOnlyOwner(Harmony.GetPatchInfo(target), owner, out owns) || !owns)
                throw new InvalidOperationException("Installed fixture patch state is not confirmed.");
            Check(true, "owned fixture patch reported after unique match");
            bool coherent = true;
            for (int i = 0; i < 3; i++)
            {
                DateTime before = fake.m_metaData.m_currentDateTime;
                fake.Tick();
                long ticks = fake.m_metaData.m_currentDateTime.Ticks - fake.m_timeOffsetTicks;
                if (fake.m_metaData.m_currentDateTime < before || fake.FrameToTime(fake.m_currentFrameIndex) != fake.m_metaData.m_currentDateTime ||
                    ticks % fake.m_timePerFrame.Ticks != 0 || ticks / fake.m_timePerFrame.Ticks != fake.m_currentFrameIndex) coherent = false;
            }
            Check(coherent && helperCalls == 3 && fake.m_metaData.m_currentDateTime == initial.AddSeconds(3),
                "patched fixture remains monotonic with matching frame conversion");
            Check(fake.m_metaData.m_currentDateTime.Kind == DateTimeKind.Utc, "patched fixture retains date kind");
            fake.Paused = true;
            uint frame = fake.m_currentFrameIndex;
            DateTime date = fake.m_metaData.m_currentDateTime;
            Check(!fake.Tick() && fake.m_currentFrameIndex == frame && fake.m_metaData.m_currentDateTime == date && helperCalls == 3,
                "paused fixture does not invoke target");
            SimulationManager overflow = new SimulationManager(DateTime.MaxValue, TimeSpan.FromTicks(1));
            bool uncertain = false;
            Check(!GuardedTick(overflow, ref uncertain) && uncertain && overflow.m_currentFrameIndex == 1U &&
                overflow.m_metaData.m_currentDateTime == DateTime.MaxValue, "overflow reports a partially advanced fixture frame");
            uint stoppedFrame = overflow.m_currentFrameIndex;
            Check(!GuardedTick(overflow, ref uncertain) && overflow.m_currentFrameIndex == stoppedFrame,
                "uncertain fixture session does not retry");
        }
        finally
        {
            if (attempted)
            {
                Patches current = Harmony.GetPatchInfo(target);
                bool owns;
                if (!ContainsOnlyOwner(current, owner, out owns))
                    throw new InvalidOperationException("Unknown owner; no fixture unpatch attempted. End the process.");
                if (current == null && installed)
                    throw new InvalidOperationException("Installed state disappeared; end the fixture process.");
                if (owns)
                {
                    Console.WriteLine("FIXTURE_SCOPED_UNPATCH_BEGIN");
                    harmony.Unpatch(target, HarmonyPatchType.All, owner);
                    Console.WriteLine("FIXTURE_SCOPED_UNPATCH_RETURNED");
                    Patches remaining = Harmony.GetPatchInfo(target);
                    bool stillOwns;
                    if (!ContainsOnlyOwner(remaining, owner, out stillOwns) || stillOwns)
                        throw new InvalidOperationException("Scoped fixture removal is uncertain. End the process.");
                }
                else if (installed)
                    throw new InvalidOperationException("Owned fixture patch is not visible. End the process.");
            }
        }
        int callsAfterRemoval = helperCalls;
        fake.Paused = false;
        fake.Tick();
        Check(helperCalls == callsAfterRemoval, "scoped fixture removal leaves no helper invocation");
        Console.WriteLine("TOTAL: " + checks);
    }
}
