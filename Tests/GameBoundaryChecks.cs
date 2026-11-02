// Signature: OpenAI-Codex_Realistic_Import_Export_Control
using RealisticImportExportControl;

static class GameBoundaryChecks
{
    public static void Run(Action<bool, string> check)
    {
        var session = new CargoSession();
        check(!session.Active && !session.CanWrite, "game session starts inactive");
        check(session.Start(null) && session.Active && session.Status == "Game data source unavailable", "loaded shell reports unavailable data");
        session.Stop(); session.Stop();
        check(!session.Active && !session.CanWrite, "unload is idempotent and cannot dispatch");
        check(!session.Start(() => throw new Exception("private detail")) && !session.Active && session.Status == "Initialization failed", "initialization exception fails closed without leaking detail");
        var old = new CargoLoading(); old.OnLevelLoaded(ICities.LoadMode.NewGame);
        var next = new CargoLoading(); next.OnLevelLoaded(ICities.LoadMode.LoadGame);
        old.OnLevelUnloading();
        check(CargoRuntime.Current != null && CargoRuntime.Current.Active, "stale loader cannot clear newer city");
        next.OnReleased(); next.OnReleased();
        check(CargoRuntime.Current == null, "release clears owned session idempotently");
        next.OnLevelLoaded(ICities.LoadMode.NewGame);
        check(CargoRuntime.Current == null, "released extension cannot restart a session");
        var editor = new CargoLoading(); editor.OnLevelLoaded(ICities.LoadMode.NewMap);
        check(CargoRuntime.Current == null, "editor mode does not start city session");
        check(new CargoMod().Name == "Realistic Import Export Control", "game mod uses full English name");
    }
}
