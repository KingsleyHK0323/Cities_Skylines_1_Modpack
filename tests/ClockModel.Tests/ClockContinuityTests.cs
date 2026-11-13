// Signature: OpenAI-Codex_Realistic_City_Time
// Signature: OpenAI-Codex
using System;
using System.Collections.Generic;
using RealisticCityTime;
using RealisticCityTime.Game;

internal static class ClockContinuityTests
{
    private static readonly DateTime Date = new DateTime(2020, 1, 1);
    private static ClockObservation Legacy(long generation, long sequence, long ticks) =>
        new ClockObservation(generation, sequence, ticks, Date, false);
    private static ClockObservation Verified(long generation, long sequence, long ticks) =>
        new ClockObservation(generation, sequence, ticks, Date, false, false, true);

    private static void Check(List<string> failures, ClockInputResult result, ClockInputStatus status, long? ticks, string label)
    {
        if (result.Status != status || result.DesiredGameTicks != ticks)
            failures.Add(label + ": received " + result.Status + " / " + result.DesiredGameTicks);
    }

    internal static void Run()
    {
        var failures = new List<string>();
        var noEvidence = new ClockInputSession(3);
        long generation = noEvidence.BeginSession();
        noEvidence.Observe(Legacy(generation, 0, 0), 1m);
        ClockInputResult unknown = noEvidence.Observe(Legacy(generation, 1, 600000000), 1m);
        if (unknown.Status == ClockInputStatus.Ready || unknown.DesiredGameTicks.HasValue)
            failures.Add("Unverified interval, including a possible hidden pause, produced elapsed game ticks.");
        Check(failures, unknown, ClockInputStatus.UnverifiedInterval, null, "legacy interval is unknown");
        Check(failures, noEvidence.Observe(Verified(generation, 2, 600000100), 1m),
            ClockInputStatus.Ready, 100, "verified interval after unknown excludes the gap");

        var changed = new ClockInputSession(3);
        generation = changed.BeginSession();
        changed.Observe(Legacy(generation, 0, 0), 1m);
        ClockInputResult changedResult = changed.Observe(Legacy(generation, 1, 100), 10m);
        if (changedResult.Status == ClockInputStatus.Ready || changedResult.DesiredGameTicks.HasValue)
            failures.Add("A multiplier change with unknown effective boundary produced elapsed game ticks.");

        var verifiedChange = new ClockInputSession(3);
        generation = verifiedChange.BeginSession();
        verifiedChange.Observe(Verified(generation, 0, 0), 1m);
        Check(failures, verifiedChange.Observe(Verified(generation, 1, 100), 10m),
            ClockInputStatus.MultiplierTransition, null, "verified interval with unknown multiplier switch point");
        Check(failures, verifiedChange.Observe(Verified(generation, 2, 102), 10m),
            ClockInputStatus.Ready, 20, "only the following stable-multiplier interval advances");

        var invalid = new ClockInputSession(3);
        generation = invalid.BeginSession();
        if (invalid.Observe(Legacy(generation, 0, 0), 11m).Status != ClockInputStatus.InvalidInput)
            failures.Add("An invalid multiplier was accepted as a baseline.");
        Check(failures, invalid.Observe(Verified(generation, 1, 100), 1m),
            ClockInputStatus.Baseline, null, "valid input after invalid baseline reanchors");
        Check(failures, invalid.Observe(Verified(generation, 2, 101), 1m),
            ClockInputStatus.Ready, 1, "invalid baseline creates no elapsed debt");

        var fractional = new ClockInputSession(3);
        generation = fractional.BeginSession();
        fractional.Observe(Verified(generation, 0, 0), 1.5m);
        Check(failures, fractional.Observe(Verified(generation, 1, 1), 1.5m), ClockInputStatus.Ready, 1, "first fractional tick");
        Check(failures, fractional.Observe(Legacy(generation, 2, 100), 1.5m), ClockInputStatus.UnverifiedInterval, null, "unknown gap");
        Check(failures, fractional.Observe(Verified(generation, 3, 101), 1.5m), ClockInputStatus.Ready, 2, "known fractional remainder survives unknown gap");
        Check(failures, fractional.Observe(new ClockObservation(generation, 4, 102, Date, false, false, false), 1.5m),
            ClockInputStatus.UnverifiedInterval, null, "explicitly interrupted interval");

        var missed = new ClockInputSession(3);
        generation = missed.BeginSession();
        missed.Observe(Verified(generation, 0, 0), 1m);
        ClockInputResult missingSamples = missed.Observe(Verified(generation, 3, 300), 1m);
        if (missingSamples.Status == ClockInputStatus.Ready || missingSamples.DesiredGameTicks.HasValue)
            failures.Add("Sequence gap allowed evidence about a later interval to cover missing samples.");
        Check(failures, missingSamples, ClockInputStatus.SampleGap, null, "missed pause or multiplier-change samples");
        Check(failures, missed.Observe(Verified(generation, 4, 301), 1m),
            ClockInputStatus.Ready, 1, "only a contiguous verified interval after a sample gap advances");

        var fractionalGap = new ClockInputSession(3);
        generation = fractionalGap.BeginSession();
        fractionalGap.Observe(Verified(generation, 0, 0), 1.5m);
        Check(failures, fractionalGap.Observe(Verified(generation, 1, 1), 1.5m), ClockInputStatus.Ready, 1, "fraction before sequence gap");
        Check(failures, fractionalGap.Observe(Verified(generation, 4, 100), 1.5m), ClockInputStatus.SampleGap, null, "fractional sequence gap");
        Check(failures, fractionalGap.Observe(Verified(generation, 5, 101), 1.5m), ClockInputStatus.Ready, 2, "known fractional remainder survives sequence gap");

        var cache = new GameClockState();
        var boundary = new ClockControlBoundary(cache);
        long city = cache.Begin();
        cache.Publish(city, Date, false, 1, 0);
        ClockControlPreview unverified = boundary.Evaluate(cache.Latest, 1m, 3, true, true);
        if (unverified.Status != ClockControlStatus.UnverifiedFreshness || boundary.TryConsumeDryRun(unverified, true, true))
            failures.Add("Cached unpaused sample became eligible without current observation freshness evidence.");
        var known = boundary.Evaluate(cache.Latest, 1m, 3, true, true, true);
        if (known.Status != ClockControlStatus.Candidate || known.CanWrite)
            failures.Add("Synthetic freshness evidence should allow only a dry-run candidate.");
        if (boundary.TryConsumeDryRun(known, true, true) || boundary.TryConsumeDryRun(known, true, true, false))
            failures.Add("Freshness must be supplied again at dry-run consumption.");
        if (!boundary.TryConsumeDryRun(known, true, true, true) || known.CanWrite)
            failures.Add("Revalidated synthetic preview was not consumed safely.");

        if (failures.Count != 0) throw new Exception(string.Join(" | ", failures));
        Console.WriteLine("PASS fail-closed continuity and multiplier-boundary checks");
    }
}
