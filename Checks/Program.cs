// Signature: OpenAI-Codex_railway_checks
// Observation batch signature: OpenAI-Codex_Realistic_Railway_Traffic_Control
using System;
using RealisticRailwayTrafficControl.Model;
using RealisticRailwayTrafficControl.Observation;

internal static partial class Program
{
    private delegate void Check();
    private static int groups;
    private static int failures;
    private static int assertions;

    private static int Main()
    {
        Run("Complete clear capture", CompleteClearCapture);
        Run("Snapshot defensive copies", SnapshotCopies);
        Run("Resource and movement defensive copies", ResourceAndMovementCopies);
        Run("Decision defensive copies", DecisionCopies);
        Run("Null and empty inputs", NullAndEmpty);
        Run("Invalid nested members", InvalidMembers);
        Run("Duplicate identities", DuplicateIdentities);
        Run("Incomplete capture and tail evidence", IncompleteEvidence);
        Run("Occupancy state consistency", OccupancyConsistency);
        Run("Unknown enum values are not clear", UnknownEnums);
        Run("Unsupported network", UnsupportedNetwork);
        Run("Malformed conflict graph", MalformedGraph);
        Run("Missing required resources", MissingResources);
        Run("Identity verification", IdentityVerification);
        Run("Capture epoch and serial mismatch", StaleCapture);
        Run("Topology mismatch", TopologyMismatch);
        Run("Validation precedence", ValidationPrecedence);
        Run("Opposing routes share a resource", OpposingRoutes);
        Run("Distinct crossing lanes conflict", CrossingLanes);
        Run("Direct occupied resource", DirectOccupancy);
        Run("Occupied conflict neighbor", NeighborOccupancy);
        Run("Own starting block remains occupied", OwnStartingBlock);
        Run("Failed request reserves nothing", AtomicConflict);
        Run("Fresh captures do not inherit reservations", FreshCapture);
        Run("Malformed batch propagates failure", FailurePropagation);
        Run("Deterministic ordering and unchanged input", DeterministicOrdering);
        Run("Duplicate resource within one request", RepeatedResource);
        Run("Inclusive configured limits", InclusiveLimits);
        Run("Oversized captures remain unknown", OversizedCapture);
        Run("Observation starts disabled", ObservationStartsDisabled);
        Run("Observation requires an explicit city and scope", ObservationActivation);
        Run("Only one capture can remain pending", ObservationSinglePending);
        Run("Incomplete capture evidence remains unknown", ObservationPreservesUnknown);
        Run("Invalidation rejects a late callback", ObservationInvalidation);
        Run("Old callback cannot consume a newer request", ObservationNewerRequest);
        Run("City reload invalidates old requests", ObservationCityReload);
        Run("Disable clears only local observations", ObservationDisable);
        Run("Mismatched completion headers are rejected", ObservationHeaderMismatch);
        Run("Foreign session ticket is rejected", ObservationForeignTicket);
        Run("New capture drops the previous preview", ObservationDropsOldPreview);
        Run("Completion tickets are single use", ObservationSingleCompletion);
        Run("Inactive shell lifecycle starts unloaded", LifecycleStartsUnloaded);
        Run("Only gameplay activates a local city", LifecycleGameplayOnly);
        Run("Unload clears pending observation state", LifecycleUnloadInvalidatesCapture);
        Run("Load failure is isolated and cleaned up", LifecycleLoadFailure);
        Run("Cleanup failure prevents load", LifecycleCleanupFailure);
        Run("Secondary cleanup error preserves first failure", LifecycleSecondaryFailure);
        Run("Extension callback failure clears local state", LifecycleExternalFailure);
        Run("Repeated load unload does not retain old captures", LifecycleRepeatedCycles);
        Console.WriteLine("Groups: {0}; failures: {1}; assertions: {2}", groups, failures, assertions);
        return failures == 0 ? 0 : 1;
    }

    private static void Run(string name, Check check)
    {
        groups++;
        try { check(); Console.WriteLine("PASS: " + name); }
        catch (Exception error)
        {
            failures++;
            Console.WriteLine("FAIL: {0}: {1}: {2}", name, error.GetType().Name, error.Message);
        }
    }

