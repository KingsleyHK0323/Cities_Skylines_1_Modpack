// Signature: OpenAI-Codex
using System;
using System.Threading;
using RealisticCityTime.Game;

// These are in-memory fixtures, not a save format or an integration with the game.
internal static class SyntheticClockIntegrationTests
{
    private static readonly DateTime Start = new DateTime(2026, 1, 1, 23, 59, 59);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Synthetic clock: " + message);
    }

    private static GameClockSnapshot Publish(GameClockState state, long generation,
        DateTime? date, bool? paused, uint? frame, long? ticks)
    {
        Check(state.Publish(generation, date, paused, frame, ticks), "publish current sample");
        return state.Latest;
    }

    private static SyntheticClockPlan Prepare(SyntheticClockCoordinator coordinator,
        GameClockSnapshot sample, bool? continuous)
    {
        return coordinator.Prepare(sample, continuous, true, true, true);
    }

    private static bool FakeUnload(GameClockState state, long generation)
    {
        // This fixture owns the cache lock; the real game callback does not use this helper.
        lock (state.SyntheticGate)
        {
            if (state.CurrentGeneration != generation) return false;
            state.End();
            return true;
        }
    }

    private static void Accept(SyntheticClockCoordinator coordinator,
        SyntheticClockPlan plan, SyntheticClockStatus expected)
    {
        Check(plan.Status == expected, "expected " + expected + ", received " + plan.Status);
        Check(!plan.CanWrite, "a synthetic plan must never allow native writes");
        Check(coordinator.TryAccept(plan, true, true, true), "accept current " + expected);
    }

    private static void RatePolicy()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        Check(coordinator.Multiplier == 1m, "default multiplier is 1");
        Check(coordinator.TrySetMultiplier("10.00") && coordinator.Multiplier == 10m, "upper bound accepted");
        foreach (string invalid in new string[] { null, "", "0.99", "10.01", "1.001", "1.250", "1e0", " 1.25", "+1", "1,25" })
        {
            Check(!coordinator.TrySetMultiplier(invalid), "reject invalid input " + invalid);
            Check(coordinator.Multiplier == 10m, "rejection preserves selected multiplier");
        }
        Check(coordinator.TrySetMultiplier("1.50") && coordinator.Multiplier == 1.5m, "fractional multiplier accepted");
    }

    private static void PauseAndRemainder()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        Check(coordinator.TrySetMultiplier("1.50"), "set fractional multiplier");
        long g = state.Begin();
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, false, 0, 0), null), SyntheticClockStatus.Baseline);
        SyntheticClockPlan first = Prepare(coordinator, Publish(state, g, Start, false, 1, 1), true);
        Check(first.DesiredTicks == 1, "first half tick retained");
        Accept(coordinator, first, SyntheticClockStatus.Ready);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, true, 1, 50), false), SyntheticClockStatus.Paused);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, false, 2, 100), false), SyntheticClockStatus.Baseline);
        SyntheticClockPlan resumed = Prepare(coordinator, Publish(state, g, Start, false, 3, 101), true);
        Check(resumed.DesiredTicks == 2, "pause omits unknown time and retains known fraction");
        Accept(coordinator, resumed, SyntheticClockStatus.Ready);
        SyntheticClockPlan hidden = Prepare(coordinator, Publish(state, g, Start, false, 4, 500), null);
        Check(!hidden.DesiredTicks.HasValue, "hidden pause is not counted");
        Accept(coordinator, hidden, SyntheticClockStatus.UnverifiedInterval);
        SyntheticClockPlan next = Prepare(coordinator, Publish(state, g, Start, false, 5, 501), true);
        Check(next.DesiredTicks == 1, "only verified interval after hidden pause advances");
        Accept(coordinator, next, SyntheticClockStatus.Ready);
    }

    private static void OneMinuteAtBounds()
    {
        foreach (decimal rate in new decimal[] { 1m, 10m })
        {
            GameClockState state = new GameClockState();
            SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
            Check(coordinator.TrySetMultiplier(rate == 1m ? "1.00" : "10.00"), "set rate bound");
            long g = state.Begin();
            Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, false, 0, 0), null), SyntheticClockStatus.Baseline);
            SyntheticClockPlan plan = Prepare(coordinator,
                Publish(state, g, Start, false, 1, TimeSpan.TicksPerMinute), true);
            long expected = rate == 1m ? TimeSpan.TicksPerMinute : 10 * TimeSpan.TicksPerMinute;
            Check(plan.DesiredTicks == expected && plan.ProposedGameTime == Start.AddTicks(expected),
                "one real minute scales correctly at the rate bound");
            Accept(coordinator, plan, SyntheticClockStatus.Ready);
        }
    }

    private static void TransitionsAndGaps()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        long g = state.Begin();
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, false, 0, 0), null), SyntheticClockStatus.Baseline);
        Check(coordinator.TrySetMultiplier("10") && coordinator.TrySetMultiplier("1"), "temporary rate changes accepted");
        SyntheticClockPlan change = Prepare(coordinator, Publish(state, g, Start, false, 1, 100), true);
        Check(!change.DesiredTicks.HasValue, "1 to 10 to 1 must not hide a rate change");
        Accept(coordinator, change, SyntheticClockStatus.MultiplierTransition);
        SyntheticClockPlan ready = Prepare(coordinator, Publish(state, g, Start, false, 2, 101), true);
        Check(ready.DesiredTicks == 1, "verified interval after rate change");
        Accept(coordinator, ready, SyntheticClockStatus.Ready);
        Publish(state, g, Start, false, 3, 200);
        SyntheticClockPlan gap = Prepare(coordinator, Publish(state, g, Start, false, 4, 300), true);
        Check(!gap.DesiredTicks.HasValue, "missing sequence cannot be covered by later evidence");
        Accept(coordinator, gap, SyntheticClockStatus.SampleGap);
        ready = Prepare(coordinator, Publish(state, g, Start, false, 5, 301), true);
        Check(ready.DesiredTicks == 1, "gap is not caught up");
        Accept(coordinator, ready, SyntheticClockStatus.Ready);
    }

    private static void AcrossMidnight()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        Check(coordinator.TrySetMultiplier("1.25"), "set midnight multiplier");
        long g = state.Begin();
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, false, 0, 0), null), SyntheticClockStatus.Baseline);
        SyntheticClockPlan plan = Prepare(coordinator, Publish(state, g, Start, false, 1, TimeSpan.TicksPerSecond), true);
        Check(plan.DesiredTicks == 12500000L, "scaled second");
        Check(plan.ProposedGameTime == new DateTime(2026, 1, 2).AddMilliseconds(250), "candidate crosses midnight");
        Accept(coordinator, plan, SyntheticClockStatus.Ready);
        SyntheticClockPlan next = Prepare(coordinator, Publish(state, g, Start, false, 2, 2 * TimeSpan.TicksPerSecond), true);
        Check(next.ProposedGameTime == new DateTime(2026, 1, 2).AddMilliseconds(1500), "accepted synthetic time accumulates across intervals");
        Accept(coordinator, next, SyntheticClockStatus.Ready);
    }

    private static void RewindAfterPause()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        Check(coordinator.TrySetMultiplier("1.50"), "set rewind test rate");
        long g = state.Begin();
        DateTime later = Start.AddDays(1);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, later, false, 0, 0), null), SyntheticClockStatus.Baseline);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, later, false, 1, 1), true), SyntheticClockStatus.Ready);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, later, true, 1, 2), null), SyntheticClockStatus.Paused);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, false, 2, 3), true), SyntheticClockStatus.Discontinuity);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, false, 3, 4), true), SyntheticClockStatus.Baseline);
        SyntheticClockPlan next = Prepare(coordinator, Publish(state, g, Start, false, 4, 5), true);
        Check(next.DesiredTicks == 1 && next.ProposedGameTime == Start.AddTicks(1), "rewind resets previous virtual time and fraction");
        Accept(coordinator, next, SyntheticClockStatus.Ready);
    }

    private static void RewindWhilePaused()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        Check(coordinator.TrySetMultiplier("1.50"), "set paused rewind rate");
        long g = state.Begin();
        DateTime later = Start.AddDays(1);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, later, false, 0, 0), null), SyntheticClockStatus.Baseline);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, later, false, 1, 1), true), SyntheticClockStatus.Ready);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, true, 1, 2), null), SyntheticClockStatus.Discontinuity);
        Accept(coordinator, Prepare(coordinator, Publish(state, g, later, false, 2, 3), true), SyntheticClockStatus.Baseline);
        SyntheticClockPlan next = Prepare(coordinator, Publish(state, g, later, false, 3, 4), true);
        Check(next.DesiredTicks == 1 && next.ProposedGameTime == later.AddTicks(1), "observed paused rewind clears old virtual state");
        Accept(coordinator, next, SyntheticClockStatus.Ready);
    }

    private static void StaleAndCommitChecks()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        Check(coordinator.TrySetMultiplier("1.50"), "set rate for stale tests");
        long g = state.Begin();
        GameClockSnapshot baseline = Publish(state, g, Start, false, 0, 0);
        SyntheticClockPlan initial = Prepare(coordinator, baseline, null);
        Check(!coordinator.TryAccept(initial, false, false, false), "baseline evidence is revalidated at acceptance");
        Accept(coordinator, initial, SyntheticClockStatus.Baseline);
        SyntheticClockPlan half = Prepare(coordinator, Publish(state, g, Start, false, 1, 1), true);
        half.CopyScaler().Reset();
        Accept(coordinator, half, SyntheticClockStatus.Ready);
        half.CopyScaler().Reset();
        SyntheticClockPlan abandoned = Prepare(coordinator, Publish(state, g, Start, false, 2, 2), true);
        Publish(state, g, Start, false, 3, 3);
        Check(!coordinator.TryAccept(abandoned, true, true, true), "superseded plan rejected");
        SyntheticClockPlan gap = Prepare(coordinator, state.Latest, true);
        Accept(coordinator, gap, SyntheticClockStatus.SampleGap);
        SyntheticClockPlan retained = Prepare(coordinator, Publish(state, g, Start, false, 4, 4), true);
        Check(retained.DesiredTicks == 2, "failed proposal did not consume fractional remainder");
        Check(!coordinator.TryAccept(retained, true, true, null), "freshness must be reconfirmed");
        Accept(coordinator, retained, SyntheticClockStatus.Ready);
        Check(!coordinator.TryAccept(retained, true, true, true), "accepted plan cannot be reused");
        SyntheticClockPlan stale = Prepare(coordinator, baseline, true);
        Check(stale.Status == SyntheticClockStatus.Stale && !coordinator.TryAccept(stale, true, true, true), "old snapshot is inert");
        SyntheticClockPlan unbound = Prepare(coordinator, null, true);
        Check(unbound.Status == SyntheticClockStatus.Unknown && !coordinator.TryAccept(unbound, true, true, true), "unbound null is inert");
        SyntheticClockPlan planned = Prepare(coordinator, Publish(state, g, Start, false, 5, 5), true);
        state.End();
        Check(!coordinator.TryAccept(planned, true, true, true), "unload invalidates prepared plan");
    }

    private static void LoadAndSaveFixtures()
    {
        // Date values stand in for in-memory fixtures only. They define no product save schema.
        DateTime saveA = Start;
        DateTime saveB = new DateTime(2030, 6, 1);
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        Check(coordinator.TrySetMultiplier("1.50"), "set fixture rate");
        long cityA = state.Begin();
        GameClockSnapshot oldA = Publish(state, cityA, saveA, false, 0, 0);
        Accept(coordinator, Prepare(coordinator, oldA, null), SyntheticClockStatus.Baseline);
        Accept(coordinator, Prepare(coordinator, Publish(state, cityA, saveA, false, 1, 1), true), SyntheticClockStatus.Ready);
        long cityB = state.Begin();
        Accept(coordinator, Prepare(coordinator, Publish(state, cityB, saveB, false, 0, 100), null), SyntheticClockStatus.Baseline);
        Check(!FakeUnload(state, cityA) && state.CurrentGeneration == cityB, "old fake unload cannot end the new generation");
        Check(Prepare(coordinator, oldA, true).Status == SyntheticClockStatus.Stale, "old city sample rejected");
        Check(!state.Publish(cityA, null, null, null, null), "old city cannot publish unknown into new city");
        SyntheticClockPlan b = Prepare(coordinator, Publish(state, cityB, saveB, false, 1, 101), true);
        Check(b.DesiredTicks == 1, "fraction from city A is not transferred to city B");
        Accept(coordinator, b, SyntheticClockStatus.Ready);
        long cityA2 = state.Begin();
        Accept(coordinator, Prepare(coordinator, Publish(state, cityA2, saveA, false, 0, 200), null), SyntheticClockStatus.Baseline);
        SyntheticClockPlan restored = Prepare(coordinator, Publish(state, cityA2, saveA, false, 1, 201), true);
        Check(restored.DesiredTicks == 1, "reopened city starts a new generation without borrowed remainder");
        Accept(coordinator, restored, SyntheticClockStatus.Ready);
        Accept(coordinator, Prepare(coordinator, Publish(state, cityA2, null, null, null, null), null), SyntheticClockStatus.Unknown);
        Accept(coordinator, Prepare(coordinator, Publish(state, cityA2, saveA, false, 2, 400), null), SyntheticClockStatus.Baseline);
    }

    private static void PublishDuringAccept()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator left = new SyntheticClockCoordinator(state);
        SyntheticClockCoordinator right = new SyntheticClockCoordinator(state);
        long g = state.Begin();
        GameClockSnapshot baseline = Publish(state, g, Start, false, 0, 0);
        Accept(left, Prepare(left, baseline, null), SyntheticClockStatus.Baseline);
        Accept(right, Prepare(right, baseline, null), SyntheticClockStatus.Baseline);
        SyntheticClockPlan pending = Prepare(left, Publish(state, g, Start, false, 1, 1), true);
        ManualResetEvent started = new ManualResetEvent(false);
        bool accepted = true;
        Thread worker = new Thread(delegate() { started.Set(); accepted = left.TryAccept(pending, true, true, true); });
        worker.IsBackground = true;
        lock (state.SyntheticGate)
        {
            worker.Start();
            Check(started.WaitOne(5000), "accept worker started");
            Publish(state, g, Start, false, 2, 2);
        }
        Check(worker.Join(5000) && !accepted, "publication under the gate rejects an older proposal");
        started.Close();
        Accept(right, Prepare(right, state.Latest, true), SyntheticClockStatus.SampleGap);
        SyntheticClockPlan next = Prepare(right, Publish(state, g, Start, false, 3, 3), true);
        Accept(right, next, SyntheticClockStatus.Ready);
    }

    private static void ConcurrentOwners()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator left = new SyntheticClockCoordinator(state);
        SyntheticClockCoordinator right = new SyntheticClockCoordinator(state);
        long g = state.Begin();
        GameClockSnapshot sample = Publish(state, g, Start, false, 0, 0);
        Accept(left, Prepare(left, sample, null), SyntheticClockStatus.Baseline);
        Accept(right, Prepare(right, sample, null), SyntheticClockStatus.Baseline);
        sample = Publish(state, g, Start, false, 1, 1);
        SyntheticClockPlan lp = Prepare(left, sample, true);
        SyntheticClockPlan rp = Prepare(right, sample, true);
        Check(lp.Status == SyntheticClockStatus.Ready && rp.Status == SyntheticClockStatus.Ready, "both synthetic plans are candidates before acquisition");
        ManualResetEvent start = new ManualResetEvent(false);
        bool a = false, b = false;
        Thread first = new Thread(delegate() { start.WaitOne(); a = left.TryAccept(lp, true, true, true); });
        Thread second = new Thread(delegate() { start.WaitOne(); b = right.TryAccept(rp, true, true, true); });
        first.IsBackground = true; second.IsBackground = true;
        first.Start(); second.Start(); start.Set();
        Check(first.Join(5000) && second.Join(5000), "owner threads complete");
        start.Close();
        Check(a != b, "exactly one coordinator acquires the fake preview owner");
        SyntheticClockCoordinator loser = a ? right : left;
        sample = Publish(state, g, Start, false, 2, 2);
        Check(Prepare(loser, sample, true).Status == SyntheticClockStatus.ConflictingOrUnknownWriter, "loser cannot take over in this generation");
    }

    private static void BoundaryAndCoordinatorRace()
    {
        GameClockState state = new GameClockState();
        SyntheticClockCoordinator coordinator = new SyntheticClockCoordinator(state);
        long g = state.Begin();
        Accept(coordinator, Prepare(coordinator, Publish(state, g, Start, false, 0, 0), null), SyntheticClockStatus.Baseline);
        SyntheticClockPlan plan = Prepare(coordinator, Publish(state, g, Start, false, 1, 1), true);
        Check(plan.Status == SyntheticClockStatus.Ready && plan.Control != null, "shared boundary race has a ready plan");
        ManualResetEvent start = new ManualResetEvent(false);
        bool coordinatorWon = false, directWon = false;
        Thread first = new Thread(delegate() { start.WaitOne(); coordinatorWon = coordinator.TryAccept(plan, true, true, true); });
        Thread second = new Thread(delegate() { start.WaitOne(); directWon = plan.Control.Owner.TryConsumeDryRun(plan.Control, true, true, true); });
        first.IsBackground = true; second.IsBackground = true;
        first.Start(); second.Start(); start.Set();
        Check(first.Join(5000) && second.Join(5000), "direct and coordinated boundary consumers finish");
        start.Close();
        Check(coordinatorWon != directWon, "only one route consumes the synthetic snapshot");
    }

    internal static void Run()
    {
        RatePolicy(); OneMinuteAtBounds(); PauseAndRemainder(); TransitionsAndGaps(); AcrossMidnight(); RewindAfterPause(); RewindWhilePaused();
        StaleAndCommitChecks(); LoadAndSaveFixtures(); ConcurrentOwners(); BoundaryAndCoordinatorRace(); PublishDuringAccept();
        Console.WriteLine("PASS synthetic lifecycle, rates, midnight, in-memory save exchange, and owner conflict (no game writes)");
    }
}
