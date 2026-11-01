using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

// Signature: OpenAI-Codex_Automated_Public_Transport_Planning
internal static class CompiledLifecycleChecks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static void Run()
    {
        // The workflow builds this DLL first and invokes checks from the repository root.
        string path = Path.GetFullPath("bin/Release/AutomatedPublicTransportPlanning.dll");
        Require(File.Exists(path), "compiled Development DLL must exist before lifecycle checks");
        Assembly module = Assembly.LoadFrom(path);
        Type gateType = module.GetType("AutomatedPublicTransportPlanning.Planning.RouteEditRequestGate", true);
        object gate = Activator.CreateInstance(gateType, true);
        object old = Begin(gateType, gate);
        object current = null;
        int errors = 0;
        int cleanups = 0;
        Action<Exception> failed = delegate(Exception error) { errors++; };
        Action oldFinished = delegate { cleanups++; Invoke(gateType, gate, "Complete", old); };
        Action work = delegate
        {
            Invoke(gateType, gate, "Reset", new Action(delegate { }));
            current = Begin(gateType, gate);
            throw new InvalidOperationException("obsolete compiled worker");
        };
        Invoke(gateType, gate, "RunOwned", old, work, failed, oldFinished);
        Require(errors == 0 && cleanups == 0 && (bool)Invoke(gateType, gate, "IsOwner", current),
            "compiled runner must suppress old exception/finally after replacement during work");
        bool obsoleteStarted = false;
        Invoke(gateType, gate, "RunOwned", old, new Action(delegate { obsoleteStarted = true; }), failed, oldFinished);
        Require(!obsoleteStarted && errors == 0 && cleanups == 0,
            "compiled runner must reject a delayed obsolete queued action");
        Invoke(gateType, gate, "RunOwned", current,
            new Action(delegate { throw new InvalidOperationException("current compiled worker"); }), failed,
            new Action(delegate { cleanups++; Invoke(gateType, gate, "Complete", current); }));
        Require(errors == 1 && cleanups == 1 && !(bool)gateType.GetProperty("IsBusy").GetValue(gate, null),
            "compiled runner must retain current failure and cleanup behavior");

        // This is compiled call-site evidence, separate from the behavioral checks above.
        MethodInfo runner = gateType.GetMethod("RunOwned", Members);
        Require(HasRunnerCall(module, "PlanningCapture", runner), "compiled preview callback must call RunOwned");
        Require(HasRunnerCall(module, "RouteCreationController", runner), "compiled creation callback must call RunOwned");
    }

    private static object Begin(Type gateType, object gate)
    {
        object[] args = new object[] { null };
        Require((bool)gateType.GetMethod("TryBegin", Members).Invoke(gate, args), "compiled gate must accept request");
        return args[0];
    }

    private static object Invoke(Type type, object instance, string name, params object[] args)
    {
        return type.GetMethod(name, Members).Invoke(instance, args);
    }

    private static bool HasRunnerCall(Assembly module, string controller, MethodInfo runner)
    {
        string root = "AutomatedPublicTransportPlanning.Planning." + controller;
        Dictionary<short, OpCode> codes = new Dictionary<short, OpCode>();
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(OpCode))
            {
                OpCode code = (OpCode)field.GetValue(null);
                codes[code.Value] = code;
            }
        }
        List<Type> types = new List<Type>();
        types.Add(module.GetType(root, true));
        for (int index = 0; index < types.Count; index++)
        {
            Type type = types[index];
            types.AddRange(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic));
            foreach (MethodInfo method in type.GetMethods(Members))
            {
                MethodBody body = method.GetMethodBody();
                if (body == null) { continue; }
                byte[] il = body.GetILAsByteArray();
                for (int offset = 0; offset < il.Length; )
                {
                    short value = il[offset++];
                    if (value == 0xfe) { value = (short)(0xfe00 | il[offset++]); }
                    OpCode code = codes[value];
                    if (code.OperandType == OperandType.InlineMethod)
                    {
                        if (method.Module == runner.Module &&
                            (code.Value == OpCodes.Callvirt.Value || code.Value == OpCodes.Call.Value) &&
                            BitConverter.ToInt32(il, offset) == runner.MetadataToken)
                        { return true; }
                    }
                    offset += OperandSize(code.OperandType, il, offset);
                }
            }
        }
        return false;
    }

    private static int OperandSize(OperandType type, byte[] il, int offset)
    {
        switch (type)
        {
            case OperandType.InlineNone: return 0;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar: return 1;
            case OperandType.InlineVar: return 2;
            case OperandType.InlineI8:
            case OperandType.InlineR: return 8;
            case OperandType.InlineSwitch: return 4 + 4 * BitConverter.ToInt32(il, offset);
            default: return 4;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException(message); }
    }
}
