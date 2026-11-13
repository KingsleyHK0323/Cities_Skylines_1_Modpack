// Signature: OpenAI-Codex_clock_surface
// Synthetic ICities contracts for an isolated fake test assembly.

namespace ICities
{
    public interface IThreading
    {
    }

    public interface IThreadingExtension
    {
        void OnCreated(IThreading threading);
        void OnReleased();
        void OnUpdate(float realTimeDelta, float simulationTimeDelta);
        void OnBeforeSimulationTick();
        void OnAfterSimulationTick();
        void OnBeforeSimulationFrame();
        void OnAfterSimulationFrame();
    }
}
