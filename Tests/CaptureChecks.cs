// Signature: OpenAI-Codex_Realistic_Vehicle_Traffic_Control
using RealisticVehicleTrafficControl.Model;

internal static class CaptureChecks
{
    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    // Fixture storage never reads or represents an actual game installation.
    private sealed class FixtureSource : IReadOnlyRoadNetworkSource
    {
        public DataOrigin Origin { get; set; } = DataOrigin.SyntheticFixture;
        public bool IsOnSimulationThread { get; set; } = true;
        public long CityEpoch { get; set; } = 1;
        public int Reads;
        public bool ChangeNodeOnSecondRead;
        public bool ChangeEpochDuringRead;
        public bool ThrowOnRead;
        private int nodeReads;
        public RoadNodeRecord Node = new RoadNodeRecord(7, new ushort[] { 10, 20, 0, 0, 0, 0, 0, 0 });
        public readonly Dictionary<ushort, RoadSegmentRecord> Segments = new();
        public readonly Dictionary<uint, RoadLaneRecord> Lanes = new();
        public FixtureSource()
        {
            var definitions = new[] { new RoadLaneDefinition("Vehicle", null, "Forward") };
            Segments[10] = new RoadSegmentRecord(10, 1, 7, 8, 100, false, definitions);
            Segments[20] = new RoadSegmentRecord(20, 1, 7, 9, 200, false, definitions);
            Lanes[100] = new RoadLaneRecord(100, 10, 0);
            Lanes[200] = new RoadLaneRecord(200, 20, 0);
        }
        public bool TryReadNode(ushort id, out RoadNodeRecord node)
        {
            Reads++; nodeReads++;
            if (ThrowOnRead) throw new InvalidOperationException("fixture source failure");
            if (ChangeEpochDuringRead) CityEpoch = 2;
            node = ChangeNodeOnSecondRead && nodeReads > 1
                ? new RoadNodeRecord(7, new ushort[] { 10 }) : Node;
            return id == node.NodeId;
        }
        public bool TryReadSegment(ushort id, out RoadSegmentRecord segment)
        {
            Reads++; return Segments.TryGetValue(id, out segment);
        }
        public bool TryReadLane(uint id, out RoadLaneRecord lane)
        {
            Reads++; return Lanes.TryGetValue(id, out lane);
        }
    }

