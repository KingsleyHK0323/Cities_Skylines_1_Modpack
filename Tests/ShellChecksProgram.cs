// Signature: OpenAI-Codex
using System;
using ICities;
using RealisticBorderControl.Game;

static class ShellChecksProgram
{
    static int count;
    static void Check(bool ok) { if (!ok) throw new Exception("Shell check failed: " + (count + 1)); count++; }
    static void Main()
    {
        var mod = new BorderControlMod();
        var loader = new BorderControlLoading();
        mod.OnDisabled();
        Check(!BorderControlLifecycle.Enabled && !BorderControlLifecycle.CityLoaded);
        loader.OnLevelLoaded(LoadMode.LoadGame);
        Check(!BorderControlLifecycle.CityLoaded);
        mod.OnEnabled();
        loader.OnLevelLoaded(LoadMode.LoadGame);
        Check(BorderControlLifecycle.CityLoaded);
        mod.OnEnabled();
        Check(BorderControlLifecycle.CityLoaded);
        loader.OnLevelUnloading();
        loader.OnLevelUnloading();
        Check(BorderControlLifecycle.Enabled && !BorderControlLifecycle.CityLoaded);
        loader.OnLevelLoaded(LoadMode.NewGame);
        Check(BorderControlLifecycle.CityLoaded);
        loader.OnReleased();
        loader.OnReleased();
        Check(BorderControlLifecycle.Enabled && !BorderControlLifecycle.CityLoaded);
        loader.OnLevelLoaded(LoadMode.NewMap);
        Check(!BorderControlLifecycle.CityLoaded);
        loader.OnLevelLoaded((LoadMode)int.MaxValue);
        Check(!BorderControlLifecycle.CityLoaded);
        loader.OnLevelLoaded(LoadMode.LoadGame);
        mod.OnDisabled();
        Check(!BorderControlLifecycle.Enabled && !BorderControlLifecycle.CityLoaded);
        Check(mod.Name == "Realistic Border Control");
        Check(mod.Description.Contains("inactive"));
        Console.WriteLine(count + " synthetic shell checks passed; real ICities compilation and game loading remain unverified.");
    }
}
