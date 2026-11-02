// Signature: OpenAI-Codex_Realistic_Pedestrian_Traffic_Control
namespace PedestrianTrafficControl {
    // Internal source seam only; no native game reader has been verified or connected.
    internal delegate CrossingObservation CrossingObservationReader(string city, int segment, int node, bool start);

    internal sealed class CrossingReadAdapter {
        private readonly object gate = new object();
        private readonly CrossingObservationReader reader;
        private object generation = new object();
        private string currentCity;

        internal CrossingReadAdapter(CrossingObservationReader reader) { this.reader = reader; }

        internal void BeginSession(string city) {
            lock (gate) {
                generation = new object();
                currentCity = string.IsNullOrEmpty(city) || city.Trim().Length == 0 ? null : city;
            }
        }

        internal void Invalidate() { BeginSession(null); }

        internal CrossingPreview Read(string city, int segment, int node, bool start, int topology, int revision) {
            object capturedGeneration;
            lock (gate) {
                if (currentCity == null)
                    return Unknown("No active observation session.");
                if (string.IsNullOrEmpty(city) || city.Trim().Length == 0 || segment <= 0 || node <= 0 || topology < 0 || revision < 0)
                    return Unknown("Invalid crossing identity.");
                if (!string.Equals(city, currentCity, System.StringComparison.Ordinal))
                    return Stale();
                if (reader == null)
                    return Unknown("No verified game observation reader is connected.");
                capturedGeneration = generation;
            }
            CrossingObservation observation;
            try {
                // Reader scheduling must be established by a future native integration.
                // No background work, native API calls, or cached successful data are created here.
                observation = reader(city, segment, node, start);
            } catch (System.Exception) {
                return Unknown("Observation reader failed; all effective states remain unknown.");
            }
            lock (gate) {
                if (!object.ReferenceEquals(capturedGeneration, generation))
                    return Stale();
                return CrossingPreview.Evaluate(observation, city, segment, node, start, topology, revision);
            }
        }

        private static CrossingPreview Unknown(string reason) {
            return new CrossingPreview(PreviewStatus.Unavailable, reason, null, ObservationSource.Unknown);
        }

        private static CrossingPreview Stale() {
            return new CrossingPreview(PreviewStatus.Stale, "Observation session changed; discard this result.", null, ObservationSource.Unknown);
        }
    }
}
