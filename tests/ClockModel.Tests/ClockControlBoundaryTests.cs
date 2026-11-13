// Signature: OpenAI-Codex_Realistic_City_Time
// Additional ownership checks: OpenAI-Codex_city_time_development
// Synthetic freshness evidence: OpenAI-Codex_Realistic_City_Time
// Update signature: OpenAI-Codex
using System;
using System.Threading;
using RealisticCityTime.Game;

internal static class ClockControlBoundaryTests
{
    private static void Require(bool condition) { if (!condition) throw new Exception("Control dry-run boundary failed"); }
    private static void RequireOwner(bool condition, string scenario)
    {
        if (!condition) throw new Exception("Single preview owner: " + scenario);
    }

    private static void RunSingleOwnerChecks()
    {
        var state = new GameClockState();
        var first = new ClockControlBoundary(state);
        var second = new ClockControlBoundary(state);
        long token = state.Begin();
        state.Publish(token, new DateTime(2020, 1, 1), false, 1, 10);
        var one = first.Evaluate(state.Latest, 1m, 3, true, true, true);
        var two = second.Evaluate(state.Latest, 1m, 3, true, true, true);
        RequireOwner(!first.TryConsumeDryRun(one, false, true, true), "rejected candidate must not acquire ownership");
        RequireOwner(second.TryConsumeDryRun(two, true, true, true), "first successful consumer acquires ownership");
        RequireOwner(!first.TryConsumeDryRun(one, true, true, true), "same snapshot cannot be consumed by another boundary");
        state.Publish(token, new DateTime(2020, 1, 1), false, 2, 20);
        RequireOwner(first.Evaluate(state.Latest, 1m, 3, true, true, true).Status == ClockControlStatus.ConflictingOrUnknownWriter,
            "another boundary reports the in-session ownership conflict");
        RequireOwner(!first.TryConsumeDryRun(first.Evaluate(state.Latest, 1m, 3, true, true, true), true, true, true),
            "another boundary cannot take over on the next snapshot");
        RequireOwner(second.TryConsumeDryRun(second.Evaluate(state.Latest, 1m, 3, true, true, true), true, true, true),
            "the current owner can consume the next snapshot");
        state.End();
        token = state.Begin();
        state.Publish(token, new DateTime(2020, 1, 2), false, 1, 30);
        RequireOwner(first.TryConsumeDryRun(first.Evaluate(state.Latest, 1m, 3, true, true, true), true, true, true),
            "a new city can acquire a new owner");
        token = state.Begin();
        state.Publish(token, new DateTime(2020, 1, 3), false, 1, 40);
        RequireOwner(second.TryConsumeDryRun(second.Evaluate(state.Latest, 1m, 3, true, true, true), true, true, true),
            "a new generation clears ownership even without an explicit unload");

        var racingState = new GameClockState();
        var left = new ClockControlBoundary(racingState);
        var right = new ClockControlBoundary(racingState);
        long raceToken = racingState.Begin();
        racingState.Publish(raceToken, new DateTime(2020, 1, 1), false, 1, 10);
        var leftPlan = left.Evaluate(racingState.Latest, 1m, 3, true, true, true);
        var rightPlan = right.Evaluate(racingState.Latest, 1m, 3, true, true, true);
        var start = new ManualResetEvent(false);
        bool leftWon = false, rightWon = false;
        var leftThread = new Thread(() => { start.WaitOne(); leftWon = left.TryConsumeDryRun(leftPlan, true, true, true); }) { IsBackground = true };
        var rightThread = new Thread(() => { start.WaitOne(); rightWon = right.TryConsumeDryRun(rightPlan, true, true, true); }) { IsBackground = true };
        leftThread.Start();
        rightThread.Start();
        start.Set();
        RequireOwner(leftThread.Join(5000) && rightThread.Join(5000), "concurrent consumers must finish");
        start.Close();
        RequireOwner(leftWon != rightWon, "exactly one concurrent consumer acquires ownership");
        Console.WriteLine("PASS single dry-run owner and concurrent acquisition checks (no native writer)");
    }
    internal static void Run()
    {
        var state = new GameClockState();
        var boundary = new ClockControlBoundary(state);
        Require(boundary.Evaluate(null,1m,null,null,null,true).Status == ClockControlStatus.Inactive);
        long token = state.Begin();
        Require(boundary.Evaluate(null,1m,3,true,true,true).Status == ClockControlStatus.Unknown);
        state.Publish(token,new DateTime(2020,1,1),false,1,10);
        var sample = state.Latest;
        Require(boundary.Evaluate(sample,1.25m,null,true,true,true).Status == ClockControlStatus.PrecisionRequired);
        Require(boundary.Evaluate(sample,1.25m,1,true,true,true).Status == ClockControlStatus.InvalidMultiplier);
        Require(boundary.Evaluate(sample,0.9m,3,true,true,true).Status == ClockControlStatus.InvalidMultiplier);
        Require(boundary.Evaluate(sample,10.1m,3,true,true,true).Status == ClockControlStatus.InvalidMultiplier);
        Require(boundary.Evaluate(sample,1m,-1,true,true,true).Status == ClockControlStatus.InvalidMultiplier);
        Require(boundary.Evaluate(sample,1m,29,true,true,true).Status == ClockControlStatus.InvalidMultiplier);
        Require(boundary.Evaluate(sample,1m,3,null,true,true).Status == ClockControlStatus.UnverifiedBoundary);
        Require(boundary.Evaluate(sample,1m,3,true,null,true).Status == ClockControlStatus.ConflictingOrUnknownWriter);
        var plan = boundary.Evaluate(sample,1.25m,3,true,true,true);
        Require(plan.Status == ClockControlStatus.Candidate && !plan.CanWrite);
        Require(!boundary.TryConsumeDryRun(plan,false,true,true));
        Require(!new ClockControlBoundary(state).TryConsumeDryRun(plan,true,true,true));
        Require(boundary.TryConsumeDryRun(plan,true,true,true) && !plan.CanWrite);
        Require(!boundary.TryConsumeDryRun(plan,true,true,true));
        var sameSample = boundary.Evaluate(sample,2.5m,3,true,true,true);
        Require(!boundary.TryConsumeDryRun(sameSample,true,true,true));
        state.Publish(token,new DateTime(2020,1,1),false,2,20);
        Require(boundary.Evaluate(sample,1m,3,true,true,true).Status == ClockControlStatus.Stale);
        Require(!boundary.TryConsumeDryRun(plan,true,true,true));
        var next = boundary.Evaluate(state.Latest,10m,3,true,true,true);
        Require(boundary.TryConsumeDryRun(next,true,true,true));
        state.Publish(token,new DateTime(2020,1,1),true,3,30);
        Require(boundary.Evaluate(state.Latest,1m,3,true,true,true).Status == ClockControlStatus.Paused);
        state.Publish(token,null,null,null,null);
        Require(boundary.Evaluate(state.Latest,1m,3,true,true,true).Status == ClockControlStatus.Unknown);
        state.End();
        Require(!boundary.TryConsumeDryRun(next,true,true,true));
        token=state.Begin();
        state.Publish(token,new DateTime(2020,1,2),false,0,0);
        var newCity=boundary.Evaluate(state.Latest,1m,3,true,true,true);
        Require(boundary.TryConsumeDryRun(newCity,true,true,true) && !newCity.CanWrite);
        RunSingleOwnerChecks();
        Console.WriteLine("PASS control decision and one-shot dry-run checks (native writes always disabled)");
    }
}
