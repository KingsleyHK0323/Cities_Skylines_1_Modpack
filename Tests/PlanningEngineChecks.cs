using System;
using System.Collections.Generic;
using System.Reflection;
using AutomatedPublicTransportPlanning.Planning;
using UnityEngine;

// OpenAI-Codex: no-third-party-package characterization checks for the read-only planner.
internal static class PlanningEngineChecks
{
    private static int s_failures;

    private static int Main()
    {
        Run("citizen unit categories convert to five people", CitizenUnitPeopleConversion);
        Run("planning snapshots isolate arrays and expose read-only views", SnapshotCopyIsolation);
        Run("preview warns that its snapshot may become stale", SnapshotStaleWarning);
        Run("route creation accepts only three to six distinct stop sites", RouteCreationStopCountRules);
        Run("people totals and weighted route-segment coverage", PeopleTotalsAndCoverage);
        Run("endpoint pairing uses normalized complementary demand", NormalizedPairing);
        Run("one-way route without a return path is diagnosed", MissingReturnPath);
        Run("planner limits endpoint pools and evaluated pairs", CandidateLimits);
        // Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
        Run("route edit plans retain identity, isolate inputs and reject unsafe or stale edits", RouteEditPlanChecks.Run);
        Run("route execution verifies observed recovery and refuses invalidated writes", RouteEditExecutionChecks.Run);
        Run("route edit request ownership rejects stale completion after reset", RouteEditRequestGateChecks.Run);
        // Contributor: OpenAI-Codex_manual_input_tests
        Run("manual route input validates order, road directions and malformed tokens", RouteManualInputChecks.Run);
        Run("ordinary road classification is independent of placement flag", RouteStreetStopChecks.Run);
        foreach (MethodInfo method in typeof(TransportLine).GetMethods())
        {
            if (method.Name == "MoveStop") { Console.WriteLine("TransportLine API: " + method); }
        }

        Console.WriteLine(s_failures == 0
            ? "All planning engine checks passed."
            : s_failures + " planning engine check(s) failed.");
        return s_failures == 0 ? 0 : 1;
    }

    private static void CitizenUnitPeopleConversion()
    {
        int home = 0;
        int work = 0;
        int visit = 0;

        Require(CitizenUnitDemand.Accumulate(CitizenUnit.Flags.Home,
            ref home, ref work, ref visit), "Home units must be recorded as demand");
        Equal(5, home, "people per Home unit");
        Equal(0, work, "Home unit must not add Work people");
        Equal(0, visit, "Home unit must not add Visit people");

        Require(CitizenUnitDemand.Accumulate(CitizenUnit.Flags.Work,
            ref home, ref work, ref visit), "Work units must be recorded as demand");
        Equal(5, work, "people per Work unit");

        Require(CitizenUnitDemand.Accumulate(CitizenUnit.Flags.Visit,
            ref home, ref work, ref visit), "Visit units must be recorded as demand");
        Equal(5, visit, "people per Visit unit");

        Require(!CitizenUnitDemand.Accumulate(CitizenUnit.Flags.None,
            ref home, ref work, ref visit), "empty flags must not report demand");
        Equal(5, home, "empty flags must not change Home people");
        Equal(5, work, "empty flags must not change Work people");
        Equal(5, visit, "empty flags must not change Visit people");
    }

