// Offline checks of a synthetic target; no Harmony or game assembly is loaded.
// Signature: OpenAI-Codex
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using RealisticCityTime.CalendarInterception;

public static class FakeIlProbeChecks
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        checks++;
        Console.WriteLine("PASS: " + name);
    }

    private static CalendarInstruction[] DecodeFakeMethod(MethodInfo method)
    {
        byte[] bytes = method.GetMethodBody().GetILAsByteArray();
        Dictionary<ushort, OpCode> codes = new Dictionary<ushort, OpCode>();
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(OpCode))
            {
                OpCode opcode = (OpCode)field.GetValue(null);
                codes[unchecked((ushort)opcode.Value)] = opcode;
            }
        }
        List<CalendarInstruction> decoded = new List<CalendarInstruction>();
        int position = 0;
        while (position < bytes.Length)
        {
            ushort key = bytes[position++];
            if (key == 0xFE)
            {
                if (position >= bytes.Length) throw new InvalidOperationException("Truncated fake opcode.");
                key = (ushort)(0xFE00 | bytes[position++]);
            }
            OpCode opcode;
            if (!codes.TryGetValue(key, out opcode)) throw new InvalidOperationException("Unknown fake opcode.");
            object operand = null;
            if (opcode.OperandType == OperandType.InlineField || opcode.OperandType == OperandType.InlineMethod)
            {
                if (position + 4 > bytes.Length) throw new InvalidOperationException("Truncated fake token.");
                int token = BitConverter.ToInt32(bytes, position);
                position += 4;
                operand = opcode.OperandType == OperandType.InlineField
                    ? (object)method.Module.ResolveField(token) : method.Module.ResolveMethod(token);
            }
            else if (opcode.OperandType != OperandType.InlineNone)
                throw new InvalidOperationException("Unsupported fake instruction operand.");
            decoded.Add(new CalendarInstruction(opcode, operand));
        }
        return decoded.ToArray();
    }

    private static bool GuardedTick(SimulationManager fake, ref bool uncertain)
    {
        if (uncertain) return false;
        try { return fake.Tick(); }
        catch (ArgumentOutOfRangeException) { uncertain = true; return false; }
        catch (OverflowException) { uncertain = true; return false; }
    }

    public static void Run()
    {
        checks = 0;
        Type type = typeof(SimulationManager);
        MethodInfo method = type.GetMethod("SimulationStep", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Check(type.Assembly == typeof(FakeIlProbeChecks).Assembly && method != null && method.DeclaringType == type, "target is the directly bound synthetic type and method");
        CalendarInstruction[] body = DecodeFakeMethod(method);
        CalendarMatch match = CalendarIlMatcher.Find(type, method, body);
        Check(match.Status == CalendarMatchStatus.Unique, "matcher recognizes real synthetic method body");
        MethodInfo expected = typeof(DateTime).GetMethod("op_Addition", new Type[] { typeof(DateTime), typeof(TimeSpan) });
        Check(body[match.AdditionInstructionIndex].OpCode.Equals(OpCodes.Call) && expected.Equals(body[match.AdditionInstructionIndex].Operand), "index identifies synthetic date addition");
        List<CalendarInstruction> modified = new List<CalendarInstruction>(body);
        modified.Add(new CalendarInstruction(OpCodes.Ldflda, typeof(SimulationMetaData).GetField("m_currentDateTime")));
        Check(CalendarIlMatcher.Find(type, method, modified.ToArray()).Status == CalendarMatchStatus.Ambiguous, "extra synthetic date address is rejected");

        DateTime start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        SimulationManager fake = new SimulationManager(start, TimeSpan.FromSeconds(1));
        bool monotonic = true;
        for (int i = 0; i < 3; i++)
        {
            DateTime before = fake.m_metaData.m_currentDateTime;
            fake.Tick();
            if (fake.m_metaData.m_currentDateTime < before || fake.FrameToTime(fake.m_currentFrameIndex) != fake.m_metaData.m_currentDateTime) monotonic = false;
        }
        Check(monotonic && fake.m_metaData.m_currentDateTime == start.AddSeconds(3), "unpatched synthetic frames keep date and conversion coherent");
        Check(fake.m_metaData.m_currentDateTime.Kind == DateTimeKind.Utc, "unpatched synthetic date retains kind");
        fake.Paused = true;
        uint frame = fake.m_currentFrameIndex;
        DateTime date = fake.m_metaData.m_currentDateTime;
        Check(!fake.Tick() && fake.m_currentFrameIndex == frame && fake.m_metaData.m_currentDateTime == date, "synthetic pause skips the frame");

        SimulationManager limit = new SimulationManager(DateTime.MaxValue, TimeSpan.FromTicks(1));
        bool uncertain = false;
        Check(!GuardedTick(limit, ref uncertain) && uncertain && limit.m_currentFrameIndex == 1U && limit.m_metaData.m_currentDateTime == DateTime.MaxValue, "synthetic overflow marks partially advanced frame uncertain");
        uint afterFailure = limit.m_currentFrameIndex;
        Check(!GuardedTick(limit, ref uncertain) && limit.m_currentFrameIndex == afterFailure, "uncertain synthetic session refuses another tick");
        Console.WriteLine("TOTAL: " + checks);
    }
}
