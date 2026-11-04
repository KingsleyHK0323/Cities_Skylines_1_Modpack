using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>One route stop bound to the actual lane selected during capture.</summary>
    internal struct RouteStopSite
    {
        public readonly ushort SegmentId;
        public readonly uint SegmentBuildIndex;
        public readonly bool Forward;
        public readonly int LaneIndex;
        public readonly Vector3 Position;
        public readonly bool FixedPlatform;

        public RouteStopSite(ushort segmentId, uint segmentBuildIndex, bool forward,
                             int laneIndex, Vector3 position, bool fixedPlatform)
        {
            SegmentId = segmentId;
            SegmentBuildIndex = segmentBuildIndex;
            Forward = forward;
            LaneIndex = laneIndex;
            Position = position;
            FixedPlatform = fixedPlatform;
        }
    }

    /// <summary>Fail-closed limits for Development proposal creation.</summary>
    internal static class RouteCreationRules
    {
        public const int MinimumStops = 3;
        public const int MaximumStops = 6;
        private const float MinimumDistinctStopDistanceSquared = 6.25f;

        public static bool IsStopCountAllowed(int count)
        {
            return count >= MinimumStops && count <= MaximumStops;
        }

        public static bool HasValidStopSites(IList<RouteStopSite> sites)
        {
            return GetStopSiteRejectionReason(sites).Length == 0;
        }

        // Signature: OpenAI-Codex_Automated_Public_Transport_Planning
        public static string GetStopSiteRejectionReason(IList<RouteStopSite> sites)
        {
            if (sites == null) { return "No captured stop list."; }
            if (!IsStopCountAllowed(sites.Count)) { return "Requires 3-6 captured stops."; }
            for (int i = 0; i < sites.Count; i++)
            {
                string stop = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (sites[i].SegmentId == 0) { return "Stop " + stop + " has no captured segment."; }
                if (sites[i].LaneIndex < 0) { return "Stop " + stop + " has no captured lane."; }
                Vector3 position = sites[i].Position;
                if (float.IsNaN(position.x) || float.IsInfinity(position.x) ||
                    float.IsNaN(position.y) || float.IsInfinity(position.y) ||
                    float.IsNaN(position.z) || float.IsInfinity(position.z))
                { return "Stop " + stop + " has non-finite captured coordinates."; }
                for (int j = 0; j < i; j++)
                {
                    string pair = "Stops " + (j + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) +
                        " and " + stop;
                    if (sites[i].SegmentId == sites[j].SegmentId &&
                        sites[i].LaneIndex == sites[j].LaneIndex)
                    {
                        return pair + " use the same captured segment/lane.";
                    }
                    if ((sites[i].Position - sites[j].Position).sqrMagnitude <=
                        MinimumDistinctStopDistanceSquared)
                    {
                        return pair + " are within 2.5 metres of each other.";
                    }
                }
            }
            return string.Empty;
        }
    }

    /// <summary>A single immutable route proposal and the reasons behind its score.</summary>
    internal sealed class RouteProposal
    {
        public readonly ushort HomeEndpoint;
        public readonly ushort AttractionEndpoint;
        public readonly double PairingScore;
        public readonly double HomeCoverage;
        public readonly double AttractionCoverage;
        public readonly double CoverageIndex;
        public readonly int HomePeopleCovered;
        public readonly int AttractionPeopleCovered;
        public readonly int StopCount;
        public readonly string Diagnostic;
        public readonly ReadOnlyCollection<RouteStopSite> StopSites;
        public readonly ReadOnlyCollection<ushort> StopSegments;

        public RouteProposal(ushort homeEndpoint, ushort attractionEndpoint,
                             double pairingScore, double homeCoverage,
                             double attractionCoverage, double coverageIndex,
                             int homePeopleCovered, int attractionPeopleCovered,
                             IList<RouteStopSite> stopSites, string diagnostic)
        {
            HomeEndpoint = homeEndpoint;
            AttractionEndpoint = attractionEndpoint;
            PairingScore = pairingScore;
            HomeCoverage = homeCoverage;
            AttractionCoverage = attractionCoverage;
            CoverageIndex = coverageIndex;
            HomePeopleCovered = homePeopleCovered;
            AttractionPeopleCovered = attractionPeopleCovered;
            List<RouteStopSite> stopCopy = new List<RouteStopSite>(stopSites ?? new RouteStopSite[0]);
            StopSites = stopCopy.AsReadOnly();
            List<ushort> segmentCopy = new List<ushort>(StopSites.Count);
            for (int i = 0; i < StopSites.Count; i++)
            {
                segmentCopy.Add(StopSites[i].SegmentId);
            }
            StopSegments = segmentCopy.AsReadOnly();
            StopCount = StopSites.Count;
            Diagnostic = diagnostic;
        }
    }

    /// <summary>Immutable result of the pure-data planning pass.</summary>
    internal sealed class PlanningPreviewResult
    {
        private readonly ReadOnlyCollection<RouteProposal> m_proposals;
        private readonly ReadOnlyCollection<string> m_diagnostics;

        public ReadOnlyCollection<RouteProposal> Proposals { get { return m_proposals; } }
        public ReadOnlyCollection<string> Diagnostics { get { return m_diagnostics; } }
        public int HomePeople { get; private set; }
        public int WorkPeople { get; private set; }
        public int VisitPeople { get; private set; }
        public int AssignedBuildings { get; private set; }
        public int UnassignedBuildings { get; private set; }
        public int GraphNodes { get; private set; }
        public PlanningSnapshot Snapshot { get; private set; }

        public PlanningPreviewResult(PlanningSnapshot snapshot, IList<RouteProposal> proposals,
                                     IList<string> diagnostics, int homePeople,
                                     int workPeople, int visitPeople,
                                     int assignedBuildings, int unassignedBuildings,
                                     int graphNodes)
        {
            Snapshot = snapshot == null ? null : snapshot.Copy();
            m_proposals = new List<RouteProposal>(proposals ?? new RouteProposal[0]).AsReadOnly();
            m_diagnostics = new List<string>(diagnostics ?? new string[0]).AsReadOnly();
            HomePeople = homePeople;
            WorkPeople = workPeople;
            VisitPeople = visitPeople;
            AssignedBuildings = assignedBuildings;
            UnassignedBuildings = unassignedBuildings;
            GraphNodes = graphNodes;
        }

        public string Describe()
        {
            System.Text.StringBuilder text = new System.Text.StringBuilder();
            text.Append("Read-only planning preview\n")
                .Append("Capture: ").Append(Snapshot == null ? "unavailable" : Snapshot.ElapsedMillis.ToString("F1"))
                .Append(" ms; slices=").Append(Snapshot == null ? 0 : Snapshot.SliceCount)
                .Append("; retries=").Append(Snapshot == null ? 0 : Snapshot.RetryCount)
                .Append("; max slice=").Append(Snapshot == null ? "0.00" : Snapshot.MaxSliceMillis.ToString("F2"))
                .Append(" ms; over 5 ms=").Append(Snapshot == null ? 0 : Snapshot.OverBudgetSliceCount)
                .Append("\nCaptured segments=").Append(Snapshot == null ? 0 : Snapshot.Segments.Count)
                .Append(" (created in network=").Append(Snapshot == null ? 0 : Snapshot.CreatedSegmentCount).Append(')')
                .Append("; buildings=").Append(Snapshot == null ? 0 : Snapshot.CreatedBuildingCount)
                .Append("; existing bus stops=").Append(Snapshot == null ? 0 : Snapshot.ExistingStopCount)
                .Append("; graph nodes=").Append(GraphNodes)
                // Signature: OpenAI-Codex_aptp_release_boundary.
                .Append("\n").Append(PlanningEngine.DemandProxyPlaceholder)
                .Append("\nCapacity proxy: home=").Append(HomePeople)
                .Append("; work=").Append(WorkPeople)
                .Append("; visit=").Append(VisitPeople)
                .Append("; assigned buildings=").Append(AssignedBuildings)
                .Append("; unassigned buildings=").Append(UnassignedBuildings)
                // OpenAI-Codex: clarify that coverage is route-segment based and expose search caps.
                .Append("\nRoute-segment demand coverage estimate: 50% home + 50% work/visit; this is not stop-access coverage. Creation requires 3-6 distinct captured stop lanes.")
                .Append("\nCandidate search caps: up to 12 home and 12 work/visit endpoints; up to 8 pairs ranked by normalized complementary demand are evaluated.");

            if (Snapshot != null && Snapshot.RetryCount > 0)
            {
                text.Append("\nCapture retries=").Append(Snapshot.RetryCount)
                    .Append("; retry reason(s): ").Append(Snapshot.RetryDiagnostic);
            }

            if (Snapshot != null)
            {
                text.Append("\nVersion diagnostics: buildIndex start=").Append(Snapshot.BuildIndexAtStart)
                    .Append(" end=").Append(Snapshot.BuildIndexAtEnd)
                    .Append("; structure start=[").Append(Snapshot.SignatureAtStart)
                    .Append("] end=[").Append(Snapshot.SignatureAtEnd).Append(']');
            }

            for (int i = 0; i < m_proposals.Count; i++)
            {
                RouteProposal proposal = m_proposals[i];
                text.Append("\nProposal ").Append(i + 1)
                    .Append(": endpoints ").Append(proposal.HomeEndpoint).Append(" ↔ ")
                    .Append(proposal.AttractionEndpoint)
                    .Append("; pairing=").Append(proposal.PairingScore.ToString("P1"));
                if (proposal.CoverageIndex >= 0.0)
                {
                    text.Append("; coverage=").Append(proposal.CoverageIndex.ToString("F1"))
                        .Append("; home=").Append(proposal.HomeCoverage.ToString("P1"))
                        .Append("; work/visit=").Append(proposal.AttractionCoverage.ToString("P1"));
                }
                text.Append("; stop sites (segment/direction, lane, build)=");
                int shownStops = Math.Min(proposal.StopSites.Count, 12);
                for (int stop = 0; stop < shownStops; stop++)
                {
                    if (stop > 0) { text.Append(','); }
                    RouteStopSite site = proposal.StopSites[stop];
                    text.Append(RouteStopPreview.Describe(site.SegmentId, site.SegmentBuildIndex,
                        site.Forward, site.LaneIndex));
                }
                if (proposal.StopSites.Count > shownStops) { text.Append(",..."); }
                string rejection = RouteCreationRules.GetStopSiteRejectionReason(proposal.StopSites);
                text.Append("; captured stop list: ").Append(rejection.Length == 0
                    ? "passes copied-data checks only; manual confirmation and live revalidation still required"
                    : "not eligible - " + rejection);
                text.Append("; ").Append(proposal.Diagnostic);
            }

            for (int i = 0; i < m_diagnostics.Count; i++)
            {
                text.Append("\nDiagnostic: ").Append(m_diagnostics[i]);
            }

            text.Append("\n").Append(PlanningEngine.SnapshotStalePlaceholder)
                .Append(" Turn restrictions, lane-level turns, and TM:PE rules are not modeled.");
            return text.ToString();
        }
    }

    /// <summary>
    /// Calculates demand assignment, endpoint pairing, directed shortest paths and
    /// coverage using copied values only. It never calls a game manager or API.
    /// </summary>
    internal static class PlanningEngine
    {
        internal const string SnapshotStalePlaceholder = "Snapshot may become stale after city changes.";
        internal const string DemandProxyPlaceholder = "Demand is estimated from five capacity slots per citizen unit, not actual people or ridership.";
        private const float GridCellSize = 256f;
        private const float TargetStopSpacing = 400f;
        private const float MinimumStopSpacing = TargetStopSpacing * 0.75f;
        private const float MaximumStopSpacing = TargetStopSpacing * 1.25f;
        private const int MaxEndpointsPerCategory = 12;
        private const int MaxEvaluatedPairs = 8;

        public static PlanningPreviewResult CreatePreview(PlanningSnapshot snapshot)
        {
            List<string> diagnostics = new List<string>();
            List<RouteProposal> proposals = new List<RouteProposal>();
            if (snapshot == null)
            {
                diagnostics.Add("No validated snapshot was supplied.");
                return new PlanningPreviewResult(null, proposals, diagnostics, 0, 0, 0, 0, 0, 0);
            }

            DemandField field = BuildDemandField(snapshot);
            WorldSnapshot world = snapshot.ToWorldSnapshot();
            StopGraph graph = StopGraph.Build(world);
            int graphNodes = graph == null ? 0 : graph.Count;
            if (field.TotalHome == 0)
            {
                diagnostics.Add("No Home demand was found; the 50/50 coverage index is unavailable.");
            }
            if (field.TotalAttraction == 0)
            {
                diagnostics.Add("No Work or Visit demand was found; the 50/50 coverage index is unavailable.");
            }
            if (field.UnassignedBuildings > 0)
            {
                diagnostics.Add(field.UnassignedBuildings + " demand building(s) had no anchorable road segment and were left unassigned.");
            }
            if (graph == null || graph.Count == 0)
            {
                diagnostics.Add("No directed route graph nodes could be built from anchorable segments.");
                return new PlanningPreviewResult(snapshot, proposals, diagnostics,
                    field.TotalHome, field.TotalWork, field.TotalVisit,
                    field.AssignedBuildings, field.UnassignedBuildings, graphNodes);
            }

            List<int> homes = TopIndices(field.HomeBySegment, MaxEndpointsPerCategory);
            List<int> attractions = TopIndices(field.AttractionBySegment, MaxEndpointsPerCategory);
            List<EndpointPair> pairs = BuildPairs(homes, attractions, field);
            if (pairs.Count == 0)
            {
                diagnostics.Add("No distinct home-to-work/visit endpoint pair had measurable demand.");
                return new PlanningPreviewResult(snapshot, proposals, diagnostics,
                    field.TotalHome, field.TotalWork, field.TotalVisit,
                    field.AssignedBuildings, field.UnassignedBuildings, graphNodes);
            }

            Dictionary<ushort, int> snapshotIndexById = new Dictionary<ushort, int>();
            for (int i = 0; i < snapshot.Segments.Count; i++)
            {
                snapshotIndexById[snapshot.Segments[i].Id] = i;
            }

            int evaluated = 0;
            for (int i = 0; i < pairs.Count && evaluated < MaxEvaluatedPairs; i++)
            {
                EndpointPair pair = pairs[i];
                evaluated++;
                ushort homeId = snapshot.Segments[pair.HomeIndex].Id;
                ushort attractionId = snapshot.Segments[pair.AttractionIndex].Id;
                int[] outward;
                int[] returnPath;
                string reason;

                if (!FindPath(graph, snapshot, homeId, attractionId, snapshotIndexById, out outward))
                {
                    reason = "no directed path from the home endpoint to the work/visit endpoint";
                    proposals.Add(Unroutable(homeId, attractionId, pair.Score, reason));
                    continue;
                }
                if (!FindPath(graph, snapshot, attractionId, homeId, snapshotIndexById, out returnPath))
                {
                    reason = "outbound path exists, but no directed return path was found";
                    proposals.Add(Unroutable(homeId, attractionId, pair.Score, reason));
                    continue;
                }

                List<RouteStopSite> stopSites = new List<RouteStopSite>();
                AppendStops(outward, graph, snapshot, field, stopSites);
                AppendStops(returnPath, graph, snapshot, field, stopSites);
                int[] covered = DistinctPathSegments(outward, returnPath, graph);
                int homeCovered;
                int attractionCovered;
                Coverage(field, covered, out homeCovered, out attractionCovered);
                double homeRate = field.TotalHome == 0 ? -1.0 : (double)homeCovered / field.TotalHome;
                double attractionRate = field.TotalAttraction == 0 ? -1.0 : (double)attractionCovered / field.TotalAttraction;
                double coverageIndex = homeRate < 0.0 || attractionRate < 0.0
                    ? -1.0 : 100.0 * (0.5 * homeRate + 0.5 * attractionRate);
                string diagnostic = "directed out-and-back path; shortest-path cost uses segment length / fastest eligible lane speed; stop candidates use 300-500 m spacing and demand density";
                proposals.Add(new RouteProposal(homeId, attractionId, pair.Score,
                    homeRate, attractionRate, coverageIndex, homeCovered,
                    attractionCovered, stopSites, diagnostic));
            }

            if (pairs.Count > evaluated)
            {
                diagnostics.Add("Evaluated the top " + evaluated + " endpoint pairs by normalized complementary demand; " +
                    (pairs.Count - evaluated) + " lower-ranked pairs were not searched in this preview.");
            }
            if (proposals.Count == 0)
            {
                diagnostics.Add("No endpoint pair was evaluated.");
            }

            proposals.Sort(CompareProposals);
            return new PlanningPreviewResult(snapshot, proposals, diagnostics,
                field.TotalHome, field.TotalWork, field.TotalVisit,
                field.AssignedBuildings, field.UnassignedBuildings, graphNodes);
        }

        private static RouteProposal Unroutable(ushort homeId, ushort attractionId,
                                                double score, string reason)
        {
            return new RouteProposal(homeId, attractionId, score, -1.0, -1.0,
                -1.0, 0, 0, new RouteStopSite[0], "unavailable: " + reason);
        }

        private static int CompareProposals(RouteProposal left, RouteProposal right)
        {
            int leftAvailable = left.CoverageIndex >= 0.0 ? 1 : 0;
            int rightAvailable = right.CoverageIndex >= 0.0 ? 1 : 0;
            if (leftAvailable != rightAvailable) { return rightAvailable.CompareTo(leftAvailable); }
            int coverage = right.CoverageIndex.CompareTo(left.CoverageIndex);
            if (coverage != 0) { return coverage; }
            return right.PairingScore.CompareTo(left.PairingScore);
        }

        private static DemandField BuildDemandField(PlanningSnapshot snapshot)
        {
            int count = snapshot.Segments.Count;
            DemandField field = new DemandField(count);
            SpatialIndex index = new SpatialIndex(snapshot);

            for (int b = 0; b < snapshot.Buildings.Count; b++)
            {
                BuildingDemandData building = snapshot.Buildings[b];
                if (building.HomePeople <= 0 && building.WorkPeople <= 0 && building.VisitPeople <= 0)
                {
                    continue;
                }

                field.TotalHome += building.HomePeople;
                field.TotalWork += building.WorkPeople;
                field.TotalVisit += building.VisitPeople;
                int segment = index.FindNearest(building.Position);
                if (segment < 0)
                {
                    field.UnassignedBuildings++;
                    continue;
                }

                field.AssignedBuildings++;
                field.HomeBySegment[segment] += building.HomePeople;
                field.WorkBySegment[segment] += building.WorkPeople;
                field.AttractionBySegment[segment] += building.WorkPeople + building.VisitPeople;
            }

            return field;
        }

        private static List<int> TopIndices(double[] values, int limit)
        {
            List<int> indices = new List<int>();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] <= 0.0) { continue; }
                int insert = indices.Count;
                for (int j = 0; j < indices.Count; j++)
                {
                    if (values[i] > values[indices[j]]) { insert = j; break; }
                }
                indices.Insert(insert, i);
                if (indices.Count > limit) { indices.RemoveAt(indices.Count - 1); }
            }
            return indices;
        }

        private static List<EndpointPair> BuildPairs(List<int> homes, List<int> attractions,
                                                     DemandField field)
        {
            List<EndpointPair> pairs = new List<EndpointPair>();
            if (field.TotalHome <= 0 || field.TotalAttraction <= 0) { return pairs; }
            for (int h = 0; h < homes.Count; h++)
            {
                for (int a = 0; a < attractions.Count; a++)
                {
                    if (homes[h] == attractions[a]) { continue; }
                    double homeH = field.HomeBySegment[homes[h]] / field.TotalHome;
                    double homeA = field.HomeBySegment[attractions[a]] / field.TotalHome;
                    double attractionH = field.AttractionBySegment[homes[h]] / field.TotalAttraction;
                    double attractionA = field.AttractionBySegment[attractions[a]] / field.TotalAttraction;
                    pairs.Add(new EndpointPair(homes[h], attractions[a],
                        homeH * attractionA + homeA * attractionH));
                }
            }
            pairs.Sort(delegate(EndpointPair left, EndpointPair right)
            {
                return right.Score.CompareTo(left.Score);
            });
            return pairs;
        }

        private static bool FindPath(StopGraph graph, PlanningSnapshot snapshot,
                                     ushort startId, ushort targetId,
                                     Dictionary<ushort, int> snapshotIndexById,
                                     out int[] result)
        {
            result = null;
            int startSegment;
            int targetSegment;
            if (!snapshotIndexById.TryGetValue(startId, out startSegment) ||
                !snapshotIndexById.TryGetValue(targetId, out targetSegment)) { return false; }

            int count = graph.Count;
            float[] cost = new float[count];
            int[] previous = new int[count];
            bool[] closed = new bool[count];
            for (int i = 0; i < count; i++)
            {
                cost[i] = float.PositiveInfinity;
                previous[i] = -1;
            }

            MinHeap open = new MinHeap();
            Vector3 targetPosition = snapshotPositionForSegment(graph, targetSegment);
            for (int i = 0; i < count; i++)
            {
                if (graph.SegmentIndex[i] != startSegment) { continue; }
                cost[i] = graph.TraversalCost[i];
                float startLength = snapshot.Segments[graph.SegmentIndex[i]].Length;
                open.Push(i, cost[i] + Heuristic(graph.Position[i], targetPosition,
                                                  graph.MinCostPerMetre, startLength));
            }
            if (open.Count == 0) { return false; }

            int found = -1;
            while (open.Count > 0)
            {
                HeapEntry entry = open.Pop();
                int current = entry.Node;
                if (closed[current]) { continue; }
                closed[current] = true;
                if (graph.SegmentIndex[current] == targetSegment)
                {
                    found = current;
                    break;
                }

                int[] neighbours = graph.Adjacency[current];
                for (int n = 0; n < neighbours.Length; n++)
                {
                    int next = neighbours[n];
                    if (closed[next]) { continue; }
                    float candidate = cost[current] + graph.TraversalCost[next];
                    if (candidate >= cost[next]) { continue; }
                    cost[next] = candidate;
                    previous[next] = current;
                    float nextLength = snapshot.Segments[graph.SegmentIndex[next]].Length;
                    float priority = candidate + Heuristic(graph.Position[next], targetPosition,
                                                            graph.MinCostPerMetre, nextLength);
                    open.Push(next, priority);
                }
            }
            if (found < 0) { return false; }

            List<int> path = new List<int>();
            for (int at = found; at >= 0; at = previous[at])
            {
                path.Add(at);
            }
            path.Reverse();
            result = path.ToArray();
            return true;
        }

        private static Vector3 snapshotPositionForSegment(StopGraph graph, int segmentIndex)
        {
            for (int i = 0; i < graph.Count; i++)
            {
                if (graph.SegmentIndex[i] == segmentIndex) { return graph.Position[i]; }
            }
            return Vector3.zero;
        }

        private static float Heuristic(Vector3 from, Vector3 to, float minimumCostPerMetre,
                                       float currentSegmentLength)
        {
            // The graph charges a full segment when its node is entered. Subtracting
            // half of the current segment keeps the midpoint-to-midpoint Euclidean
            // estimate below the remaining traversal cost.
            float remainingDistance = Vector3.Distance(from, to) - currentSegmentLength * 0.5f;
            return Math.Max(0f, remainingDistance) * minimumCostPerMetre;
        }

        private static void AppendStops(int[] path, StopGraph graph,
                                        PlanningSnapshot snapshot, DemandField field,
                                        List<RouteStopSite> output)
        {
            if (path == null || path.Length == 0) { return; }

            int current = -1;
            while (current < path.Length - 1)
            {
                float distance = 0f;
                int chosen = -1;
                int nextEligible = -1;
                double bestDemand = -1.0;
                int first = current + 1;
                for (int i = first; i < path.Length; i++)
                {
                    int segment = graph.SegmentIndex[path[i]];
                    if (current >= 0)
                    {
                        distance += snapshot.Segments[segment].Length;
                        if (distance > MaximumStopSpacing) { break; }
                    }

                    RouteStopSite candidate;
                    if (TryCreateStopSite(path[i], graph, snapshot, out candidate) &&
                        !ContainsStopSite(output, candidate))
                    {
                        nextEligible = i;
                        if (current < 0)
                        {
                            chosen = i;
                            break;
                        }

                        double demand = field.HomeBySegment[segment] + field.AttractionBySegment[segment];
                        if (distance >= MinimumStopSpacing && distance <= MaximumStopSpacing &&
                            demand > bestDemand)
                        {
                            bestDemand = demand;
                            chosen = i;
                        }
                    }
                }

                if (chosen < 0) { chosen = nextEligible; }
                if (chosen < 0) { break; }

                RouteStopSite selected;
                if (!TryCreateStopSite(path[chosen], graph, snapshot, out selected)) { break; }
                if (!ContainsStopSite(output, selected)) { output.Add(selected); }
                current = chosen;
            }
        }

        private static bool TryCreateStopSite(int node, StopGraph graph,
                                              PlanningSnapshot snapshot,
                                              out RouteStopSite stopSite)
        {
            SegmentSnapshotData segment = snapshot.Segments[graph.SegmentIndex[node]];
            bool forward = graph.Forward[node];
            if (forward)
            {
                if (!segment.ForwardCanAnchor || segment.ForwardLaneIndex < 0)
                {
                    stopSite = new RouteStopSite();
                    return false;
                }
                stopSite = new RouteStopSite(segment.Id, segment.BuildIndex, true,
                    segment.ForwardLaneIndex, segment.ForwardStopPosition,
                    segment.ForwardFixedPlatform);
                return true;
            }

            if (!segment.BackwardCanAnchor || segment.BackwardLaneIndex < 0)
            {
                stopSite = new RouteStopSite();
                return false;
            }
            stopSite = new RouteStopSite(segment.Id, segment.BuildIndex, false,
                segment.BackwardLaneIndex, segment.BackwardStopPosition,
                segment.BackwardFixedPlatform);
            return true;
        }

        private static bool ContainsStopSite(IList<RouteStopSite> sites, RouteStopSite candidate)
        {
            for (int i = 0; i < sites.Count; i++)
            {
                if (sites[i].SegmentId == candidate.SegmentId &&
                    sites[i].LaneIndex == candidate.LaneIndex)
                {
                    return true;
                }
            }
            return false;
        }

        private static int[] DistinctPathSegments(int[] outward, int[] returning, StopGraph graph)
        {
            HashSet<int> unique = new HashSet<int>();
            if (outward != null)
            {
                for (int i = 0; i < outward.Length; i++) { unique.Add(graph.SegmentIndex[outward[i]]); }
            }
            if (returning != null)
            {
                for (int i = 0; i < returning.Length; i++) { unique.Add(graph.SegmentIndex[returning[i]]); }
            }
            int[] result = new int[unique.Count];
            unique.CopyTo(result);
            return result;
        }

        private static void Coverage(DemandField field, int[] segments,
                                     out int home, out int attraction)
        {
            home = 0;
            attraction = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                home += (int)field.HomeBySegment[segments[i]];
                attraction += (int)field.AttractionBySegment[segments[i]];
            }
        }

        private sealed class DemandField
        {
            public readonly double[] HomeBySegment;
            public readonly double[] WorkBySegment;
            public readonly double[] AttractionBySegment;
            public int TotalHome;
            public int TotalWork;
            public int TotalVisit;
            public int AssignedBuildings;
            public int UnassignedBuildings;
            public int TotalAttraction { get { return TotalWork + TotalVisit; } }

            public DemandField(int count)
            {
                HomeBySegment = new double[count];
                WorkBySegment = new double[count];
                AttractionBySegment = new double[count];
            }
        }

        private sealed class SpatialIndex
        {
            private readonly Dictionary<long, List<int>> m_cells = new Dictionary<long, List<int>>();
            private readonly int m_minX;
            private readonly int m_maxX;
            private readonly int m_minZ;
            private readonly int m_maxZ;
            private readonly PlanningSnapshot m_snapshot;
            private bool m_hasBounds;

            public SpatialIndex(PlanningSnapshot snapshot)
            {
                m_snapshot = snapshot;
                int minX = int.MaxValue;
                int maxX = int.MinValue;
                int minZ = int.MaxValue;
                int maxZ = int.MinValue;
                for (int i = 0; i < snapshot.Segments.Count; i++)
                {
                    SegmentSnapshotData segment = snapshot.Segments[i];
                    if (!segment.CanAnchor) { continue; }
                    int x = Cell(segment.MidPoint.x);
                    int z = Cell(segment.MidPoint.z);
                    long key = Key(x, z);
                    List<int> list;
                    if (!m_cells.TryGetValue(key, out list))
                    {
                        list = new List<int>();
                        m_cells.Add(key, list);
                    }
                    list.Add(i);
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minZ = Math.Min(minZ, z);
                    maxZ = Math.Max(maxZ, z);
                }
                m_hasBounds = minX != int.MaxValue;
                m_minX = m_hasBounds ? minX : 0;
                m_maxX = m_hasBounds ? maxX : 0;
                m_minZ = m_hasBounds ? minZ : 0;
                m_maxZ = m_hasBounds ? maxZ : 0;
            }

            public int FindNearest(Vector3 point)
            {
                if (!m_hasBounds) { return -1; }
                int cx = Cell(point.x);
                int cz = Cell(point.z);
                int maxRing = Math.Max(Math.Max(Math.Abs(cx - m_minX), Math.Abs(cx - m_maxX)),
                                       Math.Max(Math.Abs(cz - m_minZ), Math.Abs(cz - m_maxZ))) + 1;
                int best = -1;
                float bestSquared = float.PositiveInfinity;
                for (int ring = 0; ring <= maxRing; ring++)
                {
                    for (int x = cx - ring; x <= cx + ring; x++)
                    {
                        VisitCell(x, cz - ring, point, ref best, ref bestSquared);
                        if (ring > 0) { VisitCell(x, cz + ring, point, ref best, ref bestSquared); }
                    }
                    for (int z = cz - ring + 1; z < cz + ring; z++)
                    {
                        VisitCell(cx - ring, z, point, ref best, ref bestSquared);
                        if (ring > 0) { VisitCell(cx + ring, z, point, ref best, ref bestSquared); }
                    }

                    if (best >= 0)
                    {
                        float left = point.x - ((cx - ring) * GridCellSize);
                        float right = ((cx + ring + 1) * GridCellSize) - point.x;
                        float bottom = point.z - ((cz - ring) * GridCellSize);
                        float top = ((cz + ring + 1) * GridCellSize) - point.z;
                        float outside = Math.Min(Math.Min(left, right), Math.Min(bottom, top));
                        if (outside < 0f) { outside = 0f; }
                        if (outside * outside > bestSquared) { break; }
                    }
                }
                return best;
            }

            private void VisitCell(int x, int z, Vector3 point, ref int best, ref float bestSquared)
            {
                List<int> candidates;
                if (!m_cells.TryGetValue(Key(x, z), out candidates)) { return; }
                for (int i = 0; i < candidates.Count; i++)
                {
                    int index = candidates[i];
                    Vector3 position = m_snapshot.Segments[index].MidPoint;
                    float dx = position.x - point.x;
                    float dz = position.z - point.z;
                    float squared = dx * dx + dz * dz;
                    if (squared < bestSquared)
                    {
                        bestSquared = squared;
                        best = index;
                    }
                }
            }

            private static int Cell(float coordinate)
            {
                return (int)Math.Floor(coordinate / GridCellSize);
            }

            private static long Key(int x, int z)
            {
                return ((long)x << 32) ^ (uint)z;
            }
        }

        private struct EndpointPair
        {
            public readonly int HomeIndex;
            public readonly int AttractionIndex;
            public readonly double Score;
            public EndpointPair(int homeIndex, int attractionIndex, double score)
            {
                HomeIndex = homeIndex;
                AttractionIndex = attractionIndex;
                Score = score;
            }
        }

        private struct HeapEntry
        {
            public readonly int Node;
            public readonly float Priority;
            public HeapEntry(int node, float priority) { Node = node; Priority = priority; }
        }

        private sealed class MinHeap
        {
            private readonly List<HeapEntry> m_items = new List<HeapEntry>();
            public int Count { get { return m_items.Count; } }

            public void Push(int node, float priority)
            {
                int index = m_items.Count;
                m_items.Add(new HeapEntry(node, priority));
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (m_items[parent].Priority <= priority) { break; }
                    m_items[index] = m_items[parent];
                    index = parent;
                }
                m_items[index] = new HeapEntry(node, priority);
            }

            public HeapEntry Pop()
            {
                HeapEntry root = m_items[0];
                HeapEntry last = m_items[m_items.Count - 1];
                m_items.RemoveAt(m_items.Count - 1);
                if (m_items.Count == 0) { return root; }
                int index = 0;
                while (true)
                {
                    int left = index * 2 + 1;
                    if (left >= m_items.Count) { break; }
                    int right = left + 1;
                    int child = right < m_items.Count && m_items[right].Priority < m_items[left].Priority
                        ? right : left;
                    if (m_items[child].Priority >= last.Priority) { break; }
                    m_items[index] = m_items[child];
                    index = child;
                }
                m_items[index] = last;
                return root;
            }
        }
    }
}
