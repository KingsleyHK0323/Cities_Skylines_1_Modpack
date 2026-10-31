using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    internal enum RouteEditMode { Manual, Automatic }

    internal struct ExistingRouteStop
    {
        public readonly ushort NodeId;
        public readonly Vector3 Position;
        public readonly bool FixedPlatform; // Placement API argument, not a road asset classification.

        public ExistingRouteStop(ushort nodeId, Vector3 position) : this(nodeId, position, false) { }

        public ExistingRouteStop(ushort nodeId, Vector3 position, bool fixedPlatform)
        {
            NodeId = nodeId;
            Position = position;
            FixedPlatform = fixedPlatform;
        }
    }

    /// <summary>A copied line identity and ordered stop fingerprint for exact live revalidation.</summary>
    internal sealed class ExistingRouteSnapshot
    {
        public readonly ushort LineId;
        public readonly uint BuildIndex;
        public readonly string PrefabName;
        public readonly ReadOnlyCollection<ExistingRouteStop> Stops;

        public ExistingRouteSnapshot(ushort lineId, uint buildIndex, string prefabName,
                                     IList<ExistingRouteStop> stops)
        {
            LineId = lineId;
            BuildIndex = buildIndex;
            PrefabName = prefabName ?? string.Empty;
            Stops = new List<ExistingRouteStop>(stops ?? new ExistingRouteStop[0]).AsReadOnly();
        }

        public bool Matches(ushort lineId, uint buildIndex, string prefabName,
                            IList<ExistingRouteStop> stops)
        {
            if (lineId != LineId || buildIndex != BuildIndex ||
                !string.Equals(prefabName, PrefabName, StringComparison.Ordinal) ||
                stops == null || stops.Count != Stops.Count) { return false; }
            for (int i = 0; i < Stops.Count; i++)
            {
                Vector3 expected = Stops[i].Position;
                Vector3 actual = stops[i].Position;
                if (stops[i].NodeId != Stops[i].NodeId || stops[i].FixedPlatform != Stops[i].FixedPlatform ||
                    actual.x != expected.x || actual.y != expected.y || actual.z != expected.z)
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>Immutable move-only edit: preserve the existing line and ordered node identities.</summary>
    internal sealed class RouteEditPlan
    {
        public readonly ExistingRouteSnapshot Original;
        public readonly RouteEditMode Mode;
        public readonly ReadOnlyCollection<RouteStopSite> Stops;

        private RouteEditPlan(ExistingRouteSnapshot original, RouteEditMode mode,
                               IList<RouteStopSite> stops)
        {
            Original = original;
            Mode = mode;
            Stops = new List<RouteStopSite>(stops).AsReadOnly();
        }

        public static bool TryCreate(ExistingRouteSnapshot original, RouteEditMode mode,
                                      IList<RouteStopSite> stops, out RouteEditPlan plan,
                                      out string reason)
        {
            plan = null;
            reason = string.Empty;
            if (original == null || original.LineId == 0 || string.IsNullOrEmpty(original.PrefabName))
            {
                reason = "An existing line identity and bus prefab are required.";
                return false;
            }
            if (mode != RouteEditMode.Manual && mode != RouteEditMode.Automatic)
            {
                reason = "Unknown route editing mode.";
                return false;
            }
            if (!RouteCreationRules.IsStopCountAllowed(original.Stops.Count))
            {
                reason = "Existing route editing supports three to six stops.";
                return false;
            }
            if (stops == null || stops.Count != original.Stops.Count)
            {
                reason = "Move-only editing must retain the existing stop count; adding or removing stops is not supported.";
                return false;
            }
            HashSet<ushort> nodeIds = new HashSet<ushort>();
            for (int i = 0; i < original.Stops.Count; i++)
            {
                if (original.Stops[i].NodeId == 0 || !nodeIds.Add(original.Stops[i].NodeId) ||
                    !IsFinite(original.Stops[i].Position))
                {
                    reason = "The original route must contain distinct nonzero stop nodes and finite positions.";
                    return false;
                }
                if (!IsFinite(stops[i].Position))
                {
                    reason = "Target stop coordinates must be finite.";
                    return false;
                }
            }
            if (!RouteCreationRules.HasValidStopSites(stops))
            {
                reason = "Target stops must use three to six distinct captured lanes and remain more than 2.5 metres apart.";
                return false;
            }
            plan = new RouteEditPlan(original, mode, stops);
            return true;
        }

        private static bool IsFinite(Vector3 position)
        {
            return !float.IsNaN(position.x) && !float.IsInfinity(position.x) &&
                   !float.IsNaN(position.y) && !float.IsInfinity(position.y) &&
                   !float.IsNaN(position.z) && !float.IsInfinity(position.z);
        }
    }
}