    private static void SnapshotCopyIsolation()
    {
        SegmentSnapshotData[] segmentInput = { Segment(10, 1, 2, 0f, true, true) };
        BuildingDemandData[] buildingInput = { Building(1, 0f, 5, 0, 0) };
        StructureSignature signature = new StructureSignature(1, 2, 1);
        PlanningSnapshot snapshot = new PlanningSnapshot(segmentInput, buildingInput,
            1, 1, 0, 0, string.Empty, 1, 0, 0, 0, signature, signature,
            0.0, 0.0, 0.0);

        segmentInput[0] = Segment(20, 3, 4, 400f, true, true);
        buildingInput[0] = Building(2, 400f, 0, 5, 0);
        Equal((ushort)10, snapshot.Segments[0].Id, "snapshot must copy segment input");
        Equal((ushort)1, snapshot.Buildings[0].BuildingId, "snapshot must copy building input");

        PlanningSnapshot copy = snapshot.Copy();
        Require(!object.ReferenceEquals(snapshot, copy), "Copy must return a distinct snapshot");
        Require(!object.ReferenceEquals(snapshot.Segments, copy.Segments),
            "Copy must create a distinct segment view");
        Require(!object.ReferenceEquals(snapshot.Buildings, copy.Buildings),
            "Copy must create a distinct building view");
        Require(((IList<SegmentSnapshotData>)snapshot.Segments).IsReadOnly &&
                ((IList<BuildingDemandData>)snapshot.Buildings).IsReadOnly,
            "snapshot views must be read-only");

        FieldInfo segmentsField = typeof(PlanningSnapshot).GetField("m_segments",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo buildingsField = typeof(PlanningSnapshot).GetField("m_buildings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Require(segmentsField != null && buildingsField != null,
            "snapshot backing arrays must remain discoverable for isolation checks");
        Require(!object.ReferenceEquals(segmentsField.GetValue(snapshot), segmentsField.GetValue(copy)),
            "Copy must own an independent segment array");
        Require(!object.ReferenceEquals(buildingsField.GetValue(snapshot), buildingsField.GetValue(copy)),
            "Copy must own an independent building array");
    }

    private static void SnapshotStaleWarning()
    {
        PlanningSnapshot snapshot = Snapshot(new SegmentSnapshotData[0],
            new BuildingDemandData[0]);
        PlanningPreviewResult result = PlanningEngine.CreatePreview(snapshot);
        Require(result.Describe().IndexOf("Snapshot may become stale after city changes.",
            StringComparison.Ordinal) >= 0,
            "preview description must warn that a snapshot may become stale after city changes");
    }

    private static void RouteCreationStopCountRules()
    {
        Require(RouteCreationRules.IsStopCountAllowed(3), "allow minimum stop count");
        Require(RouteCreationRules.IsStopCountAllowed(6), "allow maximum stop count");
        Require(!RouteCreationRules.IsStopCountAllowed(2), "reject fewer than three stops");
        Require(!RouteCreationRules.IsStopCountAllowed(7), "reject more than six stops");

        RouteStopSite[] distinctSites =
        {
            new RouteStopSite(10, 1, true, 2, new Vector3(0f, 0f, 0f), false),
            new RouteStopSite(20, 1, true, 3, new Vector3(10f, 0f, 0f), false),
            new RouteStopSite(30, 1, false, 4, new Vector3(20f, 0f, 0f), false)
        };
        Require(RouteCreationRules.HasValidStopSites(distinctSites),
            "allow three distinct captured segment lanes");

        RouteStopSite[] duplicateLane =
        {
            distinctSites[0], distinctSites[1],
            new RouteStopSite(10, 1, false, 2, new Vector3(30f, 0f, 0f), false)
        };
        Require(!RouteCreationRules.HasValidStopSites(duplicateLane),
            "reject a lane repeated in the opposite direction as a distinct stop");

        RouteStopSite[] nearSites =
        {
            distinctSites[0],
            new RouteStopSite(20, 1, true, 5, new Vector3(2f, 0f, 0f), false),
            distinctSites[2]
        };
        Require(!RouteCreationRules.HasValidStopSites(nearSites),
            "reject stop positions close enough for AddStop to close the loop early");
    }

    private static void Run(string name, Action check)
    {
        try
        {
            check();
            Console.WriteLine("PASS " + name);
        }
        catch (Exception e)
        {
            s_failures++;
            Console.WriteLine("FAIL " + name + ": " + e.Message);
        }
    }

    private static void PeopleTotalsAndCoverage()
    {
        SegmentSnapshotData[] segments =
        {
            Segment(10, 1, 2, 0f, true, true),
            Segment(20, 2, 3, 400f, true, true),
            Segment(30, 3, 4, 800f, true, true),
            Segment(40, 5, 6, 1200f, true, true)
        };
        BuildingDemandData[] buildings =
        {
            Building(1, 0f, 50, 0, 0),
            Building(2, 800f, 0, 25, 0),
            Building(3, 800f, 0, 0, 25),
            Building(4, 1200f, 50, 0, 0)
        };

        PlanningPreviewResult result = PlanningEngine.CreatePreview(Snapshot(segments, buildings));

        Equal(100, result.HomePeople, "home people total");
        Equal(25, result.WorkPeople, "work people total");
        Equal(25, result.VisitPeople, "visit people total");
        Equal(4, result.AssignedBuildings, "assigned buildings");
        Equal(0, result.UnassignedBuildings, "unassigned buildings");
        Equal(2, result.Proposals.Count, "proposal count");
        Near(0.5, result.Proposals[0].HomeCoverage, 0.0001, "home coverage rate");
        Near(1.0, result.Proposals[0].AttractionCoverage, 0.0001, "work/visit coverage rate");
        Near(75.0, result.Proposals[0].CoverageIndex, 0.0001, "50/50 weighted route-segment coverage index");
        Equal(50, result.Proposals[0].HomePeopleCovered, "home people covered on route segments");
        Equal(50, result.Proposals[0].AttractionPeopleCovered, "work and visit people covered on route segments");
        Require(result.Proposals[0].StopSites.Count >= 3,
            "route proposal must retain concrete directional stop sites");
        Require(result.Proposals[0].StopSites[0].LaneIndex >= 0,
            "route proposal must retain the selected bus lane index");
    }

    private static void NormalizedPairing()
    {
        SegmentSnapshotData[] segments =
        {
            Segment(10, 1, 2, 0f, true, false),
            Segment(20, 3, 4, 100f, true, false),
            Segment(30, 5, 6, 200f, true, false),
            Segment(40, 7, 8, 300f, true, false)
        };
        BuildingDemandData[] buildings =
        {
            Building(1, 0f, 80, 0, 0),
            Building(2, 100f, 20, 0, 0),
            Building(3, 200f, 0, 80, 0),
            Building(4, 300f, 0, 20, 0)
        };

        PlanningPreviewResult result = PlanningEngine.CreatePreview(Snapshot(segments, buildings));

        Equal(4, result.Proposals.Count, "all four endpoint pairs are retained");
        Equal((ushort)10, result.Proposals[0].HomeEndpoint, "highest-ranked home endpoint");
        Equal((ushort)30, result.Proposals[0].AttractionEndpoint, "highest-ranked attraction endpoint");
        Near(0.64, result.Proposals[0].PairingScore, 0.000001, "normalized complementary score");
        Near(0.16, result.Proposals[1].PairingScore, 0.000001, "next normalized score");
    }

    private static void MissingReturnPath()
    {
        SegmentSnapshotData[] segments =
        {
            Segment(10, 1, 2, 0f, true, false),
            Segment(20, 2, 3, 400f, true, false)
        };
        BuildingDemandData[] buildings =
        {
            Building(1, 0f, 50, 0, 0),
            Building(2, 400f, 0, 50, 0)
        };

        PlanningPreviewResult result = PlanningEngine.CreatePreview(Snapshot(segments, buildings));

        Equal(1, result.Proposals.Count, "proposal count");
        Require(result.Proposals[0].Diagnostic.IndexOf(
            "outbound path exists, but no directed return path was found", StringComparison.Ordinal) >= 0,
            "the diagnostic must distinguish a missing return path from a missing outbound path");
    }

    private static void CandidateLimits()
    {
        const int EndpointsPerClassInInput = 13;
        SegmentSnapshotData[] segments = new SegmentSnapshotData[EndpointsPerClassInInput * 2];
        BuildingDemandData[] buildings = new BuildingDemandData[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            ushort id = (ushort)(i + 1);
            ushort startNode = (ushort)(i * 2 + 1);
            ushort endNode = (ushort)(i * 2 + 2);
            float x = i * 100f;
            segments[i] = Segment(id, startNode, endNode, x, true, false);
            buildings[i] = i < EndpointsPerClassInInput
                ? Building(id, x, 5, 0, 0)
                : Building(id, x, 0, 5, 0);
        }

        PlanningPreviewResult result = PlanningEngine.CreatePreview(Snapshot(segments, buildings));

        Equal(8, result.Proposals.Count, "evaluated endpoint-pair count");
        Require(ContainsDiagnostic(result.Diagnostics,
            "136 lower-ranked pairs were not searched in this preview"),
            "12 endpoints per class produce 144 pairs, of which only the top eight are evaluated");
    }

    private static PlanningSnapshot Snapshot(SegmentSnapshotData[] segments,
                                             BuildingDemandData[] buildings)
    {
        StructureSignature signature = new StructureSignature(
            segments.Length, segments.Length * 2, buildings.Length);
        return new PlanningSnapshot(segments, buildings,
            segments.Length, buildings.Length, 0, 0, string.Empty, 1, 0,
            0, 0, signature, signature, 0.0, 0.0, 0.0);
    }

    private static SegmentSnapshotData Segment(ushort id, ushort startNode, ushort endNode,
                                                float x, bool forwardOpen, bool backwardOpen)
    {
        Vector3 midpoint = new Vector3(x, 0f, 0f);
        return new SegmentSnapshotData(id, 1, 400f, startNode, endNode, 10f,
            forwardOpen, backwardOpen, midpoint, midpoint, false, true);
    }

    private static BuildingDemandData Building(ushort id, float x,
                                                int home, int work, int visit)
    {
        return new BuildingDemandData(id, new Vector3(x, 0f, 0f), home, work, visit);
    }

    private static bool ContainsDiagnostic(IList<string> diagnostics, string expected)
    {
        for (int i = 0; i < diagnostics.Count; i++)
        {
            if (diagnostics[i].IndexOf(expected, StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    private static void Equal<T>(T expected, T actual, string description)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(description + ": expected " + expected + ", got " + actual + ".");
        }
    }

    private static void Near(double expected, double actual, double tolerance, string description)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(description + ": expected " + expected + ", got " + actual + ".");
        }
    }

    private static void Require(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }
}
