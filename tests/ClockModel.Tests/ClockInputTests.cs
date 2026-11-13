// Signature: OpenAI-Codex_Realistic_City_Time
// Explicit synthetic interval evidence: OpenAI-Codex_Realistic_City_Time
// Update signature: OpenAI-Codex
using System;
using RealisticCityTime;

internal static class ClockInputTests
{
    private static readonly DateTime Date = new DateTime(2020, 1, 1, 23, 59, 59);
    private static ClockObservation Sample(long generation, long sequence, long ticks, bool paused = false) =>
        new ClockObservation(generation, sequence, ticks, Date, paused, false, true);
    private static void Check(ClockInputResult result, ClockInputStatus status, long? ticks = null)
    {
        if (result.Status != status || result.DesiredGameTicks != ticks)
            throw new Exception("Unexpected observation result: " + result.Status);
    }
    public static void Run()
    {
        var input = new ClockInputSession(3);
        Check(input.Observe(null,1m),ClockInputStatus.Inactive);
        long first = input.BeginSession();
        Check(input.Observe(Sample(first,0,100),1.25m),ClockInputStatus.Baseline);
        Check(input.Observe(Sample(first,1,104),1.25m),ClockInputStatus.Ready,5);
        Check(input.Observe(Sample(first,1,900),1m),ClockInputStatus.Stale);
        Check(input.Observe(Sample(first,2,108),1.25m),ClockInputStatus.Ready,5);
        long second = input.BeginSession();
        Check(input.Observe(Sample(second,0,0),1.25m),ClockInputStatus.Baseline);
        Check(input.Observe(Sample(first,999,900),1m),ClockInputStatus.Stale);
        Check(input.Observe(Sample(second,1,4),1.25m),ClockInputStatus.Ready,5);
        Check(input.Observe(Sample(second,2,10,true),1m),ClockInputStatus.Paused);
        Check(input.Observe(Sample(second,3,10000),1.25m),ClockInputStatus.Baseline);
        Check(input.Observe(Sample(second,4,10004),1.25m),ClockInputStatus.Ready,5);
        Check(input.Observe(new ClockObservation(second,5,null,Date,null),1m),ClockInputStatus.Unknown);
        Check(input.Observe(Sample(second,6,20000),1m),ClockInputStatus.Baseline);
        Check(input.Observe(Sample(second,7,19999),1m),ClockInputStatus.Discontinuity);
        Check(input.Observe(Sample(second,8,20000),1m),ClockInputStatus.Baseline);
        Check(input.Observe(new ClockObservation(second,9,20001,Date.AddDays(-1),false),1m),ClockInputStatus.Discontinuity);
        Check(input.Observe(Sample(second,10,0),10m),ClockInputStatus.Baseline);
        Check(input.Observe(Sample(second,11,long.MaxValue),10m),ClockInputStatus.Overflow);
        Check(input.Observe(Sample(second,12,10),1m),ClockInputStatus.Baseline);
        Check(input.Observe(Sample(second,13,20),11m),ClockInputStatus.InvalidInput);
        Check(input.Observe(Sample(second,14,30),1m),ClockInputStatus.Baseline);
        Check(input.Observe(Sample(second,15,31),1m),ClockInputStatus.Ready,1);
        Check(input.Observe(new ClockObservation(second,16,32,Date,false,true),1m),ClockInputStatus.Discontinuity);
        input.EndSession();
        Check(input.Observe(Sample(second,17,40),1m),ClockInputStatus.Inactive);
        var fractional = new ClockInputSession(3);
        long token = fractional.BeginSession();
        Check(fractional.Observe(Sample(token,0,0),1.5m),ClockInputStatus.Baseline);
        Check(fractional.Observe(Sample(token,1,1),1.5m),ClockInputStatus.Ready,1);
        Check(fractional.Observe(Sample(token,2,2,true),1.5m),ClockInputStatus.Paused);
        Check(fractional.Observe(Sample(token,3,100),1.5m),ClockInputStatus.Baseline);
        Check(fractional.Observe(Sample(token,4,101),1.5m),ClockInputStatus.Ready,2);
        token = fractional.BeginSession();
        Check(fractional.Observe(Sample(token,0,0),1.5m),ClockInputStatus.Baseline);
        Check(fractional.Observe(Sample(token,1,1),1.5m),ClockInputStatus.Ready,1);
        Check(fractional.Observe(new ClockObservation(token,2,2,null,false),1m),ClockInputStatus.Unknown);
        Check(fractional.Observe(Sample(token,3,10),1m),ClockInputStatus.Baseline);
        Check(fractional.Observe(Sample(token+1,1000,1000),1m),ClockInputStatus.Stale);
        Check(fractional.Observe(Sample(token,-1,1000),1m),ClockInputStatus.Stale);
        Check(fractional.Observe(new ClockObservation(token,4,11,Date.AddSeconds(2),false,false,true),1m),ClockInputStatus.Ready,1);
        Check(fractional.Observe(null,1m),ClockInputStatus.Unknown);
        Check(fractional.Observe(Sample(token,5,-1),1m),ClockInputStatus.InvalidInput);
        Console.WriteLine("PASS read-only observation contract and lifecycle checks (no live game reader)");
    }
}
