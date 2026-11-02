// Signature: OpenAI-Codex_Realistic_City_First_Response
namespace RealisticCityFirstResponse
{
    public enum ObservationStatus
    {
        Missing, Stale, Invalid, Unknown, NoReportedFire,
        NoReportedRain, RainAndFireReported
    }

    public static class FireWeatherAnalysis
    {
        // Exact capture identity prevents reuse across levels, buildings or later captures.
        // The caller must advance the sequence when an ID is recycled or observation is invalidated.
        public static ObservationStatus Inspect(FireWeatherSnapshot snapshot,
            long levelGeneration, ushort buildingId, ulong captureSequence)
        {
            if (snapshot == null) return ObservationStatus.Missing;
            if (snapshot.LevelGeneration != levelGeneration || snapshot.BuildingId != buildingId ||
                snapshot.CaptureSequence != captureSequence) return ObservationStatus.Stale;
            if (!snapshot.BuildingExists || snapshot.BuildingId == 0) return ObservationStatus.Invalid;
            if (!snapshot.RawRain.HasValue || !snapshot.RawFireIntensity.HasValue)
                return ObservationStatus.Unknown;
            double rain = snapshot.RawRain.Value;
            double fire = snapshot.RawFireIntensity.Value;
            if (double.IsNaN(rain) || double.IsInfinity(rain) || rain < 0 || double.IsNaN(fire) || double.IsInfinity(fire) || fire < 0)
                return ObservationStatus.Invalid;
            if (fire == 0) return ObservationStatus.NoReportedFire;
            if (rain == 0) return ObservationStatus.NoReportedRain;
            // Positive raw values express co-occurrence only, never suppression eligibility.
            return ObservationStatus.RainAndFireReported;
        }
    }
}
