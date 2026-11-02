// Signature: OpenAI-Codex_Realistic_City_Time
using System;
using RealisticCityTime.Game;

internal static class GameClockStateTests
{
    private static void Require(bool condition) { if (!condition) throw new Exception("Game clock cache boundary failed"); }
    internal static void Run()
    {
        var state = new GameClockState();
        Require(state.CurrentGeneration == 0 && state.Latest == null);
        Require(!state.Publish(0, DateTime.MinValue, false, 0, 0));
        long first = state.Begin();
        Require(state.Latest == null);
        Require(state.Publish(first, null, null, null, null));
        Require(!state.Latest.GameTime.HasValue && !state.Latest.Paused.HasValue);
        Require(state.Publish(first, new DateTime(2020,1,1), true, 10, 100));
        Require(state.Latest.Sequence == 1 && state.Latest.Paused == true);
        state.End();
        Require(state.Latest == null && !state.Publish(first, null, null, null, null));
        long second = state.Begin();
        Require(second > first && !state.Publish(first, DateTime.MinValue, false, 0, 0));
        Require(state.Latest == null);
        Require(state.Publish(second, new DateTime(2020,1,2), false, 0, 200));
        Require(state.Latest.Generation == second && state.Latest.Sequence == 0);
        Require(state.Publish(second, null, null, null, null) && !state.Latest.GameTime.HasValue);
        Console.WriteLine("PASS game clock cache lifecycle checks (not native callback integration)");
    }
}
