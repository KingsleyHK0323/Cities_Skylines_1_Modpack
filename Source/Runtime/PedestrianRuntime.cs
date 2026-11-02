// Signature: OpenAI-Codex
using System;
namespace PedestrianTrafficControl {
    internal enum LifecycleEvent { Enable, Disable, CityLoaded, OtherLevelLoaded, Unload, Release }
    internal sealed class PedestrianRuntime {
        private bool enabled;
        private CrossingReadAdapter adapter = new CrossingReadAdapter(null);
        internal bool Enabled { get { return enabled; } }
        internal bool HasSession { get; private set; }
        internal void Apply(LifecycleEvent value) {
            adapter.Invalidate();
            HasSession = false;
            if (value == LifecycleEvent.Enable) enabled = true;
            else if (value == LifecycleEvent.Disable) enabled = false;
            else if (value == LifecycleEvent.CityLoaded && enabled) {
                // A lifetime token only, not an observed city identity or verified data source.
                adapter.BeginSession(Guid.NewGuid().ToString("N"));
                HasSession = true;
            }
        }
        internal void Stop() { adapter.Invalidate(); HasSession = false; }
    }
    internal static class LifecycleGuard {
        internal static bool Run(Action operation, Action stop) {
            try { operation(); return true; }
            catch (Exception) {
                try { stop(); } catch (Exception) { }
                return false;
            }
        }
    }
}
