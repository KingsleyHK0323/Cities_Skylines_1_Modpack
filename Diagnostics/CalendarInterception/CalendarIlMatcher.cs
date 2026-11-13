// Isolated structural matcher; it neither installs a patch nor changes instructions.
// A unique result describes only the supplied snapshot, not provenance or runtime execution.
// The caller must keep its own input unchanged; the internal clone is not returned.
// The adapter must retain that snapshot and independently validate labels and exception blocks.
// Signature: OpenAI-Codex
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;

namespace RealisticCityTime.CalendarInterception
{
    public sealed class CalendarInstruction
    {
        public readonly OpCode OpCode;
        public readonly object Operand;

        public CalendarInstruction(OpCode opCode, object operand)
        {
            OpCode = opCode;
            Operand = operand;
        }
    }

    public enum CalendarMatchStatus
    {
        InvalidInput,
        InvalidTarget,
        NoMatch,
        Ambiguous,
        Unique
    }

    public sealed class CalendarMatch
    {
        public readonly CalendarMatchStatus Status;
        public readonly int AdditionInstructionIndex;

        internal CalendarMatch(CalendarMatchStatus status, int index)
        {
            Status = status;
            AdditionInstructionIndex = index;
        }
    }

    public static class CalendarIlMatcher
    {
        private const BindingFlags DeclaredFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        private const BindingFlags DeclaredMethods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public static CalendarMatch Find(Type trustedManagerType, MethodInfo original, CalendarInstruction[] instructions)
        {
            if (instructions == null) return Result(CalendarMatchStatus.InvalidInput, -1);
            CalendarInstruction[] snapshot = (CalendarInstruction[])instructions.Clone();
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i] == null || snapshot[i].OpCode.Size == 0)
                    return Result(CalendarMatchStatus.InvalidInput, -1);
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i].OpCode.OpCodeType == OpCodeType.Nternal) return Result(CalendarMatchStatus.InvalidInput, -1);
                if (!HasSupportedOperand(snapshot[i])) return Result(CalendarMatchStatus.InvalidInput, -1);
            }

            try
            {
                if (trustedManagerType == null || original == null || trustedManagerType.FullName != "SimulationManager")
                    return Result(CalendarMatchStatus.InvalidTarget, -1);
                if (original.DeclaringType != trustedManagerType || original.Name != "SimulationStep" ||
                    !original.IsPrivate || original.IsStatic || original.IsAbstract || original.IsGenericMethod ||
                    original.ContainsGenericParameters || original.ReturnType != typeof(void) || original.GetParameters().Length != 0 ||
                    (original.CallingConvention & CallingConventions.HasThis) == 0 ||
                    (original.CallingConvention & (CallingConventions.ExplicitThis | CallingConventions.VarArgs)) != 0)
                    return Result(CalendarMatchStatus.InvalidTarget, -1);

                int namedMethods = 0;
                MethodInfo[] methods = trustedManagerType.GetMethods(DeclaredMethods);
                for (int i = 0; i < methods.Length; i++)
                    if (methods[i].Name == "SimulationStep") namedMethods++;
                if (namedMethods != 1) return Result(CalendarMatchStatus.InvalidTarget, -1);

                FieldInfo metadata = trustedManagerType.GetField("m_metaData", DeclaredFields);
                FieldInfo timePerFrame = trustedManagerType.GetField("m_timePerFrame", DeclaredFields);
                if (!IsPublicInstanceField(metadata, trustedManagerType) || !IsPublicInstanceField(timePerFrame, trustedManagerType) ||
                    timePerFrame.FieldType != typeof(TimeSpan))
                    return Result(CalendarMatchStatus.InvalidTarget, -1);
                Type metadataType = metadata.FieldType;
                if (metadataType.FullName != "SimulationMetaData" || metadataType.IsValueType ||
                    metadataType.Assembly != trustedManagerType.Assembly)
                    return Result(CalendarMatchStatus.InvalidTarget, -1);
                FieldInfo currentDate = metadataType.GetField("m_currentDateTime", DeclaredFields);
                if (!IsPublicInstanceField(currentDate, metadataType) || currentDate.IsInitOnly || currentDate.IsLiteral ||
                    currentDate.FieldType != typeof(DateTime))
                    return Result(CalendarMatchStatus.InvalidTarget, -1);
                MethodInfo addition = typeof(DateTime).GetMethod("op_Addition", BindingFlags.Public | BindingFlags.Static, null,
                    new Type[] { typeof(DateTime), typeof(TimeSpan) }, null);
                if (addition == null || addition.ReturnType != typeof(DateTime))
                    return Result(CalendarMatchStatus.InvalidTarget, -1);

                int dateAdditions = 0;
                int dateStores = 0;
                for (int i = 0; i < snapshot.Length; i++)
                {
                    if (IsMember(snapshot[i], OpCodes.Call, addition)) dateAdditions++;
                    if (IsMember(snapshot[i], OpCodes.Ldflda, currentDate)) return Result(CalendarMatchStatus.Ambiguous, -1);
                    if (IsMember(snapshot[i], OpCodes.Stfld, currentDate)) dateStores++;
                }
                if (dateAdditions > 1 || dateStores > 1) return Result(CalendarMatchStatus.Ambiguous, -1);

                int found = -1;
                for (int i = 0; i <= snapshot.Length - 8; i++)
                {
                    if (IsNoOperand(snapshot[i], OpCodes.Ldarg_0) &&
                        IsMember(snapshot[i + 1], OpCodes.Ldfld, metadata) &&
                        IsNoOperand(snapshot[i + 2], OpCodes.Dup) &&
                        IsMember(snapshot[i + 3], OpCodes.Ldfld, currentDate) &&
                        IsNoOperand(snapshot[i + 4], OpCodes.Ldarg_0) &&
                        IsMember(snapshot[i + 5], OpCodes.Ldfld, timePerFrame) &&
                        IsMember(snapshot[i + 6], OpCodes.Call, addition) &&
                        IsMember(snapshot[i + 7], OpCodes.Stfld, currentDate))
                    {
                        if (found >= 0) return Result(CalendarMatchStatus.Ambiguous, -1);
                        found = i + 6;
                    }
                }
                return found < 0 ? Result(CalendarMatchStatus.NoMatch, -1) : Result(CalendarMatchStatus.Unique, found);
            }
            catch (AmbiguousMatchException) { return Result(CalendarMatchStatus.InvalidTarget, -1); }
            catch (NotSupportedException) { return Result(CalendarMatchStatus.InvalidTarget, -1); }
            catch (TypeLoadException) { return Result(CalendarMatchStatus.InvalidTarget, -1); }
            catch (SecurityException) { return Result(CalendarMatchStatus.InvalidTarget, -1); }
        }

        private static bool HasSupportedOperand(CalendarInstruction instruction)
        {
            OperandType kind = instruction.OpCode.OperandType;
            object operand = instruction.Operand;
            if (kind == OperandType.InlineNone) return operand == null;
            if (operand == null) return false;
            if (kind == OperandType.InlineField) return operand is FieldInfo;
            if (kind == OperandType.InlineMethod) return operand is MethodBase;
            if (kind == OperandType.InlineType) return operand is Type;
            if (kind == OperandType.InlineTok) return operand is Type || operand is FieldInfo || operand is MethodBase;
            if (kind == OperandType.InlineBrTarget || kind == OperandType.ShortInlineBrTarget) return operand is Label;
            if (kind == OperandType.InlineSwitch) return operand is Label[];
            if (kind == OperandType.InlineI) return operand is int;
            if (kind == OperandType.InlineI8) return operand is long;
            if (kind == OperandType.ShortInlineI) return operand is sbyte || operand is byte || operand is int;
            if (kind == OperandType.InlineR) return operand is double;
            if (kind == OperandType.ShortInlineR) return operand is float;
            if (kind == OperandType.InlineString) return operand is string;
            if (kind == OperandType.InlineVar || kind == OperandType.ShortInlineVar)
                return operand is LocalBuilder || operand is LocalVariableInfo || operand is byte || operand is sbyte || operand is short || operand is ushort || operand is int;
            // InlinePhi and reserved or unknown operand kinds are unsupported.
            if ((int)kind == 6 || (int)kind == 8 || (int)kind < 0 || (int)kind > 18) return false;
            if (kind == OperandType.InlineSig) return false;
            return true;
        }

        private static bool IsPublicInstanceField(FieldInfo field, Type declaringType)
        {
            return field != null && field.IsPublic && !field.IsStatic && field.DeclaringType == declaringType;
        }

        private static bool IsNoOperand(CalendarInstruction instruction, OpCode opcode)
        {
            return instruction.OpCode.Equals(opcode) && instruction.Operand == null;
        }

        private static bool IsMember(CalendarInstruction instruction, OpCode opcode, MemberInfo member)
        {
            return instruction.OpCode.Equals(opcode) && member.Equals(instruction.Operand);
        }

        private static CalendarMatch Result(CalendarMatchStatus status, int index)
        {
            return new CalendarMatch(status, index);
        }
    }
}
