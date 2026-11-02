// Signature: OpenAI-Codex_Wireless_Utilities_And_Global_Coverage
using System;
using ICities;
using WirelessUtilitiesAndGlobalCoverage;

internal static class EntryPointChecks
{
    private static int passed;

    private static int Main()
    {
        try
        {
            Check("real IUserMod identity and inactive scope", CheckIdentity);
            Check("all load modes keep editors inactive", CheckLoadModes);
            Check("duplicate load preserves the current session", CheckDuplicateLoad);
            Check("unload resets the session and is repeatable", CheckUnload);
            Check("release resets a loaded session", CheckRelease);
            Check("created callback resets a reused extension", CheckCreated);
            Check("a new city gets a new session", CheckNewSession);
            Check("editor transition clears a previous gameplay session", CheckEditorTransition);
            Check("report failure does not escape the load callback", CheckLoadReportFailure);
            Check("report failure does not prevent unload cleanup", CheckUnloadReportFailure);
            Check("report failure does not prevent release cleanup", CheckReleaseReportFailure);
            Check("separate extension instances keep independent state", CheckIsolation);
            Console.WriteLine("PASS: " + passed + " entry-point checks. No gameplay validation.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void Check(string name, Action action)
    {
        action();
        passed++;
        Console.WriteLine("PASS: " + name);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static WirelessUtilitiesLoadingExtension Create()
    {
        return new WirelessUtilitiesLoadingExtension(delegate(string message) { });
    }

    private static WirelessUtilitiesLoadingExtension CreateWithFailingReporter()
    {
        return new WirelessUtilitiesLoadingExtension(delegate(string message)
        {
            throw new InvalidOperationException("Synthetic reporting failure.");
        });
    }

    private static void RequireInactive(WirelessUtilitiesLoadingExtension extension)
    {
        Require(!extension.IsGameplayLoaded, "Gameplay state was not cleared.");
        Require(extension.SessionId == Guid.Empty, "Session identity was not cleared.");
    }

    private static void CheckIdentity()
    {
        IUserMod mod = new WirelessUtilitiesMod();
        Require(mod.Name == "Wireless Utilities And Global Coverage", "Incorrect module name.");
        Require(mod.Description.IndexOf("inactive", StringComparison.OrdinalIgnoreCase) >= 0,
            "The inactive scope must be visible in the description.");
        Require(!ModuleIdentity.HasDevTool, "The foundation must not claim a DevTool.");
        Require(!ModuleIdentity.HasUtilityControl, "Utility control must remain inactive.");
        Require(mod.Name.IndexOf("This version has DevTool", StringComparison.Ordinal) < 0,
            "The foundation must not use the complete development-version label.");
    }

    private static void CheckLoadModes()
    {
        foreach (LoadMode mode in Enum.GetValues(typeof(LoadMode)))
        {
            WirelessUtilitiesLoadingExtension extension = Create();
            extension.OnLevelLoaded(mode);
            bool expected = mode == LoadMode.NewGame || mode == LoadMode.LoadGame
                || mode == LoadMode.NewGameFromScenario;
            Require(extension.IsGameplayLoaded == expected, "Wrong load-mode behavior: " + mode);
            Require((extension.SessionId != Guid.Empty) == expected, "Wrong session: " + mode);
        }
        WirelessUtilitiesLoadingExtension unknown = Create();
        unknown.OnLevelLoaded((LoadMode)int.MaxValue);
        RequireInactive(unknown);
    }

    private static void CheckDuplicateLoad()
    {
        WirelessUtilitiesLoadingExtension extension = Create();
        extension.OnLevelLoaded(LoadMode.NewGame);
        Guid session = extension.SessionId;
        extension.OnLevelLoaded(LoadMode.NewGame);
        Require(session != Guid.Empty && extension.SessionId == session, "Duplicate load changed session.");
    }

    private static void CheckUnload()
    {
        WirelessUtilitiesLoadingExtension extension = Create();
        extension.OnLevelLoaded(LoadMode.LoadGame);
        extension.OnLevelUnloading();
        RequireInactive(extension);
        extension.OnLevelUnloading();
        RequireInactive(extension);
    }

    private static void CheckRelease()
    {
        WirelessUtilitiesLoadingExtension extension = Create();
        extension.OnLevelLoaded(LoadMode.LoadGame);
        extension.OnReleased();
        extension.OnReleased();
        RequireInactive(extension);
    }

    private static void CheckCreated()
    {
        WirelessUtilitiesLoadingExtension extension = Create();
        extension.OnLevelLoaded(LoadMode.LoadGame);
        extension.OnCreated(null);
        RequireInactive(extension);
    }

    private static void CheckNewSession()
    {
        WirelessUtilitiesLoadingExtension extension = Create();
        extension.OnLevelLoaded(LoadMode.NewGame);
        Guid previous = extension.SessionId;
        extension.OnLevelUnloading();
        extension.OnLevelLoaded(LoadMode.LoadGame);
        Require(previous != Guid.Empty && extension.SessionId != Guid.Empty
            && previous != extension.SessionId, "A new city reused the old session.");
    }

    private static void CheckEditorTransition()
    {
        WirelessUtilitiesLoadingExtension extension = Create();
        extension.OnLevelLoaded(LoadMode.LoadGame);
        extension.OnLevelLoaded(LoadMode.LoadMap);
        RequireInactive(extension);
    }

    private static void CheckLoadReportFailure()
    {
        WirelessUtilitiesLoadingExtension extension = CreateWithFailingReporter();
        extension.OnLevelLoaded(LoadMode.LoadGame);
        Require(extension.IsGameplayLoaded && extension.SessionId != Guid.Empty,
            "Reporting failure corrupted the load state.");
    }

    private static void CheckUnloadReportFailure()
    {
        WirelessUtilitiesLoadingExtension extension = CreateWithFailingReporter();
        extension.OnLevelLoaded(LoadMode.LoadGame);
        extension.OnLevelUnloading();
        RequireInactive(extension);
    }

    private static void CheckReleaseReportFailure()
    {
        WirelessUtilitiesLoadingExtension extension = CreateWithFailingReporter();
        extension.OnLevelLoaded(LoadMode.LoadGame);
        extension.OnReleased();
        RequireInactive(extension);
    }

    private static void CheckIsolation()
    {
        WirelessUtilitiesLoadingExtension first = Create();
        WirelessUtilitiesLoadingExtension second = Create();
        first.OnLevelLoaded(LoadMode.LoadGame);
        second.OnLevelLoaded(LoadMode.LoadGame);
        Guid secondSession = second.SessionId;
        first.OnReleased();
        RequireInactive(first);
        Require(second.IsGameplayLoaded && second.SessionId == secondSession,
            "One instance modified another instance's session.");
    }
}
