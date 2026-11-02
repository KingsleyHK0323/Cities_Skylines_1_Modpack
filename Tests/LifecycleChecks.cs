// OpenAI-Codex_Intercity_Resource_Sharing
using System;
using ICities;
using IntercityResourceSharing;
internal static class LifecycleChecks
{
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception(name); }
    private static void Main()
    {
        var loading = new Loading();
        Check(!loading.IsActive, "initially inactive");
        loading.OnLevelLoaded(LoadMode.NewGame);
        Check(loading.IsActive, "new game");
        loading.OnLevelUnloading(); loading.OnLevelUnloading();
        Check(!loading.IsActive, "idempotent unload");
        loading.OnLevelLoaded(LoadMode.LoadGame);
        Check(loading.IsActive, "reload");
        loading.OnLevelLoaded(LoadMode.NewMap);
        Check(!loading.IsActive, "editor clears state");
        loading.OnLevelLoaded((LoadMode)999);
        Check(!loading.IsActive, "unknown mode");
        loading.OnLevelLoaded(LoadMode.LoadGame); loading.OnReleased();
        Check(!loading.IsActive, "release clears state");
        var other = new Loading(); other.OnLevelLoaded(LoadMode.NewGame);
        loading.OnReleased();
        Check(other.IsActive, "instances isolated");
        var state = new Lifecycle();
        state.Begin(true, delegate { throw new InvalidOperationException("synthetic"); });
        Check(!state.Active && state.Failed, "initialization failure isolated");
        state.End();
        Check(!state.Active && !state.Failed, "failure cleared on unload");
        state.Begin(true, null);
        Check(state.Active && !state.Failed, "recovery");
        IUserMod mod = new Mod();
        Check(mod.Name == "Intercity Resource Sharing", "non-DevTool identity");
        Console.WriteLine("Lifecycle boundary checks passed; real ICities and game runtime unverified.");
    }
}
