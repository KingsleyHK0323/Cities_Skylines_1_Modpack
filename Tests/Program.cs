// Signature: OpenAI-Codex_Realistic_City_First_Response
using System;
using RealisticCityFirstResponse;

internal static class Program
{
    private static void Expect(ObservationStatus expected, FireWeatherSnapshot snapshot)
    {
        if (FireWeatherAnalysis.Inspect(snapshot, 7, 12, 100) != expected)
            throw new Exception("Unexpected observation status: " + expected);
    }

    private static FireWeatherSnapshot Sample(double? rain, double? fire)
    {
        return new FireWeatherSnapshot(7, 12, 100, true, rain, fire);
    }

    private static void Main()
    {
        Expect(ObservationStatus.Missing, null);
        Expect(ObservationStatus.Unknown, Sample(null, 1));
        Expect(ObservationStatus.Unknown, Sample(1, null));
        Expect(ObservationStatus.Invalid, Sample(double.NaN, 1));
        Expect(ObservationStatus.Invalid, Sample(double.PositiveInfinity, 1));
        Expect(ObservationStatus.Invalid, Sample(-1, 1));
        Expect(ObservationStatus.Invalid, Sample(1, -1));
        Expect(ObservationStatus.Invalid, Sample(1, double.NaN));
        Expect(ObservationStatus.Stale, new FireWeatherSnapshot(6, 12, 100, true, 1, 1));
        Expect(ObservationStatus.Stale, new FireWeatherSnapshot(7, 13, 100, true, 1, 1));
        Expect(ObservationStatus.Stale, new FireWeatherSnapshot(7, 12, 99, true, 1, 1));
        Expect(ObservationStatus.Invalid, new FireWeatherSnapshot(7, 12, 100, false, 1, 1));
        Expect(ObservationStatus.Invalid, new FireWeatherSnapshot(7, 0, 100, true, 1, 1));
        Expect(ObservationStatus.NoReportedFire, Sample(1, 0));
        Expect(ObservationStatus.NoReportedRain, Sample(0, 1));
        Expect(ObservationStatus.RainAndFireReported, Sample(2, 500));
        var original = Sample(2, 500);
        FireWeatherAnalysis.Inspect(original, 7, 12, 100);
        if (original.RawRain != 2 || original.RawFireIntensity != 500)
            throw new Exception("Analysis mutated its input.");
        var lifecycle = new ObservationLifecycle();
        if (lifecycle.IsLoaded) throw new Exception("Initially unloaded.");
        lifecycle.Load(true);
        if (!lifecycle.IsLoaded) throw new Exception("Playable city did not load.");
        if (lifecycle.Inspect(null, 7, 12, 100) != ObservationStatus.Missing)
            throw new Exception("Missing capture was not rejected.");
        lifecycle.Unload();
        lifecycle.Unload();
        if (lifecycle.IsLoaded) throw new Exception("Repeated unload did not remain safe.");
        if (lifecycle.Inspect(original, 7, 12, 100) != ObservationStatus.Missing)
            throw new Exception("Unloaded observation accepted.");
        lifecycle.Load(true);
        lifecycle.Load(false);
        if (lifecycle.IsLoaded) throw new Exception("Editor retained loaded state.");
        if (lifecycle.Inspect(original, 7, 12, 100) != ObservationStatus.Missing)
            throw new Exception("Editor observation accepted.");
        Console.WriteLine("17 observation and 7 lifecycle checks passed; game entry points remain untested.");
    }
}
