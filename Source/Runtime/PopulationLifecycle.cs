// Signature: OpenAI-Codex_Realistic_Urban_Population
using System;
using RealisticUrbanPopulation.Demand;

namespace RealisticUrbanPopulation.Runtime
{
    // Owns model state only. No demand hook, population write, save adapter or data source is enabled.
    public sealed class PopulationLifecycle
    {
        private readonly DemandSession session = new DemandSession();
        private long epoch;

        public long BeginLevel(bool gameplay)
        {
            if (epoch != 0) EndLevel(epoch);
            if (!gameplay) return 0;
            epoch = session.BeginCity();
            // Readiness and control permission deliberately remain false / Unknown.
            return epoch;
        }

        public bool EndLevel(long expectedEpoch)
        {
            if (epoch == 0 || epoch != expectedEpoch) return false;
            bool ended = session.EndCity(expectedEpoch);
            if (ended) epoch = 0;
            return ended;
        }

        public DemandSessionState ReadState() { return session.ReadState(); }
    }

    public sealed class LifecycleBoundary
    {
        private Exception lastError;
        public Exception LastError { get { return lastError; } }

        public bool TryRun(Action operation)
        {
            lastError = null;
            if (operation == null)
            { lastError = new ArgumentNullException("operation"); return false; }
            try { operation(); return true; }
            catch (Exception error) { lastError = error; return false; }
        }
    }
}
