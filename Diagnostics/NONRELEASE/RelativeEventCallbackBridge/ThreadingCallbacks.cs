// Signature: OpenAI-Codex
// Offline research source only; it provides no loader, registration, UI, or output.
#if NONRELEASE_RELATIVE_CALLBACKS
using ICities;
using RealisticCityTime.Diagnostics.RelativeEventTrace;

namespace RealisticCityTime.Diagnostics.NonReleaseCallbackBridge
{
    public sealed class ThreadingCallbacks : IThreadingExtension
    {
        private readonly CallbackSessionGate session;

        public ThreadingCallbacks() : this(128, 8) { }

        internal ThreadingCallbacks(int eventCapacity, int threadCapacity)
        {
            session = new CallbackSessionGate(eventCapacity, threadCapacity);
        }

        internal CallbackSessionGate Session { get { return session; } }

        public void OnCreated(IThreading threading) { }
        public void OnReleased() { session.RequestStop(); }
        public void OnBeforeSimulationTick() { Capture(EventKind.BeforeSimulationTick); }
        public void OnAfterSimulationTick() { Capture(EventKind.AfterSimulationTick); }
        public void OnBeforeSimulationFrame() { Capture(EventKind.BeforeSimulationFrame); }
        public void OnAfterSimulationFrame() { Capture(EventKind.AfterSimulationFrame); }
        public void OnUpdate(float realTimeDelta, float simulationTimeDelta) { Capture(EventKind.Update); }

        private void Capture(EventKind kind)
        {
            CallbackTicket ticket;
            if (!session.TryBegin(kind, out ticket)) return;
            try { }
            finally { ticket.End(); }
        }
    }
}
#endif
