#if DEVTOOLS
using System;
using System.Collections.Generic;
using System.Threading;
using ColossalFramework;
using UnityEngine;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning.Planning
{
    // OpenAI-Codex: confirmed Development-only route creation with live and asynchronous checks.
    /// <summary>
    /// Creates one confirmed Development proposal on the simulation thread. Every
    /// live precondition is checked again after the confirmation dialog has closed.
    /// </summary>
    internal static class RouteCreationController
    {
        private static readonly object s_statusLock = new object();
        private static string s_statusKey = Strings.RouteStatusIdle;
        private static ushort s_statusLineId;
        // Signature: OpenAI-Codex_Automated_Public_Transport_Planning
        private static readonly RouteEditRequestGate s_requestGate = new RouteEditRequestGate();
        private static RouteEditRequestTicket s_pendingTicket;
        private static ushort s_pendingLineId;
        private static int s_pendingStopCount;
        private static int s_pendingFrames;
        private static TransportManager s_pendingTransport;
        private static NetManager s_pendingNet;

        public static string StatusText
        {
            get
            {
                lock (s_statusLock)
                {
                    string text = Loc.Get(s_statusKey);
                    return s_statusLineId == 0 ? text : string.Format(text, s_statusLineId);
                }
            }
        }

        public static bool IsBusy
        {
            get { return s_requestGate.IsBusy; }
        }

        public static void RequestCreate(PlanningPreviewResult preview, int proposalIndex)
        {
            // OpenAI-Codex_Responsible: Automated_Public_Transport_Planning: serialize city route operations.
            if (RouteEditingController.IsBusy || preview == null || preview.Snapshot == null ||
                !object.ReferenceEquals(PlanningCapture.LatestPreview, preview) ||
                proposalIndex < 0 || proposalIndex >= preview.Proposals.Count ||
                !RouteCreationRules.HasValidStopSites(preview.Proposals[proposalIndex].StopSites))
            {
                SetStatus(Strings.RouteStatusRefused, 0);
                Log.Warning("Refused route creation request with no eligible proposal.");
                return;
            }

            RouteEditRequestTicket ticket;
            if (!s_requestGate.TryBegin(out ticket))
            {
                SetStatus(Strings.RouteStatusRefused, 0);
                Log.Warning("Refused route creation request because another creation or validation is pending.");
                return;
            }

            if (!Singleton<SimulationManager>.exists)
            {
                SetStatus(ticket, Strings.RouteStatusRefused, 0);
                s_requestGate.Complete(ticket);
                Log.Warning("Refused route creation request because no city is loaded.");
                return;
            }

            SetStatus(ticket, Strings.RouteStatusQueued, 0);
            PlanningPreviewResult frozenPreview = preview;
            int frozenProposalIndex = proposalIndex;
            try
            {
                Singleton<SimulationManager>.instance.AddAction(delegate
                {
                    s_requestGate.RunOwned(ticket, delegate
                    {
                        if (!s_requestGate.IsOwner(ticket)) { return; }
                        CreateOnSimulationThread(ticket, frozenPreview, frozenProposalIndex);
                    }, delegate(Exception e)
                    {
                        SetStatus(ticket, Strings.RouteStatusFailed, 0);
                        Log.Exception("Confirmed proposal creation failed", e);
                    }, delegate
                    {
                        s_requestGate.TryPublish(ticket, delegate
                        {
                            if (!object.ReferenceEquals(s_pendingTicket, ticket)) { s_requestGate.Complete(ticket); }
                        });
                    });
                });
            }
            catch (Exception e)
            {
                SetStatus(ticket, Strings.RouteStatusFailed, 0);
                s_requestGate.Complete(ticket);
                Log.Exception("Queueing confirmed route creation failed", e);
            }
        }

        public static void ResetForLevelChange()
        {
            s_requestGate.Reset(delegate
            {
                s_pendingTicket = null;
                s_pendingLineId = 0;
                s_pendingStopCount = 0;
                s_pendingFrames = 0;
                s_pendingTransport = null;
                s_pendingNet = null;
                SetStatus(Strings.RouteStatusIdle, 0);
            });
        }
        public static void OnAfterSimulationFrame()
        {
            RouteEditRequestTicket ticket = s_pendingTicket;
            if (!s_requestGate.IsOwner(ticket) || s_pendingLineId == 0) { return; }
            try
            {
                ValidatePendingLine(ticket);
            }
            catch (Exception e)
            {
                ushort lineId = s_pendingLineId;
                FinishPending(ticket, Strings.RouteStatusWarning);
                Log.Exception("Asynchronous validation could not inspect line " + lineId, e);
            }
        }

        private static void ValidatePendingLine(RouteEditRequestTicket ticket)
        {
            ushort lineId = 0;
            int expectedStopCount = 0;
            int frames = 0;
            TransportManager expectedTransport = null;
            NetManager expectedNet = null;
            bool ready = false;
            if (!s_requestGate.TryPublish(ticket, delegate
            {
                s_pendingFrames++;
                frames = s_pendingFrames;
                ready = frames >= 60;
                lineId = s_pendingLineId;
                expectedStopCount = s_pendingStopCount;
                expectedTransport = s_pendingTransport;
                expectedNet = s_pendingNet;
            }) || !ready) { return; }

            if (!Singleton<TransportManager>.exists || !Singleton<NetManager>.exists)
            {
                FinishPending(ticket, Strings.RouteStatusWarning);
                return;
            }

            TransportManager tm = Singleton<TransportManager>.instance;
            NetManager net = Singleton<NetManager>.instance;
            if (!s_requestGate.IsOwner(ticket) || !object.ReferenceEquals(tm, expectedTransport) ||
                !object.ReferenceEquals(net, expectedNet))
            {
                FinishPending(ticket, Strings.RouteStatusWarning);
                return;
            }
            if ((tm.m_lines.m_buffer[lineId].m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None)
            {
                FinishPending(ticket, Strings.RouteStatusWarning);
                Log.Warning("Created route " + lineId + " was no longer present during asynchronous validation.");
                return;
            }

            int stopCount = tm.m_lines.m_buffer[lineId].CountStops(lineId);
            int noLane = 0;
            int notConnected = 0;
            int pathFailed = 0;
            for (int i = 0; i < stopCount; i++)
            {
                ushort node = tm.m_lines.m_buffer[lineId].GetStop(i);
                if (node == 0) { continue; }
                if (!StopPlacement.HasLaneConnection(node)) { noLane++; }
                if ((net.m_nodes.m_buffer[node].m_problems & Notification.Problem1.LineNotConnected).IsNotNone)
                {
                    notConnected++;
                }
                for (int s = 0; s < 8; s++)
                {
                    ushort segment = net.m_nodes.m_buffer[node].GetSegment(s);
                    if (segment != 0 &&
                        (net.m_segments.m_buffer[segment].m_flags & NetSegment.Flags.PathFailed) != NetSegment.Flags.None)
                    {
                        pathFailed++;
                        break;
                    }
                }
            }

            bool lineShapeValid = tm.m_lines.m_buffer[lineId].Complete &&
                                  stopCount == expectedStopCount;
            if (lineShapeValid && noLane == 0 && notConnected == 0 && pathFailed == 0)
            {
                FinishPending(ticket, Strings.RouteStatusCreated);
                Log.Info("Asynchronous validation found all " + stopCount +
                    " stop lanes connected and no PathFailed flags on adjacent segments for line " + lineId + ".");
                return;
            }

            if (lineShapeValid && frames < 300 && pathFailed == 0 &&
                (noLane > 0 || notConnected > 0))
            {
                return;
            }

            FinishPending(ticket, Strings.RouteStatusWarning);
            Log.Warning("Asynchronous validation warning for line " + lineId + ": stops=" + stopCount +
                ", expected=" + expectedStopCount + ", complete=" + tm.m_lines.m_buffer[lineId].Complete +
                ", noLane=" + noLane + ", notConnected=" + notConnected + ", pathFailed=" + pathFailed + ".");
        }

        private static void CreateOnSimulationThread(RouteEditRequestTicket ticket, PlanningPreviewResult preview, int proposalIndex)
        {
            if (!Singleton<SimulationManager>.exists || !Singleton<NetManager>.exists ||
                !Singleton<TransportManager>.exists)
            {
                Refuse(ticket, "a required city manager is unavailable");
                return;
            }
            if (!object.ReferenceEquals(PlanningCapture.LatestPreview, preview))
            {
                Refuse(ticket, "the selected preview was replaced before the simulation action ran");
                return;
            }

            PlanningSnapshot snapshot = preview.Snapshot;
            RouteProposal proposal = preview.Proposals[proposalIndex];
            if (!RouteCreationRules.HasValidStopSites(proposal.StopSites) ||
                snapshot.BuildIndexAtStart != snapshot.BuildIndexAtEnd ||
                Singleton<SimulationManager>.instance.m_currentBuildIndex != snapshot.BuildIndexAtEnd)
            {
                // OpenAI-Codex: distinguish stale capture, later city changes and invalid stops without weakening the guard.
                try
                {
                    uint currentBuildIndex = Singleton<SimulationManager>.instance.m_currentBuildIndex;
                    Log.Warning("Route creation eligibility: proposal=" + (proposalIndex + 1) +
                        ", stops=" + proposal.StopSites.Count +
                        ", validStopSites=" + RouteCreationRules.HasValidStopSites(proposal.StopSites) +
                        ", captureBuildIndexChanged=" + (snapshot.BuildIndexAtStart != snapshot.BuildIndexAtEnd) +
                        ", cityBuildIndexChanged=" + (currentBuildIndex != snapshot.BuildIndexAtEnd) +
                        ", buildIndexStart=" + snapshot.BuildIndexAtStart +
                        ", buildIndexEnd=" + snapshot.BuildIndexAtEnd +
                        ", buildIndexCurrent=" + currentBuildIndex + ".");
                }
                catch (Exception)
                {
                    // A diagnostic failure must not prevent the existing refusal.
                }
                Refuse(ticket, "the preview is stale or its stop list is outside the 3-6 distinct-stop limit");
                return;
            }

            TransportInfo busInfo = BusPrefabResolver.ResolveCityBus();
            if (busInfo == null)
            {
                Refuse(ticket, "no city bus prefab is available");
                return;
            }
            if (string.IsNullOrEmpty(snapshot.BusPrefabName) ||
                !string.Equals(busInfo.name, snapshot.BusPrefabName, StringComparison.Ordinal))
            {
                Refuse(ticket, "the resolved city bus prefab differs from the captured prefab");
                return;
            }

            NetManager net = Singleton<NetManager>.instance;
            TransportManager tm = Singleton<TransportManager>.instance;
            if (!net.CheckLimits() || !tm.CheckLimits())
            {
                Refuse(ticket, "the network or transport line limit has been reached");
                return;
            }

            for (int i = 0; i < proposal.StopSites.Count; i++)
            {
                RouteStopSite stop = proposal.StopSites[i];
                if (stop.SegmentId == 0 || stop.SegmentId >= net.m_segments.m_buffer.Length)
                {
                    Refuse(ticket, "stop " + i + " references an invalid segment id");
                    return;
                }

                NetSegment segment = net.m_segments.m_buffer[stop.SegmentId];
                if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None ||
                    segment.m_buildIndex != stop.SegmentBuildIndex ||
                    !StopPlacement.IsSegmentUsableForStops(stop.SegmentId, busInfo))
                {
                    Refuse(ticket, "stop " + i + " no longer matches its captured segment or bus lane");
                    return;
                }

                Vector3 livePosition;
                bool liveFixedPlatform;
                int liveLaneIndex;
                bool liveForward;
                bool liveBackward;
                if (!StopPlacement.TryGetStopPosition(stop.SegmentId, segment.m_middlePosition, busInfo,
                        out livePosition, out liveFixedPlatform, out liveLaneIndex,
                        out liveForward, out liveBackward) ||
                    liveLaneIndex != stop.LaneIndex ||
                    (stop.Forward ? !liveForward : !liveBackward) ||
                    liveFixedPlatform != stop.FixedPlatform ||
                    (livePosition - stop.Position).sqrMagnitude > 0.25f ||
                    !StopPlacement.CanAnchorStop(livePosition, busInfo))
                {
                    Refuse(ticket, "stop " + i + " no longer resolves to the captured lane and position");
                    return;
                }
            }

            ushort lineId;
            if (!tm.CreateLine(out lineId, ref Singleton<SimulationManager>.instance.m_randomizer, busInfo, true) ||
                lineId == 0)
            {
                Refuse(ticket, "CreateLine returned false");
                return;
            }

            try
            {
                for (int i = 0; i < proposal.StopSites.Count; i++)
                {
                    RouteStopSite stop = proposal.StopSites[i];
                    if (!tm.m_lines.m_buffer[lineId].AddStop(lineId, -1, stop.Position, stop.FixedPlatform))
                    {
                        tm.ReleaseLine(lineId);
                        SetStatus(ticket, Strings.RouteStatusFailed, 0);
                        Log.Error("AddStop rejected planned stop " + i + " on segment " + stop.SegmentId +
                            "; released line " + lineId + ".");
                        return;
                    }
                }

                if (tm.m_lines.m_buffer[lineId].CountStops(lineId) != proposal.StopSites.Count)
                {
                    tm.ReleaseLine(lineId);
                    SetStatus(ticket, Strings.RouteStatusFailed, 0);
                    Log.Error("The game did not retain all planned stop sites on line " + lineId + "; released it.");
                    return;
                }

                ushort firstStop = tm.m_lines.m_buffer[lineId].m_stops;
                if (firstStop == 0)
                {
                    tm.ReleaseLine(lineId);
                    SetStatus(ticket, Strings.RouteStatusFailed, 0);
                    Log.Error("Created line " + lineId + " has no first stop; released it.");
                    return;
                }

                Vector3 closingPosition = net.m_nodes.m_buffer[firstStop].m_position;
                if (!tm.m_lines.m_buffer[lineId].AddStop(lineId, -1, closingPosition,
                        proposal.StopSites[0].FixedPlatform) || !tm.m_lines.m_buffer[lineId].Complete)
                {
                    tm.ReleaseLine(lineId);
                    SetStatus(ticket, Strings.RouteStatusFailed, 0);
                    Log.Error("Could not close planned line " + lineId + "; released it.");
                    return;
                }

                string name = "APTP Planned " + DateTime.Now.ToString("HH:mm:ss") + " " + lineId;
                bool renamed = false;
                IEnumerator<bool> rename = tm.SetLineName(lineId, name);
                while (rename.MoveNext()) { renamed = rename.Current; }
                if (!renamed)
                {
                    tm.ReleaseLine(lineId);
                    SetStatus(ticket, Strings.RouteStatusFailed, 0);
                    Log.Error("SetLineName failed for planned line " + lineId + "; released it.");
                    return;
                }

                tm.UpdateLine(lineId);
                s_requestGate.TryPublish(ticket, delegate
                {
                    s_pendingTicket = ticket;
                    s_pendingLineId = lineId;
                    s_pendingStopCount = proposal.StopSites.Count;
                    s_pendingFrames = 0;
                    s_pendingTransport = tm;
                    s_pendingNet = net;
                    SetStatus(Strings.RouteStatusChecking, lineId);
                });
                Log.Info("Created planned line " + lineId + " with " + proposal.StopSites.Count +
                    " distinct stops; asynchronous connection/path checks are pending.");
            }
            catch
            {
                try
                {
                    if (lineId != 0 &&
                        (tm.m_lines.m_buffer[lineId].m_flags & TransportLine.Flags.Created) != TransportLine.Flags.None)
                    {
                        tm.ReleaseLine(lineId);
                    }
                }
                catch (Exception rollbackException)
                {
                    Log.Exception("Rolling back incomplete planned line " + lineId + " failed", rollbackException);
                }
                s_requestGate.TryPublish(ticket, delegate
                {
                    if (object.ReferenceEquals(s_pendingTicket, ticket) && s_pendingLineId == lineId)
                    {
                        s_pendingTicket = null;
                        s_pendingLineId = 0;
                        s_pendingStopCount = 0;
                        s_pendingFrames = 0;
                        s_pendingTransport = null;
                        s_pendingNet = null;
                    }
                });
                SetStatus(ticket, Strings.RouteStatusFailed, 0);
                throw;
            }
        }

        private static void Refuse(RouteEditRequestTicket ticket, string reason)
        {
            SetStatus(ticket, Strings.RouteStatusRefused, 0);
            Log.Warning("Refused planned route creation: " + reason + ".");
        }

        private static void FinishPending(RouteEditRequestTicket ticket, string statusKey)
        {
            s_requestGate.TryPublish(ticket, delegate
            {
                ushort lineId = s_pendingLineId;
                s_pendingTicket = null;
                s_pendingLineId = 0;
                s_pendingStopCount = 0;
                s_pendingFrames = 0;
                s_pendingTransport = null;
                s_pendingNet = null;
                SetStatus(statusKey, lineId);
                s_requestGate.Complete(ticket);
            });
        }

        private static void SetStatus(RouteEditRequestTicket ticket, string key, ushort lineId)
        {
            s_requestGate.TryPublish(ticket, delegate { SetStatus(key, lineId); });
        }
        private static void SetStatus(string key, ushort lineId)
        {
            lock (s_statusLock)
            {
                s_statusKey = key;
                s_statusLineId = lineId;
            }
        }
    }
}
#endif
