// Signature: OpenAI-Codex
// These checks use only synthetic callbacks and do not reference game assemblies.
using System;
using System.Reflection;

namespace RealisticCityTime.Diagnostics.RelativeEventTrace
{
    internal static class RelativeEventRecorderChecks
    {
        private static int checks;

        private static void Check(bool condition, string name)
        {
            checks++;
            if (!condition) throw new InvalidOperationException(name);
        }

        private static void StartsDisabledAndRejectsObservations()
        {
            Type type = Assembly.GetExecutingAssembly().GetType(
                "RealisticCityTime.Diagnostics.RelativeEventTrace.RelativeEventRecorder", false);
            Check(type != null, "The isolated relative event recorder is missing.");
            object recorder = Activator.CreateInstance(type, new object[] { 8, 4 });
            PropertyInfo state = type.GetProperty("State");
            MethodInfo observe = type.GetMethod("Observe");
            Check(state != null && observe != null, "The recorder contract is missing.");
            Check(state.GetValue(recorder, null).ToString() == "Disabled",
                "A new recorder must be disabled.");
            ParameterInfo[] parameters = observe.GetParameters();
            object kind = Enum.Parse(parameters[0].ParameterType, "BeforeSimulationFrame");
            object point = Enum.Parse(parameters[1].ParameterType, "Enter");
            object outcome = observe.Invoke(recorder, new object[] { kind, point });
            Check(outcome.ToString() == "NotRecording",
                "A disabled recorder must reject an observation.");
        }

        public static int Main()
        {
            try
            {
                StartsDisabledAndRejectsObservations();
                checks += RelativeEventRecorderBehaviorChecks.Run();
                Console.WriteLine("PASS: " + checks + " synthetic checks");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("FAIL: " + error.Message);
                return 1;
            }
        }
    }
}
