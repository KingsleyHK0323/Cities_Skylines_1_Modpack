using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ColossalFramework;
using ICities;
using UnityEngine;
using AutomatedPublicTransportPlanning.Util;

namespace AutomatedPublicTransportPlanning.Planning
{
    /// <summary>
    /// Incrementally copies a city on the simulation thread. A callback does no more
    /// than the 5 ms target, then continues on a later simulation frame. The copied
    /// values are validated before the pure-data worker receives them.
    /// </summary>
    internal static class PlanningCapture
    {
        private const double SliceBudgetMillis = 5.0;
        private const int MaximumAttempts = 3;

        // Signature: OpenAI-Codex_aptp_release_boundary.
        private static readonly PlanningPreviewAdmission s_admission = new PlanningPreviewAdmission();
        private static readonly object s_displayLock = new object();
        // OpenAI-Codex: publish immutable previews safely to the Development settings UI.
        private static readonly object s_previewLock = new object();
        // Signature: OpenAI-Codex_Automated_Public_Transport_Planning
        private static readonly RouteEditRequestGate s_workerGate = new RouteEditRequestGate();
        private static RouteEditRequestTicket s_captureTicket;
        private static string s_display = "No planning preview has been generated in this city session.";
        private static PlanningPreviewResult s_latestPreview;
        private static int s_previewGeneration;
        private static int s_captureGeneration;

        // These fields are owned by the simulation thread after a request is accepted.
        private static bool s_active;
        private static int s_attempt;
        private static int s_phase;
        private static int s_segmentCursor;
        private static int s_buildingCursor;
        private static int s_lineCursor;
        private static int s_createdSegments;
        private static int s_createdBuildings;
        private static int s_existingBusStops;
        private static uint s_currentUnit;
        private static int s_currentUnitSteps;
        private static ushort s_currentBuildingId;
        private static Vector3 s_currentBuildingPosition;
        private static int s_currentHome;
        private static int s_currentWork;
        private static int s_currentVisit;
        private static bool s_currentBuildingHasDemand;
        private static bool s_lineActive;
        private static ushort s_firstLineNode;
        private static ushort s_currentLineNode;
        private static int s_lineNodeSteps;
        private static int s_verificationCursor;
        private static int s_sliceCount;
        private static int s_overBudgetSlices;
        private static double s_totalSliceMillis;
        private static double s_maxSliceMillis;
        private static double s_accumulatedElapsedMillis;
        private static double s_totalRequestSliceMillis;
        private static double s_totalRequestMaxSliceMillis;
        private static int s_totalRequestSlices;
        private static int s_totalRequestOverBudgetSlices;
        private static string s_retryDiagnostic;
        private static bool s_retryPending;
        private static string s_retryPendingReason;
        private static bool s_readyToComplete;
        private static Stopwatch s_attemptClock;
        private static TransportInfo s_busInfo;
        private static string s_busPrefabName;
        private static StructureSignature s_signatureAtStart;
        private static StructureSignature s_signatureAtCaptureEnd;
        private static uint s_buildIndexAtStart;
        private static uint s_buildIndexAtEnd;
        private static List<SegmentSnapshotData> s_segments;
        private static List<BuildingDemandData> s_buildings;

        public static string DisplayText
        {
            get { lock (s_displayLock) { return s_display; } }
        }

        public static PlanningPreviewResult LatestPreview
        {
            get { lock (s_previewLock) { return s_latestPreview; } }
        }

        public static void ClearLatestPreview()
        {
            s_workerGate.Reset(delegate
            {
                lock (s_previewLock)
                {
                    Interlocked.Increment(ref s_previewGeneration);
                    s_latestPreview = null;
                }
            });
        }

