// Signature: OpenAI-Codex_native_validation_options
// Reflection-only entry point. It remains compilable before the host exists.
using System;
using System.Reflection;

internal static class HostPresenceChecks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Controlled host: " + message);
    }

    private static MethodInfo Required(Type owner, string name, Type result, params Type[] parameters)
    {
        MethodInfo method = owner.GetMethod(name, Members, null, parameters, null);
        Check(method != null && method.ReturnType == result, "missing or incompatible method " + name);
        return method;
    }

    private static int Main()
    {
        try
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type host = assembly.GetType("RealisticCityTime.Diagnostics.ControlledReadOnlyHost", false);
            Check(host != null, "independent read-only host is missing");
            Type source = assembly.GetType("RealisticCityTime.Diagnostics.IExistingManagerSource", false);
            Type state = assembly.GetType("RealisticCityTime.Diagnostics.ControlledHostState", false);
            Type ui = assembly.GetType("RealisticCityTime.Diagnostics.ControlledDiagnosticUi", false);
            Type snapshot = assembly.GetType("RealisticCityTime.Diagnostics.ClockDiagnosticReadOnlySnapshot", false);
            Type helper = assembly.GetType("ICities.UIHelperBase", false);
            Type simulation = assembly.GetType("SimulationManager", false);
            Type loading = assembly.GetType("LoadingManager", false);
            Check(source != null && source.IsInterface && state != null && state.IsEnum,
                "manager-source or host-state contract is missing");
            Check(ui != null && snapshot != null && helper != null && simulation != null && loading != null,
                "UI or fake manager contract is missing");
            Check(host.GetConstructor(Members, null, new Type[] { source }, null) != null,
                "host cannot receive an existing-manager source");
            Check(ui.GetConstructor(Members, null, new Type[] { host }, null) != null,
                "UI cannot receive the independent host");
            Required(source, "TryGetExisting", typeof(bool), simulation.MakeByRefType(), loading.MakeByRefType());
            Required(host, "OnLevelLoaded", typeof(void), typeof(bool));
            Required(host, "BeginView", typeof(int));
            Required(host, "StartFromView", typeof(bool), typeof(int));
            Required(host, "StopFromView", typeof(bool), typeof(int));
            Required(host, "TryReadSnapshotFromView", snapshot, typeof(int));
            Required(host, "CanRefreshFromView", typeof(bool), typeof(int));
            Required(host, "RetryRevocationFromView", typeof(bool), typeof(int));
            Required(host, "OnLevelUnloading", typeof(void));
            Required(host, "OnReleased", typeof(void));
            Required(host, "RetryStop", typeof(bool));
            Required(host, "RetryRevocation", typeof(bool));
            Required(host, "BeforeFrame", typeof(bool));
            Required(host, "AfterFrame", typeof(bool));
            Required(host, "DisplayUpdate", typeof(bool));
            Required(ui, "Show", typeof(bool), helper);
            Check(host.GetMethod("StartFromManualAction", Members, null, Type.EmptyTypes, null) == null
                && host.GetMethod("StopFromManualAction", Members, null, Type.EmptyTypes, null) == null
                && host.GetMethod("TryReadSnapshot", Members, null, Type.EmptyTypes, null) == null,
                "host exposes a no-token start, stop, or snapshot");
            Check(host.GetProperty("State", Members) != null && host.GetProperty("InFlightTickets", Members) != null,
                "host progress state is missing");
            Console.WriteLine("PASS independent host contract is present; no game or real storage accessed");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }
}