    private static RoadCaptureResult Capture(FixtureSource source) => new ReadOnlyJunctionCapture().Capture(source, 1, 7);
    public static int RunAll()
    {
        var checks = new (string, Action)[] {
            ("RawCapturePreservesFixtureOriginAndGeneration", () => {
                var result=Capture(new FixtureSource());
                Expect(result.Status==RoadCaptureStatus.CapturedRaw,"Raw capture refused");
                Expect(result.Observation.Origin==DataOrigin.SyntheticFixture,"Synthetic data labelled live");
                Expect(result.Observation.Segments[0].Segment.BuildIndex==1,"Segment generation lost");
                Expect(result.Observation.Segments[0].Lanes[0].LaneId==100,"Live lane identity lost");
            }),
            ("UnknownSourceDoesNotReadBuffers", () => {
                var source=new FixtureSource { Origin=DataOrigin.Unknown }; var result=Capture(source);
                Expect(result.Status==RoadCaptureStatus.Unknown && source.Reads==0,"Unknown source was read");
            }),
            ("WrongThreadDoesNotReadBuffers", () => {
                var source=new FixtureSource { IsOnSimulationThread=false }; var result=Capture(source);
                Expect(result.Status==RoadCaptureStatus.Unsupported && source.Reads==0,"Wrong-thread buffer read");
            }),
            ("WrongCityDoesNotReadBuffers", () => {
                var source=new FixtureSource { CityEpoch=2 }; var result=Capture(source);
                Expect(result.Status==RoadCaptureStatus.Invalidated && source.Reads==0,"Wrong-city buffer read");
            }),
            ("CityChangedDuringCaptureDropsEverything", () => {
                var result=Capture(new FixtureSource { ChangeEpochDuringRead=true });
                Expect(result.Status==RoadCaptureStatus.Invalidated && result.Observation==null,"Partial cross-city data escaped");
            }),
            ("TopologyChangedDuringCaptureDropsEverything", () => {
                var result=Capture(new FixtureSource { ChangeNodeOnSecondRead=true });
                Expect(result.Status==RoadCaptureStatus.Invalidated && result.Observation==null,"Stale topology escaped");
            }),
            ("LaneCycleIsBoundedAndRefused", () => {
                var source=new FixtureSource(); source.Lanes[100]=new RoadLaneRecord(100,10,100);
                var result=Capture(source);
                Expect(result.Status==RoadCaptureStatus.Unsupported && result.Observation==null && source.Reads<12,"Lane cycle was traversed or exposed");
            }),
            ("LaneInAnotherSegmentIsRefused", () => {
                var source=new FixtureSource(); source.Lanes[100]=new RoadLaneRecord(100,20,0);
                Expect(Capture(source).Status==RoadCaptureStatus.Unsupported,"Detached lane accepted");
            }),
            ("MissingLaneIsUnknownNotEmpty", () => {
                var source=new FixtureSource(); source.Lanes.Remove(100); var result=Capture(source);
                Expect(result.Status==RoadCaptureStatus.Unknown && result.Observation==null,"Missing lane became empty capture");
            }),
            ("DuplicateNodeSegmentIsRefused", () => {
                var source=new FixtureSource(); source.Node=new RoadNodeRecord(7,new ushort[]{10,10});
                Expect(Capture(source).Status==RoadCaptureStatus.Unsupported,"Duplicate segment accepted");
            }),
            ("UnrelatedSegmentIsRefused", () => {
                var source=new FixtureSource(); source.Segments[10]=new RoadSegmentRecord(10,1,4,5,100,false,new RoadLaneDefinition[0]);
                Expect(Capture(source).Status==RoadCaptureStatus.Unsupported,"Segment not connected to node accepted");
            }),
            ("MissingVehicleTypeRemainsUnknown", () => {
                var observation=Capture(new FixtureSource()).Observation;
                Expect(observation.Segments[0].Segment.Definitions[0].VehicleType==null,"Unknown type invented");
                Expect(!observation.HasCompleteLaneMetadata,"Unknown metadata claimed complete");
            }),
            ("LaneDefinitionMismatchDoesNotGuessIndex", () => {
                var source=new FixtureSource(); source.Segments[10]=new RoadSegmentRecord(10,1,7,8,100,false,new RoadLaneDefinition[0]);
                var result=Capture(source);
                Expect(result.Status==RoadCaptureStatus.CapturedRaw && !result.Observation.HasCompleteLaneMetadata,"Mismatch hidden or guessed");
            }),
            ("RawSnapshotIsImmutable", () => {
                ushort[] ids={10,20}; var source=new FixtureSource(); source.Node=new RoadNodeRecord(7,ids);
                var result=Capture(source); ids[0]=99;
                var rows=result.Observation.Segments; rows[0]=null;
                var nodeIds=result.Observation.Node.SegmentIds; nodeIds[0]=99;
                Expect(result.Observation.Segments[0]!=null && result.Observation.Node.SegmentIds[0]==10,"Mutable raw snapshot escaped");
            }),
            ("SourceFailureDoesNotExposePartialData", () => {
                var result=Capture(new FixtureSource { ThrowOnRead=true });
                Expect(result.Status==RoadCaptureStatus.Unknown && result.Observation==null,"Source failure leaked partial data");
            }),
            ("EmptyNodeIsNotInventedAsRoadJunction", () => {
                var source=new FixtureSource(); source.Node=new RoadNodeRecord(7,new ushort[8]);
                Expect(Capture(source).Status==RoadCaptureStatus.Unsupported,"Empty node became a road junction");
            })
        };
        int failed=0;
        foreach(var (name,run) in checks) {
            try { run(); Console.WriteLine("PASS "+name); }
            catch(Exception error) { failed++; Console.WriteLine("FAIL "+name+": "+error.Message); }
        }
        Console.WriteLine($"CAPTURE RESULT {checks.Length-failed}/{checks.Length} passed; {failed} failed. Fixture source only; no game capture.");
        return failed;
    }
}
