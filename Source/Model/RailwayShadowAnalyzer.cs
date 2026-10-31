// Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
using System;
using System.Collections.Generic;

namespace RealisticRailwayTrafficControl.Model
{
    public static class RailwayShadowAnalyzer
    {
        public static ShadowDecision[] Evaluate(RailwaySnapshot snapshot, MovementRequest[] requests)
        {
            if (requests == null) return new ShadowDecision[0];
            requests = (MovementRequest[])requests.Clone();
            ShadowDecision[] result = new ShadowDecision[requests.Length];
            ValidationReport report = RailwaySnapshotValidator.Validate(snapshot, requests);
            if (!report.IsValid)
            {
                for (int i = 0; i < result.Length; i++)
                    result[i] = new ShadowDecision(requests[i] == null ? null : requests[i].RequestId,
                        report.FailureOutcome, report.ReasonCode, new string[0]);
                return result;
            }

            Dictionary<string, ResourceEvidence> map = new Dictionary<string, ResourceEvidence>(StringComparer.Ordinal);
            foreach (ResourceEvidence resource in snapshot.Resources) map.Add(resource.ResourceKey, resource);
            // Reservations exist only for this call, never for a vehicle or game resource outside the model.
            Dictionary<string, bool> reserved = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int i = 0; i < requests.Length; i++)
            {
                MovementRequest request = requests[i];
                List<string> required = new List<string>();
                Dictionary<string, bool> neighborhood = new Dictionary<string, bool>(StringComparer.Ordinal);
                foreach (string key in request.RequiredResourceKeys)
                {
                    if (!required.Contains(key)) required.Add(key);
                    neighborhood[key] = true;
                    foreach (string conflict in map[key].ConflictKeys) neighborhood[conflict] = true;
                }

                ShadowReasonCode reason = ShadowReasonCode.None;
                foreach (string key in neighborhood.Keys)
                    foreach (string occupant in map[key].OccupantKeys)
                        if (!String.Equals(occupant, request.VehicleKey, StringComparison.Ordinal))
                            reason = ShadowReasonCode.OccupiedResource;
                if (reason == ShadowReasonCode.None)
                    foreach (string key in neighborhood.Keys)
                        if (reserved.ContainsKey(key)) reason = ShadowReasonCode.ReservedConflict;

                if (reason == ShadowReasonCode.None)
                {
                    // Publish the entire request to the shadow set only after every check succeeds.
                    foreach (string key in required) reserved[key] = true;
                    result[i] = new ShadowDecision(request.RequestId, ShadowOutcome.WouldReserve,
                        ShadowReasonCode.None, required.ToArray());
                }
                else result[i] = new ShadowDecision(request.RequestId, ShadowOutcome.Conflict, reason, required.ToArray());
            }
            return result;
        }
    }
}