    private static void Assert(bool value, string message)
    {
        assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    private static ResourceEvidence Clear(string key, params string[] conflicts)
    {
        return new ResourceEvidence(key, EvidenceState.Clear, conflicts, true, new string[0]);
    }

    private static ResourceEvidence Occupied(string key, string vehicle, params string[] conflicts)
    {
        return new ResourceEvidence(key, EvidenceState.Occupied, conflicts, true, new string[] { vehicle });
    }

    private static VehicleEvidence Vehicle(string key)
    {
        return new VehicleEvidence(key, true, true);
    }

    private static RailwaySnapshot Snapshot(params ResourceEvidence[] resources)
    {
        return MakeSnapshot(1, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            resources, new VehicleEvidence[] { Vehicle("train-a"), Vehicle("train-b"), Vehicle("train-c") });
    }

    private static RailwaySnapshot MakeSnapshot(long epoch, long serial, string topology, int segments,
        bool complete, NetworkSupport network, ResourceEvidence[] resources, VehicleEvidence[] vehicles)
    {
        return new RailwaySnapshot(epoch, serial, topology, 100, segments, complete, network, resources, vehicles);
    }

    private static MovementRequest Request(string id, string vehicle, params string[] resources)
    {
        return new MovementRequest(id, vehicle, 1, 1, "topology-1", resources);
    }

    private static void Invalid(RailwaySnapshot snapshot, MovementRequest[] requests, ShadowReasonCode reason)
    {
        Invalid(snapshot, requests, ShadowOutcome.Unknown, reason);
    }

    private static void Invalid(RailwaySnapshot snapshot, MovementRequest[] requests,
        ShadowOutcome outcome, ShadowReasonCode reason)
    {
        ValidationReport report = RailwaySnapshotValidator.Validate(snapshot, requests);
        Assert(report != null, "Validation must return a report.");
        Assert(!report.IsValid, "Invalid data must not validate.");
        Assert(report.FailureOutcome == outcome, "Wrong validation outcome: " + report.FailureOutcome);
        Assert(report.ReasonCode == reason, "Wrong validation reason: " + report.ReasonCode);
    }

    private static void Decision(ShadowDecision result, ShadowOutcome outcome, ShadowReasonCode reason)
    {
        Assert(result != null, "Analysis must return a decision for every supplied request.");
        Assert(result.Outcome == outcome, "Wrong analysis outcome: " + result.Outcome);
        Assert(result.ReasonCode == reason, "Wrong analysis reason: " + result.ReasonCode);
    }

    private static void CompleteClearCapture()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("block"));
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "block") };
        ValidationReport report = RailwaySnapshotValidator.Validate(snapshot, requests);
        Assert(report.IsValid, "A complete dedicated-rail snapshot must validate.");
        Assert(report.ReasonCode == ShadowReasonCode.None, "Valid data must have no failure reason.");
        ShadowDecision[] result = RailwayShadowAnalyzer.Evaluate(snapshot, requests);
        Assert(result.Length == 1 && result[0].RequestId == "first", "Preserve request identity.");
        Decision(result[0], ShadowOutcome.WouldReserve, ShadowReasonCode.None);
    }

    private static void SnapshotCopies()
    {
        ResourceEvidence original = Clear("block");
        ResourceEvidence[] resources = new ResourceEvidence[] { original };
        VehicleEvidence[] vehicles = new VehicleEvidence[] { Vehicle("train-a") };
        RailwaySnapshot snapshot = MakeSnapshot(1, 1, "topology-1", 1, true,
            NetworkSupport.SupportedDedicatedRail, resources, vehicles);
        resources[0] = Clear("changed"); vehicles[0] = Vehicle("changed");
        Assert(snapshot.Resources[0].ResourceKey == "block", "Do not retain the input resource array.");
        Assert(snapshot.Occupants[0].VehicleKey == "train-a", "Do not retain the input vehicle array.");
        ResourceEvidence[] outputResources = snapshot.Resources;
        VehicleEvidence[] outputVehicles = snapshot.Occupants;
        outputResources[0] = null; outputVehicles[0] = null;
        Assert(snapshot.Resources[0] == original, "Do not expose the internal resource array.");
        Assert(snapshot.Occupants[0].VehicleKey == "train-a", "Do not expose the internal vehicle array.");
    }

    private static void ResourceAndMovementCopies()
    {
        string[] conflicts = new string[] { "neighbor" };
        string[] occupants = new string[] { "train-a" };
        ResourceEvidence resource = new ResourceEvidence("block", EvidenceState.Occupied, conflicts, true, occupants);
        conflicts[0] = "changed"; occupants[0] = "changed";
        Assert(resource.ConflictKeys[0] == "neighbor", "Copy input conflict keys.");
        Assert(resource.OccupantKeys[0] == "train-a", "Copy input occupant keys.");
        resource.ConflictKeys[0] = "changed"; resource.OccupantKeys[0] = "changed";
        Assert(resource.ConflictKeys[0] == "neighbor", "Copy output conflict keys.");
        Assert(resource.OccupantKeys[0] == "train-a", "Copy output occupant keys.");
        string[] needed = new string[] { "block" };
        MovementRequest request = Request("first", "train-a", needed);
        needed[0] = "changed"; request.RequiredResourceKeys[0] = "changed";
        Assert(request.RequiredResourceKeys[0] == "block", "Movement arrays must be isolated both ways.");
    }

    private static void DecisionCopies()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("block"));
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "block") };
        ShadowDecision[] result = RailwayShadowAnalyzer.Evaluate(snapshot, requests);
        Decision(result[0], ShadowOutcome.WouldReserve, ShadowReasonCode.None);
        Assert(result[0].ResourceKeys.Length == 1, "Successful analysis must identify its model resources.");
        result[0].ResourceKeys[0] = "changed";
        Assert(result[0].ResourceKeys[0] == "block", "A result must not expose its internal keys.");
        result[0] = null;
        Assert(RailwayShadowAnalyzer.Evaluate(snapshot, requests)[0] != null, "Do not reuse a result array.");
    }

    private static void NullAndEmpty()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("block"));
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "block") };
        Invalid(null, requests, ShadowReasonCode.InvalidRequest);
        Invalid(snapshot, null, ShadowReasonCode.InvalidRequest);
        Invalid(snapshot, new MovementRequest[0], ShadowReasonCode.InvalidRequest);
        Assert(RailwayShadowAnalyzer.Evaluate(snapshot, null).Length == 0, "Null requests produce no grant.");
        Assert(RailwayShadowAnalyzer.Evaluate(snapshot, new MovementRequest[0]).Length == 0, "Empty requests produce no grant.");
        Decision(RailwayShadowAnalyzer.Evaluate(null, requests)[0], ShadowOutcome.Unknown, ShadowReasonCode.InvalidRequest);
    }

    private static void InvalidMembers()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("block"));
        Invalid(snapshot, new MovementRequest[] { null }, ShadowReasonCode.InvalidRequest);
        Invalid(snapshot, new MovementRequest[] { Request("", "train-a", "block") }, ShadowReasonCode.InvalidRequest);
        Invalid(snapshot, new MovementRequest[] { Request("first", "", "block") }, ShadowReasonCode.InvalidRequest);
        Invalid(snapshot, new MovementRequest[] { Request("first", "train-a", new string[0]) }, ShadowReasonCode.InvalidRequest);
        Invalid(snapshot, new MovementRequest[] { Request("first", "train-a", new string[] { null }) }, ShadowReasonCode.InvalidRequest);
        Invalid(Snapshot(new ResourceEvidence[] { null }), new MovementRequest[] { Request("first", "train-a", "block") }, ShadowReasonCode.InvalidRequest);
        Invalid(Snapshot(new ResourceEvidence("block", EvidenceState.Clear, null, true, new string[0])),
            new MovementRequest[] { Request("first", "train-a", "block") }, ShadowReasonCode.InvalidRequest);
    }

    private static void DuplicateIdentities()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("block"));
        Invalid(snapshot, new MovementRequest[] { Request("same", "train-a", "block"), Request("same", "train-b", "block") }, ShadowReasonCode.InvalidRequest);
        Invalid(snapshot, new MovementRequest[] { Request("one", "train-a", "block"), Request("two", "train-a", "block") }, ShadowReasonCode.InvalidRequest);
        Invalid(Snapshot(Clear("block"), Clear("block")), new MovementRequest[] { Request("first", "train-a", "block") }, ShadowReasonCode.InvalidRequest);
    }

    private static void IncompleteEvidence()
    {
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "block") };
        Invalid(MakeSnapshot(1, 1, "topology-1", 1, false, NetworkSupport.SupportedDedicatedRail,
            new ResourceEvidence[] { Clear("block") }, new VehicleEvidence[] { Vehicle("train-a") }), requests, ShadowReasonCode.IncompleteCapture);
        Invalid(MakeSnapshot(1, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            new ResourceEvidence[] { Clear("block") }, new VehicleEvidence[] { new VehicleEvidence("train-a", true, false) }), requests, ShadowReasonCode.IncompleteCapture);
    }

    private static void OccupancyConsistency()
    {
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "block") };
        Invalid(Snapshot(new ResourceEvidence("block", EvidenceState.Clear, new string[0], true, new string[] { "train-b" })), requests, ShadowReasonCode.IncompleteCapture);
        Invalid(Snapshot(new ResourceEvidence("block", EvidenceState.Occupied, new string[0], true, new string[0])), requests, ShadowReasonCode.IncompleteCapture);
    }

    private static void UnknownEnums()
    {
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "block") };
        Invalid(Snapshot(new ResourceEvidence("block", default(EvidenceState), new string[0], true, new string[0])), requests, ShadowReasonCode.IncompleteCapture);
        Invalid(Snapshot(new ResourceEvidence("block", (EvidenceState)99, new string[0], true, new string[0])), requests, ShadowReasonCode.IncompleteCapture);
        Invalid(MakeSnapshot(1, 1, "topology-1", 1, true, default(NetworkSupport), new ResourceEvidence[] { Clear("block") },
            new VehicleEvidence[] { Vehicle("train-a") }), requests, ShadowReasonCode.InvalidTopology);
    }

    private static void UnsupportedNetwork()
    {
        RailwaySnapshot snapshot = MakeSnapshot(1, 1, "topology-1", 1, true, NetworkSupport.Unsupported,
            new ResourceEvidence[] { Clear("block") }, new VehicleEvidence[] { Vehicle("train-a") });
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "block") };
        Invalid(snapshot, requests, ShadowOutcome.Unsupported, ShadowReasonCode.UnsupportedNetwork);
        Decision(RailwayShadowAnalyzer.Evaluate(snapshot, requests)[0], ShadowOutcome.Unsupported, ShadowReasonCode.UnsupportedNetwork);
    }

    private static void MalformedGraph()
    {
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "left") };
        Invalid(Snapshot(Clear("left", "right"), Clear("right")), requests, ShadowReasonCode.InvalidTopology);
        Invalid(Snapshot(Clear("left", "missing")), requests, ShadowReasonCode.InvalidTopology);
        Invalid(Snapshot(new ResourceEvidence("left", EvidenceState.Clear, new string[0], false, new string[0])), requests, ShadowReasonCode.InvalidTopology);
    }

    private static void MissingResources()
    {
        Invalid(Snapshot(Clear("block")), new MovementRequest[] { Request("first", "train-a", "missing") }, ShadowReasonCode.InvalidTopology);
    }

    private static void IdentityVerification()
    {
        MovementRequest[] requests = new MovementRequest[] { Request("first", "train-a", "block") };
        Invalid(MakeSnapshot(0, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            new ResourceEvidence[] { Clear("block") }, new VehicleEvidence[] { Vehicle("train-a") }), requests, ShadowReasonCode.InvalidIdentity);
        Invalid(MakeSnapshot(1, 0, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            new ResourceEvidence[] { Clear("block") }, new VehicleEvidence[] { Vehicle("train-a") }), requests, ShadowReasonCode.InvalidIdentity);
        Invalid(MakeSnapshot(1, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            new ResourceEvidence[] { Clear("block") }, new VehicleEvidence[] { new VehicleEvidence("train-a", false, true) }), requests, ShadowReasonCode.InvalidIdentity);
        Invalid(Snapshot(Clear("block")), new MovementRequest[] { Request("first", "unobserved", "block") }, ShadowReasonCode.InvalidIdentity);
        Invalid(Snapshot(Occupied("block", "unobserved")), requests, ShadowReasonCode.InvalidIdentity);
    }

    private static void StaleCapture()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("block"));
        Invalid(snapshot, new MovementRequest[] { new MovementRequest("first", "train-a", 2, 1, "topology-1", new string[] { "block" }) }, ShadowReasonCode.StaleCapture);
        Invalid(snapshot, new MovementRequest[] { new MovementRequest("first", "train-a", 1, 2, "topology-1", new string[] { "block" }) }, ShadowReasonCode.StaleCapture);
    }

    private static void TopologyMismatch()
    {
        Invalid(Snapshot(Clear("block")), new MovementRequest[] { new MovementRequest("first", "train-a", 1, 1, "rebuilt", new string[] { "block" }) }, ShadowReasonCode.InvalidTopology);
    }

    private static void ValidationPrecedence()
    {
        RailwaySnapshot invalid = MakeSnapshot(0, 1, "topology-1", 1, false, NetworkSupport.Unsupported,
            new ResourceEvidence[] { Clear("block", "missing") }, new VehicleEvidence[] { Vehicle("train-a") });
        Invalid(invalid, new MovementRequest[] { Request("", "train-a", "block") }, ShadowReasonCode.InvalidRequest);
        Invalid(invalid, new MovementRequest[] { Request("first", "train-a", "block") }, ShadowReasonCode.IncompleteCapture);
        RailwaySnapshot unsupported = MakeSnapshot(0, 1, "topology-1", 1, true, NetworkSupport.Unsupported,
            new ResourceEvidence[] { Clear("block", "missing") }, new VehicleEvidence[] { Vehicle("train-a") });
        Invalid(unsupported, new MovementRequest[] { Request("first", "train-a", "block") }, ShadowOutcome.Unsupported, ShadowReasonCode.UnsupportedNetwork);
        Invalid(MakeSnapshot(0, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            new ResourceEvidence[] { Clear("block", "missing") }, new VehicleEvidence[] { Vehicle("train-a") }),
            new MovementRequest[] { Request("first", "train-a", "block") }, ShadowReasonCode.InvalidTopology);
    }

    private static void OpposingRoutes()
    {
        ShadowDecision[] result = RailwayShadowAnalyzer.Evaluate(Snapshot(Clear("single-track")),
            new MovementRequest[] { Request("eastbound", "train-a", "single-track"), Request("westbound", "train-b", "single-track") });
        Decision(result[0], ShadowOutcome.WouldReserve, ShadowReasonCode.None);
        Decision(result[1], ShadowOutcome.Conflict, ShadowReasonCode.ReservedConflict);
    }

    private static void CrossingLanes()
    {
        ShadowDecision[] result = RailwayShadowAnalyzer.Evaluate(Snapshot(Clear("lane-a", "lane-b"), Clear("lane-b", "lane-a")),
            new MovementRequest[] { Request("first", "train-a", "lane-a"), Request("second", "train-b", "lane-b") });
        Decision(result[0], ShadowOutcome.WouldReserve, ShadowReasonCode.None);
        Decision(result[1], ShadowOutcome.Conflict, ShadowReasonCode.ReservedConflict);
    }

    private static void DirectOccupancy()
    {
        Decision(RailwayShadowAnalyzer.Evaluate(Snapshot(Occupied("block", "train-b")),
            new MovementRequest[] { Request("first", "train-a", "block") })[0], ShadowOutcome.Conflict, ShadowReasonCode.OccupiedResource);
    }

    private static void NeighborOccupancy()
    {
        Decision(RailwayShadowAnalyzer.Evaluate(Snapshot(Clear("left", "right"), Occupied("right", "train-b", "left")),
            new MovementRequest[] { Request("first", "train-a", "left") })[0], ShadowOutcome.Conflict, ShadowReasonCode.OccupiedResource);
    }

    private static void OwnStartingBlock()
    {
        RailwaySnapshot snapshot = Snapshot(Occupied("start", "train-a"), Clear("exit"));
        ShadowDecision[] result = RailwayShadowAnalyzer.Evaluate(snapshot,
            new MovementRequest[] { Request("leaving", "train-a", "start", "exit"), Request("following", "train-b", "start") });
        Decision(result[0], ShadowOutcome.WouldReserve, ShadowReasonCode.None);
        Decision(result[1], ShadowOutcome.Conflict, ShadowReasonCode.OccupiedResource);
        Assert(snapshot.Resources[0].State == EvidenceState.Occupied, "The leading train's request cannot clear its occupied block.");
        Assert(snapshot.Resources[0].OccupantKeys[0] == "train-a", "The tail remains attributed to its train.");
    }

    private static void AtomicConflict()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("free"), Occupied("busy", "train-c"));
        ShadowDecision[] result = RailwayShadowAnalyzer.Evaluate(snapshot,
            new MovementRequest[] { Request("failed", "train-a", "free", "busy"), Request("later", "train-b", "free") });
        Decision(result[0], ShadowOutcome.Conflict, ShadowReasonCode.OccupiedResource);
        Decision(result[1], ShadowOutcome.WouldReserve, ShadowReasonCode.None);
    }

    private static void FreshCapture()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("block"));
        RailwayShadowAnalyzer.Evaluate(snapshot, new MovementRequest[] { Request("first", "train-a", "block") });
        RailwaySnapshot fresh = MakeSnapshot(1, 2, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            new ResourceEvidence[] { Clear("block") }, snapshot.Occupants);
        Decision(RailwayShadowAnalyzer.Evaluate(fresh, new MovementRequest[] {
            new MovementRequest("fresh", "train-b", 1, 2, "topology-1", new string[] { "block" }) })[0],
            ShadowOutcome.WouldReserve, ShadowReasonCode.None);
        Decision(RailwayShadowAnalyzer.Evaluate(fresh, new MovementRequest[] { Request("old", "train-a", "block") })[0],
            ShadowOutcome.Unknown, ShadowReasonCode.StaleCapture);
    }

    private static void FailurePropagation()
    {
        ShadowDecision[] result = RailwayShadowAnalyzer.Evaluate(Snapshot(Clear("block")),
            new MovementRequest[] { Request("duplicate", "train-a", "block"), Request("duplicate", "train-b", "block") });
        Assert(result.Length == 2, "Invalid batches must not silently drop supplied requests.");
        for (int i = 0; i < result.Length; i++) Decision(result[i], ShadowOutcome.Unknown, ShadowReasonCode.InvalidRequest);
    }

    private static void DeterministicOrdering()
    {
        RailwaySnapshot snapshot = Snapshot(Clear("block"));
        MovementRequest first = Request("first", "train-a", "block");
        MovementRequest second = Request("second", "train-b", "block");
        MovementRequest[] requests = new MovementRequest[] { first, second };
        ShadowDecision[] a = RailwayShadowAnalyzer.Evaluate(snapshot, requests);
        ShadowDecision[] b = RailwayShadowAnalyzer.Evaluate(snapshot, requests);
        Assert(a[0].Outcome == b[0].Outcome && a[1].Outcome == b[1].Outcome, "Equal input must produce equal outcomes.");
        Assert(a[0].ReasonCode == b[0].ReasonCode && a[1].ReasonCode == b[1].ReasonCode, "Equal input must produce equal reasons.");
        Assert(requests[0] == first && requests[1] == second, "The caller's request ordering must be unchanged.");
        Assert(snapshot.Resources[0].State == EvidenceState.Clear && snapshot.Resources[0].OccupantKeys.Length == 0, "Analysis cannot change observed occupancy.");
        ShadowDecision[] reversed = RailwayShadowAnalyzer.Evaluate(snapshot, new MovementRequest[] { second, first });
        Assert(reversed[0].RequestId == "second", "Analysis follows the supplied ordering.");
        Decision(reversed[0], ShadowOutcome.WouldReserve, ShadowReasonCode.None);
        Decision(reversed[1], ShadowOutcome.Conflict, ShadowReasonCode.ReservedConflict);
    }

    private static void RepeatedResource()
    {
        ShadowDecision[] result = RailwayShadowAnalyzer.Evaluate(Snapshot(Clear("block")),
            new MovementRequest[] { Request("first", "train-a", "block", "block") });
        Decision(result[0], ShadowOutcome.WouldReserve, ShadowReasonCode.None);
        Assert(result[0].ResourceKeys.Length == 1 && result[0].ResourceKeys[0] == "block", "Resource aliases within a request are deduplicated.");
    }

    private static ResourceEvidence[] Resources(int count)
    {
        ResourceEvidence[] values = new ResourceEvidence[count];
        for (int i = 0; i < count; i++) values[i] = Clear("r" + i);
        return values;
    }

    private static VehicleEvidence[] Vehicles(int count)
    {
        VehicleEvidence[] values = new VehicleEvidence[count];
        for (int i = 0; i < count; i++) values[i] = Vehicle("v" + i);
        return values;
    }

    private static MovementRequest[] Requests(int count)
    {
        MovementRequest[] values = new MovementRequest[count];
        for (int i = 0; i < count; i++) values[i] = Request("q" + i, "v" + i, "r0");
        return values;
    }

    private static void InclusiveLimits()
    {
        ResourceEvidence[] resources = Resources(256);
        VehicleEvidence[] vehicles = Vehicles(64);
        MovementRequest[] requests = Requests(64);
        string[] needed = new string[128];
        for (int i = 0; i < needed.Length; i++) needed[i] = "r" + i;
        requests[0] = Request("q0", "v0", needed);
        ValidationReport report = RailwaySnapshotValidator.Validate(MakeSnapshot(1, 1, "topology-1", 128, true,
            NetworkSupport.SupportedDedicatedRail, resources, vehicles), requests);
        Assert(report.IsValid, "Exact segment, vehicle, resource, request and per-request limits are inclusive.");
    }

    private static void OversizedCapture()
    {
        MovementRequest[] requests = Requests(1);
        Invalid(MakeSnapshot(1, 1, "topology-1", 129, true, NetworkSupport.SupportedDedicatedRail,
            Resources(1), Vehicles(1)), requests, ShadowReasonCode.IncompleteCapture);
        Invalid(MakeSnapshot(1, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            Resources(257), Vehicles(1)), requests, ShadowReasonCode.IncompleteCapture);
        Invalid(MakeSnapshot(1, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            Resources(1), Vehicles(65)), requests, ShadowReasonCode.IncompleteCapture);
        Invalid(MakeSnapshot(1, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            Resources(1), Vehicles(64)), Requests(65), ShadowReasonCode.IncompleteCapture);
        string[] needed = new string[129];
        for (int i = 0; i < needed.Length; i++) needed[i] = "r" + i;
        Invalid(MakeSnapshot(1, 1, "topology-1", 1, true, NetworkSupport.SupportedDedicatedRail,
            Resources(129), Vehicles(1)), new MovementRequest[] { Request("q0", "v0", needed) }, ShadowReasonCode.IncompleteCapture);
        string[] conflicts = new string[257];
        for (int i = 0; i < conflicts.Length; i++) conflicts[i] = "neighbor";
        Invalid(Snapshot(new ResourceEvidence("block", EvidenceState.Clear, conflicts, true, new string[0]), Clear("neighbor", "block")),
            new MovementRequest[] { Request("first", "train-a", "block") }, ShadowReasonCode.IncompleteCapture);
    }

    private static CaptureTicket Begin(RailwayObservationSession session)
    {
        CaptureTicket ticket;
        Assert(session.TryBeginCapture("observed-topology", out ticket) == CaptureStartResult.Started, "Capture must start.");
        Assert(ticket != null, "A started capture must have a ticket.");
        return ticket;
    }

    private static RailwaySnapshot ObservationSnapshot(CaptureTicket ticket)
    {
        // Synthetic lifecycle fixture: this deliberately contains no complete live occupancy evidence.
        return new RailwaySnapshot(ticket.CityEpoch, ticket.CaptureSerial, ticket.TopologyToken, 123,
            1, false, NetworkSupport.Unknown,
            new ResourceEvidence[] { new ResourceEvidence("unverified", EvidenceState.Unknown, new string[0], false, new string[0]) },
            new VehicleEvidence[0]);
    }

    private static RailwayObservationSession ActiveSession()
    {
        RailwayObservationSession session = new RailwayObservationSession();
        Assert(session.ActivateCity(), "A local city lifecycle must activate.");
        return session;
    }

    private static void ObservationStartsDisabled()
    {
        RailwayObservationSession session = new RailwayObservationSession();
        CaptureTicket ticket;
        Assert(session.State == ObservationState.Disabled && session.CurrentSnapshot == null, "No city or data is inferred at construction.");
        Assert(session.TryBeginCapture("scope", out ticket) == CaptureStartResult.Inactive && ticket == null, "No capture before explicit activation.");
    }

    private static void ObservationActivation()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket ticket;
        Assert(session.State == ObservationState.NoSnapshot && session.CurrentSnapshot == null, "Activation cannot manufacture data.");
        Assert(session.TryBeginCapture(" ", out ticket) == CaptureStartResult.InvalidScope && ticket == null, "No fabricated topology token.");
        ticket = Begin(session);
        Assert(ticket.CityEpoch > 0 && ticket.CaptureSerial > 0, "Local counters must be positive.");
    }

    private static void ObservationSinglePending()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket first = Begin(session);
        CaptureTicket rejected;
        Assert(session.TryBeginCapture("second", out rejected) == CaptureStartResult.Busy && rejected == null, "Do not enqueue another capture.");
        session.Invalidate();
        CaptureTicket second = Begin(session);
        Assert(second.CityEpoch == first.CityEpoch && second.CaptureSerial > first.CaptureSerial, "Invalidation does not invent a new city or reuse a capture serial.");
    }

    private static void ObservationPreservesUnknown()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket ticket = Begin(session);
        RailwaySnapshot incomplete = ObservationSnapshot(ticket);
        Assert(session.TryComplete(ticket, incomplete) == CaptureCompletion.Accepted, "Matching callback can deliver incomplete evidence.");
        Assert(session.CurrentSnapshot == incomplete && !session.CurrentSnapshot.Complete, "Do not turn receipt into evidence completeness.");
        Assert(session.CurrentSnapshot.Network == NetworkSupport.Unknown && session.CurrentSnapshot.Resources[0].State == EvidenceState.Unknown,
            "No topology or occupancy is inferred from successful receipt.");
        Invalid(session.CurrentSnapshot, new MovementRequest[] {
            new MovementRequest("preview", "unverified-train", ticket.CityEpoch, ticket.CaptureSerial,
                ticket.TopologyToken, new string[] { "unverified" }) }, ShadowReasonCode.IncompleteCapture);
    }

    private static void ObservationInvalidation()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket old = Begin(session);
        session.Invalidate();
        Assert(session.TryComplete(old, ObservationSnapshot(old)) == CaptureCompletion.Stale, "Invalidated callbacks cannot revive a snapshot.");
        Assert(session.CurrentSnapshot == null && session.State == ObservationState.NoSnapshot, "Only local preview state is cleared.");
    }

    private static void ObservationNewerRequest()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket old = Begin(session);
        session.Invalidate();
        CaptureTicket current = Begin(session);
        Assert(session.TryComplete(old, ObservationSnapshot(old)) == CaptureCompletion.Stale, "Old response must be ignored.");
        Assert(session.State == ObservationState.Pending, "Old response cannot cancel a newer pending request.");
        Assert(session.TryComplete(current, ObservationSnapshot(current)) == CaptureCompletion.Accepted, "New request must remain usable.");
    }

    private static void ObservationCityReload()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket old = Begin(session);
        Assert(session.ActivateCity(), "A new explicit city lifecycle must activate.");
        CaptureTicket current = Begin(session);
        Assert(current.CityEpoch > old.CityEpoch, "Local city epochs must not be reused across activation.");
        Assert(session.TryComplete(old, ObservationSnapshot(old)) == CaptureCompletion.Stale, "Previous city cannot publish results.");
        Assert(session.CurrentSnapshot == null && session.State == ObservationState.Pending, "No cross-city data leak.");
    }

    private static void ObservationDisable()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket old = Begin(session);
        session.Disable();
        Assert(session.State == ObservationState.Disabled && session.CurrentSnapshot == null, "Disable drops local pending and preview state.");
        Assert(session.TryComplete(old, ObservationSnapshot(old)) == CaptureCompletion.Stale, "Disabled callback must be ignored.");
        CaptureTicket rejected;
        Assert(session.TryBeginCapture("scope", out rejected) == CaptureStartResult.Inactive, "Disable does not silently reactivate.");
        Assert(session.ActivateCity(), "Explicit activation can start a new lifecycle.");
        Assert(Begin(session).CityEpoch > old.CityEpoch, "Reactivation cannot reuse the old lifecycle.");
    }

    private static void ObservationHeaderMismatch()
    {
        for (int kind = 0; kind < 5; kind++)
        {
            RailwayObservationSession session = ActiveSession();
            CaptureTicket ticket = Begin(session);
            RailwaySnapshot wrong = kind == 4 ? null : new RailwaySnapshot(
                kind == 0 ? ticket.CityEpoch + 1 : ticket.CityEpoch,
                kind == 1 ? ticket.CaptureSerial + 1 : ticket.CaptureSerial,
                kind == 2 ? "other-topology" : ticket.TopologyToken,
                kind == 3 ? -1 : 123, 0, false, NetworkSupport.Unknown, new ResourceEvidence[0], new VehicleEvidence[0]);
            Assert(session.TryComplete(ticket, wrong) == CaptureCompletion.InvalidCapture, "Mismatched header or missing data must be rejected.");
            Assert(session.CurrentSnapshot == null && session.State == ObservationState.NoSnapshot, "Invalid capture must not leave stale data or a stuck pending request.");
            Begin(session);
        }
    }

    private static void ObservationForeignTicket()
    {
        RailwayObservationSession first = ActiveSession();
        RailwayObservationSession second = ActiveSession();
        CaptureTicket foreign = Begin(first);
        CaptureTicket own = Begin(second);
        Assert(foreign.CityEpoch == own.CityEpoch && foreign.CaptureSerial == own.CaptureSerial, "Fixture must exercise equal numeric counters.");
        Assert(second.TryComplete(foreign, ObservationSnapshot(foreign)) == CaptureCompletion.Stale, "Numeric equality does not authorize another session's ticket.");
        Assert(second.TryComplete(own, ObservationSnapshot(own)) == CaptureCompletion.Accepted, "Foreign ticket cannot consume the correct ticket.");
    }

    private static void ObservationDropsOldPreview()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket old = Begin(session);
        session.TryComplete(old, ObservationSnapshot(old));
        Assert(session.CurrentSnapshot != null, "Fixture must contain an old preview.");
        Begin(session);
        Assert(session.CurrentSnapshot == null && session.State == ObservationState.Pending, "A new request makes the previous preview unavailable immediately.");
    }

    private static void ObservationSingleCompletion()
    {
        RailwayObservationSession session = ActiveSession();
        CaptureTicket ticket = Begin(session);
        RailwaySnapshot current = ObservationSnapshot(ticket);
        Assert(session.TryComplete(ticket, current) == CaptureCompletion.Accepted, "First completion must be accepted.");
        Assert(session.TryComplete(ticket, ObservationSnapshot(ticket)) == CaptureCompletion.Stale, "A callback ticket can be consumed only once.");
        Assert(session.CurrentSnapshot == current, "A duplicate completion cannot replace the accepted preview.");
    }
}
