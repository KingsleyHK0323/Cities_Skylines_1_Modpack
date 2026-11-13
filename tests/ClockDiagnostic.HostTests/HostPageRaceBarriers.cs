// Signature: OpenAI-Codex_native_validation_options
// Used only by an explicitly instrumented, reversible copy of the host UI source.
using System;

internal static class HostPageRaceBarriers
{
    internal static Action BeforeStartCall;
    internal static Action<bool> AfterStartCall;
    internal static Action BeforeStopCall;
    internal static Action BeforeRefreshSnapshot;
    internal static Action<string> AfterRefreshSnapshot;
    internal static Action BeforeResultSetter;

    internal static void BeforeStart()
    {
        Action callback = BeforeStartCall;
        if (callback != null) callback();
    }

    internal static void AfterStart(bool started)
    {
        Action<bool> callback = AfterStartCall;
        if (callback != null) callback(started);
    }

    internal static void BeforeStop()
    {
        Action callback = BeforeStopCall;
        if (callback != null) callback();
    }

    internal static void BeforeRefresh()
    {
        Action callback = BeforeRefreshSnapshot;
        if (callback != null) callback();
    }

    internal static void AfterRefresh(string text)
    {
        Action<string> callback = AfterRefreshSnapshot;
        if (callback != null) callback(text);
    }

    internal static string AfterRefreshReturn(string text)
    {
        AfterRefresh(text);
        return text;
    }

    internal static void BeforeResult()
    {
        Action callback = BeforeResultSetter;
        if (callback != null) callback();
    }
}
