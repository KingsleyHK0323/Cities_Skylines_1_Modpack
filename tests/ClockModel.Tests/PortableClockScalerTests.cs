// Signature: OpenAI-Codex_Realistic_City_Time
// Signature: OpenAI-Codex
using System;
using System.Reflection;
using RealisticCityTime;

internal static class PortableClockScalerTests
{
    private static readonly Type Scaler = typeof(ClockModel).Assembly.GetType("RealisticCityTime.Game.PortableClockScaler");
    private static object New(int precision)
    {
        try { return Activator.CreateInstance(Scaler, precision); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static long Advance(object instance, long ticks, decimal multiplier)
    {
        try { return (long)Scaler.GetMethod("Advance").Invoke(instance, new object[] { ticks, multiplier }); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static void Reset(object instance)
    {
        try { Scaler.GetMethod("Reset").Invoke(instance, null); }
        catch (TargetInvocationException ex) { throw ex.InnerException; }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("Portable scaler: " + message);
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Portable scaler: expected " + typeof(T).Name);
    }

    internal static void Run()
    {
        if (Scaler == null) throw new Exception("PortableClockScaler is missing; no CLR-compatible conversion bridge exists.");
        Reject<ArgumentOutOfRangeException>(() => New(-1));
        Reject<ArgumentOutOfRangeException>(() => New(29));
        var invalid = New(2);
        Reject<ArgumentException>(() => Advance(invalid, 1, 0.9m));
        Reject<ArgumentException>(() => Advance(invalid, 1, 10.1m));
        Reject<ArgumentException>(() => Advance(invalid, 1, 1.001m));
        Reject<ArgumentOutOfRangeException>(() => Advance(invalid, -1, 1m));

        int[] policies = { 0, 1, 3, 10, 28 };
        decimal[] rates = { 1m, 1.25m, 1.333m, 1.5m, 9.9m, 10m,
            1.0000000000000000000000000001m, 1.9999999999999999999999999999m,
            7.9228162514264337593543950335m, 9.999999999999999999999999999m };
        var random = new Random(20261113);
        int compared = 0;
        foreach (int precision in policies)
        {
            var expected = new ClockModel(precision);
            object actual = New(precision);
            for (int i = 0; i < 2000; i++)
            {
                decimal rate = rates[random.Next(rates.Length)];
                if (decimal.Round(rate, precision) != rate) continue;
                long ticks = random.NextInt64(0, 1000000000001L);
                long expectedTicks = expected.Advance(ticks, rate);
                long actualTicks = Advance(actual, ticks, rate);
                Require(expectedTicks == actualTicks, "reference mismatch at precision " + precision + ", iteration " + i);
                compared++;
            }
        }
        Require(compared > 1000, "insufficient differential cases");

        var oracle = new ClockModel(28);
        object bridge = New(28);
        decimal tinyFraction = 1.0000000000000000000000000001m;
        Require(oracle.Advance(long.MaxValue, tinyFraction) == Advance(bridge, long.MaxValue, tinyFraction), "maximum elapsed exactness");
        Require(oracle.Advance(1, 1m) == Advance(bridge, 1, 1m), "remainder following maximum elapsed");
        Reset(bridge);
        Require(Advance(bridge, 1, 1.5m) == 1, "fraction precondition");
        Reject<OverflowException>(() => Advance(bridge, long.MaxValue, 10m));
        Require(Advance(bridge, 1, 1.5m) == 2, "overflow changed the stored fraction");
        Reset(bridge);
        Require(Advance(bridge, 1, 1.5m) == 1, "reset clears fraction");
        Reject<ArgumentException>(() => Advance(bridge, 1, 11m));
        Reject<ArgumentOutOfRangeException>(() => Advance(bridge, -1, 1m));
        Require(Advance(bridge, 0, 1m) == 0, "zero elapsed produced ticks");
        Require(Advance(bridge, 1, 1.5m) == 2, "invalid or zero input changed the stored fraction");
        Console.WriteLine("PASS portable CLR arithmetic against BigInteger oracle: " + compared + " deterministic comparisons (no native clock writes)");
    }
}
