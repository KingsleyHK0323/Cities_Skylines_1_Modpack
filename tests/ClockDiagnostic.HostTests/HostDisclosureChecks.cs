// Signature: OpenAI-Codex_native_validation_options
// Verifies only text displayed by an in-memory fake UI; no real game or UI is accessed.
using System;
using RealisticCityTime.Diagnostics;

internal static class HostDisclosureChecks
{
    private sealed class Source : IExistingManagerSource
    {
        internal int Calls;
        public bool TryGetExisting(out SimulationManager simulation, out LoadingManager loading)
        {
            Calls++;
            simulation = null;
            loading = null;
            return false;
        }
    }

    private static bool Contains(string text, string value)
    {
        return text != null && text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Controlled host disclosure: " + message);
    }

    private static void CheckDisclosure(string text)
    {
        Check(Contains(text, "city") && Contains(text, "lifecycle") && Contains(text, "unverified"),
            "city and lifecycle uncertainty was omitted");
        Check(Contains(text, "8 frame") && Contains(text, "8 display"), "record limits were omitted");
        Check(Contains(text, "first display") && Contains(text, "baseline"), "display baseline was omitted");
        Check(Contains(text, "does not stop"), "non-automatic stop was omitted");
        Check(Contains(text, "frequency") && Contains(text, "not recorded"), "counter frequency limit was omitted");
        Check(Contains(text, "retain") && Contains(text, "forward"), "host copy or forwarding risk was omitted");
        Check(Contains(text, "GameMod") && Contains(text, "absolute") && Contains(text, "date"),
            "separate date-reading observer was omitted");
    }

    private static int Main()
    {
        try
        {
            Source source = new Source();
            ControlledReadOnlyHost host = new ControlledReadOnlyHost(source);
            host.OnLevelLoaded(true);
            HostUiFake helper = new HostUiFake();
            Check(new ControlledDiagnosticUi(host).Show(helper) && helper.Fields.Count == 2,
                "fake UI did not initialize");
            Check(source.Calls == 0, "display construction looked up a manager");
            Check(Contains(helper.Fields[0].text, "Ready") && Contains(helper.Fields[0].text, "snapshot")
                && Contains(helper.Fields[0].text, "Check status"), "status snapshot and refresh instruction were omitted");
            CheckDisclosure(helper.Fields[0].text);
            CheckDisclosure(helper.Fields[1].text);
            Console.WriteLine("PASS controlled host disclosure fake checks");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
