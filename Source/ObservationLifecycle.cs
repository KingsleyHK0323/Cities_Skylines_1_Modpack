// Signature: OpenAI-Codex_Realistic_City_First_Response
namespace RealisticCityFirstResponse
{
    // Instance-local state only; no simulation subscriptions, timers or saved-game data.
    public sealed class ObservationLifecycle
    {
        private bool loaded;
        public bool IsLoaded { get { return loaded; } }

        public void Load(bool playableCity)
        {
            loaded = playableCity;
        }

        public void Unload()
        {
            loaded = false;
        }

        public ObservationStatus Inspect(FireWeatherSnapshot snapshot, long generation,
            ushort buildingId, ulong captureSequence)
        {
            return loaded ? FireWeatherAnalysis.Inspect(snapshot, generation, buildingId, captureSequence)
                : ObservationStatus.Missing;
        }
    }
}
