#if DEVTOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ColossalFramework;
using UnityEngine;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning.Planning
{
    // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
    // Move-only Development prototype. Never releases/recreates a line or claims atomic rollback.
    internal static class RouteEditingController
    {
        private static readonly object Sync = new object();
        private static RouteEditPlan s_plan;
        private static string s_status = "No existing-route edit prepared.";
        private static string s_preview = string.Empty;
        private static readonly RouteEditRequestGate Requests = new RouteEditRequestGate();
        private static PendingCheck s_check;
        public static RouteEditPlan CurrentPlan { get { lock (Sync) { return s_plan; } } }
        public static string StatusText { get { lock (Sync) { return s_status; } } }
        public static string PreviewText { get { lock (Sync) { return s_preview; } } }
        public static bool IsBusy { get { return Requests.IsBusy; } }

        public static void ResetForLevelChange()
        {
            Requests.Reset(delegate
            {
                lock (Sync) { s_check = null; s_plan = null; s_preview = string.Empty; s_status = "City changed; prepare a new edit."; }
            });
        }

        public static void RequestPreview(ushort lineId, string manualOrder, int proposalIndex, bool automatic)
        {
            Queue(delegate(RouteEditRequestTicket ticket)
            {
                if (!Requests.TryPublish(ticket, delegate { lock (Sync) { s_plan = null; s_preview = string.Empty; } })) { return; }
                ExistingRouteSnapshot original = Capture(lineId);
                PlanningPreviewResult planning = PlanningCapture.LatestPreview;
                List<RouteStopSite> targets = new List<RouteStopSite>();
                if (automatic)
                {
                    RequirePlanning(planning, original);
                    if (proposalIndex < 0 || proposalIndex >= planning.Proposals.Count) { throw new InvalidOperationException("Select a valid one-based proposal number."); }
                    targets.AddRange(planning.Proposals[proposalIndex].StopSites);
                }
                else
                {
                    System.Collections.ObjectModel.ReadOnlyCollection<RouteManualToken> tokens;
                    string inputReason;
                    if (!RouteManualInput.TryParse(manualOrder, original.Stops.Count, out tokens, out inputReason))
                    { throw new InvalidOperationException(inputReason); }
                    foreach (RouteManualToken token in tokens)
                    {
                        if (token.OriginalStopIndex != 0)
                        {
                            targets.Add(ExistingSite(original.Stops[token.OriginalStopIndex - 1]));
                        }
                        else
                        {
                            RequirePlanning(planning, original);
                            int index = token.SegmentId;
                            bool found = false;
                            foreach (SegmentSnapshotData segment in planning.Snapshot.Segments)
                            {
                                if (segment.Id != index) { continue; }
                                bool forward = token.Forward;
                                if (forward ? !segment.ForwardCanAnchor : !segment.BackwardCanAnchor) { throw new InvalidOperationException("The requested captured road direction is unavailable."); }
                                targets.Add(new RouteStopSite(segment.Id, segment.BuildIndex, forward,
                                    forward ? segment.ForwardLaneIndex : segment.BackwardLaneIndex,
                                    forward ? segment.ForwardStopPosition : segment.BackwardStopPosition,
                                    forward ? segment.ForwardFixedPlatform : segment.BackwardFixedPlatform));
                                found = true;
                                break;
                            }
                            if (!found) { throw new InvalidOperationException("Road segment is not in the current planning capture."); }
                        }
                    }
                }
                RouteEditPlan plan;
                string reason;
                if (!RouteEditPlan.TryCreate(original, automatic ? RouteEditMode.Automatic : RouteEditMode.Manual, targets, out plan, out reason)) { throw new InvalidOperationException(reason); }
                ValidateTargets(plan);
                StringBuilder text = new StringBuilder("Line ").Append(lineId).Append(" | ").Append(plan.Mode).Append(" | ").Append(targets.Count).Append(" stops (unchanged)\n");
                for (int i = 0; i < targets.Count; i++)
                {
                    text.Append(i + 1).Append(": node ").Append(original.Stops[i].NodeId).Append(' ').Append(original.Stops[i].Position)
                        .Append(" -> segment ").Append(targets[i].SegmentId).Append(" lane ").Append(targets[i].LaneIndex).Append(' ').Append(targets[i].Position).Append('\n');
                }
                Requests.TryPublish(ticket, delegate { lock (Sync) { s_plan = plan; s_preview = text.ToString(); s_status = "Preview only. Back up the save; two confirmations are required to apply."; } });
            }, "Prepare existing-route edit");
        }

        public static void RequestApply(RouteEditPlan plan)
        {
            Queue(delegate(RouteEditRequestTicket ticket)
            {
                if (plan == null || !object.ReferenceEquals(plan, CurrentPlan)) { throw new InvalidOperationException("The displayed edit is no longer current."); }
                ExistingRouteSnapshot current = Capture(plan.Original.LineId);
                if (!plan.Original.Matches(current.LineId, current.BuildIndex, current.PrefabName, current.Stops)) { throw new InvalidOperationException("The city or original route changed; prepare a new edit."); }
                ValidateTargets(plan);
                if (!Requests.TryPublish(ticket, delegate { lock (Sync) { s_plan = null; } })) { return; }
                LiveSurface surface = new LiveSurface(plan.Original.LineId, plan.Stops.Count, ticket);
                RouteEditExecutionResult result = RouteEditExecutor.Execute(plan, surface);
                if (result.Outcome == RouteEditExecutionOutcome.Applied)
                {
                    Requests.TryPublish(ticket, delegate
                    {
                        lock (Sync)
                        {
                            s_check = new PendingCheck(plan.Original.LineId, plan.Stops.Count, ticket, surface);
                            s_status = "Edit applied to existing line " + plan.Original.LineId + "; " + result.Detail;
                        }
                    });
                }
                else
                {
                    SetOwnedStatus(ticket, result.Outcome + ": " + result.Detail);
                    Log.Warning("Existing-route execution " + result.Outcome + ": " + result.Detail);
                }
            }, "Apply existing-route edit");
        }

        private static void Queue(Action<RouteEditRequestTicket> action, string context)
        {
            RouteEditRequestTicket ticket;
            if (RouteCreationController.IsBusy || !Requests.TryBegin(out ticket)) { SetStatus("Another route operation is pending."); return; }
            if (!Singleton<SimulationManager>.exists)
            {
                SetOwnedStatus(ticket, "No city is loaded."); Requests.Complete(ticket); return;
            }
            try
            {
                Singleton<SimulationManager>.instance.AddAction(delegate
                {
                    if (!Requests.IsOwner(ticket)) { return; }
                    try { action(ticket); }
                    catch (Exception error)
                    {
                        Requests.TryPublish(ticket, delegate { lock (Sync) { s_plan = null; s_status = context + " refused: " + error.Message; } });
                        Log.Exception(context, error);
                    }
                    finally
                    {
                        PendingCheck pending;
                        lock (Sync) { pending = s_check; }
                        if (pending == null || !object.ReferenceEquals(pending.Ticket, ticket)) { Requests.Complete(ticket); }
                    }
                });
            }
            catch (Exception error)
            {
                SetOwnedStatus(ticket, context + " could not be queued."); Requests.Complete(ticket); Log.Exception(context, error);
            }
        }

        private sealed class LiveSurface : IRouteEditSurface
        {
            private readonly ushort m_line;
            private readonly int m_count;
            private readonly RouteEditRequestTicket m_ticket;
            private readonly TransportManager m_transport;
            private readonly NetManager m_net;
            private readonly SimulationManager m_simulation;
            private readonly TransportInfo m_info;
            private readonly ushort[] m_nodes;
            public LiveSurface(ushort line, int count, RouteEditRequestTicket ticket)
            {
                m_line = line; m_count = count; m_ticket = ticket;
                m_transport = Singleton<TransportManager>.instance;
                m_net = Singleton<NetManager>.instance;
                m_simulation = Singleton<SimulationManager>.instance;
                m_info = m_transport.m_lines.m_buffer[line].Info;
                m_nodes = new ushort[count];
                RememberOwnStopNodes();
            }
            public bool IsCurrent
            {
                get { return Requests.IsOwner(m_ticket) && Singleton<TransportManager>.exists && Singleton<NetManager>.exists && Singleton<SimulationManager>.exists &&
                    object.ReferenceEquals(m_transport, Singleton<TransportManager>.instance) && object.ReferenceEquals(m_net, Singleton<NetManager>.instance) && object.ReferenceEquals(m_simulation, Singleton<SimulationManager>.instance); }
            }
            public bool HasExpectedIdentity
            {
                get
                {
                    try
                    {
                        if (!HeaderMatches() || StopCount != m_count) { return false; }
                        for (int i = 0; i < m_count; i++)
                        {
                            if (m_nodes[i] == 0 || m_transport.m_lines.m_buffer[m_line].GetStop(i) != m_nodes[i]) { return false; }
                        }
                        return true;
                    }
                    catch { return false; }
                }
            }
            private bool HeaderMatches()
            {
                return IsCurrent && m_line != 0 && m_line < m_transport.m_lines.m_buffer.Length &&
                    (m_transport.m_lines.m_buffer[m_line].m_flags & TransportLine.Flags.Created) != 0 && object.ReferenceEquals(m_info, m_transport.m_lines.m_buffer[m_line].Info);
            }
            private void RememberOwnStopNodes()
            {
                // A native MoveStop can change node IDs even when it returns false.
                // Adopt only the result of our own call, never an intervening external replacement.
                if (!HeaderMatches() || StopCount != m_count) { return; }
                for (int i = 0; i < m_count; i++) { m_nodes[i] = m_transport.m_lines.m_buffer[m_line].GetStop(i); }
            }
            public int StopCount { get { return m_transport.m_lines.m_buffer[m_line].CountStops(m_line); } }
            public bool Complete { get { return m_transport.m_lines.m_buffer[m_line].Complete; } }
            private void EnsureCurrent()
            {
                if (!IsCurrent || !HasExpectedIdentity || StopCount != m_count) { throw new InvalidOperationException("The request, city or line identity changed."); }
            }
            public Vector3 ReadPosition(int index)
            {
                EnsureCurrent();
                if (index < 0 || index >= m_count) { throw new ArgumentOutOfRangeException("index"); }
                ushort node = m_transport.m_lines.m_buffer[m_line].GetStop(index);
                if (node == 0 || node >= m_net.m_nodes.m_buffer.Length) { throw new InvalidOperationException("A route stop is missing."); }
                return m_net.m_nodes.m_buffer[node].m_position;
            }
            public bool MoveStop(int index, Vector3 position, bool fixedPlatform)
            {
                EnsureCurrent();
                try { return m_transport.m_lines.m_buffer[m_line].MoveStop(m_line, index, position, fixedPlatform); }
                finally { RememberOwnStopNodes(); }
            }
            public void UpdateLine() { EnsureCurrent(); m_transport.UpdateLine(m_line); }
        }

        private sealed class PendingCheck
        {
            public readonly ushort Line;
            public readonly int Stops;
            public readonly RouteEditRequestTicket Ticket;
            public readonly LiveSurface Surface;
            public int Frames;
            public PendingCheck(ushort line, int stops, RouteEditRequestTicket ticket, LiveSurface surface)
            { Line = line; Stops = stops; Ticket = ticket; Surface = surface; }
        }

        private static ExistingRouteSnapshot Capture(ushort line)
        {
            if (!Singleton<TransportManager>.exists || !Singleton<NetManager>.exists || !Singleton<SimulationManager>.exists) { throw new InvalidOperationException("City managers are unavailable."); }
            TransportManager tm = Singleton<TransportManager>.instance;
            if (line == 0 || line >= tm.m_lines.m_buffer.Length || (tm.m_lines.m_buffer[line].m_flags & TransportLine.Flags.Created) == 0 || !tm.m_lines.m_buffer[line].Complete) { throw new InvalidOperationException("Select an existing complete city bus line ID."); }
            TransportInfo info = tm.m_lines.m_buffer[line].Info;
            if (info == null || info.m_transportType != TransportInfo.TransportType.Bus || info.m_class == null || info.m_class.m_level != ItemClass.Level.Level1) { throw new InvalidOperationException("Only ordinary Level1 city bus lines are supported."); }
            int count = tm.m_lines.m_buffer[line].CountStops(line);
            if (!RouteCreationRules.IsStopCountAllowed(count)) { throw new InvalidOperationException("This move-only prototype supports three to six existing stops."); }
            List<ExistingRouteStop> stops = new List<ExistingRouteStop>();
            for (int i = 0; i < count; i++)
            {
                ushort node = tm.m_lines.m_buffer[line].GetStop(i);
                if (node == 0) { throw new InvalidOperationException("The line has a missing stop."); }
                stops.Add(new ExistingRouteStop(node, Singleton<NetManager>.instance.m_nodes.m_buffer[node].m_position));
            }
            // Classify road support independently of the placement API argument.
            for (int i = 0; i < stops.Count; i++)
            {
                ExistingRouteStop stop = stops[i];
                RouteStopSite site = ExistingSite(stop);
                Vector3 position; bool fixedPlatform;
                bool resolved = StopPlacement.TryGetStopPosition(site.SegmentId, stop.Position, info, out position, out fixedPlatform);
                if (!SupportsStreet(site.SegmentId, info, resolved, fixedPlatform))
                { throw new InvalidOperationException("Only usable ordinary RoadAI street stops are supported; bridges, tunnels and station-owned roads are refused."); }
                stops[i] = new ExistingRouteStop(stop.NodeId, stop.Position, fixedPlatform);
            }
            return new ExistingRouteSnapshot(line, Singleton<SimulationManager>.instance.m_currentBuildIndex, info.name, stops);
        }

        private static RouteStopSite ExistingSite(ExistingRouteStop stop)
        {
            NetManager net = Singleton<NetManager>.instance;
            uint lane = net.m_nodes.m_buffer[stop.NodeId].m_lane;
            if (lane == 0 || lane >= net.m_lanes.m_buffer.Length) { throw new InvalidOperationException("An existing stop has no connected lane."); }
            ushort segment = net.m_lanes.m_buffer[lane].m_segment;
            if (segment == 0) { throw new InvalidOperationException("An existing stop lane has no road segment."); }
            uint cursor = net.m_segments.m_buffer[segment].m_lanes;
            for (int index = 0; cursor != 0 && index < 256; index++, cursor = net.m_lanes.m_buffer[cursor].m_nextLane)
            {
                if (cursor == lane) { return new RouteStopSite(segment, net.m_segments.m_buffer[segment].m_buildIndex, true, index, stop.Position, stop.FixedPlatform); }
            }
            throw new InvalidOperationException("An existing stop lane could not be identified.");
        }

        private static void RequirePlanning(PlanningPreviewResult planning, ExistingRouteSnapshot original)
        {
            if (planning == null || planning.Snapshot == null || planning.Snapshot.BuildIndexAtStart != planning.Snapshot.BuildIndexAtEnd ||
                planning.Snapshot.BuildIndexAtEnd != original.BuildIndex || planning.Snapshot.BusPrefabName != original.PrefabName) { throw new InvalidOperationException("Generate a fresh planning preview for the same bus prefab first."); }
        }

        // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
        private static bool SupportsStreet(ushort segmentId, TransportInfo info, bool resolved, bool fixedPlatform)
        {
            NetInfo road = Singleton<NetManager>.instance.m_segments.m_buffer[segmentId].Info;
            string ai = road == null || road.m_netAI == null ? null : road.m_netAI.GetType().FullName;
            return RouteStreetStopRules.Supports(ai, StopPlacement.IsSegmentUsableForStops(segmentId, info), resolved, fixedPlatform);
        }

        private static void ValidateTargets(RouteEditPlan plan)
        {
            NetManager net = Singleton<NetManager>.instance;
            TransportInfo info = Singleton<TransportManager>.instance.m_lines.m_buffer[plan.Original.LineId].Info;
            foreach (RouteStopSite target in plan.Stops)
            {
                if (target.SegmentId == 0 || target.SegmentId >= net.m_segments.m_buffer.Length) { throw new InvalidOperationException("A target road disappeared."); }
                if (!SupportsStreet(target.SegmentId, info, true, target.FixedPlatform)) { throw new InvalidOperationException("A target is not a supported ordinary street stop."); }
                NetSegment segment = net.m_segments.m_buffer[target.SegmentId];
                if ((segment.m_flags & NetSegment.Flags.Created) == 0 || segment.m_buildIndex != target.SegmentBuildIndex ||
                    !StopPlacement.IsSegmentUsableForStops(target.SegmentId, info) || !StopPlacement.CanAnchorStop(target.Position, info)) { throw new InvalidOperationException("A target road or anchor no longer matches."); }
                bool originalSite = false;
                foreach (ExistingRouteStop old in plan.Original.Stops)
                {
                    if ((old.Position - target.Position).sqrMagnitude < 0.0001f) { originalSite = true; break; }
                }
                if (originalSite) { continue; }
                Vector3 position; bool fixedPlatform; int lane; bool forward; bool backward;
                if (!StopPlacement.TryGetStopPosition(target.SegmentId, target.Position, info, out position, out fixedPlatform, out lane, out forward, out backward) ||
                    lane != target.LaneIndex || fixedPlatform != target.FixedPlatform || (target.Forward ? !forward : !backward) || (position - target.Position).sqrMagnitude > 0.25f) { throw new InvalidOperationException("A target lane, direction or position changed."); }
            }
        }

        public static void OnAfterSimulationFrame()
        {
            PendingCheck pending;
            lock (Sync) { pending = s_check; }
            if (pending == null || !Requests.IsOwner(pending.Ticket) || ++pending.Frames < 60) { return; }
            string status;
            try
            {
                if (!pending.Surface.IsCurrent) { FinishCheck(pending, "City changed; stop checks were cancelled."); return; }
                TransportManager tm = Singleton<TransportManager>.instance;
                NetManager net = Singleton<NetManager>.instance;
                ushort line = pending.Line;
                bool valid = pending.Surface.HasExpectedIdentity && pending.Surface.Complete && pending.Surface.StopCount == pending.Stops;
                for (int i = 0; valid && i < pending.Stops; i++)
                {
                    ushort node = tm.m_lines.m_buffer[line].GetStop(i);
                    valid = node != 0 && StopPlacement.HasLaneConnection(node) && !net.m_nodes.m_buffer[node].m_problems.IsNotNone;
                    for (int j = 0; valid && j < 8; j++)
                    {
                        ushort segment = net.m_nodes.m_buffer[node].GetSegment(j);
                        if (segment != 0 && (net.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.PathFailed) != 0) { valid = false; }
                    }
                }
                if (!valid && pending.Frames < 300) { return; }
                status = valid ? "Edited line " + line + " passed adjacent-stop checks only; verify the whole route and save/reload in game." : "Edited line " + line + " has connectivity/path warnings. Inspect it and restore your backup if needed.";
            }
            catch (Exception error) { status = "Could not verify the edited line; inspect the city."; Log.Exception("Existing-route post-check", error); }
            FinishCheck(pending, status);
        }

        private static void FinishCheck(PendingCheck pending, string status)
        {
            Requests.TryPublish(pending.Ticket, delegate { lock (Sync) { if (object.ReferenceEquals(s_check, pending)) { s_check = null; s_status = status; } } });
            Requests.Complete(pending.Ticket);
        }

        private static void SetOwnedStatus(RouteEditRequestTicket ticket, string status)
        {
            Requests.TryPublish(ticket, delegate { lock (Sync) { s_status = status; } });
        }

        private static void SetStatus(string text) { lock (Sync) { s_status = text; } }
    }
}
#endif
