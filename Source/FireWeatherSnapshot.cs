// Signature: OpenAI-Codex_Realistic_City_First_Response
namespace RealisticCityFirstResponse
{
    // Caller-supplied observations, not a live game capture API. Units remain unverified.
    public sealed class FireWeatherSnapshot
    {
        public readonly long LevelGeneration;
        public readonly ushort BuildingId;
        public readonly ulong CaptureSequence;
        public readonly bool BuildingExists;
        public readonly double? RawRain;
        public readonly double? RawFireIntensity;

        public FireWeatherSnapshot(long levelGeneration, ushort buildingId,
            ulong captureSequence, bool buildingExists, double? rawRain, double? rawFireIntensity)
        {
            LevelGeneration = levelGeneration;
            BuildingId = buildingId;
            CaptureSequence = captureSequence;
            BuildingExists = buildingExists;
            RawRain = rawRain;
            RawFireIntensity = rawFireIntensity;
        }
    }
}
