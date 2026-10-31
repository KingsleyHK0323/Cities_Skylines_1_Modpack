// Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
using System;
using System.Collections.Generic;

namespace RealisticRailwayTrafficControl.Model
{
    public static class RailwaySnapshotValidator
    {
        public static ValidationReport Validate(RailwaySnapshot snapshot, MovementRequest[] requests)
        {
            if (snapshot == null || requests == null || requests.Length == 0)
                return Fail(ShadowReasonCode.InvalidRequest);
            requests = (MovementRequest[])requests.Clone();
            ResourceEvidence[] resources = snapshot.Resources;
            VehicleEvidence[] vehicles = snapshot.Occupants;
            if (resources == null || vehicles == null || Empty(snapshot.TopologyToken) || snapshot.SegmentCount < 0)
                return Fail(ShadowReasonCode.InvalidRequest);

            Dictionary<string, ResourceEvidence> map = new Dictionary<string, ResourceEvidence>(StringComparer.Ordinal);
            Dictionary<string, bool> known = new Dictionary<string, bool>(StringComparer.Ordinal);
            Dictionary<string, bool> ids = new Dictionary<string, bool>(StringComparer.Ordinal);
            Dictionary<string, bool> requestedVehicles = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (ResourceEvidence resource in resources)
            {
                if (resource == null || Empty(resource.ResourceKey) || !KeysValid(resource.ConflictKeys)
                    || !KeysValid(resource.OccupantKeys) || map.ContainsKey(resource.ResourceKey))
                    return Fail(ShadowReasonCode.InvalidRequest);
                map.Add(resource.ResourceKey, resource);
            }
            foreach (VehicleEvidence vehicle in vehicles)
            {
                if (vehicle == null || Empty(vehicle.VehicleKey) || known.ContainsKey(vehicle.VehicleKey))
                    return Fail(ShadowReasonCode.InvalidRequest);
                known.Add(vehicle.VehicleKey, true);
            }
            foreach (MovementRequest request in requests)
            {
                if (request == null || Empty(request.RequestId) || Empty(request.VehicleKey)
                    || Empty(request.TopologyToken) || !KeysValid(request.RequiredResourceKeys)
                    || request.RequiredResourceKeys.Length == 0 || ids.ContainsKey(request.RequestId)
                    || requestedVehicles.ContainsKey(request.VehicleKey))
                    return Fail(ShadowReasonCode.InvalidRequest);
                ids.Add(request.RequestId, true);
                requestedVehicles.Add(request.VehicleKey, true);
            }

            // Completeness is checked before topology or identity, including inclusive engineering caps.
            if (!snapshot.Complete || snapshot.SegmentCount > 128 || vehicles.Length > 64
                || resources.Length > 256 || requests.Length > 64)
                return Fail(ShadowReasonCode.IncompleteCapture);
            foreach (VehicleEvidence vehicle in vehicles)
                if (!vehicle.TailVerified) return Fail(ShadowReasonCode.IncompleteCapture);
            foreach (ResourceEvidence resource in resources)
            {
                string[] occupants = resource.OccupantKeys;
                if (resource.ConflictKeys.Length > 256 || occupants.Length > 64
                    || (resource.State != EvidenceState.Clear && resource.State != EvidenceState.Occupied)
                    || (resource.State == EvidenceState.Clear && occupants.Length != 0)
                    || (resource.State == EvidenceState.Occupied && occupants.Length == 0))
                    return Fail(ShadowReasonCode.IncompleteCapture);
            }
            foreach (MovementRequest request in requests)
                if (request.RequiredResourceKeys.Length > 128) return Fail(ShadowReasonCode.IncompleteCapture);

            if (snapshot.Network == NetworkSupport.Unsupported)
                return new ValidationReport(false, ShadowOutcome.Unsupported, ShadowReasonCode.UnsupportedNetwork);
            if (snapshot.Network != NetworkSupport.SupportedDedicatedRail)
                return Fail(ShadowReasonCode.InvalidTopology);
            foreach (ResourceEvidence resource in resources)
            {
                if (!resource.ConflictsComplete) return Fail(ShadowReasonCode.InvalidTopology);
                foreach (string conflict in resource.ConflictKeys)
                {
                    ResourceEvidence neighbor;
                    if (!map.TryGetValue(conflict, out neighbor)
                        || Array.IndexOf(neighbor.ConflictKeys, resource.ResourceKey) < 0)
                        return Fail(ShadowReasonCode.InvalidTopology);
                }
            }
            foreach (MovementRequest request in requests)
                foreach (string key in request.RequiredResourceKeys)
                    if (!map.ContainsKey(key)) return Fail(ShadowReasonCode.InvalidTopology);

            if (snapshot.CityEpoch <= 0 || snapshot.CaptureSerial <= 0 || snapshot.CapturedFrame < 0)
                return Fail(ShadowReasonCode.InvalidIdentity);
            foreach (VehicleEvidence vehicle in vehicles)
                if (!vehicle.IdentityVerified) return Fail(ShadowReasonCode.InvalidIdentity);
            foreach (ResourceEvidence resource in resources)
                foreach (string occupant in resource.OccupantKeys)
                    if (!known.ContainsKey(occupant)) return Fail(ShadowReasonCode.InvalidIdentity);
            foreach (MovementRequest request in requests)
                if (request.CityEpoch <= 0 || request.CaptureSerial <= 0 || !known.ContainsKey(request.VehicleKey))
                    return Fail(ShadowReasonCode.InvalidIdentity);
            foreach (MovementRequest request in requests)
                if (request.CityEpoch != snapshot.CityEpoch || request.CaptureSerial != snapshot.CaptureSerial)
                    return Fail(ShadowReasonCode.StaleCapture);
            foreach (MovementRequest request in requests)
                if (!String.Equals(request.TopologyToken, snapshot.TopologyToken, StringComparison.Ordinal))
                    return Fail(ShadowReasonCode.InvalidTopology);
            return new ValidationReport(true, ShadowOutcome.Unknown, ShadowReasonCode.None);
        }

        private static bool Empty(string value) { return value == null || value.Trim().Length == 0; }
        private static bool KeysValid(string[] values)
        {
            if (values == null) return false;
            foreach (string value in values) if (Empty(value)) return false;
            return true;
        }
        private static ValidationReport Fail(ShadowReasonCode reason)
        {
            return new ValidationReport(false, ShadowOutcome.Unknown, reason);
        }
    }
}
