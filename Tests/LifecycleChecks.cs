// Signature: OpenAI-Codex
using System;
using PedestrianTrafficControl;
internal static class LifecycleChecks {
    internal static int Run() {
        int checks = 0;
        PedestrianRuntime runtime = new PedestrianRuntime();
        Check(!runtime.Enabled && !runtime.HasSession, "runtime starts disabled", ref checks);
        runtime.Apply(LifecycleEvent.CityLoaded);
        Check(!runtime.HasSession, "disabled load stays inactive", ref checks);
        runtime.Apply(LifecycleEvent.Enable);
        Check(runtime.Enabled && !runtime.HasSession, "enable alone does not invent loaded city", ref checks);
        runtime.Apply(LifecycleEvent.CityLoaded);
        Check(runtime.HasSession, "enabled city gets lifetime only", ref checks);
        runtime.Apply(LifecycleEvent.Unload);
        Check(!runtime.HasSession, "unload closes lifetime", ref checks);
        runtime.Apply(LifecycleEvent.CityLoaded);
        runtime.Apply(LifecycleEvent.OtherLevelLoaded);
        Check(!runtime.HasSession, "editor does not keep city session", ref checks);
        runtime.Apply(LifecycleEvent.CityLoaded);
        runtime.Apply(LifecycleEvent.Disable);
        Check(!runtime.Enabled && !runtime.HasSession, "disable invalidates session", ref checks);
        runtime.Apply(LifecycleEvent.Release);
        runtime.Apply(LifecycleEvent.Release);
        Check(!runtime.HasSession, "repeated release stays inactive", ref checks);
        runtime.Apply(LifecycleEvent.Enable);
        runtime.Apply(LifecycleEvent.CityLoaded);
        Check(runtime.HasSession, "error-isolation fixture begins with active lifetime", ref checks);
        bool okay = LifecycleGuard.Run(delegate { throw new InvalidOperationException("fixture"); }, delegate { runtime.Stop(); });
        Check(!okay && !runtime.HasSession, "callback errors isolated", ref checks);
        Check(!LifecycleGuard.Run(delegate { throw new Exception("fixture"); }, delegate { throw new Exception("stop fixture"); }),
            "cleanup errors also isolated", ref checks);
        return checks;
    }
    private static void Check(bool value, string name, ref int checks) {
        if (!value) throw new Exception("FAIL: " + name);
        checks++; Console.WriteLine("PASS: " + name);
    }
}