        public static bool RequestPreview()
        {
            bool accepted = s_admission.TryQueue(delegate
            {
                ClearLatestPreview();
                if (!s_workerGate.TryBegin(out s_captureTicket)) { return false; }
                s_captureGeneration = Interlocked.CompareExchange(ref s_previewGeneration, 0, 0);
                lock (s_displayLock)
                {
                    s_display = "Waiting for the next simulation frame to start a read-only capture.";
                }
                return true;
            });
            if (!accepted)
            {
                Log.Warning("Planning preview request ignored; a capture or preview is already active.");
                return false;
            }
            Log.Info("Planning preview requested; the city will be read in simulation-frame slices.");
            return true;
        }

        public static void CancelForLevelChange()
        {
            s_admission.Cancel(delegate
            {
                ClearLatestPreview();
                lock (s_displayLock)
                {
                    s_display = "No planning preview has been generated in this city session.";
                }
            });
        }

        /// <summary>Called only from ThreadingExtensionBase.OnAfterSimulationFrame.</summary>
        public static void OnAfterSimulationFrame()
        {
            try
            {
                bool wasBusy = false;
                if (s_admission.TryResetCancelled(delegate
                {
                    wasBusy = s_active || s_admission.IsBusy;
                    ResetCapture("Capture cancelled because the level changed.");
                }))
                {
                    if (wasBusy) { Log.Info("Planning capture cancelled because the level is changing."); }
                    return;
                }

                if (!s_active)
                {
                    if (!s_admission.TryTake()) { return; }
                    s_attempt = 0;
                    s_retryDiagnostic = string.Empty;
                    s_accumulatedElapsedMillis = 0.0;
                    s_totalRequestSliceMillis = 0.0;
                    s_totalRequestMaxSliceMillis = 0.0;
                    s_totalRequestSlices = 0;
                    s_totalRequestOverBudgetSlices = 0;
                    BeginAttempt();
                }

                if (!s_active) { return; }
                RunSlice();
                if (s_retryPending) { FinishPendingRetry(); }
            }
            catch (Exception e)
            {
                Log.Exception("Planning capture frame failed", e);
                FailPermanently("capture exception: " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static void BeginAttempt()
        {
            s_attempt++;
            s_attemptClock = null;
            s_segments = null;
            s_buildings = null;
            s_createdSegments = 0;
            s_createdBuildings = 0;
            s_existingBusStops = 0;
            s_sliceCount = 0;
            s_overBudgetSlices = 0;
            s_totalSliceMillis = 0.0;
            s_maxSliceMillis = 0.0;
            if (!Singleton<NetManager>.exists || !Singleton<BuildingManager>.exists ||
                !Singleton<SimulationManager>.exists || !Singleton<CitizenManager>.exists)
            {
                FailPermanently("required city managers are unavailable (net, building, simulation, or citizen manager)");
                return;
            }

            s_busInfo = BusPrefabResolver.ResolveCityBus();
            if (s_busInfo == null)
            {
                FailPermanently("no city bus TransportInfo is available");
                return;
            }
            s_busPrefabName = s_busInfo.name ?? string.Empty;

            s_attemptClock = Stopwatch.StartNew();
            s_segments = new List<SegmentSnapshotData>();
            s_buildings = new List<BuildingDemandData>();
            s_signatureAtStart = ReadSignature();
            s_signatureAtCaptureEnd = s_signatureAtStart;
            s_buildIndexAtStart = Singleton<SimulationManager>.instance.m_currentBuildIndex;
            s_buildIndexAtEnd = s_buildIndexAtStart;
            s_phase = 0;
            s_segmentCursor = 1;
            s_buildingCursor = 1;
            s_lineCursor = 1;
            s_createdSegments = 0;
            s_createdBuildings = 0;
            s_existingBusStops = 0;
            s_currentUnit = 0U;
            s_currentUnitSteps = 0;
            s_currentBuildingHasDemand = false;
            s_lineActive = false;
            s_verificationCursor = 0;
            s_sliceCount = 0;
            s_overBudgetSlices = 0;
            s_totalSliceMillis = 0.0;
            s_maxSliceMillis = 0.0;
            s_active = true;
            s_retryPending = false;
            s_readyToComplete = false;
            UpdateDisplay("Capturing city data (attempt " + s_attempt + "/" + MaximumAttempts + ").");
            Log.Info("Planning capture attempt " + s_attempt + "/" + MaximumAttempts +
                     " started; structure=[" + s_signatureAtStart + "]; buildIndex=" + s_buildIndexAtStart +
                     " (diagnostic only).");
        }

        private static void RunSlice()
        {
            long start = Stopwatch.GetTimestamp();
            s_sliceCount++;
            StructureSignature before = ReadSignature();
            if (!before.Equals(s_signatureAtStart))
            {
                RecordSliceElapsed(ElapsedMillis(start));
                RetryAttempt("structure counters changed before slice: expected [" + s_signatureAtStart +
                             "], observed [" + before + "]");
                return;
            }

            while (ElapsedMillis(start) < SliceBudgetMillis && s_active && !s_readyToComplete)
            {
                if (s_phase == 0) { CaptureOneSegment(); }
                else if (s_phase == 1) { CaptureOneBuildingOrUnit(); }
                else if (s_phase == 2) { CaptureOneLineOrStop(); }
                else if (s_phase == 3) { VerifyOneSegment(); }
                else { s_readyToComplete = true; }
            }

            SegmentSnapshotData[] frozenSegments = null;
            BuildingDemandData[] frozenBuildings = null;
            if (s_readyToComplete)
            {
                frozenSegments = s_segments.ToArray();
                frozenBuildings = s_buildings.ToArray();
            }

            RecordSliceElapsed(ElapsedMillis(start));
            if (s_active)
            {
                UpdateDisplay("Capturing and validating city data (attempt " + s_attempt + "/" + MaximumAttempts +
                    ", slice " + s_sliceCount + ", retries " + (s_attempt - 1) + ").");
            }
            if (s_readyToComplete) { CompleteCapture(frozenSegments, frozenBuildings); }
        }

        private static void RecordSliceElapsed(double elapsed)
        {
            s_totalSliceMillis += elapsed;
            s_totalRequestSliceMillis += elapsed;
            s_totalRequestSlices++;
            if (elapsed > s_maxSliceMillis) { s_maxSliceMillis = elapsed; }
            if (elapsed > s_totalRequestMaxSliceMillis) { s_totalRequestMaxSliceMillis = elapsed; }
            if (elapsed > SliceBudgetMillis)
            {
                s_overBudgetSlices++;
                s_totalRequestOverBudgetSlices++;
            }
        }

        private static void CaptureOneSegment()
        {
            NetManager net = Singleton<NetManager>.instance;
            int limit = net.m_segments.m_buffer.Length;
            if (s_segmentCursor >= limit)
            {
                s_phase = 1;
                return;
            }

            int id = s_segmentCursor++;
            NetSegment segment = net.m_segments.m_buffer[id];
            if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) { return; }
            s_createdSegments++;
            if (!StopPlacement.IsSegmentUsableForStops((ushort)id, s_busInfo)) { return; }

            NetInfo info = segment.Info;
            if (info == null || info.m_lanes == null) { return; }
            bool invert = (segment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            bool forward;
            bool backward;
            float speed = WorldSnapshot.FastestUsableLane(info, s_busInfo, invert,
                                                           out forward, out backward);
            if (!forward && !backward) { return; }

            Vector3 middle = segment.m_middlePosition;
            Vector3 stopPosition;
            bool fixedPlatform;
            int stopLaneIndex;
            bool stopLaneForward;
            bool stopLaneBackward;
            bool canAnchor = StopPlacement.TryGetStopPosition((ushort)id, middle, s_busInfo,
                                      out stopPosition, out fixedPlatform, out stopLaneIndex,
                                      out stopLaneForward, out stopLaneBackward) &&
                             StopPlacement.CanAnchorStop(stopPosition, s_busInfo);
            bool forwardStopCanAnchor = canAnchor && forward && stopLaneForward;
            bool backwardStopCanAnchor = canAnchor && backward && stopLaneBackward;
            s_segments.Add(new SegmentSnapshotData((ushort)id, segment.m_buildIndex,
                segment.m_averageLength, segment.m_startNode, segment.m_endNode,
                speed, forward, backward, middle,
                stopPosition, fixedPlatform, forwardStopCanAnchor,
                forwardStopCanAnchor ? stopLaneIndex : -1,
                stopPosition, fixedPlatform, backwardStopCanAnchor,
                backwardStopCanAnchor ? stopLaneIndex : -1));
        }

        private static void CaptureOneBuildingOrUnit()
        {
            BuildingManager buildings = Singleton<BuildingManager>.instance;
            CitizenManager citizens = Singleton<CitizenManager>.instance;
            int limit = buildings.m_buildings.m_buffer.Length;

            if (s_currentUnit != 0U)
            {
                if (s_currentUnit >= citizens.m_units.m_buffer.Length ||
                    s_currentUnitSteps >= citizens.m_units.m_buffer.Length)
                {
                    // A corrupt or stale chain is bounded and reported; it cannot turn
                    // a read-only preview into an unbounded loop.
                    s_currentUnit = 0U;
                    FinishCurrentBuilding();
                    return;
                }

                CitizenUnit unit = citizens.m_units.m_buffer[(int)s_currentUnit];
                if (CitizenUnitDemand.Accumulate(unit.m_flags, ref s_currentHome,
                        ref s_currentWork, ref s_currentVisit))
                {
                    s_currentBuildingHasDemand = true;
                }
                s_currentUnit = unit.m_nextUnit;
                s_currentUnitSteps++;
                if (s_currentUnit == 0U || s_currentUnitSteps >= citizens.m_units.m_buffer.Length)
                {
                    FinishCurrentBuilding();
                }
                return;
            }

            if (s_buildingCursor >= limit)
            {
                s_phase = 2;
                return;
            }

            int id = s_buildingCursor++;
            Building building = buildings.m_buildings.m_buffer[id];
            if ((building.m_flags & Building.Flags.Created) == Building.Flags.None) { return; }
            s_createdBuildings++;
            s_currentBuildingId = (ushort)id;
            s_currentBuildingPosition = building.m_position;
            s_currentHome = 0;
            s_currentWork = 0;
            s_currentVisit = 0;
            s_currentBuildingHasDemand = false;
            s_currentUnit = building.m_citizenUnits;
            s_currentUnitSteps = 0;
            if (s_currentUnit == 0U) { FinishCurrentBuilding(); }
        }

        private static void FinishCurrentBuilding()
        {
            if (s_currentBuildingHasDemand)
            {
                s_buildings.Add(new BuildingDemandData(s_currentBuildingId,
                    s_currentBuildingPosition, s_currentHome, s_currentWork, s_currentVisit));
            }
            s_currentUnit = 0U;
        }

        private static void CaptureOneLineOrStop()
        {
            if (!Singleton<TransportManager>.exists)
            {
                s_phase = 3;
                return;
            }

            TransportManager transport = Singleton<TransportManager>.instance;
            if (s_lineActive)
            {
                if (s_currentLineNode == 0 || (s_lineNodeSteps > 0 && s_currentLineNode == s_firstLineNode) ||
                    s_lineNodeSteps >= Singleton<NetManager>.instance.m_nodes.m_buffer.Length)
                {
                    s_lineActive = false;
                    s_lineCursor++;
                    return;
                }

                NetManager net = Singleton<NetManager>.instance;
                ushort node = s_currentLineNode;
                if (node < net.m_nodes.m_buffer.Length &&
                    (net.m_nodes.m_buffer[node].m_flags & NetNode.Flags.Created) != NetNode.Flags.None)
                {
                    s_existingBusStops++;
                    s_currentLineNode = TransportLine.GetNextStop(node);
                }
                else
                {
                    s_currentLineNode = 0;
                }
                s_lineNodeSteps++;
                return;
            }

            int limit = transport.m_lines.m_buffer.Length;
            if (s_lineCursor >= limit)
            {
                s_phase = 3;
                s_verificationCursor = 0;
                s_signatureAtCaptureEnd = ReadSignature();
                if (!s_signatureAtCaptureEnd.Equals(s_signatureAtStart))
                {
                    RetryAttempt("structure counters changed at capture end: expected [" + s_signatureAtStart +
                                 "], observed [" + s_signatureAtCaptureEnd + "]");
                }
                return;
            }

            TransportLine line = transport.m_lines.m_buffer[s_lineCursor];
            if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None ||
                line.Info == null || line.Info.m_transportType != TransportInfo.TransportType.Bus)
            {
                s_lineCursor++;
                return;
            }

            s_firstLineNode = line.m_stops;
            s_currentLineNode = s_firstLineNode;
            s_lineNodeSteps = 0;
            if (s_currentLineNode == 0)
            {
                s_lineCursor++;
                return;
            }
            s_lineActive = true;
        }

        private static void VerifyOneSegment()
        {
            if (s_verificationCursor >= s_segments.Count)
            {
                StructureSignature finalSignature = ReadSignature();
                if (!finalSignature.Equals(s_signatureAtStart))
                {
                    RetryAttempt("structure counters changed during validation: expected [" + s_signatureAtStart +
                                 "], observed [" + finalSignature + "]");
                    return;
                }
                s_buildIndexAtEnd = Singleton<SimulationManager>.instance.m_currentBuildIndex;
                s_readyToComplete = true;
                return;
            }

            SegmentSnapshotData captured = s_segments[s_verificationCursor++];
            NetManager net = Singleton<NetManager>.instance;
            if (captured.Id == 0 || captured.Id >= net.m_segments.m_buffer.Length)
            {
                RetryAttempt("captured segment " + captured.Id + " no longer exists during validation");
                return;
            }

            NetSegment live = net.m_segments.m_buffer[captured.Id];
            if ((live.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None)
            {
                RetryAttempt("captured segment " + captured.Id + " was removed before validation");
                return;
            }
            if (live.m_buildIndex != captured.BuildIndex)
            {
                RetryAttempt("captured segment " + captured.Id + " was replaced (buildIndex " +
                    captured.BuildIndex + " -> " + live.m_buildIndex + ")");
            }
        }

        private static void CompleteCapture(SegmentSnapshotData[] frozenSegments,
                                           BuildingDemandData[] frozenBuildings)
        {
            if (!s_active) { return; }
            s_active = false;
            s_attemptClock.Stop();
            double attemptElapsed = Millis(s_attemptClock);
            double elapsed = s_accumulatedElapsedMillis + attemptElapsed;
            double sliceWork = s_totalRequestSliceMillis;
            PlanningSnapshot snapshot = PlanningSnapshot.TakeCapturedArrays(frozenSegments, frozenBuildings,
                s_createdSegments, s_createdBuildings, s_existingBusStops, s_attempt - 1,
                s_retryDiagnostic, s_totalRequestSlices, s_totalRequestOverBudgetSlices, s_buildIndexAtStart,
                s_buildIndexAtEnd, s_signatureAtStart, s_signatureAtCaptureEnd,
                elapsed, sliceWork, s_totalRequestMaxSliceMillis, s_busPrefabName);
            int previewGeneration = s_captureGeneration;
            RouteEditRequestTicket workerTicket = s_captureTicket;
            UpdateDisplay("Capture validated; computing demand and route proposals on copied data. Retries=" +
                          (s_attempt - 1) + ".");
            Log.Info("Planning capture validated; attempts=" + s_attempt + ", retries=" + (s_attempt - 1) +
                ", elapsedMs=" + elapsed.ToString("F2") + ", createdSegments=" + s_createdSegments +
                ", capturedRouteSegments=" + s_segments.Count + ", createdBuildings=" + s_createdBuildings +
                ", demandBuildings=" + s_buildings.Count + ", existingBusStops=" + s_existingBusStops +
                ", slices=" + s_totalRequestSlices + ", sliceWorkMs=" + sliceWork.ToString("F2") +
                ", maxSliceMs=" + s_totalRequestMaxSliceMillis.ToString("F2") +
                ", overBudgetSlices=" + s_totalRequestOverBudgetSlices +
                ", buildIndexStart=" + s_buildIndexAtStart + ", buildIndexEnd=" + s_buildIndexAtEnd +
                " (diagnostic only), structure=[" + s_signatureAtStart + "]");
            s_segments = null;
            s_buildings = null;
            s_busInfo = null;

            ThreadPool.QueueUserWorkItem(delegate(object state)
            {
                s_workerGate.RunOwned(workerTicket, delegate
                {
                    PlanningPreviewResult result = PlanningEngine.CreatePreview((PlanningSnapshot)state);
                    string description = result.Describe();
                    bool published = false;
                    s_workerGate.TryPublish(workerTicket, delegate
                    {
                        lock (s_previewLock)
                        {
                            if (previewGeneration == Interlocked.CompareExchange(ref s_previewGeneration, 0, 0))
                            {
                                s_latestPreview = result;
                                UpdateDisplay(description);
                                published = true;
                            }
                        }
                    });

                    if (published)
                    {
                        Log.Info("Planning preview complete.\n" + description.Replace("\n", "\n  "));
                    }
                    else
                    {
                        Log.Info("Discarded a planning preview completed after its city session changed.");
                    }
                }, delegate(Exception e)
                {
                    Log.Exception("Planning preview worker failed", e);
                    s_workerGate.TryPublish(workerTicket, delegate
                    {
                        UpdateDisplay("Planning preview failed during pure-data calculation: " + e.GetType().Name + ": " + e.Message);
                    });
                }, delegate
                {
                    s_workerGate.TryPublish(workerTicket, delegate { s_admission.Complete(); });
                    s_workerGate.Complete(workerTicket);
                });
            }, snapshot);
        }

        private static void RetryAttempt(string reason)
        {
            if (!s_active) { return; }
            s_active = false;
            if (s_attemptClock != null) { s_attemptClock.Stop(); }
            s_retryPendingReason = reason;
            s_retryPending = true;
        }

        private static void FinishPendingRetry()
        {
            s_retryPending = false;
            string reason = s_retryPendingReason;
            double elapsed = s_attemptClock == null ? 0.0 : Millis(s_attemptClock);
            s_accumulatedElapsedMillis += elapsed;
            StructureSignature observed = ReadSignature();
            uint buildIndexEnd = Singleton<SimulationManager>.exists
                ? Singleton<SimulationManager>.instance.m_currentBuildIndex : 0U;
            Log.Warning("Planning capture attempt " + s_attempt + "/" + MaximumAttempts + " discarded; retries=" +
                (s_attempt - 1) + ", reason='" + reason + "', elapsedMs=" + elapsed.ToString("F2") +
                ", createdSegments=" + s_createdSegments + ", capturedRouteSegments=" + s_segments.Count +
                ", createdBuildings=" + s_createdBuildings + ", demandBuildings=" + s_buildings.Count +
                ", existingBusStops=" + s_existingBusStops + ", slices=" + s_sliceCount +
                ", sliceWorkMs=" + s_totalSliceMillis.ToString("F2") +
                ", maxSliceMs=" + s_maxSliceMillis.ToString("F2") +
                ", overBudgetSlices=" + s_overBudgetSlices + ", buildIndexStart=" + s_buildIndexAtStart +
                ", buildIndexEnd=" + buildIndexEnd + " (diagnostic only), structureStart=[" +
                s_signatureAtStart + "], structureObserved=[" + observed + "]");
            s_retryDiagnostic = string.IsNullOrEmpty(s_retryDiagnostic)
                ? reason : s_retryDiagnostic + " | " + reason;

            if (s_attempt >= MaximumAttempts)
            {
                string abandoned = "城市持續變動，請暫停後重試\nCapture abandoned after " + MaximumAttempts +
                    " attempts (retries=" + (MaximumAttempts - 1) + "). Reason(s): " + s_retryDiagnostic +
                    "\nLast attempt: elapsed=" + elapsed.ToString("F2") + " ms; created segments=" +
                    s_createdSegments + "; captured route segments=" + s_segments.Count +
                    "; buildings=" + s_createdBuildings + "; existing bus stops=" + s_existingBusStops +
                    "; slices=" + s_sliceCount + "; max slice=" + s_maxSliceMillis.ToString("F2") +
                    " ms; over 5 ms=" + s_overBudgetSlices + ".";
                UpdateDisplay(abandoned);
                Log.Warning("Planning capture abandoned after " + MaximumAttempts + " attempts; retries=" +
                    (MaximumAttempts - 1) + "; reason(s)='" + s_retryDiagnostic + "'. " +
                    "城市持續變動，請暫停後重試");
                s_segments = null;
                s_buildings = null;
                s_busInfo = null;
                s_admission.Complete();
                return;
            }

            UpdateDisplay("Capture changed during attempt " + s_attempt + "; retrying (retry " + s_attempt +
                          "/" + (MaximumAttempts - 1) + "). Reason: " + reason +
                          "\nLast attempt: elapsed=" + elapsed.ToString("F2") + " ms; created segments=" +
                          s_createdSegments + "; captured route segments=" + s_segments.Count +
                          "; buildings=" + s_createdBuildings + "; existing bus stops=" + s_existingBusStops +
                          "; slices=" + s_sliceCount + "; max slice=" + s_maxSliceMillis.ToString("F2") + " ms.");
            BeginAttempt();
        }

        private static void FailPermanently(string reason)
        {
            s_active = false;
            string message = "Capture unavailable; no retry was attempted. Reason: " + reason;
            UpdateDisplay(message);
            double elapsed = s_attemptClock == null ? 0.0 : Millis(s_attemptClock);
            Log.Warning("Planning capture stopped; retries=" + (s_attempt > 0 ? s_attempt - 1 : 0) +
                "; abandonment reason='" + reason + "'; elapsedMs=" + elapsed.ToString("F2") +
                "; createdSegments=" + s_createdSegments + "; capturedRouteSegments=" +
                (s_segments == null ? 0 : s_segments.Count) + "; createdBuildings=" + s_createdBuildings +
                "; demandBuildings=" + (s_buildings == null ? 0 : s_buildings.Count) +
                "; existingBusStops=" + s_existingBusStops + "; slices=" + s_sliceCount +
                "; maxSliceMs=" + s_maxSliceMillis.ToString("F2") + ".");
            if (s_attemptClock != null) { s_attemptClock.Stop(); }
            s_segments = null;
            s_buildings = null;
            s_busInfo = null;
            s_admission.Complete();
        }

        private static void ResetCapture(string message)
        {
            s_active = false;
            if (s_attemptClock != null) { s_attemptClock.Stop(); }
            s_retryPending = false;
            s_readyToComplete = false;
            s_segments = null;
            s_buildings = null;
            s_busInfo = null;
            s_lineActive = false;
            // Cancellation reset runs under the admission lock before a new request can start.
            lock (s_displayLock) { s_display = message ?? string.Empty; }
        }

        private static StructureSignature ReadSignature()
        {
            if (!Singleton<NetManager>.exists || !Singleton<BuildingManager>.exists)
            {
                return new StructureSignature(-1, -1, -1);
            }
            NetManager net = Singleton<NetManager>.instance;
            BuildingManager buildings = Singleton<BuildingManager>.instance;
            return new StructureSignature(net.m_segmentCount, net.m_nodeCount,
                                          buildings.m_buildingCount);
        }

        private static double ElapsedMillis(long start)
        {
            return (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }

        private static double Millis(Stopwatch stopwatch)
        {
            return stopwatch.ElapsedTicks * 1000.0 / Stopwatch.Frequency;
        }

        private static void UpdateDisplay(string text)
        {
            // Do not publish status from a capture invalidated by a level change.
            s_workerGate.TryPublish(s_captureTicket, delegate
            {
                lock (s_displayLock) { s_display = text ?? string.Empty; }
            });
        }
    }
}
