// Self-contained matcher checks using only authored fake types and runtime members.
// Signature: OpenAI-Codex
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using RealisticCityTime.CalendarInterception;

public static class PortableMatcherChecks
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    private static CalendarInstruction[] Shape()
    {
        FieldInfo metadata = typeof(SimulationManager).GetField("m_metaData");
        FieldInfo date = typeof(SimulationMetaData).GetField("m_currentDateTime");
        FieldInfo perFrame = typeof(SimulationManager).GetField("m_timePerFrame");
        MethodInfo addition = typeof(DateTime).GetMethod("op_Addition", new Type[] { typeof(DateTime), typeof(TimeSpan) });
        return new CalendarInstruction[] {
            new CalendarInstruction(OpCodes.Ldarg_0, null), new CalendarInstruction(OpCodes.Ldfld, metadata),
            new CalendarInstruction(OpCodes.Dup, null), new CalendarInstruction(OpCodes.Ldfld, date),
            new CalendarInstruction(OpCodes.Ldarg_0, null), new CalendarInstruction(OpCodes.Ldfld, perFrame),
            new CalendarInstruction(OpCodes.Call, addition), new CalendarInstruction(OpCodes.Stfld, date)
        };
    }

    public static void Run()
    {
        checks = 0;
        Type type = typeof(SimulationManager);
        MethodInfo method = type.GetMethod("SimulationStep", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        CalendarInstruction[] source = Shape();
        CalendarInstruction first = source[0];
        CalendarMatch match = CalendarIlMatcher.Find(type, method, source);
        Check(match.Status == CalendarMatchStatus.Unique && match.AdditionInstructionIndex == 6, "unique authored fake shape");
        Check(Object.ReferenceEquals(source[0], first), "input sequence remains unchanged");

        CalendarInstruction[] missing = (CalendarInstruction[])source.Clone();
        missing[6] = new CalendarInstruction(OpCodes.Nop, null);
        Check(CalendarIlMatcher.Find(type, method, missing).Status == CalendarMatchStatus.NoMatch, "missing addition rejected");
        List<CalendarInstruction> duplicate = new List<CalendarInstruction>(source);
        duplicate.AddRange(source);
        Check(CalendarIlMatcher.Find(type, method, duplicate.ToArray()).Status == CalendarMatchStatus.Ambiguous, "duplicate shape rejected");
        CalendarInstruction[] wrongField = (CalendarInstruction[])source.Clone();
        wrongField[1] = new CalendarInstruction(OpCodes.Ldfld, typeof(SimulationManager).GetField("m_timePerFrame"));
        Check(CalendarIlMatcher.Find(type, method, wrongField).Status == CalendarMatchStatus.NoMatch, "wrong field identity rejected");
        List<CalendarInstruction> address = new List<CalendarInstruction>(source);
        address.Add(new CalendarInstruction(OpCodes.Ldflda, typeof(SimulationMetaData).GetField("m_currentDateTime")));
        Check(CalendarIlMatcher.Find(type, method, address.ToArray()).Status == CalendarMatchStatus.Ambiguous, "date address load rejected");
        List<CalendarInstruction> extraStore = new List<CalendarInstruction>(source);
        extraStore.Add(new CalendarInstruction(OpCodes.Stfld, typeof(SimulationMetaData).GetField("m_currentDateTime")));
        Check(CalendarIlMatcher.Find(type, method, extraStore.ToArray()).Status == CalendarMatchStatus.Ambiguous, "second direct date store rejected");
        List<CalendarInstruction> malformed = new List<CalendarInstruction>(source);
        malformed.Add(new CalendarInstruction(OpCodes.Ldc_I4, "not-an-int"));
        Check(CalendarIlMatcher.Find(type, method, malformed.ToArray()).Status == CalendarMatchStatus.InvalidInput, "wrong operand type rejected");
        malformed[malformed.Count - 1] = new CalendarInstruction(OpCodes.Prefix1, null);
        Check(CalendarIlMatcher.Find(type, method, malformed.ToArray()).Status == CalendarMatchStatus.InvalidInput, "reserved opcode rejected");
        Check(CalendarIlMatcher.Find(type, null, source).Status == CalendarMatchStatus.InvalidTarget, "missing method rejected");
        Check(CalendarIlMatcher.Find(typeof(SimulationMetaData), method, source).Status == CalendarMatchStatus.InvalidTarget, "wrong trusted type rejected");
        Check(CalendarIlMatcher.Find(type, method, null).Status == CalendarMatchStatus.InvalidInput, "missing input rejected");
        Console.WriteLine("TOTAL: " + checks);
    }
}
