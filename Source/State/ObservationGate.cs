// Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Vehicle_Management
using AutomatedPublicTransportVehicleManagement.Model;

namespace AutomatedPublicTransportVehicleManagement.State
{
    public static class ObservationGate
    {
        // Signature: OpenAI-Codex_Automated_Public_Transport_Vehicle_Management
        // Unknown and unavailable values are displayable; stale identities and negative counts are not.
        public static bool CanDisplay(LineObservation observation, LineKey expected)
        {
            return observation != null && IsCurrent(expected, expected.SessionId, expected.ObservationSequence) &&
                IsCurrent(observation.Key, expected.SessionId, expected.ObservationSequence) &&
                observation.Key.LineId == expected.LineId &&
                (!observation.TargetVehicles.HasValue || observation.TargetVehicles.Value >= 0) &&
                (!observation.ActualVehicles.HasValue || observation.ActualVehicles.Value >= 0);
        }

        // This only validates a published snapshot's local context. It grants no write authority.
        public static bool IsCurrent(LineKey key, int currentSessionId, long currentSequence)
        {
            return key.SessionId > 0 && key.LineId != 0 && key.ObservationSequence > 0 &&
                key.SessionId == currentSessionId && key.ObservationSequence == currentSequence;
        }
    }
}
